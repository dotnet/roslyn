// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.CodeCleanUp;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup
{
    /// <summary>
    /// This is intentionally not exported as a concrete type and not an instance of
    /// <see cref="ICodeCleanUpFixerProvider"/>. Roslyn is responsible for registering its own fixer provider, as
    /// opposed to the implementation importing the instances of some interface.
    /// </summary>
    [Export]
    internal class CodeCleanUpFixerProvider : ICodeCleanUpFixerProvider
    {
        private readonly IList<Lazy<CodeCleanUpFixer, ContentTypeMetadata>> _codeCleanUpFixers;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeCleanUpFixerProvider(
            [ImportMany] IEnumerable<Lazy<CodeCleanUpFixer, ContentTypeMetadata>> codeCleanUpFixers)
        {
            _codeCleanUpFixers = codeCleanUpFixers.ToList();
        }

        public IReadOnlyCollection<ICodeCleanUpFixer> GetFixers()
        {
            var fixers = new List<CodeCleanUpFixer>();
            foreach (var fixerLazy in _codeCleanUpFixers)
            {
                fixers.Add(fixerLazy.Value);
            }

            return fixers;
        }

        public IReadOnlyCollection<ICodeCleanUpFixer> GetFixers(IContentType contentType)
        {
            var fixers = _codeCleanUpFixers
               .Where(handler => handler.Metadata.ContentTypes.Contains(contentType.TypeName)).ToList();

            return fixers.ConvertAll(l => l.Value);
        }
    }
}
