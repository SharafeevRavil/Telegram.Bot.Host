// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using Telegram.Bot.Host.ApplicationBuilder;

namespace Telegram.Bot.Host.Startup
{
    internal class StartupMethods
    {
        public StartupMethods(object instance, Action<IApplicationBuilder> configure)
        {
            StartupInstance = instance;
            ConfigureDelegate = configure;
        }

        public object StartupInstance { get; }
        public Action<IApplicationBuilder> ConfigureDelegate { get; }
    }
}