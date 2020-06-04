﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    /// This portion of the binder converts an <see cref="ExpressionSyntax"/> into a <see cref="BoundExpression"/>.
    /// </summary>
    internal partial class Binder
    {
        private BoundExpression BindMethodGroup(ExpressionSyntax node, bool invoked, bool indexed, DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.IdentifierName:
                case SyntaxKind.GenericName:
                    return BindIdentifier((SimpleNameSyntax)node, invoked, indexed, diagnostics);
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
        /// <param name="allowUnexpandedForm">False to prevent selecting a params method in unexpanded form.</param>
        /// <returns>Synthesized method invocation expression.</returns>
        internal BoundExpression MakeInvocationExpression(
            SyntaxNode node,
            BoundExpression receiver,
            string methodName,
            ImmutableArray<BoundExpression> args,
            DiagnosticBag diagnostics,
            SeparatedSyntaxList<TypeSyntax> typeArgsSyntax = default(SeparatedSyntaxList<TypeSyntax>),
            ImmutableArray<TypeWithAnnotations> typeArgs = default(ImmutableArray<TypeWithAnnotations>),
            CSharpSyntaxNode queryClause = null,
            bool allowFieldsAndProperties = false,
            bool allowUnexpandedForm = true)
        {
            Debug.Assert(receiver != null);

            receiver = BindToNaturalType(receiver, diagnostics);
            var boundExpression = BindInstanceMemberAccess(node, node, receiver, methodName, typeArgs.NullToEmpty().Length, typeArgsSyntax, typeArgs, invoked: true, indexed: false, diagnostics);

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
            Debug.Assert(!args.Any(e => e.Kind == BoundKind.OutVariablePendingInference ||
                                        e.Kind == BoundKind.OutDeconstructVarPendingInference ||
                                        e.Kind == BoundKind.DiscardExpression && !e.HasExpressionType()));
            analyzedArguments.Arguments.AddRange(args);
            BoundExpression result = BindInvocationExpression(
                node, node, methodName, boundExpression, analyzedArguments, diagnostics, queryClause,
                allowUnexpandedForm: allowUnexpandedForm);

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
            if (TryBindNameofOperator(node, diagnostics, out result))
            {
                return result; // all of the binding is done by BindNameofOperator
            }

            // M(__arglist()) is legal, but M(__arglist(__arglist()) is not!
            bool isArglist = node.Expression.Kind() == SyntaxKind.ArgListExpression;
            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();

            if (isArglist)
            {
                BindArgumentsAndNames(node.ArgumentList, diagnostics, analyzedArguments, allowArglist: false);
                result = BindArgListOperator(node, diagnostics, analyzedArguments);
            }
            else
            {
                BoundExpression boundExpression = BindMethodGroup(node.Expression, invoked: true, indexed: false, diagnostics: diagnostics);
                boundExpression = CheckValue(boundExpression, BindValueKind.RValueOrMethodGroup, diagnostics);
                string name = boundExpression.Kind == BoundKind.MethodGroup ? GetName(node.Expression) : null;
                BindArgumentsAndNames(node.ArgumentList, diagnostics, analyzedArguments, allowArglist: true);
                result = BindInvocationExpression(node, node.Expression, name, boundExpression, analyzedArguments, diagnostics);
            }

            analyzedArguments.Free();
            return result;
        }

        private BoundExpression BindArgListOperator(InvocationExpressionSyntax node, DiagnosticBag diagnostics, AnalyzedArguments analyzedArguments)
        {
            bool hasErrors = analyzedArguments.HasErrors;

            // We allow names, oddly enough; M(__arglist(x : 123)) is legal. We just ignore them.
            TypeSymbol objType = GetSpecialType(SpecialType.System_Object, diagnostics, node);
            for (int i = 0; i < analyzedArguments.Arguments.Count; ++i)
            {
                BoundExpression argument = analyzedArguments.Arguments[i];

                if (argument.Kind == BoundKind.OutVariablePendingInference)
                {
                    analyzedArguments.Arguments[i] = ((OutVariablePendingInference)argument).FailInference(this, diagnostics);
                }
                else if ((object)argument.Type == null && !argument.HasAnyErrors)
                {
                    // We are going to need every argument in here to have a type. If we don't have one,
                    // try converting it to object. We'll either succeed (if it is a null literal)
                    // or fail with a good error message.
                    //
                    // Note that the native compiler converts null literals to object, and for everything
                    // else it either crashes, or produces nonsense code. Roslyn improves upon this considerably.

                    analyzedArguments.Arguments[i] = GenerateConversionForAssignment(objType, argument, diagnostics);
                }
                else if (argument.Type.IsVoidType())
                {
                    Error(diagnostics, ErrorCode.ERR_CantUseVoidInArglist, argument.Syntax);
                    hasErrors = true;
                }
                else if (analyzedArguments.RefKind(i) == RefKind.None)
                {
                    analyzedArguments.Arguments[i] = BindToNaturalType(analyzedArguments.Arguments[i], diagnostics);
                }

                switch (analyzedArguments.RefKind(i))
                {
                    case RefKind.None:
                    case RefKind.Ref:
                        break;
                    default:
                        // Disallow "in" or "out" arguments
                        Error(diagnostics, ErrorCode.ERR_CantUseInOrOutInArglist, argument.Syntax);
                        hasErrors = true;
                        break;
                }
            }

            ImmutableArray<BoundExpression> arguments = analyzedArguments.Arguments.ToImmutable();
            ImmutableArray<RefKind> refKinds = analyzedArguments.RefKinds.ToImmutableOrNull();
            return new BoundArgListOperator(node, arguments, refKinds, null, hasErrors);
        }

        /// <summary>
        /// Bind an expression as a method invocation.
        /// </summary>
        private BoundExpression BindInvocationExpression(
            SyntaxNode node,
            SyntaxNode expression,
            string methodName,
            BoundExpression boundExpression,
            AnalyzedArguments analyzedArguments,
            DiagnosticBag diagnostics,
            CSharpSyntaxNode queryClause = null,
            bool allowUnexpandedForm = true)
        {
            BoundExpression result;
            NamedTypeSymbol delegateType;

            if ((object)boundExpression.Type != null && boundExpression.Type.IsDynamic())
            {
                // Either we have a dynamic method group invocation "dyn.M(...)" or 
                // a dynamic delegate invocation "dyn(...)" -- either way, bind it as a dynamic
                // invocation and let the lowering pass sort it out.
                ReportSuppressionIfNeeded(boundExpression, diagnostics);
                result = BindDynamicInvocation(node, boundExpression, analyzedArguments, ImmutableArray<MethodSymbol>.Empty, diagnostics, queryClause);
            }
            else if (boundExpression.Kind == BoundKind.MethodGroup)
            {
                ReportSuppressionIfNeeded(boundExpression, diagnostics);
                result = BindMethodGroupInvocation(
                    node, expression, methodName, (BoundMethodGroup)boundExpression, analyzedArguments,
                    diagnostics, queryClause, allowUnexpandedForm: allowUnexpandedForm, anyApplicableCandidates: out _);
            }
            else if ((object)(delegateType = GetDelegateType(boundExpression)) != null)
            {
                if (ReportDelegateInvokeUseSiteDiagnostic(diagnostics, delegateType, node: node))
                {
                    return CreateBadCall(node, boundExpression, LookupResultKind.Viable, analyzedArguments);
                }

                result = BindDelegateInvocation(node, expression, methodName, boundExpression, analyzedArguments, diagnostics, queryClause, delegateType);
            }
            else if (boundExpression.Type?.Kind == SymbolKind.FunctionPointer)
            {
                ReportSuppressionIfNeeded(boundExpression, diagnostics);
                result = BindFunctionPointerInvocation(node, boundExpression, analyzedArguments, diagnostics);
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
            SyntaxNode node,
            BoundExpression expression,
            AnalyzedArguments arguments,
            ImmutableArray<MethodSymbol> applicableMethods,
            DiagnosticBag diagnostics,
            CSharpSyntaxNode queryClause)
        {
            CheckNamedArgumentsForDynamicInvocation(arguments, diagnostics);

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
                            // Can't call the HasThis method due to EE doing odd things with containing member and its containing type.
                            if ((InConstructorInitializer || InFieldInitializer) && receiver.WasCompilerGenerated)
                            {
                                // Only a static method can be called in a constructor initializer. If we were not in a ctor initializer
                                // the runtime binder would ignore the receiver, but in a ctor initializer we can't read "this" before 
                                // the base constructor is called. We need to handle this as a type qualified static method call.
                                // Also applicable to things like field initializers, which run before the ctor initializer.
                                expression = methodGroup.Update(
                                    methodGroup.TypeArgumentsOpt,
                                    methodGroup.Name,
                                    methodGroup.Methods,
                                    methodGroup.LookupSymbolOpt,
                                    methodGroup.LookupError,
                                    methodGroup.Flags & ~BoundMethodGroupFlags.HasImplicitReceiver,
                                    receiverOpt: new BoundTypeExpression(node, null, this.ContainingType).MakeCompilerGenerated(),
                                    resultKind: methodGroup.ResultKind);
                            }

                            break;

                        case BoundKind.TypeOrValueExpression:
                            var typeOrValue = (BoundTypeOrValueExpression)receiver;

                            // Unfortunately, the runtime binder doesn't have APIs that would allow us to pass both "type or value".
                            // Ideally the runtime binder would choose between type and value based on the result of the overload resolution.
                            // We need to pick one or the other here. Dev11 compiler passes the type only if the value can't be accessed.
                            bool inStaticContext;
                            bool useType = IsInstance(typeOrValue.Data.ValueSymbol) && !HasThis(isExplicit: false, inStaticContext: out inStaticContext);

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
            else
            {
                expression = BindToNaturalType(expression, diagnostics);
            }

            ImmutableArray<BoundExpression> argArray = BuildArgumentsForDynamicInvocation(arguments, diagnostics);
            var refKindsArray = arguments.RefKinds.ToImmutableOrNull();

            hasErrors &= ReportBadDynamicArguments(node, argArray, refKindsArray, diagnostics, queryClause);

            return new BoundDynamicInvocation(
                node,
                arguments.GetNames(),
                refKindsArray,
                applicableMethods,
                expression,
                argArray,
                type: Compilation.DynamicType,
                hasErrors: hasErrors);
        }

        private void CheckNamedArgumentsForDynamicInvocation(AnalyzedArguments arguments, DiagnosticBag diagnostics)
        {
            if (arguments.Names.Count == 0)
            {
                return;
            }

            if (!Compilation.LanguageVersion.AllowNonTrailingNamedArguments())
            {
                return;
            }

            bool seenName = false;
            for (int i = 0; i < arguments.Names.Count; i++)
            {
                if (arguments.Names[i] != null)
                {
                    seenName = true;
                }
                else if (seenName)
                {
                    Error(diagnostics, ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgumentInDynamicInvocation, arguments.Arguments[i].Syntax);
                    return;
                }
            }
        }

        private ImmutableArray<BoundExpression> BuildArgumentsForDynamicInvocation(AnalyzedArguments arguments, DiagnosticBag diagnostics)
        {
            var builder = ArrayBuilder<BoundExpression>.GetInstance(arguments.Arguments.Count);
            builder.AddRange(arguments.Arguments);
            for (int i = 0, n = builder.Count; i < n; i++)
            {
                builder[i] = builder[i] switch
                {
                    OutVariablePendingInference outvar => outvar.FailInference(this, diagnostics),
                    BoundDiscardExpression discard when !discard.HasExpressionType() => discard.FailInference(this, diagnostics),
                    var arg => BindToNaturalType(arg, diagnostics)
                };
            }

            return builder.ToImmutableAndFree();
        }

        // Returns true if there were errors.
        private static bool ReportBadDynamicArguments(
            SyntaxNode node,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> refKinds,
            DiagnosticBag diagnostics,
            CSharpSyntaxNode queryClause)
        {
            bool hasErrors = false;
            bool reportedBadQuery = false;

            if (!refKinds.IsDefault)
            {
                for (int argIndex = 0; argIndex < refKinds.Length; argIndex++)
                {
                    if (refKinds[argIndex] == RefKind.In)
                    {
                        Error(diagnostics, ErrorCode.ERR_InDynamicMethodArg, arguments[argIndex].Syntax);
                        hasErrors = true;
                    }
                }
            }

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
            SyntaxNode node,
            SyntaxNode expression,
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
            OverloadResolution.MethodInvocationOverloadResolution(
                methods: methodGroup.Methods,
                typeArguments: methodGroup.TypeArguments,
                receiver: methodGroup.Receiver,
                arguments: analyzedArguments,
                result: overloadResolutionResult,
                useSiteDiagnostics: ref useSiteDiagnostics);
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
            SyntaxNode syntax,
            SyntaxNode expression,
            string methodName,
            BoundMethodGroup methodGroup,
            AnalyzedArguments analyzedArguments,
            DiagnosticBag diagnostics,
            CSharpSyntaxNode queryClause,
            bool allowUnexpandedForm,
            out bool anyApplicableCandidates)
        {
            BoundExpression result;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var resolution = this.ResolveMethodGroup(
                methodGroup, expression, methodName, analyzedArguments, isMethodGroupConversion: false,
                useSiteDiagnostics: ref useSiteDiagnostics, allowUnexpandedForm: allowUnexpandedForm);
            diagnostics.Add(expression, useSiteDiagnostics);
            anyApplicableCandidates = resolution.ResultKind == LookupResultKind.Viable && resolution.OverloadResolutionResult.HasAnyApplicableMember;

            if (!methodGroup.HasAnyErrors) diagnostics.AddRange(resolution.Diagnostics); // Suppress cascading.

            if (resolution.HasAnyErrors)
            {
                ImmutableArray<MethodSymbol> originalMethods;
                LookupResultKind resultKind;
                ImmutableArray<TypeWithAnnotations> typeArguments;
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
                        result = BindInvocationExpressionContinued(
                            syntax, expression, methodName, resolution.OverloadResolutionResult, resolution.AnalyzedArguments,
                            resolution.MethodGroup, delegateTypeOpt: null, diagnostics: discarded, queryClause: queryClause);
                        discarded.Free();
                    }

                    // Since the resolution is non-empty and has no diagnostics, the LookupResultKind in its MethodGroup is uninteresting.
                    result = CreateBadCall(syntax, methodGroup, methodGroup.ResultKind, analyzedArguments);
                }
                else
                {
                    // If overload resolution found one or more applicable methods and at least one argument
                    // was dynamic then treat this as a dynamic call.
                    if (resolution.AnalyzedArguments.HasDynamicArgument &&
                        resolution.OverloadResolutionResult.HasAnyApplicableMember)
                    {
                        if (resolution.IsLocalFunctionInvocation)
                        {
                            result = BindLocalFunctionInvocationWithDynamicArgument(
                                syntax, expression, methodName, methodGroup,
                                diagnostics, queryClause, resolution);
                        }
                        else if (resolution.IsExtensionMethodGroup)
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
                                // warning CS1974: The dynamically dispatched call to method 'Goo' may fail at runtime
                                // because one or more applicable overloads are conditional methods
                                Error(diagnostics, ErrorCode.WRN_DynamicDispatchToConditionalMethod, syntax, methodGroup.Name);
                            }

                            // Note that the runtime binder may consider candidates that haven't passed compile-time final validation 
                            // and an ambiguity error may be reported. Also additional checks are performed in runtime final validation 
                            // that are not performed at compile-time.
                            // Only if the set of final applicable candidates is empty we know for sure the call will fail at runtime.
                            var finalApplicableCandidates = GetCandidatesPassingFinalValidation(syntax, resolution.OverloadResolutionResult,
                                                                                                methodGroup.ReceiverOpt,
                                                                                                methodGroup.TypeArgumentsOpt,
                                                                                                diagnostics);
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
                        result = BindInvocationExpressionContinued(
                            syntax, expression, methodName, resolution.OverloadResolutionResult, resolution.AnalyzedArguments,
                            resolution.MethodGroup, delegateTypeOpt: null, diagnostics: diagnostics, queryClause: queryClause);
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

        private BoundExpression BindLocalFunctionInvocationWithDynamicArgument(
            SyntaxNode syntax,
            SyntaxNode expression,
            string methodName,
            BoundMethodGroup boundMethodGroup,
            DiagnosticBag diagnostics,
            CSharpSyntaxNode queryClause,
            MethodGroupResolution resolution)
        {
            // Invocations of local functions with dynamic arguments don't need
            // to be dispatched as dynamic invocations since they cannot be
            // overloaded. Instead, we'll just emit a standard call with
            // dynamic implicit conversions for any dynamic arguments. There
            // are two exceptions: "params", and unconstructed generics. While
            // implementing those cases with dynamic invocations is possible,
            // we have decided the implementation complexity is not worth it.
            // Refer to the comments below for the exact semantics.

            Debug.Assert(resolution.IsLocalFunctionInvocation);
            Debug.Assert(resolution.OverloadResolutionResult.Succeeded);
            Debug.Assert(queryClause == null);

            var validResult = resolution.OverloadResolutionResult.ValidResult;
            var args = resolution.AnalyzedArguments.Arguments.ToImmutable();
            var refKindsArray = resolution.AnalyzedArguments.RefKinds.ToImmutableOrNull();

            ReportBadDynamicArguments(syntax, args, refKindsArray, diagnostics, queryClause);

            var localFunction = validResult.Member;
            var methodResult = validResult.Result;

            // We're only in trouble if a dynamic argument is passed to the
            // params parameter and is ambiguous at compile time between normal
            // and expanded form i.e., there is exactly one dynamic argument to
            // a params parameter
            // See https://github.com/dotnet/roslyn/issues/10708
            if (OverloadResolution.IsValidParams(localFunction) &&
                methodResult.Kind == MemberResolutionKind.ApplicableInNormalForm)
            {
                var parameters = localFunction.Parameters;

                Debug.Assert(parameters.Last().IsParams);

                var lastParamIndex = parameters.Length - 1;

                for (int i = 0; i < args.Length; ++i)
                {
                    var arg = args[i];
                    if (arg.HasDynamicType() &&
                        methodResult.ParameterFromArgument(i) == lastParamIndex)
                    {
                        Error(diagnostics,
                            ErrorCode.ERR_DynamicLocalFunctionParamsParameter,
                            syntax, parameters.Last().Name, localFunction.Name);
                        return BindDynamicInvocation(
                            syntax,
                            boundMethodGroup,
                            resolution.AnalyzedArguments,
                            resolution.OverloadResolutionResult.GetAllApplicableMembers(),
                            diagnostics,
                            queryClause);
                    }
                }
            }

            // If we call an unconstructed generic local function with a
            // dynamic argument in a place where it influences the type
            // parameters, we need to dynamically dispatch the call (as the
            // function must be constructed at runtime). We cannot do that, so
            // disallow that. However, doing a specific analysis of each
            // argument and its corresponding parameter to check if it's
            // generic (and allow dynamic in non-generic parameters) may break
            // overload resolution in the future, if we ever allow overloaded
            // local functions. So, just disallow any mixing of dynamic and
            // inferred generics. (Explicit generic arguments are fine)
            // See https://github.com/dotnet/roslyn/issues/21317
            if (boundMethodGroup.TypeArgumentsOpt.IsDefaultOrEmpty && localFunction.IsGenericMethod)
            {
                Error(diagnostics,
                    ErrorCode.ERR_DynamicLocalFunctionTypeParameter,
                    syntax, localFunction.Name);
                return BindDynamicInvocation(
                    syntax,
                    boundMethodGroup,
                    resolution.AnalyzedArguments,
                    resolution.OverloadResolutionResult.GetAllApplicableMembers(),
                    diagnostics,
                    queryClause);
            }

            return BindInvocationExpressionContinued(
                node: syntax,
                expression: expression,
                methodName: methodName,
                result: resolution.OverloadResolutionResult,
                analyzedArguments: resolution.AnalyzedArguments,
                methodGroup: resolution.MethodGroup,
                delegateTypeOpt: null,
                diagnostics: diagnostics,
                queryClause: queryClause);
        }

        private ImmutableArray<TMethodOrPropertySymbol> GetCandidatesPassingFinalValidation<TMethodOrPropertySymbol>(
            SyntaxNode syntax,
            OverloadResolutionResult<TMethodOrPropertySymbol> overloadResolutionResult,
            BoundExpression receiverOpt,
            ImmutableArray<TypeWithAnnotations> typeArgumentsOpt,
            DiagnosticBag diagnostics) where TMethodOrPropertySymbol : Symbol
        {
            Debug.Assert(overloadResolutionResult.HasAnyApplicableMember);

            var finalCandidates = ArrayBuilder<TMethodOrPropertySymbol>.GetInstance();
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
                    //   or a member-access whose receiver can't be classified as a type or value until after overload resolution (see §7.6.4.1). 
                    // * If F is an instance method, the method group must have resulted from a simple-name, a member-access through a variable or value, 
                    //   or a member-access whose receiver can't be classified as a type or value until after overload resolution (see §7.6.4.1).

                    if (!MemberGroupFinalValidationAccessibilityChecks(receiverOpt, result.Member, syntax, candidateDiagnostics, invokedAsExtensionMethod: false) &&
                        (typeArgumentsOpt.IsDefault || ((MethodSymbol)(object)result.Member).CheckConstraints(this.Conversions, syntax, this.Compilation, candidateDiagnostics)))
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

        private void CheckRestrictedTypeReceiver(BoundExpression expression, CSharpCompilation compilation, DiagnosticBag diagnostics)
        {
            Debug.Assert(diagnostics != null);

            // It is never legal to box a restricted type, even if we are boxing it as the receiver
            // of a method call. When must be box? We skip boxing when the method in question is defined
            // on the restricted type or overridden by the restricted type.
            switch (expression.Kind)
            {
                case BoundKind.Call:
                    {
                        var call = (BoundCall)expression;
                        if (!call.HasAnyErrors && call.ReceiverOpt != null && (object)call.ReceiverOpt.Type != null)
                        {
                            // error CS0029: Cannot implicitly convert type 'A' to 'B'

                            // Case 1: receiver is a restricted type, and method called is defined on a parent type
                            if (call.ReceiverOpt.Type.IsRestrictedType() && !TypeSymbol.Equals(call.Method.ContainingType, call.ReceiverOpt.Type, TypeCompareKind.ConsiderEverything2))
                            {
                                SymbolDistinguisher distinguisher = new SymbolDistinguisher(compilation, call.ReceiverOpt.Type, call.Method.ContainingType);
                                Error(diagnostics, ErrorCode.ERR_NoImplicitConv, call.ReceiverOpt.Syntax, distinguisher.First, distinguisher.Second);
                            }
                            // Case 2: receiver is a base reference, and the child type is restricted
                            else if (call.ReceiverOpt.Kind == BoundKind.BaseReference && this.ContainingType.IsRestrictedType())
                            {
                                SymbolDistinguisher distinguisher = new SymbolDistinguisher(compilation, this.ContainingType, call.Method.ContainingType);
                                Error(diagnostics, ErrorCode.ERR_NoImplicitConv, call.ReceiverOpt.Syntax, distinguisher.First, distinguisher.Second);
                            }
                        }
                    }
                    break;
                case BoundKind.DynamicInvocation:
                    {
                        var dynInvoke = (BoundDynamicInvocation)expression;
                        if (!dynInvoke.HasAnyErrors &&
                            (object)dynInvoke.Expression.Type != null &&
                            dynInvoke.Expression.Type.IsRestrictedType())
                        {
                            // eg: b = typedReference.Equals(dyn);
                            // error CS1978: Cannot use an expression of type 'TypedReference' as an argument to a dynamically dispatched operation
                            Error(diagnostics, ErrorCode.ERR_BadDynamicMethodArg, dynInvoke.Expression.Syntax, dynInvoke.Expression.Type);
                        }
                    }
                    break;
                case BoundKind.FunctionPointerInvocation:
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(expression.Kind);
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
            SyntaxNode node,
            SyntaxNode expression,
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
                if (analyzedArguments.HasErrors)
                {
                    // Errors for arguments have already been reported, except for unbound lambdas and switch expressions.
                    // We report those now.
                    foreach (var argument in analyzedArguments.Arguments)
                    {
                        switch (argument)
                        {
                            case UnboundLambda unboundLambda:
                                var boundWithErrors = unboundLambda.BindForErrorRecovery();
                                diagnostics.AddRange(boundWithErrors.Diagnostics);
                                break;
                            case BoundUnconvertedObjectCreationExpression _:
                            case BoundTupleLiteral _:
                                // Tuple literals can contain unbound lambdas or switch expressions.
                                _ = BindToNaturalType(argument, diagnostics);
                                break;
                            case BoundUnconvertedSwitchExpression { Type: { } naturalType } switchExpr:
                                _ = ConvertSwitchExpression(switchExpr, naturalType ?? CreateErrorType(), conversionIfTargetTyped: null, diagnostics);
                                break;
                        }
                    }
                }
                else
                {
                    // Since there were no argument errors to report, we report an error on the invocation itself.
                    string name = (object)delegateTypeOpt == null ? methodName : null;
                    result.ReportDiagnostics(
                        binder: this, location: GetLocationForOverloadResolutionDiagnostic(node, expression), nodeOpt: node, diagnostics: diagnostics, name: name,
                        receiver: methodGroup.Receiver, invokedExpression: expression, arguments: analyzedArguments, memberGroup: methodGroup.Methods.ToImmutable(),
                        typeContainingConstructor: null, delegateTypeBeingInvoked: delegateTypeOpt, queryClause: queryClause);
                }

                return CreateBadCall(node, methodGroup.Name, invokedAsExtensionMethod && analyzedArguments.Arguments.Count > 0 && (object)methodGroup.Receiver == (object)analyzedArguments.Arguments[0] ? null : methodGroup.Receiver,
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

            var receiver = ReplaceTypeOrValueReceiver(methodGroup.Receiver, !method.RequiresInstanceReceiver && !invokedAsExtensionMethod, diagnostics);

            // Note: we specifically want to do final validation (7.6.5.1) without checking delegate compatibility (15.2),
            // so we're calling MethodGroupFinalValidation directly, rather than via MethodGroupConversionHasErrors.
            // Note: final validation wants the receiver that corresponds to the source representation
            // (i.e. the first argument, if invokedAsExtensionMethod).
            var gotError = MemberGroupFinalValidation(receiver, method, expression, diagnostics, invokedAsExtensionMethod);

            CheckImplicitThisCopyInReadOnlyMember(receiver, method, diagnostics);

            if (invokedAsExtensionMethod)
            {
                BoundExpression receiverArgument = analyzedArguments.Argument(0);
                ParameterSymbol receiverParameter = method.Parameters.First();

                // we will have a different receiver if ReplaceTypeOrValueReceiver has unwrapped TypeOrValue
                if ((object)receiver != receiverArgument)
                {
                    // Because the receiver didn't pass through CoerceArguments, we need to apply an appropriate conversion here.
                    Debug.Assert(argsToParams.IsDefault || argsToParams[0] == 0);
                    receiverArgument = CreateConversion(receiver, methodResult.Result.ConversionForArg(0),
                        receiverParameter.Type, diagnostics);
                }

                if (receiverParameter.RefKind == RefKind.Ref)
                {
                    // If this was a ref extension method, receiverArgument must be checked for L-value constraints.
                    // This helper method will also replace it with a BoundBadExpression if it was invalid.
                    receiverArgument = CheckValue(receiverArgument, BindValueKind.RefOrOut, diagnostics);

                    if (analyzedArguments.RefKinds.Count == 0)
                    {
                        analyzedArguments.RefKinds.Count = analyzedArguments.Arguments.Count;
                    }

                    // receiver of a `ref` extension method is a `ref` argument. (and we have checked above that it can be passed as a Ref)
                    // we need to adjust the argument refkind as if we had a `ref` modifier in a call.
                    analyzedArguments.RefKinds[0] = RefKind.Ref;
                    CheckFeatureAvailability(receiverArgument.Syntax, MessageID.IDS_FeatureRefExtensionMethods, diagnostics);
                }
                else if (receiverParameter.RefKind == RefKind.In)
                {
                    // NB: receiver of an `in` extension method is treated as a `byval` argument, so no changes from the default refkind is needed in that case. 
                    Debug.Assert(analyzedArguments.RefKind(0) == RefKind.None);
                    CheckFeatureAvailability(receiverArgument.Syntax, MessageID.IDS_FeatureRefExtensionMethods, diagnostics);
                }

                analyzedArguments.Arguments[0] = receiverArgument;
            }

            // This will be the receiver of the BoundCall node that we create.
            // For extension methods, there is no receiver because the receiver in source was actually the first argument.
            // For instance methods, we may have synthesized an implicit this node.  We'll keep it for the emitter.
            // For static methods, we may have synthesized a type expression.  It serves no purpose, so we'll drop it.
            if (invokedAsExtensionMethod || (!method.RequiresInstanceReceiver && receiver != null && receiver.WasCompilerGenerated))
            {
                receiver = null;
            }

            var argNames = analyzedArguments.GetNames();
            var argRefKinds = analyzedArguments.RefKinds.ToImmutableOrNull();
            var args = analyzedArguments.Arguments.ToImmutable();

            if (!gotError && method.RequiresInstanceReceiver && receiver != null && receiver.Kind == BoundKind.ThisReference && receiver.WasCompilerGenerated)
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

            Debug.Assert(args.IsDefaultOrEmpty || (object)receiver != (object)args[0]);

            if (!gotError)
            {
                gotError = !CheckInvocationArgMixing(
                    node,
                    method,
                    receiver,
                    method.Parameters,
                    args,
                    argsToParams,
                    this.LocalScopeDepth,
                    diagnostics);
            }

            bool isDelegateCall = (object)delegateTypeOpt != null;
            if (!isDelegateCall)
            {
                if (method.RequiresInstanceReceiver)
                {
                    WarnOnAccessOfOffDefault(node.Kind() == SyntaxKind.InvocationExpression ?
                                                ((InvocationExpressionSyntax)node).Expression :
                                                node,
                                             receiver,
                                             diagnostics);
                }
            }

            return new BoundCall(node, receiver, method, args, argNames, argRefKinds, isDelegateCall: isDelegateCall,
                        expanded: expanded, invokedAsExtensionMethod: invokedAsExtensionMethod,
                        argsToParamsOpt: argsToParams, resultKind: LookupResultKind.Viable, binderOpt: this, type: returnType, hasErrors: gotError);
        }

        /// <summary>
        /// Returns false if an implicit 'this' copy will occur due to an instance member invocation in a readonly member.
        /// </summary>
        internal bool CheckImplicitThisCopyInReadOnlyMember(BoundExpression receiver, MethodSymbol method, DiagnosticBag diagnostics)
        {
            // For now we are warning only in implicit copy scenarios that are only possible with readonly members.
            // Eventually we will warn on implicit value copies in more scenarios. See https://github.com/dotnet/roslyn/issues/33968.
            if (receiver is BoundThisReference &&
                receiver.Type.IsValueType &&
                ContainingMemberOrLambda is MethodSymbol containingMethod &&
                containingMethod.IsEffectivelyReadOnly &&
                // Ignore calls to base members.
                TypeSymbol.Equals(containingMethod.ContainingType, method.ContainingType, TypeCompareKind.ConsiderEverything) &&
                !method.IsEffectivelyReadOnly &&
                method.RequiresInstanceReceiver)
            {
                Error(diagnostics, ErrorCode.WRN_ImplicitCopyInReadOnlyMember, receiver.Syntax, method, ThisParameterSymbol.SymbolName);
                return false;
            }

            return true;
        }

        /// <param name="node">Invocation syntax node.</param>
        /// <param name="expression">The syntax for the invoked method, including receiver.</param>
        private static Location GetLocationForOverloadResolutionDiagnostic(SyntaxNode node, SyntaxNode expression)
        {
            if (node != expression)
            {
                switch (expression.Kind())
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
        /// or a value (if useType is false).  Any other node is bound to its natural type.
        /// </summary>
        /// <remarks>
        /// Call this once overload resolution has succeeded on the method group of which the BoundTypeOrValueExpression
        /// is the receiver.  Generally, useType will be true if the chosen method is static and false otherwise.
        /// </remarks>
        private BoundExpression ReplaceTypeOrValueReceiver(BoundExpression receiver, bool useType, DiagnosticBag diagnostics)
        {
            if ((object)receiver == null)
            {
                return null;
            }

            switch (receiver.Kind)
            {
                case BoundKind.TypeOrValueExpression:
                    var typeOrValue = (BoundTypeOrValueExpression)receiver;
                    if (useType)
                    {
                        diagnostics.AddRange(typeOrValue.Data.TypeDiagnostics);
                        return typeOrValue.Data.TypeExpression;
                    }
                    else
                    {
                        diagnostics.AddRange(typeOrValue.Data.ValueDiagnostics);
                        return CheckValue(typeOrValue.Data.ValueExpression, BindValueKind.RValue, diagnostics);
                    }

                case BoundKind.QueryClause:
                    // a query clause may wrap a TypeOrValueExpression.
                    var q = (BoundQueryClause)receiver;
                    var value = q.Value;
                    var replaced = ReplaceTypeOrValueReceiver(value, useType, diagnostics);
                    return (value == replaced) ? q : q.Update(replaced, q.DefinedSymbol, q.Operation, q.Cast, q.Binder, q.UnoptimizedForm, q.Type);

                default:
                    return BindToNaturalType(receiver, diagnostics);
            }
        }

        /// <summary>
        /// Return the delegate type if this expression represents a delegate.
        /// </summary>
        private static NamedTypeSymbol GetDelegateType(BoundExpression expr)
        {
            if ((object)expr != null && expr.Kind != BoundKind.TypeExpression)
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
            SyntaxNode node,
            string name,
            BoundExpression receiver,
            ImmutableArray<MethodSymbol> methods,
            LookupResultKind resultKind,
            ImmutableArray<TypeWithAnnotations> typeArgumentsWithAnnotations,
            AnalyzedArguments analyzedArguments,
            bool invokedAsExtensionMethod,
            bool isDelegate)
        {
            MethodSymbol method;
            ImmutableArray<BoundExpression> args;
            if (!typeArgumentsWithAnnotations.IsDefaultOrEmpty)
            {
                var constructedMethods = ArrayBuilder<MethodSymbol>.GetInstance();
                foreach (var m in methods)
                {
                    constructedMethods.Add(m.ConstructedFrom == m && m.Arity == typeArgumentsWithAnnotations.Length ? m.Construct(typeArgumentsWithAnnotations) : m);
                }

                methods = constructedMethods.ToImmutableAndFree();
            }

            if (methods.Length == 1 && !IsUnboundGeneric(methods[0]))
            {
                method = methods[0];
            }
            else
            {
                var returnType = GetCommonTypeOrReturnType(methods) ?? new ExtendedErrorTypeSymbol(this.Compilation, string.Empty, arity: 0, errorInfo: null);
                var methodContainer = (object)receiver != null && (object)receiver.Type != null
                    ? receiver.Type
                    : this.ContainingType;
                method = new ErrorMethodSymbol(methodContainer, returnType, name);
            }

            args = BuildArgumentsForErrorRecovery(analyzedArguments, methods);
            var argNames = analyzedArguments.GetNames();
            var argRefKinds = analyzedArguments.RefKinds.ToImmutableOrNull();
            receiver = BindToTypeForErrorRecovery(receiver);
            return BoundCall.ErrorCall(node, receiver, method, args, argNames, argRefKinds, isDelegate, invokedAsExtensionMethod: invokedAsExtensionMethod, originalMethods: methods, resultKind: resultKind, binder: this);
        }

        private static bool IsUnboundGeneric(MethodSymbol method)
        {
            return method.IsGenericMethod && method.ConstructedFrom() == method;
        }

        // Arbitrary limit on the number of parameter lists from overload
        // resolution candidates considered when binding argument types.
        // Any additional parameter lists are ignored.
        internal const int MaxParameterListsForErrorRecovery = 10;

        private ImmutableArray<BoundExpression> BuildArgumentsForErrorRecovery(AnalyzedArguments analyzedArguments, ImmutableArray<MethodSymbol> methods)
        {
            var parameterListList = ArrayBuilder<ImmutableArray<ParameterSymbol>>.GetInstance();
            foreach (var m in methods)
            {
                if (!IsUnboundGeneric(m) && m.ParameterCount > 0)
                {
                    parameterListList.Add(m.Parameters);
                    if (parameterListList.Count == MaxParameterListsForErrorRecovery)
                    {
                        break;
                    }
                }
            }

            var result = BuildArgumentsForErrorRecovery(analyzedArguments, parameterListList);
            parameterListList.Free();
            return result;
        }

        private ImmutableArray<BoundExpression> BuildArgumentsForErrorRecovery(AnalyzedArguments analyzedArguments, ImmutableArray<PropertySymbol> properties)
        {
            var parameterListList = ArrayBuilder<ImmutableArray<ParameterSymbol>>.GetInstance();
            foreach (var p in properties)
            {
                if (p.ParameterCount > 0)
                {
                    parameterListList.Add(p.Parameters);
                    if (parameterListList.Count == MaxParameterListsForErrorRecovery)
                    {
                        break;
                    }
                }
            }

            var result = BuildArgumentsForErrorRecovery(analyzedArguments, parameterListList);
            parameterListList.Free();
            return result;
        }

        private ImmutableArray<BoundExpression> BuildArgumentsForErrorRecovery(AnalyzedArguments analyzedArguments, IEnumerable<ImmutableArray<ParameterSymbol>> parameterListList)
        {
            var discardedDiagnostics = DiagnosticBag.GetInstance();
            int argumentCount = analyzedArguments.Arguments.Count;
            ArrayBuilder<BoundExpression> newArguments = ArrayBuilder<BoundExpression>.GetInstance(argumentCount);
            newArguments.AddRange(analyzedArguments.Arguments);
            for (int i = 0; i < argumentCount; i++)
            {
                var argument = newArguments[i];
                switch (argument.Kind)
                {
                    case BoundKind.UnboundLambda:
                        {
                            // bind the argument against each applicable parameter
                            var unboundArgument = (UnboundLambda)argument;
                            foreach (var parameterList in parameterListList)
                            {
                                var parameterType = GetCorrespondingParameterType(analyzedArguments, i, parameterList);
                                if (parameterType?.Kind == SymbolKind.NamedType &&
                                    (object)parameterType.GetDelegateType() != null)
                                {
                                    var discarded = unboundArgument.Bind((NamedTypeSymbol)parameterType);
                                }
                            }

                            // replace the unbound lambda with its best inferred bound version
                            newArguments[i] = unboundArgument.BindForErrorRecovery();
                            break;
                        }
                    case BoundKind.OutVariablePendingInference:
                    case BoundKind.DiscardExpression:
                        {
                            if (argument.HasExpressionType())
                            {
                                break;
                            }

                            var candidateType = getCorrespondingParameterType(i);
                            if (argument.Kind == BoundKind.OutVariablePendingInference)
                            {
                                if ((object)candidateType == null)
                                {
                                    newArguments[i] = ((OutVariablePendingInference)argument).FailInference(this, null);
                                }
                                else
                                {
                                    newArguments[i] = ((OutVariablePendingInference)argument).SetInferredTypeWithAnnotations(TypeWithAnnotations.Create(candidateType), null);
                                }
                            }
                            else if (argument.Kind == BoundKind.DiscardExpression)
                            {
                                if ((object)candidateType == null)
                                {
                                    newArguments[i] = ((BoundDiscardExpression)argument).FailInference(this, null);
                                }
                                else
                                {
                                    newArguments[i] = ((BoundDiscardExpression)argument).SetInferredTypeWithAnnotations(TypeWithAnnotations.Create(candidateType));
                                }
                            }

                            break;
                        }
                    case BoundKind.OutDeconstructVarPendingInference:
                        {
                            newArguments[i] = ((OutDeconstructVarPendingInference)argument).FailInference(this);
                            break;
                        }
                    case BoundKind.Parameter:
                    case BoundKind.Local:
                        {
                            newArguments[i] = BindToTypeForErrorRecovery(argument);
                            break;
                        }
                    default:
                        {
                            newArguments[i] = BindToTypeForErrorRecovery(argument, getCorrespondingParameterType(i));
                            break;
                        }
                }
            }

            discardedDiagnostics.Free();
            return newArguments.ToImmutableAndFree();

            TypeSymbol getCorrespondingParameterType(int i)
            {
                // See if all applicable parameters have the same type
                TypeSymbol candidateType = null;
                foreach (var parameterList in parameterListList)
                {
                    var parameterType = GetCorrespondingParameterType(analyzedArguments, i, parameterList);
                    if ((object)parameterType != null)
                    {
                        if ((object)candidateType == null)
                        {
                            candidateType = parameterType;
                        }
                        else if (!candidateType.Equals(parameterType, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes))
                        {
                            // type mismatch
                            candidateType = null;
                            break;
                        }
                    }
                }
                return candidateType;
            }
        }

        /// <summary>
        /// Compute the type of the corresponding parameter, if any. This is used to improve error recovery,
        /// for bad invocations, not for semantic analysis of correct invocations, so it is a heuristic.
        /// If no parameter appears to correspond to the given argument, we return null.
        /// </summary>
        /// <param name="analyzedArguments">The analyzed argument list</param>
        /// <param name="i">The index of the argument</param>
        /// <param name="parameterList">The parameter list to match against</param>
        /// <returns>The type of the corresponding parameter.</returns>
        private static TypeSymbol GetCorrespondingParameterType(AnalyzedArguments analyzedArguments, int i, ImmutableArray<ParameterSymbol> parameterList)
        {
            string name = analyzedArguments.Name(i);
            if (name != null)
            {
                // look for a parameter by that name
                foreach (var parameter in parameterList)
                {
                    if (parameter.Name == name) return parameter.Type;
                }

                return null;
            }

            return (i < parameterList.Length) ? parameterList[i].Type : null;
            // CONSIDER: should we handle variable argument lists?
        }

        /// <summary>
        /// Absent parameter types to bind the arguments, we simply use the arguments provided for error recovery.
        /// </summary>
        private ImmutableArray<BoundExpression> BuildArgumentsForErrorRecovery(AnalyzedArguments analyzedArguments)
        {
            return BuildArgumentsForErrorRecovery(analyzedArguments, Enumerable.Empty<ImmutableArray<ParameterSymbol>>());
        }

        private BoundCall CreateBadCall(
            SyntaxNode node,
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

            return BoundCall.ErrorCall(node, expr, method, args, argNames, argRefKinds, isDelegateCall: false, invokedAsExtensionMethod: false, originalMethods: originalMethods, resultKind: resultKind, binder: this);
        }

        private static TypeSymbol GetCommonTypeOrReturnType<TMember>(ImmutableArray<TMember> members)
            where TMember : Symbol
        {
            TypeSymbol type = null;
            for (int i = 0, n = members.Length; i < n; i++)
            {
                TypeSymbol returnType = members[i].GetTypeOrReturnType().Type;
                if ((object)type == null)
                {
                    type = returnType;
                }
                else if (!TypeSymbol.Equals(type, returnType, TypeCompareKind.ConsiderEverything2))
                {
                    return null;
                }
            }

            return type;
        }

        private bool TryBindNameofOperator(InvocationExpressionSyntax node, DiagnosticBag diagnostics, out BoundExpression result)
        {
            result = null;
            if (node.Expression.Kind() != SyntaxKind.IdentifierName ||
                ((IdentifierNameSyntax)node.Expression).Identifier.ContextualKind() != SyntaxKind.NameOfKeyword ||
                node.ArgumentList.Arguments.Count != 1)
            {
                return false;
            }

            ArgumentSyntax argument = node.ArgumentList.Arguments[0];
            if (argument.NameColon != null || argument.RefOrOutKeyword != default(SyntaxToken) || InvocableNameofInScope())
            {
                return false;
            }

            result = BindNameofOperatorInternal(node, diagnostics);
            return true;
        }

        private BoundExpression BindNameofOperatorInternal(InvocationExpressionSyntax node, DiagnosticBag diagnostics)
        {
            CheckFeatureAvailability(node, MessageID.IDS_FeatureNameof, diagnostics);
            var argument = node.ArgumentList.Arguments[0].Expression;
            string name = "";
            // We relax the instance-vs-static requirement for top-level member access expressions by creating a NameofBinder binder.
            var nameofBinder = new NameofBinder(argument, this);
            var boundArgument = nameofBinder.BindExpression(argument, diagnostics);
            if (!boundArgument.HasAnyErrors && CheckSyntaxForNameofArgument(argument, out name, diagnostics) && boundArgument.Kind == BoundKind.MethodGroup)
            {
                var methodGroup = (BoundMethodGroup)boundArgument;
                if (!methodGroup.TypeArgumentsOpt.IsDefaultOrEmpty)
                {
                    // method group with type parameters not allowed
                    diagnostics.Add(ErrorCode.ERR_NameofMethodGroupWithTypeParameters, argument.Location);
                }
                else
                {
                    nameofBinder.EnsureNameofExpressionSymbols(methodGroup, diagnostics);
                }
            }

            boundArgument = BindToNaturalType(boundArgument, diagnostics, reportNoTargetType: false);
            return new BoundNameOfOperator(node, boundArgument, ConstantValue.Create(name), Compilation.GetSpecialType(SpecialType.System_String));
        }

        private void EnsureNameofExpressionSymbols(BoundMethodGroup methodGroup, DiagnosticBag diagnostics)
        {
            // Check that the method group contains something applicable. Otherwise error.
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var resolution = ResolveMethodGroup(methodGroup, analyzedArguments: null, isMethodGroupConversion: false, useSiteDiagnostics: ref useSiteDiagnostics);
            diagnostics.Add(methodGroup.Syntax, useSiteDiagnostics);
            diagnostics.AddRange(resolution.Diagnostics);
            if (resolution.IsExtensionMethodGroup)
            {
                diagnostics.Add(ErrorCode.ERR_NameofExtensionMethod, methodGroup.Syntax.Location);
            }
        }

        /// <summary>
        /// Returns true if syntax form is OK (so no errors were reported)
        /// </summary>
        private bool CheckSyntaxForNameofArgument(ExpressionSyntax argument, out string name, DiagnosticBag diagnostics, bool top = true)
        {
            switch (argument.Kind())
            {
                case SyntaxKind.IdentifierName:
                    {
                        var syntax = (IdentifierNameSyntax)argument;
                        name = syntax.Identifier.ValueText;
                        return true;
                    }
                case SyntaxKind.GenericName:
                    {
                        var syntax = (GenericNameSyntax)argument;
                        name = syntax.Identifier.ValueText;
                        return true;
                    }
                case SyntaxKind.SimpleMemberAccessExpression:
                    {
                        var syntax = (MemberAccessExpressionSyntax)argument;
                        bool ok = true;
                        switch (syntax.Expression.Kind())
                        {
                            case SyntaxKind.BaseExpression:
                            case SyntaxKind.ThisExpression:
                                break;
                            default:
                                ok = CheckSyntaxForNameofArgument(syntax.Expression, out name, diagnostics, false);
                                break;
                        }
                        name = syntax.Name.Identifier.ValueText;
                        return ok;
                    }
                case SyntaxKind.AliasQualifiedName:
                    {
                        var syntax = (AliasQualifiedNameSyntax)argument;
                        bool ok = true;
                        if (top)
                        {
                            diagnostics.Add(ErrorCode.ERR_AliasQualifiedNameNotAnExpression, argument.Location);
                            ok = false;
                        }
                        name = syntax.Name.Identifier.ValueText;
                        return ok;
                    }
                case SyntaxKind.ThisExpression:
                case SyntaxKind.BaseExpression:
                case SyntaxKind.PredefinedType:
                    name = "";
                    if (top) goto default;
                    return true;
                default:
                    {
                        var code = top ? ErrorCode.ERR_ExpressionHasNoName : ErrorCode.ERR_SubexpressionNotInNameof;
                        diagnostics.Add(code, argument.Location);
                        name = "";
                        return false;
                    }
            }
        }

        /// <summary>
        /// Helper method that checks whether there is an invocable 'nameof' in scope.
        /// </summary>
        private bool InvocableNameofInScope()
        {
            var lookupResult = LookupResult.GetInstance();
            const LookupOptions options = LookupOptions.AllMethodsOnArityZero | LookupOptions.MustBeInvocableIfMember;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            this.LookupSymbolsWithFallback(lookupResult, SyntaxFacts.GetText(SyntaxKind.NameOfKeyword), useSiteDiagnostics: ref useSiteDiagnostics, arity: 0, options: options);

            var result = lookupResult.IsMultiViable;
            lookupResult.Free();
            return result;
        }

#nullable enable
        private BoundFunctionPointerInvocation BindFunctionPointerInvocation(SyntaxNode node, BoundExpression boundExpression, AnalyzedArguments analyzedArguments, DiagnosticBag diagnostics)
        {
            RoslynDebug.Assert(boundExpression.Type is FunctionPointerTypeSymbol);

            var funcPtr = (FunctionPointerTypeSymbol)boundExpression.Type;

            var overloadResolutionResult = OverloadResolutionResult<FunctionPointerMethodSymbol>.GetInstance();
            HashSet<DiagnosticInfo>? useSiteDiagnostics = null;
            var methodsBuilder = ArrayBuilder<FunctionPointerMethodSymbol>.GetInstance(1);
            methodsBuilder.Add(funcPtr.Signature);
            OverloadResolution.FunctionPointerOverloadResolution(
                methodsBuilder,
                analyzedArguments,
                overloadResolutionResult,
                ref useSiteDiagnostics);

            if (!overloadResolutionResult.Succeeded)
            {
                overloadResolutionResult.ReportDiagnostics(
                    binder: this,
                    node.Location,
                    nodeOpt: null,
                    diagnostics,
                    name: null,
                    boundExpression,
                    boundExpression.Syntax,
                    analyzedArguments,
                    methodsBuilder.ToImmutableAndFree(),
                    typeContainingConstructor: null,
                    delegateTypeBeingInvoked: null,
                    returnRefKind: funcPtr.Signature.RefKind);

                return new BoundFunctionPointerInvocation(
                    node,
                    boundExpression,
                    analyzedArguments.Arguments.SelectAsArray((expr, args) => args.binder.BindToNaturalType(expr, args.diagnostics), (binder: this, diagnostics)),
                    analyzedArguments.RefKinds.ToImmutableOrNull(),
                    LookupResultKind.OverloadResolutionFailure,
                    funcPtr.Signature.ReturnType,
                    hasErrors: true);
            }

            methodsBuilder.Free();

            MemberResolutionResult<FunctionPointerMethodSymbol> methodResult = overloadResolutionResult.ValidResult;
            CoerceArguments(
                methodResult,
                analyzedArguments.Arguments,
                diagnostics);

            var args = analyzedArguments.Arguments.ToImmutable();
            var refKinds = analyzedArguments.RefKinds.ToImmutableOrNull();

            bool hasErrors = ReportUnsafeIfNotAllowed(node, diagnostics);
            if (!hasErrors)
            {
                hasErrors = !CheckInvocationArgMixing(
                    node,
                    funcPtr.Signature,
                    receiverOpt: null,
                    funcPtr.Signature.Parameters,
                    args,
                    methodResult.Result.ArgsToParamsOpt,
                    LocalScopeDepth,
                    diagnostics);
            }

            return new BoundFunctionPointerInvocation(
                node,
                boundExpression,
                args,
                refKinds,
                LookupResultKind.Viable,
                funcPtr.Signature.ReturnType,
                hasErrors);
        }
    }
}
