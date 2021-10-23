// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Telegram.Bot.Host.Startup
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class HostingStartupAttribute : Attribute
    {
        public HostingStartupAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            Type hostingStartupType)
        {
            if (hostingStartupType == null) throw new ArgumentNullException(nameof(hostingStartupType));

            if (!typeof(IHostingStartup).GetTypeInfo().IsAssignableFrom(hostingStartupType.GetTypeInfo()))
                throw new ArgumentException($@"""{hostingStartupType}"" does not implement {typeof(IHostingStartup)}.",
                    nameof(hostingStartupType));

            HostingStartupType = hostingStartupType;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public Type HostingStartupType { get; }
    }
}