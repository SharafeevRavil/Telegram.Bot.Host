// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBot.Host.BotApplication
{
    public interface IBotApplication<TContext> where TContext : notnull
    {
        TContext CreateContext(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken);

        Task ProcessRequestAsync(TContext context);

        void DisposeContext(TContext context, Exception exception);
    }
}