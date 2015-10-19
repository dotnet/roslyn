// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal sealed class NodeKeyValidation
    {
        private readonly Dictionary<ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>, List<GlobalNodeKey>> _nodeKeysMap;

        public NodeKeyValidation()
        {
            _nodeKeysMap = new Dictionary<ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>, List<GlobalNodeKey>>();
        }

        public void AddProject(AbstractProject project)
        {
            var provider = project as IProjectCodeModelProvider;
            if (provider != null)
            {
                var fcms = provider.ProjectCodeModel.GetCachedFileCodeModelInstances();

                foreach (var fcm in fcms)
                {
                    var globalNodeKeys = fcm.Object.GetCurrentNodeKeys();

                    _nodeKeysMap.Add(fcm, globalNodeKeys);
                }
            }
        }

        public void AddFileCodeModel(FileCodeModel fileCodeModel)
        {
            var handle = new ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>(fileCodeModel);
            var globalNodeKeys = fileCodeModel.GetCurrentNodeKeys();

            _nodeKeysMap.Add(handle, globalNodeKeys);
        }

        public void RestoreKeys()
        {
            foreach (var e in _nodeKeysMap)
            {
                e.Key.Object.ResetElementKeys(e.Value);
            }

            _nodeKeysMap.Clear();
        }
    }
}
