// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
{
    internal class CSharpProjectCodeModel : AbstractProjectCodeModel
    {
        private readonly CSharpProjectShimWithServices _project;

        public CSharpProjectCodeModel(CSharpProjectShimWithServices project, VisualStudioWorkspace visualStudioWorkspace, IServiceProvider serviceProvider)
            : base(project, visualStudioWorkspace, serviceProvider)
        {
            _project = project;
        }

        internal override bool CanCreateFileCodeModelThroughProject(string fileName)
        {
            return _project.CanCreateFileCodeModelThroughProject(fileName);
        }

        internal override object CreateFileCodeModelThroughProject(string fileName)
        {
            return _project.CreateFileCodeModelThroughProject(fileName);
        }
    }
}
