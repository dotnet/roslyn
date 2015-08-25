// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : ISuppressionFixProvider
    {
        internal sealed class GlobalSuppressMessageCodeAction : CodeAction
        {
            private readonly AbstractSuppressionCodeFixProvider _fixer;
            private readonly string _title;
            private readonly ISymbol _targetSymbol;
            private readonly Document _document;
            private readonly Diagnostic _diagnostic;

            public GlobalSuppressMessageCodeAction(AbstractSuppressionCodeFixProvider fixer, ISymbol targetSymbol, Document document, Diagnostic diagnostic)
            {
                _fixer = fixer;

                _targetSymbol = targetSymbol;
                _document = document;
                _diagnostic = diagnostic;

                _title = FeaturesResources.SuppressWithGlobalSuppressMessage;
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            {
                var changedSuppressionDocument = await GetChangedSuppressionDocumentAsync(cancellationToken).ConfigureAwait(false);
                return new CodeActionOperation[]
                {
                    new ApplyChangesOperation(changedSuppressionDocument.Project.Solution),
                    new OpenDocumentOperation(changedSuppressionDocument.Id, activateIfAlreadyOpen: true),
                    new NavigationOperation(changedSuppressionDocument.Id, position: 0)
                };
            }

            public override string Title
            {
                get
                {
                    return _title;
                }
            }

            private async Task<Document> GetChangedSuppressionDocumentAsync(CancellationToken cancellationToken)
            {
                var suppressionsDoc = await GetOrCreateSuppressionsDocumentAsync(_document, cancellationToken).ConfigureAwait(false);
                var suppressionsRoot = await suppressionsDoc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var newSuppressionsRoot = _fixer.AddGlobalSuppressMessageAttribute(suppressionsRoot, _targetSymbol, _diagnostic);
                return suppressionsDoc.WithSyntaxRoot(newSuppressionsRoot);
            }

            private async Task<Document> GetOrCreateSuppressionsDocumentAsync(Document document, CancellationToken c)
            {
                int index = 1;
                var suppressionsFileName = s_globalSuppressionsFileName + _fixer.DefaultFileExtension;
                if (document.Name == suppressionsFileName)
                {
                    index++;
                    suppressionsFileName = s_globalSuppressionsFileName + index.ToString() + _fixer.DefaultFileExtension;
                }

                Document suppressionsDoc = null;
                while (suppressionsDoc == null)
                {
                    var hasDocWithSuppressionsName = false;
                    foreach (var d in document.Project.Documents)
                    {
                        if (d.Name == suppressionsFileName)
                        {
                            // Existing global suppressions file, see if this file only has global assembly attributes.
                            hasDocWithSuppressionsName = true;

                            var t = await d.GetSyntaxTreeAsync(c).ConfigureAwait(false);
                            var r = await t.GetRootAsync(c).ConfigureAwait(false);
                            if (r.ChildNodes().All(n => _fixer.IsAttributeListWithAssemblyAttributes(n)))
                            {
                                suppressionsDoc = d;
                                break;
                            }
                        }
                    }

                    if (suppressionsDoc == null)
                    {
                        if (hasDocWithSuppressionsName)
                        {
                            index++;
                            suppressionsFileName = s_globalSuppressionsFileName + index.ToString() + _fixer.DefaultFileExtension;
                        }
                        else
                        {
                            // Create an empty global suppressions file.
                            suppressionsDoc = document.Project.AddDocument(suppressionsFileName, string.Empty);
                        }
                    }
                }

                return suppressionsDoc;
            }
        }
    }
}
