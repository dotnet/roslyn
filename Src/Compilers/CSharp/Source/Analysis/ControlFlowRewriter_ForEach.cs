// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Lowers foreach-loops to simpler loops enclosed in try-finally blocks (for disposal).
    /// </summary>
    /// <remarks>
    /// This is not a separate rewrite pass.  It simply encapsulates ControlFlowRewriter functionality
    /// that is specific to foreach-loops.
    /// </remarks>
    internal partial class ControlFlowRewriter
    {
        /// <summary>
        /// This is the entry point for foreach-loop lowering.  It delegates to
        ///   RewriteEnumeratorForEachStatement
        ///   RewriteSingleDimensionalArrayForEachStatement
        ///   RewriteMultiDimensionalArrayForEachStatement
        ///   RewriteStringForEachStatement
        /// </summary>
        /// <remarks>
        /// NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE
        /// We are diverging from the C# 4 spec (and Dev10) to follow the C# 5 spec.
        /// The iteration variable will be declared *inside* each loop iteration,
        /// rather than outside the loop.
        /// NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE
        /// </remarks>
        public override BoundNode VisitForEachStatement(BoundForEachStatement node)
        {
            // No point in performing this lowering if the node won't be emitted.
            if (node.HasErrors)
            {
                return node;
            }
            else
            {
                BoundExpression collectionExpression = GetUnconvertedCollectionExpression(node);
                TypeSymbol nodeExpressionType = collectionExpression.Type;
                if (nodeExpressionType.Kind == SymbolKind.ArrayType)
                {
                    ArrayTypeSymbol arrayType = (ArrayTypeSymbol)nodeExpressionType;
                    if (arrayType.Rank == 1)
                    {
                        return RewriteSingleDimensionalArrayForEachStatement(node);
                    }
                    else
                    {
                        return node;
                        //TODO: return RewriteMultiDimensionalArrayForEachStatement(node);
                    }
                }
                else if(nodeExpressionType.SpecialType == SpecialType.System_String)
                {
                    return RewriteStringForEachStatement(node);
                }
                else
                {
                    return RewriteEnumeratorForEachStatement(node);
                }
            }
        }

        /// <summary>
        /// Lower a foreach loop that will enumerate a collection using an enumerator.
        /// 
        /// E e = ((C)(x)).GetEnumerator()
        /// try {
        ///     while (e.MoveNext()) {
        ///         V v = (V)(T)e.Current;
        ///         // body
        ///     }
        /// }
        /// finally {
        ///     // clean up e
        /// }
        /// </summary>
        private BoundStatement RewriteEnumeratorForEachStatement(BoundForEachStatement node)
        {
            ForEachStatementSyntax forEachSyntax = (ForEachStatementSyntax)node.Syntax;

            ForEachEnumeratorInfo enumeratorInfo = node.EnumeratorInfoOpt;
            Debug.Assert(enumeratorInfo != null);

            BoundExpression collectionExpression = GetUnconvertedCollectionExpression(node);
            BoundExpression rewrittenExpression = (BoundExpression)Visit(collectionExpression);
            BoundStatement rewrittenBody = (BoundStatement)Visit(node.Body);

            TypeSymbol enumeratorType = enumeratorInfo.GetEnumeratorMethod.ReturnType;
            TypeSymbol elementType = enumeratorInfo.ElementType;

            // E e
            LocalSymbol enumeratorVar = new TempLocalSymbol(enumeratorType, RefKind.None, this.containingMethod);

            // Reference to e.
            BoundLocal boundEnumeratorVar = MakeBoundLocal(forEachSyntax, enumeratorVar, enumeratorType);

            // ((C)(x)).GetEnumerator() or (x).GetEnumerator();
            BoundExpression enumeratorVarInitValue = SynthesizeCall(forEachSyntax, rewrittenExpression, enumeratorInfo.GetEnumeratorMethod, enumeratorInfo.CollectionConversion, enumeratorInfo.CollectionType);

            // E e = ((C)(x)).GetEnumerator();
            BoundStatement enumeratorVarDecl = MakeLocalDeclaration(forEachSyntax, enumeratorVar, enumeratorVarInitValue);

            AddForEachExpressionSequencePoint(forEachSyntax, ref enumeratorVarDecl);

            // V v
            LocalSymbol iterationVar = node.IterationVariable;

            //(V)(T)e.Current
            BoundExpression iterationVarAssignValue = SynthesizeConversion(
                syntax: forEachSyntax,
                operand: SynthesizeConversion(
                    syntax: forEachSyntax,
                    operand: BoundCall.Synthesized(
                        syntax: forEachSyntax,
                        receiverOpt: boundEnumeratorVar,
                        method: enumeratorInfo.CurrentPropertyGetter),
                    conversion: enumeratorInfo.CurrentConversion,
                    type: elementType),
                conversion: node.ElementConversion,
                type: iterationVar.Type);

            // V v = (V)(T)e.Current;
            BoundStatement iterationVarDecl = MakeLocalDeclaration(forEachSyntax, iterationVar, iterationVarAssignValue);

            AddForEachIterationVariableSequencePoint(forEachSyntax, ref iterationVarDecl);

            // while (e.MoveNext()) {
            //     V v = (V)(T)e.Current;
            //     /* node.Body */
            // }
            BoundStatement whileLoop = RewriteWhileStatement(
                syntax: forEachSyntax,
                rewrittenCondition: BoundCall.Synthesized(
                    syntax: forEachSyntax,
                    receiverOpt: boundEnumeratorVar,
                    method: enumeratorInfo.MoveNextMethod),
                conditionSequencePointSpan: forEachSyntax.InKeyword.Span,
                rewrittenBody: new BoundBlock(rewrittenBody.Syntax,
                    statements: ReadOnlyArray<BoundStatement>.CreateFrom(iterationVarDecl, rewrittenBody),
                    localsOpt: ReadOnlyArray<LocalSymbol>.CreateFrom(iterationVar)),
                breakLabel: node.BreakLabel,
                continueLabel: node.ContinueLabel,
                hasErrors: false);

            BoundStatement result;

            if (enumeratorInfo.DisposeMethodOpt != null)
            {
                BoundBlock finallyBlockOpt;
                var idisposableTypeSymbol = enumeratorInfo.DisposeMethodOpt.ContainingType;
                var conversions = new TypeConversions(this.containingMethod.ContainingAssembly.CorLibrary);

                if (conversions.ClassifyImplicitConversion(enumeratorType, idisposableTypeSymbol).IsImplicit)
                {
                    Debug.Assert(enumeratorInfo.DisposeMethodOpt != null);

                    Conversion receiverConversion = enumeratorType.IsStructType() ?
                        Conversion.Boxing :
                        Conversion.ImplicitReference;

                    // ((IDisposable)e).Dispose(); or e.Dispose();
                    BoundStatement disposeCall = new BoundExpressionStatement(forEachSyntax,
                        expression: SynthesizeCall(forEachSyntax, boundEnumeratorVar, enumeratorInfo.DisposeMethodOpt, receiverConversion, idisposableTypeSymbol));

                    BoundStatement disposeStmt;
                    if (enumeratorType.IsValueType)
                    {
                        // No way for the struct to be nullable and disposable.
                        Debug.Assert(((TypeSymbol)enumeratorType.OriginalDefinition).SpecialType != SpecialType.System_Nullable_T);

                        // For non-nullable structs, no null check is required.
                        disposeStmt = disposeCall;
                    }
                    else
                    {
                        // NB: cast to object missing from spec.  Needed to ignore user-defined operators and box type parameters.
                        // if ((object)e != null) ((IDisposable)e).Dispose(); 
                        disposeStmt = RewriteIfStatement(
                            syntax: forEachSyntax,
                            rewrittenCondition: new BoundBinaryOperator(forEachSyntax,
                                operatorKind: BinaryOperatorKind.NotEqual,
                                left: SynthesizeConversion(
                                    syntax: forEachSyntax,
                                    operand: boundEnumeratorVar,
                                    conversion: enumeratorInfo.EnumeratorConversion,
                                    type: this.compilation.GetSpecialType(SpecialType.System_Object)),
                                right: new BoundLiteral(forEachSyntax,
                                    constantValueOpt: ConstantValue.Null,
                                    type: null),
                                constantValueOpt: null,
                                methodOpt: null,
                                resultKind: LookupResultKind.Viable,
                                type: this.compilation.GetSpecialType(SpecialType.System_Boolean)),
                            rewrittenConsequence: disposeCall,
                            rewrittenAlternativeOpt: null,
                            hasErrors: false);
                    }

                    finallyBlockOpt = new BoundBlock(forEachSyntax,
                        localsOpt: ReadOnlyArray<LocalSymbol>.Null,
                        statements: ReadOnlyArray<BoundStatement>.CreateFrom(disposeStmt));
                }
                else
                {
                    Debug.Assert(!enumeratorType.IsSealed);

                    // IDisposable d
                    LocalSymbol disposableVar = new TempLocalSymbol(idisposableTypeSymbol, RefKind.None, this.containingMethod);

                    // Reference to d.
                    BoundLocal boundDisposableVar = MakeBoundLocal(forEachSyntax, disposableVar, idisposableTypeSymbol);

                    BoundTypeExpression boundIDisposableTypeExpr = new BoundTypeExpression(forEachSyntax,
                        type: idisposableTypeSymbol);

                    // e as IDisposable
                    BoundExpression disposableVarInitValue = new BoundAsOperator(forEachSyntax,
                        operand: boundEnumeratorVar,
                        targetType: boundIDisposableTypeExpr,
                        conversion: Conversion.ExplicitReference, // Explicit so the emitter won't optimize it away.
                        type: idisposableTypeSymbol);

                    // IDisposable d = e as IDisposable;
                    BoundStatement disposableVarDecl = MakeLocalDeclaration(forEachSyntax, disposableVar, disposableVarInitValue);

                    // if (d != null) d.Dispose();
                    BoundStatement ifStmt = RewriteIfStatement(
                        syntax: forEachSyntax,
                        rewrittenCondition: new BoundBinaryOperator(forEachSyntax,
                            operatorKind: BinaryOperatorKind.NotEqual, // reference equality
                            left: boundDisposableVar,
                            right: new BoundLiteral(forEachSyntax,
                                constantValueOpt: ConstantValue.Null,
                                type: null),
                            constantValueOpt: null,
                            methodOpt: null,
                            resultKind: LookupResultKind.Viable,
                            type: this.compilation.GetSpecialType(SpecialType.System_Boolean)),
                        rewrittenConsequence: new BoundExpressionStatement(forEachSyntax,
                            expression: BoundCall.Synthesized(
                                syntax: forEachSyntax,
                                receiverOpt: boundDisposableVar,
                                method: enumeratorInfo.DisposeMethodOpt)),
                        rewrittenAlternativeOpt: null,
                        hasErrors: false);

                    // IDisposable d = e as IDisposable;
                    // if (d != null) d.Dispose();
                    finallyBlockOpt = new BoundBlock(forEachSyntax,
                        localsOpt: ReadOnlyArray<LocalSymbol>.CreateFrom(disposableVar),
                        statements: ReadOnlyArray<BoundStatement>.CreateFrom(disposableVarDecl, ifStmt));
                }

                // try {
                //     while (e.MoveNext()) {
                //         V v = (V)(T)e.Current;
                //         /* loop body */
                //     }
                // }
                // finally {
                //     /* dispose of e */
                // }
                BoundStatement tryFinally = new BoundTryStatement(forEachSyntax,
                    tryBlock: new BoundBlock(forEachSyntax,
                        localsOpt: ReadOnlyArray<LocalSymbol>.Empty,
                        statements: ReadOnlyArray<BoundStatement>.CreateFrom(whileLoop)),
                    catchBlocks: ReadOnlyArray<BoundCatchBlock>.Empty,
                    finallyBlockOpt: finallyBlockOpt);

                // E e = ((C)(x)).GetEnumerator();
                // try {
                //     /* as above */
                result = new BoundBlock(
                    syntax: forEachSyntax,
                    localsOpt: ReadOnlyArray<LocalSymbol>.CreateFrom(enumeratorVar),
                    statements: ReadOnlyArray<BoundStatement>.CreateFrom(enumeratorVarDecl, tryFinally));
            }
            else
            {
                // E e = ((C)(x)).GetEnumerator();
                // while (e.MoveNext()) {
                //     V v = (V)(T)e.Current;
                //     /* loop body */
                // }
                result = new BoundBlock(
                    syntax: forEachSyntax,
                    localsOpt: ReadOnlyArray<LocalSymbol>.CreateFrom(enumeratorVar),
                    statements: ReadOnlyArray<BoundStatement>.CreateFrom(enumeratorVarDecl, whileLoop));
            }

            AddForEachKeywordSequencePoint(forEachSyntax, ref result);

            return result;
        }

        /// <summary>
        /// Synthesize a no-argument call to a given method, possibly applying a conversion to the receiver.
        /// 
        /// If the receiver is of struct type and the method is an interface method, then skip the conversion
        /// and just call the interface method directly - the code generator will detect this and generate a 
        /// constrained virtual call.
        /// </summary>
        /// <param name="syntax">A syntax node to attach to the synthesized bound node.</param>
        /// <param name="receiver">Receiver of method call.</param>
        /// <param name="method">Method to invoke.</param>
        /// <param name="receiverConversion">Conversion to be applied to the receiver if not calling an interface method on a struct.</param>
        /// <param name="convertedReceiverType">Type of the receiver after applying the conversion.</param>
        /// <returns>A BoundExpression representing the call.</returns>
        private static BoundExpression SynthesizeCall(SyntaxNode syntax, BoundExpression receiver, MethodSymbol method, Conversion receiverConversion, TypeSymbol convertedReceiverType)
        {
            if (receiver.Type.TypeKind == TypeKind.Struct && method.ContainingType.TypeKind == TypeKind.Interface)
            {
                Debug.Assert(receiverConversion.IsBoxing);

                // NOTE: The spec says that disposing of a struct enumerator won't cause any 
                // unnecessary boxing to occur.  However, Dev10 extends this improvement to the
                // GetEnumerator call as well.

                // We're going to let the emitter take care of avoiding the extra boxing. 
                // When it sees an interface call to a struct, it will generate a constrained
                // virtual call, which will skip boxing, if possible.

                // CONSIDER: In cases where the struct implicitly implements the interface method
                // (i.e. with a public method), we could save a few bytes of IL by creating a 
                // BoundCall to the struct method rather than the interface method (so that the
                // emitter wouldn't need to create a constrained virtual call).  It is not clear 
                // what effect this would have on back compat.

                // NOTE: This call does not correspond to anything that can be written in C# source.
                // We're invoking the interface method directly on the struct (which may have a private
                // explicit implementation).  The code generator knows how to handle it though.

                // receiver.InterfaceMethod()
                return BoundCall.Synthesized(syntax, receiver, method);
            }
            else
            {
                // ((Interface)receiver).InterfaceMethod()
                return BoundCall.Synthesized(
                    syntax: syntax,
                    receiverOpt: SynthesizeConversion(
                        syntax: syntax,
                        operand: receiver,
                        conversion: receiverConversion,
                        type: convertedReceiverType),
                    method: method);
            }
        }

        /// <summary>
        /// Helper method that synthesizes a BoundConversion, unless the conversion would be an identity conversion.
        /// </summary>
        private static BoundExpression SynthesizeConversion(SyntaxNode syntax, BoundExpression operand, Conversion conversion, TypeSymbol type)
        {
            if (type == operand.Type)
            {
                Debug.Assert(conversion.Kind == ConversionKind.Identity);
                return operand;
            }
            else
            {
                // TODO: when the ControlFlowRewriter is merged into the LocalRewriter, switch to LocalRewriter.MakeConversion.
                return BoundConversion.Synthesized(syntax, operand, conversion, false, false, ConstantValue.NotAvailable, type);
            }
        }

        /// <summary>
        /// Lower a foreach loop that will enumerate the characters of a string.
        /// 
        /// string s = x;
        /// for (int p = 0; p &lt; s.Length; p = p + 1) {
        ///     V v = (V)s.Chars[p];
        ///     // body
        /// }
        /// </summary>
        /// <remarks>
        /// We will follow Dev10 in diverging from the C# 4 spec by ignoring string's 
        /// implementation of IEnumerable and just indexing into its characters.
        /// 
        /// NOTE: We're assuming that sequence points have already been generated.
        /// Otherwise, lowering to for-loops would generated spurious ones.
        /// </remarks>
        private BoundStatement RewriteStringForEachStatement(BoundForEachStatement node)
        {
            ForEachStatementSyntax forEachSyntax = (ForEachStatementSyntax)node.Syntax;

            BoundExpression collectionExpression = GetUnconvertedCollectionExpression(node);
            TypeSymbol stringType = collectionExpression.Type;
            Debug.Assert(stringType.SpecialType == SpecialType.System_String);

            TypeSymbol intType = compilation.GetSpecialType(SpecialType.System_Int32);
            TypeSymbol boolType = compilation.GetSpecialType(SpecialType.System_Boolean);

            BoundExpression rewrittenExpression = (BoundExpression)Visit(collectionExpression);
            BoundStatement rewrittenBody = (BoundStatement)Visit(node.Body);

            // string s;
            LocalSymbol stringVar = new TempLocalSymbol(stringType, RefKind.None, containingMethod);
            // int p;
            LocalSymbol positionVar = new TempLocalSymbol(intType, RefKind.None, containingMethod);

            // Reference to s.
            BoundLocal boundStringVar = MakeBoundLocal(forEachSyntax, stringVar, stringType);

            // Reference to p.
            BoundLocal boundPositionVar = MakeBoundLocal(forEachSyntax, positionVar, intType);

            // string s = /*expr*/;
            BoundStatement stringVarDecl = MakeLocalDeclaration(forEachSyntax, stringVar, rewrittenExpression);

            AddForEachExpressionSequencePoint(forEachSyntax, ref stringVarDecl);

            // int p = 0;
            BoundStatement positionVariableDecl = MakeLocalDeclaration(forEachSyntax, positionVar, 
                new BoundLiteral(forEachSyntax, ConstantValue.ConstantValueZero.Int32, intType));

            // string s = /*node.Expression*/; int p = 0;
            BoundStatement initializer = new BoundStatementList(forEachSyntax, 
                statements: ReadOnlyArray<BoundStatement>.CreateFrom(stringVarDecl, positionVariableDecl));

            BoundExpression stringLength = BoundCall.Synthesized(
                    syntax: forEachSyntax,
                    receiverOpt: boundStringVar,
                    method: (MethodSymbol)compilation.GetSpecialTypeMember(SpecialMember.System_String__Length),
                    arguments: ReadOnlyArray<BoundExpression>.Empty);

            // p < s.Length
            BoundExpression exitCondition = new BoundBinaryOperator(
                syntax: forEachSyntax, 
                operatorKind: BinaryOperatorKind.IntLessThan,
                left: boundPositionVar,
                right: stringLength,
                constantValueOpt: null, 
                methodOpt: null, 
                resultKind: LookupResultKind.Viable, 
                type: boolType);

            // p = p + 1;
            BoundStatement positionIncrement = MakePositionIncrement(forEachSyntax, boundPositionVar, intType);

            LocalSymbol iterationVar = node.IterationVariable;
            TypeSymbol iterationVarType = iterationVar.Type;
            Debug.Assert(node.ElementConversion.Exists);

            // (V)s.Chars[p]
            BoundExpression iterationVarInitValue = SynthesizeConversion(
                syntax: forEachSyntax,
                operand: BoundCall.Synthesized(
                    syntax: forEachSyntax,
                    receiverOpt: boundStringVar,
                    method: (MethodSymbol)this.compilation.GetSpecialTypeMember(SpecialMember.System_String__Chars),
                    arguments: ReadOnlyArray<BoundExpression>.CreateFrom(boundPositionVar)),
                conversion: node.ElementConversion,
                type: iterationVarType);

            // V v = (V)s.Chars[p];
            BoundStatement iterationVarDecl = MakeLocalDeclaration(forEachSyntax, iterationVar, iterationVarInitValue);

            AddForEachIterationVariableSequencePoint(forEachSyntax, ref iterationVarDecl);

            // { V v = (V)s.Chars[p]; /*node.Body*/ }
            BoundStatement loopBody = new BoundBlock(forEachSyntax,
                localsOpt: ReadOnlyArray<LocalSymbol>.CreateFrom(iterationVar),
                statements: ReadOnlyArray<BoundStatement>.CreateFrom(iterationVarDecl, rewrittenBody));

            // for (string s = /*node.Expression*/, int p = 0; p < s.Length; p = p + 1) {
            //     V v = (V)s.Chars[p];
            //     /*node.Body*/
            // }
            BoundStatement result = RewriteForStatement(
                syntax: forEachSyntax,
                locals: ReadOnlyArray<LocalSymbol>.CreateFrom(stringVar, positionVar),
                rewrittenInitializer: initializer,
                rewrittenCondition: exitCondition,
                conditionSyntax: forEachSyntax.InKeyword,
                rewrittenIncrement: positionIncrement,
                rewrittenBody: loopBody,
                breakLabel: node.BreakLabel,
                continueLabel: node.ContinueLabel, hasErrors: node.HasErrors);

            AddForEachKeywordSequencePoint(forEachSyntax, ref result);

            return result;
        }

        /// <summary>
        /// Lower a foreach loop that will enumerate a single-dimensional array.
        /// 
        /// A[] a = x;
        /// for (int p = 0; p &lt; a.Length; p = p + 1) {
        ///     V v = (V)a[p];
        ///     // body
        /// }
        /// </summary>
        /// <remarks>
        /// We will follow Dev10 in diverging from the C# 4 spec by ignoring Array's 
        /// implementation of IEnumerable and just indexing into its elements.
        /// 
        /// NOTE: We're assuming that sequence points have already been generated.
        /// Otherwise, lowering to for-loops would generated spurious ones.
        /// </remarks>
        private BoundStatement RewriteSingleDimensionalArrayForEachStatement(BoundForEachStatement node)
        {
            ForEachStatementSyntax forEachSyntax = (ForEachStatementSyntax)node.Syntax;

            BoundExpression collectionExpression = GetUnconvertedCollectionExpression(node);
            Debug.Assert(collectionExpression.Type.IsArray());

            ArrayTypeSymbol arrayType = (ArrayTypeSymbol)collectionExpression.Type;

            Debug.Assert(arrayType.Rank == 1);

            TypeSymbol intType = compilation.GetSpecialType(SpecialType.System_Int32);
            TypeSymbol boolType = compilation.GetSpecialType(SpecialType.System_Boolean);

            BoundExpression rewrittenExpression = (BoundExpression)Visit(collectionExpression);
            BoundStatement rewrittenBody = (BoundStatement)Visit(node.Body);

            // A[] a
            LocalSymbol arrayVar = new TempLocalSymbol(arrayType, RefKind.None, containingMethod);

            // A[] a = /*node.Expression*/;
            BoundStatement arrayVarDecl = MakeLocalDeclaration(forEachSyntax, arrayVar, rewrittenExpression);

            AddForEachExpressionSequencePoint(forEachSyntax, ref arrayVarDecl);

            // Reference to a.
            BoundLocal boundArrayVar = MakeBoundLocal(forEachSyntax, arrayVar, arrayType);

            // int p
            LocalSymbol positionVar = new TempLocalSymbol(intType, RefKind.None, containingMethod);

            // Reference to p.
            BoundLocal boundPositionVar = MakeBoundLocal(forEachSyntax, positionVar, intType);

            // int p = 0;
            BoundStatement positionVarDecl = MakeLocalDeclaration(forEachSyntax, positionVar, 
                new BoundLiteral(forEachSyntax, ConstantValue.ConstantValueZero.Int32, intType));

            // V v
            LocalSymbol iterationVar = node.IterationVariable;
            TypeSymbol iterationVarType = iterationVar.Type;

            // (V)a[p]
            BoundExpression iterationVarInitValue = SynthesizeConversion(
                syntax: forEachSyntax,
                operand: new BoundArrayAccess(
                    syntax: forEachSyntax,
                    expression: boundArrayVar,
                    indices: ReadOnlyArray<BoundExpression>.CreateFrom(boundPositionVar),
                    type: arrayType.ElementType),
                conversion: node.ElementConversion,
                type: iterationVarType);

            // V v = (V)a[p];
            BoundStatement iterationVariableDecl = MakeLocalDeclaration(forEachSyntax, iterationVar, iterationVarInitValue);

            AddForEachIterationVariableSequencePoint(forEachSyntax, ref iterationVariableDecl);

            BoundStatement initializer = new BoundStatementList(forEachSyntax, 
                        statements: ReadOnlyArray<BoundStatement>.CreateFrom(arrayVarDecl, positionVarDecl));

            // a.Length
            BoundExpression arrayLength = new BoundArrayLength(
                syntax: forEachSyntax,
                expression: boundArrayVar,
                type: intType);

            // p < a.Length
            BoundExpression exitCondition = new BoundBinaryOperator(
                syntax: forEachSyntax, 
                operatorKind: BinaryOperatorKind.IntLessThan,
                left: boundPositionVar,
                right: arrayLength,
                constantValueOpt: null, 
                methodOpt: null, 
                resultKind: LookupResultKind.Viable, 
                type: boolType);

            // p = p + 1;
            BoundStatement positionIncrement = MakePositionIncrement(forEachSyntax, boundPositionVar, intType);

            // { V v = (V)a[p]; /* node.Body */ }
            BoundStatement loopBody = new BoundBlock(forEachSyntax, 
                localsOpt: ReadOnlyArray<LocalSymbol>.CreateFrom(iterationVar),
                statements: ReadOnlyArray<BoundStatement>.CreateFrom(iterationVariableDecl, rewrittenBody));

            // for (A[] a = /*node.Expression*/, int p = 0; p < a.Length; p = p + 1) {
            //     V v = (V)a[p];
            //     /*node.Body*/
            // }
            BoundStatement result = RewriteForStatement(
                syntax: node.Syntax,
                locals: ReadOnlyArray<LocalSymbol>.CreateFrom(arrayVar, positionVar),
                rewrittenInitializer: initializer,
                rewrittenCondition: exitCondition,
                conditionSyntax: forEachSyntax.InKeyword,
                rewrittenIncrement: positionIncrement,
                rewrittenBody: loopBody,
                breakLabel: node.BreakLabel,
                continueLabel: node.ContinueLabel, hasErrors: node.HasErrors);

            AddForEachKeywordSequencePoint(forEachSyntax, ref result);

            return result;
        }

        /// <summary>
        /// Lower a foreach loop that will enumerate a multi-dimensional array.
        /// 
        /// A[...] a = x;
        /// for (int p_0 = a.GetLowerBound(0); p_0 &lt;= a.GetUpperBound(0); p_0 = p_0 + 1)
        ///     for (int p_1 = a.GetLowerBound(1); p_1 &lt;= a.GetUpperBound(0); p_1 = p_1 + 1)
        ///         ...
        ///             { V v = (V)a[p_0, p_1, ...]; /* body */ }
        /// </summary>
        /// <remarks>
        /// We will follow Dev10 in diverging from the C# 4 spec by ignoring Array's 
        /// implementation of IEnumerable and just indexing into its elements.
        /// 
        /// NOTE: We're assuming that sequence points have already been generated.
        /// Otherwise, lowering to nested for-loops would generated spurious ones.
        /// </remarks>
        [System.Obsolete("This code is effectively unreachable until we implement multidimensional arrays.", true)]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private BoundStatement RewriteMultiDimensionalArrayForEachStatement(BoundForEachStatement node)
        {
            ForEachStatementSyntax forEachSyntax = (ForEachStatementSyntax)node.Syntax;

            BoundExpression collectionExpression = GetUnconvertedCollectionExpression(node);
            Debug.Assert(collectionExpression.Type.IsArray());

            ArrayTypeSymbol arrayType = (ArrayTypeSymbol)collectionExpression.Type;

            int rank = arrayType.Rank;
            Debug.Assert(rank > 1);

            TypeSymbol intType = compilation.GetSpecialType(SpecialType.System_Int32);
            TypeSymbol boolType = compilation.GetSpecialType(SpecialType.System_Boolean);

            BoundExpression rewrittenExpression = (BoundExpression)Visit(collectionExpression);
            BoundStatement rewrittenBody = (BoundStatement)Visit(node.Body);

            // A[...] a
            LocalSymbol arrayVar = new TempLocalSymbol(arrayType, RefKind.None, containingMethod);

            // A[...] a = /*node.Expression*/;
            BoundStatement arrayVarDecl = MakeLocalDeclaration(forEachSyntax, arrayVar, rewrittenExpression);

            AddForEachExpressionSequencePoint(forEachSyntax, ref arrayVarDecl);

            // Reference to a.
            BoundLocal boundArrayVar = MakeBoundLocal(forEachSyntax, arrayVar, arrayType);

            // int p_0, p_1, ...
            LocalSymbol[] positionVar = new LocalSymbol[rank];
            BoundLocal[] boundPositionVar = new BoundLocal[rank];
            for (int dimension = 0; dimension < rank; dimension++)
            {
                positionVar[dimension] = new TempLocalSymbol(intType, RefKind.None, containingMethod);
                boundPositionVar[dimension] = MakeBoundLocal(forEachSyntax, positionVar[dimension], intType);
            }

            // V v
            LocalSymbol iterationVar = node.IterationVariable;
            TypeSymbol iterationVarType = iterationVar.Type;

            // (V)a[p_0, p_1, ...]
            BoundExpression iterationVarInitValue = SynthesizeConversion(
                syntax: forEachSyntax,
                operand: new BoundArrayAccess(forEachSyntax, 
                    expression: boundArrayVar,
                    indices: ReadOnlyArray<BoundExpression>.CreateFrom((BoundExpression[])boundPositionVar),
                    type: arrayType.ElementType),
                conversion: node.ElementConversion,
                type: iterationVarType);

            // V v = (V)a[p_0, p_1, ...];
            BoundStatement iterationVarDecl = MakeLocalDeclaration(forEachSyntax, iterationVar, iterationVarInitValue);

            AddForEachIterationVariableSequencePoint(forEachSyntax, ref iterationVarDecl);

            // { V v = (V)a[p_0, p_1, ...]; /* node.Body */ }
            BoundStatement innermostLoopBody = new BoundBlock(forEachSyntax, 
                localsOpt: ReadOnlyArray<LocalSymbol>.CreateFrom(iterationVar),
                statements: ReadOnlyArray<BoundStatement>.CreateFrom(iterationVarDecl, rewrittenBody));

            // Values we'll use every iteration
            MethodSymbol getLowerBoundMethod = (MethodSymbol)this.compilation.GetSpecialTypeMember(SpecialMember.System_Array__GetLowerBound);
            MethodSymbol getUpperBoundMethod = (MethodSymbol)this.compilation.GetSpecialTypeMember(SpecialMember.System_Array__GetUpperBound);

            // work from most-nested to least-nested
            // for (A[...] a = /*node.Expression*/; int p_0 = a.GetLowerBound(0); p_0 <= a.GetUpperBound(0); p_0 = p_0 + 1)
            //     for (int p_1 = a.GetLowerBound(0); p_1 <= a.GetUpperBound(0); p_1 = p_1 + 1)
            //         ...
            //             { V v = (V)a[p_0, p_1, ...]; /* node.Body */ }
            BoundStatement forLoop = null;
            for (int dimension = rank - 1; dimension >= 0; dimension--)
            {
                ReadOnlyArray<BoundExpression> dimensionArgument = ReadOnlyArray<BoundExpression>.CreateFrom(
                    new BoundLiteral(forEachSyntax, 
                        constantValueOpt: ConstantValue.Create(dimension, ConstantValueTypeDiscriminator.Int32),
                        type: intType));

                // a.GetLowerBound(/*dimension*/)
                BoundExpression currentDimensionLowerBound = BoundCall.Synthesized(forEachSyntax, boundArrayVar, getLowerBoundMethod, dimensionArgument);
                // a.GetUpperBound(/*dimension*/) //CONSIDER: dev10 creates a temp for each dimension's upper bound
                BoundExpression currentDimensionUpperBound = BoundCall.Synthesized(forEachSyntax, boundArrayVar, getUpperBoundMethod, dimensionArgument);

                // int p_/*dimension*/ = a.GetLowerBound(/*dimension*/);
                BoundStatement positionVarDecl = MakeLocalDeclaration(forEachSyntax, positionVar[dimension], currentDimensionLowerBound);

                ReadOnlyArray<LocalSymbol> locals;
                BoundStatement initializer;
                GeneratedLabelSymbol breakLabel;

                if (dimension == 0)
                {
                    // outermost for-loop
                    locals = ReadOnlyArray<LocalSymbol>.CreateFrom(arrayVar, positionVar[dimension]);
                    initializer = new BoundStatementList(forEachSyntax, 
                        statements: ReadOnlyArray<BoundStatement>.CreateFrom(arrayVarDecl, positionVarDecl));
                    breakLabel = node.BreakLabel; // i.e. the one that break statements will jump to
                }
                else
                {
                    locals = ReadOnlyArray<LocalSymbol>.CreateFrom(positionVar[dimension]);
                    initializer = positionVarDecl;
                    breakLabel = new GeneratedLabelSymbol("break"); // Should not affect emitted code since unused
                }

                // p_/*dimension*/ <= a.GetUpperBound(/*dimension*/)  //NB: OrEqual
                BoundExpression exitCondition = new BoundBinaryOperator(
                    syntax: forEachSyntax, 
                    operatorKind: BinaryOperatorKind.IntLessThanOrEqual,
                    left: boundPositionVar[dimension],
                    right: currentDimensionUpperBound,
                    constantValueOpt: null, 
                    methodOpt: null, 
                    resultKind: LookupResultKind.Viable, 
                    type: boolType);

                // p_/*dimension*/ = p_/*dimension*/ + 1;
                BoundStatement positionIncrement = MakePositionIncrement(forEachSyntax, boundPositionVar[dimension], intType);

                BoundStatement body;
                GeneratedLabelSymbol continueLabel;

                if(forLoop == null)
                {
                    // innermost for-loop
                    body = innermostLoopBody;
                    continueLabel = node.ContinueLabel; //i.e. the one continue statements will actually jump to
                }
                else
                {
                    body = forLoop;
                    continueLabel = new GeneratedLabelSymbol("continue"); // Should not affect emitted code since unused
                }

                forLoop = RewriteForStatement(
                    node.Syntax,
                    locals,
                    initializer,
                    exitCondition,
                    forEachSyntax.InKeyword,
                    positionIncrement,
                    body,
                    breakLabel,
                    continueLabel,
                    node.HasErrors);
            }

            Debug.Assert(forLoop != null);

            AddForEachExpressionSequencePoint(forEachSyntax, ref forLoop);

            return forLoop;
        }

        /// <summary>
        /// So that the binding info can return an appropriate SemanticInfo.Converted type for the collection
        /// expression of a foreach node, it is wrapped in a BoundConversion to the collection type in the
        /// initial bound tree.  However, we may be able to optimize away (or entirely disregard) the conversion
        /// so we pull out the bound node for the underlying expression.
        /// </summary>
        private static BoundExpression GetUnconvertedCollectionExpression(BoundForEachStatement node)
        {
            var boundExpression = node.Expression;
            if (boundExpression.Kind == BoundKind.Conversion)
            {
                return ((BoundConversion)boundExpression).Operand;
            }

            // Conversion was an identity conversion and the LocalRewriter must have optimized away the
            // BoundConversion node, we can return the expression itself.
            return boundExpression;
        }

        private static BoundLocal MakeBoundLocal(SyntaxNode syntax, LocalSymbol local, TypeSymbol type)
        {
            return new BoundLocal(syntax,
                localSymbol: local,
                constantValueOpt: null,
                type: type);
        }

        private static BoundStatement MakeLocalDeclaration(SyntaxNode syntax, LocalSymbol local, BoundExpression rewrittenInitialValue)
        {
            return RewriteLocalDeclaration(
                syntax: syntax,
                localSymbol: local,
                rewrittenInitializer: rewrittenInitialValue,
                hasErrors: false);
        }

        // Used to increment integer index into an array or string.
        private static BoundExpressionStatement MakePositionIncrement(SyntaxNode syntax, BoundLocal boundPositionVar, TypeSymbol intType)
        {
            return new BoundExpressionStatement(syntax,
                expression: new BoundAssignmentOperator(syntax, 
                    left: boundPositionVar,
                    right: new BoundBinaryOperator(syntax,
                        operatorKind: BinaryOperatorKind.IntAddition, // unchecked, never overflows since array/string index can't be >= Int32.MaxValue
                        left: boundPositionVar,
                        right: new BoundLiteral(syntax, 
                            constantValueOpt: ConstantValue.ConstantValueOne.Int32,
                            type: intType),
                        constantValueOpt: null, 
                        methodOpt: null, 
                        resultKind: LookupResultKind.Viable, 
                        type: intType),
                    type: intType));
        }

        /// <summary>
        /// Add sequence point |here|:
        /// 
        /// foreach (Type var in |expr|) { }
        /// </summary>
        /// <remarks>
        /// Hit once, before looping begins.
        /// </remarks>
        private void AddForEachExpressionSequencePoint(ForEachStatementSyntax forEachSyntax, ref BoundStatement collectionVarDecl)
        {
            if (this.generateDebugInfo)
            {
                // NOTE: This is slightly different from Dev10.  In Dev10, when you stop the debugger
                // on the collection expression, you can see the (uninitialized) iteration variable.
                // In Roslyn, you cannot because the iteration variable is re-declared in each iteration
                // of the loop and is, therefore, not yet in scope.
                collectionVarDecl = new BoundSequencePoint(forEachSyntax.Expression, collectionVarDecl);
            }
        }

        /// <summary>
        /// Add sequence point |here|:
        /// 
        /// foreach (|Type var| in expr) { }
        /// </summary>
        /// <remarks>
        /// Hit every iteration.
        /// </remarks>
        private void AddForEachIterationVariableSequencePoint(ForEachStatementSyntax forEachSyntax, ref BoundStatement iterationVarDecl)
        {
            if (this.generateDebugInfo)
            {
                TextSpan iterationVarDeclSpan = TextSpan.FromBounds(forEachSyntax.Type.Span.Start, forEachSyntax.Identifier.Span.End);
                iterationVarDecl = new BoundSequencePointWithSpan(forEachSyntax, iterationVarDecl, iterationVarDeclSpan);
            }
        }

        /// <summary>
        /// Add sequence point |here|:
        /// 
        /// |foreach| (Type var in expr) { }
        /// </summary>
        /// <remarks>
        /// Hit once, before looping begins.
        /// </remarks>
        private void AddForEachKeywordSequencePoint(ForEachStatementSyntax forEachSyntax, ref BoundStatement result)
        {
            if (this.generateDebugInfo)
            {
                BoundSequencePointWithSpan foreachKeywordSequencePoint = new BoundSequencePointWithSpan(forEachSyntax, null, forEachSyntax.ForEachKeyword.Span);
                result = new BoundStatementList(forEachSyntax, ReadOnlyArray<BoundStatement>.CreateFrom(foreachKeywordSequencePoint, result));
            }
        }
    }
}
