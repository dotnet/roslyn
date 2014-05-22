// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        private bool HasThis(bool isExplicit, out bool inStaticContext)
        {
            var member = this.ContainingMemberOrLambda.ContainingNonLambdaMember();
            if (member.IsStatic)
            {
                inStaticContext = member.Kind == SymbolKind.Field || member.Kind == SymbolKind.Method || member.Kind == SymbolKind.Property;
                return false;
            }

            inStaticContext = false;

            if (InConstructorInitializer || InAttributeArgument)
            {
                return false;
            }

            var containingType = member.ContainingType;
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

        protected bool InCref
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
            return next.IsUnboundTypeAllowed(syntax);
        }

        /// <summary>
        /// Generates a new BoundBadExpression with no known type, and the given bound children.
        /// </summary>
        private BoundBadExpression BadExpression(CSharpSyntaxNode syntax, params BoundNode[] childNodes)
        {
            return BadExpression(syntax, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, childNodes);
        }

        /// <summary>
        /// Generates a new BoundBadExpression with no known type, given lookup resultKind and the given bound children.
        /// </summary>
        protected BoundBadExpression BadExpression(CSharpSyntaxNode syntax, LookupResultKind lookupResultKind, params BoundNode[] childNodes)
        {
            return BadExpression(syntax, lookupResultKind, ImmutableArray<Symbol>.Empty, childNodes);
        }

        /// <summary>
        /// Generates a new BoundBadExpression with no known type, given lookupResultKind and given symbols for GetSemanticInfo API,
        /// and the given bound children.
        /// </summary>
        private BoundBadExpression BadExpression(CSharpSyntaxNode syntax, LookupResultKind resultKind, ImmutableArray<Symbol> symbols, params BoundNode[] childNodes)
        {
            return new BoundBadExpression(syntax,
                resultKind,
                symbols,
                ImmutableArray.Create<BoundNode>(childNodes),
                CreateErrorType());
        }

        /// <summary>
        /// Generates a new BoundBadExpression with no known type, given lookupResultKind and given symbols for GetSemanticInfo API,
        /// and the given bound children.
        /// </summary>
        private BoundBadExpression BadExpression(CSharpSyntaxNode syntax, LookupResultKind resultKind, ImmutableArray<Symbol> symbols, ImmutableArray<BoundExpression> childNodes)
        {
            return new BoundBadExpression(syntax,
                resultKind,
                symbols,
                StaticCast<BoundNode>.From(childNodes),
                CreateErrorType());
        }

        /// <summary>
        /// Helper method to generate a bound expression with HasErrors set to true.
        /// Returned bound expression is guaranteed to have a non-null type, except when <paramref name="expr"/> is an unbound lambda.
        /// If <paramref name="expr"/> already has errors and meets the above type requirements, then it is returned unchanged.
        /// Otherwise, if <paramref name="expr"/> is a BoundBadExpression, then it is updated with the <paramref name="resultKind"/> and non-null type.
        /// Otherwise, a new BoundBadExpression wrapping <paramref name="expr"/> is returned. 
        /// </summary>
        /// <remarks>
        /// Returned expression need not be a BoundBadExpression, but is guaranteed to have HasErrors set to true.
        /// </remarks>
        private BoundExpression ToBadExpression(BoundExpression expr, LookupResultKind resultKind = LookupResultKind.Empty)
        {
            Debug.Assert(expr != null);
            Debug.Assert(resultKind != LookupResultKind.Viable);

            TypeSymbol resultType = expr.Type;
            BoundKind exprKind = expr.Kind;

            if (expr.HasAnyErrors && ((object)resultType != null || exprKind == BoundKind.UnboundLambda))
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
                    ImmutableArray.Create<BoundNode>(expr),
                    resultType ?? CreateErrorType());
            }
        }

        internal TypeSymbol CreateErrorType(string name = "")
        {
            return new ExtendedErrorTypeSymbol(this.Compilation, name, arity: 0, errorInfo: null, unreported: false);
        }

        private static bool RequiresGettingValue(BindValueKind kind)
        {
            switch (kind)
            {
                case BindValueKind.RValue:
                case BindValueKind.RValueOrMethodGroup:
                case BindValueKind.CompoundAssignment:
                case BindValueKind.IncrementDecrement:
                    return true;

                case BindValueKind.OutParameter:
                case BindValueKind.AddressOf:
                case BindValueKind.Assignment:
                    return false;

                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private static bool RequiresSettingValue(BindValueKind kind)
        {
            switch (kind)
            {
                case BindValueKind.RValue:
                case BindValueKind.RValueOrMethodGroup:
                    return false;

                case BindValueKind.CompoundAssignment:
                case BindValueKind.IncrementDecrement:
                case BindValueKind.OutParameter:
                case BindValueKind.AddressOf:
                case BindValueKind.Assignment:
                    return true;

                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        /// <summary>
        /// Bind the expression and verify the expression matches the combination of lvalue and
        /// rvalue requirements given by valueKind. If the expression was bound successfully, but
        /// did not meet the requirements, the return value will be a BoundBadExpression that
        /// (typically) wraps the subexpression.
        /// </summary>
        internal BoundExpression BindValue(ExpressionSyntax node, DiagnosticBag diagnostics, BindValueKind valueKind)
        {
            var result = this.BindExpression(node, diagnostics: diagnostics, invoked: false, indexed: false);
            return CheckValue(result, valueKind, diagnostics);
        }

        internal BoundExpression BindValueAllowArgList(ExpressionSyntax node, DiagnosticBag diagnostics, BindValueKind valueKind)
        {
            var result = this.BindExpressionAllowArgList(node, diagnostics: diagnostics);
            return CheckValue(result, valueKind, diagnostics);
        }

        internal BoundExpression BindVariableOrAutoPropInitializer(
            EqualsValueClauseSyntax initializerOpt,
            TypeSymbol varType,
            DiagnosticBag diagnostics)
        {
            if (initializerOpt == null)
            {
                return null;
            }

            var initializer = BindPossibleArrayInitializer(initializerOpt.Value, varType, diagnostics);
            return GenerateConversionForAssignment(varType, initializer, diagnostics);
        }

        internal BoundExpression BindParameterDefaultValue(
            Symbol containingSymbol,
            EqualsValueClauseSyntax defaultValueSyntax,
            TypeSymbol parameterType,
            DiagnosticBag diagnostics,
            out BoundExpression valueBeforeConversion)
        {
            // UNDONE: The binding and conversion has to be executed in a checked context.

            var scopeBinder = new ScopedExpressionBinder(this.WithContainingMemberOrLambda(containingSymbol),
                                                         defaultValueSyntax.Value);
            valueBeforeConversion = scopeBinder.WithAdditionalFlags(BinderFlags.ParameterDefaultValue).BindValue(defaultValueSyntax.Value, diagnostics, BindValueKind.RValue);

            // Always generate the conversion, even if the expression is not convertible to the given type.
            // We want the erroneous conversion in the tree.
            BoundExpression result = GenerateConversionForAssignment(parameterType, valueBeforeConversion, diagnostics, isDefaultParameter: true);

            if (!scopeBinder.Locals.IsDefaultOrEmpty)
            {
                result = scopeBinder.AddLocalScopeToExpression(result);
            }

            return result;
        }

        internal BoundExpression BindEnumConstantInitializer(
            SourceEnumConstantSymbol symbol,
            ExpressionSyntax valueSyntax,
            DiagnosticBag diagnostics)
        {
            var initializer = BindValue(valueSyntax, diagnostics, BindValueKind.RValue);
            return GenerateConversionForAssignment(symbol.ContainingType.EnumUnderlyingType, initializer, diagnostics);
        }

        public BoundExpression BindExpression(ExpressionSyntax node, DiagnosticBag diagnostics)
        {
            return BindExpression(node, diagnostics: diagnostics, invoked: false, indexed: false);
        }

        protected BoundExpression BindExpression(ExpressionSyntax node, DiagnosticBag diagnostics, bool invoked, bool indexed)
        {
            BoundExpression expr = BindExpressionInternal(node, diagnostics, invoked, indexed);
            VerifyUnchecked(node, diagnostics, expr);

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
        protected BoundExpression BindExpressionAllowArgList(ExpressionSyntax node, DiagnosticBag diagnostics)
        {
            BoundExpression expr = BindExpressionInternal(node, diagnostics, invoked: false, indexed: false);
            VerifyUnchecked(node, diagnostics, expr);
            return expr;
        }

        private void VerifyUnchecked(ExpressionSyntax node, DiagnosticBag diagnostics, BoundExpression expr)
        {
            if (!expr.HasAnyErrors)
            {
                TypeSymbol exprType = expr.Type;
                if ((object)exprType != null && exprType.IsUnsafe())
                {
                    ReportUnsafeIfNotAllowed(node, diagnostics);
                    //CONSIDER: Return a bad expression so that HasErrors is true?
                }
            }
        }

        private BoundExpression BindExpressionInternal(ExpressionSyntax node, DiagnosticBag diagnostics, bool invoked, bool indexed)
        {
            if (IsEarlyAttributeBinder && !EarlyWellKnownAttributeBinder.CanBeValidAttributeArgument(node, this))
            {
                return BadExpression(node, LookupResultKind.NotAValue);
            }

            Debug.Assert(node != null);
            switch (node.Kind)
            {
                case SyntaxKind.AnonymousMethodExpression:
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                    return BindAnonymousFunction(node, diagnostics);
                case SyntaxKind.ThisExpression:
                    return BindThis((ThisExpressionSyntax)node, diagnostics);
                case SyntaxKind.BaseExpression:
                    return BindBase((BaseExpressionSyntax)node, diagnostics);
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
                case SyntaxKind.ObjectCreationExpression:
                    return BindObjectCreationExpression((ObjectCreationExpressionSyntax)node, diagnostics);
                case SyntaxKind.IdentifierName:
                case SyntaxKind.GenericName:
                    return BindIdentifier((SimpleNameSyntax)node, invoked, diagnostics);
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.PointerMemberAccessExpression:
                    return BindMemberAccess((MemberAccessExpressionSyntax)node, invoked, indexed, diagnostics: diagnostics);
                case SyntaxKind.SimpleAssignmentExpression:
                    return BindAssignment((BinaryExpressionSyntax)node, diagnostics);
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
                    return BindElementBindingExpression((ElementBindingExpressionSyntax)node, invoked, indexed, diagnostics);

                case SyntaxKind.IsExpression:
                    return BindIsOperator((BinaryExpressionSyntax)node, diagnostics);

                case SyntaxKind.AsExpression:
                    return BindAsOperator((BinaryExpressionSyntax)node, diagnostics);

                case SyntaxKind.UnaryPlusExpression:
                case SyntaxKind.UnaryMinusExpression:
                case SyntaxKind.LogicalNotExpression:
                case SyntaxKind.BitwiseNotExpression:
                    return BindUnaryOperator((PrefixUnaryExpressionSyntax)node, diagnostics);

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

                case SyntaxKind.NumericLiteralExpression:
                case SyntaxKind.StringLiteralExpression:
                case SyntaxKind.CharacterLiteralExpression:
                case SyntaxKind.TrueLiteralExpression:
                case SyntaxKind.FalseLiteralExpression:
                case SyntaxKind.NullLiteralExpression:
                    return BindLiteralConstant((LiteralExpressionSyntax)node, diagnostics);

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
                case SyntaxKind.SubtractAssignmentExpression:
                    return BindCompoundAssignment((BinaryExpressionSyntax)node, diagnostics);

                case SyntaxKind.AliasQualifiedName:
                case SyntaxKind.PredefinedType:
                    return this.BindNamespaceOrType(node, diagnostics);

                case SyntaxKind.QueryExpression:
                    return this.BindQuery((QueryExpressionSyntax)node, diagnostics);

                case SyntaxKind.AnonymousObjectCreationExpression:
                    return BindAnonymousObjectCreation((AnonymousObjectCreationExpressionSyntax)node, diagnostics);

                case SyntaxKind.QualifiedName:
                    // Not reachable during method body binding, but
                    // may be used by SemanticModel for error cases.
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
                    return BindAwait((PrefixUnaryExpressionSyntax)node, diagnostics);

                case SyntaxKind.OmittedArraySizeExpression:
                case SyntaxKind.OmittedTypeArgument:
                case SyntaxKind.ObjectInitializerExpression:
                    // Not reachable during method body binding, but
                    // may be used by SemanticModel for error cases.
                    return BadExpression(node);

                case SyntaxKind.NullableType:
                    // Not reachable during method body binding, but
                    // may be used by SemanticModel for error cases.
                    // NOTE: This happens when there's a problem with the Nullable<T> type (e.g. it's missing).
                    // There is no corresponding problem for array or pointer types (which seem analogous), since
                    // they are not constructed types; the element type can be an error type, but the array/pointer 
                    // type cannot.
                    return BadExpression(node);

                case SyntaxKind.DeclarationExpression:
                    return BindDeclarationExpression((DeclarationExpressionSyntax)node, diagnostics);

                default:
                    // NOTE: We could probably throw an exception here, but it's conceivable
                    // that a non-parser syntax tree could reach this point with an unexpected
                    // SyntaxKind and we don't want to throw if that occurs.
                    Debug.Assert(false, "Unexpected SyntaxKind " + node.Kind);
                    return BadExpression(node);
            }
        }

        private BoundExpression BindDeclarationExpression(DeclarationExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var typeSyntax = node.Type;

            bool isConst = false;
            bool isVar;
            AliasSymbol alias;
            TypeSymbol declType = BindVariableType(node, diagnostics, typeSyntax, ref isConst, /*isFixed*/ false, out isVar, out alias);

            if ((ContainingMemberOrLambda.Kind != SymbolKind.Method && !this.InFieldInitializer) || this.InParameterDefaultValue)
            {
                Error(diagnostics, ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, node);
            }

            if (isVar && node.Variable.Initializer == null)
            {
                SourceLocalSymbol localSymbol = LocateDeclaredVariableSymbol(node.Variable, typeSyntax);

                return new UninitializedVarDeclarationExpression(node,
                                                                 LocateDeclaredVariableSymbol(node.Variable, typeSyntax),
                                                                 BindDeclaratorArguments(node.Variable, diagnostics),
                                                                 // Check for variable declaration errors.
                                                                 hasErrors: this.ValidateDeclarationNameConflictsInScope(localSymbol, diagnostics));
            }

            BoundLocalDeclaration localDeclaration = BindVariableDeclaration(LocalDeclarationKind.Variable, isVar, node.Variable, typeSyntax, declType, alias, diagnostics, node);

            return new BoundDeclarationExpression(node, 
                                                  localDeclaration.LocalSymbol, 
                                                  localDeclaration.DeclaredType, 
                                                  localDeclaration.InitializerOpt, 
                                                  localDeclaration.ArgumentsOpt, 
                                                  localDeclaration.LocalSymbol.Type, 
                                                  localDeclaration.HasErrors);
        }

        private BoundExpression BindRefValue(RefValueExpressionSyntax node, DiagnosticBag diagnostics)
        {
            // __refvalue(tr, T) requires that tr be a TypedReference and T be a type.
            // The result is a *variable* of type T.

            BoundExpression argument = BindValue(node.Expression, diagnostics, BindValueKind.RValue);
            bool hasErrors = argument.HasAnyErrors;

            TypeSymbol typedReferenceType = this.Compilation.GetSpecialType(SpecialType.System_TypedReference);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            Conversion conversion = this.Conversions.ClassifyConversionFromExpression(argument, typedReferenceType, ref useSiteDiagnostics);
            diagnostics.Add(node, useSiteDiagnostics);
            if (!conversion.IsImplicit || !conversion.IsValid)
            {
                hasErrors = true;
                GenerateImplicitConversionError(diagnostics, node, conversion, argument, typedReferenceType);
            }

            argument = CreateConversion(argument, conversion, typedReferenceType, diagnostics);

            TypeSymbol type = BindType(node.Type, diagnostics);

            return new BoundRefValueOperator(node, argument, type, hasErrors);
        }

        private BoundExpression BindMakeRef(MakeRefExpressionSyntax node, DiagnosticBag diagnostics)
        {
            // __makeref(x) requires that x be a variable, and not be of a restricted type.
            BoundExpression argument = this.BindValue(node.Expression, diagnostics, BindValueKind.OutParameter);

            if (argument.Kind == BoundKind.UninitializedVarDeclarationExpression)
            {
                argument = ((UninitializedVarDeclarationExpression)argument).FailInference(this, diagnostics);
            }

            bool hasErrors = argument.HasAnyErrors;

            TypeSymbol typedReferenceType = this.Compilation.GetSpecialType(SpecialType.System_TypedReference);

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

        private BoundExpression BindRefType(RefTypeExpressionSyntax node, DiagnosticBag diagnostics)
        {
            // __reftype(x) requires that x be implicitly convertible to TypedReference.

            BoundExpression argument = BindValue(node.Expression, diagnostics, BindValueKind.RValue);
            bool hasErrors = argument.HasAnyErrors;

            TypeSymbol typedReferenceType = this.Compilation.GetSpecialType(SpecialType.System_TypedReference);
            TypeSymbol typeType = this.Compilation.GetWellKnownType(WellKnownType.System_Type);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            Conversion conversion = this.Conversions.ClassifyConversionFromExpression(argument, typedReferenceType, ref useSiteDiagnostics);
            diagnostics.Add(node, useSiteDiagnostics);
            if (!conversion.IsImplicit || !conversion.IsValid)
            {
                hasErrors = true;
                GenerateImplicitConversionError(diagnostics, node, conversion, argument, typedReferenceType);
            }

            argument = CreateConversion(argument, conversion, typedReferenceType, diagnostics);
            return new BoundRefTypeOperator(node, argument, null, typeType, hasErrors);
        }

        private BoundExpression BindArgList(CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            // There are two forms of __arglist expression. In a method with an __arglist parameter,
            // it is legal to use __arglist as an expression of type RuntimeArgumentHandle. In 
            // a call to such a method, it is legal to use __arglist(x, y, z) as the final argument.\
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
        /// This function is only needed for SemanticModel to perform binding for erroneous cases.
        /// </summary>
        private BoundExpression BindQualifiedName(QualifiedNameSyntax node, DiagnosticBag diagnostics)
        {
            return BindMemberAccessWithBoundLeft(node, this.BindExpression(node.Left, diagnostics), node.Right, node.DotToken, invoked: false, indexed: false, diagnostics: diagnostics);
        }

        private BoundExpression BindParenthesizedExpression(ExpressionSyntax innerExpression, DiagnosticBag diagnostics)
        {
            var result = BindExpression(innerExpression, diagnostics);

            // A parenthesized expression may not be a namespace or a type. If it is a parenthesized
            // namespace or type then report the error but let it go; we'll just ignore the
            // parenthesis and keep on trucking.
            CheckNotNamespaceOrType(result, diagnostics);
            return result;
        }

        private BoundExpression BindTypeOf(TypeOfExpressionSyntax node, DiagnosticBag diagnostics)
        {
            ExpressionSyntax typeSyntax = node.Type;

            TypeofBinder typeofBinder = new TypeofBinder(typeSyntax, this); //has special handling for unbound types
            AliasSymbol alias;
            TypeSymbol type = typeofBinder.BindType(typeSyntax, diagnostics, out alias);

            bool hasError = false;

            // NB: Dev10 has an error for typeof(dynamic), but allows typeof(dynamic[]),
            // typeof(C<dynamic>), etc.
            if (type.IsDynamic())
            {
                diagnostics.Add(ErrorCode.ERR_BadDynamicTypeof, node.Location);
                hasError = true;
            }

            BoundTypeExpression boundType = new BoundTypeExpression(typeSyntax, alias, type, type.IsErrorType());
            return new BoundTypeOfOperator(node, boundType, null, this.GetWellKnownType(WellKnownType.System_Type, diagnostics, node), hasError);
        }

        private BoundExpression BindSizeOf(SizeOfExpressionSyntax node, DiagnosticBag diagnostics)
        {
            ExpressionSyntax typeSyntax = node.Type;
            AliasSymbol alias;
            TypeSymbol type = this.BindType(typeSyntax, diagnostics, out alias);

            bool typeHasErrors = type.IsErrorType();

            if (!typeHasErrors && type.IsManagedType)
            {
                diagnostics.Add(ErrorCode.ERR_ManagedAddr, node.Location, type);
                typeHasErrors = true;
            }

            BoundTypeExpression boundType = new BoundTypeExpression(typeSyntax, alias, type, typeHasErrors);
            ConstantValue constantValue = GetConstantSizeOf(type);
            bool hasErrors = ReferenceEquals(constantValue, null) && ReportUnsafeIfNotAllowed(node, type, diagnostics);
            return new BoundSizeOfOperator(node, boundType, constantValue,
                this.GetSpecialType(SpecialType.System_Int32, diagnostics, node), hasErrors);
        }

        internal static ConstantValue GetConstantSizeOf(TypeSymbol type)
        {
            return ConstantValue.CreateSizeOf((type.GetEnumUnderlyingType() ?? type).SpecialType);
        }

        private BoundExpression BindDefaultExpression(DefaultExpressionSyntax node, DiagnosticBag diagnostics)
        {
            TypeSymbol type = this.BindType(node.Type, diagnostics);
            return new BoundDefaultOperator(node, type);
        }

        /// <summary>
        /// Binds a simple identifier.
        /// </summary>
        private BoundExpression BindIdentifier(
            SimpleNameSyntax node,
            bool invoked,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);

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

            int arity = node.Arity;
            bool hasTypeArguments = arity > 0;

            SeparatedSyntaxList<TypeSyntax> typeArgumentList = node.Kind == SyntaxKind.GenericName
                ? ((GenericNameSyntax)node).TypeArgumentList.Arguments
                : default(SeparatedSyntaxList<TypeSyntax>);

            Debug.Assert(arity == typeArgumentList.Count);

            var typeArguments = hasTypeArguments ?
                BindTypeArguments(typeArgumentList, diagnostics) :
                default(ImmutableArray<TypeSymbol>);

            var lookupResult = LookupResult.GetInstance();
            LookupOptions options = LookupOptions.AllMethodsOnArityZero;
            if (invoked)
            {
                options |= LookupOptions.MustBeInvocableIfMember;
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            this.LookupSymbolsWithFallback(lookupResult, node.Identifier.ValueText, arity: arity, useSiteDiagnostics: ref useSiteDiagnostics, options: options);
            diagnostics.Add(node, useSiteDiagnostics);

            if (lookupResult.Kind != LookupResultKind.Empty)
            {
                // have we detected an error with the current node?
                bool isError = false;
                bool wasError;
                var members = ArrayBuilder<Symbol>.GetInstance();
                Symbol symbol = GetSymbolOrMethodOrPropertyGroup(lookupResult, node, node.Identifier.ValueText, node.Arity, members, diagnostics, out wasError);  // reports diagnostics in result.

                isError |= wasError;

                if ((object)symbol == null)
                {
                    Debug.Assert(members.Count > 0);

                    var receiver = SynthesizeMethodGroupReceiver(node, members);
                    expression = ConstructBoundMemberGroupAndReportOmittedTypeArguments(
                        node,
                        typeArgumentList,
                        typeArguments,
                        receiver,
                        node.Identifier.ValueText,
                        members,
                        lookupResult,
                        receiver != null ? BoundMethodGroupFlags.HasImplicitReceiver : BoundMethodGroupFlags.None,
                        isError,
                        diagnostics);
                }
                else
                {
                    bool isNamedType = (symbol.Kind == SymbolKind.NamedType) || (symbol.Kind == SymbolKind.ErrorType);

                    if (hasTypeArguments && isNamedType)
                    {
                        symbol = ConstructNamedTypeUnlessTypeArgumentOmitted(node, (NamedTypeSymbol)symbol, typeArgumentList, typeArguments, diagnostics);
                    }

                    expression = BindNonMethod(node, symbol, diagnostics, lookupResult.Kind, isError);

                    if (!isNamedType && (hasTypeArguments || node.Kind == SyntaxKind.GenericName))
                    {
                        Debug.Assert(isError); // Should have been reported by GetSymbolOrMethodOrPropertyGroup.
                        expression = new BoundBadExpression(
                            syntax: node,
                            resultKind: LookupResultKind.WrongArity,
                            symbols: ImmutableArray.Create<Symbol>(symbol),
                            childBoundNodes: ImmutableArray.Create<BoundNode>(expression),
                            type: expression.Type,
                            hasErrors: isError);
                    }
                }

                members.Free();
            }
            else
            {
                // Otherwise, the simple-name is undefined and a compile-time error occurs.
                expression = BadExpression(node);
                if (lookupResult.Error != null)
                {
                    Error(diagnostics, lookupResult.Error, node);
                }
                else if (IsJoinRangeVariableInLeftKey(node))
                {
                    Error(diagnostics, ErrorCode.ERR_QueryOuterKey, node, node.Identifier.ValueText);
                }
                else if (IsInJoinRightKey(node))
                {
                    Error(diagnostics, ErrorCode.ERR_QueryInnerKey, node, node.Identifier.ValueText);
                }
                else
                {
                    Error(diagnostics, ErrorCode.ERR_NameNotInContext, node, node.Identifier.ValueText);
                }
            }

            lookupResult.Free();
            return expression;
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
            var declaringType = members[0].ContainingType;
            var instanceType = (NamedTypeSymbol)declaringType.OriginalDefinition;

            if (SynthesizeMethodGroupReceiverIsInstanceTypeOrAnyBaseInstanceType(instanceType, currentType))
            {
                return ThisReference(syntax, wasCompilerGenerated: true);
            }
            else
            {
                return TryBindInteractiveReceiver(syntax, this.ContainingMemberOrLambda, currentType, declaringType);
            }
        }

        // is the instance type of symbol1 identical to the instance type of symbol2, or 
        // the instance type of any base type of symbol2?
        private static bool SynthesizeMethodGroupReceiverIsInstanceTypeOrAnyBaseInstanceType(NamedTypeSymbol symbol1, NamedTypeSymbol symbol2)
        {
            for (var current = symbol2; (object)current != null; current = current.BaseTypeNoUseSiteDiagnostics)
            {
                if (symbol1 == current.OriginalDefinition)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsBindingImplicitlyTypedLocal(LocalSymbol symbol)
        {
            foreach (LocalSymbol s in this.ImplicitlyTypedLocalsBeingBound)
            {
                if (s == symbol)
                {
                    return true;
                }
            }
            return false;
        }

        private BoundExpression BindNonMethod(SimpleNameSyntax node, Symbol symbol, DiagnosticBag diagnostics, LookupResultKind resultKind, bool isError)
        {
            // Events are handled later as we don't know yet if we are binding to the event or it's backing field.
            if (symbol.Kind != SymbolKind.Event)
            {
                ReportDiagnosticsIfObsolete(diagnostics, symbol, node, hasBaseReceiver: false);
            }

            switch (symbol.Kind)
            {
                case SymbolKind.Local:
                    {
                        var localSymbol = (LocalSymbol)symbol;
                        var constantValueOpt = localSymbol.IsConst ? localSymbol.GetConstantValue(this.LocalInProgress) : null;
                        TypeSymbol type;

                        bool usedBeforeDecl =
                            node.SyntaxTree == localSymbol.Locations[0].SourceTree &&
                            node.SpanStart < localSymbol.Locations[0].SourceSpan.Start;
                        bool isBindingVar = IsBindingImplicitlyTypedLocal(localSymbol);

                        if (usedBeforeDecl || isBindingVar)
                        {
                            // Here we check for errors 
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
                            // The second case is when an implicitly typed local is used
                            // in its initializer, like "var x = N(out x);". Since overload
                            // resolution cannot proceed until the type of x is known, this is 
                            // an error.
                            //
                            // CONSIDER:
                            // In the case of something like "var x = M(out x);" we give the error 
                            // that x cannot be used before it is *declared*. But that seems like 
                            // the wrong error; we didn't say "x = M(out x); int x;" -- the usage
                            // happened after the declaration. Should we give a better error?
                            //
                            // In the native compiler we give the "hides field" error even
                            // in this case:
                            //
                            // class C { 
                            //  int x; 
                            //  void M() { 
                            //    var x = N(out x);
                            //  } }
                            //
                            // but that seems like the wrong error to give in this scenario. The "out x" is not
                            // "hiding the field" the same way the "Print(x);" is in the previous example. In
                            // Roslyn we do not report that error in this scenario; we only report that
                            // the offending usage is possibly hiding a field if it really is lexically
                            // before the declaration.

                            FieldSymbol possibleField = null;
                            if (usedBeforeDecl)
                            {
                                var lookupResult = LookupResult.GetInstance();
                                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                                this.LookupMembersInType(
                                    lookupResult,
                                    ContainingType,
                                    localSymbol.Name,
                                    arity: 0,
                                    basesBeingResolved: null,
                                    options: LookupOptions.Default,
                                    originalBinder: this,
                                    diagnose: false,
                                    useSiteDiagnostics: ref useSiteDiagnostics);

                                diagnostics.Add(node, useSiteDiagnostics);

                                possibleField = lookupResult.SingleSymbolOrDefault as FieldSymbol;
                                lookupResult.Free();
                            }

                            if ((object)possibleField != null)
                            {
                                Error(diagnostics, ErrorCode.ERR_VariableUsedBeforeDeclarationAndHidesField, node, node, possibleField);
                            }
                            else
                            {
                                Error(diagnostics, ErrorCode.ERR_VariableUsedBeforeDeclaration, node, node);
                            }

                            // Moreover: we wish to avoid any deadlocks, unbounded recursion or
                            // race conditions that might result from a situation like:
                            //
                            // var x = M(y);
                            // var y = x;
                            //
                            // We therefore say that if an illegal forward reference is made
                            // to a local variable then we do not attempt to determine its
                            // type for the purposes of error recovery; we only look
                            // backwards. In the case above, the analysis of "= M(y)" will not
                            // attempt to determine the type of y, but the analysis of "= x"
                            // will attempt to determine the type of x.
                            //
                            // CONSIDER:
                            // We could interrogate the local symbol and only skip checking its 
                            // type if it is "var".

                            type = new ExtendedErrorTypeSymbol(this.Compilation, name: "var", arity: 0, errorInfo: null, variableUsedBeforeDeclaration: true);
                        }
                        else
                        {
                            var sourceLocal = localSymbol as SourceLocalSymbol;

                            if ((object)sourceLocal != null)
                            {
                                CSharpSyntaxNode declarationParent = (CSharpSyntaxNode)sourceLocal.IdentifierToken.Parent;
                                VariableDeclaratorSyntax declarator = null;

                                if (declarationParent != null && 
                                    declarationParent.Kind == SyntaxKind.VariableDeclarator &&
                                    (declarator = (VariableDeclaratorSyntax)declarationParent) != null &&
                                    (declarationParent = declarator.Parent) != null &&
                                    declarationParent.Kind == SyntaxKind.DeclarationExpression &&
                                    ((DeclarationExpressionSyntax)declarationParent).Variable == declarator &&
                                    declarator.Identifier == sourceLocal.IdentifierToken &&
                                    declarator.Initializer == null &&
                                    sourceLocal.IsVarPendingTypeInference)
                                {
                                    // Declaration expression, which is a 'var' without an initializer, is an argument and we are referring to
                                    // the declared local in a different argument in the same argument list.
                                    // TODO: This assumption is correct only if there is a guarantee that 
                                    // all statements are bound in order, which is not correct for SemanticModel, I believe.
                                    Error(diagnostics, ErrorCode.ERR_VariableUsedInTheSameArgumentList, node, node);
                                    type = new ExtendedErrorTypeSymbol(this.Compilation, name: "var", arity: 0, errorInfo: null, variableUsedBeforeDeclaration: false);
                                }
                                else
                                {
                            type = localSymbol.Type;
                        }
                            }
                            else
                            {
                                type = localSymbol.Type;
                            }
                        }

                        return new BoundLocal(node, localSymbol, constantValueOpt, type, hasErrors: isError);
                    }

                case SymbolKind.Parameter:
                    {
                        var parameter = (ParameterSymbol)symbol;

                        if ((object)parameter.ContainingSymbol != (object)this.ContainingMemberOrLambda && this.ContainingMemberOrLambda.Kind == SymbolKind.Method)
                        {
                            // Captured in a lambda.
                            if (parameter.RefKind != RefKind.None)
                            {
                                Error(diagnostics, ErrorCode.ERR_AnonDelegateCantUse, node, parameter.Name);
                            }
                        }

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
                        return BindFieldAccess(node, receiver, (FieldSymbol)symbol, diagnostics, resultKind, hasErrors: isError);
                    }

                case SymbolKind.Namespace:
                    return new BoundNamespaceExpression(node, (NamespaceSymbol)symbol, hasErrors: isError);

                case SymbolKind.Alias:
                    {
                        var alias = symbol as AliasSymbol;
                        symbol = alias.Target;
                        switch (symbol.Kind)
                        {
                            case SymbolKind.NamedType:
                            case SymbolKind.ErrorType:
                                return new BoundTypeExpression(node, alias, false, (NamedTypeSymbol)symbol, hasErrors: isError);
                            case SymbolKind.Namespace:
                                return new BoundNamespaceExpression(node, (NamespaceSymbol)symbol, alias, hasErrors: isError);
                            default:
                                throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
                        }
                    }

                case SymbolKind.RangeVariable:
                    return BindRangeVariable(node, symbol as RangeVariableSymbol, diagnostics);

                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
        }

        protected virtual BoundExpression BindRangeVariable(SimpleNameSyntax node, RangeVariableSymbol qv, DiagnosticBag diagnostics)
        {
            return Next.BindRangeVariable(node, qv, diagnostics);
        }

        private BoundExpression SynthesizeReceiver(SimpleNameSyntax node, Symbol member, DiagnosticBag diagnostics)
        {
            // SPEC: Otherwise, if T is the instance type of the immediately enclosing class or
            // struct type, if the lookup identifies an instance member, and if the reference occurs
            // within the block of an instance constructor, an instance method, or an instance
            // accessor, the result is the same as a member access of the form this.I. This can only
            // happen when K is zero.

            if (member.IsStatic)
            {
                return null;
            }

            var currentType = this.ContainingType;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            if (currentType.IsEqualToOrDerivedFrom(member.ContainingType, ignoreDynamic: false, useSiteDiagnostics: ref useSiteDiagnostics))
            {
                bool hasErrors = false;
                if (InFieldInitializer && !currentType.IsScriptClass)
                {
                    //can't access "this" in field initializers
                    Error(diagnostics, ErrorCode.ERR_FieldInitRefNonstatic, node, member);
                    hasErrors = true;
                }
                else if (InConstructorInitializer || InAttributeArgument)
                {
                    //can't access "this" in constructor initializers or attribute arguments
                    Error(diagnostics, ErrorCode.ERR_ObjectRequired, node, member);
                    hasErrors = true;
                }

                // not an instance member if the container is a type, like when binding default parameter values.
                var containingMember = ContainingMember();
                bool locationIsInstanceMember = !containingMember.IsStatic &&
                    (containingMember.Kind != SymbolKind.NamedType || currentType.IsScriptClass);

                if (!hasErrors && !locationIsInstanceMember)
                {
                    // error CS0120: An object reference is required for the non-static field, method, or property '{0}'
                    Error(diagnostics, ErrorCode.ERR_ObjectRequired, node, member);
                    hasErrors = true;
                }

                hasErrors = hasErrors || IsRefOrOutThisParameterCaptured(node, diagnostics);
                return ThisReference(node, hasErrors, wasCompilerGenerated: true);
            }
            else
            {
                return TryBindInteractiveReceiver(node, this.ContainingMemberOrLambda, currentType, member.ContainingType);
            }
        }

        internal Symbol ContainingMember()
        {
            // We skip intervening lambdas to find the actual member.
            var containingMember = this.ContainingMemberOrLambda;
            while (containingMember.Kind != SymbolKind.NamedType && (object)containingMember.ContainingSymbol != null && containingMember.ContainingSymbol.Kind != SymbolKind.NamedType)
            {
                containingMember = containingMember.ContainingSymbol;
            }
            return containingMember;
        }

        private BoundExpression TryBindInteractiveReceiver(CSharpSyntaxNode syntax, Symbol currentMember, NamedTypeSymbol currentType, NamedTypeSymbol memberDeclaringType)
        {
            if (currentType.TypeKind == TypeKind.Submission && !currentMember.IsStatic)
            {
                if (memberDeclaringType.TypeKind == TypeKind.Submission)
                {
                    return new BoundPreviousSubmissionReference(syntax, currentType, memberDeclaringType) { WasCompilerGenerated = true };
                }
                else
                {
                    TypeSymbol hostObjectType = Compilation.GetHostObjectTypeSymbol();
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    if ((object)hostObjectType != null && hostObjectType.IsEqualToOrDerivedFrom(memberDeclaringType, ignoreDynamic: false, useSiteDiagnostics: ref useSiteDiagnostics))
                    {
                        return new BoundHostObjectMemberReference(syntax, hostObjectType) { WasCompilerGenerated = true };
                    }
                }
            }

            return null;
        }

        public BoundExpression BindNamespaceOrTypeOrExpression(ExpressionSyntax node, DiagnosticBag diagnostics)
        {
            if (node.Kind == SyntaxKind.PredefinedType)
            {
                return this.BindNamespaceOrType(node, diagnostics);
            }

            if (SyntaxFacts.IsName(node.Kind))
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
            else if (SyntaxFacts.IsTypeSyntax(node.Kind))
            {
                return this.BindNamespaceOrType(node, diagnostics);
            }

            return this.BindExpression(node, diagnostics, SyntaxFacts.IsInvoked(node), SyntaxFacts.IsIndexed(node));
        }

        public BoundExpression BindLabel(ExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var name = node as IdentifierNameSyntax;
            if (name == null)
            {
                Debug.Assert(node.ContainsDiagnostics);
                return BadExpression(node, LookupResultKind.NotLabel);
            }

            var result = LookupResult.GetInstance();
            string labelName = name.Identifier.ValueText;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            this.LookupSymbolsWithFallback(result, labelName, arity: 0, useSiteDiagnostics: ref useSiteDiagnostics, options: LookupOptions.LabelsOnly);
            diagnostics.Add(node, useSiteDiagnostics);

            if (!result.IsMultiViable)
            {
                Error(diagnostics, ErrorCode.ERR_LabelNotFound, node, labelName);
                result.Free();
                return BadExpression(node, result.Kind);
            }

            if (!result.IsSingleViable)
            {
                Debug.Assert(false, "If this happens, we need to deal with multiple label definitions.");
            }

            var symbol = (LabelSymbol)result.Symbols.First();
            result.Free();
            return new BoundLabel(node, symbol, null);
        }

        public BoundExpression BindNamespaceOrType(ExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var symbol = this.BindNamespaceOrTypeOrAliasSymbol(node, diagnostics, null, false);
            return CreateBoundNamespaceOrTypeExpression(node, symbol);
        }

        public BoundExpression BindNamespaceAlias(IdentifierNameSyntax node, DiagnosticBag diagnostics)
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
                return new BoundTypeExpression(node, alias, false, type);
            }

            var namespaceSymbol = symbol as NamespaceSymbol;
            if ((object)namespaceSymbol != null)
            {
                return new BoundNamespaceExpression(node, namespaceSymbol, alias);
            }

            throw ExceptionUtilities.UnexpectedValue(symbol);
        }

        private BoundThisReference BindThis(ThisExpressionSyntax node, DiagnosticBag diagnostics)
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
                hasErrors = IsRefOrOutThisParameterCaptured(node, diagnostics);
            }

            return ThisReference(node, hasErrors);
        }

        private BoundThisReference ThisReference(CSharpSyntaxNode node, bool hasErrors = false, bool wasCompilerGenerated = false)
        {
            return new BoundThisReference(node, this.ContainingType ?? CreateErrorType(), hasErrors) { WasCompilerGenerated = wasCompilerGenerated };
        }

        private bool IsRefOrOutThisParameterCaptured(CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            ParameterSymbol thisSymbol = this.ThisParameter;
            // If there is no this parameter, then it is definitely not captured and 
            // any diagnostic would be cascading.
            if ((object)thisSymbol != null && thisSymbol.ContainingSymbol != ContainingMemberOrLambda && thisSymbol.RefKind != RefKind.None)
            {
                Error(diagnostics, ErrorCode.ERR_ThisStructNotInAnonMeth, node);
                return true;
            }

            return false;
        }

        private BoundBaseReference BindBase(BaseExpressionSyntax node, DiagnosticBag diagnostics)
        {
            NamedTypeSymbol baseType = (object)this.ContainingType == null ? null : this.ContainingType.BaseTypeNoUseSiteDiagnostics;
            bool hasErrors = true;

            bool inStaticContext;
            if (!HasThis(isExplicit: true, inStaticContext: out inStaticContext))
            {
                //this error is returned in the field initializer case
                Error(diagnostics, inStaticContext ? ErrorCode.ERR_BaseInStaticMeth : ErrorCode.ERR_BaseInBadContext, node);
            }
            else if ((object)baseType == null) // e.g. in System.Object
            {
                Error(diagnostics, ErrorCode.ERR_NoBaseClass, node);
            }
            else if (node.Parent.Kind != SyntaxKind.SimpleMemberAccessExpression && node.Parent.Kind != SyntaxKind.ElementAccessExpression)
            {
                Error(diagnostics, ErrorCode.ERR_BaseIllegal, node);
            }
            else if (IsRefOrOutThisParameterCaptured(node, diagnostics))
            {
                // error has been reported by CheckThisReference
            }
            else
            {
                hasErrors = false;
            }

            return new BoundBaseReference(node, baseType, hasErrors);
        }

        private BoundExpression BindCast(CastExpressionSyntax node, DiagnosticBag diagnostics)
        {
            // TODO: I added this with enough implementation to get numeric conversions to work.
            // Needs to be fleshed out a lot, i think.

            BoundExpression operand = this.BindValue(node.Expression, diagnostics, BindValueKind.RValue);
            TypeSymbol targetType = this.BindType(node.Type, diagnostics);

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            Conversion conversion = this.Conversions.ClassifyConversionForCast(operand, targetType, ref useSiteDiagnostics);
            diagnostics.Add(node, useSiteDiagnostics);
            bool targetTypeIsStatic = targetType.IsStatic;
            if (operand.HasAnyErrors || targetType.IsErrorType() || !conversion.IsValid || targetTypeIsStatic)
            {
                // Make sure that errors within the unbound lambda don't get lost.
                if (operand.Kind == BoundKind.UnboundLambda)
                {
                    GenerateAnonymousFunctionConversionError(diagnostics, operand.Syntax, (UnboundLambda)operand, targetType);
                }
                else if (operand.HasAnyErrors || targetType.IsErrorType())
                {
                    // an error has already been reported elsewhere
                }
                else if (targetTypeIsStatic)
                {
                    // The specification states in the section titled "Referencing Static
                    // Class Types" that it is always illegal to have a static class in a
                    // cast operator.
                    diagnostics.Add(ErrorCode.ERR_ConvertToStaticClass, node.Location, targetType);
                }
                else if (!targetType.IsReferenceType && !targetType.IsNullableType() && operand.IsLiteralNull())
                {
                    diagnostics.Add(ErrorCode.ERR_ValueCantBeNull, node.Location, targetType);
                }
                else if (conversion.ResultKind == LookupResultKind.OverloadResolutionFailure)
                {
                    Debug.Assert(conversion.IsUserDefined);

                    ImmutableArray<MethodSymbol> originalUserDefinedConversions = conversion.OriginalUserDefinedConversions;
                    if (originalUserDefinedConversions.Length > 1)
                    {
                        diagnostics.Add(ErrorCode.ERR_AmbigUDConv, node.Location, originalUserDefinedConversions[0], originalUserDefinedConversions[1], operand.Type, targetType);
                    }
                    else
                    {
                        Debug.Assert(originalUserDefinedConversions.Length == 0,
                            "How can there be exactly one applicable user-defined conversion if the conversion doesn't exist?");
                        SymbolDistinguisher distinguisher = new SymbolDistinguisher(this.Compilation, operand.Type, targetType);
                        diagnostics.Add(ErrorCode.ERR_NoExplicitConv, node.Location, distinguisher.First, distinguisher.Second);
                    }
                }
                else
                {
                    // TODO: report more specific diagnostics here for failed method group conversions
                    if (operand.Kind == BoundKind.MethodGroup)
                    {
                        diagnostics.Add(ErrorCode.ERR_NoExplicitConv, node.Location, MessageID.IDS_SK_METHOD.Localize(), targetType);
                    }
                    else
                    {
                        Debug.Assert((object)operand.Type != null);
                        SymbolDistinguisher distinguisher = new SymbolDistinguisher(this.Compilation, operand.Type, targetType);
                        diagnostics.Add(ErrorCode.ERR_NoExplicitConv, node.Location, distinguisher.First, distinguisher.Second);
                    }
                }

                return new BoundConversion(
                    node,
                    operand,
                    conversion,
                    @checked: CheckOverflowAtRuntime,
                    explicitCastInCode: true,
                    constantValueOpt: ConstantValue.NotAvailable,
                    type: targetType,
                    hasErrors: true);
            }

            return CreateConversion(node, operand, conversion, isCast: true, destination: targetType, diagnostics: diagnostics);
        }

        private BoundExpression BindMethodGroup(ExpressionSyntax node, bool invoked, bool indexed, DiagnosticBag diagnostics)
        {
            switch (node.Kind)
            {
                case SyntaxKind.IdentifierName:
                case SyntaxKind.GenericName:
                    return BindIdentifier((SimpleNameSyntax)node, invoked, diagnostics);
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.PointerMemberAccessExpression:
                    return BindMemberAccess((MemberAccessExpressionSyntax)node, invoked, indexed, diagnostics);
                case SyntaxKind.ParenthesizedExpression:
                    return BindMethodGroup(((ParenthesizedExpressionSyntax)node).Expression, invoked: false, indexed: false, diagnostics: diagnostics);
                default:
                    return BindExpression(node, diagnostics, invoked, indexed);
            }
        }

        private static ImmutableArray<MethodSymbol> GetOriginalMethods(OverloadResolutionResult<MethodSymbol> overloadResolutionResult)
        {
            // If overload resolution has failed then we want to stash away the original methods that we 
            // considered so that the IDE can display tooltips or other information about them.
            // However, if a method group contained a generic method that was type inferred then
            // the IDE wants information about the *inferred* method, not the original unconstructed
            // generic method.

            if (overloadResolutionResult == null)
            {
                return ImmutableArray<MethodSymbol>.Empty;
            }

            var builder = ArrayBuilder<MethodSymbol>.GetInstance();
            foreach (var result in overloadResolutionResult.Results)
            {
                builder.Add(result.Member);
            }
            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Helper method to create a synthesized method invocation expression.
        /// </summary>
        /// <param name="node">Syntax Node.</param>
        /// <param name="receiver">Receiver for the method call.</param>
        /// <param name="methodName">Method to be invoked on the receiver.</param>
        /// <param name="args">Arguments to the method call.</param>
        /// <param name="diagnostics">Diagnostics.</param>
        /// <param name="typeArgsSyntax">Optional type arguments syntax.</param>
        /// <param name="typeArgs">Optional type arguments.</param>
        /// <param name="queryClause">The syntax for the query clause generating this invocation expression, if any.</param>
        /// <param name="allowFieldsAndProperties">True to allow invocation of fields and properties of delegate type. Only methods are allowed otherwise.</param>
        /// <returns>Synthesized method invocation expression.</returns>
        protected BoundExpression MakeInvocationExpression(
            CSharpSyntaxNode node,
            BoundExpression receiver,
            string methodName,
            ImmutableArray<BoundExpression> args,
            DiagnosticBag diagnostics,
            SeparatedSyntaxList<TypeSyntax> typeArgsSyntax = default(SeparatedSyntaxList<TypeSyntax>),
            ImmutableArray<TypeSymbol> typeArgs = default(ImmutableArray<TypeSymbol>),
            CSharpSyntaxNode queryClause = null,
            bool allowFieldsAndProperties = false)
        {
            Debug.Assert(receiver != null);

            var boundExpression = BindInstanceMemberAccess(node, node, receiver, methodName, typeArgs.NullToEmpty().Length, typeArgsSyntax, typeArgs, true, diagnostics);

            // The other consumers of this helper (await and collection initializers) require the target member to be a method.
            if (!allowFieldsAndProperties && (boundExpression.Kind == BoundKind.FieldAccess || boundExpression.Kind == BoundKind.PropertyAccess))
            {
                Symbol symbol;
                MessageID msgId;
                if (boundExpression.Kind == BoundKind.FieldAccess)
                {
                    msgId = MessageID.IDS_SK_FIELD;
                    symbol = ((BoundFieldAccess)boundExpression).FieldSymbol;
                }
                else
                {
                    msgId = MessageID.IDS_SK_PROPERTY;
                    symbol = ((BoundPropertyAccess)boundExpression).PropertySymbol;
                }

                diagnostics.Add(
                    ErrorCode.ERR_BadSKknown,
                    node.Location,
                    methodName,
                    msgId.Localize(),
                    MessageID.IDS_SK_METHOD.Localize());

                return BadExpression(node, LookupResultKind.Empty, ImmutableArray.Create(symbol), args.Add(receiver));
            }

            boundExpression = CheckValue(boundExpression, BindValueKind.RValueOrMethodGroup, diagnostics);
            boundExpression.WasCompilerGenerated = true;

            var analyzedArguments = AnalyzedArguments.GetInstance();
            Debug.Assert(!args.Any(e => e.Kind == BoundKind.UninitializedVarDeclarationExpression));
            analyzedArguments.Arguments.AddRange(args);
            BoundExpression result = BindInvocationExpression(node, node, methodName, boundExpression, analyzedArguments, diagnostics, queryClause);

            // Query operator can't be called dynamically. 
            if (queryClause != null && result.Kind == BoundKind.DynamicInvocation)
            {
                // the error has already been reported by BindInvocationExpression
                Debug.Assert(diagnostics.HasAnyErrors());

                result = CreateBadCall(node, boundExpression, LookupResultKind.Viable, analyzedArguments);
            }

            result.WasCompilerGenerated = true;
            analyzedArguments.Free();
            return result;
        }

        /// <summary>
        /// Bind an expression as a method invocation.
        /// </summary>
        private BoundExpression BindInvocationExpression(
            InvocationExpressionSyntax node,
            DiagnosticBag diagnostics)
        {
            BoundExpression result;
            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();

            // M(__arglist()) is legal, but M(__arglist(__arglist()) is not!
            bool isArglist = node.Expression.Kind == SyntaxKind.ArgListExpression;
            BindArgumentsAndNames(node.ArgumentList, diagnostics, analyzedArguments, allowArglist: !isArglist);

            if (isArglist)
            {
                result = BindArgListOperator(node, diagnostics, analyzedArguments);
            }
            else
            {
                BoundExpression boundExpression = BindMethodGroup(node.Expression, invoked: true, indexed: false, diagnostics: diagnostics);
                boundExpression = CheckValue(boundExpression, BindValueKind.RValueOrMethodGroup, diagnostics);
                string name = boundExpression.Kind == BoundKind.MethodGroup ? GetName(node.Expression) : null;
                result = BindInvocationExpression(node, node.Expression, name, boundExpression, analyzedArguments, diagnostics);
            }
            analyzedArguments.Free();
            return result;
        }

        private BoundExpression BindArgListOperator(InvocationExpressionSyntax node, DiagnosticBag diagnostics, AnalyzedArguments analyzedArguments)
        {
            // We allow names, oddly enough; M(__arglist(x : 123)) is legal. We just ignore them.
            TypeSymbol objType = GetSpecialType(SpecialType.System_Object, diagnostics, node);
            for (int i = 0; i < analyzedArguments.Arguments.Count; ++i)
            {
                BoundExpression argument = analyzedArguments.Arguments[i];
                if ((object)argument.Type == null && !argument.HasAnyErrors)
                {
                    // We are going to need every argument in here to have a type. If we don't have one,
                    // try converting it to object. We'll either succeed (if it is a null literal)
                    // or fail with a good error message.
                    //
                    // Note that the native compiler converts null literals to object, and for everything
                    // else it either crashes, or produces nonsense code. Roslyn improves upon this considerably.

                    if (argument.Kind == BoundKind.UninitializedVarDeclarationExpression)
                    {
                        analyzedArguments.Arguments[i] = ((UninitializedVarDeclarationExpression)argument).FailInference(this, diagnostics);
                    }
                    else
                    {
                    analyzedArguments.Arguments[i] = GenerateConversionForAssignment(objType, argument, diagnostics);
                }
            }
            }

            ImmutableArray<BoundExpression> arguments = analyzedArguments.Arguments.ToImmutable();
            ImmutableArray<RefKind> refKinds = analyzedArguments.RefKinds.ToImmutableOrNull();
            return new BoundArgListOperator(node, arguments, refKinds, null, analyzedArguments.HasErrors);
        }

        /// <summary>
        /// Bind an expression as a method invocation.
        /// </summary>
        private BoundExpression BindInvocationExpression(
            CSharpSyntaxNode node,
            CSharpSyntaxNode expression,
            string methodName,
            BoundExpression boundExpression,
            AnalyzedArguments analyzedArguments,
            DiagnosticBag diagnostics,
            CSharpSyntaxNode queryClause = null)
        {
            BoundExpression result;
            NamedTypeSymbol delegateType;

            if ((object)boundExpression.Type != null && boundExpression.Type.IsDynamic())
            {
                // Either we have a dynamic method group invocation "dyn.M(...)" or 
                // a dynamic delegate invocation "dyn(...)" -- either way, bind it as a dynamic
                // invocation and let the lowering pass sort it out.
                result = BindDynamicInvocation(node, boundExpression, analyzedArguments, ImmutableArray<MethodSymbol>.Empty, diagnostics, queryClause);
            }
            else if (boundExpression.Kind == BoundKind.MethodGroup)
            {
                result = BindMethodGroupInvocation(node, expression, methodName, (BoundMethodGroup)boundExpression, analyzedArguments, diagnostics, queryClause);
            }
            else if ((object)(delegateType = GetDelegateType(boundExpression)) != null)
            {
                if (ReportDelegateInvokeUseSiteDiagnostic(diagnostics, delegateType, node: node))
                {
                    return CreateBadCall(node, boundExpression, LookupResultKind.Viable, analyzedArguments);
                }

                result = BindDelegateInvocation(node, expression, methodName, boundExpression, analyzedArguments, diagnostics, queryClause, delegateType);
            }
            else
            {
                if (!boundExpression.HasAnyErrors)
                {
                    diagnostics.Add(new CSDiagnosticInfo(ErrorCode.ERR_MethodNameExpected), expression.Location);
                }
                result = CreateBadCall(node, boundExpression, LookupResultKind.NotInvocable, analyzedArguments);
            }

            CheckRestrictedTypeReceiver(result, this.Compilation, diagnostics);

            return result;
        }

        private BoundExpression BindDynamicInvocation(
            CSharpSyntaxNode node,
            BoundExpression expression,
            AnalyzedArguments arguments,
            ImmutableArray<MethodSymbol> applicableMethods,
            DiagnosticBag diagnostics,
            CSharpSyntaxNode queryClause)
        {
            bool hasErrors = false;
            if (expression.Kind == BoundKind.MethodGroup)
            {
                BoundMethodGroup methodGroup = (BoundMethodGroup)expression;
                BoundExpression receiver = methodGroup.ReceiverOpt;

                // receiver is null if we are calling a static method declared on an outer class via its simple name:
                if (receiver != null)
                {
                    switch (receiver.Kind)
                    {
                        case BoundKind.BaseReference:
                            Error(diagnostics, ErrorCode.ERR_NoDynamicPhantomOnBase, node, methodGroup.Name);
                            hasErrors = true;
                            break;

                        case BoundKind.ThisReference:
                            if (InConstructorInitializer && receiver.WasCompilerGenerated)
                            {
                                // Only a static method can be called in a constructor initializer. If we were not in a ctor initializer
                                // the runtime binder would ignore the receiver, but in a ctor initializer we can't read "this" before 
                                // the base constructor is called. We need to handle thisas a type qualified static method call.
                                expression = methodGroup.Update(
                                    methodGroup.TypeArgumentsOpt,
                                    methodGroup.Name,
                                    methodGroup.Methods,
                                    methodGroup.LookupSymbolOpt,
                                    methodGroup.LookupError,
                                    methodGroup.Flags & ~BoundMethodGroupFlags.HasImplicitReceiver,
                                    receiverOpt: new BoundTypeExpression(node, null, this.ContainingType),
                                    resultKind: methodGroup.ResultKind);
                            }

                            break;

                        case BoundKind.TypeOrValueExpression:
                            var typeOrValue = (BoundTypeOrValueExpression)receiver;

                            // Unfortunately, the runtime binder doesn't have APIs that would allow us to pass both "type or value".
                            // Ideally the runtime binder would choose between type and value based on the result of the overload resolution.
                            // We need to pick one or the other here. Dev11 compiler passes the type only if the value can't be accessed.
                            bool inStaticContext;
                            bool useType = IsInstance(typeOrValue.Variable) && !HasThis(isExplicit: false, inStaticContext: out inStaticContext);

                            BoundExpression finalReceiver = ReplaceTypeOrValueReceiver(typeOrValue, useType, diagnostics);

                            expression = methodGroup.Update(
                                    methodGroup.TypeArgumentsOpt,
                                    methodGroup.Name,
                                    methodGroup.Methods,
                                    methodGroup.LookupSymbolOpt,
                                    methodGroup.LookupError,
                                    methodGroup.Flags,
                                    finalReceiver,
                                    methodGroup.ResultKind);
                            break;
                    }
                }
            }

            ImmutableArray<BoundExpression> argArray = BuildArgumentsForDynamicInvocation(arguments, diagnostics);

            hasErrors &= ReportBadDynamicArguments(node, argArray, diagnostics, queryClause);

            return new BoundDynamicInvocation(
                node,
                expression,
                argArray,
                arguments.GetNames(),
                arguments.RefKinds.ToImmutableOrNull(),
                applicableMethods,
                Compilation.DynamicType,
                hasErrors);
        }

        private ImmutableArray<BoundExpression> BuildArgumentsForDynamicInvocation(AnalyzedArguments arguments, DiagnosticBag diagnostics)
        {
            for (int i = 0; i < arguments.Arguments.Count; i++)
            {
                if (arguments.Arguments[i].Kind == BoundKind.UninitializedVarDeclarationExpression)
                {
                    var builder = ArrayBuilder<BoundExpression>.GetInstance(arguments.Arguments.Count);
                    builder.AddRange(arguments.Arguments);

                    do
                    {
                        BoundExpression argument = builder[i];

                        if (argument.Kind == BoundKind.UninitializedVarDeclarationExpression)
                        {
                            builder[i] = ((UninitializedVarDeclarationExpression)argument).FailInference(this, diagnostics);
                        }

                        i++;
                    }
                    while (i < builder.Count);

                    return builder.ToImmutableAndFree();
                }
            }

            return arguments.Arguments.ToImmutable();
        }

        // Returns true if there were errors.
        private static bool ReportBadDynamicArguments(
            CSharpSyntaxNode node,
            ImmutableArray<BoundExpression> arguments,
            DiagnosticBag diagnostics,
            CSharpSyntaxNode queryClause)
        {
            bool hasErrors = false;
            bool reportedBadQuery = false;

            foreach (var arg in arguments)
            {
                if (!IsLegalDynamicOperand(arg))
                {
                    if (queryClause != null && !reportedBadQuery)
                    {
                        reportedBadQuery = true;
                        Error(diagnostics, ErrorCode.ERR_BadDynamicQuery, node);
                        hasErrors = true;
                        continue;
                    }

                    if (arg.Kind == BoundKind.Lambda || arg.Kind == BoundKind.UnboundLambda)
                    {
                        // Cannot use a lambda expression as an argument to a dynamically dispatched operation without first casting it to a delegate or expression tree type.
                        Error(diagnostics, ErrorCode.ERR_BadDynamicMethodArgLambda, arg.Syntax);
                        hasErrors = true;
                    }
                    else if (arg.Kind == BoundKind.MethodGroup)
                    {
                        // Cannot use a method group as an argument to a dynamically dispatched operation. Did you intend to invoke the method?
                        Error(diagnostics, ErrorCode.ERR_BadDynamicMethodArgMemgrp, arg.Syntax);
                        hasErrors = true;
                    }
                    else if (arg.Kind == BoundKind.ArgListOperator)
                    {
                        // Not a great error message, since __arglist is not a type, but it'll do.

                        // error CS1978: Cannot use an expression of type '__arglist' as an argument to a dynamically dispatched operation
                        Error(diagnostics, ErrorCode.ERR_BadDynamicMethodArg, arg.Syntax, "__arglist");
                    }
                    else
                    {
                        // Lambdas,anonymous methods and method groups are the typeless expressions that
                        // are not usable as dynamic arguments; if we get here then the expression must have a type.
                        Debug.Assert((object)arg.Type != null);
                        // error CS1978: Cannot use an expression of type 'int*' as an argument to a dynamically dispatched operation

                        Error(diagnostics, ErrorCode.ERR_BadDynamicMethodArg, arg.Syntax, arg.Type);
                        hasErrors = true;
                    }
                }
            }
            return hasErrors;
        }

        private BoundExpression BindDelegateInvocation(
            CSharpSyntaxNode node,
            CSharpSyntaxNode expression,
            string methodName,
            BoundExpression boundExpression,
            AnalyzedArguments analyzedArguments,
            DiagnosticBag diagnostics,
            CSharpSyntaxNode queryClause,
            NamedTypeSymbol delegateType)
        {
            BoundExpression result;
            var methodGroup = MethodGroup.GetInstance();
            methodGroup.PopulateWithSingleMethod(boundExpression, delegateType.DelegateInvokeMethod);
            var overloadResolutionResult = OverloadResolutionResult<MethodSymbol>.GetInstance();
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            OverloadResolution.MethodInvocationOverloadResolution(methodGroup.Methods, methodGroup.TypeArguments, analyzedArguments, overloadResolutionResult, ref useSiteDiagnostics);
            diagnostics.Add(node, useSiteDiagnostics);

            // If overload resolution on the "Invoke" method found an applicable candidate, and one of the arguments
            // was dynamic then treat this as a dynamic call.
            if (analyzedArguments.HasDynamicArgument && overloadResolutionResult.HasAnyApplicableMember)
            {
                result = BindDynamicInvocation(node, boundExpression, analyzedArguments, overloadResolutionResult.GetAllApplicableMembers(), diagnostics, queryClause);
            }
            else
            {
                result = BindInvocationExpressionContinued(node, expression, methodName, overloadResolutionResult, analyzedArguments, methodGroup, delegateType, diagnostics, queryClause);
            }

            overloadResolutionResult.Free();
            methodGroup.Free();
            return result;
        }

        private static bool HasApplicableConditionalMethod(OverloadResolutionResult<MethodSymbol> results)
        {
            var r = results.Results;
            for (int i = 0; i < r.Length; ++i)
            {
                if (r[i].IsApplicable && r[i].Member.IsConditional)
                {
                    return true;
                }
            }

            return false;
        }

        private BoundExpression BindMethodGroupInvocation(
            CSharpSyntaxNode syntax,
            CSharpSyntaxNode expression,
            string methodName,
            BoundMethodGroup methodGroup,
            AnalyzedArguments analyzedArguments,
            DiagnosticBag diagnostics,
            CSharpSyntaxNode queryClause)
        {
            BoundExpression result;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var resolution = this.ResolveMethodGroup(methodGroup, expression, methodName, analyzedArguments, isMethodGroupConversion: false, useSiteDiagnostics: ref useSiteDiagnostics);
            diagnostics.Add(expression, useSiteDiagnostics);

            if (!methodGroup.HasAnyErrors) diagnostics.AddRange(resolution.Diagnostics); // Suppress cascading.

            if (resolution.HasAnyErrors)
            {
                ImmutableArray<MethodSymbol> originalMethods;
                LookupResultKind resultKind;
                ImmutableArray<TypeSymbol> typeArguments;
                if (resolution.OverloadResolutionResult != null)
                {
                    originalMethods = GetOriginalMethods(resolution.OverloadResolutionResult);
                    resultKind = resolution.MethodGroup.ResultKind;
                    typeArguments = resolution.MethodGroup.TypeArguments.ToImmutable();
                }
                else
                {
                    originalMethods = methodGroup.Methods;
                    resultKind = methodGroup.ResultKind;
                    typeArguments = methodGroup.TypeArgumentsOpt;
                }

                result = CreateBadCall(
                    syntax,
                    methodName,
                    methodGroup.ReceiverOpt,
                    originalMethods,
                    resultKind,
                    typeArguments,
                    analyzedArguments,
                    invokedAsExtensionMethod: resolution.IsExtensionMethodGroup,
                    isDelegate: false);
            }
            else if (!resolution.IsEmpty)
            {
                // We're checking resolution.ResultKind, rather than methodGroup.HasErrors
                // to better handle the case where there's a problem with the receiver
                // (e.g. inaccessible), but the method group resolved correctly (e.g. because
                // it's actually an accessible static method on a base type).
                // CONSIDER: could check for error types amongst method group type arguments.
                if (resolution.ResultKind != LookupResultKind.Viable)
                {
                    if (resolution.MethodGroup != null)
                    {
                        // we want to force any unbound lambda arguments to cache an appropriate conversion if possible; see 9448.
                        DiagnosticBag discarded = DiagnosticBag.GetInstance();
                        result = BindInvocationExpressionContinued(syntax, expression, methodName, resolution.OverloadResolutionResult, resolution.AnalyzedArguments, resolution.MethodGroup, null, discarded, queryClause);
                        discarded.Free();
                    }
                    // Since the resolution is non-empty and has no diagnostics, the LookupResultKind in its MethodGroup is uninteresting.
                    result = CreateBadCall(syntax, methodGroup, methodGroup.ResultKind, analyzedArguments);
                }
                else
                {
                    // If overload resolution found one or more applicable methods and at least one argument
                    // was dynamic then treat this as a dynamic call.
                    if (resolution.AnalyzedArguments.HasDynamicArgument && resolution.OverloadResolutionResult.HasAnyApplicableMember)
                    {
                        if (resolution.IsExtensionMethodGroup)
                        {
                            // error CS1973: 'T' has no applicable method named 'M' but appears to have an
                            // extension method by that name. Extension methods cannot be dynamically dispatched. Consider
                            // casting the dynamic arguments or calling the extension method without the extension method
                            // syntax.

                            // We found an extension method, so the instance associated with the method group must have 
                            // existed and had a type.
                            Debug.Assert(methodGroup.InstanceOpt != null && (object)methodGroup.InstanceOpt.Type != null);

                            Error(diagnostics, ErrorCode.ERR_BadArgTypeDynamicExtension, syntax, methodGroup.InstanceOpt.Type, methodGroup.Name);
                            result = CreateBadCall(syntax, methodGroup, methodGroup.ResultKind, analyzedArguments);
                        }
                        else
                        {
                            if (HasApplicableConditionalMethod(resolution.OverloadResolutionResult))
                            {
                                // warning CS1974: The dynamically dispatched call to method 'Foo' may fail at runtime
                                // because one or more applicable overloads are conditional methods
                                Error(diagnostics, ErrorCode.WRN_DynamicDispatchToConditionalMethod, syntax, methodGroup.Name);
                            }

                            // Note that the runtime binder may consider candidates that haven't passed compile-time final validation 
                            // and an ambiguity error may be reported. Also additional checks are performed in runtime final validation 
                            // that are not performed at compile-time.
                            // Only if the set of final applicable candidates is empty we know for sure the call will fail at runtime.
                            var finalApplicableCandidates = GetCandidatesPassingFinalValidation(syntax, resolution.OverloadResolutionResult, methodGroup, diagnostics);
                            if (finalApplicableCandidates.Length > 0)
                            {
                                result = BindDynamicInvocation(syntax, methodGroup, resolution.AnalyzedArguments, finalApplicableCandidates, diagnostics, queryClause);
                            }
                            else
                            {
                                result = CreateBadCall(syntax, methodGroup, methodGroup.ResultKind, analyzedArguments);
                            }
                        }
                    }
                    else
                    {
                        result = BindInvocationExpressionContinued(syntax, expression, methodName, resolution.OverloadResolutionResult, resolution.AnalyzedArguments, resolution.MethodGroup, null, diagnostics, queryClause);
                    }
                }
            }
            else
            {
                result = CreateBadCall(syntax, methodGroup, methodGroup.ResultKind, analyzedArguments);
            }
            resolution.Free();
            return result;
        }

        private ImmutableArray<MethodSymbol> GetCandidatesPassingFinalValidation(CSharpSyntaxNode syntax, OverloadResolutionResult<MethodSymbol> overloadResolutionResult, BoundMethodGroup methodGroup, DiagnosticBag diagnostics)
        {
            Debug.Assert(overloadResolutionResult.HasAnyApplicableMember);

            var finalCandidates = ArrayBuilder<MethodSymbol>.GetInstance();
            DiagnosticBag firstFailed = null;
            DiagnosticBag candidateDiagnostics = DiagnosticBag.GetInstance();

            for (int i = 0, n = overloadResolutionResult.ResultsBuilder.Count; i < n; i++)
            {
                var result = overloadResolutionResult.ResultsBuilder[i];
                if (result.Result.IsApplicable)
                {
                    // For F to pass the check, all of the following must hold:
                    //      ...
                    // * If the type parameters of F were substituted in the step above, their constraints are satisfied.
                    // * If F is a static method, the method group must have resulted from a simple-name, a member-access through a type, 
                    //   or a member-access whose receiver can�t be classified as a type or value until after overload resolution (see �7.6.4.1). 
                    // * If F is an instance method, the method group must have resulted from a simple-name, a member-access through a variable or value, 
                    //   or a member-access whose receiver can�t be classified as a type or value until after overload resolution (see �7.6.4.1).

                    if (!MemberGroupFinalValidationAccessibilityChecks(methodGroup.ReceiverOpt, result.Member, syntax, candidateDiagnostics, invokedAsExtensionMethod: false) &&
                        (methodGroup.TypeArgumentsOpt.IsDefault || result.Member.CheckConstraints(this.Conversions, syntax, this.Compilation, candidateDiagnostics)))
                    {
                        finalCandidates.Add(result.Member);
                        continue;
                    }

                    if (firstFailed == null)
                    {
                        firstFailed = candidateDiagnostics;
                        candidateDiagnostics = DiagnosticBag.GetInstance();
                    }
                    else
                    {
                        candidateDiagnostics.Clear();
                    }
                }
            }

            if (firstFailed != null)
            {
                // Report diagnostics of the first candidate that failed the validation
                // unless we have at least one candidate that passes.
                if (finalCandidates.Count == 0)
                {
                    diagnostics.AddRange(firstFailed);
                }

                firstFailed.Free();
            }

            candidateDiagnostics.Free();

            return finalCandidates.ToImmutableAndFree();
        }

        private static void CheckRestrictedTypeReceiver(BoundExpression expression, Compilation compilation, DiagnosticBag diagnostics)
        {
            Debug.Assert(expression is BoundDynamicInvocation || expression is BoundCall);
            Debug.Assert(diagnostics != null);

            // It is never legal to box a restricted type, even if we are boxing it as the receiver
            // of a method call. When must be box? We skip boxing when the method in question is defined
            // on the restricted type or overridden by the restricted type.

            BoundCall call = expression as BoundCall;

            if (call != null &&
                !call.HasAnyErrors &&
                call.ReceiverOpt != null &&
                (object)call.ReceiverOpt.Type != null &&
                call.ReceiverOpt.Type.IsRestrictedType() &&
                call.Method.ContainingType != call.ReceiverOpt.Type)
            {
                // error CS0029: Cannot implicitly convert type 'TypedReference' to 'object'
                SymbolDistinguisher distinguisher = new SymbolDistinguisher(compilation, call.ReceiverOpt.Type, call.Method.ContainingType);
                Error(diagnostics, ErrorCode.ERR_NoImplicitConv, call.ReceiverOpt.Syntax, distinguisher.First, distinguisher.Second);
            }

            BoundDynamicInvocation dynInvoke = expression as BoundDynamicInvocation;
            if (dynInvoke != null &&
                !dynInvoke.HasAnyErrors &&
                (object)dynInvoke.Expression.Type != null &&
                dynInvoke.Expression.Type.IsRestrictedType())
            {
                // eg: b = typedReference.Equals(dyn);
                // error CS1978: Cannot use an expression of type 'TypedReference' as an argument to a dynamically dispatched operation
                Error(diagnostics, ErrorCode.ERR_BadDynamicMethodArg, dynInvoke.Expression.Syntax, dynInvoke.Expression.Type);
            }
        }

        /// <summary>
        /// Perform overload resolution on the method group or expression (BoundMethodGroup)
        /// and arguments and return a BoundExpression representing the invocation.
        /// </summary>
        /// <param name="node">Invocation syntax node.</param>
        /// <param name="expression">The syntax for the invoked method, including receiver.</param>
        /// <param name="methodName">Name of the invoked method.</param>
        /// <param name="result">Overload resolution result for method group executed by caller.</param>
        /// <param name="analyzedArguments">Arguments bound by the caller.</param>
        /// <param name="methodGroup">Method group if the invocation represents a potentially overloaded member.</param>
        /// <param name="delegateTypeOpt">Delegate type if method group represents a delegate.</param>
        /// <param name="diagnostics">Diagnostics.</param>
        /// <param name="queryClause">The syntax for the query clause generating this invocation expression, if any.</param>
        /// <returns>BoundCall or error expression representing the invocation.</returns>
        private BoundCall BindInvocationExpressionContinued(
            CSharpSyntaxNode node,
            CSharpSyntaxNode expression,
            string methodName,
            OverloadResolutionResult<MethodSymbol> result,
            AnalyzedArguments analyzedArguments,
            MethodGroup methodGroup,
            NamedTypeSymbol delegateTypeOpt,
            DiagnosticBag diagnostics,
            CSharpSyntaxNode queryClause = null)
        {
            Debug.Assert(node != null);
            Debug.Assert(methodGroup != null);
            Debug.Assert(methodGroup.Error == null);
            Debug.Assert(methodGroup.Methods.Count > 0);
            Debug.Assert(((object)delegateTypeOpt == null) || (methodGroup.Methods.Count == 1));

            var invokedAsExtensionMethod = methodGroup.IsExtensionMethodGroup;

            // Delegate invocations should never be considered extension method
            // invocations (even though the delegate may refer to an extension method).
            Debug.Assert(!invokedAsExtensionMethod || ((object)delegateTypeOpt == null));

            // We have already determined that we are not in a situation where we can successfully do
            // a dynamic binding. We might be in one of the following situations:
            //
            // * There were dynamic arguments but overload resolution still found zero applicable candidates.
            // * There were no dynamic arguments and overload resolution found zero applicable candidates.
            // * There were no dynamic arguments and overload resolution found multiple applicable candidates
            //   without being able to find the best one.
            //
            // In those three situations we might give an additional error.

            if (!result.Succeeded)
            {
                // If the arguments had an error reported about them then suppress further error
                // reporting for overload resolution. 

                if (!analyzedArguments.HasErrors)
                {
                    string name = (object)delegateTypeOpt == null ? methodName : null;
                    result.ReportDiagnostics(this, GetLocationForOverloadResolutionDiagnostic(node, expression), diagnostics, name,
                        methodGroup.Receiver, analyzedArguments, methodGroup.Methods.ToImmutable(),
                        typeContainingConstructor: null, delegateTypeBeingInvoked: delegateTypeOpt,
                        queryClause: queryClause);
                }

                return CreateBadCall(node, methodGroup.Name, methodGroup.Receiver,
                    GetOriginalMethods(result), methodGroup.ResultKind, methodGroup.TypeArguments.ToImmutable(), analyzedArguments, invokedAsExtensionMethod: invokedAsExtensionMethod, isDelegate: ((object)delegateTypeOpt != null));
            }

            // Otherwise, there were no dynamic arguments and overload resolution found a unique best candidate. 
            // We still have to determine if it passes final validation.

            var methodResult = result.ValidResult;
            var returnType = methodResult.Member.ReturnType;
            this.CoerceArguments(methodResult, analyzedArguments.Arguments, diagnostics);

            var method = methodResult.Member;
            var expanded = methodResult.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm;
            var argsToParams = methodResult.Result.ArgsToParamsOpt;

            // It is possible that overload resolution succeeded, but we have chosen an
            // instance method and we're in a static method. A careful reading of the
            // overload resolution spec shows that the "final validation" stage allows an
            // "implicit this" on any method call, not just method calls from inside
            // instance methods. Therefore we must detect this scenario here, rather than in
            // overload resolution.

            var receiver = ReplaceTypeOrValueReceiver(methodGroup.Receiver, method.IsStatic && !invokedAsExtensionMethod, diagnostics);

            // Note: we specifically want to do final validation (7.6.5.1) without checking delegate compatibility (15.2),
            // so we're calling MethodGroupFinalValidation directly, rather than via MethodGroupConversionHasErrors.
            // Note: final validation wants the receiver that corresponds to the source representation
            // (i.e. the first argument, if invokedAsExtensionMethod).
            var gotError = MemberGroupFinalValidation(receiver, method, expression, diagnostics, invokedAsExtensionMethod);

            // Skip building up a new array if the first argument doesn't have to be modified.
            ImmutableArray<BoundExpression> args;
            if (invokedAsExtensionMethod && !ReferenceEquals(receiver, methodGroup.Receiver))
            {
                ArrayBuilder<BoundExpression> builder = ArrayBuilder<BoundExpression>.GetInstance();

                // Because the receiver didn't pass through CoerceArguments, we need to apply an appropriate
                // conversion here.
                Debug.Assert(method.ParameterCount > 0);
                Debug.Assert(argsToParams.IsDefault || argsToParams[0] == 0);
                BoundExpression convertedReceiver = CreateConversion(receiver, methodResult.Result.ConversionForArg(0), method.Parameters[0].Type, diagnostics);
                builder.Add(convertedReceiver);

                bool first = true;
                foreach (BoundExpression arg in analyzedArguments.Arguments)
                {
                    if (first)
                    {
                        // Skip the first argument (the receiver), since we added our own.
                        first = false;
                    }
                    else
                    {
                        builder.Add(arg);
                    }
                }
                args = builder.ToImmutableAndFree();
            }
            else
            {
                args = analyzedArguments.Arguments.ToImmutable();
            }

            // This will be the receiver of the BoundCall node that we create.
            // For extension methods, there is no receiver because the receiver in source was actually the first argument.
            // For instance methods, we may have synthesized an implicit this node.  We'll keep it for the emitter.
            // For static methods, we may have synthesized a type expression.  It serves no purpose, so we'll drop it.
            if (invokedAsExtensionMethod || (method.IsStatic && receiver != null && receiver.WasCompilerGenerated))
            {
                receiver = null;
            }

            var argNames = analyzedArguments.GetNames();
            var argRefKinds = analyzedArguments.RefKinds.ToImmutableOrNull();

            if (!gotError && !method.IsStatic && receiver != null && receiver.Kind == BoundKind.ThisReference && receiver.WasCompilerGenerated)
            {
                gotError = IsRefOrOutThisParameterCaptured(node, diagnostics);
            }

            // What if some of the arguments are implicit?  Dev10 reports unsafe errors
            // if the implied argument would have an unsafe type.  We need to check
            // the parameters explicitly, since there won't be bound nodes for the implied
            // arguments until lowering.
            if (method.HasUnsafeParameter())
            {
                // Don't worry about double reporting (i.e. for both the argument and the parameter)
                // because only one unsafe diagnostic is allowed per scope - the others are suppressed.
                gotError = ReportUnsafeIfNotAllowed(node, diagnostics) || gotError;
            }

            bool hasBaseReceiver = receiver != null && receiver.Kind == BoundKind.BaseReference;

            ReportDiagnosticsIfObsolete(diagnostics, method, node, hasBaseReceiver);

            // No use site errors, but there could be use site warnings.
            // If there are any use site warnings, they have already been reported by overload resolution.
            Debug.Assert(!method.HasUseSiteError, "Shouldn't have reached this point if there were use site errors.");

            if (method.IsRuntimeFinalizer())
            {
                ErrorCode code = hasBaseReceiver
                    ? ErrorCode.ERR_CallingBaseFinalizeDeprecated
                    : ErrorCode.ERR_CallingFinalizeDeprecated;
                Error(diagnostics, code, node);
                gotError = true;
            }

            if ((object)delegateTypeOpt != null)
            {
                return new BoundCall(node, receiver, method, args, argNames, argRefKinds, isDelegateCall: true,
                            expanded: expanded, invokedAsExtensionMethod: invokedAsExtensionMethod,
                            argsToParamsOpt: argsToParams, resultKind: LookupResultKind.Viable, type: returnType, hasErrors: gotError);
            }
            else
            {
                if ((object)receiver != null && receiver.Kind == BoundKind.BaseReference && method.IsAbstract)
                {
                    Error(diagnostics, ErrorCode.ERR_AbstractBaseCall, node, method);
                    gotError = true;
                }

                return new BoundCall(node, receiver, method, args, argNames, argRefKinds, isDelegateCall: false,
                            expanded: expanded, invokedAsExtensionMethod: invokedAsExtensionMethod,
                            argsToParamsOpt: argsToParams, resultKind: LookupResultKind.Viable, type: returnType, hasErrors: gotError);
            }
        }

        /// <param name="node">Invocation syntax node.</param>
        /// <param name="expression">The syntax for the invoked method, including receiver.</param>
        private Location GetLocationForOverloadResolutionDiagnostic(CSharpSyntaxNode node, CSharpSyntaxNode expression)
        {
            if (node != expression)
            {
                switch (expression.CSharpKind())
                {
                    case SyntaxKind.QualifiedName:
                        return ((QualifiedNameSyntax)expression).Right.GetLocation();

                    case SyntaxKind.SimpleMemberAccessExpression:
                    case SyntaxKind.PointerMemberAccessExpression:
                        return ((MemberAccessExpressionSyntax)expression).Name.GetLocation();
                }
            }

            return expression.GetLocation();
        }

        /// <summary>
        /// Replace a BoundTypeOrValueExpression with a BoundExpression for either a type (if useType is true)
        /// or a value (if useType is false).  Any other node is unmodified.
        /// </summary>
        /// <remarks>
        /// Call this once overload resolution has succeeded on the method group of which the BoundTypeOrValueExpression
        /// is the receiver.  Generally, useType will be true if the chosen method is static and false otherwise.
        /// </remarks>
        private static BoundExpression ReplaceTypeOrValueReceiver(BoundExpression receiver, bool useType, DiagnosticBag diagnostics)
        {
            if (receiver == null) return receiver;
            switch (receiver.Kind)
            {
                case BoundKind.TypeOrValueExpression:
                    var typeOrValue = (BoundTypeOrValueExpression)receiver;
                    Binder binder = typeOrValue.Binder;
                    ExpressionSyntax syntax = (ExpressionSyntax)receiver.Syntax;
                    receiver = useType ?
                        binder.BindNamespaceOrType(syntax, diagnostics) :
                        binder.BindValue(syntax, diagnostics, BindValueKind.RValue);

                    return receiver;

                case BoundKind.QueryClause:
                    // a query clause may wrap a TypeOrValueExpression.
                    var q = (BoundQueryClause)receiver;
                    var value = q.Value;
                    var replaced = ReplaceTypeOrValueReceiver(value, useType, diagnostics);
                    return (value == replaced) ? q : q.Update(replaced, q.DefinedSymbol, q.Operation, q.Cast, q.Binder, q.UnoptimizedForm, q.Type);
            }

            return receiver;
        }

        /// <summary>
        /// Return the delegate type if this expression represents a delegate.
        /// </summary>
        private static NamedTypeSymbol GetDelegateType(BoundExpression expr)
        {
            if ((expr != null) && (expr.Kind != BoundKind.TypeExpression))
            {
                var type = expr.Type as NamedTypeSymbol;
                if (((object)type != null) && type.IsDelegateType())
                {
                    return type;
                }
            }
            return null;
        }

        private BoundCall CreateBadCall(
            CSharpSyntaxNode node,
            string name,
            BoundExpression receiver,
            ImmutableArray<MethodSymbol> methods,
            LookupResultKind resultKind,
            ImmutableArray<TypeSymbol> typeArguments,
            AnalyzedArguments analyzedArguments,
            bool invokedAsExtensionMethod,
            bool isDelegate)
        {
            MethodSymbol method;
            ImmutableArray<BoundExpression> args;
            if (!typeArguments.IsDefaultOrEmpty)
            {
                var constructedMethods = ArrayBuilder<MethodSymbol>.GetInstance();
                foreach (var m in methods)
                {
                    constructedMethods.Add(m.ConstructedFrom == m && m.Arity == typeArguments.Length ? m.Construct(typeArguments) : m);
                }

                methods = constructedMethods.ToImmutableAndFree();
            }

            if (methods.Length == 1)
            {
                // If there is only one method in the group, we should attempt to bind to it.  That includes
                // binding any lambdas in the argument list against the method's parameter types.
                method = methods[0];
                args = BuildArgumentsForErrorRecovery(analyzedArguments, method.Parameters);
            }
            else
            {
                var returnType = GetCommonTypeOrReturnType(methods) ?? new ExtendedErrorTypeSymbol(this.Compilation, string.Empty, arity: 0, errorInfo: null);
                var methodContainer = receiver != null && (object)receiver.Type != null
                    ? receiver.Type
                    : this.ContainingType;
                method = new ErrorMethodSymbol(methodContainer, returnType, name);
                args = BuildArgumentsForErrorRecovery(analyzedArguments);
            }

            var argNames = analyzedArguments.GetNames();
            var argRefKinds = analyzedArguments.RefKinds.ToImmutableOrNull();
            return BoundCall.ErrorCall(node, receiver, method, args, argNames, argRefKinds, isDelegate, invokedAsExtensionMethod: invokedAsExtensionMethod, originalMethods: methods, resultKind: resultKind);
        }

        private ImmutableArray<BoundExpression> BuildArgumentsForErrorRecovery(AnalyzedArguments analyzedArguments, ImmutableArray<ParameterSymbol> parameters)
        {
            ArrayBuilder<BoundExpression> oldArguments = analyzedArguments.Arguments;
            int argumentCount = oldArguments.Count;
            int parameterCount = parameters.Length;

            for (int i = 0; i < argumentCount; i++)
            {
                BoundKind argumentKind = oldArguments[i].Kind;

                if (argumentKind == BoundKind.UninitializedVarDeclarationExpression ||
                    (argumentKind == BoundKind.UnboundLambda && i < parameterCount))
                {
                    ArrayBuilder<BoundExpression> newArguments = ArrayBuilder<BoundExpression>.GetInstance(argumentCount);
                    newArguments.AddRange(oldArguments);

                    do
                {
                        BoundExpression oldArgument = newArguments[i];

                        if (i < parameterCount)
                        {
                            switch (oldArgument.Kind)
                            {
                                case BoundKind.UnboundLambda:
                    NamedTypeSymbol parameterType = parameters[i].Type as NamedTypeSymbol;
                    if ((object)parameterType != null)
                    {
                                        newArguments[i] = ((UnboundLambda)oldArgument).Bind(parameterType);
                    }
                                    break;

                                case BoundKind.UninitializedVarDeclarationExpression:
                                    newArguments[i] = ((UninitializedVarDeclarationExpression)oldArgument).SetInferredType(parameters[i].Type, success: true);
                                    break;
                }
            }
                        else if (oldArgument.Kind == BoundKind.UninitializedVarDeclarationExpression)
                        {
                            newArguments[i] = ((UninitializedVarDeclarationExpression)oldArgument).FailInference(this, null);
                        }

                        i++;
                    }
                    while (i < argumentCount);

            return newArguments.ToImmutableAndFree();
        }
            }

            return oldArguments.ToImmutable();
        }

        private ImmutableArray<BoundExpression> BuildArgumentsForErrorRecovery(AnalyzedArguments analyzedArguments)
        {
            return BuildArgumentsForErrorRecovery(analyzedArguments, ImmutableArray<ParameterSymbol>.Empty);
        }

        private BoundCall CreateBadCall(
            CSharpSyntaxNode node,
            BoundExpression expr,
            LookupResultKind resultKind,
            AnalyzedArguments analyzedArguments)
        {
            TypeSymbol returnType = new ExtendedErrorTypeSymbol(this.Compilation, string.Empty, arity: 0, errorInfo: null);
            var methodContainer = expr.Type ?? this.ContainingType;
            MethodSymbol method = new ErrorMethodSymbol(methodContainer, returnType, string.Empty);

            var args = BuildArgumentsForErrorRecovery(analyzedArguments);
            var argNames = analyzedArguments.GetNames();
            var argRefKinds = analyzedArguments.RefKinds.ToImmutableOrNull();
            var originalMethods = (expr.Kind == BoundKind.MethodGroup) ? ((BoundMethodGroup)expr).Methods : ImmutableArray<MethodSymbol>.Empty;

            return BoundCall.ErrorCall(node, expr, method, args, argNames, argRefKinds, isDelegateCall: false, invokedAsExtensionMethod: false, originalMethods: originalMethods, resultKind: resultKind);
        }

        private static TypeSymbol GetCommonTypeOrReturnType<TMember>(ImmutableArray<TMember> members)
            where TMember : Symbol
        {
            TypeSymbol type = null;
            for (int i = 0, n = members.Length; i < n; i++)
            {
                TypeSymbol returnType = members[i].GetTypeOrReturnType();
                if ((object)type == null)
                {
                    type = returnType;
                }
                else if (type != returnType)
                {
                    return null;
                }
            }

            return type;
        }

        private static NameSyntax GetNameSyntax(CSharpSyntaxNode syntax)
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
        /// <returns></returns>
        private static NameSyntax GetNameSyntax(CSharpSyntaxNode syntax, out string nameString)
        {
            nameString = string.Empty;
            while (true)
            {
                switch (syntax.Kind)
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
        private void BindArgumentsAndNames(ArgumentListSyntax argumentListOpt, DiagnosticBag diagnostics, AnalyzedArguments result, bool allowArglist = false)
        {
            if (argumentListOpt != null)
            {
                BindArgumentsAndNames(argumentListOpt.Arguments, diagnostics, result, allowArglist);
            }
        }

        private void BindArgumentsAndNames(BracketedArgumentListSyntax argumentListOpt, DiagnosticBag diagnostics, AnalyzedArguments result)
        {
            if (argumentListOpt != null)
            {
                BindArgumentsAndNames(argumentListOpt.Arguments, diagnostics, result, allowArglist: false);
            }
        }

        private void BindArgumentsAndNames(
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            DiagnosticBag diagnostics,
            AnalyzedArguments result,
            bool allowArglist)
        {
            // Only report the first "duplicate name" or "named before positional" error, 
            // so as to avoid "cascading" errors.
            bool hadError = false;

            for (int i = 0, l = arguments.Count; i < l; i++)
            {
                var argumentSyntax = arguments[i];

                hadError = BindArgumentAndName(result, diagnostics, hadError, argumentSyntax, allowArglist);
            }
        }

        private bool BindArgumentAndName(
            AnalyzedArguments result,
            DiagnosticBag diagnostics,
            bool hadError,
            ArgumentSyntax argumentSyntax,
            bool allowArglist)
        {
                RefKind refKind = argumentSyntax.RefOrOutKeyword.CSharpKind().GetRefKind();

                hadError |= BindArgumentAndName(
                    result,
                    diagnostics,
                    hadError,
                    argumentSyntax,
                    argumentSyntax.Expression,
                    argumentSyntax.NameColon,
                    refKind,
                    allowArglist);

            return hadError;
            }


        // Bind a named/positional argument.
        // Prevent cascading diagnostic by considering the previous
        // error state and returning the updated error state.
        private bool BindArgumentAndName(
            AnalyzedArguments result,
            DiagnosticBag diagnostics,
            bool hadError,
            CSharpSyntaxNode argumentSyntax,
            ExpressionSyntax argumentExpression,
            NameColonSyntax nameColonSyntax,
            RefKind refKind,
            bool allowArglist)
        {
            Debug.Assert(argumentSyntax is ArgumentSyntax || argumentSyntax is AttributeArgumentSyntax);

            BindValueKind valueKind = refKind == RefKind.None ? BindValueKind.RValue : BindValueKind.OutParameter;

            // Bind argument and verify argument matches rvalue or out param requirements.
            BoundExpression argument;
            if (allowArglist)
            {
                argument = this.BindValueAllowArgList(argumentExpression, diagnostics, valueKind);
            }
            else
            {
                argument = this.BindValue(argumentExpression, diagnostics, valueKind);
            }

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

                string name = nameColonSyntax.Name.Identifier.ValueText;

                // Note that because of this nested loop this is an O(n^2) algorithm; 
                // however, the set of named arguments in an invocation is likely to be small.

                bool hasNameCollision = false;
                for (int i = 0; i < result.Names.Count; ++i)
                {
                    if (result.Name(i) == name)
                    {
                        hasNameCollision = true;
                        break;
                    }
                }

                if (hasNameCollision)
                {
                    if (!hadError)
                    {
                        // CS: Named argument '{0}' cannot be specified multiple times
                        Error(diagnostics, ErrorCode.ERR_DuplicateNamedArgument, nameColonSyntax.Name, name);

                        hadError = true;
                    }

                    argument = ToBadExpression(argument);
                }

                result.Names.Add(nameColonSyntax.Name);
            }
            else if (hasNames)
            {
                // We just saw a fixed-position argument after a named argument.
                if (!hadError)
                {
                    // CS1738: Named argument specifications must appear after all fixed arguments have been specified
                    Error(diagnostics, ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, argumentSyntax);
                    hadError = true;
                }

                argument = ToBadExpression(argument);

                result.Names.Add(null);
            }

            result.Arguments.Add(argument);

            return hadError;
        }

        private void CoerceArguments<TMember>(
            MemberResolutionResult<TMember> methodResult,
            ArrayBuilder<BoundExpression> arguments,
            DiagnosticBag diagnostics)
            where TMember : Symbol
        {
            var result = methodResult.Result;

            // Parameter types should be taken from the least overridden member:
            var parameters = methodResult.LeastOverriddenMember.GetParameters();

            for (int arg = 0; arg < arguments.Count; ++arg)
            {
                var kind = result.ConversionForArg(arg);
                BoundExpression argument = arguments[arg];

                if (!kind.IsIdentity)
                {
                    TypeSymbol type = GetCorrespondingParameterType(ref result, parameters, arg);

                    // NOTE: for some reason, dev10 doesn't report this for indexer accesses.
                    if (!methodResult.Member.IsIndexer() && !argument.HasAnyErrors && type.IsUnsafe())
                    {
                        // CONSIDER: dev10 uses the call syntax, but this seems clearer.
                        ReportUnsafeIfNotAllowed(argument.Syntax, diagnostics);
                        //CONSIDER: Return a bad expression so that HasErrors is true?
                    }

                    arguments[arg] = CreateConversion(argument.Syntax, argument, kind, false, type, diagnostics);
                }
                else if (argument.Kind == BoundKind.UninitializedVarDeclarationExpression)
                {
                    TypeSymbol parameterType = GetCorrespondingParameterType(ref result, parameters, arg);
                    bool hasErrors = false;

                    if (this.ContainingMemberOrLambda.Kind == SymbolKind.Method
                        && ((MethodSymbol)this.ContainingMemberOrLambda).IsAsync
                        && parameterType.IsRestrictedType())
                    {
                        Error(diagnostics, ErrorCode.ERR_BadSpecialByRefLocal,
                              argument.Syntax.CSharpKind() == SyntaxKind.DeclarationExpression ?
                                    ((DeclarationExpressionSyntax)argument.Syntax).Type :
                                    argument.Syntax,
                              parameterType);

                        hasErrors = true;
                    }

                    arguments[arg] = ((UninitializedVarDeclarationExpression)argument).SetInferredType(parameterType, success: !hasErrors);
                }
            }
        }

        private static TypeSymbol GetCorrespondingParameterType(ref MemberAnalysisResult result, ImmutableArray<ParameterSymbol> parameters, int arg)
        {
            int paramNum = result.ParameterFromArgument(arg);
            var type =
                (paramNum == parameters.Length - 1 && result.Kind == MemberResolutionKind.ApplicableInExpandedForm) ?
                ((ArrayTypeSymbol)parameters[paramNum].Type).ElementType :
                parameters[paramNum].Type;
            return type;
        }

        private BoundExpression BindArrayCreationExpression(ArrayCreationExpressionSyntax node, DiagnosticBag diagnostics)
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

            var type = (ArrayTypeSymbol)BindType(node.Type, diagnostics);

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
            foreach (var arg in node.Type.RankSpecifiers[0].Sizes)
            {
                // These make the parse tree nicer, but they shouldn't actually appear in the bound tree.
                if (arg.Kind != SyntaxKind.OmittedArraySizeExpression)
                {
                    var size = BindValue(arg, diagnostics, BindValueKind.RValue);
                    if (!size.HasAnyErrors)
                    {
                        size = ConvertToArrayIndex(size, node, diagnostics);
                        if (IsNegativeConstantForArraySize(size))
                        {
                            Error(diagnostics, ErrorCode.ERR_NegativeArraySize, arg);
                        }
                    }

                    sizes.Add(size);
                }
            }

            ImmutableArray<BoundExpression> arraySizes = sizes.ToImmutableAndFree();

            return node.Initializer == null
                ? new BoundArrayCreation(node, arraySizes, null, type)
                : BindArrayCreationWithInitializer(diagnostics, node, node.Initializer, type, arraySizes);
        }

        private BoundExpression BindImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node, DiagnosticBag diagnostics)
        {
            // See BindArrayCreationExpression method above for implicitly typed array creation SPEC.

            InitializerExpressionSyntax initializer = node.Initializer;
            int rank = node.Commas.Count + 1;

            ImmutableArray<BoundExpression> boundInitializerExpressions = BindArrayInitializerExpressions(initializer, diagnostics, dimension: 1, rank: rank);

            bool hadMultipleCandidates;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            TypeSymbol bestType = BestTypeInferrer.InferBestType(boundInitializerExpressions, this.Conversions, out hadMultipleCandidates, ref useSiteDiagnostics);
            diagnostics.Add(node, useSiteDiagnostics);

            if ((object)bestType == null || bestType.SpecialType == SpecialType.System_Void) // Dev10 also reports ERR_ImplicitlyTypedArrayNoBestType for void.
            {
                Error(diagnostics, ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, node);
                bestType = CreateErrorType();
            }

            if (bestType.IsRestrictedType())
            {
                // CS0611: Array elements cannot be of type '{0}'
                Error(diagnostics, ErrorCode.ERR_ArrayElementCantBeRefAny, node, bestType);
            }

            var arrayType = new ArrayTypeSymbol(Compilation.Assembly, bestType, ImmutableArray<CustomModifier>.Empty, rank);
            return BindArrayCreationWithInitializer(diagnostics, node, initializer, arrayType,
                sizes: ImmutableArray<BoundExpression>.Empty, boundInitExprOpt: boundInitializerExpressions);
        }

        // This method binds all the array initializer expressions.
        // NOTE: It doesn't convert the bound initializer expressions to array's element type.
        // NOTE: This is done separately in ConvertAndBindArrayInitialization method below.
        private ImmutableArray<BoundExpression> BindArrayInitializerExpressions(InitializerExpressionSyntax initializer, DiagnosticBag diagnostics, int dimension, int rank)
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
        private void BindArrayInitializerExpressions(InitializerExpressionSyntax initializer, ArrayBuilder<BoundExpression> exprBuilder, DiagnosticBag diagnostics, int dimension, int rank)
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
                    if (expression.Kind == SyntaxKind.ArrayInitializerExpression)
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
                                ImmutableArray.Create<Symbol>(boundExpression.ExpressionSymbol),
                                ImmutableArray.Create<BoundExpression>(boundExpression));
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
            DiagnosticBag diagnostics,
            InitializerExpressionSyntax node,
            ArrayTypeSymbol type,
            int?[] knownSizes,
            int dimension,
            ImmutableArray<BoundExpression> boundInitExpr,
            ref int boundInitExprIndex)
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
                    if (expr.Kind == SyntaxKind.ArrayInitializerExpression)
                    {
                        init = ConvertAndBindArrayInitialization(diagnostics, (InitializerExpressionSyntax)expr,
                             type, knownSizes, dimension + 1, boundInitExpr, ref boundInitExprIndex);
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

            return new BoundArrayInitialization(node, initializers.ToImmutableAndFree(), hasErrors: hasErrors);
        }

        private BoundArrayInitialization BindArrayInitializerList(
           DiagnosticBag diagnostics,
           InitializerExpressionSyntax node,
           ArrayTypeSymbol type,
           int?[] knownSizes,
           int dimension,
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
            return ConvertAndBindArrayInitialization(diagnostics, node, type, knownSizes, dimension, boundInitExprOpt, ref boundInitExprIndex);
        }

        private BoundArrayInitialization BindUnexpectedArrayInitializer(
            InitializerExpressionSyntax node,
            DiagnosticBag diagnostics,
            ErrorCode errorCode,
            CSharpSyntaxNode errorNode = null)
        {
            var result = BindArrayInitializerList(
                diagnostics,
                node,
                this.Compilation.CreateArrayTypeSymbol(GetSpecialType(SpecialType.System_Object, diagnostics, node)),
                new int?[1],
                dimension: 1);

            if (!result.HasAnyErrors)
            {
                result = new BoundArrayInitialization(node, result.Initializers, hasErrors: true);
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
            DiagnosticBag diagnostics,
            ExpressionSyntax creationSyntax,
            InitializerExpressionSyntax initSyntax,
            ArrayTypeSymbol type,
            ImmutableArray<BoundExpression> sizes,
            ImmutableArray<BoundExpression> boundInitExprOpt = default(ImmutableArray<BoundExpression>))
        {
            Debug.Assert(creationSyntax == null ||
                creationSyntax.Kind == SyntaxKind.ArrayCreationExpression ||
                creationSyntax.Kind == SyntaxKind.ImplicitArrayCreationExpression);
            Debug.Assert(initSyntax != null);
            Debug.Assert((object)type != null);
            Debug.Assert(boundInitExprOpt.IsDefault || creationSyntax.Kind == SyntaxKind.ImplicitArrayCreationExpression);

            bool error = false;

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
                    error = true;
                }
            }

            // KnownSizes is further mutated by BindArrayInitializerList as it works out more
            // information about the sizes.
            BoundArrayInitialization initializer = BindArrayInitializerList(diagnostics, initSyntax, type, knownSizes, 1, boundInitExprOpt);

            error = error || initializer.HasAnyErrors;

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
            else if (!error && rank != numSizes)
            {
                Error(diagnostics, ErrorCode.ERR_BadIndexCount, nonNullSyntax, type.Rank);
                error = true;
            }

            return new BoundArrayCreation(nonNullSyntax, sizes, initializer, type, hasErrors: error) { WasCompilerGenerated = !hasCreationSyntax && 
                                                                                                       (initSyntax.Parent == null || 
                                                                                                        initSyntax.Parent.Kind != SyntaxKind.EqualsValueClause ||
                                                                                                        ((EqualsValueClauseSyntax)initSyntax.Parent).Value != initSyntax)};
        }

        private BoundExpression BindStackAllocArrayCreationExpression(StackAllocArrayCreationExpressionSyntax node, DiagnosticBag diagnostics)
        {
            bool hasErrors = ReportUnsafeIfNotAllowed(node, diagnostics);

            // Check if we're syntactically within a catch or finally clause.
            if (this.Flags.Includes(BinderFlags.InCatchBlock) ||
                this.Flags.Includes(BinderFlags.InCatchFilter) ||
                this.Flags.Includes(BinderFlags.InFinallyBlock))
            {
                Error(diagnostics, ErrorCode.ERR_StackallocInCatchFinally, node);
            }

            TypeSyntax typeSyntax = node.Type;

            if (typeSyntax.Kind != SyntaxKind.ArrayType)
            {
                // If the syntax node comes from the parser, it should already have ERR_BadStackAllocExpr.
                if (!typeSyntax.ContainsDiagnostics)
                {
                    Error(diagnostics, ErrorCode.ERR_BadStackAllocExpr, typeSyntax);
                }
                return new BoundBadExpression(
                    node,
                    LookupResultKind.NotCreatable, //in this context, anyway
                    ImmutableArray<Symbol>.Empty,
                    ImmutableArray<BoundNode>.Empty,
                    new PointerTypeSymbol(BindType(typeSyntax, diagnostics)));
            }

            ArrayTypeSyntax arrayTypeSyntax = (ArrayTypeSyntax)typeSyntax;
            TypeSyntax elementTypeSyntax = arrayTypeSyntax.ElementType;

            TypeSymbol elementType = BindType(elementTypeSyntax, diagnostics);
            PointerTypeSymbol pointerType = new PointerTypeSymbol(elementType);

            bool typeHasErrors = elementType.IsErrorType();
            if (!typeHasErrors && elementType.IsManagedType)
            {
                Error(diagnostics, ErrorCode.ERR_ManagedAddr, elementTypeSyntax, elementType);
                typeHasErrors = true;
            }

            SyntaxList<ArrayRankSpecifierSyntax> rankSpecifiers = arrayTypeSyntax.RankSpecifiers;

            if (rankSpecifiers.Count != 1 ||
                rankSpecifiers[0].Sizes.Count != 1 ||
                rankSpecifiers[0].Sizes[0].Kind == SyntaxKind.OmittedArraySizeExpression)
            {
                // NOTE: Dev10 reported several parse errors here.
                Error(diagnostics, ErrorCode.ERR_BadStackAllocExpr, typeSyntax);

                ArrayBuilder<BoundNode> builder = ArrayBuilder<BoundNode>.GetInstance();
                DiagnosticBag discardedDiagnostics = DiagnosticBag.GetInstance();
                foreach (ArrayRankSpecifierSyntax rankSpecifier in rankSpecifiers)
                {
                    foreach (ExpressionSyntax size in rankSpecifier.Sizes)
                    {
                        if (size.Kind != SyntaxKind.OmittedArraySizeExpression)
                        {
                            builder.Add(BindExpression(size, discardedDiagnostics));
                        }
                    }
                }
                discardedDiagnostics.Free();

                return new BoundBadExpression(
                    node,
                    LookupResultKind.Empty,
                    ImmutableArray<Symbol>.Empty,
                    builder.ToImmutableAndFree(),
                    pointerType);
            }

            ExpressionSyntax countSyntax = rankSpecifiers[0].Sizes[0];
            var count = BindValue(countSyntax, diagnostics, BindValueKind.RValue);
            if (!count.HasAnyErrors)
            {
                // NOTE: this is different from how we would bind an array size (in which case we would allow uint, long, or ulong).
                count = GenerateConversionForAssignment(GetSpecialType(SpecialType.System_Int32, diagnostics, node), count, diagnostics);
                if (!count.HasAnyErrors && IsNegativeConstantForArraySize(count))
                {
                    Error(diagnostics, ErrorCode.ERR_NegativeStackAllocSize, countSyntax);
                    hasErrors = true;
                }
            }

            return new BoundStackAllocArrayCreation(node, count, pointerType, hasErrors || typeHasErrors);
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

            var constantValue = expression.ConstantValue;

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

            var constantValue = expression.ConstantValue;
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
        /// BaseConstructorInitializerSyntax.ArgumentList, or 
        /// ThisConstructorInitializerSyntax.ArgumentList, or 
        /// BaseClassWithArgumentsSyntax.ArgumentList for explicit.</param>
        /// <param name="constructor">Constructor containing the initializer.</param>
        /// <param name="diagnostics">Accumulates errors (e.g. unable to find constructor to invoke).</param>
        /// <returns>A bound expression for the constructor initializer call.</returns>
        /// <remarks>
        /// This method should be kept consistent with Compiler.BindConstructorInitializer (e.g. same error codes).
        /// </remarks>
        internal BoundExpression BindConstructorInitializer(
            ArgumentListSyntax initializerArgumentListOpt,
            MethodSymbol constructor,
            DiagnosticBag diagnostics)
        {
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
                Debug.Assert(constructorReturnType.SpecialType == SpecialType.System_Void); //true of all constructors

                // Get the bound arguments and the argument names.
                // : this(__arglist()) is legal
                if (initializerArgumentListOpt != null)
                {
                    this.BindArgumentsAndNames(initializerArgumentListOpt, diagnostics, analyzedArguments, allowArglist: true);
                }

                NamedTypeSymbol initializerType = containingType;

                bool isBaseConstructorInitializer = initializerArgumentListOpt == null || 
                                                    initializerArgumentListOpt.Parent.Kind == SyntaxKind.BaseConstructorInitializer ||
                                                    initializerArgumentListOpt.Parent.Kind == SyntaxKind.BaseClassWithArguments;

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
                            diagnostics.Add(ErrorCode.ERR_ObjectCallingBaseConstructor, constructor.Locations[0], containingType);
                            return new BoundBadExpression(
                                syntax: initializerArgumentListOpt.Parent,
                                resultKind: LookupResultKind.Empty,
                                symbols: ImmutableArray<Symbol>.Empty,
                                childBoundNodes: BuildArgumentsForErrorRecovery(analyzedArguments).Cast<BoundExpression, BoundNode>(),
                                type: constructorReturnType);
                        }
                    }
                    else if (initializerArgumentListOpt != null && containingType.TypeKind == TypeKind.Struct)
                    {
                        diagnostics.Add(ErrorCode.ERR_StructWithBaseConstructorCall, constructor.Locations[0], containingType);
                        return new BoundBadExpression(
                            syntax: initializerArgumentListOpt.Parent,
                            resultKind: LookupResultKind.Empty,
                            symbols: ImmutableArray<Symbol>.Empty, //CONSIDER: we could look for a matching constructor on System.ValueType
                            childBoundNodes: BuildArgumentsForErrorRecovery(analyzedArguments).Cast<BoundExpression, BoundNode>(),
                            type: constructorReturnType);
                    }
                }
                else
                {
                    Debug.Assert(initializerArgumentListOpt.Parent.Kind == SyntaxKind.ThisConstructorInitializer);
                }

                if (initializerArgumentListOpt != null && analyzedArguments.HasDynamicArgument)
                {
                    diagnostics.Add(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor,
                                    initializerArgumentListOpt.Parent.Kind == SyntaxKind.BaseClassWithArguments ? 
                                        initializerArgumentListOpt.GetLocation() :
                                        ((ConstructorInitializerSyntax)initializerArgumentListOpt.Parent).ThisOrBaseKeyword.GetLocation());
                    return new BoundBadExpression(
                            syntax: initializerArgumentListOpt.Parent,
                            resultKind: LookupResultKind.Empty,
                            symbols: ImmutableArray<Symbol>.Empty, //CONSIDER: we could look for a matching constructor on System.ValueType
                            childBoundNodes: BuildArgumentsForErrorRecovery(analyzedArguments).Cast<BoundExpression, BoundNode>(),
                            type: constructorReturnType);
                }

                CSharpSyntaxNode nonNullSyntax;
                Location errorLocation;
                if (initializerArgumentListOpt != null)
                {
                    if (initializerArgumentListOpt.Parent.Kind == SyntaxKind.BaseClassWithArguments)
                    {
                        nonNullSyntax = initializerArgumentListOpt;
                        errorLocation = initializerArgumentListOpt.GetLocation();
                    }
                    else
                {
                        nonNullSyntax = initializerArgumentListOpt.Parent;
                        errorLocation = ((ConstructorInitializerSyntax)nonNullSyntax).ThisOrBaseKeyword.GetLocation();
                    }
                }
                else
                {
                    // Note: use syntax node of constructor with initializer, not constructor invoked by initializer (i.e. methodResolutionResult).
                    nonNullSyntax = constructor.GetNonNullSyntaxNode();
                    errorLocation = constructor.Locations[0];
                }

                BoundExpression receiver = new BoundThisReference(nonNullSyntax, initializerType) { WasCompilerGenerated = true };

                if (initializerType.IsErrorType())
                {
                    var sourceMethod = constructor as SourceMethodSymbol;
                    if ((object)sourceMethod != null && sourceMethod.IsPrimaryCtor)
                    {
                        // Do not perform overload resolution on erroneous base for primary constructor.
                        // The errors could be confusing because the type associated with the error type symbol
                        // might be different from the one the arguments are applied to (base type mismatch across 
                        // partial declarations, etc). 
                        var result = CreateBadCall(
                            node: nonNullSyntax,
                            name: WellKnownMemberNames.InstanceConstructorName,
                            receiver: receiver,
                            methods: ImmutableArray<MethodSymbol>.Empty,
                            resultKind: LookupResultKind.OverloadResolutionFailure,
                            typeArguments: ImmutableArray<TypeSymbol>.Empty,
                            analyzedArguments: analyzedArguments,
                            invokedAsExtensionMethod: false,
                            isDelegate: false);
                        result.WasCompilerGenerated = initializerArgumentListOpt == null;
                        return result;
                    }
                }

                MemberResolutionResult<MethodSymbol> memberResolutionResult;
                ImmutableArray<MethodSymbol> candidateConstructors;
                if (TryPerformConstructorOverloadResolution(
                    initializerType,
                    analyzedArguments,
                    WellKnownMemberNames.InstanceConstructorName,
                    errorLocation,
                    false, // Don't suppress result diagnostics
                    diagnostics,
                    out memberResolutionResult,
                    out candidateConstructors,
                    allowProtectedConstructorsOfBaseType: true))
                {

                    bool hasErrors = false;
                    MethodSymbol resultMember = memberResolutionResult.Member;

                    if (resultMember == constructor)
                    {
                        Debug.Assert(initializerType.IsErrorType() ||
                            (initializerArgumentListOpt != null && initializerArgumentListOpt.Parent.Kind == SyntaxKind.ThisConstructorInitializer));
                        diagnostics.Add(ErrorCode.ERR_RecursiveConstructorCall,
                            initializerArgumentListOpt.Parent.Kind == SyntaxKind.BaseClassWithArguments ? 
                                initializerArgumentListOpt.GetLocation() : 
                                ((ConstructorInitializerSyntax)initializerArgumentListOpt.Parent).ThisOrBaseKeyword.GetLocation(), 
                            constructor);
                        hasErrors = true; //will prevent recursive constructor from being emitted
                    }
                    else if (resultMember.HasUnsafeParameter())
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

                    return new BoundCall(
                        nonNullSyntax,
                        receiver,
                        resultMember,
                        analyzedArguments.Arguments.ToImmutable(),
                        analyzedArguments.GetNames(),
                        analyzedArguments.RefKinds.ToImmutableOrNull(),
                        isDelegateCall: false,
                        expanded: memberResolutionResult.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm,
                        invokedAsExtensionMethod: false,
                        argsToParamsOpt: memberResolutionResult.Result.ArgsToParamsOpt,
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
                        typeArguments: ImmutableArray<TypeSymbol>.Empty,
                        analyzedArguments: analyzedArguments,
                        invokedAsExtensionMethod: false,
                        isDelegate: false);
                    result.WasCompilerGenerated = initializerArgumentListOpt == null;
                    return result;
                }
            }
            finally
            {
                analyzedArguments.Free();
            }
        }

        protected BoundExpression BindObjectCreationExpression(ObjectCreationExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var type = BindType(node.Type, diagnostics);

            BoundExpression boundInitializerOpt = node.Initializer == null ?
                null :
                BindInitializerExpressionOrValue(
                    syntax: node.Initializer,
                    type: type,
                    typeSyntax: node.Type,
                    diagnostics: diagnostics);

            switch (type.TypeKind)
            {
                case TypeKind.Struct:
                case TypeKind.Class:
                case TypeKind.Enum:
                case TypeKind.Error:
                    return BindClassCreationExpression(node, (NamedTypeSymbol)type, GetName(node.Type), boundInitializerOpt, diagnostics);

                case TypeKind.Delegate:
                    return BindDelegateCreationExpression(node, (NamedTypeSymbol)type, diagnostics);

                case TypeKind.Interface:
                    return BindInterfaceCreationExpression(node, (NamedTypeSymbol)type, boundInitializerOpt, diagnostics);

                case TypeKind.TypeParameter:
                    return BindTypeParameterCreationExpression(node, (TypeParameterSymbol)type, boundInitializerOpt, diagnostics);

                case TypeKind.Submission:
                    // script class is synthesized and should not be used as a type of a new expression:
                    throw ExceptionUtilities.UnexpectedValue(type.TypeKind);

                case TypeKind.DynamicType:
                    // we didn't find any type called "dynamic" so we are using the builtin dynamic type, which has no constructors:
                    Error(diagnostics, ErrorCode.ERR_NoConstructors, node.Type, type);
                    return BadExpression(node, LookupResultKind.NotCreatable);

                case TypeKind.PointerType:
                    type = new ExtendedErrorTypeSymbol(type, LookupResultKind.NotCreatable,
                        diagnostics.Add(ErrorCode.ERR_UnsafeTypeInObjectCreation, node.Location, type));
                    goto case TypeKind.Class;

                default:
                    throw ExceptionUtilities.UnexpectedValue(type.TypeKind);
            }
        }

        private BoundExpression BindDelegateCreationExpression(ObjectCreationExpressionSyntax node, NamedTypeSymbol type, DiagnosticBag diagnostics)
        {
            // Get the bound arguments and the argument names.
            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();

            try
            {
                BindArgumentsAndNames(node.ArgumentList, diagnostics, analyzedArguments);

                bool hasErrors = false;
                if (analyzedArguments.HasErrors)
                {
                    hasErrors = true;
                }
                else if (node.ArgumentList == null || analyzedArguments.Arguments.Count == 0)
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

                if (node.Initializer != null)
                {
                    Error(diagnostics, ErrorCode.ERR_ObjectOrCollectionInitializerWithDelegateCreation, node);
                    hasErrors = true;
                }

                BoundExpression argument = analyzedArguments.Arguments.Count >= 1 ? analyzedArguments.Arguments[0] : null;

                if (hasErrors)
                {
                    // skip the rest of this binding
                }

                // There are four cases for a delegate creation expression (7.6.10.5):
                // 1. A method group
                else if (argument.Kind == BoundKind.MethodGroup)
                {
                    Conversion conversion;
                    BoundMethodGroup methodGroup = (BoundMethodGroup)argument;
                    if (!MethodGroupConversionDoesNotExistOrHasErrors(methodGroup, type, node.Location, diagnostics, out conversion))
                    {
                        methodGroup = FixMethodGroupWithTypeOrValue(methodGroup, conversion, diagnostics);
                        return new BoundDelegateCreationExpression(node, methodGroup, conversion.Method, conversion.IsExtensionMethod, type);
                    }
                }

                // 2. An anonymous function is treated as a conversion from the anonymous function to the delegate type.
                else if (argument is UnboundLambda)
                {
                    UnboundLambda unboundLambda = (UnboundLambda)argument;
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    var conversion = this.Conversions.ClassifyConversionFromExpression(unboundLambda, type, ref useSiteDiagnostics);
                    diagnostics.Add(node, useSiteDiagnostics);
                    // Attempting to make the conversion caches the diagnostics and the bound state inside
                    // the unbound lambda. Fetch the result from the cache.
                    BoundLambda boundLambda = unboundLambda.Bind(type);

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

                    // Just stuff the bound lambda into the delegate creation expression. When we lower the lambda to
                    // its method form we will rewrite this expression to refer to the method.

                    return new BoundDelegateCreationExpression(node, boundLambda, methodOpt: null, isExtensionMethod: false, type: type, hasErrors: !conversion.IsImplicit);
                }

                else if ((object)argument.Type == null)
                {
                    diagnostics.Add(ErrorCode.ERR_MethodNameExpected, argument.Syntax.Location);
                }

                // 3. A value of the compile-time type dynamic (which is dynamically case 4), or
                else if (argument.HasDynamicType())
                {
                    return new BoundDelegateCreationExpression(node, argument, methodOpt: null, isExtensionMethod: false, type: type);
                }

                // 4. A delegate type.
                else if (argument.Type.TypeKind == TypeKind.Delegate)
                {
                    var sourceDelegate = argument.Type as NamedTypeSymbol;
                    MethodGroup methodGroup = MethodGroup.GetInstance();
                    try
                    {
                        if (ReportDelegateInvokeUseSiteDiagnostic(diagnostics, argument.Type, node: node))
                        {
                            // We want failed "new" expression to use the constructors as their symbols.
                            return new BoundBadExpression(node, LookupResultKind.NotInvocable, StaticCast<Symbol>.From(type.InstanceConstructors), ImmutableArray.Create<BoundNode>(argument), type);
                        }

                        methodGroup.PopulateWithSingleMethod(argument, sourceDelegate.DelegateInvokeMethod);

                        HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                        Conversion conv = Conversions.MethodGroupConversion(argument.Syntax, methodGroup, type, ref useSiteDiagnostics);
                        diagnostics.Add(node, useSiteDiagnostics);
                        if (!conv.Exists)
                        {
                            // No overload for '{0}' matches delegate '{1}'
                            diagnostics.Add(ErrorCode.ERR_MethDelegateMismatch, node.Location,
                                sourceDelegate.Name, // duplicate questionable Dev10 diagnostic
                                type);
                        }
                        else
                        {
                            Debug.Assert(!conv.IsExtensionMethod);
                            Debug.Assert(conv.IsValid); // i.e. if it exists, then it is valid.

                            if (!this.MethodGroupConversionHasErrors(argument.Syntax, conv, argument, conv.IsExtensionMethod, type, diagnostics))
                            {
                            // we do not place the "Invoke" method in the node, indicating that it did not appear in source.
                            return new BoundDelegateCreationExpression(node, argument, methodOpt: null, isExtensionMethod: false, type: type);
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
                ArrayBuilder<BoundNode> childNodes = ArrayBuilder<BoundNode>.GetInstance();
                childNodes.AddRange(BuildArgumentsForErrorRecovery(analyzedArguments));
                return new BoundBadExpression(node, LookupResultKind.OverloadResolutionFailure, StaticCast<Symbol>.From(type.InstanceConstructors), childNodes.ToImmutableAndFree(), type);
            }
            finally
            {
                analyzedArguments.Free();
            }
        }

        private BoundExpression BindClassCreationExpression(ObjectCreationExpressionSyntax node, NamedTypeSymbol type, string typeName, BoundExpression boundInitializerOpt, DiagnosticBag diagnostics)
        {
            // Get the bound arguments and the argument names.
            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();
            try
            {
                // new C(__arglist()) is legal
                BindArgumentsAndNames(node.ArgumentList, diagnostics, analyzedArguments, allowArglist: true);

                // No point in performing overload resolution if the type is static.  Just return the a bad expression containing
                // the arguments.
                if (type.IsStatic)
                {
                    diagnostics.Add(ErrorCode.ERR_InstantiatingStaticClass, node.Location, type);

                    var children = ArrayBuilder<BoundNode>.GetInstance();
                    children.AddRange(BuildArgumentsForErrorRecovery(analyzedArguments));
                    if (boundInitializerOpt != null)
                    {
                        children.Add(boundInitializerOpt);
                    }

                    return new BoundBadExpression(node, LookupResultKind.NotCreatable, ImmutableArray.Create<Symbol>(type), children.ToImmutableAndFree(), type);
                }

                return BindClassCreationExpression(node, typeName, node.Type, type, analyzedArguments, diagnostics, boundInitializerOpt);
            }
            finally
            {
                analyzedArguments.Free();
            }
        }

        private BoundExpression BindInitializerExpressionOrValue(
            ExpressionSyntax syntax,
            TypeSymbol type,
            CSharpSyntaxNode typeSyntax,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(syntax != null);
            Debug.Assert((object)type != null);

            switch (syntax.Kind)
            {
                case SyntaxKind.ObjectInitializerExpression:
                    {
                        var implicitReceiver = new BoundImplicitReceiver(typeSyntax, type);
                        return BindObjectInitializerExpression((InitializerExpressionSyntax)syntax, type, diagnostics, implicitReceiver);
                    }

                case SyntaxKind.CollectionInitializerExpression:
                    {
                        var implicitReceiver = new BoundImplicitReceiver(typeSyntax, type);
                        return BindCollectionInitializerExpression((InitializerExpressionSyntax)syntax, type, diagnostics, implicitReceiver);
                    }

                default:
                    return BindValue(syntax, diagnostics, BindValueKind.RValue);
            }
        }

        private BoundObjectInitializerExpression BindObjectInitializerExpression(
            InitializerExpressionSyntax initializerSyntax,
            TypeSymbol initializerType,
            DiagnosticBag diagnostics,
            BoundImplicitReceiver implicitReceiver)
        {
            // SPEC:    7.6.10.2 Object initializers
            //
            // SPEC:    An object initializer consists of a sequence of member initializers, enclosed by { and } tokens and separated by commas.
            // SPEC:    Each member initializer must name an accessible field or property of the object being initialized, followed by an equals sign and
            // SPEC:    an expression or an object initializer or collection initializer.

            Debug.Assert(initializerSyntax.Kind == SyntaxKind.ObjectInitializerExpression);
            Debug.Assert((object)initializerType != null);

            var initializerBuilder = ArrayBuilder<BoundExpression>.GetInstance();

            // Member name map to report duplicate assignments to a field/property.
            var memberNameMap = new HashSet<string>();

            // We use a location specific binder for binding object initializer field/property access to generate object initializer specific diagnostics:
            //  1) CS1914 (ERR_StaticMemberInObjectInitializer)
            //  2) CS1917 (ERR_ReadonlyValueTypeInObjectInitializer)
            //  3) CS1918 (ERR_ValueTypePropertyInObjectInitializer)
            // Note that this is only used for the LHS of the assignment - these diagnostics do not apply on the RHS.
            // For this reason, we will actually need two binders: this and this.WithAdditionalFlags.
            var objectInitializerMemberBinder = this.WithAdditionalFlags(BinderFlags.ObjectInitializerMember);

            foreach (var memberInitializer in initializerSyntax.Expressions)
            {
                BoundExpression boundMemberInitializer = BindObjectInitializerMemberAssignment(
                    memberInitializer, initializerType, objectInitializerMemberBinder, diagnostics, implicitReceiver);

                initializerBuilder.Add(boundMemberInitializer);

                ReportDuplicateObjectMemberInitializers(boundMemberInitializer, memberNameMap, diagnostics);
            }

            return new BoundObjectInitializerExpression(initializerSyntax, initializerBuilder.ToImmutableAndFree(), initializerType);
        }

        private BoundExpression BindObjectInitializerMemberAssignment(
            ExpressionSyntax memberInitializer,
            TypeSymbol initializerType,
            Binder objectInitializerMemberBinder,
            DiagnosticBag diagnostics,
            BoundImplicitReceiver implicitReceiver)
        {
            // SPEC:    A member initializer that specifies an expression after the equals sign is processed in the same way as an assignment (spec 7.17.1) to the field or property.

            if (memberInitializer.Kind == SyntaxKind.SimpleAssignmentExpression)
            {
                var namedAssignment = (BinaryExpressionSyntax)memberInitializer;

                if (namedAssignment.Left.Kind == SyntaxKind.IdentifierName)
                {
                    // Bind member initializer identifier, i.e. left part of assignment

                    BoundExpression boundLeft;
                    if (initializerType.IsDynamic())
                    {
                        // D = { ..., <identifier> = <expr>, ... }, where D : dynamic
                        var memberName = ((IdentifierNameSyntax)namedAssignment.Left).Identifier.Text;
                        boundLeft = new BoundDynamicObjectInitializerMember(namedAssignment, memberName, initializerType, hasErrors: false);
                    }
                    else
                    {
                        // We use a location specific binder for binding object initializer field/property access to generate object initializer specific diagnostics:
                        //  1) CS1914 (ERR_StaticMemberInObjectInitializer)
                        //  2) CS1917 (ERR_ReadonlyValueTypeInObjectInitializer)
                        //  3) CS1918 (ERR_ValueTypePropertyInObjectInitializer)
                        // See comments in BindObjectInitializerExpression for more details.

                        Debug.Assert(objectInitializerMemberBinder != null);
                        Debug.Assert(objectInitializerMemberBinder.Flags.Includes(BinderFlags.ObjectInitializerMember));

                        boundLeft = objectInitializerMemberBinder.BindObjectInitializerMember(namedAssignment, implicitReceiver, diagnostics);
                    }

                    Debug.Assert((object)boundLeft.Type != null);

                    // Bind member initializer value, i.e. right part of assignment
                    BoundExpression boundRight = BindInitializerExpressionOrValue(
                        syntax: namedAssignment.Right,
                        type: boundLeft.Type,
                        typeSyntax: boundLeft.Syntax,
                        diagnostics: diagnostics);

                    // Bind member initializer assignment expression
                    return BindAssignment(namedAssignment, boundLeft, boundRight, diagnostics);
                }
            }

            var boundExpression = BindValue(memberInitializer, diagnostics, BindValueKind.RValue);
            Error(diagnostics, ErrorCode.ERR_InvalidInitializerElementInitializer, memberInitializer);
            return ToBadExpression(boundExpression, LookupResultKind.NotAValue);
        }

        // returns BadBoundExpression or BoundObjectInitializerMember
        private BoundExpression BindObjectInitializerMember(
            BinaryExpressionSyntax namedAssignment,
            BoundImplicitReceiver implicitReceiver,
            DiagnosticBag diagnostics)
        {
            var memberNameSyntax = (IdentifierNameSyntax)namedAssignment.Left;

            // SPEC:    Each member initializer must name an accessible field or property of the object being initialized, followed by an equals sign and
            // SPEC:    an expression or an object initializer or collection initializer.
            // SPEC:    A member initializer that specifies an expression after the equals sign is processed in the same way as an assignment (7.17.1) to the field or property.

            // SPEC VIOLATION:  Native compiler also allows initialization of fieldlike events in object initializers, so we allow it as well.

            BoundExpression boundMember = BindInstanceMemberAccess(
                node: memberNameSyntax,
                right: memberNameSyntax,
                boundLeft: implicitReceiver,
                rightName: memberNameSyntax.Identifier.ValueText,
                rightArity: 0,
                typeArgumentsSyntax: default(SeparatedSyntaxList<TypeSyntax>),
                typeArguments: default(ImmutableArray<TypeSymbol>),
                invoked: false,
                diagnostics: diagnostics);

            LookupResultKind resultKind = boundMember.ResultKind;
            bool hasErrors = boundMember.HasAnyErrors || implicitReceiver.HasAnyErrors;

            if (boundMember.Kind == BoundKind.PropertyGroup)
            {
                boundMember = BindIndexedPropertyAccess((BoundPropertyGroup)boundMember, mustHaveAllOptionalParameters: true, diagnostics: diagnostics);
                if (boundMember.HasAnyErrors)
                {
                    hasErrors = true;
                }
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
            SyntaxKind rhsKind = namedAssignment.Right.Kind;
            bool isRhsNestedInitializer = rhsKind == SyntaxKind.ObjectInitializerExpression || rhsKind == SyntaxKind.CollectionInitializerExpression;
            BindValueKind valueKind = isRhsNestedInitializer ? BindValueKind.RValue : BindValueKind.Assignment;

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
                                Error(diagnostics, ErrorCode.ERR_ReadonlyValueTypeInObjectInitializer, memberNameSyntax, fieldSymbol, fieldSymbol.Type);
                                hasErrors = true;
                            }

                            resultKind = LookupResultKind.NotAValue;
                        }

                        break;
                    }

                case BoundKind.PropertyAccess:
                case BoundKind.IndexerAccess:
                    {
                        var propertySymbol = (boundMemberKind == BoundKind.PropertyAccess) ?
                            ((BoundPropertyAccess)boundMember).PropertySymbol :
                            ((BoundIndexerAccess)boundMember).Indexer;
                        Debug.Assert(!propertySymbol.IsIndexer());

                        if (isRhsNestedInitializer && propertySymbol.Type.IsValueType)
                        {
                            if (!hasErrors)
                            {
                                // TODO: distinct error code for collection initializers?  (Dev11 doesn't have one.)
                                Error(diagnostics, ErrorCode.ERR_ValueTypePropertyInObjectInitializer, memberNameSyntax, propertySymbol, propertySymbol.Type);
                                hasErrors = true;
                            }

                            resultKind = LookupResultKind.NotAValue;
                        }

                        // UNDONE:  For BoundKind.PropertyAccess:
                        // UNDONE:  If we have a NoPIA object, we need to populate the local type with the 
                        // UNDONE:  property we bound, and bind to the local property instead.
                        //      PropWithType pwt = (PropWithType)swt;
                        //      ConsumePropertyWithNoPIA(GetSymbolLoader(), object, pwt);                

                        break;
                    }

                case BoundKind.EventAccess:
                    break;

                default:
                    return BadObjectInitializerMemberAccess(boundMember, implicitReceiver, memberNameSyntax, diagnostics, valueKind, hasErrors);
            }

            if (!hasErrors)
            {
                // CheckValueKind to generate possible diagnostics for invalid initializers non-viable member lookup result:
                //      1) CS0154 (ERR_PropertyLacksGet)
                //      2) CS0200 (ERR_AssgReadonlyProp)

                Debug.Assert(Flags.Includes(CSharp.BinderFlags.ObjectInitializerMember));
                if (!CheckValueKind(boundMember, valueKind, diagnostics))
                {
                    hasErrors = true;
                    resultKind = isRhsNestedInitializer ? LookupResultKind.NotAValue : LookupResultKind.NotAVariable;
                }
            }

            return new BoundObjectInitializerMember(memberNameSyntax, boundMember.ExpressionSymbol, resultKind, boundMember.Type, hasErrors);
        }

        private BoundExpression BadObjectInitializerMemberAccess(
            BoundExpression boundMember,
            BoundImplicitReceiver implicitReceiver,
            IdentifierNameSyntax memberNameSyntax,
            DiagnosticBag diagnostics,
            BindValueKind valueKind,
            bool suppressErrors)
        {
            if (!suppressErrors)
            {
                switch (boundMember.ResultKind)
                {
                    case LookupResultKind.Empty:
                        Error(diagnostics, ErrorCode.ERR_NoSuchMember, memberNameSyntax, implicitReceiver.Type, memberNameSyntax.Identifier.ValueText);
                        break;

                    case LookupResultKind.Inaccessible:
                        boundMember = CheckValue(boundMember, valueKind, diagnostics);
                        Debug.Assert(boundMember.HasAnyErrors);
                        break;

                    default:
                        Error(diagnostics, ErrorCode.ERR_MemberCannotBeInitialized, memberNameSyntax, memberNameSyntax.Identifier.ValueText);
                        break;
                }
            }

            return ToBadExpression(boundMember, (valueKind == BindValueKind.RValue) ? LookupResultKind.NotAValue : LookupResultKind.NotAVariable);
        }

        private static void ReportDuplicateObjectMemberInitializers(BoundExpression boundMemberInitializer, HashSet<string> memberNameMap, DiagnosticBag diagnostics)
        {
            Debug.Assert(memberNameMap != null);

            // SPEC:    It is an error for an object initializer to include more than one member initializer for the same field or property.

            if (!boundMemberInitializer.HasAnyErrors)
            {
                // SPEC:    A member initializer that specifies an expression after the equals sign is processed in the same way as an assignment (7.17.1) to the field or property.

                var memberInitializerSyntax = boundMemberInitializer.Syntax;

                Debug.Assert(memberInitializerSyntax.Kind == SyntaxKind.SimpleAssignmentExpression);
                var namedAssignment = (BinaryExpressionSyntax)memberInitializerSyntax;

                Debug.Assert(namedAssignment.Left.Kind == SyntaxKind.IdentifierName);
                var memberNameSyntax = (IdentifierNameSyntax)namedAssignment.Left;
                var memberName = memberNameSyntax.Identifier.ValueText;

                if (!memberNameMap.Add(memberName))
                {
                    Error(diagnostics, ErrorCode.ERR_MemberAlreadyInitialized, memberNameSyntax, memberName);
                }
            }
        }

        private BoundCollectionInitializerExpression BindCollectionInitializerExpression(
            InitializerExpressionSyntax initializerSyntax,
            TypeSymbol initializerType,
            DiagnosticBag diagnostics,
            BoundImplicitReceiver implicitReceiver)
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

            Debug.Assert(initializerSyntax.Kind == SyntaxKind.CollectionInitializerExpression);
            Debug.Assert(initializerSyntax.Expressions.Any());
            Debug.Assert((object)initializerType != null);

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

            return new BoundCollectionInitializerExpression(initializerSyntax, initializerBuilder.ToImmutableAndFree(), initializerType);
        }

        private bool CollectionInitializerTypeImplementsIEnumerable(TypeSymbol initializerType, CSharpSyntaxNode node, DiagnosticBag diagnostics)
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
                TypeSymbol collectionsIEnumerableType = this.GetSpecialType(SpecialType.System_Collections_IEnumerable, diagnostics, node);

                // NOTE:    Ideally, to check if the initializer type implements System.Collections.IEnumerable we can walk through
                // NOTE:    its implemented interfaces. However the native compiler checks to see if there is conversion from initializer
                // NOTE:    type to the predefined System.Collections.IEnumerable type, so we do the same.

                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                var result = Conversions.ClassifyImplicitConversion(initializerType, collectionsIEnumerableType, ref useSiteDiagnostics).IsValid;
                diagnostics.Add(node, useSiteDiagnostics);
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
            DiagnosticBag diagnostics,
            BoundImplicitReceiver implicitReceiver)
        {
            // SPEC:    Each element initializer specifies an element to be added to the collection object being initialized, and consists of
            // SPEC:    a list of expressions enclosed by { and } tokens and separated by commas.
            // SPEC:    A single-expression element initializer can be written without braces, but cannot then be an assignment expression,
            // SPEC:    to avoid ambiguity with member initializers. The non-assignment-expression production is defined in 7.18.

            if (elementInitializer.Kind == SyntaxKind.ComplexElementInitializerExpression)
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
                if (SyntaxFacts.IsAssignmentExpression(elementInitializer.Kind))
                {
                    Error(diagnostics, ErrorCode.ERR_InvalidInitializerElementInitializer, elementInitializer);
                }

                var boundElementInitializer = BindInitializerExpressionOrValue(elementInitializer, initializerType, implicitReceiver.Syntax, diagnostics);

                return BindCollectionInitializerElementAddMethod(
                    elementInitializer,
                    ImmutableArray.Create(boundElementInitializer),
                    hasEnumerableInitializerType,
                    collectionInitializerAddMethodBinder,
                    diagnostics,
                    implicitReceiver);
            }
        }

        private BoundExpression BindComplexElementInitializerExpression(
            InitializerExpressionSyntax elementInitializer,
            DiagnosticBag diagnostics,
            bool hasEnumerableInitializerType,
            Binder collectionInitializerAddMethodBinder = null,
            BoundImplicitReceiver implicitReceiver = null)
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

        private BoundExpression BindUnexpectedComplexElementInitializer(InitializerExpressionSyntax node, DiagnosticBag diagnostics)
        {
            Debug.Assert(node.Kind == SyntaxKind.ComplexElementInitializerExpression);

            return BindComplexElementInitializerExpression(node, diagnostics, hasEnumerableInitializerType: false);
        }

        private BoundExpression BindCollectionInitializerElementAddMethod(
            ExpressionSyntax elementInitializer,
            ImmutableArray<BoundExpression> boundElementInitializerExpressions,
            bool hasEnumerableInitializerType,
            Binder collectionInitializerAddMethodBinder,
            DiagnosticBag diagnostics,
            BoundImplicitReceiver implicitReceiver)
        {
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

            Debug.Assert(collectionInitializerAddMethodBinder != null);
            Debug.Assert(collectionInitializerAddMethodBinder.Flags.Includes(BinderFlags.CollectionInitializerAddMethod));
            Debug.Assert(implicitReceiver != null);
            Debug.Assert((object)implicitReceiver.Type != null);

            if (implicitReceiver.Type.IsDynamic())
            {
                var hasErrors = ReportBadDynamicArguments(elementInitializer, boundElementInitializerExpressions, diagnostics, queryClause: null);

                return new BoundDynamicCollectionElementInitializer(
                    elementInitializer,
                    arguments: boundElementInitializerExpressions,
                    applicableMethods: ImmutableArray<MethodSymbol>.Empty,
                    type: GetSpecialType(SpecialType.System_Void, diagnostics, elementInitializer),
                    hasErrors: hasErrors);
            }

            // Receiver is early bound, find method Add and invoke it (may still be a dynamic invocation):

            var addMethodInvocation = collectionInitializerAddMethodBinder.MakeInvocationExpression(
                elementInitializer,
                implicitReceiver,
                methodName: WellKnownMemberNames.CollectionInitializerAddMethodName,
                args: boundElementInitializerExpressions,
                diagnostics: diagnostics);

            if (addMethodInvocation.Kind == BoundKind.DynamicInvocation)
            {
                var dynamicInvocation = (BoundDynamicInvocation)addMethodInvocation;
                return new BoundDynamicCollectionElementInitializer(
                    elementInitializer,
                    dynamicInvocation.Arguments,
                    dynamicInvocation.ApplicableMethods,
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
                    boundCall.Expanded,
                    boundCall.ArgsToParamsOpt,
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

        internal ImmutableArray<MethodSymbol> FilterInaccessibleConstructors(ImmutableArray<MethodSymbol> constructors, bool allowProtectedConstructorsOfBaseType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            ArrayBuilder<MethodSymbol> builder = null;

            for (int i = 0; i < constructors.Length; i++)
            {
                MethodSymbol constructor = constructors[i];

                if (!IsConstructorAccessible(constructor, ref useSiteDiagnostics, allowProtectedConstructorsOfBaseType))
                {
                    if (builder == null)
                    {
                        builder = ArrayBuilder<MethodSymbol>.GetInstance();
                        builder.AddRange(constructors, i);
                    }
                }
                else if (builder != null)
                {
                    builder.Add(constructor);
                }
            }

            return builder == null ? constructors : builder.ToImmutableAndFree();
        }

        private bool IsConstructorAccessible(MethodSymbol constructor, ref HashSet<DiagnosticInfo> useSiteDiagnostics, bool allowProtectedConstructorsOfBaseType = false)
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
                    this.IsAccessible(constructor, ref useSiteDiagnostics, null) :
                    this.IsSymbolAccessibleConditional(constructor, containingType, ref useSiteDiagnostics, constructor.ContainingType);
            }
            else
            {
                Debug.Assert((object)this.Compilation.Assembly != null);
                return this.IsSymbolAccessibleConditional(constructor, this.Compilation.Assembly, ref useSiteDiagnostics);
            }
        }

        protected BoundExpression BindClassCreationExpression(
            CSharpSyntaxNode node,
            string typeName,
            CSharpSyntaxNode typeNode,
            NamedTypeSymbol type,
            AnalyzedArguments analyzedArguments,
            DiagnosticBag diagnostics,
            BoundExpression boundInitializerOpt = null)
        {
            BoundExpression result = null;
            bool hasErrors = type.IsErrorType();
            if (type.IsAbstract)
            {
                // Report error for new of abstract type.
                diagnostics.Add(ErrorCode.ERR_NoNewAbstract, node.Location, type);
                hasErrors = true;
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            // If we have a dynamic argument then do overload resolution to see if there are one or more
            // applicable candidates. If there are, then this is a dynamic object creation; we'll work out
            // which ctor to call at runtime. If we have a dynamic argument but no applicable candidates
            // then we do the analysis again for error reporting purposes.

            if (analyzedArguments.HasDynamicArgument)
            {
                OverloadResolutionResult<MethodSymbol> overloadResolutionResult = OverloadResolutionResult<MethodSymbol>.GetInstance();
                this.OverloadResolution.ObjectCreationOverloadResolution(GetAccessibleConstructorsForOverloadResolution(type, ref useSiteDiagnostics), analyzedArguments, overloadResolutionResult, ref useSiteDiagnostics);
                diagnostics.Add(node, useSiteDiagnostics);
                useSiteDiagnostics = null;

                if (overloadResolutionResult.HasAnyApplicableMember)
                {
                    var argArray = BuildArgumentsForDynamicInvocation(analyzedArguments, diagnostics);

                    hasErrors &= ReportBadDynamicArguments(node, argArray, diagnostics, queryClause: null);

                    result = new BoundDynamicObjectCreationExpression(
                        node,
                        argArray,
                        analyzedArguments.GetNames(),
                        analyzedArguments.RefKinds.ToImmutableOrNull(),
                        boundInitializerOpt,
                        overloadResolutionResult.GetAllApplicableMembers(),
                        type,
                        hasErrors);
                }

                overloadResolutionResult.Free();
                if (result != null)
                {
                    return result;
                }
            }

            MemberResolutionResult<MethodSymbol> memberResolutionResult;
            ImmutableArray<MethodSymbol> candidateConstructors;

            if (TryPerformConstructorOverloadResolution(
                type,
                analyzedArguments,
                typeName,
                typeNode.Location,
                hasErrors, //don't cascade in these cases
                diagnostics,
                out memberResolutionResult,
                out candidateConstructors,
                allowProtectedConstructorsOfBaseType: false))
            {
                var method = memberResolutionResult.Member;

                bool hasError = false;

                // What if some of the arguments are implicit?  Dev10 reports unsafe errors
                // if the implied argument would have an unsafe type.  We need to check
                // the parameters explicitly, since there won't be bound nodes for the implied
                // arguments until lowering.
                if (method.HasUnsafeParameter())
                {
                    // Don't worry about double reporting (i.e. for both the argument and the parameter)
                    // because only one unsafe diagnostic is allowed per scope - the others are suppressed.
                    hasError = ReportUnsafeIfNotAllowed(node, diagnostics) || hasError;
                }

                ReportDiagnosticsIfObsolete(diagnostics, method, node, hasBaseReceiver: false);
                // NOTE: Use-site diagnostics were reported during overload resolution.

                ConstantValue constantValueOpt = (boundInitializerOpt == null && method.IsParameterlessValueTypeConstructor(requireSynthesized: true)) ?
                    FoldParameterlessValueTypeConstructor(type) :
                    null;

                result = new BoundObjectCreationExpression(
                    node,
                    method,
                    candidateConstructors,
                    analyzedArguments.Arguments.ToImmutable(),
                    analyzedArguments.GetNames(),
                    analyzedArguments.RefKinds.ToImmutableOrNull(),
                    memberResolutionResult.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm,
                    memberResolutionResult.Result.ArgsToParamsOpt,
                    constantValueOpt,
                    boundInitializerOpt,
                    type,
                    hasError);

                // CONSIDER: Add ResultKind field to BoundObjectCreationExpression to avoid wrapping result with BoundBadExpression.
                if (type.IsAbstract)
                {
                    result = BadExpression(node, LookupResultKind.NotCreatable, result);
                }

                return result;
            }

            LookupResultKind resultKind;

            if (type.IsAbstract)
            {
                resultKind = LookupResultKind.NotCreatable;
            }
            else if (memberResolutionResult.IsValid && !IsConstructorAccessible(memberResolutionResult.Member, ref useSiteDiagnostics))
            {
                resultKind = LookupResultKind.Inaccessible;
            }
            else
            {
                resultKind = LookupResultKind.OverloadResolutionFailure;
            }

            diagnostics.Add(node, useSiteDiagnostics);

            ArrayBuilder<Symbol> symbols = ArrayBuilder<Symbol>.GetInstance();
            symbols.AddRange(candidateConstructors);

            // NOTE: The use site diagnostics of the candidate constructors have already been reported (in PerformConstructorOverloadResolution).

            ArrayBuilder<BoundNode> childNodes = ArrayBuilder<BoundNode>.GetInstance();

            if (candidateConstructors.Length == 1)
            {
                ImmutableArray<BoundExpression> args = BuildArgumentsForErrorRecovery(analyzedArguments, candidateConstructors[0].Parameters);
                childNodes.AddRange(args);
            }
            else
            {
                childNodes.AddRange(BuildArgumentsForErrorRecovery(analyzedArguments));
            }

            if (boundInitializerOpt != null)
            {
                childNodes.Add(boundInitializerOpt);
            }

            return new BoundBadExpression(node, resultKind, symbols.ToImmutableAndFree(), childNodes.ToImmutableAndFree(), type);
        }

        private BoundExpression BindInterfaceCreationExpression(ObjectCreationExpressionSyntax node, NamedTypeSymbol type, BoundExpression boundInitializerOpt, DiagnosticBag diagnostics)
        {
            Debug.Assert((object)type != null);

            // COM interfaces which have ComImportAttribute and CoClassAttribute can be instantiated with "new". 
            // CoClassAttribute contains the type information of the original CoClass for the interface.
            // We replace the interface creation with CoClass object creation for this case.

            // NOTE: We don't attempt binding interface creation to CoClass creation if we are within an attribute argument.
            // NOTE: This is done to prevent a cycle in an error scenario where we have a "new InterfaceType" expression in an attribute argument.
            // NOTE: Accessing IsComImport/ComImportCoClass properties on given type symbol would attempt ForceCompeteAttributes, which would again try binding all attributes on the symbol.
            // NOTE: causing infinite recursion. We avoid this cycle by checking if we are within in context of an Attribute argument.
            if (!this.InAttributeArgument && type.IsComImport)
            {
                NamedTypeSymbol coClassType = type.ComImportCoClass;
                if ((object)coClassType != null)
                {
                    return BindComImportCoClassCreationExpression(node, type, coClassType, boundInitializerOpt, diagnostics);
                }
            }

            // interfaces can't be instantiated in C#
            diagnostics.Add(ErrorCode.ERR_NoNewAbstract, node.Location, type);
            return BindBadInterfaceCreationExpression(node, type, diagnostics);
        }

        private BoundExpression BindBadInterfaceCreationExpression(ObjectCreationExpressionSyntax node, NamedTypeSymbol type, DiagnosticBag diagnostics)
        {
            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();

            BindArgumentsAndNames(node.ArgumentList, diagnostics, analyzedArguments);
            ImmutableArray<BoundNode> childNodes = BuildArgumentsForErrorRecovery(analyzedArguments).Cast<BoundExpression, BoundNode>();

            BoundExpression result = new BoundBadExpression(node, LookupResultKind.NotCreatable, ImmutableArray.Create<Symbol>(type), childNodes, type);
            analyzedArguments.Free();
            return result;
        }

        private BoundExpression BindComImportCoClassCreationExpression(ObjectCreationExpressionSyntax node, NamedTypeSymbol interfaceType, NamedTypeSymbol coClassType, BoundExpression boundInitializerOpt, DiagnosticBag diagnostics)
        {
            Debug.Assert((object)interfaceType != null);
            Debug.Assert(interfaceType.IsInterfaceType());
            Debug.Assert((object)coClassType != null);
            Debug.Assert(interfaceType.ComImportCoClass == coClassType);
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
                    return BindNoPiaObjectCreationExpression(node, interfaceType, coClassType, boundInitializerOpt, diagnostics);
                }

                var classCreation = BindClassCreationExpression(node, coClassType, coClassType.Name, boundInitializerOpt, diagnostics);
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                Conversion conversion = this.Conversions.ClassifyConversionForCast(classCreation, interfaceType, ref useSiteDiagnostics);
                diagnostics.Add(node, useSiteDiagnostics);
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
                                               creation.ArgumentRefKindsOpt, creation.Expanded, creation.ArgsToParamsOpt, creation.ConstantValueOpt,
                                               creation.InitializerExpressionOpt, interfaceType);

                    case BoundKind.BadExpression:
                        var bad = (BoundBadExpression)classCreation;
                        return bad.Update(bad.ResultKind, bad.Symbols, bad.ChildBoundNodes, interfaceType);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(classCreation.Kind);
                }
            }

            return BindBadInterfaceCreationExpression(node, interfaceType, diagnostics);
        }

        private BoundExpression BindNoPiaObjectCreationExpression(
            ObjectCreationExpressionSyntax node,
            NamedTypeSymbol interfaceType,
            NamedTypeSymbol coClassType,
            BoundExpression boundInitializerOpt,
            DiagnosticBag diagnostics)
        {
            string guidString;
            if (!coClassType.GetGuidString(out guidString))
            {
                // At this point, VB reports ERRID_NoPIAAttributeMissing2 if guid isn't there.
                // C# doesn't complain and instead uses zero guid.
                guidString = System.Guid.Empty.ToString("D");
            }

            var creation = new BoundNoPiaObjectCreationExpression(node, guidString, boundInitializerOpt, interfaceType);

            // Get the bound arguments and the argument names, it is an error if any are present.
            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();
            try
            {
                BindArgumentsAndNames(node.ArgumentList, diagnostics, analyzedArguments, allowArglist: false);

                if (analyzedArguments.Arguments.Count > 0)
                {
                    diagnostics.Add(ErrorCode.ERR_BadCtorArgCount, node.ArgumentList.Location, interfaceType, analyzedArguments.Arguments.Count);

                    var children = ArrayBuilder<BoundNode>.GetInstance();
                    children.AddRange(BuildArgumentsForErrorRecovery(analyzedArguments));
                    children.Add(creation);

                    return new BoundBadExpression(node, LookupResultKind.OverloadResolutionFailure, ImmutableArray<Symbol>.Empty, children.ToImmutableAndFree(), creation.Type);
                }
            }
            finally
            {
                analyzedArguments.Free();
            }

            return creation;
        }

        private BoundExpression BindTypeParameterCreationExpression(ObjectCreationExpressionSyntax node, TypeParameterSymbol typeParameter, BoundExpression boundInitializerOpt, DiagnosticBag diagnostics)
        {
            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();
            BindArgumentsAndNames(node.ArgumentList, diagnostics, analyzedArguments);
            bool hasArguments = analyzedArguments.Arguments.Count > 0;
            analyzedArguments.Free();

            if (!typeParameter.HasConstructorConstraint && !typeParameter.IsValueType)
            {
                diagnostics.Add(ErrorCode.ERR_NoNewTyvar, node.Location, typeParameter);
            }
            else if (hasArguments)
            {
                diagnostics.Add(ErrorCode.ERR_NewTyvarWithArgs, node.Location, typeParameter);
            }
            else
            {
                return new BoundNewT(node, boundInitializerOpt, typeParameter);
            }

            return new BoundBadExpression(node, LookupResultKind.NotCreatable, ImmutableArray.Create<Symbol>(typeParameter), ImmutableArray<BoundNode>.Empty, typeParameter);
        }

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
        private bool TryPerformConstructorOverloadResolution(
            NamedTypeSymbol typeContainingConstructors,
            AnalyzedArguments analyzedArguments,
            string errorName,
            Location errorLocation,
            bool suppressResultDiagnostics,
            DiagnosticBag diagnostics,
            out MemberResolutionResult<MethodSymbol> memberResolutionResult,
            out ImmutableArray<MethodSymbol> candidateConstructors,
            bool allowProtectedConstructorsOfBaseType) // Last to make named arguments more convenient.
        {
            // Get accessible constructors for performing overload resolution.
            ImmutableArray<MethodSymbol> allInstanceConstructors;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            candidateConstructors = GetAccessibleConstructorsForOverloadResolution(typeContainingConstructors, allowProtectedConstructorsOfBaseType, out allInstanceConstructors, ref useSiteDiagnostics);

            OverloadResolutionResult<MethodSymbol> result = OverloadResolutionResult<MethodSymbol>.GetInstance();

            // Indicates whether overload resolution successfully chose an accessible constructor.
            bool succeededConsideringAccessibility = false;

            // Indicates whether overload resolution resulted in a single best match, even though it might be inaccessible.
            bool succeededIgnoringAccessibility = false;

            if (candidateConstructors.Any())
            {
                // We have at least one accessible candidate constructor, perform overload resolution with accessible candidateConstructors.
                this.OverloadResolution.ObjectCreationOverloadResolution(candidateConstructors, analyzedArguments, result, ref useSiteDiagnostics);

                if (result.Succeeded)
                {
                    succeededConsideringAccessibility = true;
                    succeededIgnoringAccessibility = true;
                }
            }

            if (!succeededConsideringAccessibility && allInstanceConstructors.Length > candidateConstructors.Length)
            {
                // Overload resolution failed on the accessible candidateConstructors, but we have at least one inaccessible constructor.
                // We might have a best match constructor which is inaccessible.
                // Try overload resolution with all instance constructors to generate correct diagnostics and semantic info for this case.
                OverloadResolutionResult<MethodSymbol> inaccessibleResult = OverloadResolutionResult<MethodSymbol>.GetInstance();
                this.OverloadResolution.ObjectCreationOverloadResolution(allInstanceConstructors, analyzedArguments, inaccessibleResult, ref useSiteDiagnostics);

                if (inaccessibleResult.Succeeded)
                {
                    succeededIgnoringAccessibility = true;
                    candidateConstructors = allInstanceConstructors;
                    result.Free();
                    result = inaccessibleResult;
                }
                else
                {
                    inaccessibleResult.Free();
                }
            }

            diagnostics.Add(errorLocation, useSiteDiagnostics);
            useSiteDiagnostics = null;

            if (succeededIgnoringAccessibility)
            {
                this.CoerceArguments<MethodSymbol>(result.ValidResult, analyzedArguments.Arguments, diagnostics);
            }

            // Fill in the out parameter with the result, if there was one; it might be inaccessible.
            memberResolutionResult = succeededIgnoringAccessibility ?
                result.ValidResult :
                default(MemberResolutionResult<MethodSymbol>); // Invalid results are not interesting - we have enough info in candidateConstructors.

            // If something failed and we are reporting errors, then report the right errors.
            // * If the failure was due to inaccessibility, just report that.
            // * If the failure was not due to inaccessibility then only report an error
            //   on the constructor if there were no errors on the arguments.
            if (!succeededConsideringAccessibility && !suppressResultDiagnostics)
            {
                if (succeededIgnoringAccessibility)
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
                else if (!analyzedArguments.HasErrors)
                {
                    // If the arguments had an error reported then do not report further errors for 
                    // overload resolution failure.
                    result.ReportDiagnostics(this, errorLocation, diagnostics,
                        errorName, null, analyzedArguments, candidateConstructors, typeContainingConstructors, null);
                }
            }

            result.Free();
            return succeededConsideringAccessibility;
        }

        private ImmutableArray<MethodSymbol> GetAccessibleConstructorsForOverloadResolution(NamedTypeSymbol type, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            ImmutableArray<MethodSymbol> allInstanceConstructors;
            return GetAccessibleConstructorsForOverloadResolution(type, false, out allInstanceConstructors, ref useSiteDiagnostics);
        }

        private ImmutableArray<MethodSymbol> GetAccessibleConstructorsForOverloadResolution(NamedTypeSymbol type, bool allowProtectedConstructorsOfBaseType, out ImmutableArray<MethodSymbol> allInstanceConstructors, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (type.IsErrorType())
            {
                // For Caas, we want to supply the constructors even in error cases
                // We may end up supplying the constructors of an unconstructed symbol,
                // but that's better than nothing.
                type = type.GetNonErrorGuess() as NamedTypeSymbol ?? type;
            }

            allInstanceConstructors = type.InstanceConstructors;
            return FilterInaccessibleConstructors(allInstanceConstructors, allowProtectedConstructorsOfBaseType, ref useSiteDiagnostics);
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

        private BoundLiteral BindLiteralConstant(LiteralExpressionSyntax node, DiagnosticBag diagnostics)
        {
            // bug.Assert(node.Kind == SyntaxKind.LiteralExpression);

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
                Debug.Assert(specialType != SpecialType.None);

                cv = ConstantValue.Create(value, specialType);
                type = GetSpecialType(specialType, diagnostics, node);
            }

            return new BoundLiteral(node, cv, type);
        }

        private BoundExpression BindCheckedExpression(CheckedExpressionSyntax node, DiagnosticBag diagnostics)
        {
            // the binder is not cached since we only cache statement level binders
            return this.WithCheckedOrUncheckedRegion(node.Kind == SyntaxKind.CheckedExpression).
                BindParenthesizedExpression(node.Expression, diagnostics);
        }

        /// <summary>
        /// Binds a member access expression
        /// </summary>
        private BoundExpression BindMemberAccess(
            MemberAccessExpressionSyntax node,
            bool invoked,
            bool indexed,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);

            BoundExpression boundLeft;

            ExpressionSyntax exprSyntax = node.Expression;
            if (node.Kind == SyntaxKind.SimpleMemberAccessExpression)
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                boundLeft =
                    // First, try to detect the Color Color case.
                    BindLeftOfPotentialColorColorMemberAccess(exprSyntax, ref useSiteDiagnostics) ??
                    // Then, if this is definitely not a Color Color case, just bind the expression normally.
                    // NOTE: CheckValue will be called explicitly in BindMemberAccessWithBoundLeft.
                    this.BindExpression(exprSyntax, diagnostics);
                diagnostics.Add(node, useSiteDiagnostics);
            }
            else
            {
                Debug.Assert(node.Kind == SyntaxKind.PointerMemberAccessExpression);
                boundLeft = this.BindExpression(exprSyntax, diagnostics); // Not Color Color issues with ->

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
                    boundLeft = new BoundPointerIndirectionOperator(exprSyntax, boundLeft, pointedAtType, hasErrors)
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
        private BoundExpression BindLeftOfPotentialColorColorMemberAccess(ExpressionSyntax left, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            BoundExpression boundLeft = null;

            // SPEC: 7.6.4.1 Identical simple names and type names
            // SPEC: In a member access of the form E.I, if E is a single identifier, and if the meaning of E as
            // SPEC: a simple-name (spec 7.6.2) is a constant, field, property, local variable, or parameter with the
            // SPEC: same type as the meaning of E as a type-name (spec 3.8), then both possible meanings of E are 
            // SPEC: permitted. The two possible meanings of E.I are never ambiguous, since I must necessarily be
            // SPEC: a member of the type E in both cases. In other words, the rule simply permits access to the 
            // SPEC: static members and nested types of E where a compile-time error would otherwise have occurred. 

            // NOTE: We don't want to bind left until we know how to interpret it, because binding can have side-effects.

            if (left.Kind == SyntaxKind.IdentifierName)
            {
                LookupResult result = LookupResult.GetInstance();
                string leftName = ((IdentifierNameSyntax)left).Identifier.ValueText;
                TypeSymbol leftType = null;

                this.LookupSymbolsWithFallback(result, leftName, arity: 0, useSiteDiagnostics: ref useSiteDiagnostics, options: LookupOptions.AllMethodsOnArityZero);

                if (result.IsSingleViable)
                {
                    Symbol leftSymbol = result.SingleSymbolOrDefault;

                    switch (leftSymbol.Kind)
                    {
                        case SymbolKind.Local:
                            {
                                var localSymbol = (LocalSymbol)leftSymbol;

                                bool isBindingVar = IsBindingImplicitlyTypedLocal(localSymbol);
                                if (!isBindingVar)
                                {
                                    bool usedBeforeDecl =
                                        left.SyntaxTree == localSymbol.Locations[0].SourceTree &&
                                        left.SpanStart < localSymbol.Locations[0].SourceSpan.Start;

                                    if (!usedBeforeDecl)
                                    {
                                        leftType = localSymbol.Type;
                                    }
                                }
                                break;
                            }
                        case SymbolKind.Field:
                            {
                                FieldSymbol field = (FieldSymbol)leftSymbol;
                                TypeSymbol fieldContainingType = field.ContainingType;
                                if (fieldContainingType.IsEnumType() && this.InEnumMemberInitializer())
                                {
                                    leftType = fieldContainingType.GetEnumUnderlyingType();
                                    Debug.Assert((object)leftType != null);
                                }
                                else
                                {
                                    leftType = field.Type;
                                }
                                break;
                            }
                        case SymbolKind.Property:
                            leftType = ((PropertySymbol)leftSymbol).Type;
                            break;
                        case SymbolKind.Parameter:
                            leftType = ((ParameterSymbol)leftSymbol).Type;
                            break;
                            // case SymbolKind.Event: //SPEC: 7.6.4.1 (a.k.a. Color Color) doesn't cover events
                    }

                    if ((object)leftType != null && (leftType.Name == leftName || IsUsingAliasInScope(leftName)))
                    {
                        result.Clear();

                        this.LookupSymbolsWithFallback(result, leftName, arity: 0, useSiteDiagnostics: ref useSiteDiagnostics, options: LookupOptions.NamespacesOrTypesOnly);

                        Symbol leftTypeOrNamespaceSymbol = result.SingleSymbolOrDefault;
                        if (result.IsSingleViable &&
                            ((leftTypeOrNamespaceSymbol.Kind == SymbolKind.Alias && ((AliasSymbol)leftTypeOrNamespaceSymbol).Target == leftType) ||
                                leftTypeOrNamespaceSymbol == leftType))
                        {
                            // We don't have enough information to determine how the left name should be interpreted.
                            // Instantiate a placeholder node until overload resolution is done and we know what to replace it with.
                            boundLeft = new BoundTypeOrValueExpression(left, leftSymbol, this, leftType);
                        }
                    }
                }

                result.Free();
            }
            return boundLeft;
        }

        // returns true if name matches a using alias in scope
        // NOTE: when true is returned, the corresponding using is also marked as "used" 
        private bool IsUsingAliasInScope(string name)
        {
            var isSemanticModel = this.IsSemanticModelBinder;
            foreach (var importsList in this.ImportsList)
            {
                if (importsList.IsUsingAlias(name, isSemanticModel))
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
            DiagnosticBag diagnostics)
        {
            // We have an expression of the form "dynExpr.Name" or "dynExpr.Name<X>"

            SeparatedSyntaxList<TypeSyntax> typeArgumentsSyntax = right.Kind == SyntaxKind.GenericName ?
                ((GenericNameSyntax)right).TypeArgumentList.Arguments :
                default(SeparatedSyntaxList<TypeSyntax>);
            bool rightHasTypeArguments = typeArgumentsSyntax.Count > 0;
            ImmutableArray<TypeSymbol> typeArguments = rightHasTypeArguments ?
                BindTypeArguments(typeArgumentsSyntax, diagnostics) :
                default(ImmutableArray<TypeSymbol>);

            bool hasErrors = false;

            if (!invoked && rightHasTypeArguments)
            {
                // error CS0307: The property 'P' cannot be used with type arguments
                Error(diagnostics, ErrorCode.ERR_TypeArgsNotAllowed, right, right.Identifier.Text, SymbolKind.Property.Localize());
                hasErrors = true;
            }

            if (rightHasTypeArguments)
            {
                for (int i = 0; i < typeArguments.Length; ++i)
                {
                    var typeArgument = typeArguments[i];
                    if ((typeArgument.IsPointerType()) || typeArgument.IsRestrictedType())
                    {
                        // "The type '{0}' may not be used as a type argument"
                        Error(diagnostics, ErrorCode.ERR_BadTypeArgument, typeArgumentsSyntax[i], typeArgument);
                        hasErrors = true;
                    }
                }
            }

            return new BoundDynamicMemberAccess(
                syntax: node,
                receiver: boundLeft,
                typeArgumentsOpt: typeArguments,
                name: right.Identifier.ValueText,
                invoked: invoked,
                indexed: indexed,
                type: Compilation.DynamicType,
                hasErrors: hasErrors);
        }

        /// <summary>
        /// Bind the RHS of a member access expression, given the bound LHS.
        /// It is assumed that CheckValue has not been called on the LHS.
        /// </summary>
        private BoundExpression BindMemberAccessWithBoundLeft(
            ExpressionSyntax node,
            BoundExpression boundLeft,
            SimpleNameSyntax right,
            SyntaxToken operatorToken,
            bool invoked,
            bool indexed,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            Debug.Assert(boundLeft != null);

            if ((object)boundLeft.Type != null && boundLeft.Type.IsDynamic())
            {
                return BindDynamicMemberAccess(node, boundLeft, right, invoked, indexed, diagnostics);
            }

            boundLeft = MakeMemberAccessValue(boundLeft, diagnostics);

            TypeSymbol leftType = boundLeft.Type;

            // No member accesses on void
            if ((object)leftType != null && leftType.SpecialType == SpecialType.System_Void)
            {
                DiagnosticInfo diagnosticInfo = new CSDiagnosticInfo(ErrorCode.ERR_BadUnaryOp, SyntaxFacts.GetText(operatorToken.CSharpKind()), leftType);
                diagnostics.Add(new CSDiagnostic(diagnosticInfo, operatorToken.GetLocation()));
                return BadExpression(node, boundLeft);
            }

            if (boundLeft.Kind == BoundKind.UnboundLambda)
            {
                Debug.Assert((object)leftType == null);

                var msgId = ((UnboundLambda)boundLeft).MessageID;
                DiagnosticInfo diagnosticInfo = new CSDiagnosticInfo(ErrorCode.ERR_BadUnaryOp, SyntaxFacts.GetText(operatorToken.CSharpKind()), msgId.Localize());
                diagnostics.Add(new CSDiagnostic(diagnosticInfo, node.Location));
                return BadExpression(node, boundLeft);
            }

            if (boundLeft.Kind == BoundKind.DefaultOperator && boundLeft.ConstantValue == ConstantValue.Null)
            {
                Error(diagnostics, ErrorCode.WRN_DotOnDefault, node, leftType);
            }

            var lookupResult = LookupResult.GetInstance();
            try
            {
                LookupOptions options = LookupOptions.AllMethodsOnArityZero;
                if (invoked)
                {
                    options |= LookupOptions.MustBeInvocableIfMember;
                }

                var typeArgumentsSyntax = right.Kind == SyntaxKind.GenericName ? ((GenericNameSyntax)right).TypeArgumentList.Arguments : default(SeparatedSyntaxList<TypeSyntax>);
                bool rightHasTypeArguments = typeArgumentsSyntax.Count > 0;
                var typeArguments = rightHasTypeArguments ? BindTypeArguments(typeArgumentsSyntax, diagnostics) : default(ImmutableArray<TypeSymbol>);

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

                switch (boundLeft.Kind)
                {
                    case BoundKind.NamespaceExpression:
                        {
                            // If K is zero and E is a namespace and E contains a nested namespace with name I, 
                            // then the result is that namespace.

                            var ns = ((BoundNamespaceExpression)boundLeft).NamespaceSymbol;
                            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                            this.LookupMembersWithFallback(lookupResult, ns, rightName, rightArity, ref useSiteDiagnostics, options: options);
                            diagnostics.Add(right, useSiteDiagnostics);

                            ArrayBuilder<Symbol> symbols = lookupResult.Symbols;

                            if (lookupResult.IsMultiViable)
                            {
                                bool wasError;
                                Symbol sym = ResultSymbol(lookupResult, rightName, rightArity, node, diagnostics, false, out wasError, ns, options);
                                if (wasError)
                                {
                                    return new BoundBadExpression(node, LookupResultKind.Ambiguous, lookupResult.Symbols.AsImmutable(), ImmutableArray.Create<BoundNode>(boundLeft), CreateErrorType(rightName), hasErrors: true);
                                }
                                else if (sym.Kind == SymbolKind.Namespace)
                                {
                                    return new BoundNamespaceExpression(node, (NamespaceSymbol)sym);
                                }
                                else
                                {
                                    Debug.Assert(sym.Kind == SymbolKind.NamedType);
                                    var type = (NamedTypeSymbol)sym;

                                    if (rightHasTypeArguments)
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
                                        new ExtendedErrorTypeSymbol(GetContainingNamespaceOrType(symbols[0]), symbols.ToImmutable(), lookupResult.Kind, lookupResult.Error, rightArity));
                            }
                            else if (lookupResult.Kind == LookupResultKind.Empty)
                            {
                                Debug.Assert(lookupResult.IsClear, "If there's a legitimate reason for having candidates without a reason, then we should produce something intelligent in such cases.");
                                Debug.Assert(lookupResult.Error == null);
                                NotFound(node, rightName, rightArity, rightName, diagnostics, aliasOpt: null, qualifierOpt: ns, options: options);

                                return new BoundBadExpression(node, lookupResult.Kind, symbols.AsImmutable(), ImmutableArray.Create<BoundNode>(boundLeft), CreateErrorType(rightName), hasErrors: true);
                            }
                            break;
                        }
                    case BoundKind.TypeExpression:
                        {
                            Debug.Assert((object)leftType != null);
                            if (leftType.TypeKind == TypeKind.TypeParameter)
                            {
                                Error(diagnostics, ErrorCode.ERR_BadSKunknown, boundLeft.Syntax, leftType, MessageID.IDS_SK_TYVAR.Localize());
                                return BadExpression(node, LookupResultKind.NotAValue, boundLeft);
                            }
                            else
                            {
                                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                                this.LookupMembersWithFallback(lookupResult, leftType, rightName, rightArity, ref useSiteDiagnostics, basesBeingResolved: null, options: options);
                                diagnostics.Add(right, useSiteDiagnostics);
                                if (lookupResult.IsMultiViable)
                                {
                                    return BindMemberOfType(node, right, rightName, rightArity, boundLeft, typeArgumentsSyntax, typeArguments, lookupResult, BoundMethodGroupFlags.None, diagnostics: diagnostics);
                                }
                            }
                            break;
                        }
                    case BoundKind.TypeOrValueExpression:
                        {
                            // CheckValue call will occur in ReplaceTypeOrValueReceiver.
                            // NOTE: This means that we won't get CheckValue diagnostics in error scenarios,
                            // but they would be cascading anyway.
                            return BindInstanceMemberAccess(node, right, boundLeft, rightName, rightArity, typeArgumentsSyntax, typeArguments, invoked, diagnostics);
                        }
                    default:
                        {
                            // Can't dot into the null literal.
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
                                boundLeft = CheckValue(boundLeft, BindValueKind.RValue, diagnostics);
                                return BindInstanceMemberAccess(node, right, boundLeft, rightName, rightArity, typeArgumentsSyntax, typeArguments, invoked, diagnostics);
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
        }

        /// <summary>
        /// Create a value from the expression that can be used as a left-hand-side
        /// of a member access. This method special-cases method and property
        /// groups only. All other expressions are returned as is.
        /// </summary>
        private BoundExpression MakeMemberAccessValue(BoundExpression expr, DiagnosticBag diagnostics)
        {
            switch (expr.Kind)
            {
                case BoundKind.MethodGroup:
                    {
                        var methodGroup = (BoundMethodGroup)expr;
                        HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                        var resolution = this.ResolveMethodGroup(methodGroup, analyzedArguments: null, isMethodGroupConversion: false, useSiteDiagnostics: ref useSiteDiagnostics);
                        diagnostics.Add(expr.Syntax, useSiteDiagnostics);
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
                    return expr;
            }
        }

        private BoundExpression BindInstanceMemberAccess(
            CSharpSyntaxNode node,
            CSharpSyntaxNode right,
            BoundExpression boundLeft,
            string rightName,
            int rightArity,
            SeparatedSyntaxList<TypeSyntax> typeArgumentsSyntax,
            ImmutableArray<TypeSymbol> typeArguments,
            bool invoked,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(rightArity == (typeArguments.IsDefault ? 0 : typeArguments.Length));
            var leftType = boundLeft.Type;
            LookupOptions options = LookupOptions.AllMethodsOnArityZero;
            if (invoked)
            {
                options |= LookupOptions.MustBeInvocableIfMember;
            }

            var lookupResult = LookupResult.GetInstance();
            try
            {
                // If E is a property access, indexer access, variable, or value, the type of
                // which is T, and a member lookup of I in T with K type arguments produces a
                // match, then E.I is evaluated and classified as follows:

                // UNDONE: Classify E as prop access, indexer access, variable or value

                bool leftIsBaseReference = boundLeft.Kind == BoundKind.BaseReference;
                if (leftIsBaseReference)
                {
                    options |= LookupOptions.UseBaseReferenceAccessibility;
                }

                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                this.LookupMembersWithFallback(lookupResult, leftType, rightName, rightArity, ref useSiteDiagnostics, basesBeingResolved: null, options: options);
                diagnostics.Add(right, useSiteDiagnostics);

                // SPEC: Otherwise, an attempt is made to process E.I as an extension method invocation.
                // SPEC: If this fails, E.I is an invalid member reference, and a binding-time error occurs.
                var searchExtensionMethodsIfNecessary = !leftIsBaseReference;

                BoundMethodGroupFlags flags = 0;
                if (searchExtensionMethodsIfNecessary)
                {
                    flags |= BoundMethodGroupFlags.SearchExtensionMethods;
                }

                if (lookupResult.IsMultiViable)
                {
                    return BindMemberOfType(node, right, rightName, rightArity, boundLeft, typeArgumentsSyntax, typeArguments, lookupResult, flags, diagnostics);
                }

                if (searchExtensionMethodsIfNecessary)
                {
                    return new BoundMethodGroup(
                        node,
                        typeArguments,
                        boundLeft,
                        rightName,
                        ImmutableArray<MethodSymbol>.Empty,
                        lookupResult,
                        flags);
                }

                this.BindMemberAccessReportError(node, right, rightName, boundLeft, lookupResult.Error, diagnostics);
                return BindMemberAccessBadResult(node, rightName, boundLeft, lookupResult.Error, lookupResult.Symbols.ToImmutable(), lookupResult.Kind);
            }
            finally
            {
                lookupResult.Free();
            }
        }

        private void BindMemberAccessReportError(BoundMethodGroup node, DiagnosticBag diagnostics)
        {
            var nameSyntax = node.NameSyntax;
            var syntax = node.MemberAccessExpressionSyntax ?? (CSharpSyntaxNode)nameSyntax;
            this.BindMemberAccessReportError(syntax, nameSyntax, node.Name, node.ReceiverOpt, node.LookupError, diagnostics);
        }

        /// <summary>
        /// Report the error from member access lookup. Or, if there
        /// was no explicit error from lookup, report "no such member".
        /// </summary>
        private void BindMemberAccessReportError(
            CSharpSyntaxNode node,
            CSharpSyntaxNode name,
            string plainName,
            BoundExpression boundLeft,
            DiagnosticInfo lookupError,
            DiagnosticBag diagnostics)
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
                if (boundLeft.Kind == BoundKind.TypeExpression ||
                    boundLeft.Kind == BoundKind.BaseReference ||
                    node.Kind == SyntaxKind.AwaitExpression && plainName == WellKnownMemberNames.GetResult)
                {
                    Error(diagnostics, ErrorCode.ERR_NoSuchMember, name, boundLeft.Type, plainName);
                }
                else if (WouldUsingSystemFindExtension(boundLeft.Type, plainName))
                {
                    Error(diagnostics, ErrorCode.ERR_NoSuchMemberOrExtensionNeedUsing, name, boundLeft.Type, plainName, "System");
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
            return IsWinRTAsyncInterface(type) || type.AllInterfacesNoUseSiteDiagnostics.Any(i => IsWinRTAsyncInterface(i));
        }

        private bool IsWinRTAsyncInterface(TypeSymbol type)
        {
            if (!type.IsInterfaceType())
            {
                return false;
            }

            var namedType = ((NamedTypeSymbol)type).ConstructedFrom;
            return
                namedType == Compilation.GetWellKnownType(WellKnownType.Windows_Foundation_IAsyncAction) ||
                namedType == Compilation.GetWellKnownType(WellKnownType.Windows_Foundation_IAsyncActionWithProgress_T) ||
                namedType == Compilation.GetWellKnownType(WellKnownType.Windows_Foundation_IAsyncOperation_T) ||
                namedType == Compilation.GetWellKnownType(WellKnownType.Windows_Foundation_IAsyncOperationWithProgress_T2);
        }

        private BoundExpression BindMemberAccessBadResult(BoundMethodGroup node)
        {
            var nameSyntax = node.NameSyntax;
            var syntax = node.MemberAccessExpressionSyntax ?? (CSharpSyntaxNode)nameSyntax;
            return this.BindMemberAccessBadResult(syntax, node.Name, node.ReceiverOpt, node.LookupError, StaticCast<Symbol>.From(node.Methods), node.ResultKind);
        }

        /// <summary>
        /// Return a BoundExpression representing the invalid member.
        /// </summary>
        private BoundExpression BindMemberAccessBadResult(
            CSharpSyntaxNode node,
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
                    default(ImmutableArray<TypeSymbol>),
                    nameString,
                    methods,
                    methods.Length == 1 ? methods[0] : null,
                    lookupError,
                    flags: BoundMethodGroupFlags.None,
                    receiverOpt: boundLeft,
                    resultKind: lookupKind,
                    hasErrors: true);
            }

            var symbolOpt = symbols.Length == 1 ? symbols[0] : null;
            return new BoundBadExpression(
                node,
                lookupKind,
                (object)symbolOpt == null ? ImmutableArray<Symbol>.Empty : ImmutableArray.Create<Symbol>(symbolOpt),
                boundLeft == null ? ImmutableArray<BoundNode>.Empty : ImmutableArray.Create<BoundNode>(boundLeft),
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
                        resultType = ((FieldSymbol)symbolOpt).GetFieldType(this.FieldsBeingBound);
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

        /// <summary>
        /// Combine the receiver and arguments of an extension method
        /// invocation into a single argument list to allow overload resolution
        /// to treat the invocation as a static method invocation with no receiver.
        /// </summary>
        private static void CombineExtensionMethodArguments(BoundExpression receiver, AnalyzedArguments originalArguments, AnalyzedArguments extensionMethodArguments)
        {
            Debug.Assert(receiver != null);
            Debug.Assert(extensionMethodArguments.Arguments.Count == 0);
            Debug.Assert(extensionMethodArguments.Names.Count == 0);
            Debug.Assert(extensionMethodArguments.RefKinds.Count == 0);

            extensionMethodArguments.IsExtensionMethodInvocation = true;
            extensionMethodArguments.Arguments.Add(receiver);
            extensionMethodArguments.Arguments.AddRange(originalArguments.Arguments);

            if (originalArguments.Names.Count > 0)
            {
                extensionMethodArguments.Names.Add(null);
                extensionMethodArguments.Names.AddRange(originalArguments.Names);
            }

            if (originalArguments.RefKinds.Count > 0)
            {
                extensionMethodArguments.RefKinds.Add(RefKind.None);
                extensionMethodArguments.RefKinds.AddRange(originalArguments.RefKinds);
            }
        }

        /// <summary>
        /// Binds a static or instance member access.
        /// </summary>
        private BoundExpression BindMemberOfType(
            CSharpSyntaxNode node,
            CSharpSyntaxNode right,
            string plainName,
            int arity,
            BoundExpression left,
            SeparatedSyntaxList<TypeSyntax> typeArgumentsSyntax,
            ImmutableArray<TypeSymbol> typeArguments,
            LookupResult lookupResult,
            BoundMethodGroupFlags methodGroupFlags,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            Debug.Assert(left != null);
            Debug.Assert(lookupResult.IsMultiViable);
            Debug.Assert(lookupResult.Symbols.Any());

            var members = ArrayBuilder<Symbol>.GetInstance();
            BoundExpression result;
            bool wasError;
            Symbol symbol = GetSymbolOrMethodOrPropertyGroup(lookupResult, right, plainName, arity, members, diagnostics, out wasError);

            if ((object)symbol == null)
            {
                Debug.Assert(members.Count > 0);

                // If I identifies one or more methods, then the result is a method group with
                // no associated instance expression. If a type argument list was specified, it
                // is used in calling a generic method.

                // (Note that for static methods, we are stashing away the type expression in
                // the receiver of the method group, even though the spec notes that there is
                // no associated instance expression.)

                result = ConstructBoundMemberGroupAndReportOmittedTypeArguments(
                    node,
                    typeArgumentsSyntax,
                    typeArguments,
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
                // methods are special because of extension methods.
                Debug.Assert(symbol.Kind != SymbolKind.Method);
                left = ReplaceTypeOrValueReceiver(left, symbol.IsStatic || symbol.Kind == SymbolKind.NamedType, diagnostics);

                // Events are handled later as we don't know yet if we are binding to the event or it's backing field.
                if (symbol.Kind != SymbolKind.Event)
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
                        if (!typeArguments.IsDefault)
                        {
                            type = ConstructNamedTypeUnlessTypeArgumentOmitted(right, type, typeArgumentsSyntax, typeArguments, diagnostics);
                        }

                        result = new BoundTypeExpression(
                            syntax: node,
                            aliasOpt: null,
                            inferredType: false,
                            boundContainingTypeOpt: left as BoundTypeExpression,
                            type: type);
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
                        result = BindFieldAccess(node, left, (FieldSymbol)symbol, diagnostics, lookupResult.Kind, hasErrors: wasError);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
                }
            }

            members.Free();
            return result;
        }

        private MethodGroupResolution BindExtensionMethod(
            CSharpSyntaxNode expression,
            string methodName,
            AnalyzedArguments analyzedArguments,
            BoundExpression left,
            ImmutableArray<TypeSymbol> typeArguments,
            bool isMethodGroupConversion)
        {
            var firstResult = new MethodGroupResolution();
            AnalyzedArguments actualArguments = null;

            foreach (var scope in new ExtensionMethodScopes(this))
            {
                var methodGroup = MethodGroup.GetInstance();
                var diagnostics = DiagnosticBag.GetInstance();

                this.PopulateExtensionMethodsFromSingleBinder(scope, methodGroup, expression, left, methodName, typeArguments, diagnostics);
                if (methodGroup.Methods.Count == 0)
                {
                    methodGroup.Free();
                    diagnostics.Free();
                    continue;
                }

                // Arguments will be null if the caller is resolving to the first available
                // method group, regardless of arguments, when the signature cannot
                // be inferred. (In the error case of o.M = null; for instance.)
                if (analyzedArguments == null)
                {
                    return new MethodGroupResolution(methodGroup, diagnostics.ToReadOnlyAndFree());
                }

                if (actualArguments == null)
                {
                    // Create a set of arguments for overload resolution of the
                    // extension methods that includes the "this" parameter.
                    actualArguments = AnalyzedArguments.GetInstance();
                    CombineExtensionMethodArguments(left, analyzedArguments, actualArguments);
                }

                var overloadResolutionResult = OverloadResolutionResult<MethodSymbol>.GetInstance();
                bool allowRefOmittedArguments = methodGroup.Receiver.IsExpressionOfComImportType();
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                OverloadResolution.MethodInvocationOverloadResolution(methodGroup.Methods, methodGroup.TypeArguments, actualArguments, overloadResolutionResult, ref useSiteDiagnostics, isMethodGroupConversion, allowRefOmittedArguments);
                diagnostics.Add(expression, useSiteDiagnostics);
                var sealedDiagnostics = diagnostics.ToReadOnlyAndFree();
                var result = new MethodGroupResolution(methodGroup, null, overloadResolutionResult, actualArguments, methodGroup.ResultKind, sealedDiagnostics);

                // If the search in the current scope resulted in any applicable method (regardless of whether a best 
                // applicable method could be determined) then our search is complete. Otherwise, store aside the
                // first non-applicable result and continue searching for an applicable result.
                if (result.HasAnyApplicableMethod)
                {
                    if (!firstResult.IsEmpty)
                    {
                        // Free parts of the previous result but do not free AnalyzedArguments
                        // since we're using the same arguments for the returned result.
                        firstResult.MethodGroup.Free();
                        firstResult.OverloadResolutionResult.Free();
                    }
                    return result;
                }
                else if (firstResult.IsEmpty)
                {
                    firstResult = result;
                }
                else
                {
                    // Neither the first result, nor applicable. No need to save result.
                    overloadResolutionResult.Free();
                    methodGroup.Free();
                }
            }

            Debug.Assert((actualArguments == null) || !firstResult.IsEmpty);
            return firstResult;
        }

        private void PopulateExtensionMethodsFromSingleBinder(
            ExtensionMethodScope scope,
            MethodGroup methodGroup,
            CSharpSyntaxNode node,
            BoundExpression left,
            string rightName,
            ImmutableArray<TypeSymbol> typeArguments,
            DiagnosticBag diagnostics)
        {
            int arity;
            LookupOptions options;
            if (typeArguments.IsDefault)
            {
                arity = 0;
                options = LookupOptions.AllMethodsOnArityZero;
            }
            else
            {
                arity = typeArguments.Length;
                options = LookupOptions.Default;
            }

            var lookupResult = LookupResult.GetInstance();
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            this.LookupExtensionMethodsInSingleBinder(scope, lookupResult, rightName, arity, options, ref useSiteDiagnostics);
            diagnostics.Add(node, useSiteDiagnostics);

            if (!lookupResult.IsClear)
            {
                Debug.Assert(lookupResult.Symbols.Any());
                var members = ArrayBuilder<Symbol>.GetInstance();
                bool wasError;
                Symbol symbol = GetSymbolOrMethodOrPropertyGroup(lookupResult, node, rightName, arity, members, diagnostics, out wasError);
                Debug.Assert((object)symbol == null);
                Debug.Assert(members.Count > 0);
                methodGroup.PopulateWithExtensionMethods(left, members, typeArguments, lookupResult.Kind);
                members.Free();
            }

            lookupResult.Free();
        }

        protected BoundExpression BindFieldAccess(
            CSharpSyntaxNode node,
            BoundExpression receiver,
            FieldSymbol fieldSymbol,
            DiagnosticBag diagnostics,
            LookupResultKind resultKind,
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

            if (!hasError && fieldSymbol.IsFixed)
            {
                TypeSymbol receiverType = receiver.Type;
                hasError =
                    // Reflect errors that have been reported elsewhere...
                    (object)receiverType == null || !receiverType.IsValueType ||
                    // ...and errors that are reported here.
                    !CheckIsVariable(node, receiver, BindValueKind.FixedReceiver, checkingReceiver: false, diagnostics: diagnostics);
                if (!hasError)
                {
                    var isFixedStatementExpression = SyntaxFacts.IsFixedStatementExpression(node);
                    Symbol accessedLocalOrParameterOpt;
                    if (IsNonMoveableVariable(receiver, out accessedLocalOrParameterOpt) == isFixedStatementExpression)
                    {
                        Error(diagnostics, isFixedStatementExpression ? ErrorCode.ERR_FixedNotNeeded : ErrorCode.ERR_FixedBufferNotFixed, node);
                        hasErrors = true;
                    }
                }
            }

            ConstantValue constantValueOpt = null;

            if (fieldSymbol.IsConst)
            {
                constantValueOpt = fieldSymbol.GetConstantValue(this.ConstantFieldsInProgress, this.IsEarlyAttributeBinder);
                if (constantValueOpt == ConstantValue.Unset)
                {
                    // Evaluating constant expression before dependencies
                    // have been evaluated. Treat this as a Bad value.
                    constantValueOpt = ConstantValue.Bad;
                }
            }

            TypeSymbol fieldType = fieldSymbol.GetFieldType(this.FieldsBeingBound);
            BoundExpression expr = new BoundFieldAccess(node, receiver, fieldSymbol, constantValueOpt, resultKind, fieldType, hasErrors: (hasErrors || hasError));

            // Spec 14.3: "Within an enum member initializer, values of other enum members are
            // always treated as having the type of their underlying type"
            if (this.InEnumMemberInitializer())
            {
                NamedTypeSymbol enumType = null;
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
                else if (constantValueOpt != null && fieldType.IsEnumType())
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
                    NamedTypeSymbol underlyingType = enumType.EnumUnderlyingType;
                    Debug.Assert((object)underlyingType != null);
                    expr = new BoundConversion(
                        node,
                        expr,
                        Conversion.ImplicitNumeric,
                        @checked: true,
                        explicitCastInCode: false,
                        constantValueOpt: expr.ConstantValue,
                        type: underlyingType);
                }
            }

            return expr;
        }

        private bool InEnumMemberInitializer()
        {
            var containingType = this.ContainingType;
            return this.InFieldInitializer && (object)containingType != null && containingType.IsEnumType();
        }

        private BoundExpression BindPropertyAccess(
            CSharpSyntaxNode node,
            BoundExpression receiver,
            PropertySymbol propertySymbol,
            DiagnosticBag diagnostics,
            LookupResultKind lookupResult,
            bool hasErrors)
        {
            bool hasError = this.CheckInstanceOrStatic(node, receiver, propertySymbol, ref lookupResult, diagnostics);

            return new BoundPropertyAccess(node, receiver, propertySymbol, lookupResult, propertySymbol.Type, hasErrors: (hasErrors || hasError));
        }

        private BoundExpression BindEventAccess(
            CSharpSyntaxNode node,
            BoundExpression receiver,
            EventSymbol eventSymbol,
            DiagnosticBag diagnostics,
            LookupResultKind lookupResult,
            bool hasErrors)
        {
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            bool isUsableAsField = eventSymbol.HasAssociatedField && this.IsAccessible(eventSymbol.AssociatedField, ref useSiteDiagnostics, (receiver != null) ? receiver.Type : null);
            diagnostics.Add(node, useSiteDiagnostics);

            bool hasError = this.CheckInstanceOrStatic(node, receiver, eventSymbol, ref lookupResult, diagnostics);

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
            CSharpSyntaxNode node,
            BoundExpression receiver,
            Symbol symbol,
            ref LookupResultKind resultKind,
            DiagnosticBag diagnostics)
        {
            bool? instanceReceiver = IsInstanceReceiver(receiver);

            if (symbol.IsStatic)
            {
                if (instanceReceiver == true)
                {
                    ErrorCode errorCode = this.Flags.Includes(BinderFlags.ObjectInitializerMember) ?
                        ErrorCode.ERR_StaticMemberInObjectInitializer :
                        ErrorCode.ERR_ObjectProhibited;
                    Error(diagnostics, errorCode, node, symbol);
                    resultKind = LookupResultKind.StaticInstanceMismatch;
                    return true;
                }
            }
            else
            {
                if (instanceReceiver == false)
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
        private Symbol GetSymbolOrMethodOrPropertyGroup(LookupResult result, CSharpSyntaxNode node, string plainName, int arity, ArrayBuilder<Symbol> methodOrPropertyGroup, DiagnosticBag diagnostics, out bool wasError)
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
            return ResultSymbol(result, plainName, arity, node, diagnostics, false, out wasError);
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

        private BoundExpression BindElementAccess(ElementAccessExpressionSyntax node, DiagnosticBag diagnostics)
        {
            BoundExpression receiver = BindExpression(node.Expression, diagnostics: diagnostics, invoked: false, indexed: true);
            return BindElementAccess(node, receiver, node.ArgumentList, diagnostics);
        }

        private BoundExpression BindElementAccess(ExpressionSyntax node, BoundExpression receiver, BracketedArgumentListSyntax argumentList, DiagnosticBag diagnostics)
        {
            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();
            try
            {
                BindArgumentsAndNames(argumentList, diagnostics, analyzedArguments);

                if (receiver.Kind == BoundKind.PropertyGroup)
                {
                    var propertyGroup = (BoundPropertyGroup)receiver;
                    return BindIndexedPropertyAccess(node, propertyGroup.ReceiverOpt, propertyGroup.Properties, analyzedArguments, diagnostics);
                }

                receiver = CheckValue(receiver, BindValueKind.RValue, diagnostics);

                return BindElementOrIndexerAccess(node, receiver, analyzedArguments, diagnostics);
            }
            finally
            {
                analyzedArguments.Free();
            }
        }

        private BoundExpression BindElementOrIndexerAccess(ExpressionSyntax node, BoundExpression expr, AnalyzedArguments analyzedArguments, DiagnosticBag diagnostics)
        {
                if ((object)expr.Type == null)
                {
                    return BadIndexerExpression(node, expr, analyzedArguments, null, diagnostics);
                }
                else if (expr.Kind == BoundKind.DefaultOperator && ((BoundDefaultOperator)expr).ConstantValue == ConstantValue.Null)
                {
                    Error(diagnostics, ErrorCode.WRN_DotOnDefault, node, expr.Type);
                }

                // Did we have any errors?
                if (analyzedArguments.HasErrors || expr.HasAnyErrors)
                {
                    // At this point we definitely have reported an error, but we still might be 
                    // able to get more semantic analysis of the indexing operation. We do not
                    // want to report cascading errors.

                    DiagnosticBag tmp = DiagnosticBag.GetInstance();
                    BoundExpression result = BindElementAccessCore(node, expr, analyzedArguments, tmp);
                    tmp.Free();
                    return result;
                }

                return BindElementAccessCore(node, expr, analyzedArguments, diagnostics);
            }

        private BoundExpression BadIndexerExpression(ExpressionSyntax node, BoundExpression expr, AnalyzedArguments analyzedArguments, DiagnosticInfo errorOpt, DiagnosticBag diagnostics)
        {
            if (!expr.HasAnyErrors)
            {
                diagnostics.Add(errorOpt ?? new CSDiagnosticInfo(ErrorCode.ERR_BadIndexLHS, expr.Display), node.Location);
            }

            var childBoundNodes = StaticCast<BoundNode>.From(BuildArgumentsForErrorRecovery(analyzedArguments)).Add(expr);
            return new BoundBadExpression(node, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, childBoundNodes, CreateErrorType(), hasErrors: true);
        }

        private BoundExpression BindElementAccessCore(
             ExpressionSyntax node,
             BoundExpression expr,
             AnalyzedArguments arguments,
             DiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            Debug.Assert(expr != null);
            Debug.Assert((object)expr.Type != null);
            Debug.Assert(arguments != null);

            var exprType = expr.Type;
            switch (exprType.TypeKind)
            {
                case TypeKind.ArrayType:
                    return BindArrayAccess(node, expr, arguments, diagnostics);

                case TypeKind.DynamicType:
                    return BindDynamicIndexer(node, expr, arguments, ImmutableArray<PropertySymbol>.Empty, diagnostics);

                case TypeKind.PointerType:
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

        private BoundExpression BindArrayAccess(ExpressionSyntax node, BoundExpression expr, AnalyzedArguments arguments, DiagnosticBag diagnostics)
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

            // UNDONE: An array access cannot be ref/out either.

            if (arguments.Names.Count > 0)
            {
                Error(diagnostics, ErrorCode.ERR_NamedArgumentForArray, node);
            }

            var arrayType = (ArrayTypeSymbol)expr.Type;

            // Note that the spec says to determine which of {int, uint, long, ulong} *each* index
            // expression is convertible to. That is not what C# 1 through 4 did; the
            // implementations instead determined which of those four types *all* of the index
            // expressions converted to. 

            int rank = arrayType.Rank;

            if (arguments.Arguments.Count != arrayType.Rank)
            {
                Error(diagnostics, ErrorCode.ERR_BadIndexCount, node, rank);
                return new BoundArrayAccess(node, expr, BuildArgumentsForErrorRecovery(arguments), arrayType.ElementType, hasErrors: true);
            }

            // Convert all the arguments to the array index type.
            BoundExpression[] convertedArguments = new BoundExpression[arguments.Arguments.Count];
            for (int i = 0; i < arguments.Arguments.Count; ++i)
            {
                BoundExpression argument = arguments.Arguments[i];

                if (argument.Kind == BoundKind.UninitializedVarDeclarationExpression)
                {
                    argument = ((UninitializedVarDeclarationExpression)argument).FailInference(this, diagnostics);
                }

                BoundExpression index = ConvertToArrayIndex(argument, node, diagnostics);
                convertedArguments[i] = index;

                // NOTE: Dev10 only warns if rank == 1
                // Question: Why do we limit this warning to one-dimensional arrays?
                // Answer: Because multidimensional arrays can have nonzero lower bounds in the CLR.
                if (rank == 1 && !index.HasAnyErrors)
                {
                    ConstantValue constant = index.ConstantValue;
                    if (constant != null && constant.IsNegativeNumeric)
                    {
                        Error(diagnostics, ErrorCode.WRN_NegativeArrayIndex, index.Syntax);
                    }
                }
            }

            return new BoundArrayAccess(node, expr, convertedArguments.AsImmutableOrNull(), arrayType.ElementType);
        }

        private BoundExpression ConvertToArrayIndex(BoundExpression index, CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            Debug.Assert(index != null);

            var result =
                TryImplicitConversionToArrayIndex(index, SpecialType.System_Int32, node, diagnostics) ??
                TryImplicitConversionToArrayIndex(index, SpecialType.System_UInt32, node, diagnostics) ??
                TryImplicitConversionToArrayIndex(index, SpecialType.System_Int64, node, diagnostics) ??
                TryImplicitConversionToArrayIndex(index, SpecialType.System_UInt64, node, diagnostics);

            if (result == null)
            {
                // Give the error that would be given upon conversion to int32.
                NamedTypeSymbol int32 = GetSpecialType(SpecialType.System_Int32, diagnostics, node);
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                Conversion failedConversion = this.Conversions.ClassifyConversionFromExpression(index, int32, ref useSiteDiagnostics);
                diagnostics.Add(node, useSiteDiagnostics);
                GenerateImplicitConversionError(diagnostics, node, failedConversion, index, int32);
                return new BoundConversion(
                    index.Syntax,
                    index,
                    failedConversion,
                    CheckOverflowAtRuntime,
                    explicitCastInCode: false,
                    constantValueOpt: ConstantValue.NotAvailable,
                    type: int32,
                    hasErrors: true);
            }

            return result;
        }

        private BoundExpression TryImplicitConversionToArrayIndex(BoundExpression expr, SpecialType specialType, CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            DiagnosticBag attemptDiagnostics = DiagnosticBag.GetInstance();

            TypeSymbol type = GetSpecialType(specialType, attemptDiagnostics, node);

            Debug.Assert(expr != null);
            Debug.Assert((object)type != null);

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            Conversion conversion = this.Conversions.ClassifyImplicitConversionFromExpression(expr, type, ref useSiteDiagnostics);
            diagnostics.Add(node, useSiteDiagnostics);
            if (!conversion.Exists)
            {
                attemptDiagnostics.Free();
                return null;
            }

            BoundExpression result = CreateConversion(expr.Syntax, expr, conversion.ToArrayIndexConversion(), isCast: false, destination: type, diagnostics: attemptDiagnostics); // UNDONE: was cast?
            Debug.Assert(result != null); // If this ever fails (it shouldn't), then put a null-check around the diagnostics update.

            diagnostics.AddRange(attemptDiagnostics);
            attemptDiagnostics.Free();

            return result;
        }

        private BoundExpression BindPointerElementAccess(ExpressionSyntax node, BoundExpression expr, AnalyzedArguments analyzedArguments, DiagnosticBag diagnostics)
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

            ArrayBuilder<BoundExpression> arguments = analyzedArguments.Arguments;
            int numArguments = arguments.Count;

            if (!hasErrors)
            {
                for (int i = 0; i < numArguments; i++)
                {
                    // NOTE: probably can't hit this if the syntax node came from the parser.
                    RefKind refKind = analyzedArguments.RefKind(i);
                    if (refKind != RefKind.None)
                    {
                        // CONSIDER: would it be nicer to have a resource string for the keyword?
                        Error(diagnostics, ErrorCode.ERR_BadArgExtraRef, analyzedArguments.Argument(i).Syntax, i + 1, refKind.ToDisplayString());
                        hasErrors = true;
                        break;
                    }
                }
            }

            Debug.Assert(expr.Type.IsPointerType());
            PointerTypeSymbol pointerType = (PointerTypeSymbol)expr.Type;
            TypeSymbol pointedAtType = pointerType.PointedAtType;

            if (numArguments != 1)
            {
                if (!hasErrors)
                {
                    Error(diagnostics, ErrorCode.ERR_PtrIndexSingle, node);
                }
                return new BoundPointerElementAccess(node, expr, BadExpression(node, BuildArgumentsForErrorRecovery(analyzedArguments).ToArray()), CheckOverflowAtRuntime, pointedAtType, hasErrors: true);
            }

            if (pointedAtType.SpecialType == SpecialType.System_Void)
            {
                Error(diagnostics, ErrorCode.ERR_VoidError, expr.Syntax);
                hasErrors = true;
            }

            BoundExpression index = arguments[0];

            if (index.Kind == BoundKind.UninitializedVarDeclarationExpression)
            {
                index = ((UninitializedVarDeclarationExpression)index).FailInference(this, diagnostics);
            }

            index = ConvertToArrayIndex(index, index.Syntax, diagnostics);
            return new BoundPointerElementAccess(node, expr, index, CheckOverflowAtRuntime, pointedAtType, hasErrors);
        }

        private BoundExpression BindIndexerAccess(ExpressionSyntax node, BoundExpression expr, AnalyzedArguments analyzedArguments, DiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);
            Debug.Assert(expr != null);
            Debug.Assert((object)expr.Type != null);
            Debug.Assert(analyzedArguments != null);

            LookupResult lookupResult = LookupResult.GetInstance();
            LookupOptions lookupOptions = expr.Kind == BoundKind.BaseReference ? LookupOptions.UseBaseReferenceAccessibility : LookupOptions.Default;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            this.LookupMembersWithFallback(lookupResult, expr.Type, WellKnownMemberNames.Indexer, arity: 0, useSiteDiagnostics: ref useSiteDiagnostics, options: lookupOptions);
            diagnostics.Add(node, useSiteDiagnostics);

            // Store, rather than return, so that we can release resources.
            BoundExpression indexerAccessExpression;

            if (!lookupResult.IsMultiViable)
            {
                indexerAccessExpression = BadIndexerExpression(node, expr, analyzedArguments, lookupResult.Error, diagnostics);
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

        private static readonly Func<PropertySymbol, bool> IsIndexedPropertyWithNonOptionalArguments = property =>
            {
                if (property.IsIndexer || !property.IsIndexedProperty)
                {
                    return false;
                }

                Debug.Assert(property.ParameterCount > 0);
                var parameter = property.Parameters[0];
                return !parameter.IsOptional && !parameter.IsParams;
            };

        private static readonly SymbolDisplayFormat PropertyGroupFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeContainingType,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private BoundExpression BindIndexedPropertyAccess(BoundPropertyGroup propertyGroup, bool mustHaveAllOptionalParameters, DiagnosticBag diagnostics)
        {
            var syntax = propertyGroup.Syntax;
            var receiverOpt = propertyGroup.ReceiverOpt;
            var properties = propertyGroup.Properties;

            if (properties.All(IsIndexedPropertyWithNonOptionalArguments))
            {
                Error(diagnostics,
                    mustHaveAllOptionalParameters ? ErrorCode.ERR_IndexedPropertyMustHaveAllOptionalParams : ErrorCode.ERR_IndexedPropertyRequiresParams,
                    syntax,
                    properties[0].ToDisplayString(PropertyGroupFormat));
                return BoundIndexerAccess.ErrorAccess(
                    syntax,
                    receiverOpt,
                    CreateErrorPropertySymbol(properties),
                    ImmutableArray<BoundExpression>.Empty,
                    default(ImmutableArray<string>),
                    default(ImmutableArray<RefKind>),
                    properties);
            }

            var arguments = AnalyzedArguments.GetInstance();
            var result = BindIndexedPropertyAccess(syntax, receiverOpt, properties, arguments, diagnostics);
            arguments.Free();
            return result;
        }

        private BoundExpression BindIndexedPropertyAccess(CSharpSyntaxNode syntax, BoundExpression receiverOpt, ImmutableArray<PropertySymbol> propertyGroup, AnalyzedArguments arguments, DiagnosticBag diagnostics)
        {
            // TODO: We're creating an extra copy of the properties array in BindIndexerOrIndexedProperty
            // converting the ArrayBuilder to ImmutableArray. Avoid the extra copy.
            var properties = ArrayBuilder<PropertySymbol>.GetInstance();
            properties.AddRange(propertyGroup);
            var result = BindIndexerOrIndexedPropertyAccess(syntax, receiverOpt, properties, arguments, diagnostics);
            properties.Free();
            return result;
        }

        private BoundExpression BindDynamicIndexer(
             CSharpSyntaxNode syntax,
             BoundExpression receiverOpt,
             AnalyzedArguments arguments,
             ImmutableArray<PropertySymbol> applicableProperties,
             DiagnosticBag diagnostics)
        {
            bool hasErrors = false;

            if (receiverOpt != null)
            {
                BoundKind receiverKind = receiverOpt.Kind;
                if (receiverKind == BoundKind.BaseReference)
                {
                    Error(diagnostics, ErrorCode.ERR_NoDynamicPhantomOnBaseIndexer, syntax);
                    hasErrors = true;
                }
                else if (receiverKind == BoundKind.TypeOrValueExpression)
                {
                    var typeOrValue = (BoundTypeOrValueExpression)receiverOpt;

                    // Unfortunately, the runtime binder doesn't have APIs that would allow us to pass both "type or value".
                    // Ideally the runtime binder would choose between type and value based on the result of the overload resolution.
                    // We need to pick one or the other here. Dev11 compiler passes the type only if the value can't be accessed.
                    bool inStaticContext;
                    bool useType = IsInstance(typeOrValue.Variable) && !HasThis(isExplicit: false, inStaticContext: out inStaticContext);

                    receiverOpt = ReplaceTypeOrValueReceiver(typeOrValue, useType, diagnostics);
                }
            }

            var argArray = BuildArgumentsForDynamicInvocation(arguments, diagnostics);

            hasErrors &= ReportBadDynamicArguments(syntax, argArray, diagnostics, queryClause: null);

            return new BoundDynamicIndexerAccess(
                syntax,
                receiverOpt,
                argArray,
                arguments.GetNames(),
                arguments.RefKinds.ToImmutableOrNull(),
                applicableProperties,
                AssemblySymbol.DynamicType,
                hasErrors);
        }

        private BoundExpression BindIndexerOrIndexedPropertyAccess(
            CSharpSyntaxNode syntax,
            BoundExpression receiverOpt,
            ArrayBuilder<PropertySymbol> propertyGroup,
            AnalyzedArguments analyzedArguments,
            DiagnosticBag diagnostics)
        {
            ImmutableArray<string> argumentNames = analyzedArguments.GetNames();
            ImmutableArray<RefKind> argumentRefKinds = analyzedArguments.RefKinds.ToImmutableOrNull();

            OverloadResolutionResult<PropertySymbol> overloadResolutionResult = OverloadResolutionResult<PropertySymbol>.GetInstance();
            bool allowRefOmittedArguments = receiverOpt.IsExpressionOfComImportType();
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            this.OverloadResolution.PropertyOverloadResolution(propertyGroup, analyzedArguments, overloadResolutionResult, allowRefOmittedArguments, ref useSiteDiagnostics);
            diagnostics.Add(syntax, useSiteDiagnostics);
            BoundExpression propertyAccess;

            if (analyzedArguments.HasDynamicArgument && overloadResolutionResult.HasAnyApplicableMember)
            {
                var result = BindDynamicIndexer(syntax, receiverOpt, analyzedArguments, overloadResolutionResult.GetAllApplicableMembers(), diagnostics);
                overloadResolutionResult.Free();
                return result;
            }

            if (!overloadResolutionResult.Succeeded)
            {
                // If the arguments had an error reported about them then suppress further error
                // reporting for overload resolution. 

                ImmutableArray<PropertySymbol> candidates = propertyGroup.ToImmutable();

                if (!analyzedArguments.HasErrors)
                {
                    // Dev10 uses the "this" keyword as the method name for indexers.
                    var candidate = candidates[0];
                    var name = candidate.IsIndexer ? SyntaxFacts.GetText(SyntaxKind.ThisKeyword) : candidate.Name;

                    overloadResolutionResult.ReportDiagnostics(
                        this,
                        syntax.Location,
                        diagnostics,
                        name,
                        null,
                        analyzedArguments,
                        candidates,
                        typeContainingConstructor: null,
                        delegateTypeBeingInvoked: null);
                }

                PropertySymbol property;
                ImmutableArray<BoundExpression> arguments;
                if (candidates.Length == 1)
                {
                    property = candidates[0];
                    arguments = BuildArgumentsForErrorRecovery(analyzedArguments, property.Parameters);
                }
                else
                {
                    // A bad BoundIndexerAccess containing an ErrorPropertySymbol will produce better flow analysis results than
                    // a BoundBadExpression containing the candidate indexers.
                    property = CreateErrorPropertySymbol(candidates);
                    arguments = BuildArgumentsForErrorRecovery(analyzedArguments);
                }

                propertyAccess = BoundIndexerAccess.ErrorAccess(
                    syntax,
                    receiverOpt,
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
                this.CoerceArguments<PropertySymbol>(resolutionResult, analyzedArguments.Arguments, diagnostics);

                var isExpanded = resolutionResult.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm;
                var argsToParams = resolutionResult.Result.ArgsToParamsOpt;

                ReportDiagnosticsIfObsolete(diagnostics, property, syntax, hasBaseReceiver: receiverOpt != null && receiverOpt.Kind == BoundKind.BaseReference);

                // Make sure that the result of overload resolution is valid.
                var gotError = MemberGroupFinalValidationAccessibilityChecks(receiverOpt, property, syntax, diagnostics, invokedAsExtensionMethod: false);

                var receiver = ReplaceTypeOrValueReceiver(receiverOpt, property.IsStatic, diagnostics);

                if (!gotError && receiver != null && receiver.Kind == BoundKind.ThisReference && receiver.WasCompilerGenerated)
                {
                    gotError = IsRefOrOutThisParameterCaptured(syntax, diagnostics);
                }

                propertyAccess = new BoundIndexerAccess(
                    syntax,
                    receiver,
                    property,
                    analyzedArguments.Arguments.ToImmutable(),
                    argumentNames,
                    argumentRefKinds,
                    isExpanded,
                    argsToParams,
                    property.Type,
                    gotError);
            }

            overloadResolutionResult.Free();
            return propertyAccess;
        }

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
        internal MethodGroupResolution ResolveMethodGroup(
            BoundMethodGroup node,
            AnalyzedArguments analyzedArguments,
            bool isMethodGroupConversion,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            bool inferWithDynamic = false)
        {
            return ResolveMethodGroup(node, node.Syntax, node.Name, analyzedArguments, isMethodGroupConversion, ref useSiteDiagnostics, inferWithDynamic: inferWithDynamic);
        }

        internal MethodGroupResolution ResolveMethodGroup(
            BoundMethodGroup node,
            CSharpSyntaxNode expression,
            string methodName,
            AnalyzedArguments analyzedArguments,
            bool isMethodGroupConversion,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            bool inferWithDynamic = false)
        {
            var methodResolution = ResolveMethodGroupInternal(
                node, expression, methodName, analyzedArguments, isMethodGroupConversion, ref useSiteDiagnostics, inferWithDynamic: inferWithDynamic);
            if (methodResolution.IsEmpty && !methodResolution.HasAnyErrors)
            {
                Debug.Assert(node.LookupError == null);

                var diagnostics = DiagnosticBag.GetInstance();
                diagnostics.AddRange(methodResolution.Diagnostics); // Could still have use site warnings.
                BindMemberAccessReportError(node, diagnostics);
                return new MethodGroupResolution(methodResolution.MethodGroup, methodResolution.OtherSymbol, methodResolution.OverloadResolutionResult, methodResolution.AnalyzedArguments, methodResolution.ResultKind, diagnostics.ToReadOnlyAndFree());
            }
            return methodResolution;
        }

        private MethodGroupResolution ResolveMethodGroupInternal(
            BoundMethodGroup methodGroup,
            CSharpSyntaxNode expression,
            string methodName,
            AnalyzedArguments analyzedArguments,
            bool isMethodGroupConversion,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            bool inferWithDynamic = false)
        {
            var methodResolution = ResolveDefaultMethodGroup(methodGroup, analyzedArguments, isMethodGroupConversion, ref useSiteDiagnostics, inferWithDynamic: inferWithDynamic);

            // If the method group's receiver is dynamic then there is no point in looking for extension methods; 
            // it's going to be a dynamic invocation.
            if (!methodGroup.SearchExtensionMethods || methodResolution.HasAnyApplicableMethod || methodGroup.MethodGroupReceiverIsDynamic())
            {
                return methodResolution;
            }

            var extensionMethodResolution = BindExtensionMethod(expression, methodName, analyzedArguments, methodGroup.ReceiverOpt, methodGroup.TypeArgumentsOpt, isMethodGroupConversion);

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
            bool isMethodGroupConversion,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            bool inferWithDynamic = false)
        {
            var methods = node.Methods;
            if (methods.Length == 0)
            {
                var method = node.LookupSymbolOpt as MethodSymbol;
                if ((object)method != null)
                {
                    methods = ImmutableArray.Create(method);
                }
            }

            ImmutableArray<Diagnostic> sealedDiagnostics = ImmutableArray<Diagnostic>.Empty;
            if (node.LookupError != null)
            {
                DiagnosticBag diagnostics = DiagnosticBag.GetInstance();
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
                bool allowRefOmittedArguments = methodGroup.Receiver.IsExpressionOfComImportType();
                OverloadResolution.MethodInvocationOverloadResolution(
                    methodGroup.Methods, methodGroup.TypeArguments, analyzedArguments,
                    result, ref useSiteDiagnostics, isMethodGroupConversion, allowRefOmittedArguments, inferWithDynamic: inferWithDynamic);
                return new MethodGroupResolution(methodGroup, null, result, analyzedArguments, methodGroup.ResultKind, sealedDiagnostics);
            }
        }

        internal static bool ReportDelegateInvokeUseSiteDiagnostic(DiagnosticBag diagnostics, TypeSymbol possibleDelegateType,
            Location location = null, CSharpSyntaxNode node = null)
        {
            Debug.Assert((location == null) ^ (node == null));

            if (!possibleDelegateType.IsDelegateType())
            {
                return false;
            }

            MethodSymbol invoke = possibleDelegateType.DelegateInvokeMethod();
            if ((object)invoke == null)
            {
                diagnostics.Add(new CSDiagnosticInfo(ErrorCode.ERR_InvalidDelegateType, possibleDelegateType), location ?? node.Location);
                return true;
            }

            DiagnosticInfo info = invoke.GetUseSiteDiagnostic();
            if (info == null)
            {
                return false;
            }

            if (location == null)
            {
                location = node.Location;
            }

            if (info.Code == (int)ErrorCode.ERR_InvalidDelegateType)
            {
                diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_InvalidDelegateType, possibleDelegateType), location));
                return true;
            }

            return Symbol.ReportUseSiteDiagnostic(info, diagnostics, location);
        }

        private BoundConditionalAccess BindConditionalAccessExpression(ConditionalAccessExpressionSyntax node, DiagnosticBag diagnostics)
        {
            BoundExpression receiver = BindConditionalAccessReceiver(node, diagnostics);

            var conditionalAccessBinder = new BinderWithConditionalReceiver(this, receiver);
            var access = conditionalAccessBinder.BindValue(node.WhenNotNull, diagnostics, BindValueKind.RValue);

            if (receiver.HasAnyErrors || access.HasAnyErrors)
            {
                return new BoundConditionalAccess(node, receiver, access, CreateErrorType(), hasErrors: true);
            }

            var receiverType = receiver.Type;
            Debug.Assert(receiverType != null);

            // access cannot be a method group
            if (access.Kind == BoundKind.MethodGroup)
            {
                return GenerateBadConditionalAccessNodeError(node, receiver, access, diagnostics);
            }

            var accessType = access.Type;

            // access cannot have no type
            if (accessType == null)
            {
                return GenerateBadConditionalAccessNodeError(node, receiver, access, diagnostics);
            }

            // access cannot have unconstrained generic type
            if (!accessType.IsReferenceType && !accessType.IsValueType)
            {
                return GenerateBadConditionalAccessNodeError(node, receiver, access, diagnostics);
            }

            // access cannot be void
            if (accessType.SpecialType == SpecialType.System_Void)
            {
                return GenerateBadConditionalAccessNodeError(node, receiver, access, diagnostics);
            }

            // if access has value type, the type of the conditional access is nullable of that
            if (accessType.IsValueType && !accessType.IsNullableType())
            {
                accessType = GetSpecialType(SpecialType.System_Nullable_T, diagnostics, node).Construct(accessType);
            }

            return new BoundConditionalAccess(node, receiver, access, accessType);
        }

        private BoundConditionalAccess GenerateBadConditionalAccessNodeError(ConditionalAccessExpressionSyntax node, BoundExpression receiver, BoundExpression access, DiagnosticBag diagnostics)
        {
            var operatorToken = node.OperatorToken;
            // TODO: need a special ERR for this.
            //       conditional access is not really a binary operator.
            DiagnosticInfo diagnosticInfo = new CSDiagnosticInfo(ErrorCode.ERR_BadUnaryOp, SyntaxFacts.GetText(operatorToken.CSharpKind()), access.Display);
            diagnostics.Add(new CSDiagnostic(diagnosticInfo, operatorToken.GetLocation()));
            access = BadExpression(access.Syntax, access);

            return new BoundConditionalAccess(node, receiver, access, CreateErrorType(), hasErrors: true);
        }

        private BoundExpression BindMemberBindingExpression(MemberBindingExpressionSyntax node, bool invoked, bool indexed, DiagnosticBag diagnostics)
        {
            BoundExpression receiver = GetReceiverForConditionalAccessor(node, diagnostics);

            var memberAccess = BindMemberAccessWithBoundLeft(node, receiver, node.Name, node.OperatorToken, invoked, indexed, diagnostics);
            return memberAccess;
        }

        private BoundExpression BindElementBindingExpression(ElementBindingExpressionSyntax node, bool invoked, bool indexed, DiagnosticBag diagnostics)
        {
            BoundExpression receiver = GetReceiverForConditionalAccessor(node, diagnostics);

            var memberAccess = BindElementAccess(node, receiver, node.ArgumentList, diagnostics);
            return memberAccess;
        }

        private BoundExpression GetReceiverForConditionalAccessor(ExpressionSyntax node, DiagnosticBag diagnostics)
        {
            BoundExpression receiver = null;

            var conditionalAccessNode = node.Parent;
            for (; conditionalAccessNode.Kind != SyntaxKind.ConditionalAccessExpression; conditionalAccessNode = conditionalAccessNode.Parent) { };

            var currentBinder = this;
            while (currentBinder != null)
            {
                var conditionalAccessBinder = currentBinder as BinderWithConditionalReceiver;
                if (conditionalAccessBinder != null)
                {
                    receiver = conditionalAccessBinder.receiverExpression;
                    break;
                }
                currentBinder = currentBinder.Next;
            }

            if (receiver == null || receiver.Syntax != ((ConditionalAccessExpressionSyntax)conditionalAccessNode).Expression)
            {
                // this can happen when semantic model binds parts of a Call or a broken access expression. 
                // We may not have receiver available in such cases.
                // Not a problem - we only need receiver to get its type and we can bind it here.
                receiver = BindConditionalAccessReceiver((ConditionalAccessExpressionSyntax)conditionalAccessNode, diagnostics);
            }

            if (receiver.HasAnyErrors)
            {
                return receiver;
            }

            // create surrogate receiver
            var receiverType = receiver.Type;
            if (receiverType != null && receiverType.IsNullableType())
            {
                receiverType = receiverType.GetNullableUnderlyingType();
            }

            receiver = new BoundConditionalReceiver(receiver.Syntax, receiverType);
            return receiver;
        }

        private BoundExpression BindConditionalAccessReceiver(ConditionalAccessExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var receiverSyntax = node.Expression;
            var receiver = BindValue(receiverSyntax, diagnostics, BindValueKind.RValue);
            receiver = MakeMemberAccessValue(receiver, diagnostics);

            if (receiver.HasAnyErrors)
            {
                return receiver;
            }

            var operatorToken = node.OperatorToken;

            if (receiver.Kind == BoundKind.UnboundLambda)
            {
                var msgId = ((UnboundLambda)receiver).MessageID;
                DiagnosticInfo diagnosticInfo = new CSDiagnosticInfo(ErrorCode.ERR_BadUnaryOp, SyntaxFacts.GetText(operatorToken.CSharpKind()), msgId.Localize());
                diagnostics.Add(new CSDiagnostic(diagnosticInfo, node.Location));
                return BadExpression(receiverSyntax, receiver);
            }

            var receiverType = receiver.Type;

            // Can't dot into the null literal or anything that has no type
            if (receiverType == null)
            {
                Error(diagnostics, ErrorCode.ERR_BadUnaryOp, node, operatorToken.Text, receiver.Display);
                return BadExpression(node, receiver);
            }

            // receiver cannot be "base"
            if (receiverType == null)
            {
                Error(diagnostics, ErrorCode.ERR_BadUnaryOp, node, operatorToken.Text, receiver.Display);
                return BadExpression(node, receiver);
            }

            // No member accesses on void
            if (receiverType != null && receiverType.SpecialType == SpecialType.System_Void)
            {
                DiagnosticInfo diagnosticInfo = new CSDiagnosticInfo(ErrorCode.ERR_BadUnaryOp, SyntaxFacts.GetText(operatorToken.CSharpKind()), receiverType);
                diagnostics.Add(new CSDiagnostic(diagnosticInfo, operatorToken.GetLocation()));
                return BadExpression(receiverSyntax, receiver);
            }

            if (receiverType != null && receiverType.IsValueType && !receiverType.IsNullableType())
            {
                // must be nullable or reference type
                DiagnosticInfo diagnosticInfo = new CSDiagnosticInfo(ErrorCode.ERR_BadUnaryOp, SyntaxFacts.GetText(operatorToken.CSharpKind()), receiverType);
                diagnostics.Add(new CSDiagnostic(diagnosticInfo, operatorToken.GetLocation()));
                return BadExpression(receiverSyntax, receiver);
            }

            return receiver;
        }
    }
}
