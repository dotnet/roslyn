// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not use banned APIs (MEFv1)

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.CodeAnalysis.BrokeredServices;

/// <summary>
/// MEF service that can be used to fetch an <see cref="IServiceBroker"/> instance without having to use legacy MEF imports.
/// </summary>
[Export(typeof(IServiceBrokerProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ServiceBrokerProvider([Import(typeof(SVsFullAccessServiceBroker))] IServiceBroker serviceBroker) : IServiceBrokerProvider
{
    public IServiceBroker ServiceBroker { get; } = serviceBroker;
}
