// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This portion of the binder converts an <see cref="ExpressionSyntax"/> into a <see cref="BoundExpression"/>.
    /// </summary>
    internal partial class Binder
    {
        /// <summary>
        /// Determines whether "this" reference is available within the current context.
        /// </summary>
        /// <param name="isExplicit">The reference was explicitly specified in syntax.</param>
        /// <param name="inStaticContext">True if "this" is not available due to the current method/property/field initializer being static.</param>
        /// <returns>True if a reference to "this" is available.</returns>
        internal bool HasThis(bool isExplicit, out bool inStaticContext)
        {
            if (!isExplicit && IsInsideNameof && Compilation.IsFeatureEnabled(MessageID.IDS_FeatureInstanceMemberInNameof))
            {
                inStaticContext = false;
                return true;
            }

            var memberOpt = this.ContainingMemberOrLambda?.ContainingNonLambdaMember();
            if (memberOpt?.IsStatic == true)
            {
                inStaticContext = memberOpt.Kind == SymbolKind.Field || memberOpt.Kind == SymbolKind.Method || memberOpt.Kind == SymbolKind.Property;
                return false;
            }

            inStaticContext = false;

            if (InConstructorInitializer || InAttributeArgument)
            {
                return false;
            }

            if (memberOpt is { ContainingSymbol: NamedTypeSymbol { IsExtension: true } })
            {
                return false;
            }

            var containingType = memberOpt?.ContainingType;
            bool inTopLevelScriptMember = (object)containingType != null && containingType.IsScriptClass;

            // "this" is not allowed in field initializers (that are not script variable initializers):
            if (InFieldInitializer && !inTopLevelScriptMember)
            {
                return false;
            }

            // top-level script code only allows implicit "this" reference:
            return !inTopLevelScriptMember || !isExplicit;
        }

        internal bool InFieldInitializer
        {
            get { return this.Flags.Includes(BinderFlags.FieldInitializer); }
        }

        internal bool InParameterDefaultValue
        {
            get { return this.Flags.Includes(BinderFlags.ParameterDefaultValue); }
        }

        protected bool InConstructorInitializer
        {
            get { return this.Flags.Includes(BinderFlags.ConstructorInitializer); }
        }

        internal bool InAttributeArgument
        {
            get { return this.Flags.Includes(BinderFlags.AttributeArgument); }
        }

        internal bool InCref
        {
            get { return this.Flags.Includes(BinderFlags.Cref); }
        }

        protected bool InCrefButNotParameterOrReturnType
        {
            get { return InCref && !this.Flags.Includes(BinderFlags.CrefParameterOrReturnType); }
        }

        /// <summary>
        /// Returns true if the node is in a position where an unbound type
        /// such as (C&lt;,&gt;) is allowed.
        /// </summary>
        protected virtual bool IsUnboundTypeAllowed(GenericNameSyntax syntax)
        {
            return Next.IsUnboundTypeAllowed(syntax);
        }

        /// <summary>
        /// Generates a new <see cref="BoundBadExpression"/> with no known type
        /// </summary>
        private BoundBadExpression BadExpression(SyntaxNode syntax)
        {
            return BadExpression(syntax, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty);
        }

        /// <summary>
        /// Generates a new <see cref="BoundBadExpression"/> with no known type, and the given bound child.
        /// </summary>
        private BoundBadExpression BadExpression(SyntaxNode syntax, BoundExpression childNode)
        {
            return BadExpression(syntax, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, childNode);
        }

        /// <summary>
        /// Generates a new <see cref="BoundBadExpression"/> with no known type, and the given bound children.
        /// </summary>
        private BoundBadExpression BadExpression(SyntaxNode syntax, ImmutableArray<BoundExpression> childNodes)
        {
            return BadExpression(syntax, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, childNodes);
        }

        /// <summary>
        /// Generates a new <see cref="BoundBadExpression"/> with no known type, given lookup resultKind.
        /// </summary>
        protected BoundBadExpression BadExpression(SyntaxNode syntax, LookupResultKind lookupResultKind)
        {
            return BadExpression(syntax, lookupResultKind, ImmutableArray<Symbol>.Empty);
        }

        /// <summary>
        /// Generates a new <see cref="BoundBadExpression"/> with no known type, given lookup resultKind and the given bound child.
        /// </summary>
        protected BoundBadExpression BadExpression(SyntaxNode syntax, LookupResultKind lookupResultKind, BoundExpression childNode)
        {
            return BadExpression(syntax, lookupResultKind, ImmutableArray<Symbol>.Empty, childNode);
        }

        /// <summary>
        /// Generates a new <see cref="BoundBadExpression"/> with no known type, given lookupResultKind and given symbols for GetSemanticInfo API.
        /// </summary>
        private BoundBadExpression BadExpression(SyntaxNode syntax, LookupResultKind resultKind, ImmutableArray<Symbol> symbols)
        {
            return new BoundBadExpression(syntax,
                resultKind,
                symbols,
                ImmutableArray<BoundExpression>.Empty,
                CreateErrorType());
        }

        /// <summary>
        /// Generates a new <see cref="BoundBadExpression"/> with no known type, given lookupResultKind and given symbols for GetSemanticInfo API,
        /// and the given bound child.
        /// </summary>
        private BoundBadExpression BadExpression(SyntaxNode syntax, LookupResultKind resultKind, ImmutableArray<Symbol> symbols, BoundExpression childNode)
        {
            return new BoundBadExpression(syntax,
                resultKind,
                symbols,
                ImmutableArray.Create(AdjustBadExpressionChild(BindToTypeForErrorRecovery(childNode))),
                CreateErrorType());
        }

        internal BoundExpression AdjustBadExpressionChild(BoundExpression childNode)
        {
            if (childNode is BoundMethodGroup methodGroup)
            {
                return FixMethodGroupWithTypeOrValue(methodGroup, Conversion.NoConversion, BindingDiagnosticBag.Discarded);
            }
            else
            {
                return ReplaceTypeOrValueReceiver(childNode, useType: false, BindingDiagnosticBag.Discarded);
            }
        }

        /// <summary>
        /// Generates a new <see cref="BoundBadExpression"/> with no known type, given lookupResultKind and given symbols for GetSemanticInfo API,
        /// and the given bound children.
        /// </summary>
        private BoundBadExpression BadExpression(SyntaxNode syntax, LookupResultKind resultKind, ImmutableArray<Symbol> symbols, ImmutableArray<BoundExpression> childNodes, bool wasCompilerGenerated = false)
        {
            return new BoundBadExpression(syntax,
                resultKind,
                symbols,
                childNodes.SelectAsArray((e, self) => self.AdjustBadExpressionChild(self.BindToTypeForErrorRecovery(e)), this),
                CreateErrorType())
            { WasCompilerGenerated = wasCompilerGenerated };
        }

        /// <summary>
        /// Helper method to generate a bound expression with HasErrors set to true.
        /// Returned bound expression is guaranteed to have a non-null type, except when <paramref name="expr"/> is an unbound lambda or a default literal
        /// If <paramref name="expr"/> already has errors and meets the above type requirements, then it is returned unchanged.
        /// Otherwise, if <paramref name="expr"/> is a BoundBadExpression, then it is updated with the <paramref name="resultKind"/> and non-null type.
        /// Otherwise, a new <see cref="BoundBadExpression"/> wrapping <paramref name="expr"/> is returned.
        /// The returned expression has not been converted if needed, so callers need to make sure that the expression is converted before being put into the
        /// bound tree. Make sure to test with unconverted constructs such as switch expressions, target-typed new, or interpolated strings.
        /// </summary>
        /// <remarks>
        /// Returned expression need not be a <see cref="BoundBadExpression"/>, but is guaranteed to have HasErrors set to true.
        /// </remarks>
        private BoundExpression ToBadExpression(BoundExpression expr, LookupResultKind resultKind = LookupResultKind.Empty)
        {
            Debug.Assert(expr != null);
            Debug.Assert(resultKind != LookupResultKind.Viable);

            TypeSymbol resultType = expr.Type;
            BoundKind exprKind = expr.Kind;

            if (expr.HasAnyErrors && ((object)resultType != null || exprKind == BoundKind.UnboundLambda || exprKind == BoundKind.DefaultLiteral))
            {
                return expr;
            }

            if (exprKind == BoundKind.BadExpression)
            {
                var badExpression = (BoundBadExpression)expr;
                return badExpression.Update(resultKind, badExpression.Symbols, badExpression.ChildBoundNodes, resultType);
            }
            else
            {
                ArrayBuilder<Symbol> symbols = ArrayBuilder<Symbol>.GetInstance();
                expr.GetExpressionSymbols(symbols, parent: null, binder: this);
                return new BoundBadExpression(
                    expr.Syntax,
                    resultKind,
                    symbols.ToImmutableAndFree(),
                    ImmutableArray.Create(AdjustBadExpressionChild(BindToTypeForErrorRecovery(expr))),
                    resultType ?? CreateErrorType());
            }
        }

        internal NamedTypeSymbol CreateErrorType(string name = "")
        {
            return new ExtendedErrorTypeSymbol(this.Compilation, name, arity: 0, errorInfo: null, unreported: false);
        }

        /// <summary>
        /// Bind the expression and verify the expression matches the combination of lvalue and
        /// rvalue requirements given by valueKind. If the expression was bound successfully, but
        /// did not meet the requirements, the return value will be a <see cref="BoundBadExpression"/> that
        /// (typically) wraps the subexpression.
        /// </summary>
        internal BoundExpression BindValue(ExpressionSyntax node, BindingDiagnosticBag diagnostics, BindValueKind valueKind)
        {
            var result = this.BindExpression(node, diagnostics: diagnostics, invoked: false, indexed: false);
            return CheckValue(result, valueKind, diagnostics);
        }

        internal BoundExpression BindRValueWithoutTargetType(ExpressionSyntax node, BindingDiagnosticBag diagnostics, bool reportNoTargetType = true)
        {
            return BindToNaturalType(BindValue(node, diagnostics, BindValueKind.RValue), diagnostics, reportNoTargetType);
        }

        /// <summary>
        /// When binding a switch case's expression, it is possible that it resolves to a type (technically, a type pattern).
        /// This implementation permits either an rvalue or a BoundTypeExpression.
        /// </summary>
        internal BoundExpression BindTypeOrRValue(ExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            var valueOrType = BindExpression(node, diagnostics: diagnostics, invoked: false, indexed: false);
            if (valueOrType.Kind == BoundKind.TypeExpression)
            {
                // In the Color Color case (Kind == BoundKind.TypeOrValueExpression), we treat it as a value
                // by not entering this if statement
                return valueOrType;
            }

            return CheckValue(valueOrType, BindValueKind.RValue, diagnostics);
        }

        internal BoundExpression BindToTypeForErrorRecovery(BoundExpression expression, TypeSymbol type = null)
        {
            return BindToTypeForErrorRecovery(expression, BindingDiagnosticBag.Discarded, type);
        }

        internal BoundExpression BindToTypeForErrorRecovery(BoundExpression expression, BindingDiagnosticBag diagnostics, TypeSymbol type = null)
        {
            if (expression is null)
                return null;
            var result =
                !expression.NeedsToBeConverted() ? expression :
                type is null ? BindToNaturalType(expression, diagnostics, reportNoTargetType: false) :
                GenerateConversionForAssignment(type, expression, diagnostics);
            return result;
        }

        /// <summary>
        /// Bind an rvalue expression to its natural type.  For example, a switch expression that has not been
        /// converted to another type has to be converted to its own natural type by applying a conversion to
        /// that type to each of the arms of the switch expression.  This method is a bottleneck for ensuring
        /// that such a conversion occurs when needed.  It also handles tuple expressions which need to be
        /// converted to their own natural type because they may contain switch expressions.
        /// </summary>
        internal BoundExpression BindToNaturalType(BoundExpression expression, BindingDiagnosticBag diagnostics, bool reportNoTargetType = true)
        {
            if (!expression.NeedsToBeConverted())
                return expression;

            BoundExpression result;
            switch (expression)
            {
                case BoundUnconvertedSwitchExpression expr:
                    {
                        var commonType = expr.Type;
                        var exprSyntax = (SwitchExpressionSyntax)expr.Syntax;
                        bool hasErrors = expression.HasErrors;
                        if (commonType is null)
                        {
                            if (!expr.HasAnyErrors)
                            {
                                diagnostics.Add(ErrorCode.ERR_SwitchExpressionNoBestType, exprSyntax.SwitchKeyword.GetLocation());
                            }

                            commonType = CreateErrorType();
                            hasErrors = true;
                        }
                        result = ConvertSwitchExpression(expr, commonType, conversionIfTargetTyped: null, diagnostics, hasErrors);
                    }
                    break;
                case BoundUnconvertedConditionalOperator op:
                    {
                        TypeSymbol type = op.Type;
                        bool hasErrors = op.HasErrors;
                        if (type is null)
                        {
                            Debug.Assert(op.NoCommonTypeError != 0);
                            type = CreateErrorType();
                            hasErrors = true;

                            if (!op.HasAnyErrors)
                            {
                                object trueArg = op.Consequence.Display;
                                object falseArg = op.Alternative.Display;
                                if (op.NoCommonTypeError == ErrorCode.ERR_InvalidQM && trueArg is Symbol trueSymbol && falseArg is Symbol falseSymbol)
                                {
                                    // ERR_InvalidQM is an error that there is no conversion between the two types. They might be the same
                                    // type name from different assemblies, so we disambiguate the display.
                                    SymbolDistinguisher distinguisher = new SymbolDistinguisher(this.Compilation, trueSymbol, falseSymbol);
                                    trueArg = distinguisher.First;
                                    falseArg = distinguisher.Second;
                                }

                                diagnostics.Add(op.NoCommonTypeError, op.Syntax.Location, trueArg, falseArg);
                            }
                        }

                        result = ConvertConditionalExpression(op, type, conversionIfTargetTyped: null, diagnostics, hasErrors);
                    }
                    break;
                case BoundTupleLiteral sourceTuple:
                    {
                        var boundArgs = ArrayBuilder<BoundExpression>.GetInstance(sourceTuple.Arguments.Length);
                        foreach (var arg in sourceTuple.Arguments)
                        {
                            boundArgs.Add(BindToNaturalType(arg, diagnostics, reportNoTargetType));
                        }
                        result = new BoundConvertedTupleLiteral(
                            sourceTuple.Syntax,
                            sourceTuple,
                            wasTargetTyped: false,
                            boundArgs.ToImmutableAndFree(),
                            sourceTuple.ArgumentNamesOpt,
                            sourceTuple.InferredNamesOpt,
                            sourceTuple.Type, // same type to keep original element names
                            sourceTuple.HasErrors).WithSuppression(sourceTuple.IsSuppressed);
                    }
                    break;
                case BoundDefaultLiteral defaultExpr:
                    {
                        if (reportNoTargetType)
                        {
                            // In some cases, we let the caller report the error
                            diagnostics.Add(ErrorCode.ERR_DefaultLiteralNoTargetType, defaultExpr.Syntax.GetLocation());
                        }

                        result = new BoundDefaultExpression(
                            defaultExpr.Syntax,
                            targetType: null,
                            defaultExpr.ConstantValueOpt,
                            CreateErrorType(),
                            hasErrors: true).WithSuppression(defaultExpr.IsSuppressed);
                    }
                    break;
                case BoundStackAllocArrayCreation { Type: null } boundStackAlloc:
                    {
                        // This is a context in which the stackalloc could be either a pointer
                        // or a span.  For backward compatibility we treat it as a pointer.
                        var type = new PointerTypeSymbol(TypeWithAnnotations.Create(boundStackAlloc.ElementType));
                        result = GenerateConversionForAssignment(type, boundStackAlloc, diagnostics);
                    }
                    break;
                case BoundUnconvertedObjectCreationExpression expr:
                    {
                        if (reportNoTargetType && !expr.HasAnyErrors)
                        {
                            diagnostics.Add(ErrorCode.ERR_ImplicitObjectCreationNoTargetType, expr.Syntax.GetLocation(), expr.Display);
                        }

                        result = BindObjectCreationForErrorRecovery(expr, diagnostics);
                    }
                    break;
                case BoundUnconvertedInterpolatedString unconvertedInterpolatedString:
                    {
                        result = BindUnconvertedInterpolatedStringToString(unconvertedInterpolatedString, diagnostics);
                    }
                    break;
                case BoundBinaryOperator unconvertedBinaryOperator:
                    {
                        result = RebindSimpleBinaryOperatorAsConverted(unconvertedBinaryOperator, diagnostics);
                    }
                    break;
                case BoundUnconvertedCollectionExpression expr:
                    {
                        if (reportNoTargetType && !expr.HasAnyErrors)
                        {
                            diagnostics.Add(ErrorCode.ERR_CollectionExpressionNoTargetType, expr.Syntax.GetLocation());
                        }
                        result = BindCollectionExpressionForErrorRecovery(expr, CreateErrorType(), inConversion: false, diagnostics);
                    }
                    break;
                default:
                    result = expression;
                    break;
            }

            return result?.WithWasConverted();
        }

        private BoundExpression BindToInferredDelegateType(BoundExpression expr, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(expr.Kind is BoundKind.UnboundLambda or BoundKind.MethodGroup);

            var syntax = expr.Syntax;
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            var delegateType = expr.GetInferredDelegateType(ref useSiteInfo);
            diagnostics.Add(syntax, useSiteInfo);

            if (delegateType is null)
            {
                if (CheckFeatureAvailability(syntax, MessageID.IDS_FeatureInferredDelegateType, diagnostics))
                {
                    diagnostics.Add(ErrorCode.ERR_CannotInferDelegateType, syntax.GetLocation());
                }
                delegateType = CreateErrorType();
            }

            return GenerateConversionForAssignment(delegateType, expr, diagnostics);
        }

        internal BoundExpression BindValueAllowArgList(ExpressionSyntax node, BindingDiagnosticBag diagnostics, BindValueKind valueKind)
        {
            var result = this.BindExpressionAllowArgList(node, diagnostics: diagnostics);
            return CheckValue(result, valueKind, diagnostics);
        }

        internal BoundFieldEqualsValue BindFieldInitializer(
            FieldSymbol field,
            EqualsValueClauseSyntax initializerOpt,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert((object)this.ContainingMemberOrLambda == field);

            if (initializerOpt == null)
            {
                return null;
            }

            Binder initializerBinder = this.GetBinder(initializerOpt);
            Debug.Assert(initializerBinder != null);

            BoundExpression result = initializerBinder.BindVariableOrAutoPropInitializerValue(initializerOpt, field.RefKind,
                                                           field.GetFieldType(initializerBinder.FieldsBeingBound).Type, diagnostics);

            if (field is { IsStatic: false, RefKind: RefKind.None, ContainingSymbol: SourceMemberContainerTypeSymbol { PrimaryConstructor: { } primaryConstructor } } &&
                TryGetPrimaryConstructorParameterUsedAsValue(primaryConstructor, result) is (ParameterSymbol parameter, SyntaxNode syntax) &&
                primaryConstructor.GetCapturedParameters().ContainsKey(parameter))
            {
                diagnostics.Add(ErrorCode.WRN_CapturedPrimaryConstructorParameterInFieldInitializer, syntax.Location, parameter);
            }

            return new BoundFieldEqualsValue(initializerOpt, field, initializerBinder.GetDeclaredLocalsForScope(initializerOpt), result);
        }

        internal BoundExpression BindVariableOrAutoPropInitializerValue(
            EqualsValueClauseSyntax initializerOpt,
            RefKind refKind,
            TypeSymbol varType,
            BindingDiagnosticBag diagnostics)
        {
            if (initializerOpt == null)
            {
                return null;
            }

            BindValueKind valueKind;
            ExpressionSyntax value;
            IsInitializerRefKindValid(initializerOpt, initializerOpt, refKind, diagnostics, out valueKind, out value);
            BoundExpression initializer = BindPossibleArrayInitializer(value, varType, valueKind, diagnostics);
            initializer = GenerateConversionForAssignment(varType, initializer, diagnostics);
            return initializer;
        }

        internal Binder CreateBinderForParameterDefaultValue(
            ParameterSymbol parameter,
            EqualsValueClauseSyntax defaultValueSyntax)
        {
            var binder = new LocalScopeBinder(this.WithContainingMemberOrLambda(parameter.ContainingSymbol).WithAdditionalFlags(BinderFlags.ParameterDefaultValue));
            return new ExecutableCodeBinder(defaultValueSyntax,
                                            parameter.ContainingSymbol,
                                            binder);
        }

        internal BoundParameterEqualsValue BindParameterDefaultValue(
            EqualsValueClauseSyntax defaultValueSyntax,
            ParameterSymbol parameter,
            BindingDiagnosticBag diagnostics,
            out BoundExpression valueBeforeConversion)
        {
            Debug.Assert(this.InParameterDefaultValue);
            Debug.Assert(this.ContainingMemberOrLambda.Kind == SymbolKind.Method
                || this.ContainingMemberOrLambda.Kind == SymbolKind.Property
                || this.ContainingMemberOrLambda is NamedTypeSymbol { IsExtension: true });

            // UNDONE: The binding and conversion has to be executed in a checked context.
            Binder defaultValueBinder = this.GetBinder(defaultValueSyntax);
            Debug.Assert(defaultValueBinder != null);

            valueBeforeConversion = defaultValueBinder.BindValue(defaultValueSyntax.Value, diagnostics, BindValueKind.RValue);

            // Always generate the conversion, even if the expression is not convertible to the given type.
            // We want the erroneous conversion in the tree.
            var result = new BoundParameterEqualsValue(defaultValueSyntax, parameter, defaultValueBinder.GetDeclaredLocalsForScope(defaultValueSyntax),
                              defaultValueBinder.GenerateConversionForAssignment(parameter.Type, valueBeforeConversion, diagnostics, ConversionForAssignmentFlags.DefaultParameter));

            return result;
        }

        internal BoundFieldEqualsValue BindEnumConstantInitializer(
            SourceEnumConstantSymbol symbol,
            EqualsValueClauseSyntax equalsValueSyntax,
            BindingDiagnosticBag diagnostics)
        {
            Binder initializerBinder = this.GetBinder(equalsValueSyntax);
            Debug.Assert(initializerBinder != null);

            var initializer = initializerBinder.BindValue(equalsValueSyntax.Value, diagnostics, BindValueKind.RValue);
            initializer = initializerBinder.GenerateConversionForAssignment(symbol.ContainingType.EnumUnderlyingType, initializer, diagnostics);
            return new BoundFieldEqualsValue(equalsValueSyntax, symbol, initializerBinder.GetDeclaredLocalsForScope(equalsValueSyntax), initializer);
        }

        public BoundExpression BindExpression(ExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            return BindExpression(node, diagnostics: diagnostics, invoked: false, indexed: false);
        }

        protected BoundExpression BindExpression(ExpressionSyntax node, BindingDiagnosticBag diagnostics, bool invoked, bool indexed)
        {
            BoundExpression expr = BindExpressionInternal(node, diagnostics, invoked, indexed);
            CheckContextForPointerTypes(node, diagnostics, expr);

            if (expr.Kind == BoundKind.ArgListOperator)
            {
                // CS0226: An __arglist expression may only appear inside of a call or new expression
                Error(diagnostics, ErrorCode.ERR_IllegalArglist, node);
                expr = ToBadExpression(expr);
            }

            return expr;
        }

        // PERF: allowArgList is not a parameter because it is fairly uncommon case where arglists are allowed
        //       so we do not want to pass that argument to every BindExpression which is often recursive 
        //       and extra arguments contribute to the stack size.
        protected BoundExpression BindExpressionAllowArgList(ExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            BoundExpression expr = BindExpressionInternal(node, diagnostics, invoked: false, indexed: false);
            CheckContextForPointerTypes(node, diagnostics, expr);
            return expr;
        }

        private void CheckContextForPointerTypes(ExpressionSyntax node, BindingDiagnosticBag diagnostics, BoundExpression expr)
        {
            if (!expr.HasAnyErrors && !IsInsideNameof)
            {
                TypeSymbol exprType = expr.Type;
                if ((object)exprType != null && exprType.ContainsPointerOrFunctionPointer())
                {
                    ReportUnsafeIfNotAllowed(node, diagnostics);
                    //CONSIDER: Return a bad expression so that HasErrors is true?
                }
            }
        }

        private BoundExpression BindExpressionInternal(ExpressionSyntax node, BindingDiagnosticBag diagnostics, bool invoked, bool indexed)
        {
            if (IsEarlyAttributeBinder && !EarlyWellKnownAttributeBinder.CanBeValidAttributeArgument(node))
            {
                return BadExpression(node, LookupResultKind.NotAValue);
            }

            BoundExpression result = bindExpressionInternal(node, diagnostics, invoked, indexed);

            if (IsEarlyAttributeBinder && result.Kind == BoundKind.MethodGroup && (!IsInsideNameof || EnclosingNameofArgument != node))
            {
                return BadExpression(node, LookupResultKind.NotAValue);
            }

            return result;

            BoundExpression bindExpressionInternal(ExpressionSyntax node, BindingDiagnosticBag diagnostics, bool invoked, bool indexed)
            {
                Debug.Assert(node != null);
                switch (node.Kind())
                {
                    case SyntaxKind.AnonymousMethodExpression:
                    case SyntaxKind.ParenthesizedLambdaExpression:
                    case SyntaxKind.SimpleLambdaExpression:
                        return BindAnonymousFunction((AnonymousFunctionExpressionSyntax)node, diagnostics);
                    case SyntaxKind.ThisExpression:
                        return BindThis((ThisExpressionSyntax)node, diagnostics);
                    case SyntaxKind.BaseExpression:
                        return BindBase((BaseExpressionSyntax)node, diagnostics);
                    case SyntaxKind.FieldExpression:
                        return BindFieldExpression((FieldExpressionSyntax)node, diagnostics);
                    case SyntaxKind.InvocationExpression:
                        return BindInvocationExpression((InvocationExpressionSyntax)node, diagnostics);
                    case SyntaxKind.ArrayInitializerExpression:
                        return BindUnexpectedArrayInitializer((InitializerExpressionSyntax)node, diagnostics, ErrorCode.ERR_ArrayInitInBadPlace);
                    case SyntaxKind.ArrayCreationExpression:
                        return BindArrayCreationExpression((ArrayCreationExpressionSyntax)node, diagnostics);
                    case SyntaxKind.ImplicitArrayCreationExpression:
                        return BindImplicitArrayCreationExpression((ImplicitArrayCreationExpressionSyntax)node, diagnostics);
                    case SyntaxKind.StackAllocArrayCreationExpression:
                        return BindStackAllocArrayCreationExpression((StackAllocArrayCreationExpressionSyntax)node, diagnostics);
                    case SyntaxKind.ImplicitStackAllocArrayCreationExpression:
                        return BindImplicitStackAllocArrayCreationExpression((ImplicitStackAllocArrayCreationExpressionSyntax)node, diagnostics);
                    case SyntaxKind.ObjectCreationExpression:
                        return BindObjectCreationExpression((ObjectCreationExpressionSyntax)node, diagnostics);
                    case SyntaxKind.ImplicitObjectCreationExpression:
                        return BindImplicitObjectCreationExpression((ImplicitObjectCreationExpressionSyntax)node, diagnostics);
                    case SyntaxKind.IdentifierName:
                    case SyntaxKind.GenericName:
                        return BindIdentifier((SimpleNameSyntax)node, invoked, indexed, diagnostics);
                    case SyntaxKind.SimpleMemberAccessExpression:
                    case SyntaxKind.PointerMemberAccessExpression:
                        return BindMemberAccess((MemberAccessExpressionSyntax)node, invoked, indexed, diagnostics: diagnostics);
                    case SyntaxKind.SimpleAssignmentExpression:
                        return BindAssignment((AssignmentExpressionSyntax)node, diagnostics);
                    case SyntaxKind.CastExpression:
                        return BindCast((CastExpressionSyntax)node, diagnostics);
                    case SyntaxKind.ElementAccessExpression:
                        return BindElementAccess((ElementAccessExpressionSyntax)node, diagnostics);
                    case SyntaxKind.AddExpression:
                    case SyntaxKind.MultiplyExpression:
                    case SyntaxKind.SubtractExpression:
                    case SyntaxKind.DivideExpression:
                    case SyntaxKind.ModuloExpression:
                    case SyntaxKind.EqualsExpression:
                    case SyntaxKind.NotEqualsExpression:
                    case SyntaxKind.GreaterThanExpression:
                    case SyntaxKind.LessThanExpression:
                    case SyntaxKind.GreaterThanOrEqualExpression:
                    case SyntaxKind.LessThanOrEqualExpression:
                    case SyntaxKind.BitwiseAndExpression:
                    case SyntaxKind.BitwiseOrExpression:
                    case SyntaxKind.ExclusiveOrExpression:
                    case SyntaxKind.LeftShiftExpression:
                    case SyntaxKind.RightShiftExpression:
                    case SyntaxKind.UnsignedRightShiftExpression:
                        return BindSimpleBinaryOperator((BinaryExpressionSyntax)node, diagnostics);

                    case SyntaxKind.LogicalAndExpression:
                    case SyntaxKind.LogicalOrExpression:
                        return BindConditionalLogicalOperator((BinaryExpressionSyntax)node, diagnostics);
                    case SyntaxKind.CoalesceExpression:
                        return BindNullCoalescingOperator((BinaryExpressionSyntax)node, diagnostics);
                    case SyntaxKind.ConditionalAccessExpression:
                        return BindConditionalAccessExpression((ConditionalAccessExpressionSyntax)node, diagnostics);

                    case SyntaxKind.MemberBindingExpression:
                        return BindMemberBindingExpression((MemberBindingExpressionSyntax)node, invoked, indexed, diagnostics);

                    case SyntaxKind.ElementBindingExpression:
                        return BindElementBindingExpression((ElementBindingExpressionSyntax)node, diagnostics);

                    case SyntaxKind.IsExpression:
                        return BindIsOperator((BinaryExpressionSyntax)node, diagnostics);

                    case SyntaxKind.AsExpression:
                        return BindAsOperator((BinaryExpressionSyntax)node, diagnostics);

                    case SyntaxKind.UnaryPlusExpression:
                    case SyntaxKind.UnaryMinusExpression:
                    case SyntaxKind.LogicalNotExpression:
                    case SyntaxKind.BitwiseNotExpression:
                        return BindUnaryOperator((PrefixUnaryExpressionSyntax)node, diagnostics);

                    case SyntaxKind.IndexExpression:
                        return BindFromEndIndexExpression((PrefixUnaryExpressionSyntax)node, diagnostics);

                    case SyntaxKind.RangeExpression:
                        return BindRangeExpression((RangeExpressionSyntax)node, diagnostics);

                    case SyntaxKind.AddressOfExpression:
                        return BindAddressOfExpression((PrefixUnaryExpressionSyntax)node, diagnostics);

                    case SyntaxKind.PointerIndirectionExpression:
                        return BindPointerIndirectionExpression((PrefixUnaryExpressionSyntax)node, diagnostics);

                    case SyntaxKind.PostIncrementExpression:
                    case SyntaxKind.PostDecrementExpression:
                        return BindIncrementOperator(node, ((PostfixUnaryExpressionSyntax)node).Operand, ((PostfixUnaryExpressionSyntax)node).OperatorToken, diagnostics);

                    case SyntaxKind.PreIncrementExpression:
                    case SyntaxKind.PreDecrementExpression:
                        return BindIncrementOperator(node, ((PrefixUnaryExpressionSyntax)node).Operand, ((PrefixUnaryExpressionSyntax)node).OperatorToken, diagnostics);

                    case SyntaxKind.ConditionalExpression:
                        return BindConditionalOperator((ConditionalExpressionSyntax)node, diagnostics);

                    case SyntaxKind.SwitchExpression:
                        return BindSwitchExpression((SwitchExpressionSyntax)node, diagnostics);

                    case SyntaxKind.NumericLiteralExpression:
                    case SyntaxKind.StringLiteralExpression:
                    case SyntaxKind.CharacterLiteralExpression:
                    case SyntaxKind.TrueLiteralExpression:
                    case SyntaxKind.FalseLiteralExpression:
                    case SyntaxKind.NullLiteralExpression:
                        return BindLiteralConstant((LiteralExpressionSyntax)node, diagnostics);

                    case SyntaxKind.Utf8StringLiteralExpression:
                        return BindUtf8StringLiteral((LiteralExpressionSyntax)node, diagnostics);

                    case SyntaxKind.DefaultLiteralExpression:
                        MessageID.IDS_FeatureDefaultLiteral.CheckFeatureAvailability(diagnostics, node);
                        return new BoundDefaultLiteral(node);

                    case SyntaxKind.ParenthesizedExpression:
                        // Parenthesis tokens are ignored, and operand is bound in the context of parent
                        // expression.
                        return BindParenthesizedExpression(((ParenthesizedExpressionSyntax)node).Expression, diagnostics);

                    case SyntaxKind.UncheckedExpression:
                    case SyntaxKind.CheckedExpression:
                        return BindCheckedExpression((CheckedExpressionSyntax)node, diagnostics);

                    case SyntaxKind.DefaultExpression:
                        return BindDefaultExpression((DefaultExpressionSyntax)node, diagnostics);

                    case SyntaxKind.TypeOfExpression:
                        return BindTypeOf((TypeOfExpressionSyntax)node, diagnostics);

                    case SyntaxKind.SizeOfExpression:
                        return BindSizeOf((SizeOfExpressionSyntax)node, diagnostics);

                    case SyntaxKind.AddAssignmentExpression:
                    case SyntaxKind.AndAssignmentExpression:
                    case SyntaxKind.DivideAssignmentExpression:
                    case SyntaxKind.ExclusiveOrAssignmentExpression:
                    case SyntaxKind.LeftShiftAssignmentExpression:
                    case SyntaxKind.ModuloAssignmentExpression:
                    case SyntaxKind.MultiplyAssignmentExpression:
                    case SyntaxKind.OrAssignmentExpression:
                    case SyntaxKind.RightShiftAssignmentExpression:
                    case SyntaxKind.UnsignedRightShiftAssignmentExpression:
                    case SyntaxKind.SubtractAssignmentExpression:
                        return BindCompoundAssignment((AssignmentExpressionSyntax)node, diagnostics);

                    case SyntaxKind.CoalesceAssignmentExpression:
                        return BindNullCoalescingAssignmentOperator((AssignmentExpressionSyntax)node, diagnostics);

                    case SyntaxKind.AliasQualifiedName:
                    case SyntaxKind.PredefinedType:
                        return this.BindNamespaceOrType(node, diagnostics);

                    case SyntaxKind.QueryExpression:
                        return this.BindQuery((QueryExpressionSyntax)node, diagnostics);

                    case SyntaxKind.AnonymousObjectCreationExpression:
                        return BindAnonymousObjectCreation((AnonymousObjectCreationExpressionSyntax)node, diagnostics);

                    case SyntaxKind.QualifiedName:
                        return BindQualifiedName((QualifiedNameSyntax)node, diagnostics);

                    case SyntaxKind.ComplexElementInitializerExpression:
                        return BindUnexpectedComplexElementInitializer((InitializerExpressionSyntax)node, diagnostics);

                    case SyntaxKind.ArgListExpression:
                        return BindArgList(node, diagnostics);

                    case SyntaxKind.RefTypeExpression:
                        return BindRefType((RefTypeExpressionSyntax)node, diagnostics);

                    case SyntaxKind.MakeRefExpression:
                        return BindMakeRef((MakeRefExpressionSyntax)node, diagnostics);

                    case SyntaxKind.RefValueExpression:
                        return BindRefValue((RefValueExpressionSyntax)node, diagnostics);

                    case SyntaxKind.AwaitExpression:
                        return BindAwait((AwaitExpressionSyntax)node, diagnostics);

                    case SyntaxKind.OmittedArraySizeExpression:
                    case SyntaxKind.OmittedTypeArgument:
                    case SyntaxKind.ObjectInitializerExpression:
                        // Not reachable during method body binding, but
                        // may be used by SemanticModel for error cases.
                        return BadExpression(node);

                    case SyntaxKind.CollectionExpression:
                        return BindCollectionExpression((CollectionExpressionSyntax)node, diagnostics);

                    case SyntaxKind.NullableType:
                        // Not reachable during method body binding, but
                        // may be used by SemanticModel for error cases.
                        // NOTE: This happens when there's a problem with the Nullable<T> type (e.g. it's missing).
                        // There is no corresponding problem for array or pointer types (which seem analogous), since
                        // they are not constructed types; the element type can be an error type, but the array/pointer 
                        // type cannot.
                        return BadExpression(node);

                    case SyntaxKind.InterpolatedStringExpression:
                        return BindInterpolatedString((InterpolatedStringExpressionSyntax)node, diagnostics);

                    case SyntaxKind.IsPatternExpression:
                        return BindIsPatternExpression((IsPatternExpressionSyntax)node, diagnostics);

                    case SyntaxKind.TupleExpression:
                        return BindTupleExpression((TupleExpressionSyntax)node, diagnostics);

                    case SyntaxKind.ThrowExpression:
                        return BindThrowExpression((ThrowExpressionSyntax)node, diagnostics);

                    case SyntaxKind.RefType:
                        return BindRefType(node, diagnostics);

                    case SyntaxKind.ScopedType:
                        return BindScopedType(node, diagnostics);

                    case SyntaxKind.RefExpression:
                        return BindRefExpression((RefExpressionSyntax)node, diagnostics);

                    case SyntaxKind.DeclarationExpression:
                        return BindDeclarationExpressionAsError((DeclarationExpressionSyntax)node, diagnostics);

                    case SyntaxKind.SuppressNullableWarningExpression:
                        return BindSuppressNullableWarningExpression((PostfixUnaryExpressionSyntax)node, diagnostics);

                    case SyntaxKind.WithExpression:
                        return BindWithExpression((WithExpressionSyntax)node, diagnostics);

                    default:
                        // NOTE: We could probably throw an exception here, but it's conceivable
                        // that a non-parser syntax tree could reach this point with an unexpected
                        // SyntaxKind and we don't want to throw if that occurs.
                        Debug.Assert(false, "Unexpected SyntaxKind " + node.Kind());
                        diagnostics.Add(ErrorCode.ERR_InternalError, node.Location);
                        return BadExpression(node);
                }
            }
        }

#nullable enable
        internal virtual BoundSwitchExpressionArm BindSwitchExpressionArm(SwitchExpressionArmSyntax node, TypeSymbol switchGoverningType, BindingDiagnosticBag diagnostics)
        {
            return this.NextRequired.BindSwitchExpressionArm(node, switchGoverningType, diagnostics);
        }
#nullable disable

        private BoundExpression BindRefExpression(RefExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            var firstToken = node.GetFirstToken();
            diagnostics.Add(ErrorCode.ERR_UnexpectedToken, firstToken.GetLocation(), firstToken.ValueText);
            return new BoundBadExpression(
                node, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundExpression>(BindToTypeForErrorRecovery(BindValue(node.Expression, BindingDiagnosticBag.Discarded, BindValueKind.RefersToLocation))),
                CreateErrorType("ref"));
        }

        private BoundExpression BindRefType(ExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            var firstToken = node.GetFirstToken();
            diagnostics.Add(ErrorCode.ERR_UnexpectedToken, firstToken.GetLocation(), firstToken.ValueText);
            return new BoundTypeExpression(node, null, CreateErrorType("ref"));
        }

        private BoundExpression BindScopedType(ExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            var firstToken = node.GetFirstToken();
            diagnostics.Add(ErrorCode.ERR_UnexpectedToken, firstToken.GetLocation(), firstToken.ValueText);
            return new BoundTypeExpression(node, null, CreateErrorType("scoped"));
        }

        private BoundExpression BindThrowExpression(ThrowExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            MessageID.IDS_FeatureThrowExpression.CheckFeatureAvailability(diagnostics, node.ThrowKeyword);

            bool hasErrors = node.HasErrors;
            if (!IsThrowExpressionInProperContext(node))
            {
                diagnostics.Add(ErrorCode.ERR_ThrowMisplaced, node.ThrowKeyword.GetLocation());
                hasErrors = true;
            }

            var thrownExpression = BindThrownExpression(node.Expression, diagnostics, ref hasErrors);
            return new BoundThrowExpression(node, thrownExpression, null, hasErrors);
        }

        private static bool IsThrowExpressionInProperContext(ThrowExpressionSyntax node)
        {
            var parent = node.Parent;
            if (parent == null || node.HasErrors)
            {
                return true;
            }

            switch (parent.Kind())
            {
                case SyntaxKind.ConditionalExpression: // ?:
                    {
                        var conditionalParent = (ConditionalExpressionSyntax)parent;
                        return node == conditionalParent.WhenTrue || node == conditionalParent.WhenFalse;
                    }
                case SyntaxKind.CoalesceExpression: // ??
                    {
                        var binaryParent = (BinaryExpressionSyntax)parent;
                        return node == binaryParent.Right;
                    }
                case SyntaxKind.SwitchExpressionArm:
                case SyntaxKind.ArrowExpressionClause:
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                    return true;
                // We do not support && and || because
                // 1. The precedence would not syntactically allow it
                // 2. It isn't clear what the semantics should be
                // 3. It isn't clear what use cases would motivate us to change the precedence to support it
                default:
                    return false;
            }
        }

        // Bind a declaration expression where it isn't permitted.
        private BoundExpression BindDeclarationExpressionAsError(DeclarationExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            // This is an error, as declaration expressions are handled specially in every context in which
            // they are permitted. So we have a context in which they are *not* permitted. Nevertheless, we
            // bind it and then give one nice message.

            bool isVar;
            bool isConst = false;
            AliasSymbol alias;
            var declType = BindVariableTypeWithAnnotations(node.Designation, diagnostics, node.Type.SkipScoped(out _).SkipRef(), ref isConst, out isVar, out alias);
            Error(diagnostics, ErrorCode.ERR_DeclarationExpressionNotPermitted, node);
            return BindDeclarationVariablesForErrorRecovery(declType, node.Designation, node, diagnostics);
        }

        /// <summary>
        /// Bind a declaration variable where it isn't permitted. The caller is expected to produce a diagnostic.
        /// </summary>
        private BoundExpression BindDeclarationVariablesForErrorRecovery(TypeWithAnnotations declTypeWithAnnotations, VariableDesignationSyntax node, CSharpSyntaxNode syntax, BindingDiagnosticBag diagnostics)
        {
            declTypeWithAnnotations = declTypeWithAnnotations.HasType ? declTypeWithAnnotations : TypeWithAnnotations.Create(CreateErrorType("var"));
            switch (node.Kind())
            {
                case SyntaxKind.SingleVariableDesignation:
                    {
                        var single = (SingleVariableDesignationSyntax)node;
                        var result = BindDeconstructionVariable(declTypeWithAnnotations, single, syntax, diagnostics);
                        return BindToTypeForErrorRecovery(result);
                    }
                case SyntaxKind.DiscardDesignation:
                    {
                        return BindDiscardExpression(syntax, declTypeWithAnnotations);
                    }
                case SyntaxKind.ParenthesizedVariableDesignation:
                    {
                        var tuple = (ParenthesizedVariableDesignationSyntax)node;
                        int count = tuple.Variables.Count;
                        var builder = ArrayBuilder<BoundExpression>.GetInstance(count);
                        var namesBuilder = ArrayBuilder<string>.GetInstance(count);

                        foreach (var n in tuple.Variables)
                        {
                            builder.Add(BindDeclarationVariablesForErrorRecovery(declTypeWithAnnotations, n, n, diagnostics));
                            namesBuilder.Add(InferTupleElementName(n));
                        }
                        ImmutableArray<BoundExpression> subExpressions = builder.ToImmutableAndFree();

                        var uniqueFieldNames = PooledHashSet<string>.GetInstance();
                        RemoveDuplicateInferredTupleNamesAndFreeIfEmptied(ref namesBuilder, uniqueFieldNames);
                        uniqueFieldNames.Free();

                        ImmutableArray<string> tupleNames = namesBuilder is null ? default : namesBuilder.ToImmutableAndFree();
                        ImmutableArray<bool> inferredPositions = tupleNames.IsDefault ? default : tupleNames.SelectAsArray(n => n != null);
                        bool disallowInferredNames = this.Compilation.LanguageVersion.DisallowInferredTupleElementNames();

                        // We will not check constraints at this point as this code path
                        // is failure-only and the caller is expected to produce a diagnostic.
                        var tupleType = NamedTypeSymbol.CreateTuple(
                            locationOpt: null,
                            subExpressions.SelectAsArray(e => TypeWithAnnotations.Create(e.Type)),
                            elementLocations: default,
                            tupleNames,
                            Compilation,
                            shouldCheckConstraints: false,
                            includeNullability: false,
                            errorPositions: disallowInferredNames ? inferredPositions : default);

                        return new BoundConvertedTupleLiteral(syntax, sourceTuple: null, wasTargetTyped: true, subExpressions, tupleNames, inferredPositions, tupleType);
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        private BoundExpression BindTupleExpression(TupleExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            MessageID.IDS_FeatureTuples.CheckFeatureAvailability(diagnostics, node);

            SeparatedSyntaxList<ArgumentSyntax> arguments = node.Arguments;
            int numElements = arguments.Count;

            if (numElements < 2)
            {
                // this should be a parse error already.
                var args = numElements == 1 ?
                    ImmutableArray.Create(BindValue(arguments[0].Expression, diagnostics, BindValueKind.RValue)) :
                    ImmutableArray<BoundExpression>.Empty;

                return BadExpression(node, args);
            }

            bool hasNaturalType = true;

            var boundArguments = ArrayBuilder<BoundExpression>.GetInstance(arguments.Count);
            var elementTypesWithAnnotations = ArrayBuilder<TypeWithAnnotations>.GetInstance(arguments.Count);
            var elementLocations = ArrayBuilder<Location>.GetInstance(arguments.Count);

            // prepare names
            var (elementNames, inferredPositions, hasErrors) = ExtractTupleElementNames(arguments, diagnostics);

            // prepare types and locations
            for (int i = 0; i < numElements; i++)
            {
                ArgumentSyntax argumentSyntax = arguments[i];
                IdentifierNameSyntax nameSyntax = argumentSyntax.NameColon?.Name;

                if (nameSyntax != null)
                {
                    elementLocations.Add(nameSyntax.Location);
                }
                else
                {
                    elementLocations.Add(argumentSyntax.Location);
                }

                BoundExpression boundArgument = BindValue(argumentSyntax.Expression, diagnostics, BindValueKind.RValue);
                if (boundArgument.Type?.SpecialType == SpecialType.System_Void)
                {
                    diagnostics.Add(ErrorCode.ERR_VoidInTuple, argumentSyntax.Location);
                    boundArgument = new BoundBadExpression(
                        argumentSyntax, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty,
                        ImmutableArray.Create<BoundExpression>(boundArgument), CreateErrorType("void"));
                }

                boundArguments.Add(boundArgument);

                var elementTypeWithAnnotations = TypeWithAnnotations.Create(boundArgument.Type);
                elementTypesWithAnnotations.Add(elementTypeWithAnnotations);

                if (!elementTypeWithAnnotations.HasType)
                {
                    hasNaturalType = false;
                }
            }

            NamedTypeSymbol tupleTypeOpt = null;
            var elements = elementTypesWithAnnotations.ToImmutableAndFree();
            var locations = elementLocations.ToImmutableAndFree();

            if (hasNaturalType)
            {
                bool disallowInferredNames = this.Compilation.LanguageVersion.DisallowInferredTupleElementNames();

                tupleTypeOpt = NamedTypeSymbol.CreateTuple(node.Location, elements, locations, elementNames,
                    this.Compilation, syntax: node, diagnostics: diagnostics, shouldCheckConstraints: true,
                    includeNullability: false, errorPositions: disallowInferredNames ? inferredPositions : default(ImmutableArray<bool>));
            }
            else
            {
                NamedTypeSymbol.VerifyTupleTypePresent(elements.Length, node, this.Compilation, diagnostics);
            }

            // Always track the inferred positions in the bound node, so that conversions don't produce a warning
            // for "dropped names" on tuple literal when the name was inferred.
            return new BoundTupleLiteral(node, boundArguments.ToImmutableAndFree(), elementNames, inferredPositions, tupleTypeOpt, hasErrors);
        }

        private static (ImmutableArray<string> elementNamesArray, ImmutableArray<bool> inferredArray, bool hasErrors) ExtractTupleElementNames(
            SeparatedSyntaxList<ArgumentSyntax> arguments, BindingDiagnosticBag diagnostics)
        {
            bool hasErrors = false;
            int numElements = arguments.Count;
            var uniqueFieldNames = PooledHashSet<string>.GetInstance();
            ArrayBuilder<string> elementNames = null;
            ArrayBuilder<string> inferredElementNames = null;

            for (int i = 0; i < numElements; i++)
            {
                ArgumentSyntax argumentSyntax = arguments[i];
                IdentifierNameSyntax nameSyntax = argumentSyntax.NameColon?.Name;

                string name = null;
                string inferredName = null;

                if (nameSyntax != null)
                {
                    name = nameSyntax.Identifier.ValueText;

                    if (diagnostics != null && !CheckTupleMemberName(name, i, argumentSyntax.NameColon.Name, diagnostics, uniqueFieldNames))
                    {
                        hasErrors = true;
                    }
                }
                else
                {
                    inferredName = InferTupleElementName(argumentSyntax.Expression);
                }

                CollectTupleFieldMemberName(name, i, numElements, ref elementNames);
                CollectTupleFieldMemberName(inferredName, i, numElements, ref inferredElementNames);
            }

            RemoveDuplicateInferredTupleNamesAndFreeIfEmptied(ref inferredElementNames, uniqueFieldNames);
            uniqueFieldNames.Free();

            var result = MergeTupleElementNames(elementNames, inferredElementNames);
            elementNames?.Free();
            inferredElementNames?.Free();
            return (result.names, result.inferred, hasErrors);
        }

        private static (ImmutableArray<string> names, ImmutableArray<bool> inferred) MergeTupleElementNames(
            ArrayBuilder<string> elementNames, ArrayBuilder<string> inferredElementNames)
        {
            if (elementNames == null)
            {
                if (inferredElementNames == null)
                {
                    return (default(ImmutableArray<string>), default(ImmutableArray<bool>));
                }
                else
                {
                    var finalNames = inferredElementNames.ToImmutable();
                    return (finalNames, finalNames.SelectAsArray(n => n != null));
                }
            }

            if (inferredElementNames == null)
            {
                return (elementNames.ToImmutable(), default(ImmutableArray<bool>));
            }

            Debug.Assert(elementNames.Count == inferredElementNames.Count);
            var builder = ArrayBuilder<bool>.GetInstance(elementNames.Count);
            for (int i = 0; i < elementNames.Count; i++)
            {
                string inferredName = inferredElementNames[i];
                if (elementNames[i] == null && inferredName != null)
                {
                    elementNames[i] = inferredName;
                    builder.Add(true);
                }
                else
                {
                    builder.Add(false);
                }
            }

            return (elementNames.ToImmutable(), builder.ToImmutableAndFree());
        }

        /// <summary>
        /// Removes duplicate entries in <paramref name="inferredElementNames"/> and frees it if only nulls remain.
        /// </summary>
        private static void RemoveDuplicateInferredTupleNamesAndFreeIfEmptied(ref ArrayBuilder<string> inferredElementNames, HashSet<string> uniqueFieldNames)
        {
            if (inferredElementNames == null)
            {
                return;
            }

            // Inferred names that duplicate an explicit name or a previous inferred name are tagged for removal
            var toRemove = PooledHashSet<string>.GetInstance();
            foreach (var name in inferredElementNames)
            {
                if (name != null && !uniqueFieldNames.Add(name))
                {
                    toRemove.Add(name);
                }
            }

            for (int i = 0; i < inferredElementNames.Count; i++)
            {
                var inferredName = inferredElementNames[i];
                if (inferredName != null && toRemove.Contains(inferredName))
                {
                    inferredElementNames[i] = null;
                }
            }
            toRemove.Free();

            if (inferredElementNames.All(n => n is null))
            {
                inferredElementNames.Free();
                inferredElementNames = null;
            }
        }

        private static string InferTupleElementName(SyntaxNode syntax)
        {
            string name = syntax.TryGetInferredMemberName();

            // Reserved names are never candidates to be inferred names, at any position
            if (name == null || NamedTypeSymbol.IsTupleElementNameReserved(name) != -1)
            {
                return null;
            }

            return name;
        }

        private BoundExpression BindRefValue(RefValueExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            // __refvalue(tr, T) requires that tr be a TypedReference and T be a type.
            // The result is a *variable* of type T.

            BoundExpression argument = BindValue(node.Expression, diagnostics, BindValueKind.RValue);
            bool hasErrors = argument.HasAnyErrors;

            TypeSymbol typedReferenceType = this.Compilation.GetSpecialType(SpecialType.System_TypedReference);
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            Conversion conversion = this.Conversions.ClassifyConversionFromExpression(argument, typedReferenceType, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);
            diagnostics.Add(node, useSiteInfo);
            if (!conversion.IsImplicit || !conversion.IsValid)
            {
                hasErrors = true;
                GenerateImplicitConversionError(diagnostics, node, conversion, argument, typedReferenceType);
            }

            argument = CreateConversion(argument, conversion, typedReferenceType, diagnostics);

            TypeWithAnnotations typeWithAnnotations = BindType(node.Type, diagnostics);

            return new BoundRefValueOperator(node, typeWithAnnotations.NullableAnnotation, argument, typeWithAnnotations.Type, hasErrors);
        }

        private BoundExpression BindMakeRef(MakeRefExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            // __makeref(x) requires that x be a variable, and not be of a restricted type.
            BoundExpression argument = this.BindValue(node.Expression, diagnostics, BindValueKind.RefOrOut);

            bool hasErrors = argument.HasAnyErrors;

            TypeSymbol typedReferenceType = GetSpecialType(SpecialType.System_TypedReference, diagnostics, node);

            if ((object)argument.Type != null && argument.Type.IsRestrictedType())
            {
                // CS1601: Cannot make reference to variable of type '{0}'
                Error(diagnostics, ErrorCode.ERR_MethodArgCantBeRefAny, node, argument.Type);
                hasErrors = true;
            }

            // UNDONE: We do not yet implement warnings anywhere for:
            // UNDONE: * taking a ref to a volatile field
            // UNDONE: * taking a ref to a "non-agile" field
            // UNDONE: We should do so here when we implement this feature for regular out/ref parameters.

            return new BoundMakeRefOperator(node, argument, typedReferenceType, hasErrors);
        }

        private BoundExpression BindRefType(RefTypeExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            // __reftype(x) requires that x be implicitly convertible to TypedReference.

            BoundExpression argument = BindValue(node.Expression, diagnostics, BindValueKind.RValue);
            bool hasErrors = argument.HasAnyErrors;

            TypeSymbol typedReferenceType = this.Compilation.GetSpecialType(SpecialType.System_TypedReference);
            TypeSymbol typeType = this.GetWellKnownType(WellKnownType.System_Type, diagnostics, node);
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            Conversion conversion = this.Conversions.ClassifyConversionFromExpression(argument, typedReferenceType, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);
            diagnostics.Add(node, useSiteInfo);
            if (!conversion.IsImplicit || !conversion.IsValid)
            {
                hasErrors = true;
                GenerateImplicitConversionError(diagnostics, node, conversion, argument, typedReferenceType);
            }

            argument = CreateConversion(argument, conversion, typedReferenceType, diagnostics);
            return new BoundRefTypeOperator(node, argument, null, typeType, hasErrors);
        }

        private BoundExpression BindArgList(CSharpSyntaxNode node, BindingDiagnosticBag diagnostics)
        {
            // There are two forms of __arglist expression. In a method with an __arglist parameter,
            // it is legal to use __arglist as an expression of type RuntimeArgumentHandle. In 
            // a call to such a method, it is legal to use __arglist(x, y, z) as the final argument.
            // This method only handles the first usage; the second usage is parsed as a call syntax.

            // The native compiler allows __arglist in a lambda:
            //
            // class C
            // {
            //   delegate int D(RuntimeArgumentHandle r);
            //   static void M(__arglist)
            //   {
            //     D f = null;
            //     f = r=>f(__arglist);
            //   }
            // }
            //
            // This is clearly wrong. Either the developer intends __arglist to refer to the 
            // arg list of the *lambda*, or to the arg list of *M*. The former makes no sense;
            // lambdas cannot have an arg list. The latter we have no way to generate code for;
            // you cannot hoist the arg list to a field of a closure class.
            //
            // The native compiler allows this and generates code as though the developer
            // was attempting to access the arg list of the lambda! We should simply disallow it.

            TypeSymbol runtimeArgumentHandleType = GetSpecialType(SpecialType.System_RuntimeArgumentHandle, diagnostics, node);

            MethodSymbol method = this.ContainingMember() as MethodSymbol;

            bool hasError = false;

            if ((object)method == null || !method.IsVararg)
            {
                // CS0190: The __arglist construct is valid only within a variable argument method
                Error(diagnostics, ErrorCode.ERR_ArgsInvalid, node);
                hasError = true;
            }
            else
            {
                // We're in a varargs method; are we also inside a lambda?
                Symbol container = this.ContainingMemberOrLambda;
                if (container != method)
                {
                    // We also need to report this any time a local variable of a restricted type
                    // would be hoisted into a closure for an anonymous function, iterator or async method.
                    // We do that during the actual rewrites.

                    // CS4013: Instance of type '{0}' cannot be used inside an anonymous function, query expression, iterator block or async method
                    Error(diagnostics, ErrorCode.ERR_SpecialByRefInLambda, node, runtimeArgumentHandleType);

                    hasError = true;
                }
            }

            return new BoundArgList(node, runtimeArgumentHandleType, hasError);
        }

        /// <summary>
        /// This can be reached for the qualified name on the right-hand-side of an `is` operator.
        /// For compatibility we parse it as a qualified name, as the is-type expression only permitted
        /// a type on the right-hand-side in C# 6. But the same syntax now, in C# 7 and later, can
        /// refer to a constant, which would normally be represented as a *simple member access expression*.
        /// Since the parser cannot distinguish, it parses it as before and depends on the binder
        /// to handle a qualified name appearing as an expression.
        /// </summary>
        private BoundExpression BindQualifiedName(QualifiedNameSyntax node, BindingDiagnosticBag diagnostics)
        {
            return BindMemberAccessWithBoundLeft(node, this.BindLeftOfPotentialColorColorMemberAccess(node.Left, diagnostics), node.Right, node.DotToken, invoked: false, indexed: false, diagnostics: diagnostics);
        }

        private BoundExpression BindParenthesizedExpression(ExpressionSyntax innerExpression, BindingDiagnosticBag diagnostics)
        {
            var result = BindExpression(innerExpression, diagnostics);

            // A parenthesized expression may not be a namespace or a type. If it is a parenthesized
            // namespace or type then report the error but let it go; we'll just ignore the
            // parenthesis and keep on trucking.
            CheckNotNamespaceOrType(result, diagnostics);
            return result;
        }

#nullable enable
        private BoundExpression BindTypeOf(TypeOfExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            ExpressionSyntax typeSyntax = node.Type;

            TypeofBinder typeofBinder = new TypeofBinder(typeSyntax, this); //has special handling for unbound types
            AliasSymbol alias;
            TypeWithAnnotations typeWithAnnotations = typeofBinder.BindType(typeSyntax, diagnostics, out alias);
            TypeSymbol type = typeWithAnnotations.Type;

            bool hasError = false;

            // NB: Dev10 has an error for typeof(dynamic), but allows typeof(dynamic[]),
            // typeof(C<dynamic>), etc.
            if (type.IsDynamic())
            {
                diagnostics.Add(ErrorCode.ERR_BadDynamicTypeof, node.Location);
                hasError = true;
            }
            else if (typeWithAnnotations.NullableAnnotation.IsAnnotated() && type.IsReferenceType)
            {
                // error: cannot take the `typeof` a nullable reference type.
                diagnostics.Add(ErrorCode.ERR_BadNullableTypeof, node.Location);
                hasError = true;
            }

            BoundTypeExpression boundType = new BoundTypeExpression(typeSyntax, alias, typeWithAnnotations, type.IsErrorType());
            return new BoundTypeOfOperator(node, boundType, null, this.GetWellKnownType(WellKnownType.System_Type, diagnostics, node), hasError);
        }

        /// <summary>Called when an "attribute-dependent" type such as 'dynamic', 'string?', etc. is not permitted.</summary>
        private void CheckDisallowedAttributeDependentType(TypeWithAnnotations typeArgument, NameSyntax attributeName, BindingDiagnosticBag diagnostics)
        {
            typeArgument.VisitType(type: null, static (typeWithAnnotations, arg, _) =>
            {
                var (attributeName, diagnostics) = arg;
                var type = typeWithAnnotations.Type;
                if (type.IsDynamic()
                    || (typeWithAnnotations.NullableAnnotation.IsAnnotated() && !type.IsValueType)
                    || type.IsNativeIntegerWrapperType
                    || (type.IsTupleType && !type.TupleElementNames.IsDefault))
                {
                    diagnostics.Add(ErrorCode.ERR_AttrDependentTypeNotAllowed, attributeName, type);
                    return true;
                }

                if (type.IsUnboundGenericType() || type.Kind == SymbolKind.TypeParameter)
                {
                    diagnostics.Add(ErrorCode.ERR_AttrTypeArgCannotBeTypeVar, attributeName, type);
                    return true;
                }

                return false;
            }, typePredicate: null, arg: (attributeName, diagnostics));
        }

        private BoundExpression BindSizeOf(SizeOfExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            ExpressionSyntax typeSyntax = node.Type;
            AliasSymbol alias;
            TypeWithAnnotations typeWithAnnotations = this.BindType(typeSyntax, diagnostics, out alias);
            TypeSymbol type = typeWithAnnotations.Type;

            bool typeHasErrors = type.IsErrorType() || CheckManagedAddr(Compilation, type, node.Location, diagnostics);

            BoundTypeExpression boundType = new BoundTypeExpression(typeSyntax, alias, typeWithAnnotations, typeHasErrors);
            ConstantValue constantValue = GetConstantSizeOf(type);
            bool hasErrors = constantValue is null && ReportUnsafeIfNotAllowed(node, diagnostics, type);
            return new BoundSizeOfOperator(node, boundType, constantValue,
                this.GetSpecialType(SpecialType.System_Int32, diagnostics, node), hasErrors);
        }

        private BoundExpression BindFieldExpression(FieldExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(ContainingType is { });
            FieldSymbol? field = null;

            if (hasOtherFieldSymbolInScope())
            {
                diagnostics.Add(ErrorCode.WRN_FieldIsAmbiguous, node, Compilation.LanguageVersion.ToDisplayString());
            }

            switch (ContainingMember())
            {
                case SynthesizedBackingFieldSymbolBase backingField:
                    field = backingField;
                    break;
                case MethodSymbol { AssociatedSymbol: SourcePropertySymbol property }:
                    field = property.BackingField;
                    break;
                case MethodSymbol { AssociatedSymbol.OriginalDefinition: PEPropertySymbol property } method when
                        (Flags & BinderFlags.InEEMethodBinder) != 0 &&
                        IsPropertyWithBackingField(property, out FieldSymbol? backingField):

                    field = backingField.AsMember(method.ContainingType);
                    break;
                default:
                    {
                        Debug.Assert((this.Flags & BinderFlags.InContextualAttributeBinder) != 0);
                        var contextualAttributeBinder = TryGetContextualAttributeBinder(this);
                        if (contextualAttributeBinder is { AttributeTarget: MethodSymbol { AssociatedSymbol: SourcePropertySymbol property } })
                        {
                            field = property.BackingField;
                        }
                        break;
                    }
            }

            // Field will be null when binding a field expression in a speculative
            // semantic model when the property does not have a backing field.
            if (field is null)
            {
                diagnostics.Add(ErrorCode.ERR_NoSuchMember, node, ContainingMember(), "field");
                return BadExpression(node);
            }

            var implicitReceiver = field.IsStatic ? null : ThisReference(node, field.ContainingType, wasCompilerGenerated: true);
            return new BoundFieldAccess(node, implicitReceiver, field, constantValueOpt: null);

            bool hasOtherFieldSymbolInScope()
            {
                var lookupResult = LookupResult.GetInstance();
                var useSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                this.LookupIdentifier(lookupResult, name: "field", arity: 0, invoked: false, ref useSiteInfo);
                bool result = lookupResult.Kind != LookupResultKind.Empty;
                Debug.Assert(!result || lookupResult.Symbols.Count > 0);
                lookupResult.Free();
                return result;
            }
        }

        internal static bool IsPropertyWithBackingField(PEPropertySymbol property, [NotNullWhen(true)] out FieldSymbol? backingField)
        {
            if (!property.IsExtensionBlockMember() &&
                property.ContainingType.GetMembers(GeneratedNames.MakeBackingFieldName(property.Name)) is [FieldSymbol candidateField] &&
                candidateField.RefKind == property.RefKind &&
                candidateField.IsStatic == property.IsStatic &&
                candidateField.Type.Equals(property.Type, TypeCompareKind.AllIgnoreOptions))
            {
                backingField = candidateField;
                return true;
            }

            backingField = null;
            return false;
        }

        /// <summary>
        /// Report diagnostic for variable declared with name 'field' within an accessor.
        /// </summary>
        /// <param name="symbol">
        /// Optional symbol for the variable. The symbol should be locally declared.
        /// That is, it should be a symbol that would hide a backing field in earlier
        /// language versions. If a symbol is not provided, the caller is responsible
        /// for ensuring the identifier refers to a locally declared variable.
        /// </param>
        internal void ReportFieldContextualKeywordConflictIfAny(Symbol? symbol, SyntaxNode syntax, SyntaxToken identifier, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(symbol is null or LocalSymbol or LocalFunctionSymbol or RangeVariableSymbol or TypeParameterSymbol);

            string name = identifier.Text;
            if (name == "field" &&
                ContainingMember() is MethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet, AssociatedSymbol: PropertySymbol { IsIndexer: false } })
            {
                var requiredVersion = MessageID.IDS_FeatureFieldKeyword.RequiredVersion();
                if (Compilation.LanguageVersion >= requiredVersion)
                {
                    diagnostics.Add(ErrorCode.ERR_VariableDeclarationNamedField, syntax, requiredVersion.ToDisplayString());
                }
            }
        }

        internal void ReportFieldContextualKeywordConflictIfAny(ParameterSyntax syntax, BindingDiagnosticBag diagnostics)
        {
            ReportFieldContextualKeywordConflictIfAny(symbol: null, syntax, syntax.Identifier, diagnostics);
        }

        /// <returns>true if managed type-related errors were found, otherwise false.</returns>
        internal static bool CheckManagedAddr(CSharpCompilation compilation, TypeSymbol type, Location location, BindingDiagnosticBag diagnostics, bool errorForManaged = false)
        {
            // Skip the check for error types that represent truly missing types (not found),
            // but still report for error types due to other issues (e.g., inaccessibility).
            if (type is ErrorTypeSymbol { ResultKind: LookupResultKind.Empty })
                return false;

            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, compilation.Assembly);
            var managedKind = type.GetManagedKind(ref useSiteInfo);
            diagnostics.Add(location, useSiteInfo);

            return CheckManagedAddr(compilation, type, managedKind, location, diagnostics, errorForManaged);
        }

        /// <returns>true if managed type-related errors were found, otherwise false.</returns>
        internal static bool CheckManagedAddr(CSharpCompilation compilation, TypeSymbol type, ManagedKind managedKind, Location location, BindingDiagnosticBag diagnostics, bool errorForManaged = false)
        {
            switch (managedKind)
            {
                case ManagedKind.Managed:
                    if (errorForManaged)
                    {
                        diagnostics.Add(ErrorCode.ERR_ManagedAddr, location, type);
                        return true;
                    }

                    diagnostics.Add(ErrorCode.WRN_ManagedAddr, location, type);
                    return false;
                case ManagedKind.UnmanagedWithGenerics when MessageID.IDS_FeatureUnmanagedConstructedTypes.GetFeatureAvailabilityDiagnosticInfo(compilation) is CSDiagnosticInfo diagnosticInfo:
                    diagnostics.Add(diagnosticInfo, location);
                    return true;
                case ManagedKind.Unknown:
                    throw ExceptionUtilities.UnexpectedValue(managedKind);
                default:
                    return false;
            }
        }
#nullable disable

        internal static ConstantValue GetConstantSizeOf(TypeSymbol type)
        {
            return ConstantValue.CreateSizeOf((type.GetEnumUnderlyingType() ?? type).SpecialType);
        }

        private BoundExpression BindDefaultExpression(DefaultExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            MessageID.IDS_FeatureDefault.CheckFeatureAvailability(diagnostics, node.Keyword);

            TypeWithAnnotations typeWithAnnotations = this.BindType(node.Type, diagnostics, out AliasSymbol alias);
            var typeExpression = new BoundTypeExpression(node.Type, aliasOpt: alias, typeWithAnnotations);
            TypeSymbol type = typeWithAnnotations.Type;
            return new BoundDefaultExpression(node, typeExpression, constantValueOpt: type.GetDefaultValue(), type);
        }

        /// <summary>
        /// Binds a simple identifier.
        /// </summary>
        private BoundExpression BindIdentifier(
            SimpleNameSyntax node,
            bool invoked,
            bool indexed,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);

#if DEBUG
            AdjustIdentifierMapIfAny(node, invoked);
#endif

            // If the syntax tree is ill-formed and the identifier is missing then we've already
            // given a parse error. Just return an error local and continue with analysis.
            if (node.IsMissing)
            {
                return BadExpression(node);
            }

            // A simple-name is either of the form I or of the form I<A1, ..., AK>, where I is a
            // single identifier and <A1, ..., AK> is an optional type-argument-list. When no
            // type-argument-list is specified, consider K to be zero. The simple-name is evaluated
            // and classified as follows:

            // If K is zero and the simple-name appears within a block and if the block's (or an
            // enclosing block's) local variable declaration space contains a local variable,
            // parameter or constant with name I, then the simple-name refers to that local
            // variable, parameter or constant and is classified as a variable or value.

            // If K is zero and the simple-name appears within the body of a generic method
            // declaration and if that declaration includes a type parameter with name I, then the
            // simple-name refers to that type parameter.

            BoundExpression expression;

            // It's possible that the argument list is malformed; if so, do not attempt to bind it;
            // just use the null array.

            bool hasTypeArguments = node.Arity > 0;

            SeparatedSyntaxList<TypeSyntax> typeArgumentList = node.Kind() == SyntaxKind.GenericName
                ? ((GenericNameSyntax)node).TypeArgumentList.Arguments
                : default(SeparatedSyntaxList<TypeSyntax>);

            Debug.Assert(node.Arity == typeArgumentList.Count);

            var typeArgumentsWithAnnotations = hasTypeArguments ?
                BindTypeArguments(typeArgumentList, diagnostics) :
                default(ImmutableArray<TypeWithAnnotations>);

            var lookupResult = LookupResult.GetInstance();
            var name = node.Identifier.ValueText;
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            this.LookupIdentifier(lookupResult, node, invoked, ref useSiteInfo);
            diagnostics.Add(node, useSiteInfo);

            if (lookupResult.Kind != LookupResultKind.Empty)
            {
                // have we detected an error with the current node?
                bool isError;
                var members = ArrayBuilder<Symbol>.GetInstance();
                Symbol symbol = GetSymbolOrMethodOrPropertyGroup(lookupResult, node, name, node.Arity, members, diagnostics, out isError, qualifierOpt: null);  // reports diagnostics in result.

                if ((object)symbol == null)
                {
                    Debug.Assert(members.Count > 0);

                    var receiver = SynthesizeMethodGroupReceiver(node, members);
                    Debug.Assert(!IsTypeOrValueExpression(receiver));

                    expression = ConstructBoundMemberGroupAndReportOmittedTypeArguments(
                        node,
                        typeArgumentList,
                        typeArgumentsWithAnnotations,
                        receiver,
                        name,
                        members,
                        lookupResult,
                        receiver != null ? BoundMethodGroupFlags.HasImplicitReceiver : BoundMethodGroupFlags.None,
                        isError,
                        diagnostics);

                    ReportSimpleProgramLocalReferencedOutsideOfTopLevelStatement(node, members[0], diagnostics);
                }
                else
                {
                    bool isNamedType = (symbol.Kind == SymbolKind.NamedType) || (symbol.Kind == SymbolKind.ErrorType);

                    if (hasTypeArguments && isNamedType)
                    {
                        symbol = ConstructNamedTypeUnlessTypeArgumentOmitted(node, (NamedTypeSymbol)symbol, typeArgumentList, typeArgumentsWithAnnotations, diagnostics);
                    }

                    expression = BindNonMethod(node, symbol, diagnostics, lookupResult.Kind, indexed, isError);

                    if (!isNamedType && (hasTypeArguments || node.Kind() == SyntaxKind.GenericName))
                    {
                        Debug.Assert(isError); // Should have been reported by GetSymbolOrMethodOrPropertyGroup.
                        expression = new BoundBadExpression(
                            syntax: node,
                            resultKind: LookupResultKind.WrongArity,
                            symbols: ImmutableArray.Create(symbol),
                            childBoundNodes: ImmutableArray.Create(BindToTypeForErrorRecovery(expression)),
                            type: expression.Type,
                            hasErrors: isError);
                    }
                }

                if (GetShadowedPrimaryConstructorParameter(node, symbol ?? members[0], invoked, members) is { } shadowedParameter)
                {
                    diagnostics.Add(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, node.Location, shadowedParameter);
                }

                members.Free();
            }
            else
            {
                expression = null;
                if (node is IdentifierNameSyntax identifier)
                {
                    var type = BindNativeIntegerSymbolIfAny(identifier, diagnostics);
                    if (type is { })
                    {
                        expression = new BoundTypeExpression(node, null, type);
                    }
                    else if (FallBackOnDiscard(identifier, diagnostics))
                    {
                        expression = new BoundDiscardExpression(node, NullableAnnotation.Annotated, isInferred: true, type: null);
                    }
                }

                // Otherwise, the simple-name is undefined and a compile-time error occurs.
                if (expression is null)
                {
                    expression = BadExpression(node);
                    if (lookupResult.Error != null)
                    {
                        Error(diagnostics, lookupResult.Error, node);
                    }
                    else if (IsJoinRangeVariableInLeftKey(node))
                    {
                        Error(diagnostics, ErrorCode.ERR_QueryOuterKey, node, name);
                    }
                    else if (IsInJoinRightKey(node))
                    {
                        Error(diagnostics, ErrorCode.ERR_QueryInnerKey, node, name);
                    }
                    else
                    {
                        Error(diagnostics, ErrorCode.ERR_NameNotInContext, node, name);
                    }
                }
            }

            lookupResult.Free();
            return expression;
        }

#if DEBUG
        /// <summary>
        /// Here we record all identifiers that we are trying to bind so that MethodCompiler.BindMethodBody
        /// could assert that we are able to syntactically locate all of them.
        /// Correctness of SynthesizedPrimaryConstructor.GetCapturedParameters depends on this.
        /// </summary>
        private void AdjustIdentifierMapIfAny(SimpleNameSyntax node, bool invoked)
        {
            if (node is IdentifierNameSyntax id && !this.IsSemanticModelBinder)
            {
                Binder current = this;
                while (current is not (null or InMethodBinder { IdentifierMap: not null }))
                {
                    current = current.Next;
                }

                if (current is InMethodBinder { IdentifierMap: { } identifierMap })
                {
                    // Assert that we can always figure out lookup mode from syntax
                    Debug.Assert(SyntaxFacts.IsInvoked(id) == invoked);

                    if (identifierMap.ContainsKey(id))
                    {
                        identifierMap[id] |= 2;
                    }
                    else
                    {
                        identifierMap.Add(id, 2);
                    }
                }
            }
        }
#endif

        private ParameterSymbol GetShadowedPrimaryConstructorParameter(SimpleNameSyntax node, Symbol symbol, bool invoked, ArrayBuilder<Symbol> membersOpt)
        {
            var name = node.Identifier.ValueText;

            if (symbol.ContainingSymbol is NamedTypeSymbol { OriginalDefinition: var symbolContainerDefinition } &&
                ContainingType is SourceMemberContainerTypeSymbol { IsRecord: false, IsRecordStruct: false, PrimaryConstructor: SynthesizedPrimaryConstructor { ParameterCount: not 0 } primaryConstructor, OriginalDefinition: var containingTypeDefinition } &&
                this.ContainingMember() is { Kind: not SymbolKind.NamedType, IsStatic: false } && // We are in an instance member
                primaryConstructor.Parameters.Any(static (p, name) => p.Name == name, name) &&
                // And not shadowed by a member in the same type
                symbolContainerDefinition != (object)containingTypeDefinition &&
                membersOpt?.Any(static (m, containingTypeDefinition) => m.ContainingSymbol.OriginalDefinition == (object)containingTypeDefinition, containingTypeDefinition) != true)
            {
                NamedTypeSymbol baseToCheck = containingTypeDefinition.BaseTypeNoUseSiteDiagnostics;
                while (baseToCheck is not null)
                {
                    if (symbolContainerDefinition == (object)baseToCheck.OriginalDefinition)
                    {
                        break;
                    }

                    baseToCheck = baseToCheck.OriginalDefinition.BaseTypeNoUseSiteDiagnostics;
                }

                if (baseToCheck is null)
                {
                    // The found symbol is not coming from the base
                    return null;
                }

                // Get above the InContainerBinder for the enclosing type to see if we would find a primary constructor parameter in that scope instead
                Binder binder = this;

                while (binder is not null &&
                        !(binder is InContainerBinder { Container: var container } && container.OriginalDefinition == (object)containingTypeDefinition))
                {
                    binder = binder.Next;
                }

                if (binder is { Next: Binder withPrimaryConstructorParametersBinder })
                {
                    var lookupResult = LookupResult.GetInstance();
                    var discardedInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                    withPrimaryConstructorParametersBinder.LookupIdentifier(lookupResult, node, invoked, ref discardedInfo);

                    var singleSymbol = lookupResult.IsSingleViable ? lookupResult.Symbols[0] : null;
                    lookupResult.Free();

                    if (singleSymbol is ParameterSymbol shadowedParameter &&
                        shadowedParameter.ContainingSymbol == (object)primaryConstructor)
                    {
                        Debug.Assert(!primaryConstructor.GetCapturedParameters().ContainsKey(shadowedParameter)); // How could we capture a shadowed parameter?
                        if (!primaryConstructor.GetParametersPassedToTheBase().Contains(shadowedParameter))
                        {
                            return shadowedParameter;
                        }
                    }
                }
            }

            return null;
        }

        private void LookupIdentifier(LookupResult lookupResult, SimpleNameSyntax node, bool invoked, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            LookupIdentifier(lookupResult, name: node.Identifier.ValueText, arity: node.Arity, invoked, useSiteInfo: ref useSiteInfo);
        }

        private void LookupIdentifier(LookupResult lookupResult, string name, int arity, bool invoked, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            LookupOptions options = LookupOptions.AllMethodsOnArityZero;
            if (invoked)
            {
                options |= LookupOptions.MustBeInvocableIfMember;
            }

            if (!IsInMethodBody && !IsInsideNameof)
            {
                Debug.Assert((options & LookupOptions.NamespacesOrTypesOnly) == 0);
                options |= LookupOptions.MustNotBeMethodTypeParameter;
            }

            this.LookupSymbolsWithFallback(lookupResult, name, arity, useSiteInfo: ref useSiteInfo, options: options);
        }

        /// <summary>
        /// Is this is an _ identifier in a context where discards are allowed?
        /// </summary>
        private static bool FallBackOnDiscard(IdentifierNameSyntax node, BindingDiagnosticBag diagnostics)
        {
            if (!node.Identifier.IsUnderscoreToken())
            {
                return false;
            }

            CSharpSyntaxNode containingDeconstruction = node.GetContainingDeconstruction();
            bool isDiscard = containingDeconstruction != null || IsOutVarDiscardIdentifier(node);
            if (isDiscard)
            {
                CheckFeatureAvailability(node, MessageID.IDS_FeatureDiscards, diagnostics);
            }

            return isDiscard;
        }

        private static bool IsOutVarDiscardIdentifier(SimpleNameSyntax node)
        {
            Debug.Assert(node.Identifier.IsUnderscoreToken());

            CSharpSyntaxNode parent = node.Parent;
            return (parent?.Kind() == SyntaxKind.Argument &&
                ((ArgumentSyntax)parent).RefOrOutKeyword.Kind() == SyntaxKind.OutKeyword);
        }

        private BoundExpression SynthesizeMethodGroupReceiver(CSharpSyntaxNode syntax, ArrayBuilder<Symbol> members)
        {
            // SPEC: For each instance type T starting with the instance type of the immediately
            // SPEC: enclosing type declaration, and continuing with the instance type of each
            // SPEC: enclosing class or struct declaration, [do a lot of things to find a match].
            // SPEC: ...
            // SPEC: If T is the instance type of the immediately enclosing class or struct type 
            // SPEC: and the lookup identifies one or more methods, the result is a method group 
            // SPEC: with an associated instance expression of this. 

            // Explanation of spec:
            //
            // We are looping over a set of types, from inner to outer, attempting to resolve the
            // meaning of a simple name; for example "M(123)".
            //
            // There are a number of possibilities:
            // 
            // If the lookup finds M in an outer class:
            //
            // class Outer { 
            //     static void M(int x) {}
            //     class Inner {
            //         void X() { M(123); }
            //     }
            // }
            //
            // or the base class of an outer class:
            //
            // class Base { 
            //     public static void M(int x) {}
            // }
            // class Outer : Base {
            //     class Inner {
            //         void X() { M(123); }
            //     }
            // }
            //
            // Then there is no "associated instance expression" of the method group.  That is, there
            // is no possibility of there being an "implicit this".
            //
            // If the lookup finds M on the class that triggered the lookup on the other hand, or
            // one of its base classes:
            //
            // class Base { 
            //     public static void M(int x) {}
            // }
            // class Derived : Base {
            //   void X() { M(123); }
            // }
            //
            // Then the associated instance expression is "this" *even if one or more methods in the
            // method group are static*. If it turns out that the method was static, then we'll
            // check later to determine if there was a receiver actually present in the source code
            // or not.  (That happens during the "final validation" phase of overload resolution.

            // Implementation explanation:
            //
            // If we're here, then lookup has identified one or more methods.  
            Debug.Assert(members.Count > 0);

            // The lookup implementation loops over the set of types from inner to outer, and stops 
            // when it makes a match. (This is correct because any matches found on more-outer types
            // would be hidden, and discarded.) This means that we only find members associated with 
            // one containing class or struct. The method is possibly on that type directly, or via 
            // inheritance from a base type of the type.
            //
            // The question then is what the "associated instance expression" is; is it "this" or
            // nothing at all? If the type that we found the method on is the current type, or is a
            // base type of the current type, then there should be a "this" associated with the
            // method group. Otherwise, it should be null.

            var currentType = this.ContainingType;
            if ((object)currentType == null)
            {
                // This may happen if there is no containing type, 
                // e.g. we are binding an expression in an assembly-level attribute
                return null;
            }

            var declaringType = members[0].ContainingType;

            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            if (currentType.IsEqualToOrDerivedFrom(declaringType, TypeCompareKind.ConsiderEverything, useSiteInfo: ref discardedUseSiteInfo) ||
                (currentType.IsInterface && (declaringType.IsObjectType() || currentType.AllInterfacesNoUseSiteDiagnostics.Contains(declaringType))))
            {
                return ThisReference(syntax, currentType, wasCompilerGenerated: true);
            }
            else
            {
                return TryBindInteractiveReceiver(syntax, declaringType);
            }
        }

        private bool IsBadLocalOrParameterCapture(Symbol symbol, TypeSymbol type, RefKind refKind)
        {
            if (refKind != RefKind.None || type.IsRestrictedType())
            {
                var containingMethod = this.ContainingMemberOrLambda as MethodSymbol;
                if ((object)containingMethod != null && (object)symbol.ContainingSymbol != (object)containingMethod)
                {
                    // Not expecting symbol from constructed method.
                    Debug.Assert(!symbol.ContainingSymbol.Equals(containingMethod));

                    // Captured in a lambda.
                    return (containingMethod.MethodKind == MethodKind.AnonymousFunction || containingMethod.MethodKind == MethodKind.LocalFunction) && !IsInsideNameof; // false in EE evaluation method
                }
            }
            return false;
        }

        private BoundExpression BindNonMethod(SimpleNameSyntax node, Symbol symbol, BindingDiagnosticBag diagnostics, LookupResultKind resultKind, bool indexed, bool isError)
        {
            // Events are handled later as we don't know yet if we are binding to the event or it's backing field.
            if (symbol.Kind is not (SymbolKind.Event or SymbolKind.Property))
            {
                ReportDiagnosticsIfObsolete(diagnostics, symbol, node, hasBaseReceiver: false);
            }

            switch (symbol.Kind)
            {
                case SymbolKind.Local:
                    {
                        var localSymbol = (LocalSymbol)symbol;
                        bool isNullableUnknown;
                        bool isTypeError;
                        // The type calculation here should be kept in sync with logic in BindLeftIdentifierOfPotentialColorColorMemberAccess.
                        TypeSymbol type = BindResultTypeForLocalVariableReference(node, localSymbol, diagnostics, out isNullableUnknown, out isTypeError);
                        isError |= isTypeError;

                        var constantValueOpt = localSymbol.IsConst && !IsInsideNameof && !type.IsErrorType()
                            ? localSymbol.GetConstantValue(node, this.LocalInProgress, diagnostics) : null;
                        return new BoundLocal(node, localSymbol, BoundLocalDeclarationKind.None, constantValueOpt: constantValueOpt, isNullableUnknown: isNullableUnknown, type: type, hasErrors: isError);
                    }

                case SymbolKind.Parameter:
                    {
                        var parameter = (ParameterSymbol)symbol;
                        var primaryCtor = parameter.ContainingSymbol as SynthesizedPrimaryConstructor;

                        if (primaryCtor is not null &&
                            (!IsInDeclaringTypeInstanceMember(primaryCtor) ||
                             (this.ContainingMember() is MethodSymbol { MethodKind: MethodKind.Constructor } containingMember && (object)containingMember != primaryCtor)) && // We are in a non-primary instance constructor
                            !IsInsideNameof)
                        {
                            Error(diagnostics, ErrorCode.ERR_InvalidPrimaryConstructorParameterReference, node, parameter);
                        }
                        else if (parameter.IsExtensionParameter() &&
                                (InParameterDefaultValue || InAttributeArgument ||
                                 this.ContainingMember() is null or { Kind: SymbolKind.NamedType } or { IsStatic: true } || // We are not in an instance member
                                 (object)this.ContainingMember().ContainingSymbol != parameter.ContainingSymbol) &&
                                !IsInsideNameof)
                        {
                            // Give a better error for the simple case of using an extension parameter in a static member, while avoiding any of the other cases where it is always illegal
                            if (this.ContainingMember() is { IsStatic: true } && !InParameterDefaultValue && !InAttributeArgument && (object)this.ContainingMember().ContainingSymbol == parameter.ContainingSymbol)
                            {
                                // Static members cannot access the value of extension parameter '{0}'.
                                Error(diagnostics, ErrorCode.ERR_ExtensionParameterInStaticContext, node, parameter.Name);
                            }
                            else
                            {
                                // Cannot use extension parameter '{0}' in this context.
                                Error(diagnostics, ErrorCode.ERR_InvalidExtensionParameterReference, node, parameter);
                            }
                        }
                        else
                        {
                            // Records never capture parameters within the type
                            Debug.Assert(primaryCtor is null ||
                                         primaryCtor.ContainingSymbol is NamedTypeSymbol { IsRecord: false, IsRecordStruct: false } ||
                                         (this.ContainingMember() is FieldSymbol || (object)primaryCtor == this.ContainingMember()) ||
                                         IsInsideNameof);

                            if (IsBadLocalOrParameterCapture(parameter, parameter.Type, parameter.RefKind))
                            {
                                isError = true;

                                if (parameter.RefKind != RefKind.None)
                                {
                                    Error(diagnostics, ErrorCode.ERR_AnonDelegateCantUse, node, parameter.Name);
                                }
                                else if (parameter.Type.IsRestrictedType(ignoreSpanLikeTypes: true))
                                {
                                    Error(diagnostics, ErrorCode.ERR_SpecialByRefInLambda, node, parameter.Type);
                                }
                                else
                                {
                                    Debug.Assert(parameter.Type.IsRefLikeOrAllowsRefLikeType());
                                    Error(diagnostics, ErrorCode.ERR_AnonDelegateCantUseRefLike, node, parameter.Name);
                                }
                            }
                            else if (primaryCtor is not null)
                            {
                                // Quick check if this reference itself causes the parameter capture in a field 
                                bool capture = (this.ContainingMember() is MethodSymbol containingMethod && (object)primaryCtor != containingMethod);

                                if (capture &&
                                    (parameter.RefKind != RefKind.None || parameter.Type.IsRestrictedType()) &&
                                    !IsInsideNameof)
                                {
                                    if (parameter.RefKind != RefKind.None)
                                    {
                                        Error(diagnostics, ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, node, parameter.Name);
                                    }
                                    else if (parameter.Type.IsRestrictedType(ignoreSpanLikeTypes: true))
                                    {
                                        Error(diagnostics, ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRefAny, node, parameter.Type);
                                    }
                                    else
                                    {
                                        Debug.Assert(parameter.Type.IsRefLikeOrAllowsRefLikeType());
                                        Error(diagnostics, ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRefLike, node, parameter.Name);
                                    }
                                }
                                else if (primaryCtor is { ThisParameter.RefKind: not RefKind.None } &&
                                         this.ContainingMemberOrLambda is MethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction } &&
                                         !IsInsideNameof)
                                {
                                    // Captured in a lambda.

                                    if (capture)
                                    {
                                        // This reference itself causes the parameter capture in a field  
                                        Error(diagnostics, ErrorCode.ERR_AnonDelegateCantUseStructPrimaryConstructorParameterInMember, node);
                                    }
                                    else if (primaryCtor.GetCapturedParameters().ContainsKey(parameter)) // check other references in the entire type
                                    {
                                        Error(diagnostics, ErrorCode.ERR_AnonDelegateCantUseStructPrimaryConstructorParameterCaptured, node);
                                    }
                                }
                            }
                        }

                        // The Type calculation for the node that we return here should be kept in sync with logic in BindLeftIdentifierOfPotentialColorColorMemberAccess.
                        return new BoundParameter(node, parameter, hasErrors: isError);
                    }

                case SymbolKind.NamedType:
                case SymbolKind.ErrorType:
                case SymbolKind.TypeParameter:
                    // If I identifies a type, then the result is that type constructed with the
                    // given type arguments. UNDONE: Construct the child type if it is generic!
                    return new BoundTypeExpression(node, null, (TypeSymbol)symbol, hasErrors: isError);

                case SymbolKind.Property:
                    {
                        BoundExpression receiver = SynthesizeReceiver(node, symbol, diagnostics);
                        return BindPropertyAccess(node, receiver, (PropertySymbol)symbol, diagnostics, resultKind, hasErrors: isError);
                    }

                case SymbolKind.Event:
                    {
                        BoundExpression receiver = SynthesizeReceiver(node, symbol, diagnostics);
                        return BindEventAccess(node, receiver, (EventSymbol)symbol, diagnostics, resultKind, hasErrors: isError);
                    }

                case SymbolKind.Field:
                    {
                        BoundExpression receiver = SynthesizeReceiver(node, symbol, diagnostics);
                        return BindFieldAccess(node, receiver, (FieldSymbol)symbol, diagnostics, resultKind, indexed, hasErrors: isError);
                    }

                case SymbolKind.Namespace:
                    return new BoundNamespaceExpression(node, (NamespaceSymbol)symbol, hasErrors: isError);

                case SymbolKind.Alias:
                    {
                        var alias = (AliasSymbol)symbol;
                        return alias.Target switch
                        {
                            TypeSymbol typeSymbol => new BoundTypeExpression(node, alias, typeSymbol, hasErrors: isError),
                            NamespaceSymbol namespaceSymbol => new BoundNamespaceExpression(node, namespaceSymbol, alias, hasErrors: isError),
                            _ => throw ExceptionUtilities.UnexpectedValue(alias.Target.Kind),
                        };
                    }

                case SymbolKind.RangeVariable:
                    // The type calculation here should be kept in sync with logic in BindLeftIdentifierOfPotentialColorColorMemberAccess.
                    return BindRangeVariable(node, (RangeVariableSymbol)symbol, diagnostics);

                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
        }

        private TypeSymbol BindResultTypeForLocalVariableReference(SimpleNameSyntax node, LocalSymbol localSymbol, BindingDiagnosticBag diagnostics, out bool isNullableUnknown, out bool isError)
        {
            isError = false;
            TypeSymbol type;

            if (ReportSimpleProgramLocalReferencedOutsideOfTopLevelStatement(node, localSymbol, diagnostics))
            {
                type = new ExtendedErrorTypeSymbol(
                    this.Compilation, name: "var", arity: 0, errorInfo: null, variableUsedBeforeDeclaration: true);
                isNullableUnknown = true;
            }
            else if (isUsedBeforeDeclaration(node, localSymbol))
            {
                // Here we report a local variable being used before its declaration
                //
                // There are two possible diagnostics for this:
                //
                // CS0841: ERR_VariableUsedBeforeDeclaration
                // Cannot use local variable 'x' before it is declared
                //
                // CS0844: ERR_VariableUsedBeforeDeclarationAndHidesField
                // Cannot use local variable 'x' before it is declared. The 
                // declaration of the local variable hides the field 'C.x'.
                //
                // There are two situations in which we give these errors.
                //
                // First, the scope of a local variable -- that is, the region of program 
                // text in which it can be looked up by name -- is throughout the entire
                // block which declares it. It is therefore possible to use a local
                // before it is declared, which is an error.
                //
                // As an additional help to the user, we give a special error for this
                // scenario:
                //
                // class C { 
                //  int x; 
                //  void M() { 
                //    Print(x); 
                //    int x = 5;
                //  } }
                //
                // Because a too-clever C++ user might be attempting to deliberately
                // bind to "this.x" in the "Print". (In C++ the local does not come
                // into scope until its declaration.)
                //
                FieldSymbol possibleField = null;
                var lookupResult = LookupResult.GetInstance();
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                this.LookupMembersInType(
                    lookupResult,
                    ContainingType,
                    localSymbol.Name,
                    arity: 0,
                    basesBeingResolved: null,
                    options: LookupOptions.Default,
                    originalBinder: this,
                    diagnose: false,
                    useSiteInfo: ref useSiteInfo);
                diagnostics.Add(node, useSiteInfo);
                possibleField = lookupResult.SingleSymbolOrDefault as FieldSymbol;
                lookupResult.Free();
                if ((object)possibleField != null)
                {
                    Error(diagnostics, ErrorCode.ERR_VariableUsedBeforeDeclarationAndHidesField, node, node, possibleField);
                }
                else
                {
                    Error(diagnostics, ErrorCode.ERR_VariableUsedBeforeDeclaration, node, node);
                }

                type = new ExtendedErrorTypeSymbol(
                    this.Compilation, name: "var", arity: 0, errorInfo: null, variableUsedBeforeDeclaration: true);
                isNullableUnknown = true;
            }
            else
            {
                type = localSymbol.GetTypeWithAnnotations(node, diagnostics).Type;
                isNullableUnknown = (type == (object)Compilation.ImplicitlyTypedVariableUsedInForbiddenZoneType);
                if (IsBadLocalOrParameterCapture(localSymbol, type, localSymbol.RefKind))
                {
                    isError = true;

                    if (localSymbol.RefKind == RefKind.None && type.IsRestrictedType(ignoreSpanLikeTypes: true))
                    {
                        Error(diagnostics, ErrorCode.ERR_SpecialByRefInLambda, node, type);
                    }
                    else
                    {
                        Error(diagnostics, ErrorCode.ERR_AnonDelegateCantUseLocal, node, localSymbol);
                    }
                }
            }

            return type;

            bool isUsedBeforeDeclaration(SimpleNameSyntax node, LocalSymbol localSymbol)
            {
                if (!localSymbol.HasSourceLocation)
                    return false;

                var declarator = localSymbol.GetDeclaratorSyntax();

                // trivial position check, before more costly tree check (which requires walking up the nodes). Most
                // code is correct, so this check is expected to succeed nearly every time.
                if (node.SpanStart >= declarator.SpanStart)
                    return false;

                return node.SyntaxTree == declarator.SyntaxTree;
            }
        }

        private bool IsInDeclaringTypeInstanceMember(SynthesizedPrimaryConstructor primaryCtor)
        {
            return !(InParameterDefaultValue ||
                     InAttributeArgument ||
                     this.ContainingMember() is not { Kind: not SymbolKind.NamedType, IsStatic: false } containingMember || // We are not in an instance member
                     (object)containingMember.ContainingSymbol != primaryCtor.ContainingSymbol); // The member doesn't belong to our type, i.e. from nested type
        }

        private bool ReportSimpleProgramLocalReferencedOutsideOfTopLevelStatement(SimpleNameSyntax node, Symbol symbol, BindingDiagnosticBag diagnostics)
        {
            if (symbol.ContainingSymbol is SynthesizedSimpleProgramEntryPointSymbol &&
                ContainingMember() is not SynthesizedSimpleProgramEntryPointSymbol)
            {
                Error(diagnostics, ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement, node, node);
                return true;
            }

            return false;
        }

        protected virtual BoundExpression BindRangeVariable(SimpleNameSyntax node, RangeVariableSymbol qv, BindingDiagnosticBag diagnostics)
        {
            return Next.BindRangeVariable(node, qv, diagnostics);
        }

        private BoundExpression SynthesizeReceiver(SyntaxNode node, Symbol member, BindingDiagnosticBag diagnostics)
        {
            // SPEC: Otherwise, if T is the instance type of the immediately enclosing class or
            // struct type, if the lookup identifies an instance member, and if the reference occurs
            // within the block of an instance constructor, an instance method, or an instance
            // accessor, the result is the same as a member access of the form this.I. This can only
            // happen when K is zero.

            if (!member.RequiresInstanceReceiver())
            {
                return null;
            }

            var currentType = this.ContainingType;
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            NamedTypeSymbol declaringType = member.ContainingType;
            if (currentType.IsEqualToOrDerivedFrom(declaringType, TypeCompareKind.ConsiderEverything, useSiteInfo: ref discardedUseSiteInfo) ||
                (currentType.IsInterface && (declaringType.IsObjectType() || currentType.AllInterfacesNoUseSiteDiagnostics.Contains(declaringType))))
            {
                bool hasErrors = false;
                if (!IsInsideNameof || (EnclosingNameofArgument != node && !node.IsFeatureEnabled(MessageID.IDS_FeatureInstanceMemberInNameof)))
                {
                    DiagnosticInfo diagnosticInfoOpt = null;
                    if (InFieldInitializer && !currentType.IsScriptClass)
                    {
                        //can't access "this" in field initializers
                        diagnosticInfoOpt = new CSDiagnosticInfo(ErrorCode.ERR_FieldInitRefNonstatic, member);
                    }
                    else if (InConstructorInitializer || InAttributeArgument)
                    {
                        //can't access "this" in constructor initializers or attribute arguments
                        diagnosticInfoOpt = new CSDiagnosticInfo(ErrorCode.ERR_ObjectRequired, member);
                    }
                    else
                    {
                        // not an instance member if the container is a type, like when binding default parameter values.
                        var containingMember = ContainingMember();
                        bool locationIsInstanceMember = !containingMember.IsStatic &&
                            (containingMember.Kind != SymbolKind.NamedType || currentType.IsScriptClass);

                        if (!locationIsInstanceMember)
                        {
                            // error CS0120: An object reference is required for the non-static field, method, or property '{0}'
                            diagnosticInfoOpt = new CSDiagnosticInfo(ErrorCode.ERR_ObjectRequired, member);
                        }
                    }

                    diagnosticInfoOpt ??= GetDiagnosticIfRefOrOutThisParameterCaptured();
                    hasErrors = diagnosticInfoOpt is not null;

                    if (hasErrors)
                    {
                        if (IsInsideNameof)
                        {
                            CheckFeatureAvailability(node, MessageID.IDS_FeatureInstanceMemberInNameof, diagnostics);
                        }
                        else
                        {
                            Error(diagnostics, diagnosticInfoOpt, node);
                        }
                    }
                }

                return ThisReference(node, currentType, hasErrors, wasCompilerGenerated: true);
            }
            else
            {
                return TryBindInteractiveReceiver(node, declaringType);
            }
        }

        internal Symbol ContainingMember()
        {
            return this.ContainingMemberOrLambda.ContainingNonLambdaMember();
        }

        private BoundExpression TryBindInteractiveReceiver(SyntaxNode syntax, NamedTypeSymbol memberDeclaringType)
        {
            if (this.ContainingType.TypeKind == TypeKind.Submission
                // check we have access to `this`
                && isInstanceContext())
            {
                if (memberDeclaringType.TypeKind == TypeKind.Submission)
                {
                    return new BoundPreviousSubmissionReference(syntax, memberDeclaringType) { WasCompilerGenerated = true };
                }
                else
                {
                    TypeSymbol hostObjectType = Compilation.GetHostObjectTypeSymbol();
                    var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                    if ((object)hostObjectType != null && hostObjectType.IsEqualToOrDerivedFrom(memberDeclaringType, TypeCompareKind.ConsiderEverything, useSiteInfo: ref discardedUseSiteInfo))
                    {
                        return new BoundHostObjectMemberReference(syntax, hostObjectType) { WasCompilerGenerated = true };
                    }
                }
            }

            return null;

            bool isInstanceContext()
            {
                var containingMember = this.ContainingMemberOrLambda;
                do
                {
                    if (containingMember.IsStatic)
                    {
                        return false;
                    }
                    if (containingMember.Kind == SymbolKind.NamedType)
                    {
                        break;
                    }
                    containingMember = containingMember.ContainingSymbol;
                } while ((object)containingMember != null);
                return true;
            }
        }

        public BoundExpression BindNamespaceOrTypeOrExpression(ExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            if (node.Kind() == SyntaxKind.PredefinedType)
            {
                return this.BindNamespaceOrType(node, diagnostics);
            }

            if (SyntaxFacts.IsName(node.Kind()))
            {
                if (SyntaxFacts.IsNamespaceAliasQualifier(node))
                {
                    return this.BindNamespaceAlias((IdentifierNameSyntax)node, diagnostics);
                }
                else if (SyntaxFacts.IsInNamespaceOrTypeContext(node))
                {
                    return this.BindNamespaceOrType(node, diagnostics);
                }
            }
            else if (SyntaxFacts.IsTypeSyntax(node.Kind()))
            {
                return this.BindNamespaceOrType(node, diagnostics);
            }

            return this.BindExpression(node, diagnostics, SyntaxFacts.IsInvoked(node), SyntaxFacts.IsIndexed(node));
        }

        public BoundExpression BindLabel(ExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            var name = node as IdentifierNameSyntax;
            if (name == null)
            {
                Debug.Assert(node.ContainsDiagnostics);
                return BadExpression(node, LookupResultKind.NotLabel);
            }

            var result = LookupResult.GetInstance();
            string labelName = name.Identifier.ValueText;
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            this.LookupSymbolsWithFallback(result, labelName, arity: 0, useSiteInfo: ref useSiteInfo, options: LookupOptions.LabelsOnly);
            diagnostics.Add(node, useSiteInfo);

            if (!result.IsMultiViable)
            {
                Error(diagnostics, ErrorCode.ERR_LabelNotFound, node, labelName);
                result.Free();
                return BadExpression(node, result.Kind);
            }

            Debug.Assert(result.IsSingleViable, "If this happens, we need to deal with multiple label definitions.");
            var symbol = (LabelSymbol)result.Symbols.First();
            result.Free();
            return new BoundLabel(node, symbol, null);
        }

        public BoundExpression BindNamespaceOrType(ExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            var symbol = this.BindNamespaceOrTypeOrAliasSymbol(node, diagnostics, null, false);
            return CreateBoundNamespaceOrTypeExpression(node, symbol.Symbol);
        }

        public BoundExpression BindNamespaceAlias(IdentifierNameSyntax node, BindingDiagnosticBag diagnostics)
        {
            var symbol = this.BindNamespaceAliasSymbol(node, diagnostics);
            return CreateBoundNamespaceOrTypeExpression(node, symbol);
        }

        private static BoundExpression CreateBoundNamespaceOrTypeExpression(ExpressionSyntax node, Symbol symbol)
        {
            var alias = symbol as AliasSymbol;

            if ((object)alias != null)
            {
                symbol = alias.Target;
            }

            var type = symbol as TypeSymbol;
            if ((object)type != null)
            {
                return new BoundTypeExpression(node, alias, type);
            }

            var namespaceSymbol = symbol as NamespaceSymbol;
            if ((object)namespaceSymbol != null)
            {
                return new BoundNamespaceExpression(node, namespaceSymbol, alias);
            }

            throw ExceptionUtilities.UnexpectedValue(symbol);
        }

        private BoundThisReference BindThis(ThisExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            bool hasErrors = true;

            bool inStaticContext;
            if (!HasThis(isExplicit: true, inStaticContext: out inStaticContext))
            {
                //this error is returned in the field initializer case
                Error(diagnostics, inStaticContext ? ErrorCode.ERR_ThisInStaticMeth : ErrorCode.ERR_ThisInBadContext, node);
            }
            else
            {
                hasErrors = IsRefOrOutThisParameterCaptured(node.Token, diagnostics);
            }

            return ThisReference(node, this.ContainingType, hasErrors);
        }

        private BoundThisReference ThisReference(SyntaxNode node, NamedTypeSymbol thisTypeOpt, bool hasErrors = false, bool wasCompilerGenerated = false)
        {
            return new BoundThisReference(node, thisTypeOpt ?? CreateErrorType(), hasErrors) { WasCompilerGenerated = wasCompilerGenerated };
        }

#nullable enable
        private bool IsRefOrOutThisParameterCaptured(SyntaxNodeOrToken thisOrBaseToken, BindingDiagnosticBag diagnostics)
        {
            if (GetDiagnosticIfRefOrOutThisParameterCaptured() is { } diagnosticInfo)
            {
                var location = thisOrBaseToken.GetLocation();
                Debug.Assert(location is not null);
                Error(diagnostics, diagnosticInfo, location);
                return true;
            }

            return false;
        }

        private DiagnosticInfo? GetDiagnosticIfRefOrOutThisParameterCaptured()
        {
            Debug.Assert(this.ContainingMemberOrLambda is not null);
            ParameterSymbol? thisSymbol = this.ContainingMemberOrLambda.EnclosingThisSymbol();
            // If there is no this parameter, then it is definitely not captured and 
            // any diagnostic would be cascading.
            if (thisSymbol is not null && thisSymbol.ContainingSymbol != ContainingMemberOrLambda && thisSymbol.RefKind != RefKind.None)
            {
                return new CSDiagnosticInfo(ErrorCode.ERR_ThisStructNotInAnonMeth);
            }

            return null;
        }
#nullable disable

        private BoundBaseReference BindBase(BaseExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            bool hasErrors = false;
            TypeSymbol baseType = this.ContainingType is null ? null : this.ContainingType.BaseTypeNoUseSiteDiagnostics;
            bool inStaticContext;

            if (!HasThis(isExplicit: true, inStaticContext: out inStaticContext))
            {
                //this error is returned in the field initializer case
                Error(diagnostics, inStaticContext ? ErrorCode.ERR_BaseInStaticMeth : ErrorCode.ERR_BaseInBadContext, node.Token);
                hasErrors = true;
            }
            else if ((object)baseType == null) // e.g. in System.Object
            {
                Error(diagnostics, ErrorCode.ERR_NoBaseClass, node);
                hasErrors = true;
            }
            else if (this.ContainingType is null || node.Parent is null || (node.Parent.Kind() != SyntaxKind.SimpleMemberAccessExpression && node.Parent.Kind() != SyntaxKind.ElementAccessExpression))
            {
                Error(diagnostics, ErrorCode.ERR_BaseIllegal, node.Token);
                hasErrors = true;
            }
            else if (IsRefOrOutThisParameterCaptured(node.Token, diagnostics))
            {
                // error has been reported by IsRefOrOutThisParameterCaptured
                hasErrors = true;
            }

            return new BoundBaseReference(node, baseType, hasErrors);
        }

        private BoundExpression BindCast(CastExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            BoundExpression operand = this.BindValue(node.Expression, diagnostics, BindValueKind.RValue);
            TypeWithAnnotations targetTypeWithAnnotations = this.BindType(node.Type, diagnostics);
            TypeSymbol targetType = targetTypeWithAnnotations.Type;

            if (targetType.IsNullableType() &&
                !operand.HasAnyErrors &&
                (object)operand.Type != null &&
                !operand.Type.IsNullableType() &&
                !TypeSymbol.Equals(targetType.GetNullableUnderlyingType(), operand.Type, TypeCompareKind.ConsiderEverything2))
            {
                return BindExplicitNullableCastFromNonNullable(node, operand, targetTypeWithAnnotations, diagnostics);
            }

            return BindCastCore(node, operand, targetTypeWithAnnotations, wasCompilerGenerated: operand.WasCompilerGenerated, diagnostics: diagnostics);
        }

        private BoundExpression BindFromEndIndexExpression(PrefixUnaryExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(node.OperatorToken.IsKind(SyntaxKind.CaretToken));

            CheckFeatureAvailability(node, MessageID.IDS_FeatureIndexOperator, diagnostics);

            // Used in lowering as the second argument to the constructor. Example: new Index(value, fromEnd: true)
            GetSpecialType(SpecialType.System_Boolean, diagnostics, node);

            BoundExpression boundOperand = BindValue(node.Operand, diagnostics, BindValueKind.RValue);
            TypeSymbol intType = GetSpecialType(SpecialType.System_Int32, diagnostics, node);
            TypeSymbol indexType = GetWellKnownType(WellKnownType.System_Index, diagnostics, node);

            if ((object)boundOperand.Type != null && boundOperand.Type.IsNullableType())
            {
                // Used in lowering to construct the nullable
                GetSpecialTypeMember(SpecialMember.System_Nullable_T__ctor, diagnostics, node);
                NamedTypeSymbol nullableType = GetSpecialType(SpecialType.System_Nullable_T, diagnostics, node);

                if (!indexType.IsNonNullableValueType())
                {
                    Error(diagnostics, ErrorCode.ERR_ValConstraintNotSatisfied, node, nullableType, nullableType.TypeParameters.Single(), indexType);
                }

                intType = nullableType.Construct(intType);
                indexType = nullableType.Construct(indexType);
            }

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            Conversion conversion = this.Conversions.ClassifyImplicitConversionFromExpression(boundOperand, intType, ref useSiteInfo);
            diagnostics.Add(node, useSiteInfo);

            if (!conversion.IsValid)
            {
                GenerateImplicitConversionError(diagnostics, node, conversion, boundOperand, intType);
            }

            BoundExpression boundConversion = CreateConversion(boundOperand, conversion, intType, diagnostics);
            MethodSymbol symbolOpt = GetWellKnownTypeMember(WellKnownMember.System_Index__ctor, diagnostics, syntax: node) as MethodSymbol;

            return new BoundFromEndIndexExpression(node, boundConversion, symbolOpt, indexType);
        }

        private BoundExpression BindRangeExpression(RangeExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            CheckFeatureAvailability(node, MessageID.IDS_FeatureRangeOperator, diagnostics);

            TypeSymbol rangeType = GetWellKnownType(WellKnownType.System_Range, diagnostics, node);
            MethodSymbol symbolOpt = null;

            if (!rangeType.IsErrorType())
            {
                // Depending on the available arguments to the range expression, there are four
                // possible well-known members we could bind to. The constructor is always the
                // fallback member, usable in any situation. However, if any of the other members
                // are available and applicable, we will prefer that.

                WellKnownMember? memberOpt = null;
                if (node.LeftOperand is null && node.RightOperand is null)
                {
                    memberOpt = WellKnownMember.System_Range__get_All;
                }
                else if (node.LeftOperand is null)
                {
                    memberOpt = WellKnownMember.System_Range__EndAt;
                }
                else if (node.RightOperand is null)
                {
                    memberOpt = WellKnownMember.System_Range__StartAt;
                }

                if (memberOpt is object)
                {
                    symbolOpt = (MethodSymbol)GetWellKnownTypeMember(
                        memberOpt.GetValueOrDefault(),
                        diagnostics,
                        syntax: node,
                        isOptional: true);
                }

                if (symbolOpt is null)
                {
                    symbolOpt = (MethodSymbol)GetWellKnownTypeMember(
                        WellKnownMember.System_Range__ctor,
                        diagnostics,
                        syntax: node);
                }
            }

            BoundExpression left = BindRangeExpressionOperand(node.LeftOperand, diagnostics);
            BoundExpression right = BindRangeExpressionOperand(node.RightOperand, diagnostics);

            if (left?.Type.IsNullableType() == true || right?.Type.IsNullableType() == true)
            {
                // Used in lowering to construct the nullable
                GetSpecialType(SpecialType.System_Boolean, diagnostics, node);
                GetSpecialTypeMember(SpecialMember.System_Nullable_T__ctor, diagnostics, node);
                NamedTypeSymbol nullableType = GetSpecialType(SpecialType.System_Nullable_T, diagnostics, node);

                if (!rangeType.IsNonNullableValueType())
                {
                    Error(diagnostics, ErrorCode.ERR_ValConstraintNotSatisfied, node, nullableType, nullableType.TypeParameters.Single(), rangeType);
                }

                rangeType = nullableType.Construct(rangeType);
            }

            return new BoundRangeExpression(node, left, right, symbolOpt, rangeType);
        }

        private BoundExpression BindRangeExpressionOperand(ExpressionSyntax operand, BindingDiagnosticBag diagnostics)
        {
            if (operand is null)
            {
                return null;
            }

            BoundExpression boundOperand = BindValue(operand, diagnostics, BindValueKind.RValue);
            TypeSymbol indexType = GetWellKnownType(WellKnownType.System_Index, diagnostics, operand);

            if (boundOperand.Type?.IsNullableType() == true)
            {
                // Used in lowering to construct the nullable
                GetSpecialTypeMember(SpecialMember.System_Nullable_T__ctor, diagnostics, operand);
                NamedTypeSymbol nullableType = GetSpecialType(SpecialType.System_Nullable_T, diagnostics, operand);

                if (!indexType.IsNonNullableValueType())
                {
                    Error(diagnostics, ErrorCode.ERR_ValConstraintNotSatisfied, operand, nullableType, nullableType.TypeParameters.Single(), indexType);
                }

                indexType = nullableType.Construct(indexType);
            }

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            Conversion conversion = this.Conversions.ClassifyImplicitConversionFromExpression(boundOperand, indexType, ref useSiteInfo);
            diagnostics.Add(operand, useSiteInfo);

            if (!conversion.IsValid)
            {
                GenerateImplicitConversionError(diagnostics, operand, conversion, boundOperand, indexType);
            }

            return CreateConversion(boundOperand, conversion, indexType, diagnostics);
        }

        private BoundExpression BindCastCore(ExpressionSyntax node, BoundExpression operand, TypeWithAnnotations targetTypeWithAnnotations, bool wasCompilerGenerated, BindingDiagnosticBag diagnostics)
        {
            TypeSymbol targetType = targetTypeWithAnnotations.Type;
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            Conversion conversion = this.Conversions.ClassifyConversionFromExpression(operand, targetType, isChecked: CheckOverflowAtRuntime, ref useSiteInfo, forCast: true);
            diagnostics.Add(node, useSiteInfo);

            var conversionGroup = new ConversionGroup(conversion, targetTypeWithAnnotations);
            bool suppressErrors = operand.HasAnyErrors || targetType.IsErrorType();
            bool hasErrors = !conversion.IsValid || targetType.IsStatic;
            if (hasErrors && !suppressErrors)
            {
                GenerateExplicitConversionErrors(diagnostics, node, conversion, operand, targetType);
            }

            return CreateConversion(node, operand, conversion, isCast: true, conversionGroupOpt: conversionGroup, InConversionGroupFlags.Unspecified, wasCompilerGenerated: wasCompilerGenerated, destination: targetType, diagnostics: diagnostics, hasErrors: hasErrors | suppressErrors);
        }

        private void GenerateExplicitConversionErrors(
            BindingDiagnosticBag diagnostics,
            SyntaxNode syntax,
            Conversion conversion,
            BoundExpression operand,
            TypeSymbol targetType)
        {
            // Make sure that errors within the unbound lambda don't get lost.
            if (operand.Kind == BoundKind.UnboundLambda)
            {
                GenerateAnonymousFunctionConversionError(diagnostics, operand.Syntax, (UnboundLambda)operand, targetType);
                return;
            }

            if (operand.HasAnyErrors || targetType.IsErrorType())
            {
                // an error has already been reported elsewhere
                return;
            }

            if (targetType.IsStatic)
            {
                // The specification states in the section titled "Referencing Static
                // Class Types" that it is always illegal to have a static class in a
                // cast operator.
                diagnostics.Add(ErrorCode.ERR_ConvertToStaticClass, syntax.Location, targetType);
                return;
            }

            if (!targetType.IsReferenceType && !targetType.IsNullableType() && operand.IsLiteralNull())
            {
                diagnostics.Add(ErrorCode.ERR_ValueCantBeNull, syntax.Location, targetType);
                return;
            }

            if (conversion.ResultKind == LookupResultKind.OverloadResolutionFailure)
            {
                Debug.Assert(conversion.IsUserDefined);

                ImmutableArray<MethodSymbol> originalUserDefinedConversions = conversion.OriginalUserDefinedConversions;
                if (originalUserDefinedConversions.Length > 1)
                {
                    diagnostics.Add(ErrorCode.ERR_AmbigUDConv, syntax.Location, originalUserDefinedConversions[0], originalUserDefinedConversions[1], operand.Display, targetType);
                }
                else
                {
                    Debug.Assert(originalUserDefinedConversions.Length == 0,
                        "How can there be exactly one applicable user-defined conversion if the conversion doesn't exist?");
                    SymbolDistinguisher distinguisher1 = new SymbolDistinguisher(this.Compilation, operand.Type, targetType);
                    diagnostics.Add(ErrorCode.ERR_NoExplicitConv, syntax.Location, distinguisher1.First, distinguisher1.Second);
                }

                return;
            }

            switch (operand.Kind)
            {
                case BoundKind.MethodGroup:
                    {
                        if (targetType.TypeKind != TypeKind.Delegate ||
                            !MethodGroupConversionDoesNotExistOrHasErrors((BoundMethodGroup)operand, (NamedTypeSymbol)targetType, syntax.Location, diagnostics, out _))
                        {
                            diagnostics.Add(ErrorCode.ERR_NoExplicitConv, syntax.Location, MessageID.IDS_SK_METHOD.Localize(), targetType);
                        }

                        return;
                    }
                case BoundKind.TupleLiteral:
                    {
                        var tuple = (BoundTupleLiteral)operand;
                        var targetElementTypesWithAnnotations = default(ImmutableArray<TypeWithAnnotations>);

                        // If target is a tuple or compatible type with the same number of elements,
                        // report errors for tuple arguments that failed to convert, which would be more useful.
                        if (targetType.TryGetElementTypesWithAnnotationsIfTupleType(out targetElementTypesWithAnnotations) &&
                            targetElementTypesWithAnnotations.Length == tuple.Arguments.Length)
                        {
                            GenerateExplicitConversionErrorsForTupleLiteralArguments(diagnostics, tuple.Arguments, targetElementTypesWithAnnotations);
                            return;
                        }

                        // target is not compatible with source and source does not have a type
                        if ((object)tuple.Type == null)
                        {
                            Error(diagnostics, ErrorCode.ERR_ConversionNotTupleCompatible, syntax, tuple.Arguments.Length, targetType);
                            return;
                        }

                        // Otherwise it is just a regular conversion failure from T1 to T2.
                        break;
                    }
                case BoundKind.StackAllocArrayCreation:
                    {
                        var stackAllocExpression = (BoundStackAllocArrayCreation)operand;
                        Error(diagnostics, ErrorCode.ERR_StackAllocConversionNotPossible, syntax, stackAllocExpression.ElementType, targetType);
                        return;
                    }
                case BoundKind.UnconvertedConditionalOperator when operand.Type is null:
                case BoundKind.UnconvertedSwitchExpression when operand.Type is null:
                    {
                        GenerateImplicitConversionError(diagnostics, operand.Syntax, conversion, operand, targetType);
                        return;
                    }
                case BoundKind.UnconvertedCollectionExpression:
                    {
                        GenerateImplicitConversionErrorForCollectionExpression((BoundUnconvertedCollectionExpression)operand, targetType, diagnostics);
                        return;
                    }
                case BoundKind.UnconvertedAddressOfOperator:
                    {
                        var errorCode = targetType.TypeKind switch
                        {
                            TypeKind.FunctionPointer => ErrorCode.ERR_MethFuncPtrMismatch,
                            TypeKind.Delegate => ErrorCode.ERR_CannotConvertAddressOfToDelegate,
                            _ => ErrorCode.ERR_AddressOfToNonFunctionPointer
                        };

                        diagnostics.Add(errorCode, syntax.Location, ((BoundUnconvertedAddressOfOperator)operand).Operand.Name, targetType);
                        return;
                    }
            }

            Debug.Assert((object)operand.Type != null);
            SymbolDistinguisher distinguisher = new SymbolDistinguisher(this.Compilation, operand.Type, targetType);
            diagnostics.Add(ErrorCode.ERR_NoExplicitConv, syntax.Location, distinguisher.First, distinguisher.Second);
        }

        private void GenerateExplicitConversionErrorsForTupleLiteralArguments(
            BindingDiagnosticBag diagnostics,
            ImmutableArray<BoundExpression> tupleArguments,
            ImmutableArray<TypeWithAnnotations> targetElementTypesWithAnnotations)
        {
            // report all leaf elements of the tuple literal that failed to convert
            // NOTE: we are not responsible for reporting use site errors here, just the failed leaf conversions.
            // By the time we get here we have done analysis and know we have failed the cast in general, and diagnostics collected in the process is already in the bag. 
            // The only thing left is to form a diagnostics about the actually failing conversion(s).
            // This whole method does not itself collect any usesite diagnostics. Its only purpose is to produce an error better than "conversion failed here"           
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;

            for (int i = 0; i < targetElementTypesWithAnnotations.Length; i++)
            {
                var argument = tupleArguments[i];
                var targetElementType = targetElementTypesWithAnnotations[i].Type;

                var elementConversion = Conversions.ClassifyConversionFromExpression(argument, targetElementType, isChecked: CheckOverflowAtRuntime, ref discardedUseSiteInfo);
                if (!elementConversion.IsValid)
                {
                    GenerateExplicitConversionErrors(diagnostics, argument.Syntax, elementConversion, argument, targetElementType);
                }
            }
        }

        /// <summary>
        /// This implements the casting behavior described in section 6.2.3 of the spec:
        /// 
        /// - If the nullable conversion is from S to T?, the conversion is evaluated as the underlying conversion 
        ///   from S to T followed by a wrapping from T to T?.
        ///
        /// This particular check is done in the binder because it involves conversion processing rules (like overflow
        /// checking and constant folding) which are not handled by Conversions.
        /// </summary>
        private BoundExpression BindExplicitNullableCastFromNonNullable(ExpressionSyntax node, BoundExpression operand, TypeWithAnnotations targetTypeWithAnnotations, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(targetTypeWithAnnotations.HasType && targetTypeWithAnnotations.IsNullableType());
            Debug.Assert((object)operand.Type != null && !operand.Type.IsNullableType());

            // Section 6.2.3 of the spec only applies when the non-null version of the types involved have a
            // built in conversion.
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            TypeWithAnnotations underlyingTargetTypeWithAnnotations = targetTypeWithAnnotations.Type.GetNullableUnderlyingTypeWithAnnotations();
            var underlyingConversion = Conversions.ClassifyBuiltInConversion(operand.Type, underlyingTargetTypeWithAnnotations.Type, isChecked: CheckOverflowAtRuntime, ref discardedUseSiteInfo);
            if (!underlyingConversion.Exists)
            {
                return BindCastCore(node, operand, targetTypeWithAnnotations, wasCompilerGenerated: operand.WasCompilerGenerated, diagnostics: diagnostics);
            }

            var bag = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: diagnostics.AccumulatesDependencies);
            try
            {
                var underlyingExpr = BindCastCore(node, operand, underlyingTargetTypeWithAnnotations, wasCompilerGenerated: false, diagnostics: bag);
                diagnostics.AddDependencies(bag);

                // It's possible for the S -> T conversion to produce a 'better' constant value.  If this 
                // constant value is produced place it in the tree so that it gets emitted.  This maintains 
                // parity with the native compiler which also evaluated the conversion at compile time. 
                if (underlyingExpr.ConstantValueOpt != null &&
                    !underlyingExpr.HasErrors && !bag.HasAnyErrors())
                {
                    underlyingExpr.WasCompilerGenerated = true;
                    diagnostics.AddRange(bag.DiagnosticBag);
                    return BindCastCore(node, underlyingExpr, targetTypeWithAnnotations, wasCompilerGenerated: operand.WasCompilerGenerated, diagnostics: diagnostics);
                }

                var bag2 = BindingDiagnosticBag.GetInstance(diagnostics);

                var result = BindCastCore(node, operand, targetTypeWithAnnotations, wasCompilerGenerated: operand.WasCompilerGenerated, diagnostics: bag2);

                if (bag2.AccumulatesDiagnostics && bag.HasAnyErrors() && !bag2.HasAnyErrors())
                {
                    diagnostics.AddRange(bag.DiagnosticBag);
                }

                diagnostics.AddRange(bag2);
                bag2.Free();
                return result;
            }
            finally
            {
                bag.Free();
            }
        }

        private static NameSyntax GetNameSyntax(SyntaxNode syntax)
        {
            string nameString;
            return GetNameSyntax(syntax, out nameString);
        }

        /// <summary>
        /// Gets the NameSyntax associated with the syntax node
        /// If no syntax is attached it sets the nameString to plain text
        /// name and returns a null NameSyntax
        /// </summary>
        /// <param name="syntax">Syntax node</param>
        /// <param name="nameString">Plain text name</param>
        internal static NameSyntax GetNameSyntax(SyntaxNode syntax, out string nameString)
        {
            nameString = string.Empty;
            while (true)
            {
                switch (syntax.Kind())
                {
                    case SyntaxKind.PredefinedType:
                        nameString = ((PredefinedTypeSyntax)syntax).Keyword.ValueText;
                        return null;
                    case SyntaxKind.SimpleLambdaExpression:
                        nameString = MessageID.IDS_Lambda.Localize().ToString();
                        return null;
                    case SyntaxKind.ParenthesizedExpression:
                        syntax = ((ParenthesizedExpressionSyntax)syntax).Expression;
                        continue;
                    case SyntaxKind.CastExpression:
                        syntax = ((CastExpressionSyntax)syntax).Expression;
                        continue;
                    case SyntaxKind.SimpleMemberAccessExpression:
                    case SyntaxKind.PointerMemberAccessExpression:
                        return ((MemberAccessExpressionSyntax)syntax).Name;
                    case SyntaxKind.MemberBindingExpression:
                        return ((MemberBindingExpressionSyntax)syntax).Name;
                    default:
                        return syntax as NameSyntax;
                }
            }
        }

        /// <summary>
        /// Gets the plain text name associated with the expression syntax node
        /// </summary>
        /// <param name="syntax">Expression syntax node</param>
        /// <returns>Plain text name</returns>
        private static string GetName(ExpressionSyntax syntax)
        {
            string nameString;
            var nameSyntax = GetNameSyntax(syntax, out nameString);
            if (nameSyntax != null)
            {
                return nameSyntax.GetUnqualifiedName().Identifier.ValueText;
            }
            return nameString;
        }

        // Given a list of arguments, create arrays of the bound arguments and the names of those
        // arguments.
        private void BindArgumentsAndNames(BaseArgumentListSyntax argumentListOpt, BindingDiagnosticBag diagnostics, AnalyzedArguments result, bool allowArglist = false, bool isDelegateCreation = false)
        {
            if (argumentListOpt is null)
            {
                return;
            }

            // Only report the first "duplicate name" or "named before positional" error,
            // so as to avoid "cascading" errors.
            bool hadError = false;

            // Only report the first "non-trailing named args required C# 7.2" error,
            // so as to avoid "cascading" errors.
            bool hadLangVersionError = false;

            foreach (var argumentSyntax in argumentListOpt.Arguments)
            {
                BindArgumentAndName(result, diagnostics, ref hadError, ref hadLangVersionError,
                    argumentSyntax, allowArglist, isDelegateCreation: isDelegateCreation);
            }
        }

        private bool RefMustBeObeyed(bool isDelegateCreation, ArgumentSyntax argumentSyntax)
        {
            if (Compilation.FeatureStrictEnabled || !isDelegateCreation)
            {
                return true;
            }

            switch (argumentSyntax.Expression.Kind())
            {
                // The next 3 cases should never be allowed as they cannot be ref/out. Assuming a bug in legacy compiler.
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
                case SyntaxKind.InvocationExpression:
                case SyntaxKind.ObjectCreationExpression:
                case SyntaxKind.ImplicitObjectCreationExpression:
                case SyntaxKind.ParenthesizedExpression: // this is never allowed in legacy compiler
                case SyntaxKind.DeclarationExpression:
                    // A property/indexer is also invalid as it cannot be ref/out, but cannot be checked here. Assuming a bug in legacy compiler.
                    return true;
                default:
                    // The only ones that concern us here for compat is: locals, params, fields
                    // BindArgumentAndName correctly rejects all other cases, except for properties and indexers.
                    // They are handled after BindArgumentAndName returns and the binding can be checked.
                    return false;
            }
        }

        private void BindArgumentAndName(
            AnalyzedArguments result,
            BindingDiagnosticBag diagnostics,
            ref bool hadError,
            ref bool hadLangVersionError,
            ArgumentSyntax argumentSyntax,
            bool allowArglist,
            bool isDelegateCreation)
        {
            RefKind origRefKind = argumentSyntax.RefOrOutKeyword.Kind().GetRefKind();
            // The old native compiler ignores ref/out in a delegate creation expression.
            // For compatibility we implement the same bug except in strict mode.
            // Note: Some others should still be rejected when ref/out present. See RefMustBeObeyed.
            RefKind refKind = origRefKind == RefKind.None || RefMustBeObeyed(isDelegateCreation, argumentSyntax) ? origRefKind : RefKind.None;
            BoundExpression boundArgument = BindArgumentValue(diagnostics, argumentSyntax, allowArglist, refKind);

            BindArgumentAndName(
                result,
                diagnostics,
                ref hadLangVersionError,
                argumentSyntax,
                boundArgument,
                argumentSyntax.NameColon,
                refKind);

            // check for ref/out property/indexer, only needed for 1 parameter version
            if (!hadError && isDelegateCreation && origRefKind != RefKind.None && result.Arguments.Count == 1)
            {
                var arg = result.Argument(0);
                switch (arg.Kind)
                {
                    case BoundKind.PropertyAccess:
                    case BoundKind.IndexerAccess:
                        var requiredValueKind = origRefKind == RefKind.In ? BindValueKind.ReadonlyRef : BindValueKind.RefOrOut;
                        hadError = !CheckValueKind(argumentSyntax, arg, requiredValueKind, false, diagnostics);
                        return;
                }
            }

            if (argumentSyntax.RefOrOutKeyword.Kind() != SyntaxKind.None)
            {
                argumentSyntax.Expression.CheckDeconstructionCompatibleArgument(diagnostics);
            }
        }

        private BoundExpression BindArgumentValue(BindingDiagnosticBag diagnostics, ArgumentSyntax argumentSyntax, bool allowArglist, RefKind refKind)
        {
            if (argumentSyntax.RefKindKeyword.IsKind(SyntaxKind.InKeyword))
                MessageID.IDS_FeatureReadOnlyReferences.CheckFeatureAvailability(diagnostics, argumentSyntax.RefKindKeyword);

            if (argumentSyntax.Expression.Kind() == SyntaxKind.DeclarationExpression)
            {
                if (argumentSyntax.RefKindKeyword.IsKind(SyntaxKind.OutKeyword))
                    MessageID.IDS_FeatureOutVar.CheckFeatureAvailability(diagnostics, argumentSyntax.RefKindKeyword);

                var declarationExpression = (DeclarationExpressionSyntax)argumentSyntax.Expression;
                if (declarationExpression.IsOutDeclaration())
                {
                    return BindOutDeclarationArgument(declarationExpression, diagnostics);
                }
            }

            return BindArgumentExpression(diagnostics, argumentSyntax.Expression, refKind, allowArglist);
        }

        private BoundExpression BindOutDeclarationArgument(DeclarationExpressionSyntax declarationExpression, BindingDiagnosticBag diagnostics)
        {
            TypeSyntax typeSyntax = declarationExpression.Type;
            VariableDesignationSyntax designation = declarationExpression.Designation;

            switch (designation.Kind())
            {
                case SyntaxKind.DiscardDesignation:
                    {
                        if (typeSyntax is ScopedTypeSyntax scopedType)
                        {
                            diagnostics.Add(ErrorCode.ERR_ScopedDiscard, scopedType.ScopedKeyword.GetLocation());
                            typeSyntax = scopedType.Type;
                        }

                        if (typeSyntax is RefTypeSyntax refType)
                        {
                            diagnostics.Add(ErrorCode.ERR_OutVariableCannotBeByRef, refType.Location);
                            typeSyntax = refType.Type;
                        }

                        bool isVar;
                        bool isConst = false;
                        AliasSymbol alias;
                        var declType = BindVariableTypeWithAnnotations(designation, diagnostics, typeSyntax, ref isConst, out isVar, out alias);
                        Debug.Assert(isVar != declType.HasType);
                        var type = declType.Type;

                        return new BoundDiscardExpression(declarationExpression, declType.NullableAnnotation, isInferred: type is null, type);
                    }
                case SyntaxKind.SingleVariableDesignation:
                    return BindOutVariableDeclarationArgument(declarationExpression, diagnostics);
                default:
                    throw ExceptionUtilities.UnexpectedValue(designation.Kind());
            }
        }

        private BoundExpression BindOutVariableDeclarationArgument(
             DeclarationExpressionSyntax declarationExpression,
             BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(declarationExpression.IsOutVarDeclaration());
            bool isVar;
            var designation = (SingleVariableDesignationSyntax)declarationExpression.Designation;
            TypeSyntax typeSyntax = declarationExpression.Type;

            // Is this a local?
            SourceLocalSymbol localSymbol = this.LookupLocal(designation.Identifier);
            if ((object)localSymbol != null)
            {
                ReportFieldContextualKeywordConflictIfAny(localSymbol, designation, designation.Identifier, diagnostics);

                if (typeSyntax is ScopedTypeSyntax scopedType)
                {
                    // Check for support for 'scoped'.
                    ModifierUtils.CheckScopedModifierAvailability(typeSyntax, scopedType.ScopedKeyword, diagnostics);
                    typeSyntax = scopedType.Type;
                }

                if (typeSyntax is RefTypeSyntax refType)
                {
                    diagnostics.Add(ErrorCode.ERR_OutVariableCannotBeByRef, refType.Location);
                    typeSyntax = refType.Type;
                }

                Debug.Assert(localSymbol.DeclarationKind == LocalDeclarationKind.OutVariable);
                if ((InConstructorInitializer || InFieldInitializer) && ContainingMemberOrLambda.ContainingSymbol.Kind == SymbolKind.NamedType)
                {
                    CheckFeatureAvailability(declarationExpression, MessageID.IDS_FeatureExpressionVariablesInQueriesAndInitializers, diagnostics);
                }

                bool isConst = false;
                AliasSymbol alias;
                var declType = BindVariableTypeWithAnnotations(declarationExpression, diagnostics, typeSyntax, ref isConst, out isVar, out alias);

                localSymbol.ScopeBinder.ValidateDeclarationNameConflictsInScope(localSymbol, diagnostics);

                if (isVar)
                {
                    return new OutVariablePendingInference(declarationExpression, localSymbol, null);
                }

                CheckRestrictedTypeInAsyncMethod(this.ContainingMemberOrLambda, declType.Type, diagnostics, typeSyntax);

                if (localSymbol.Scope == ScopedKind.ScopedValue && !declType.Type.IsErrorOrRefLikeOrAllowsRefLikeType())
                {
                    diagnostics.Add(ErrorCode.ERR_ScopedRefAndRefStructOnly, typeSyntax.Location);
                }

                return new BoundLocal(declarationExpression, localSymbol, BoundLocalDeclarationKind.WithExplicitType, constantValueOpt: null, isNullableUnknown: false, type: declType.Type);
            }
            else
            {
                // Is this a field?
                GlobalExpressionVariable expressionVariableField = LookupDeclaredField(designation);

                if ((object)expressionVariableField == null)
                {
                    // We should have the right binder in the chain, cannot continue otherwise.
                    throw ExceptionUtilities.Unreachable();
                }

                BoundExpression receiver = SynthesizeReceiver(designation, expressionVariableField, diagnostics);

                if (typeSyntax is ScopedTypeSyntax scopedType)
                {
                    diagnostics.Add(ErrorCode.ERR_UnexpectedToken, scopedType.ScopedKeyword.GetLocation(), scopedType.ScopedKeyword.ValueText);
                    typeSyntax = scopedType.Type;
                }

                if (typeSyntax is RefTypeSyntax refType)
                {
                    diagnostics.Add(ErrorCode.ERR_UnexpectedToken, refType.RefKeyword.GetLocation(), refType.RefKeyword.ValueText);
                    typeSyntax = refType.Type;
                }

                if (typeSyntax.IsVar)
                {
                    BindTypeOrAliasOrVarKeyword(typeSyntax, BindingDiagnosticBag.Discarded, out isVar);

                    if (isVar)
                    {
                        return new OutVariablePendingInference(declarationExpression, expressionVariableField, receiver);
                    }
                }

                TypeSymbol fieldType = expressionVariableField.GetFieldType(this.FieldsBeingBound).Type;
                return new BoundFieldAccess(declarationExpression,
                                            receiver,
                                            expressionVariableField,
                                            null,
                                            LookupResultKind.Viable,
                                            isDeclaration: true,
                                            type: fieldType);
            }
        }

        /// <summary>
        /// Reports an error when a bad special by-ref local was found.
        /// </summary>
        internal static void CheckRestrictedTypeInAsyncMethod(Symbol containingSymbol, TypeSymbol type, BindingDiagnosticBag diagnostics, SyntaxNode syntax)
        {
            if (containingSymbol.Kind == SymbolKind.Method
                && ((MethodSymbol)containingSymbol).IsAsync
                && type.IsRestrictedType())
            {
                CheckFeatureAvailability(syntax, MessageID.IDS_FeatureRefUnsafeInIteratorAsync, diagnostics);
            }
        }

        internal GlobalExpressionVariable LookupDeclaredField(SingleVariableDesignationSyntax variableDesignator)
        {
            return LookupDeclaredField(variableDesignator, variableDesignator.Identifier.ValueText);
        }

        internal GlobalExpressionVariable LookupDeclaredField(SyntaxNode node, string identifier)
        {
            foreach (Symbol member in ContainingType?.GetMembers(identifier) ?? ImmutableArray<Symbol>.Empty)
            {
                GlobalExpressionVariable field;
                if (member.Kind == SymbolKind.Field &&
                    (field = member as GlobalExpressionVariable)?.SyntaxTree == node.SyntaxTree &&
                    field.SyntaxNode == node)
                {
                    return field;
                }
            }

            return null;
        }

        // Bind a named/positional argument.
        // Prevent cascading diagnostic by considering the previous
        // error state and returning the updated error state.
        private void BindArgumentAndName(
            AnalyzedArguments result,
            BindingDiagnosticBag diagnostics,
            ref bool hadLangVersionError,
            CSharpSyntaxNode argumentSyntax,
            BoundExpression boundArgumentExpression,
            NameColonSyntax nameColonSyntax,
            RefKind refKind)
        {
            Debug.Assert(argumentSyntax is ArgumentSyntax || argumentSyntax is AttributeArgumentSyntax);

            if (nameColonSyntax != null)
                CheckFeatureAvailability(nameColonSyntax, MessageID.IDS_FeatureNamedArgument, diagnostics);

            bool hasRefKinds = result.RefKinds.Any();
            if (refKind != RefKind.None)
            {
                // The common case is no ref or out arguments. So we defer all work until the first one is seen.
                if (!hasRefKinds)
                {
                    hasRefKinds = true;

                    int argCount = result.Arguments.Count;
                    for (int i = 0; i < argCount; ++i)
                    {
                        result.RefKinds.Add(RefKind.None);
                    }
                }
            }

            if (hasRefKinds)
            {
                result.RefKinds.Add(refKind);
            }

            bool hasNames = result.Names.Any();
            if (nameColonSyntax != null)
            {
                // The common case is no named arguments. So we defer all work until the first named argument is seen.
                if (!hasNames)
                {
                    hasNames = true;

                    int argCount = result.Arguments.Count;
                    for (int i = 0; i < argCount; ++i)
                    {
                        result.Names.Add(null);
                    }
                }

                result.AddName(nameColonSyntax.Name);
            }
            else if (hasNames)
            {
                // We just saw a fixed-position argument after a named argument.
                if (!hadLangVersionError && !Compilation.LanguageVersion.AllowNonTrailingNamedArguments())
                {
                    // CS1738: Named argument specifications must appear after all fixed arguments have been specified
                    Error(diagnostics, ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, argumentSyntax,
                        new CSharpRequiredLanguageVersion(MessageID.IDS_FeatureNonTrailingNamedArguments.RequiredVersion()));

                    hadLangVersionError = true;
                }

                result.Names.Add(null);
            }

            result.Arguments.Add(boundArgumentExpression);
        }

        /// <summary>
        /// Bind argument and verify argument matches rvalue or out param requirements.
        /// </summary>
        private BoundExpression BindArgumentExpression(BindingDiagnosticBag diagnostics, ExpressionSyntax argumentExpression, RefKind refKind, bool allowArglist)
        {
            BindValueKind valueKind =
                refKind == RefKind.None ?
                        BindValueKind.RValue :
                        refKind == RefKind.In ?
                            BindValueKind.ReadonlyRef :
                            BindValueKind.RefOrOut;

            BoundExpression argument;
            if (allowArglist)
            {
                argument = this.BindValueAllowArgList(argumentExpression, diagnostics, valueKind);
            }
            else
            {
                argument = this.BindValue(argumentExpression, diagnostics, valueKind);
            }

            return argument;
        }

#nullable enable
        private void CheckAndCoerceArguments<TMember>(
            SyntaxNode node,
            MemberResolutionResult<TMember> methodResult,
            AnalyzedArguments analyzedArguments,
            BindingDiagnosticBag diagnostics,
            BoundExpression? receiver,
            bool invokedAsExtensionMethod,
            out ImmutableArray<int> argsToParamsOpt)
            where TMember : Symbol
        {
            var result = methodResult.Result;
            bool expanded = result.Kind == MemberResolutionKind.ApplicableInExpandedForm;
            int firstParamsArgument = -1;
            ArrayBuilder<BoundExpression>? paramsArgsBuilder = null;
            var arguments = analyzedArguments.Arguments;

            // Parameter types should be taken from the least overridden member:
            var parameters = methodResult.LeastOverriddenMember.GetParameters();

            for (int arg = 0; arg < arguments.Count; ++arg)
            {
                BoundExpression argument = arguments[arg];

                if (argument is BoundArgListOperator)
                {
                    Debug.Assert(result.ConversionForArg(arg).IsIdentity);
                    Debug.Assert(!argument.NeedsToBeConverted());
                    Debug.Assert(!expanded || result.ParameterFromArgument(arg) != parameters.Length - 1);
                    continue;
                }

                int paramNum = result.ParameterFromArgument(arg);
                CheckArgumentRefKind(analyzedArguments.RefKind(arg), argument, arg, parameters[paramNum], invokedAsExtensionMethod, diagnostics);

                if (expanded && paramNum == parameters.Length - 1)
                {
                    Debug.Assert(paramsArgsBuilder is null);
                    firstParamsArgument = arg;
                    paramsArgsBuilder = collectParamsArgs(in methodResult, parameters, arguments, ref arg, diagnostics);
                    continue;
                }

                arguments[arg] = coerceArgument(in methodResult, receiver, parameters, argumentsForInterpolationConversion: arguments, argument, arg, parameters[paramNum].TypeWithAnnotations, diagnostics);
            }

            argsToParamsOpt = result.ArgsToParamsOpt;

            if (paramsArgsBuilder is not null)
            {
                // Note, this call is going to free paramsArgsBuilder
                createParamsCollection(node, in methodResult, receiver, parameters, analyzedArguments, firstParamsArgument, paramsArgsBuilder, ref argsToParamsOpt, diagnostics);
            }

            Debug.Assert(analyzedArguments.RefKinds.Count == 0 || analyzedArguments.RefKinds.Count == arguments.Count);
            Debug.Assert(analyzedArguments.Names.Count == 0 || analyzedArguments.Names.Count == arguments.Count);
            Debug.Assert(argsToParamsOpt.IsDefault || argsToParamsOpt.Length == arguments.Count);

            result.ArgumentsWereCoerced();
            return;

            BoundExpression coerceArgument(
                in MemberResolutionResult<TMember> methodResult,
                BoundExpression? receiver,
                ImmutableArray<ParameterSymbol> parameters,
                ArrayBuilder<BoundExpression>? argumentsForInterpolationConversion,
                BoundExpression argument,
                int arg,
                TypeWithAnnotations parameterTypeWithAnnotations,
                BindingDiagnosticBag diagnostics)
            {
                var result = methodResult.Result;
                var kind = result.ConversionForArg(arg);
                BoundExpression coercedArgument = argument;

                if (kind.IsInterpolatedStringHandler)
                {
                    Debug.Assert(argument is BoundUnconvertedInterpolatedString or BoundBinaryOperator { IsUnconvertedInterpolatedStringAddition: true });
                    reportUnsafeIfNeeded(methodResult, diagnostics, argument, parameterTypeWithAnnotations);
                    coercedArgument = bindInterpolatedStringHandlerInMemberCall(argument, parameterTypeWithAnnotations.Type, argumentsForInterpolationConversion, parameters, in methodResult, arg, receiver, diagnostics);
                }
                // https://github.com/dotnet/roslyn/issues/37119 : should we create an (Identity) conversion when the kind is Identity but the types differ?
                else if (!kind.IsIdentity)
                {
                    reportUnsafeIfNeeded(methodResult, diagnostics, argument, parameterTypeWithAnnotations);

                    coercedArgument = CreateConversion(argument.Syntax, argument, kind, isCast: false, conversionGroupOpt: null, InConversionGroupFlags.Unspecified, parameterTypeWithAnnotations.Type, diagnostics);
                }
                else if (argument.Kind == BoundKind.OutVariablePendingInference)
                {
                    coercedArgument = ((OutVariablePendingInference)argument).SetInferredTypeWithAnnotations(parameterTypeWithAnnotations, diagnostics);
                }
                else if (argument.Kind == BoundKind.OutDeconstructVarPendingInference)
                {
                    coercedArgument = ((OutDeconstructVarPendingInference)argument).SetInferredTypeWithAnnotations(parameterTypeWithAnnotations, success: true);
                }
                else if (argument.Kind == BoundKind.DiscardExpression && !argument.HasExpressionType())
                {
                    Debug.Assert(parameterTypeWithAnnotations.HasType);
                    coercedArgument = ((BoundDiscardExpression)argument).SetInferredTypeWithAnnotations(parameterTypeWithAnnotations);
                }
                else if (argument.NeedsToBeConverted())
                {
                    Debug.Assert(kind.IsIdentity);
                    if (argument is BoundTupleLiteral)
                    {
                        // CreateConversion reports tuple literal name mismatches, and constructs the expected pattern of bound nodes.
                        coercedArgument = CreateConversion(argument.Syntax, argument, kind, isCast: false, conversionGroupOpt: null, InConversionGroupFlags.Unspecified, parameterTypeWithAnnotations.Type, diagnostics);
                    }
                    else
                    {
                        coercedArgument = BindToNaturalType(argument, diagnostics);
                    }
                }

                return coercedArgument;
            }

            static ArrayBuilder<BoundExpression> collectParamsArgs(
                in MemberResolutionResult<TMember> methodResult,
                ImmutableArray<ParameterSymbol> parameters,
                ArrayBuilder<BoundExpression> arguments,
                ref int arg,
                BindingDiagnosticBag diagnostics)
            {
                var result = methodResult.Result;
                var paramsArgsBuilder = ArrayBuilder<BoundExpression>.GetInstance();
                int paramsIndex = parameters.Length - 1;

                while (true)
                {
                    Debug.Assert(arguments[arg].Kind is not
                        (BoundKind.OutVariablePendingInference or BoundKind.OutDeconstructVarPendingInference or BoundKind.DiscardExpression or BoundKind.ArgListOperator));

                    // Conversions to elements of collection are applied in the process of collection construction
                    paramsArgsBuilder.Add(arguments[arg]);

                    if (arg + 1 == arguments.Count || result.ParameterFromArgument(arg + 1) != paramsIndex)
                    {
                        break;
                    }

                    arg++;
                }

                return paramsArgsBuilder;
            }

            // Note, this function is going to free paramsArgsBuilder
            void createParamsCollection(
                SyntaxNode node,
                in MemberResolutionResult<TMember> methodResult,
                BoundExpression? receiver,
                ImmutableArray<ParameterSymbol> parameters,
                AnalyzedArguments analyzedArguments,
                int firstParamsArgument,
                ArrayBuilder<BoundExpression> paramsArgsBuilder,
                ref ImmutableArray<int> argsToParamsOpt,
                BindingDiagnosticBag diagnostics)
            {
                Debug.Assert(methodResult.Result.ParamsElementTypeOpt.HasType);
                Debug.Assert(methodResult.Result.ParamsElementTypeOpt.Type != (object)ErrorTypeSymbol.EmptyParamsCollectionElementTypeSentinel);

                int paramsIndex = parameters.Length - 1;

                if (parameters[paramsIndex].Type.IsSZArray())
                {
                    var result = methodResult.Result;
                    TypeWithAnnotations paramsElementTypeOpt = result.ParamsElementTypeOpt;

                    for (int i = 0; i < paramsArgsBuilder.Count; i++)
                    {
                        paramsArgsBuilder[i] = coerceArgument(
                            in methodResult, receiver, parameters,
                            argumentsForInterpolationConversion: null, // We do not use arguments for interpolations as param array elements
                            paramsArgsBuilder[i],
                            arg: firstParamsArgument + i,
                            paramsElementTypeOpt,
                            diagnostics);
                    }
                }

                ImmutableArray<BoundExpression> collectionArgs = paramsArgsBuilder.ToImmutableAndFree();
                Debug.Assert(collectionArgs.Length != 0);

                BoundExpression collection = CreateParamsCollection(node, parameters[paramsIndex], collectionArgs, diagnostics);
                var arguments = analyzedArguments.Arguments;

                Debug.Assert(firstParamsArgument != -1);
                Debug.Assert(collectionArgs.Length == 1 || firstParamsArgument + collectionArgs.Length == arguments.Count);

                ArrayBuilder<int>? argsToParamsBuilder = null;
                if (!argsToParamsOpt.IsDefault && collectionArgs.Length > 1)
                {
                    argsToParamsBuilder = ArrayBuilder<int>.GetInstance(argsToParamsOpt.Length);
                    argsToParamsBuilder.AddRange(argsToParamsOpt);
                }

                for (var i = firstParamsArgument + collectionArgs.Length - 1; i != firstParamsArgument; i--)
                {
                    arguments.RemoveAt(i);

                    Debug.Assert(argsToParamsBuilder is not null || argsToParamsOpt.IsDefault);
                    argsToParamsBuilder?.RemoveAt(i);

                    if (analyzedArguments.RefKinds is { Count: > 0 } refKindsBuilder)
                    {
                        refKindsBuilder.RemoveAt(i);
                    }

                    if (analyzedArguments.Names is { Count: > 0 } namesBuilder)
                    {
                        namesBuilder.RemoveAt(i);
                    }
                }

                arguments[firstParamsArgument] = collection;

                if (argsToParamsBuilder is object)
                {
                    argsToParamsOpt = argsToParamsBuilder.ToImmutableOrNull();
                    argsToParamsBuilder.Free();
                }
            }

            void reportUnsafeIfNeeded(MemberResolutionResult<TMember> methodResult, BindingDiagnosticBag diagnostics, BoundExpression argument, TypeWithAnnotations parameterTypeWithAnnotations)
            {
                // NOTE: for some reason, dev10 doesn't report this for indexer accesses.
                if (!methodResult.Member.IsIndexer() && !argument.HasAnyErrors && parameterTypeWithAnnotations.Type.ContainsPointerOrFunctionPointer())
                {
                    // CONSIDER: dev10 uses the call syntax, but this seems clearer.
                    ReportUnsafeIfNotAllowed(argument.Syntax, diagnostics);
                    //CONSIDER: Return a bad expression so that HasErrors is true?
                }
            }

            static ParameterSymbol getCorrespondingParameter(in MemberAnalysisResult result, ImmutableArray<ParameterSymbol> parameters, int arg)
            {
                int paramNum = result.ParameterFromArgument(arg);
                return parameters[paramNum];
            }

            BoundExpression bindInterpolatedStringHandlerInMemberCall(
                BoundExpression unconvertedString,
                TypeSymbol handlerType,
                ArrayBuilder<BoundExpression>? arguments,
                ImmutableArray<ParameterSymbol> parameters,
                in MemberResolutionResult<TMember> methodResult,
                int interpolatedStringArgNum,
                BoundExpression? receiver,
                BindingDiagnosticBag diagnostics)
            {
                var result = methodResult.Result;
                Debug.Assert(unconvertedString is BoundUnconvertedInterpolatedString or BoundBinaryOperator { IsUnconvertedInterpolatedStringAddition: true });
                var interpolatedStringConversion = result.ConversionForArg(interpolatedStringArgNum);
                Debug.Assert(interpolatedStringConversion.IsInterpolatedStringHandler);
                Debug.Assert(handlerType is NamedTypeSymbol { IsInterpolatedStringHandlerType: true });

                var correspondingParameter = getCorrespondingParameter(in result, parameters, interpolatedStringArgNum);
                var handlerParameterIndexes = correspondingParameter.InterpolatedStringHandlerArgumentIndexes;

                if (result.Kind == MemberResolutionKind.ApplicableInExpandedForm && correspondingParameter.Ordinal == parameters.Length - 1)
                {
                    Debug.Assert(handlerParameterIndexes.IsEmpty);

                    // No arguments, fall back to the standard conversion steps.
                    return CreateConversion(
                        unconvertedString.Syntax,
                        unconvertedString,
                        interpolatedStringConversion,
                        isCast: false,
                        conversionGroupOpt: null,
                        InConversionGroupFlags.Unspecified,
                        handlerType,
                        diagnostics);
                }

                Debug.Assert(arguments is not null);

                if (correspondingParameter.HasInterpolatedStringHandlerArgumentError)
                {
                    // The InterpolatedStringHandlerArgumentAttribute applied to parameter '{0}' is malformed and cannot be interpreted. Construct an instance of '{1}' manually.
                    diagnostics.Add(ErrorCode.ERR_InterpolatedStringHandlerArgumentAttributeMalformed, unconvertedString.Syntax.Location, correspondingParameter, handlerType);
                    return CreateConversion(
                        unconvertedString.Syntax,
                        unconvertedString,
                        interpolatedStringConversion,
                        isCast: false,
                        conversionGroupOpt: null,
                        InConversionGroupFlags.Unspecified,
                        wasCompilerGenerated: false,
                        handlerType,
                        diagnostics,
                        hasErrors: true);
                }

                if (handlerParameterIndexes.IsEmpty)
                {
                    // No arguments, fall back to the standard conversion steps.
                    return CreateConversion(
                        unconvertedString.Syntax,
                        unconvertedString,
                        interpolatedStringConversion,
                        isCast: false,
                        conversionGroupOpt: null,
                        InConversionGroupFlags.Unspecified,
                        handlerType,
                        diagnostics);
                }

                Debug.Assert(handlerParameterIndexes.All((index, paramLength) => index >= BoundInterpolatedStringArgumentPlaceholder.ExtensionReceiver && index < paramLength,
                                                         parameters.Length));

                // We need to find the appropriate argument expression for every expected parameter, and error on any that occur after the current parameter

                ImmutableArray<int> handlerArgumentIndexes;

                if (result.ArgsToParamsOpt.IsDefault && arguments.Count == parameters.Length)
                {
                    // No parameters are missing and no remapped indexes, we can just use the original indexes
                    handlerArgumentIndexes = handlerParameterIndexes;
                }
                else
                {
                    // Args and parameters were reordered via named parameters, or parameters are missing. Find the correct argument index for each parameter.
                    var handlerArgumentIndexesBuilder = ArrayBuilder<int>.GetInstance(handlerParameterIndexes.Length, fillWithValue: BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter);
                    for (int handlerParameterIndex = 0; handlerParameterIndex < handlerParameterIndexes.Length; handlerParameterIndex++)
                    {
                        int handlerParameter = handlerParameterIndexes[handlerParameterIndex];
                        Debug.Assert(handlerArgumentIndexesBuilder[handlerParameterIndex] is BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter);

                        if (handlerParameter is BoundInterpolatedStringArgumentPlaceholder.InstanceParameter or BoundInterpolatedStringArgumentPlaceholder.ExtensionReceiver)
                        {
                            handlerArgumentIndexesBuilder[handlerParameterIndex] = handlerParameter;
                            continue;
                        }

                        for (int argumentIndex = 0; argumentIndex < arguments.Count; argumentIndex++)
                        {
                            // The index in the original parameter list we're looking to match up.
                            int argumentParameterIndex = result.ParameterFromArgument(argumentIndex);
                            // Is the original parameter index of the current argument the parameter index that was specified in the attribute?
                            if (argumentParameterIndex == handlerParameter)
                            {
                                // We can't just bail out on the first match: users can duplicate parameters in attributes, causing the same value to be passed twice.
                                handlerArgumentIndexesBuilder[handlerParameterIndex] = argumentIndex;
                            }
                        }
                    }

                    handlerArgumentIndexes = handlerArgumentIndexesBuilder.ToImmutableAndFree();
                }

                var argumentPlaceholdersBuilder = ArrayBuilder<BoundInterpolatedStringArgumentPlaceholder>.GetInstance(handlerArgumentIndexes.Length);
                var argumentRefKindsBuilder = ArrayBuilder<RefKind>.GetInstance(handlerArgumentIndexes.Length);
                bool hasErrors = false;

                // Now, go through all the specified arguments and see if any were specified _after_ the interpolated string, and construct
                // a set of placeholders for overload resolution.
                for (int i = 0; i < handlerArgumentIndexes.Length; i++)
                {
                    int argumentIndex = handlerArgumentIndexes[i];
                    Debug.Assert(argumentIndex != interpolatedStringArgNum);

                    RefKind refKind;
                    TypeSymbol placeholderType;
                    switch (argumentIndex)
                    {
                        case BoundInterpolatedStringArgumentPlaceholder.InstanceParameter:
                            Debug.Assert(receiver!.Type is not null);
                            refKind = RefKind.None;
                            placeholderType = receiver.Type;
                            break;
                        case BoundInterpolatedStringArgumentPlaceholder.ExtensionReceiver:
                            Debug.Assert(methodResult.Member.IsExtensionBlockMember());
                            var receiverParameter = ((NamedTypeSymbol)methodResult.Member.ContainingSymbol).ExtensionParameter;
                            Debug.Assert(receiverParameter is not null);
                            refKind = receiverParameter.RefKind;
                            placeholderType = receiverParameter.Type;
                            break;
                        case BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter:
                            {
                                // Don't error if the parameter isn't optional or params: the user will already have an error for missing an optional parameter or overload resolution failed.
                                // If it is optional, then they could otherwise not specify the parameter and that's an error
                                var originalParameterIndex = handlerParameterIndexes[i];
                                var parameter = parameters[originalParameterIndex];
                                if (parameter.IsOptional ||
                                    (result.Kind == MemberResolutionKind.ApplicableInExpandedForm && originalParameterIndex + 1 == parameters.Length))
                                {
                                    // Parameter '{0}' is not explicitly provided, but is used as an argument to the interpolated string handler conversion on parameter '{1}'. Specify the value of '{0}' before '{1}'.
                                    diagnostics.Add(
                                        ErrorCode.ERR_InterpolatedStringHandlerArgumentOptionalNotSpecified,
                                        unconvertedString.Syntax.Location,
                                        parameter.Name,
                                        correspondingParameter.Name);
                                    hasErrors = true;
                                }

                                refKind = parameter.RefKind;
                                placeholderType = parameter.Type;
                            }
                            break;
                        default:
                            {
                                var originalParameterIndex = handlerParameterIndexes[i];
                                var parameter = parameters[originalParameterIndex];
                                if (argumentIndex > interpolatedStringArgNum)
                                {
                                    // Parameter '{0}' is an argument to the interpolated string handler conversion on parameter '{1}', but the corresponding argument is specified after the interpolated string expression. Reorder the arguments to move '{0}' before '{1}'.
                                    diagnostics.Add(
                                        ErrorCode.ERR_InterpolatedStringHandlerArgumentLocatedAfterInterpolatedString,
                                        arguments[argumentIndex].Syntax.Location,
                                        parameter.Name,
                                        correspondingParameter.Name);
                                    hasErrors = true;
                                }

                                refKind = parameter.RefKind;
                                placeholderType = parameter.Type;
                            }
                            break;
                    }

                    SyntaxNode placeholderSyntax;
                    bool isSuppressed;

                    switch (argumentIndex)
                    {
                        case BoundInterpolatedStringArgumentPlaceholder.InstanceParameter:
                        case BoundInterpolatedStringArgumentPlaceholder.ExtensionReceiver:
                            Debug.Assert(receiver != null);
                            isSuppressed = receiver.IsSuppressed;
                            placeholderSyntax = receiver.Syntax;
                            break;
                        case BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter:
                            placeholderSyntax = unconvertedString.Syntax;
                            isSuppressed = false;
                            break;
                        case >= 0:
                            placeholderSyntax = arguments[argumentIndex].Syntax;
                            isSuppressed = arguments[argumentIndex].IsSuppressed;
                            break;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(argumentIndex);
                    }

                    argumentPlaceholdersBuilder.Add(
                        (BoundInterpolatedStringArgumentPlaceholder)(new BoundInterpolatedStringArgumentPlaceholder(
                            placeholderSyntax,
                            argumentIndex,
                            placeholderType,
                            hasErrors: argumentIndex == BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter)
                        { WasCompilerGenerated = true }.WithSuppression(isSuppressed)));
                    // We use the parameter refkind, rather than what the argument was actually passed with, because that will suppress duplicated errors
                    // about arguments being passed with the wrong RefKind. The user will have already gotten an error about mismatched RefKinds or it will
                    // be a place where refkinds are allowed to differ
                    argumentRefKindsBuilder.Add(refKind == RefKind.RefReadOnlyParameter ? RefKind.In : refKind);
                }

                var interpolatedString = BindUnconvertedInterpolatedExpressionToHandlerType(
                    unconvertedString,
                    (NamedTypeSymbol)handlerType,
                    diagnostics,
                    additionalConstructorArguments: argumentPlaceholdersBuilder.ToImmutableAndFree(),
                    additionalConstructorRefKinds: argumentRefKindsBuilder.ToImmutableAndFree());

                return new BoundConversion(
                    interpolatedString.Syntax,
                    interpolatedString,
                    interpolatedStringConversion,
                    @checked: CheckOverflowAtRuntime,
                    explicitCastInCode: false,
                    conversionGroupOpt: null,
                    InConversionGroupFlags.Unspecified,
                    constantValueOpt: null,
                    handlerType,
                    hasErrors || interpolatedString.HasErrors);
            }
        }

        private void CheckArgumentRefKind(RefKind argRefKind, BoundExpression argument, int arg, ParameterSymbol parameter,
            bool invokedAsExtensionMethod, BindingDiagnosticBag diagnostics)
        {
            if (argument.HasAnyErrors)
            {
                return;
            }

            if (!Compilation.IsFeatureEnabled(MessageID.IDS_FeatureRefReadonlyParameters))
            {
                // Disallow using `ref readonly` parameters with no or `in` argument modifier,
                // same as older versions of the compiler would (since they would see the parameter as `ref`).
                if (argRefKind is RefKind.None or RefKind.In && parameter.RefKind == RefKind.RefReadOnlyParameter)
                {
                    var available = CheckFeatureAvailability(argument.Syntax, MessageID.IDS_FeatureRefReadonlyParameters, diagnostics);
                    Debug.Assert(!available);
                }
            }
            else
            {
                // Tracked by https://github.com/dotnet/roslyn/issues/78830 : diagnostic quality, consider removing or adjusting the reported argument position
                var argNumber = invokedAsExtensionMethod ? arg : arg + 1;

                // Warn for `ref`/`in` or None/`ref readonly` mismatch.
                if (argRefKind == RefKind.Ref)
                {
                    if (parameter.RefKind == RefKind.In)
                    {
                        Debug.Assert(argNumber > 0);
                        // The 'ref' modifier for argument {0} corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
                        diagnostics.Add(
                            ErrorCode.WRN_BadArgRef,
                            argument.Syntax,
                            argNumber);
                    }
                }
                else if (argRefKind == RefKind.None && parameter.RefKind == RefKind.RefReadOnlyParameter)
                {
                    if (!this.CheckValueKind(argument.Syntax, argument, BindValueKind.RefersToLocation, checkingReceiver: false, BindingDiagnosticBag.Discarded))
                    {
                        Debug.Assert(argNumber >= 0); // can be 0 for receiver of extension method
                                                      // Argument {0} should be a variable because it is passed to a 'ref readonly' parameter
                        diagnostics.Add(
                            ErrorCode.WRN_RefReadonlyNotVariable,
                            argument.Syntax,
                            argNumber);
                    }
                    else if (!invokedAsExtensionMethod || arg != 0)
                    {
                        Debug.Assert(argNumber > 0);
                        if (this.CheckValueKind(argument.Syntax, argument, BindValueKind.Assignable, checkingReceiver: false, BindingDiagnosticBag.Discarded))
                        {
                            // Argument {0} should be passed with 'ref' or 'in' keyword
                            diagnostics.Add(
                                ErrorCode.WRN_ArgExpectedRefOrIn,
                                argument.Syntax,
                                argNumber);
                        }
                        else
                        {
                            // Argument {0} should be passed with the 'in' keyword
                            diagnostics.Add(
                                ErrorCode.WRN_ArgExpectedIn,
                                argument.Syntax,
                                argNumber);
                        }
                    }
                }
            }
        }
#nullable disable

        private BoundExpression BindArrayCreationExpression(ArrayCreationExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            // SPEC begins
            //
            // An array-creation-expression is used to create a new instance of an array-type.
            //
            // array-creation-expression:
            //     new non-array-type[expression-list] rank-specifiersopt array-initializeropt
            //     new array-type array-initializer 
            //     new rank-specifier array-initializer
            //
            // An array creation expression of the first form allocates an array instance of the
            // type that results from deleting each of the individual expressions from the 
            // expression list. For example, the array creation expression new int[10, 20] produces
            // an array instance of type int[,], and the array creation expression new int[10][,]
            // produces an array of type int[][,]. Each expression in the expression list must be of
            // type int, uint, long, or ulong, or implicitly convertible to one or more of these
            // types. The value of each expression determines the length of the corresponding
            // dimension in the newly allocated array instance. Since the length of an array
            // dimension must be nonnegative, it is a compile-time error to have a 
            // constant-expression with a negative value in the expression list.
            //
            // If an array creation expression of the first form includes an array initializer, each
            // expression in the expression list must be a constant and the rank and dimension 
            // lengths specified by the expression list must match those of the array initializer.
            //
            // In an array creation expression of the second or third form, the rank of the
            // specified array type or rank specifier must match that of the array initializer. The
            // individual dimension lengths are inferred from the number of elements in each of the
            // corresponding nesting levels of the array initializer. Thus, the expression new
            // int[,] {{0, 1}, {2, 3}, {4, 5}} exactly corresponds to new int[3, 2] {{0, 1}, {2, 3},
            // {4, 5}}
            //
            // An array creation expression of the third form is referred to as an implicitly typed
            // array creation expression. It is similar to the second form, except that the element
            // type of the array is not explicitly given, but determined as the best common type
            // (7.5.2.14) of the set of expressions in the array initializer. For a multidimensional
            // array, i.e., one where the rank-specifier contains at least one comma, this set
            // comprises all expressions found in nested array-initializers.
            //
            // An array creation expression permits instantiation of an array with elements of an
            // array type, but the elements of such an array must be manually initialized. For
            // example, the statement
            //
            // int[][] a = new int[100][];
            //
            // creates a single-dimensional array with 100 elements of type int[]. The initial value
            // of each element is null. It is not possible for the same array creation expression to
            // also instantiate the sub-arrays, and the statement
            //
            // int[][] a = new int[100][5];		// Error
            //
            // results in a compile-time error. 
            //
            // The following are examples of implicitly typed array creation expressions:
            //
            // var a = new[] { 1, 10, 100, 1000 };                     // int[]
            // var b = new[] { 1, 1.5, 2, 2.5 };                       // double[]
            // var c = new[,] { { "hello", null }, { "world", "!" } }; // string[,]
            // var d = new[] { 1, "one", 2, "two" };                   // Error
            //
            // The last expression causes a compile-time error because neither int nor string is 
            // implicitly convertible to the other, and so there is no best common type. An
            // explicitly typed array creation expression must be used in this case, for example
            // specifying the type to be object[]. Alternatively, one of the elements can be cast to
            // a common base type, which would then become the inferred element type.
            //
            // SPEC ends

            var type = (ArrayTypeSymbol)BindArrayType(node.Type, diagnostics, permitDimensions: true, basesBeingResolved: null, disallowRestrictedTypes: true).Type;

            // CONSIDER: 
            //
            // There may be erroneous rank specifiers in the source code, for example:
            //
            // int y = 123; 
            // int[][] z = new int[10][y];
            //
            // The "10" is legal but the "y" is not. If we are in such a situation we do have the
            // "y" expression syntax stashed away in the syntax tree. However, we do *not* perform
            // semantic analysis. This means that "go to definition" on "y" does not work, and so
            // on. We might consider doing a semantic analysis here (with error suppression; a parse
            // error has already been reported) so that "go to definition" works.

            ArrayBuilder<BoundExpression> sizes = ArrayBuilder<BoundExpression>.GetInstance();
            ArrayRankSpecifierSyntax firstRankSpecifier = node.Type.RankSpecifiers[0];
            bool hasErrors = false;
            foreach (var arg in firstRankSpecifier.Sizes)
            {
                var size = BindArrayDimension(arg, diagnostics, ref hasErrors);
                if (size != null)
                {
                    sizes.Add(size);
                }
                else if (node.Initializer is null && arg == firstRankSpecifier.Sizes[0])
                {
                    Error(diagnostics, ErrorCode.ERR_MissingArraySize, firstRankSpecifier);
                    hasErrors = true;
                }
            }

            // produce errors for additional sizes in the ranks
            for (int additionalRankIndex = 1; additionalRankIndex < node.Type.RankSpecifiers.Count; additionalRankIndex++)
            {
                var rank = node.Type.RankSpecifiers[additionalRankIndex];
                var dimension = rank.Sizes;
                foreach (var arg in dimension)
                {
                    if (arg.Kind() != SyntaxKind.OmittedArraySizeExpression)
                    {
                        var size = BindRValueWithoutTargetType(arg, diagnostics);
                        Error(diagnostics, ErrorCode.ERR_InvalidArray, arg);
                        hasErrors = true;
                        // Capture the invalid sizes for `SemanticModel` and `IOperation`
                        sizes.Add(size);
                    }
                }
            }

            ImmutableArray<BoundExpression> arraySizes = sizes.ToImmutableAndFree();

            return node.Initializer == null
                ? new BoundArrayCreation(node, arraySizes, null, type, hasErrors)
                : BindArrayCreationWithInitializer(diagnostics, node, node.Initializer, type, arraySizes, hasErrors: hasErrors);
        }

        private BoundExpression BindArrayDimension(ExpressionSyntax dimension, BindingDiagnosticBag diagnostics, ref bool hasErrors)
        {
            // These make the parse tree nicer, but they shouldn't actually appear in the bound tree.
            if (dimension.Kind() != SyntaxKind.OmittedArraySizeExpression)
            {
                var size = BindValue(dimension, diagnostics, BindValueKind.RValue);
                if (!size.HasAnyErrors)
                {
                    size = ConvertToArrayIndex(size, diagnostics, allowIndexAndRange: false, indexOrRangeWellknownType: out _);
                    if (IsNegativeConstantForArraySize(size))
                    {
                        Error(diagnostics, ErrorCode.ERR_NegativeArraySize, dimension);
                        hasErrors = true;
                    }
                }
                else
                {
                    size = BindToTypeForErrorRecovery(size);
                }

                return size;
            }
            return null;
        }

        private BoundExpression BindImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            // See BindArrayCreationExpression method above for implicitly typed array creation SPEC.
            MessageID.IDS_FeatureImplicitArray.CheckFeatureAvailability(diagnostics, node.NewKeyword);

            InitializerExpressionSyntax initializer = node.Initializer;
            int rank = node.Commas.Count + 1;

            ImmutableArray<BoundExpression> boundInitializerExpressions = BindArrayInitializerExpressions(initializer, diagnostics, dimension: 1, rank: rank);

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            TypeSymbol bestType = BestTypeInferrer.InferBestType(boundInitializerExpressions, this.Conversions, ref useSiteInfo, out _);
            diagnostics.Add(node, useSiteInfo);

            if ((object)bestType == null || bestType.IsVoidType()) // Dev10 also reports ERR_ImplicitlyTypedArrayNoBestType for void.
            {
                Error(diagnostics, ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, node);
                bestType = CreateErrorType();
            }

            if (bestType.IsRestrictedType())
            {
                // CS0611: Array elements cannot be of type '{0}'
                Error(diagnostics, ErrorCode.ERR_ArrayElementCantBeRefAny, node, bestType);
            }

            // Element type nullability will be inferred in flow analysis and does not need to be set here.
            var arrayType = ArrayTypeSymbol.CreateCSharpArray(Compilation.Assembly, TypeWithAnnotations.Create(bestType), rank);
            return BindArrayCreationWithInitializer(diagnostics, node, initializer, arrayType,
                sizes: ImmutableArray<BoundExpression>.Empty, boundInitExprOpt: boundInitializerExpressions);
        }

        private BoundExpression BindImplicitStackAllocArrayCreationExpression(ImplicitStackAllocArrayCreationExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            InitializerExpressionSyntax initializer = node.Initializer;
            ImmutableArray<BoundExpression> boundInitializerExpressions = BindArrayInitializerExpressions(initializer, diagnostics, dimension: 1, rank: 1);

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            TypeSymbol bestType = BestTypeInferrer.InferBestType(boundInitializerExpressions, this.Conversions, ref useSiteInfo, out _);
            diagnostics.Add(node, useSiteInfo);

            if ((object)bestType == null || bestType.IsVoidType())
            {
                Error(diagnostics, ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, node);
                bestType = CreateErrorType();
            }

            if (!bestType.IsErrorType())
            {
                CheckManagedAddr(Compilation, bestType, node.Location, diagnostics, errorForManaged: true);
            }

            return BindStackAllocWithInitializer(
                node,
                node.StackAllocKeyword,
                initializer,
                type: GetStackAllocType(node, TypeWithAnnotations.Create(bestType), diagnostics, out bool hasErrors),
                elementType: bestType,
                sizeOpt: null,
                diagnostics,
                hasErrors: hasErrors,
                boundInitializerExpressions);
        }

        // This method binds all the array initializer expressions.
        // NOTE: It doesn't convert the bound initializer expressions to array's element type.
        // NOTE: This is done separately in ConvertAndBindArrayInitialization method below.
        private ImmutableArray<BoundExpression> BindArrayInitializerExpressions(InitializerExpressionSyntax initializer, BindingDiagnosticBag diagnostics, int dimension, int rank)
        {
            var exprBuilder = ArrayBuilder<BoundExpression>.GetInstance();
            BindArrayInitializerExpressions(initializer, exprBuilder, diagnostics, dimension, rank);
            return exprBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// This method walks through the array's InitializerExpressionSyntax and binds all the initializer expressions recursively.
        /// NOTE: It doesn't convert the bound initializer expressions to array's element type.
        /// NOTE: This is done separately in ConvertAndBindArrayInitialization method below.
        /// </summary>
        /// <param name="initializer">Initializer Syntax.</param>
        /// <param name="exprBuilder">Bound expression builder.</param>
        /// <param name="diagnostics">Diagnostics.</param>
        /// <param name="dimension">Current array dimension being processed.</param>
        /// <param name="rank">Rank of the array type.</param>
        private void BindArrayInitializerExpressions(InitializerExpressionSyntax initializer, ArrayBuilder<BoundExpression> exprBuilder, BindingDiagnosticBag diagnostics, int dimension, int rank)
        {
            Debug.Assert(rank > 0);
            Debug.Assert(dimension > 0 && dimension <= rank);
            Debug.Assert(exprBuilder != null);

            if (dimension == rank)
            {
                // We are processing the nth dimension of a rank-n array. We expect that these will
                // only be values, not array initializers.
                foreach (var expression in initializer.Expressions)
                {
                    var boundExpression = BindValue(expression, diagnostics, BindValueKind.RValue);
                    exprBuilder.Add(boundExpression);
                }
            }
            else
            {
                // Inductive case; we'd better have another array initializer
                foreach (var expression in initializer.Expressions)
                {
                    if (expression.Kind() == SyntaxKind.ArrayInitializerExpression)
                    {
                        BindArrayInitializerExpressions((InitializerExpressionSyntax)expression, exprBuilder, diagnostics, dimension + 1, rank);
                    }
                    else
                    {
                        // We have non-array initializer expression, but we expected an array initializer expression.

                        var boundExpression = BindValue(expression, diagnostics, BindValueKind.RValue);
                        if ((object)boundExpression.Type == null || !boundExpression.Type.IsErrorType())
                        {
                            if (!boundExpression.HasAnyErrors)
                            {
                                Error(diagnostics, ErrorCode.ERR_ArrayInitializerExpected, expression);
                            }

                            // Wrap the expression with a bound bad expression with error type.
                            boundExpression = BadExpression(
                                expression,
                                LookupResultKind.Empty,
                                ImmutableArray.Create(boundExpression.ExpressionSymbol),
                                ImmutableArray.Create(boundExpression));
                        }

                        exprBuilder.Add(boundExpression);
                    }
                }
            }
        }

        /// <summary>
        /// Given an array of bound initializer expressions, this method converts these bound expressions
        /// to array's element type and generates a BoundArrayInitialization with the converted initializers.
        /// </summary>
        /// <param name="diagnostics">Diagnostics.</param>
        /// <param name="node">Initializer Syntax.</param>
        /// <param name="type">Array type.</param>
        /// <param name="knownSizes">Known array bounds.</param>
        /// <param name="dimension">Current array dimension being processed.</param>
        /// <param name="boundInitExpr">Array of bound initializer expressions.</param>
        /// <param name="boundInitExprIndex">
        /// Index into the array of bound initializer expressions to fetch the next bound expression.
        /// </param>
        /// <returns></returns>
        private BoundArrayInitialization ConvertAndBindArrayInitialization(
            BindingDiagnosticBag diagnostics,
            InitializerExpressionSyntax node,
            ArrayTypeSymbol type,
            int?[] knownSizes,
            int dimension,
            ImmutableArray<BoundExpression> boundInitExpr,
            ref int boundInitExprIndex,
            bool isInferred)
        {
            Debug.Assert(!boundInitExpr.IsDefault);

            ArrayBuilder<BoundExpression> initializers = ArrayBuilder<BoundExpression>.GetInstance();
            if (dimension == type.Rank)
            {
                // We are processing the nth dimension of a rank-n array. We expect that these will
                // only be values, not array initializers.
                TypeSymbol elemType = type.ElementType;
                foreach (var expressionSyntax in node.Expressions)
                {
                    Debug.Assert(boundInitExprIndex >= 0 && boundInitExprIndex < boundInitExpr.Length);

                    BoundExpression boundExpression = boundInitExpr[boundInitExprIndex];
                    boundInitExprIndex++;

                    BoundExpression convertedExpression = GenerateConversionForAssignment(elemType, boundExpression, diagnostics);
                    initializers.Add(convertedExpression);
                }
            }
            else
            {
                // Inductive case; we'd better have another array initializer
                foreach (var expr in node.Expressions)
                {
                    BoundExpression init = null;
                    if (expr.Kind() == SyntaxKind.ArrayInitializerExpression)
                    {
                        init = ConvertAndBindArrayInitialization(diagnostics, (InitializerExpressionSyntax)expr,
                             type, knownSizes, dimension + 1, boundInitExpr, ref boundInitExprIndex, isInferred);
                    }
                    else
                    {
                        // We have non-array initializer expression, but we expected an array initializer expression.
                        // We have already generated the diagnostics during binding, so just fetch the bound expression.

                        Debug.Assert(boundInitExprIndex >= 0 && boundInitExprIndex < boundInitExpr.Length);

                        init = boundInitExpr[boundInitExprIndex];
                        Debug.Assert(init.HasAnyErrors);
                        Debug.Assert(init.Type.IsErrorType());

                        boundInitExprIndex++;
                    }

                    initializers.Add(init);
                }
            }

            bool hasErrors = false;
            var knownSizeOpt = knownSizes[dimension - 1];

            if (knownSizeOpt == null)
            {
                knownSizes[dimension - 1] = initializers.Count;
            }
            else if (knownSizeOpt != initializers.Count)
            {
                // No need to report an error if the known size is negative
                // since we've already reported CS0248 earlier and it's
                // expected that the number of initializers won't match.
                if (knownSizeOpt >= 0)
                {
                    Error(diagnostics, ErrorCode.ERR_ArrayInitializerIncorrectLength, node, knownSizeOpt.Value);
                    hasErrors = true;
                }
            }

            return new BoundArrayInitialization(node, isInferred, initializers.ToImmutableAndFree(), hasErrors: hasErrors);
        }

        private BoundArrayInitialization BindArrayInitializerList(
           BindingDiagnosticBag diagnostics,
           InitializerExpressionSyntax node,
           ArrayTypeSymbol type,
           int?[] knownSizes,
           int dimension,
           bool isInferred,
           ImmutableArray<BoundExpression> boundInitExprOpt = default(ImmutableArray<BoundExpression>))
        {
            // Bind the array initializer expressions, if not already bound.
            // NOTE: Initializer expressions might already be bound for implicitly type array creation
            // NOTE: during array's element type inference.
            if (boundInitExprOpt.IsDefault)
            {
                boundInitExprOpt = BindArrayInitializerExpressions(node, diagnostics, dimension, type.Rank);
            }

            // Convert the bound array initializer expressions to array's element type and
            // generate BoundArrayInitialization with the converted initializers.
            int boundInitExprIndex = 0;
            return ConvertAndBindArrayInitialization(diagnostics, node, type, knownSizes, dimension, boundInitExprOpt, ref boundInitExprIndex, isInferred);
        }

        private BoundArrayInitialization BindUnexpectedArrayInitializer(
            InitializerExpressionSyntax node,
            BindingDiagnosticBag diagnostics,
            ErrorCode errorCode,
            CSharpSyntaxNode errorNode = null)
        {
            var result = BindArrayInitializerList(
                diagnostics,
                node,
                this.Compilation.CreateArrayTypeSymbol(GetSpecialType(SpecialType.System_Object, diagnostics, node)),
                new int?[1],
                dimension: 1,
                isInferred: false);

            if (!result.HasAnyErrors)
            {
                result = new BoundArrayInitialization(node, isInferred: false, result.Initializers, hasErrors: true);
            }

            Error(diagnostics, errorCode, errorNode ?? node);
            return result;
        }

        // We could be in the cases
        //
        // (1) int[] x = { a, b }
        // (2) new int[] { a, b }
        // (3) new int[2] { a, b }
        // (4) new [] { a, b }
        //
        // In case (1) there is no creation syntax.
        // In cases (2) and (3) creation syntax is an ArrayCreationExpression.
        // In case (4) creation syntax is an ImplicitArrayCreationExpression.
        //
        // In cases (1), (2) and (4) there are no sizes.
        //
        // The initializer syntax is always provided.
        //
        // If we are in case (3) and sizes are provided then the number of sizes must match the rank
        // of the array type passed in.

        // For case (4), i.e. ImplicitArrayCreationExpression, we must have already bound the
        // initializer expressions for best type inference.
        // These bound expressions are stored in boundInitExprOpt and reused in creating
        // BoundArrayInitialization to avoid binding them twice.

        private BoundArrayCreation BindArrayCreationWithInitializer(
            BindingDiagnosticBag diagnostics,
            ExpressionSyntax creationSyntax,
            InitializerExpressionSyntax initSyntax,
            ArrayTypeSymbol type,
            ImmutableArray<BoundExpression> sizes,
            ImmutableArray<BoundExpression> boundInitExprOpt = default(ImmutableArray<BoundExpression>),
            bool hasErrors = false)
        {
            Debug.Assert(creationSyntax == null ||
                creationSyntax.Kind() == SyntaxKind.ArrayCreationExpression ||
                creationSyntax.Kind() == SyntaxKind.ImplicitArrayCreationExpression);
            Debug.Assert(initSyntax != null);
            Debug.Assert((object)type != null);
            Debug.Assert(boundInitExprOpt.IsDefault || creationSyntax.Kind() == SyntaxKind.ImplicitArrayCreationExpression);

            // NOTE: In error scenarios, it may be the case sizes.Count > type.Rank.
            // For example, new int[1 2] has 2 sizes, but rank 1 (since there are 0 commas).
            int rank = type.Rank;
            int numSizes = sizes.Length;
            int?[] knownSizes = new int?[Math.Max(rank, numSizes)];

            // If there are sizes given and there is an array initializer, then every size must be a
            // constant. (We'll check later that it matches)
            for (int i = 0; i < numSizes; ++i)
            {
                // Here we are being bug-for-bug compatible with C# 4. When you have code like
                // byte[] b = new[uint.MaxValue] { 2 };
                // you might expect an error that says that the number of elements in the initializer does
                // not match the size of the array. But in C# 4 if the constant does not fit into an integer
                // then we confusingly give the error "that's not a constant".
                // NOTE: in the example above, GetIntegerConstantForArraySize is returning null because the
                // size doesn't fit in an int - not because it doesn't match the initializer length.
                var size = sizes[i];
                knownSizes[i] = GetIntegerConstantForArraySize(size);
                if (!size.HasAnyErrors && knownSizes[i] == null)
                {
                    Error(diagnostics, ErrorCode.ERR_ConstantExpected, size.Syntax);
                    hasErrors = true;
                }
            }

            // KnownSizes is further mutated by BindArrayInitializerList as it works out more
            // information about the sizes.
            var isInferred = creationSyntax.IsKind(SyntaxKind.ImplicitArrayCreationExpression);
            BoundArrayInitialization initializer = BindArrayInitializerList(diagnostics, initSyntax, type, knownSizes, 1, isInferred, boundInitExprOpt);

            hasErrors = hasErrors || initializer.HasAnyErrors;

            bool hasCreationSyntax = creationSyntax != null;
            CSharpSyntaxNode nonNullSyntax = (CSharpSyntaxNode)creationSyntax ?? initSyntax;

            // Construct a set of size expressions if we were not given any.
            //
            // It is possible in error scenarios that some of the bounds were not determined. Substitute
            // zeroes for those.
            if (numSizes == 0)
            {
                BoundExpression[] sizeArray = new BoundExpression[rank];
                for (int i = 0; i < rank; i++)
                {
                    sizeArray[i] = new BoundLiteral(
                        nonNullSyntax,
                        ConstantValue.Create(knownSizes[i] ?? 0),
                        GetSpecialType(SpecialType.System_Int32, diagnostics, nonNullSyntax))
                    { WasCompilerGenerated = true };
                }
                sizes = sizeArray.AsImmutableOrNull();
            }
            else if (!hasErrors && rank != numSizes)
            {
                Error(diagnostics, ErrorCode.ERR_BadIndexCount, nonNullSyntax, type.Rank);
                hasErrors = true;
            }

            return new BoundArrayCreation(nonNullSyntax, sizes, initializer, type, hasErrors: hasErrors)
            {
                WasCompilerGenerated = !hasCreationSyntax &&
                    (initSyntax.Parent == null ||
                    initSyntax.Parent.Kind() != SyntaxKind.EqualsValueClause ||
                    ((EqualsValueClauseSyntax)initSyntax.Parent).Value != initSyntax)
            };
        }

        private BoundExpression BindStackAllocArrayCreationExpression(
            StackAllocArrayCreationExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            TypeSyntax typeSyntax = node.Type;

            if (typeSyntax.Kind() != SyntaxKind.ArrayType)
            {
                Error(diagnostics, ErrorCode.ERR_BadStackAllocExpr, typeSyntax);

                return new BoundBadExpression(
                    node,
                    LookupResultKind.NotCreatable, //in this context, anyway
                    ImmutableArray<Symbol>.Empty,
                    ImmutableArray<BoundExpression>.Empty,
                    new PointerTypeSymbol(BindType(typeSyntax, diagnostics)));
            }

            ArrayTypeSyntax arrayTypeSyntax = (ArrayTypeSyntax)typeSyntax;
            var elementTypeSyntax = arrayTypeSyntax.ElementType;
            var arrayType = (ArrayTypeSymbol)BindArrayType(arrayTypeSyntax, diagnostics, permitDimensions: true, basesBeingResolved: null, disallowRestrictedTypes: false).Type;
            var elementType = arrayType.ElementTypeWithAnnotations;

            TypeSymbol type = GetStackAllocType(node, elementType, diagnostics, out bool hasErrors);
            if (!elementType.Type.IsErrorType())
            {
                hasErrors = hasErrors || CheckManagedAddr(Compilation, elementType.Type, elementTypeSyntax.Location, diagnostics, errorForManaged: true);
            }

            SyntaxList<ArrayRankSpecifierSyntax> rankSpecifiers = arrayTypeSyntax.RankSpecifiers;

            if (rankSpecifiers.Count != 1 ||
                rankSpecifiers[0].Sizes.Count != 1)
            {
                // NOTE: Dev10 reported several parse errors here.
                Error(diagnostics, ErrorCode.ERR_BadStackAllocExpr, typeSyntax);

                var builder = ArrayBuilder<BoundExpression>.GetInstance();
                foreach (ArrayRankSpecifierSyntax rankSpecifier in rankSpecifiers)
                {
                    foreach (ExpressionSyntax size in rankSpecifier.Sizes)
                    {
                        if (size.Kind() != SyntaxKind.OmittedArraySizeExpression)
                        {
                            builder.Add(BindToTypeForErrorRecovery(BindExpression(size, BindingDiagnosticBag.Discarded)));
                        }
                    }
                }

                return new BoundBadExpression(
                    node,
                    LookupResultKind.Empty,
                    ImmutableArray<Symbol>.Empty,
                    builder.ToImmutableAndFree(),
                    new PointerTypeSymbol(elementType));
            }

            ExpressionSyntax countSyntax = rankSpecifiers[0].Sizes[0];
            BoundExpression count = null;
            if (countSyntax.Kind() != SyntaxKind.OmittedArraySizeExpression)
            {
                count = BindValue(countSyntax, diagnostics, BindValueKind.RValue);
                count = GenerateConversionForAssignment(GetSpecialType(SpecialType.System_Int32, diagnostics, node), count, diagnostics);
                if (IsNegativeConstantForArraySize(count))
                {
                    Error(diagnostics, ErrorCode.ERR_NegativeStackAllocSize, countSyntax);
                    hasErrors = true;
                }
            }
            else if (node.Initializer == null)
            {
                Error(diagnostics, ErrorCode.ERR_MissingArraySize, rankSpecifiers[0]);
                count = BadExpression(countSyntax);
                hasErrors = true;
            }

            return node.Initializer is null
                ? new BoundStackAllocArrayCreation(node, elementType.Type, count, initializerOpt: null, type, hasErrors: hasErrors)
                : BindStackAllocWithInitializer(node, node.StackAllocKeyword, node.Initializer, type, elementType.Type, count, diagnostics, hasErrors);
        }

        private bool ReportBadStackAllocPosition(SyntaxNode node, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(node is StackAllocArrayCreationExpressionSyntax || node is ImplicitStackAllocArrayCreationExpressionSyntax);
            bool inLegalPosition = true;

            // If we are using a language version that does not restrict the position of a stackalloc expression, skip that test.
            LanguageVersion requiredVersion = MessageID.IDS_FeatureNestedStackalloc.RequiredVersion();
            if (requiredVersion > Compilation.LanguageVersion)
            {
                inLegalPosition = (IsInMethodBody || IsLocalFunctionsScopeBinder) && node.IsLegalCSharp73SpanStackAllocPosition();
                if (!inLegalPosition)
                {
                    MessageID.IDS_FeatureNestedStackalloc.CheckFeatureAvailability(diagnostics, node.GetFirstToken());
                }
            }

            // Check if we're syntactically within a catch or finally clause.
            if (this.Flags.IncludesAny(BinderFlags.InCatchBlock | BinderFlags.InCatchFilter | BinderFlags.InFinallyBlock))
            {
                Error(diagnostics, ErrorCode.ERR_StackallocInCatchFinally, node);
            }

            return inLegalPosition;
        }

        private TypeSymbol GetStackAllocType(SyntaxNode node, TypeWithAnnotations elementTypeWithAnnotations, BindingDiagnosticBag diagnostics, out bool hasErrors)
        {
            var inLegalPosition = ReportBadStackAllocPosition(node, diagnostics);
            hasErrors = !inLegalPosition;
            if (inLegalPosition && !isStackallocTargetTyped(node))
            {
                CheckFeatureAvailability(node, MessageID.IDS_FeatureRefStructs, diagnostics);

                var spanType = GetWellKnownType(WellKnownType.System_Span_T, diagnostics, node);
                return ConstructNamedType(
                    type: spanType,
                    typeSyntax: node.Kind() == SyntaxKind.StackAllocArrayCreationExpression
                        ? ((StackAllocArrayCreationExpressionSyntax)node).Type
                        : node,
                    typeArgumentsSyntax: default,
                    typeArguments: ImmutableArray.Create(elementTypeWithAnnotations),
                    basesBeingResolved: null,
                    diagnostics: diagnostics);
            }

            // We treat the stackalloc as target-typed, so we give it a null type for now.
            return null;

            // Is this a context in which a stackalloc expression could be converted to the corresponding pointer
            // type? The only context that permits it is the initialization of a local variable declaration (when
            // the declaration appears as a statement or as the first part of a for loop).
            static bool isStackallocTargetTyped(SyntaxNode node)
            {
                Debug.Assert(node != null);

                SyntaxNode equalsValueClause = node.Parent;

                if (!equalsValueClause.IsKind(SyntaxKind.EqualsValueClause))
                {
                    return false;
                }

                SyntaxNode variableDeclarator = equalsValueClause.Parent;

                if (!variableDeclarator.IsKind(SyntaxKind.VariableDeclarator))
                {
                    return false;
                }

                SyntaxNode variableDeclaration = variableDeclarator.Parent;
                if (!variableDeclaration.IsKind(SyntaxKind.VariableDeclaration))
                {
                    return false;
                }

                return
                    variableDeclaration.Parent.IsKind(SyntaxKind.LocalDeclarationStatement) ||
                    variableDeclaration.Parent.IsKind(SyntaxKind.ForStatement);
            }
        }

        private BoundExpression BindStackAllocWithInitializer(
            SyntaxNode node,
            SyntaxToken stackAllocKeyword,
            InitializerExpressionSyntax initSyntax,
            TypeSymbol type,
            TypeSymbol elementType,
            BoundExpression sizeOpt,
            BindingDiagnosticBag diagnostics,
            bool hasErrors,
            ImmutableArray<BoundExpression> boundInitExprOpt = default)
        {
            Debug.Assert(node.IsKind(SyntaxKind.ImplicitStackAllocArrayCreationExpression) || node.IsKind(SyntaxKind.StackAllocArrayCreationExpression));

            MessageID.IDS_FeatureStackAllocInitializer.CheckFeatureAvailability(diagnostics, stackAllocKeyword);

            if (boundInitExprOpt.IsDefault)
            {
                boundInitExprOpt = BindArrayInitializerExpressions(initSyntax, diagnostics, dimension: 1, rank: 1);
            }

            boundInitExprOpt = boundInitExprOpt.SelectAsArray((expr, t) => GenerateConversionForAssignment(t.elementType, expr, t.diagnostics), (elementType, diagnostics));

            if (sizeOpt != null)
            {
                if (!sizeOpt.HasAnyErrors)
                {
                    int? constantSizeOpt = GetIntegerConstantForArraySize(sizeOpt);
                    if (constantSizeOpt == null)
                    {
                        Error(diagnostics, ErrorCode.ERR_ConstantExpected, sizeOpt.Syntax);
                        hasErrors = true;
                    }
                    else if (boundInitExprOpt.Length != constantSizeOpt)
                    {
                        Error(diagnostics, ErrorCode.ERR_ArrayInitializerIncorrectLength, node, constantSizeOpt.Value);
                        hasErrors = true;
                    }
                }
            }
            else
            {
                sizeOpt = new BoundLiteral(
                        node,
                        ConstantValue.Create(boundInitExprOpt.Length),
                        GetSpecialType(SpecialType.System_Int32, diagnostics, node))
                { WasCompilerGenerated = true };
            }

            bool isInferred = node.IsKind(SyntaxKind.ImplicitStackAllocArrayCreationExpression);
            return new BoundStackAllocArrayCreation(node, elementType, sizeOpt, new BoundArrayInitialization(initSyntax, isInferred, boundInitExprOpt), type, hasErrors);
        }

        private static int? GetIntegerConstantForArraySize(BoundExpression expression)
        {
            // If the bound could have been converted to int, then it was.  If it could not have been
            // converted to int, and it was a constant, then it was out of range.

            Debug.Assert(expression != null);
            if (expression.HasAnyErrors)
            {
                return null;
            }

            var constantValue = expression.ConstantValueOpt;

            if (constantValue == null || constantValue.IsBad || expression.Type.SpecialType != SpecialType.System_Int32)
            {
                return null;
            }

            return constantValue.Int32Value;
        }

        private static bool IsNegativeConstantForArraySize(BoundExpression expression)
        {
            Debug.Assert(expression != null);

            if (expression.HasAnyErrors)
            {
                return false;
            }

            var constantValue = expression.ConstantValueOpt;
            if (constantValue == null || constantValue.IsBad)
            {
                return false;
            }

            var type = expression.Type.SpecialType;
            if (type == SpecialType.System_Int32)
            {
                return constantValue.Int32Value < 0;
            }

            if (type == SpecialType.System_Int64)
            {
                return constantValue.Int64Value < 0;
            }

            // By the time we get here we definitely have int, long, uint or ulong.  Obviously the
            // latter two are never negative.
            Debug.Assert(type == SpecialType.System_UInt32 || type == SpecialType.System_UInt64);

            return false;
        }

        /// <summary>
        /// Bind the (implicit or explicit) constructor initializer of a constructor symbol (in source).
        /// </summary>
        /// <param name="initializerArgumentListOpt">
        /// Null for implicit, 
        /// <see cref="ConstructorInitializerSyntax.ArgumentList"/>, or 
        /// <see cref="PrimaryConstructorBaseTypeSyntax.ArgumentList"/> for explicit.</param>
        /// <param name="constructor">Constructor containing the initializer.</param>
        /// <param name="diagnostics">Accumulates errors (e.g. unable to find constructor to invoke).</param>
        /// <returns>A bound expression for the constructor initializer call.</returns>
        /// <remarks>
        /// This method should be kept consistent with Compiler.BindConstructorInitializer (e.g. same error codes).
        /// </remarks>
        internal BoundExpression BindConstructorInitializer(
            ArgumentListSyntax initializerArgumentListOpt,
            MethodSymbol constructor,
            BindingDiagnosticBag diagnostics)
        {
            Binder argumentListBinder = null;

            if (initializerArgumentListOpt != null)
            {
                argumentListBinder = this.GetBinder(initializerArgumentListOpt);
            }

            var result = (argumentListBinder ?? this).BindConstructorInitializerCore(initializerArgumentListOpt, constructor, diagnostics);

            if (argumentListBinder != null)
            {
                // This code is reachable only for speculative SemanticModel.
                Debug.Assert(argumentListBinder.IsSemanticModelBinder);
                result = argumentListBinder.WrapWithVariablesIfAny(initializerArgumentListOpt, result);
            }

            return result;
        }

        private BoundExpression BindConstructorInitializerCore(
            ArgumentListSyntax initializerArgumentListOpt,
            MethodSymbol constructor,
            BindingDiagnosticBag diagnostics)
        {
            // Either our base type is not object, or we have an initializer syntax, or both. We're going to
            // need to do overload resolution on the set of constructors of the base type, either on
            // the provided initializer syntax, or on an implicit ": base()" syntax.

            // SPEC ERROR: The specification states that if you have the situation 
            // SPEC ERROR: class B { ... } class D1 : B {} then the default constructor
            // SPEC ERROR: generated for D1 must call an accessible *parameterless* constructor
            // SPEC ERROR: in B. However, it also states that if you have 
            // SPEC ERROR: class B { ... } class D2 : B { D2() {} }  or
            // SPEC ERROR: class B { ... } class D3 : B { D3() : base() {} }  then
            // SPEC ERROR: the compiler performs *overload resolution* to determine
            // SPEC ERROR: which accessible constructor of B is called. Since B might have
            // SPEC ERROR: a ctor with all optional parameters, overload resolution might
            // SPEC ERROR: succeed even if there is no parameterless constructor. This
            // SPEC ERROR: is unintentionally inconsistent, and the native compiler does not
            // SPEC ERROR: implement this behavior. Rather, we should say in the spec that
            // SPEC ERROR: if there is no ctor in D1, then a ctor is created for you exactly
            // SPEC ERROR: as though you'd said "D1() : base() {}". 
            // SPEC ERROR: This is what we now do in Roslyn.

            Debug.Assert((object)constructor != null);
            Debug.Assert(constructor.MethodKind == MethodKind.Constructor ||
                constructor.MethodKind == MethodKind.StaticConstructor); // error scenario: constructor initializer on static constructor
            Debug.Assert(diagnostics != null);

            NamedTypeSymbol containingType = constructor.ContainingType;

            // Structs and enums do not have implicit constructor initializers.
            if ((containingType.TypeKind == TypeKind.Enum || containingType.TypeKind == TypeKind.Struct) && initializerArgumentListOpt == null)
            {
                return null;
            }

            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();
            try
            {
                TypeSymbol constructorReturnType = constructor.ReturnType;
                Debug.Assert(constructorReturnType.IsVoidType()); //true of all constructors

                // Get the bound arguments and the argument names.
                // : this(__arglist()) is legal
                if (initializerArgumentListOpt != null)
                {
                    this.BindArgumentsAndNames(initializerArgumentListOpt, diagnostics, analyzedArguments, allowArglist: true);
                }

                NamedTypeSymbol initializerType = containingType;

                bool isBaseConstructorInitializer = initializerArgumentListOpt == null ||
                                                    initializerArgumentListOpt.Parent.Kind() != SyntaxKind.ThisConstructorInitializer;

                if (isBaseConstructorInitializer)
                {
                    initializerType = initializerType.BaseTypeNoUseSiteDiagnostics;

                    // Soft assert: we think this is the case, and we're asserting to catch scenarios that violate our expectations
                    Debug.Assert((object)initializerType != null ||
                    containingType.SpecialType == SpecialType.System_Object ||
                        containingType.IsInterface);

                    if ((object)initializerType == null || containingType.SpecialType == SpecialType.System_Object) //e.g. when defining System.Object in source
                    {
                        // If the constructor initializer is implicit and there is no base type, we're done.
                        // Otherwise, if the constructor initializer is explicit, we're in an error state.
                        if (initializerArgumentListOpt == null)
                        {
                            return null;
                        }
                        else
                        {
                            diagnostics.Add(ErrorCode.ERR_ObjectCallingBaseConstructor, constructor.GetFirstLocation(), containingType);
                            return new BoundBadExpression(
                                syntax: initializerArgumentListOpt.Parent,
                                resultKind: LookupResultKind.Empty,
                                symbols: ImmutableArray<Symbol>.Empty,
                                childBoundNodes: BuildArgumentsForErrorRecovery(analyzedArguments),
                                type: constructorReturnType);
                        }
                    }
                    else if (initializerArgumentListOpt != null && containingType.TypeKind == TypeKind.Struct)
                    {
                        diagnostics.Add(ErrorCode.ERR_StructWithBaseConstructorCall, constructor.GetFirstLocation(), containingType);
                        return new BoundBadExpression(
                            syntax: initializerArgumentListOpt.Parent,
                            resultKind: LookupResultKind.Empty,
                            symbols: ImmutableArray<Symbol>.Empty, //CONSIDER: we could look for a matching constructor on System.ValueType
                            childBoundNodes: BuildArgumentsForErrorRecovery(analyzedArguments),
                            type: constructorReturnType);
                    }
                }
                else
                {
                    Debug.Assert(initializerArgumentListOpt.Parent.Kind() == SyntaxKind.ThisConstructorInitializer);
                }

                CSharpSyntaxNode nonNullSyntax;
                Location errorLocation;
                bool enableCallerInfo;

                switch (initializerArgumentListOpt?.Parent)
                {
                    case ConstructorInitializerSyntax initializerSyntax:
                        nonNullSyntax = initializerSyntax;
                        errorLocation = initializerSyntax.ThisOrBaseKeyword.GetLocation();
                        enableCallerInfo = true;
                        break;

                    case PrimaryConstructorBaseTypeSyntax baseWithArguments:
                        nonNullSyntax = baseWithArguments;
                        errorLocation = initializerArgumentListOpt.GetLocation();
                        enableCallerInfo = true;
                        break;

                    default:
                        // Note: use syntax node of constructor with initializer, not constructor invoked by initializer (i.e. methodResolutionResult).
                        nonNullSyntax = constructor.GetNonNullSyntaxNode();
                        errorLocation = constructor.GetFirstLocation();
                        enableCallerInfo = false;
                        break;
                }

                if (initializerArgumentListOpt != null && analyzedArguments.HasDynamicArgument)
                {
                    diagnostics.Add(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, errorLocation);

                    return new BoundBadExpression(
                            syntax: initializerArgumentListOpt.Parent,
                            resultKind: LookupResultKind.Empty,
                            symbols: ImmutableArray<Symbol>.Empty, //CONSIDER: we could look for a matching constructor on System.ValueType
                            childBoundNodes: BuildArgumentsForErrorRecovery(analyzedArguments),
                            type: constructorReturnType);
                }

                MemberResolutionResult<MethodSymbol> memberResolutionResult;
                ImmutableArray<MethodSymbol> candidateConstructors;
                bool found = TryPerformConstructorOverloadResolution(
                                    initializerType,
                                    analyzedArguments,
                                    WellKnownMemberNames.InstanceConstructorName,
                                    errorLocation,
                                    false, // Don't suppress result diagnostics
                                    diagnostics,
                                    out memberResolutionResult,
                                    out candidateConstructors,
                                    allowProtectedConstructorsOfBaseType: true,
                                    out CompoundUseSiteInfo<AssemblySymbol> overloadResolutionUseSiteInfo);

                return BindConstructorInitializerCoreContinued(found, initializerArgumentListOpt, constructor, analyzedArguments, constructorReturnType,
                    initializerType, isBaseConstructorInitializer, nonNullSyntax, errorLocation, enableCallerInfo, memberResolutionResult, candidateConstructors, in overloadResolutionUseSiteInfo, diagnostics);
            }
            finally
            {
                analyzedArguments.Free();
            }
        }

        private BoundExpression BindConstructorInitializerCoreContinued(
            bool found,
            ArgumentListSyntax initializerArgumentListOpt,
            MethodSymbol constructor,
            AnalyzedArguments analyzedArguments,
            TypeSymbol constructorReturnType,
            NamedTypeSymbol initializerType,
            bool isBaseConstructorInitializer,
            CSharpSyntaxNode nonNullSyntax,
            Location errorLocation,
            bool enableCallerInfo,
            MemberResolutionResult<MethodSymbol> memberResolutionResult,
            ImmutableArray<MethodSymbol> candidateConstructors,
            in CompoundUseSiteInfo<AssemblySymbol> overloadResolutionUseSiteInfo,
            BindingDiagnosticBag diagnostics)
        {
            ReportConstructorUseSiteDiagnostics(errorLocation, diagnostics, suppressUnsupportedRequiredMembersError: true, in overloadResolutionUseSiteInfo);

            ImmutableArray<int> argsToParamsOpt;

            if (memberResolutionResult.IsNotNull)
            {
                this.CheckAndCoerceArguments<MethodSymbol>(nonNullSyntax, memberResolutionResult, analyzedArguments, diagnostics, receiver: null, invokedAsExtensionMethod: false, out argsToParamsOpt);
            }
            else
            {
                argsToParamsOpt = memberResolutionResult.Result.ArgsToParamsOpt;
            }

            NamedTypeSymbol baseType = constructor.ContainingType.BaseTypeNoUseSiteDiagnostics;

            MethodSymbol resultMember = memberResolutionResult.Member;

            BoundExpression receiver = ThisReference(nonNullSyntax, initializerType, wasCompilerGenerated: true);
            validateRecordCopyConstructor(constructor, baseType, resultMember, errorLocation, diagnostics);

            if (found)
            {
                bool hasErrors = false;

                if (resultMember == constructor)
                {
                    Debug.Assert(initializerType.IsErrorType() ||
                        (initializerArgumentListOpt != null && initializerArgumentListOpt.Parent.Kind() == SyntaxKind.ThisConstructorInitializer));
                    diagnostics.Add(ErrorCode.ERR_RecursiveConstructorCall,
                                    errorLocation,
                                    constructor);

                    hasErrors = true; // prevent recursive constructor from being emitted
                }
                else if (resultMember.HasParameterContainingPointerType())
                {
                    // What if some of the arguments are implicit?  Dev10 reports unsafe errors
                    // if the implied argument would have an unsafe type.  We need to check
                    // the parameters explicitly, since there won't be bound nodes for the implied
                    // arguments until lowering.

                    // Don't worry about double reporting (i.e. for both the argument and the parameter)
                    // because only one unsafe diagnostic is allowed per scope - the others are suppressed.
                    hasErrors = ReportUnsafeIfNotAllowed(errorLocation, diagnostics);
                }

                ReportDiagnosticsIfObsolete(diagnostics, resultMember, nonNullSyntax, hasBaseReceiver: isBaseConstructorInitializer);

                var expanded = memberResolutionResult.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm;

                if (constructor is SynthesizedPrimaryConstructor primaryConstructor)
                {
                    var parametersPassedToBase = new OrderedSet<ParameterSymbol>();

                    for (int i = 0; i < analyzedArguments.Arguments.Count; i++)
                    {
                        if (analyzedArguments.RefKind(i) is (RefKind.Ref or RefKind.Out))
                        {
                            continue;
                        }

                        if (TryGetPrimaryConstructorParameterUsedAsValue(primaryConstructor, analyzedArguments.Argument(i)) is (ParameterSymbol parameter, SyntaxNode syntax))
                        {
                            if (expanded)
                            {
                                var baseParameter = GetCorrespondingParameter(i, resultMember.Parameters, argsToParamsOpt, expanded: true);

                                if (baseParameter.Ordinal == resultMember.ParameterCount - 1)
                                {
                                    continue;
                                }
                            }

                            if (parametersPassedToBase.Add(parameter))
                            {
                                if (primaryConstructor.GetCapturedParameters().ContainsKey(parameter))
                                {
                                    diagnostics.Add(ErrorCode.WRN_CapturedPrimaryConstructorParameterPassedToBase, syntax.Location, parameter);
                                }
                            }
                        }
                    }

                    primaryConstructor.SetParametersPassedToTheBase(parametersPassedToBase);
                }

                Debug.Assert(!resultMember.IsExtensionBlockMember());
                BindDefaultArguments(nonNullSyntax, resultMember.Parameters, extensionReceiver: null, analyzedArguments.Arguments, analyzedArguments.RefKinds, analyzedArguments.Names, ref argsToParamsOpt, out var defaultArguments, expanded, enableCallerInfo, diagnostics);

                var arguments = analyzedArguments.Arguments.ToImmutable();
                var refKinds = analyzedArguments.RefKinds.ToImmutableOrNull();

                if (resultMember.HasSetsRequiredMembers && !constructor.HasSetsRequiredMembers)
                {
                    hasErrors = true;
                    // This constructor must add 'SetsRequiredMembers' because it chains to a constructor that has that attribute.
                    diagnostics.Add(ErrorCode.ERR_ChainingToSetsRequiredMembersRequiresSetsRequiredMembers, errorLocation);
                }

                return new BoundCall(
                    nonNullSyntax,
                    receiver,
                    initialBindingReceiverIsSubjectToCloning: ReceiverIsSubjectToCloning(receiver, resultMember),
                    resultMember,
                    arguments,
                    analyzedArguments.GetNames(),
                    refKinds,
                    isDelegateCall: false,
                    expanded,
                    invokedAsExtensionMethod: false,
                    argsToParamsOpt: argsToParamsOpt,
                    defaultArguments: defaultArguments,
                    resultKind: LookupResultKind.Viable,
                    type: constructorReturnType,
                    hasErrors: hasErrors)
                { WasCompilerGenerated = initializerArgumentListOpt == null };
            }
            else
            {
                var result = CreateBadCall(
                    node: nonNullSyntax,
                    name: WellKnownMemberNames.InstanceConstructorName,
                    receiver: receiver,
                    methods: candidateConstructors,
                    resultKind: LookupResultKind.OverloadResolutionFailure,
                    typeArgumentsWithAnnotations: ImmutableArray<TypeWithAnnotations>.Empty,
                    analyzedArguments: analyzedArguments,
                    invokedAsExtensionMethod: false,
                    isDelegate: false,
                    BindingDiagnosticBag.Discarded);
                result.WasCompilerGenerated = initializerArgumentListOpt == null;
                return result;
            }

            static void validateRecordCopyConstructor(MethodSymbol constructor, NamedTypeSymbol baseType, MethodSymbol resultMember, Location errorLocation, BindingDiagnosticBag diagnostics)
            {
                if (IsUserDefinedRecordCopyConstructor(constructor))
                {
                    if (baseType.SpecialType == SpecialType.System_Object)
                    {
                        if (resultMember is null || resultMember.ContainingType.SpecialType != SpecialType.System_Object)
                        {
                            // Record deriving from object must use `base()`, not `this()`
                            diagnostics.Add(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, errorLocation);
                        }

                        return;
                    }

                    // Unless the base type is 'object', the constructor should invoke a base type copy constructor
                    if (resultMember is null || !SynthesizedRecordCopyCtor.HasCopyConstructorSignature(resultMember))
                    {
                        diagnostics.Add(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, errorLocation);
                    }
                }
            }
        }

        private static (ParameterSymbol, SyntaxNode) TryGetPrimaryConstructorParameterUsedAsValue(SynthesizedPrimaryConstructor primaryConstructor, BoundExpression boundExpression)
        {
            BoundParameter boundParameter;

            switch (boundExpression)
            {
                case BoundParameter param:
                    boundParameter = param;
                    break;

                case BoundConversion { Conversion.IsIdentity: true, Operand: BoundParameter param }:
                    boundParameter = param;
                    break;

                default:
                    return (null, null);
            }

            if (boundParameter.ParameterSymbol is { } parameter &&
                parameter.ContainingSymbol == (object)primaryConstructor)
            {
                return (parameter, boundParameter.Syntax);
            }

            return (null, null);
        }

        internal static bool IsUserDefinedRecordCopyConstructor(MethodSymbol constructor)
        {
            return constructor.ContainingType is SourceNamedTypeSymbol sourceType &&
                sourceType.IsRecord &&
                constructor is not SynthesizedPrimaryConstructor &&
                SynthesizedRecordCopyCtor.HasCopyConstructorSignature(constructor);
        }

        private BoundExpression BindImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            MessageID.IDS_FeatureImplicitObjectCreation.CheckFeatureAvailability(diagnostics, node.NewKeyword);

            var arguments = AnalyzedArguments.GetInstance();
            BindArgumentsAndNames(node.ArgumentList, diagnostics, arguments, allowArglist: true);
            var result = new BoundUnconvertedObjectCreationExpression(
                node,
                arguments.Arguments.ToImmutable(),
                arguments.Names.ToImmutableOrNull(),
                arguments.RefKinds.ToImmutableOrNull(),
                node.Initializer,
                binder: this);
            arguments.Free();
            return result;
        }

        protected BoundExpression BindObjectCreationExpression(ObjectCreationExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            BoundExpression result = bindObjectCreationExpression(node, diagnostics);

            // Assert that the shape of the BoundBadExpression is sound and is not going to confuse NullableWalker for target-typed 'new'.
            Debug.Assert(result is not BoundBadExpression { ChildBoundNodes: var children } || !children.Any((child, node) => child.Syntax == node, node));

            return result;

            BoundExpression bindObjectCreationExpression(ObjectCreationExpressionSyntax node, BindingDiagnosticBag diagnostics)
            {
                var typeWithAnnotations = BindType(node.Type, diagnostics);
                var type = typeWithAnnotations.Type;
                var originalType = type;

                if (typeWithAnnotations.NullableAnnotation.IsAnnotated() && !type.IsNullableType())
                {
                    diagnostics.Add(ErrorCode.ERR_AnnotationDisallowedInObjectCreation, node.Location);
                }

                switch (type.TypeKind)
                {
                    case TypeKind.Struct:
                    case TypeKind.Class:
                    case TypeKind.Enum:
                    case TypeKind.Error:
                        return BindClassCreationExpression(node, (NamedTypeSymbol)type, GetName(node.Type), diagnostics, originalType);

                    case TypeKind.Delegate:
                        return BindDelegateCreationExpression(node, (NamedTypeSymbol)type, diagnostics);

                    case TypeKind.Interface:
                        return BindInterfaceCreationExpression(node, (NamedTypeSymbol)type, diagnostics);

                    case TypeKind.TypeParameter:
                        return BindTypeParameterCreationExpression(node, (TypeParameterSymbol)type, diagnostics);

                    case TypeKind.Pointer:
                    case TypeKind.FunctionPointer:
                        type = new ExtendedErrorTypeSymbol(type, LookupResultKind.NotCreatable,
                            diagnostics.Add(ErrorCode.ERR_UnsafeTypeInObjectCreation, node.Location, type));
                        goto case TypeKind.Class;

                    case TypeKind.Submission:
                    // script class is synthesized and should not be used as a type of a new expression:
                    case TypeKind.Dynamic:
                    // we didn't find any type called "dynamic" so we are using the builtin dynamic type, which has no constructors:
                    case TypeKind.Array:
                        // ex: new ref[]
                        type = new ExtendedErrorTypeSymbol(type, LookupResultKind.NotCreatable,
                            diagnostics.Add(ErrorCode.ERR_InvalidObjectCreation, node.Type.Location));
                        goto case TypeKind.Class;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(type.TypeKind);
                }
            }
        }

#nullable enable
        private BoundExpression BindCollectionExpression(CollectionExpressionSyntax syntax, BindingDiagnosticBag diagnostics, int nestingLevel = 0)
        {
            const int MaxNestingLevel = 64;
            if (nestingLevel >= MaxNestingLevel)
            {
                // An expression is too long or complex to compile
                diagnostics.Add(ErrorCode.ERR_InsufficientStack, syntax.Location);
                return new BoundBadExpression(syntax, LookupResultKind.Empty, ImmutableArray<Symbol?>.Empty, ImmutableArray<BoundExpression>.Empty, CreateErrorType());
            }

            MessageID.IDS_FeatureCollectionExpressions.CheckFeatureAvailability(diagnostics, syntax, syntax.OpenBracketToken.GetLocation());

            BoundUnconvertedWithElement? firstWithElement = null;

            var builder = ArrayBuilder<BoundNode>.GetInstance(syntax.Elements.Count);
            foreach (var element in syntax.Elements)
            {
#pragma warning disable RSEXPERIMENTAL006 // With Element: https://github.com/dotnet/roslyn/issues/80613
                if (element is WithElementSyntax withElementSyntax)
#pragma warning restore RSEXPERIMENTAL006
                {
                    MessageID.IDS_FeatureCollectionExpressionArguments.CheckFeatureAvailability(diagnostics, syntax, withElementSyntax.WithKeyword.GetLocation());

                    var (withElement, badElement) = bindWithElement(
                        this, syntax, withElementSyntax, diagnostics);
                    firstWithElement ??= withElement;
                    builder.AddIfNotNull(badElement);
                }
                else
                {
                    builder.Add(bindElement(element, diagnostics, this, nestingLevel));
                }
            }

            return new BoundUnconvertedCollectionExpression(syntax, firstWithElement, builder.ToImmutableAndFree());

            static BoundNode bindElement(CollectionElementSyntax syntax, BindingDiagnosticBag diagnostics, Binder @this, int nestingLevel)
            {
                return syntax switch
                {
                    ExpressionElementSyntax { Expression: CollectionExpressionSyntax nestedCollectionExpression } => @this.BindCollectionExpression(nestedCollectionExpression, diagnostics, nestingLevel + 1),
                    ExpressionElementSyntax expressionElementSyntax => @this.BindValue(expressionElementSyntax.Expression, diagnostics, BindValueKind.RValue),
                    SpreadElementSyntax spreadElementSyntax => bindSpreadElement(spreadElementSyntax, diagnostics, @this),
                    _ => throw ExceptionUtilities.UnexpectedValue(syntax.Kind())
                };
            }

            static BoundNode bindSpreadElement(SpreadElementSyntax syntax, BindingDiagnosticBag diagnostics, Binder @this)
            {
                // Spreads are blocked in exception filters because the try/finally from disposing the enumerator is not allowed in a filter.
                if (@this.Flags.Includes(BinderFlags.InCatchFilter))
                {
                    Error(diagnostics, ErrorCode.ERR_BadSpreadInCatchFilter, syntax);
                }

                var expression = @this.BindRValueWithoutTargetType(syntax.Expression, diagnostics);
                ForEachEnumeratorInfo.Builder builder;
                bool hasErrors = !@this.GetEnumeratorInfoAndInferCollectionElementType(syntax, syntax.Expression, ref expression, isAsync: false, isSpread: true, diagnostics, inferredType: out _, out builder) ||
                    builder.IsIncomplete;
                if (hasErrors)
                {
                    return new BoundCollectionExpressionSpreadElement(
                        syntax,
                        expression,
                        expressionPlaceholder: null,
                        conversion: null,
                        enumeratorInfoOpt: null,
                        lengthOrCount: null,
                        elementPlaceholder: null,
                        iteratorBody: null,
                        hasErrors);
                }

                Debug.Assert(expression.Type is { });

                var expressionPlaceholder = new BoundCollectionExpressionSpreadExpressionPlaceholder(syntax.Expression, expression.Type);
                var enumeratorInfo = builder.Build(location: default);
                var collectionType = enumeratorInfo.CollectionType;
                var useSiteInfo = @this.GetNewCompoundUseSiteInfo(diagnostics);
                var conversion = @this.Conversions.ClassifyConversionFromExpression(expression, collectionType, isChecked: @this.CheckOverflowAtRuntime, ref useSiteInfo);
                Debug.Assert(conversion.IsValid);
                diagnostics.Add(syntax.Expression, useSiteInfo);
                var convertedExpression = @this.ConvertForEachCollection(expressionPlaceholder, conversion, collectionType, diagnostics);

                BoundExpression? lengthOrCount;

                if (enumeratorInfo is { InlineArraySpanType: not WellKnownType.Unknown })
                {
                    _ = expression.Type.HasInlineArrayAttribute(out int length);
                    Debug.Assert(length > 0);
                    lengthOrCount = new BoundLiteral(expression.Syntax, ConstantValue.Create(length), @this.GetSpecialType(SpecialType.System_Int32, diagnostics, expression.Syntax)) { WasCompilerGenerated = true };
                }
                else if (!@this.TryBindLengthOrCount(syntax.Expression, expressionPlaceholder, out lengthOrCount, diagnostics))
                {
                    lengthOrCount = null;
                }

                return new BoundCollectionExpressionSpreadElement(
                    syntax,
                    expression,
                    expressionPlaceholder: expressionPlaceholder,
                    conversion: convertedExpression,
                    enumeratorInfo,
                    lengthOrCount: lengthOrCount,
                    elementPlaceholder: null,
                    iteratorBody: null,
                    hasErrors: false);
            }

            static (BoundUnconvertedWithElement? withElement, BoundBadExpression? badExpression) bindWithElement(
                Binder @this,
                CollectionExpressionSyntax syntax,
#pragma warning disable RSEXPERIMENTAL006 // With Element: https://github.com/dotnet/roslyn/issues/80613
                WithElementSyntax withElementSyntax,
#pragma warning restore RSEXPERIMENTAL006
                BindingDiagnosticBag diagnostics)
            {
                // Report a withElement that is not first. Note: for the purposes of error recovery and diagnostics
                // we still bind the arguments in those later with elements.  However, we only validate those
                // arguments against the final arguments against the destination target type if the with element
                // was in the proper position.

                var analyzedArguments = AnalyzedArguments.GetInstance();

                @this.BindArgumentsAndNames(withElementSyntax.ArgumentList, diagnostics, analyzedArguments, allowArglist: true);

                var arguments = analyzedArguments.Arguments;
                for (int i = 0; i < arguments.Count; i++)
                {
                    var arg = arguments[i];
                    if (arg.Type is { TypeKind: TypeKind.Dynamic })
                    {
                        // Collection arguments cannot be dynamic
                        diagnostics.Add(ErrorCode.ERR_CollectionArgumentsDynamicBinding, arg.Syntax);
                        arguments[i] = new BoundBadExpression(
                            arg.Syntax, LookupResultKind.Empty, symbols: [],
                            childBoundNodes: [@this.BindToNaturalType(arg, diagnostics, reportNoTargetType: false)],
                            type: @this.Compilation.GetSpecialType(SpecialType.System_Object));
                    }
                }

                BoundUnconvertedWithElement? withElement;
                BoundBadExpression? badExpression;

                if (withElementSyntax == syntax.Elements.First())
                {
                    // Got a with-element, and it was in the right place.  Pass it along directly in
                    // unconverted-collection-expression so that we can construct the collection properly.
                    withElement = new BoundUnconvertedWithElement(
                        withElementSyntax,
                        analyzedArguments.Arguments.ToImmutable(),
                        analyzedArguments.Names.ToImmutableOrNull(),
                        analyzedArguments.RefKinds.ToImmutableOrNull());
                    badExpression = null;
                }
                else
                {
                    // Improperly placed with-element.  Report an error and pass along the arguments so they remain
                    // in the tree for further analysis, but replace the with-element itself with a bad node so that
                    // it doesn't influence later transformations.
                    diagnostics.Add(ErrorCode.ERR_CollectionArgumentsMustBeFirst, withElementSyntax.WithKeyword);

                    withElement = null;
                    badExpression = @this.BadExpression(withElementSyntax, @this.BuildArgumentsForErrorRecovery(analyzedArguments));
                }

                analyzedArguments.Free();
                return (withElement, badExpression);
            }
        }
#nullable disable

        private BoundExpression BindDelegateCreationExpression(ObjectCreationExpressionSyntax node, NamedTypeSymbol type, BindingDiagnosticBag diagnostics)
        {
            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();
            BindArgumentsAndNames(node.ArgumentList, diagnostics, analyzedArguments, isDelegateCreation: true);
            var result = BindDelegateCreationExpression(node, type, analyzedArguments, node.Initializer, wasTargetTyped: false, diagnostics);
            analyzedArguments.Free();
            return result;
        }

        private BoundExpression BindDelegateCreationExpression(SyntaxNode node, NamedTypeSymbol type, AnalyzedArguments analyzedArguments, InitializerExpressionSyntax initializerOpt, bool wasTargetTyped, BindingDiagnosticBag diagnostics)
        {
            bool hasErrors = false;
            if (analyzedArguments.HasErrors)
            {
                // Let's skip this part of further error checking without marking hasErrors = true here,
                // as the argument could be an unbound lambda, and the error could come from inside.
                // We'll check analyzedArguments.HasErrors again after we find if this is not the case.
            }
            else if (analyzedArguments.Arguments.Count == 0)
            {
                diagnostics.Add(ErrorCode.ERR_BadCtorArgCount, node.Location, type, 0);
                hasErrors = true;
            }
            else if (analyzedArguments.Names.Count != 0 || analyzedArguments.RefKinds.Count != 0 || analyzedArguments.Arguments.Count != 1)
            {
                // Use a smaller span that excludes the parens.
                var argSyntax = analyzedArguments.Arguments[0].Syntax;
                var start = argSyntax.SpanStart;
                var end = analyzedArguments.Arguments[analyzedArguments.Arguments.Count - 1].Syntax.Span.End;
                var errorSpan = new TextSpan(start, end - start);

                var loc = new SourceLocation(argSyntax.SyntaxTree, errorSpan);

                diagnostics.Add(ErrorCode.ERR_MethodNameExpected, loc);
                hasErrors = true;
            }

            if (initializerOpt != null)
            {
                Error(diagnostics, ErrorCode.ERR_ObjectOrCollectionInitializerWithDelegateCreation, node);
                hasErrors = true;
            }

            BoundExpression argument = analyzedArguments.Arguments.Count >= 1 ? BindToNaturalType(analyzedArguments.Arguments[0], diagnostics) : null;

            if (hasErrors)
            {
                // skip the rest of this binding
            }

            // There are four cases for a delegate creation expression (7.6.10.5):
            // 1. An anonymous function is treated as a conversion from the anonymous function to the delegate type.
            else if (argument is UnboundLambda unboundLambda)
            {
                // analyzedArguments.HasErrors could be true,
                // but here the argument is an unbound lambda, the error comes from inside
                // eg: new Action<int>(x => x.)
                // We should try to bind it anyway in order for intellisense to work.

                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                var conversion = this.Conversions.ClassifyConversionFromExpression(unboundLambda, type, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);
                diagnostics.Add(node, useSiteInfo);
                // Attempting to make the conversion caches the diagnostics and the bound state inside
                // the unbound lambda. Fetch the result from the cache.
                Debug.Assert(!type.IsGenericOrNonGenericExpressionType(out _));
                BoundLambda boundLambda = unboundLambda.Bind(type, isExpressionTree: false);

                if (!conversion.IsImplicit || !conversion.IsValid)
                {
                    GenerateImplicitConversionError(diagnostics, unboundLambda.Syntax, conversion, unboundLambda, type);
                }
                else
                {
                    // We're not going to produce an error, but it is possible that the conversion from
                    // the lambda to the delegate type produced a warning, which we have not reported.
                    // Instead, we've cached it in the bound lambda. Report it now.
                    diagnostics.AddRange(boundLambda.Diagnostics);
                }

                hasErrors = !conversion.IsImplicit;
                if (!hasErrors)
                {
                    CheckParameterModifierMismatchMethodConversion(unboundLambda.Syntax, boundLambda.Symbol, type, invokedAsExtensionMethod: false, diagnostics);
                    CheckLambdaConversion((LambdaSymbol)boundLambda.Symbol, type, diagnostics);
                }

                // Just stuff the bound lambda into the delegate creation expression. When we lower the lambda to
                // its method form we will rewrite this expression to refer to the method.

                return new BoundDelegateCreationExpression(node, boundLambda, methodOpt: null, isExtensionMethod: false, wasTargetTyped, type: type, hasErrors: hasErrors);
            }

            else if (analyzedArguments.HasErrors)
            {
                // There is no hope, skip.
            }

            // 2. A method group
            else if (argument.Kind == BoundKind.MethodGroup)
            {
                Conversion conversion;
                BoundMethodGroup methodGroup = (BoundMethodGroup)argument;
                hasErrors = MethodGroupConversionDoesNotExistOrHasErrors(methodGroup, type, node.Location, diagnostics, out conversion);
                methodGroup = FixMethodGroupWithTypeOrValue(methodGroup, conversion, diagnostics);
                return new BoundDelegateCreationExpression(node, methodGroup, conversion.Method, conversion.IsExtensionMethod, wasTargetTyped, type, hasErrors);
            }

            else if ((object)argument.Type == null)
            {
                diagnostics.Add(ErrorCode.ERR_MethodNameExpected, argument.Syntax.Location);
            }

            // 3. A value of the compile-time type dynamic (which is dynamically case 4), or
            else if (argument.HasDynamicType())
            {
                return new BoundDelegateCreationExpression(node, argument, methodOpt: null, isExtensionMethod: false, wasTargetTyped, type: type);
            }

            // 4. A delegate type.
            else if (argument.Type.TypeKind == TypeKind.Delegate)
            {
                var sourceDelegate = (NamedTypeSymbol)argument.Type;
                MethodGroup methodGroup = MethodGroup.GetInstance();
                try
                {
                    if (ReportDelegateInvokeUseSiteDiagnostic(diagnostics, argument.Type, node: node))
                    {
                        // We want failed "new" expression to use the constructors as their symbols.
                        return new BoundBadExpression(node, LookupResultKind.NotInvocable, StaticCast<Symbol>.From(type.InstanceConstructors), ImmutableArray.Create(argument), type);
                    }

                    methodGroup.PopulateWithSingleMethod(argument, sourceDelegate.DelegateInvokeMethod);
                    CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                    Conversion conv = Conversions.MethodGroupConversion(argument.Syntax, methodGroup, type, ref useSiteInfo);
                    diagnostics.Add(node, useSiteInfo);
                    if (!conv.Exists)
                    {
                        var boundMethodGroup = new BoundMethodGroup(
                            argument.Syntax, default, WellKnownMemberNames.DelegateInvokeName, ImmutableArray.Create(sourceDelegate.DelegateInvokeMethod),
                            sourceDelegate.DelegateInvokeMethod, null, BoundMethodGroupFlags.None, functionType: null, argument, LookupResultKind.Viable);
                        if (!Conversions.ReportDelegateOrFunctionPointerMethodGroupDiagnostics(this, boundMethodGroup, type, diagnostics))
                        {
                            // If we could not produce a more specialized diagnostic, we report
                            // No overload for '{0}' matches delegate '{1}'
                            diagnostics.Add(ErrorCode.ERR_MethDelegateMismatch, node.Location,
                                sourceDelegate.DelegateInvokeMethod,
                                type);
                        }
                    }
                    else
                    {
                        Debug.Assert(!conv.IsExtensionMethod);
                        Debug.Assert(conv.IsValid); // i.e. if it exists, then it is valid.

                        if (!this.MethodGroupConversionHasErrors(argument.Syntax, conv, argument, conv.IsExtensionMethod, isAddressOf: false, type, diagnostics))
                        {
                            // we do not place the "Invoke" method in the node, indicating that it did not appear in source.
                            return new BoundDelegateCreationExpression(node, argument, methodOpt: null, isExtensionMethod: false, wasTargetTyped, type: type);
                        }
                    }
                }
                finally
                {
                    methodGroup.Free();
                }
            }

            // Not a valid delegate creation expression
            else
            {
                diagnostics.Add(ErrorCode.ERR_MethodNameExpected, argument.Syntax.Location);
            }

            // Note that we want failed "new" expression to use the constructors as their symbols.
            var childNodes = BuildArgumentsForErrorRecovery(analyzedArguments);
            return new BoundBadExpression(node, LookupResultKind.OverloadResolutionFailure, StaticCast<Symbol>.From(type.InstanceConstructors), childNodes, type);
        }

        private BoundExpression BindClassCreationExpression(ObjectCreationExpressionSyntax node, NamedTypeSymbol type, string typeName, BindingDiagnosticBag diagnostics, TypeSymbol initializerType = null)
        {
            // Get the bound arguments and the argument names.
            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();
            try
            {
                // new C(__arglist()) is legal
                BindArgumentsAndNames(node.ArgumentList, diagnostics, analyzedArguments, allowArglist: true);

                // No point in performing overload resolution if the type is static or a tuple literal.  
                // Just return a bad expression containing the arguments.
                if (type.IsStatic)
                {
                    diagnostics.Add(ErrorCode.ERR_InstantiatingStaticClass, node.Location, type);
                    return MakeBadExpressionForObjectCreation(node, type, analyzedArguments, diagnostics);
                }
                else if (node.Type.Kind() == SyntaxKind.TupleType)
                {
                    diagnostics.Add(ErrorCode.ERR_NewWithTupleTypeSyntax, node.Type.GetLocation());
                    return MakeBadExpressionForObjectCreation(node, type, analyzedArguments, diagnostics);
                }

                return BindClassCreationExpression(node, typeName, node.Type, type, analyzedArguments, diagnostics, node.Initializer, initializerType);
            }
            finally
            {
                analyzedArguments.Free();
            }
        }

#nullable enable
        /// <summary>
        /// Helper method to create a synthesized constructor invocation.
        /// </summary>
        private BoundExpression MakeConstructorInvocation(
            NamedTypeSymbol type,
            ArrayBuilder<BoundExpression> arguments,
            ArrayBuilder<RefKind> refKinds,
            SyntaxNode node,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(type.TypeKind is TypeKind.Class or TypeKind.Struct);
            var analyzedArguments = AnalyzedArguments.GetInstance();

            try
            {
                analyzedArguments.Arguments.AddRange(arguments);
                analyzedArguments.RefKinds.AddRange(refKinds);

                if (type.IsStatic)
                {
                    diagnostics.Add(ErrorCode.ERR_InstantiatingStaticClass, node.Location, type);
                    return MakeBadExpressionForObjectCreation(node, type, analyzedArguments, initializerOpt: null, typeSyntax: null, diagnostics, wasCompilerGenerated: true);
                }

                var creation = BindClassCreationExpression(node, type.Name, node, type, analyzedArguments, diagnostics);
                creation.WasCompilerGenerated = true;
                return creation;
            }
            finally
            {
                analyzedArguments.Free();
            }
        }

        internal BoundExpression BindObjectCreationForErrorRecovery(BoundUnconvertedObjectCreationExpression node, BindingDiagnosticBag diagnostics)
        {
            var arguments = AnalyzedArguments.GetInstance(node.Arguments, node.ArgumentRefKindsOpt, node.ArgumentNamesOpt);
            var result = MakeBadExpressionForObjectCreation(node.Syntax, CreateErrorType(), arguments, node.InitializerOpt, typeSyntax: node.Syntax, diagnostics);
            arguments.Free();
            return result;
        }

        private BoundExpression MakeBadExpressionForObjectCreation(ObjectCreationExpressionSyntax node, TypeSymbol type, AnalyzedArguments analyzedArguments, BindingDiagnosticBag diagnostics, bool wasCompilerGenerated = false)
        {
            return MakeBadExpressionForObjectCreation(node, type, analyzedArguments, node.Initializer, node.Type, diagnostics, wasCompilerGenerated);
        }

        /// <param name="typeSyntax">Shouldn't be null if <paramref name="initializerOpt"/> is not null.</param>
        private BoundBadExpression MakeBadExpressionForObjectCreation(SyntaxNode node, TypeSymbol type, AnalyzedArguments analyzedArguments, InitializerExpressionSyntax? initializerOpt, SyntaxNode? typeSyntax, BindingDiagnosticBag diagnostics, bool wasCompilerGenerated = false)
        {
            var children = ArrayBuilder<BoundExpression>.GetInstance();
            children.AddRange(BuildArgumentsForErrorRecovery(analyzedArguments));
            if (initializerOpt != null)
            {
                Debug.Assert(typeSyntax is not null);
                var boundInitializer = BindInitializerExpression(syntax: initializerOpt,
                                                                 type: type,
                                                                 typeSyntax: typeSyntax,
                                                                 isForNewInstance: true,
                                                                 diagnostics: diagnostics);
                children.Add(boundInitializer);
            }

            return new BoundBadExpression(node, LookupResultKind.NotCreatable, ImmutableArray.Create<Symbol?>(type), children.ToImmutableAndFree(), type) { WasCompilerGenerated = wasCompilerGenerated };
        }

        private BoundObjectInitializerExpressionBase BindInitializerExpression(
            InitializerExpressionSyntax syntax,
            TypeSymbol type,
            SyntaxNode typeSyntax,
            bool isForNewInstance,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(syntax != null);
            Debug.Assert((object)type != null);

            var implicitReceiver = new BoundObjectOrCollectionValuePlaceholder(typeSyntax, isForNewInstance, type) { WasCompilerGenerated = true };

            switch (syntax.Kind())
            {
                case SyntaxKind.ObjectInitializerExpression:
                    // Uses a special binder to produce customized diagnostics for the object initializer
                    return BindObjectInitializerExpression(
                        syntax, type, diagnostics, implicitReceiver, useObjectInitDiagnostics: true);

                case SyntaxKind.WithInitializerExpression:
                    return BindObjectInitializerExpression(
                        syntax, type, diagnostics, implicitReceiver, useObjectInitDiagnostics: false);

                case SyntaxKind.CollectionInitializerExpression:
                    return BindCollectionInitializerExpression(syntax, type, diagnostics, implicitReceiver);

                default:
                    throw ExceptionUtilities.Unreachable();
            }
        }
#nullable disable

        private BoundExpression BindInitializerExpressionOrValue(
            ExpressionSyntax syntax,
            TypeSymbol type,
            BindValueKind rhsValueKind,
            SyntaxNode typeSyntax,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(syntax != null);
            Debug.Assert((object)type != null);

            switch (syntax.Kind())
            {
                case SyntaxKind.ObjectInitializerExpression:
                case SyntaxKind.CollectionInitializerExpression:
                    Debug.Assert(syntax.Parent.Parent.Kind() != SyntaxKind.WithInitializerExpression);
                    Debug.Assert(rhsValueKind == BindValueKind.RValue);
                    return BindInitializerExpression((InitializerExpressionSyntax)syntax, type, typeSyntax, isForNewInstance: false, diagnostics);
                default:
                    return BindValue(syntax, diagnostics, rhsValueKind);
            }
        }

        private BoundObjectInitializerExpression BindObjectInitializerExpression(
            InitializerExpressionSyntax initializerSyntax,
            TypeSymbol initializerType,
            BindingDiagnosticBag diagnostics,
            BoundObjectOrCollectionValuePlaceholder implicitReceiver,
            bool useObjectInitDiagnostics)
        {
            // SPEC:    7.6.10.2 Object initializers
            //
            // SPEC:    An object initializer consists of a sequence of member initializers, enclosed by { and } tokens and separated by commas.
            // SPEC:    Each member initializer must name an accessible field or property of the object being initialized, followed by an equals sign and
            // SPEC:    an expression or an object initializer or collection initializer.

            Debug.Assert(initializerSyntax.Kind() == SyntaxKind.ObjectInitializerExpression ||
                         initializerSyntax.Kind() == SyntaxKind.WithInitializerExpression);
            Debug.Assert((object)initializerType != null);

            if (initializerSyntax.Kind() == SyntaxKind.ObjectInitializerExpression)
                MessageID.IDS_FeatureObjectInitializer.CheckFeatureAvailability(diagnostics, initializerSyntax.OpenBraceToken);

            // We use a location specific binder for binding object initializer field/property access to generate object initializer specific diagnostics:
            //  1) CS1914 (ERR_StaticMemberInObjectInitializer)
            //  2) CS1917 (ERR_ReadonlyValueTypeInObjectInitializer)
            //  3) CS1918 (ERR_ValueTypePropertyInObjectInitializer)
            // Note that this is only used for the LHS of the assignment - these diagnostics do not apply on the RHS.
            // For this reason, we will actually need two binders: this and this.WithAdditionalFlags.
            var objectInitializerMemberBinder = useObjectInitDiagnostics
                ? this.WithAdditionalFlags(BinderFlags.ObjectInitializerMember)
                : this;

            var initializers = ArrayBuilder<BoundExpression>.GetInstance(initializerSyntax.Expressions.Count);

            // Member name map to report duplicate assignments to a field/property.
            var memberNameMap = PooledHashSet<string>.GetInstance();
            foreach (var memberInitializer in initializerSyntax.Expressions)
            {
                BoundExpression boundMemberInitializer = BindInitializerMemberAssignment(
                    memberInitializer, objectInitializerMemberBinder, diagnostics, implicitReceiver);

                initializers.Add(boundMemberInitializer);

                ReportDuplicateObjectMemberInitializers(boundMemberInitializer, memberNameMap, diagnostics);
            }

            return new BoundObjectInitializerExpression(
                initializerSyntax,
                implicitReceiver,
                initializers.ToImmutableAndFree(),
                initializerType);
        }

        private BoundExpression BindInitializerMemberAssignment(
            ExpressionSyntax memberInitializer,
            Binder objectInitializerMemberBinder,
            BindingDiagnosticBag diagnostics,
            BoundObjectOrCollectionValuePlaceholder implicitReceiver)
        {
            // SPEC:    A member initializer that specifies an expression after the equals sign is processed in the same way as an assignment (spec 7.17.1) to the field or property.

            switch (memberInitializer.Kind())
            {
                case SyntaxKind.SimpleAssignmentExpression:
                    {
                        var initializer = (AssignmentExpressionSyntax)memberInitializer;

                        // We use a location specific binder for binding object initializer field/property access to generate object initializer specific diagnostics:
                        //  1) CS1914 (ERR_StaticMemberInObjectInitializer)
                        //  2) CS1917 (ERR_ReadonlyValueTypeInObjectInitializer)
                        //  3) CS1918 (ERR_ValueTypePropertyInObjectInitializer)
                        // See comments in BindObjectInitializerExpression for more details.

                        Debug.Assert(objectInitializerMemberBinder != null);

                        BoundExpression boundLeft = objectInitializerMemberBinder.BindObjectInitializerMember(initializer, implicitReceiver, diagnostics);

                        if (boundLeft != null)
                        {
                            Debug.Assert((object)boundLeft.Type != null);

                            var rhsExpr = initializer.Right.CheckAndUnwrapRefExpression(diagnostics, out RefKind refKind);
                            bool isRef = refKind == RefKind.Ref;
                            var rhsKind = isRef ? GetRequiredRHSValueKindForRefAssignment(boundLeft) : BindValueKind.RValue;

                            // Bind member initializer value, i.e. right part of assignment
                            BoundExpression boundRight = BindInitializerExpressionOrValue(
                                syntax: rhsExpr,
                                type: boundLeft.Type,
                                rhsKind,
                                typeSyntax: boundLeft.Syntax,
                                diagnostics: diagnostics);

                            // Bind member initializer assignment expression
                            return BindAssignment(initializer, boundLeft, boundRight, isRef, diagnostics);
                        }
                        break;
                    }

                // We fall back on simply binding the name as an expression for proper recovery
                // and also report a diagnostic about a simple identifier being an invalid expression
                // in the object initializer, to indicate to the user that they are missing an assignment
                case SyntaxKind.IdentifierName:
                    {
                        Error(diagnostics, ErrorCode.ERR_InvalidInitializerElementInitializer, memberInitializer);

                        var identifierName = (IdentifierNameSyntax)memberInitializer;
                        Debug.Assert(objectInitializerMemberBinder != null);

                        var boundNode = objectInitializerMemberBinder.BindObjectInitializerMemberMissingAssignment(identifierName, implicitReceiver, diagnostics);

                        var badRight = new BoundBadExpression(
                            identifierName,
                            LookupResultKind.Empty,
                            symbols: [],
                            childBoundNodes: [],
                            type: null,
                            hasErrors: true)
                        {
                            WasCompilerGenerated = true,
                        };
                        boundNode = new BoundAssignmentOperator(
                            identifierName, boundNode, badRight, isRef: false, ErrorTypeSymbol.UnknownResultType, hasErrors: true)
                        {
                            WasCompilerGenerated = true,
                        };

                        return boundNode;
                    }
            }

            var boundExpression = BindValue(memberInitializer, diagnostics, BindValueKind.RValue);
            Error(diagnostics, ErrorCode.ERR_InvalidInitializerElementInitializer, memberInitializer);
            return BindToTypeForErrorRecovery(ToBadExpression(boundExpression, LookupResultKind.NotAValue));
        }

        // returns BadBoundExpression or BoundObjectInitializerMember or BoundDynamicObjectInitializerMember or BoundImplicitIndexerAccess or BoundArrayAccess or BoundPointerElementAccess
        private BoundExpression BindObjectInitializerMember(
            AssignmentExpressionSyntax namedAssignment,
            BoundObjectOrCollectionValuePlaceholder implicitReceiver,
            BindingDiagnosticBag diagnostics)
        {
            var leftSyntax = namedAssignment.Left;
            SyntaxKind rhsKind = namedAssignment.Right.Kind();
            bool isRef = rhsKind is SyntaxKind.RefExpression;
            bool isRhsNestedInitializer = rhsKind is SyntaxKind.ObjectInitializerExpression or SyntaxKind.CollectionInitializerExpression;
            BindValueKind valueKind = isRhsNestedInitializer ? BindValueKind.RValue : (isRef ? BindValueKind.RefAssignable : BindValueKind.Assignable);

            return BindObjectInitializerMemberCommon(
                leftSyntax, implicitReceiver, valueKind, isRhsNestedInitializer, diagnostics);
        }

        // returns BadBoundExpression or BoundObjectInitializerMember or BoundDynamicObjectInitializerMember or BoundImplicitIndexerAccess or BoundArrayAccess or BoundPointerElementAccess
        private BoundExpression BindObjectInitializerMemberMissingAssignment(
            ExpressionSyntax leftSyntax,
            BoundObjectOrCollectionValuePlaceholder implicitReceiver,
            BindingDiagnosticBag diagnostics)
        {
            return BindObjectInitializerMemberCommon(
                leftSyntax, implicitReceiver, BindValueKind.Assignable, false, diagnostics);
        }

        // returns BadBoundExpression or BoundObjectInitializerMember or BoundDynamicObjectInitializerMember or BoundImplicitIndexerAccess or BoundArrayAccess or BoundPointerElementAccess
        private BoundExpression BindObjectInitializerMemberCommon(
            ExpressionSyntax leftSyntax,
            BoundObjectOrCollectionValuePlaceholder implicitReceiver,
            BindValueKind valueKind,
            bool isRhsNestedInitializer,
            BindingDiagnosticBag diagnostics)
        {
            BoundExpression boundMember;
            LookupResultKind resultKind;
            bool hasErrors;

            var initializerType = implicitReceiver.Type;

            if (leftSyntax.Kind() == SyntaxKind.IdentifierName)
            {
                var memberName = (IdentifierNameSyntax)leftSyntax;

                if (initializerType.IsDynamic())
                {
                    // D = { ..., <identifier> = <expr>, ... }, where D : dynamic
                    boundMember = new BoundDynamicObjectInitializerMember(leftSyntax, memberName.Identifier.Text, implicitReceiver.Type, initializerType, hasErrors: false);
                    return CheckValue(boundMember, valueKind, diagnostics);
                }
                else
                {
                    // SPEC:    Each member initializer must name an accessible field or property of the object being initialized, followed by an equals sign and
                    // SPEC:    an expression or an object initializer or collection initializer.
                    // SPEC:    A member initializer that specifies an expression after the equals sign is processed in the same way as an assignment (7.17.1) to the field or property.

                    // SPEC VIOLATION:  Native compiler also allows initialization of field-like events in object initializers, so we allow it as well.

                    boundMember = BindInstanceMemberAccess(
                        node: memberName,
                        right: memberName,
                        boundLeft: implicitReceiver,
                        rightName: memberName.Identifier.ValueText,
                        rightArity: 0,
                        typeArgumentsSyntax: default,
                        typeArgumentsWithAnnotations: default,
                        invoked: false,
                        indexed: false,
                        diagnostics: diagnostics);

                    hasErrors = boundMember.HasAnyErrors || implicitReceiver.HasAnyErrors;

                    if (boundMember.Kind == BoundKind.PropertyGroup)
                    {
                        boundMember = BindIndexedPropertyAccess((BoundPropertyGroup)boundMember, mustHaveAllOptionalParameters: true, diagnostics: diagnostics);
                        if (boundMember.HasAnyErrors)
                        {
                            hasErrors = true;
                        }
                    }
                }

                resultKind = boundMember.ResultKind;
            }
            else if (leftSyntax.Kind() == SyntaxKind.ImplicitElementAccess)
            {
                var implicitIndexing = (ImplicitElementAccessSyntax)leftSyntax;

                MessageID.IDS_FeatureDictionaryInitializer.CheckFeatureAvailability(diagnostics, implicitIndexing.ArgumentList.OpenBracketToken);

                boundMember = BindElementAccess(implicitIndexing, implicitReceiver, implicitIndexing.ArgumentList, allowInlineArrayElementAccess: false, diagnostics);

                resultKind = boundMember.ResultKind;
                hasErrors = boundMember.HasAnyErrors || implicitReceiver.HasAnyErrors;
            }
            else
            {
                return null;
            }

            // SPEC:    A member initializer that specifies an object initializer after the equals sign is a nested object initializer,
            // SPEC:    i.e. an initialization of an embedded object. Instead of assigning a new value to the field or property,
            // SPEC:    the assignments in the nested object initializer are treated as assignments to members of the field or property.
            // SPEC:    Nested object initializers cannot be applied to properties with a value type, or to read-only fields with a value type.

            // NOTE:    The dev11 behavior does not match the spec that was current at the time (quoted above).  However, in the roslyn
            // NOTE:    timeframe, the spec will be updated to apply the same restriction to nested collection initializers.  Therefore,
            // NOTE:    roslyn will implement the dev11 behavior and it will be spec-compliant.

            // NOTE:    In the roslyn timeframe, an additional restriction will (likely) be added to the spec - it is not sufficient for the
            // NOTE:    type of the member to not be a value type - it must actually be a reference type (i.e. unconstrained type parameters
            // NOTE:    should be prohibited).  To avoid breaking existing code, roslyn will not implement this new spec clause.
            // TODO:    If/when we have a way to version warnings, we should add a warning for this.

            BoundKind boundMemberKind = boundMember.Kind;

            ImmutableArray<BoundExpression> arguments = ImmutableArray<BoundExpression>.Empty;
            ImmutableArray<string> argumentNamesOpt = default;
            ImmutableArray<int> argsToParamsOpt = default;
            ImmutableArray<RefKind> argumentRefKindsOpt = default;
            BitVector defaultArguments = default;
            bool expanded = false;
            AccessorKind accessorKind = AccessorKind.Unknown;

            switch (boundMemberKind)
            {
                case BoundKind.FieldAccess:
                    {
                        var fieldSymbol = ((BoundFieldAccess)boundMember).FieldSymbol;
                        if (isRhsNestedInitializer && fieldSymbol.IsReadOnly && fieldSymbol.Type.IsValueType)
                        {
                            if (!hasErrors)
                            {
                                // TODO: distinct error code for collection initializers?  (Dev11 doesn't have one.)
                                Error(diagnostics, ErrorCode.ERR_ReadonlyValueTypeInObjectInitializer, leftSyntax, fieldSymbol, fieldSymbol.Type);
                                hasErrors = true;
                            }

                            resultKind = LookupResultKind.NotAValue;
                        }
                        break;
                    }

                case BoundKind.EventAccess:
                    break;

                case BoundKind.PropertyAccess:
                    hasErrors |= isRhsNestedInitializer && !CheckNestedObjectInitializerPropertySymbol(((BoundPropertyAccess)boundMember).PropertySymbol, leftSyntax, diagnostics, hasErrors, ref resultKind);
                    break;

                case BoundKind.IndexerAccess:
                    {
                        var indexer = BindIndexerDefaultArgumentsAndParamsCollection((BoundIndexerAccess)boundMember, valueKind, diagnostics);
                        boundMember = indexer;
                        hasErrors |= isRhsNestedInitializer && !CheckNestedObjectInitializerPropertySymbol(indexer.Indexer, leftSyntax, diagnostics, hasErrors, ref resultKind);
                        arguments = indexer.Arguments;
                        argumentNamesOpt = indexer.ArgumentNamesOpt;
                        argsToParamsOpt = indexer.ArgsToParamsOpt;
                        argumentRefKindsOpt = indexer.ArgumentRefKindsOpt;
                        defaultArguments = indexer.DefaultArguments;
                        expanded = indexer.Expanded;
                        accessorKind = indexer.AccessorKind;

                        // If any of the arguments is an interpolated string handler that takes the receiver as an argument for creation,
                        // we disallow this. During lowering, indexer arguments are evaluated before the receiver for this scenario, and
                        // we therefore can't get the receiver at the point it will be needed for the constructor. We could technically
                        // support it for top-level member indexer initializers (ie, initializers directly on the `new Type` instance),
                        // but for user and language simplicity we blanket forbid this.
                        foreach (var argument in arguments)
                        {
                            if (argument is BoundConversion { Conversion.IsInterpolatedStringHandler: true, Operand: var operand })
                            {
                                var handlerPlaceholders = operand.GetInterpolatedStringHandlerData().ArgumentPlaceholders;
                                if (handlerPlaceholders.Any(static placeholder => placeholder.ArgumentIndex is BoundInterpolatedStringArgumentPlaceholder.InstanceParameter or BoundInterpolatedStringArgumentPlaceholder.ExtensionReceiver))
                                {
                                    diagnostics.Add(ErrorCode.ERR_InterpolatedStringsReferencingInstanceCannotBeInObjectInitializers, argument.Syntax.Location);
                                    hasErrors = true;
                                }
                            }
                        }

                        break;
                    }

                case BoundKind.ImplicitIndexerAccess:
                    var implicitIndexer = (BoundImplicitIndexerAccess)boundMember;
                    MessageID.IDS_FeatureImplicitIndexerInitializer.CheckFeatureAvailability(diagnostics, implicitIndexer.Syntax);

                    if (isRhsNestedInitializer && GetPropertySymbol(implicitIndexer, out _, out _) is { } property)
                    {
                        hasErrors |= !CheckNestedObjectInitializerPropertySymbol(property, leftSyntax, diagnostics, hasErrors, ref resultKind);
                    }

                    return hasErrors ? boundMember : CheckValue(boundMember, valueKind, diagnostics);

                case BoundKind.DynamicObjectInitializerMember:
                    break;

                case BoundKind.DynamicIndexerAccess:
                    {
                        var indexer = (BoundDynamicIndexerAccess)boundMember;
                        arguments = indexer.Arguments;
                        argumentNamesOpt = indexer.ArgumentNamesOpt;
                        argumentRefKindsOpt = indexer.ArgumentRefKindsOpt;
                    }

                    break;

                case BoundKind.ArrayAccess:
                case BoundKind.PointerElementAccess:
                    return CheckValue(boundMember, valueKind, diagnostics);

                default:
                    return BadObjectInitializerMemberAccess(boundMember, implicitReceiver, leftSyntax, diagnostics, valueKind, hasErrors);
            }

            if (!hasErrors)
            {
                // CheckValueKind to generate possible diagnostics for invalid initializers non-viable member lookup result:
                //      1) CS0154 (ERR_PropertyLacksGet)
                //      2) CS0200 (ERR_AssgReadonlyProp)
                if (!CheckValueKind(boundMember.Syntax, boundMember, valueKind, checkingReceiver: false, diagnostics: diagnostics))
                {
                    hasErrors = true;
                    resultKind = isRhsNestedInitializer ? LookupResultKind.NotAValue : LookupResultKind.NotAVariable;
                }
            }

            return new BoundObjectInitializerMember(
                leftSyntax,
                boundMember.ExpressionSymbol,
                arguments,
                argumentNamesOpt,
                argumentRefKindsOpt,
                expanded,
                argsToParamsOpt,
                defaultArguments,
                resultKind,
                accessorKind,
                implicitReceiver.Type,
                type: boundMember.Type,
                hasErrors: hasErrors);
        }

        private static bool CheckNestedObjectInitializerPropertySymbol(
            PropertySymbol propertySymbol,
            ExpressionSyntax memberNameSyntax,
            BindingDiagnosticBag diagnostics,
            bool suppressErrors,
            ref LookupResultKind resultKind)
        {
            bool hasErrors = false;
            if (propertySymbol.Type.IsValueType)
            {
                if (!suppressErrors)
                {
                    // TODO: distinct error code for collection initializers?  (Dev11 doesn't have one.)
                    Error(diagnostics, ErrorCode.ERR_ValueTypePropertyInObjectInitializer, memberNameSyntax, propertySymbol, propertySymbol.Type);
                    hasErrors = true;
                }

                resultKind = LookupResultKind.NotAValue;
            }

            return !hasErrors;
        }

        private BoundExpression BadObjectInitializerMemberAccess(
            BoundExpression boundMember,
            BoundObjectOrCollectionValuePlaceholder implicitReceiver,
            ExpressionSyntax memberNameSyntax,
            BindingDiagnosticBag diagnostics,
            BindValueKind valueKind,
            bool suppressErrors)
        {
            Debug.Assert(!boundMember.NeedsToBeConverted());
            if (!suppressErrors)
            {
                string member;
                var identName = memberNameSyntax as IdentifierNameSyntax;
                if (identName != null)
                {
                    member = identName.Identifier.ValueText;
                }
                else
                {
                    member = memberNameSyntax.ToString();
                }

                switch (boundMember.ResultKind)
                {
                    case LookupResultKind.Empty:
                        Error(diagnostics, ErrorCode.ERR_NoSuchMember, memberNameSyntax, implicitReceiver.Type, member);
                        break;

                    case LookupResultKind.Inaccessible:
                        boundMember = CheckValue(boundMember, valueKind, diagnostics);
                        Debug.Assert(boundMember.HasAnyErrors);
                        break;

                    default:
                        Error(diagnostics, ErrorCode.ERR_MemberCannotBeInitialized, memberNameSyntax, member);
                        break;
                }
            }

            return ToBadExpression(boundMember, (valueKind == BindValueKind.RValue) ? LookupResultKind.NotAValue : LookupResultKind.NotAVariable);
        }

        private static void ReportDuplicateObjectMemberInitializers(BoundExpression boundMemberInitializer, HashSet<string> memberNameMap, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(memberNameMap != null);

            // SPEC:    It is an error for an object initializer to include more than one member initializer for the same field or property.

            if (!boundMemberInitializer.HasAnyErrors)
            {
                // SPEC:    A member initializer that specifies an expression after the equals sign is processed in the same way as an assignment (7.17.1) to the field or property.

                var memberInitializerSyntax = boundMemberInitializer.Syntax;

                Debug.Assert(memberInitializerSyntax.Kind() == SyntaxKind.SimpleAssignmentExpression);
                var namedAssignment = (AssignmentExpressionSyntax)memberInitializerSyntax;

                var memberNameSyntax = namedAssignment.Left as IdentifierNameSyntax;
                if (memberNameSyntax != null)
                {
                    var memberName = memberNameSyntax.Identifier.ValueText;

                    if (!memberNameMap.Add(memberName))
                    {
                        Error(diagnostics, ErrorCode.ERR_MemberAlreadyInitialized, memberNameSyntax, memberName);
                    }
                }
            }
        }

#nullable enable
        private static ImmutableSegmentedDictionary<string, Symbol> GetMembersRequiringInitialization(MethodSymbol constructor)
        {
            if (!constructor.ShouldCheckRequiredMembers() ||
                constructor.ContainingType.HasRequiredMembersError) // An error will be reported on the constructor if from source, or a use-site diagnostic will be reported on the use if from metadata.
            {
                return ImmutableSegmentedDictionary<string, Symbol>.Empty;
            }

            return constructor.ContainingType.AllRequiredMembers;
        }

        internal static void CheckRequiredMembersInObjectInitializer(
            MethodSymbol constructor,
            ImmutableArray<BoundExpression> initializers,
            SyntaxNode creationSyntax,
            BindingDiagnosticBag diagnostics)
        {
            ImmutableSegmentedDictionary<string, Symbol> requiredMembers = GetMembersRequiringInitialization(constructor);

            if (requiredMembers.Count == 0)
            {
                return;
            }

            var requiredMembersBuilder = requiredMembers.ToBuilder();

            if (initializers.IsDefaultOrEmpty)
            {
                ReportMembersRequiringInitialization(creationSyntax, requiredMembersBuilder, diagnostics);
                return;
            }

            foreach (var initializer in initializers)
            {
                if (initializer is not BoundAssignmentOperator assignmentOperator)
                {
                    continue;
                }

                var memberSymbol = assignmentOperator.Left switch
                {
                    // Regular initializers
                    BoundObjectInitializerMember member => member.MemberSymbol,
                    // Attribute initializers
                    BoundPropertyAccess propertyAccess => propertyAccess.PropertySymbol,
                    BoundFieldAccess fieldAccess => fieldAccess.FieldSymbol,
                    // Error cases
                    _ => null
                };

                if (memberSymbol is null)
                {
                    continue;
                }

                if (!requiredMembersBuilder.TryGetValue(memberSymbol.Name, out var requiredMember))
                {
                    continue;
                }

                if (!memberSymbol.Equals(requiredMember, TypeCompareKind.ConsiderEverything))
                {
                    continue;
                }

                requiredMembersBuilder.Remove(memberSymbol.Name);

                if (assignmentOperator.Right is BoundObjectInitializerExpressionBase initializerExpression)
                {
                    // Required member '{0}' must be assigned a value, it cannot use a nested member or collection initializer.
                    diagnostics.Add(ErrorCode.ERR_RequiredMembersMustBeAssignedValue, initializerExpression.Syntax.Location, requiredMember);
                }
            }

            ReportMembersRequiringInitialization(creationSyntax, requiredMembersBuilder, diagnostics);
        }

        private static void ReportMembersRequiringInitialization(SyntaxNode creationSyntax, ImmutableSegmentedDictionary<string, Symbol>.Builder requiredMembersBuilder, BindingDiagnosticBag diagnostics)
        {
            if (requiredMembersBuilder.Count == 0)
            {
                // Avoid Location allocation.
                return;
            }

            Location location = creationSyntax switch
            {
                ObjectCreationExpressionSyntax { Type: { } type } => type.Location,
                BaseObjectCreationExpressionSyntax { NewKeyword: { } newKeyword } => newKeyword.GetLocation(),
                AttributeSyntax { Name: { } name } => name.Location,
                _ => creationSyntax.Location
            };

            foreach (var (_, member) in requiredMembersBuilder)
            {
                // Required member '{0}' must be set in the object initializer or attribute constructor.
                diagnostics.Add(ErrorCode.ERR_RequiredMemberMustBeSet, location, member);
            }
        }
#nullable disable

        private BoundCollectionInitializerExpression BindCollectionInitializerExpression(
            InitializerExpressionSyntax initializerSyntax,
            TypeSymbol initializerType,
            BindingDiagnosticBag diagnostics,
            BoundObjectOrCollectionValuePlaceholder implicitReceiver)
        {
            // SPEC:    7.6.10.3 Collection initializers
            //
            // SPEC:    A collection initializer consists of a sequence of element initializers, enclosed by { and } tokens and separated by commas.
            // SPEC:    The following is an example of an object creation expression that includes a collection initializer:
            // SPEC:        List<int> digits = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            // SPEC:    The collection object to which a collection initializer is applied must be of a type that implements System.Collections.IEnumerable or
            // SPEC:    a compile-time error occurs. For each specified element in order, the collection initializer invokes an Add method on the target object
            // SPEC:    with the expression list of the element initializer as argument list, applying normal overload resolution for each invocation.
            // SPEC:    Thus, the collection object must contain an applicable Add method for each element initializer.

            Debug.Assert(initializerSyntax.Kind() == SyntaxKind.CollectionInitializerExpression);
            Debug.Assert(initializerSyntax.Expressions.Any());
            Debug.Assert((object)initializerType != null);

            MessageID.IDS_FeatureCollectionInitializer.CheckFeatureAvailability(diagnostics, initializerSyntax.OpenBraceToken);

            var initializerBuilder = ArrayBuilder<BoundExpression>.GetInstance();

            // SPEC:    The collection object to which a collection initializer is applied must be of a type that implements System.Collections.IEnumerable or
            // SPEC:    a compile-time error occurs.

            bool hasEnumerableInitializerType = CollectionInitializerTypeImplementsIEnumerable(initializerType, initializerSyntax, diagnostics);
            if (!hasEnumerableInitializerType && !initializerSyntax.HasErrors && !initializerType.IsErrorType())
            {
                Error(diagnostics, ErrorCode.ERR_CollectionInitRequiresIEnumerable, initializerSyntax, initializerType);
            }

            // We use a location specific binder for binding collection initializer Add method to generate specific overload resolution diagnostics:
            //  1) CS1921 (ERR_InitializerAddHasWrongSignature)
            //  2) CS1950 (ERR_BadArgTypesForCollectionAdd)
            //  3) CS1954 (ERR_InitializerAddHasParamModifiers)
            var collectionInitializerAddMethodBinder = this.WithAdditionalFlags(BinderFlags.CollectionInitializerAddMethod);

            foreach (var elementInitializer in initializerSyntax.Expressions)
            {
                // NOTE:    collectionInitializerAddMethodBinder is used only for binding the Add method invocation expression, but not the entire initializer.
                // NOTE:    Hence it is being passed as a parameter to BindCollectionInitializerElement().
                // NOTE:    Ideally we would want to avoid this and bind the entire initializer with the collectionInitializerAddMethodBinder.
                // NOTE:    However, this approach has few issues. These issues also occur when binding object initializer member assignment.
                // NOTE:    See comments for objectInitializerMemberBinder in BindObjectInitializerExpression method for details about the pitfalls of alternate approaches.

                BoundExpression boundElementInitializer = BindCollectionInitializerElement(elementInitializer, initializerType,
                    hasEnumerableInitializerType, collectionInitializerAddMethodBinder, diagnostics, implicitReceiver);

                initializerBuilder.Add(boundElementInitializer);
            }

            return new BoundCollectionInitializerExpression(initializerSyntax, implicitReceiver, initializerBuilder.ToImmutableAndFree(), initializerType);
        }

        private bool CollectionInitializerTypeImplementsIEnumerable(TypeSymbol initializerType, CSharpSyntaxNode node, BindingDiagnosticBag diagnostics)
        {
            // SPEC:    The collection object to which a collection initializer is applied must be of a type that implements System.Collections.IEnumerable or
            // SPEC:    a compile-time error occurs.

            if (initializerType.IsDynamic())
            {
                // We cannot determine at compile time if initializerType implements System.Collections.IEnumerable, we must assume that it does.
                return true;
            }
            else if (!initializerType.IsErrorType())
            {
                NamedTypeSymbol collectionsIEnumerableType = this.GetSpecialType(SpecialType.System_Collections_IEnumerable, diagnostics, node);

                // NOTE:    Ideally, to check if the initializer type implements System.Collections.IEnumerable we can walk through
                // NOTE:    its implemented interfaces. However the native compiler checks to see if there is conversion from initializer
                // NOTE:    type to the predefined System.Collections.IEnumerable type, so we do the same.

                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                var result = Conversions.HasImplicitConversionToOrImplementsVarianceCompatibleInterface(initializerType, collectionsIEnumerableType, ref useSiteInfo, out bool needSupportForRefStructInterfaces);
                diagnostics.Add(node, useSiteInfo);

                if (needSupportForRefStructInterfaces &&
                    initializerType.ContainingModule != Compilation.SourceModule)
                {
                    CheckFeatureAvailability(node, MessageID.IDS_FeatureRefStructInterfaces, diagnostics);
                }

                return result;
            }
            else
            {
                return false;
            }
        }

        private BoundExpression BindCollectionInitializerElement(
            ExpressionSyntax elementInitializer,
            TypeSymbol initializerType,
            bool hasEnumerableInitializerType,
            Binder collectionInitializerAddMethodBinder,
            BindingDiagnosticBag diagnostics,
            BoundObjectOrCollectionValuePlaceholder implicitReceiver)
        {
            // SPEC:    Each element initializer specifies an element to be added to the collection object being initialized, and consists of
            // SPEC:    a list of expressions enclosed by { and } tokens and separated by commas.
            // SPEC:    A single-expression element initializer can be written without braces, but cannot then be an assignment expression,
            // SPEC:    to avoid ambiguity with member initializers. The non-assignment-expression production is defined in 7.18.

            if (elementInitializer.Kind() == SyntaxKind.ComplexElementInitializerExpression)
            {
                return BindComplexElementInitializerExpression(
                    (InitializerExpressionSyntax)elementInitializer,
                    diagnostics,
                    hasEnumerableInitializerType,
                    collectionInitializerAddMethodBinder,
                    implicitReceiver);
            }
            else
            {
                // Must be a non-assignment expression.
                if (SyntaxFacts.IsAssignmentExpression(elementInitializer.Kind()))
                {
                    Error(diagnostics, ErrorCode.ERR_InvalidInitializerElementInitializer, elementInitializer);
                }

                var boundElementInitializer = BindInitializerExpressionOrValue(elementInitializer, initializerType, BindValueKind.RValue, implicitReceiver.Syntax, diagnostics);

                BoundExpression result = BindCollectionInitializerElementAddMethod(
                    elementInitializer,
                    ImmutableArray.Create(boundElementInitializer),
                    hasEnumerableInitializerType,
                    collectionInitializerAddMethodBinder,
                    diagnostics,
                    implicitReceiver);

                result.WasCompilerGenerated = true;
                return result;
            }
        }

        private BoundExpression BindComplexElementInitializerExpression(
            InitializerExpressionSyntax elementInitializer,
            BindingDiagnosticBag diagnostics,
            bool hasEnumerableInitializerType,
            Binder collectionInitializerAddMethodBinder = null,
            BoundObjectOrCollectionValuePlaceholder implicitReceiver = null)
        {
            var elementInitializerExpressions = elementInitializer.Expressions;

            if (elementInitializerExpressions.Any())
            {
                var exprBuilder = ArrayBuilder<BoundExpression>.GetInstance();
                foreach (var childElementInitializer in elementInitializerExpressions)
                {
                    exprBuilder.Add(BindValue(childElementInitializer, diagnostics, BindValueKind.RValue));
                }

                return BindCollectionInitializerElementAddMethod(
                    elementInitializer,
                    exprBuilder.ToImmutableAndFree(),
                    hasEnumerableInitializerType,
                    collectionInitializerAddMethodBinder,
                    diagnostics,
                    implicitReceiver);
            }
            else
            {
                Error(diagnostics, ErrorCode.ERR_EmptyElementInitializer, elementInitializer);
                return BadExpression(elementInitializer, LookupResultKind.NotInvocable);
            }
        }

        private BoundExpression BindUnexpectedComplexElementInitializer(InitializerExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(node.Kind() == SyntaxKind.ComplexElementInitializerExpression);

            return BindComplexElementInitializerExpression(node, diagnostics, hasEnumerableInitializerType: false);
        }

        private BoundExpression BindCollectionInitializerElementAddMethod(
            SyntaxNode elementInitializer,
            ImmutableArray<BoundExpression> boundElementInitializerExpressions,
            bool hasEnumerableInitializerType,
            Binder collectionInitializerAddMethodBinder,
            BindingDiagnosticBag diagnostics,
            BoundObjectOrCollectionValuePlaceholder implicitReceiver)
        {
            //
            // !!! ATTENTION !!!
            //
            // In terms of errors relevant for HasCollectionExpressionApplicableAddMethod check
            // this function should be kept in sync with local function
            // HasCollectionExpressionApplicableAddMethod.bindCollectionInitializerElementAddMethod
            //

            // SPEC:    For each specified element in order, the collection initializer invokes an Add method on the target object
            // SPEC:    with the expression list of the element initializer as argument list, applying normal overload resolution for each invocation.
            // SPEC:    Thus, the collection object must contain an applicable Add method for each element initializer.

            // We use a location specific binder for binding collection initializer Add method to generate specific overload resolution diagnostics.
            //  1) CS1921 (ERR_InitializerAddHasWrongSignature)
            //  2) CS1950 (ERR_BadArgTypesForCollectionAdd)
            //  3) CS1954 (ERR_InitializerAddHasParamModifiers)
            // See comments in BindCollectionInitializerExpression for more details.

            Debug.Assert(!boundElementInitializerExpressions.IsEmpty);

            if (!hasEnumerableInitializerType)
            {
                return BadExpression(elementInitializer, LookupResultKind.NotInvocable, ImmutableArray<Symbol>.Empty, boundElementInitializerExpressions);
            }

            var result = bindCollectionInitializerElementAddMethod(elementInitializer, boundElementInitializerExpressions, collectionInitializerAddMethodBinder, diagnostics, implicitReceiver);

#if DEBUG
            if (!result.HasErrors &&
                boundElementInitializerExpressions.Length == 1 &&
                boundElementInitializerExpressions[0] is not
                    ({ Type: null } or BoundLiteral or BoundUnconvertedInterpolatedString or BoundBinaryOperator { IsUnconvertedInterpolatedStringAddition: true }) &&
                !implicitReceiver.Type.IsDynamic())
            {
                var d = BindingDiagnosticBag.GetInstance();

                // This assert provides some validation that, if the real invocation binding succeeds, then the HasCollectionExpressionApplicableAddMethod helper succeeds as well.
                Debug.Assert(collectionInitializerAddMethodBinder.HasCollectionExpressionApplicableAddMethod(elementInitializer, implicitReceiver.Type, addMethods: out _, d));

                d.Free();
            }
#endif 
            return result;

            BoundExpression bindCollectionInitializerElementAddMethod(
                SyntaxNode elementInitializer,
                ImmutableArray<BoundExpression> boundElementInitializerExpressions,
                Binder collectionInitializerAddMethodBinder,
                BindingDiagnosticBag diagnostics,
                BoundObjectOrCollectionValuePlaceholder implicitReceiver)
            {
                Debug.Assert(collectionInitializerAddMethodBinder != null);
                Debug.Assert(collectionInitializerAddMethodBinder.Flags.Includes(BinderFlags.CollectionInitializerAddMethod));
                Debug.Assert(implicitReceiver != null);
                Debug.Assert((object)implicitReceiver.Type != null);

                if (implicitReceiver.Type.IsDynamic())
                {
                    var hasErrors = ReportBadDynamicArguments(elementInitializer, implicitReceiver, boundElementInitializerExpressions, refKinds: default, diagnostics, queryClause: null);

                    return new BoundDynamicCollectionElementInitializer(
                        elementInitializer,
                        applicableMethods: ImmutableArray<MethodSymbol>.Empty,
                        implicitReceiver,
                        arguments: boundElementInitializerExpressions.SelectAsArray(e => BindToNaturalType(e, diagnostics)),
                        type: GetSpecialType(SpecialType.System_Void, diagnostics, elementInitializer),
                        hasErrors: hasErrors);
                }

                // Receiver is early bound, find method Add and invoke it (may still be a dynamic invocation):

                var addMethodDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: diagnostics.AccumulatesDependencies);
                var addMethodInvocation = collectionInitializerAddMethodBinder.MakeInvocationExpression(
                    elementInitializer,
                    implicitReceiver,
                    methodName: WellKnownMemberNames.CollectionInitializerAddMethodName,
                    args: boundElementInitializerExpressions,
                    diagnostics: addMethodDiagnostics);
                copyRelevantAddMethodDiagnostics(addMethodDiagnostics, diagnostics);

                if (addMethodInvocation.Kind == BoundKind.DynamicInvocation)
                {
                    var dynamicInvocation = (BoundDynamicInvocation)addMethodInvocation;
                    return new BoundDynamicCollectionElementInitializer(
                        elementInitializer,
                        dynamicInvocation.ApplicableMethods,
                        implicitReceiver,
                        dynamicInvocation.Arguments,
                        dynamicInvocation.Type,
                        hasErrors: dynamicInvocation.HasAnyErrors);
                }
                else if (addMethodInvocation.Kind == BoundKind.Call)
                {
                    var boundCall = (BoundCall)addMethodInvocation;

                    // Either overload resolution succeeded for this call or it did not. If it
                    // did not succeed then we've stashed the original method symbols from the
                    // method group, and we should use those as the symbols displayed for the
                    // call. If it did succeed then we did not stash any symbols.
                    if (boundCall.HasErrors && !boundCall.OriginalMethodsOpt.IsDefault)
                    {
                        return boundCall;
                    }

                    return new BoundCollectionElementInitializer(
                        elementInitializer,
                        boundCall.Method,
                        boundCall.Arguments,
                        boundCall.ReceiverOpt,
                        boundCall.Expanded,
                        boundCall.ArgsToParamsOpt,
                        boundCall.DefaultArguments,
                        boundCall.InvokedAsExtensionMethod,
                        boundCall.ResultKind,
                        boundCall.Type,
                        boundCall.HasAnyErrors)
                    { WasCompilerGenerated = true };
                }
                else
                {
                    Debug.Assert(addMethodInvocation.Kind == BoundKind.BadExpression);
                    return addMethodInvocation;
                }
            }

            static void copyRelevantAddMethodDiagnostics(BindingDiagnosticBag source, BindingDiagnosticBag target)
            {
                target.AddDependencies(source);

                if (source.DiagnosticBag is { IsEmptyWithoutResolution: false } bag)
                {
                    foreach (var diagnostic in bag.AsEnumerableWithoutResolution())
                    {
                        // Filter diagnostics that cannot be fixed since one cannot use ref modifiers in collection initializers.
                        if (!((ErrorCode)diagnostic.Code is ErrorCode.WRN_ArgExpectedRefOrIn or ErrorCode.WRN_ArgExpectedIn))
                        {
                            target.Add(diagnostic);
                        }
                    }
                }

                source.Free();
            }
        }

#nullable enable
        private BoundCollectionExpressionSpreadElement BindCollectionExpressionSpreadElementAddMethod(
            SpreadElementSyntax syntax,
            BoundCollectionExpressionSpreadElement element,
            Binder collectionInitializerAddMethodBinder,
            BoundObjectOrCollectionValuePlaceholder implicitReceiver,
            BindingDiagnosticBag diagnostics)
        {
            var enumeratorInfo = element.EnumeratorInfoOpt;
            if (enumeratorInfo is null)
            {
                return element.Update(
                    BindToNaturalType(element.Expression, BindingDiagnosticBag.Discarded, reportNoTargetType: false),
                    expressionPlaceholder: element.ExpressionPlaceholder,
                    conversion: null,
                    enumeratorInfo,
                    lengthOrCount: null,
                    elementPlaceholder: null,
                    iteratorBody: null);
            }

            Debug.Assert(enumeratorInfo.ElementType is { }); // ElementType is set always, even for IEnumerable.
            var addElementPlaceholder = new BoundValuePlaceholder(syntax, enumeratorInfo.ElementType);
            var addMethodInvocation = BindCollectionInitializerElementAddMethod(
                syntax.Expression,
                ImmutableArray.Create((BoundExpression)addElementPlaceholder),
                hasEnumerableInitializerType: true,
                collectionInitializerAddMethodBinder,
                diagnostics,
                implicitReceiver);
            return element.Update(
                element.Expression,
                expressionPlaceholder: element.ExpressionPlaceholder,
                conversion: element.Conversion,
                enumeratorInfo,
                lengthOrCount: element.LengthOrCount,
                elementPlaceholder: addElementPlaceholder,
                iteratorBody: new BoundExpressionStatement(syntax, addMethodInvocation) { WasCompilerGenerated = true });
        }
#nullable disable

        internal ImmutableArray<MethodSymbol> FilterInaccessibleConstructors(ImmutableArray<MethodSymbol> constructors, bool allowProtectedConstructorsOfBaseType, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            ArrayBuilder<MethodSymbol> builder = null;

            for (int i = 0; i < constructors.Length; i++)
            {
                MethodSymbol constructor = constructors[i];

                if (!IsConstructorAccessible(constructor, ref useSiteInfo, allowProtectedConstructorsOfBaseType))
                {
                    if (builder == null)
                    {
                        builder = ArrayBuilder<MethodSymbol>.GetInstance();
                        builder.AddRange(constructors, i);
                    }
                }
                else
                {
                    builder?.Add(constructor);
                }
            }

            return builder == null ? constructors : builder.ToImmutableAndFree();
        }

        private bool IsConstructorAccessible(MethodSymbol constructor, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo, bool allowProtectedConstructorsOfBaseType = false)
        {
            Debug.Assert((object)constructor != null);
            Debug.Assert(constructor.MethodKind == MethodKind.Constructor || constructor.MethodKind == MethodKind.StaticConstructor);

            NamedTypeSymbol containingType = this.ContainingType;
            if ((object)containingType != null)
            {
                // SPEC VIOLATION: The specification implies that when considering 
                // SPEC VIOLATION: instance methods or instance constructors, we first 
                // SPEC VIOLATION: do overload resolution on the accessible members, and 
                // SPEC VIOLATION: then if the best method chosen is protected and accessed 
                // SPEC VIOLATION: through the wrong type, then an error occurs. The native 
                // SPEC VIOLATION: compiler however does it in the opposite order. First it
                // SPEC VIOLATION: filters out the protected methods that cannot be called
                // SPEC VIOLATION: through the given type, and then it does overload resolution
                // SPEC VIOLATION: on the rest.
                // 
                // That said, it is somewhat odd that the same rule applies to constructors
                // as instance methods. A protected constructor is never going to be called
                // via an instance of a *more derived but different class* the way a 
                // virtual method might be. Nevertheless, that's what we do.
                //
                // A constructor is accessed through an instance of the type being constructed:
                return allowProtectedConstructorsOfBaseType ?
                    this.IsAccessible(constructor, ref useSiteInfo, null) :
                    this.IsSymbolAccessibleConditional(constructor, containingType, ref useSiteInfo, constructor.ContainingType);
            }
            else
            {
                Debug.Assert((object)this.Compilation.Assembly != null);
                return IsSymbolAccessibleConditional(constructor, this.Compilation.Assembly, ref useSiteInfo);
            }
        }

        protected BoundExpression BindClassCreationExpression(
            SyntaxNode node,
            string typeName,
            SyntaxNode typeNode,
            NamedTypeSymbol type,
            AnalyzedArguments analyzedArguments,
            BindingDiagnosticBag diagnostics,
            InitializerExpressionSyntax initializerSyntaxOpt = null,
            TypeSymbol initializerTypeOpt = null,
            bool wasTargetTyped = false)
        {
            //
            // !!! ATTENTION !!!
            //
            // In terms of errors relevant for HasCollectionExpressionApplicableConstructor check
            // this function should be kept in sync with HasCollectionExpressionApplicableConstructor.
            //

            BoundExpression result = null;
            bool hasErrors = type.IsErrorType();
            if (type.IsAbstract)
            {
                // Report error for new of abstract type.
                diagnostics.Add(ErrorCode.ERR_NoNewAbstract, node.Location, type);
                hasErrors = true;
            }

            // If we have a dynamic argument then do overload resolution to see if there are one or more
            // applicable candidates. If there are, then this is a dynamic object creation; we'll work out
            // which ctor to call at runtime. If we have a dynamic argument but no applicable candidates
            // then we do the analysis again for error reporting purposes.

            if (analyzedArguments.HasDynamicArgument)
            {
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                OverloadResolutionResult<MethodSymbol> overloadResolutionResult = OverloadResolutionResult<MethodSymbol>.GetInstance();
                ImmutableArray<MethodSymbol> accessibleConstructors = GetAccessibleConstructorsForOverloadResolution(type, ref useSiteInfo);
                this.OverloadResolution.ObjectCreationOverloadResolution(accessibleConstructors, analyzedArguments, overloadResolutionResult, dynamicResolution: true, isEarlyAttributeBinding: IsEarlyAttributeBinder, ref useSiteInfo);

                if (overloadResolutionResult.HasAnyApplicableMember)
                {
                    var finalApplicableCandidates = GetCandidatesPassingFinalValidation(node, overloadResolutionResult, receiverOpt: null, default(ImmutableArray<TypeWithAnnotations>), isExtensionMethodGroup: false, diagnostics);

                    if (finalApplicableCandidates.Length == 1)
                    {
                        Debug.Assert(finalApplicableCandidates[0].IsApplicable);
                        ReportMemberNotSupportedByDynamicDispatch(node, finalApplicableCandidates[0], diagnostics);
                    }

                    var argArray = BuildArgumentsForDynamicInvocation(analyzedArguments, diagnostics);
                    var refKindsArray = analyzedArguments.RefKinds.ToImmutableOrNull();

                    hasErrors &= ReportBadDynamicArguments(node, receiver: null, argArray, refKindsArray, diagnostics, queryClause: null);

                    BoundObjectInitializerExpressionBase boundInitializerOpt;
                    boundInitializerOpt = MakeBoundInitializerOpt(typeNode, type, initializerSyntaxOpt, initializerTypeOpt, diagnostics);
                    result = new BoundDynamicObjectCreationExpression(
                        node,
                        typeName,
                        argArray,
                        analyzedArguments.GetNames(),
                        refKindsArray,
                        boundInitializerOpt,
                        overloadResolutionResult.GetAllApplicableMembers(),
                        wasTargetTyped,
                        type,
                        hasErrors);

                    diagnostics.Add(node, useSiteInfo);
                }

                overloadResolutionResult.Free();
                if (result != null)
                {
                    return result;
                }
            }

            if (TryPerformConstructorOverloadResolution(
                    type,
                    analyzedArguments,
                    typeName,
                    typeNode.Location,
                    hasErrors, //don't cascade in these cases
                    diagnostics,
                    out MemberResolutionResult<MethodSymbol> memberResolutionResult,
                    out ImmutableArray<MethodSymbol> candidateConstructors,
                    allowProtectedConstructorsOfBaseType: false,
                    out CompoundUseSiteInfo<AssemblySymbol> overloadResolutionUseSiteInfo) &&
                !type.IsAbstract)
            {
                return BindClassCreationExpressionContinued(node, typeNode, type, analyzedArguments, initializerSyntaxOpt, initializerTypeOpt, wasTargetTyped, memberResolutionResult, candidateConstructors, in overloadResolutionUseSiteInfo, diagnostics);
            }

            return CreateBadClassCreationExpression(node, typeNode, type, analyzedArguments, initializerSyntaxOpt, initializerTypeOpt, memberResolutionResult, candidateConstructors, in overloadResolutionUseSiteInfo, diagnostics);
        }

        private BoundObjectCreationExpression BindClassCreationExpressionContinued(
            SyntaxNode node,
            SyntaxNode typeNode,
            NamedTypeSymbol type,
            AnalyzedArguments analyzedArguments,
            InitializerExpressionSyntax initializerSyntaxOpt,
            TypeSymbol initializerTypeOpt,
            bool wasTargetTyped,
            MemberResolutionResult<MethodSymbol> memberResolutionResult,
            ImmutableArray<MethodSymbol> candidateConstructors,
            in CompoundUseSiteInfo<AssemblySymbol> overloadResolutionUseSiteInfo,
            BindingDiagnosticBag diagnostics)
        {
            //
            // !!! ATTENTION !!!
            //
            // In terms of errors relevant for HasCollectionExpressionApplicableConstructor check
            // this function should be kept in sync with local function
            // HasCollectionExpressionApplicableConstructor.bindClassCreationExpressionContinued,
            // assuming that it only needs to cover scenario with no explicit arguments and no initializers.
            //

            ReportConstructorUseSiteDiagnostics(typeNode.Location, diagnostics, suppressUnsupportedRequiredMembersError: false, in overloadResolutionUseSiteInfo);

            ImmutableArray<int> argToParams;

            if (memberResolutionResult.IsNotNull)
            {
                this.CheckAndCoerceArguments<MethodSymbol>(node, memberResolutionResult, analyzedArguments, diagnostics, receiver: null, invokedAsExtensionMethod: false, out argToParams);
            }
            else
            {
                argToParams = memberResolutionResult.Result.ArgsToParamsOpt;
            }

            var method = memberResolutionResult.Member;
            Debug.Assert(!method.IsExtensionBlockMember());

            bool hasError = false;

            // What if some of the arguments are implicit?  Dev10 reports unsafe errors
            // if the implied argument would have an unsafe type.  We need to check
            // the parameters explicitly, since there won't be bound nodes for the implied
            // arguments until lowering.
            if (method.HasParameterContainingPointerType())
            {
                // Don't worry about double reporting (i.e. for both the argument and the parameter)
                // because only one unsafe diagnostic is allowed per scope - the others are suppressed.
                hasError = ReportUnsafeIfNotAllowed(node, diagnostics) || hasError;
            }

            ReportDiagnosticsIfObsolete(diagnostics, method, node, hasBaseReceiver: false);
            // NOTE: Use-site diagnostics were reported during overload resolution.

            ConstantValue constantValueOpt = (initializerSyntaxOpt == null && method.IsDefaultValueTypeConstructor()) ?
                FoldParameterlessValueTypeConstructor(type) :
                null;

            var expanded = memberResolutionResult.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm;
            BindDefaultArguments(node, method.Parameters, extensionReceiver: null, analyzedArguments.Arguments, analyzedArguments.RefKinds, analyzedArguments.Names, ref argToParams, out var defaultArguments, expanded, enableCallerInfo: true, diagnostics: diagnostics);

            var arguments = analyzedArguments.Arguments.ToImmutable();
            var refKinds = analyzedArguments.RefKinds.ToImmutableOrNull();
            BoundObjectInitializerExpressionBase boundInitializerOpt;
            boundInitializerOpt = MakeBoundInitializerOpt(typeNode, type, initializerSyntaxOpt, initializerTypeOpt, diagnostics);
            var creation = new BoundObjectCreationExpression(
                node,
                method,
                candidateConstructors,
                arguments,
                analyzedArguments.GetNames(),
                refKinds,
                expanded,
                argToParams,
                defaultArguments,
                constantValueOpt,
                boundInitializerOpt,
                wasTargetTyped,
                type,
                hasError);

            CheckRequiredMembersInObjectInitializer(creation.Constructor, creation.InitializerExpressionOpt?.Initializers ?? default, creation.Syntax, diagnostics);

            return creation;
        }

        private BoundExpression CreateBadClassCreationExpression(
            SyntaxNode node,
            SyntaxNode typeNode,
            NamedTypeSymbol type,
            AnalyzedArguments analyzedArguments,
            InitializerExpressionSyntax initializerSyntaxOpt,
            TypeSymbol initializerTypeOpt,
            MemberResolutionResult<MethodSymbol> memberResolutionResult,
            ImmutableArray<MethodSymbol> candidateConstructors,
            in CompoundUseSiteInfo<AssemblySymbol> overloadResolutionUseSiteInfo,
            BindingDiagnosticBag diagnostics)
        {
            //
            // !!! ATTENTION !!!
            //
            // In terms of reported errors this function should be kept in sync with local function
            // HasCollectionExpressionApplicableConstructor.reportAdditionalDiagnosticsForOverloadResolutionFailure,
            // assuming that it only needs to cover scenario with no explicit arguments and no initializers.
            // 

            ReportConstructorUseSiteDiagnostics(typeNode.Location, diagnostics, suppressUnsupportedRequiredMembersError: false, in overloadResolutionUseSiteInfo);

            if (memberResolutionResult.IsNotNull)
            {
                this.CheckAndCoerceArguments<MethodSymbol>(node, memberResolutionResult, analyzedArguments, diagnostics, receiver: null, invokedAsExtensionMethod: false, argsToParamsOpt: out _);
            }

            LookupResultKind resultKind;
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);

            if (type.IsAbstract)
            {
                resultKind = LookupResultKind.NotCreatable;
            }
            else if (memberResolutionResult.IsValid && !IsConstructorAccessible(memberResolutionResult.Member, ref useSiteInfo))
            {
                resultKind = LookupResultKind.Inaccessible;
            }
            else
            {
                resultKind = LookupResultKind.OverloadResolutionFailure;
            }

            diagnostics.Add(node, useSiteInfo);

            ArrayBuilder<Symbol> symbols = ArrayBuilder<Symbol>.GetInstance();
            symbols.AddRange(candidateConstructors);

            // NOTE: The use site diagnostics of the candidate constructors have already been reported (in PerformConstructorOverloadResolution).

            var childNodes = ArrayBuilder<BoundExpression>.GetInstance();
            childNodes.AddRange(BuildArgumentsForErrorRecovery(analyzedArguments, candidateConstructors, BindingDiagnosticBag.Discarded));
            if (initializerSyntaxOpt != null)
            {
                childNodes.Add(MakeBoundInitializerOpt(typeNode, type, initializerSyntaxOpt, initializerTypeOpt, diagnostics));
            }

            return new BoundBadExpression(node, resultKind, symbols.ToImmutableAndFree(), childNodes.ToImmutableAndFree(), type);
        }

        private BoundObjectInitializerExpressionBase MakeBoundInitializerOpt(SyntaxNode typeNode, NamedTypeSymbol type, InitializerExpressionSyntax initializerSyntaxOpt, TypeSymbol initializerTypeOpt, BindingDiagnosticBag diagnostics)
        {
            if (initializerSyntaxOpt != null)
            {
                return BindInitializerExpression(syntax: initializerSyntaxOpt,
                                                 type: initializerTypeOpt ?? type,
                                                 typeSyntax: typeNode,
                                                 isForNewInstance: true,
                                                 diagnostics: diagnostics);
            }
            return null;
        }

        private BoundExpression BindInterfaceCreationExpression(ObjectCreationExpressionSyntax node, NamedTypeSymbol type, BindingDiagnosticBag diagnostics)
        {
            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();
            BindArgumentsAndNames(node.ArgumentList, diagnostics, analyzedArguments);
            var result = BindInterfaceCreationExpression(node, type, diagnostics, node.Type, analyzedArguments, node.Initializer, wasTargetTyped: false);
            analyzedArguments.Free();
            return result;
        }

        private BoundExpression BindInterfaceCreationExpression(SyntaxNode node, NamedTypeSymbol type, BindingDiagnosticBag diagnostics, SyntaxNode typeNode, AnalyzedArguments analyzedArguments, InitializerExpressionSyntax initializerOpt, bool wasTargetTyped)
        {
            Debug.Assert((object)type != null);

            // COM interfaces which have ComImportAttribute and CoClassAttribute can be instantiated with "new". 
            // CoClassAttribute contains the type information of the original CoClass for the interface.
            // We replace the interface creation with CoClass object creation for this case.

            // NOTE: We don't attempt binding interface creation to CoClass creation if we are within an attribute argument or default parameter value.
            // NOTE: This is done to prevent a cycle in an error scenario where we have a "new InterfaceType" expression in an attribute argument/default parameter value.
            // NOTE: Accessing IsComImport/ComImportCoClass properties on given type symbol would attempt ForceCompeteAttributes, which would again try binding all attributes on the symbol.
            // NOTE: causing infinite recursion. We avoid this cycle by checking if we are within in context of an Attribute argument.
            if (!this.InAttributeArgument && !this.InParameterDefaultValue && type.IsComImport)
            {
                NamedTypeSymbol coClassType = type.ComImportCoClass;
                if ((object)coClassType != null)
                {
                    return BindComImportCoClassCreationExpression(node, type, coClassType, diagnostics, typeNode, analyzedArguments, initializerOpt, wasTargetTyped);
                }
            }

            // interfaces can't be instantiated in C#
            diagnostics.Add(ErrorCode.ERR_NoNewAbstract, node.Location, type);
            return MakeBadExpressionForObjectCreation(node, type, analyzedArguments, initializerOpt, typeNode, diagnostics);
        }

        private BoundExpression BindComImportCoClassCreationExpression(SyntaxNode node, NamedTypeSymbol interfaceType, NamedTypeSymbol coClassType, BindingDiagnosticBag diagnostics, SyntaxNode typeNode, AnalyzedArguments analyzedArguments, InitializerExpressionSyntax initializerOpt, bool wasTargetTyped)
        {
            Debug.Assert((object)interfaceType != null);
            Debug.Assert(interfaceType.IsInterfaceType());
            Debug.Assert((object)coClassType != null);
            Debug.Assert(TypeSymbol.Equals(interfaceType.ComImportCoClass, coClassType, TypeCompareKind.ConsiderEverything2));
            Debug.Assert(coClassType.TypeKind == TypeKind.Class || coClassType.TypeKind == TypeKind.Error);

            if (coClassType.IsErrorType())
            {
                Error(diagnostics, ErrorCode.ERR_MissingCoClass, node, coClassType, interfaceType);
            }
            else if (coClassType.IsUnboundGenericType)
            {
                // BREAKING CHANGE:     Dev10 allows the following code to compile, even though the output assembly is not verifiable and generates a runtime exception:
                //
                //          [ComImport, Guid("00020810-0000-0000-C000-000000000046")]
                //          [CoClass(typeof(GenericClass<>))]
                //          public interface InterfaceType {}
                //          public class GenericClass<T>: InterfaceType {}
                // 
                //          public class Program
                //          {
                //              public static void Main() { var i = new InterfaceType(); }
                //          }
                //
                //  We disallow CoClass creation if coClassType is an unbound generic type and report a compile time error.

                Error(diagnostics, ErrorCode.ERR_BadCoClassSig, node, coClassType, interfaceType);
            }
            else
            {
                // NoPIA support
                if (interfaceType.ContainingAssembly.IsLinked)
                {
                    return BindNoPiaObjectCreationExpression(node, interfaceType, coClassType, diagnostics, typeNode, analyzedArguments, initializerOpt, wasTargetTyped);
                }

                var classCreation = BindClassCreationExpression(
                    node,
                    coClassType.Name,
                    typeNode,
                    coClassType,
                    analyzedArguments,
                    diagnostics,
                    initializerOpt,
                    interfaceType,
                    wasTargetTyped);
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                Conversion conversion = this.Conversions.ClassifyConversionFromExpression(classCreation, interfaceType, isChecked: CheckOverflowAtRuntime, ref useSiteInfo, forCast: true);
                diagnostics.Add(node, useSiteInfo);
                if (!conversion.IsValid)
                {
                    SymbolDistinguisher distinguisher = new SymbolDistinguisher(this.Compilation, coClassType, interfaceType);
                    Error(diagnostics, ErrorCode.ERR_NoExplicitConv, node, distinguisher.First, distinguisher.Second);
                }

                // Bind the conversion, but drop the conversion node.
                CreateConversion(classCreation, conversion, interfaceType, diagnostics);

                // Override result type to be the interface type.
                switch (classCreation.Kind)
                {
                    case BoundKind.ObjectCreationExpression:
                        var creation = (BoundObjectCreationExpression)classCreation;
                        return creation.Update(creation.Constructor, creation.ConstructorsGroup, creation.Arguments, creation.ArgumentNamesOpt,
                                               creation.ArgumentRefKindsOpt, creation.Expanded, creation.ArgsToParamsOpt, creation.DefaultArguments, creation.ConstantValueOpt,
                                               creation.InitializerExpressionOpt, interfaceType);

                    case BoundKind.BadExpression:
                        var bad = (BoundBadExpression)classCreation;
                        return bad.Update(bad.ResultKind, bad.Symbols, bad.ChildBoundNodes, interfaceType);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(classCreation.Kind);
                }
            }

            return MakeBadExpressionForObjectCreation(node, interfaceType, analyzedArguments, initializerOpt, typeNode, diagnostics);
        }

        private BoundExpression BindNoPiaObjectCreationExpression(
            SyntaxNode node,
            NamedTypeSymbol interfaceType,
            NamedTypeSymbol coClassType,
            BindingDiagnosticBag diagnostics,
            SyntaxNode typeNode,
            AnalyzedArguments analyzedArguments,
            InitializerExpressionSyntax initializerOpt,
            bool wasTargetTyped)
        {
            string guidString;
            if (!coClassType.GetGuidString(out guidString))
            {
                // At this point, VB reports ERRID_NoPIAAttributeMissing2 if guid isn't there.
                // C# doesn't complain and instead uses zero guid.
                guidString = System.Guid.Empty.ToString("D");
            }

            var boundInitializerOpt = initializerOpt == null ? null :
                BindInitializerExpression(syntax: initializerOpt,
                    type: interfaceType,
                    typeSyntax: typeNode,
                    isForNewInstance: true,
                    diagnostics: diagnostics);

            if (analyzedArguments.Arguments.Count > 0)
            {
                diagnostics.Add(ErrorCode.ERR_BadCtorArgCount, typeNode.Location, interfaceType, analyzedArguments.Arguments.Count);

                var children = BuildArgumentsForErrorRecovery(analyzedArguments);

                if (boundInitializerOpt is not null)
                {
                    children = children.Add(boundInitializerOpt);
                }

                return new BoundBadExpression(node, LookupResultKind.OverloadResolutionFailure, ImmutableArray<Symbol>.Empty, children, interfaceType);
            }

            return new BoundNoPiaObjectCreationExpression(node, guidString, boundInitializerOpt, wasTargetTyped, interfaceType);
        }

        private BoundExpression BindTypeParameterCreationExpression(ObjectCreationExpressionSyntax node, TypeParameterSymbol typeParameter, BindingDiagnosticBag diagnostics)
        {
            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();
            BindArgumentsAndNames(node.ArgumentList, diagnostics, analyzedArguments);
            var result = BindTypeParameterCreationExpression(node, typeParameter, analyzedArguments, node.Initializer, node.Type, wasTargetTyped: false, diagnostics);
            analyzedArguments.Free();
            return result;
        }

#nullable enable
        private static bool TypeParameterHasParameterlessConstructor(SyntaxNode node, TypeParameterSymbol typeParameter, BindingDiagnosticBag diagnostics)
        {
            if (!typeParameter.HasConstructorConstraint && !typeParameter.IsValueType)
            {
                diagnostics.Add(ErrorCode.ERR_NoNewTyvar, node.Location, typeParameter);
                return false;
            }

            return true;
        }

        private BoundExpression BindTypeParameterCreationExpression(
            SyntaxNode node, TypeParameterSymbol typeParameter, AnalyzedArguments analyzedArguments, InitializerExpressionSyntax? initializerOpt,
            SyntaxNode typeSyntax, bool wasTargetTyped, BindingDiagnosticBag diagnostics)
        {
            if (TypeParameterHasParameterlessConstructor(node, typeParameter, diagnostics))
            {
                if (analyzedArguments.Arguments.Count > 0)
                {
                    diagnostics.Add(ErrorCode.ERR_NewTyvarWithArgs, node.Location, typeParameter);
                }
                else
                {
                    var boundInitializerOpt = initializerOpt == null ?
                         null :
                         BindInitializerExpression(
                            syntax: initializerOpt,
                            type: typeParameter,
                            typeSyntax: typeSyntax,
                            isForNewInstance: true,
                            diagnostics: diagnostics);
                    return new BoundNewT(node, boundInitializerOpt, wasTargetTyped, typeParameter);
                }
            }

            return MakeBadExpressionForObjectCreation(node, typeParameter, analyzedArguments, initializerOpt, typeSyntax, diagnostics);
        }
#nullable disable

        /// <summary>
        /// Given the type containing constructors, gets the list of candidate instance constructors and uses overload resolution to determine which one should be called.
        /// </summary>
        /// <param name="typeContainingConstructors">The containing type of the constructors.</param>
        /// <param name="analyzedArguments">The already bound arguments to the constructor.</param>
        /// <param name="errorName">The name to use in diagnostics if overload resolution fails.</param>
        /// <param name="errorLocation">The location at which to report overload resolution result diagnostics.</param>
        /// <param name="suppressResultDiagnostics">True to suppress overload resolution result diagnostics (but not argument diagnostics).</param>
        /// <param name="diagnostics">Where diagnostics will be reported.</param>
        /// <param name="memberResolutionResult">If this method returns true, then it will contain a valid MethodResolutionResult.
        /// Otherwise, it may contain a MethodResolutionResult for an inaccessible constructor (in which case, it will incorrectly indicate success) or nothing at all.</param>
        /// <param name="candidateConstructors">Candidate instance constructors of type <paramref name="typeContainingConstructors"/> used for overload resolution.</param>
        /// <param name="allowProtectedConstructorsOfBaseType">It is always legal to access a protected base class constructor
        /// via a constructor initializer, but not from an object creation expression.</param>
        /// <returns>True if overload resolution successfully chose an accessible constructor.</returns>
        /// <remarks>
        /// The two-pass algorithm (accessible constructors, then all constructors) is the reason for the unusual signature
        /// of this method (i.e. not populating a pre-existing <see cref="OverloadResolutionResult{MethodSymbol}"/>).
        /// Presently, rationalizing this behavior is not worthwhile.
        /// </remarks>
        internal bool TryPerformConstructorOverloadResolution(
            NamedTypeSymbol typeContainingConstructors,
            AnalyzedArguments analyzedArguments,
            string errorName,
            Location errorLocation,
            bool suppressResultDiagnostics,
            BindingDiagnosticBag diagnostics,
            out MemberResolutionResult<MethodSymbol> memberResolutionResult,
            out ImmutableArray<MethodSymbol> candidateConstructors,
            bool allowProtectedConstructorsOfBaseType,
            out CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
            bool isParamsModifierValidation = false)
        {
            // Get all accessible constructors for performing overload resolution.
            useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            candidateConstructors = GetAccessibleConstructorsForOverloadResolution(
                typeContainingConstructors,
                allowProtectedConstructorsOfBaseType,
                out var allInstanceConstructors, ref useSiteInfo);

            // Then perform overload resolution with all the accessible constructors.
            return TryPerformOverloadResolutionWithConstructorSubset(
                typeContainingConstructors,
                ref candidateConstructors,
                allInstanceConstructors,
                analyzedArguments,
                errorName,
                errorLocation,
                suppressResultDiagnostics, diagnostics,
                out memberResolutionResult,
                ref useSiteInfo,
                isParamsModifierValidation);
        }

        /// <summary>
        /// Core implementation for <see cref="TryPerformConstructorOverloadResolution"/>, just with the ability for the
        /// caller to specify the candidate constructors instead of computing them from <paramref
        /// name="typeContainingConstructors"/>.
        /// </summary>
        private bool TryPerformOverloadResolutionWithConstructorSubset(
            NamedTypeSymbol typeContainingConstructors,
            ref ImmutableArray<MethodSymbol> candidateConstructors,
            ImmutableArray<MethodSymbol> allInstanceConstructors,
            AnalyzedArguments analyzedArguments,
            string errorName,
            Location errorLocation,
            bool suppressResultDiagnostics,
            BindingDiagnosticBag diagnostics,
            out MemberResolutionResult<MethodSymbol> memberResolutionResult,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
            bool isParamsModifierValidation)
        {
            OverloadResolutionResult<MethodSymbol> result = OverloadResolutionResult<MethodSymbol>.GetInstance();

            // Indicates whether overload resolution successfully chose an accessible constructor.
            bool succeededConsideringAccessibility = false;

            if (candidateConstructors.Any())
            {
                // We have at least one accessible candidate constructor, perform overload resolution with accessible candidateConstructors.
                this.OverloadResolution.ObjectCreationOverloadResolution(candidateConstructors, analyzedArguments, result, dynamicResolution: false, isEarlyAttributeBinding: IsEarlyAttributeBinder, ref useSiteInfo);

                if (result.Succeeded)
                {
                    succeededConsideringAccessibility = true;
                }
            }

            if (!succeededConsideringAccessibility && allInstanceConstructors.Length > candidateConstructors.Length)
            {
                // Overload resolution failed on the accessible candidateConstructors, but we have at least one inaccessible constructor.
                // We might have a best match constructor which is inaccessible.
                // Try overload resolution with all instance constructors to generate correct diagnostics and semantic info for this case.
                OverloadResolutionResult<MethodSymbol> inaccessibleResult = OverloadResolutionResult<MethodSymbol>.GetInstance();
                this.OverloadResolution.ObjectCreationOverloadResolution(allInstanceConstructors, analyzedArguments, inaccessibleResult, dynamicResolution: false, isEarlyAttributeBinding: IsEarlyAttributeBinder, ref useSiteInfo);

                if (inaccessibleResult.Succeeded)
                {
                    candidateConstructors = allInstanceConstructors;
                    result.Free();
                    result = inaccessibleResult;
                }
                else
                {
                    inaccessibleResult.Free();
                }
            }

            // Fill in the out parameter with the result, if there was one; it might be inaccessible.
            memberResolutionResult = result.Succeeded ?
                result.ValidResult :
                default(MemberResolutionResult<MethodSymbol>); // Invalid results are not interesting - we have enough info in candidateConstructors.

            // If something failed and we are reporting errors, then report the right errors.
            // * If the failure was due to inaccessibility, just report that.
            // * If the failure was not due to inaccessibility then only report an error
            //   on the constructor if there were no errors on the arguments.
            if (!succeededConsideringAccessibility && !suppressResultDiagnostics)
            {
                if (result.Succeeded)
                {
                    // It is not legal to directly call a protected constructor on a base class unless
                    // the "this" of the call is known to be of the current type. That is, it is
                    // perfectly legal to say ": base()" to call a protected base class ctor, but
                    // it is not legal to say "new MyBase()" if the ctor is protected. 
                    //
                    // The native compiler produces the error CS1540:
                    //
                    //   Cannot access protected member 'MyBase.MyBase' via a qualifier of type 'MyBase'; 
                    //   the qualifier must be of type 'Derived' (or derived from it)
                    //
                    // Though technically correct, this is a very confusing error message for this scenario;
                    // one does not typically think of the constructor as being a method that is 
                    // called with an implicit "this" of a particular receiver type, even though of course
                    // that is exactly what it is.
                    //
                    // The better error message here is to simply say that the best possible ctor cannot
                    // be accessed because it is not accessible.
                    //
                    // CONSIDER: We might consider making up a new error message for this situation.

                    // 
                    // CS0122: 'MyBase.MyBase' is inaccessible due to its protection level
                    diagnostics.Add(ErrorCode.ERR_BadAccess, errorLocation, result.ValidResult.Member);
                }
                else
                {
                    result.ReportDiagnostics(
                        binder: this, location: errorLocation, nodeOpt: null, diagnostics,
                        name: errorName, receiver: null, invokedExpression: null, analyzedArguments,
                        memberGroup: candidateConstructors, typeContainingConstructors, delegateTypeBeingInvoked: null,
                        isParamsModifierValidation: isParamsModifierValidation);
                }
            }

            result.Free();
            return succeededConsideringAccessibility;
        }

        internal static void ReportConstructorUseSiteDiagnostics(Location errorLocation, BindingDiagnosticBag diagnostics, bool suppressUnsupportedRequiredMembersError, in CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            if (suppressUnsupportedRequiredMembersError && useSiteInfo.AccumulatesDiagnostics && useSiteInfo.Diagnostics is { Count: not 0 })
            {
                diagnostics.AddDependencies(useSiteInfo);
                foreach (var diagnostic in useSiteInfo.Diagnostics)
                {
                    // We don't want to report this error here because we'll report ERR_RequiredMembersBaseTypeInvalid. That error is suppressable by the
                    // user using the `SetsRequiredMembers` attribute on the constructor, so reporting this error would prevent that from working.
                    if ((ErrorCode)diagnostic.Code == ErrorCode.ERR_RequiredMembersInvalid)
                    {
                        continue;
                    }

                    diagnostics.ReportUseSiteDiagnostic(diagnostic, errorLocation);
                }
            }
            else
            {
                diagnostics.Add(errorLocation, useSiteInfo);
            }
        }

        private ImmutableArray<MethodSymbol> GetAccessibleConstructorsForOverloadResolution(NamedTypeSymbol type, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            ImmutableArray<MethodSymbol> allInstanceConstructors;
            return GetAccessibleConstructorsForOverloadResolution(type, false, out allInstanceConstructors, ref useSiteInfo);
        }

        private ImmutableArray<MethodSymbol> GetAccessibleConstructorsForOverloadResolution(NamedTypeSymbol type, bool allowProtectedConstructorsOfBaseType, out ImmutableArray<MethodSymbol> allInstanceConstructors, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            if (type.IsErrorType())
            {
                // For Caas, we want to supply the constructors even in error cases
                // We may end up supplying the constructors of an unconstructed symbol,
                // but that's better than nothing.
                type = type.GetNonErrorGuess() as NamedTypeSymbol ?? type;
            }

            allInstanceConstructors = type.InstanceConstructors;
            return FilterInaccessibleConstructors(allInstanceConstructors, allowProtectedConstructorsOfBaseType, ref useSiteInfo);
        }

        private static ConstantValue FoldParameterlessValueTypeConstructor(NamedTypeSymbol type)
        {
            // DELIBERATE SPEC VIOLATION:
            //
            // Object creation expressions like "new int()" are not considered constant expressions
            // by the specification but they are by the native compiler; we maintain compatibility
            // with this bug.
            // 
            // Additionally, it also treats "new X()", where X is an enum type, as a
            // constant expression with default value 0, we maintain compatibility with it.

            var specialType = type.SpecialType;

            if (type.TypeKind == TypeKind.Enum)
            {
                specialType = type.EnumUnderlyingType.SpecialType;
            }

            switch (specialType)
            {
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Byte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                    return ConstantValue.Default(specialType);
            }

            return null;
        }

        private BoundLiteral BindLiteralConstant(LiteralExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            // bug.Assert(node.Kind == SyntaxKind.LiteralExpression);

            // Warn about a lower-cased 'l' being confused with a '1'.
            if (node.Kind() is SyntaxKind.NumericLiteralExpression)
            {
                var token = node.Token;
                var text = node.Token.Text;
                if (text.EndsWith("l", StringComparison.Ordinal))
                {
                    // don't warn on the ul and uL cases.  The 'u' clearly separates the number from the 'l' suffix.
                    if (!text.EndsWith("ul") && !text.EndsWith("Ul"))
                        diagnostics.Add(new CSDiagnosticInfo(ErrorCode.WRN_LowercaseEllSuffix), Location.Create(node.SyntaxTree, new TextSpan(token.Span.End - 1, 1)));
                }
                else if (text.EndsWith("lu", StringComparison.Ordinal) || text.EndsWith("lU", StringComparison.Ordinal))
                {
                    diagnostics.Add(new CSDiagnosticInfo(ErrorCode.WRN_LowercaseEllSuffix), Location.Create(node.SyntaxTree, new TextSpan(token.Span.End - 2, 1)));
                }
            }

            var value = node.Token.Value;

            ConstantValue cv;
            TypeSymbol type = null;

            if (value == null)
            {
                cv = ConstantValue.Null;
            }
            else
            {
                Debug.Assert(!value.GetType().GetTypeInfo().IsEnum);

                var specialType = SpecialTypeExtensions.FromRuntimeTypeOfLiteralValue(value);

                // C# literals can't be of type byte, sbyte, short, ushort:
                Debug.Assert(
                    specialType != SpecialType.None &&
                    specialType != SpecialType.System_Byte &&
                    specialType != SpecialType.System_SByte &&
                    specialType != SpecialType.System_Int16 &&
                    specialType != SpecialType.System_UInt16);

                cv = ConstantValue.Create(value, specialType);
                type = GetSpecialType(specialType, diagnostics, node);
            }

            if (node.Token.Kind() is SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken)
            {
                MessageID.IDS_FeatureRawStringLiterals.CheckFeatureAvailability(diagnostics, node);
            }

            return new BoundLiteral(node, cv, type);
        }

        private BoundUtf8String BindUtf8StringLiteral(LiteralExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(node.Kind() == SyntaxKind.Utf8StringLiteralExpression);
            Debug.Assert(node.Token.Kind() is SyntaxKind.Utf8StringLiteralToken or SyntaxKind.Utf8SingleLineRawStringLiteralToken or SyntaxKind.Utf8MultiLineRawStringLiteralToken);

            if (node.Token.Kind() is SyntaxKind.Utf8SingleLineRawStringLiteralToken or SyntaxKind.Utf8MultiLineRawStringLiteralToken)
            {
                CheckFeatureAvailability(node, MessageID.IDS_FeatureRawStringLiterals, diagnostics);
            }

            CheckFeatureAvailability(node, MessageID.IDS_FeatureUtf8StringLiterals, diagnostics);

            var value = (string)node.Token.Value;
            var type = GetWellKnownType(WellKnownType.System_ReadOnlySpan_T, diagnostics, node).Construct(GetSpecialType(SpecialType.System_Byte, diagnostics, node));

            return new BoundUtf8String(node, value, type);
        }

        private BoundExpression BindCheckedExpression(CheckedExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            var binder = this.GetBinder(node);
            return binder.BindParenthesizedExpression(node.Expression, diagnostics);
        }

        /// <summary>
        /// Binds a member access expression
        /// </summary>
        private BoundExpression BindMemberAccess(
            MemberAccessExpressionSyntax node,
            bool invoked,
            bool indexed,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            Debug.Assert(invoked == SyntaxFacts.IsInvoked(node));

            BoundExpression boundLeft;

            ExpressionSyntax exprSyntax = node.Expression;
            if (node.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                // NOTE: CheckValue will be called explicitly in BindMemberAccessWithBoundLeft.
                boundLeft = BindLeftOfPotentialColorColorMemberAccess(exprSyntax, diagnostics);
            }
            else
            {
                Debug.Assert(node.Kind() == SyntaxKind.PointerMemberAccessExpression);
                boundLeft = BindRValueWithoutTargetType(exprSyntax, diagnostics); // Not Color Color issues with ->

                // CONSIDER: another approach would be to construct a BoundPointerMemberAccess (assuming such a type existed),
                // but that would be much more cumbersome because we'd be unable to build upon the BindMemberAccess infrastructure,
                // which expects a receiver.

                // Dereference before binding member;
                TypeSymbol pointedAtType;
                bool hasErrors;
                BindPointerIndirectionExpressionInternal(node, boundLeft, diagnostics, out pointedAtType, out hasErrors);

                // If there is no pointed-at type, fall back on the actual type (i.e. assume the user meant "." instead of "->").
                if (ReferenceEquals(pointedAtType, null))
                {
                    boundLeft = ToBadExpression(boundLeft);
                }
                else
                {
                    boundLeft = new BoundPointerIndirectionOperator(exprSyntax, boundLeft, refersToLocation: false, pointedAtType, hasErrors)
                    {
                        WasCompilerGenerated = true, // don't interfere with the type info for exprSyntax.
                    };
                }
            }

            return BindMemberAccessWithBoundLeft(node, boundLeft, node.Name, node.OperatorToken, invoked, indexed, diagnostics);
        }

        /// <summary>
        /// Attempt to bind the LHS of a member access expression.  If this is a Color Color case (spec 7.6.4.1),
        /// then return a BoundExpression if we can easily disambiguate or a BoundTypeOrValueExpression if we
        /// cannot.  If this is not a Color Color case, then return null.
        /// </summary>
        private BoundExpression BindLeftOfPotentialColorColorMemberAccess(ExpressionSyntax left, BindingDiagnosticBag diagnostics)
        {
            if (left is IdentifierNameSyntax identifier)
            {
                return BindLeftIdentifierOfPotentialColorColorMemberAccess(identifier, diagnostics);
            }

            // NOTE: it is up to the caller to call CheckValue on the result.
            return BindExpression(left, diagnostics);
        }

        // Avoid inlining to minimize stack size in caller.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private BoundExpression BindLeftIdentifierOfPotentialColorColorMemberAccess(IdentifierNameSyntax left, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert((left.Parent is MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression } memberAccess && memberAccess.Expression == left) ||
                         (left.Parent is QualifiedNameSyntax qualifiedName && qualifiedName.Left == left) ||
                         (left.Parent is FromClauseSyntax { Parent: QueryExpressionSyntax query } fromClause && query.FromClause == fromClause && fromClause.Expression == left));

            // SPEC: 7.6.4.1 Identical simple names and type names
            // SPEC: In a member access of the form E.I, if E is a single identifier, and if the meaning of E as
            // SPEC: a simple-name (spec 7.6.2) is a constant, field, property, local variable, or parameter with the
            // SPEC: same type as the meaning of E as a type-name (spec 3.8), then both possible meanings of E are 
            // SPEC: permitted. The two possible meanings of E.I are never ambiguous, since I must necessarily be
            // SPEC: a member of the type E in both cases. In other words, the rule simply permits access to the 
            // SPEC: static members and nested types of E where a compile-time error would otherwise have occurred. 

            Debug.Assert(left.Arity == 0);

#if DEBUG
            AdjustIdentifierMapIfAny(left, invoked: false);
#endif

            if (left.IsMissing)
            {
                return bindAsValue(left, diagnostics);
            }

            var lookupResult = LookupResult.GetInstance();
            var useSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            this.LookupIdentifier(lookupResult, left, invoked: false, ref useSiteInfo);

            Symbol leftSymbol = lookupResult.IsSingleViable ? lookupResult.Symbols[0] : null;
            lookupResult.Free();

            if (leftSymbol is null)
            {
                return bindAsValue(left, diagnostics);
            }

            TypeSymbol leftType = null;

            switch (leftSymbol.Kind)
            {
                case SymbolKind.Field:
                    var fieldSymbol = (FieldSymbol)leftSymbol;
                    leftType = fieldSymbol.GetFieldType(this.FieldsBeingBound).Type;
                    leftType = GetAdjustedTypeForEnumMemberReference(fieldSymbol, leftType) ?? leftType;
                    break;
                case SymbolKind.Local:
                    leftType = BindResultTypeForLocalVariableReference(left, (LocalSymbol)leftSymbol, BindingDiagnosticBag.Discarded, isNullableUnknown: out _, isError: out _);
                    break;
                case SymbolKind.Parameter:
                    leftType = ((ParameterSymbol)leftSymbol).Type;
                    break;
                case SymbolKind.Property:
                    leftType = ((PropertySymbol)leftSymbol).Type;
                    break;
                case SymbolKind.RangeVariable:
                    leftType = BindRangeVariable(left, (RangeVariableSymbol)leftSymbol, BindingDiagnosticBag.Discarded).Type;
                    break;

                    // case SymbolKind.Event: //SPEC: 7.6.4.1 (a.k.a. Color Color) doesn't cover events
            }

            if (leftType is null)
            {
                return bindAsValue(left, diagnostics);
            }

            var leftName = left.Identifier.ValueText;
            if (leftType.Name == leftName || IsUsingAliasInScope(leftName))
            {
                var boundType = BindNamespaceOrType(left, BindingDiagnosticBag.Discarded);
                if (TypeSymbol.Equals(boundType.Type, leftType, TypeCompareKind.AllIgnoreOptions))
                {
                    Debug.Assert(!leftType.IsDynamic());
                    Debug.Assert(IsPotentialColorColorReceiver(left, leftType));

                    return new BoundTypeOrValueExpression(left, this, leftSymbol, leftType);
                }
            }

            Debug.Assert(!IsPotentialColorColorReceiver(left, leftType));

            var boundValue = bindAsValue(left, diagnostics);
            Debug.Assert(leftType.Equals(boundValue.Type, TypeCompareKind.ConsiderEverything));
            Debug.Assert(leftSymbol == (boundValue.ExpressionSymbol ?? ((BoundConversion)boundValue).Operand.ExpressionSymbol));

            return boundValue;

            BoundExpression bindAsValue(IdentifierNameSyntax left, BindingDiagnosticBag diagnostics)
            {
                // Not a Color Color case; return the bound member.
                // NOTE: it is up to the caller to call CheckValue on the result.
                return BindIdentifier(left, invoked: false, indexed: false, diagnostics: diagnostics);
            }
        }

        private bool IsPotentialColorColorReceiver(IdentifierNameSyntax id, TypeSymbol type)
        {
            string name = id.Identifier.ValueText;

            return (type.Name == name || IsUsingAliasInScope(name)) &&
                   TypeSymbol.Equals(BindNamespaceOrType(id, BindingDiagnosticBag.Discarded).Type, type, TypeCompareKind.AllIgnoreOptions);
        }

        // returns true if name matches a using alias in scope
        // NOTE: when true is returned, the corresponding using is also marked as "used" 
        private bool IsUsingAliasInScope(string name)
        {
            var isSemanticModel = this.IsSemanticModelBinder;
            for (var chain = this.ImportChain; chain != null; chain = chain.ParentOpt)
            {
                if (IsUsingAlias(chain.Imports.UsingAliases, name, isSemanticModel))
                {
                    return true;
                }
            }

            return false;
        }

        private BoundExpression BindDynamicMemberAccess(
            ExpressionSyntax node,
            BoundExpression boundLeft,
            SimpleNameSyntax right,
            bool invoked,
            bool indexed,
            BindingDiagnosticBag diagnostics)
        {
            // We have an expression of the form "dynExpr.Name" or "dynExpr.Name<X>"

            SeparatedSyntaxList<TypeSyntax> typeArgumentsSyntax = right.Kind() == SyntaxKind.GenericName ?
                ((GenericNameSyntax)right).TypeArgumentList.Arguments :
                default(SeparatedSyntaxList<TypeSyntax>);
            bool rightHasTypeArguments = typeArgumentsSyntax.Count > 0;
            ImmutableArray<TypeWithAnnotations> typeArgumentsWithAnnotations = rightHasTypeArguments ?
                BindTypeArguments(typeArgumentsSyntax, diagnostics) :
                default(ImmutableArray<TypeWithAnnotations>);

            bool hasErrors = false;

            if (!invoked && rightHasTypeArguments)
            {
                // error CS0307: The property 'P' cannot be used with type arguments
                Error(diagnostics, ErrorCode.ERR_TypeArgsNotAllowed, right, right.Identifier.Text, SymbolKind.Property.Localize());
                hasErrors = true;
            }

            if (rightHasTypeArguments)
            {
                for (int i = 0; i < typeArgumentsWithAnnotations.Length; ++i)
                {
                    var typeArgument = typeArgumentsWithAnnotations[i];
                    if (typeArgument.Type.IsPointerOrFunctionPointer() || typeArgument.Type.IsRestrictedType())
                    {
                        // "The type '{0}' may not be used as a type argument"
                        Error(diagnostics, ErrorCode.ERR_BadTypeArgument, typeArgumentsSyntax[i], typeArgument.Type);
                        hasErrors = true;
                    }
                }
            }

            if (!hasErrors && typeArgumentsSyntax.Any(SyntaxKind.OmittedTypeArgument))
            {
                Error(diagnostics, ErrorCode.ERR_OmittedTypeArgument, node);
                hasErrors = true;
            }

            return new BoundDynamicMemberAccess(
                syntax: node,
                receiver: boundLeft,
                typeArgumentsOpt: typeArgumentsWithAnnotations,
                name: right.Identifier.ValueText,
                invoked: invoked,
                indexed: indexed,
                type: Compilation.DynamicType,
                hasErrors: hasErrors);
        }

#if DEBUG
        /// <summary>
        /// Bind the RHS of a member access expression, given the bound LHS.
        /// It is assumed that CheckValue has not been called on the LHS.
        /// </summary>
        /// <remarks>
        /// If new checks are added to this method, they will also need to be added to
        /// <see cref="MakeQueryInvocation(CSharpSyntaxNode, BoundExpression, string, SeparatedSyntaxList{TypeSyntax}, ImmutableArray{TypeWithAnnotations}, ImmutableArray{BoundExpression}, BindingDiagnosticBag, string?)"/>.
        /// </remarks>
#else
        /// <summary>
        /// Bind the RHS of a member access expression, given the bound LHS.
        /// It is assumed that CheckValue has not been called on the LHS.
        /// </summary>
        /// <remarks>
        /// If new checks are added to this method, they will also need to be added to
        /// <see cref="MakeQueryInvocation(CSharpSyntaxNode, BoundExpression, string, SeparatedSyntaxList{TypeSyntax}, ImmutableArray{TypeWithAnnotations}, ImmutableArray{BoundExpression}, BindingDiagnosticBag)"/>.
        /// </remarks>
#endif
        private BoundExpression BindMemberAccessWithBoundLeft(
            ExpressionSyntax node,
            BoundExpression boundLeft,
            SimpleNameSyntax right,
            SyntaxToken operatorToken,
            bool invoked,
            bool indexed,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            Debug.Assert(boundLeft != null);

            boundLeft = MakeMemberAccessValue(boundLeft, diagnostics);

            TypeSymbol leftType = boundLeft.Type;

            if ((object)leftType != null && leftType.IsDynamic())
            {
                // There are some sources of a `dynamic` typed value that can be known before runtime
                // to be invalid. For example, accessing a set-only property whose type is dynamic:
                //   dynamic Goo { set; }
                // If Goo itself is a dynamic thing (e.g. in `x.Goo.Bar`, `x` is dynamic, and we're
                // currently checking Bar), then CheckValue will do nothing.
                boundLeft = CheckValue(boundLeft, BindValueKind.RValue, diagnostics);
                return BindDynamicMemberAccess(node, boundLeft, right, invoked, indexed, diagnostics);
            }

            // No member accesses on void
            if ((object)leftType != null && leftType.IsVoidType())
            {
                diagnostics.Add(ErrorCode.ERR_BadUnaryOp, operatorToken.GetLocation(), SyntaxFacts.GetText(operatorToken.Kind()), leftType);
                return BadExpression(node, boundLeft);
            }

            // No member accesses on default
            if (boundLeft.IsLiteralDefault())
            {
                DiagnosticInfo diagnosticInfo = new CSDiagnosticInfo(ErrorCode.ERR_BadOpOnNullOrDefaultOrNew, SyntaxFacts.GetText(operatorToken.Kind()), boundLeft.Display);
                diagnostics.Add(new CSDiagnostic(diagnosticInfo, operatorToken.GetLocation()));
                return BadExpression(node, boundLeft);
            }

            if (boundLeft.Kind == BoundKind.UnboundLambda)
            {
                Debug.Assert((object)leftType == null);

                var msgId = ((UnboundLambda)boundLeft).MessageID;
                diagnostics.Add(ErrorCode.ERR_BadUnaryOp, node.Location, SyntaxFacts.GetText(operatorToken.Kind()), msgId.Localize());
                return BadExpression(node, boundLeft);
            }

            boundLeft = BindToNaturalType(boundLeft, diagnostics);
            leftType = boundLeft.Type;
            var lookupResult = LookupResult.GetInstance();
            try
            {
                LookupOptions options = LookupOptions.AllMethodsOnArityZero;
                if (invoked)
                {
                    options |= LookupOptions.MustBeInvocableIfMember;
                }

                var typeArgumentsSyntax = right.Kind() == SyntaxKind.GenericName ? ((GenericNameSyntax)right).TypeArgumentList.Arguments : default(SeparatedSyntaxList<TypeSyntax>);
                var typeArguments = typeArgumentsSyntax.Count > 0 ? BindTypeArguments(typeArgumentsSyntax, diagnostics) : default(ImmutableArray<TypeWithAnnotations>);

                // A member-access consists of a primary-expression, a predefined-type, or a 
                // qualified-alias-member, followed by a "." token, followed by an identifier, 
                // optionally followed by a type-argument-list.

                // A member-access is either of the form E.I or of the form E.I<A1, ..., AK>, where
                // E is a primary-expression, I is a single identifier and <A1, ..., AK> is an
                // optional type-argument-list. When no type-argument-list is specified, consider K
                // to be zero. 

                // UNDONE: A member-access with a primary-expression of type dynamic is dynamically bound. 
                // UNDONE: In this case the compiler classifies the member access as a property access of 
                // UNDONE: type dynamic. The rules below to determine the meaning of the member-access are 
                // UNDONE: then applied at run-time, using the run-time type instead of the compile-time 
                // UNDONE: type of the primary-expression. If this run-time classification leads to a method 
                // UNDONE: group, then the member access must be the primary-expression of an invocation-expression.

                // The member-access is evaluated and classified as follows:

                var rightName = right.Identifier.ValueText;
                var rightArity = right.Arity;
                BoundExpression result;

                switch (boundLeft.Kind)
                {
                    case BoundKind.NamespaceExpression:
                        {
                            result = tryBindMemberAccessWithBoundNamespaceLeft(((BoundNamespaceExpression)boundLeft).NamespaceSymbol, node, boundLeft, right, diagnostics, lookupResult, options, typeArgumentsSyntax, typeArguments, rightName, rightArity);
                            if (result is object)
                            {
                                return result;
                            }

                            break;
                        }
                    case BoundKind.TypeExpression:
                        {
                            result = tryBindMemberAccessWithBoundTypeLeft(node, boundLeft, right, invoked, indexed, diagnostics, leftType, lookupResult, options, typeArgumentsSyntax, typeArguments, rightName, rightArity);
                            if (result is object)
                            {
                                return result;
                            }

                            break;
                        }
                    case BoundKind.TypeOrValueExpression:
                        {
                            // CheckValue call will occur in ReplaceTypeOrValueReceiver.
                            // NOTE: This means that we won't get CheckValue diagnostics in error scenarios,
                            // but they would be cascading anyway.
                            return BindInstanceMemberAccess(node, right, boundLeft, rightName, rightArity, typeArgumentsSyntax, typeArguments, invoked, indexed, diagnostics);
                        }
                    default:
                        {
                            // Can't dot into the null literal
                            if (boundLeft.Kind == BoundKind.Literal && ((BoundLiteral)boundLeft).ConstantValueOpt == ConstantValue.Null)
                            {
                                if (!boundLeft.HasAnyErrors)
                                {
                                    Error(diagnostics, ErrorCode.ERR_BadUnaryOp, node, operatorToken.Text, boundLeft.Display);
                                }

                                return BadExpression(node, boundLeft);
                            }
                            else if ((object)leftType != null)
                            {
                                // NB: We don't know if we really only need RValue access, or if we are actually
                                // passing the receiver implicitly by ref (e.g. in a struct instance method invocation).
                                // These checks occur later.
                                boundLeft = CheckValue(boundLeft, BindValueKind.RValue, diagnostics);
                                boundLeft = BindToNaturalType(boundLeft, diagnostics);
                                return BindInstanceMemberAccess(node, right, boundLeft, rightName, rightArity, typeArgumentsSyntax, typeArguments, invoked, indexed, diagnostics);
                            }
                            break;
                        }
                }

                this.BindMemberAccessReportError(node, right, rightName, boundLeft, lookupResult.Error, diagnostics);
                return BindMemberAccessBadResult(node, rightName, boundLeft, lookupResult.Error, lookupResult.Symbols.ToImmutable(), lookupResult.Kind);
            }
            finally
            {
                lookupResult.Free();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            BoundExpression tryBindMemberAccessWithBoundNamespaceLeft(
                NamespaceSymbol ns,
                ExpressionSyntax node,
                BoundExpression boundLeft,
                SimpleNameSyntax right,
                BindingDiagnosticBag diagnostics,
                LookupResult lookupResult,
                LookupOptions options,
                SeparatedSyntaxList<TypeSyntax> typeArgumentsSyntax,
                ImmutableArray<TypeWithAnnotations> typeArguments,
                string rightName,
                int rightArity)
            {
                // If K is zero and E is a namespace and E contains a nested namespace with name I, 
                // then the result is that namespace.

                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                this.LookupMembersWithFallback(lookupResult, ns, rightName, rightArity, ref useSiteInfo, options: options);
                diagnostics.Add(right, useSiteInfo);

                ArrayBuilder<Symbol> symbols = lookupResult.Symbols;

                if (lookupResult.IsMultiViable)
                {
                    bool wasError;
                    Symbol sym = ResultSymbol(lookupResult, rightName, rightArity, node, diagnostics, false, out wasError, ns, options);
                    if (wasError)
                    {
                        return new BoundBadExpression(node, LookupResultKind.Ambiguous, lookupResult.Symbols.AsImmutable(), ImmutableArray.Create(AdjustBadExpressionChild(boundLeft)), CreateErrorType(rightName), hasErrors: true);
                    }
                    else if (sym.Kind == SymbolKind.Namespace)
                    {
                        return new BoundNamespaceExpression(node, (NamespaceSymbol)sym);
                    }
                    else
                    {
                        Debug.Assert(sym.Kind == SymbolKind.NamedType);
                        var type = (NamedTypeSymbol)sym;

                        if (!typeArguments.IsDefault)
                        {
                            type = ConstructNamedTypeUnlessTypeArgumentOmitted(right, type, typeArgumentsSyntax, typeArguments, diagnostics);
                        }

                        ReportDiagnosticsIfObsolete(diagnostics, type, node, hasBaseReceiver: false);

                        return new BoundTypeExpression(node, null, type);
                    }
                }
                else if (lookupResult.Kind == LookupResultKind.WrongArity)
                {
                    Debug.Assert(symbols.Count > 0);
                    Debug.Assert(symbols[0].Kind == SymbolKind.NamedType);

                    Error(diagnostics, lookupResult.Error, right);

                    return new BoundTypeExpression(node, null,
                                new ExtendedErrorTypeSymbol(GetContainingNamespaceOrNonExtensionType(symbols[0]), symbols.ToImmutable(), lookupResult.Kind, lookupResult.Error, rightArity));
                }
                else if (lookupResult.Kind == LookupResultKind.Empty)
                {
                    Debug.Assert(lookupResult.IsClear, "If there's a legitimate reason for having candidates without a reason, then we should produce something intelligent in such cases.");
                    Debug.Assert(lookupResult.Error == null);
                    NotFound(node, rightName, rightArity, rightName, diagnostics, aliasOpt: null, qualifierOpt: ns, options: options);

                    return new BoundBadExpression(node, lookupResult.Kind, symbols.AsImmutable(), ImmutableArray.Create(AdjustBadExpressionChild(boundLeft)), CreateErrorType(rightName), hasErrors: true);
                }

                return null;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            BoundExpression tryBindMemberAccessWithBoundTypeLeft(
                ExpressionSyntax node,
                BoundExpression boundLeft,
                SimpleNameSyntax right,
                bool invoked,
                bool indexed,
                BindingDiagnosticBag diagnostics,
                TypeSymbol leftType,
                LookupResult lookupResult,
                LookupOptions options,
                SeparatedSyntaxList<TypeSyntax> typeArgumentsSyntax,
                ImmutableArray<TypeWithAnnotations> typeArguments,
                string rightName,
                int rightArity)
            {
                Debug.Assert(boundLeft is BoundTypeExpression);
                Debug.Assert((object)leftType != null);
                if (leftType.TypeKind == TypeKind.TypeParameter)
                {
                    CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                    this.LookupMembersWithFallback(lookupResult, leftType, rightName, rightArity, ref useSiteInfo, basesBeingResolved: null, options: options | LookupOptions.MustNotBeInstance | LookupOptions.MustBeAbstractOrVirtual);
                    diagnostics.Add(right, useSiteInfo);
                    if (lookupResult.IsMultiViable)
                    {
                        CheckFeatureAvailability(boundLeft.Syntax, MessageID.IDS_FeatureStaticAbstractMembersInInterfaces, diagnostics);
                        return BindMemberOfType(node, right, rightName, rightArity, invoked, indexed, boundLeft, typeArgumentsSyntax, typeArguments, lookupResult, BoundMethodGroupFlags.None, diagnostics: diagnostics);
                    }
                    else if (lookupResult.IsClear)
                    {
                        Error(diagnostics, ErrorCode.ERR_LookupInTypeVariable, boundLeft.Syntax, leftType);
                        return BadExpression(node, LookupResultKind.NotAValue, boundLeft);
                    }
                }
                else if (this.EnclosingNameofArgument == node)
                {
                    // Support selecting an extension method from a type name in nameof(.)
                    return BindInstanceMemberAccess(node, right, boundLeft, rightName, rightArity, typeArgumentsSyntax, typeArguments, invoked, indexed, diagnostics);
                }
                else
                {
                    CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                    this.LookupMembersWithFallback(lookupResult, leftType, rightName, rightArity, ref useSiteInfo, basesBeingResolved: null, options: options);
                    diagnostics.Add(right, useSiteInfo);

                    if (lookupResult.IsMultiViable)
                    {
                        return BindMemberOfType(node, right, rightName, rightArity, invoked, indexed, boundLeft, typeArgumentsSyntax, typeArguments, lookupResult, BoundMethodGroupFlags.SearchExtensions, diagnostics: diagnostics);
                    }

                    if (!invoked)
                    {
                        var nonMethodExtensionMember = ResolveExtensionMemberAccessIfResultIsNonMethod(node, boundLeft, rightName,
                            typeArguments, diagnostics);

                        if (nonMethodExtensionMember is not null)
                        {
                            return nonMethodExtensionMember;
                        }
                    }

                    return MakeBoundMethodGroupAndCheckOmittedTypeArguments(boundLeft, rightName, typeArguments, lookupResult,
                        flags: BoundMethodGroupFlags.SearchExtensions, node, typeArgumentsSyntax, diagnostics);
                }

                return null;
            }
        }

        private void WarnOnAccessOfOffDefault(SyntaxNode node, BoundExpression boundLeft, BindingDiagnosticBag diagnostics)
        {
            if ((boundLeft is BoundDefaultLiteral || boundLeft is BoundDefaultExpression) && boundLeft.ConstantValueOpt == ConstantValue.Null &&
                Compilation.LanguageVersion < MessageID.IDS_FeatureNullableReferenceTypes.RequiredVersion())
            {
                Error(diagnostics, ErrorCode.WRN_DotOnDefault, node, boundLeft.Type);
            }
        }

        /// <summary>
        /// Create a value from the expression that can be used as a left-hand-side
        /// of a member access. This method special-cases method and property
        /// groups only. All other expressions are returned as is.
        /// </summary>
        private BoundExpression MakeMemberAccessValue(BoundExpression expr, BindingDiagnosticBag diagnostics)
        {
            switch (expr.Kind)
            {
                case BoundKind.MethodGroup:
                    {
                        var methodGroup = (BoundMethodGroup)expr;
                        CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                        var resolution = this.ResolveMethodGroup(methodGroup, analyzedArguments: null, useSiteInfo: ref useSiteInfo, options: OverloadResolution.Options.None, acceptOnlyMethods: true);
                        Debug.Assert(!resolution.IsNonMethodExtensionMember(out _));
                        diagnostics.Add(expr.Syntax, useSiteInfo);
                        if (!expr.HasAnyErrors)
                        {
                            diagnostics.AddRange(resolution.Diagnostics);

                            if (resolution.MethodGroup != null && !resolution.HasAnyErrors)
                            {
                                Debug.Assert(!resolution.IsEmpty);
                                var method = resolution.MethodGroup.Methods[0];
                                Error(diagnostics, ErrorCode.ERR_BadSKunknown, methodGroup.NameSyntax, method, MessageID.IDS_SK_METHOD.Localize());
                            }
                        }
                        expr = this.BindMemberAccessBadResult(methodGroup);
                        resolution.Free();
                        return expr;
                    }

                case BoundKind.PropertyGroup:
                    return BindIndexedPropertyAccess((BoundPropertyGroup)expr, mustHaveAllOptionalParameters: false, diagnostics: diagnostics);

                default:
                    return BindToNaturalType(expr, diagnostics);
            }
        }

#nullable enable
        // When we're binding a member access that is not invoked and the member lookup yielded no result:
        // - if an extension member lookup finds a non-method extension member, then that's the member being accessed.
        //
        // - if the extension member lookup finds a method (classic extension method compatible with the receiver or method in extension declaration;
        //   closer than any non-method extension member), then we return nothing and let the caller represent the failed member lookup with a BoundMethodGroup.
        //   Note: Such method group will be resolved specially in scenarios that can handle method groups
        //     (such as inferred local `var x = A.B;`, conversion to a delegate type `System.Action a = A.B;`).
        //     It will be an error in other scenarios.
        //
        // - if the extension member lookup is ambiguous, then we'll use an error symbol as the result of the member access.
        //
        // - if the extension member lookup finds nothing, then we return nothing and let the caller represent the failed member lookup with a BoundMethodGroup.
        internal BoundExpression? ResolveExtensionMemberAccessIfResultIsNonMethod(SyntaxNode syntax, BoundExpression receiver, string name,
            ImmutableArray<TypeWithAnnotations> typeArgumentsOpt, BindingDiagnosticBag diagnostics)
        {
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = this.GetNewCompoundUseSiteInfo(diagnostics);

            // Note: we're resolving without arguments, which means we're not treating the member access as invoked
            var resolution = this.ResolveExtension(
                syntax, name, analyzedArguments: null, receiver, typeArgumentsOpt, options: OverloadResolution.Options.None,
                returnRefKind: default, returnType: null, ref useSiteInfo, acceptOnlyMethods: false);

            diagnostics.Add(syntax, useSiteInfo);

            if (resolution.IsNonMethodExtensionMember(out Symbol? extensionMember))
            {
                Debug.Assert(typeArgumentsOpt.IsDefault);
                if (!receiver.HasErrors)
                {
                    diagnostics.AddRange(resolution.Diagnostics);
                }

                resolution.Free();

                return GetExtensionMemberAccess(syntax, receiver, extensionMember, diagnostics);
            }

            resolution.Free();
            return null;
        }

        private BoundExpression GetExtensionMemberAccess(SyntaxNode syntax, BoundExpression? receiver, Symbol extensionMember, BindingDiagnosticBag diagnostics)
        {
            MessageID.IDS_FeatureExtensions.CheckFeatureAvailability(diagnostics, syntax);
            receiver = ReplaceTypeOrValueReceiver(receiver, useType: extensionMember.IsStatic, diagnostics);

            switch (extensionMember)
            {
                case PropertySymbol propertySymbol:
                    Debug.Assert(propertySymbol.ContainingType.ExtensionParameter is not null);

                    if (receiver is not BoundTypeExpression)
                    {
                        receiver = CheckAndConvertExtensionReceiver(receiver, propertySymbol.ContainingType.ExtensionParameter, diagnostics);
                    }

                    return BindPropertyAccess(syntax, receiver, propertySymbol, diagnostics, LookupResultKind.Viable, hasErrors: false);

                case ExtendedErrorTypeSymbol errorTypeSymbol:
                    // Tracked by https://github.com/dotnet/roslyn/issues/78957 : public API, we should likely reduce (ie. do type inference and substitute) the candidates (like ToBadExpression)
                    return new BoundBadExpression(syntax, LookupResultKind.Viable, errorTypeSymbol.CandidateSymbols!, [AdjustBadExpressionChild(receiver)], CreateErrorType());

                default:
                    throw ExceptionUtilities.UnexpectedValue(extensionMember.Kind);
            }
        }
#nullable disable

        private BoundExpression BindInstanceMemberAccess(
            SyntaxNode node,
            SyntaxNode right,
            BoundExpression boundLeft,
            string rightName,
            int rightArity,
            SeparatedSyntaxList<TypeSyntax> typeArgumentsSyntax,
            ImmutableArray<TypeWithAnnotations> typeArgumentsWithAnnotations,
            bool invoked,
            bool indexed,
            BindingDiagnosticBag diagnostics,
            bool searchExtensionsIfNecessary = true)
        {
            Debug.Assert(rightArity == (typeArgumentsWithAnnotations.IsDefault ? 0 : typeArgumentsWithAnnotations.Length));
            var leftType = boundLeft.Type;

            var lookupResult = LookupResult.GetInstance();
            try
            {
                // If E is a property access, indexer access, variable, or value, the type of
                // which is T, and a member lookup of I in T with K type arguments produces a
                // match, then E.I is evaluated and classified as follows:

                // UNDONE: Classify E as prop access, indexer access, variable or value

                bool leftIsBaseReference = boundLeft.Kind == BoundKind.BaseReference;
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                this.LookupInstanceMember(lookupResult, leftType, leftIsBaseReference, rightName, rightArity, invoked, ref useSiteInfo);
                diagnostics.Add(right, useSiteInfo);

                // SPEC: Otherwise, an attempt is made to process E.I as an extension method invocation.
                // SPEC: If this fails, E.I is an invalid member reference, and a binding-time error occurs.
                searchExtensionsIfNecessary = searchExtensionsIfNecessary && !leftIsBaseReference;

                BoundMethodGroupFlags flags = BoundMethodGroupFlags.None;
                if (searchExtensionsIfNecessary)
                {
                    flags |= BoundMethodGroupFlags.SearchExtensions;
                }

                if (lookupResult.IsMultiViable)
                {
                    return BindMemberOfType(node, right, rightName, rightArity, invoked, indexed, boundLeft, typeArgumentsSyntax, typeArgumentsWithAnnotations, lookupResult, flags, diagnostics);
                }

                if (searchExtensionsIfNecessary)
                {
                    var members = ArrayBuilder<Symbol>.GetInstance();
                    boundLeft = CheckAmbiguousPrimaryConstructorParameterAsColorColorReceiver(boundLeft, right, rightName, typeArgumentsWithAnnotations, invoked, members, diagnostics);
                    members.Free();

                    if (!invoked)
                    {
                        var nonMethodExtensionMember = ResolveExtensionMemberAccessIfResultIsNonMethod(node, boundLeft, rightName,
                            typeArgumentsWithAnnotations, diagnostics);

                        if (nonMethodExtensionMember is not null)
                        {
                            return nonMethodExtensionMember;
                        }
                    }

                    return MakeBoundMethodGroupAndCheckOmittedTypeArguments(boundLeft, rightName, typeArgumentsWithAnnotations, lookupResult,
                        flags, node, typeArgumentsSyntax, diagnostics);
                }

                this.BindMemberAccessReportError(node, right, rightName, boundLeft, lookupResult.Error, diagnostics);
                return BindMemberAccessBadResult(node, rightName, boundLeft, lookupResult.Error, lookupResult.Symbols.ToImmutable(), lookupResult.Kind);
            }
            finally
            {
                lookupResult.Free();
            }
        }

        private BoundMethodGroup MakeBoundMethodGroupAndCheckOmittedTypeArguments(BoundExpression boundLeft, string rightName,
            ImmutableArray<TypeWithAnnotations> typeArgumentsWithAnnotations, LookupResult lookupResult, BoundMethodGroupFlags flags, SyntaxNode node,
            SeparatedSyntaxList<TypeSyntax> typeArgumentsSyntax, BindingDiagnosticBag diagnostics)
        {
            var boundMethodGroup = new BoundMethodGroup(
                node,
                typeArgumentsWithAnnotations,
                boundLeft,
                rightName,
                lookupResult.Symbols.All(s => s.Kind == SymbolKind.Method) ? lookupResult.Symbols.SelectAsArray(s_toMethodSymbolFunc) : ImmutableArray<MethodSymbol>.Empty,
                lookupResult,
                flags,
                this);

            if (!boundMethodGroup.HasErrors && typeArgumentsSyntax.Any(SyntaxKind.OmittedTypeArgument))
            {
                Error(diagnostics, ErrorCode.ERR_OmittedTypeArgument, node);
            }

            return boundMethodGroup;
        }

        private void LookupInstanceMember(LookupResult lookupResult, TypeSymbol leftType, bool leftIsBaseReference, string rightName, int rightArity, bool invoked, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            LookupOptions options = LookupOptions.AllMethodsOnArityZero;
            if (invoked)
            {
                options |= LookupOptions.MustBeInvocableIfMember;
            }

            if (leftIsBaseReference)
            {
                options |= LookupOptions.UseBaseReferenceAccessibility;
            }

            this.LookupMembersWithFallback(lookupResult, leftType, rightName, rightArity, ref useSiteInfo, basesBeingResolved: null, options: options);
        }

        private void BindMemberAccessReportError(BoundMethodGroup node, BindingDiagnosticBag diagnostics)
        {
            var nameSyntax = node.NameSyntax;
            var syntax = node.MemberAccessExpressionSyntax ?? nameSyntax;
            this.BindMemberAccessReportError(syntax, nameSyntax, node.Name, node.ReceiverOpt, node.LookupError, diagnostics);
        }

        /// <summary>
        /// Report the error from member access lookup. Or, if there
        /// was no explicit error from lookup, report "no such member".
        /// </summary>
        private void BindMemberAccessReportError(
            SyntaxNode node,
            SyntaxNode name,
            string plainName,
            BoundExpression boundLeft,
            DiagnosticInfo lookupError,
            BindingDiagnosticBag diagnostics)
        {
            if (boundLeft.HasAnyErrors && boundLeft.Kind != BoundKind.TypeOrValueExpression)
            {
                return;
            }

            if (lookupError != null)
            {
                // CONSIDER: there are some cases where Dev10 uses the span of "node",
                // rather than "right".
                diagnostics.Add(new CSDiagnostic(lookupError, name.Location));
            }
            else if (node.IsQuery())
            {
                ReportQueryLookupFailed(node, boundLeft, plainName, ImmutableArray<Symbol>.Empty, diagnostics);
            }
            else
            {
                if ((object)boundLeft.Type == null)
                {
                    Error(diagnostics, ErrorCode.ERR_NoSuchMember, name, boundLeft.Display, plainName);
                }
                else if (boundLeft.Kind == BoundKind.TypeExpression ||
                    boundLeft.Kind == BoundKind.BaseReference ||
                    (node.Kind() == SyntaxKind.AwaitExpression && plainName == WellKnownMemberNames.GetResult) ||
                    (Flags.Includes(BinderFlags.CollectionExpressionConversionValidation | BinderFlags.CollectionInitializerAddMethod) && name is ParameterSyntax))
                {
                    Error(diagnostics, ErrorCode.ERR_NoSuchMember, name, boundLeft.Type, plainName);
                }
                else if (WouldUsingSystemFindExtension(boundLeft.Type, plainName))
                {
                    Error(diagnostics, ErrorCode.ERR_NoSuchMemberOrExtensionNeedUsing, name, boundLeft.Type, plainName, "System");
                }
                else if (boundLeft.Kind == BoundKind.AwaitableValuePlaceholder && boundLeft.Type.IsIAsyncEnumerableType(Compilation))
                {
                    Error(diagnostics, ErrorCode.ERR_NoAwaitOnAsyncEnumerable, name, boundLeft.Type, plainName);
                }
                else
                {
                    Error(diagnostics, ErrorCode.ERR_NoSuchMemberOrExtension, name, boundLeft.Type, plainName);
                }
            }
        }

        private bool WouldUsingSystemFindExtension(TypeSymbol receiver, string methodName)
        {
            // we have a special case to make the diagnostic for await expressions more clear for Windows:
            // if the receiver type is a windows RT async interface and the method name is GetAwaiter,
            // then we would suggest a using directive for "System".
            // TODO: we should check if such a using directive would actually help, or if there is already one in scope.
            return methodName == WellKnownMemberNames.GetAwaiter && ImplementsWinRTAsyncInterface(receiver);
        }

        /// <summary>
        /// Return true if the given type is or implements a WinRTAsyncInterface.
        /// </summary>
        private bool ImplementsWinRTAsyncInterface(TypeSymbol type)
        {
            return IsWinRTAsyncInterface(type) || type.AllInterfacesNoUseSiteDiagnostics.Any(static (i, self) => self.IsWinRTAsyncInterface(i), this);
        }

        private bool IsWinRTAsyncInterface(TypeSymbol type)
        {
            if (!type.IsInterfaceType())
            {
                return false;
            }

            var namedType = ((NamedTypeSymbol)type).ConstructedFrom;
            return
                TypeSymbol.Equals(namedType, Compilation.GetWellKnownType(WellKnownType.Windows_Foundation_IAsyncAction), TypeCompareKind.ConsiderEverything2) ||
                TypeSymbol.Equals(namedType, Compilation.GetWellKnownType(WellKnownType.Windows_Foundation_IAsyncActionWithProgress_T), TypeCompareKind.ConsiderEverything2) ||
                TypeSymbol.Equals(namedType, Compilation.GetWellKnownType(WellKnownType.Windows_Foundation_IAsyncOperation_T), TypeCompareKind.ConsiderEverything2) ||
                TypeSymbol.Equals(namedType, Compilation.GetWellKnownType(WellKnownType.Windows_Foundation_IAsyncOperationWithProgress_T2), TypeCompareKind.ConsiderEverything2);
        }

        private BoundExpression BindMemberAccessBadResult(BoundMethodGroup node)
        {
            var nameSyntax = node.NameSyntax;
            var syntax = node.MemberAccessExpressionSyntax ?? nameSyntax;
            return this.BindMemberAccessBadResult(syntax, node.Name, node.ReceiverOpt, node.LookupError, StaticCast<Symbol>.From(node.Methods), node.ResultKind);
        }

        /// <summary>
        /// Return a BoundExpression representing the invalid member.
        /// </summary>
        private BoundExpression BindMemberAccessBadResult(
            SyntaxNode node,
            string nameString,
            BoundExpression boundLeft,
            DiagnosticInfo lookupError,
            ImmutableArray<Symbol> symbols,
            LookupResultKind lookupKind)
        {
            if (symbols.Length > 0 && symbols[0].Kind == SymbolKind.Method)
            {
                var builder = ArrayBuilder<MethodSymbol>.GetInstance();
                foreach (var s in symbols)
                {
                    var m = s as MethodSymbol;
                    if ((object)m != null) builder.Add(m);
                }
                var methods = builder.ToImmutableAndFree();

                // Expose the invalid methods as a BoundMethodGroup.
                // Since we do not want to perform further method
                // lookup, searchExtensionMethods is set to false.
                // Don't bother calling ConstructBoundMethodGroupAndReportOmittedTypeArguments -
                // we've reported other errors.
                return new BoundMethodGroup(
                    node,
                    default(ImmutableArray<TypeWithAnnotations>),
                    nameString,
                    methods,
                    methods.Length == 1 ? methods[0] : null,
                    lookupError,
                    flags: BoundMethodGroupFlags.None,
                    functionType: null,
                    receiverOpt: ReplaceTypeOrValueReceiver(boundLeft, useType: true, diagnostics: BindingDiagnosticBag.Discarded),
                    resultKind: lookupKind,
                    hasErrors: true);
            }

            var symbolOpt = symbols.Length == 1 ? symbols[0] : null;
            return new BoundBadExpression(
                node,
                lookupKind,
                (object)symbolOpt == null ? ImmutableArray<Symbol>.Empty : ImmutableArray.Create(symbolOpt),
                boundLeft == null ? ImmutableArray<BoundExpression>.Empty : ImmutableArray.Create(AdjustBadExpressionChild(BindToTypeForErrorRecovery(boundLeft))),
                GetNonMethodMemberType(symbolOpt));
        }

        private TypeSymbol GetNonMethodMemberType(Symbol symbolOpt)
        {
            TypeSymbol resultType = null;
            if ((object)symbolOpt != null)
            {
                switch (symbolOpt.Kind)
                {
                    case SymbolKind.Field:
                        resultType = ((FieldSymbol)symbolOpt).GetFieldType(this.FieldsBeingBound).Type;
                        break;
                    case SymbolKind.Property:
                        resultType = ((PropertySymbol)symbolOpt).Type;
                        break;
                    case SymbolKind.Event:
                        resultType = ((EventSymbol)symbolOpt).Type;
                        break;
                }
            }
            return resultType ?? CreateErrorType();
        }

#nullable enable 

        /// <summary>
        /// Combine the receiver and arguments of an extension method
        /// invocation into a single argument list to allow overload resolution
        /// to treat the invocation as a static method invocation with no receiver.
        /// </summary>
        private static void CombineExtensionMethodArguments(BoundExpression receiver, AnalyzedArguments? originalArguments, AnalyzedArguments extensionMethodArguments)
        {
            Debug.Assert(receiver != null);
            Debug.Assert(extensionMethodArguments.Arguments.Count == 0);
            Debug.Assert(extensionMethodArguments.Names.Count == 0);
            Debug.Assert(extensionMethodArguments.RefKinds.Count == 0);

            extensionMethodArguments.IncludesReceiverAsArgument = true;
            extensionMethodArguments.Arguments.Add(receiver);

            if (originalArguments is not null)
            {
                extensionMethodArguments.Arguments.AddRange(originalArguments.Arguments);
            }

            if (originalArguments?.Names.Count > 0)
            {
                extensionMethodArguments.Names.Add(null);
                extensionMethodArguments.Names.AddRange(originalArguments.Names);
            }

            if (originalArguments?.RefKinds.Count > 0)
            {
                extensionMethodArguments.RefKinds.Add(RefKind.None);
                extensionMethodArguments.RefKinds.AddRange(originalArguments.RefKinds);
            }
        }

#nullable disable

        private static void InitializeExtensionPropertyArguments(BoundExpression receiver, AnalyzedArguments extensionPropertyArguments)
        {
            Debug.Assert(receiver != null);
            Debug.Assert(extensionPropertyArguments.Arguments.Count == 0);
            Debug.Assert(extensionPropertyArguments.Names.Count == 0);
            Debug.Assert(extensionPropertyArguments.RefKinds.Count == 0);

            extensionPropertyArguments.IncludesReceiverAsArgument = true;
            extensionPropertyArguments.Arguments.Add(receiver);
        }

        /// <summary>
        /// Binds a static or instance member access.
        /// </summary>
        private BoundExpression BindMemberOfType(
            SyntaxNode node,
            SyntaxNode right,
            string plainName,
            int arity,
            bool invoked,
            bool indexed,
            BoundExpression left,
            SeparatedSyntaxList<TypeSyntax> typeArgumentsSyntax,
            ImmutableArray<TypeWithAnnotations> typeArgumentsWithAnnotations,
            LookupResult lookupResult,
            BoundMethodGroupFlags methodGroupFlags,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            Debug.Assert(left != null);
            Debug.Assert(lookupResult.IsMultiViable);
            Debug.Assert(lookupResult.Symbols.Any());

            var members = ArrayBuilder<Symbol>.GetInstance();
            BoundExpression result;
            bool wasError;
            Symbol symbol = GetSymbolOrMethodOrPropertyGroup(lookupResult, right, plainName, arity, members, diagnostics, out wasError,
                                                             qualifierOpt: left is BoundTypeExpression typeExpr ? typeExpr.Type : null);

            if ((object)symbol == null)
            {
                Debug.Assert(members.Count > 0);

                // If I identifies one or more methods, then the result is a method group with
                // no associated instance expression. If a type argument list was specified, it
                // is used in calling a generic method.

                // (Note that for static methods, we are stashing away the type expression in
                // the receiver of the method group, even though the spec notes that there is
                // no associated instance expression.)

                left = CheckAmbiguousPrimaryConstructorParameterAsColorColorReceiver(left, right, plainName, typeArgumentsWithAnnotations, invoked, members, diagnostics);

                result = ConstructBoundMemberGroupAndReportOmittedTypeArguments(
                    node,
                    typeArgumentsSyntax,
                    typeArgumentsWithAnnotations,
                    left,
                    plainName,
                    members,
                    lookupResult,
                    methodGroupFlags,
                    wasError,
                    diagnostics);
            }
            else
            {
                Debug.Assert(symbol.Kind != SymbolKind.Method);
                left = ReplaceTypeOrValueReceiver(left, symbol.IsStatic || symbol.Kind == SymbolKind.NamedType, diagnostics);

                // Events are handled later as we don't know yet if we are binding to the event or it's backing field.
                // Properties are handled in BindPropertyAccess
                if (symbol.Kind is not (SymbolKind.Event or SymbolKind.Property))
                {
                    ReportDiagnosticsIfObsolete(diagnostics, symbol, node, hasBaseReceiver: left.Kind == BoundKind.BaseReference);
                }

                switch (symbol.Kind)
                {
                    case SymbolKind.NamedType:
                    case SymbolKind.ErrorType:
                        if (IsInstanceReceiver(left) == true && !wasError)
                        {
                            // CS0572: 'B': cannot reference a type through an expression; try 'A.B' instead
                            Error(diagnostics, ErrorCode.ERR_BadTypeReference, right, plainName, symbol);
                            wasError = true;
                        }

                        // If I identifies a type, then the result is that type constructed with
                        // the given type arguments.
                        var type = (NamedTypeSymbol)symbol;
                        if (!typeArgumentsWithAnnotations.IsDefault)
                        {
                            type = ConstructNamedTypeUnlessTypeArgumentOmitted(right, type, typeArgumentsSyntax, typeArgumentsWithAnnotations, diagnostics);
                        }

                        result = new BoundTypeExpression(
                            syntax: node,
                            aliasOpt: null,
                            boundContainingTypeOpt: left as BoundTypeExpression,
                            boundDimensionsOpt: ImmutableArray<BoundExpression>.Empty,
                            typeWithAnnotations: TypeWithAnnotations.Create(type));
                        break;

                    case SymbolKind.Property:
                        // If I identifies a static property, then the result is a property
                        // access with no associated instance expression.
                        result = BindPropertyAccess(node, left, (PropertySymbol)symbol, diagnostics, lookupResult.Kind, hasErrors: wasError);
                        break;

                    case SymbolKind.Event:
                        // If I identifies a static event, then the result is an event
                        // access with no associated instance expression.
                        result = BindEventAccess(node, left, (EventSymbol)symbol, diagnostics, lookupResult.Kind, hasErrors: wasError);
                        break;

                    case SymbolKind.Field:
                        // If I identifies a static field:
                        // UNDONE: If the field is readonly and the reference occurs outside the static constructor of 
                        // UNDONE: the class or struct in which the field is declared, then the result is a value, namely
                        // UNDONE: the value of the static field I in E.
                        // UNDONE: Otherwise, the result is a variable, namely the static field I in E.
                        // UNDONE: Need a way to mark an expression node as "I am a variable, not a value".
                        result = BindFieldAccess(node, left, (FieldSymbol)symbol, diagnostics, lookupResult.Kind, indexed, hasErrors: wasError);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
                }
            }

            members.Free();
            return result;
        }

#nullable enable
        protected MethodGroupResolution ResolveExtension(
            SyntaxNode expression,
            string memberName,
            AnalyzedArguments? analyzedArguments,
            BoundExpression left,
            ImmutableArray<TypeWithAnnotations> typeArgumentsWithAnnotations,
            OverloadResolution.Options options,
            RefKind returnRefKind,
            TypeSymbol? returnType,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
            bool acceptOnlyMethods,
            in CallingConventionInfo callingConvention = default)
        {
            Debug.Assert(left.Type is not null);
            Debug.Assert(!left.Type.IsDynamic());
            Debug.Assert((options & ~(OverloadResolution.Options.IsMethodGroupConversion |
                                      OverloadResolution.Options.IsFunctionPointerResolution |
                                      OverloadResolution.Options.InferWithDynamic |
                                      OverloadResolution.Options.IgnoreNormalFormIfHasValidParamsParameter |
                                      OverloadResolution.Options.DisallowExpandedNonArrayParams |
                                      OverloadResolution.Options.DynamicResolution |
                                      OverloadResolution.Options.DynamicConvertsToAnything)) == 0);

            var firstResult = new MethodGroupResolution();
            var lookupResult = LookupResult.GetInstance();
            var classicExtensionLookupResult = LookupResult.GetInstance();
            var diagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, useSiteInfo.AccumulatesDependencies);
            AnalyzedArguments? actualMethodArguments = null;
            AnalyzedArguments? actualReceiverArguments = null;

            int arity = typeArgumentsWithAnnotations.IsDefault ? 0 : typeArgumentsWithAnnotations.Length;
            var lookupOptions = (arity == 0) ? LookupOptions.AllMethodsOnArityZero : LookupOptions.Default;
            if (analyzedArguments is not null)
            {
                lookupOptions |= LookupOptions.MustBeInvocableIfMember;
            }

            if (this.AllowRefOmittedArguments(left))
            {
                options |= OverloadResolution.Options.AllowRefOmittedArguments;
            }

            foreach (var scope in new ExtensionScopes(this))
            {
                lookupResult.Clear();
                classicExtensionLookupResult.Clear();
                diagnostics.Clear();

                if (tryResolveExtensionInScope(
                    expression, left, memberName, arity,
                    typeArgumentsWithAnnotations, returnType, returnRefKind, lookupResult,
                    analyzedArguments, ref actualMethodArguments, ref actualReceiverArguments, ref useSiteInfo, ref firstResult,
                    options, callingConvention, classicExtensionLookupResult, lookupOptions, binder: this, scope: scope, diagnostics: diagnostics,
                    acceptOnlyMethods: acceptOnlyMethods,
                    result: out MethodGroupResolution result))
                {
                    lookupResult.Free();
                    classicExtensionLookupResult.Free();
                    diagnostics.Free();
                    actualReceiverArguments?.Free();

                    if (result.AnalyzedArguments != actualMethodArguments)
                    {
                        actualMethodArguments?.Free();
                    }

                    return result;
                }
            }

            lookupResult.Free();
            classicExtensionLookupResult.Free();
            diagnostics.Free();
            actualReceiverArguments?.Free();

            if (firstResult.AnalyzedArguments != actualMethodArguments)
            {
                actualMethodArguments?.Free();
            }

            return firstResult;

            static bool tryResolveExtensionInScope(
                SyntaxNode expression,
                BoundExpression left,
                string memberName,
                int arity,
                ImmutableArray<TypeWithAnnotations> typeArgumentsWithAnnotations,
                TypeSymbol? returnType,
                RefKind returnRefKind,
                LookupResult lookupResult,
                AnalyzedArguments? analyzedArguments,
                ref AnalyzedArguments? actualMethodArguments,
                ref AnalyzedArguments? actualReceiverArguments,
                ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
                ref MethodGroupResolution firstResult,
                OverloadResolution.Options options,
                CallingConventionInfo callingConvention,
                LookupResult classicExtensionLookupResult,
                LookupOptions lookupOptions,
                Binder binder,
                ExtensionScope scope,
                bool acceptOnlyMethods,
                BindingDiagnosticBag diagnostics,
                out MethodGroupResolution result)
            {
                Debug.Assert(left.Type is not null);
                result = default;

                // 1. gather candidates
                CompoundUseSiteInfo<AssemblySymbol> classicExtensionUseSiteInfo = binder.GetNewCompoundUseSiteInfo(diagnostics);
                scope.Binder.LookupAllExtensionMembersInSingleBinder(
                    lookupResult, memberName, arity, lookupOptions,
                    originalBinder: binder, useSiteInfo: ref useSiteInfo, classicExtensionUseSiteInfo: ref classicExtensionUseSiteInfo);

                diagnostics.Add(expression, classicExtensionUseSiteInfo);

                if (!lookupResult.IsMultiViable)
                {
                    return false;
                }

                // 2. resolve methods
                MethodGroupResolution methodResult = resolveMethods(expression, left, typeArgumentsWithAnnotations, returnType, returnRefKind,
                    lookupResult, analyzedArguments, ref actualMethodArguments, options, in callingConvention, binder, diagnostics);

                // 3. resolve properties
                Debug.Assert(arity == 0 || lookupResult.Symbols.All(s => s.Kind != SymbolKind.Property));

                // Tracked by https://github.com/dotnet/roslyn/issues/78827 : MQ, Regarding 'acceptOnlyMethods', consider if it would be better to add a special 'LookupOptions' value to filter out properties during lookup
                OverloadResolutionResult<PropertySymbol>? propertyResult = arity != 0 || acceptOnlyMethods ? null : resolveProperties(left, lookupResult, binder, ref actualReceiverArguments, ref useSiteInfo);

                // 4. determine member kind
                if (!methodResult.HasAnyApplicableMethod && propertyResult?.HasAnyApplicableMember != true)
                {
                    // Found nothing applicable. Store aside the first non-applicable result and continue searching for an applicable result.
                    if (firstResult.IsEmpty)
                    {
                        if (propertyResult != null)
                        {
                            Debug.Assert(actualReceiverArguments is not null);
                            firstResult = makeErrorResult(methodResult, propertyResult, expression, left, memberName, arity, lookupResult, actualReceiverArguments, binder, diagnostics);
                            methodResult.Free(keepArguments: true);
                            propertyResult.Free();
                        }
                        else
                        {
                            firstResult = methodResult;
                        }
                    }

                    return false;
                }

                if (methodResult.HasAnyApplicableMethod)
                {
                    // If the search in the current scope resulted in any applicable method (regardless of whether a best
                    // applicable method could be determined) then our search is complete.
                    if (propertyResult?.HasAnyApplicableMember != true)
                    {
                        // methods win
                        propertyResult?.Free();
                        result = methodResult;
                        return true;
                    }

                    // ambiguous between methods and properties
                    Debug.Assert(actualReceiverArguments is not null);
                    result = makeErrorResult(methodResult, propertyResult, expression, left, memberName, arity, lookupResult, actualReceiverArguments, binder, diagnostics);
                    methodResult.Free(keepArguments: true);
                    propertyResult?.Free();
                    return true;
                }

                // If the search in the current scope resulted in any applicable property (regardless of whether a best
                // applicable property could be determined) then our search is complete.
                Debug.Assert(propertyResult?.HasAnyApplicableMember == true);
                if (propertyResult.Succeeded && propertyResult.BestResult.Member is { } bestProperty)
                {
                    // property wins
                    methodResult.Free(keepArguments: true);
                    propertyResult.Free();
                    result = new MethodGroupResolution(bestProperty, LookupResultKind.Viable, diagnostics.ToReadOnly());
                    return true;
                }

                // ambiguous between multiple applicable properties
                Debug.Assert(actualReceiverArguments is not null);
                result = makeErrorResult(methodResult, propertyResult, expression, left, memberName, arity, lookupResult, actualReceiverArguments, binder, diagnostics);
                methodResult.Free(keepArguments: true);
                propertyResult.Free();
                return true;
            }

            static MethodGroupResolution resolveMethods(
                SyntaxNode expression,
                BoundExpression left,
                ImmutableArray<TypeWithAnnotations> typeArgumentsWithAnnotations,
                TypeSymbol? returnType,
                RefKind returnRefKind,
                LookupResult lookupResult,
                AnalyzedArguments? analyzedArguments,
                ref AnalyzedArguments? actualMethodArguments,
                OverloadResolution.Options options,
                in CallingConventionInfo callingConvention,
                Binder binder,
                BindingDiagnosticBag diagnostics)
            {
                var methodGroup = MethodGroup.GetInstance();
                methodGroup.PopulateWithExtensionMethods(left, lookupResult.Symbols, typeArgumentsWithAnnotations, resultKind: lookupResult.Kind);

                if (analyzedArguments == null)
                {
                    // Without arguments (for scenarios such as `nameof` or conversion to non-delegate/dynamic type)
                    // we can still prune the inapplicable extension methods using the receiver type
                    for (int i = methodGroup.Methods.Count - 1; i >= 0; i--)
                    {
                        MethodSymbol method = methodGroup.Methods[i];
                        TypeSymbol? receiverType = left.Type;
                        Debug.Assert(receiverType is not null);

                        bool inapplicable = false;
                        if (method.IsExtensionMethod
                            && method.ReduceExtensionMethod(receiverType, binder.Compilation) is null)
                        {
                            inapplicable = true;
                        }
                        else if (method.IsExtensionBlockMember()
                            && SourceNamedTypeSymbol.ReduceExtensionMember(binder.Compilation, method, receiverType, wasExtensionFullyInferred: out _) is null)
                        {
                            inapplicable = true;
                        }

                        if (inapplicable)
                        {
                            methodGroup.Methods.RemoveAt(i);
                        }
                    }

                    if (methodGroup.Methods.Count != 0)
                    {
                        return new MethodGroupResolution(methodGroup, diagnostics.ToReadOnly());
                    }
                }

                if (methodGroup.Methods.Count == 0)
                {
                    methodGroup.Free();
                    return default;
                }

                if (actualMethodArguments == null)
                {
                    // Create a set of arguments for overload resolution including the receiver.
                    actualMethodArguments = AnalyzedArguments.GetInstance();
                    CombineExtensionMethodArguments(left, analyzedArguments, actualMethodArguments);
                }

                var overloadResolutionResult = OverloadResolutionResult<MethodSymbol>.GetInstance();
                CompoundUseSiteInfo<AssemblySymbol> overloadResolutionUseSiteInfo = binder.GetNewCompoundUseSiteInfo(diagnostics);
                binder.OverloadResolution.MethodInvocationOverloadResolution(
                    methods: methodGroup.Methods,
                    typeArguments: methodGroup.TypeArguments,
                    receiver: methodGroup.Receiver,
                    arguments: actualMethodArguments,
                    result: overloadResolutionResult,
                    ref overloadResolutionUseSiteInfo,
                    options: options | OverloadResolution.Options.IsExtensionMethodResolution,
                    returnRefKind: returnRefKind,
                    returnType: returnType,
                    in callingConvention);

                diagnostics.Add(expression, overloadResolutionUseSiteInfo);

                // Note: the MethodGroupResolution instance is responsible for freeing the method group,
                //   the overload resolution result and the arguments
                return new MethodGroupResolution(methodGroup, null, overloadResolutionResult, actualMethodArguments, methodGroup.ResultKind, diagnostics.ToReadOnly());
            }

            // The caller is responsible for freeing the result
            static OverloadResolutionResult<PropertySymbol>? resolveProperties(
                BoundExpression left,
                LookupResult lookupResult,
                Binder binder,
                ref AnalyzedArguments? actualReceiverArguments,
                ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
            {
                ArrayBuilder<PropertySymbol>? properties = null;
                foreach (var member in lookupResult.Symbols)
                {
                    if (member is PropertySymbol property)
                    {
                        properties ??= ArrayBuilder<PropertySymbol>.GetInstance();
                        properties.Add(property);
                    }
                }

                if (properties is null)
                {
                    return null;
                }

                if (actualReceiverArguments == null)
                {
                    actualReceiverArguments = AnalyzedArguments.GetInstance();
                    InitializeExtensionPropertyArguments(left, actualReceiverArguments);
                }

                OverloadResolutionResult<PropertySymbol> overloadResolutionResult = OverloadResolutionResult<PropertySymbol>.GetInstance();

                binder.OverloadResolution.PropertyOverloadResolution(properties, left, actualReceiverArguments, overloadResolutionResult,
                    allowRefOmittedArguments: binder.AllowRefOmittedArguments(left), dynamicResolution: actualReceiverArguments.HasDynamicArgument, ref useSiteInfo);

                properties.Free();
                return overloadResolutionResult;
            }

            static MethodGroupResolution makeErrorResult(
                MethodGroupResolution methodResult,
                OverloadResolutionResult<PropertySymbol> propertyResult,
                SyntaxNode expression,
                BoundExpression left,
                string memberName,
                int arity,
                LookupResult lookupResult,
                AnalyzedArguments actualReceiverArguments,
                Binder binder,
                BindingDiagnosticBag diagnostics)
            {
                Debug.Assert(propertyResult is not null);
                ImmutableArray<Symbol> symbols = lookupResult.Symbols.ToImmutable();

                DiagnosticInfo errorInfo;
                if (methodResult.HasAnyApplicableMethod && propertyResult.HasAnyApplicableMember)
                {
                    MethodSymbol representativeMethod = methodResult.OverloadResolutionResult is { } methodResolution
                        ? methodResolution.PickRepresentativeMember()
                        : methodResult.MethodGroup.Methods[0];

                    PropertySymbol representativeProperty = propertyResult.PickRepresentativeMember();

                    errorInfo = OverloadResolutionResult<Symbol>.CreateAmbiguousCallDiagnosticInfo(binder.Compilation, representativeMethod, representativeProperty, symbols, isExtension: true);

                    diagnostics.Add(errorInfo, expression.Location);
                }
                else
                {
                    propertyResult.ReportDiagnostics(binder, expression.Location, expression, diagnostics, memberName, left, left.Syntax, actualReceiverArguments, symbols,
                        typeContainingConstructor: null, delegateTypeBeingInvoked: null, isMethodGroupConversion: false, isExtension: true);

                    errorInfo = new CSDiagnosticInfo(ErrorCode.ERR_ExtensionResolutionFailed, left.Type, memberName);
                }

                ExtendedErrorTypeSymbol resultSymbol = new ExtendedErrorTypeSymbol(containingSymbol: null, symbols, LookupResultKind.OverloadResolutionFailure, errorInfo, arity);
                Debug.Assert(lookupResult.Kind == LookupResultKind.Viable);
                return new MethodGroupResolution(resultSymbol, lookupResult.Kind, diagnostics.ToReadOnly());
            }
        }

        private bool AllowRefOmittedArguments(BoundExpression receiver)
        {
            // We don't consider when we're in default parameter values or attribute arguments so that we avoid cycles. This is an error scenario,
            // so we don't care if we accidentally miss a parameter being applicable.
            return !InParameterDefaultValue && !InAttributeArgument && receiver.IsExpressionOfComImportType();
        }
#nullable disable

        protected BoundExpression BindFieldAccess(
            SyntaxNode node,
            BoundExpression receiver,
            FieldSymbol fieldSymbol,
            BindingDiagnosticBag diagnostics,
            LookupResultKind resultKind,
            bool indexed,
            bool hasErrors)
        {
            bool hasError = false;
            NamedTypeSymbol type = fieldSymbol.ContainingType;
            var isEnumField = (fieldSymbol.IsStatic && type.IsEnumType());

            if (isEnumField && !type.IsValidEnumType())
            {
                Error(diagnostics, ErrorCode.ERR_BindToBogus, node, fieldSymbol);
                hasError = true;
            }

            if (!hasError)
            {
                hasError = this.CheckInstanceOrStatic(node, receiver, fieldSymbol, ref resultKind, diagnostics);
            }

            if (!hasError && fieldSymbol.IsFixedSizeBuffer && !IsInsideNameof)
            {
                // SPEC: In a member access of the form E.I, if E is of a struct type and a member lookup of I in
                // that struct type identifies a fixed size member, then E.I is evaluated and classified as follows:
                // * If the expression E.I does not occur in an unsafe context, a compile-time error occurs.
                // * If E is classified as a value, a compile-time error occurs.
                // * Otherwise, if E is a moveable variable and the expression E.I is not a fixed_pointer_initializer,
                //   a compile-time error occurs.
                // * Otherwise, E references a fixed variable and the result of the expression is a pointer to the
                //   first element of the fixed size buffer member I in E. The result is of type S*, where S is
                //   the element type of I, and is classified as a value.

                TypeSymbol receiverType = receiver.Type;

                // Reflect errors that have been reported elsewhere...
                hasError = (object)receiverType == null || !receiverType.IsValueType;

                if (!hasError)
                {
                    var isFixedStatementExpression = SyntaxFacts.IsFixedStatementExpression(node);

                    if (IsMoveableVariable(receiver, accessedLocalOrParameterOpt: out _) != isFixedStatementExpression)
                    {
                        if (indexed)
                        {
                            // SPEC C# 7.3: If the fixed size buffer access is the receiver of an element_access_expression,
                            // E may be either fixed or moveable
                            CheckFeatureAvailability(node, MessageID.IDS_FeatureIndexingMovableFixedBuffers, diagnostics);
                        }
                        else
                        {
                            Error(diagnostics, isFixedStatementExpression ? ErrorCode.ERR_FixedNotNeeded : ErrorCode.ERR_FixedBufferNotFixed, node);
                            hasErrors = hasError = true;
                        }
                    }
                }

                if (!hasError)
                {
                    hasError = !CheckValueKind(node, receiver, BindValueKind.FixedReceiver, checkingReceiver: false, diagnostics: diagnostics);
                }
            }

            ConstantValue constantValueOpt = null;

            if (fieldSymbol.IsConst && !IsInsideNameof)
            {
                constantValueOpt = fieldSymbol.GetConstantValue(this.ConstantFieldsInProgress, this.IsEarlyAttributeBinder);
                if (constantValueOpt == ConstantValue.Unset)
                {
                    // Evaluating constant expression before dependencies
                    // have been evaluated. Treat this as a Bad value.
                    constantValueOpt = ConstantValue.Bad;
                }
            }

            if (!fieldSymbol.IsStatic)
            {
                WarnOnAccessOfOffDefault(node, receiver, diagnostics);
            }

            if (!IsBadBaseAccess(node, receiver, fieldSymbol, diagnostics))
            {
                CheckReceiverAndRuntimeSupportForSymbolAccess(node, receiver, fieldSymbol, diagnostics);
            }

            // If this is a ref field from another compilation, check for support for ref fields.
            // No need to check for a reference to a field declared in this compilation since
            // we check at the declaration site. (Check RefKind after checking compilation to
            // avoid cycles for source symbols.)
            if ((object)Compilation.SourceModule != fieldSymbol.OriginalDefinition.ContainingModule &&
                fieldSymbol.RefKind != RefKind.None)
            {
                CheckFeatureAvailability(node, MessageID.IDS_FeatureRefFields, diagnostics);
                if (!Compilation.Assembly.RuntimeSupportsByRefFields)
                {
                    diagnostics.Add(ErrorCode.ERR_RuntimeDoesNotSupportRefFields, node.Location);
                }
            }

            // The type calculation here should be kept in sync with logic in BindLeftIdentifierOfPotentialColorColorMemberAccess.
            TypeSymbol fieldType = fieldSymbol.GetFieldType(this.FieldsBeingBound).Type;
            BoundExpression expr = new BoundFieldAccess(node, receiver, fieldSymbol, constantValueOpt, resultKind, fieldType, hasErrors: (hasErrors || hasError));

            // Spec 14.3: "Within an enum member initializer, values of other enum members are
            // always treated as having the type of their underlying type"

            // The type calculation here should be kept in sync with logic in BindLeftIdentifierOfPotentialColorColorMemberAccess.
            if (GetAdjustedTypeForEnumMemberReference(fieldSymbol, fieldType) is { } underlyingType)
            {
                expr = new BoundConversion(
                    node,
                    expr,
                    Conversion.ImplicitNumeric,
                    @checked: true,
                    explicitCastInCode: false,
                    conversionGroupOpt: null,
                    InConversionGroupFlags.Unspecified,
                    constantValueOpt: expr.ConstantValueOpt,
                    type: underlyingType);
            }

            return expr;
        }

        private TypeSymbol GetAdjustedTypeForEnumMemberReference(FieldSymbol fieldSymbol, TypeSymbol fieldType)
        {
            // Spec 14.3: "Within an enum member initializer, values of other enum members are
            // always treated as having the type of their underlying type"
            NamedTypeSymbol underlyingType = null;

            if (this.InEnumMemberInitializer())
            {
                NamedTypeSymbol enumType = null;
                NamedTypeSymbol type = fieldSymbol.ContainingType;
                var isEnumField = (fieldSymbol.IsStatic && type.IsEnumType());
                if (isEnumField)
                {
                    // This is an obvious consequence of the spec.
                    // It is for cases like:
                    // enum E {
                    //     A,
                    //     B = A + 1, //A is implicitly converted to int (underlying type)
                    // }
                    enumType = type;
                }
                else if (fieldSymbol.IsConst && fieldType.IsEnumType())
                {
                    // This seems like a borderline SPEC VIOLATION that we're preserving for back compat.
                    // It is for cases like:
                    // const E e = E.A;
                    // enum E {
                    //     A,
                    //     B = e + 1, //e is implicitly converted to int (underlying type)
                    // }
                    enumType = (NamedTypeSymbol)fieldType;
                }

                if ((object)enumType != null)
                {
                    underlyingType = enumType.EnumUnderlyingType;
                    Debug.Assert((object)underlyingType != null);
                }
            }

            return underlyingType;
        }

        private bool InEnumMemberInitializer()
        {
            var containingType = this.ContainingType;
            return this.InFieldInitializer && (object)containingType != null && containingType.IsEnumType();
        }

#nullable enable
        private BoundExpression BindPropertyAccess(
            SyntaxNode node,
            BoundExpression? receiver,
            PropertySymbol propertySymbol,
            BindingDiagnosticBag diagnostics,
            LookupResultKind lookupResult,
            bool hasErrors)
        {
            ReportDiagnosticsIfObsolete(diagnostics, propertySymbol, node, hasBaseReceiver: receiver?.Kind == BoundKind.BaseReference);

            bool hasError = this.CheckInstanceOrStatic(node, receiver, propertySymbol, ref lookupResult, diagnostics);

            if (!propertySymbol.IsStatic)
            {
                WarnOnAccessOfOffDefault(node, receiver, diagnostics);
            }

            // The type calculation here should be kept in sync with logic in BindLeftIdentifierOfPotentialColorColorMemberAccess.
            return new BoundPropertyAccess(node, receiver, initialBindingReceiverIsSubjectToCloning: ReceiverIsSubjectToCloning(receiver, propertySymbol), propertySymbol, autoPropertyAccessorKind: AccessorKind.Unknown, lookupResult, propertySymbol.Type, hasErrors: (hasErrors || hasError));
        }
#nullable disable

        private void CheckReceiverAndRuntimeSupportForSymbolAccess(SyntaxNode node, BoundExpression receiverOpt, Symbol symbol, BindingDiagnosticBag diagnostics)
        {
            if (symbol.ContainingType?.IsInterface == true)
            {
                if (symbol.IsStatic && (symbol.IsAbstract || symbol.IsVirtual))
                {
                    Debug.Assert(symbol is not TypeSymbol);

                    if (receiverOpt is BoundQueryClause { Value: var value })
                    {
                        receiverOpt = value;
                    }

                    if (receiverOpt is not BoundTypeExpression { Type: { TypeKind: TypeKind.TypeParameter } })
                    {
                        Error(diagnostics, ErrorCode.ERR_BadAbstractStaticMemberAccess, node);
                        return;
                    }

                    if (!Compilation.Assembly.RuntimeSupportsStaticAbstractMembersInInterfaces && Compilation.SourceModule != symbol.ContainingModule)
                    {
                        Error(diagnostics, ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, node);
                        return;
                    }
                }

                if (receiverOpt is { Type: TypeParameterSymbol { AllowsRefLikeType: true } } &&
                    isNotImplementableInstanceMember(symbol))
                {
                    Error(diagnostics, ErrorCode.ERR_BadNonVirtualInterfaceMemberAccessOnAllowsRefLike, node);
                }
                else if (!Compilation.Assembly.RuntimeSupportsDefaultInterfaceImplementation && Compilation.SourceModule != symbol.ContainingModule)
                {
                    if (isNotImplementableInstanceMember(symbol))
                    {
                        Error(diagnostics, ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, node);
                    }
                    else
                    {
                        switch (symbol.DeclaredAccessibility)
                        {
                            case Accessibility.Protected:
                            case Accessibility.ProtectedOrInternal:
                            case Accessibility.ProtectedAndInternal:

                                Error(diagnostics, ErrorCode.ERR_RuntimeDoesNotSupportProtectedAccessForInterfaceMember, node);
                                break;
                        }
                    }
                }
            }

            static bool isNotImplementableInstanceMember(Symbol symbol)
            {
                return !symbol.IsStatic && !(symbol is TypeSymbol) &&
                       !symbol.IsImplementableInterfaceMember();
            }
        }

        private BoundExpression BindEventAccess(
            SyntaxNode node,
            BoundExpression receiver,
            EventSymbol eventSymbol,
            BindingDiagnosticBag diagnostics,
            LookupResultKind lookupResult,
            bool hasErrors)
        {
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            bool isUsableAsField = eventSymbol.HasAssociatedField && this.IsAccessible(eventSymbol.AssociatedField, ref useSiteInfo, (receiver != null) ? receiver.Type : null);
            diagnostics.Add(node, useSiteInfo);

            bool hasError = this.CheckInstanceOrStatic(node, receiver, eventSymbol, ref lookupResult, diagnostics);

            if (!eventSymbol.IsStatic)
            {
                WarnOnAccessOfOffDefault(node, receiver, diagnostics);
            }

            return new BoundEventAccess(node, receiver, eventSymbol, isUsableAsField, lookupResult, eventSymbol.Type, hasErrors: (hasErrors || hasError));
        }

        // Say if the receive is an instance or a type, or could be either (returns null).
        private static bool? IsInstanceReceiver(BoundExpression receiver)
        {
            if (receiver == null)
            {
                return false;
            }
            else
            {
                switch (receiver.Kind)
                {
                    case BoundKind.PreviousSubmissionReference:
                        // Could be either instance or static reference.
                        return null;
                    case BoundKind.TypeExpression:
                        return false;
                    case BoundKind.QueryClause:
                        return IsInstanceReceiver(((BoundQueryClause)receiver).Value);
                    default:
                        return true;
                }
            }
        }

        private bool CheckInstanceOrStatic(
            SyntaxNode node,
            BoundExpression receiver,
            Symbol symbol,
            ref LookupResultKind resultKind,
            BindingDiagnosticBag diagnostics)
        {
            bool? instanceReceiver = IsInstanceReceiver(receiver);

            if (!symbol.RequiresInstanceReceiver())
            {
                if (instanceReceiver == true)
                {
                    if (!IsInsideNameof)
                    {
                        ErrorCode errorCode = this.Flags.Includes(BinderFlags.ObjectInitializerMember) ?
                            ErrorCode.ERR_StaticMemberInObjectInitializer :
                            ErrorCode.ERR_ObjectProhibited;
                        Error(diagnostics, errorCode, node, symbol);
                    }
                    else if (CheckFeatureAvailability(node, MessageID.IDS_FeatureInstanceMemberInNameof, diagnostics))
                    {
                        return false;
                    }
                    resultKind = LookupResultKind.StaticInstanceMismatch;
                    return true;
                }
            }
            else
            {
                if (instanceReceiver == false && !IsInsideNameof)
                {
                    Error(diagnostics, ErrorCode.ERR_ObjectRequired, node, symbol);
                    resultKind = LookupResultKind.StaticInstanceMismatch;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Given a viable LookupResult, report any ambiguity errors and return either a single
        /// non-method symbol or a method or property group. If the result set represents a
        /// collection of methods or a collection of properties where at least one of the properties
        /// is an indexed property, then 'methodOrPropertyGroup' is populated with the method or
        /// property group and the method returns null. Otherwise, the method returns a single
        /// symbol and 'methodOrPropertyGroup' is empty. (Since the result set is viable, there
        /// must be at least one symbol.) If the result set is ambiguous - either containing multiple
        /// members of different member types, or multiple properties but no indexed properties -
        /// then a diagnostic is reported for the ambiguity and a single symbol is returned.
        /// </summary>
        private Symbol GetSymbolOrMethodOrPropertyGroup(LookupResult result, SyntaxNode node, string plainName, int arity, ArrayBuilder<Symbol> methodOrPropertyGroup, BindingDiagnosticBag diagnostics, out bool wasError, NamespaceOrTypeSymbol qualifierOpt)
        {
            Debug.Assert(!methodOrPropertyGroup.Any());

            node = GetNameSyntax(node) ?? node;
            wasError = false;

            Debug.Assert(result.Kind != LookupResultKind.Empty);
            Debug.Assert(!result.Symbols.Any(s => s.IsIndexer()));

            Symbol other = null; // different member type from 'methodOrPropertyGroup'

            // Populate 'methodOrPropertyGroup' with a set of methods if any,
            // or a set of properties if properties but no methods. If there are
            // other member types, 'other' will be set to one of those members.
            foreach (var symbol in result.Symbols)
            {
                var kind = symbol.Kind;
                if (methodOrPropertyGroup.Count > 0)
                {
                    var existingKind = methodOrPropertyGroup[0].Kind;
                    if (existingKind != kind)
                    {
                        // Mix of different member kinds. Prefer methods over
                        // properties and properties over other members.
                        if ((existingKind == SymbolKind.Method) ||
                            ((existingKind == SymbolKind.Property) && (kind != SymbolKind.Method)))
                        {
                            other = symbol;
                            continue;
                        }

                        other = methodOrPropertyGroup[0];
                        methodOrPropertyGroup.Clear();
                    }
                }

                if ((kind == SymbolKind.Method) || (kind == SymbolKind.Property))
                {
                    // SPEC VIOLATION: The spec states "Members that include an override modifier are excluded from the set"
                    // SPEC VIOLATION: However, we are not going to do that here; we will keep the overriding member
                    // SPEC VIOLATION: in the method group. The reason is because for features like "go to definition"
                    // SPEC VIOLATION: we wish to go to the overriding member, not to the member of the base class.
                    // SPEC VIOLATION: Or, for code generation of a call to Int32.ToString() we want to generate
                    // SPEC VIOLATION: code that directly calls the Int32.ToString method with an int on the stack,
                    // SPEC VIOLATION: rather than making a virtual call to ToString on a boxed int.
                    methodOrPropertyGroup.Add(symbol);
                }
                else
                {
                    other = symbol;
                }
            }

            Debug.Assert(methodOrPropertyGroup.Any() || ((object)other != null));

            if ((methodOrPropertyGroup.Count > 0) &&
                IsMethodOrPropertyGroup(methodOrPropertyGroup))
            {
                // Ambiguities between methods and non-methods are reported here,
                // but all other ambiguities, including those between properties and
                // non-methods, are reported in ResultSymbol.
                if ((methodOrPropertyGroup[0].Kind == SymbolKind.Method) || ((object)other == null))
                {
                    // Result will be treated as a method or property group. Any additional
                    // checks, such as use-site errors, must be handled by the caller when
                    // converting to method invocation or property access.

                    if (result.Error != null)
                    {
                        Error(diagnostics, result.Error, node);
                        wasError = (result.Error.Severity == DiagnosticSeverity.Error);
                    }

                    return null;
                }
            }

            methodOrPropertyGroup.Clear();
            return ResultSymbol(result, plainName, arity, node, diagnostics, false, out wasError, qualifierOpt);
        }

        private static bool IsMethodOrPropertyGroup(ArrayBuilder<Symbol> members)
        {
            Debug.Assert(members.Count > 0);

            var member = members[0];

            // Members should be a consistent type.
            Debug.Assert(members.All(m => m.Kind == member.Kind));

            switch (member.Kind)
            {
                case SymbolKind.Method:
                    return true;

                case SymbolKind.Property:
                    Debug.Assert(members.All(m => !m.IsIndexer()));

                    // Do not treat a set of non-indexed properties as a property group, to
                    // avoid the overhead of a BoundPropertyGroup node and overload
                    // resolution for the common property access case. If there are multiple
                    // non-indexed properties (two properties P that differ by custom attributes
                    // for instance), the expectation is that the caller will report an ambiguity
                    // and choose one for error recovery.
                    foreach (PropertySymbol property in members)
                    {
                        if (property.IsIndexedProperty)
                        {
                            return true;
                        }
                    }
                    return false;

                default:
                    throw ExceptionUtilities.UnexpectedValue(member.Kind);
            }
        }

        private BoundExpression BindElementAccess(ElementAccessExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            BoundExpression receiver = BindExpression(node.Expression, diagnostics: diagnostics, invoked: false, indexed: true);
            return BindElementAccess(node, receiver, node.ArgumentList, allowInlineArrayElementAccess: true, diagnostics);
        }

        private BoundExpression BindElementAccess(ExpressionSyntax node, BoundExpression receiver, BracketedArgumentListSyntax argumentList, bool allowInlineArrayElementAccess, BindingDiagnosticBag diagnostics)
        {
            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();
            try
            {
                BindArgumentsAndNames(argumentList, diagnostics, analyzedArguments);

                if (receiver.Kind == BoundKind.PropertyGroup)
                {
                    var propertyGroup = (BoundPropertyGroup)receiver;
                    Debug.Assert(propertyGroup.ReceiverOpt is not null);
                    return BindIndexedPropertyAccess(node, propertyGroup.ReceiverOpt, propertyGroup.Properties, analyzedArguments, diagnostics);
                }

                receiver = CheckValue(receiver, BindValueKind.RValue, diagnostics);
                receiver = BindToNaturalType(receiver, diagnostics);

                return BindElementOrIndexerAccess(node, receiver, analyzedArguments, allowInlineArrayElementAccess, diagnostics);
            }
            finally
            {
                analyzedArguments.Free();
            }
        }

        private BoundExpression BindElementOrIndexerAccess(ExpressionSyntax node, BoundExpression expr, AnalyzedArguments analyzedArguments, bool allowInlineArrayElementAccess, BindingDiagnosticBag diagnostics)
        {
            if ((object)expr.Type == null)
            {
                return BadIndexerExpression(node, expr, analyzedArguments, null, diagnostics);
            }

            WarnOnAccessOfOffDefault(node, expr, diagnostics);

            // Did we have any errors?
            if (analyzedArguments.HasErrors || expr.HasAnyErrors)
            {
                // At this point we definitely have reported an error, but we still might be 
                // able to get more semantic analysis of the indexing operation. We do not
                // want to report cascading errors.

                diagnostics = BindingDiagnosticBag.Discarded;
            }

            bool tryInlineArrayAccess = false;

            if (allowInlineArrayElementAccess &&
                !InAttributeArgument && !InParameterDefaultValue && // These checks prevent cycles caused by attribute binding when HasInlineArrayAttribute check triggers that.
                expr.Type.HasInlineArrayAttribute(out int length) && expr.Type.TryGetPossiblyUnsupportedByLanguageInlineArrayElementField() is FieldSymbol elementField)
            {
                tryInlineArrayAccess = true;

                if (analyzedArguments.Arguments.Count == 1 &&
                    tryImplicitConversionToInlineArrayIndex(node, analyzedArguments.Arguments[0], diagnostics, out WellKnownType indexOrRangeWellknownType) is { } convertedIndex)
                {
                    if (!TypeSymbol.IsInlineArrayElementFieldSupported(elementField))
                    {
                        return BadIndexerExpression(node, expr, analyzedArguments, null, diagnostics);
                    }

                    Debug.Assert(expr.Type.TryGetInlineArrayElementField() is not null);
                    return bindInlineArrayElementAccess(node, expr, length, analyzedArguments, convertedIndex, indexOrRangeWellknownType, elementField, diagnostics);
                }
            }

            BindingDiagnosticBag diagnosticsForBindElementAccessCore = diagnostics;

            if (tryInlineArrayAccess && diagnostics.AccumulatesDiagnostics)
            {
                diagnosticsForBindElementAccessCore = BindingDiagnosticBag.GetInstance(diagnostics);
            }

            BoundExpression result = BindElementAccessCore(node, expr, analyzedArguments, diagnosticsForBindElementAccessCore);

            if (diagnosticsForBindElementAccessCore != diagnostics)
            {
                Debug.Assert(tryInlineArrayAccess);
                Debug.Assert(diagnosticsForBindElementAccessCore.DiagnosticBag is { });

                if (diagnosticsForBindElementAccessCore.DiagnosticBag.AsEnumerableWithoutResolution().AsSingleton() is
                    { Code: (int)ErrorCode.ERR_BadIndexLHS, Arguments: [TypeSymbol type] } && type.Equals(expr.Type, TypeCompareKind.ConsiderEverything))
                {
                    diagnosticsForBindElementAccessCore.DiagnosticBag.Clear();
                    Error(diagnosticsForBindElementAccessCore, ErrorCode.ERR_InlineArrayBadIndex, node.Location);
                }

                diagnostics.AddRangeAndFree(diagnosticsForBindElementAccessCore);
            }

            return result;

            BoundExpression tryImplicitConversionToInlineArrayIndex(ExpressionSyntax node, BoundExpression index, BindingDiagnosticBag diagnostics, out WellKnownType indexOrRangeWellknownType)
            {
                indexOrRangeWellknownType = WellKnownType.Unknown;
                BoundExpression convertedIndex = TryImplicitConversionToArrayIndex(index, SpecialType.System_Int32, node, diagnostics);

                if (convertedIndex is null)
                {
                    convertedIndex = TryImplicitConversionToArrayIndex(index, WellKnownType.System_Index, node, diagnostics);

                    if (convertedIndex is null)
                    {
                        convertedIndex = TryImplicitConversionToArrayIndex(index, WellKnownType.System_Range, node, diagnostics);
                        if (convertedIndex is object)
                        {
                            indexOrRangeWellknownType = WellKnownType.System_Range;
                        }
                    }
                    else
                    {
                        indexOrRangeWellknownType = WellKnownType.System_Index;
                    }
                }

                return convertedIndex;
            }

            BoundExpression bindInlineArrayElementAccess(ExpressionSyntax node, BoundExpression expr, int length, AnalyzedArguments analyzedArguments, BoundExpression convertedIndex, WellKnownType indexOrRangeWellknownType, FieldSymbol elementField, BindingDiagnosticBag diagnostics)
            {
                // Check required well-known members. They may not be needed
                // during lowering, but it's simpler to always require them to prevent
                // the user from getting surprising errors when optimizations fail
                if (indexOrRangeWellknownType != WellKnownType.Unknown)
                {
                    if (indexOrRangeWellknownType == WellKnownType.System_Range)
                    {
                        _ = GetWellKnownTypeMember(WellKnownMember.System_Range__get_Start, diagnostics, syntax: node);
                        _ = GetWellKnownTypeMember(WellKnownMember.System_Range__get_End, diagnostics, syntax: node);
                    }

                    _ = GetWellKnownTypeMember(WellKnownMember.System_Index__GetOffset, diagnostics, syntax: node);
                }

                if (analyzedArguments.Names.Count > 0)
                {
                    Error(diagnostics, ErrorCode.ERR_NamedArgumentForInlineArray, node);
                }

                ReportRefOrOutArgument(analyzedArguments, diagnostics);

                WellKnownMember createSpanHelper;
                WellKnownMember getItemOrSliceHelper;
                bool isValue = false;

                if (CheckValueKind(node, expr, BindValueKind.RefersToLocation | BindValueKind.Assignable, checkingReceiver: false, BindingDiagnosticBag.Discarded))
                {
                    createSpanHelper = WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateSpan;
                    getItemOrSliceHelper = indexOrRangeWellknownType == WellKnownType.System_Range ? WellKnownMember.System_Span_T__Slice_Int_Int : WellKnownMember.System_Span_T__get_Item;
                }
                else
                {
                    createSpanHelper = WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateReadOnlySpan;
                    getItemOrSliceHelper = indexOrRangeWellknownType == WellKnownType.System_Range ? WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int : WellKnownMember.System_ReadOnlySpan_T__get_Item;

                    _ = GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_Unsafe__AsRef_T, diagnostics, syntax: node);

                    if (!CheckValueKind(node, expr, BindValueKind.RefersToLocation, checkingReceiver: false, BindingDiagnosticBag.Discarded))
                    {
                        if (indexOrRangeWellknownType == WellKnownType.System_Range)
                        {
                            Location location;

                            if (expr.Syntax.Parent is ConditionalAccessExpressionSyntax conditional &&
                                conditional.Expression == expr.Syntax)
                            {
                                location = expr.Syntax.SyntaxTree.GetLocation(TextSpan.FromBounds(expr.Syntax.SpanStart, conditional.OperatorToken.Span.End));
                            }
                            else
                            {
                                location = expr.Syntax.GetLocation();
                            }

                            Error(diagnostics, ErrorCode.ERR_RefReturnLvalueExpected, location);
                        }
                        else
                        {
                            isValue = true;
                        }
                    }
                }

                // Check bounds
                if (convertedIndex.ConstantValueOpt is { SpecialType: SpecialType.System_Int32, Int32Value: int constIndex })
                {
                    checkInlineArrayBounds(convertedIndex.Syntax, constIndex, length, excludeEnd: true, diagnostics);
                }
                else if (indexOrRangeWellknownType == WellKnownType.System_Index)
                {
                    checkInlineArrayBoundsForSystemIndex(convertedIndex, length, excludeEnd: true, diagnostics);
                }
                else if (indexOrRangeWellknownType == WellKnownType.System_Range && convertedIndex is BoundRangeExpression rangeExpr)
                {
                    if (rangeExpr.LeftOperandOpt is BoundExpression left)
                    {
                        checkInlineArrayBoundsForSystemIndex(left, length, excludeEnd: false, diagnostics);
                    }

                    if (rangeExpr.RightOperandOpt is BoundExpression right)
                    {
                        checkInlineArrayBoundsForSystemIndex(right, length, excludeEnd: false, diagnostics);
                    }
                }

                _ = GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_Unsafe__As_T, diagnostics, syntax: node);
                _ = GetWellKnownTypeMember(createSpanHelper, diagnostics, syntax: node);
                _ = GetWellKnownTypeMember(getItemOrSliceHelper, diagnostics, syntax: node);

                CheckInlineArrayTypeIsSupported(node, expr.Type, elementField.Type, diagnostics);

                if (!Compilation.Assembly.RuntimeSupportsInlineArrayTypes)
                {
                    Error(diagnostics, ErrorCode.ERR_RuntimeDoesNotSupportInlineArrayTypes, node);
                }

                CheckFeatureAvailability(node, MessageID.IDS_FeatureInlineArrays, diagnostics);
                diagnostics.ReportUseSite(elementField, node);

                TypeSymbol resultType;

                if (indexOrRangeWellknownType == WellKnownType.System_Range)
                {
                    // The symbols will be verified as return types of 'createSpanHelper', no need to check them again
                    resultType = Compilation.GetWellKnownType(
                        getItemOrSliceHelper is WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int ? WellKnownType.System_ReadOnlySpan_T : WellKnownType.System_Span_T).
                        Construct(ImmutableArray.Create(elementField.TypeWithAnnotations));
                }
                else
                {
                    resultType = elementField.Type;
                }

                return new BoundInlineArrayAccess(node, expr, convertedIndex, isValue, getItemOrSliceHelper, resultType);
            }

            static void checkInlineArrayBounds(SyntaxNode location, int index, int end, bool excludeEnd, BindingDiagnosticBag diagnostics)
            {
                if (index < 0 || (excludeEnd ? index >= end : index > end))
                {
                    Error(diagnostics, ErrorCode.ERR_InlineArrayIndexOutOfRange, location);
                }
            }

            void checkInlineArrayBoundsForSystemIndex(BoundExpression convertedIndex, int length, bool excludeEnd, BindingDiagnosticBag diagnostics)
            {
                SyntaxNode location;
                int? constIndex = InferConstantIndexFromSystemIndex(Compilation, convertedIndex, length, out location);

                if (constIndex.HasValue)
                {
                    checkInlineArrayBounds(location, constIndex.GetValueOrDefault(), length, excludeEnd, diagnostics);
                }
            }
        }

        internal static int? InferConstantIndexFromSystemIndex(CSharpCompilation compilation, BoundExpression convertedIndex, int length, out SyntaxNode location)
        {
            int? constIndexOpt = null;
            location = null;
            if (TypeSymbol.Equals(convertedIndex.Type, compilation.GetWellKnownType(WellKnownType.System_Index), TypeCompareKind.AllIgnoreOptions))
            {
                if (convertedIndex is BoundFromEndIndexExpression hatExpression)
                {
                    // `^index`
                    if (hatExpression.Operand.ConstantValueOpt is { SpecialType: SpecialType.System_Int32, Int32Value: int constIndex })
                    {
                        location = hatExpression.Syntax;
                        constIndexOpt = length - constIndex;
                    }
                }
                else if (convertedIndex is BoundConversion { Operand: { ConstantValueOpt: { SpecialType: SpecialType.System_Int32, Int32Value: int constIndex } } operand })
                {
                    location = operand.Syntax;
                    constIndexOpt = constIndex;
                }
                else if (convertedIndex is BoundObjectCreationExpression { Constructor: MethodSymbol constructor, Arguments: { Length: 2 } arguments, ArgsToParamsOpt: { IsDefaultOrEmpty: true }, InitializerExpressionOpt: null } &&
                         (object)constructor == compilation.GetWellKnownTypeMember(WellKnownMember.System_Index__ctor) &&
                         arguments[0] is { ConstantValueOpt: { SpecialType: SpecialType.System_Int32, Int32Value: int constIndex1 } } index &&
                         arguments[1] is { ConstantValueOpt: { SpecialType: SpecialType.System_Boolean, BooleanValue: bool isFromEnd } })
                {
                    location = index.Syntax;
                    constIndexOpt = isFromEnd ? length - constIndex1 : constIndex1;
                }
            }

            return constIndexOpt;
        }

        private BoundExpression BadIndexerExpression(SyntaxNode node, BoundExpression expr, AnalyzedArguments analyzedArguments, DiagnosticInfo errorOpt, BindingDiagnosticBag diagnostics)
        {
            if (!expr.HasAnyErrors)
            {
                diagnostics.Add(errorOpt ?? new CSDiagnosticInfo(ErrorCode.ERR_BadIndexLHS, expr.Display), node.Location);
            }

            var childBoundNodes = BuildArgumentsForErrorRecovery(analyzedArguments).Add(expr);
            return new BoundBadExpression(node, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, childBoundNodes, CreateErrorType(), hasErrors: true);
        }

        private BoundExpression BindElementAccessCore(
             SyntaxNode node,
             BoundExpression expr,
             AnalyzedArguments arguments,
             BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            Debug.Assert(expr != null);
            Debug.Assert((object)expr.Type != null);
            Debug.Assert(arguments != null);

            var exprType = expr.Type;
            switch (exprType.TypeKind)
            {
                case TypeKind.Array:
                    return BindArrayAccess(node, expr, arguments, diagnostics);

                case TypeKind.Dynamic:
                    return BindDynamicIndexer(node, expr, arguments, ImmutableArray<PropertySymbol>.Empty, diagnostics);

                case TypeKind.Pointer:
                    return BindPointerElementAccess(node, expr, arguments, diagnostics);

                case TypeKind.Class:
                case TypeKind.Struct:
                case TypeKind.Interface:
                case TypeKind.TypeParameter:
                    return BindIndexerAccess(node, expr, arguments, diagnostics);

                case TypeKind.Submission: // script class is synthesized and should not be used as a type of an indexer expression:
                default:
                    return BadIndexerExpression(node, expr, arguments, null, diagnostics);
            }
        }

        private BoundExpression BindArrayAccess(SyntaxNode node, BoundExpression expr, AnalyzedArguments arguments, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            Debug.Assert(expr != null);
            Debug.Assert(arguments != null);

            // For an array access, the primary-no-array-creation-expression of the element-access
            // must be a value of an array-type. Furthermore, the argument-list of an array access
            // is not allowed to contain named arguments.The number of expressions in the
            // argument-list must be the same as the rank of the array-type, and each expression
            // must be of type int, uint, long, ulong, or must be implicitly convertible to one or
            // more of these types.

            if (arguments.Names.Count > 0)
            {
                Error(diagnostics, ErrorCode.ERR_NamedArgumentForArray, node);
            }

            ReportRefOrOutArgument(arguments, diagnostics);
            var arrayType = (ArrayTypeSymbol)expr.Type;

            // Note that the spec says to determine which of {int, uint, long, ulong} *each* index
            // expression is convertible to. That is not what C# 1 through 4 did; the
            // implementations instead determined which of those four types *all* of the index
            // expressions converted to. 

            int rank = arrayType.Rank;

            if (arguments.Arguments.Count != rank)
            {
                Error(diagnostics, ErrorCode.ERR_BadIndexCount, node, rank);
                return new BoundArrayAccess(node, expr, BuildArgumentsForErrorRecovery(arguments), arrayType.ElementType, hasErrors: true);
            }

            // Convert all the arguments to the array index type.
            BoundExpression[] convertedArguments = new BoundExpression[arguments.Arguments.Count];
            WellKnownType indexOrRangeWellknownType = WellKnownType.Unknown;
            for (int i = 0; i < arguments.Arguments.Count; ++i)
            {
                BoundExpression argument = arguments.Arguments[i];

                BoundExpression index = ConvertToArrayIndex(argument, diagnostics, allowIndexAndRange: rank == 1, out indexOrRangeWellknownType);
                convertedArguments[i] = index;

                // NOTE: Dev10 only warns if rank == 1
                // Question: Why do we limit this warning to one-dimensional arrays?
                // Answer: Because multidimensional arrays can have nonzero lower bounds in the CLR.
                if (rank == 1 && !index.HasAnyErrors)
                {
                    ConstantValue constant = index.ConstantValueOpt;
                    if (constant != null && constant.IsNegativeNumeric)
                    {
                        Error(diagnostics, ErrorCode.WRN_NegativeArrayIndex, index.Syntax);
                    }
                }
            }

            TypeSymbol resultType = indexOrRangeWellknownType == WellKnownType.System_Range
                ? arrayType
                : arrayType.ElementType;

            if (indexOrRangeWellknownType == WellKnownType.System_Index)
            {
                Debug.Assert(convertedArguments.Length == 1);

                var int32 = GetSpecialType(SpecialType.System_Int32, diagnostics, node);
                var receiverPlaceholder = new BoundImplicitIndexerReceiverPlaceholder(expr.Syntax, isEquivalentToThisReference: expr.IsEquivalentToThisReference, expr.Type) { WasCompilerGenerated = true };
                var argumentPlaceholders = ImmutableArray.Create(new BoundImplicitIndexerValuePlaceholder(convertedArguments[0].Syntax, int32) { WasCompilerGenerated = true });

                return new BoundImplicitIndexerAccess(
                    node,
                    receiver: expr,
                    argument: convertedArguments[0],
                    lengthOrCountAccess: new BoundArrayLength(node, receiverPlaceholder, int32) { WasCompilerGenerated = true },
                    receiverPlaceholder,
                    indexerOrSliceAccess: new BoundArrayAccess(node, receiverPlaceholder, ImmutableArray<BoundExpression>.CastUp(argumentPlaceholders), resultType) { WasCompilerGenerated = true },
                    argumentPlaceholders,
                    resultType);
            }

            return new BoundArrayAccess(node, expr, convertedArguments.AsImmutableOrNull(), resultType);
        }

        private BoundExpression ConvertToArrayIndex(BoundExpression index, BindingDiagnosticBag diagnostics, bool allowIndexAndRange, out WellKnownType indexOrRangeWellknownType)
        {
            Debug.Assert(index != null);

            indexOrRangeWellknownType = WellKnownType.Unknown;

            if (index.Kind == BoundKind.OutVariablePendingInference)
            {
                return ((OutVariablePendingInference)index).FailInference(this, diagnostics);
            }
            else if (index.Kind == BoundKind.DiscardExpression && !index.HasExpressionType())
            {
                return ((BoundDiscardExpression)index).FailInference(this, diagnostics);
            }

            var node = index.Syntax;
            var result =
                TryImplicitConversionToArrayIndex(index, SpecialType.System_Int32, node, diagnostics) ??
                TryImplicitConversionToArrayIndex(index, SpecialType.System_UInt32, node, diagnostics) ??
                TryImplicitConversionToArrayIndex(index, SpecialType.System_Int64, node, diagnostics) ??
                TryImplicitConversionToArrayIndex(index, SpecialType.System_UInt64, node, diagnostics);

            if (result is null && allowIndexAndRange)
            {
                result = TryImplicitConversionToArrayIndex(index, WellKnownType.System_Index, node, diagnostics);

                if (result is null)
                {
                    result = TryImplicitConversionToArrayIndex(index, WellKnownType.System_Range, node, diagnostics);
                    if (result is object)
                    {
                        indexOrRangeWellknownType = WellKnownType.System_Range;
                        // This member is needed for lowering and should produce an error if not present
                        _ = GetWellKnownTypeMember(
                            WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__GetSubArray_T,
                            diagnostics,
                            syntax: node);
                    }
                }
                else
                {
                    indexOrRangeWellknownType = WellKnownType.System_Index;

                    // This member is needed for lowering and should produce an error if not present
                    _ = GetWellKnownTypeMember(
                        WellKnownMember.System_Index__GetOffset,
                        diagnostics,
                        syntax: node);
                }
            }

            if (result is null)
            {
                // Give the error that would be given upon conversion to int32.
                NamedTypeSymbol int32 = GetSpecialType(SpecialType.System_Int32, diagnostics, node);
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                Conversion failedConversion = this.Conversions.ClassifyConversionFromExpression(index, int32, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);
                diagnostics.Add(node, useSiteInfo);
                GenerateImplicitConversionError(diagnostics, node, failedConversion, index, int32);

                // Suppress any additional diagnostics
                return CreateConversion(node, index, failedConversion, isCast: false, conversionGroupOpt: null, InConversionGroupFlags.Unspecified, destination: int32, diagnostics: BindingDiagnosticBag.Discarded);
            }

            return result;
        }

        private BoundExpression TryImplicitConversionToArrayIndex(BoundExpression expr, WellKnownType wellKnownType, SyntaxNode node, BindingDiagnosticBag diagnostics)
        {
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            TypeSymbol type = GetWellKnownType(wellKnownType, ref useSiteInfo);

            if (type.IsErrorType())
            {
                return null;
            }

            var attemptDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics);
            var result = TryImplicitConversionToArrayIndex(expr, type, node, attemptDiagnostics);
            if (result is object)
            {
                diagnostics.Add(node, useSiteInfo);
                diagnostics.AddRange(attemptDiagnostics);
            }
            attemptDiagnostics.Free();
            return result;
        }

        private BoundExpression TryImplicitConversionToArrayIndex(BoundExpression expr, SpecialType specialType, SyntaxNode node, BindingDiagnosticBag diagnostics)
        {
            var attemptDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics);

            TypeSymbol type = GetSpecialType(specialType, attemptDiagnostics, node);

            var result = TryImplicitConversionToArrayIndex(expr, type, node, attemptDiagnostics);

            if (result is object)
            {
                diagnostics.AddRange(attemptDiagnostics);
            }

            attemptDiagnostics.Free();
            return result;
        }

        private BoundExpression TryImplicitConversionToArrayIndex(BoundExpression expr, TypeSymbol targetType, SyntaxNode node, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(expr != null);
            Debug.Assert((object)targetType != null);

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            Conversion conversion = this.Conversions.ClassifyImplicitConversionFromExpression(expr, targetType, ref useSiteInfo);
            diagnostics.Add(node, useSiteInfo);
            if (!conversion.Exists)
            {
                return null;
            }

            if (conversion.IsDynamic)
            {
                conversion = conversion.SetArrayIndexConversionForDynamic();
            }

            BoundExpression result = CreateConversion(expr.Syntax, expr, conversion, isCast: false, conversionGroupOpt: null, InConversionGroupFlags.Unspecified, destination: targetType, diagnostics); // UNDONE: was cast?
            Debug.Assert(result != null); // If this ever fails (it shouldn't), then put a null-check around the diagnostics update.

            return result;
        }

        private BoundExpression BindPointerElementAccess(SyntaxNode node, BoundExpression expr, AnalyzedArguments analyzedArguments, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            Debug.Assert(expr != null);
            Debug.Assert(analyzedArguments != null);

            bool hasErrors = false;

            if (analyzedArguments.Names.Count > 0)
            {
                // CONSIDER: the error text for this error code mentions "arrays".  It might be nice if we had
                // a separate error code for pointer element access.
                Error(diagnostics, ErrorCode.ERR_NamedArgumentForArray, node);
                hasErrors = true;
            }

            hasErrors = hasErrors || ReportRefOrOutArgument(analyzedArguments, diagnostics);

            Debug.Assert(expr.Type.IsPointerType());
            PointerTypeSymbol pointerType = (PointerTypeSymbol)expr.Type;
            TypeSymbol pointedAtType = pointerType.PointedAtType;

            ArrayBuilder<BoundExpression> arguments = analyzedArguments.Arguments;
            if (arguments.Count != 1)
            {
                if (!hasErrors)
                {
                    Error(diagnostics, ErrorCode.ERR_PtrIndexSingle, node);
                }
                return new BoundPointerElementAccess(node, expr, BadExpression(node, BuildArgumentsForErrorRecovery(analyzedArguments)).MakeCompilerGenerated(),
                    CheckOverflowAtRuntime, refersToLocation: false, pointedAtType, hasErrors: true);
            }

            if (pointedAtType.IsVoidType())
            {
                Error(diagnostics, ErrorCode.ERR_VoidError, expr.Syntax);
                hasErrors = true;
            }

            BoundExpression index = arguments[0];

            index = ConvertToArrayIndex(index, diagnostics, allowIndexAndRange: false, indexOrRangeWellknownType: out _);
            return new BoundPointerElementAccess(node, expr, index, CheckOverflowAtRuntime, refersToLocation: false, pointedAtType, hasErrors);
        }

        private static bool ReportRefOrOutArgument(AnalyzedArguments analyzedArguments, BindingDiagnosticBag diagnostics)
        {
            int numArguments = analyzedArguments.Arguments.Count;
            for (int i = 0; i < numArguments; i++)
            {
                RefKind refKind = analyzedArguments.RefKind(i);
                if (refKind != RefKind.None)
                {
                    Error(diagnostics, ErrorCode.ERR_BadArgExtraRef, analyzedArguments.Argument(i).Syntax, i + 1, refKind.ToArgumentDisplayString());
                    return true;
                }
            }

            return false;
        }

        private BoundExpression BindIndexerAccess(SyntaxNode node, BoundExpression expr, AnalyzedArguments analyzedArguments, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            Debug.Assert(expr != null);
            Debug.Assert((object)expr.Type != null);
            Debug.Assert(analyzedArguments != null);

            LookupResult lookupResult = LookupResult.GetInstance();
            LookupOptions lookupOptions = expr.Kind == BoundKind.BaseReference ? LookupOptions.UseBaseReferenceAccessibility : LookupOptions.Default;
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            this.LookupMembersWithFallback(lookupResult, expr.Type, WellKnownMemberNames.Indexer, arity: 0, useSiteInfo: ref useSiteInfo, options: lookupOptions);
            diagnostics.Add(node, useSiteInfo);

            // Store, rather than return, so that we can release resources.
            BoundExpression indexerAccessExpression;

            if (!lookupResult.IsMultiViable)
            {
                if (TryBindIndexOrRangeImplicitIndexer(
                    node,
                    expr,
                    analyzedArguments,
                    diagnostics,
                    out var implicitIndexerAccess))
                {
                    indexerAccessExpression = implicitIndexerAccess;
                }
                else
                {
                    indexerAccessExpression = BadIndexerExpression(node, expr, analyzedArguments, lookupResult.Error, diagnostics);
                }
            }
            else
            {
                ArrayBuilder<PropertySymbol> indexerGroup = ArrayBuilder<PropertySymbol>.GetInstance();
                foreach (Symbol symbol in lookupResult.Symbols)
                {
                    Debug.Assert(symbol.IsIndexer());
                    indexerGroup.Add((PropertySymbol)symbol);
                }

                indexerAccessExpression = BindIndexerOrIndexedPropertyAccess(node, expr, indexerGroup, analyzedArguments, diagnostics);
                indexerGroup.Free();
            }

            lookupResult.Free();
            return indexerAccessExpression;
        }

        private static readonly Func<PropertySymbol, bool> s_isIndexedPropertyWithNonOptionalArguments = property =>
            {
                if (property.IsIndexer || !property.IsIndexedProperty)
                {
                    return false;
                }

                Debug.Assert(property.ParameterCount > 0);
                var parameter = property.Parameters[0];
                return !parameter.IsOptional && !parameter.IsParams;
            };

        private static readonly SymbolDisplayFormat s_propertyGroupFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeContainingType,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private BoundExpression BindIndexedPropertyAccess(BoundPropertyGroup propertyGroup, bool mustHaveAllOptionalParameters, BindingDiagnosticBag diagnostics)
        {
            var syntax = propertyGroup.Syntax;
            var receiver = propertyGroup.ReceiverOpt;
            Debug.Assert(receiver is not null);
            var properties = propertyGroup.Properties;

            if (properties.All(s_isIndexedPropertyWithNonOptionalArguments))
            {
                Error(diagnostics,
                    mustHaveAllOptionalParameters ? ErrorCode.ERR_IndexedPropertyMustHaveAllOptionalParams : ErrorCode.ERR_IndexedPropertyRequiresParams,
                    syntax,
                    properties[0].ToDisplayString(s_propertyGroupFormat));
                return BoundIndexerAccess.ErrorAccess(
                    syntax,
                    receiver,
                    CreateErrorPropertySymbol(properties),
                    ImmutableArray<BoundExpression>.Empty,
                    default(ImmutableArray<string>),
                    default(ImmutableArray<RefKind>),
                    properties);
            }

            var arguments = AnalyzedArguments.GetInstance();
            var result = BindIndexedPropertyAccess(syntax, receiver, properties, arguments, diagnostics);
            arguments.Free();
            return result;
        }

#nullable enable
        private BoundExpression BindIndexedPropertyAccess(SyntaxNode syntax, BoundExpression receiver, ImmutableArray<PropertySymbol> propertyGroup, AnalyzedArguments arguments, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(receiver is not null);
            // TODO: We're creating an extra copy of the properties array in BindIndexerOrIndexedProperty
            // converting the ArrayBuilder to ImmutableArray. Avoid the extra copy.
            var properties = ArrayBuilder<PropertySymbol>.GetInstance();
            properties.AddRange(propertyGroup);
            var result = BindIndexerOrIndexedPropertyAccess(syntax, receiver, properties, arguments, diagnostics);
            properties.Free();
            return result;
        }
#nullable disable

        private BoundExpression BindDynamicIndexer(
             SyntaxNode syntax,
             BoundExpression receiver,
             AnalyzedArguments arguments,
             ImmutableArray<PropertySymbol> applicableProperties,
             BindingDiagnosticBag diagnostics)
        {
            bool hasErrors = false;

            BoundKind receiverKind = receiver.Kind;
            if (receiverKind == BoundKind.BaseReference)
            {
                Error(diagnostics, ErrorCode.ERR_NoDynamicPhantomOnBaseIndexer, syntax);
                hasErrors = true;
            }
            else if (receiverKind == BoundKind.TypeOrValueExpression)
            {
                var typeOrValue = (BoundTypeOrValueExpression)receiver;

                // Unfortunately, the runtime binder doesn't have APIs that would allow us to pass both "type or value".
                // Ideally the runtime binder would choose between type and value based on the result of the overload resolution.
                // We need to pick one or the other here. Dev11 compiler passes the type only if the value can't be accessed.
                bool inStaticContext;
                bool useType = IsInstance(typeOrValue.ValueSymbol) && !HasThis(isExplicit: false, inStaticContext: out inStaticContext);

                receiver = ReplaceTypeOrValueReceiver(typeOrValue, useType, diagnostics);
            }

            var argArray = BuildArgumentsForDynamicInvocation(arguments, diagnostics);
            var refKindsArray = arguments.RefKinds.ToImmutableOrNull();

            hasErrors &= ReportBadDynamicArguments(syntax, receiver, argArray, refKindsArray, diagnostics, queryClause: null);

            return new BoundDynamicIndexerAccess(
                syntax,
                receiver,
                argArray,
                arguments.GetNames(),
                refKindsArray,
                applicableProperties,
                AssemblySymbol.DynamicType,
                hasErrors);
        }

        private BoundExpression BindIndexerOrIndexedPropertyAccess(
            SyntaxNode syntax,
            BoundExpression receiver,
            ArrayBuilder<PropertySymbol> propertyGroup,
            AnalyzedArguments analyzedArguments,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(receiver is not null);
            OverloadResolutionResult<PropertySymbol> overloadResolutionResult = OverloadResolutionResult<PropertySymbol>.GetInstance();
            // We don't consider when we're in default parameter values or attribute arguments so that we avoid cycles. This is an error scenario,
            // so we don't care if we accidentally miss a parameter being applicable.
            bool allowRefOmittedArguments = !InParameterDefaultValue && !InAttributeArgument && receiver.IsExpressionOfComImportType();
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            this.OverloadResolution.PropertyOverloadResolution(propertyGroup, receiver, analyzedArguments, overloadResolutionResult,
                allowRefOmittedArguments: allowRefOmittedArguments,
                dynamicResolution: analyzedArguments.HasDynamicArgument,
                ref useSiteInfo);
            diagnostics.Add(syntax, useSiteInfo);

            if (analyzedArguments.HasDynamicArgument && overloadResolutionResult.HasAnyApplicableMember)
            {
                // Note that the runtime binder may consider candidates that haven't passed compile-time final validation 
                // and an ambiguity error may be reported. Also additional checks are performed in runtime final validation 
                // that are not performed at compile-time.
                // Only if the set of final applicable candidates is empty we know for sure the call will fail at runtime.
                var finalApplicableCandidates = GetCandidatesPassingFinalValidation(syntax, overloadResolutionResult, receiver, default(ImmutableArray<TypeWithAnnotations>), isExtensionMethodGroup: false, diagnostics);

                if (finalApplicableCandidates.Length == 1)
                {
                    Debug.Assert(finalApplicableCandidates[0].IsApplicable);
                    ReportMemberNotSupportedByDynamicDispatch(syntax, finalApplicableCandidates[0], diagnostics);
                }

                overloadResolutionResult.Free();
                return BindDynamicIndexer(syntax, receiver, analyzedArguments, finalApplicableCandidates.SelectAsArray(r => r.Member), diagnostics);
            }

            return BindIndexerOrIndexedPropertyAccessContinued(syntax, receiver, propertyGroup, analyzedArguments, overloadResolutionResult, diagnostics);
        }

        private BoundExpression BindIndexerOrIndexedPropertyAccessContinued(
            SyntaxNode syntax,
            BoundExpression receiver,
            ArrayBuilder<PropertySymbol> propertyGroup,
            AnalyzedArguments analyzedArguments,
            OverloadResolutionResult<PropertySymbol> overloadResolutionResult,
            BindingDiagnosticBag diagnostics)
        {
            BoundExpression propertyAccess;
            ImmutableArray<string> argumentNames = analyzedArguments.GetNames();
            ImmutableArray<RefKind> argumentRefKinds = analyzedArguments.RefKinds.ToImmutableOrNull();
            if (!overloadResolutionResult.Succeeded)
            {
                // If the arguments had an error reported about them then suppress further error
                // reporting for overload resolution. 

                ImmutableArray<PropertySymbol> candidates = propertyGroup.ToImmutable();

                if (TryBindIndexOrRangeImplicitIndexer(
                        syntax,
                        receiver,
                        analyzedArguments,
                        diagnostics,
                        out var implicitIndexerAccess))
                {
                    return implicitIndexerAccess;
                }
                else
                {
                    // Dev10 uses the "this" keyword as the method name for indexers.
                    var candidate = candidates[0];
                    var name = candidate.IsIndexer ? SyntaxFacts.GetText(SyntaxKind.ThisKeyword) : candidate.Name;

                    overloadResolutionResult.ReportDiagnostics(
                        binder: this,
                        location: syntax.Location,
                        nodeOpt: syntax,
                        diagnostics: diagnostics,
                        name: name,
                        receiver: null,
                        invokedExpression: null,
                        arguments: analyzedArguments,
                        memberGroup: candidates,
                        typeContainingConstructor: null,
                        delegateTypeBeingInvoked: null);
                }

                ImmutableArray<BoundExpression> arguments = BuildArgumentsForErrorRecovery(analyzedArguments, candidates);

                // A bad BoundIndexerAccess containing an ErrorPropertySymbol will produce better flow analysis results than
                // a BoundBadExpression containing the candidate indexers.
                PropertySymbol property = (candidates.Length == 1) ? candidates[0] : CreateErrorPropertySymbol(candidates);

                propertyAccess = BoundIndexerAccess.ErrorAccess(
                    syntax,
                    receiver,
                    property,
                    arguments,
                    argumentNames,
                    argumentRefKinds,
                    candidates);
            }
            else
            {
                MemberResolutionResult<PropertySymbol> resolutionResult = overloadResolutionResult.ValidResult;
                PropertySymbol property = resolutionResult.Member;

                ReportDiagnosticsIfObsolete(diagnostics, property, syntax, hasBaseReceiver: receiver != null && receiver.Kind == BoundKind.BaseReference);

                // Make sure that the result of overload resolution is valid.
                var gotError = MemberGroupFinalValidationAccessibilityChecks(receiver, property, syntax, diagnostics, invokedAsExtensionMethod: false);

                receiver = ReplaceTypeOrValueReceiver(receiver, property.IsStatic, diagnostics);

                ImmutableArray<int> argsToParams;
                this.CheckAndCoerceArguments<PropertySymbol>(syntax, resolutionResult, analyzedArguments, diagnostics, receiver, invokedAsExtensionMethod: false, out argsToParams);

                if (!gotError && receiver != null && receiver.Kind == BoundKind.ThisReference && receiver.WasCompilerGenerated)
                {
                    gotError = IsRefOrOutThisParameterCaptured(syntax, diagnostics);
                }

                var arguments = analyzedArguments.Arguments.ToImmutable();

                // Note that we do not bind default arguments here, because at this point we do not know whether
                // the indexer is being used in a 'get', or 'set', or 'get+set' (compound assignment) context.
                propertyAccess = new BoundIndexerAccess(
                    syntax,
                    receiver,
                    initialBindingReceiverIsSubjectToCloning: ReceiverIsSubjectToCloning(receiver, property),
                    property,
                    arguments,
                    argumentNames,
                    argumentRefKinds,
                    expanded: resolutionResult.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm,
                    AccessorKind.Unknown,
                    argsToParams,
                    defaultArguments: default,
                    property.Type,
                    gotError);
            }

            overloadResolutionResult.Free();
            return propertyAccess;
        }

#nullable enable
        private bool TryBindIndexOrRangeImplicitIndexer(
            SyntaxNode syntax,
            BoundExpression receiver,
            AnalyzedArguments arguments,
            BindingDiagnosticBag diagnostics,
            [NotNullWhen(true)] out BoundImplicitIndexerAccess? implicitIndexerAccess)
        {
            Debug.Assert(receiver is not null);
            implicitIndexerAccess = null;

            // Verify a few things up-front, namely that we have a single argument
            // to this indexer that has an Index or Range type and that there is
            // a real receiver with a known type

            if (arguments.Arguments.Count != 1)
            {
                return false;
            }

            var argument = arguments.Arguments[0];

            var argType = argument.Type;
            ThreeState argIsIndexNotRange =
                TypeSymbol.Equals(argType, Compilation.GetWellKnownType(WellKnownType.System_Index), TypeCompareKind.ConsiderEverything) ? ThreeState.True :
                TypeSymbol.Equals(argType, Compilation.GetWellKnownType(WellKnownType.System_Range), TypeCompareKind.ConsiderEverything) ? ThreeState.False :
                ThreeState.Unknown;

            Debug.Assert(receiver.Type is not null);
            if (!argIsIndexNotRange.HasValue())
            {
                return false;
            }

            bool argIsIndex = argIsIndexNotRange.Value();
            var receiverPlaceholder = new BoundImplicitIndexerReceiverPlaceholder(receiver.Syntax, isEquivalentToThisReference: receiver.IsEquivalentToThisReference, receiver.Type) { WasCompilerGenerated = true };
            if (!TryBindIndexOrRangeImplicitIndexerParts(syntax, receiverPlaceholder, argIsIndex: argIsIndex,
                    out var lengthOrCountAccess, out var indexerOrSliceAccess, out var argumentPlaceholders, diagnostics))
            {
                return false;
            }

            Debug.Assert(lengthOrCountAccess is BoundPropertyAccess);
            Debug.Assert(indexerOrSliceAccess is BoundIndexerAccess or BoundCall);
            Debug.Assert(indexerOrSliceAccess.Type is not null);

            implicitIndexerAccess = new BoundImplicitIndexerAccess(
                syntax,
                receiver: receiver,
                argument: BindToNaturalType(argument, diagnostics),
                lengthOrCountAccess: lengthOrCountAccess,
                receiverPlaceholder,
                indexerOrSliceAccess: indexerOrSliceAccess,
                argumentPlaceholders,
                indexerOrSliceAccess.Type);

            if (!argIsIndex)
            {
                checkWellKnown(WellKnownMember.System_Range__get_Start);
                checkWellKnown(WellKnownMember.System_Range__get_End);
            }
            checkWellKnown(WellKnownMember.System_Index__GetOffset);

            _ = MessageID.IDS_FeatureIndexOperator.CheckFeatureAvailability(diagnostics, syntax);
            if (arguments.Names.Count > 0)
            {
                diagnostics.Add(
                    argIsIndex
                        ? ErrorCode.ERR_ImplicitIndexIndexerWithName
                        : ErrorCode.ERR_ImplicitRangeIndexerWithName,
                    arguments.Names[0].GetValueOrDefault().Location);
            }
            return true;

            void checkWellKnown(WellKnownMember member)
            {
                // Check required well-known member. They may not be needed
                // during lowering, but it's simpler to always require them to prevent
                // the user from getting surprising errors when optimizations fail
                _ = GetWellKnownTypeMember(member, diagnostics, syntax: syntax);
            }
        }

        /// <summary>
        /// Finds pattern-based implicit indexer and Length/Count property.
        /// </summary>
        private bool TryBindIndexOrRangeImplicitIndexerParts(
            SyntaxNode syntax,
            BoundImplicitIndexerReceiverPlaceholder receiverPlaceholder,
            bool argIsIndex,
            [NotNullWhen(true)] out BoundExpression? lengthOrCountAccess,
            [NotNullWhen(true)] out BoundExpression? indexerOrSliceAccess,
            out ImmutableArray<BoundImplicitIndexerValuePlaceholder> argumentPlaceholders,
            BindingDiagnosticBag diagnostics)
        {
            // SPEC:

            // An indexer invocation with a single argument of System.Index or System.Range will
            // succeed if the receiver type conforms to an appropriate pattern, namely

            // 1. The receiver type's original definition has an accessible property getter that returns
            //    an int and has the name Length or Count
            // 2. For Index: Has an accessible indexer with a single int parameter
            //    For Range: Has an accessible Slice method that takes two int parameters

            if (TryBindLengthOrCount(syntax, receiverPlaceholder, out lengthOrCountAccess, diagnostics) &&
                tryBindUnderlyingIndexerOrSliceAccess(syntax, receiverPlaceholder, argIsIndex, out indexerOrSliceAccess, out argumentPlaceholders, diagnostics))
            {
                return true;
            }

            lengthOrCountAccess = null;
            indexerOrSliceAccess = null;
            argumentPlaceholders = default;
            return false;

            // Binds pattern-based implicit indexer:
            // - for Index indexer, this will find `this[int]`.
            // - for Range indexer, this will find `Slice(int, int)` or `string.Substring(int, int)`.
            bool tryBindUnderlyingIndexerOrSliceAccess(
                SyntaxNode syntax,
                BoundImplicitIndexerReceiverPlaceholder receiver,
                bool argIsIndex,
                [NotNullWhen(true)] out BoundExpression? indexerOrSliceAccess,
                out ImmutableArray<BoundImplicitIndexerValuePlaceholder> argumentPlaceholders,
                BindingDiagnosticBag diagnostics)
            {
                Debug.Assert(receiver.Type is not null);
                var useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                var lookupResult = LookupResult.GetInstance();

                if (argIsIndex)
                {
                    // Look for `T this[int i]` indexer

                    LookupMembersInType(
                        lookupResult,
                        receiver.Type,
                        WellKnownMemberNames.Indexer,
                        arity: 0,
                        basesBeingResolved: null,
                        LookupOptions.Default,
                        originalBinder: this,
                        diagnose: false,
                        ref useSiteInfo);
                    diagnostics.Add(syntax, useSiteInfo);

                    if (lookupResult.IsMultiViable)
                    {
                        foreach (var candidate in lookupResult.Symbols)
                        {
                            if (!candidate.IsStatic &&
                                candidate is PropertySymbol property &&
                                IsAccessible(property, syntax, diagnostics) &&
                                property.OriginalDefinition is { ParameterCount: 1 } original &&
                                original.Parameters[0] is { Type.SpecialType: SpecialType.System_Int32, RefKind: RefKind.None })
                            {
                                var intPlaceholder = new BoundImplicitIndexerValuePlaceholder(syntax, Compilation.GetSpecialType(SpecialType.System_Int32)) { WasCompilerGenerated = true };
                                argumentPlaceholders = ImmutableArray.Create(intPlaceholder);

                                var analyzedArguments = AnalyzedArguments.GetInstance();
                                analyzedArguments.Arguments.Add(intPlaceholder);
                                var properties = ArrayBuilder<PropertySymbol>.GetInstance();
                                properties.AddRange(property);
                                indexerOrSliceAccess = BindIndexerOrIndexedPropertyAccess(syntax, receiver, properties, analyzedArguments, diagnostics).MakeCompilerGenerated();
                                properties.Free();
                                analyzedArguments.Free();
                                lookupResult.Free();
                                return true;
                            }
                        }
                    }
                }
                else if (receiver.Type.SpecialType == SpecialType.System_String)
                {
                    Debug.Assert(!argIsIndex);
                    // Look for Substring
                    var substring = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_String__Substring, diagnostics, syntax);
                    if (substring is object)
                    {
                        makeCall(syntax, receiver, substring, out indexerOrSliceAccess, out argumentPlaceholders);
                        lookupResult.Free();
                        return true;
                    }
                }
                else
                {
                    Debug.Assert(!argIsIndex);
                    // Look for `T Slice(int, int)` indexer

                    LookupMembersInType(
                        lookupResult,
                        receiver.Type,
                        WellKnownMemberNames.SliceMethodName,
                        arity: 0,
                        basesBeingResolved: null,
                        LookupOptions.Default,
                        originalBinder: this,
                        diagnose: false,
                        ref useSiteInfo);
                    diagnostics.Add(syntax, useSiteInfo);

                    if (lookupResult.IsMultiViable)
                    {
                        foreach (var candidate in lookupResult.Symbols)
                        {
                            if (!candidate.IsStatic &&
                                IsAccessible(candidate, syntax, diagnostics) &&
                                candidate is MethodSymbol method &&
                                MethodHasValidSliceSignature(method))
                            {
                                makeCall(syntax, receiver, method, out indexerOrSliceAccess, out argumentPlaceholders);
                                lookupResult.Free();
                                return true;
                            }
                        }
                    }
                }

                indexerOrSliceAccess = null;
                argumentPlaceholders = default;
                lookupResult.Free();
                return false;
            }

            void makeCall(SyntaxNode syntax, BoundExpression receiver, MethodSymbol method,
                out BoundExpression indexerOrSliceAccess, out ImmutableArray<BoundImplicitIndexerValuePlaceholder> argumentPlaceholders)
            {
                var startArgumentPlaceholder = new BoundImplicitIndexerValuePlaceholder(syntax, Compilation.GetSpecialType(SpecialType.System_Int32)) { WasCompilerGenerated = true };
                var lengthArgumentPlaceholder = new BoundImplicitIndexerValuePlaceholder(syntax, Compilation.GetSpecialType(SpecialType.System_Int32)) { WasCompilerGenerated = true };
                argumentPlaceholders = ImmutableArray.Create(startArgumentPlaceholder, lengthArgumentPlaceholder);

                var analyzedArguments = AnalyzedArguments.GetInstance();
                analyzedArguments.Arguments.Add(startArgumentPlaceholder);
                analyzedArguments.Arguments.Add(lengthArgumentPlaceholder);

                var boundMethodGroup = new BoundMethodGroup(
                    syntax, typeArgumentsOpt: default, method.Name, ImmutableArray.Create(method),
                    method, lookupError: null, BoundMethodGroupFlags.None, functionType: null, receiver, LookupResultKind.Viable)
                { WasCompilerGenerated = true };

                indexerOrSliceAccess = BindMethodGroupInvocation(syntax, syntax, method.Name, boundMethodGroup, analyzedArguments,
                    diagnostics, queryClause: null, ignoreNormalFormIfHasValidParamsParameter: true, anyApplicableCandidates: out bool _,
                    disallowExpandedNonArrayParams: false,
                    acceptOnlyMethods: true) // acceptOnlyMethods is not relevant since we won't search extensions
                    .MakeCompilerGenerated();

                analyzedArguments.Free();
            }
        }

        internal static bool MethodHasValidSliceSignature(MethodSymbol method)
        {
            return method.OriginalDefinition is var original &&
                   !original.ReturnsVoid &&
                   original.ParameterCount == 2 &&
                   original.Parameters[0] is { Type.SpecialType: SpecialType.System_Int32, RefKind: RefKind.None } &&
                   original.Parameters[1] is { Type.SpecialType: SpecialType.System_Int32, RefKind: RefKind.None };
        }

        private bool TryBindLengthOrCount(
            SyntaxNode syntax,
            BoundValuePlaceholderBase receiverPlaceholder,
            out BoundExpression lengthOrCountAccess,
            BindingDiagnosticBag diagnostics)
        {
            var lookupResult = LookupResult.GetInstance();

            Debug.Assert(receiverPlaceholder.Type is not null);
            if (TryLookupLengthOrCount(syntax, receiverPlaceholder.Type, lookupResult, out var lengthOrCountProperty, diagnostics))
            {
                diagnostics.ReportUseSite(lengthOrCountProperty, syntax);
                lengthOrCountAccess = BindPropertyAccess(syntax, receiverPlaceholder, lengthOrCountProperty, diagnostics, lookupResult.Kind, hasErrors: false).MakeCompilerGenerated();
                lengthOrCountAccess = CheckValue(lengthOrCountAccess, BindValueKind.RValue, diagnostics);

                lookupResult.Free();
                return true;
            }

            lengthOrCountAccess = BadExpression(syntax);
            lookupResult.Free();

            return false;
        }

        private bool TryLookupLengthOrCount(
            SyntaxNode syntax,
            TypeSymbol receiverType,
            LookupResult lookupResult,
            [NotNullWhen(true)] out PropertySymbol? lengthOrCountProperty,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(lookupResult.IsClear);
            if (tryLookupLengthOrCount(syntax, WellKnownMemberNames.LengthPropertyName, out lengthOrCountProperty, diagnostics) ||
                tryLookupLengthOrCount(syntax, WellKnownMemberNames.CountPropertyName, out lengthOrCountProperty, diagnostics))
            {
                return true;
            }

            return false;

            bool tryLookupLengthOrCount(SyntaxNode syntax, string propertyName, [NotNullWhen(true)] out PropertySymbol? valid, BindingDiagnosticBag diagnostics)
            {
                var useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                LookupMembersInType(
                    lookupResult,
                    receiverType,
                    propertyName,
                    arity: 0,
                    basesBeingResolved: null,
                    LookupOptions.Default,
                    originalBinder: this,
                    diagnose: false,
                    useSiteInfo: ref useSiteInfo);
                diagnostics.Add(syntax, useSiteInfo);

                if (lookupResult.IsSingleViable &&
                    lookupResult.Symbols[0] is PropertySymbol property &&
                    property.GetOwnOrInheritedGetMethod()?.OriginalDefinition is MethodSymbol getMethod &&
                    getMethod.ReturnType.SpecialType == SpecialType.System_Int32 &&
                    getMethod.RefKind == RefKind.None &&
                    !getMethod.IsStatic &&
                    IsAccessible(getMethod, syntax, diagnostics))
                {
                    lookupResult.Clear();
                    valid = property;
                    return true;
                }

                lookupResult.Clear();
                valid = null;
                return false;
            }
        }
#nullable disable

        private ErrorPropertySymbol CreateErrorPropertySymbol(ImmutableArray<PropertySymbol> propertyGroup)
        {
            TypeSymbol propertyType = GetCommonTypeOrReturnType(propertyGroup) ?? CreateErrorType();
            var candidate = propertyGroup[0];
            return new ErrorPropertySymbol(candidate.ContainingType, propertyType, candidate.Name, candidate.IsIndexer, candidate.IsIndexedProperty);
        }

        /// <summary>
        /// Perform lookup and overload resolution on methods defined directly on the class and any
        /// extension methods in scope. Lookup will occur for extension methods in all nested scopes
        /// as necessary until an appropriate method is found. If analyzedArguments is null, the first
        /// method group is returned, without overload resolution being performed. That method group
        /// will either be the methods defined on the receiver class directly (no extension methods)
        /// or the first set of extension methods.
        /// </summary>
        /// <param name="node">The node associated with the method group</param>
        /// <param name="analyzedArguments">The arguments of the invocation (or the delegate type, if a method group conversion)</param>
        /// <param name="useSiteInfo"></param>
        /// <param name="options"></param>
        /// <param name="returnRefKind">If a method group conversion, the desired ref kind of the delegate</param>
        /// <param name="returnType">If a method group conversion, the desired return type of the delegate.
        /// May be null during inference if the return type of the delegate needs to be computed.</param>
        internal MethodGroupResolution ResolveMethodGroup(
            BoundMethodGroup node,
            AnalyzedArguments analyzedArguments,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
            OverloadResolution.Options options,
            bool acceptOnlyMethods,
            RefKind returnRefKind = default,
            TypeSymbol returnType = null,
            in CallingConventionInfo callingConventionInfo = default)
        {
            Debug.Assert((options & ~(OverloadResolution.Options.IsMethodGroupConversion |
                                      OverloadResolution.Options.IsFunctionPointerResolution |
                                      OverloadResolution.Options.InferWithDynamic)) == 0);

            return ResolveMethodGroup(
                node, node.Syntax, node.Name, analyzedArguments, ref useSiteInfo,
                options,
                acceptOnlyMethods: acceptOnlyMethods,
                returnRefKind: returnRefKind, returnType: returnType,
                callingConventionInfo: callingConventionInfo);
        }

        internal MethodGroupResolution ResolveMethodGroup(
            BoundMethodGroup node,
            SyntaxNode expression,
            string memberName,
            AnalyzedArguments analyzedArguments,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
            OverloadResolution.Options options,
            bool acceptOnlyMethods,
            RefKind returnRefKind = default,
            TypeSymbol returnType = null,
            in CallingConventionInfo callingConventionInfo = default)
        {
            var methodResolution = ResolveMethodGroupInternal(
                node, expression, memberName, analyzedArguments, ref useSiteInfo,
                options,
                acceptOnlyMethods: acceptOnlyMethods,
                returnRefKind: returnRefKind, returnType: returnType,
                callingConvention: callingConventionInfo);
            if (methodResolution.IsEmpty && !methodResolution.HasAnyErrors)
            {
                Debug.Assert(node.LookupError == null);

                var diagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, useSiteInfo.AccumulatesDependencies);
                diagnostics.AddRange(methodResolution.Diagnostics); // Could still have use site warnings.
                BindMemberAccessReportError(node, diagnostics);

                // Note: no need to free `methodResolution`, we're transferring the pooled objects it owned
                return new MethodGroupResolution(methodResolution.MethodGroup, methodResolution.OtherSymbol, methodResolution.OverloadResolutionResult, methodResolution.AnalyzedArguments, methodResolution.ResultKind, diagnostics.ToReadOnlyAndFree());
            }
            return methodResolution;
        }

        private MethodGroupResolution ResolveMethodGroupInternal(
            BoundMethodGroup methodGroup,
            SyntaxNode expression,
            string memberName,
            AnalyzedArguments analyzedArguments,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
            OverloadResolution.Options options,
            bool acceptOnlyMethods,
            RefKind returnRefKind = default,
            TypeSymbol returnType = null,
            in CallingConventionInfo callingConvention = default)
        {
            var methodResolution = ResolveDefaultMethodGroup(
                methodGroup, analyzedArguments, ref useSiteInfo,
                options,
                returnRefKind, returnType, callingConvention);

            // If the method group's receiver is dynamic then there is no point in looking for extension methods; 
            // it's going to be a dynamic invocation.
            if (!methodGroup.SearchExtensions || methodResolution.HasAnyApplicableMethod || methodGroup.MethodGroupReceiverIsDynamic())
            {
                return methodResolution;
            }

            var extensionMethodResolution = ResolveExtension(
                expression, memberName, analyzedArguments, methodGroup.ReceiverOpt, methodGroup.TypeArgumentsOpt, options,
                returnRefKind: returnRefKind, returnType: returnType, ref useSiteInfo,
                acceptOnlyMethods: acceptOnlyMethods,
                in callingConvention);
            bool preferExtensionMethodResolution = false;

            if (extensionMethodResolution.HasAnyApplicableMethod)
            {
                preferExtensionMethodResolution = true;
            }
            else if (extensionMethodResolution.IsEmpty)
            {
                preferExtensionMethodResolution = false;
            }
            else if (methodResolution.IsEmpty)
            {
                preferExtensionMethodResolution = true;
            }
            else
            {
                // At this point, both method group resolutions are non-empty but neither contains any applicable method.
                // Choose the MethodGroupResolution with the better (i.e. less worse) result kind.

                Debug.Assert(!methodResolution.HasAnyApplicableMethod);
                Debug.Assert(!extensionMethodResolution.HasAnyApplicableMethod);
                Debug.Assert(!methodResolution.IsEmpty);
                Debug.Assert(!extensionMethodResolution.IsEmpty);

                LookupResultKind methodResultKind = methodResolution.ResultKind;
                LookupResultKind extensionMethodResultKind = extensionMethodResolution.ResultKind;
                if (methodResultKind != extensionMethodResultKind &&
                    methodResultKind == extensionMethodResultKind.WorseResultKind(methodResultKind))
                {
                    preferExtensionMethodResolution = true;
                }
            }

            if (preferExtensionMethodResolution)
            {
                methodResolution.Free();
                Debug.Assert(!extensionMethodResolution.IsEmpty);
                return extensionMethodResolution;  //NOTE: the first argument of this MethodGroupResolution could be a BoundTypeOrValueExpression
            }

            extensionMethodResolution.Free();

            return methodResolution;
        }

        private MethodGroupResolution ResolveDefaultMethodGroup(
            BoundMethodGroup node,
            AnalyzedArguments analyzedArguments,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
            OverloadResolution.Options options,
            RefKind returnRefKind = default,
            TypeSymbol returnType = null,
            in CallingConventionInfo callingConvention = default)
        {
            Debug.Assert((options & ~(OverloadResolution.Options.IsMethodGroupConversion |
                                      OverloadResolution.Options.IsFunctionPointerResolution |
                                      OverloadResolution.Options.InferWithDynamic |
                                      OverloadResolution.Options.IgnoreNormalFormIfHasValidParamsParameter |
                                      OverloadResolution.Options.DisallowExpandedNonArrayParams |
                                      OverloadResolution.Options.DynamicResolution |
                                      OverloadResolution.Options.DynamicConvertsToAnything)) == 0);

            var methods = node.Methods;
            if (methods.Length == 0)
            {
                var method = node.LookupSymbolOpt as MethodSymbol;
                if ((object)method != null)
                {
                    methods = ImmutableArray.Create(method);
                }
            }

            var sealedDiagnostics = ReadOnlyBindingDiagnostic<AssemblySymbol>.Empty;
            if (node.LookupError != null)
            {
                var diagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
                Error(diagnostics, node.LookupError, node.NameSyntax);
                sealedDiagnostics = diagnostics.ToReadOnlyAndFree();
            }

            if (methods.Length == 0)
            {
                return new MethodGroupResolution(node.LookupSymbolOpt, node.ResultKind, sealedDiagnostics);
            }

            var methodGroup = MethodGroup.GetInstance();
            // NOTE: node.ReceiverOpt could be a BoundTypeOrValueExpression - users need to check.
            methodGroup.PopulateWithNonExtensionMethods(node.ReceiverOpt, methods, node.TypeArgumentsOpt, node.ResultKind, node.LookupError);

            if (node.LookupError != null)
            {
                return new MethodGroupResolution(methodGroup, sealedDiagnostics);
            }

            // Arguments will be null if the caller is resolving to the first available
            // method group, regardless of arguments, when the signature cannot
            // be inferred. (In the error case of o.M = null; for instance.)
            if (analyzedArguments == null)
            {
                return new MethodGroupResolution(methodGroup, sealedDiagnostics);
            }
            else
            {
                var result = OverloadResolutionResult<MethodSymbol>.GetInstance();
                if (AllowRefOmittedArguments(methodGroup.Receiver))
                {
                    options |= OverloadResolution.Options.AllowRefOmittedArguments;
                }

                OverloadResolution.MethodInvocationOverloadResolution(
                    methodGroup.Methods,
                    methodGroup.TypeArguments,
                    methodGroup.Receiver,
                    analyzedArguments,
                    result,
                    ref useSiteInfo,
                    options,
                    returnRefKind,
                    returnType,
                    callingConvention);

                // Note: the MethodGroupResolution instance is responsible for freeing its copy of analyzed arguments
                return new MethodGroupResolution(methodGroup, null, result, AnalyzedArguments.GetInstance(analyzedArguments), methodGroup.ResultKind, sealedDiagnostics);
            }
        }

#nullable enable
        internal NamedTypeSymbol? GetMethodGroupDelegateType(BoundMethodGroup node)
        {
            var method = GetUniqueSignatureFromMethodGroup(node, out bool useParams);
            if (method is null)
            {
                return null;
            }

            return GetMethodGroupOrLambdaDelegateType(node.Syntax, method, hasParams: useParams);
        }

        /// <summary>
        /// Returns one of the methods from the method group if all methods in the method group
        /// have the same signature, ignoring parameter names and custom modifiers. The particular
        /// method returned is not important since the caller is interested in the signature only.
        /// </summary>
        /// <param name="useParams">
        /// Whether the last parameter of the signature should have the <see langword="params"/> modifier.
        /// </param>
        private MethodSymbol? GetUniqueSignatureFromMethodGroup_CSharp10(BoundMethodGroup node, out bool useParams)
        {
            MethodSymbol? method = null;
            var methods = ArrayBuilder<MethodSymbol>.GetInstance(capacity: node.Methods.Length);
            foreach (var m in node.Methods)
            {
                switch (node.ReceiverOpt)
                {
                    case BoundTypeExpression:
                    case null: // if `using static Class` is in effect, the receiver is missing
                        if (!m.IsStatic) continue;
                        break;
                    case BoundThisReference { WasCompilerGenerated: true }:
                        break;
                    default:
                        if (m.IsStatic) continue;
                        break;
                }
                methods.Add(m);
            }

            if (!OverloadResolution.FilterMethodsForUniqueSignature(methods, out useParams))
            {
                methods.Free();
                return null;
            }

            var seenAnyApplicableCandidates = methods.Count != 0;

            foreach (var m in methods)
            {
                if (!isCandidateUnique(ref method, m))
                {
                    methods.Free();
                    useParams = false;
                    return null;
                }
            }

            if (node.SearchExtensions)
            {
                Debug.Assert(node.ReceiverOpt!.Type is not null); // extensions are only considered on member access

                BoundExpression receiver = node.ReceiverOpt;
                ImmutableArray<TypeWithAnnotations> typeArguments = node.TypeArgumentsOpt;
                int arity = typeArguments.IsDefaultOrEmpty ? 0 : typeArguments.Length;
                LookupOptions options = arity == 0 ? LookupOptions.AllMethodsOnArityZero : LookupOptions.Default;
                var singleLookupResults = ArrayBuilder<SingleLookupResult>.GetInstance();
                CompoundUseSiteInfo<AssemblySymbol> discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;

                foreach (var scope in new ExtensionScopes(this))
                {
                    methods.Clear();
                    singleLookupResults.Clear();
                    scope.Binder.EnumerateAllExtensionMembersInSingleBinder(singleLookupResults, node.Name, arity, options, originalBinder: this, ref discardedUseSiteInfo, ref discardedUseSiteInfo);

                    foreach (SingleLookupResult singleLookupResult in singleLookupResults)
                    {
                        var extensionMember = singleLookupResult.Symbol;
                        Debug.Assert(extensionMember is not null);
                        if (IsStaticInstanceMismatchForUniqueSignatureFromMethodGroup(receiver, extensionMember))
                        {
                            // Remove static/instance mismatches
                            continue;
                        }

                        // Note: we only care about methods. If the expression resolved to a non-method extension member, we wouldn't get here to compute the function type for the expression.
                        if (extensionMember is MethodSymbol m)
                        {
                            if (m.IsExtensionBlockMember())
                            {
                                // Note: new extension methods are subject to more stringent checks
                                var substituted = (MethodSymbol?)extensionMember.GetReducedAndFilteredSymbol(typeArguments, receiver.Type, Compilation, checkFullyInferred: true);
                                if (substituted is not null)
                                {
                                    methods.Add(substituted);
                                }
                            }
                            else if (m.ReduceExtensionMethod(receiver.Type, Compilation) is { } reduced)
                            {
                                methods.Add(reduced);
                            }
                        }
                    }

                    if (methods.Count == 0)
                    {
                        continue;
                    }

                    if (!OverloadResolution.FilterMethodsForUniqueSignature(methods, out bool useParamsForScope))
                    {
                        methods.Free();
                        useParams = false;
                        singleLookupResults.Free();
                        return null;
                    }

                    Debug.Assert(methods.Count != 0);

                    // If we had some candidates that differ in `params` from the current scope, we don't have a unique signature.
                    if (seenAnyApplicableCandidates && useParamsForScope != useParams)
                    {
                        methods.Free();
                        useParams = false;
                        singleLookupResults.Free();
                        return null;
                    }

                    useParams = useParamsForScope;
                    seenAnyApplicableCandidates = true;

                    foreach (var reduced in methods)
                    {
                        if (!isCandidateUnique(ref method, reduced))
                        {
                            methods.Free();
                            useParams = false;
                            singleLookupResults.Free();
                            return null;
                        }
                    }
                }

                singleLookupResults.Free();
            }

            methods.Free();

            if (method is null)
            {
                useParams = false;
                return null;
            }
            int n = node.TypeArgumentsOpt.IsDefaultOrEmpty ? 0 : node.TypeArgumentsOpt.Length;
            if (method.Arity != n)
            {
                useParams = false;
                return null;
            }
            else if (n > 0)
            {
                method = method.ConstructedFrom.Construct(node.TypeArgumentsOpt);
            }
            return method;

            static bool isCandidateUnique(ref MethodSymbol? method, MethodSymbol candidate)
            {
                if (method is null)
                {
                    method = candidate;
                    return true;
                }
                if (MemberSignatureComparer.CSharp10MethodGroupSignatureComparer.Equals(method, candidate))
                {
                    return true;
                }
                method = null;
                return false;
            }
        }

        private static bool IsStaticInstanceMismatchForUniqueSignatureFromMethodGroup(BoundExpression receiver, Symbol extensionMember)
        {
            bool memberCountsAsStatic = extensionMember is MethodSymbol { IsExtensionMethod: true } ? false : extensionMember.IsStatic;
            return receiver switch
            {
                BoundTypeOrValueExpression => false,
                BoundTypeExpression => !memberCountsAsStatic,
                _ => memberCountsAsStatic,
            };
        }

        /// <summary>
        /// For C# 13 onwards, returns one of the methods from the method group if all instance methods, or extension methods
        /// in the nearest scope, have the same signature ignoring parameter names and custom modifiers.
        /// The particular method returned is not important since the caller is interested in the signature only.
        /// </summary>
        /// <param name="useParams">
        /// Whether the last parameter of the signature should have the <see langword="params"/> modifier.
        /// </param>
        private MethodSymbol? GetUniqueSignatureFromMethodGroup(BoundMethodGroup node, out bool useParams)
        {
            if (Compilation.LanguageVersion < LanguageVersion.CSharp13)
            {
                return GetUniqueSignatureFromMethodGroup_CSharp10(node, out useParams);
            }

            useParams = false;
            MethodSymbol? foundMethod = null;
            var typeArguments = node.TypeArgumentsOpt;
            int arity = typeArguments.IsDefaultOrEmpty ? 0 : typeArguments.Length;

            // 1. instance methods
            if (node.ResultKind == LookupResultKind.Viable)
            {
                var methods = ArrayBuilder<MethodSymbol>.GetInstance(capacity: node.Methods.Length);
                foreach (var memberMethod in node.Methods)
                {
                    switch (node.ReceiverOpt)
                    {
                        case BoundTypeOrValueExpression:
                            break;
                        case BoundTypeExpression:
                        case null: // if `using static Class` is in effect, the receiver is missing
                            if (!memberMethod.IsStatic) continue;
                            break;
                        case BoundThisReference { WasCompilerGenerated: true }:
                            break;
                        default:
                            if (memberMethod.IsStatic) continue;
                            break;
                    }

                    if (memberMethod.Arity != arity)
                    {
                        // We have no way of inferring type arguments, so if the given type arguments
                        // don't match the method's arity, the method is not a candidate
                        continue;
                    }

                    var substituted = typeArguments.IsDefaultOrEmpty ? memberMethod : memberMethod.Construct(typeArguments);
                    if (!satisfiesConstraintChecks(substituted))
                    {
                        continue;
                    }

                    methods.Add(substituted);
                }

                if (!OverloadResolution.FilterMethodsForUniqueSignature(methods, out useParams))
                {
                    methods.Free();
                    return null;
                }

                foreach (var substituted in methods)
                {
                    if (!isCandidateUnique(ref foundMethod, substituted))
                    {
                        methods.Free();
                        useParams = false;
                        return null;
                    }
                }

                methods.Free();

                if (foundMethod is not null)
                {
                    return foundMethod;
                }
            }

            // 2. extensions
            if (node.SearchExtensions)
            {
                Debug.Assert(node.ReceiverOpt!.Type is not null); // extensions are only considered on member access

                BoundExpression receiver = node.ReceiverOpt;
                LookupOptions options = arity == 0 ? LookupOptions.AllMethodsOnArityZero : LookupOptions.Default;
                var singleLookupResults = ArrayBuilder<SingleLookupResult>.GetInstance();
                CompoundUseSiteInfo<AssemblySymbol> discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;

                foreach (var scope in new ExtensionScopes(this))
                {
                    singleLookupResults.Clear();
                    scope.Binder.EnumerateAllExtensionMembersInSingleBinder(singleLookupResults, node.Name, arity, options, originalBinder: this, ref discardedUseSiteInfo, ref discardedUseSiteInfo);

                    var methods = ArrayBuilder<MethodSymbol>.GetInstance(capacity: singleLookupResults.Count);
                    foreach (SingleLookupResult singleLookupResult in singleLookupResults)
                    {
                        var extensionMember = singleLookupResult.Symbol;
                        Debug.Assert(extensionMember is not null);
                        if (IsStaticInstanceMismatchForUniqueSignatureFromMethodGroup(receiver, extensionMember))
                        {
                            // Remove static/instance mismatches
                            continue;
                        }

                        // Note: we only care about methods since we're already decided this is a method group (ie. not resolving to some other kind of extension member)
                        if (extensionMember is MethodSymbol)
                        {
                            var substituted = (MethodSymbol?)extensionMember.GetReducedAndFilteredSymbol(typeArguments, receiver.Type, Compilation, checkFullyInferred: true);
                            if (substituted is not null)
                            {
                                methods.Add(substituted);
                            }
                        }
                    }

                    if (!OverloadResolution.FilterMethodsForUniqueSignature(methods, out useParams))
                    {
                        singleLookupResults.Free();
                        methods.Free();
                        return null;
                    }

                    foreach (var method in methods)
                    {
                        if (!isCandidateUnique(ref foundMethod, method))
                        {
                            singleLookupResults.Free();
                            methods.Free();
                            useParams = false;
                            return null;
                        }
                    }

                    methods.Free();

                    if (foundMethod is not null)
                    {
                        singleLookupResults.Free();
                        return foundMethod;
                    }
                }

                singleLookupResults.Free();
            }

            useParams = false;
            return null;

            static bool isCandidateUnique(ref MethodSymbol? foundMethod, MethodSymbol candidate)
            {
                if (foundMethod is null)
                {
                    foundMethod = candidate;
                    return true;
                }
                if (MemberSignatureComparer.MethodGroupSignatureComparer.Equals(foundMethod, candidate))
                {
                    return true;
                }
                foundMethod = null;
                return false;
            }

            bool satisfiesConstraintChecks(MethodSymbol method)
            {
                if (!ConstraintsHelper.RequiresChecking(method))
                {
                    return true;
                }

                var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
                ArrayBuilder<TypeParameterDiagnosticInfo>? useSiteDiagnosticsBuilder = null;

                bool constraintsSatisfied = ConstraintsHelper.CheckMethodConstraints(
                    method,
                    new ConstraintsHelper.CheckConstraintsArgs(this.Compilation, this.Conversions, includeNullability: false, location: NoLocation.Singleton, diagnostics: null),
                    diagnosticsBuilder,
                    nullabilityDiagnosticsBuilderOpt: null,
                    ref useSiteDiagnosticsBuilder);

                diagnosticsBuilder.Free();
                useSiteDiagnosticsBuilder?.Free();

                return constraintsSatisfied;
            }
        }

        // This method was adapted from LoweredDynamicOperationFactory.GetDelegateType().
        internal NamedTypeSymbol? GetMethodGroupOrLambdaDelegateType(
            SyntaxNode syntax,
            MethodSymbol methodSymbol,
            bool hasParams,
            ImmutableArray<ScopedKind>? parameterScopesOverride = null,
            ImmutableArray<bool>? parameterHasUnscopedRefAttributesOverride = null,
            RefKind? returnRefKindOverride = null,
            TypeWithAnnotations? returnTypeOverride = null)
        {
            var parameters = methodSymbol.Parameters;
            var parameterRefKinds = methodSymbol.ParameterRefKinds;
            var parameterTypes = methodSymbol.ParameterTypesWithAnnotations;
            var returnType = returnTypeOverride ?? methodSymbol.ReturnTypeWithAnnotations;
            var returnRefKind = returnRefKindOverride ?? methodSymbol.RefKind;
            var parameterScopes = parameterScopesOverride ??
                (parameters.Any(p => p.EffectiveScope != ScopedKind.None) ? parameters.SelectAsArray(p => p.EffectiveScope) : default);
            var parameterHasUnscopedRefAttributes = parameterHasUnscopedRefAttributesOverride ??
                (parameters.Any(p => p.HasUnscopedRefAttribute && p.UseUpdatedEscapeRules) ? parameters.SelectAsArray(p => p.HasUnscopedRefAttribute && p.UseUpdatedEscapeRules) : default);
            var parameterDefaultValues = parameters.Any(p => p.HasExplicitDefaultValue) ?
                parameters.SelectAsArray(p => p.ExplicitDefaultConstantValue) :
                default;

            Debug.Assert(ContainingMemberOrLambda is { });
            Debug.Assert(parameterRefKinds.IsDefault || parameterRefKinds.Length == parameterTypes.Length);
            Debug.Assert(parameterDefaultValues.IsDefault || parameterDefaultValues.Length == parameterTypes.Length);
            Debug.Assert(returnType.Type is { }); // Expecting System.Void rather than null return type.
            Debug.Assert(!hasParams || parameterTypes.Length != 0);

            bool returnsVoid = returnType.Type.IsVoidType();
            var typeArguments = returnsVoid ? parameterTypes : parameterTypes.Add(returnType);

            if (returnsVoid && returnRefKind != RefKind.None)
            {
                // Invalid return type.
                return null;
            }

            if (!typeArguments.All(t => t.HasType))
            {
                // Invalid parameter or return type.
                return null;
            }

            // Use System.Action<...> or System.Func<...> if possible.
            if (!hasParams &&
                returnRefKind == RefKind.None &&
                parameterDefaultValues.IsDefault &&
                (parameterRefKinds.IsDefault || parameterRefKinds.All(refKind => refKind == RefKind.None)) &&
                (parameterScopes.IsDefault || parameterScopes.All(scope => scope == ScopedKind.None)) &&
                (parameterHasUnscopedRefAttributes.IsDefault || parameterHasUnscopedRefAttributes.All(p => !p)))
            {
                var wkDelegateType = returnsVoid ?
                    WellKnownTypes.GetWellKnownActionDelegate(invokeArgumentCount: parameterTypes.Length) :
                    WellKnownTypes.GetWellKnownFunctionDelegate(invokeArgumentCount: parameterTypes.Length);

                if (wkDelegateType != WellKnownType.Unknown)
                {
                    // The caller of GetMethodGroupOrLambdaDelegateType() is responsible for
                    // checking and reporting use-site diagnostics for the returned delegate type.
                    var delegateType = Compilation.GetWellKnownType(wkDelegateType);
                    if (typeArguments.Length == 0)
                    {
                        return delegateType;
                    }
                    if (checkConstraints(Compilation, Conversions, delegateType, typeArguments))
                    {
                        return delegateType.Construct(typeArguments);
                    }
                }
            }

            // Synthesize a delegate type for other cases.
            var fieldsBuilder = ArrayBuilder<AnonymousTypeField>.GetInstance(parameterTypes.Length + 1);
            var location = syntax.Location;
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                fieldsBuilder.Add(
                    new AnonymousTypeField(
                        name: "",
                        location,
                        parameterTypes[i],
                        parameterRefKinds.IsDefault ? RefKind.None : parameterRefKinds[i],
                        parameterScopes.IsDefault ? ScopedKind.None : parameterScopes[i],
                        parameterDefaultValues.IsDefault ? null : parameterDefaultValues[i],
                        isParams: hasParams && i == parameterTypes.Length - 1,
                        hasUnscopedRefAttribute: parameterHasUnscopedRefAttributes.IsDefault ? false : parameterHasUnscopedRefAttributes[i]));
            }
            fieldsBuilder.Add(new AnonymousTypeField(name: "", location, returnType, returnRefKind, ScopedKind.None));

            var typeDescr = new AnonymousTypeDescriptor(fieldsBuilder.ToImmutableAndFree(), location);
            return Compilation.AnonymousTypeManager.ConstructAnonymousDelegateSymbol(typeDescr);

            static bool checkConstraints(CSharpCompilation compilation, ConversionsBase conversions, NamedTypeSymbol delegateType, ImmutableArray<TypeWithAnnotations> typeArguments)
            {
                var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
                var typeParameters = delegateType.TypeParameters;
                var substitution = new TypeMap(typeParameters, typeArguments);
                ArrayBuilder<TypeParameterDiagnosticInfo>? useSiteDiagnosticsBuilder = null;
                var result = delegateType.CheckConstraints(
                    new ConstraintsHelper.CheckConstraintsArgs(compilation, conversions, includeNullability: false, NoLocation.Singleton, diagnostics: null, template: CompoundUseSiteInfo<AssemblySymbol>.Discarded),
                    substitution,
                    typeParameters,
                    typeArguments,
                    diagnosticsBuilder,
                    nullabilityDiagnosticsBuilderOpt: null,
                    ref useSiteDiagnosticsBuilder);
                diagnosticsBuilder.Free();
                return result;
            }
        }
#nullable disable

        internal static bool ReportDelegateInvokeUseSiteDiagnostic(BindingDiagnosticBag diagnostics, TypeSymbol possibleDelegateType,
            Location location = null, SyntaxNode node = null)
        {
            Debug.Assert((location == null) ^ (node == null));

            if (!possibleDelegateType.IsDelegateType())
            {
                return false;
            }

            MethodSymbol invoke = possibleDelegateType.DelegateInvokeMethod();
            if ((object)invoke == null)
            {
                diagnostics.Add(new CSDiagnosticInfo(ErrorCode.ERR_InvalidDelegateType, possibleDelegateType), getErrorLocation());
                return true;
            }

            UseSiteInfo<AssemblySymbol> info = invoke.GetUseSiteInfo();
            diagnostics.AddDependencies(info);

            DiagnosticInfo diagnosticInfo = info.DiagnosticInfo;
            if (diagnosticInfo == null)
            {
                return false;
            }

            if (diagnosticInfo.Code == (int)ErrorCode.ERR_InvalidDelegateType)
            {
                diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_InvalidDelegateType, possibleDelegateType), getErrorLocation()));
                return true;
            }

            return Symbol.ReportUseSiteDiagnostic(diagnosticInfo, diagnostics, getErrorLocation());

            Location getErrorLocation()
                => location ?? GetAnonymousFunctionLocation(node);
        }

        private BoundConditionalAccess BindConditionalAccessExpression(ConditionalAccessExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            MessageID.IDS_FeatureNullPropagatingOperator.CheckFeatureAvailability(diagnostics, node.OperatorToken);

            BoundExpression receiver = BindConditionalAccessReceiver(node, diagnostics);

            var conditionalAccessBinder = new BinderWithConditionalReceiver(this, receiver);
            var access = conditionalAccessBinder.BindValue(node.WhenNotNull, diagnostics, BindValueKind.RValue);
            if (access.Syntax is AssignmentExpressionSyntax assignment)
            {
                MessageID.IDS_FeatureNullConditionalAssignment.CheckFeatureAvailability(diagnostics, assignment.OperatorToken);
            }

            if (receiver.HasAnyErrors || access.HasAnyErrors)
            {
                return new BoundConditionalAccess(node, receiver, access, CreateErrorType(), hasErrors: true);
            }

            var receiverType = receiver.Type;
            Debug.Assert((object)receiverType != null);

            // access cannot be a method group
            if (access.Kind == BoundKind.MethodGroup)
            {
                return GenerateBadConditionalAccessNodeError(node, receiver, access, diagnostics);
            }

            var accessType = access.Type;

            // access cannot have no type
            if ((object)accessType == null)
            {
                return GenerateBadConditionalAccessNodeError(node, receiver, access, diagnostics);
            }

            // The resulting type must be either a reference type T, Nullable<T>, or a pointer type.
            // Therefore we must reject cases resulting in types that are not reference types and cannot be lifted into nullable.
            // - access cannot have unconstrained generic type
            // - access cannot be a restricted type
            // Note: Pointers (including function pointers) are allowed because they can represent null (as the zero value).
            if ((!accessType.IsReferenceType && !accessType.IsValueType) || accessType.IsRestrictedType())
            {
                // Result type of the access is void when result value cannot be made nullable.
                // For improved diagnostics we detect the cases where the value will be used and produce a
                // more specific (though not technically correct) diagnostic here:
                // "Error CS0023: Operator '?' cannot be applied to operand of type 'T'"
                if (ResultIsUsed(node))
                {
                    return GenerateBadConditionalAccessNodeError(node, receiver, access, diagnostics);
                }

                accessType = GetSpecialType(SpecialType.System_Void, diagnostics, node);
            }

            // if access has value type (but not a pointer), the type of the conditional access is nullable of that
            // https://github.com/dotnet/roslyn/issues/35075: The test `accessType.IsValueType && !accessType.IsNullableType()`
            // should probably be `accessType.IsNonNullableValueType()`
            // Note: As far as the language is concerned, pointers (including function pointers) are not value types.
            // However, due to a historical quirk in the compiler implementation, we do treat them as value types.
            // Since we're checking for value types here, we exclude pointers to avoid wrapping them in Nullable<>.
            if (accessType.IsValueType && !accessType.IsNullableType() && !accessType.IsVoidType() && !accessType.IsPointerOrFunctionPointer())
            {
                accessType = GetSpecialType(SpecialType.System_Nullable_T, diagnostics, node).Construct(accessType);
            }

            return new BoundConditionalAccess(node, receiver, access, accessType);
        }

        private bool ResultIsUsed(ExpressionSyntax node)
        {
            bool resultIsUsed = true;
            CSharpSyntaxNode parent = node.Parent;

            if (parent != null)
            {
                switch (parent.Kind())
                {
                    case SyntaxKind.ExpressionStatement:
                        resultIsUsed = ((ExpressionStatementSyntax)parent).Expression != node;
                        break;

                    case SyntaxKind.SimpleLambdaExpression:
                        resultIsUsed = (((SimpleLambdaExpressionSyntax)parent).Body != node) || MethodOrLambdaRequiresValue(ContainingMemberOrLambda, Compilation);
                        break;

                    case SyntaxKind.ParenthesizedLambdaExpression:
                        resultIsUsed = (((ParenthesizedLambdaExpressionSyntax)parent).Body != node) || MethodOrLambdaRequiresValue(ContainingMemberOrLambda, Compilation);
                        break;

                    case SyntaxKind.ArrowExpressionClause:
                        resultIsUsed = (((ArrowExpressionClauseSyntax)parent).Expression != node) || MethodOrLambdaRequiresValue(ContainingMemberOrLambda, Compilation);
                        break;

                    case SyntaxKind.ForStatement:
                        // Incrementors and Initializers doesn't have to produce a value
                        var loop = (ForStatementSyntax)parent;
                        resultIsUsed = !loop.Incrementors.Contains(node) && !loop.Initializers.Contains(node);
                        break;
                }
            }

            return resultIsUsed;
        }

        internal static bool MethodOrLambdaRequiresValue(Symbol symbol, CSharpCompilation compilation)
        {
            return symbol is MethodSymbol method &&
                !method.ReturnsVoid &&
                !method.IsAsyncEffectivelyReturningTask(compilation);
        }

        private BoundConditionalAccess GenerateBadConditionalAccessNodeError(ConditionalAccessExpressionSyntax node, BoundExpression receiver, BoundExpression access, BindingDiagnosticBag diagnostics)
        {
            DiagnosticInfo diagnosticInfo = new CSDiagnosticInfo(ErrorCode.ERR_CannotBeMadeNullable, access.Display);
            diagnostics.Add(new CSDiagnostic(diagnosticInfo, access.Syntax.Location));
            receiver = BadExpression(receiver.Syntax, receiver);

            return new BoundConditionalAccess(node, receiver, access, CreateErrorType(), hasErrors: true);
        }

        private BoundExpression BindMemberBindingExpression(MemberBindingExpressionSyntax node, bool invoked, bool indexed, BindingDiagnosticBag diagnostics)
        {
            BoundExpression receiver = GetReceiverForConditionalBinding(node, diagnostics);

            var memberAccess = BindMemberAccessWithBoundLeft(node, receiver, node.Name, node.OperatorToken, invoked, indexed, diagnostics);
            return memberAccess;
        }

        private BoundExpression BindElementBindingExpression(ElementBindingExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            BoundExpression receiver = GetReceiverForConditionalBinding(node, diagnostics);

            var memberAccess = BindElementAccess(node, receiver, node.ArgumentList, allowInlineArrayElementAccess: true, diagnostics);
            return memberAccess;
        }

        private static CSharpSyntaxNode GetConditionalReceiverSyntax(ConditionalAccessExpressionSyntax node)
        {
            Debug.Assert(node != null);
            Debug.Assert(node.Expression != null);

            var receiver = node.Expression;
            while (receiver.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                receiver = ((ParenthesizedExpressionSyntax)receiver).Expression;
                Debug.Assert(receiver != null);
            }

            return receiver;
        }

        private BoundExpression GetReceiverForConditionalBinding(ExpressionSyntax binding, BindingDiagnosticBag diagnostics)
        {
            var conditionalAccessNode = SyntaxFactory.FindConditionalAccessNodeForBinding(binding);
            Debug.Assert(conditionalAccessNode != null);

            BoundExpression receiver = this.ConditionalReceiverExpression;
            if (receiver?.Syntax != GetConditionalReceiverSyntax(conditionalAccessNode))
            {
                // this can happen when semantic model binds parts of a Call or a broken access expression. 
                // We may not have receiver available in such cases.
                // Not a problem - we only need receiver to get its type and we can bind it here.
                receiver = BindConditionalAccessReceiver(conditionalAccessNode, diagnostics);
            }

            // create surrogate receiver
            var receiverType = receiver.Type;
            if (receiverType?.IsNullableType() == true)
            {
                receiverType = receiverType.GetNullableUnderlyingType();
            }

            receiver = new BoundConditionalReceiver(receiver.Syntax, 0, receiverType ?? CreateErrorType(), hasErrors: receiver.HasErrors) { WasCompilerGenerated = true };
            return receiver;
        }

        private BoundExpression BindConditionalAccessReceiver(ConditionalAccessExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            var receiverSyntax = node.Expression;
            var receiver = BindRValueWithoutTargetType(receiverSyntax, diagnostics);
            receiver = MakeMemberAccessValue(receiver, diagnostics);

            if (receiver.HasAnyErrors)
            {
                return receiver;
            }

            var operatorToken = node.OperatorToken;

            if (receiver.Kind == BoundKind.UnboundLambda)
            {
                var msgId = ((UnboundLambda)receiver).MessageID;
                DiagnosticInfo diagnosticInfo = new CSDiagnosticInfo(ErrorCode.ERR_BadUnaryOp, SyntaxFacts.GetText(operatorToken.Kind()), msgId.Localize());
                diagnostics.Add(new CSDiagnostic(diagnosticInfo, node.Location));
                return BadExpression(receiverSyntax, receiver);
            }

            var receiverType = receiver.Type;

            // Can't dot into the null literal or anything that has no type
            if ((object)receiverType == null)
            {
                Error(diagnostics, ErrorCode.ERR_BadUnaryOp, operatorToken.GetLocation(), operatorToken.Text, receiver.Display);
                return BadExpression(receiverSyntax, receiver);
            }

            // No member accesses on void
            if (receiverType.IsVoidType())
            {
                Error(diagnostics, ErrorCode.ERR_BadUnaryOp, operatorToken.GetLocation(), operatorToken.Text, receiverType);
                return BadExpression(receiverSyntax, receiver);
            }

            if (receiverType.IsValueType && !receiverType.IsNullableType())
            {
                // must be nullable or reference type
                Error(diagnostics, ErrorCode.ERR_BadUnaryOp, operatorToken.GetLocation(), operatorToken.Text, receiverType);
                return BadExpression(receiverSyntax, receiver);
            }

            return receiver;
        }
    }
}
