// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.BrokeredServices;

/// <summary>
/// Exports the workspace service <see cref="IServiceBrokerProvider"/> to allow (EnC) services to access the VS wide
/// service broker instance when running in-proc.
/// </summary>
[ExportWorkspaceService(typeof(IServiceBrokerProvider), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class VSServiceBrokerProvider([Import] VSServiceBrokerWrapper serviceBrokerWrapper) : IServiceBrokerProvider
{
    public IServiceBroker ServiceBroker { get; } = serviceBrokerWrapper.ServiceBroker;
}

