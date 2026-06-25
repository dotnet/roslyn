// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.CodeAnalysis.BrokeredServices;

/// <summary>
/// Wrapper MEF service to import the VS-wide <see cref="IServiceBroker"/> which requires <see cref="System.ComponentModel.Composition"/>
/// and cannot be combined with the workspace service <see cref="VSServiceBrokerProvider"/> which require <see cref="System.Composition"/>
/// </summary>
[Export(typeof(VSServiceBrokerWrapper))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
#pragma warning disable VSMEF008 // Import contract type not assignable to member type; TODO: remove once https://github.com/microsoft/vs-servicehub/pull/531 is available
internal sealed partial class VSServiceBrokerWrapper([Import(typeof(SVsFullAccessServiceBroker))] IServiceBroker serviceBroker)
#pragma warning restore VSMEF008 // Import contract type not assignable to member type
{
    public IServiceBroker ServiceBroker { get; } = serviceBroker;
}

