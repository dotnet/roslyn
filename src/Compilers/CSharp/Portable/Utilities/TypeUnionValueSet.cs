// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The implementation of a value set for instances of types from a given set (a union), and a 'null' value.
    /// 
    /// For the sake of simplicity, the implementation is intentionally kept not thread safe.
    /// </summary>
    internal sealed class TypeUnionValueSet : IValueSet
    {
        private readonly ConversionsBase _conversions;

        /// <summary>
        /// The set of types defining a union of types, instances of which could be in the value set.
        /// </summary>
        private readonly ImmutableArray<TypeSymbol> _typesInUnion;

        /// <summary>
        /// Root of a logical tree defining values contained in this value set.
        /// 
        /// If an instance of type cannot be an instance of any of the types in the union,
        /// instances of that type are definitely not in the set. Otherwise, <see cref="_root"/>
        /// defines if instances of a given type are in the set.
        /// 
        /// See <see cref="EvaluateNodeForInputValue"/> function.
        /// If the tree evaluates to true for a given input type, instances of that type are definitely in the set.
        /// If the tree evaluates to false for a given input type, instances of that type are definitely not in the set.
        /// If the tree evaluates to 'null' (or an unknown result) for a given input type, instances of that type
        /// might or might not be in the set, we cannot give a definitive answer.
        /// 
        /// </summary>
        private readonly Node _root;

        private bool? _lazyMightIncludeNonNull;
        private bool? _lazyIncludesNull;

        private TypeUnionValueSet(
            ImmutableArray<TypeSymbol> typesInUnion,
            Node root,
            ConversionsBase conversions)
        {
            Debug.Assert(!typesInUnion.IsEmpty);
            Debug.Assert(!typesInUnion.Any(t => t.IsNullableType()));
            Debug.Assert(typesInUnion.Distinct().Length == typesInUnion.Length);
            _typesInUnion = typesInUnion;
            _root = root;
            _conversions = conversions;
        }

        internal static TypeUnionValueSet AllValues(ImmutableArray<TypeSymbol> typesInUnion, ConversionsBase conversions)
        {
            return new TypeUnionValueSet(typesInUnion, IsTrueNode.Instance, conversions);
        }

        internal static TypeUnionValueSet FromTypeMatch(ImmutableArray<TypeSymbol> typesInUnion, TypeSymbol type, ConversionsBase conversions, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            if (AnyTypeFromUnionMightMatch(typesInUnion, type, conversions, ref useSiteInfo))
            {
                return new TypeUnionValueSet(typesInUnion, new IsTypeNode(type), conversions);
            }

            // An empty set
            return new TypeUnionValueSet(typesInUnion, IsFalseNode.Instance, conversions);
        }

        private static bool AnyTypeFromUnionMightMatch(ImmutableArray<TypeSymbol> typesInUnion, TypeSymbol type, ConversionsBase conversions, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(!typesInUnion.IsEmpty);
            Debug.Assert(!typesInUnion.Any(TypeSymbolExtensions.IsNullableType));
            Debug.Assert(typesInUnion.Distinct().Length == typesInUnion.Length);

            foreach (var t in typesInUnion)
            {
                ConstantValue? matches = DecisionDagBuilder.ExpressionOfTypeMatchesPatternTypeForLearningFromSuccessfulTypeTest(conversions, type, t, ref useSiteInfo);
                if (matches == ConstantValue.False)
                {
                    // If 'type' could never be 't'
                    // v is type --> !(v is t)
                    continue;
                }

                return true;
            }

            return false;
        }

        internal static TypeUnionValueSet FromNullMatch(ImmutableArray<TypeSymbol> typesInUnion, ConversionsBase conversions)
        {
            return new TypeUnionValueSet(typesInUnion, IsNullNode.Instance, conversions);
        }

        internal static TypeUnionValueSet FromNonNullMatch(ImmutableArray<TypeSymbol> typesInUnion, ConversionsBase conversions)
        {
            return new TypeUnionValueSet(typesInUnion, new NotNode(IsNullNode.Instance), conversions);
        }

        public bool MightIncludeNonNull(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            if (!_lazyMightIncludeNonNull.HasValue)
            {
                if (_root == (object)IsTrueNode.Instance)
                {
                    _lazyMightIncludeNonNull = true;
                }
                else if (_root == (object)IsFalseNode.Instance)
                {
                    _lazyMightIncludeNonNull = false;
                }
                else
                {
                    _lazyMightIncludeNonNull = TryGetSampleType(_root, ref useSiteInfo) is not null;
                }
            }

            return _lazyMightIncludeNonNull.GetValueOrDefault();
        }

        public bool IncludesNull
        {
            get
            {
                if (!_lazyIncludesNull.HasValue)
                {
                    if (_root == (object)IsTrueNode.Instance)
                    {
                        _lazyIncludesNull = true;
                    }
                    else if (_root == (object)IsFalseNode.Instance)
                    {
                        _lazyIncludesNull = false;
                    }
                    else
                    {
                        // Null checks do not check conversions, therefore we can pass discarded use-site info,
                        // and not ask consumers to pass it to us.
                        var discardedInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                        bool? result = EvaluateNodeForInputValue(_root, null, ref discardedInfo);
                        Debug.Assert(result.HasValue);
                        _lazyIncludesNull = result.GetValueOrDefault();
                    }
                }

                return _lazyIncludesNull.GetValueOrDefault();
            }
        }

        /// <summary>
        /// Returns true only when the set is definetely empty, i.e. it does not include 'null' value and
        /// definitely doesn't include an instance of a type from the union.
        /// </summary>
        public bool IsEmpty(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            return !IncludesNull && !MightIncludeNonNull(ref useSiteInfo);
        }

        /// <param name="inputValue">Type symbol, or 'null' when we want to perform a check for null value.</param>
        private bool? EvaluateNodeForInputValue(Node node, TypeSymbol? inputValue, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            switch (node)
            {
                case IsTrueNode:
                    return true;
                case IsFalseNode:
                    return false;
                case IsTypeNode { Type: var t2 }:
                    {
                        switch (inputValue)
                        {
                            case null:
                                return false;
                            case TypeSymbol t1:
                                return evaluateTypeMatch(t1, t2, ref useSiteInfo);
                            default:
                                throw ExceptionUtilities.UnexpectedValue(inputValue);
                        }
                    }
                case NotNode not:
                    {
                        return !EvaluateNodeForInputValue(not.Negated, inputValue, ref useSiteInfo);
                    }
                case IsNullNode:
                    {
                        switch (inputValue)
                        {
                            case null:
                                return true;
                            case TypeSymbol:
                                return false;
                            default:
                                throw ExceptionUtilities.UnexpectedValue(inputValue);
                        }
                    }
                case AndNode andNode:
                    {
                        var leftResult = EvaluateNodeForInputValue(andNode.Left, inputValue, ref useSiteInfo);
                        var rightResult = EvaluateNodeForInputValue(andNode.Right, inputValue, ref useSiteInfo);
                        if (leftResult == false || rightResult == false)
                            return false;
                        if (leftResult == true && rightResult == true)
                            return true;

                        // Propagate unknown
                        return null;
                    }
                case OrNode orNode:
                    {
                        var leftResult = EvaluateNodeForInputValue(orNode.Left, inputValue, ref useSiteInfo);
                        var rightResult = EvaluateNodeForInputValue(orNode.Right, inputValue, ref useSiteInfo);
                        if (leftResult == true || rightResult == true)
                            return true;
                        if (leftResult == false && rightResult == false)
                            return false;

                        // Propagate unknown
                        return null;
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(node);
            }

            bool? evaluateTypeMatch(TypeSymbol t1, TypeSymbol t2, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
            {
                ConstantValue? matches = DecisionDagBuilder.ExpressionOfTypeMatchesPatternTypeForLearningFromSuccessfulTypeTest(_conversions, t1, t2, ref useSiteInfo);
                if (matches == ConstantValue.False)
                {
                    // If T1 could never be T2
                    // v is T1 --> !(v is T2)
                    return false;
                }
                else if (matches == ConstantValue.True)
                {
                    // If T1: T2
                    // v is T1 --> v is T2
                    return true;
                }

                return null;
            }
        }

        public TypeSymbol? SampleType(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            if (IsEmpty(ref useSiteInfo))
                throw new ArgumentException();

            if (_lazyMightIncludeNonNull != false)
            {
                return TryGetSampleType(_root, ref useSiteInfo);
            }

            return null;
        }

        private TypeSymbol? TryGetSampleType(Node root, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            foreach (var t in _typesInUnion)
            {
                if (EvaluateNodeForInputValue(root, t, ref useSiteInfo) != false)
                    return t;
            }

            return null;
        }

        IValueSet IValueSet.Complement()
        {
            return Complement();
        }

        IValueSet IValueSet.Intersect(IValueSet other)
        {
            return Intersect((TypeUnionValueSet)other);
        }

        IValueSet IValueSet.Union(IValueSet other)
        {
            return Union((TypeUnionValueSet)other);
        }

        public TypeUnionValueSet Complement()
        {
            if (_root == (object)IsTrueNode.Instance)
            {
                return new TypeUnionValueSet(_typesInUnion, IsFalseNode.Instance, _conversions);
            }

            if (_root == (object)IsFalseNode.Instance)
            {
                return new TypeUnionValueSet(_typesInUnion, IsTrueNode.Instance, _conversions);
            }

            if (_root is not NotNode { Negated: var negated })
            {
                negated = new NotNode(_root);
            }

            return new TypeUnionValueSet(_typesInUnion, negated, _conversions);
        }

        public TypeUnionValueSet Intersect(TypeUnionValueSet other)
        {
            Debug.Assert(_typesInUnion.SequenceEqual(other._typesInUnion));

            if (_root == (object)IsFalseNode.Instance)
            {
                return this;
            }

            if (other._root == (object)IsFalseNode.Instance)
            {
                return other;
            }

            if (_root == (object)IsTrueNode.Instance)
            {
                return other;
            }

            if (other._root == (object)IsTrueNode.Instance)
            {
                return this;
            }

            return new TypeUnionValueSet(_typesInUnion, new AndNode(_root, other._root), _conversions);
        }

        public TypeUnionValueSet Union(TypeUnionValueSet other)
        {
            Debug.Assert(_typesInUnion.SequenceEqual(other._typesInUnion));

            if (_root == (object)IsFalseNode.Instance)
            {
                return other;
            }

            if (other._root == (object)IsFalseNode.Instance)
            {
                return this;
            }

            if (_root == (object)IsTrueNode.Instance)
            {
                return this;
            }

            if (other._root == (object)IsTrueNode.Instance)
            {
                return other;
            }

            return new TypeUnionValueSet(_typesInUnion, new OrNode(_root, other._root), _conversions);
        }

        public bool TypeMatchesAllValuesIfAny(TypeSymbol type, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            if (IsEmpty(ref useSiteInfo) || IncludesNull)
            {
                return false;
            }

            if (!AnyTypeFromUnionMightMatch(_typesInUnion, type, _conversions, ref useSiteInfo))
            {
                return false;
            }

            if (EvaluateNodeForInputValue(_root, type, ref useSiteInfo) == false)
            {
                return false;
            }

            // Nothing else can match after we exclude all instances of the 'type' from the set
            return TryGetSampleType(new AndNode(_root, new NotNode(new IsTypeNode(type))), ref useSiteInfo) is null;
        }

        /// <summary>
        /// For debugging purposes only.
        /// </summary>
        public override string ToString()
        {
            var copy = new TypeUnionValueSet(_typesInUnion, _root, _conversions);
            string prefix = "";

            var discardedInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            if (copy.IsEmpty(ref discardedInfo))
            {
                prefix += "Empty: ";
            }

            if (_root == (object)IsTrueNode.Instance)
            {
                prefix += "AllValues: ";
            }

            return prefix + _root.ToString();
        }

        /// <summary>
        /// Base class for nodes in the logical tree defining values contained in the set.
        /// </summary>
        private abstract class Node
        {
            /// <summary>
            /// For debugging purposes only.
            /// </summary>
            public abstract override string ToString();
        }

        private sealed class IsTypeNode(TypeSymbol type) : Node
        {
            public TypeSymbol Type { get; } = type;

            public sealed override string ToString()
            {
                return Type.ToDisplayString();
            }
        }

        private sealed class IsNullNode : Node
        {
            public static readonly IsNullNode Instance = new IsNullNode();
            private IsNullNode() { }

            public override string ToString()
            {
                return "null";
            }
        }

        /// <summary>
        /// Can be used only as a root.  
        /// </summary>
        private sealed class IsTrueNode : Node
        {
            public static readonly IsTrueNode Instance = new IsTrueNode();
            private IsTrueNode() { }

            public override string ToString()
            {
                return "true";
            }
        }

        /// <summary>
        /// Can be used only as a root.  
        /// </summary>
        private sealed class IsFalseNode : Node
        {
            public static readonly IsFalseNode Instance = new IsFalseNode();
            private IsFalseNode() { }

            public override string ToString()
            {
                return "false";
            }
        }

        private abstract class BinaryNode : Node
        {
            public Node Left { get; }
            public Node Right { get; }

            public BinaryNode(Node left, Node right)
            {
                Debug.Assert(left is not (IsTrueNode or IsFalseNode));
                Debug.Assert(right is not (IsTrueNode or IsFalseNode));
                Left = left;
                Right = right;
            }

            public override string ToString()
            {
                return "(" + Left.ToString() + (this is AndNode ? " & " : " | ") + Right.ToString() + ")";
            }
        }

        private sealed class AndNode(Node left, Node right) : BinaryNode(left, right)
        {
        }

        private sealed class OrNode(Node left, Node right) : BinaryNode(left, right)
        {
        }

        private sealed class NotNode : Node
        {
            public Node Negated { get; }

            public NotNode(Node negated)
            {
                Debug.Assert(negated is not (IsTrueNode or IsFalseNode));
                Negated = negated;
            }

            public override string ToString()
            {
                return "!(" + Negated.ToString() + ")";
            }
        }
    }
}
