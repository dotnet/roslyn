// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

internal static class HandlerReflection
{
    /// <summary>
    /// Retrieves the generic argument information from the request handler type without instantiating it.
    /// </summary>
    public static ImmutableArray<HandlerDetails> GetHandlerDetails(Type handlerType)
    {
        var builder = ImmutableArray.CreateBuilder<HandlerDetails>();

        foreach (var interfaceType in handlerType.GetInterfaces())
        {
            if (!interfaceType.IsGenericType)
            {
                continue;
            }

            var genericDefinition = interfaceType.GetGenericTypeDefinition();

            HandlerDetails details;
            if (genericDefinition == typeof(IRequestHandler<,,>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                details = new HandlerDetails(RequestType: genericArguments[0], ResponseType: genericArguments[1], RequestContextType: genericArguments[2]);
            }
            else if (genericDefinition == typeof(IRequestHandler<,>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                details = new HandlerDetails(RequestType: null, ResponseType: genericArguments[0], RequestContextType: genericArguments[1]);
            }
            else if (genericDefinition == typeof(INotificationHandler<,>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                details = new HandlerDetails(RequestType: genericArguments[0], ResponseType: null, RequestContextType: genericArguments[1]);
            }
            else if (genericDefinition == typeof(INotificationHandler<>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                details = new HandlerDetails(RequestType: null, ResponseType: null, RequestContextType: genericArguments[0]);
            }
            else
            {
                continue;
            }

            builder.Add(details);
        }

        if (builder.Count == 0)
        {
            throw new InvalidOperationException($"Provided handler type {handlerType.FullName} does not implement {nameof(IMethodHandler)}");
        }

        return builder.ToImmutable();
    }

    public static ImmutableArray<MethodHandlerDescriptor> GetMethodHandlers(Type handlerType)
    {
        var allHandlerDetails = GetHandlerDetails(handlerType);

        var builder = ImmutableArray.CreateBuilder<MethodHandlerDescriptor>(initialCapacity: allHandlerDetails.Length);

        foreach (var (requestType, responseType, requestContextType) in allHandlerDetails)
        {
            var (method, languages) = GetRequestHandlerMethod(handlerType, requestType, requestContextType, responseType);

            foreach (var language in languages)
            {
                builder.Add(new(method, language, requestType, responseType, requestContextType));
            }
        }

        return builder.DrainToImmutable();
    }

    public static (string name, IEnumerable<string> languages) GetRequestHandlerMethod(Type handlerType, Type? requestType, Type contextType, Type? responseType)
    {
        // Get the LSP method name from the handler's method name attribute.
        var methodAttribute = GetMethodAttributeFromClassOrInterface(handlerType);
        if (methodAttribute is null)
        {
            methodAttribute = GetMethodAttributeFromHandlerMethod(handlerType, requestType, contextType, responseType);

            if (methodAttribute is null)
            {
                throw new InvalidOperationException($"{handlerType.FullName} is missing {nameof(LanguageServerEndpointAttribute)}");
            }
        }

        return (methodAttribute.Method, methodAttribute.Languages);

        static LanguageServerEndpointAttribute? GetMethodAttributeFromHandlerMethod(Type handlerType, Type? requestType, Type contextType, Type? responseType)
        {
            const string HandleRequestName = nameof(IRequestHandler<object, object, object>.HandleRequestAsync);
            const string HandleRequestSuffix = "." + HandleRequestName;
            const string HandleNotificationName = nameof(INotificationHandler<object, object>.HandleNotificationAsync);
            const string HandleNotificationSuffix = "." + HandleNotificationName;

            foreach (var methodInfo in handlerType.GetRuntimeMethods())
            {
                if (MethodInfoMatches(methodInfo))
                    return methodInfo.GetCustomAttribute<LanguageServerEndpointAttribute>();
            }

            throw new InvalidOperationException("Somehow we are missing the method for our registered handler");

            bool MethodInfoMatches(MethodInfo methodInfo)
            {
                var methodName = methodInfo.Name;

                switch (requestType != null, responseType != null)
                {
                    case (true, true):
                        return NameMatches(methodInfo, HandleRequestName, HandleRequestSuffix)
                            && TypesMatch(methodInfo, [requestType, contextType, typeof(CancellationToken)]);

                    case (false, true):
                        return NameMatches(methodInfo, HandleRequestName, HandleNotificationSuffix)
                            && TypesMatch(methodInfo, [contextType, typeof(CancellationToken)]);

                    case (true, false):
                        return NameMatches(methodInfo, HandleNotificationName, HandleNotificationSuffix)
                            && TypesMatch(methodInfo, [requestType, contextType, typeof(CancellationToken)]);

                    case (false, false):
                        return NameMatches(methodInfo, HandleNotificationName, HandleNotificationSuffix)
                            && TypesMatch(methodInfo, [contextType, typeof(CancellationToken)]);
                }
            }

            static bool NameMatches(MethodInfo methodInfo, string name, string suffix)
            {
                var methodName = methodInfo.Name;

                return methodName == name || methodName.EndsWith(suffix);
            }

            static bool TypesMatch(MethodInfo methodInfo, Type?[] types)
            {
                var parameters = methodInfo.GetParameters();
                if (parameters.Length != types.Length)
                    return false;

                for (int i = 0, n = parameters.Length; i < n; i++)
                {
                    if (!Equals(types[i], parameters[i].ParameterType))
                        return false;
                }

                return true;
            }
        }

        static LanguageServerEndpointAttribute? GetMethodAttributeFromClassOrInterface(Type type)
        {
            var attribute = Attribute.GetCustomAttribute(type, typeof(LanguageServerEndpointAttribute)) as LanguageServerEndpointAttribute;

            if (attribute is null)
            {
                var interfaces = type.GetInterfaces();
                foreach (var @interface in interfaces)
                {
                    attribute = GetMethodAttributeFromClassOrInterface(@interface);
                    if (attribute is not null)
                    {
                        break;
                    }
                }
            }

            return attribute;
        }
    }
}
