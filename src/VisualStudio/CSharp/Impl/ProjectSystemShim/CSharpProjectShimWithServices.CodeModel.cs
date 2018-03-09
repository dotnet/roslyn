// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    internal partial class CSharpProjectShimWithServices : IProjectCodeModelProvider
    {
        private IProjectCodeModel _projectCodeModel;

        public IProjectCodeModel ProjectCodeModel
        {
            get
            {
                if (_projectCodeModel == null)
                {
                    _projectCodeModel = new ProjectCodeModel(this.Id, this, (VisualStudioWorkspaceImpl)this.Workspace, ServiceProvider);
                }

                return _projectCodeModel;
            }
        }

        public override void Disconnect()
        {
            // clear code model cache and shutdown instances, if any exists.
            _projectCodeModel?.OnProjectClosed();

            base.Disconnect();
        }

        protected override void OnDocumentRemoved(string filePath)
        {
            base.OnDocumentRemoved(filePath);

            // We may have a code model floating around for it
            ProjectCodeModel.OnSourceFileRemoved(filePath);
        }

        public override int CreateCodeModel(object parent, out EnvDTE.CodeModel codeModel)
        {
            codeModel = ProjectCodeModel.GetOrCreateRootCodeModel((EnvDTE.Project)parent);
            return VSConstants.S_OK;
        }

        public override int CreateFileCodeModel(string fileName, object parent, out EnvDTE.FileCodeModel ppFileCodeModel)
        {
            ppFileCodeModel = ProjectCodeModel.GetOrCreateFileCodeModel(fileName, parent);
            return VSConstants.S_OK;
        }
    }
}
