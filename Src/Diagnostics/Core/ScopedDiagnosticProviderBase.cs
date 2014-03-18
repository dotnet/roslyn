// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopRules.DiagnosticProviders
{
    public abstract class ScopedDiagnosticProviderBase : ScopedDiagnosticProvider
    {
        protected abstract IEnumerable<SyntaxNode> GetNodes(SyntaxNode root, TextSpan span);

        protected abstract IEnumerable<Diagnostic> GetDiagnosticsForNode(SyntaxNode node, SemanticModel model);

        protected override async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            if (this.semanticsService == null)
            {
                this.semanticsService = LanguageService.GetService<ISemanticFactsService>(document);
            }

            IEnumerable<Diagnostic> diagnostics = SpecializedCollections.EmptyEnumerable<Diagnostic>();

            var model = await document.GetSemanticModelAsync(cancellationToken);
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            foreach (var node in GetNodes(root, span))
            {
                var newDiags = GetDiagnosticsForNode(node, model);
                if (newDiags != null)
                {
                    diagnostics = diagnostics.Concat(newDiags);
                }
            }

            return diagnostics;
        }

        private ISemanticFactsService semanticsService;

        protected bool IsAssignableTo(ITypeSymbol fromSymbol, ITypeSymbol toSymbol, Compilation compilation)
        {
            Debug.Assert(semanticsService != null);
            return this.semanticsService.IsAssignableTo(fromSymbol, toSymbol, compilation);
        }
    }
}
