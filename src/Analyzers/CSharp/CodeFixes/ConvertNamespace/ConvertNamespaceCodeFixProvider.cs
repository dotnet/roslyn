﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertNamespace
{
    using static ConvertNamespaceAnalysis;
    using static ConvertNamespaceTransform;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ConvertNamespace), Shared]
    internal class ConvertNamespaceCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConvertNamespaceCodeFixProvider()
        {
        }

        internal override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseBlockScopedNamespaceDiagnosticId, IDEDiagnosticIds.UseFileScopedNamespaceDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();

            var (title, equivalenceKey) = GetInfo(
                diagnostic.Id switch
                {
                    IDEDiagnosticIds.UseBlockScopedNamespaceDiagnosticId => NamespaceDeclarationPreference.BlockScoped,
                    IDEDiagnosticIds.UseFileScopedNamespaceDiagnosticId => NamespaceDeclarationPreference.FileScoped,
                    _ => throw ExceptionUtilities.UnexpectedValue(diagnostic.Id),
                });

            context.RegisterCodeFix(
                new MyCodeAction(title, c => FixAsync(context.Document, diagnostic, c), equivalenceKey),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var diagnostic = diagnostics.First();

            var namespaceDecl = (BaseNamespaceDeclarationSyntax)diagnostic.AdditionalLocations[0].FindNode(cancellationToken);
            var converted = Convert(namespaceDecl);

            editor.ReplaceNode(namespaceDecl, converted);
            return Task.CompletedTask;
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey)
                : base(title, createChangedDocument, equivalenceKey)
            {
            }
        }
    }
}
