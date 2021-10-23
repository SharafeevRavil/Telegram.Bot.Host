// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotHost.ApplicationBuilder;
using TelegramBotHost.BotHost;
using TelegramBotHost.GenericBotHost;

namespace TelegramBotHost.Startup
{
    internal class HostingStartupBotHostBuilder : IBotHostBuilder, ISupportsStartup, ISupportsUseDefaultServiceProvider
    {
        private readonly GenericBotHostBuilder _builder;
        private Action<BotHostBuilderContext, IConfigurationBuilder> _configureConfiguration;
        private Action<BotHostBuilderContext, IServiceCollection> _configureServices;

        public HostingStartupBotHostBuilder(GenericBotHostBuilder builder)
        {
            _builder = builder;
        }

        public IBotHost Build()
        {
            throw new NotSupportedException(
                $"Building this implementation of {nameof(IBotHostBuilder)} is not supported.");
        }

        public IBotHostBuilder ConfigureAppConfiguration(
            Action<BotHostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _configureConfiguration += configureDelegate;
            return this;
        }

        public IBotHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            return ConfigureServices((context, services) => configureServices(services));
        }

        public IBotHostBuilder ConfigureServices(Action<BotHostBuilderContext, IServiceCollection> configureServices)
        {
            _configureServices += configureServices;
            return this;
        }

        public string GetSetting(string key)
        {
            return _builder.GetSetting(key);
        }

        public IBotHostBuilder UseSetting(string key, string value)
        {
            _builder.UseSetting(key, value);
            return this;
        }

        public IBotHostBuilder Configure(Action<BotHostBuilderContext, IApplicationBuilder> configure)
        {
            return _builder.Configure(configure);
        }

        public IBotHostBuilder UseStartup(
            [DynamicallyAccessedMembers(StartupLinkerOptions.Accessibility)]
            Type startupType)
        {
            return _builder.UseStartup(startupType);
        }

        public IBotHostBuilder UseStartup<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            TStartup>(
            Func<BotHostBuilderContext, TStartup> startupFactory)
        {
            return _builder.UseStartup(startupFactory);
        }

        public IBotHostBuilder UseDefaultServiceProvider(
            Action<BotHostBuilderContext, ServiceProviderOptions> configure)
        {
            return _builder.UseDefaultServiceProvider(configure);
        }

        public void ConfigureServices(BotHostBuilderContext context, IServiceCollection services)
        {
            _configureServices?.Invoke(context, services);
        }

        public void ConfigureAppConfiguration(BotHostBuilderContext context, IConfigurationBuilder builder)
        {
            _configureConfiguration?.Invoke(context, builder);
        }
    }
}