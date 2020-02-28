// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, RoslynMethods.ProjectsName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynProjectsHandler : ProjectsHandler
    {
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, RoslynMethods.ProjectsName)]
    internal class CSharpProjectsHandler : ProjectsHandler
    {
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, RoslynMethods.ProjectsName)]
    internal class VisualBasicProjectsHandler : ProjectsHandler
    {
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, RoslynMethods.ProjectsName)]
    internal class TypeScriptProjectsHandler : ProjectsHandler
    {
    }
}
