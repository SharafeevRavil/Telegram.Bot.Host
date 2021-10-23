// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using Telegram.Bot.Host.Middleware;

namespace Telegram.Bot.Host.ApplicationBuilder
{
    public interface IApplicationBuilder
    {
        IServiceProvider ApplicationServices { get; set; }
        IApplicationBuilder Use(Func<BotUpdateDelegate, BotUpdateDelegate> middleware);
        IApplicationBuilder New();

        BotUpdateDelegate Build();
    }
}