// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    internal sealed partial class ConvertSwitchStatementToExpressionDiagnosticAnalyzer
    {
        private readonly struct AnalysisResult
        {
            private enum ResultKind
            {
                // Note: default(AnalysisResult) should yield a
                // failure if we ever reached that in the visitor
                Failure = 0,
                Neutral,
                Throw = SyntaxKind.ThrowStatement,
                Break = SyntaxKind.BreakStatement,
                Return = SyntaxKind.ReturnStatement,
                SimpleAssignment = SyntaxKind.SimpleAssignmentExpression,
                CompoundAssignment,
            }

            private readonly ResultKind _resultKind;
            private readonly SyntaxKind _assignmentKind;

            private AnalysisResult(ResultKind resultKind,  SyntaxKind assignmentKind = default)
            {
                _resultKind = resultKind;
                _assignmentKind = assignmentKind;
            }

            public static AnalysisResult Failure => new AnalysisResult(ResultKind.Failure);
            public static AnalysisResult Neutral => new AnalysisResult(ResultKind.Neutral);
            public static AnalysisResult Return => new AnalysisResult(ResultKind.Return);
            public static AnalysisResult Break => new AnalysisResult(ResultKind.Break);
            public static AnalysisResult Throw => new AnalysisResult(ResultKind.Throw);

            public static AnalysisResult Assignment(SyntaxKind _assignmentKind)
            {
                Debug.Assert(SyntaxFacts.IsAssignmentExpression(_assignmentKind));
                return new AnalysisResult(
                    _assignmentKind == SyntaxKind.SimpleAssignmentExpression
                        ? ResultKind.SimpleAssignment
                        : ResultKind.CompoundAssignment,
                    _assignmentKind);
            }

            public bool IsFailure => _resultKind == ResultKind.Failure;
            public bool IsNeutral => _resultKind == ResultKind.Neutral || IsThrow;
            public bool IsBreak => _resultKind == ResultKind.Break;
            public bool IsThrow => _resultKind == ResultKind.Throw;
            public bool IsReturn => _resultKind == ResultKind.Return;
            public bool IsAssignment => _resultKind == ResultKind.SimpleAssignment || _resultKind == ResultKind.CompoundAssignment;

            public bool Success => IsReturn || IsThrow || IsAssignment;

            public SyntaxKind GetSyntaxKind()
            {
                Debug.Assert(Success);
                return _resultKind == ResultKind.CompoundAssignment ? _assignmentKind : (SyntaxKind)_resultKind;
            }

            public override string ToString() => _resultKind.ToString();

            public AnalysisResult Union(AnalysisResult other)
            {
                if (this.IsAssignment)
                {
                    // Assignment can be followed by another assignment if it's not compound.
                    if (this._resultKind == ResultKind.SimpleAssignment &&
                        other._resultKind == ResultKind.SimpleAssignment)
                    {
                        return this;
                    }

                    // Assignment can be followed by a "break" statement
                    if (other.IsBreak)
                    {
                        return this;
                    }

                    return Failure;
                }

                return Common(this, other);
            }

            public AnalysisResult Intersect(AnalysisResult other)
            {
                if (this._assignmentKind == other._assignmentKind && this.IsAssignment)
                {
                    // Assignments only match if they have the same set of targets
                    return this;
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
