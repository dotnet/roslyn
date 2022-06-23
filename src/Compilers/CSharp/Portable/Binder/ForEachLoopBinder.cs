// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
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
        private const string GetEnumeratorMethodName = WellKnownMemberNames.GetEnumeratorMethodName;
        private const string CurrentPropertyName = WellKnownMemberNames.CurrentPropertyName;
        private const string MoveNextMethodName = WellKnownMemberNames.MoveNextMethodName;

        private const string GetAsyncEnumeratorMethodName = WellKnownMemberNames.GetAsyncEnumeratorMethodName;
        private const string MoveNextAsyncMethodName = WellKnownMemberNames.MoveNextAsyncMethodName;

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
        internal override BoundStatement BindForEachParts(BindingDiagnosticBag diagnostics, Binder originalBinder)
        {
            BoundForEachStatement result = BindForEachPartsWorker(diagnostics, originalBinder);
            return result;
        }

        /// <summary>
        /// Like BindForEachParts, but only bind the deconstruction part of the foreach, for purpose of inferring the types of the declared locals.
        /// </summary>
        internal override BoundStatement BindForEachDeconstruction(BindingDiagnosticBag diagnostics, Binder originalBinder)
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

        private BoundForEachStatement BindForEachPartsWorker(BindingDiagnosticBag diagnostics, Binder originalBinder)
        {
            if (IsAsync)
            {
                CheckFeatureAvailability(_syntax, MessageID.IDS_FeatureAsyncStreams, diagnostics, _syntax.AwaitKeyword.GetLocation());
            }

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
                            iterationErrorExpression = BindExpression(node.Variable, BindingDiagnosticBag.Discarded);
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
                    elementPlaceholder: null,
                    elementConversion: null,
                    boundIterationVariableType,
                    iterationVariables,
                    iterationErrorExpression,
                    collectionExpr,
                    deconstructStep,
                    awaitInfo,
                    body,
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

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            Conversion elementConversionClassification = this.Conversions.ClassifyConversionFromType(inferredType.Type, iterationVariableType.Type, isChecked: CheckOverflowAtRuntime, ref useSiteInfo, forCast: true);

            var elementPlaceholder = new BoundValuePlaceholder(_syntax, inferredType.Type).MakeCompilerGenerated();
            BindingDiagnosticBag createConversionDiagnostics;

            if (!elementConversionClassification.IsValid)
            {
                ImmutableArray<MethodSymbol> originalUserDefinedConversions = elementConversionClassification.OriginalUserDefinedConversions;
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

                createConversionDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: false, withDependencies: false);
            }
            else
            {
                createConversionDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics);
            }

            BoundExpression elementConversion = CreateConversion(_syntax, elementPlaceholder, elementConversionClassification, isCast: false, conversionGroupOpt: null, iterationVariableType.Type, createConversionDiagnostics);

            if (createConversionDiagnostics.AccumulatesDiagnostics && !createConversionDiagnostics.DiagnosticBag.IsEmptyWithoutResolution)
            {
                diagnostics.AddDependencies(createConversionDiagnostics);

                var location = _syntax.ForEachKeyword.GetLocation();
                foreach (var d in createConversionDiagnostics.DiagnosticBag.AsEnumerableWithoutResolution())
                {
                    diagnostics.Add(d.WithLocation(location));
                }
            }
            else
            {
                diagnostics.AddRange(createConversionDiagnostics);
            }

            createConversionDiagnostics.Free();

            // Spec (§8.8.4):
            // If the type X of expression is dynamic then there is an implicit conversion from >>expression<< (not the type of the expression) 
            // to the System.Collections.IEnumerable interface (§6.1.8). 
            Conversion collectionConversionClassification = this.Conversions.ClassifyConversionFromExpression(collectionExpr, builder.CollectionType, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);
            Conversion currentConversionClassification = this.Conversions.ClassifyConversionFromType(builder.CurrentPropertyGetter.ReturnType, builder.ElementType, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);

            TypeSymbol getEnumeratorType = getEnumeratorMethod.ReturnType;

            if (getEnumeratorType.IsRestrictedType() && (IsDirectlyInIterator || IsInAsyncMethod()))
            {
                diagnostics.Add(ErrorCode.ERR_BadSpecialByRefIterator, foreachKeyword.GetLocation(), getEnumeratorType);
            }

            diagnostics.Add(_syntax.ForEachKeyword.GetLocation(), useSiteInfo);

            // Due to the way we extracted the various types, these conversions should always be possible.
            // CAVEAT: if we're iterating over an array of pointers, the current conversion will fail since we
            // can't convert from object to a pointer type.  Similarly, if we're iterating over an array of
            // Nullable<Error>, the current conversion will fail because we don't know if an ErrorType is a
            // value type.  This doesn't matter in practice, since we won't actually use the enumerator pattern 
            // when we lower the loop.
            Debug.Assert(collectionConversionClassification.IsValid);
            Debug.Assert(currentConversionClassification.IsValid ||
                (builder.ElementType.IsPointerOrFunctionPointer() && collectionExpr.Type.IsArray()) ||
                (builder.ElementType.IsNullableType() && builder.ElementType.GetMemberTypeArgumentsNoUseSiteDiagnostics().Single().IsErrorType() && collectionExpr.Type.IsArray()));

            // If user-defined conversions could occur here, we would need to check for ObsoleteAttribute.
            Debug.Assert((object)collectionConversionClassification.Method == null,
                "Conversion from collection expression to collection type should not be user-defined");
            Debug.Assert((object)currentConversionClassification.Method == null,
                "Conversion from Current property type to element type should not be user-defined");

            // We're wrapping the collection expression in a (non-synthesized) conversion so that its converted
            // type (i.e. builder.CollectionType) will be available in the binding API.
            Debug.Assert(!collectionConversionClassification.IsUserDefined);

            BoundExpression convertedCollectionExpression = CreateConversion(collectionExpr.Syntax, collectionExpr, collectionConversionClassification, isCast: false, conversionGroupOpt: null, builder.CollectionType, diagnostics);

            if ((convertedCollectionExpression as BoundConversion)?.Operand != (object)collectionExpr)
            {
                Debug.Assert(collectionConversionClassification.IsIdentity);
                Debug.Assert(convertedCollectionExpression == (object)collectionExpr);

                convertedCollectionExpression = new BoundConversion(
                    collectionExpr.Syntax,
                    collectionExpr,
                    collectionConversionClassification,
                    @checked: CheckOverflowAtRuntime,
                    explicitCastInCode: false,
                    conversionGroupOpt: null,
                    ConstantValue.NotAvailable,
                    builder.CollectionType);
            }

            if (currentConversionClassification.IsValid)
            {
                builder.CurrentPlaceholder = new BoundValuePlaceholder(_syntax, builder.CurrentPropertyGetter.ReturnType).MakeCompilerGenerated();
                builder.CurrentConversion = CreateConversion(_syntax, builder.CurrentPlaceholder, currentConversionClassification, isCast: false, conversionGroupOpt: null, builder.ElementType, diagnostics);
            }

            if (builder.NeedsDisposal && IsAsync)
            {
                hasErrors |= GetAwaitDisposeAsyncInfo(ref builder, diagnostics);
            }

            Debug.Assert(
                hasErrors ||
                collectionConversionClassification.IsIdentity ||
                (collectionConversionClassification.IsImplicit &&
                 (IsIEnumerable(builder.CollectionType) ||
                  IsIEnumerableT(builder.CollectionType.OriginalDefinition, IsAsync, Compilation) ||
                  builder.GetEnumeratorInfo.Method.IsExtensionMethod)) ||
                // For compat behavior, we can enumerate over System.String even if it's not IEnumerable. That will
                // result in an explicit reference conversion in the bound nodes, but that conversion won't be emitted.
                (collectionConversionClassification.Kind == ConversionKind.ExplicitReference && collectionExpr.Type.SpecialType == SpecialType.System_String));

            return new BoundForEachStatement(
                _syntax,
                builder.Build(this.Flags),
                elementPlaceholder,
                elementConversion,
                boundIterationVariableType,
                iterationVariables,
                iterationErrorExpression,
                convertedCollectionExpression,
                deconstructStep,
                awaitInfo,
                body,
                this.BreakLabel,
                this.ContinueLabel,
                hasErrors);
        }

        private bool GetAwaitDisposeAsyncInfo(ref ForEachEnumeratorInfo.Builder builder, BindingDiagnosticBag diagnostics)
        {
            var awaitableType = builder.PatternDisposeInfo is null
                ? this.GetWellKnownType(WellKnownType.System_Threading_Tasks_ValueTask, diagnostics, this._syntax)
                : builder.PatternDisposeInfo.Method.ReturnType;

            bool hasErrors = false;
            var expr = _syntax.Expression;
            ReportBadAwaitDiagnostics(expr, _syntax.AwaitKeyword.GetLocation(), diagnostics, ref hasErrors);

            var placeholder = new BoundAwaitableValuePlaceholder(expr, valEscape: this.LocalScopeDepth, awaitableType);
            builder.DisposeAwaitableInfo = BindAwaitInfo(placeholder, expr, diagnostics, ref hasErrors);
            return hasErrors;
        }

        internal TypeWithAnnotations InferCollectionElementType(BindingDiagnosticBag diagnostics, ExpressionSyntax collectionSyntax)
        {
            // Use the right binder to avoid seeing iteration variable
            BoundExpression collectionExpr = this.GetBinder(collectionSyntax).BindValue(collectionSyntax, diagnostics, BindValueKind.RValue);

            var builder = new ForEachEnumeratorInfo.Builder();
            GetEnumeratorInfoAndInferCollectionElementType(ref builder, ref collectionExpr, diagnostics, out TypeWithAnnotations inferredType);
            return inferredType;
        }

        private bool GetEnumeratorInfoAndInferCollectionElementType(ref ForEachEnumeratorInfo.Builder builder, ref BoundExpression collectionExpr, BindingDiagnosticBag diagnostics, out TypeWithAnnotations inferredType)
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

        private BoundExpression UnwrapCollectionExpressionIfNullable(BoundExpression collectionExpr, BindingDiagnosticBag diagnostics)
        {
            TypeSymbol collectionExprType = collectionExpr.Type;

            // If collectionExprType is a nullable type, then use the underlying type and take the value (i.e. .Value) of collectionExpr.
            // This behavior is not spec'd, but it's what Dev10 does.
            if ((object)collectionExprType != null && collectionExprType.IsNullableType())
            {
                SyntaxNode exprSyntax = collectionExpr.Syntax;

                MethodSymbol nullableValueGetter = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Nullable_T_get_Value, diagnostics, exprSyntax);
                if ((object)nullableValueGetter != null)
                {
                    nullableValueGetter = nullableValueGetter.AsMember((NamedTypeSymbol)collectionExprType);

                    // Synthesized call, because we don't want to modify the type in the SemanticModel.
                    return BoundCall.Synthesized(
                        syntax: exprSyntax,
                        receiverOpt: collectionExpr,
                        method: nullableValueGetter);
                }
                else
                {
                    return new BoundBadExpression(
                        exprSyntax,
                        LookupResultKind.Empty,
                        ImmutableArray<Symbol>.Empty,
                        ImmutableArray.Create(collectionExpr),
                        collectionExprType.GetNullableUnderlyingType())
                    { WasCompilerGenerated = true }; // Don't affect the type in the SemanticModel.
                }
            }

            return collectionExpr;
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
        private bool GetEnumeratorInfo(ref ForEachEnumeratorInfo.Builder builder, ref BoundExpression collectionExpr, BindingDiagnosticBag diagnostics)
        {
            bool isAsync = IsAsync;
            builder.IsAsync = isAsync;

            EnumeratorResult found = GetEnumeratorInfo(ref builder, ref collectionExpr, isAsync, diagnostics);
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
            bool wrongAsync = GetEnumeratorInfo(ref ignoredBuilder, ref collectionExpr, !isAsync, BindingDiagnosticBag.Discarded) == EnumeratorResult.Succeeded;

            var errorCode = wrongAsync
                ? (isAsync ? ErrorCode.ERR_AwaitForEachMissingMemberWrongAsync : ErrorCode.ERR_ForEachMissingMemberWrongAsync)
                : (isAsync ? ErrorCode.ERR_AwaitForEachMissingMember : ErrorCode.ERR_ForEachMissingMember);

            diagnostics.Add(errorCode, _syntax.Expression.Location,
                collectionExprType, isAsync ? GetAsyncEnumeratorMethodName : GetEnumeratorMethodName);
            return false;
        }

        private enum EnumeratorResult
        {
            Succeeded,
            FailedNotReported,
            FailedAndReported
        }

        private EnumeratorResult GetEnumeratorInfo(ref ForEachEnumeratorInfo.Builder builder, ref BoundExpression collectionExpr, bool isAsync, BindingDiagnosticBag diagnostics)
        {
            TypeSymbol collectionExprType = collectionExpr.Type;

            if (collectionExprType is null) // There's no way to enumerate something without a type.
            {
                if (!ReportConstantNullCollectionExpr(collectionExpr, diagnostics))
                {
                    // Anything else with a null type is a method group or anonymous function
                    diagnostics.Add(ErrorCode.ERR_AnonMethGrpInForEach, _syntax.Expression.Location, collectionExpr.Display);
                }

                // CONSIDER: dev10 also reports ERR_ForEachMissingMember (i.e. failed pattern match).
                return EnumeratorResult.FailedAndReported;
            }

            if (collectionExpr.ResultKind == LookupResultKind.NotAValue)
            {
                // Short-circuiting to prevent strange behavior in the case where the collection
                // expression is a type expression and the type is enumerable.
                Debug.Assert(collectionExpr.HasAnyErrors); // should already have been reported
                return EnumeratorResult.FailedAndReported;
            }

            if (collectionExprType.Kind == SymbolKind.DynamicType && IsAsync)
            {
                diagnostics.Add(ErrorCode.ERR_BadDynamicAwaitForEach, _syntax.Expression.Location);
                return EnumeratorResult.FailedAndReported;
            }

            // The spec specifically lists the collection, enumerator, and element types for arrays and dynamic.
            if (collectionExprType.Kind == SymbolKind.ArrayType || collectionExprType.Kind == SymbolKind.DynamicType)
            {
                if (ReportConstantNullCollectionExpr(collectionExpr, diagnostics))
                {
                    return EnumeratorResult.FailedAndReported;
                }
                builder = GetDefaultEnumeratorInfo(builder, diagnostics, collectionExprType);
                return EnumeratorResult.Succeeded;
            }

            var unwrappedCollectionExpr = UnwrapCollectionExpressionIfNullable(collectionExpr, diagnostics);
            var unwrappedCollectionExprType = unwrappedCollectionExpr.Type;

            if (SatisfiesGetEnumeratorPattern(ref builder, unwrappedCollectionExpr, isAsync, viaExtensionMethod: false, diagnostics))
            {
                collectionExpr = unwrappedCollectionExpr;
                if (ReportConstantNullCollectionExpr(collectionExpr, diagnostics))
                {
                    return EnumeratorResult.FailedAndReported;
                }
                return createPatternBasedEnumeratorResult(ref builder, unwrappedCollectionExpr, isAsync, viaExtensionMethod: false, diagnostics);
            }

            if (!isAsync && IsIEnumerable(unwrappedCollectionExprType))
            {
                collectionExpr = unwrappedCollectionExpr;
                // This indicates a problem with the special IEnumerable type - it should have satisfied the GetEnumerator pattern.
                diagnostics.Add(ErrorCode.ERR_ForEachMissingMember, _syntax.Expression.Location, unwrappedCollectionExprType, GetEnumeratorMethodName);
                return EnumeratorResult.FailedAndReported;
            }
            if (isAsync && IsIAsyncEnumerable(unwrappedCollectionExprType))
            {
                collectionExpr = unwrappedCollectionExpr;
                // This indicates a problem with the well-known IAsyncEnumerable type - it should have satisfied the GetAsyncEnumerator pattern.
                diagnostics.Add(ErrorCode.ERR_AwaitForEachMissingMember, _syntax.Expression.Location, unwrappedCollectionExprType, GetAsyncEnumeratorMethodName);
                return EnumeratorResult.FailedAndReported;
            }

            if (SatisfiesIEnumerableInterfaces(ref builder, unwrappedCollectionExpr, isAsync, diagnostics, unwrappedCollectionExprType) is not EnumeratorResult.FailedNotReported and var result)
            {
                collectionExpr = unwrappedCollectionExpr;
                return result;
            }

            // COMPAT:
            // In some rare cases, like MicroFramework, System.String does not implement foreach pattern.
            // For compat reasons we must still treat System.String as valid to use in a foreach
            // Similarly to the cases with array and dynamic, we will default to IEnumerable for binding purposes.
            // Lowering will not use iterator info with strings, so it is ok.
            if (!isAsync && collectionExprType.SpecialType == SpecialType.System_String)
            {
                if (ReportConstantNullCollectionExpr(collectionExpr, diagnostics))
                {
                    return EnumeratorResult.FailedAndReported;
                }

                builder = GetDefaultEnumeratorInfo(builder, diagnostics, collectionExprType);
                return EnumeratorResult.Succeeded;
            }

            if (SatisfiesGetEnumeratorPattern(ref builder, collectionExpr, isAsync, viaExtensionMethod: true, diagnostics))
            {
                return createPatternBasedEnumeratorResult(ref builder, collectionExpr, isAsync, viaExtensionMethod: true, diagnostics);
            }

            return EnumeratorResult.FailedNotReported;

            EnumeratorResult createPatternBasedEnumeratorResult(ref ForEachEnumeratorInfo.Builder builder, BoundExpression collectionExpr, bool isAsync, bool viaExtensionMethod, BindingDiagnosticBag diagnostics)
            {
                Debug.Assert((object)builder.GetEnumeratorInfo != null);

                Debug.Assert(!(viaExtensionMethod && builder.GetEnumeratorInfo.Method.Parameters.IsDefaultOrEmpty));

                builder.CollectionType = viaExtensionMethod
                    ? builder.GetEnumeratorInfo.Method.Parameters[0].Type
                    : collectionExpr.Type;

                if (SatisfiesForEachPattern(ref builder, isAsync, diagnostics))
                {
                    builder.ElementTypeWithAnnotations = ((PropertySymbol)builder.CurrentPropertyGetter.AssociatedSymbol).TypeWithAnnotations;

                    GetDisposalInfoForEnumerator(ref builder, collectionExpr, isAsync, diagnostics);

                    return EnumeratorResult.Succeeded;
                }

                MethodSymbol getEnumeratorMethod = builder.GetEnumeratorInfo.Method;
                diagnostics.Add(isAsync ? ErrorCode.ERR_BadGetAsyncEnumerator : ErrorCode.ERR_BadGetEnumerator, _syntax.Expression.Location, getEnumeratorMethod.ReturnType, getEnumeratorMethod);
                return EnumeratorResult.FailedAndReported;
            }
        }

        private EnumeratorResult SatisfiesIEnumerableInterfaces(ref ForEachEnumeratorInfo.Builder builder, BoundExpression collectionExpr, bool isAsync, BindingDiagnosticBag diagnostics, TypeSymbol unwrappedCollectionExprType)
        {
            if (!AllInterfacesContainsIEnumerable(ref builder, unwrappedCollectionExprType, isAsync, diagnostics, out bool foundMultipleGenericIEnumerableInterfaces))
            {
                return EnumeratorResult.FailedNotReported;
            }

            if (ReportConstantNullCollectionExpr(collectionExpr, diagnostics))
            {
                return EnumeratorResult.FailedAndReported;
            }

            CSharpSyntaxNode errorLocationSyntax = _syntax.Expression;

            if (foundMultipleGenericIEnumerableInterfaces)
            {
                diagnostics.Add(isAsync ? ErrorCode.ERR_MultipleIAsyncEnumOfT : ErrorCode.ERR_MultipleIEnumOfT, errorLocationSyntax.Location, unwrappedCollectionExprType,
                    isAsync ?
                        this.Compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T) :
                        this.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T));
                return EnumeratorResult.FailedAndReported;
            }

            Debug.Assert((object)builder.CollectionType != null);

            NamedTypeSymbol collectionType = (NamedTypeSymbol)builder.CollectionType;
            if (collectionType.IsGenericType)
            {
                // If the type is generic, we have to search for the methods
                builder.ElementTypeWithAnnotations = collectionType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Single();

                MethodSymbol getEnumeratorMethod;
                if (isAsync)
                {
                    Debug.Assert(IsIAsyncEnumerable(collectionType.OriginalDefinition));

                    getEnumeratorMethod = (MethodSymbol)GetWellKnownTypeMember(Compilation, WellKnownMember.System_Collections_Generic_IAsyncEnumerable_T__GetAsyncEnumerator,
                        diagnostics, errorLocationSyntax.Location, isOptional: false);

                    // Well-known members are matched by signature: we shouldn't find it if it doesn't have exactly 1 parameter.
                    Debug.Assert(getEnumeratorMethod is null or { ParameterCount: 1 });

                    if (getEnumeratorMethod?.Parameters[0].IsOptional == false)
                    {
                        // This indicates a problem with the well-known IAsyncEnumerable type - it should have an optional cancellation token.
                        diagnostics.Add(ErrorCode.ERR_AwaitForEachMissingMember, _syntax.Expression.Location, unwrappedCollectionExprType, GetAsyncEnumeratorMethodName);
                        return EnumeratorResult.FailedAndReported;
                    }
                }
                else
                {
                    Debug.Assert(collectionType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
                    getEnumeratorMethod = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IEnumerable_T__GetEnumerator, diagnostics, errorLocationSyntax);
                }

                MethodSymbol moveNextMethod = null;
                if ((object)getEnumeratorMethod != null)
                {
                    MethodSymbol specificGetEnumeratorMethod = getEnumeratorMethod.AsMember(collectionType);
                    TypeSymbol enumeratorType = specificGetEnumeratorMethod.ReturnType;

                    // IAsyncEnumerable<T>.GetAsyncEnumerator has a default param, so let's fill it in
                    builder.GetEnumeratorInfo = BindDefaultArguments(
                        specificGetEnumeratorMethod,
                        extensionReceiverOpt: null,
                        expanded: false,
                        collectionExpr.Syntax,
                        diagnostics,
                        // C# 8 shipped allowing the CancellationToken of `IAsyncEnumerable.GetAsyncEnumerator` to be non-optional,
                        // filling in a default value in that case. https://github.com/dotnet/roslyn/issues/50182 tracks making
                        // this an error and breaking the scenario.
                        assertMissingParametersAreOptional: false);

                    MethodSymbol currentPropertyGetter;
                    if (isAsync)
                    {
                        Debug.Assert(enumeratorType.OriginalDefinition.Equals(Compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T)));

                        MethodSymbol moveNextAsync = (MethodSymbol)GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__MoveNextAsync,
                            diagnostics, errorLocationSyntax.Location, isOptional: false);

                        if ((object)moveNextAsync != null)
                        {
                            moveNextMethod = moveNextAsync.AsMember((NamedTypeSymbol)enumeratorType);
                        }

                        currentPropertyGetter = (MethodSymbol)GetWellKnownTypeMember(Compilation, WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__get_Current, diagnostics, errorLocationSyntax.Location, isOptional: false);
                    }
                    else
                    {
                        currentPropertyGetter = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IEnumerator_T__get_Current, diagnostics, errorLocationSyntax);
                    }

                    if ((object)currentPropertyGetter != null)
                    {
                        builder.CurrentPropertyGetter = currentPropertyGetter.AsMember((NamedTypeSymbol)enumeratorType);
                    }
                }

                if (!isAsync)
                {
                    // NOTE: MoveNext is actually inherited from System.Collections.IEnumerator
                    moveNextMethod = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext, diagnostics, errorLocationSyntax);
                }

                // We're operating with well-known members: we know MoveNext/MoveNextAsync have no parameters
                if (moveNextMethod is not null)
                {
                    builder.MoveNextInfo = MethodArgumentInfo.CreateParameterlessMethod(moveNextMethod);
                }
            }
            else
            {
                // Non-generic - use special members to avoid re-computing
                Debug.Assert(collectionType.SpecialType == SpecialType.System_Collections_IEnumerable);

                builder.GetEnumeratorInfo = GetParameterlessSpecialTypeMemberInfo(SpecialMember.System_Collections_IEnumerable__GetEnumerator, errorLocationSyntax, diagnostics);
                builder.CurrentPropertyGetter = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__get_Current, diagnostics, errorLocationSyntax);
                builder.MoveNextInfo = GetParameterlessSpecialTypeMemberInfo(SpecialMember.System_Collections_IEnumerator__MoveNext, errorLocationSyntax, diagnostics);
                builder.ElementTypeWithAnnotations = builder.CurrentPropertyGetter?.ReturnTypeWithAnnotations ?? TypeWithAnnotations.Create(GetSpecialType(SpecialType.System_Object, diagnostics, errorLocationSyntax));

                Debug.Assert((object)builder.GetEnumeratorInfo == null ||
                    builder.GetEnumeratorInfo.Method.ReturnType.SpecialType == SpecialType.System_Collections_IEnumerator);
            }

            // We don't know the runtime type, so we will have to insert a runtime check for IDisposable (with a conditional call to IDisposable.Dispose).
            builder.NeedsDisposal = true;
            return EnumeratorResult.Succeeded;
        }

        private bool ReportConstantNullCollectionExpr(BoundExpression collectionExpr, BindingDiagnosticBag diagnostics)
        {
            if (collectionExpr.ConstantValue is { IsNull: true })
            {
                // Spec seems to refer to null literals, but Dev10 reports anything known to be null.
                diagnostics.Add(ErrorCode.ERR_NullNotValid, _syntax.Expression.Location);
                return true;
            }
            return false;
        }

        private void GetDisposalInfoForEnumerator(ref ForEachEnumeratorInfo.Builder builder, BoundExpression expr, bool isAsync, BindingDiagnosticBag diagnostics)
        {
            // NOTE: if IDisposable is not available at all, no diagnostics will be reported - we will just assume that
            // the enumerator is not disposable.  If it has IDisposable in its interface list, there will be a diagnostic there.
            // If IDisposable is available but its Dispose method is not, then diagnostics will be reported only if the enumerator
            // is potentially disposable.

            TypeSymbol enumeratorType = builder.GetEnumeratorInfo.Method.ReturnType;
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);

            MethodSymbol patternDisposeMethod = null;
            if (enumeratorType.IsRefLikeType || isAsync)
            {
                // we throw away any binding diagnostics, and assume it's not disposable if we encounter errors
                var receiver = new BoundDisposableValuePlaceholder(_syntax, enumeratorType);
                patternDisposeMethod = TryFindDisposePatternMethod(receiver, _syntax, isAsync, BindingDiagnosticBag.Discarded);
                if (patternDisposeMethod is object)
                {
                    Debug.Assert(!patternDisposeMethod.IsExtensionMethod);
                    Debug.Assert(patternDisposeMethod.ParameterRefKinds.IsDefaultOrEmpty);

                    var argsBuilder = ArrayBuilder<BoundExpression>.GetInstance(patternDisposeMethod.ParameterCount);
                    var argsToParams = default(ImmutableArray<int>);
                    bool expanded = patternDisposeMethod.HasParamsParameter();

                    BindDefaultArguments(
                        _syntax,
                        patternDisposeMethod.Parameters,
                        argsBuilder,
                        argumentRefKindsBuilder: null,
                        ref argsToParams,
                        out BitVector defaultArguments,
                        expanded,
                        enableCallerInfo: true,
                        diagnostics);

                    builder.NeedsDisposal = true;
                    builder.PatternDisposeInfo = new MethodArgumentInfo(patternDisposeMethod, argsBuilder.ToImmutableAndFree(), argsToParams, defaultArguments, expanded);

                    if (!isAsync)
                    {
                        // We already checked feature availability for async scenarios
                        CheckFeatureAvailability(expr.Syntax, MessageID.IDS_FeatureDisposalPattern, diagnostics);
                    }
                }
            }

            if (!enumeratorType.IsRefLikeType && patternDisposeMethod is null)
            {
                // If it wasn't pattern-disposable, see if it's directly convertable to IDisposable
                // For async foreach, we don't do the runtime check in unsealed case
                if ((!enumeratorType.IsSealed && !isAsync) ||
                    this.Conversions.ClassifyImplicitConversionFromType(enumeratorType,
                        isAsync ? this.Compilation.GetWellKnownType(WellKnownType.System_IAsyncDisposable) : this.Compilation.GetSpecialType(SpecialType.System_IDisposable),
                        ref useSiteInfo).IsImplicit)
                {
                    builder.NeedsDisposal = true;
                }

                diagnostics.Add(_syntax, useSiteInfo);
            }
        }

        private ForEachEnumeratorInfo.Builder GetDefaultEnumeratorInfo(ForEachEnumeratorInfo.Builder builder, BindingDiagnosticBag diagnostics, TypeSymbol collectionExprType)
        {
            // NOTE: for arrays, we won't actually use any of these members - they're just for the API.
            builder.CollectionType = GetSpecialType(SpecialType.System_Collections_IEnumerable, diagnostics, _syntax);

            if (collectionExprType.IsDynamic())
            {
                builder.ElementTypeWithAnnotations = TypeWithAnnotations.Create(
                    ((_syntax as ForEachStatementSyntax)?.Type.IsVar == true) ?
                        (TypeSymbol)DynamicTypeSymbol.Instance :
                        GetSpecialType(SpecialType.System_Object, diagnostics, _syntax));
            }
            else
            {
                builder.ElementTypeWithAnnotations = collectionExprType.SpecialType == SpecialType.System_String ?
                    TypeWithAnnotations.Create(GetSpecialType(SpecialType.System_Char, diagnostics, _syntax)) :
                    ((ArrayTypeSymbol)collectionExprType).ElementTypeWithAnnotations;
            }

            // CONSIDER: 
            // For arrays and string none of these members will actually be emitted, so it seems strange to prevent compilation if they can't be found.
            // skip this work in the batch case?
            builder.GetEnumeratorInfo = GetParameterlessSpecialTypeMemberInfo(SpecialMember.System_Collections_IEnumerable__GetEnumerator, _syntax, diagnostics);
            builder.CurrentPropertyGetter = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__get_Current, diagnostics, _syntax);
            builder.MoveNextInfo = GetParameterlessSpecialTypeMemberInfo(SpecialMember.System_Collections_IEnumerator__MoveNext, _syntax, diagnostics);

            Debug.Assert((object)builder.GetEnumeratorInfo == null ||
                TypeSymbol.Equals(builder.GetEnumeratorInfo.Method.ReturnType, this.Compilation.GetSpecialType(SpecialType.System_Collections_IEnumerator), TypeCompareKind.ConsiderEverything2));

            // We don't know the runtime type, so we will have to insert a runtime check for IDisposable (with a conditional call to IDisposable.Dispose).
            builder.NeedsDisposal = true;
            return builder;
        }

        /// <summary>
        /// Check for a GetEnumerator (or GetAsyncEnumerator) method on collectionExprType.  Failing to satisfy the pattern is not an error -
        /// it just means that we have to check for an interface instead.
        /// </summary>
        /// <param name="collectionExpr">Expression over which to iterate.</param>
        /// <param name="diagnostics">Populated with *warnings* if there are near misses.</param>
        /// <param name="builder">Builder to fill in. <see cref="ForEachEnumeratorInfo.Builder.GetEnumeratorInfo"/> set if the pattern in satisfied.</param>
        /// <returns>True if the method was found (still have to verify that the return (i.e. enumerator) type is acceptable).</returns>
        /// <remarks>
        /// Only adds warnings, so does not affect control flow (i.e. no need to check for failure).
        /// </remarks>
        private bool SatisfiesGetEnumeratorPattern(ref ForEachEnumeratorInfo.Builder builder, BoundExpression collectionExpr, bool isAsync, bool viaExtensionMethod, BindingDiagnosticBag diagnostics)
        {
            string methodName = isAsync ? GetAsyncEnumeratorMethodName : GetEnumeratorMethodName;
            MethodArgumentInfo getEnumeratorInfo;
            if (viaExtensionMethod)
            {
                getEnumeratorInfo = FindForEachPatternMethodViaExtension(collectionExpr, methodName, diagnostics);
            }
            else
            {
                var lookupResult = LookupResult.GetInstance();
                getEnumeratorInfo = FindForEachPatternMethod(collectionExpr.Type, methodName, lookupResult, warningsOnly: true, diagnostics, isAsync);
                lookupResult.Free();
            }

            builder.GetEnumeratorInfo = getEnumeratorInfo;
            return (object)getEnumeratorInfo != null;
        }

        /// <summary>
        /// Perform a lookup for the specified method on the specified type.  Perform overload resolution
        /// on the lookup results.
        /// </summary>
        /// <param name="patternType">Type to search.</param>
        /// <param name="methodName">Method to search for.</param>
        /// <param name="lookupResult">Passed in for reusability.</param>
        /// <param name="warningsOnly">True if failures should result in warnings; false if they should result in errors.</param>
        /// <param name="diagnostics">Populated with binding diagnostics.</param>
        /// <returns>The desired method or null.</returns>
        private MethodArgumentInfo FindForEachPatternMethod(TypeSymbol patternType, string methodName, LookupResult lookupResult, bool warningsOnly, BindingDiagnosticBag diagnostics, bool isAsync)
        {
            Debug.Assert(lookupResult.IsClear);

            // Not using LookupOptions.MustBeInvocableMember because we don't want the corresponding lookup error.
            // We filter out non-methods below.
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            this.LookupMembersInType(
                lookupResult,
                patternType,
                methodName,
                arity: 0,
                basesBeingResolved: null,
                options: LookupOptions.Default,
                originalBinder: this,
                diagnose: false,
                useSiteInfo: ref useSiteInfo);

            diagnostics.Add(_syntax.Expression, useSiteInfo);

            if (!lookupResult.IsMultiViable)
            {
                ReportPatternMemberLookupDiagnostics(lookupResult, patternType, methodName, warningsOnly, diagnostics);
                return null;
            }

            ArrayBuilder<MethodSymbol> candidateMethods = ArrayBuilder<MethodSymbol>.GetInstance();

            foreach (Symbol member in lookupResult.Symbols)
            {
                if (member.Kind != SymbolKind.Method)
                {
                    candidateMethods.Free();

                    if (warningsOnly)
                    {
                        ReportEnumerableWarning(diagnostics, patternType, member);
                    }
                    return null;
                }

                MethodSymbol method = (MethodSymbol)member;

                // SPEC VIOLATION: The spec says we should apply overload resolution, but Dev10 uses
                // some custom logic in ExpressionBinder.BindGrpToParams.  The biggest difference
                // we've found (so far) is that it only considers methods with expected number of parameters
                // (i.e. doesn't work with "params" or optional parameters).

                // Note: for pattern-based lookup for `await foreach` we accept `GetAsyncEnumerator` and
                // `MoveNextAsync` methods with optional/params parameters.
                if (method.ParameterCount == 0 || isAsync)
                {
                    candidateMethods.Add((MethodSymbol)member);
                }
            }

            MethodArgumentInfo patternInfo = PerformForEachPatternOverloadResolution(patternType, candidateMethods, warningsOnly, diagnostics, isAsync);

            candidateMethods.Free();

            return patternInfo;
        }

        /// <summary>
        /// The overload resolution portion of FindForEachPatternMethod.
        /// If no arguments are passed in, then an empty argument list will be used.
        /// </summary>
        private MethodArgumentInfo PerformForEachPatternOverloadResolution(TypeSymbol patternType, ArrayBuilder<MethodSymbol> candidateMethods, bool warningsOnly, BindingDiagnosticBag diagnostics, bool isAsync)
        {
            var analyzedArguments = AnalyzedArguments.GetInstance();
            var typeArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            var overloadResolutionResult = OverloadResolutionResult<MethodSymbol>.GetInstance();

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            // We create a dummy receiver of the invocation so MethodInvocationOverloadResolution knows it was invoked from an instance, not a type
            var dummyReceiver = new BoundImplicitReceiver(_syntax.Expression, patternType);
            this.OverloadResolution.MethodInvocationOverloadResolution(
                methods: candidateMethods,
                typeArguments: typeArguments,
                receiver: dummyReceiver,
                arguments: analyzedArguments,
                result: overloadResolutionResult,
                useSiteInfo: ref useSiteInfo);
            diagnostics.Add(_syntax.Expression, useSiteInfo);

            MethodSymbol result = null;
            MethodArgumentInfo info = null;

            if (overloadResolutionResult.Succeeded)
            {
                result = overloadResolutionResult.ValidResult.Member;

                if (result.IsStatic || result.DeclaredAccessibility != Accessibility.Public)
                {
                    if (warningsOnly)
                    {
                        MessageID patternName = isAsync ? MessageID.IDS_FeatureAsyncStreams : MessageID.IDS_Collection;
                        diagnostics.Add(ErrorCode.WRN_PatternNotPublicOrNotInstance, _syntax.Expression.Location, patternType, patternName.Localize(), result);
                    }
                    result = null;
                }
                else if (result.CallsAreOmitted(_syntax.SyntaxTree))
                {
                    // Calls to this method are omitted in the current syntax tree, i.e it is either a partial method with no implementation part OR a conditional method whose condition is not true in this source file.
                    // We don't want to allow this case.
                    result = null;
                }
                else
                {
                    var argsToParams = overloadResolutionResult.ValidResult.Result.ArgsToParamsOpt;
                    var expanded = overloadResolutionResult.ValidResult.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm;
                    BindDefaultArguments(
                        _syntax,
                        result.Parameters,
                        analyzedArguments.Arguments,
                        analyzedArguments.RefKinds,
                        ref argsToParams,
                        out BitVector defaultArguments,
                        expanded,
                        enableCallerInfo: true,
                        diagnostics);

                    info = new MethodArgumentInfo(result, analyzedArguments.Arguments.ToImmutable(), argsToParams, defaultArguments, expanded);
                }
            }
            else if (overloadResolutionResult.GetAllApplicableMembers() is var applicableMembers && applicableMembers.Length > 1)
            {
                if (warningsOnly)
                {
                    diagnostics.Add(ErrorCode.WRN_PatternIsAmbiguous, _syntax.Expression.Location, patternType, MessageID.IDS_Collection.Localize(),
                        applicableMembers[0], applicableMembers[1]);
                }
            }

            overloadResolutionResult.Free();
            analyzedArguments.Free();
            typeArguments.Free();

            return info;
        }

        private MethodArgumentInfo FindForEachPatternMethodViaExtension(BoundExpression collectionExpr, string methodName, BindingDiagnosticBag diagnostics)
        {
            var analyzedArguments = AnalyzedArguments.GetInstance();

            var methodGroupResolutionResult = this.BindExtensionMethod(
                _syntax.Expression,
                methodName,
                analyzedArguments,
                collectionExpr,
                typeArgumentsWithAnnotations: default,
                isMethodGroupConversion: false,
                returnRefKind: default,
                returnType: null,
                withDependencies: diagnostics.AccumulatesDependencies);

            diagnostics.AddRange(methodGroupResolutionResult.Diagnostics);

            var overloadResolutionResult = methodGroupResolutionResult.OverloadResolutionResult;
            if (overloadResolutionResult?.Succeeded ?? false)
            {
                var result = overloadResolutionResult.ValidResult.Member;

                if (result.CallsAreOmitted(_syntax.SyntaxTree))
                {
                    // Calls to this method are omitted in the current syntax tree, i.e it is either a partial method with no implementation part OR a conditional method whose condition is not true in this source file.
                    // We don't want to allow this case.
                    methodGroupResolutionResult.Free();
                    analyzedArguments.Free();
                    return null;
                }

                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                var collectionConversion = this.Conversions.ClassifyConversionFromExpression(collectionExpr, result.Parameters[0].Type, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);
                diagnostics.Add(_syntax, useSiteInfo);

                // Unconditionally convert here, to match what we set the ConvertedExpression to in the main BoundForEachStatement node.
                Debug.Assert(!collectionConversion.IsUserDefined);
                collectionExpr = new BoundConversion(
                    collectionExpr.Syntax,
                    collectionExpr,
                    collectionConversion,
                    @checked: CheckOverflowAtRuntime,
                    explicitCastInCode: false,
                    conversionGroupOpt: null,
                    ConstantValue.NotAvailable,
                    result.Parameters[0].Type);

                var info = BindDefaultArguments(
                    result,
                    collectionExpr,
                    expanded: overloadResolutionResult.ValidResult.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm,
                    collectionExpr.Syntax,
                    diagnostics);
                methodGroupResolutionResult.Free();
                analyzedArguments.Free();
                return info;
            }
            else if (overloadResolutionResult?.GetAllApplicableMembers() is { } applicableMembers && applicableMembers.Length > 1)
            {
                diagnostics.Add(ErrorCode.WRN_PatternIsAmbiguous, _syntax.Expression.Location, collectionExpr.Type, MessageID.IDS_Collection.Localize(),
                    applicableMembers[0], applicableMembers[1]);
            }
            else if (overloadResolutionResult != null)
            {
                overloadResolutionResult.ReportDiagnostics(
                    binder: this,
                    location: _syntax.Expression.Location,
                    nodeOpt: _syntax.Expression,
                    diagnostics: diagnostics,
                    name: methodName,
                    receiver: null,
                    invokedExpression: _syntax.Expression,
                    arguments: methodGroupResolutionResult.AnalyzedArguments,
                    memberGroup: methodGroupResolutionResult.MethodGroup.Methods.ToImmutable(),
                    typeContainingConstructor: null,
                    delegateTypeBeingInvoked: null);
            }

            methodGroupResolutionResult.Free();
            analyzedArguments.Free();
            return null;
        }

        /// <summary>
        /// Called after it is determined that the expression being enumerated is of a type that
        /// has a GetEnumerator (or GetAsyncEnumerator) method.  Checks to see if the return type of the GetEnumerator
        /// method is suitable (i.e. has Current and MoveNext for regular case, 
        /// or Current and MoveNextAsync for async case).
        /// </summary>
        /// <param name="builder">Must be non-null and contain a non-null GetEnumeratorMethod.</param>
        /// <param name="diagnostics">Will be populated with pattern diagnostics.</param>
        /// <returns>True if the return type has suitable members.</returns>
        /// <remarks>
        /// It seems that every failure path reports the same diagnostics, so that is left to the caller.
        /// </remarks>
        private bool SatisfiesForEachPattern(ref ForEachEnumeratorInfo.Builder builder, bool isAsync, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert((object)builder.GetEnumeratorInfo.Method != null);

            MethodSymbol getEnumeratorMethod = builder.GetEnumeratorInfo.Method;
            TypeSymbol enumeratorType = getEnumeratorMethod.ReturnType;

            switch (enumeratorType.TypeKind)
            {
                case TypeKind.Class:
                case TypeKind.Struct:
                case TypeKind.Interface:
                case TypeKind.TypeParameter: // Not specifically mentioned in the spec, but consistent with Dev10.
                case TypeKind.Dynamic: // Not specifically mentioned in the spec, but consistent with Dev10.
                    break;

                case TypeKind.Submission:
                    // submission class is synthesized and should never appear in a foreach:
                    throw ExceptionUtilities.UnexpectedValue(enumeratorType.TypeKind);

                default:
                    return false;
            }

            // Use a try-finally since there are many return points
            LookupResult lookupResult = LookupResult.GetInstance();
            try
            {
                // If we searched for the accessor directly, we could reuse FindForEachPatternMethod and we
                // wouldn't have to mangle CurrentPropertyName.  However, Dev10 searches for the property and
                // then extracts the accessor, so we should do the same (in case of accessors with non-standard
                // names).
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                this.LookupMembersInType(
                    lookupResult,
                    enumeratorType,
                    CurrentPropertyName,
                    arity: 0,
                    basesBeingResolved: null,
                    options: LookupOptions.Default, // properties are not invocable - their accessors are
                    originalBinder: this,
                    diagnose: false,
                    useSiteInfo: ref useSiteInfo);

                diagnostics.Add(_syntax.Expression, useSiteInfo);
                useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(useSiteInfo);

                if (!lookupResult.IsSingleViable)
                {
                    ReportPatternMemberLookupDiagnostics(lookupResult, enumeratorType, CurrentPropertyName, warningsOnly: false, diagnostics: diagnostics);
                    return false;
                }

                // lookupResult.IsSingleViable above guaranteed there is exactly one symbol.
                Symbol lookupSymbol = lookupResult.SingleSymbolOrDefault;
                Debug.Assert((object)lookupSymbol != null);

                if (lookupSymbol.IsStatic || lookupSymbol.DeclaredAccessibility != Accessibility.Public || lookupSymbol.Kind != SymbolKind.Property)
                {
                    return false;
                }

                // NOTE: accessor can be inherited from overridden property
                MethodSymbol currentPropertyGetterCandidate = ((PropertySymbol)lookupSymbol).GetOwnOrInheritedGetMethod();

                if ((object)currentPropertyGetterCandidate == null)
                {
                    return false;
                }
                else
                {
                    bool isAccessible = this.IsAccessible(currentPropertyGetterCandidate, ref useSiteInfo);
                    diagnostics.Add(_syntax.Expression, useSiteInfo);

                    if (!isAccessible)
                    {
                        // NOTE: per Dev10 and the spec, the property has to be public, but the accessor just has to be accessible
                        return false;
                    }
                }

                builder.CurrentPropertyGetter = currentPropertyGetterCandidate;

                lookupResult.Clear(); // Reuse the same LookupResult

                MethodArgumentInfo moveNextMethodCandidate = FindForEachPatternMethod(enumeratorType,
                    isAsync ? MoveNextAsyncMethodName : MoveNextMethodName,
                    lookupResult, warningsOnly: false, diagnostics, isAsync);

                if ((object)moveNextMethodCandidate == null ||
                    moveNextMethodCandidate.Method.IsStatic || moveNextMethodCandidate.Method.DeclaredAccessibility != Accessibility.Public ||
                    IsInvalidMoveNextMethod(moveNextMethodCandidate.Method, isAsync))
                {
                    return false;
                }

                builder.MoveNextInfo = moveNextMethodCandidate;

                return true;
            }
            finally
            {
                lookupResult.Free();
            }
        }

        private bool IsInvalidMoveNextMethod(MethodSymbol moveNextMethodCandidate, bool isAsync)
        {
            if (isAsync)
            {
                // We'll verify the return type from `MoveNextAsync` when we try to bind the `await` for it
                return false;
            }

            // SPEC VIOLATION: Dev10 checks the return type of the original definition, rather than the return type of the actual method.
            return moveNextMethodCandidate.OriginalDefinition.ReturnType.SpecialType != SpecialType.System_Boolean;
        }

        private void ReportEnumerableWarning(BindingDiagnosticBag diagnostics, TypeSymbol enumeratorType, Symbol patternMemberCandidate)
        {
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            if (this.IsAccessible(patternMemberCandidate, ref useSiteInfo))
            {
                diagnostics.Add(ErrorCode.WRN_PatternBadSignature, _syntax.Expression.Location, enumeratorType, MessageID.IDS_Collection.Localize(), patternMemberCandidate);
            }

            diagnostics.Add(_syntax.Expression, useSiteInfo);
        }

        private static bool IsIEnumerable(TypeSymbol type)
        {
            switch (((TypeSymbol)type.OriginalDefinition).SpecialType)
            {
                case SpecialType.System_Collections_IEnumerable:
                case SpecialType.System_Collections_Generic_IEnumerable_T:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsIAsyncEnumerable(TypeSymbol type)
        {
            return type.OriginalDefinition.Equals(Compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T));
        }

        /// <summary>
        /// Checks if the given type implements (or extends, in the case of an interface),
        /// System.Collections.IEnumerable or System.Collections.Generic.IEnumerable&lt;T&gt;,
        /// (or System.Collections.Generic.IAsyncEnumerable&lt;T&gt;)
        /// for at least one T.
        /// </summary>
        /// <param name="builder">builder to fill in CollectionType.</param>
        /// <param name="type">Type to check.</param>
        /// <param name="diagnostics" />
        /// <param name="foundMultiple">True if multiple T's are found.</param>
        /// <returns>True if some IEnumerable is found (may still be ambiguous).</returns>
        private bool AllInterfacesContainsIEnumerable(
            ref ForEachEnumeratorInfo.Builder builder,
            TypeSymbol type,
            bool isAsync,
            BindingDiagnosticBag diagnostics,
            out bool foundMultiple)
        {
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            NamedTypeSymbol implementedIEnumerable = GetIEnumerableOfT(type, isAsync, Compilation, ref useSiteInfo, out foundMultiple);

            // Prefer generic to non-generic, unless it is inaccessible.
            if (((object)implementedIEnumerable == null) || !this.IsAccessible(implementedIEnumerable, ref useSiteInfo))
            {
                implementedIEnumerable = null;

                if (!isAsync)
                {
                    var implementedNonGeneric = this.Compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
                    if ((object)implementedNonGeneric != null)
                    {
                        var conversion = this.Conversions.ClassifyImplicitConversionFromType(type, implementedNonGeneric, ref useSiteInfo);
                        if (conversion.IsImplicit)
                        {
                            implementedIEnumerable = implementedNonGeneric;
                        }
                    }
                }
            }

            diagnostics.Add(_syntax.Expression, useSiteInfo);

            builder.CollectionType = implementedIEnumerable;
            return (object)implementedIEnumerable != null;
        }

        internal static NamedTypeSymbol GetIEnumerableOfT(TypeSymbol type, bool isAsync, CSharpCompilation compilation, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo, out bool foundMultiple)
        {
            NamedTypeSymbol implementedIEnumerable = null;
            foundMultiple = false;

            if (type.TypeKind == TypeKind.TypeParameter)
            {
                var typeParameter = (TypeParameterSymbol)type;
                var allInterfaces = typeParameter.EffectiveBaseClass(ref useSiteInfo).AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteInfo)
                    .Concat(typeParameter.AllEffectiveInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteInfo));
                GetIEnumerableOfT(allInterfaces, isAsync, compilation, ref @implementedIEnumerable, ref foundMultiple);
            }
            else
            {
                GetIEnumerableOfT(type.AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteInfo), isAsync, compilation, ref @implementedIEnumerable, ref foundMultiple);
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

        /// <summary>
        /// Report appropriate diagnostics when lookup of a pattern member (i.e. GetEnumerator, Current, or MoveNext) fails.
        /// </summary>
        /// <param name="lookupResult">Failed lookup result.</param>
        /// <param name="patternType">Type in which member was looked up.</param>
        /// <param name="memberName">Name of looked up member.</param>
        /// <param name="warningsOnly">True if failures should result in warnings; false if they should result in errors.</param>
        /// <param name="diagnostics">Populated appropriately.</param>
        private void ReportPatternMemberLookupDiagnostics(LookupResult lookupResult, TypeSymbol patternType, string memberName, bool warningsOnly, BindingDiagnosticBag diagnostics)
        {
            if (lookupResult.Symbols.Any())
            {
                if (warningsOnly)
                {
                    ReportEnumerableWarning(diagnostics, patternType, lookupResult.Symbols.First());
                }
                else
                {
                    lookupResult.Clear();

                    CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                    this.LookupMembersInType(
                        lookupResult,
                        patternType,
                        memberName,
                        arity: 0,
                        basesBeingResolved: null,
                        options: LookupOptions.Default,
                        originalBinder: this,
                        diagnose: true,
                        useSiteInfo: ref useSiteInfo);

                    diagnostics.Add(_syntax.Expression, useSiteInfo);

                    if (lookupResult.Error != null)
                    {
                        diagnostics.Add(lookupResult.Error, _syntax.Expression.Location);
                    }
                }
            }
            else if (!warningsOnly)
            {
                diagnostics.Add(ErrorCode.ERR_NoSuchMember, _syntax.Expression.Location, patternType, memberName);
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

        private MethodArgumentInfo GetParameterlessSpecialTypeMemberInfo(SpecialMember member, SyntaxNode syntax, BindingDiagnosticBag diagnostics)
        {
            var resolvedMember = (MethodSymbol)GetSpecialTypeMember(member, diagnostics, syntax);
            Debug.Assert(resolvedMember is null or { ParameterCount: 0 });
            return resolvedMember is not null
                    ? MethodArgumentInfo.CreateParameterlessMethod(resolvedMember)
                    : null;
        }

        /// <param name="extensionReceiverOpt">If method is an extension method, this must be non-null.</param>
        private MethodArgumentInfo BindDefaultArguments(MethodSymbol method, BoundExpression extensionReceiverOpt, bool expanded, SyntaxNode syntax, BindingDiagnosticBag diagnostics, bool assertMissingParametersAreOptional = true)
        {
            Debug.Assert((extensionReceiverOpt != null) == method.IsExtensionMethod);

            if (method.ParameterCount == 0)
            {
                return MethodArgumentInfo.CreateParameterlessMethod(method);
            }

            var argsBuilder = ArrayBuilder<BoundExpression>.GetInstance(method.ParameterCount);

            if (method.IsExtensionMethod)
            {
                argsBuilder.Add(extensionReceiverOpt);
            }

            ImmutableArray<int> argsToParams = default;
            BindDefaultArguments(
                syntax,
                method.Parameters,
                argsBuilder,
                argumentRefKindsBuilder: null,
                ref argsToParams,
                defaultArguments: out BitVector defaultArguments,
                expanded,
                enableCallerInfo: true,
                diagnostics,
                assertMissingParametersAreOptional);

            return new MethodArgumentInfo(method, argsBuilder.ToImmutableAndFree(), argsToParams, defaultArguments, expanded);
        }
    }
}
