// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.BrokeredServices;

internal interface IServiceBrokerProvider
{
    [Obsolete]
    IServiceBroker ServiceBroker { get; }

    Task<IServiceBroker> GetServiceBrokerAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// MEF service that can be used to fetch an <see cref="IServiceBroker"/> instance without having to use legacy MEF imports.
/// </summary>
[Export(typeof(IServiceBrokerProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ServiceBrokerProvider([Import(typeof(SVsFullAccessServiceBroker))] Task<IServiceBroker> serviceBrokerTask) : IServiceBrokerProvider
{
    [Obsolete]
    public IServiceBroker ServiceBroker
    {
        get
        {
            if (serviceBrokerTask.IsCompleted)
            {
                return serviceBrokerTask.Result;
            }
            else
            {
                throw new InvalidOperationException("Service broker is not yet available.");
            }
        }
    }

    public Task<IServiceBroker> GetServiceBrokerAsync(CancellationToken cancellationToken)
        => serviceBrokerTask.WithCancellation(cancellationToken);
}
