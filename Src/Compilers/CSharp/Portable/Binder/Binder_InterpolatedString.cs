// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        private BoundExpression BindInterpolatedString(InterpolatedStringExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var builder = ArrayBuilder<BoundExpression>.GetInstance();
            var stringType = GetSpecialType(SpecialType.System_String, diagnostics, node);
            var objectType = GetSpecialType(SpecialType.System_Object, diagnostics, node);
            var intType = GetSpecialType(SpecialType.System_Int32, diagnostics, node);
            foreach (var content in node.Contents)
            {
                switch (content.Kind)
                {
                    case SyntaxKind.Interpolation:
                        {
                            var interpolation = (InterpolationSyntax)content;
                            var value = GenerateConversionForAssignment(objectType, this.BindExpression(interpolation.Expression, diagnostics), diagnostics);
                            BoundExpression alignment = null, format = null;
                            if (interpolation.AlignmentClause != null)
                            {
                                alignment = GenerateConversionForAssignment(intType, this.BindExpression(interpolation.AlignmentClause.Value, diagnostics), diagnostics);
                                if (!alignment.HasErrors && alignment.ConstantValue == null)
                                {
                                    diagnostics.Add(ErrorCode.ERR_ConstantExpected, interpolation.AlignmentClause.Value.Location);
                                }
                            }
                            if (interpolation.FormatClause != null)
                            {
                                var text = interpolation.FormatClause.FormatStringToken.ValueText;
                                char lastChar;
                                bool hasErrors = false;
                                if (text.Length == 0)
                                {
                                    diagnostics.Add(ErrorCode.ERR_EmptyFormatSpecifier, interpolation.FormatClause.Location);
                                    hasErrors = true;
                                }
                                else if (SyntaxFacts.IsWhitespace(lastChar = text[text.Length - 1]) || SyntaxFacts.IsNewLine(lastChar))
                                {
                                    diagnostics.Add(ErrorCode.ERR_TrailingWhitespaceInFormatSpecifier, interpolation.FormatClause.Location);
                                    hasErrors = true;
                                }

                                format = new BoundLiteral(interpolation.FormatClause, ConstantValue.Create(text), stringType, hasErrors);
                            }
                            builder.Add(new BoundStringInsert(interpolation, value, alignment, format, null));
                            continue;
                        }
                    case SyntaxKind.InterpolatedStringText:
                        {
                            var text = ((InterpolatedStringTextSyntax)content).TextToken.ValueText;
                            builder.Add(new BoundLiteral(node, ConstantValue.Create(text, SpecialType.System_String), stringType));
                            continue;
                        }
                    default:
                        throw ExceptionUtilities.Unreachable;
                }
            }

            return new BoundInterpolatedString(node, builder.ToImmutableAndFree(), stringType);
        }
    }
}
