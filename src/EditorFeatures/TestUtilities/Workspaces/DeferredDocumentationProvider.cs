// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Threading;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    /// <summary>
    /// A simple doc provider used for tests that works by wrapping a readl compilation from another language and 
    /// deffering doc questions to it.
    /// </summary>
    internal class TestDeferredDocumentationProvider : DocumentationProvider
    {
        private readonly Compilation _compilation;

        public TestDeferredDocumentationProvider(Compilation compilation)
        {
            _compilation = compilation;
        }

        protected override string? GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default)
        {
            var symbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(documentationMemberID, _compilation);
            return symbol?.GetDocumentationCommentXml(preferredCulture, cancellationToken: cancellationToken) ?? "";
        }

        public override bool Equals(object? obj)
            => object.ReferenceEquals(this, obj);

        public override int GetHashCode()
            => _compilation.GetHashCode();
    }
}
