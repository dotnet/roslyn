// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

internal sealed class CustomMessageHandlerFactory : ICustomMessageHandlerFactory
{
    public ICustomMessageHandlerWrapper Create(Type customMessageHandlerType)
    {
        _ = customMessageHandlerType ?? throw new ArgumentNullException(nameof(customMessageHandlerType));

        var handler = Activator.CreateInstance(customMessageHandlerType)
            ?? throw new InvalidOperationException($"Cannot create {customMessageHandlerType.FullName}.");

        return new CustomMessageHandlerWrapper(handler);
    }
}
