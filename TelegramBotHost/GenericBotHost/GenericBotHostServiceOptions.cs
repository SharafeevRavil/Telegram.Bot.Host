// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using TelegramBotHost.ApplicationBuilder;
using TelegramBotHost.BotHost;

namespace TelegramBotHost.GenericBotHost
{
    public class GenericBotHostServiceOptions
    {
        public Action<IApplicationBuilder> ConfigureApplication { get; set; }

        public BotHostOptions BotHostOptions { get; set; }

        public AggregateException HostingStartupExceptions { get; set; }
    }
}