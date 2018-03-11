// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Globalization;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal class DeferredDocumentationProvider : DocumentationProvider
    {
        private readonly Compilation _compilation;

        public DeferredDocumentationProvider(Compilation compilation)
        {
            _compilation = compilation;
        }

        protected override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default)
        {
            var symbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(documentationMemberID, _compilation);

            if (symbol != null)
            {
                return symbol.GetDocumentationCommentXml(preferredCulture, cancellationToken: cancellationToken);
            }

            return string.Empty;
        }

        public override bool Equals(object obj)
        {
            return object.ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return _compilation.GetHashCode();
        }
    }
}
