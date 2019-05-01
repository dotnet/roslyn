// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    [Export(typeof(ICodeModelFactory))]
    internal partial class CPSCodeModelFactory : ICodeModelFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CPSCodeModelFactory()
        {
        }

        public EnvDTE.CodeModel GetCodeModel(IWorkspaceProjectContext context, EnvDTE.Project project)
        {
            return ((CPSProject)context).GetCodeModel(project);
        }

        public EnvDTE.FileCodeModel GetFileCodeModel(IWorkspaceProjectContext context, EnvDTE.ProjectItem item)
        {
            return ((CPSProject)context).GetFileCodeModel(item);
        }
    }
}
