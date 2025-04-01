// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare;

[ExportLspRequestHandler(LiveShareConstants.RoslynContractName, RoslynMethods.ProjectsName)]
[Obsolete("Used for backwards compatibility with old liveshare clients.")]
internal sealed class RoslynProjectsHandler : ProjectsHandler
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RoslynProjectsHandler()
    {
    }
}

[ExportLspRequestHandler(LiveShareConstants.CSharpContractName, RoslynMethods.ProjectsName)]
internal sealed class CSharpProjectsHandler : ProjectsHandler
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpProjectsHandler()
    {
    }
}

[ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, RoslynMethods.ProjectsName)]
internal sealed class VisualBasicProjectsHandler : ProjectsHandler
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualBasicProjectsHandler()
    {
    }
}

[ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, RoslynMethods.ProjectsName)]
internal sealed class TypeScriptProjectsHandler : ProjectsHandler
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TypeScriptProjectsHandler()
    {
    }
}
