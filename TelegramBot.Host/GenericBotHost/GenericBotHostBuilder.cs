// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using TelegramBot.Host.ApplicationBuilder;
using TelegramBot.Host.BotHost;
using TelegramBot.Host.HostBuilderExtensions;
using TelegramBot.Host.Hosting;
using TelegramBot.Host.Middleware;
using TelegramBot.Host.Startup;

namespace TelegramBot.Host.GenericBotHost
{
    internal class GenericBotHostBuilder : IBotHostBuilder, ISupportsStartup, ISupportsUseDefaultServiceProvider
    {
        private readonly IHostBuilder _builder;
        private readonly IConfiguration _config;
        private readonly object _startupKey = new();
        private HostingStartupBotHostBuilder _hostingStartupBotHostBuilder;

        private AggregateException _hostingStartupErrors;
        private object _startupObject;

        public GenericBotHostBuilder(IHostBuilder builder, BotHostBuilderOptions options)
        {
            _builder = builder;
            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection();

            if (!options.SuppressEnvironmentConfiguration) configBuilder.AddEnvironmentVariables("TelegramBot.Host_");

            _config = configBuilder.Build();

            _builder.ConfigureHostConfiguration(config =>
            {
                config.AddConfiguration(_config);

                ExecuteHostingStartups();
            });

            _builder.ConfigureAppConfiguration((context, configurationBuilder) =>
            {
                if (_hostingStartupBotHostBuilder == null) return;
                var botHostContext = GetBotHostBuilderContext(context);
                _hostingStartupBotHostBuilder.ConfigureAppConfiguration(botHostContext, configurationBuilder);
            });

            _builder.ConfigureServices((context, services) =>
            {
                var botHostContext = GetBotHostBuilderContext(context);
                var botHostOptions = (BotHostOptions)context.Properties[typeof(BotHostOptions)];

                services.AddSingleton(botHostContext.HostingEnvironment);

                services.Configure<GenericBotHostServiceOptions>(botHostServiceOptions =>
                {
                    botHostServiceOptions.BotHostOptions = botHostOptions;
                    botHostServiceOptions.HostingStartupExceptions = _hostingStartupErrors;
                });

                var listener = new DiagnosticListener("TelegramBot.Host");
                services.TryAddSingleton(listener);
                services.TryAddSingleton<DiagnosticSource>(listener);

                services.TryAddScoped<IMiddlewareFactory, MiddlewareFactory>();
                services.TryAddSingleton<IApplicationBuilderFactory, ApplicationBuilderFactory>();

                _hostingStartupBotHostBuilder?.ConfigureServices(botHostContext, services);

                if (string.IsNullOrEmpty(botHostOptions.StartupAssembly)) return;
                try
                {
                    var startupType = StartupLoader.FindStartupType(botHostOptions.StartupAssembly,
                        botHostContext.HostingEnvironment.EnvironmentName);
                    UseStartup(startupType, context, services);
                }
                catch (Exception ex) when (botHostOptions.CaptureStartupErrors)
                {
                    var capture = ExceptionDispatchInfo.Capture(ex);

                    services.Configure<GenericBotHostServiceOptions>(botHostServiceOptions =>
                    {
                        botHostServiceOptions.ConfigureApplication = app => { capture.Throw(); };
                    });
                }
            });
        }

        public IBotHost Build()
        {
            throw new NotSupportedException(
                $"Building this implementation of {nameof(IBotHostBuilder)} is not supported.");
        }

        public IBotHostBuilder ConfigureAppConfiguration(
            Action<BotHostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _builder.ConfigureAppConfiguration((context, builder) =>
            {
                var botHostBuilderContext = GetBotHostBuilderContext(context);
                configureDelegate(botHostBuilderContext, builder);
            });

            return this;
        }

        public IBotHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            return ConfigureServices((context, services) => configureServices(services));
        }

        public IBotHostBuilder ConfigureServices(Action<BotHostBuilderContext, IServiceCollection> configureServices)
        {
            _builder.ConfigureServices((context, builder) =>
            {
                var botHostBuilderContext = GetBotHostBuilderContext(context);
                configureServices(botHostBuilderContext, builder);
            });

            return this;
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

        public IBotHostBuilder UseStartup(
            [DynamicallyAccessedMembers(StartupLinkerOptions.Accessibility)]
            Type startupType)
        {
            _startupObject = startupType;

            _builder.ConfigureServices((context, services) =>
            {
                if (ReferenceEquals(_startupObject, startupType))
                    UseStartup(startupType, context, services);
            });

            return this;
        }

        public IBotHostBuilder UseStartup<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            TStartup>(
            Func<BotHostBuilderContext, TStartup> startupFactory)
        {
            _startupObject = startupFactory;

            _builder.ConfigureServices((context, services) =>
            {
                if (!ReferenceEquals(_startupObject, startupFactory)) return;
                var botHostBuilderContext = GetBotHostBuilderContext(context);
                var instance = startupFactory(botHostBuilderContext) ??
                               throw new InvalidOperationException(
                                   "The specified factory returned null startup instance.");
                UseStartup(instance.GetType(), context, services, instance);
            });

            return this;
        }

        public IBotHostBuilder Configure(Action<BotHostBuilderContext, IApplicationBuilder> configure)
        {
            _startupObject = configure;

            _builder.ConfigureServices((context, services) =>
            {
                if (ReferenceEquals(_startupObject, configure))
                    services.Configure<GenericBotHostServiceOptions>(options =>
                    {
                        var botHostBuilderContext = GetBotHostBuilderContext(context);
                        options.ConfigureApplication = app => configure(botHostBuilderContext, app);
                    });
            });

            return this;
        }

        public IBotHostBuilder UseDefaultServiceProvider(
            Action<BotHostBuilderContext, ServiceProviderOptions> configure)
        {
            _builder.UseServiceProviderFactory(context =>
            {
                var botHostBuilderContext = GetBotHostBuilderContext(context);
                var options = new ServiceProviderOptions();
                configure(botHostBuilderContext, options);
                return new DefaultServiceProviderFactory(options);
            });

            return this;
        }

        private void ExecuteHostingStartups()
        {
            var botHostOptions = new BotHostOptions(_config, Assembly.GetEntryAssembly()?.GetName().Name);

            if (botHostOptions.PreventHostingStartup)
                return;

            var exceptions = new List<Exception>();
            _hostingStartupBotHostBuilder = new HostingStartupBotHostBuilder(this);

            foreach (var assemblyName in botHostOptions.GetFinalHostingStartupAssemblies()
                .Distinct(StringComparer.OrdinalIgnoreCase))
                try
                {
                    var assembly = Assembly.Load(new AssemblyName(assemblyName));

                    foreach (var attribute in assembly.GetCustomAttributes<HostingStartupAttribute>())
                    {
                        var hostingStartup = (IHostingStartup)Activator.CreateInstance(attribute.HostingStartupType);
                        hostingStartup!.Configure(_hostingStartupBotHostBuilder);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(new InvalidOperationException(
                        $"Startup assembly {assemblyName} failed to execute. See the inner exception for more details.",
                        ex));
                }

            if (exceptions.Count > 0) _hostingStartupErrors = new AggregateException(exceptions);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2006:UnrecognizedReflectionPattern",
            Justification = "We need to call a generic method on IHostBuilder.")]
        private void UseStartup([DynamicallyAccessedMembers(StartupLinkerOptions.Accessibility)] Type startupType,
            HostBuilderContext context, IServiceCollection services, object instance = null)
        {
            var botHostBuilderContext = GetBotHostBuilderContext(context);
            var botHostOptions = (BotHostOptions)context.Properties[typeof(BotHostOptions)];

            ExceptionDispatchInfo startupError = null;
            ConfigureBuilder configureBuilder = null;

            try
            {
                if (typeof(IStartup).IsAssignableFrom(startupType))
                    throw new NotSupportedException($"{typeof(IStartup)} isn't supported");

                if (StartupLoader.HasConfigureServicesIServiceProviderDelegate(startupType,
                    context.HostingEnvironment.EnvironmentName))
                    throw new NotSupportedException(
                        $"ConfigureServices returning an {typeof(IServiceProvider)} isn't supported.");

                instance ??=
                    ActivatorUtilities.CreateInstance(new HostServiceProvider(botHostBuilderContext), startupType!);
                context.Properties[_startupKey] = instance;

                var configureServicesBuilder =
                    StartupLoader.FindConfigureServicesDelegate(startupType,
                        context.HostingEnvironment.EnvironmentName);
                var configureServices = configureServicesBuilder.Build(instance);

                configureServices(services);

                var configureContainerBuilder =
                    StartupLoader.FindConfigureContainerDelegate(startupType,
                        context.HostingEnvironment.EnvironmentName);
                if (configureContainerBuilder.MethodInfo != null)
                {
                    var containerType = configureContainerBuilder.GetContainerType();

                    var actionType = typeof(Action<,>).MakeGenericType(typeof(HostBuilderContext), containerType);

                    var configureCallback = typeof(GenericBotHostBuilder).GetMethod(nameof(ConfigureContainerImpl),
                            BindingFlags.NonPublic | BindingFlags.Instance)
                        ?.MakeGenericMethod(containerType)
                        .CreateDelegate(actionType, this);

                    typeof(IHostBuilder).GetMethod(nameof(IHostBuilder.ConfigureContainer))
                        ?.MakeGenericMethod(containerType)
                        .InvokeWithoutWrappingExceptions(_builder, new object[] { configureCallback });
                }

                configureBuilder =
                    StartupLoader.FindConfigureDelegate(startupType, context.HostingEnvironment.EnvironmentName);
            }
            catch (Exception ex) when (botHostOptions.CaptureStartupErrors)
            {
                startupError = ExceptionDispatchInfo.Capture(ex);
            }

            services.Configure<GenericBotHostServiceOptions>(options =>
            {
                options.ConfigureApplication = app =>
                {
                    startupError?.Throw();

                    if (instance != null && configureBuilder != null) configureBuilder.Build(instance)(app);
                };
            });
        }

        private void ConfigureContainerImpl<TContainer>(HostBuilderContext context, TContainer container)
        {
            var instance = context.Properties[_startupKey];
            var builder = (ConfigureContainerBuilder)context.Properties[typeof(ConfigureContainerBuilder)];
            builder.Build(instance)(container);
        }

        private static BotHostBuilderContext GetBotHostBuilderContext(HostBuilderContext context)
        {
            if (!context.Properties.TryGetValue(typeof(BotHostBuilderContext), out var contextVal))
            {
                var options = new BotHostOptions(context.Configuration, Assembly.GetEntryAssembly()?.GetName().Name);
                var botHostBuilderContext = new BotHostBuilderContext
                {
                    Configuration = context.Configuration,
                    HostingEnvironment = new HostingEnvironment()
                };
                botHostBuilderContext.HostingEnvironment.Initialize(context.HostingEnvironment.ContentRootPath,
                    options);
                context.Properties[typeof(BotHostBuilderContext)] = botHostBuilderContext;
                context.Properties[typeof(BotHostOptions)] = options;
                return botHostBuilderContext;
            }

            var botHostContext = (BotHostBuilderContext)contextVal;
            botHostContext.Configuration = context.Configuration;
            return botHostContext;
        }

        private class HostServiceProvider : IServiceProvider
        {
            private readonly BotHostBuilderContext _context;

            public HostServiceProvider(BotHostBuilderContext context)
            {
                _context = context;
            }

            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(IHostEnvironment))
                    return _context.HostingEnvironment;

                return serviceType == typeof(IConfiguration) ? _context.Configuration : null;
            }
        }
    }

    internal interface ISupportsUseDefaultServiceProvider
    {
        IBotHostBuilder UseDefaultServiceProvider(
            Action<BotHostBuilderContext, ServiceProviderOptions> configure);
    }
}