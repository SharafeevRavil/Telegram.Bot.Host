// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System.Threading;
using System.Threading.Tasks;
using TelegramBot.Host.BotApplication;

namespace TelegramBot.Host.BotServer
{
    public interface IServer
    {
        Task StartAsync<TContext>(IBotApplication<TContext> application, CancellationToken cancellationToken)
            where TContext : notnull;

        Task StopAsync(CancellationToken cancellationToken);
    }
}