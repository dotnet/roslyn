// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UseCompoundAssignment;

namespace Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class CSharpUseCompoundAssignmentDiagnosticAnalyzer
    : AbstractUseCompoundAssignmentDiagnosticAnalyzer<SyntaxKind, AssignmentExpressionSyntax, BinaryExpressionSyntax>
{
    public CSharpUseCompoundAssignmentDiagnosticAnalyzer()
        : base(CSharpSyntaxFacts.Instance, Utilities.Kinds)
    {
    }

    protected override SyntaxKind GetAnalysisKind()
        => SyntaxKind.SimpleAssignmentExpression;

    protected override bool IsSupported(SyntaxKind assignmentKind, ParseOptions options)
        => assignmentKind != SyntaxKind.CoalesceExpression ||
           options.LanguageVersion() >= LanguageVersion.CSharp8;

    protected override int TryGetIncrementOrDecrement(SyntaxKind opKind, object constantValue)
    {
        if (constantValue is
            (sbyte)1 or (short)1 or (int)1 or (long)1 or
            (byte)1 or (ushort)1 or (uint)1 or (ulong)1 or
            1.0 or 1.0f or 1.0m)
        {
            return opKind switch
            {
                SyntaxKind.AddExpression => 1,
                SyntaxKind.SubtractExpression => -1,
                _ => 0
            };
        }
        else if (constantValue is
            (sbyte)-1 or (short)-1 or (int)-1 or (long)-1 or
            -1.0 or -1.0f or -1.0m)
        {
            return opKind switch
            {
                SyntaxKind.AddExpression => -1,
                SyntaxKind.SubtractExpression => 1,
                _ => 0
            };
        }

        return 0;
    }
}
