// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System.Reflection;

namespace TelegramBot.Host.Startup
{
    internal static class MethodInfoExtensions
    {
        public static object InvokeWithoutWrappingExceptions(this MethodInfo methodInfo, object obj,
            object[] parameters)
        {
            return methodInfo.Invoke(obj, BindingFlags.DoNotWrapExceptions, null, parameters,
                null);
        }
    }
}