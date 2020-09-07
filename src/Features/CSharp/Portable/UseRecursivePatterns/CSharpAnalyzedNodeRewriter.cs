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
            Debug.Assert(!(node is Evaluation e) || e.Syntax is ExpressionSyntax,
                        "!(node is Evaluation e) || e.Syntax is ExpressionSyntax");

            return node switch
            {
                Evaluation p => (ExpressionSyntax)p.Syntax,
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
            var pattern = node switch
            {
                OrSequence p when CanSimplifyConsecutiveConstantTests(p, out var constants)
                    => AsPatternSyntax(SimplifyConsecutiveConstantTests(constants)),
                OrSequence p => p.Nodes
                    .Select(p => AsPatternSyntax(p))
                    .Aggregate((left, right) => BinaryPattern(SyntaxKind.OrPattern, left, right)),
                True => DiscardPattern(),
                False => UnaryPattern(DiscardPattern()),
                Not { Operand: NotNull } => ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression)),
                NotNull => UnaryPattern(ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression))),
                Relational p => RelationalPattern(Token(AsSyntaxKind(p.OperatorKind)), ((ExpressionSyntax)p.Value.Syntax)),
                Constant p => ConstantPattern(((ExpressionSyntax)p.Value.Syntax).WithoutTrailingTrivia()),
                Type p => TypePattern(p.TypeSymbol.GenerateTypeSyntax()),
                Variable p => VarPattern(SingleVariableDesignation(Identifier(p.DeclaredSymbol.Name))),
                var p when !recurse => AsRecursivePatternSyntax(p, out expression),
                var p => throw UnexpectedNode(p)
            };

            return pattern;
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

        private static PatternSyntax AsRecursivePatternSyntax(AnalyzedNode node, out ExpressionSyntax? expression)
        {
            Debug.Assert(!(node is OrSequence), "!(node is OrSequence)");

            ITypeSymbol? type = null;
            VariableDesignationSyntax? designation = null;

            using var _0 = ArrayBuilder<PositionalPatternClauseSyntax>.GetInstance(out var positionalClauses);
            using var _1 = ArrayBuilder<(MemberEvaluation Member, PatternSyntax Pattern)>.GetInstance(out var propertySubpatterns);
            using var _2 = ArrayBuilder<PatternSyntax?>.GetInstance(out var tupleSubpatterns);
            using var _3 = ArrayBuilder<PatternSyntax?>.GetInstance(out var indexedSubpatterns);
            using var _4 = ArrayBuilder<PatternSyntax>.GetInstance(out var remainingPatterns);
            using var _5 = ArrayBuilder<ExpressionSyntax>.GetInstance(out var remainingExpressions);

            INamedTypeSymbol? tupleTypeOpt = null;

            Visit(node);
            void Visit(AnalyzedNode node)
            {
                switch (node)
                {
                    case AndSequence seq:
                        foreach (var item in seq.Nodes)
                            Visit(item);
                        break;

                    case Pair { Input: Type t } p:
                        if (type is null)
                        {
                            type = t.TypeSymbol;
                            Visit(p.Pattern);
                        }
                        else
                        {
                            remainingPatterns.Add(AsRecursivePatternSyntax(p, out var remainingExpr));
                            remainingExpressions.AddIfNotNull(remainingExpr);
                        }
                        break;

                    case MemberEvaluation { IsTupleItemField: true } field:
                        AddTupleSubpattern(field.Symbol, AsConstantPattern(true));
                        break;
                    case Not { Operand: MemberEvaluation { IsTupleItemField: true } field }:
                        AddTupleSubpattern(field.Symbol, AsConstantPattern(false));
                        break;
                    case Pair { Input: MemberEvaluation { IsTupleItemField: true } field } pair:
                        AddTupleSubpattern(field.Symbol, AsPatternSyntax(pair.Pattern));
                        break;

                    case Pair { Input: MemberEvaluation { IsITupleLengthProperty: true }, Pattern: Constant constant }:
                        indexedSubpatterns.SetItem((int)constant.Value.ConstantValue.Value - 1, null);
                        break;

                    case IndexEvaluation indexer:
                        indexedSubpatterns.SetItem(indexer.Index, AsConstantPattern(true));
                        type ??= indexer.Property.ContainingType;
                        break;
                    case Not { Operand: IndexEvaluation indexer }:
                        indexedSubpatterns.SetItem(indexer.Index, AsConstantPattern(false));
                        type ??= indexer.Property.ContainingType;
                        break;
                    case Pair { Input: IndexEvaluation indexer } pair:
                        indexedSubpatterns.SetItem(indexer.Index, AsPatternSyntax(pair.Pattern));
                        type ??= indexer.Property.ContainingType;
                        break;

                    case MemberEvaluation member:
                        propertySubpatterns.Add((member, AsConstantPattern(true)));
                        break;
                    case Not { Operand: MemberEvaluation member }:
                        propertySubpatterns.Add((member, AsConstantPattern(false)));
                        break;
                    case Pair { Input: MemberEvaluation member } pair:
                        propertySubpatterns.Add((member, AsPatternSyntax(pair.Pattern)));
                        break;

                    case Pair { Input: DeconstructEvaluation input } pair:
                        positionalClauses.Add(AsPositionalPatternClauseSyntax(input, pair));
                        break;

                    case Variable v when designation is null:
                        designation = SingleVariableDesignation(Identifier(v.DeclaredSymbol.Name));
                        break;
                    case Type t when type is null:
                        type = t.TypeSymbol;
                        break;

                    case NotNull:
                        break;

                    case Pair { Input: OperationEvaluation } p:
                        remainingExpressions.Add(AsExpressionSyntax(p));
                        break;
                    case OperationEvaluation p:
                        remainingExpressions.Add(AsExpressionSyntax(p));
                        break;

                    case Not p:
                        remainingPatterns.Add(UnaryPattern(AsPatternSyntax(p.Operand)));
                        break;

                    case var v:
                        remainingPatterns.Add(AsPatternSyntax(v, recurse: true));
                        break;
                }
            }

            if (!tupleSubpatterns.IsEmpty())
            {
                Debug.Assert(tupleTypeOpt != null);
                var arity = tupleTypeOpt.Arity;
                if (tupleSubpatterns.Count < arity)
                    tupleSubpatterns.SetItem(arity - 1, null);
                var list = tupleSubpatterns.Select(p => Subpattern(p ?? DiscardPattern()));
                var positional = PositionalPatternClause(SeparatedList(list));
                remainingPatterns.Add(RecursivePattern(null, positional, null, null));
            }
            else if (!indexedSubpatterns.IsEmpty())
            {
                var list = indexedSubpatterns.Select(p => Subpattern(p ?? DiscardPattern()));
                var positional = PositionalPatternClause(SeparatedList(list));
                remainingPatterns.Add(RecursivePattern(null, positional, null, null));
            }

            if (!positionalClauses.IsEmpty() ||
                !propertySubpatterns.IsEmpty())
            {
                var properties = PropertyPatternClause(SeparatedList(propertySubpatterns
                    .OrderBy(p => p.Member.Syntax.SpanStart)
                    .Select(p => AsSubpatternSyntax(p.Member, p.Pattern))));
                var recursive = RecursivePattern(type?.GenerateTypeSyntax(), positionalClauses.FirstOrDefault(), properties, designation);
                remainingPatterns.Insert(0, recursive.WithAdditionalAnnotations(Simplifier.Annotation));
                remainingPatterns.AddRange(positionalClauses.Skip(1).Select(
                    positional => RecursivePattern(null, positional, null, null)));
            }
            else
            {
                PatternSyntax? pattern = (type, designation) switch
                {
                    ({ } t, { } d) => DeclarationPattern(t.GenerateTypeSyntax(), d),
                    ({ } t, _) => TypePattern(t.GenerateTypeSyntax()),
                    (_, { } d) => RecursivePattern(null, null, PropertyPatternClause(SeparatedList<SubpatternSyntax>()), d),
                    _ => null,
                };

                if (pattern != null)
                    remainingPatterns.Insert(0, pattern);
            }

            expression = remainingExpressions.AggregateOrDefault(
                (left, right) => BinaryExpression(SyntaxKind.LogicalAndExpression, left, right));

            return remainingPatterns.Aggregate(
                (left, right) => BinaryPattern(SyntaxKind.AndPattern, left, right));

            void AddTupleSubpattern(ISymbol tupleField, PatternSyntax pattern)
            {
                Debug.Assert(Regex.IsMatch(tupleField.Name, @"^Item\d+$"));
                Debug.Assert(tupleTypeOpt is null || tupleTypeOpt.Equals(tupleField.ContainingSymbol));
                var position = int.Parse(tupleField.Name.Substring(4));
                tupleSubpatterns.SetItem(position - 1, pattern);
                tupleTypeOpt ??= (INamedTypeSymbol)tupleField.ContainingSymbol;
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
            var tests = ArrayBuilder<AnalyzedNode>.GetInstance();
            foreach (var group in ConsecutiveGroups(constants))
            {
                var bucket = group.AsList();
                Debug.Assert(bucket.Count > 0);
                if (bucket.Count <= 2)
                {
                    tests.AddRange(bucket);
                }
                else
                {
                    var builder = ArrayBuilder<AnalyzedNode>.GetInstance(2);
                    builder.Add(new Relational(null, BinaryOperatorKind.GreaterThanOrEqual, bucket[0].Value));
                    builder.Add(new Relational(null, BinaryOperatorKind.LessThanOrEqual, bucket[^1].Value));
                    tests.Add(AndSequence.Create(builder));
                }
            }

            return OrSequence.Create(tests);

            static IEnumerable<IEnumerable<Constant>> ConsecutiveGroups(ImmutableArray<Constant> source)
            {
                // Finding consecutive groups. This works by grouping constants by the difference with their ordered index
                // For instance:
                //
                //      Constants:      8, 3, 5, 1, 6, 2
                //      Ordered:        1, 2, 3, 5, 6, 8
                //      Index:          0, 1, 2, 3, 4, 5
                //      Difference:     1, 1, 1, 2, 2, 3
                //      Groups:         {1, 2, 3}, {5, 6}, {8}
                //      Result:         >=1 and <=3, 5, 6, 8
                //
                // We'll convert groups with more than two elements to a range check
                //
                return source
                    .Select(constant => (constant, value: (IConvertible)constant.Value.ConstantValue.Value))
                    .OrderBy(item => item.value)
                    .Select((item, index) => (item.constant, item.value, index))
                    .GroupBy(item => item.value.ToUInt64(null) - (uint)item.index, item => item.constant);
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

            private static SyntaxToken MakeToken(SyntaxKind kind, bool newlineBefore = false, bool newlineAfter = false)
                => Token(
                    leading: newlineBefore ? TriviaList(ElasticCarriageReturnLineFeed) : default,
                    kind,
                    trailing: newlineAfter ? TriviaList(ElasticCarriageReturnLineFeed) : default);

            private static IEnumerable<SyntaxToken> MakeSeparators(int count, bool multiline)
                => ArrayBuilder<SyntaxToken>.GetInstance(count,
                    MakeToken(SyntaxKind.CommaToken, newlineAfter: multiline)).ToArrayAndFree();

            public override SyntaxNode VisitPropertyPatternClause(PropertyPatternClauseSyntax node)
            {
                var multiline = node.Width() > 50;
                var subpatterns = node.Subpatterns;
                // Add trailing comma if multiline
                var separatorCount = subpatterns.Count - (multiline ? 0 : 1);
                return node.Update(
                    MakeToken(SyntaxKind.OpenBraceToken, newlineAfter: multiline),
                    SeparatedList(VisitList(subpatterns), MakeSeparators(separatorCount, multiline)),
                    MakeToken(SyntaxKind.CloseBraceToken, newlineBefore: multiline));
            }
        }
    }
}
