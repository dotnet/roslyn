// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Api = Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Api;
using Legacy = Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Internal;

[Export(typeof(Api.ITempPECompiler)), Shared]
internal sealed class TempPECompilerAdapter : Api.ITempPECompiler
{
    private readonly Legacy.ITempPECompiler _compiler;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TempPECompilerAdapter(Legacy.ITempPECompiler compiler)
        => _compiler = compiler;

    public Task<bool> CompileAsync(Api.IWorkspaceProjectContext context, string outputFileName, ISet<string> filesToInclude, CancellationToken cancellationToken)
        => _compiler.CompileAsync(WorkspaceProjectContextAdapter.GetUnderlying(context), outputFileName, filesToInclude, cancellationToken);

}
