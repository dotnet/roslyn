// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    internal partial class CSharpProjectShimWithServices : IProjectCodeModelProvider
    {
        private AbstractProjectCodeModel _projectCodeModel;

        public AbstractProjectCodeModel ProjectCodeModel
        {
            get
            {
                if (_projectCodeModel == null)
                {
                    _projectCodeModel = new CSharpProjectCodeModel(this, (VisualStudioWorkspace)this.Workspace, ServiceProvider);
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
            var codeModelCache = ProjectCodeModel.GetCodeModelCache();
            if (codeModelCache != null)
            {
                codeModelCache.OnSourceFileRemoved(filePath);
            }
        }

        public override int CreateCodeModel(object parent, out EnvDTE.CodeModel codeModel)
        {
            var codeModelCache = ProjectCodeModel.GetCodeModelCache();
            if (codeModelCache == null)
            {
                codeModel = null;
                return VSConstants.E_FAIL;
            }

            codeModel = codeModelCache.GetOrCreateRootCodeModel(parent);
            return VSConstants.S_OK;
        }

        public override int CreateFileCodeModel(string fileName, object parent, out EnvDTE.FileCodeModel ppFileCodeModel)
        {
            var codeModelCache = ProjectCodeModel.GetCodeModelCache();
            if (codeModelCache == null)
            {
                ppFileCodeModel = null;
                return VSConstants.E_FAIL;
            }

            ppFileCodeModel = codeModelCache.GetOrCreateFileCodeModel(fileName, parent).Handle;
            return VSConstants.S_OK;
        }
    }
}
