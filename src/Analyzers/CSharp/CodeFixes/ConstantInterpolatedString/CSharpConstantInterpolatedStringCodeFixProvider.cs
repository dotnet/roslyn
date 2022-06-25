// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.ConstantInterpolatedString;
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
            while (operation is not null)
            {
                AddToBuilder(builder, operation.RightOperand);

                if (operation.LeftOperand is IBinaryOperation leftOperand)
                {
                    operation = leftOperand;
                }
                else
                {
                    AddToBuilder(builder, operation.LeftOperand);
                    break;
                }
            }

            builder.ReverseContents();

            return SyntaxFactory.InterpolatedStringExpression(stringStartToken: SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken), SyntaxFactory.List(builder));
        }

        private static void AddToBuilder(ArrayBuilder<InterpolatedStringContentSyntax> builder, IOperation operation)
        {
            if (operation.Kind == OperationKind.Literal)
            {
                var constantValue = ((string)operation.ConstantValue.Value!).Replace("{", "{{").Replace("}", "}}");
                builder.Add(SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(leading: default, SyntaxKind.InterpolatedStringTextToken, text: constantValue, valueText: constantValue, trailing: default)));
            }
            else
            {
                builder.Add(SyntaxFactory.Interpolation((ExpressionSyntax)operation.Syntax));
            }
        }
    }
}
