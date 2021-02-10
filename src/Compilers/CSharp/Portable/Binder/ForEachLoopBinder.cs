// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A loop binder that (1) knows how to bind foreach loops and (2) has the foreach iteration variable in scope.
    /// </summary>
    /// <remarks>
    /// This binder produces BoundForEachStatements.  The lowering described in the spec is performed in ControlFlowRewriter.
    /// </remarks>
    internal sealed class ForEachLoopBinder : LoopBinder
    {
        private readonly CommonForEachStatementSyntax _syntax;
        private SourceLocalSymbol IterationVariable
        {
            get
            {
                return (_syntax.Kind() == SyntaxKind.ForEachStatement) ? (SourceLocalSymbol)this.Locals[0] : null;
            }
        }

        private bool IsAsync
            => _syntax.AwaitKeyword != default;

        public ForEachLoopBinder(Binder enclosing, CommonForEachStatementSyntax syntax)
            : base(enclosing)
        {
            Debug.Assert(syntax != null);
            _syntax = syntax;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            switch (_syntax.Kind())
            {
                case SyntaxKind.ForEachVariableStatement:
                    {
                        var syntax = (ForEachVariableStatementSyntax)_syntax;
                        var locals = ArrayBuilder<LocalSymbol>.GetInstance();
                        CollectLocalsFromDeconstruction(
                            syntax.Variable,
                            LocalDeclarationKind.ForEachIterationVariable,
                            locals,
                            syntax);
                        return locals.ToImmutableAndFree();
                    }
                case SyntaxKind.ForEachStatement:
                    {
                        var syntax = (ForEachStatementSyntax)_syntax;
                        var iterationVariable = SourceLocalSymbol.MakeForeachLocal(
                            (MethodSymbol)this.ContainingMemberOrLambda,
                            this,
                            syntax.Type,
                            syntax.Identifier,
                            syntax.Expression);
                        return ImmutableArray.Create<LocalSymbol>(iterationVariable);
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(_syntax.Kind());
            }
        }

        internal void CollectLocalsFromDeconstruction(
            ExpressionSyntax declaration,
            LocalDeclarationKind kind,
            ArrayBuilder<LocalSymbol> locals,
            SyntaxNode deconstructionStatement,
            Binder enclosingBinderOpt = null)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.TupleExpression:
                    {
                        var tuple = (TupleExpressionSyntax)declaration;
                        foreach (var arg in tuple.Arguments)
                        {
                            CollectLocalsFromDeconstruction(arg.Expression, kind, locals, deconstructionStatement, enclosingBinderOpt);
                        }
                        break;
                    }
                case SyntaxKind.DeclarationExpression:
                    {
                        var declarationExpression = (DeclarationExpressionSyntax)declaration;
                        CollectLocalsFromDeconstruction(
                            declarationExpression.Designation, declarationExpression.Type,
                            kind, locals, deconstructionStatement, enclosingBinderOpt);

                        break;
                    }
                case SyntaxKind.IdentifierName:
                    break;
                default:
                    // In broken code, we can have an arbitrary expression here. Collect its expression variables.
                    ExpressionVariableFinder.FindExpressionVariables(this, locals, declaration);
                    break;
            }
        }

        internal void CollectLocalsFromDeconstruction(
            VariableDesignationSyntax designation,
            TypeSyntax closestTypeSyntax,
            LocalDeclarationKind kind,
            ArrayBuilder<LocalSymbol> locals,
            SyntaxNode deconstructionStatement,
            Binder enclosingBinderOpt)
        {
            switch (designation.Kind())
            {
                case SyntaxKind.SingleVariableDesignation:
                    {
                        var single = (SingleVariableDesignationSyntax)designation;
                        SourceLocalSymbol localSymbol = SourceLocalSymbol.MakeDeconstructionLocal(
                                                                    this.ContainingMemberOrLambda,
                                                                    this,
                                                                    enclosingBinderOpt ?? this,
                                                                    closestTypeSyntax,
                                                                    single.Identifier,
                                                                    kind,
                                                                    deconstructionStatement);
                        locals.Add(localSymbol);
                        break;
                    }
                case SyntaxKind.ParenthesizedVariableDesignation:
                    {
                        var tuple = (ParenthesizedVariableDesignationSyntax)designation;
                        foreach (var d in tuple.Variables)
                        {
                            CollectLocalsFromDeconstruction(d, closestTypeSyntax, kind, locals, deconstructionStatement, enclosingBinderOpt);
                        }
                        break;
                    }
                case SyntaxKind.DiscardDesignation:
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(designation.Kind());
            }
        }

        /// <summary>
        /// Bind the ForEachStatementSyntax at the root of this binder.
        /// </summary>
        internal override BoundStatement BindForEachParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            BoundForEachStatement result = BindForEachPartsWorker(diagnostics, originalBinder);
            return result;
        }

        /// <summary>
        /// Like BindForEachParts, but only bind the deconstruction part of the foreach, for purpose of inferring the types of the declared locals.
        /// </summary>
        internal override BoundStatement BindForEachDeconstruction(DiagnosticBag diagnostics, Binder originalBinder)
        {
            // Use the right binder to avoid seeing iteration variable
            BoundExpression collectionExpr = originalBinder.GetBinder(_syntax.Expression).BindRValueWithoutTargetType(_syntax.Expression, diagnostics);

            var builder = new ForEachEnumeratorInfo.Builder();
            TypeWithAnnotations inferredType;
            bool hasErrors = !GetEnumeratorInfoAndInferCollectionElementType(ref builder, ref collectionExpr, diagnostics, out inferredType);

            ExpressionSyntax variables = ((ForEachVariableStatementSyntax)_syntax).Variable;

            // Tracking narrowest safe-to-escape scope by default, the proper val escape will be set when doing full binding of the foreach statement
            var valuePlaceholder = new BoundDeconstructValuePlaceholder(_syntax.Expression, this.LocalScopeDepth, inferredType.Type ?? CreateErrorType("var"));

            DeclarationExpressionSyntax declaration = null;
            ExpressionSyntax expression = null;
            BoundDeconstructionAssignmentOperator deconstruction = BindDeconstruction(
                                                        variables,
                                                        variables,
                                                        right: _syntax.Expression,
                                                        diagnostics: diagnostics,
                                                        rightPlaceholder: valuePlaceholder,
                                                        declaration: ref declaration,
                                                        expression: ref expression);

            return new BoundExpressionStatement(_syntax, deconstruction);
        }

        private BoundForEachStatement BindForEachPartsWorker(DiagnosticBag diagnostics, Binder originalBinder)
        {
            // Use the right binder to avoid seeing iteration variable
            BoundExpression collectionExpr = originalBinder.GetBinder(_syntax.Expression).BindRValueWithoutTargetType(_syntax.Expression, diagnostics);

            var builder = new ForEachEnumeratorInfo.Builder();
            TypeWithAnnotations inferredType;
            bool hasErrors = !GetEnumeratorInfoAndInferCollectionElementType(ref builder, ref collectionExpr, diagnostics, out inferredType);

            // These occur when special types are missing or malformed, or the patterns are incompletely implemented.
            hasErrors |= builder.IsIncomplete;

            BoundAwaitableInfo awaitInfo = null;
            MethodSymbol getEnumeratorMethod = builder.GetEnumeratorInfo?.Method;
            if (getEnumeratorMethod != null)
            {
                originalBinder.CheckImplicitThisCopyInReadOnlyMember(collectionExpr, getEnumeratorMethod, diagnostics);

                if (getEnumeratorMethod.IsExtensionMethod && !hasErrors)
                {
                    var messageId = IsAsync ? MessageID.IDS_FeatureExtensionGetAsyncEnumerator : MessageID.IDS_FeatureExtensionGetEnumerator;
                    hasErrors |= !messageId.CheckFeatureAvailability(
                        diagnostics,
                        Compilation,
                        collectionExpr.Syntax.Location);

                    if (getEnumeratorMethod.ParameterRefKinds is { IsDefault: false } refKinds && refKinds[0] == RefKind.Ref)
                    {
                        Error(diagnostics, ErrorCode.ERR_RefLvalueExpected, collectionExpr.Syntax);
                        hasErrors = true;
                    }
                }
            }
            if (IsAsync)
            {
                var expr = _syntax.Expression;
                ReportBadAwaitDiagnostics(expr, _syntax.AwaitKeyword.GetLocation(), diagnostics, ref hasErrors);
                var placeholder = new BoundAwaitableValuePlaceholder(expr, valEscape: this.LocalScopeDepth, builder.MoveNextInfo?.Method.ReturnType ?? CreateErrorType());
                awaitInfo = BindAwaitInfo(placeholder, expr, diagnostics, ref hasErrors);

                if (!hasErrors && awaitInfo.GetResult?.ReturnType.SpecialType != SpecialType.System_Boolean)
                {
                    diagnostics.Add(ErrorCode.ERR_BadGetAsyncEnumerator, expr.Location, getEnumeratorMethod.ReturnTypeWithAnnotations, getEnumeratorMethod);
                    hasErrors = true;
                }
            }

            TypeWithAnnotations iterationVariableType;
            BoundTypeExpression boundIterationVariableType;
            bool hasNameConflicts = false;
            BoundForEachDeconstructStep deconstructStep = null;
            BoundExpression iterationErrorExpression = null;
            uint collectionEscape = GetValEscape(collectionExpr, this.LocalScopeDepth);
            switch (_syntax.Kind())
            {
                case SyntaxKind.ForEachStatement:
                    {
                        var node = (ForEachStatementSyntax)_syntax;
                        // Check for local variable conflicts in the *enclosing* binder; obviously the *current*
                        // binder has a local that matches!
                        hasNameConflicts = originalBinder.ValidateDeclarationNameConflictsInScope(IterationVariable, diagnostics);

                        // If the type in syntax is "var", then the type should be set explicitly so that the
                        // Type property doesn't fail.
                        TypeSyntax typeSyntax = node.Type.SkipRef(out _);

                        bool isVar;
                        AliasSymbol alias;
                        TypeWithAnnotations declType = BindTypeOrVarKeyword(typeSyntax, diagnostics, out isVar, out alias);

                        if (isVar)
                        {
                            declType = inferredType.HasType ? inferredType : TypeWithAnnotations.Create(CreateErrorType("var"));
                        }
                        else
                        {
                            Debug.Assert(declType.HasType);
                        }

                        iterationVariableType = declType;
                        boundIterationVariableType = new BoundTypeExpression(typeSyntax, alias, iterationVariableType);

                        SourceLocalSymbol local = this.IterationVariable;
                        local.SetTypeWithAnnotations(declType);
                        local.SetValEscape(collectionEscape);

                        if (local.RefKind != RefKind.None)
                        {
                            // The ref-escape of a ref-returning property is decided
                            // by the value escape of its receiver, in this case the
                            // collection
                            local.SetRefEscape(collectionEscape);

                            if (CheckRefLocalInAsyncOrIteratorMethod(local.IdentifierToken, diagnostics))
                            {
                                hasErrors = true;
                            }
                        }

                        if (!hasErrors)
                        {
                            BindValueKind requiredCurrentKind;
                            switch (local.RefKind)
                            {
                                case RefKind.None:
                                    requiredCurrentKind = BindValueKind.RValue;
                                    break;
                                case RefKind.Ref:
                                    requiredCurrentKind = BindValueKind.Assignable | BindValueKind.RefersToLocation;
                                    break;
                                case RefKind.RefReadOnly:
                                    requiredCurrentKind = BindValueKind.RefersToLocation;
                                    break;
                                default:
                                    throw ExceptionUtilities.UnexpectedValue(local.RefKind);
                            }

                            hasErrors |= !CheckMethodReturnValueKind(
                                builder.CurrentPropertyGetter,
                                callSyntaxOpt: null,
                                collectionExpr.Syntax,
                                requiredCurrentKind,
                                checkingReceiver: false,
                                diagnostics);
                        }

                        break;
                    }
                case SyntaxKind.ForEachVariableStatement:
                    {
                        var node = (ForEachVariableStatementSyntax)_syntax;
                        iterationVariableType = inferredType.HasType ? inferredType : TypeWithAnnotations.Create(CreateErrorType("var"));

                        var variables = node.Variable;
                        if (variables.IsDeconstructionLeft())
                        {
                            var valuePlaceholder = new BoundDeconstructValuePlaceholder(_syntax.Expression, collectionEscape, iterationVariableType.Type).MakeCompilerGenerated();
                            DeclarationExpressionSyntax declaration = null;
                            ExpressionSyntax expression = null;
                            BoundDeconstructionAssignmentOperator deconstruction = BindDeconstruction(
                                                                                    variables,
                                                                                    variables,
                                                                                    right: _syntax.Expression,
                                                                                    diagnostics: diagnostics,
                                                                                    rightPlaceholder: valuePlaceholder,
                                                                                    declaration: ref declaration,
                                                                                    expression: ref expression);

                            if (expression != null)
                            {
                                // error: must declare foreach loop iteration variables.
                                Error(diagnostics, ErrorCode.ERR_MustDeclareForeachIteration, variables);
                                hasErrors = true;
                            }

                            deconstructStep = new BoundForEachDeconstructStep(variables, deconstruction, valuePlaceholder).MakeCompilerGenerated();
                        }
                        else
                        {
                            // Bind the expression for error recovery, but discard all new diagnostics
                            iterationErrorExpression = BindExpression(node.Variable, new DiagnosticBag());
                            if (iterationErrorExpression.Kind == BoundKind.DiscardExpression)
                            {
                                iterationErrorExpression = ((BoundDiscardExpression)iterationErrorExpression).FailInference(this, diagnosticsOpt: null);
                            }
                            hasErrors = true;

                            if (!node.HasErrors)
                            {
                                Error(diagnostics, ErrorCode.ERR_MustDeclareForeachIteration, variables);
                            }
                        }

                        boundIterationVariableType = new BoundTypeExpression(variables, aliasOpt: null, typeWithAnnotations: iterationVariableType).MakeCompilerGenerated();
                        break;
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(_syntax.Kind());
            }

            BoundStatement body = originalBinder.BindPossibleEmbeddedStatement(_syntax.Statement, diagnostics);

            // NOTE: in error cases, binder may collect all kind of variables, not just formally declared iteration variables.
            //       As a matter of error recovery, we will treat such variables the same as the iteration variables.
            //       I.E. - they will be considered declared and assigned in each iteration step. 
            ImmutableArray<LocalSymbol> iterationVariables = this.Locals;

            Debug.Assert(hasErrors ||
                _syntax.HasErrors ||
                iterationVariables.All(local => local.DeclarationKind == LocalDeclarationKind.ForEachIterationVariable),
                "Should not have iteration variables that are not ForEachIterationVariable in valid code");

            hasErrors = hasErrors || boundIterationVariableType.HasErrors || iterationVariableType.Type.IsErrorType();

            // Skip the conversion checks and array/enumerator differentiation if we know we have an error (except local name conflicts).
            if (hasErrors)
            {
                return new BoundForEachStatement(
                    _syntax,
                    enumeratorInfoOpt: null, // can't be sure that it's complete
                    elementConversion: default,
                    boundIterationVariableType,
                    iterationVariables,
                    iterationErrorExpression,
                    collectionExpr,
                    deconstructStep,
                    awaitInfo,
                    body,
                    CheckOverflowAtRuntime,
                    this.BreakLabel,
                    this.ContinueLabel,
                    hasErrors);
            }

            hasErrors |= hasNameConflicts;

            var foreachKeyword = _syntax.ForEachKeyword;
            ReportDiagnosticsIfObsolete(diagnostics, getEnumeratorMethod, foreachKeyword, hasBaseReceiver: false);
            ReportDiagnosticsIfUnmanagedCallersOnly(diagnostics, getEnumeratorMethod, foreachKeyword.GetLocation(), isDelegateConversion: false);
            // MoveNext is an instance method, so it does not need to have unmanaged callers only diagnostics reported.
            // Either a diagnostic was reported at the declaration of the method (for the invalid attribute), or MoveNext
            // is marked as not supported and we won't get here in the first place (for metadata import).
            ReportDiagnosticsIfObsolete(diagnostics, builder.MoveNextInfo.Method, foreachKeyword, hasBaseReceiver: false);
            ReportDiagnosticsIfObsolete(diagnostics, builder.CurrentPropertyGetter, foreachKeyword, hasBaseReceiver: false);
            ReportDiagnosticsIfObsolete(diagnostics, builder.CurrentPropertyGetter.AssociatedSymbol, foreachKeyword, hasBaseReceiver: false);

            // We want to convert from inferredType in the array/string case and builder.ElementType in the enumerator case,
            // but it turns out that these are equivalent (when both are available).

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            Conversion elementConversion = this.Conversions.ClassifyConversionFromType(inferredType.Type, iterationVariableType.Type, ref useSiteDiagnostics, forCast: true);

            if (!elementConversion.IsValid)
            {
                ImmutableArray<MethodSymbol> originalUserDefinedConversions = elementConversion.OriginalUserDefinedConversions;
                if (originalUserDefinedConversions.Length > 1)
                {
                    diagnostics.Add(ErrorCode.ERR_AmbigUDConv, foreachKeyword.GetLocation(), originalUserDefinedConversions[0], originalUserDefinedConversions[1], inferredType.Type, iterationVariableType);
                }
                else
                {
                    SymbolDistinguisher distinguisher = new SymbolDistinguisher(this.Compilation, inferredType.Type, iterationVariableType.Type);
                    diagnostics.Add(ErrorCode.ERR_NoExplicitConv, foreachKeyword.GetLocation(), distinguisher.First, distinguisher.Second);
                }
                hasErrors = true;
            }
            else
            {
                ReportDiagnosticsIfObsolete(diagnostics, elementConversion, _syntax.ForEachKeyword, hasBaseReceiver: false);
            }

            // Spec (§8.8.4):
            // If the type X of expression is dynamic then there is an implicit conversion from >>expression<< (not the type of the expression) 
            // to the System.Collections.IEnumerable interface (§6.1.8). 
            builder.CollectionConversion = this.Conversions.ClassifyConversionFromExpression(collectionExpr, builder.CollectionType, ref useSiteDiagnostics);
            builder.CurrentConversion = this.Conversions.ClassifyConversionFromType(builder.CurrentPropertyGetter.ReturnType, builder.ElementType, ref useSiteDiagnostics);

            TypeSymbol getEnumeratorType = getEnumeratorMethod.ReturnType;
            // we never convert struct enumerators to object - it is done only for null-checks.
            builder.EnumeratorConversion = getEnumeratorType.IsValueType ?
                Conversion.Identity :
                this.Conversions.ClassifyConversionFromType(getEnumeratorType, GetSpecialType(SpecialType.System_Object, diagnostics, _syntax), ref useSiteDiagnostics);

            if (getEnumeratorType.IsRestrictedType() && (IsDirectlyInIterator || IsInAsyncMethod()))
            {
                diagnostics.Add(ErrorCode.ERR_BadSpecialByRefIterator, foreachKeyword.GetLocation(), getEnumeratorType);
            }

            diagnostics.Add(_syntax.ForEachKeyword.GetLocation(), useSiteDiagnostics);

            // Due to the way we extracted the various types, these conversions should always be possible.
            // CAVEAT: if we're iterating over an array of pointers, the current conversion will fail since we
            // can't convert from object to a pointer type.  Similarly, if we're iterating over an array of
            // Nullable<Error>, the current conversion will fail because we don't know if an ErrorType is a
            // value type.  This doesn't matter in practice, since we won't actually use the enumerator pattern 
            // when we lower the loop.
            Debug.Assert(builder.CollectionConversion.IsValid);
            Debug.Assert(builder.CurrentConversion.IsValid ||
                (builder.ElementType.IsPointerOrFunctionPointer() && collectionExpr.Type.IsArray()) ||
                (builder.ElementType.IsNullableType() && builder.ElementType.GetMemberTypeArgumentsNoUseSiteDiagnostics().Single().IsErrorType() && collectionExpr.Type.IsArray()));
            Debug.Assert(builder.EnumeratorConversion.IsValid ||
                this.Compilation.GetSpecialType(SpecialType.System_Object).TypeKind == TypeKind.Error ||
                !useSiteDiagnostics.IsNullOrEmpty(),
                "Conversions to object succeed unless there's a problem with the object type or the source type");

            // If user-defined conversions could occur here, we would need to check for ObsoleteAttribute.
            Debug.Assert((object)builder.CollectionConversion.Method == null,
                "Conversion from collection expression to collection type should not be user-defined");
            Debug.Assert((object)builder.CurrentConversion.Method == null,
                "Conversion from Current property type to element type should not be user-defined");
            Debug.Assert((object)builder.EnumeratorConversion.Method == null,
                "Conversion from GetEnumerator return type to System.Object should not be user-defined");

            // We're wrapping the collection expression in a (non-synthesized) conversion so that its converted
            // type (i.e. builder.CollectionType) will be available in the binding API.
            BoundConversion convertedCollectionExpression = new BoundConversion(
                collectionExpr.Syntax,
                collectionExpr,
                builder.CollectionConversion,
                @checked: CheckOverflowAtRuntime,
                explicitCastInCode: false,
                conversionGroupOpt: null,
                ConstantValue.NotAvailable,
                builder.CollectionType);

            if (builder.NeedsDisposal && IsAsync)
            {
                hasErrors |= GetAwaitDisposeAsyncInfo(ref builder, diagnostics);
            }

            Debug.Assert(
                hasErrors ||
                builder.CollectionConversion.IsIdentity ||
                (builder.CollectionConversion.IsImplicit &&
                 (IsIEnumerable(builder.CollectionType) ||
                  IsIEnumerableT(builder.CollectionType.OriginalDefinition, IsAsync, Compilation) ||
                  builder.GetEnumeratorInfo.Method.IsExtensionMethod)) ||
                // For compat behavior, we can enumerate over System.String even if it's not IEnumerable. That will
                // result in an explicit reference conversion in the bound nodes, but that conversion won't be emitted.
                (builder.CollectionConversion.Kind == ConversionKind.ExplicitReference && collectionExpr.Type.SpecialType == SpecialType.System_String));

            return new BoundForEachStatement(
                _syntax,
                builder.Build(this.Flags),
                elementConversion,
                boundIterationVariableType,
                iterationVariables,
                iterationErrorExpression,
                convertedCollectionExpression,
                deconstructStep,
                awaitInfo,
                body,
                CheckOverflowAtRuntime,
                this.BreakLabel,
                this.ContinueLabel,
                hasErrors);
        }

        private bool GetAwaitDisposeAsyncInfo(ref ForEachEnumeratorInfo.Builder builder, DiagnosticBag diagnostics)
        {
            var awaitableType = builder.PatternDisposeInfo is null
                ? this.GetWellKnownType(WellKnownType.System_Threading_Tasks_ValueTask, diagnostics, _syntax)
                : builder.PatternDisposeInfo.Method.ReturnType;

            bool hasErrors = false;
            var expr = _syntax.Expression;
            ReportBadAwaitDiagnostics(expr, _syntax.AwaitKeyword.GetLocation(), diagnostics, ref hasErrors);

            var placeholder = new BoundAwaitableValuePlaceholder(expr, valEscape: this.LocalScopeDepth, awaitableType);
            builder.DisposeAwaitableInfo = BindAwaitInfo(placeholder, expr, diagnostics, ref hasErrors);
            return hasErrors;
        }

        internal TypeWithAnnotations InferCollectionElementType(DiagnosticBag diagnostics, ExpressionSyntax collectionSyntax)
        {
            // Use the right binder to avoid seeing iteration variable
            BoundExpression collectionExpr = this.GetBinder(collectionSyntax).BindValue(collectionSyntax, diagnostics, BindValueKind.RValue);

            var builder = new ForEachEnumeratorInfo.Builder();
            GetEnumeratorInfoAndInferCollectionElementType(ref builder, ref collectionExpr, diagnostics, out TypeWithAnnotations inferredType);
            return inferredType;
        }

        private bool GetEnumeratorInfoAndInferCollectionElementType(ref ForEachEnumeratorInfo.Builder builder, ref BoundExpression collectionExpr, DiagnosticBag diagnostics, out TypeWithAnnotations inferredType)
        {
            bool gotInfo = GetEnumeratorInfo(ref builder, ref collectionExpr, diagnostics);

            if (!gotInfo)
            {
                inferredType = default;
            }
            else if (collectionExpr.HasDynamicType())
            {
                // If the enumerator is dynamic, it yields dynamic values 
                inferredType = TypeWithAnnotations.Create(DynamicTypeSymbol.Instance);
            }
            else if (collectionExpr.Type.SpecialType == SpecialType.System_String && builder.CollectionType.SpecialType == SpecialType.System_Collections_IEnumerable)
            {
                // Reproduce dev11 behavior: we're always going to lower a foreach loop over a string to a for loop 
                // over the string's Chars indexer.  Therefore, we should infer "char", regardless of what the spec
                // indicates the element type is.  This actually matters in practice because the System.String in
                // the portable library doesn't have a pattern GetEnumerator method or implement IEnumerable<char>.
                inferredType = TypeWithAnnotations.Create(GetSpecialType(SpecialType.System_Char, diagnostics, collectionExpr.Syntax));
            }
            else
            {
                inferredType = builder.ElementTypeWithAnnotations;
            }

            return gotInfo;
        }

        /// <summary>
        /// The spec describes an algorithm for finding the following types:
        ///   1) Collection type
        ///   2) Enumerator type
        ///   3) Element type
        ///   
        /// The implementation details are a bit different.  If we're iterating over a string or an array, then we don't need to record anything
        /// but the inferredType (in case the iteration variable is implicitly typed).  If we're iterating over anything else, then we want the 
        /// inferred type plus a ForEachEnumeratorInfo.Builder with:
        ///   1) Collection type
        ///   2) Element type
        ///   3) GetEnumerator (or GetAsyncEnumerator) method of the collection type (return type will be the enumerator type from the spec)
        ///   4) Current property and MoveNext (or MoveNextAsync) method of the enumerator type
        ///   
        /// The caller will have to do some extra conversion checks before creating a ForEachEnumeratorInfo for the BoundForEachStatement.
        /// </summary>
        /// <param name="builder">Builder to fill in (partially, all but conversions).</param>
        /// <param name="collectionExpr">The expression over which to iterate.</param>
        /// <param name="diagnostics">Populated with binding diagnostics.</param>
        /// <returns>Partially populated (all but conversions) or null if there was an error.</returns>
        private bool GetEnumeratorInfo(ref ForEachEnumeratorInfo.Builder builder, ref BoundExpression collectionExpr, DiagnosticBag diagnostics)
        {
            bool isAsync = IsAsync;
            builder.IsAsync = isAsync;

            EnumeratorResult found = GetEnumeratorInfo(_syntax, _syntax.Expression, ref builder, ref collectionExpr, isAsync, diagnostics);
            switch (found)
            {
                case EnumeratorResult.Succeeded:
                    return true;
                case EnumeratorResult.FailedAndReported:
                    return false;
            }

            TypeSymbol collectionExprType = collectionExpr.Type;
            if (string.IsNullOrEmpty(collectionExprType.Name) && collectionExpr.HasErrors)
            {
                return false;
            }

            if (collectionExprType.IsErrorType())
            {
                return false;
            }

            // Retry with a different assumption about whether the foreach is async
            var ignoredBuilder = new ForEachEnumeratorInfo.Builder();
            var ignoredDiagnostics = DiagnosticBag.GetInstance();
            bool wrongAsync = GetEnumeratorInfo(_syntax, _syntax.Expression, ref ignoredBuilder, ref collectionExpr, !isAsync, ignoredDiagnostics) == EnumeratorResult.Succeeded;
            ignoredDiagnostics.Free();

            var errorCode = wrongAsync
                ? (isAsync ? ErrorCode.ERR_AwaitForEachMissingMemberWrongAsync : ErrorCode.ERR_ForEachMissingMemberWrongAsync)
                : (isAsync ? ErrorCode.ERR_AwaitForEachMissingMember : ErrorCode.ERR_ForEachMissingMember);

            diagnostics.Add(errorCode, _syntax.Expression.Location,
                collectionExprType, isAsync ? GetAsyncEnumeratorMethodName : GetEnumeratorMethodName);
            return false;
        }

        internal static NamedTypeSymbol GetIEnumerableOfT(TypeSymbol type, bool isAsync, CSharpCompilation compilation, ref HashSet<DiagnosticInfo> useSiteDiagnostics, out bool foundMultiple)
        {
            NamedTypeSymbol implementedIEnumerable = null;
            foundMultiple = false;

            if (type.TypeKind == TypeKind.TypeParameter)
            {
                var typeParameter = (TypeParameterSymbol)type;
                var allInterfaces = typeParameter.EffectiveBaseClass(ref useSiteDiagnostics).AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics)
                    .Concat(typeParameter.AllEffectiveInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics));
                GetIEnumerableOfT(allInterfaces, isAsync, compilation, ref @implementedIEnumerable, ref foundMultiple);
            }
            else
            {
                GetIEnumerableOfT(type.AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics), isAsync, compilation, ref @implementedIEnumerable, ref foundMultiple);
            }

            return implementedIEnumerable;
        }

        private static void GetIEnumerableOfT(ImmutableArray<NamedTypeSymbol> interfaces, bool isAsync, CSharpCompilation compilation, ref NamedTypeSymbol result, ref bool foundMultiple)
        {
            if (foundMultiple)
            {
                return;
            }

            interfaces = MethodTypeInferrer.ModuloReferenceTypeNullabilityDifferences(interfaces, VarianceKind.In);

            foreach (NamedTypeSymbol @interface in interfaces)
            {
                if (IsIEnumerableT(@interface.OriginalDefinition, isAsync, compilation))
                {
                    if ((object)result == null ||
                        TypeSymbol.Equals(@interface, result, TypeCompareKind.IgnoreTupleNames))
                    {
                        result = @interface;
                    }
                    else
                    {
                        foundMultiple = true;
                        return;
                    }
                }
            }
        }

        internal static bool IsIEnumerableT(TypeSymbol type, bool isAsync, CSharpCompilation compilation)
        {
            if (isAsync)
            {
                return type.Equals(compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T));
            }
            else
            {
                return type.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T;
            }
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(SyntaxNode scopeDesignator)
        {
            if (_syntax == scopeDesignator)
            {
                return this.Locals;
            }

            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override SyntaxNode ScopeDesignator
        {
            get
            {
                return _syntax;
            }
        }
    }
}
