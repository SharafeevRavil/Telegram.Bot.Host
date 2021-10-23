// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TelegramBotHost.ApplicationBuilder;
using TelegramBotHost.BotApplication;
using TelegramBotHost.BotServer;
using TelegramBotHost.GenericBotHost;
using TelegramBotHost.Hosting;
using TelegramBotHost.LoggerExtensions;
using TelegramBotHost.Middleware;
using TelegramBotHost.Startup;

namespace TelegramBotHost.BotHost
{
    internal class BotHost : IBotHost, IAsyncDisposable
    {
        private readonly IServiceCollection _applicationServiceCollection;

        private readonly IServiceProvider _hostingServiceProvider;
        private readonly AggregateException _hostingStartupErrors;
        private ApplicationLifetime _applicationLifetime;

        private ExceptionDispatchInfo _applicationServicesException;
        private HostedServiceExecutor _hostedServiceExecutor;
        private ILogger _logger = NullLogger.Instance;
        private bool _startedServer;
        private IStartup _startup;

        private bool _stopped;

        public BotHost(
            IServiceCollection appServices,
            IServiceProvider hostingServiceProvider,
            BotHostOptions options,
            AggregateException hostingStartupErrors)
        {
            _hostingStartupErrors = hostingStartupErrors;
            Options = options;
            _applicationServiceCollection = appServices ?? throw new ArgumentNullException(nameof(appServices));
            _hostingServiceProvider =
                hostingServiceProvider ?? throw new ArgumentNullException(nameof(hostingServiceProvider));
            _applicationServiceCollection.AddSingleton<ApplicationLifetime>();

            _applicationServiceCollection.AddSingleton(services
                => services.GetService<ApplicationLifetime>() as IHostApplicationLifetime);
            _applicationServiceCollection.AddSingleton<HostedServiceExecutor>();
        }

        private BotHostOptions Options { get; }

        private IServer Server { get; set; }

        public async ValueTask DisposeAsync()
        {
            if (!_stopped)
                try
                {
                    await StopAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ServerShutdownException(ex);
                }

            await DisposeServiceProviderAsync(Services).ConfigureAwait(false);
            await DisposeServiceProviderAsync(_hostingServiceProvider).ConfigureAwait(false);
        }

        public IServiceProvider Services { get; private set; }

        public void Start()
        {
            StartAsync().GetAwaiter().GetResult();
        }

        public virtual async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger = Services.GetRequiredService<ILoggerFactory>().CreateLogger("TelegramBotHost.Hosting.Diagnostics");
            _logger.Starting();

            var application = BuildApplication();

            _applicationLifetime = Services.GetRequiredService<ApplicationLifetime>();
            _hostedServiceExecutor = Services.GetRequiredService<HostedServiceExecutor>();

            await _hostedServiceExecutor.StartAsync(cancellationToken).ConfigureAwait(false);

            var hostingApp = new HostingApplication(application, Services);
            await Server.StartAsync(hostingApp, cancellationToken).ConfigureAwait(false);
            _startedServer = true;

            _applicationLifetime?.NotifyStarted();


            _logger.Started();

            if (_logger.IsEnabled(LogLevel.Debug))
                foreach (var assembly in Options.GetFinalHostingStartupAssemblies())
                    _logger.LogDebug("Loaded hosting startup assembly {assemblyName}", assembly);

            if (_hostingStartupErrors != null)
                foreach (var exception in _hostingStartupErrors.InnerExceptions)
                    _logger.HostingStartupAssemblyError(exception);
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_stopped)
                return;
            _stopped = true;

            _logger.Shutdown();

            using var timeoutCts = new CancellationTokenSource(Options.ShutdownTimeout);
            var timeoutToken = timeoutCts.Token;
            cancellationToken = !cancellationToken.CanBeCanceled
                ? timeoutToken
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken).Token;

            _applicationLifetime?.StopApplication();

            if (Server != null && _startedServer) await Server.StopAsync(cancellationToken).ConfigureAwait(false);

            if (_hostedServiceExecutor != null)
                await _hostedServiceExecutor.StopAsync(cancellationToken).ConfigureAwait(false);

            _applicationLifetime?.NotifyStopped();
        }

        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        public void Initialize()
        {
            try
            {
                EnsureApplicationServices();
            }
            catch (Exception ex)
            {
                Services ??= _applicationServiceCollection.BuildServiceProvider();

                if (!Options.CaptureStartupErrors) throw;

                _applicationServicesException = ExceptionDispatchInfo.Capture(ex);
            }
        }

        private void EnsureApplicationServices()
        {
            if (Services != null) return;
            EnsureStartup();
            Services = _startup.ConfigureServices(_applicationServiceCollection);
        }

        private void EnsureStartup()
        {
            if (_startup != null)
                return;

            _startup = _hostingServiceProvider.GetService<IStartup>();

            if (_startup == null)
                throw new InvalidOperationException(
                    $"No application configured. Please specify startup via IBotHostBuilder.UseStartup, IBotHostBuilder.Configure, injecting {nameof(IStartup)} or specifying the startup assembly via {nameof(BotHostDefaults.StartupAssemblyKey)} in the web host configuration.");
        }

        private BotUpdateDelegate BuildApplication()
        {
            try
            {
                _applicationServicesException?.Throw();
                EnsureServer();

                var builderFactory = Services.GetRequiredService<IApplicationBuilderFactory>();
                var builder = builderFactory.CreateBuilder();
                builder.ApplicationServices = Services;

                var startupFilters = Services.GetService<IEnumerable<IStartupFilter>>();
                var configure = startupFilters!.Reverse()
                    .Aggregate<IStartupFilter, Action<IApplicationBuilder>>(_startup.Configure,
                        (current, filter) => filter.Configure(current));

                configure(builder);

                return builder.Build();
            }
            catch (Exception ex)
            {
                if (!Options.SuppressStatusMessages) Console.WriteLine("Application startup exception: " + ex);
                var logger = Services.GetRequiredService<ILogger<BotHost>>();
                logger.ApplicationError(ex);

                throw;
            }
        }

        private void EnsureServer()
        {
            Server ??= Services.GetRequiredService<IServer>();
        }

        private static async ValueTask DisposeServiceProviderAsync(IServiceProvider serviceProvider)
        {
            switch (serviceProvider)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
    }
}