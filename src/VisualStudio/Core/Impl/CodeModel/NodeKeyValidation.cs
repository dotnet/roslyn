// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal sealed class NodeKeyValidation
    {
        private readonly Dictionary<ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>, List<GlobalNodeKey>> _nodeKeysMap = [];

        public NodeKeyValidation()
        {
        }

        public NodeKeyValidation(ProjectCodeModelFactory projectCodeModelFactory)
        {
            foreach (var projectCodeModel in projectCodeModelFactory.GetAllProjectCodeModels())
            {
                var fcms = projectCodeModel.GetCachedFileCodeModelInstances();

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
