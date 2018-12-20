// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    internal sealed partial class ConvertSwitchStatementToExpressionDiagnosticAnalyzer
    {
        private readonly struct AnalysisResult
        {
            // Note: default(AnalysisResult) should yield a
            // failure if we ever reached that in the visitor
            private const SyntaxKind FailureKind = SyntaxKind.None;
            private const SyntaxKind NeutralKind = unchecked((SyntaxKind)(-1));

            private readonly SyntaxKind _syntaxKind;
            // Holds assignment target expressions in each switch
            // section to ensure that they are all the same.
            private readonly ImmutableArray<ExpressionSyntax> _assignmentTargets;

            private AnalysisResult(SyntaxKind syntaxKind, ImmutableArray<ExpressionSyntax> assignmentTargets = default)
            {
                _syntaxKind = syntaxKind;
                _assignmentTargets = assignmentTargets;
            }

            public static AnalysisResult Failure => new AnalysisResult(FailureKind);
            public static AnalysisResult Neutral => new AnalysisResult(NeutralKind);
            public static AnalysisResult Return => new AnalysisResult(SyntaxKind.ReturnStatement);
            public static AnalysisResult Break => new AnalysisResult(SyntaxKind.BreakStatement);
            public static AnalysisResult Throw => new AnalysisResult(SyntaxKind.ThrowStatement);

            public AnalysisResult WithAdditionalAssignmentTargets(ImmutableArray<ExpressionSyntax> targets)
            {
                Debug.Assert(IsAssignment);
                return new AnalysisResult(_syntaxKind, _assignmentTargets.Concat(targets));
            }

            public SyntaxKind GetSyntaxKind()
            {
                Debug.Assert(this.Success);
                return _syntaxKind;
            }

            public static AnalysisResult Assignment(SyntaxKind syntaxKind, ExpressionSyntax target)
            {
                Debug.Assert(SyntaxFacts.IsAssignmentExpression(syntaxKind));
                return new AnalysisResult(syntaxKind, ImmutableArray.Create(target));
            }

            public bool IsFailure => _syntaxKind == FailureKind;
            public bool IsNeutral => _syntaxKind == NeutralKind || IsThrow;
            public bool IsBreak => _syntaxKind == SyntaxKind.BreakStatement;
            public bool IsThrow => _syntaxKind == SyntaxKind.ThrowStatement;
            public bool IsReturn => _syntaxKind == SyntaxKind.ReturnStatement;
            public bool IsAssignment => SyntaxFacts.IsAssignmentExpression(_syntaxKind);

            public bool Success => IsReturn || IsThrow || IsAssignment;

            public override string ToString() => _syntaxKind.ToString();

            public bool Equals(AnalysisResult other) => _syntaxKind == other._syntaxKind;

            public AnalysisResult Union(AnalysisResult other)
            {
                if (this.IsAssignment)
                {
                    // Assignment can be followed by another assignment if it's not compound.
                    if (_syntaxKind == SyntaxKind.SimpleAssignmentExpression && this.Equals(other))
                    {
                        return this.WithAdditionalAssignmentTargets(other._assignmentTargets);
                    }

                    // Assignment can be followed by a "break" statement
                    if (other.IsBreak)
                    {
                        return this;
                    }
                }

                return Common(this, other);
            }

            public AnalysisResult Intersect(AnalysisResult other)
            {
                if (this.IsAssignment && this.Equals(other))
                {
                    // Assignments only match if they have the same set of targets
                    if (_assignmentTargets.SequenceEqual(other._assignmentTargets,
                        (leftNode, rightNode) => leftNode.IsEquivalentTo(rightNode)))
                    {
                        return this;
                    }

                    return Failure;
                }

                // "break" alone can't be translated to an expression
                if (this.IsBreak || other.IsBreak)
                {
                    return Failure;
                }

                return Common(this, other);
            }

            private static AnalysisResult Common(AnalysisResult left, AnalysisResult right)
            {
                // This can also happen on the "Union" method when a non-exhaustive
                // "switch" statement is followed by a "return" statement.
                if (left.IsReturn && right.IsReturn)
                {
                    return left;
                }

                if (left.IsNeutral)
                {
                    return right;
                }

                if (right.IsNeutral)
                {
                    return left;
                }

                // All the other combinations will fail.
                return Failure;
            }
        }
    }
}
