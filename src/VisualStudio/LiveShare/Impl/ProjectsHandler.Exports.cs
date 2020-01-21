// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, RoslynMethods.ProjectsName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynProjectsHandler : ProjectsHandler
    {
        [ImportingConstructor]
        public RoslynProjectsHandler()
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, RoslynMethods.ProjectsName)]
    internal class CSharpProjectsHandler : ProjectsHandler
    {
        [ImportingConstructor]
        public CSharpProjectsHandler()
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, RoslynMethods.ProjectsName)]
    internal class VisualBasicProjectsHandler : ProjectsHandler
    {
        [ImportingConstructor]
        public VisualBasicProjectsHandler()
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, RoslynMethods.ProjectsName)]
    internal class TypeScriptProjectsHandler : ProjectsHandler
    {
        [ImportingConstructor]
        public TypeScriptProjectsHandler()
        {
        }
    }
}
