// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Packaging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private class AssemblyReference : Reference
        {
            private readonly string _assemblyName;

            public AssemblyReference(
                AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider,
                IPackageInstallerService installerService,
                SearchResult searchResult,
                string assemblyName)
                : base(provider, searchResult)
            {
                _assemblyName = assemblyName;
            }

            public override Task<CodeAction> CreateCodeActionAsync(
                Document document, SyntaxNode node, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
            {
                return Task.FromResult<CodeAction>(new AssemblyReferenceCodeAction(
                    this, document, node, placeSystemNamespaceFirst));
            }

            public override bool Equals(object obj)
            {
                var reference = obj as AssemblyReference;
                return base.Equals(obj) &&
                    _assemblyName == reference._assemblyName;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(_assemblyName, base.GetHashCode());
            }

            private class AssemblyReferenceCodeAction : CodeAction
            {
                private readonly AssemblyReference _reference;
                private readonly string _title;
                private readonly Document _document;
                private readonly SyntaxNode _node;
                private readonly bool _placeSystemNamespaceFirst;

                public override string Title => _title;

                internal override int? Glyph => (int)CodeAnalysis.Glyph.Reference;

                private readonly Lazy<string> _lazyResolvedPath;

                public AssemblyReferenceCodeAction(
                    AssemblyReference reference,
                    Document document,
                    SyntaxNode node,
                    bool placeSystemNamespaceFirst)
                {
                    _reference = reference;
                    _document = document;
                    _node = node;
                    _placeSystemNamespaceFirst = placeSystemNamespaceFirst;

                    _title = $"{reference.provider.GetDescription(reference.SearchResult.NameParts)} ({string.Format(FeaturesResources.from_0, reference._assemblyName)})";
                    _lazyResolvedPath = new Lazy<string>(ResolvePath);
                }

                private string ResolvePath()
                {
                    var metadataService = _document.Project.Solution.Workspace.Services.GetService<IFrameworkAssemblyPathResolver>();
                    var assemblyPath = metadataService?.ResolveAssemblyPath(_document.Project.Id, _reference._assemblyName);
                    return assemblyPath;
                }

                internal override bool PerformFinalApplicabilityCheck => true;

                internal override bool IsApplicable(Workspace workspace)
                {
                    return !string.IsNullOrWhiteSpace(_lazyResolvedPath.Value);
                }

                protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
                {
                    var service = _document.Project.Solution.Workspace.Services.GetService<IMetadataService>();
                    var resolvedPath = _lazyResolvedPath.Value;
                    var reference = service.GetReference(resolvedPath, MetadataReferenceProperties.Assembly);

                    // First add the "using/import" directive in the code.
                    var node = _node;
                    var document = _document;
                    _reference.ReplaceNameNode(ref node, ref document, cancellationToken);

                    var newDocument = await _reference.provider.AddImportAsync(
                        node, _reference.SearchResult.NameParts, document, _placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);

                    // Now add the actual assembly reference.
                    var newProject = newDocument.Project;
                    newProject = newProject.WithMetadataReferences(
                        newProject.MetadataReferences.Concat(reference));

                    var operation = new ApplyChangesOperation(newProject.Solution);
                    return SpecializedCollections.SingletonEnumerable<CodeActionOperation>(operation);
                }
            }
        }
    }
}
