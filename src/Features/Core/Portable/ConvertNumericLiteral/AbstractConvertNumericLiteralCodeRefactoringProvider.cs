// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.ConvertNumericLiteral
{
    internal abstract class AbstractConvertNumericLiteralCodeRefactoringProvider : CodeRefactoringProvider
    {
        private readonly string _hexPrefix;
        private readonly string _binaryPrefix;

        public AbstractConvertNumericLiteralCodeRefactoringProvider(string hexPrefix, string binaryPrefix)
            => (_hexPrefix, _binaryPrefix) = (hexPrefix, binaryPrefix);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var numericToken = await root.SyntaxTree.GetTouchingTokenAsync(context.Span.Start,
                token => syntaxFacts.IsNumericLiteralExpression(token.Parent), cancellationToken).ConfigureAwait(false);

            if (numericToken == default)
            {
                return;
            }

            if (numericToken.ContainsDiagnostics)
            {
                return;
            }

            if (context.Span.Length > 0 && 
                context.Span != numericToken.Span)
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

            var value = IntegerUtilities.ToInt64(valueOpt.Value);
            var numericText = numericToken.ToString();
            var (prefix, number, suffix) = GetNumericLiteralParts(numericText);
            var kind = GetNumericKind(prefix);
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
                RegisterRefactoringWithResult(_binaryPrefix + Convert.ToString(value, 2), FeaturesResources.Convert_to_binary);
            }

            if (kind != NumericKind.Hexadecimal)
            {
                RegisterRefactoringWithResult(_hexPrefix + value.ToString("X"), FeaturesResources.Convert_to_hex);
            }

            const string DigitSeparator = "_";
            if (numericText.Contains(DigitSeparator))
            {
                RegisterRefactoringWithResult(prefix + number.Replace(DigitSeparator, string.Empty), FeaturesResources.Remove_separators);
            }
            else
            {
                var supportsLeadingUnderscore = this.SupportLeadingUnderscore(root.SyntaxTree.Options);

                switch (kind)
                {
                    case NumericKind.Decimal when number.Length > 3:
                        RegisterRefactoringWithResult(AddSeparators(number, interval: 3, supportsLeadingUnderscore: false), FeaturesResources.Separate_thousands);
                        break;

                    case NumericKind.Hexadecimal when number.Length > 4:
                        RegisterRefactoringWithResult(_hexPrefix + AddSeparators(number, 4, supportsLeadingUnderscore), FeaturesResources.Separate_words);
                        break;

                    case NumericKind.Binary when number.Length > 4:
                        RegisterRefactoringWithResult(_binaryPrefix + AddSeparators(number, 4, supportsLeadingUnderscore), FeaturesResources.Separate_nibbles);
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

        private NumericKind GetNumericKind(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return NumericKind.Decimal;
            }
            else if (prefix.Equals(_hexPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return NumericKind.Hexadecimal;
            }
            else if (prefix.Equals(_binaryPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return NumericKind.Binary;
            }
            else
            {
                return NumericKind.Unknown;
            }
        }

        private (string prefix, string number, string suffix) GetNumericLiteralParts(string numericText)
        {
            // Match literal text and extract out base prefix, type suffix and the number itself.
            var groups = Regex.Match(numericText, $"({_hexPrefix}|{_binaryPrefix})?([_0-9a-f]+)(.*)", RegexOptions.IgnoreCase).Groups;
            return (prefix: groups[1].Value, number: groups[2].Value, suffix: groups[3].Value);
        }

        private static string AddSeparators(string numericText, int interval, bool supportsLeadingUnderscore)
        {
            // Insert digit separators in the given interval.
            var result = Regex.Replace(numericText, $"(.{{{interval}}})", "_$1", RegexOptions.RightToLeft);
            // Fix for the case "0x_1111" if it's not supported
            return result[0] == '_' && !supportsLeadingUnderscore ? result.Substring(1) : result;
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

        protected abstract bool SupportLeadingUnderscore(ParseOptions options);

        private enum NumericKind { Unknown, Decimal, Binary, Hexadecimal }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) : base(title, createChangedDocument)
            {
            }
        }
    }
}
