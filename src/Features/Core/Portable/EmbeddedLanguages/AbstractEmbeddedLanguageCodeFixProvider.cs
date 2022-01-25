//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.CodeAnalysis.CodeFixes;
//using Microsoft.CodeAnalysis.Editing;

//namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages
//{
//    internal abstract class AbstractEmbeddedLanguageCodeFixProvider : SyntaxEditorBasedCodeFixProvider
//    {
//        private readonly Dictionary<string, SyntaxEditorBasedCodeFixProvider> _diagnosticIdToCodeFixProvider;

//        public override ImmutableArray<string> FixableDiagnosticIds { get; }

//        protected AbstractEmbeddedLanguageCodeFixProvider(
//            IEmbeddedLanguageFeaturesProvider languagesProvider)
//        {
//            _diagnosticIdToCodeFixProvider = new Dictionary<string, SyntaxEditorBasedCodeFixProvider>();

//            // Create a mapping from each IEmbeddedCodeFixProvider.FixableDiagnosticIds back to the
//            // IEmbeddedCodeFixProvider itself.  That way, when we hear about diagnostics, we know
//            // which provider to actually do the fixing.
//            foreach (var language in languagesProvider.Languages)
//            {
//                var codeFixProvider = language.CodeFixProvider;
//                if (codeFixProvider != null)
//                {
//                    foreach (var diagnosticId in codeFixProvider.FixableDiagnosticIds)
//                    {
//                        // 'Add' is intentional.  We want to throw if multiple fix providers
//                        // register for the say diagnostic ID.
//                        _diagnosticIdToCodeFixProvider.Add(diagnosticId, codeFixProvider);
//                    }
//                }
//            }

//            this.FixableDiagnosticIds = _diagnosticIdToCodeFixProvider.Keys.ToImmutableArray();
//        }

//        public override Task RegisterCodeFixesAsync(CodeFixContext context)
//        {
//            if (TryGetProvider(context.Diagnostics, out var provider))
//            {
//                return provider.RegisterCodeFixesAsync(context);
//            }

//            return Task.CompletedTask;
//        }

//        private bool TryGetProvider(ImmutableArray<Diagnostic> diagnostics, out SyntaxEditorBasedCodeFixProvider provider)
//        {
//            if (diagnostics.Length > 0)
//            {
//                return _diagnosticIdToCodeFixProvider.TryGetValue(diagnostics[0].Id, out provider);
//            }

//            provider = null;
//            return false;
//        }

//        protected override Task FixAllAsync(
//            Document document, ImmutableArray<Diagnostic> diagnostics,
//            SyntaxEditor editor, CancellationToken cancellationToken)
//        {
//            if (TryGetProvider(diagnostics, out var provider))
//            {
//                return provider.InternalFixAllAsync(document, diagnostics, editor, cancellationToken);
//            }

//            return Task.CompletedTask;
//        }
//    }
//}
