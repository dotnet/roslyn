// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    internal abstract class AnalyzedNode
    {
        // TODO: this should be unnecessary if we redefine the tree using records
        public sealed override bool Equals(object? obj)
        {
            var other = (AnalyzedNode?)obj;
            if (ReferenceEquals(this, other))
                return true;
            if (GetType() != other?.GetType())
                return false;

            return (this, other) switch
            {
                (Test left, Test right) when !Equals(left.Input, right.Input) => false,
                (Not left, Not right) => Equals(left.Operand, right.Operand),
                (Type left, Type right) => left.TypeSymbol.Equals(right.TypeSymbol),
                (Constant left, Constant right) => SyntaxFactory.AreEquivalent(left.Value.Syntax, right.Value.Syntax),
                (Variable left, Variable right) => left.DeclaredSymbol.Equals(right.DeclaredSymbol),
                (Relational left, Relational right) => left.OperatorKind == right.OperatorKind &&
                                                       SyntaxFactory.AreEquivalent(left.Value.Syntax, right.Value.Syntax),
                (OperationEvaluation left, OperationEvaluation right) => SyntaxFactory.AreEquivalent(left.Syntax, right.Syntax),
                (MemberEvaluation left, MemberEvaluation right) => left.Symbol.Equals(right.Symbol),
                (DeconstructEvaluation left, DeconstructEvaluation right) => left.DeconstructMethod.Equals(right.DeconstructMethod),
                (IndexEvaluation left, IndexEvaluation right) => left.Index == right.Index && left.Property.Equals(right.Property),
                (Sequence left, Sequence right) => left.Nodes.SequenceEqual(right.Nodes, (object?)null, (left, right, _) => left.Equals(right)),
                (NotNull _, NotNull _) => true,
                _ => false,
            };
        }

        // TODO: this should be unnecessary if we redefine the tree using records
        public sealed override int GetHashCode()
        {
            return GetType().GetHashCode() ^
                   (GetSymbol(this)?.GetHashCode() ?? 0) ^
                   (this switch { Relational v => (int)v.OperatorKind, IndexEvaluation v => v.Index, _ => 0 }) ^
                   Hash.CombineValues(GetChildren(this));

            static ISymbol? GetSymbol(AnalyzedNode @this)
            {
                return @this switch
                {
                    DeconstructEvaluation v => v.DeconstructMethod,
                    IndexEvaluation v => v.Property,
                    MemberEvaluation v => v.Symbol,
                    Type v => v.TypeSymbol,
                    Variable v => v.DeclaredSymbol,
                    _ => null
                };
            }

            static ImmutableArray<AnalyzedNode> GetChildren(AnalyzedNode @this)
            {
                return @this switch
                {
                    Sequence seq => seq.Nodes,
                    Not not => ImmutableArray.Create(not.Operand),
                    Test { Input: { } input } => ImmutableArray.Create<AnalyzedNode>(input),
                    Pair pair => ImmutableArray.Create(pair.Input, pair.Pattern),
                    _ => ImmutableArray<AnalyzedNode>.Empty,
                };
            }
        }

        internal sealed class True : AnalyzedNode
        {
            public static readonly AnalyzedNode Instance = new True();
            private True() { }
        }

        internal sealed class False : AnalyzedNode
        {
            public static readonly AnalyzedNode Instance = new False();
            private False() { }
        }

        internal sealed class Not : AnalyzedNode
        {
            public readonly AnalyzedNode Operand;

            private Not(AnalyzedNode operand)
            {
                Operand = operand;
            }

            [return: NotNullIfNotNull("operand")]
            public static AnalyzedNode? Create(AnalyzedNode? operand)
            {
                Debug.Assert(!(operand is OperationEvaluation), "!(operand is OperationEvaluation)");

                return operand switch
                {
                    null => null,
                    True _ => False.Instance,
                    False _ => True.Instance,
                    Not p => p.Operand,
                    Relational p => new Relational(p.Input, Negate(p.OperatorKind), p.Value),
                    _ => new Not(operand)
                };

                static BinaryOperatorKind Negate(BinaryOperatorKind kind)
                {
                    return kind switch
                    {
                        BinaryOperatorKind.LessThan => BinaryOperatorKind.GreaterThanOrEqual,
                        BinaryOperatorKind.GreaterThan => BinaryOperatorKind.LessThanOrEqual,
                        BinaryOperatorKind.LessThanOrEqual => BinaryOperatorKind.GreaterThan,
                        BinaryOperatorKind.GreaterThanOrEqual => BinaryOperatorKind.LessThan,
                        var v => throw ExceptionUtilities.UnexpectedValue(v)
                    };
                }
            }
        }

        internal abstract class Sequence : AnalyzedNode
        {
            public readonly ImmutableArray<AnalyzedNode> Nodes;

            protected Sequence(ImmutableArray<AnalyzedNode> nodes)
            {
                Nodes = nodes;
            }

            /// <summary>
            /// NOTE: this will free the builder after we're done with it.
            /// </summary>
            public abstract AnalyzedNode Update(ArrayBuilder<AnalyzedNode> nodes);

            /// <summary>
            /// NOTE: this will free the builder after we're done with it.
            /// </summary>
            public abstract AnalyzedNode Negate(ArrayBuilder<AnalyzedNode> nodes);

            public static AnalyzedNode Create(bool disjunctive, AnalyzedNode left, AnalyzedNode right)
            {
                var builder = ArrayBuilder<AnalyzedNode>.GetInstance(2);
                builder.Add(left);
                builder.Add(right);
                return disjunctive ? OrSequence.Create(builder) : AndSequence.Create(builder);
            }
        }

        internal sealed class AndSequence : Sequence
        {
            public AndSequence(ImmutableArray<AnalyzedNode> nodes) : base(nodes) { }

            /// <inheritdoc/>
            public override AnalyzedNode Update(ArrayBuilder<AnalyzedNode> nodes)
            {
                return Create(nodes);
            }

            /// <inheritdoc/>
            public override AnalyzedNode Negate(ArrayBuilder<AnalyzedNode> nodes)
            {
                return OrSequence.Create(nodes);
            }

            /// <summary>
            /// NOTE: this will free the builder after we're done with it.
            /// </summary>
            public static AnalyzedNode Create(ArrayBuilder<AnalyzedNode> tests)
            {
                for (var i = tests.Count - 1; i >= 0; i--)
                {
                    switch (tests[i])
                    {
                        case True _:
                            tests.RemoveAt(i);
                            break;
                        case False f:
                            tests.Free();
                            return f;
                        case AndSequence seq:
                            var testsToInsert = seq.Nodes;
                            tests.RemoveAt(i);
                            for (int j = 0, n = testsToInsert.Length; j < n; j++)
                                tests.Insert(i + j, testsToInsert[j]);
                            break;
                    }
                }
                var result = tests.Count switch
                {
                    0 => True.Instance,
                    1 => tests[0],
                    _ => new AndSequence(tests.ToImmutable()),
                };
                tests.Free();
                return result;
            }
        }

        internal sealed class OrSequence : Sequence
        {
            public OrSequence(ImmutableArray<AnalyzedNode> nodes) : base(nodes) { }

            /// <inheritdoc/>
            public override AnalyzedNode Update(ArrayBuilder<AnalyzedNode> nodes)
            {
                return Create(nodes);
            }

            /// <inheritdoc/>
            public override AnalyzedNode Negate(ArrayBuilder<AnalyzedNode> nodes)
            {
                return AndSequence.Create(nodes);
            }

            /// <summary>
            /// NOTE: this will free the builder after we're done with it.
            /// </summary>
            public static AnalyzedNode Create(ArrayBuilder<AnalyzedNode> tests)
            {
                for (var i = tests.Count - 1; i >= 0; i--)
                {
                    switch (tests[i])
                    {
                        case False _:
                            tests.RemoveAt(i);
                            break;
                        case True t:
                            tests.Free();
                            return t;
                        case OrSequence seq:
                            tests.RemoveAt(i);
                            var testsToInsert = seq.Nodes;
                            for (int j = 0, n = testsToInsert.Length; j < n; j++)
                                tests.Insert(i + j, testsToInsert[j]);
                            break;
                    }
                }
                var result = tests.Count switch
                {
                    0 => False.Instance,
                    1 => tests[0],
                    _ => new OrSequence(tests.ToImmutable()),
                };
                tests.Free();
                return result;
            }
        }

        internal sealed class Pair : AnalyzedNode
        {
            public readonly Evaluation Input;
            public readonly AnalyzedNode Pattern;

            public Pair(Evaluation input, AnalyzedNode pattern)
            {
                Input = input;
                Pattern = pattern;
            }
        }

        internal abstract class Test : AnalyzedNode
        {
            public readonly Evaluation? Input;

            protected Test(Evaluation? input)
            {
                Input = input;
            }

            public Test WithInput(Evaluation? newInput)
            {
                return ReferenceEquals(newInput, Input) ? this : WithInputCore(newInput);
            }

            protected abstract Test WithInputCore(Evaluation? newInput);
        }

        internal sealed class Constant : Test
        {
            public readonly IOperation Value;

            public Constant(Evaluation? input, IOperation value) : base(input)
            {
                Debug.Assert(value.ConstantValue.HasValue);
                Value = value;
            }

            protected override Test WithInputCore(Evaluation? newInput)
            {
                return new Constant(newInput, Value);
            }
        }

        internal sealed class Relational : Test
        {
            public readonly BinaryOperatorKind OperatorKind;
            public readonly IOperation Value;

            public Relational(Evaluation? input, BinaryOperatorKind operatorKind, IOperation value) : base(input)
            {
                Debug.Assert(value.ConstantValue.HasValue);
                OperatorKind = operatorKind;
                Value = value;
            }

            protected override Test WithInputCore(Evaluation? newInput)
            {
                return new Relational(newInput, OperatorKind, Value);
            }
        }

        internal abstract class Evaluation : Test
        {
            // Record the syntax so we don't have to recreate the whole
            // node if this happens to be rewritten as an expression.
            public readonly SyntaxNode? Syntax;

            protected Evaluation(Evaluation? input, SyntaxNode? syntax = null) : base(input)
            {
                Syntax = syntax;
            }

            public new Evaluation WithInput(Evaluation? newInput)
            {
                return (Evaluation)base.WithInput(newInput);
            }
        }

        internal sealed class NotNull : Evaluation
        {
            public NotNull(Evaluation? input)
                : base(input)
            {
            }

            protected override Test WithInputCore(Evaluation? newInput)
            {
                return new NotNull(newInput);
            }
        }

        internal sealed class Variable : Evaluation
        {
            public readonly ISymbol DeclaredSymbol;

            public Variable(Evaluation? input, ISymbol symbol, SyntaxNode? syntax = null) : base(input, syntax)
            {
                Debug.Assert(symbol is ILocalSymbol || symbol is IFieldSymbol);
                DeclaredSymbol = symbol;
            }

            protected override Test WithInputCore(Evaluation? newInput)
            {
                return new Variable(newInput, DeclaredSymbol, Syntax);
            }
        }

        internal sealed class Type : Evaluation
        {
            public readonly ITypeSymbol TypeSymbol;

            public Type(Evaluation? input, ITypeSymbol type) : base(input)
            {
                TypeSymbol = type;
            }

            protected override Test WithInputCore(Evaluation? newInput)
            {
                return new Type(newInput, TypeSymbol);
            }
        }

        internal sealed class IndexEvaluation : Evaluation
        {
            public readonly IPropertySymbol Property;
            public readonly int Index;

            public IndexEvaluation(Evaluation? input, IPropertySymbol property, int index, SyntaxNode? syntax = null)
                : base(input, syntax)
            {
                Property = property;
                Index = index;
            }

            protected override Test WithInputCore(Evaluation? newInput)
            {
                return new IndexEvaluation(newInput, Property, Index, Syntax);
            }
        }

        internal sealed class MemberEvaluation : Evaluation
        {
            public readonly ISymbol Symbol;

            private MemberEvaluation(Evaluation? input, ISymbol symbol, SyntaxNode? syntax)
                : base(input, syntax)
            {
                Symbol = symbol;
            }

            public MemberEvaluation(Evaluation? input, IFieldReferenceOperation op)
                : this(input, op.Field.CorrespondingTupleField ?? op.Field, op.Syntax)
            {
            }

            public MemberEvaluation(Evaluation? input, IPropertyReferenceOperation op)
                : this(input, op.Property, op.Syntax)
            {
            }

            public MemberEvaluation(Evaluation? input, IFieldSymbol field)
                : this(input, field, syntax: null)
            {
                Debug.Assert(field.IsTupleField());
            }

            public bool IsTupleItemField => Symbol.IsTupleField();

            public bool IsITupleLengthProperty
            {
                get
                {
                    return Symbol is
                    {
                        Kind: SymbolKind.Property,
                        Name: WellKnownMemberNames.LengthPropertyName,
                        ContainingType: { Name: "ITuple" }
                    };
                }
            }

            protected override Test WithInputCore(Evaluation? newInput)
            {
                return new MemberEvaluation(newInput, Symbol, Syntax);
            }
        }

        internal sealed class DeconstructEvaluation : Evaluation
        {
            public readonly IMethodSymbol DeconstructMethod;

            public DeconstructEvaluation(Evaluation? input, IMethodSymbol deconstructMethod)
                : base(input)
            {
                DeconstructMethod = deconstructMethod;
            }

            protected override Test WithInputCore(Evaluation? newInput)
            {
                return new DeconstructEvaluation(newInput, DeconstructMethod);
            }
        }

        internal sealed class OutVariableEvaluation : Evaluation
        {
            public readonly int Index;

            public OutVariableEvaluation(Evaluation? input, int index) : base(input)
            {
                Debug.Assert(input is DeconstructEvaluation);
                Index = index;
            }

            protected override Test WithInputCore(Evaluation? newInput)
            {
                return new OutVariableEvaluation(newInput, Index);
            }
        }

        internal sealed class OperationEvaluation : Evaluation
        {
            public OperationEvaluation(IOperation operation)
                : base(input: null, operation.Syntax)
            {
            }

            protected override Test WithInputCore(Evaluation? newInput)
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

#if DEBUG
        internal string Dump()
        {
            return DumpNode(this);

            static string DumpNode(AnalyzedNode node, int depth = 0)
            {
                return node switch
                {
                    False _ => "FALSE",
                    True _ => "TRUE",
                    Test test => $"{(test.Input is null ? null : $"{DumpNode(test.Input)}<-")}{DumpTest(test)}",
                    Sequence seq => $"{(seq is AndSequence ? "AND" : "OR")}({string.Concat(seq.Nodes.Select(n => $"\n{new string(' ', (depth + 1) * 4)}{DumpNode(n, depth + 1)}"))})",
                    Not not => $"NOT({DumpNode(not.Operand, depth + 1)})",
                    Pair pair => $"{DumpNode(pair.Input)} is {DumpNode(pair.Pattern, depth + 1)}",
                    var p => throw ExceptionUtilities.UnexpectedValue(p),
                };
            }

            static string DumpTest(Test test)
            {
                return test switch
                {
                    Constant p => $"{p.Value.Syntax}",
                    DeconstructEvaluation p => $"D:{p.DeconstructMethod.Name}",
                    IndexEvaluation p => $"this[{p.Index}]",
                    MemberEvaluation p => $"{p.Symbol.Name}",
                    NotNull _ => "{}",
                    OperationEvaluation p => $"E:{p.Syntax}",
                    OutVariableEvaluation p => $"out var v{p.Index}",
                    Type p => $"T:{p.TypeSymbol.Name}",
                    Variable p => $"V:{p.DeclaredSymbol.Name}",
                    Relational p => $"{GetText(p.OperatorKind)}{p.Value.Syntax}",
                    var p => throw ExceptionUtilities.UnexpectedValue(p),
                };
            }

            static string GetText(BinaryOperatorKind kind)
            {
                return kind switch
                {
                    BinaryOperatorKind.LessThan => "<",
                    BinaryOperatorKind.LessThanOrEqual => "<=",
                    BinaryOperatorKind.GreaterThan => ">",
                    BinaryOperatorKind.GreaterThanOrEqual => ">=",
                    _ => "??"
                };
            }
        }
#endif
    }
}
