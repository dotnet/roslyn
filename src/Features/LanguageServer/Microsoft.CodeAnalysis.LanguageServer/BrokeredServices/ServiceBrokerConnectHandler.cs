// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;

[ExportCSharpVisualBasicStatelessLspService(typeof(ServiceBrokerConnectHandler)), Shared]
[Method("serviceBroker/connect")]
internal class ServiceBrokerConnectHandler : ILspServiceNotificationHandler<ServiceBrokerConnectHandler.NotificationParams>
{
    private readonly ServiceBrokerFactory _serviceBrokerFactory;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ServiceBrokerConnectHandler(ServiceBrokerFactory serviceBrokerFactory)
    {
        _serviceBrokerFactory = serviceBrokerFactory;
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => false;

    Task INotificationHandler<NotificationParams, RequestContext>.HandleNotificationAsync(NotificationParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        return _serviceBrokerFactory.CreateAndConnectAsync(request.PipeName);
    }

    [DataContract]
    private class NotificationParams
    {
        [DataMember(Name = "pipeName")]
        public required string PipeName { get; set; }
    }
}
