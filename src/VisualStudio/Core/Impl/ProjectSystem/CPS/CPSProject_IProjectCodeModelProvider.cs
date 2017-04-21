// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    internal sealed partial class CPSProject : IProjectCodeModelProvider
    {
        private AbstractProjectCodeModel _projectCodeModel;

        public AbstractProjectCodeModel ProjectCodeModel
        {
            get
            {
                if (_projectCodeModel == null && this.Workspace != null)
                {
                    Interlocked.CompareExchange(ref _projectCodeModel, new CPSProjectCodeModel(this, (VisualStudioWorkspaceImpl)this.Workspace, ServiceProvider), null);
                }

                return _projectCodeModel;
            }
        }

        protected override void OnDocumentRemoved(string filePath)
        {
            base.OnDocumentRemoved(filePath);

            // We may have a code model floating around for it
            var codeModelCache = _projectCodeModel?.GetCodeModelCache();
            if (codeModelCache != null)
            {
                codeModelCache.OnSourceFileRemoved(filePath);
            }
        }

        public EnvDTE.CodeModel GetCodeModel(EnvDTE.Project parent)
        {
            return ((CPSProjectCodeModel)ProjectCodeModel)?.GetCodeModel(parent);
        }

        public EnvDTE.FileCodeModel GetFileCodeModel(EnvDTE.ProjectItem item)
        {
            return ((CPSProjectCodeModel)ProjectCodeModel)?.GetFileCodeModel(item);
        }
    }
}
