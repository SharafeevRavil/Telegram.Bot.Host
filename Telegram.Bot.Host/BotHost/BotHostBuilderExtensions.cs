// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Host.ApplicationBuilder;
using Telegram.Bot.Host.GenericBotHost;
using Telegram.Bot.Host.Startup;

namespace Telegram.Bot.Host.BotHost
{
    internal static class StartupLinkerOptions
    {
        public const DynamicallyAccessedMemberTypes Accessibility = DynamicallyAccessedMemberTypes.PublicConstructors |
                                                                    DynamicallyAccessedMemberTypes.PublicMethods;
    }

    public static class BotHostBuilderExtensions
    {
        public static IBotHostBuilder Configure(this IBotHostBuilder hostBuilder,
            Action<IApplicationBuilder> configureApp)
        {
            return hostBuilder.Configure((_, app) => configureApp(app),
                configureApp.GetMethodInfo().DeclaringType!.GetTypeInfo().Assembly.GetName().Name!);
        }

        public static IBotHostBuilder Configure(this IBotHostBuilder hostBuilder,
            Action<BotHostBuilderContext, IApplicationBuilder> configureApp)
        {
            return hostBuilder.Configure(configureApp,
                configureApp.GetMethodInfo().DeclaringType!.GetTypeInfo().Assembly.GetName().Name!);
        }

        private static IBotHostBuilder Configure(this IBotHostBuilder hostBuilder,
            Action<BotHostBuilderContext, IApplicationBuilder> configureApp, string startupAssemblyName)
        {
            if (configureApp == null) throw new ArgumentNullException(nameof(configureApp));

            hostBuilder.UseSetting(BotHostDefaults.ApplicationKey, startupAssemblyName);

            if (hostBuilder is ISupportsStartup supportsStartup) return supportsStartup.Configure(configureApp);

            return hostBuilder.ConfigureServices((context, services) =>
            {
                services.AddSingleton<IStartup>(sp =>
                {
                    return new DelegateStartup(
                        sp.GetRequiredService<IServiceProviderFactory<IServiceCollection>>(),
                        app => configureApp(context, app));
                });
            });
        }

        public static IBotHostBuilder UseStartup<
            [DynamicallyAccessedMembers(StartupLinkerOptions.Accessibility)]
            TStartup>(this IBotHostBuilder hostBuilder)
            where TStartup : class
        {
            return hostBuilder.UseStartup(typeof(TStartup));
        }

        public static IBotHostBuilder UseStartup(this IBotHostBuilder hostBuilder,
            [DynamicallyAccessedMembers(StartupLinkerOptions.Accessibility)]
            Type startupType)
        {
            if (startupType == null) throw new ArgumentNullException(nameof(startupType));

            var startupAssemblyName = startupType.GetTypeInfo().Assembly.GetName().Name;

            hostBuilder.UseSetting(BotHostDefaults.ApplicationKey, startupAssemblyName);

            if (hostBuilder is ISupportsStartup supportsStartup) return supportsStartup.UseStartup(startupType);

            return hostBuilder
                .ConfigureServices(services =>
                {
                    if (typeof(IStartup).GetTypeInfo().IsAssignableFrom(startupType.GetTypeInfo()))
                        services.AddSingleton(typeof(IStartup), startupType);
                    else
                        services.AddSingleton(typeof(IStartup), sp =>
                        {
                            var hostingEnvironment = sp.GetRequiredService<IHostEnvironment>();
                            return new ConventionBasedStartup(StartupLoader.LoadMethods(sp, startupType,
                                hostingEnvironment.EnvironmentName));
                        });
                });
        }

        public static IBotHostBuilder ConfigureLogging(this IBotHostBuilder hostBuilder,
            Action<BotHostBuilderContext, ILoggingBuilder> configureLogging)
        {
            return hostBuilder.ConfigureServices((context, collection) =>
                collection.AddLogging(builder => configureLogging(context, builder)));
        }

        public static IBotHostBuilder UseDefaultServiceProvider(this IBotHostBuilder hostBuilder,
            Action<BotHostBuilderContext, ServiceProviderOptions> configure)
        {
            if (hostBuilder is ISupportsUseDefaultServiceProvider supportsDefaultServiceProvider)
                return supportsDefaultServiceProvider.UseDefaultServiceProvider(configure);

            return hostBuilder.ConfigureServices((context, services) =>
            {
                var options = new ServiceProviderOptions();
                configure(context, options);
                services.Replace(
                    ServiceDescriptor.Singleton<IServiceProviderFactory<IServiceCollection>>(
                        new DefaultServiceProviderFactory(options)));
            });
        }
    }
}