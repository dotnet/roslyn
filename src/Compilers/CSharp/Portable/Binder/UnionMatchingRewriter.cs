// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Rewrite special Union type matching with appropriate BoundRecursivePatterns
    /// against an IUnion.Value property.
    ///  
    /// The rewrite happens bottom-up. Nodes that require special Union treatment are represented with
    /// a <see cref="BoundPatternWithUnionMatching"/> node created during the rewrite. Rewriter keeps a 
    /// <see cref="BoundPatternWithUnionMatching"/> node at the top of the result until we reach a point
    /// when we are ready to perform its transformation, we call
    /// <see cref="RewritePatternWithUnionMatchingToPropertyPattern(BoundPattern)"/> helper at that point.
    /// Generally, the transformation must be performed for a pattern when, and only when, we know that no
    /// more conjunctions coming where the pattern could be the left hand side.
    /// 
    /// Assuming that '^' marks a Union matching pattern:
    /// 
    /// A pattern 'unionTypeInstance is int^' is transformed to 'unionTypeInstance is { Value: int }'.
    /// 
    /// A pattern 'unionTypeInstance is not^(int or string)' is transformed to 'unionTypeInstance is { Value: not (int or string) }'.
    ///
    /// A pattern 'unionTypeInstance is int^ or string^' is transformed to 'unionTypeInstance is { Value: int } or { Value: string }'.
    ///
    /// A pattern 'unionTypeInstance is int^ and 15 or string^' is transformed to 'unionTypeInstance is { Value: int and 15 } or { Value: string }'.
    /// </summary>
    sealed class UnionMatchingRewriter : BoundTreeRewriter
    {
        private readonly CSharpCompilation _compilation;

        private UnionMatchingRewriter(CSharpCompilation compilation)
        {
            _compilation = compilation;
        }

        public static BoundPattern Rewrite(CSharpCompilation compilation, BoundPattern pattern)
        {
            var result = new UnionMatchingRewriter(compilation).Visit(pattern);
            Debug.Assert(result != pattern);
            return RewritePatternWithUnionMatchingToPropertyPattern((BoundPattern)result);
        }

        protected override BoundNode? VisitExpressionOrPatternWithoutStackGuard(BoundNode node)
        {
            return Visit(node);
        }

        private NamedTypeSymbol ObjectType => _compilation.GetSpecialType(SpecialType.System_Object);

        private static BoundPatternWithUnionMatching CreatePatternWithUnionMatching(NamedTypeSymbol inputUnionType, BoundPattern innerPattern)
        {
            Debug.Assert(inputUnionType.IsUnionType);
            Debug.Assert(innerPattern.InputType.IsObjectType());

            PropertySymbol? valueProperty = Binder.GetUnionTypeValuePropertyNoUseSiteDiagnostics(inputUnionType);

            var member = new BoundPropertySubpatternMember(innerPattern.Syntax, receiver: null, valueProperty, type: innerPattern.InputType, hasErrors: valueProperty is null).MakeCompilerGenerated();

            return new BoundPatternWithUnionMatching(
                syntax: innerPattern.Syntax,
                inputUnionType,
                member,
                innerPattern,
                inputType: inputUnionType).MakeCompilerGenerated();
        }

        public override BoundNode? VisitConstantPattern(BoundConstantPattern node)
        {
            node = (BoundConstantPattern)base.VisitConstantPattern(node)!;
            if (node.IsUnionMatching)
            {
                return CreatePatternWithUnionMatching(
                    (NamedTypeSymbol)node.InputType,
                    node.Update(node.Value, node.ConstantValue, isUnionMatching: false, inputType: ObjectType, narrowedType: node.NarrowedType));
            }

            return node;
        }

        public override BoundNode? VisitRecursivePattern(BoundRecursivePattern node)
        {
            node = (BoundRecursivePattern)base.VisitRecursivePattern(node)!;
            if (node.IsUnionMatching)
            {
                return CreatePatternWithUnionMatching(
                    (NamedTypeSymbol)node.InputType,
                    node.Update(
                        node.DeclaredType, node.DeconstructMethod, node.Deconstruction, node.Properties, node.IsExplicitNotNullTest, node.Variable, node.VariableAccess,
                        isUnionMatching: false, inputType: ObjectType, narrowedType: node.NarrowedType));
            }

            return node;
        }

        public override BoundNode? VisitListPattern(BoundListPattern node)
        {
            Symbol? variable = node.Variable;
            ImmutableArray<BoundPattern> subpatterns = this.VisitList(node.Subpatterns).SelectAsArray(RewritePatternWithUnionMatchingToPropertyPattern);
            BoundExpression? lengthAccess = node.LengthAccess;
            BoundExpression? indexerAccess = node.IndexerAccess;
            BoundListPatternReceiverPlaceholder? receiverPlaceholder = node.ReceiverPlaceholder;
            BoundListPatternIndexPlaceholder? argumentPlaceholder = node.ArgumentPlaceholder;
            BoundExpression? variableAccess = node.VariableAccess;
            TypeSymbol? inputType = node.InputType;
            TypeSymbol? narrowedType = node.NarrowedType;

            if (node.IsUnionMatching)
            {
                return CreatePatternWithUnionMatching(
                    (NamedTypeSymbol)node.InputType,
                    node.Update(subpatterns, node.HasSlice, lengthAccess, indexerAccess, receiverPlaceholder, argumentPlaceholder, variable, variableAccess,
                        isUnionMatching: false, inputType: ObjectType, narrowedType));
            }

            return node.Update(subpatterns, node.HasSlice, lengthAccess, indexerAccess, receiverPlaceholder, argumentPlaceholder, variable, variableAccess, isUnionMatching: false, inputType, narrowedType);
        }

        public override BoundNode? VisitITuplePattern(BoundITuplePattern node)
        {
            node = (BoundITuplePattern)base.VisitITuplePattern(node)!;
            if (node.IsUnionMatching)
            {
                return CreatePatternWithUnionMatching(
                    (NamedTypeSymbol)node.InputType,
                    node.Update(node.GetLengthMethod, node.GetItemMethod, node.Subpatterns,
                        isUnionMatching: false, inputType: ObjectType, narrowedType: node.NarrowedType));
            }

            return node;
        }

        public override BoundNode? VisitDeclarationPattern(BoundDeclarationPattern node)
        {
            node = (BoundDeclarationPattern)base.VisitDeclarationPattern(node)!;
            if (node.IsUnionMatching)
            {
                return CreatePatternWithUnionMatching(
                    (NamedTypeSymbol)node.InputType,
                    node.Update(node.DeclaredType, node.IsVar, node.Variable, node.VariableAccess,
                        isUnionMatching: false, inputType: ObjectType, narrowedType: node.NarrowedType));
            }

            return node;
        }

        public override BoundNode? VisitTypePattern(BoundTypePattern node)
        {
            node = (BoundTypePattern)base.VisitTypePattern(node)!;
            if (node.IsUnionMatching)
            {
                return CreatePatternWithUnionMatching(
                    (NamedTypeSymbol)node.InputType,
                    node.Update(node.DeclaredType, node.IsExplicitNotNullTest, isUnionMatching: false, inputType: ObjectType, narrowedType: node.NarrowedType));
            }

            return node;
        }

        public override BoundNode? VisitRelationalPattern(BoundRelationalPattern node)
        {
            node = (BoundRelationalPattern)base.VisitRelationalPattern(node)!;
            if (node.IsUnionMatching)
            {
                return CreatePatternWithUnionMatching(
                    (NamedTypeSymbol)node.InputType,
                    node.Update(node.Relation, node.Value, node.ConstantValue, isUnionMatching: false, inputType: ObjectType, narrowedType: node.NarrowedType));
            }

            return node;
        }

        public override BoundNode? VisitNegatedPattern(BoundNegatedPattern node)
        {
            BoundPattern negated = RewritePatternWithUnionMatchingToPropertyPattern((BoundPattern)this.Visit(node.Negated));
            TypeSymbol? inputType = node.InputType;
            TypeSymbol? narrowedType = node.NarrowedType;

            if (node.IsUnionMatching)
            {
                return CreatePatternWithUnionMatching(
                    (NamedTypeSymbol)node.InputType,
                    node.Update(negated, isUnionMatching: false, inputType: ObjectType, narrowedType));
            }

            return node.Update(negated, isUnionMatching: false, inputType, narrowedType);
        }

        public override BoundNode? VisitSlicePattern(BoundSlicePattern node)
        {
            BoundPattern? pattern = RewritePatternWithUnionMatchingToPropertyPattern((BoundPattern)this.Visit(node.Pattern));
            BoundExpression? indexerAccess = node.IndexerAccess;
            BoundSlicePatternReceiverPlaceholder? receiverPlaceholder = node.ReceiverPlaceholder;
            BoundSlicePatternRangePlaceholder? argumentPlaceholder = node.ArgumentPlaceholder;
            TypeSymbol? inputType = node.InputType;
            TypeSymbol? narrowedType = node.NarrowedType;
            return node.Update(pattern, indexerAccess, receiverPlaceholder, argumentPlaceholder, inputType, narrowedType);
        }

        public override BoundNode? VisitPositionalSubpattern(BoundPositionalSubpattern node)
        {
            Symbol? symbol = node.Symbol;
            BoundPattern pattern = RewritePatternWithUnionMatchingToPropertyPattern((BoundPattern)this.Visit(node.Pattern));
            return node.Update(symbol, pattern);
        }

        public override BoundNode? VisitPropertySubpattern(BoundPropertySubpattern node)
        {
            BoundPropertySubpatternMember? member = node.Member;
            BoundPattern pattern = RewritePatternWithUnionMatchingToPropertyPattern((BoundPattern)this.Visit(node.Pattern));
            return node.Update(member, node.IsLengthOrCount, pattern);
        }

        public override BoundNode? VisitBinaryPattern(BoundBinaryPattern node)
        {
            var binaryPatternStack = ArrayBuilder<BoundBinaryPattern>.GetInstance();
            BoundBinaryPattern? currentNode = node;

            do
            {
                binaryPatternStack.Push(currentNode);
                currentNode = currentNode.Left as BoundBinaryPattern;
            } while (currentNode != null);

            Debug.Assert(binaryPatternStack.Count > 0);

            var binaryPattern = binaryPatternStack.Pop();
            BoundPattern result = (BoundPattern)Visit(binaryPattern.Left);
#if DEBUG
            var narrowedTypeCandidates = ArrayBuilder<TypeSymbol>.GetInstance(2);

            if (result is BoundPatternWithUnionMatching unionPattern)
            {
                narrowedTypeCandidates.Add(getDisjunctionType(unionPattern));
            }
            else
            {
                Binder.CollectDisjunctionTypes(result, narrowedTypeCandidates, hasUnionMatching: false);
            }
#endif
            do
            {
                result = rewriteBinaryPattern(
                    this,
                    result,
                    binaryPattern
#if DEBUG
                    , narrowedTypeCandidates
#endif
                    );
            }
            while (binaryPatternStack.TryPop(out binaryPattern));

            binaryPatternStack.Free();
#if DEBUG
            narrowedTypeCandidates.Free();
#endif
            return result;

            static BoundPattern rewriteBinaryPattern(
                UnionMatchingRewriter rewriter,
                BoundPattern preboundLeft,
                BoundBinaryPattern node
#if DEBUG
                , ArrayBuilder<TypeSymbol> narrowedTypeCandidates
#endif
                )
            {
                if (node.Disjunction)
                {
                    preboundLeft = RewritePatternWithUnionMatchingToPropertyPattern(preboundLeft);
                    var right = RewritePatternWithUnionMatchingToPropertyPattern((BoundPattern)rewriter.Visit(node.Right));

#if DEBUG
                    // Here we are verifying that the narrowed type computed during the initial binding phase in
                    // 'Binder.BindBinaryPattern.bindBinaryPattern' matches what we compute here
                    // with all recursive patterns in place. So, if the algorithm changes there, we might need to
                    // update it here as well. However, we are trying to share the same helpers in both places as mach
                    // as possible.

                    // Compute the common type. This algorithm is quadratic, but disjunctive patterns are unlikely to be huge
                    Binder.CollectDisjunctionTypes(right, narrowedTypeCandidates, hasUnionMatching: false);
                    var discardedSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                    TypeSymbol? leastSpecific = Binder.LeastSpecificType(narrowedTypeCandidates, rewriter._compilation.Conversions, ref discardedSiteInfo);
                    Debug.Assert(node.NarrowedType.Equals(leastSpecific ?? node.InputType, TypeCompareKind.ConsiderEverything));
#endif

                    return node.Update(disjunction: true, preboundLeft, right, inputType: node.InputType, narrowedType: node.NarrowedType);

                }
                else
                {
                    var right = (BoundPattern)rewriter.Visit(node.Right);

                    BoundPattern result = makeConjunction(node.Syntax, preboundLeft, right, makeCompilerGenerated: node.WasCompilerGenerated);

#if DEBUG
                    narrowedTypeCandidates.Clear();
                    narrowedTypeCandidates.Add(result is BoundPatternWithUnionMatching unionResult ? getDisjunctionType(unionResult) : result.NarrowedType);
#endif

                    return result;
                }

                // If left and right are not BoundPatternWithUnionMatching, simply produce a regular BoundBinaryPattern representing the conjunction.
                // Otherwise, create a BoundPatternWithUnionMatching representing the conjunction with the following properties:
                //  - There are no BoundPatternWithUnionMatching nodes under any ValuePattern.
                //  - The last union matching in evaluation order is always at the top
                //  - A preceding BoundPatternWithUnionMatching in evaluation order, if any, is the BoundPatternWithUnionMatching.LeftOfPendingConjunction.
                static BoundPattern makeConjunction(SyntaxNode node, BoundPattern left, BoundPattern? right, bool makeCompilerGenerated)
                {
                    if (right is BoundPatternWithUnionMatching rightUnionPattern)
                    {
                        // Update LeftOfPendingConjunction with the conjunction of left and LeftOfPendingConjunction

                        // The code below unwraps the following recursive operation:
                        //      return new BoundPatternWithUnionMatching(
                        //          syntax: node,
                        //          rightUnionPattern.UnionType,
                        //          makeConjunction(node, left, rightUnionPattern.LeftOfPendingConjunction, makeCompilerGenerated: true),
                        //          rightUnionPattern.ValueProperty,
                        //          rightUnionPattern.ValuePattern,
                        //          inputType: left.InputType).MakeCompilerGenerated();

                        var stack = ArrayBuilder<BoundPatternWithUnionMatching>.GetInstance();

                        stack.Push(rightUnionPattern);

                        while (rightUnionPattern.LeftOfPendingConjunction is BoundPatternWithUnionMatching other)
                        {
                            stack.Push(other);
                            rightUnionPattern = other;
                        }

                        Debug.Assert(rightUnionPattern.LeftOfPendingConjunction is not BoundPatternWithUnionMatching);
                        var leftOfPendingConjunction = makeConjunction(node, left, rightUnionPattern.LeftOfPendingConjunction, makeCompilerGenerated: true);

                        do
                        {
                            rightUnionPattern = stack.Pop();
                            leftOfPendingConjunction = new BoundPatternWithUnionMatching(
                                syntax: node,
                                rightUnionPattern.UnionType,
                                leftOfPendingConjunction,
                                rightUnionPattern.ValueProperty,
                                rightUnionPattern.ValuePattern,
                                inputType: left.InputType).MakeCompilerGenerated();
                        }
                        while (!stack.IsEmpty);

                        stack.Free();

                        return leftOfPendingConjunction;
                    }
                    else if (right is { })
                    {
                        if (left is BoundPatternWithUnionMatching leftUnionPattern)
                        {
                            // The right is just a continuation of the ValuePattern.
                            // Update ValuePattern with the conjunction of ValuePattern and right,
                            // since neither of them contain union patterns, we can simply create a BoundBinaryPattern for that.
                            return new BoundPatternWithUnionMatching(
                                syntax: node,
                                leftUnionPattern.UnionType,
                                leftUnionPattern.LeftOfPendingConjunction,
                                leftUnionPattern.ValueProperty,
                                MakeBinaryAnd(node, leftUnionPattern.ValuePattern, right, makeCompilerGenerated),
                                inputType: leftUnionPattern.InputType).MakeCompilerGenerated();
                        }
                        else
                        {
                            // Neither left nor right contain union patterns, create a BoundBinaryPattern for that.
                            return MakeBinaryAnd(node, left, right, makeCompilerGenerated);
                        }
                    }
                    else
                    {
                        return left;
                    }
                }
            }

#if DEBUG
            static TypeSymbol getDisjunctionType(BoundPatternWithUnionMatching unionPattern)
            {
                // Disjunction type is the UnionType for the first BoundPatternWithUnionMatching in evaluation order.
                // That type won't be narrowed more for the purposes of a possible upcoming disjunction, since
                // everything after that goes into a subputtern of a recursive pattern.
                while (unionPattern.LeftOfPendingConjunction is BoundPatternWithUnionMatching leftUnionPattern)
                {
                    unionPattern = leftUnionPattern;
                }

                return unionPattern.UnionType;
            }
#endif
        }

        private static BoundBinaryPattern MakeBinaryAnd(SyntaxNode node, BoundPattern left, BoundPattern right, bool makeCompilerGenerated)
        {
            return new BoundBinaryPattern(node, disjunction: false, left, right, inputType: left.InputType, narrowedType: right.NarrowedType) { WasCompilerGenerated = makeCompilerGenerated };
        }

        private static BoundPattern RewritePatternWithUnionMatchingToPropertyPattern(BoundPattern pattern)
        {
            // If pattern contains BoundPatternWithUnionMatching pending a rewrite, we should have BoundPatternWithUnionMatching
            // at the top.
            if (pattern is BoundPatternWithUnionMatching unionPattern)
            {
                // If this method is called, we are sure that no more conjunctions will follow this pattern immediately.
                // Therefore, no additional patterns are coming for the top most Value property. We can start rewriting
                // BoundPatternWithUnionMatching from the top down, converting them to appropriate BoundRecursivePatterns and nesting
                // them as we go down the chain. Effectively, we will end up with a chain of BoundRecursivePatterns in
                // reversed order, i.e. BoundRecursivePatterns corresponding to the top-most BoundPatternWithUnionMatching will be
                // at the bottom, and BoundRecursivePatterns corresponding to the bottom-most BoundPatternWithUnionMatching will be
                // at the top.

                TypeSymbol unionType = unionPattern.UnionType;
                BoundPropertySubpatternMember valueProperty = unionPattern.ValueProperty;
                BoundPattern? leftOfPendingConjunction = unionPattern.LeftOfPendingConjunction;
                BoundPattern valuePattern = unionPattern.ValuePattern;

                while (true)
                {
                    BoundPattern result = new BoundRecursivePattern(
                        syntax: valuePattern.Syntax,
                        declaredType: null,
                        deconstructMethod: null,
                        deconstruction: default,
                        properties: [new BoundPropertySubpattern(valuePattern.Syntax, valueProperty, isLengthOrCount: false, valuePattern).MakeCompilerGenerated()],
                        variable: null,
                        variableAccess: null,
                        isExplicitNotNullTest: false,
                        isUnionMatching: false,
                        inputType: unionType,
                        narrowedType: unionType).MakeCompilerGenerated();

                    if (leftOfPendingConjunction is BoundPatternWithUnionMatching leftUnionPattern)
                    {
                        unionType = leftUnionPattern.UnionType;
                        valueProperty = leftUnionPattern.ValueProperty;
                        leftOfPendingConjunction = leftUnionPattern.LeftOfPendingConjunction;
                        valuePattern = MakeBinaryAnd(pattern.Syntax, leftUnionPattern.ValuePattern, result, makeCompilerGenerated: true);

                        continue;
                    }
                    else if (leftOfPendingConjunction is { } left)
                    {
                        result = MakeBinaryAnd(pattern.Syntax, left, result, makeCompilerGenerated: true);
                    }

                    Debug.Assert(result.InputType.Equals(pattern.InputType, TypeCompareKind.AllIgnoreOptions));
                    return result;
                }
            }

            return pattern;
        }
    }
}
