// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Reflection;

namespace TelegramBot.Host.Startup
{
    internal class ConfigureContainerBuilder
    {
        public ConfigureContainerBuilder(MethodInfo configureContainerMethod)
        {
            MethodInfo = configureContainerMethod;
        }

        public MethodInfo MethodInfo { get; }

        public Action<object> Build(object instance)
        {
            return container => Invoke(instance, container);
        }

        public Type GetContainerType()
        {
            var parameters = MethodInfo.GetParameters();
            if (parameters.Length != 1)
                throw new InvalidOperationException($"The {MethodInfo.Name} method must take only one parameter.");
            return parameters[0].ParameterType;
        }

        private void Invoke(object instance, object container)
        {
        }

        private void InvokeCore(object instance, object container)
        {
            if (MethodInfo == null) return;

            var arguments = new[] { container };

            MethodInfo.InvokeWithoutWrappingExceptions(instance, arguments);
        }
    }
}