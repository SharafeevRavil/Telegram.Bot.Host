// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Host.BotHost;

namespace Telegram.Bot.Host.Startup
{
    internal class StartupLoader
    {
        public static StartupMethods LoadMethods(IServiceProvider hostingServiceProvider,
            [DynamicallyAccessedMembers(StartupLinkerOptions.Accessibility)]
            Type startupType, string environmentName,
            object instance = null)
        {
            var configureMethod = FindConfigureDelegate(startupType, environmentName);

            var servicesMethod = FindConfigureServicesDelegate(startupType, environmentName);

            if (instance == null && (!configureMethod.MethodInfo.IsStatic ||
                                     servicesMethod != null && !servicesMethod.MethodInfo.IsStatic))
                instance = ActivatorUtilities.GetServiceOrCreateInstance(hostingServiceProvider, startupType);

            return new StartupMethods(instance, configureMethod.Build(instance));
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "We're warning at the entry point. This is an implementation detail.")]
        public static Type FindStartupType(string startupAssemblyName, string environmentName)
        {
            if (string.IsNullOrEmpty(startupAssemblyName))
                throw new ArgumentException(
                    $"A startup method, startup type or startup assembly is required. If specifying an assembly, '{nameof(startupAssemblyName)}' cannot be null or empty.",
                    nameof(startupAssemblyName));

            var assembly = Assembly.Load(new AssemblyName(startupAssemblyName));
            if (assembly == null)
                throw new InvalidOperationException($"The assembly '{startupAssemblyName}' failed to load.");

            var startupNameWithEnv = "Startup" + environmentName;
            const string startupNameWithoutEnv = "Startup";

            var type =
                assembly.GetType(startupNameWithEnv) ??
                assembly.GetType(startupAssemblyName + "." + startupNameWithEnv) ??
                assembly.GetType(startupNameWithoutEnv) ??
                assembly.GetType(startupAssemblyName + "." + startupNameWithoutEnv);

            if (type == null)
            {
                var definedTypes = assembly.DefinedTypes.ToList();

                var startupType1 = definedTypes.Where(info =>
                    info.Name.Equals(startupNameWithEnv, StringComparison.OrdinalIgnoreCase));
                var startupType2 = definedTypes.Where(info =>
                    info.Name.Equals(startupNameWithoutEnv, StringComparison.OrdinalIgnoreCase));

                var typeInfo = startupType1.Concat(startupType2).FirstOrDefault();
                if (typeInfo != null) type = typeInfo.AsType();
            }

            if (type == null)
                throw new InvalidOperationException(
                    $"A type named '{startupNameWithEnv}' or '{startupNameWithoutEnv}' could not be found in assembly '{startupAssemblyName}'.");

            return type;
        }

        internal static ConfigureBuilder FindConfigureDelegate(
            [DynamicallyAccessedMembers(StartupLinkerOptions.Accessibility)]
            Type startupType, string environmentName)
        {
            var configureMethod =
                FindMethod(startupType, "Configure{0}", environmentName, typeof(void));
            return new ConfigureBuilder(configureMethod);
        }

        internal static ConfigureContainerBuilder FindConfigureContainerDelegate(
            [DynamicallyAccessedMembers(StartupLinkerOptions.Accessibility)]
            Type startupType, string environmentName)
        {
            var configureMethod = FindMethod(startupType, "Configure{0}Container", environmentName, typeof(void),
                false);
            return new ConfigureContainerBuilder(configureMethod);
        }

        internal static bool HasConfigureServicesIServiceProviderDelegate(
            [DynamicallyAccessedMembers(StartupLinkerOptions.Accessibility)]
            Type startupType, string environmentName)
        {
            return null != FindMethod(startupType, "Configure{0}Services", environmentName, typeof(IServiceProvider),
                false);
        }

        internal static ConfigureServicesBuilder FindConfigureServicesDelegate(
            [DynamicallyAccessedMembers(StartupLinkerOptions.Accessibility)]
            Type startupType, string environmentName)
        {
            var servicesMethod = FindMethod(startupType, "Configure{0}Services", environmentName,
                                     typeof(IServiceProvider), false)
                                 ?? FindMethod(startupType, "Configure{0}Services", environmentName, typeof(void),
                                     false);
            return new ConfigureServicesBuilder(servicesMethod);
        }

        private static MethodInfo FindMethod(
            [DynamicallyAccessedMembers(StartupLinkerOptions.Accessibility)]
            Type startupType, string methodName,
            string environmentName, Type returnType = null, bool required = true)
        {
            var methodNameWithEnv = string.Format(CultureInfo.InvariantCulture, methodName, environmentName);
            var methodNameWithNoEnv = string.Format(CultureInfo.InvariantCulture, methodName, "");

            var methods = startupType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            var selectedMethods = methods
                .Where(method => method.Name.Equals(methodNameWithEnv, StringComparison.OrdinalIgnoreCase)).ToList();
            switch (selectedMethods.Count)
            {
                case > 1:
                    throw new InvalidOperationException(
                        $"Having multiple overloads of method '{methodNameWithEnv}' is not supported.");
                case 0:
                {
                    selectedMethods = methods.Where(method =>
                        method.Name.Equals(methodNameWithNoEnv, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (selectedMethods.Count > 1)
                        throw new InvalidOperationException(
                            $"Having multiple overloads of method '{methodNameWithNoEnv}' is not supported.");
                    break;
                }
            }

            var methodInfo = selectedMethods.FirstOrDefault();
            if (methodInfo == null)
            {
                if (required)
                    throw new InvalidOperationException(
                        $"A public method named '{methodNameWithEnv}' or '{methodNameWithNoEnv}' could not be found in the '{startupType.FullName}' type.");

                return null;
            }

            if (returnType == null || methodInfo.ReturnType == returnType) return methodInfo;
            if (required)
                throw new InvalidOperationException(
                    $"The '{methodInfo.Name}' method in the type '{startupType.FullName}' must have a return type of '{returnType.Name}'.");

            return null;
        }
    }
}