// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

internal interface ICustomMessageHandlerFactory
{
    IEnumerable<ICustomMessageHandlerWrapper> CreateMessageHandlers(Assembly assembly);

    IEnumerable<ICustomMessageDocumentHandlerWrapper> CreateMessageDocumentHandlers(Assembly assembly);
}
