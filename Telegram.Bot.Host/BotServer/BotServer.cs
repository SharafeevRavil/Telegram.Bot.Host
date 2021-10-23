// Copyright 2021 Sharafeev Ravil
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//     You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
//     distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     See the License for the specific language governing permissions and
// limitations under the License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Host.BotApplication;
using Telegram.Bot.Host.CommandHandlerMiddleware.CommandHandlers;
using Telegram.Bot.Host.GenericBotHost;

namespace Telegram.Bot.Host.BotServer
{
    public static class BotHostBuilderBotServerExtensions
    {
        public static IBotHostBuilder UseBotServer(this IBotHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices(services => { services.AddSingleton<IServer, BotServer>(); });
        }
    }

    public class BotServer : IServer
    {
        private readonly TelegramOptions _telegramOptions;

        private CancellationTokenSource _cancellationTokenSource;

        public BotServer(IOptions<TelegramOptions> telegramOptions)
        {
            _telegramOptions = telegramOptions.Value;
        }

        public Task StartAsync<TContext>(IBotApplication<TContext> application, CancellationToken cancellationToken)
            where TContext : notnull
        {
            var botClient = new TelegramBotClient(_telegramOptions.Token);

            _cancellationTokenSource = new CancellationTokenSource();
            var updateHandler = new UpdateHandler<TContext>(application);
            botClient.StartReceiving(
                new DefaultUpdateHandler(updateHandler.HandleUpdateAsync, ErrorHandler.HandleErrorAsync),
                cancellationToken: cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        internal class UpdateHandler<TContext>
        {
            private readonly IBotApplication<TContext> _application;

            public UpdateHandler(IBotApplication<TContext> application)
            {
                _application = application;
            }

            public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
                CancellationToken cancellationToken)
            {
                var context = _application.CreateContext(botClient, update, cancellationToken);
                await _application.ProcessRequestAsync(context);
            }
        }
    }
}