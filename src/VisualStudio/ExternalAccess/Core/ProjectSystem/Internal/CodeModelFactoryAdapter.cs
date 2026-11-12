// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using ProjectSystemApi = global::Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem;
using Legacy = Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Internal;

[Export(typeof(ProjectSystemApi.ICodeModelFactory)), Shared]
internal sealed class CodeModelFactoryAdapter : ProjectSystemApi.ICodeModelFactory
{
    private readonly Legacy.ICodeModelFactory _factory;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CodeModelFactoryAdapter(Legacy.ICodeModelFactory factory)
        => _factory = factory;

    public EnvDTE.FileCodeModel GetFileCodeModel(ProjectSystemApi.IWorkspaceProjectContext context, EnvDTE.ProjectItem item)
        => _factory.GetFileCodeModel(WorkspaceProjectContextAdapter.GetUnderlying(context), item);

    public EnvDTE.CodeModel GetCodeModel(ProjectSystemApi.IWorkspaceProjectContext context, EnvDTE.Project project)
        => _factory.GetCodeModel(WorkspaceProjectContextAdapter.GetUnderlying(context), project);

}
