// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ConvertNumericLiteral
{
    internal abstract class AbstractConvertNumericLiteralCodeRefactoringProvider<TNumericLiteralExpression> : CodeRefactoringProvider where TNumericLiteralExpression : SyntaxNode
    {
        protected abstract (string hexPrefix, string binaryPrefix) GetNumericLiteralPrefixes();

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            var numericToken = await GetNumericTokenAsync(context).ConfigureAwait(false);

            if (numericToken == default || numericToken.ContainsDiagnostics)
            {
                return;
            }

            var syntaxNode = numericToken.Parent;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = semanticModel.GetTypeInfo(syntaxNode, cancellationToken).Type;
            if (symbol == null)
            {
                return;
            }

            if (!IsIntegral(symbol.SpecialType))
            {
                return;
            }

            var valueOpt = semanticModel.GetConstantValue(syntaxNode);
            if (!valueOpt.HasValue)
            {
                return;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var value = IntegerUtilities.ToInt64(valueOpt.Value);
            var numericText = numericToken.ToString();
            var (hexPrefix, binaryPrefix) = GetNumericLiteralPrefixes();
            var (prefix, number, suffix) = GetNumericLiteralParts(numericText, hexPrefix, binaryPrefix);
            var kind = string.IsNullOrEmpty(prefix) ? NumericKind.Decimal
                : prefix.Equals(hexPrefix, StringComparison.OrdinalIgnoreCase) ? NumericKind.Hexadecimal
                : prefix.Equals(binaryPrefix, StringComparison.OrdinalIgnoreCase) ? NumericKind.Binary
                : NumericKind.Unknown;

            if (kind == NumericKind.Unknown)
            {
                return;
            }

            if (kind != NumericKind.Decimal)
            {
                RegisterRefactoringWithResult(value.ToString(), FeaturesResources.Convert_to_decimal);
            }

            if (kind != NumericKind.Binary)
            {
                RegisterRefactoringWithResult(binaryPrefix + Convert.ToString(value, 2), FeaturesResources.Convert_to_binary);
            }

            if (kind != NumericKind.Hexadecimal)
            {
                RegisterRefactoringWithResult(hexPrefix + value.ToString("X"), FeaturesResources.Convert_to_hex);
            }

            const string DigitSeparator = "_";
            if (numericText.Contains(DigitSeparator))
            {
                RegisterRefactoringWithResult(prefix + number.Replace(DigitSeparator, string.Empty), FeaturesResources.Remove_separators);
            }
            else
            {
                switch (kind)
                {
                    case NumericKind.Decimal when number.Length > 3:
                        RegisterRefactoringWithResult(AddSeparators(number, 3), FeaturesResources.Separate_thousands);
                        break;

                    case NumericKind.Hexadecimal when number.Length > 4:
                        RegisterRefactoringWithResult(hexPrefix + AddSeparators(number, 4), FeaturesResources.Separate_words);
                        break;

                    case NumericKind.Binary when number.Length > 4:
                        RegisterRefactoringWithResult(binaryPrefix + AddSeparators(number, 4), FeaturesResources.Separate_nibbles);
                        break;
                }
            }

            void RegisterRefactoringWithResult(string text, string title)
            {
                context.RegisterRefactoring(new MyCodeAction(title, c =>
                {
                    var generator = SyntaxGenerator.GetGenerator(document);
                    var updatedToken = generator.NumericLiteralToken(text + suffix, (ulong)value)
                        .WithTriviaFrom(numericToken);
                    var updatedRoot = root.ReplaceToken(numericToken, updatedToken);
                    return Task.FromResult(document.WithSyntaxRoot(updatedRoot));
                }));
            }
        }

        internal virtual async Task<SyntaxToken> GetNumericTokenAsync(CodeRefactoringContext context)
        {
            var syntaxFacts = context.Document.GetLanguageService<ISyntaxFactsService>();

            var literalNode = await context.TryGetSelectedNodeAsync<TNumericLiteralExpression>().ConfigureAwait(false);
            var numericLiteralExpressionNode = syntaxFacts.IsNumericLiteralExpression(literalNode)
                ? literalNode
                : null;

            return numericLiteralExpressionNode != null
                ? numericLiteralExpressionNode.GetFirstToken()    // We know that TNumericLiteralExpression has always only one token: NumericLiteralToken
                : default;
        }

        private static (string prefix, string number, string suffix) GetNumericLiteralParts(string numericText, string hexPrefix, string binaryPrefix)
        {
            // Match literal text and extract out base prefix, type suffix and the number itself.
            var groups = Regex.Match(numericText, $"({hexPrefix}|{binaryPrefix})?([_0-9a-f]+)(.*)", RegexOptions.IgnoreCase).Groups;
            return (prefix: groups[1].Value, number: groups[2].Value, suffix: groups[3].Value);
        }

        private static string AddSeparators(string numericText, int interval)
        {
            // Insert digit separators in the given interval.
            var result = Regex.Replace(numericText, $"(.{{{interval}}})", "_$1", RegexOptions.RightToLeft);
            // Fix for the case "0x_1111" that is not supported yet.
            return result[0] == '_' ? result.Substring(1) : result;
        }

        private static bool IsIntegral(SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    return true;

                default:
                    return false;
            }
        }

        private enum NumericKind { Unknown, Decimal, Binary, Hexadecimal }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) : base(title, createChangedDocument)
            {
            }
        }
    }
}
