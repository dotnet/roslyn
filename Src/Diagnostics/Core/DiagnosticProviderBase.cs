// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
    public abstract class DiagnosticProviderBase : IDiagnosticProvider
    {
        public abstract IEnumerable<DiagnosticDescriptor> GetSupportedDiagnostics();

        protected abstract IEnumerable<SyntaxNode> GetNodes(SyntaxNode root);

        protected abstract IEnumerable<Diagnostic> GetDiagnosticsForNode(SyntaxNode node, SemanticModel model);

        bool IDiagnosticProvider.IsSupported(DiagnosticCategory category)
        {
            return category == DiagnosticCategory.SemanticInDocument;
        }

        async Task<IEnumerable<Diagnostic>> IDiagnosticProvider.GetSemanticDiagnosticsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            if (this.semanticsService == null)
            {
                this.semanticsService = LanguageService.GetService<ISemanticFactsService>(document);
            }

            IEnumerable<Diagnostic> diagnostics = SpecializedCollections.EmptyEnumerable<Diagnostic>();

            var model = await document.GetSemanticModelAsync(cancellationToken);
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            Contract.Requires(root.FullSpan == span);

            foreach (var node in GetNodes(root))
            {
                var newDiags = GetDiagnosticsForNode(node, model);
                if (newDiags != null)
                {
                    diagnostics = diagnostics.Concat(newDiags);
                }
            }

            return diagnostics;
        }

        Task<IEnumerable<Diagnostic>> IDiagnosticProvider.GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        Task<IEnumerable<Diagnostic>> IDiagnosticProvider.GetSyntaxDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        private ISemanticFactsService semanticsService;

        protected bool IsAssignableTo(ITypeSymbol fromSymbol, ITypeSymbol toSymbol, Compilation compilation)
        {
            return this.semanticsService.IsAssignableTo(fromSymbol, toSymbol, compilation);
        }
    }
}
