// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.CodeCleanUp;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup
{
    [Export(typeof(CodeCleanUpFixerProvider))]
    internal class CodeCleanUpFixerProvider : ICodeCleanUpFixerProvider
    {
        private IList<Lazy<CodeCleanUpFixer, ContentTypeMetadata>> _codeCleanUpFixers = new List<Lazy<CodeCleanUpFixer, ContentTypeMetadata>>();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeCleanUpFixerProvider(
            [ImportMany] IEnumerable<Lazy<CodeCleanUpFixer, ContentTypeMetadata>> codeCleanUpFixers)
        {
            _codeCleanUpFixers = codeCleanUpFixers.ToList();
        }

        public IReadOnlyCollection<ICodeCleanUpFixer> CreateFixers()
        {
            var fixers = new List<CodeCleanUpFixer>();
            foreach (var fixerLazy in _codeCleanUpFixers)
            {
                fixers.Add(fixerLazy.Value);
            }

            return fixers;
        }

        public IReadOnlyCollection<ICodeCleanUpFixer> CreateFixers(IContentType contentType)
        {
            var fixers = _codeCleanUpFixers
               .Where(handler => handler.Metadata.ContentTypes.Contains(contentType.TypeName)).ToList();

            return fixers.Any() ? fixers.ConvertAll(l => l.Value) : new List<CodeCleanUpFixer>();
        }
    }
}
