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

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Host.CommandHandlerMiddleware.CommandHandlers;

namespace Telegram.Bot.Host.CommandHandlerMiddleware
{
    public class CommandHandlersStorage
    {
        private readonly Dictionary<string, Type> _commandHandlerTypes;
        private readonly IServiceProvider _serviceProvider;

        public CommandHandlersStorage(IServiceProvider serviceProvider, Dictionary<string, Type> commandHandlerTypes)
        {
            _serviceProvider = serviceProvider;
            _commandHandlerTypes = commandHandlerTypes;
        }

        public IReadOnlyDictionary<string, Type> CommandHandlerTypes => _commandHandlerTypes;

        public ICommandHandler ResolveHandler(Type handlerType)
        {
            return (ICommandHandler)_serviceProvider.GetRequiredService(handlerType);
        }
    }
}