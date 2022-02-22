// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.Classification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SpellCheck;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SpellCheck
{
    [ExportLanguageService(typeof(ISpellCheckSpanService), LanguageNames.CSharp), Shared]
    internal class CSharpSpellCheckSpanService : AbstractSpellCheckSpanService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSpellCheckSpanService()
        {
        }

        protected override bool IsDeclarationIdentifier(SyntaxToken token)
        {
            // Leverage syntactic classification which already has to determine if an identifier token is the name of
            // some construct.
            var classification = ClassificationHelpers.GetClassificationForIdentifier(token);
            switch (classification)
            {
                case ClassificationTypeNames.ClassName:
                case ClassificationTypeNames.RecordClassName:
                case ClassificationTypeNames.DelegateName:
                case ClassificationTypeNames.EnumName:
                case ClassificationTypeNames.InterfaceName:
                case ClassificationTypeNames.ModuleName:
                case ClassificationTypeNames.StructName:
                case ClassificationTypeNames.RecordStructName:
                case ClassificationTypeNames.TypeParameterName:
                case ClassificationTypeNames.FieldName:
                case ClassificationTypeNames.EnumMemberName:
                case ClassificationTypeNames.ConstantName:
                case ClassificationTypeNames.LocalName:
                case ClassificationTypeNames.ParameterName:
                case ClassificationTypeNames.MethodName:
                case ClassificationTypeNames.ExtensionMethodName:
                case ClassificationTypeNames.PropertyName:
                case ClassificationTypeNames.EventName:
                case ClassificationTypeNames.NamespaceName:
                case ClassificationTypeNames.LabelName:
                    return true;
                default:
                    return false;
            }
        }

        protected override TextSpan GetSpanForComment(SyntaxTrivia trivia)
        {
            switch (trivia.Kind())
            {
                case SyntaxKind.SingleLineCommentTrivia:
                    return TextSpan.FromBounds(trivia.Span.Start + "//".Length, trivia.Span.End);

                case SyntaxKind.MultiLineCommentTrivia:
                    var start = trivia.Span.Start + "/*".Length;
                    var end = trivia.Span.End;
                    if (end >= start + 2 &&
                        trivia.ToFullString().EndsWith("*/"))
                    {
                        end -= 2;
                    }

                    return TextSpan.FromBounds(start, end);
                case SyntaxKind.ShebangDirectiveTrivia:
                    var structure = (ShebangDirectiveTriviaSyntax)trivia.GetStructure()!;
                    return TextSpan.FromBounds(structure.EndOfDirectiveToken.FullSpan.Start, structure.EndOfDirectiveToken.Span.Start);
                default:
                    throw ExceptionUtilities.UnexpectedValue(trivia.Kind());
            }
        }

        protected override TextSpan GetSpanForString(SyntaxToken token)
        {
            var start = token.SpanStart + (token.IsVerbatimStringLiteral() ? 2 : 1);
            var end = Math.Max(start, token.Span.End - (token.Text.EndsWith("\"") ? 1 : 0));
            return TextSpan.FromBounds(start, end);
        }

        protected override TextSpan GetSpanForRawString(SyntaxToken token)
        {
            var text = token.Text;
            var start = 0;
            var end = text.Length;

            while (start < text.Length && text[start] == '"')
                start++;

            while (end > start && text[end - 1] == '"')
                end--;

            return TextSpan.FromBounds(token.SpanStart + start, token.SpanStart + end);
        }
    }
}
