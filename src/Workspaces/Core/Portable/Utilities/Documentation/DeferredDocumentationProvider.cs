// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Globalization;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal class DeferredDocumentationProvider : DocumentationProvider
    {
        private readonly Compilation _compilation;

        public DeferredDocumentationProvider(Compilation compilation)
            => _compilation = compilation;

        protected override string? GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default)
        {
            var symbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(documentationMemberID, _compilation);

            if (symbol != null)
            {
                return symbol.GetDocumentationCommentXml(preferredCulture, cancellationToken: cancellationToken);
            }

            return string.Empty;
        }

        public override bool Equals(object? obj)
            => object.ReferenceEquals(this, obj);

        public override int GetHashCode()
            => _compilation.GetHashCode();
    }
}
