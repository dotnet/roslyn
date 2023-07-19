// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Language.CodeCleanUp;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup
{
    internal abstract class AbstractCodeCleanUpFixerProvider : ICodeCleanUpFixerProvider
    {
        private readonly ImmutableArray<Lazy<AbstractCodeCleanUpFixer, ContentTypeMetadata>> _codeCleanUpFixers;

        protected AbstractCodeCleanUpFixerProvider(
            IEnumerable<Lazy<AbstractCodeCleanUpFixer, ContentTypeMetadata>> codeCleanUpFixers)
        {
            _codeCleanUpFixers = codeCleanUpFixers.ToImmutableArray();
        }

        public IReadOnlyCollection<ICodeCleanUpFixer> GetFixers()
            => _codeCleanUpFixers.SelectAsArray(lazyFixer => lazyFixer.Value);

        public IReadOnlyCollection<ICodeCleanUpFixer> GetFixers(IContentType contentType)
            => _codeCleanUpFixers.WhereAsArray(handler => handler.Metadata.ContentTypes.Any(contentType.IsOfType))
                                 .SelectAsArray(l => l.Value);
    }
}
