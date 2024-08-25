// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Workspaces;

[ExportWorkspaceService(typeof(IAnalyzerAssemblyLoaderProvider), [WorkspaceKind.Host]), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSAnalyzerAssemblyLoaderProvider(
    [ImportMany] IEnumerable<IAnalyzerAssemblyResolver> externalResolvers)
    : AbstractAnalyzerAssemblyLoaderProvider(externalResolvers.ToImmutableArray());
