// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Host.ApplicationBuilder;
using Telegram.Bot.Host.GenericBotHost;
using Telegram.Bot.Host.Hosting;
using Telegram.Bot.Host.Middleware;
using Telegram.Bot.Host.Startup;

namespace Telegram.Bot.Host.BotHost
{
    public class BotHostBuilder : IBotHostBuilder
    {
        private readonly IConfiguration _config;
        private readonly BotHostBuilderContext _context;
        private readonly HostingEnvironment _hostingEnvironment;
        private bool _botHostBuilt;
        private Action<BotHostBuilderContext, IConfigurationBuilder> _configureAppConfigurationBuilder;
        private Action<BotHostBuilderContext, IServiceCollection> _configureServices;

        private BotHostOptions _options;

        public BotHostBuilder()
        {
            _hostingEnvironment = new HostingEnvironment();

            _config = new ConfigurationBuilder()
                .AddEnvironmentVariables("Telegram.Bot.Host_")
                .Build();

            if (string.IsNullOrEmpty(GetSetting(BotHostDefaults.EnvironmentKey)))
                UseSetting(BotHostDefaults.EnvironmentKey, Environment.GetEnvironmentVariable("Hosting:Environment")
                                                           ?? Environment.GetEnvironmentVariable("ASPNET_ENV"));

            _context = new BotHostBuilderContext
            {
                Configuration = _config
            };
        }

        public string GetSetting(string key)
        {
            return _config[key];
        }

        public IBotHostBuilder UseSetting(string key, string value)
        {
            _config[key] = value;
            return this;
        }

        public IBotHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            if (configureServices == null) throw new ArgumentNullException(nameof(configureServices));

            return ConfigureServices((_, services) => configureServices(services));
        }

        public IBotHostBuilder ConfigureServices(Action<BotHostBuilderContext, IServiceCollection> configureServices)
        {
            _configureServices += configureServices;
            return this;
        }

        public IBotHostBuilder ConfigureAppConfiguration(
            Action<BotHostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _configureAppConfigurationBuilder += configureDelegate;
            return this;
        }

        public IBotHost Build()
        {
            if (_botHostBuilt) throw new InvalidOperationException("Resources.BotHostBuilder_SingleInstance");
            _botHostBuilt = true;

            var hostingServices = BuildCommonServices(out var hostingStartupErrors);
            var applicationServices = hostingServices.Clone();
            var hostingServiceProvider = GetProviderFromFactory(hostingServices);

            if (!_options!.SuppressStatusMessages)
            {
                if (Environment.GetEnvironmentVariable("Hosting:Environment") != null)
                    Console.WriteLine(
                        "The environment variable 'Hosting:Environment' is obsolete and has been replaced with 'Telegram.Bot.Host_ENVIRONMENT'");

                if (Environment.GetEnvironmentVariable("ASPNET_ENV") != null)
                    Console.WriteLine(
                        "The environment variable 'ASPNET_ENV' is obsolete and has been replaced with 'Telegram.Bot.Host_ENVIRONMENT'");

                if (Environment.GetEnvironmentVariable("Telegram.Bot.Host_SERVER.URLS") != null)
                    Console.WriteLine(
                        "The environment variable 'Telegram.Bot.Host_SERVER.URLS' is obsolete and has been replaced with 'Telegram.Bot.Host_URLS'");
            }

            AddApplicationServices(applicationServices, hostingServiceProvider);

            var host = new BotHost(
                applicationServices,
                hostingServiceProvider,
                _options,
                hostingStartupErrors);
            try
            {
                host.Initialize();

                _ = host.Services.GetService<IConfiguration>();

                var logger = host.Services.GetRequiredService<ILogger<BotHost>>();

                foreach (var assemblyName in _options.GetFinalHostingStartupAssemblies()
                    .GroupBy(a => a, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
                    logger.LogWarning(
                        $"The assembly {assemblyName} was specified multiple times. Hosting startup assemblies should only be specified once.");

                return host;
            }
            catch
            {
                host.Dispose();
                throw;
            }

            IServiceProvider GetProviderFromFactory(IServiceCollection collection)
            {
                var provider = collection.BuildServiceProvider();
                var factory = provider.GetService<IServiceProviderFactory<IServiceCollection>>();

                if (factory is null or DefaultServiceProviderFactory)
                    return provider;

                using (provider)
                {
                    return factory.CreateServiceProvider(factory.CreateBuilder(collection));
                }
            }
        }

        [MemberNotNull(nameof(_options))]
        private IServiceCollection BuildCommonServices(out AggregateException hostingStartupErrors)
        {
            hostingStartupErrors = null;

            _options = new BotHostOptions(_config, Assembly.GetEntryAssembly()?.GetName().Name);

            if (!_options.PreventHostingStartup)
            {
                var exceptions = new List<Exception>();

                foreach (var assemblyName in _options.GetFinalHostingStartupAssemblies()
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                    try
                    {
                        var assembly = Assembly.Load(new AssemblyName(assemblyName));

                        foreach (var attribute in assembly.GetCustomAttributes<HostingStartupAttribute>())
                        {
                            var hostingStartup =
                                (IHostingStartup)Activator.CreateInstance(attribute.HostingStartupType)!;
                            Debug.Assert(hostingStartup != null, nameof(hostingStartup) + " != null");
                            hostingStartup.Configure(this);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(new InvalidOperationException(
                            $"Startup assembly {assemblyName} failed to execute. See the inner exception for more details.",
                            ex));
                    }

                if (exceptions.Count > 0) hostingStartupErrors = new AggregateException(exceptions);
            }

            var contentRootPath = ResolveContentRootPath(_options.ContentRootPath, AppContext.BaseDirectory);

            _hostingEnvironment.Initialize(contentRootPath, _options);
            _context.HostingEnvironment = _hostingEnvironment;

            var services = new ServiceCollection();
            services.AddSingleton(_options!);
            services.AddSingleton<IHostEnvironment>(_hostingEnvironment);
            services.AddSingleton<IHostEnvironment>(_hostingEnvironment);
            services.AddSingleton(_context);

            var builder = new ConfigurationBuilder()
                .SetBasePath(_hostingEnvironment.ContentRootPath)
                .AddConfiguration(_config, true);

            _configureAppConfigurationBuilder?.Invoke(_context, builder);

            var configuration = builder.Build();
            services.AddSingleton<IConfiguration>(_ => configuration);
            _context.Configuration = configuration;

            var listener = new DiagnosticListener("Telegram.Bot.Host");
            services.AddSingleton(listener);
            services.AddSingleton<DiagnosticSource>(listener);

            services.AddTransient<IApplicationBuilderFactory, ApplicationBuilderFactory>();
            services.AddScoped<IMiddlewareFactory, MiddlewareFactory>();
            services.AddOptions();
            services.AddLogging();

            services.AddTransient<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();

            if (!string.IsNullOrEmpty(_options.StartupAssembly))
                try
                {
                    var startupType =
                        StartupLoader.FindStartupType(_options.StartupAssembly, _hostingEnvironment.EnvironmentName);

                    if (typeof(IStartup).GetTypeInfo().IsAssignableFrom(startupType.GetTypeInfo()))
                        services.AddSingleton(typeof(IStartup), startupType);
                    else
                        services.AddSingleton(typeof(IStartup), sp =>
                        {
                            var hostingEnvironment = sp.GetRequiredService<IHostEnvironment>();
                            var methods =
                                StartupLoader.LoadMethods(sp, startupType, hostingEnvironment.EnvironmentName);
                            return new ConventionBasedStartup(methods);
                        });
                }
                catch (Exception ex)
                {
                    var capture = ExceptionDispatchInfo.Capture(ex);
                    services.AddSingleton<IStartup>(_ =>
                    {
                        capture.Throw();
                        return null;
                    });
                }

            _configureServices?.Invoke(_context, services);

            return services;
        }

        private static void AddApplicationServices(IServiceCollection services, IServiceProvider hostingServiceProvider)
        {
            var listener = hostingServiceProvider.GetService<DiagnosticListener>();
            services.Replace(ServiceDescriptor.Singleton(typeof(DiagnosticListener), listener!));
            services.Replace(ServiceDescriptor.Singleton(typeof(DiagnosticSource), listener!));
        }

        private static string ResolveContentRootPath(string contentRootPath, string basePath)
        {
            if (string.IsNullOrEmpty(contentRootPath)) return basePath;
            return Path.IsPathRooted(contentRootPath)
                ? contentRootPath
                : Path.Combine(Path.GetFullPath(basePath), contentRootPath);
        }
    }

    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection Clone(this IServiceCollection serviceCollection)
        {
            IServiceCollection clone = new ServiceCollection();
            foreach (var service in serviceCollection) clone.Add(service);
            return clone;
        }
    }
}