// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Workspaces.AnalyzerRedirecting;

namespace Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

internal sealed record ProjectSystemHostInfo(
    ImmutableArray<Lazy<IDynamicFileInfoProvider, FileExtensionsMetadata>> DynamicFileInfoProviders,
    ImmutableArray<IAnalyzerAssemblyRedirector> AnalyzerAssemblyRedirectors);
