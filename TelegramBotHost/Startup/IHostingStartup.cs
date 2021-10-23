// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using TelegramBotHost.GenericBotHost;

namespace TelegramBotHost.Startup
{
    public interface IHostingStartup
    {
        void Configure(IBotHostBuilder builder);
    }
}