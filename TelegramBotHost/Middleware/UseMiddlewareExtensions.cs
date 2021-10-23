// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotHost.ApplicationBuilder;

namespace TelegramBotHost.Middleware
{
    public static class UseMiddlewareExtensions
    {
        private const DynamicallyAccessedMemberTypes MiddlewareAccessibility =
            DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods;

        private const string InvokeMethodName = "Invoke";
        private const string InvokeAsyncMethodName = "InvokeAsync";

        private static readonly MethodInfo GetServiceInfo =
            typeof(UseMiddlewareExtensions).GetMethod(nameof(GetService),
                BindingFlags.NonPublic | BindingFlags.Static)!;

        public static IApplicationBuilder UseMiddleware<
            [DynamicallyAccessedMembers(MiddlewareAccessibility)]
            TMiddleware>(this IApplicationBuilder app,
            params object[] args)
        {
            return app.UseMiddleware(typeof(TMiddleware), args);
        }

        public static IApplicationBuilder UseMiddleware(this IApplicationBuilder app,
            [DynamicallyAccessedMembers(MiddlewareAccessibility)]
            Type middleware, params object[] args)
        {
            if (typeof(IMiddleware).GetTypeInfo().IsAssignableFrom(middleware.GetTypeInfo()))
            {
                if (args.Length > 0)
                    throw new NotSupportedException(
                        $"FormatException_UseMiddlewareExplicitArgumentsNotSupported {typeof(IMiddleware)}");

                return UseMiddlewareInterface(app, middleware);
            }

            var applicationServices = app.ApplicationServices;
            return app.Use(next =>
            {
                var methods = middleware.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                var invokeMethods = methods.Where(m =>
                    string.Equals(m.Name, InvokeMethodName, StringComparison.Ordinal)
                    || string.Equals(m.Name, InvokeAsyncMethodName, StringComparison.Ordinal)
                ).ToArray();

                switch (invokeMethods.Length)
                {
                    case > 1:
                        throw new InvalidOperationException(
                            $"FormatException_UseMiddleMutlipleInvokes({InvokeMethodName}, {InvokeAsyncMethodName})");
                    case 0:
                        throw new InvalidOperationException(
                            $"FormatException_UseMiddlewareNoInvokeMethod({InvokeMethodName}, {InvokeAsyncMethodName}, {middleware})");
                }

                var methodInfo = invokeMethods[0];
                if (!typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
                    throw new InvalidOperationException(
                        $"FormatException_UseMiddlewareNonTaskReturnType({InvokeMethodName}, {InvokeAsyncMethodName}, {nameof(Task)})");

                var parameters = methodInfo.GetParameters();
                if (parameters.Length == 0 || parameters[0].ParameterType != typeof(BotUpdateContext))
                    throw new InvalidOperationException(
                        $"FormatException_UseMiddlewareNoParameters({InvokeMethodName}, {InvokeAsyncMethodName}, {nameof(BotUpdateContext)})");

                var ctorArgs = new object[args.Length + 1];
                ctorArgs[0] = next;
                Array.Copy(args, 0, ctorArgs, 1, args.Length);
                var instance = ActivatorUtilities.CreateInstance(app.ApplicationServices, middleware, ctorArgs);
                if (parameters.Length == 1)
                    return (BotUpdateDelegate)methodInfo.CreateDelegate(typeof(BotUpdateDelegate), instance);

                var factory = Compile<object>(methodInfo, parameters);

                return context =>
                {
                    var serviceProvider = context.RequestServices ?? applicationServices;
                    if (serviceProvider == null)
                        throw new InvalidOperationException(
                            $"FormatException_UseMiddlewareIServiceProviderNotAvailable({nameof(IServiceProvider)})");

                    return factory(instance, context, serviceProvider);
                };
            });
        }

        private static object GetService(IServiceProvider sp, Type type, Type middleware)
        {
            var service = sp.GetService(type);
            if (service == null)
                throw new InvalidOperationException($"FormatException_InvokeMiddlewareNoService({type}, {middleware})");

            return service;
        }

        private static Func<T, BotUpdateContext, IServiceProvider, Task> Compile<T>(MethodInfo methodInfo,
            ParameterInfo[] parameters)
        {
            var middleware = typeof(T);

            var botUpdateContextArg = Expression.Parameter(typeof(BotUpdateContext), "botUpdateContext");
            var providerArg = Expression.Parameter(typeof(IServiceProvider), "serviceProvider");
            var instanceArg = Expression.Parameter(middleware, "middleware");

            var methodArguments = new Expression[parameters.Length];
            methodArguments[0] = botUpdateContextArg;
            for (var i = 1; i < parameters.Length; i++)
            {
                var parameterType = parameters[i].ParameterType;
                if (parameterType.IsByRef)
                    throw new NotSupportedException(
                        $"FormatException_InvokeDoesNotSupportRefOrOutParams({InvokeMethodName})");

                var parameterTypeExpression = new Expression[]
                {
                    providerArg,
                    Expression.Constant(parameterType, typeof(Type)),
                    Expression.Constant(methodInfo.DeclaringType, typeof(Type))
                };

                var getServiceCall = Expression.Call(GetServiceInfo, parameterTypeExpression);
                methodArguments[i] = Expression.Convert(getServiceCall, parameterType);
            }

            Expression middlewareInstanceArg = instanceArg;
            if (methodInfo.DeclaringType != null && methodInfo.DeclaringType != typeof(T))
                middlewareInstanceArg = Expression.Convert(middlewareInstanceArg, methodInfo.DeclaringType);

            var body = Expression.Call(middlewareInstanceArg, methodInfo, methodArguments);

            var lambda =
                Expression.Lambda<Func<T, BotUpdateContext, IServiceProvider, Task>>(body, instanceArg,
                    botUpdateContextArg, providerArg);

            return lambda.Compile();
        }

        private static IApplicationBuilder UseMiddlewareInterface(IApplicationBuilder app,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            Type middlewareType)
        {
            return app.Use(next =>
            {
                return async context =>
                {
                    var middlewareFactory =
                        (IMiddlewareFactory)context.RequestServices.GetService(typeof(IMiddlewareFactory));
                    if (middlewareFactory == null)
                        throw new InvalidOperationException(
                            $"FormatException_UseMiddlewareNoMiddlewareFactory({typeof(IMiddlewareFactory)})");

                    var middleware = middlewareFactory.Create(middlewareType);
                    if (middleware == null)
                        throw new InvalidOperationException(
                            $"FormatException_UseMiddlewareUnableToCreateMiddleware({middlewareFactory.GetType()}, {middlewareType})");

                    try
                    {
                        await middleware.InvokeAsync(context, next);
                    }
                    finally
                    {
                        middlewareFactory.Release(middleware);
                    }
                };
            });
        }
    }
}