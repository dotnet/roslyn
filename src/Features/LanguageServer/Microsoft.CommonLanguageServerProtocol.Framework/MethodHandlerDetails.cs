// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// Provides information about an <see cref="IMethodHandler"/>.
/// </summary>
/// <param name="MethodName">The name of the LSP method handled.</param>
/// <param name="Language">The language this <see cref="IMethodHandler"/> targets.</param>
/// <param name="RequestTypeRef">A <see cref="TypeRef"/> representing the request type, if any.</param>
/// <param name="ResponseTypeRef">A <see cref="TypeRef"/> representing the response type, if any.</param>
/// <param name="RequestContextTypeRef">A <see cref="TypeRef"/> representing the context type.</param>
internal sealed record MethodHandlerDetails(
    string MethodName,
    string Language,
    TypeRef? RequestTypeRef,
    TypeRef? ResponseTypeRef,
    TypeRef RequestContextTypeRef)
{
    public static ImmutableArray<MethodHandlerDetails> From(Type handlerType)
    {
        var allHandlerDetails = GetAllHandlerDetails(handlerType);
        var builder = ImmutableArray.CreateBuilder<MethodHandlerDetails>(initialCapacity: allHandlerDetails.Length);

        foreach (var (requestType, responseType, requestContextType) in allHandlerDetails)
        {
            var (method, languages) = GetRequestHandlerMethod(handlerType, requestType, requestContextType, responseType);

            foreach (var language in languages)
            {
                builder.Add(new(
                    method,
                    language,
                    TypeRef.FromOrNull(requestType),
                    TypeRef.FromOrNull(responseType),
                    TypeRef.From(requestContextType)));
            }
        }

        return builder.DrainToImmutable();
    }

    /// <summary>
    /// Retrieves the generic argument information from the request handler type without instantiating it.
    /// </summary>
    private static ImmutableArray<(Type? RequestType, Type? ResponseType, Type RequestContextType)> GetAllHandlerDetails(Type handlerType)
    {
        var builder = ImmutableArray.CreateBuilder<(Type? RequestType, Type? ResponseType, Type RequestContextType)>();

        foreach (var interfaceType in handlerType.GetInterfaces())
        {
            if (!interfaceType.IsGenericType)
            {
                continue;
            }

            var genericDefinition = interfaceType.GetGenericTypeDefinition();

            if (genericDefinition == typeof(IRequestHandler<,,>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                builder.Add((RequestType: genericArguments[0], ResponseType: genericArguments[1], RequestContextType: genericArguments[2]));
            }
            else if (genericDefinition == typeof(IRequestHandler<,>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                builder.Add((RequestType: null, ResponseType: genericArguments[0], RequestContextType: genericArguments[1]));
            }
            else if (genericDefinition == typeof(INotificationHandler<,>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                builder.Add((RequestType: genericArguments[0], ResponseType: null, RequestContextType: genericArguments[1]));
            }
            else if (genericDefinition == typeof(INotificationHandler<>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                builder.Add((RequestType: null, ResponseType: null, RequestContextType: genericArguments[0]));
            }
        }

        if (builder.Count == 0)
        {
            throw new InvalidOperationException($"Provided handler type {handlerType.FullName} does not implement {nameof(IMethodHandler)}");
        }

        return builder.DrainToImmutable();
    }

    private static (string name, IEnumerable<string> languages) GetRequestHandlerMethod(Type handlerType, Type? requestType, Type contextType, Type? responseType)
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
