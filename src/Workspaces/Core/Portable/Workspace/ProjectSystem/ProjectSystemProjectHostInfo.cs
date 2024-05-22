// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

internal record ProjectSystemHostInfo(
    ImmutableArray<Lazy<IDynamicFileInfoProvider, FileExtensionsMetadata>> DynamicFileInfoProviders,
    IProjectSystemDiagnosticSource DiagnosticSource,
    IHostDiagnosticAnalyzerProvider HostDiagnosticAnalyzerProvider);
