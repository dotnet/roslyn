// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal static class SymbolDisplayClassificationHelper
    {
        public static ImmutableArray<SymbolDisplayPart> ConvertClassifications(
            SourceText sourceText, int startPosition, IEnumerable<ClassifiedSpan> classifiedSpans, bool insertSourceTextInGaps = false)
        {
            var parts = new List<SymbolDisplayPart>();

            foreach (var span in classifiedSpans)
            {
                // If there is space between this span and the last one, then add a space.
                if (startPosition != span.TextSpan.Start)
                {
                    if (insertSourceTextInGaps)
                    {
                        parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Text, null,
                            sourceText.ToString(TextSpan.FromBounds(
                                startPosition, span.TextSpan.Start))));
                    }
                    else
                    {
                        parts.AddRange(Space());
                    }
                }

                var kind = GetDisplayPartKind(span.ClassificationType);
                parts.Add(new SymbolDisplayPart(kind, null, sourceText.ToString(span.TextSpan)));
                startPosition = span.TextSpan.End;
            }

            return parts.ToImmutableArray();
        }

        private static IEnumerable<SymbolDisplayPart> Space(int count = 1)
        {
            yield return new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, new string(' ', count));
        }

        private static SymbolDisplayPartKind GetDisplayPartKind(string classificationType)
        {
            switch (classificationType)
            {
                default:
                    return SymbolDisplayPartKind.Text;
                case ClassificationTypeNames.Identifier:
                    return SymbolDisplayPartKind.Text;
                case ClassificationTypeNames.Keyword:
                    return SymbolDisplayPartKind.Keyword;
                case ClassificationTypeNames.NumericLiteral:
                    return SymbolDisplayPartKind.NumericLiteral;
                case ClassificationTypeNames.StringLiteral:
                    return SymbolDisplayPartKind.StringLiteral;
                case ClassificationTypeNames.WhiteSpace:
                    return SymbolDisplayPartKind.Space;
                case ClassificationTypeNames.Operator:
                    return SymbolDisplayPartKind.Operator;
                case ClassificationTypeNames.Punctuation:
                    return SymbolDisplayPartKind.Punctuation;
                case ClassificationTypeNames.ClassName:
                    return SymbolDisplayPartKind.ClassName;
                case ClassificationTypeNames.StructName:
                    return SymbolDisplayPartKind.StructName;
                case ClassificationTypeNames.InterfaceName:
                    return SymbolDisplayPartKind.InterfaceName;
                case ClassificationTypeNames.DelegateName:
                    return SymbolDisplayPartKind.DelegateName;
                case ClassificationTypeNames.EnumName:
                    return SymbolDisplayPartKind.EnumName;
                case ClassificationTypeNames.TypeParameterName:
                    return SymbolDisplayPartKind.TypeParameterName;
                case ClassificationTypeNames.ModuleName:
                    return SymbolDisplayPartKind.ModuleName;
                case ClassificationTypeNames.VerbatimStringLiteral:
                    return SymbolDisplayPartKind.StringLiteral;
            }
        }
    }
}
