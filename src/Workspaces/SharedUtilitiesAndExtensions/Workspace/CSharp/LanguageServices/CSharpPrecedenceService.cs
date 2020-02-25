﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageService(typeof(IPrecedenceService), LanguageNames.CSharp), Shared]
    internal class CSharpPrecedenceService : AbstractPrecedenceService<ExpressionSyntax, OperatorPrecedence>
    {
        public static readonly CSharpPrecedenceService Instance = new CSharpPrecedenceService();

        [ImportingConstructor]
        public CSharpPrecedenceService()
        {
        }

        public override OperatorPrecedence GetOperatorPrecedence(ExpressionSyntax expression)
            => expression.GetOperatorPrecedence();

        public override PrecedenceKind GetPrecedenceKind(OperatorPrecedence precedence)
        {
            switch (precedence)
            {
                case OperatorPrecedence.NullCoalescing: return PrecedenceKind.Coalesce;
                case OperatorPrecedence.ConditionalOr:
                case OperatorPrecedence.ConditionalAnd: return PrecedenceKind.Logical;
                case OperatorPrecedence.LogicalOr:
                case OperatorPrecedence.LogicalXor:
                case OperatorPrecedence.LogicalAnd: return PrecedenceKind.Bitwise;
                case OperatorPrecedence.Equality: return PrecedenceKind.Equality;
                case OperatorPrecedence.RelationalAndTypeTesting: return PrecedenceKind.Relational;
                case OperatorPrecedence.Shift: return PrecedenceKind.Shift;
                case OperatorPrecedence.Additive:
                case OperatorPrecedence.Multiplicative: return PrecedenceKind.Arithmetic;
                default: return PrecedenceKind.Other;
            }
        }
    }
}
