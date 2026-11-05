// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using ProjectSystemApi = global::Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem;
using Legacy = Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Internal;

[Export(typeof(ProjectSystemApi.ITempPECompiler)), Shared]
internal sealed class TempPECompilerAdapter : ProjectSystemApi.ITempPECompiler
{
    private readonly Legacy.ITempPECompiler _compiler;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TempPECompilerAdapter(Legacy.ITempPECompiler compiler)
        => _compiler = compiler;

    public Task<bool> CompileAsync(ProjectSystemApi.IWorkspaceProjectContext context, string outputFileName, ISet<string> filesToInclude, CancellationToken cancellationToken)
        => _compiler.CompileAsync(WorkspaceProjectContextAdapter.GetUnderlying(context), outputFileName, filesToInclude, cancellationToken);

}
