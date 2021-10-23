// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Telegram.Bot.Host.Hosting
{
    internal class HostedServiceExecutor
    {
        private readonly IEnumerable<IHostedService> _services;

        public HostedServiceExecutor(IEnumerable<IHostedService> services)
        {
            _services = services;
        }

        public Task StartAsync(CancellationToken token)
        {
            return ExecuteAsync(service => service.StartAsync(token));
        }

        public Task StopAsync(CancellationToken token)
        {
            return ExecuteAsync(service => service.StopAsync(token), false);
        }

        private async Task ExecuteAsync(Func<IHostedService, Task> callback, bool throwOnFirstFailure = true)
        {
            List<Exception> exceptions = null;

            foreach (var service in _services)
                try
                {
                    await callback(service);
                }
                catch (Exception ex)
                {
                    if (throwOnFirstFailure) throw;

                    exceptions ??= new List<Exception>();

                    exceptions.Add(ex);
                }

            if (exceptions != null) throw new AggregateException(exceptions);
        }
    }
}