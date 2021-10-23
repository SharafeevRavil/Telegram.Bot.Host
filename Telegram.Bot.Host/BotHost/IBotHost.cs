// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Telegram.Bot.Host.BotHost
{
    public interface IBotHost : IDisposable
    {
        IServiceProvider Services { get; }

        void Start();

        Task StartAsync(CancellationToken cancellationToken = default);

        Task StopAsync(CancellationToken cancellationToken = default);
    }
}