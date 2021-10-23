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
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotHost.ApplicationBuilder;
using TelegramBotHost.BotHost;
using TelegramBotHost.CommandHandlerMiddleware.CommandHandlers;
using TelegramBotHost.Middleware;

namespace TelegramBotHost.CommandHandlerMiddleware
{
    public static class CommandHandlersExtensions
    {
        public static void AddCommandHandlers(this IServiceCollection services)
        {
            var commands = ConfigureCommands(services);
            services.AddScoped(provider =>
            {
                var commandHandlersStorage = new CommandHandlersStorage(provider, commands);
                return commandHandlersStorage;
            });
        }

        public static void UseCommandHandlers(this IApplicationBuilder app)
        {
            app.UseMiddleware<CommandHandlerMiddleware>();
        }

        private static Dictionary<string, Type> ConfigureCommands(IServiceCollection services)
        {
            var commandHandlerTypes = new Dictionary<string, Type>();

            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables("TelegramBotHost_")
                .Build();
            var options = new BotHostOptions(config, Assembly.GetEntryAssembly()?.GetName().Name);

            foreach (var assemblyName in options.GetFinalHostingStartupAssemblies()
                .Distinct(StringComparer.OrdinalIgnoreCase))
                try
                {
                    var assembly = Assembly.Load(new AssemblyName(assemblyName));

                    foreach (var type in assembly.GetTypes())
                    {
                        if (!type.IsAssignableTo(typeof(ICommandHandler))) continue;
                        var attributes = type.GetCustomAttributes(typeof(BotCommandAttribute), true);
                        if (attributes.Length == 0)
                            continue;
                        if (attributes.Length > 1)
                            //multiple attributes
                            continue;

                        var attr = (BotCommandAttribute)attributes[0];
                        services.AddScoped(type);
                        commandHandlerTypes.Add(attr.CommandText, type);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Startup assembly {assemblyName} failed to execute. See the inner exception for more details.",
                        ex);
                }

            return commandHandlerTypes;
        }
    }
}