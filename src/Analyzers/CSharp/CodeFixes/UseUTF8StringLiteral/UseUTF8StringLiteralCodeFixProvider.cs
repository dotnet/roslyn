// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseUTF8StringLiteral
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseUTF8StringLiteral), Shared]
    internal class UseUTF8StringLiteralCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private const char QuoteCharacter = '"';
        private const string Suffix = "u8";

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public UseUTF8StringLiteralCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.UseUTF8StringLiteralDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(CodeAction.Create(
                CSharpAnalyzersResources.Use_UTF8_string_literal,
                c => FixAsync(context.Document, context.Diagnostics[0], c),
                nameof(CSharpAnalyzersResources.Use_UTF8_string_literal)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var arrays = diagnostics.Select(d => d.Location.FindNode(cancellationToken)).ToImmutableArray();

            var root = editor.OriginalRoot;
            var updatedRoot = root.ReplaceNodes(
                arrays,
                (_, current) => ConvertArrayInitializerElementsToUTF8String(current, semanticModel));

            editor.ReplaceNode(root, updatedRoot);
        }

        private static SyntaxNode ConvertArrayInitializerElementsToUTF8String(SyntaxNode node, SemanticModel semanticModel)
        {
            // Convert the expressions into their constant values, as an array
            var arrayElementExpressions = node switch
            {
                ArrayCreationExpressionSyntax array => array.Initializer!.Expressions,
                ImplicitArrayCreationExpressionSyntax array => array.Initializer.Expressions,
                _ => throw ExceptionUtilities.Unreachable
            };

            if (!TryGetStringForByteArrayElements(semanticModel, arrayElementExpressions, out var stringValue))
            {
                // The analyzer shouldn't have issued a diagnostic if the array couldn't be converted
                throw ExceptionUtilities.Unreachable;
            }

            var literal = SyntaxFactory.Token(
                    node.GetLeadingTrivia(),
                    SyntaxKind.UTF8StringLiteralToken,
                    text: QuoteCharacter + stringValue + QuoteCharacter + Suffix,
                    valueText: "",
                    trailing: node.GetTrailingTrivia());

            return SyntaxFactory.LiteralExpression(SyntaxKind.UTF8StringLiteralExpression, literal);
        }

        private static bool TryGetStringForByteArrayElements(SemanticModel semanticModel, SeparatedSyntaxList<ExpressionSyntax> expressions, [NotNullWhen(true)] out string? stringValue)
        {
            stringValue = null;

            var value = new byte[expressions.Count];

            try
            {
                for (var i = 0; i < expressions.Count; i++)
                {
                    var constantValue = semanticModel.GetConstantValue(expressions[i]);
                    if (!constantValue.HasValue || constantValue.Value is null)
                        return false;

                    var byteValue = Convert.ToByte(constantValue.Value);
                    value[i] = byteValue;

                }

                stringValue = Encoding.UTF8.GetString(value);
                return true;
            }
            catch
            {
                // Ignore conversion failures, or GetString failures, and just don't offer the fix
                return false;
            }
        }
    }
}
