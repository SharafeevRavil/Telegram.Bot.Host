// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using Microsoft.Extensions.DependencyInjection;

namespace TelegramBot.Host.Middleware
{
    public class MiddlewareFactory : IMiddlewareFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public MiddlewareFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IMiddleware Create(Type middlewareType)
        {
            return _serviceProvider.GetRequiredService(middlewareType) as IMiddleware;
        }

        public void Release(IMiddleware middleware)
        {
        }
    }
}