// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            => ((CPSProject)context).GetCodeModel(project);

        public EnvDTE.FileCodeModel GetFileCodeModel(IWorkspaceProjectContext context, EnvDTE.ProjectItem item)
            => ((CPSProject)context).GetFileCodeModel(item);
    }
}
