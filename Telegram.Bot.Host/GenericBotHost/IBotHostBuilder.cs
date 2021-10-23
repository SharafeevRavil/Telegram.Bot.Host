// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Host.BotHost;

namespace Telegram.Bot.Host.GenericBotHost
{
    public interface IBotHostBuilder
    {
        IBotHost Build();

        IBotHostBuilder ConfigureAppConfiguration(
            Action<BotHostBuilderContext, IConfigurationBuilder> configureDelegate);

        IBotHostBuilder ConfigureServices(Action<IServiceCollection> configureServices);

        IBotHostBuilder ConfigureServices(Action<BotHostBuilderContext, IServiceCollection> configureServices);

        string GetSetting(string key);

        IBotHostBuilder UseSetting(string key, string value);
    }
}