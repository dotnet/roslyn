// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Extensions;

internal sealed class ExtensionDocumentMessageHandlerWrapper(
    object handler, Type customMessageHandlerInterface, string extensionIdentifier)
    : ExtensionHandlerWrapper<Document>(handler, customMessageHandlerInterface, extensionIdentifier)
{
    protected override Task ExecuteAsync(MethodInfo executeAsyncMethod, object handler, object? message, Document argument, CancellationToken cancellationToken)
        => (Task)executeAsyncMethod.Invoke(handler, [message, new ExtensionMessageContext(argument.Project.Solution), argument, cancellationToken]);
}
