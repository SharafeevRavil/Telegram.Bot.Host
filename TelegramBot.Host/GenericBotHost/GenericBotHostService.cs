// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelegramBot.Host.ApplicationBuilder;
using TelegramBot.Host.BotApplication;
using TelegramBot.Host.BotHost;
using TelegramBot.Host.BotServer;
using TelegramBot.Host.LoggerExtensions;
using TelegramBot.Host.Middleware;

namespace TelegramBot.Host.GenericBotHost
{
    public class GenericBotHostService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public GenericBotHostService(IOptions<GenericBotHostServiceOptions> options,
            IServer server,
            ILoggerFactory loggerFactory,
            DiagnosticListener diagnosticListener,
            IApplicationBuilderFactory applicationBuilderFactory,
            IEnumerable<IStartupFilter> startupFilters,
            IConfiguration configuration,
            IHostEnvironment hostingEnvironment,
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            Options = options.Value;
            Server = server;
            Logger = loggerFactory.CreateLogger("TelegramBot.Host.Hosting.Diagnostics");
            LifetimeLogger = loggerFactory.CreateLogger("Microsoft.Hosting.Lifetime");
            DiagnosticListener = diagnosticListener;
            ApplicationBuilderFactory = applicationBuilderFactory;
            StartupFilters = startupFilters;
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public GenericBotHostServiceOptions Options { get; }
        public IServer Server { get; }
        public ILogger Logger { get; }
        public ILogger LifetimeLogger { get; }
        public DiagnosticListener DiagnosticListener { get; }
        public IApplicationBuilderFactory ApplicationBuilderFactory { get; }
        public IEnumerable<IStartupFilter> StartupFilters { get; }
        public IConfiguration Configuration { get; }
        public IHostEnvironment HostingEnvironment { get; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            BotUpdateDelegate application = null;

            try
            {
                var configure = Options.ConfigureApplication;

                if (configure == null)
                    throw new InvalidOperationException(
                        $"No application configured. Please specify an application via IBotHostBuilder.UseStartup, IBotHostBuilder.Configure, or specifying the startup assembly via {nameof(BotHostDefaults.StartupAssemblyKey)} in the web host configuration.");

                var builder = ApplicationBuilderFactory.CreateBuilder();

                configure = StartupFilters.Reverse()
                    .Aggregate(configure, (current, filter) => filter.Configure(current));

                configure(builder);

                application = builder.Build();
            }
            catch (Exception ex)
            {
                if (!Options.BotHostOptions.CaptureStartupErrors)
                    throw;
            }

            var httpApplication = new HostingApplication(application, _serviceProvider /*, BotUpdateContextFactory*/);

            await Server.StartAsync(httpApplication, cancellationToken);

            LifetimeLogger.LogInformation("Now listening from GenericBotHostService");

            if (Logger.IsEnabled(LogLevel.Debug))
                foreach (var assembly in Options.BotHostOptions.GetFinalHostingStartupAssemblies())
                    Logger.LogDebug("Loaded hosting startup assembly {assemblyName}", assembly);

            if (Options.HostingStartupExceptions != null)
                foreach (var exception in Options.HostingStartupExceptions.InnerExceptions)
                    Logger.HostingStartupAssemblyError(exception);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Server.StopAsync(cancellationToken);
        }
    }

    public interface IStartupFilter
    {
        Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next);
    }
}