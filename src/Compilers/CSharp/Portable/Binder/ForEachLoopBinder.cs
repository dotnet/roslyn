// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System.Collections.Generic;

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

        private readonly CommonForEachStatementSyntax _syntax;
        private SourceLocalSymbol IterationVariable
        {
            get
            {
                return (_syntax.Kind() == SyntaxKind.ForEachStatement) ? (SourceLocalSymbol)this.Locals[0] : null;
            }
        }

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
            BoundExpression collectionExpr = originalBinder.GetBinder(_syntax.Expression).BindValue(_syntax.Expression, diagnostics, BindValueKind.RValue);

            ForEachEnumeratorInfo.Builder builder = new ForEachEnumeratorInfo.Builder();
            TypeSymbol inferredType;
            bool hasErrors = !GetEnumeratorInfoAndInferCollectionElementType(ref builder, ref collectionExpr, diagnostics, out inferredType);

            ExpressionSyntax variables = ((ForEachVariableStatementSyntax)_syntax).Variable;

            // Tracking narrowest safe-to-escape scope by default, the proper val escape will be set when doing full binding of the foreach statement
            var valuePlaceholder = new BoundDeconstructValuePlaceholder(_syntax.Expression, this.LocalScopeDepth, inferredType ?? CreateErrorType("var"));

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
            BoundExpression collectionExpr = originalBinder.GetBinder(_syntax.Expression).BindValue(_syntax.Expression, diagnostics, BindValueKind.RValue);

            ForEachEnumeratorInfo.Builder builder = new ForEachEnumeratorInfo.Builder();
            TypeSymbol inferredType;
            bool hasErrors = !GetEnumeratorInfoAndInferCollectionElementType(ref builder, ref collectionExpr, diagnostics, out inferredType);

            // These should only occur when special types are missing or malformed.
            hasErrors = hasErrors ||
                (object)builder.GetEnumeratorMethod == null ||
                (object)builder.MoveNextMethod == null ||
                (object)builder.CurrentPropertyGetter == null;

            TypeSymbol iterationVariableType;
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
                        TypeSymbol declType = BindTypeOrVarKeyword(typeSyntax, diagnostics, out isVar, out alias);

                        if (isVar)
                        {
                            iterationVariableType = inferredType ?? CreateErrorType("var");
                        }
                        else
                        {
                            Debug.Assert((object)declType != null);
                            iterationVariableType = declType;
                        }

                        boundIterationVariableType = new BoundTypeExpression(typeSyntax, alias, iterationVariableType);

                        SourceLocalSymbol local = this.IterationVariable;
                        local.SetType(iterationVariableType);
                        local.SetValEscape(collectionEscape);

                        if (local.RefKind != RefKind.None)
                        {
                            // The ref-escape of a ref-returning property is decided
                            // by the value escape of its receiverm, in this case the
                            // collection
                            local.SetRefEscape(collectionEscape);

                            if (IsDirectlyInIterator)
                            {
                                diagnostics.Add(ErrorCode.ERR_BadIteratorLocalType, local.IdentifierToken.GetLocation());
                            }
                            else if (IsInAsyncMethod())
                            {
                                diagnostics.Add(ErrorCode.ERR_BadAsyncLocalType, local.IdentifierToken.GetLocation());
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
                        iterationVariableType = inferredType ?? CreateErrorType("var");

                        var variables = node.Variable;
                        if (variables.IsDeconstructionLeft())
                        {
                            var valuePlaceholder = new BoundDeconstructValuePlaceholder(_syntax.Expression, collectionEscape, iterationVariableType).MakeCompilerGenerated();
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

                        boundIterationVariableType = new BoundTypeExpression(variables, aliasOpt: null, type: iterationVariableType).MakeCompilerGenerated();
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

            hasErrors = hasErrors || boundIterationVariableType.HasErrors || iterationVariableType.IsErrorType();

            // Skip the conversion checks and array/enumerator differentiation if we know we have an error (except local name conflicts).
            if (hasErrors)
            {
                return new BoundForEachStatement(
                    _syntax,
                    null, // can't be sure that it's complete
                    default(Conversion),
                    boundIterationVariableType,
                    iterationVariables,
                    iterationErrorExpression,
                    collectionExpr,
                    deconstructStep,
                    body,
                    CheckOverflowAtRuntime,
                    this.BreakLabel,
                    this.ContinueLabel,
                    hasErrors);
            }

            hasErrors |= hasNameConflicts;

            var foreachKeyword = _syntax.ForEachKeyword;
            ReportDiagnosticsIfObsolete(diagnostics, builder.GetEnumeratorMethod, foreachKeyword, hasBaseReceiver: false);
            ReportDiagnosticsIfObsolete(diagnostics, builder.MoveNextMethod, foreachKeyword, hasBaseReceiver: false);
            ReportDiagnosticsIfObsolete(diagnostics, builder.CurrentPropertyGetter, foreachKeyword, hasBaseReceiver: false);
            ReportDiagnosticsIfObsolete(diagnostics, builder.CurrentPropertyGetter.AssociatedSymbol, foreachKeyword, hasBaseReceiver: false);

            // We want to convert from inferredType in the array/string case and builder.ElementType in the enumerator case,
            // but it turns out that these are equivalent (when both are available).

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            Conversion elementConversion = this.Conversions.ClassifyConversionFromType(inferredType, iterationVariableType, ref useSiteDiagnostics, forCast: true);

            if (!elementConversion.IsValid)
            {
                ImmutableArray<MethodSymbol> originalUserDefinedConversions = elementConversion.OriginalUserDefinedConversions;
                if (originalUserDefinedConversions.Length > 1)
                {
                    diagnostics.Add(ErrorCode.ERR_AmbigUDConv, foreachKeyword.GetLocation(), originalUserDefinedConversions[0], originalUserDefinedConversions[1], inferredType, iterationVariableType);
                }
                else
                {
                    SymbolDistinguisher distinguisher = new SymbolDistinguisher(this.Compilation, inferredType, iterationVariableType);
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

            var getEnumeratorType = builder.GetEnumeratorMethod.ReturnType;
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
                (builder.ElementType.IsPointerType() && collectionExpr.Type.IsArray()) ||
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
                CheckOverflowAtRuntime,
                false,
                ConstantValue.NotAvailable,
                builder.CollectionType);

            return new BoundForEachStatement(
                _syntax,
                builder.Build(this.Flags),
                elementConversion,
                boundIterationVariableType,
                iterationVariables,
                convertedCollectionExpression,
                deconstructStep,
                body,
                CheckOverflowAtRuntime,
                this.BreakLabel,
                this.ContinueLabel,
                hasErrors);
        }

        internal TypeSymbol InferCollectionElementType(DiagnosticBag diagnostics, ExpressionSyntax collectionSyntax)
        {
            // Use the right binder to avoid seeing iteration variable
            BoundExpression collectionExpr = this.GetBinder(collectionSyntax).BindValue(collectionSyntax, diagnostics, BindValueKind.RValue);

            ForEachEnumeratorInfo.Builder builder = new ForEachEnumeratorInfo.Builder();
            TypeSymbol inferredType;
            GetEnumeratorInfoAndInferCollectionElementType(ref builder, ref collectionExpr, diagnostics, out inferredType);
            return inferredType;
        }

        private bool GetEnumeratorInfoAndInferCollectionElementType(ref ForEachEnumeratorInfo.Builder builder, ref BoundExpression collectionExpr, DiagnosticBag diagnostics, out TypeSymbol inferredType)
        {
            UnwrapCollectionExpressionIfNullable(ref collectionExpr, diagnostics);

            bool gotInfo = GetEnumeratorInfo(ref builder, collectionExpr, diagnostics);

            if (!gotInfo)
            {
                inferredType = null;
            }
            else if (collectionExpr.HasDynamicType())
            {
                // If the enumerator is dynamic, it yields dynamic values 
                inferredType = DynamicTypeSymbol.Instance;
            }
            else if (collectionExpr.Type.SpecialType == SpecialType.System_String && builder.CollectionType.SpecialType == SpecialType.System_Collections_IEnumerable)
            {
                // Reproduce dev11 behavior: we're always going to lower a foreach loop over a string to a for loop 
                // over the string's Chars indexer.  Therefore, we should infer "char", regardless of what the spec
                // indicates the element type is.  This actually matters in practice because the System.String in
                // the portable library doesn't have a pattern GetEnumerator method or implement IEnumerable<char>.
                inferredType = GetSpecialType(SpecialType.System_Char, diagnostics, collectionExpr.Syntax);
            }
            else
            {
                inferredType = builder.ElementType;
            }

            return gotInfo;
        }

        private void UnwrapCollectionExpressionIfNullable(ref BoundExpression collectionExpr, DiagnosticBag diagnostics)
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
                    collectionExpr = BoundCall.Synthesized(
                        syntax: exprSyntax,
                        receiverOpt: collectionExpr,
                        method: nullableValueGetter);
                }
                else
                {
                    collectionExpr = new BoundBadExpression(
                        exprSyntax,
                        LookupResultKind.Empty,
                        ImmutableArray<Symbol>.Empty,
                        ImmutableArray.Create(collectionExpr),
                        collectionExprType.GetNullableUnderlyingType())
                    { WasCompilerGenerated = true }; // Don't affect the type in the SemanticModel.
                }
            }
        }

        /// <summary>
        /// The spec describes an algorithm for finding the following types:
        ///   1) Collection type
        ///   2) Enumerator type
        ///   3) Element type
        ///   
        /// The implementation details are a bit difference.  If we're iterating over a string or an array, then we don't need to record anything
        /// but the inferredType (in case the iteration variable is implicitly typed).  If we're iterating over anything else, then we want the 
        /// inferred type plus a ForEachEnumeratorInfo.Builder with:
        ///   1) Collection type
        ///   2) Element type
        ///   3) GetEnumerator method of the collection type (return type will be the enumerator type from the spec)
        ///   4) Current property of the enumerator type
        ///   5) MoveNext method of the enumerator type
        ///   
        /// The caller will have to do some extra conversion checks before creating a ForEachEnumeratorInfo for the BoundForEachStatement.
        /// </summary>
        /// <param name="builder">Builder to fill in (partially, all but conversions).</param>
        /// <param name="collectionExpr">The expression over which to iterate.</param>
        /// <param name="diagnostics">Populated with binding diagnostics.</param>
        /// <returns>Partially populated (all but conversions) or null if there was an error.</returns>
        private bool GetEnumeratorInfo(ref ForEachEnumeratorInfo.Builder builder, BoundExpression collectionExpr, DiagnosticBag diagnostics)
        {
            TypeSymbol collectionExprType = collectionExpr.Type;

            if (collectionExpr.ConstantValue != null)
            {
                if (collectionExpr.ConstantValue.IsNull)
                {
                    // Spec seems to refer to null literals, but Dev10 reports anything known to be null.
                    diagnostics.Add(ErrorCode.ERR_NullNotValid, _syntax.Expression.Location);
                    return false;
                }
            }

            if ((object)collectionExprType == null) // There's no way to enumerate something without a type.
            {
                if (collectionExpr.Kind == BoundKind.DefaultExpression)
                {
                    diagnostics.Add(ErrorCode.ERR_DefaultLiteralNotValid, _syntax.Expression.Location);
                }
                else
                {
                    // The null and default literals were caught above, so anything else with a null type is a method group or anonymous function
                    diagnostics.Add(ErrorCode.ERR_AnonMethGrpInForEach, _syntax.Expression.Location, collectionExpr.Display);
                    // CONSIDER: dev10 also reports ERR_ForEachMissingMember (i.e. failed pattern match).
                }
                return false;
            }

            if (collectionExpr.ResultKind == LookupResultKind.NotAValue)
            {
                // Short-circuiting to prevent strange behavior in the case where the collection
                // expression is a type expression and the type is enumerable.
                Debug.Assert(collectionExpr.HasAnyErrors); // should already have been reported
                return false;
            }

            // The spec specifically lists the collection, enumerator, and element types for arrays and dynamic.
            if (collectionExprType.Kind == SymbolKind.ArrayType || collectionExprType.Kind == SymbolKind.DynamicType)
            {
                builder = GetDefaultEnumeratorInfo(builder, diagnostics, collectionExprType);
                return true;
            }

            bool foundMultipleGenericIEnumerableInterfaces;
            if (SatisfiesGetEnumeratorPattern(ref builder, collectionExprType, diagnostics))
            {
                Debug.Assert((object)builder.GetEnumeratorMethod != null);

                builder.CollectionType = collectionExprType;

                if (SatisfiesForEachPattern(ref builder, diagnostics))
                {
                    builder.ElementType = ((PropertySymbol)builder.CurrentPropertyGetter.AssociatedSymbol).Type;

                    // NOTE: if IDisposable is not available at all, no diagnostics will be reported - we will just assume that
                    // the enumerator is not disposable.  If it has IDisposable in its interface list, there will be a diagnostic there.
                    // If IDisposable is available but its Dispose method is not, then diagnostics will be reported only if the enumerator
                    // is potentially disposable.

                    var useSiteDiagnosticBag = DiagnosticBag.GetInstance();
                    TypeSymbol enumeratorType = builder.GetEnumeratorMethod.ReturnType;
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    if (!enumeratorType.IsSealed || this.Conversions.ClassifyImplicitConversionFromType(enumeratorType, this.Compilation.GetSpecialType(SpecialType.System_IDisposable), ref useSiteDiagnostics).IsImplicit)
                    {
                        builder.NeedsDisposeMethod = true;
                        diagnostics.AddRange(useSiteDiagnosticBag);
                    }
                    useSiteDiagnosticBag.Free();

                    diagnostics.Add(_syntax, useSiteDiagnostics);
                    return true;
                }

                MethodSymbol getEnumeratorMethod = builder.GetEnumeratorMethod;
                diagnostics.Add(ErrorCode.ERR_BadGetEnumerator, _syntax.Expression.Location, getEnumeratorMethod.ReturnType, getEnumeratorMethod);
                return false;
            }

            if (IsIEnumerable(collectionExprType))
            {
                // This indicates a problem with the special IEnumerable type - it should have satisfied the GetEnumerator pattern.
                diagnostics.Add(ErrorCode.ERR_ForEachMissingMember, _syntax.Expression.Location, collectionExprType, GetEnumeratorMethodName);
                return false;
            }

            if (AllInterfacesContainsIEnumerable(ref builder, collectionExprType, diagnostics, out foundMultipleGenericIEnumerableInterfaces))
            {
                CSharpSyntaxNode errorLocationSyntax = _syntax.Expression;

                if (foundMultipleGenericIEnumerableInterfaces)
                {
                    diagnostics.Add(ErrorCode.ERR_MultipleIEnumOfT, errorLocationSyntax.Location, collectionExprType, this.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T));
                    return false;
                }

                Debug.Assert((object)builder.CollectionType != null);

                NamedTypeSymbol collectionType = (NamedTypeSymbol)builder.CollectionType;
                if (collectionType.IsGenericType)
                {
                    // If the type is generic, we have to search for the methods
                    Debug.Assert(collectionType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
                    builder.ElementType = collectionType.TypeArgumentsNoUseSiteDiagnostics.Single();

                    MethodSymbol getEnumeratorMethod = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IEnumerable_T__GetEnumerator, diagnostics, errorLocationSyntax);
                    if ((object)getEnumeratorMethod != null)
                    {
                        builder.GetEnumeratorMethod = getEnumeratorMethod.AsMember(collectionType);

                        TypeSymbol enumeratorType = builder.GetEnumeratorMethod.ReturnType;
                        Debug.Assert(enumeratorType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerator_T);
                        MethodSymbol currentPropertyGetter = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IEnumerator_T__get_Current, diagnostics, errorLocationSyntax);
                        if ((object)currentPropertyGetter != null)
                        {
                            builder.CurrentPropertyGetter = currentPropertyGetter.AsMember((NamedTypeSymbol)enumeratorType);
                        }
                    }

                    builder.MoveNextMethod = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext, diagnostics, errorLocationSyntax); // NOTE: MoveNext is actually inherited from System.Collections.IEnumerator
                }
                else
                {
                    // Non-generic - use special members to avoid re-computing
                    Debug.Assert(collectionType.SpecialType == SpecialType.System_Collections_IEnumerable);
                    builder.ElementType = GetSpecialType(SpecialType.System_Object, diagnostics, errorLocationSyntax);

                    builder.GetEnumeratorMethod = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerable__GetEnumerator, diagnostics, errorLocationSyntax);
                    builder.CurrentPropertyGetter = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__get_Current, diagnostics, errorLocationSyntax);
                    builder.MoveNextMethod = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext, diagnostics, errorLocationSyntax);

                    Debug.Assert((object)builder.GetEnumeratorMethod == null ||
                        builder.GetEnumeratorMethod.ReturnType == GetSpecialType(SpecialType.System_Collections_IEnumerator, diagnostics, errorLocationSyntax));
                }

                // We don't know the runtime type, so we will have to insert a runtime check for IDisposable (with a conditional call to IDisposable.Dispose).
                builder.NeedsDisposeMethod = true;
                return true;
            }

            // COMPAT:
            // In some rare cases, like MicroFramework, System.String does not implement foreach pattern.
            // For compat reasons we must still treat System.String as valid to use in a foreach
            // Similarly to the cases with array and dynamic, we will default to IEnumerable for binding purposes.
            // Lowering will not use iterator info with strings, so it is ok.
            if (collectionExprType.SpecialType == SpecialType.System_String)
            {
                builder = GetDefaultEnumeratorInfo(builder, diagnostics, collectionExprType);
                return true;
            }

            if (!string.IsNullOrEmpty(collectionExprType.Name) || !collectionExpr.HasErrors)
            {
                diagnostics.Add(ErrorCode.ERR_ForEachMissingMember, _syntax.Expression.Location, collectionExprType, GetEnumeratorMethodName);
            }
            return false;
        }

        private ForEachEnumeratorInfo.Builder GetDefaultEnumeratorInfo(ForEachEnumeratorInfo.Builder builder, DiagnosticBag diagnostics, TypeSymbol collectionExprType)
        {
            // NOTE: for arrays, we won't actually use any of these members - they're just for the API.
            builder.CollectionType = GetSpecialType(SpecialType.System_Collections_IEnumerable, diagnostics, _syntax);

            if (collectionExprType.IsDynamic())
            {
                builder.ElementType = ((_syntax as ForEachStatementSyntax)?.Type.IsVar == true) ?
                    (TypeSymbol)DynamicTypeSymbol.Instance :
                    GetSpecialType(SpecialType.System_Object, diagnostics, _syntax);
            }
            else
            {
                builder.ElementType = collectionExprType.SpecialType == SpecialType.System_String ?
                    GetSpecialType(SpecialType.System_Char, diagnostics, _syntax) :
                    ((ArrayTypeSymbol)collectionExprType).ElementType;
            }


            // CONSIDER: 
            // For arrays and string none of these members will actually be emitted, so it seems strange to prevent compilation if they can't be found.
            // skip this work in the batch case?
            builder.GetEnumeratorMethod = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerable__GetEnumerator, diagnostics, _syntax);
            builder.CurrentPropertyGetter = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__get_Current, diagnostics, _syntax);
            builder.MoveNextMethod = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext, diagnostics, _syntax);

            Debug.Assert((object)builder.GetEnumeratorMethod == null ||
                builder.GetEnumeratorMethod.ReturnType == this.Compilation.GetSpecialType(SpecialType.System_Collections_IEnumerator));

            // We don't know the runtime type, so we will have to insert a runtime check for IDisposable (with a conditional call to IDisposable.Dispose).
            builder.NeedsDisposeMethod = true;
            return builder;
        }

        /// <summary>
        /// Check for a GetEnumerator method on collectionExprType.  Failing to satisfy the pattern is not an error -
        /// it just means that we have to check for an interface instead.
        /// </summary>
        /// <param name="collectionExprType">Type of the expression over which to iterate.</param>
        /// <param name="diagnostics">Populated with *warnings* if there are near misses.</param>
        /// <param name="builder">Builder to fill in. <see cref="ForEachEnumeratorInfo.Builder.GetEnumeratorMethod"/> set if the pattern in satisfied.</param>
        /// <returns>True if the method was found (still have to verify that the return (i.e. enumerator) type is acceptable).</returns>
        /// <remarks>
        /// Only adds warnings, so does not affect control flow (i.e. no need to check for failure).
        /// </remarks>
        private bool SatisfiesGetEnumeratorPattern(ref ForEachEnumeratorInfo.Builder builder, TypeSymbol collectionExprType, DiagnosticBag diagnostics)
        {
            LookupResult lookupResult = LookupResult.GetInstance();
            MethodSymbol getEnumeratorMethod = FindForEachPatternMethod(collectionExprType, GetEnumeratorMethodName, lookupResult, warningsOnly: true, diagnostics: diagnostics);
            lookupResult.Free();

            builder.GetEnumeratorMethod = getEnumeratorMethod;
            return (object)getEnumeratorMethod != null;
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
        private MethodSymbol FindForEachPatternMethod(TypeSymbol patternType, string methodName, LookupResult lookupResult, bool warningsOnly, DiagnosticBag diagnostics)
        {
            Debug.Assert(lookupResult.IsClear);

            // Not using LookupOptions.MustBeInvocableMember because we don't want the corresponding lookup error.
            // We filter out non-methods below.
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            this.LookupMembersInType(
                lookupResult,
                patternType,
                methodName,
                arity: 0,
                basesBeingResolved: null,
                options: LookupOptions.Default,
                originalBinder: this,
                diagnose: false,
                useSiteDiagnostics: ref useSiteDiagnostics);

            diagnostics.Add(_syntax.Expression, useSiteDiagnostics);

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
                // we've found (so far) is that it only considers methods with zero parameters
                // (i.e. doesn't work with "params" or optional parameters).
                if (!method.Parameters.Any())
                {
                    candidateMethods.Add((MethodSymbol)member);
                }
            }

            MethodSymbol patternMethod = PerformForEachPatternOverloadResolution(patternType, candidateMethods, warningsOnly, diagnostics);

            candidateMethods.Free();

            return patternMethod;
        }

        /// <summary>
        /// The overload resolution portion of FindForEachPatternMethod.
        /// </summary>
        private MethodSymbol PerformForEachPatternOverloadResolution(TypeSymbol patternType, ArrayBuilder<MethodSymbol> candidateMethods, bool warningsOnly, DiagnosticBag diagnostics)
        {
            ArrayBuilder<TypeSymbol> typeArguments = ArrayBuilder<TypeSymbol>.GetInstance();
            AnalyzedArguments arguments = AnalyzedArguments.GetInstance();
            OverloadResolutionResult<MethodSymbol> overloadResolutionResult = OverloadResolutionResult<MethodSymbol>.GetInstance();

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            // We create a dummy receiver of the invocation so MethodInvocationOverloadResolution knows it was invoked from an instance, not a type
            var dummyReceiver = new BoundImplicitReceiver(_syntax.Expression, patternType);
            this.OverloadResolution.MethodInvocationOverloadResolution(
                methods: candidateMethods,
                typeArguments: typeArguments,
                receiver: dummyReceiver,
                arguments: arguments,
                result: overloadResolutionResult,
                useSiteDiagnostics: ref useSiteDiagnostics);
            diagnostics.Add(_syntax.Expression, useSiteDiagnostics);

            MethodSymbol result = null;

            if (overloadResolutionResult.Succeeded)
            {
                result = overloadResolutionResult.ValidResult.Member;

                if (result.IsStatic || result.DeclaredAccessibility != Accessibility.Public)
                {
                    if (warningsOnly)
                    {
                        diagnostics.Add(ErrorCode.WRN_PatternStaticOrInaccessible, _syntax.Expression.Location, patternType, MessageID.IDS_Collection.Localize(), result);
                    }
                    result = null;
                }
                else if (result.CallsAreOmitted(_syntax.SyntaxTree))
                {
                    // Calls to this method are omitted in the current syntax tree, i.e it is either a partial method with no implementation part OR a conditional method whose condition is not true in this source file.
                    // We don't want to want to allow this case, see StatementBinder::bindPatternToMethod.
                    result = null;
                }
            }
            else if (overloadResolutionResult.Results.Length > 1)
            {
                if (warningsOnly)
                {
                    diagnostics.Add(ErrorCode.WRN_PatternIsAmbiguous, _syntax.Expression.Location, patternType, MessageID.IDS_Collection.Localize(),
                        overloadResolutionResult.Results[0].Member, overloadResolutionResult.Results[1].Member);
                }
            }

            overloadResolutionResult.Free();
            arguments.Free();
            typeArguments.Free();

            return result;
        }

        /// <summary>
        /// Called after it is determined that the expression being enumerated is of a type that
        /// has a GetEnumerator method.  Checks to see if the return type of the GetEnumerator
        /// method is suitable (i.e. has Current and MoveNext).
        /// </summary>
        /// <param name="builder">Must be non-null and contain a non-null GetEnumeratorMethod.</param>
        /// <param name="diagnostics">Will be populated with pattern diagnostics.</param>
        /// <returns>True if the return type has suitable members.</returns>
        /// <remarks>
        /// It seems that every failure path reports the same diagnostics, so that is left to the caller.
        /// </remarks>
        private bool SatisfiesForEachPattern(ref ForEachEnumeratorInfo.Builder builder, DiagnosticBag diagnostics)
        {
            Debug.Assert((object)builder.GetEnumeratorMethod != null);

            MethodSymbol getEnumeratorMethod = builder.GetEnumeratorMethod;
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
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                this.LookupMembersInType(
                    lookupResult,
                    enumeratorType,
                    CurrentPropertyName,
                    arity: 0,
                    basesBeingResolved: null,
                    options: LookupOptions.Default, // properties are not invocable - their accessors are
                    originalBinder: this,
                    diagnose: false,
                    useSiteDiagnostics: ref useSiteDiagnostics);

                diagnostics.Add(_syntax.Expression, useSiteDiagnostics);
                useSiteDiagnostics = null;

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
                    bool isAccessible = this.IsAccessible(currentPropertyGetterCandidate, ref useSiteDiagnostics);
                    diagnostics.Add(_syntax.Expression, useSiteDiagnostics);

                    if (!isAccessible)
                    {
                        // NOTE: per Dev10 and the spec, the property has to be public, but the accessor just has to be accessible
                        return false;
                    }
                }

                builder.CurrentPropertyGetter = currentPropertyGetterCandidate;

                lookupResult.Clear(); // Reuse the same LookupResult

                MethodSymbol moveNextMethodCandidate = FindForEachPatternMethod(enumeratorType, MoveNextMethodName, lookupResult, warningsOnly: false, diagnostics: diagnostics);

                // SPEC VIOLATION: Dev10 checks the return type of the original definition, rather than the return type of the actual method.

                if ((object)moveNextMethodCandidate == null ||
                    moveNextMethodCandidate.IsStatic || moveNextMethodCandidate.DeclaredAccessibility != Accessibility.Public ||
                    ((MethodSymbol)moveNextMethodCandidate.OriginalDefinition).ReturnType.SpecialType != SpecialType.System_Boolean)
                {
                    return false;
                }

                builder.MoveNextMethod = moveNextMethodCandidate;

                return true;
            }
            finally
            {
                lookupResult.Free();
            }
        }

        private void ReportEnumerableWarning(DiagnosticBag diagnostics, TypeSymbol enumeratorType, Symbol patternMemberCandidate)
        {
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            if (this.IsAccessible(patternMemberCandidate, ref useSiteDiagnostics))
            {
                diagnostics.Add(ErrorCode.WRN_PatternBadSignature, _syntax.Expression.Location, enumeratorType, MessageID.IDS_Collection.Localize(), patternMemberCandidate);
            }

            diagnostics.Add(_syntax.Expression, useSiteDiagnostics);
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

        /// <summary>
        /// Checks if the given type implements (or extends, in the case of an interface),
        /// System.Collections.IEnumerable or System.Collections.Generic.IEnumerable&lt;T&gt;,
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
            DiagnosticBag diagnostics,
            out bool foundMultiple)
        {
            Debug.Assert(!IsIEnumerable(type));

            NamedTypeSymbol implementedIEnumerable = null;
            foundMultiple = false;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            if (type.TypeKind == TypeKind.TypeParameter)
            {
                var typeParameter = (TypeParameterSymbol)type;
                GetIEnumerableOfT(typeParameter.EffectiveBaseClass(ref useSiteDiagnostics).AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics), ref @implementedIEnumerable, ref foundMultiple);
                GetIEnumerableOfT(typeParameter.AllEffectiveInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics), ref @implementedIEnumerable, ref foundMultiple);
            }
            else
            {
                GetIEnumerableOfT(type.AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics), ref @implementedIEnumerable, ref foundMultiple);
            }

            // Prefer generic to non-generic, unless it is inaccessible.
            if (((object)implementedIEnumerable == null) || !this.IsAccessible(implementedIEnumerable, ref useSiteDiagnostics))
            {
                implementedIEnumerable = null;

                var implementedNonGeneric = this.Compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
                if ((object)implementedNonGeneric != null)
                {
                    var conversion = this.Conversions.ClassifyImplicitConversionFromType(type, implementedNonGeneric, ref useSiteDiagnostics);
                    if (conversion.IsImplicit)
                    {
                        implementedIEnumerable = implementedNonGeneric;
                    }
                }
            }

            diagnostics.Add(_syntax.Expression, useSiteDiagnostics);

            builder.CollectionType = implementedIEnumerable;
            return (object)implementedIEnumerable != null;
        }

        private static void GetIEnumerableOfT(ImmutableArray<NamedTypeSymbol> interfaces, ref NamedTypeSymbol result, ref bool foundMultiple)
        {
            if (foundMultiple)
            {
                return;
            }
            foreach (NamedTypeSymbol @interface in interfaces)
            {
                if (@interface.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
                {
                    if ((object)result == null)
                    {
                        result = @interface;
                    }
                    else if (@interface != result)
                    {
                        foundMultiple = true;
                        return;
                    }
                }
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
        private void ReportPatternMemberLookupDiagnostics(LookupResult lookupResult, TypeSymbol patternType, string memberName, bool warningsOnly, DiagnosticBag diagnostics)
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

                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    this.LookupMembersInType(
                        lookupResult,
                        patternType,
                        memberName,
                        arity: 0,
                        basesBeingResolved: null,
                        options: LookupOptions.Default,
                        originalBinder: this,
                        diagnose: true,
                        useSiteDiagnostics: ref useSiteDiagnostics);

                    diagnostics.Add(_syntax.Expression, useSiteDiagnostics);

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
    }
}
