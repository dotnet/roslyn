// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages
{
    internal readonly struct AspNetCoreEmbeddedLanguageClassificationContext
    {
        private readonly EmbeddedLanguageClassificationContext _context;

        internal AspNetCoreEmbeddedLanguageClassificationContext(
            EmbeddedLanguageClassificationContext context)
        {
            _context = context;
        }

        /// <inheritdoc cref="EmbeddedLanguageClassificationContext.SyntaxToken"/>
        public SyntaxToken SyntaxToken => _context.SyntaxToken;

        /// <inheritdoc cref="EmbeddedLanguageClassificationContext.SemanticModel"/>
        public SemanticModel SemanticModel => _context.SemanticModel;

        /// <inheritdoc cref="EmbeddedLanguageClassificationContext.CancellationToken"/>
        public CancellationToken CancellationToken => _context.CancellationToken;

        public void AddClassification(string classificationType, TextSpan span)
            => _context.AddClassification(classificationType, span);
    }
}
