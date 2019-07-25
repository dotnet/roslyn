// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal enum TupleBinaryOperatorInfoKind
    {
        Single,
        NullNull,
        Multiple
    }

    /// <summary>
    /// A tree of binary operators for tuple comparisons.
    ///
    /// For <c>(a, (b, c)) == (d, (e, f))</c> we'll hold a Multiple with two elements.
    /// The first element is a Single (describing the binary operator and conversions that are involved in <c>a == d</c>).
    /// The second element is a Multiple containing two Singles (one for the <c>b == e</c> comparison and the other for <c>c == f</c>).
    /// </summary>
    internal abstract class TupleBinaryOperatorInfo
    {
        internal abstract TupleBinaryOperatorInfoKind InfoKind { get; }
        internal readonly TypeSymbol LeftConvertedTypeOpt;
        internal readonly TypeSymbol RightConvertedTypeOpt;
#if DEBUG
        internal abstract TreeDumperNode DumpCore();
        internal string Dump() => TreeDumper.DumpCompact(DumpCore());
#endif

        private TupleBinaryOperatorInfo(TypeSymbol leftConvertedTypeOpt, TypeSymbol rightConvertedTypeOpt)
        {
            LeftConvertedTypeOpt = leftConvertedTypeOpt;
            RightConvertedTypeOpt = rightConvertedTypeOpt;
        }

        /// <summary>
        /// Holds the information for an element-wise comparison (like <c>a == b</c> as part of <c>(a, ...) == (b, ...)</c>)
        /// </summary>
        internal class Single : TupleBinaryOperatorInfo
        {
            internal readonly BinaryOperatorKind Kind;
            internal readonly MethodSymbol MethodSymbolOpt; // User-defined comparison operator, if applicable

            internal readonly Conversion ConversionForBool; // If a conversion to bool exists, then no operator needed. If an operator is needed, this holds the conversion for input to that operator.
            internal readonly UnaryOperatorSignature BoolOperator; // Information for op_true or op_false

            internal Single(TypeSymbol leftConvertedTypeOpt, TypeSymbol rightConvertedTypeOpt, BinaryOperatorKind kind,
                MethodSymbol methodSymbolOpt,
                Conversion conversionForBool, UnaryOperatorSignature boolOperator) : base(leftConvertedTypeOpt, rightConvertedTypeOpt)
            {
                Kind = kind;
                MethodSymbolOpt = methodSymbolOpt;
                ConversionForBool = conversionForBool;
                BoolOperator = boolOperator;

                Debug.Assert(Kind.IsUserDefined() == ((object)MethodSymbolOpt != null));
            }

            internal override TupleBinaryOperatorInfoKind InfoKind
                => TupleBinaryOperatorInfoKind.Single;

            public override string ToString()
                => $"binaryOperatorKind: {Kind}";

#if DEBUG
            internal override TreeDumperNode DumpCore()
            {
                var sub = new List<TreeDumperNode>();
                if ((object)MethodSymbolOpt != null)
                {
                    sub.Add(new TreeDumperNode("methodSymbolOpt", MethodSymbolOpt.ToDisplayString(), null));
                }
                sub.Add(new TreeDumperNode("leftConversion", LeftConvertedTypeOpt.ToDisplayString(), null));
                sub.Add(new TreeDumperNode("rightConversion", RightConvertedTypeOpt.ToDisplayString(), null));

                return new TreeDumperNode("nested", Kind, sub);
            }
#endif
        }

        /// <summary>
        /// Holds the information for a tuple comparison, either at the top-level (like <c>(a, b) == ...</c>) or nested (like <c>(..., (a, b)) == (..., ...)</c>).
        /// </summary>
        internal class Multiple : TupleBinaryOperatorInfo
        {
            internal readonly ImmutableArray<TupleBinaryOperatorInfo> Operators;

            static internal readonly Multiple ErrorInstance =
                new Multiple(operators: ImmutableArray<TupleBinaryOperatorInfo>.Empty, leftConvertedTypeOpt: null, rightConvertedTypeOpt: null);

            internal Multiple(ImmutableArray<TupleBinaryOperatorInfo> operators, TypeSymbol leftConvertedTypeOpt, TypeSymbol rightConvertedTypeOpt)
                : base(leftConvertedTypeOpt, rightConvertedTypeOpt)
            {
                Debug.Assert(leftConvertedTypeOpt is null || leftConvertedTypeOpt.StrippedType().IsTupleType);
                Debug.Assert(rightConvertedTypeOpt is null || rightConvertedTypeOpt.StrippedType().IsTupleType);
                Debug.Assert(!operators.IsDefault);
                Debug.Assert(operators.IsEmpty || operators.Length > 1); // an empty array is used for error cases, otherwise tuples must have cardinality > 1

                Operators = operators;
            }

            internal override TupleBinaryOperatorInfoKind InfoKind
                => TupleBinaryOperatorInfoKind.Multiple;

#if DEBUG
            internal override TreeDumperNode DumpCore()
            {
                var sub = new List<TreeDumperNode>();
                sub.Add(new TreeDumperNode($"nestedOperators[{Operators.Length}]", null,
                    Operators.SelectAsArray(c => c.DumpCore())));

                return new TreeDumperNode("nested", null, sub);
            }
#endif
        }

        /// <summary>
        /// Represents an element-wise null/null comparison.
        /// For instance, <c>(null, ...) == (null, ...)</c>.
        /// </summary>
        internal class NullNull : TupleBinaryOperatorInfo
        {
            internal readonly BinaryOperatorKind Kind;

            internal NullNull(BinaryOperatorKind kind)
                : base(leftConvertedTypeOpt: null, rightConvertedTypeOpt: null)
            {
                Kind = kind;
            }

            internal override TupleBinaryOperatorInfoKind InfoKind
                => TupleBinaryOperatorInfoKind.NullNull;

#if DEBUG
            internal override TreeDumperNode DumpCore()
            {
                return new TreeDumperNode("nullnull", value: Kind, children: null);
            }
#endif
        }
    }
}
