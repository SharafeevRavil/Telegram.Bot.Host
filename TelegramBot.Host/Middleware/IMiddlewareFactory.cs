// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;

namespace TelegramBot.Host.Middleware
{
    public interface IMiddlewareFactory
    {
        IMiddleware Create(Type middlewareType);

        void Release(IMiddleware middleware);
    }
}