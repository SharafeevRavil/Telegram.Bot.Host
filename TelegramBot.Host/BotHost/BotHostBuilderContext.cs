// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace TelegramBot.Host.BotHost
{
    public class BotHostBuilderContext
    {
        public IHostEnvironment HostingEnvironment { get; set; } = default!;

        public IConfiguration Configuration { get; set; } = default!;
    }
}