// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class ITypeSymbolExtensions
    {
        public static ExpressionSyntax GenerateExpressionSyntax(
            this ITypeSymbol typeSymbol)
        {
            return typeSymbol.Accept(ExpressionSyntaxGeneratorVisitor.Instance).WithAdditionalAnnotations(Simplifier.Annotation);
        }

        public static TypeSyntax GenerateTypeSyntax(
            this ITypeSymbol typeSymbol)
        {
            return typeSymbol.Accept(TypeSyntaxGeneratorVisitor.Instance).WithAdditionalAnnotations(Simplifier.Annotation);
        }
    }
}