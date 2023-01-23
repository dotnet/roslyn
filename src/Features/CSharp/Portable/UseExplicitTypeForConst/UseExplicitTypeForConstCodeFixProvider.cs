// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseExplicitTypeForConst
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseExplicitTypeForConst), Shared]
    internal sealed class UseExplicitTypeForConstCodeFixProvider : CodeFixProvider
    {
        private const string CS0822 = nameof(CS0822); // Implicitly-typed variables cannot be constant

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public UseExplicitTypeForConstCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(CS0822);

        public override FixAllProvider GetFixAllProvider()
        {
            // This code fix addresses a very specific compiler error. It's unlikely there will be more than 1 of them at a time.
            return null;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root.FindNode(context.Span) is VariableDeclarationSyntax variableDeclaration &&
                variableDeclaration.Variables.Count == 1)
            {
                var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

                var type = semanticModel.GetTypeInfo(variableDeclaration.Type, context.CancellationToken).ConvertedType;
                if (type == null || type.TypeKind == TypeKind.Error || type.IsAnonymousType)
                {
                    return;
                }

                context.RegisterCodeFix(
                    CodeAction.Create(
                        CSharpAnalyzersResources.Use_explicit_type_instead_of_var,
                        c => FixAsync(context.Document, context.Span, type, c),
                        nameof(CSharpAnalyzersResources.Use_explicit_type_instead_of_var)),
                    context.Diagnostics);
            }
        }

        private static async Task<Document> FixAsync(
            Document document, TextSpan span, ITypeSymbol type, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var variableDeclaration = (VariableDeclarationSyntax)root.FindNode(span);

            var newRoot = root.ReplaceNode(variableDeclaration.Type, type.GenerateTypeSyntax(allowVar: false));
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
