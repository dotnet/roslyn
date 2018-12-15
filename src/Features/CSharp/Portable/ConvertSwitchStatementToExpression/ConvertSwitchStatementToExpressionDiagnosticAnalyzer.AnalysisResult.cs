// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    internal sealed partial class ConvertSwitchStatementToExpressionDiagnosticAnalyzer
    {
        private enum AnalysisResultKind
        {
            Neutral,
            Failure,
            Return,
            Assignment,
        }

        private readonly struct AnalysisResult
        {
            private readonly AnalysisResultKind _kind;
            private readonly ImmutableArray<ISymbol> _assignmentTargets;

            private AnalysisResult(AnalysisResultKind kind, ImmutableArray<ISymbol> assignmentTargets = default)
            {
                _kind = kind;
                _assignmentTargets = assignmentTargets;
            }

            public static AnalysisResult Neutral => new AnalysisResult(AnalysisResultKind.Neutral);
            public static AnalysisResult Failure => new AnalysisResult(AnalysisResultKind.Failure);
            public static AnalysisResult Return => new AnalysisResult(AnalysisResultKind.Return);
            public static AnalysisResult Assignment(ImmutableArray<ISymbol> targets) => new AnalysisResult(AnalysisResultKind.Assignment, targets);
            public static AnalysisResult Assignment(ISymbol target) => new AnalysisResult(AnalysisResultKind.Assignment, ImmutableArray.Create(target));

            public bool IsNeutral => _kind == AnalysisResultKind.Neutral;
            public bool IsFailure => _kind == AnalysisResultKind.Failure;
            public bool IsReturn => _kind == AnalysisResultKind.Return;
            public bool IsAssignment => _kind == AnalysisResultKind.Assignment;

            public static AnalysisResult Combine(AnalysisResult left, AnalysisResult right)
            {
                if (left.IsAssignment && right.IsAssignment)
                {
                    return Assignment(left._assignmentTargets.Concat(right._assignmentTargets));
                }

                return Common(left, right);
            }

            public static AnalysisResult Match(AnalysisResult left, AnalysisResult right)
            {
                if (left.IsAssignment && right.IsAssignment)
                {
                    if (left._assignmentTargets.SequenceEqual(right._assignmentTargets))
                    {
                        return left;
                    }
                }

                return Common(left, right);
            }

            private static AnalysisResult Common(AnalysisResult left, AnalysisResult right)
            {
                if (!left.IsFailure && !right.IsFailure)
                {
                    if (left.IsNeutral)
                    {
                        return right;
                    }

                    if (right.IsNeutral)
                    {
                        return left;
                    }

                    if (left.IsReturn && right.IsReturn)
                    {
                        return left;
                    }
                }

                return Failure;
            }
        }
    }
}
