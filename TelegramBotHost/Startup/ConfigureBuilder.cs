// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotHost.ApplicationBuilder;

namespace TelegramBotHost.Startup
{
    internal class ConfigureBuilder
    {
        public ConfigureBuilder(MethodInfo configure)
        {
            MethodInfo = configure;
        }

        public MethodInfo MethodInfo { get; }

        public Action<IApplicationBuilder> Build(object instance)
        {
            return builder => Invoke(instance, builder);
        }

        private void Invoke(object instance, IApplicationBuilder builder)
        {
            using var scope = builder.ApplicationServices.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var parameterInfos = MethodInfo.GetParameters();
            var parameters = new object[parameterInfos.Length];
            for (var index = 0; index < parameterInfos.Length; index++)
            {
                var parameterInfo = parameterInfos[index];
                if (parameterInfo.ParameterType == typeof(IApplicationBuilder))
                    parameters[index] = builder;
                else
                    try
                    {
                        parameters[index] = serviceProvider.GetRequiredService(parameterInfo.ParameterType);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(
                            $"Could not resolve a service of type '{parameterInfo.ParameterType.FullName}' for the parameter '{parameterInfo.Name}' of method '{MethodInfo.Name}' on type '{MethodInfo.DeclaringType!.FullName}'.",
                            ex);
                    }
            }

            MethodInfo.InvokeWithoutWrappingExceptions(instance, parameters);
        }
    }
}