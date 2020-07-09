// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.CSharpConvertNameOfCodeFixProvider
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertNameOfCodeFixProvider)), Shared]
    internal partial class CSharpConvertNameOfCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpConvertNameOfCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
           => ImmutableArray.Create(IDEDiagnosticIds.ConvertTypeOfToNameOfDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
               context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var isUsingSystem = root.
                                DescendantNodes().
                                OfType<UsingDirectiveSyntax>().
                                Any(node => node.Name.ToString().Equals("System"));

            foreach (var diagnostic in diagnostics)
            {
                var node = (MemberAccessExpressionSyntax)root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                ConvertNameOf(editor, node, semanticModel, isUsingSystem);
            }
        }

        /**
         * Method converts typeof(...).Name to nameof(...)
         * The isUsingSystem parameter determines whether includes the System directive.
         */
        internal static void ConvertNameOf(
            SyntaxEditor editor, MemberAccessExpressionSyntax node, SemanticModel semanticModel, bool isUsingSystem)
        {
            var exp = (TypeOfExpressionSyntax)node.Expression;
            var idName = exp.Type.ToString();

            //check if exp type is predefined type and convert
            //example: int -> Int32, string -> String, etc
            if (exp.Type.IsKind(SyntaxKind.PredefinedType))
            {
                idName = semanticModel.GetSymbolInfo(exp.Type).Symbol.
                                       GetSymbolType().SpecialType.
                                       ToPredefinedType().ToString();
            }

            //check if user is using System;
            idName = isUsingSystem ? idName : "System." + idName;

            var nameOfSyntax = InvocationExpression(IdentifierName("nameof")).
                               WithArgumentList(
                               ArgumentList(
                               SingletonSeparatedList<ArgumentSyntax>(
                               Argument(
                               IdentifierName(idName)))));

            editor.ReplaceNode(node, nameOfSyntax.WithAdditionalAnnotations(Simplifier.Annotation));
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpAnalyzersResources.Convert_typeof_to_nameof, createChangedDocument)
            {
            }
        }
    }
}
