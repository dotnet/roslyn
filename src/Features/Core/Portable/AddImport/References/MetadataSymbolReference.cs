﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        private partial class MetadataSymbolReference : SymbolReference
        {
            private readonly ProjectId _referenceProjectId;
            private readonly PortableExecutableReference _reference;

            public MetadataSymbolReference(
                AbstractAddImportFeatureService<TSimpleNameSyntax> provider,
                SymbolResult<INamespaceOrTypeSymbol> symbolResult,
                ProjectId referenceProjectId,
                PortableExecutableReference reference)
                : base(provider, symbolResult)
            {
                _referenceProjectId = referenceProjectId;
                _reference = reference;
            }

            /// <summary>
            /// If we're adding a metadata-reference, then we always offer to do the add,
            /// even if there's an existing source-import in the file.
            /// </summary>
            protected override bool ShouldAddWithExistingImport(Document document) => true;

            protected override (string description, bool hasExistingImport) GetDescription(
                Document document, SyntaxNode node,
                SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var (description, hasExistingImport) = base.GetDescription(document, node, semanticModel, cancellationToken);
                if (description == null)
                {
                    return (null, false);
                }

                return (string.Format(FeaturesResources.Add_reference_to_0, Path.GetFileName(_reference.FilePath)),
                        hasExistingImport);
            }

            protected override AddImportFixData GetFixData(
                Document document, ImmutableArray<TextChange> textChanges, string description,
                ImmutableArray<string> tags, CodeActionPriority priority)
            {
                return AddImportFixData.CreateForMetadataSymbol(
                    textChanges, description, tags, priority,
                    _referenceProjectId, _reference.FilePath);
            }

            // Adding metadata references should be considered lower pri than anything else.
            protected override CodeActionPriority GetPriority(Document document)
                => CodeActionPriority.Low;

            protected override ImmutableArray<string> GetTags(Document document)
                => WellKnownTagArrays.AddReference;

            public override bool Equals(object obj)
            {
                var reference = obj as MetadataSymbolReference;
                return base.Equals(reference) &&
                    StringComparer.OrdinalIgnoreCase.Equals(_reference.FilePath, reference._reference.FilePath);
            }

            public override int GetHashCode()
                => Hash.Combine(
                    base.GetHashCode(),
                    StringComparer.OrdinalIgnoreCase.GetHashCode(_reference.FilePath));
        }
    }
}
