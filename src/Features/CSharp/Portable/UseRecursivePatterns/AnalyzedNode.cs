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
    using static SyntaxFactory;

    internal abstract record AnalyzedNode
    {
        internal sealed record True : AnalyzedNode
        {
            public static readonly AnalyzedNode Instance = new True();
            private True() { }
        }

        internal sealed record False : AnalyzedNode
        {
            public static readonly AnalyzedNode Instance = new False();
            private False() { }
        }

        internal sealed record Not : AnalyzedNode
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
                    True => False.Instance,
                    False => True.Instance,
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

        internal abstract record Sequence : AnalyzedNode
        {
            public readonly ImmutableArray<AnalyzedNode> Nodes;

            protected Sequence(ImmutableArray<AnalyzedNode> nodes)
            {
                Nodes = nodes;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode() ^ Hash.CombineValues(Nodes);
            }

            public virtual bool Equals(Sequence? other)
            {
                return base.Equals(other) && Nodes.SequenceEqual(other.Nodes);
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

        internal sealed record AndSequence : Sequence
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
                        case True:
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

        internal sealed record OrSequence : Sequence
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
                        case False:
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

        internal sealed record Pair : AnalyzedNode
        {
            public readonly Evaluation Input;
            public readonly AnalyzedNode Pattern;

            public Pair(Evaluation input, AnalyzedNode pattern)
            {
                Input = input;
                Pattern = pattern;
            }
        }

        internal abstract record Test : AnalyzedNode
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

        internal sealed record Constant : Test
        {
            public readonly IOperation Value;

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public bool Equals(Constant? other)
            {
                return base.Equals(other) && AreEquivalent(Value.Syntax, other.Value.Syntax);
            }

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

        internal sealed record Relational : Test
        {
            public readonly BinaryOperatorKind OperatorKind;
            public readonly IOperation Value;

            public Relational(Evaluation? input, BinaryOperatorKind operatorKind, IOperation value) : base(input)
            {
                Debug.Assert(value.ConstantValue.HasValue);
                OperatorKind = operatorKind;
                Value = value;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public bool Equals(Relational? other)
            {
                return base.Equals(other) &&
                    OperatorKind == other.OperatorKind &&
                    AreEquivalent(Value.Syntax, other.Value.Syntax);
            }

            protected override Test WithInputCore(Evaluation? newInput)
            {
                return new Relational(newInput, OperatorKind, Value);
            }
        }

        internal abstract record Evaluation : Test
        {
            public readonly SyntaxNode Syntax;

            protected Evaluation(Evaluation? input, SyntaxNode syntax) : base(input)
            {
                Syntax = syntax;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public virtual bool Equals(Evaluation? other)
            {
                return base.Equals(other);
            }

            // UNDONE: Use covariant return types when available:
            // protected abstract override Evaluation WithInputCore(Evaluation? newInput);
            public new Evaluation WithInput(Evaluation? newInput)
            {
                return (Evaluation)base.WithInput(newInput);
            }
        }

        internal sealed record NotNull : Evaluation
        {
            public NotNull(Evaluation? input, SyntaxNode syntax)
                : base(input, syntax)
            {
            }

            protected override Test WithInputCore(Evaluation? newInput)
            {
                return new NotNull(newInput, Syntax);
            }
        }

        internal sealed record Variable : Evaluation
        {
            public readonly ISymbol DeclaredSymbol;

            public Variable(Evaluation? input, ISymbol symbol, SyntaxNode syntax) : base(input, syntax)
            {
                Debug.Assert(symbol is ILocalSymbol || symbol is IFieldSymbol);
                DeclaredSymbol = symbol;
            }

            protected override Test WithInputCore(Evaluation? newInput)
            {
                return new Variable(newInput, DeclaredSymbol, Syntax);
            }
        }

        internal sealed record Type : Evaluation
        {
            public readonly ITypeSymbol TypeSymbol;

            public Type(Evaluation? input, ITypeSymbol type, SyntaxNode syntax) : base(input, syntax)
            {
                TypeSymbol = type;
            }

            protected override Test WithInputCore(Evaluation? newInput)
            {
                return new Type(newInput, TypeSymbol, Syntax);
            }
        }

        internal sealed record IndexEvaluation : Evaluation
        {
            public readonly IPropertySymbol Property;
            public readonly int Index;

            public IndexEvaluation(Evaluation? input, IPropertySymbol property, int index, SyntaxNode syntax)
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

        internal sealed record MemberEvaluation : Evaluation
        {
            public readonly ISymbol Symbol;

            private MemberEvaluation(Evaluation? input, ISymbol symbol, SyntaxNode syntax)
                : base(input, syntax)
            {
                Symbol = symbol;
            }

            public MemberEvaluation(Evaluation? input, IFieldReferenceOperation op)
                : this(input, op.Field.CorrespondingTupleField ?? (ISymbol)op.Field, op.Syntax)
            {
            }

            public MemberEvaluation(Evaluation? input, IPropertyReferenceOperation op)
                : this(input, op.Property, op.Syntax)
            {
            }

            public MemberEvaluation(Evaluation? input, IFieldSymbol field, SyntaxNode syntax)
                : this(input, (ISymbol)field, syntax)
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

        internal sealed record DeconstructEvaluation : Evaluation
        {
            public readonly IMethodSymbol DeconstructMethod;

            public DeconstructEvaluation(Evaluation? input, IMethodSymbol deconstructMethod, SyntaxNode syntax)
                : base(input, syntax)
            {
                DeconstructMethod = deconstructMethod;
            }

            protected override Test WithInputCore(Evaluation? newInput)
            {
                return new DeconstructEvaluation(newInput, DeconstructMethod, Syntax);
            }
        }

        internal sealed record OutVariableEvaluation : Evaluation
        {
            public readonly int Index;

            public OutVariableEvaluation(Evaluation? input, int index, SyntaxNode syntax) : base(input, syntax)
            {
                Debug.Assert(input is DeconstructEvaluation);
                Index = index;
            }

            protected override Test WithInputCore(Evaluation? newInput)
            {
                return new OutVariableEvaluation(newInput, Index, Syntax);
            }
        }

        internal sealed record OperationEvaluation : Evaluation
        {
            public OperationEvaluation(IOperation operation)
                : base(input: null, operation.Syntax)
            {
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public bool Equals(OperationEvaluation? other)
            {
                return base.Equals(other) && AreEquivalent(Syntax, other.Syntax);
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
                    False => "FALSE",
                    True => "TRUE",
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
                    NotNull => "{}",
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
                    var v => throw ExceptionUtilities.UnexpectedValue(v)
                };
            }
        }
#endif
    }
}
