// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using Microsoft.Extensions.Hosting;
using TelegramBotHost.BotServer;
using TelegramBotHost.GenericBotHost;

namespace TelegramBotHost.HostBuilderExtensions
{
    public static class GenericHostBuilderExtensions
    {
        public static IHostBuilder ConfigureBotHostDefaults(this IHostBuilder builder,
            Action<IBotHostBuilder> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            return builder.ConfigureBotHost(botHostBuilder =>
            {
                ConfigureBotDefaults(botHostBuilder);

                configure(botHostBuilder);
            });
        }

        private static void ConfigureBotDefaults(IBotHostBuilder builder)
        {
            builder.UseBotServer()
                .ConfigureServices((hostingContext, services) => { });
        }
    }
}