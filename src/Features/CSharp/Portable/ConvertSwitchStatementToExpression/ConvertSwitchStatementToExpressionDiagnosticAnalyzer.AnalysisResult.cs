// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    internal sealed partial class ConvertSwitchStatementToExpressionDiagnosticAnalyzer
    {
        private enum AnalysisResultKind
        {
            // Note: default(AnalysisResult) should yield a
            // failure if we ever reached that in the visitor.
            Failure = 0,

            // Note: we will use this for either a "throw" statement
            // or as the insignificant part of a combination, thus we
            // named it "Neutral" rather than "Throw" to avoid confusion.
            Neutral,

            Break,
            Return,
            Assignment,
        }

        private readonly struct AnalysisResult
        {
            private readonly AnalysisResultKind _kind;

            // Holds assignment target symbols in each switch
            // section to ensure that they are all the same.
            private readonly ImmutableArray<ISymbol> _assignmentTargets;

            private AnalysisResult(AnalysisResultKind kind, ImmutableArray<ISymbol> assignmentTargets = default)
            {
                _kind = kind;
                _assignmentTargets = assignmentTargets;
            }

            public static AnalysisResult Failure => new AnalysisResult(AnalysisResultKind.Failure);
            public static AnalysisResult Return => new AnalysisResult(AnalysisResultKind.Return);
            public static AnalysisResult Break => new AnalysisResult(AnalysisResultKind.Break);
            public static AnalysisResult Neutral => new AnalysisResult(AnalysisResultKind.Neutral);
            public static AnalysisResult Assignment(ImmutableArray<ISymbol> targets) => new AnalysisResult(AnalysisResultKind.Assignment, targets);
            public static AnalysisResult Assignment(ISymbol target) => new AnalysisResult(AnalysisResultKind.Assignment, ImmutableArray.Create(target));

            public bool IsFailure => _kind == AnalysisResultKind.Failure;
            public bool IsBreak => _kind == AnalysisResultKind.Break;
            public bool IsReturn => _kind == AnalysisResultKind.Return;
            public bool IsNeutral => _kind == AnalysisResultKind.Neutral;
            public bool IsAssignment => _kind == AnalysisResultKind.Assignment;

            public bool Success => IsReturn || IsAssignment;

            public override string ToString() => _kind.ToString();

            public static AnalysisResult Combine(AnalysisResult left, AnalysisResult right)
            {
                if (left.IsAssignment)
                {
                    // Assignment can be followed by another assignment
                    if (right.IsAssignment)
                    {
                        return Assignment(left._assignmentTargets.Concat(right._assignmentTargets));
                    }

                    // Assignment can be followed by a "break" statement
                    if (right.IsBreak)
                    {
                        return left;
                    }
                }

                return Common(left, right);
            }

            public static AnalysisResult Match(AnalysisResult left, AnalysisResult right)
            {
                if (left.IsAssignment && right.IsAssignment)
                {
                    // Assignments only match if they have the same set of targets
                    if (left._assignmentTargets.SequenceEqual(right._assignmentTargets))
                    {
                        return left;
                    }

                    return Failure;
                }

                // "break" alone can't be traslated to an expression
                if (left.IsBreak || right.IsBreak)
                {
                    return Failure;
                }

                return Common(left, right);
            }

            private static AnalysisResult Common(AnalysisResult left, AnalysisResult right)
            {
                // This can also happen on the Combine method when a non-exhastive
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
