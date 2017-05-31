// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private partial class AssemblyReference : Reference
        {
            private readonly ReferenceAssemblyWithTypeResult _referenceAssemblyWithType;

            public AssemblyReference(
                AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider,
                SearchResult searchResult,
                ReferenceAssemblyWithTypeResult referenceAssemblyWithType)
                : base(provider, searchResult)
            {
                _referenceAssemblyWithType = referenceAssemblyWithType;
            }

            public override async Task<CodeAction> CreateCodeActionAsync(
                Document document, SyntaxNode node, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
            {
                var textChanges = await GetTextChangesAsync(
                    document, node, placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);

                return new AssemblyReferenceCodeAction(this, document, textChanges);
            }

            public override bool Equals(object obj)
            {
                var reference = obj as AssemblyReference;
                return base.Equals(obj) &&
                    _referenceAssemblyWithType.AssemblyName == reference._referenceAssemblyWithType.AssemblyName;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(_referenceAssemblyWithType.AssemblyName, base.GetHashCode());
            }
        }
    }
}