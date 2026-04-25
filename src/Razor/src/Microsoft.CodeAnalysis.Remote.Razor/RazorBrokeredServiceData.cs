// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed record class RazorBrokeredServiceData(
    ExportProvider? ExportProvider,
    ILoggerFactory? LoggerFactory,
    IRazorBrokeredServiceInterceptor? Interceptor,
    IWorkspaceProvider? WorkspaceProvider);
