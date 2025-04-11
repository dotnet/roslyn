// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Remote;

[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class)]
internal sealed class ExportRemoteServiceCallbackDispatcherAttribute : ExportAttribute
{
    public Type ServiceInterface { get; }

    public ExportRemoteServiceCallbackDispatcherAttribute(Type serviceInterface)
        : base(typeof(IRemoteServiceCallbackDispatcher))
    {
        Contract.ThrowIfFalse(serviceInterface.IsInterface);

        ServiceInterface = serviceInterface;
    }
}
