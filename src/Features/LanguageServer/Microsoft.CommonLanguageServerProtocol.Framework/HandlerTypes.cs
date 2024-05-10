// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

internal record HandlerTypes(Type? RequestType, Type? ResponseType, Type RequestContext)
{
    /// <summary>
    /// Retrieves the generic argument information from the request handler type without instantiating it.
    /// </summary>
    public static List<HandlerTypes> ConvertHandlerTypeToRequestResponseTypes(Type handlerType)
    {
        var handlerList = new List<HandlerTypes>();

        foreach (var interfaceType in handlerType.GetInterfaces())
        {
            if (!interfaceType.IsGenericType)
            {
                continue;
            }

            var genericDefinition = interfaceType.GetGenericTypeDefinition();

            HandlerTypes types;
            if (genericDefinition == typeof(IRequestHandler<,,>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                types = new HandlerTypes(RequestType: genericArguments[0], ResponseType: genericArguments[1], RequestContext: genericArguments[2]);
            }
            else if (genericDefinition == typeof(IRequestHandler<,>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                types = new HandlerTypes(RequestType: null, ResponseType: genericArguments[0], RequestContext: genericArguments[1]);
            }
            else if (genericDefinition == typeof(INotificationHandler<,>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                types = new HandlerTypes(RequestType: genericArguments[0], ResponseType: null, RequestContext: genericArguments[1]);
            }
            else if (genericDefinition == typeof(INotificationHandler<>))
            {
                var genericArguments = interfaceType.GetGenericArguments();
                types = new HandlerTypes(RequestType: null, ResponseType: null, RequestContext: genericArguments[0]);
            }
            else
            {
                continue;
            }

            handlerList.Add(types);
        }

        if (handlerList.Count == 0)
        {
            throw new InvalidOperationException($"Provided handler type {handlerType.FullName} does not implement {nameof(IMethodHandler)}");
        }

        return handlerList;
    }
}
