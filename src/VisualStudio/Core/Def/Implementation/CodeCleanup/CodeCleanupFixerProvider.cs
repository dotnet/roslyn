// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.CodeCleanUp;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup
{
    [Export(typeof(ICodeCleanUpFixerProvider))]
    [AppliesToProject(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.CSharpContentType)]
    internal class CodeCleanUpFixerProvider : ICodeCleanUpFixerProvider
    {
        private readonly ImmutableArray<Lazy<CodeCleanUpFixer, ContentTypeMetadata>> _codeCleanUpFixers;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeCleanUpFixerProvider(
            [ImportMany] IEnumerable<Lazy<CodeCleanUpFixer, ContentTypeMetadata>> codeCleanUpFixers)
        {
            _codeCleanUpFixers = codeCleanUpFixers.ToImmutableArray();
        }

        public IReadOnlyCollection<ICodeCleanUpFixer> GetFixers()
        {
            return _codeCleanUpFixers.SelectAsArray(lazyFixer => lazyFixer.Value);
        }

        public IReadOnlyCollection<ICodeCleanUpFixer> GetFixers(IContentType contentType)
        {
            var fixers = _codeCleanUpFixers
               .Where(handler => handler.Metadata.ContentTypes.Any(contentType.IsOfType)).ToList();

            return fixers.ConvertAll(l => l.Value);
        }
    }
}
