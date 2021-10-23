// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.Host.GenericBotHost;

namespace Telegram.Bot.Host.HostBuilderExtensions
{
    public static class GenericHostBotHostBuilderExtensions
    {
        public static IHostBuilder ConfigureBotHost(this IHostBuilder builder, Action<IBotHostBuilder> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            return builder.ConfigureBotHost(configure, _ => { });
        }

        public static IHostBuilder ConfigureBotHost(this IHostBuilder builder, Action<IBotHostBuilder> configure,
            Action<BotHostBuilderOptions> configureBotHostBuilder)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            if (configureBotHostBuilder is null) throw new ArgumentNullException(nameof(configureBotHostBuilder));

            var botHostBuilderOptions = new BotHostBuilderOptions();
            configureBotHostBuilder(botHostBuilderOptions);
            var botHostBuilder = new GenericBotHostBuilder(builder, botHostBuilderOptions);
            configure(botHostBuilder);
            builder.ConfigureServices((context, services) => services.AddHostedService<GenericBotHostService>());
            return builder;
        }
    }

    public class BotHostBuilderOptions
    {
        public bool SuppressEnvironmentConfiguration { get; set; } = false;
    }
}