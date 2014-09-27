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
        private BoundExpression BindInterpolatedString(InterpolatedStringSyntax node, DiagnosticBag diagnostics)
        {
            var builder = ArrayBuilder<BoundExpression>.GetInstance();
            SyntaxToken stringStart = node.StringStart;
            Debug.Assert(stringStart.CSharpKind() == SyntaxKind.InterpolatedStringStartToken);
            var stringType = GetSpecialType(SpecialType.System_String, diagnostics, node);
            var objectType = GetSpecialType(SpecialType.System_Object, diagnostics, node);
            builder.Add(new BoundLiteral(node, ConstantValue.Create(stringStart.ValueText, SpecialType.System_String), stringType));
            var inserts = node.InterpolatedInserts.GetWithSeparators();
            for (int i = 0; i < inserts.Count; i++)
            {
                if (i % 2 == 0)
                {
                    // an expression hole
                    var expr = (InterpolatedStringInsertSyntax)inserts[i].AsNode();
                    var bound = this.GenerateConversionForAssignment(objectType, this.BindExpression(expr.Expression, diagnostics), diagnostics);
                    BoundExpression alignment = expr.Alignment != null ? BindExpression(expr.Alignment, diagnostics) : null;
                    BoundExpression format = null;
                    if (expr.Format != default(SyntaxToken))
                    {
                        switch (expr.Format.CSharpKind())
                        {
                            case SyntaxKind.IdentifierToken:
                            case SyntaxKind.StringLiteralToken:
                                format = new BoundLiteral(expr, ConstantValue.Create(expr.Format.ValueText), stringType);
                                break;
                            default:
                                Debug.Assert(expr.HasErrors);
                                break;
                        }
                    }

                    builder.Add(new BoundStringInsert(expr, bound, alignment, format, null));
                }
                else
                {
                    // the separator token, which is part of the string literal
                    var token = inserts[i].AsToken();
                    var bound = new BoundLiteral(node, ConstantValue.Create(token.ValueText, SpecialType.System_String), stringType);
                    builder.Add(bound);
                }
            }

            SyntaxToken stringEnd = node.StringEnd;
            builder.Add(new BoundLiteral(node, ConstantValue.Create(stringEnd.ValueText, SpecialType.System_String), stringType));
            return new BoundInterpolatedString(node, builder.ToImmutableAndFree(), stringType);
        }
    }
}
