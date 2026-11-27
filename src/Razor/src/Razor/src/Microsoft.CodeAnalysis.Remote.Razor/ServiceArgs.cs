// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal readonly record struct ServiceArgs(
    IServiceBroker? ServiceBroker,
    ExportProvider ExportProvider,
    ILoggerFactory ServiceLoggerFactory,
    IWorkspaceProvider WorkspaceProvider,
    ServiceRpcDescriptor.RpcConnection? ServerConnection = null,
    IRazorBrokeredServiceInterceptor? Interceptor = null);
