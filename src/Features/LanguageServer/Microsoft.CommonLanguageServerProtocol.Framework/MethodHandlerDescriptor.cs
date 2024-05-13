// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Collections.Immutable;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

internal sealed record MethodHandlerDescriptor(string MethodName, string Language, string? RequestTypeName, string? ResponseTypeName, string RequestContextTypeName)
{
    private readonly LazyType? _lazyRequestType = RequestTypeName is not null ? new(RequestTypeName) : null;
    private readonly LazyType? _lazyResponseType = ResponseTypeName is not null ? new(ResponseTypeName) : null;
    private readonly LazyType _lazyRequestContextType = new(RequestContextTypeName);

    public Type? RequestType => _lazyRequestType?.Value;
    public Type? ResponseType => _lazyResponseType?.Value;
    public Type RequestContextType => _lazyRequestContextType.Value;

    public static ImmutableArray<MethodHandlerDescriptor> From(Type type)
    {
        var allHandlerTypes = HandlerTypes.ConvertHandlerTypeToRequestResponseTypes(type);

        var builder = ImmutableArray.CreateBuilder<MethodHandlerDescriptor>(initialCapacity: allHandlerTypes.Count);

        foreach (var handlerTypes in allHandlerTypes)
        {
            var (method, languages) = HandlerReflection.GetRequestHandlerMethod(type, handlerTypes.RequestType, handlerTypes.RequestContextType, handlerTypes.ResponseType);

            foreach (var language in languages)
            {
                builder.Add(new(
                    method,
                    language,
                    handlerTypes.RequestType?.AssemblyQualifiedName,
                    handlerTypes.ResponseType?.AssemblyQualifiedName,
                    handlerTypes.RequestContextType.AssemblyQualifiedName!));
            }
        }

        return builder.DrainToImmutable();
    }
}
