// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Host.Middleware;

namespace Telegram.Bot.Host.BotApplication
{
    internal class HostingApplication : IBotApplication<HostingApplication.Context>
    {
        private readonly BotUpdateDelegate _application;
        private readonly IServiceProvider _serviceProvider;

        public HostingApplication(BotUpdateDelegate application, IServiceProvider serviceProvider)
        {
            _application = application;
            _serviceProvider = serviceProvider;
        }

        public Context CreateContext(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            var hostContext = new Context();

            //можно фабричный метод выделить, но мне поебать
            BotUpdateContext botUpdateContext = new DefaultBotUpdateContext
            {
                Update = update,
                BotClient = botClient,
                CancellationToken = cancellationToken,

                RequestServices = _serviceProvider
            };

            hostContext.BotUpdateContext = botUpdateContext;

            return hostContext;
        }

        public Task ProcessRequestAsync(Context context)
        {
            return _application(context.BotUpdateContext);
        }

        public void DisposeContext(Context context, Exception exception)
        {
            context.Reset();
        }


        internal class Context
        {
            public BotUpdateContext BotUpdateContext { get; set; }
            public IDisposable Scope { get; set; }
            public Activity Activity { get; set; }

            public long StartTimestamp { get; set; }
            internal bool HasDiagnosticListener { get; set; }
            public bool EventLogEnabled { get; set; }

            public void Reset()
            {
                Scope = null;
                Activity = null;

                StartTimestamp = 0;
                HasDiagnosticListener = false;
                EventLogEnabled = false;
            }
        }
    }
}