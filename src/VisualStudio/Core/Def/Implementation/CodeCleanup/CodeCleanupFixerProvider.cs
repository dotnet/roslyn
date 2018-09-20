// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.CodeCleanUp;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup
{
    [Export(typeof(ICodeCleanUpFixerProvider))]
    internal class CodeCleanupFixerProvider : ICodeCleanUpFixerProvider
    {
        private IDictionary<IContentType, List<ICodeCleanUpFixer>> _fixerDictionary = new Dictionary<IContentType, List<ICodeCleanUpFixer>>();

        public void AddFixer(IContentType contentType, ICodeCleanUpFixer fixer)
        {
            if (_fixerDictionary.ContainsKey(contentType))
            {
                _fixerDictionary[contentType].Add(fixer);
            }
            else
            {
                _fixerDictionary[contentType] = new List<ICodeCleanUpFixer>() { fixer };
            }
        }

        public IReadOnlyCollection<ICodeCleanUpFixer> CreateFixers()
        {
            var fixers = new List<ICodeCleanUpFixer>();
            foreach (var val in _fixerDictionary.Values)
            {
                fixers.AddRange(val);
            }

            return fixers;
        }

        public IReadOnlyCollection<ICodeCleanUpFixer> CreateFixers(IContentType contentType)
        {
            return _fixerDictionary.ContainsKey(contentType) ? _fixerDictionary[contentType] : new List<ICodeCleanUpFixer>();
        }
    }
}
