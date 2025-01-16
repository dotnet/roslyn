// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.BrokeredServices;

/// <summary>
/// MEF service that can be used to fetch an <see cref="IServiceBroker"/> instance without having to use legacy MEF imports.
/// </summary>
[Export(typeof(IServiceBrokerProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ServiceBrokerProvider([Import("Microsoft.VisualStudio.Shell.ServiceBroker.SVsFullAccessServiceBroker")] IServiceBroker serviceBroker) : IServiceBrokerProvider
{
    public IServiceBroker ServiceBroker { get; } = serviceBroker;
}
