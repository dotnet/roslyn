// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    using static AnalyzedNode;
    using static SyntaxFactory;

    internal static class CSharpAnalyzedNodeRewriter
    {
        public static ExpressionSyntax AsExpressionSyntax(this AnalyzedNode node)
        {
            Debug.Assert(!(node is Evaluation e) || e.Syntax is ExpressionSyntax, "!(node is Evaluation e) || e.Syntax is ExpressionSyntax");

            return node switch
            {
                Evaluation p => (ExpressionSyntax)p.Syntax!,
                Pair p => IsPatternExpression(AsExpressionSyntax(p.Input), AsPatternSyntax(p.Pattern)),
                AndSequence p => p.Nodes.Select(AsExpressionSyntax).Aggregate((left, right) => BinaryExpression(SyntaxKind.LogicalAndExpression, left, right)),
                OrSequence p => p.Nodes.Select(AsExpressionSyntax).Aggregate((left, right) => BinaryExpression(SyntaxKind.LogicalOrExpression, left, right)),
                var p => throw UnexpectedNode(p),
            };
        }

        public static PatternSyntax AsPatternSyntax(this AnalyzedNode node, bool recurse = false)
        {
            var pattern = AsPatternSyntax(node, out var expression, recurse);
            Debug.Assert(expression is null);
            return pattern;
        }

        public static PatternSyntax AsPatternSyntax(this AnalyzedNode node, out ExpressionSyntax? expression, bool recurse = false)
        {
            expression = null;
            return node switch
            {
                AndSequence p => AsRecursivePatternSyntax(p.Nodes, out expression),
                OrSequence p when CanSimplifyConsecutiveConstantTests(p, out var constants) => AsPatternSyntax(SimplifyConsecutiveConstantTests(constants)),
                OrSequence p => p.Nodes.Select(p => AsPatternSyntax(p)).Aggregate((left, right) => BinaryPattern(SyntaxKind.OrPattern, left, right)),
                True _ => DiscardPattern(),
                False _ => UnaryPattern(DiscardPattern()),
                Not { Operand: NotNull _ } => ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression)),
                Not p => UnaryPattern(AsPatternSyntax(p.Operand)),
                NotNull _ => UnaryPattern(ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression))),
                Relational p => RelationalPattern(Token(AsSyntaxKind(p.OperatorKind)), (ExpressionSyntax)p.Value.Syntax),
                Constant p => ConstantPattern(((ExpressionSyntax)p.Value.Syntax).WithoutTrailingTrivia()),
                Type p => TypePattern(p.TypeSymbol.GenerateTypeSyntax()),
                Variable p => VarPattern(SingleVariableDesignation(Identifier(p.DeclaredSymbol.Name))),
                var p when !recurse => AsRecursivePatternSyntax(ImmutableArray.Create(p), out expression),
                var p => throw UnexpectedNode(p)
            };
        }

        private static Exception UnexpectedNode(AnalyzedNode p)
        {
#if DEBUG
            return new InvalidOperationException($"Unexpected node of type '{p.GetType().Name}'\n{p.Dump()}");
#else
            return ExceptionUtilities.UnexpectedValue(p);
#endif
        }

        private static SyntaxKind AsSyntaxKind(BinaryOperatorKind kind)
        {
            return kind switch
            {
                BinaryOperatorKind.LessThan => SyntaxKind.LessThanToken,
                BinaryOperatorKind.GreaterThan => SyntaxKind.GreaterThanToken,
                BinaryOperatorKind.LessThanOrEqual => SyntaxKind.LessThanEqualsToken,
                BinaryOperatorKind.GreaterThanOrEqual => SyntaxKind.GreaterThanEqualsToken,
                var v => throw ExceptionUtilities.UnexpectedValue(v)
            };
        }

        private static PatternSyntax AsRecursivePatternSyntax(ImmutableArray<AnalyzedNode> nodes, out ExpressionSyntax? expression)
        {
            TypeSyntax? type = null;
            VariableDesignationSyntax? designation = null;

            using var _0 = ArrayBuilder<PositionalPatternClauseSyntax>.GetInstance(out var positionalClauses);
            using var _1 = ArrayBuilder<SubpatternSyntax>.GetInstance(out var propertySubpatterns);
            using var _2 = ArrayBuilder<PatternSyntax?>.GetInstance(out var tupleSubpatterns);
            using var _3 = ArrayBuilder<PatternSyntax?>.GetInstance(out var indexedSubpatterns);
            using var _4 = ArrayBuilder<PatternSyntax>.GetInstance(out var remainingPatterns);
            using var _5 = ArrayBuilder<ExpressionSyntax>.GetInstance(out var remainingExpressions);

            INamedTypeSymbol? tupleTypeOpt = null;

            foreach (var item in nodes.Distinct())
            {
                switch (item)
                {
                    // UNDONE: Should we have a separate node for tuple fields?
                    case FieldEvaluation field when field.Field.IsTupleField():
                        AddTupleSubpattern(field.Field, AsConstantPattern(true));
                        continue;
                    case Not { Operand: FieldEvaluation field } when field.Field.IsTupleField():
                        AddTupleSubpattern(field.Field, AsConstantPattern(false));
                        continue;
                    case Pair { Input: FieldEvaluation field } pair when field.Field.IsTupleField():
                        AddTupleSubpattern(field.Field, AsPatternSyntax(pair.Pattern));
                        continue;

                    // UNDONE: Should we have a separate node for ITuple length?
                    // UNDONE: This would be particularly useful for supporting list patterns
                    case Pair { Input: PropertyEvaluation p, Pattern: Constant constant }
                        when p.Property.Name == WellKnownMemberNames.LengthPropertyName &&
                             p.Property.ContainingType.Name == "ITuple":
                        indexedSubpatterns.SetItem((int)constant.Value.ConstantValue.Value - 1, null);
                        continue;

                    case IndexEvaluation indexer:
                        indexedSubpatterns.SetItem(indexer.Index, AsConstantPattern(true));
                        continue;
                    case Not { Operand: IndexEvaluation indexer }:
                        indexedSubpatterns.SetItem(indexer.Index, AsConstantPattern(false));
                        continue;
                    case Pair { Input: IndexEvaluation indexer } pair:
                        indexedSubpatterns.SetItem(indexer.Index, AsPatternSyntax(pair.Pattern));
                        continue;

                    case MemberEvaluation member:
                        propertySubpatterns.Add(AsSubpatternSyntax(member, AsConstantPattern(true)));
                        continue;
                    case Not { Operand: MemberEvaluation member }:
                        propertySubpatterns.Add(AsSubpatternSyntax(member, AsConstantPattern(false)));
                        continue;
                    case Pair { Input: MemberEvaluation member } pair:
                        propertySubpatterns.Add(AsSubpatternSyntax(member, AsPatternSyntax(pair.Pattern)));
                        continue;

                    case Pair { Input: DeconstructEvaluation input } pair:
                        positionalClauses.Add(AsPositionalPatternClauseSyntax(input, pair));
                        continue;

                    case Variable v when designation is null:
                        designation = SingleVariableDesignation(Identifier(v.DeclaredSymbol.Name));
                        continue;
                    case Type t when type is null:
                        type = t.TypeSymbol.GenerateTypeSyntax();
                        continue;

                    case NotNull _:
                        continue;
                    case Pair { Input: OperationEvaluation _ } p:
                        remainingExpressions.Add(AsExpressionSyntax(p));
                        continue;
                    case OperationEvaluation p:
                        remainingExpressions.Add(AsExpressionSyntax(p));
                        continue;
                    case var v:
                        remainingPatterns.Add(AsPatternSyntax(v, recurse: true));
                        continue;
                }
            }

            if (!tupleSubpatterns.IsEmpty())
            {
                if (positionalClauses.IsEmpty())
                {
                    // Not supported with tuples
                    type = null;
                }
                Debug.Assert(tupleTypeOpt is { });
                var arity = tupleTypeOpt.Arity;
                if (tupleSubpatterns.Count < arity)
                    tupleSubpatterns.SetItem(arity - 1, null);
                var list = tupleSubpatterns.Select(p => Subpattern(p ?? DiscardPattern()));
                positionalClauses.Add(PositionalPatternClause(SeparatedList(list)));
            }
            else if (!indexedSubpatterns.IsEmpty())
            {
                if (positionalClauses.IsEmpty())
                {
                    // Not supported with ITuple
                    type = null;
                    designation = null;
                }
                var list = indexedSubpatterns.Select(p => Subpattern(p ?? DiscardPattern()));
                positionalClauses.Add(PositionalPatternClause(SeparatedList(list)));
            }

            if (!positionalClauses.IsEmpty() ||
                !propertySubpatterns.IsEmpty())
            {
                var recursive = RecursivePattern(
                    type,
                    positionalClauses.FirstOrDefault(),
                    PropertyPatternClause(SeparatedList(propertySubpatterns)),
                    designation);
                remainingPatterns.Insert(0, recursive.WithAdditionalAnnotations(Simplifier.Annotation));
                remainingPatterns.AddRange(positionalClauses.Skip(1).Select(
                    positional => RecursivePattern(null, positional, null, null)));
            }
            else
            {
#pragma warning disable IDE0007 // Use implicit type
                PatternSyntax? pattern = (type, designation) switch
#pragma warning restore IDE0007 // Use implicit type
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    ({ }, { }) => DeclarationPattern(type, designation),
                    ({ }, _) => TypePattern(type),
#pragma warning restore CS8604 // Possible null reference argument.
                    (_, { }) => RecursivePattern(null, null, PropertyPatternClause(SeparatedList<SubpatternSyntax>()), designation),
                    _ => null,
                };

                if (pattern != null)
                    remainingPatterns.Insert(0, pattern);
            }

            expression = remainingExpressions.AggregateOrDefault(
                (left, right) => BinaryExpression(SyntaxKind.LogicalAndExpression, left, right));
            return remainingPatterns.Aggregate(
                (left, right) => BinaryPattern(SyntaxKind.AndPattern, left, right));

            void AddTupleSubpattern(IFieldSymbol tupelField, PatternSyntax pattern)
            {
                Debug.Assert(Regex.IsMatch(tupelField.Name, @"^Item\d+$"));
                Debug.Assert(tupleTypeOpt is null || tupleTypeOpt.Equals(tupelField.ContainingSymbol));
                var position = int.Parse(tupelField.Name.Substring(4));
                tupleSubpatterns.SetItem(position - 1, pattern);
                tupleTypeOpt ??= (INamedTypeSymbol)tupelField.ContainingSymbol;
            }
        }

        private static PositionalPatternClauseSyntax AsPositionalPatternClauseSyntax(DeconstructEvaluation input, Pair pair)
        {
            var method = input.DeconstructMethod;
            var parameterCount = method.Parameters.Length - (method.IsExtensionMethod ? 1 : 0);
            var subpatterns = ArrayBuilder<PatternSyntax?>.GetInstance(parameterCount);
            if (pair.Pattern is AndSequence seq)
            {
                foreach (var node in seq.Nodes)
                {
                    var (index, pattern) = GetSubpattern(node);
                    subpatterns.SetItem(index, pattern);
                }
            }
            else
            {
                var (index, pattern) = GetSubpattern(pair.Pattern);
                subpatterns.SetItem(index, pattern);
            }

            Debug.Assert(subpatterns.Count == parameterCount);
            var list = subpatterns.Select(p => Subpattern(p ?? DiscardPattern()));
            var result = PositionalPatternClause(SeparatedList(list));
            subpatterns.Free();
            return result;

            static (int Index, PatternSyntax SubpatternSyntax) GetSubpattern(AnalyzedNode node)
                => node is Pair { Input: OutVariableEvaluation outVariable } p
                    ? (outVariable.Index, AsPatternSyntax(p.Pattern))
                    : throw UnexpectedNode(node);
        }

        private static SubpatternSyntax AsSubpatternSyntax(MemberEvaluation member, PatternSyntax pattern)
        {
            return Subpattern(AsNameColonSyntax(member), pattern);
        }

        private static ConstantPatternSyntax AsConstantPattern(bool sense)
        {
            return ConstantPattern(LiteralExpression(sense ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression));
        }

        private static NameColonSyntax AsNameColonSyntax(MemberEvaluation member)
        {
            return NameColon(member.Symbol.Name);
        }

        private static bool CanSimplifyConsecutiveConstantTests(OrSequence sequence, out ImmutableArray<Constant> constants)
        {
            var nodes = sequence.Nodes;
            if (nodes.Length <= 2)
            {
                constants = default;
                return false;
            }

            return nodes.TryCastArray(out constants) &&
                   IsIntegralType(constants[0].Value.Type) &&
                   constants.AreEquivalent(constant => constant.Value.Type);

            static bool IsIntegralType(ITypeSymbol type)
            {
                return !type.IsEnumType() &&
                       (type.SpecialType.IsIntegralType() ||
                        type.SpecialType == SpecialType.System_Char);
            }
        }

        private static AnalyzedNode SimplifyConsecutiveConstantTests(ImmutableArray<Constant> constants)
        {
            return constants[0].Value.Type.SpecialType switch
            {
                SpecialType.System_Char => Simplify<char, int>((v, i) => v - i),
                SpecialType.System_SByte => Simplify<sbyte, int>((v, i) => v - i),
                SpecialType.System_Byte => Simplify<byte, int>((v, i) => v - i),
                SpecialType.System_Int16 => Simplify<short, int>((v, i) => v - i),
                SpecialType.System_UInt16 => Simplify<ushort, int>((v, i) => v - i),
                SpecialType.System_Int32 => Simplify<int, int>((v, i) => v - i),
                SpecialType.System_UInt32 => Simplify<uint, long>((v, i) => v - i),
                SpecialType.System_Int64 => Simplify<long, long>((v, i) => v - i),
                SpecialType.System_UInt64 => Simplify<ulong, ulong>((v, i) => v - (uint)i),
                _ => throw ExceptionUtilities.Unreachable
            };

            AnalyzedNode Simplify<T, V>(Func<T, int, V> orderingFunc)
            {
                var tests = ArrayBuilder<AnalyzedNode>.GetInstance();
                foreach (var bucket in ConsecutiveGroups(constants, p => (T)p.Value.ConstantValue.Value, orderingFunc))
                {
                    Debug.Assert(bucket.Count > 0);
                    if (bucket.Count <= 2)
                    {
                        tests.AddRange(bucket);
                    }
                    else
                    {
                        var commonInput = constants[0].Input;
                        var builder = ArrayBuilder<AnalyzedNode>.GetInstance(2);
                        builder.Add(new Relational(commonInput, BinaryOperatorKind.GreaterThanOrEqual, bucket.First().Value));
                        builder.Add(new Relational(commonInput, BinaryOperatorKind.LessThanOrEqual, bucket.Last().Value));
                        tests.Add(AndSequence.Create(builder));
                    }
                }

                return OrSequence.Create(tests);
            }

            static IEnumerable<IReadOnlyCollection<T>> ConsecutiveGroups<T, K, V>(
                IEnumerable<T> source, Func<T, K> keySelector, Func<K, int, V> orderingFunc)
            {
                return source
                    .Select(value => (value, key: keySelector(value)))
                    .OrderBy(item => item.key)
                    .Select((item, index) => (item.value, item.key, index))
                    .GroupBy(item => orderingFunc(item.key, item.index))
                    .Select(group => group.Select(item => item.value).ToList());
            }
        }

        public static SyntaxNode WrapPropertyPatternClauses(this SyntaxNode node)
        {
            return Formatter.Instance.Visit(node);
        }

        private sealed class Formatter : CSharpSyntaxRewriter
        {
            public static readonly Formatter Instance = new Formatter();

            private Formatter() { }

            private static SyntaxToken MakeToken(SyntaxKind token, bool newlineBefore = false, bool newlineAfter = false)
                => Token(
                    newlineBefore ? TriviaList(ElasticCarriageReturnLineFeed) : default,
                    token,
                    newlineAfter ? TriviaList(ElasticCarriageReturnLineFeed) : default);

            private static IEnumerable<SyntaxToken> MakeSeparators(int count, bool multiline)
                => ArrayBuilder<SyntaxToken>.GetInstance(count,
                    MakeToken(SyntaxKind.CommaToken, newlineAfter: multiline)).ToArrayAndFree();

            public override SyntaxNode? VisitPropertyPatternClause(PropertyPatternClauseSyntax node)
            {
                var multiline = node.Width() > 50;
                var subpatterns = node.Subpatterns;
                var separatorCount = subpatterns.Count - (multiline ? 0 : 1);
                return node.Update(
                    MakeToken(SyntaxKind.OpenBraceToken, newlineAfter: multiline),
                    SeparatedList(VisitList(subpatterns), MakeSeparators(separatorCount, multiline)),
                    MakeToken(SyntaxKind.CloseBraceToken, newlineBefore: multiline));
            }
        }
    }
}
