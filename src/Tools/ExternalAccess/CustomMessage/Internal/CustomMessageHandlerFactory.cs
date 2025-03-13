// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

[Export(typeof(ICustomMessageHandlerFactory)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CustomMessageHandlerFactory() : ICustomMessageHandlerFactory
{
    public ICustomMessageHandlerWrapper Create(Type customMessageHandlerType)
    {
        _ = customMessageHandlerType ?? throw new ArgumentNullException(nameof(customMessageHandlerType));

        var handler = Activator.CreateInstance(customMessageHandlerType)
            ?? throw new InvalidOperationException($"Cannot create {customMessageHandlerType.FullName}.");

        return new CustomMessageHandlerWrapper(handler);
    }
}
