// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.ConstantInterpolatedString
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseConstantInterpolatedString), Shared]
    internal sealed class CSharpConstantInterpolatedStringCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpConstantInterpolatedStringCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.UseConstantInterpolatedStringDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
                RegisterCodeFix(context, CSharpAnalyzersResources.Use_constant_interpolated_string, nameof(CSharpAnalyzersResources.Use_constant_interpolated_string), diagnostic);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = editor.OriginalRoot;
            foreach (var diagnostic in diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                var operation = semanticModel.GetRequiredOperation(node, cancellationToken);
                if (operation is IBinaryOperation binaryOperation)
                {
                    editor.ReplaceNode(node, GetReplacement(binaryOperation));
                }
            }
        }

        private static SyntaxNode GetReplacement(IBinaryOperation operation)
        {
            using var _ = ArrayBuilder<InterpolatedStringContentSyntax>.GetInstance(out var builder);
            var constantValue = string.Empty;
            var stack = new Stack<IOperation>();
            stack.Push(operation);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current is IBinaryOperation binaryOperation)
                {
                    stack.Push(binaryOperation.RightOperand);
                    stack.Push(binaryOperation.LeftOperand);
                }
                else
                {
                    AddToBuilder(builder, current, ref constantValue);
                }
            }

            AddInterpolatedStringText(builder, ref constantValue);
            return SyntaxFactory.InterpolatedStringExpression(stringStartToken: SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken), SyntaxFactory.List(builder));
        }

        private static void AddToBuilder(ArrayBuilder<InterpolatedStringContentSyntax> builder, IOperation operation, ref string constantValue)
        {
            if (operation.Kind == OperationKind.Literal)
            {
                var newConstantValue = (string)operation.ConstantValue.Value!;
                constantValue += newConstantValue;
            }
            else
            {

                AddInterpolatedStringText(builder, ref constantValue);
                builder.Add(SyntaxFactory.Interpolation((ExpressionSyntax)operation.Syntax));
            }
        }

        private static void AddInterpolatedStringText(ArrayBuilder<InterpolatedStringContentSyntax> builder, ref string constantValue)
        {
            if (constantValue.Length > 0)
            {
                var valueText = constantValue.Replace("{", "{{").Replace("}", "}}");
                var text = valueText.Replace("\r", "\\r").Replace("\n", "\\n");
                builder.Add(SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(leading: default, SyntaxKind.InterpolatedStringTextToken, text: text, valueText: valueText, trailing: default)));
                constantValue = string.Empty;
            }
        }
    }
}
