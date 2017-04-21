// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
{
    internal class CSharpProjectCodeModel : AbstractProjectCodeModel
    {
        private readonly CSharpProjectShimWithServices _project;

        public CSharpProjectCodeModel(CSharpProjectShimWithServices project, VisualStudioWorkspaceImpl visualStudioWorkspace, IServiceProvider serviceProvider)
            : base(project, visualStudioWorkspace, serviceProvider)
        {
            _project = project;
        }

        internal override bool CanCreateFileCodeModelThroughProject(string filePath)
        {
            return _project.CanCreateFileCodeModelThroughProject(filePath);
        }

        internal override object CreateFileCodeModelThroughProject(string filePath)
        {
            return _project.CreateFileCodeModelThroughProject(filePath);
        }
    }
}
