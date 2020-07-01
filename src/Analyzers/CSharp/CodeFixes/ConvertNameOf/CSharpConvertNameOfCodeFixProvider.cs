// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
           => ImmutableArray.Create(IDEDiagnosticIds.ConvertNameOfDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
               context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;
            foreach (var diagnostic in diagnostics)
            {
                var node = (MemberAccessExpressionSyntax)root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                ConvertNameOf(editor, node);
            }

            return Task.CompletedTask;
        }

        internal static void ConvertNameOf(
            SyntaxEditor editor, MemberAccessExpressionSyntax node)
        {
            var exp = (TypeOfExpressionSyntax)node.Expression;
            var idName = exp.Type.ToString();

            var nameOfSyntax = InvocationExpression(IdentifierName("nameof")).
                WithArgumentList(
                ArgumentList(
                    SingletonSeparatedList<ArgumentSyntax>(
                        Argument(
                            IdentifierName(idName)))));

            editor.ReplaceNode(node, nameOfSyntax);
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpAnalyzersResources.Convert_type_name_to_nameof, createChangedDocument)
            {
            }
        }
    }
}
