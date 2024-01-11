// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        private BoundExpression BindMethodGroup(ExpressionSyntax node, bool invoked, bool indexed, BindingDiagnosticBag diagnostics)
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

#nullable enable
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
            BindingDiagnosticBag diagnostics,
            SeparatedSyntaxList<TypeSyntax> typeArgsSyntax = default(SeparatedSyntaxList<TypeSyntax>),
            ImmutableArray<TypeWithAnnotations> typeArgs = default(ImmutableArray<TypeWithAnnotations>),
            ImmutableArray<(string Name, Location Location)?> names = default,
            CSharpSyntaxNode? queryClause = null,
            bool allowFieldsAndProperties = false,
            bool allowUnexpandedForm = true,
            bool searchExtensionMethodsIfNecessary = true)
        {
            Debug.Assert(receiver != null);
            Debug.Assert(names.IsDefault || names.Length == args.Length);

            receiver = BindToNaturalType(receiver, diagnostics);
            var boundExpression = BindInstanceMemberAccess(node, node, receiver, methodName, typeArgs.NullToEmpty().Length, typeArgsSyntax, typeArgs, invoked: true, indexed: false, diagnostics, searchExtensionMethodsIfNecessary);

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

                return BadExpression(node, LookupResultKind.Empty, ImmutableArray.Create(symbol), args.Add(receiver), wasCompilerGenerated: true);
            }

            boundExpression = CheckValue(boundExpression, BindValueKind.RValueOrMethodGroup, diagnostics);
            boundExpression.WasCompilerGenerated = true;

            var analyzedArguments = AnalyzedArguments.GetInstance();
            Debug.Assert(!args.Any(static e => e.Kind == BoundKind.OutVariablePendingInference ||
                                        e.Kind == BoundKind.OutDeconstructVarPendingInference ||
                                        e.Kind == BoundKind.DiscardExpression && !e.HasExpressionType()));
            analyzedArguments.Arguments.AddRange(args);

            if (!names.IsDefault)
            {
                analyzedArguments.Names.AddRange(names);
            }

            BoundExpression result = BindInvocationExpression(
                node, node, methodName, boundExpression, analyzedArguments, diagnostics, queryClause,
                allowUnexpandedForm: allowUnexpandedForm);

            // Query operator can't be called dynamically. 
            if (queryClause != null && result.Kind == BoundKind.DynamicInvocation)
            {
                // the error has already been reported by BindInvocationExpression
                Debug.Assert(diagnostics.DiagnosticBag is null || diagnostics.HasAnyErrors());

                result = CreateBadCall(node, boundExpression, LookupResultKind.Viable, analyzedArguments);
            }

            result.WasCompilerGenerated = true;
            analyzedArguments.Free();
            return result;
        }
#nullable disable

        /// <summary>
        /// Bind an expression as a method invocation.
        /// </summary>
        private BoundExpression BindInvocationExpression(
            InvocationExpressionSyntax node,
            BindingDiagnosticBag diagnostics)
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
            else if (receiverIsInvocation(node, out InvocationExpressionSyntax nested))
            {
                var invocations = ArrayBuilder<InvocationExpressionSyntax>.GetInstance();

                invocations.Push(node);
                node = nested;
                while (receiverIsInvocation(node, out nested))
                {
                    invocations.Push(node);
                    node = nested;
                }

                BoundExpression boundExpression = BindMethodGroup(node.Expression, invoked: true, indexed: false, diagnostics: diagnostics);

                while (true)
                {
                    result = bindArgumentsAndInvocation(node, boundExpression, analyzedArguments, diagnostics);
                    nested = node;

                    if (!invocations.TryPop(out node))
                    {
                        break;
                    }

                    Debug.Assert(node.Expression.Kind() is SyntaxKind.SimpleMemberAccessExpression);
                    var memberAccess = (MemberAccessExpressionSyntax)node.Expression;
                    analyzedArguments.Clear();
                    CheckContextForPointerTypes(nested, diagnostics, result); // BindExpression does this after calling BindExpressionInternal
                    boundExpression = BindMemberAccessWithBoundLeft(memberAccess, result, memberAccess.Name, memberAccess.OperatorToken, invoked: true, indexed: false, diagnostics);
                }

                invocations.Free();
            }
            else
            {
                BoundExpression boundExpression = BindMethodGroup(node.Expression, invoked: true, indexed: false, diagnostics: diagnostics);
                result = bindArgumentsAndInvocation(node, boundExpression, analyzedArguments, diagnostics);
            }

            analyzedArguments.Free();
            return result;

            BoundExpression bindArgumentsAndInvocation(InvocationExpressionSyntax node, BoundExpression boundExpression, AnalyzedArguments analyzedArguments, BindingDiagnosticBag diagnostics)
            {
                boundExpression = CheckValue(boundExpression, BindValueKind.RValueOrMethodGroup, diagnostics);
                string name = boundExpression.Kind == BoundKind.MethodGroup ? GetName(node.Expression) : null;
                BindArgumentsAndNames(node.ArgumentList, diagnostics, analyzedArguments, allowArglist: true);
                return BindInvocationExpression(node, node.Expression, name, boundExpression, analyzedArguments, diagnostics);
            }

            static bool receiverIsInvocation(InvocationExpressionSyntax node, out InvocationExpressionSyntax nested)
            {
                if (node.Expression is MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax receiver, RawKind: (int)SyntaxKind.SimpleMemberAccessExpression } && !receiver.MayBeNameofOperator())
                {
                    nested = receiver;
                    return true;
                }

                nested = null;
                return false;
            }
        }

        private BoundExpression BindArgListOperator(InvocationExpressionSyntax node, BindingDiagnosticBag diagnostics, AnalyzedArguments analyzedArguments)
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
            BindingDiagnosticBag diagnostics,
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
            else if (boundExpression.Type?.Kind == SymbolKind.FunctionPointerType)
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
            BindingDiagnosticBag diagnostics,
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
                                    methodGroup.FunctionType,
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
                                    methodGroup.FunctionType,
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

        private void CheckNamedArgumentsForDynamicInvocation(AnalyzedArguments arguments, BindingDiagnosticBag diagnostics)
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

        private ImmutableArray<BoundExpression> BuildArgumentsForDynamicInvocation(AnalyzedArguments arguments, BindingDiagnosticBag diagnostics)
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
            BindingDiagnosticBag diagnostics,
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
            BindingDiagnosticBag diagnostics,
            CSharpSyntaxNode queryClause,
            NamedTypeSymbol delegateType)
        {
            BoundExpression result;
            var methodGroup = MethodGroup.GetInstance();
            methodGroup.PopulateWithSingleMethod(boundExpression, delegateType.DelegateInvokeMethod);
            var overloadResolutionResult = OverloadResolutionResult<MethodSymbol>.GetInstance();
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            OverloadResolution.MethodInvocationOverloadResolution(
                methods: methodGroup.Methods,
                typeArguments: methodGroup.TypeArguments,
                receiver: methodGroup.Receiver,
                arguments: analyzedArguments,
                result: overloadResolutionResult,
                useSiteInfo: ref useSiteInfo);
            diagnostics.Add(node, useSiteInfo);

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
            BindingDiagnosticBag diagnostics,
            CSharpSyntaxNode queryClause,
            bool allowUnexpandedForm,
            out bool anyApplicableCandidates)
        {
            BoundExpression result;
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            var resolution = this.ResolveMethodGroup(
                methodGroup, expression, methodName, analyzedArguments, isMethodGroupConversion: false,
                useSiteInfo: ref useSiteInfo, allowUnexpandedForm: allowUnexpandedForm);
            diagnostics.Add(expression, useSiteInfo);
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
                        result = BindInvocationExpressionContinued(
                            syntax, expression, methodName, resolution.OverloadResolutionResult, resolution.AnalyzedArguments,
                            resolution.MethodGroup, delegateTypeOpt: null, diagnostics: BindingDiagnosticBag.Discarded, queryClause: queryClause);
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
            BindingDiagnosticBag diagnostics,
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
            BindingDiagnosticBag diagnostics) where TMethodOrPropertySymbol : Symbol
        {
            Debug.Assert(overloadResolutionResult.HasAnyApplicableMember);

            var finalCandidates = ArrayBuilder<TMethodOrPropertySymbol>.GetInstance();
            BindingDiagnosticBag firstFailed = null;
            var candidateDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics);

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
                        (typeArgumentsOpt.IsDefault || ((MethodSymbol)(object)result.Member).CheckConstraints(new ConstraintsHelper.CheckConstraintsArgs(this.Compilation, this.Conversions, includeNullability: false, syntax.Location, candidateDiagnostics))))
                    {
                        finalCandidates.Add(result.Member);
                        continue;
                    }

                    if (firstFailed == null)
                    {
                        firstFailed = candidateDiagnostics;
                        candidateDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics);
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

        private void CheckRestrictedTypeReceiver(BoundExpression expression, CSharpCompilation compilation, BindingDiagnosticBag diagnostics)
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
            BindingDiagnosticBag diagnostics,
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
                                _ = ConvertSwitchExpression(switchExpr, naturalType, conversionIfTargetTyped: null, diagnostics);
                                break;
                            case BoundUnconvertedConditionalOperator { Type: { } naturalType } conditionalExpr:
                                _ = ConvertConditionalExpression(conditionalExpr, naturalType, conversionIfTargetTyped: null, diagnostics);
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
            var method = methodResult.Member;

            // It is possible that overload resolution succeeded, but we have chosen an
            // instance method and we're in a static method. A careful reading of the
            // overload resolution spec shows that the "final validation" stage allows an
            // "implicit this" on any method call, not just method calls from inside
            // instance methods. Therefore we must detect this scenario here, rather than in
            // overload resolution.

            var receiver = ReplaceTypeOrValueReceiver(methodGroup.Receiver, !method.RequiresInstanceReceiver && !invokedAsExtensionMethod, diagnostics);

            this.CheckAndCoerceArguments(methodResult, analyzedArguments, diagnostics, receiver, invokedAsExtensionMethod: invokedAsExtensionMethod);

            var expanded = methodResult.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm;
            var argsToParams = methodResult.Result.ArgsToParamsOpt;

            BindDefaultArgumentsAndParamsArray(node, method.Parameters, analyzedArguments.Arguments, analyzedArguments.RefKinds, analyzedArguments.Names, ref argsToParams, out var defaultArguments, expanded, enableCallerInfo: true, diagnostics);

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
            if (method.HasParameterContainingPointerType())
            {
                // Don't worry about double reporting (i.e. for both the argument and the parameter)
                // because only one unsafe diagnostic is allowed per scope - the others are suppressed.
                gotError = ReportUnsafeIfNotAllowed(node, diagnostics) || gotError;
            }

            bool hasBaseReceiver = receiver != null && receiver.Kind == BoundKind.BaseReference;

            ReportDiagnosticsIfObsolete(diagnostics, method, node, hasBaseReceiver);
            ReportDiagnosticsIfUnmanagedCallersOnly(diagnostics, method, node, isDelegateConversion: false);

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

            return new BoundCall(node, receiver, initialBindingReceiverIsSubjectToCloning: ReceiverIsSubjectToCloning(receiver, method), method, args, argNames, argRefKinds, isDelegateCall: isDelegateCall,
                        expanded: expanded, invokedAsExtensionMethod: invokedAsExtensionMethod,
                        argsToParamsOpt: argsToParams, defaultArguments, resultKind: LookupResultKind.Viable, type: returnType, hasErrors: gotError);
        }

#nullable enable

        internal ThreeState ReceiverIsSubjectToCloning(BoundExpression? receiver, PropertySymbol property)
        {
            var method = property.GetMethod ?? property.SetMethod;

            // Property might be missing accessors in invalid code.
            if (method is null)
            {
                return ThreeState.False;
            }

            return ReceiverIsSubjectToCloning(receiver, method);
        }

        internal ThreeState ReceiverIsSubjectToCloning(BoundExpression? receiver, MethodSymbol method)
        {
            if (receiver is BoundValuePlaceholderBase || receiver?.Type?.IsValueType != true)
            {
                return ThreeState.False;
            }

            var valueKind = method.IsEffectivelyReadOnly
                ? BindValueKind.RefersToLocation
                : BindValueKind.RefersToLocation | BindValueKind.Assignable;
            var result = !CheckValueKind(receiver.Syntax, receiver, valueKind, checkingReceiver: true, BindingDiagnosticBag.Discarded);
            return result.ToThreeState();
        }

        private static SourceLocation GetCallerLocation(SyntaxNode syntax)
        {
            var token = syntax switch
            {
                InvocationExpressionSyntax invocation => invocation.ArgumentList.OpenParenToken,
                BaseObjectCreationExpressionSyntax objectCreation => objectCreation.NewKeyword,
                ConstructorInitializerSyntax constructorInitializer => constructorInitializer.ArgumentList.OpenParenToken,
                PrimaryConstructorBaseTypeSyntax primaryConstructorBaseType => primaryConstructorBaseType.ArgumentList.OpenParenToken,
                ElementAccessExpressionSyntax elementAccess => elementAccess.ArgumentList.OpenBracketToken,
                _ => syntax.GetFirstToken()
            };

            return new SourceLocation(token);
        }

        private BoundExpression GetDefaultParameterSpecialNoConversion(SyntaxNode syntax, ParameterSymbol parameter, BindingDiagnosticBag diagnostics)
        {
            var parameterType = parameter.Type;
            Debug.Assert(parameterType.IsDynamic() || parameterType.SpecialType == SpecialType.System_Object);

            // We have a call to a method M([Optional] object x) which omits the argument. The value we generate
            // for the argument depends on the presence or absence of other attributes. The rules are:
            //
            // * If we're generating a default argument for an attribute, it's a compile error.
            // * If the parameter is marked as [MarshalAs(Interface)], [MarshalAs(IUnknown)] or [MarshalAs(IDispatch)]
            //   then the argument is null.
            // * Otherwise, if the parameter is marked as [IUnknownConstant] then the argument is
            //   new UnknownWrapper(null)
            // * Otherwise, if the parameter is marked as [IDispatchConstant] then the argument is
            //    new DispatchWrapper(null)
            // * Otherwise, the argument is Type.Missing.

            BoundExpression? defaultValue = null;
            if (InAttributeArgument)
            {
                // CS7067: Attribute constructor parameter '{0}' is optional, but no default parameter value was specified.
                diagnostics.Add(ErrorCode.ERR_BadAttributeParamDefaultArgument, syntax.Location, parameter.Name);
            }
            else if (parameter.IsMarshalAsObject)
            {
                // default(object)
                defaultValue = new BoundDefaultExpression(syntax, parameterType) { WasCompilerGenerated = true };
            }
            else if (parameter.IsIUnknownConstant)
            {
                if (GetWellKnownTypeMember(Compilation, WellKnownMember.System_Runtime_InteropServices_UnknownWrapper__ctor, diagnostics, syntax: syntax) is MethodSymbol methodSymbol)
                {
                    // new UnknownWrapper(default(object))
                    var unknownArgument = new BoundDefaultExpression(syntax, parameterType) { WasCompilerGenerated = true };
                    defaultValue = new BoundObjectCreationExpression(syntax, methodSymbol, unknownArgument) { WasCompilerGenerated = true };
                }
            }
            else if (parameter.IsIDispatchConstant)
            {
                if (GetWellKnownTypeMember(Compilation, WellKnownMember.System_Runtime_InteropServices_DispatchWrapper__ctor, diagnostics, syntax: syntax) is MethodSymbol methodSymbol)
                {
                    // new DispatchWrapper(default(object))
                    var dispatchArgument = new BoundDefaultExpression(syntax, parameterType) { WasCompilerGenerated = true };
                    defaultValue = new BoundObjectCreationExpression(syntax, methodSymbol, dispatchArgument) { WasCompilerGenerated = true };
                }
            }
            else
            {
                if (GetWellKnownTypeMember(Compilation, WellKnownMember.System_Type__Missing, diagnostics, syntax: syntax) is FieldSymbol fieldSymbol)
                {
                    // Type.Missing
                    defaultValue = new BoundFieldAccess(syntax, null, fieldSymbol, ConstantValue.NotAvailable) { WasCompilerGenerated = true };
                }
            }

            return defaultValue ?? BadExpression(syntax).MakeCompilerGenerated();
        }

        internal static ParameterSymbol? GetCorrespondingParameter(
            int argumentOrdinal,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<int> argsToParamsOpt,
            bool expanded)
        {
            int n = parameters.Length;
            ParameterSymbol? parameter;

            if (argsToParamsOpt.IsDefault)
            {
                if (argumentOrdinal < n)
                {
                    parameter = parameters[argumentOrdinal];
                }
                else if (expanded)
                {
                    parameter = parameters[n - 1];
                }
                else
                {
                    parameter = null;
                }
            }
            else
            {
                Debug.Assert(argumentOrdinal < argsToParamsOpt.Length);
                int parameterOrdinal = argsToParamsOpt[argumentOrdinal];

                if (parameterOrdinal < n)
                {
                    parameter = parameters[parameterOrdinal];
                }
                else
                {
                    parameter = null;
                }
            }

            return parameter;
        }

        internal void BindDefaultArgumentsAndParamsArray(
            SyntaxNode node,
            ImmutableArray<ParameterSymbol> parameters,
            ArrayBuilder<BoundExpression> argumentsBuilder,
            ArrayBuilder<RefKind>? argumentRefKindsBuilder,
            ArrayBuilder<(string Name, Location Location)?>? namesBuilder,
            ref ImmutableArray<int> argsToParamsOpt,
            out BitVector defaultArguments,
            bool expanded,
            bool enableCallerInfo,
            BindingDiagnosticBag diagnostics,
            bool assertMissingParametersAreOptional = true,
            Symbol? attributedMember = null)
        {
            int firstParamArrayArgument = -1;
            int paramsIndex = parameters.Length - 1;
            var arrayArgsBuilder = expanded ? ArrayBuilder<BoundExpression>.GetInstance() : null;

            Debug.Assert(!argumentsBuilder.Any(a => a.IsParamsArray));

            var visitedParameters = BitVector.Create(parameters.Length);
            for (var i = 0; i < argumentsBuilder.Count; i++)
            {
                var parameter = GetCorrespondingParameter(i, parameters, argsToParamsOpt, expanded);
                if (parameter is not null)
                {
                    visitedParameters[parameter.Ordinal] = true;

                    if (expanded && parameter.Ordinal == paramsIndex)
                    {
                        Debug.Assert(arrayArgsBuilder is not null);
                        Debug.Assert(arrayArgsBuilder.Count == 0);

                        firstParamArrayArgument = i;
                        arrayArgsBuilder.Add(argumentsBuilder[i]);

                        for (int remainingArgument = i + 1; remainingArgument < argumentsBuilder.Count; ++remainingArgument)
                        {
                            if (GetCorrespondingParameter(remainingArgument, parameters, argsToParamsOpt, expanded: true)?.Ordinal != paramsIndex)
                            {
                                break;
                            }

                            arrayArgsBuilder.Add(argumentsBuilder[remainingArgument]);
                            i++;
                        }
                    }
                }
            }

            if (expanded)
            {
                // expanded parameter array is not treated as an optional parameter
                visitedParameters[paramsIndex] = true;
            }

            bool haveDefaultArguments = !parameters.All(static (param, visitedParameters) => visitedParameters[param.Ordinal], visitedParameters);

            if (!haveDefaultArguments && !expanded)
            {
                Debug.Assert(argumentsBuilder.Count >= parameters.Length); // Accounting for arglist cases
                Debug.Assert(argumentRefKindsBuilder is null || argumentRefKindsBuilder.Count == 0 || argumentRefKindsBuilder.Count == argumentsBuilder.Count);
                Debug.Assert(namesBuilder is null || namesBuilder.Count == 0 || namesBuilder.Count == argumentsBuilder.Count);
                Debug.Assert(argsToParamsOpt.IsDefault || argsToParamsOpt.Length == argumentsBuilder.Count);
                Debug.Assert(arrayArgsBuilder is null);
                defaultArguments = default;
                return;
            }

            ArrayBuilder<int>? argsToParamsBuilder = null;
            if (!argsToParamsOpt.IsDefault)
            {
                argsToParamsBuilder = ArrayBuilder<int>.GetInstance(argsToParamsOpt.Length);
                argsToParamsBuilder.AddRange(argsToParamsOpt);
            }

            BoundArrayCreation? array = null;

            if (expanded)
            {
                Debug.Assert(arrayArgsBuilder is not null);
                ImmutableArray<BoundExpression> arrayArgs = arrayArgsBuilder.ToImmutableAndFree();
                int arrayArgsLength = arrayArgs.Length;

                TypeSymbol int32Type = GetSpecialType(SpecialType.System_Int32, diagnostics, node);
                TypeSymbol paramArrayType = parameters[paramsIndex].Type;
                BoundExpression arraySize = new BoundLiteral(node, ConstantValue.Create(arrayArgsLength), int32Type) { WasCompilerGenerated = true };

                array = new BoundArrayCreation(
                            node,
                            ImmutableArray.Create(arraySize),
                            new BoundArrayInitialization(node, isInferred: false, arrayArgs) { WasCompilerGenerated = true },
                            paramArrayType)
                { WasCompilerGenerated = true, IsParamsArray = true };

                if (arrayArgsLength != 0)
                {
                    Debug.Assert(firstParamArrayArgument != -1);
                    Debug.Assert(!haveDefaultArguments || arrayArgsLength == 1);
                    Debug.Assert(arrayArgsLength == 1 || firstParamArrayArgument + arrayArgsLength == argumentsBuilder.Count);

                    for (var i = firstParamArrayArgument + arrayArgsLength - 1; i != firstParamArrayArgument; i--)
                    {
                        argumentsBuilder.RemoveAt(i);
                        argsToParamsBuilder?.RemoveAt(i);

                        if (argumentRefKindsBuilder is { Count: > 0 })
                        {
                            argumentRefKindsBuilder.RemoveAt(i);
                        }

                        if (namesBuilder is { Count: > 0 })
                        {
                            namesBuilder.RemoveAt(i);
                        }
                    }

                    argumentsBuilder[firstParamArrayArgument] = array;
                    array = null;
                }
            }

            // only proceed with binding default arguments if we know there is some parameter that has not been matched by an explicit argument
            if (haveDefaultArguments)
            {
                // In a scenario like `string Prop { get; } = M();`, the containing symbol could be the synthesized field.
                // We want to use the associated user-declared symbol instead where possible.
                var containingMember = InAttributeArgument ? attributedMember : ContainingMember() switch
                {
                    FieldSymbol { AssociatedSymbol: { } symbol } => symbol,
                    var c => c
                };
                Debug.Assert(InAttributeArgument || (attributedMember is null && containingMember is not null));

                defaultArguments = BitVector.Create(parameters.Length);

                // Params methods can be invoked in normal form, so the strongest assertion we can make is that, if
                // we're in an expanded context, the last param must be params. The inverse is not necessarily true.
                Debug.Assert(!expanded || parameters[^1].IsParams);
                var lastIndex = expanded ? ^1 : ^0;

                var argumentsCount = argumentsBuilder.Count;
                // Go over missing parameters, inserting default values for optional parameters
                foreach (var parameter in parameters.AsSpan()[..lastIndex])
                {
                    if (!visitedParameters[parameter.Ordinal])
                    {
                        Debug.Assert(parameter.IsOptional || !assertMissingParametersAreOptional);

                        defaultArguments[argumentsBuilder.Count] = true;
                        argumentsBuilder.Add(bindDefaultArgument(node, parameter, containingMember, enableCallerInfo, diagnostics, argumentsBuilder, argumentsCount, argsToParamsOpt));

                        if (argumentRefKindsBuilder is { Count: > 0 })
                        {
                            argumentRefKindsBuilder.Add(RefKind.None);
                        }

                        argsToParamsBuilder?.Add(parameter.Ordinal);
                        if (namesBuilder?.Count > 0)
                        {
                            namesBuilder.Add(null);
                        }
                    }
                }
            }
            else
            {
                defaultArguments = default;
            }

            if (array is not null)
            {
                Debug.Assert(expanded);
                Debug.Assert(firstParamArrayArgument == -1);

                argumentsBuilder.Add(array);
                argsToParamsBuilder?.Add(paramsIndex);

                if (argumentRefKindsBuilder is { Count: > 0 })
                {
                    argumentRefKindsBuilder.Add(RefKind.None);
                }

                if (namesBuilder is { Count: > 0 })
                {
                    namesBuilder.Add(null);
                }
            }

            Debug.Assert(argumentsBuilder.Count == parameters.Length);
            Debug.Assert(argumentRefKindsBuilder is null || argumentRefKindsBuilder.Count == 0 || argumentRefKindsBuilder.Count == parameters.Length);
            Debug.Assert(namesBuilder is null || namesBuilder.Count == 0 || namesBuilder.Count == parameters.Length);
            Debug.Assert(argsToParamsBuilder is null || argsToParamsBuilder.Count == parameters.Length);

            if (argsToParamsBuilder is object)
            {
                argsToParamsOpt = argsToParamsBuilder.ToImmutableOrNull();
                argsToParamsBuilder.Free();
            }

            BoundExpression bindDefaultArgument(SyntaxNode syntax, ParameterSymbol parameter, Symbol? containingMember, bool enableCallerInfo, BindingDiagnosticBag diagnostics, ArrayBuilder<BoundExpression> argumentsBuilder, int argumentsCount, ImmutableArray<int> argsToParamsOpt)
            {
                TypeSymbol parameterType = parameter.Type;
                if (Flags.Includes(BinderFlags.ParameterDefaultValue))
                {
                    // This is only expected to occur in recursive error scenarios, for example: `object F(object param = F()) { }`
                    // We return a non-error expression here to ensure ERR_DefaultValueMustBeConstant (or another appropriate diagnostics) is produced by the caller.
                    return new BoundDefaultExpression(syntax, parameterType) { WasCompilerGenerated = true };
                }

                var parameterDefaultValue = parameter.ExplicitDefaultConstantValue;
                if (InAttributeArgument && parameterDefaultValue?.IsBad == true)
                {
                    diagnostics.Add(ErrorCode.ERR_BadAttributeArgument, syntax.Location);
                    return BadExpression(syntax).MakeCompilerGenerated();
                }

                var defaultConstantValue = parameterDefaultValue switch
                {
                    // Bad default values are implicitly replaced with default(T) at call sites.
                    { IsBad: true } => ConstantValue.Null,
                    var constantValue => constantValue
                };
                Debug.Assert((object?)defaultConstantValue != ConstantValue.Unset);

                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                var callerSourceLocation = enableCallerInfo ? GetCallerLocation(syntax) : null;
                BoundExpression defaultValue;
                if (callerSourceLocation is object && parameter.IsCallerLineNumber)
                {
                    int line = callerSourceLocation.SourceTree.GetDisplayLineNumber(callerSourceLocation.SourceSpan);
                    defaultValue = new BoundLiteral(syntax, ConstantValue.Create(line), Compilation.GetSpecialType(SpecialType.System_Int32)) { WasCompilerGenerated = true };
                }
                else if (callerSourceLocation is object && parameter.IsCallerFilePath)
                {
                    string path = callerSourceLocation.SourceTree.GetDisplayPath(callerSourceLocation.SourceSpan, Compilation.Options.SourceReferenceResolver);
                    defaultValue = new BoundLiteral(syntax, ConstantValue.Create(path), Compilation.GetSpecialType(SpecialType.System_String)) { WasCompilerGenerated = true };
                }
                else if (callerSourceLocation is object && parameter.IsCallerMemberName && containingMember is not null)
                {
                    var memberName = containingMember.GetMemberCallerName();
                    defaultValue = new BoundLiteral(syntax, ConstantValue.Create(memberName), Compilation.GetSpecialType(SpecialType.System_String)) { WasCompilerGenerated = true };
                }
                else if (callerSourceLocation is object
                    && !parameter.IsCallerMemberName
                    && Conversions.ClassifyBuiltInConversion(Compilation.GetSpecialType(SpecialType.System_String), parameterType, isChecked: false, ref discardedUseSiteInfo).Exists
                    && getArgumentIndex(parameter.CallerArgumentExpressionParameterIndex, argsToParamsOpt) is int argumentIndex
                    && argumentIndex > -1 && argumentIndex < argumentsCount)
                {
                    var argument = argumentsBuilder[argumentIndex];
                    defaultValue = new BoundLiteral(syntax, ConstantValue.Create(argument.Syntax.ToString()), Compilation.GetSpecialType(SpecialType.System_String)) { WasCompilerGenerated = true };
                }
                else if (defaultConstantValue == ConstantValue.NotAvailable)
                {
                    // There is no constant value given for the parameter in source/metadata.
                    if (parameterType.IsDynamic() || parameterType.SpecialType == SpecialType.System_Object)
                    {
                        // We have something like M([Optional] object x). We have special handling for such situations.
                        defaultValue = GetDefaultParameterSpecialNoConversion(syntax, parameter, diagnostics);
                    }
                    else
                    {
                        // The argument to M([Optional] int x) becomes default(int)
                        defaultValue = new BoundDefaultExpression(syntax, parameterType) { WasCompilerGenerated = true };
                    }
                }
                else if (defaultConstantValue.IsNull)
                {
                    defaultValue = new BoundDefaultExpression(syntax, parameterType) { WasCompilerGenerated = true };
                }
                else
                {
                    TypeSymbol constantType = Compilation.GetSpecialType(defaultConstantValue.SpecialType);
                    defaultValue = new BoundLiteral(syntax, defaultConstantValue, constantType) { WasCompilerGenerated = true };

                    if (InAttributeArgument && parameterType.SpecialType == SpecialType.System_Object)
                    {
                        // error CS1763: '{0}' is of type '{1}'. A default parameter value of a reference type other than string can only be initialized with null
                        diagnostics.Add(ErrorCode.ERR_NotNullRefDefaultParameter, syntax.Location, parameter.Name, parameterType);
                    }
                }

                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                Conversion conversion = Conversions.ClassifyConversionFromExpression(defaultValue, parameterType, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);
                diagnostics.Add(syntax, useSiteInfo);

                if (!conversion.IsValid && defaultConstantValue is { SpecialType: SpecialType.System_Decimal or SpecialType.System_DateTime })
                {
                    // Usually, if a default constant value fails to convert to the parameter type, we want an error at the call site.
                    // For legacy reasons, decimal and DateTime constants are special. If such a constant fails to convert to the parameter type
                    // then we want to silently replace it with default(ParameterType).
                    defaultValue = new BoundDefaultExpression(syntax, parameterType) { WasCompilerGenerated = true };
                }
                else
                {
                    if (!conversion.IsValid)
                    {
                        GenerateImplicitConversionError(diagnostics, syntax, conversion, defaultValue, parameterType);
                    }
                    var isCast = conversion.IsExplicit;
                    defaultValue = CreateConversion(
                        defaultValue.Syntax,
                        defaultValue,
                        conversion,
                        isCast,
                        isCast ? new ConversionGroup(conversion, parameter.TypeWithAnnotations) : null,
                        parameterType,
                        diagnostics);
                }

                return defaultValue;

                static int getArgumentIndex(int parameterIndex, ImmutableArray<int> argsToParamsOpt)
                    => argsToParamsOpt.IsDefault
                        ? parameterIndex
                        : argsToParamsOpt.IndexOf(parameterIndex);
            }

        }

#nullable disable

        /// <summary>
        /// Returns false if an implicit 'this' copy will occur due to an instance member invocation in a readonly member.
        /// </summary>
        internal bool CheckImplicitThisCopyInReadOnlyMember(BoundExpression receiver, MethodSymbol method, BindingDiagnosticBag diagnostics)
        {
            // For now we are warning only in implicit copy scenarios that are only possible with readonly members.
            // Eventually we will warn on implicit value copies in more scenarios. See https://github.com/dotnet/roslyn/issues/33968.
            if (receiver?.IsEquivalentToThisReference == true &&
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
        private BoundExpression ReplaceTypeOrValueReceiver(BoundExpression receiver, bool useType, BindingDiagnosticBag diagnostics)
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

                        foreach (Diagnostic d in typeOrValue.Data.ValueDiagnostics.Diagnostics)
                        {
                            // Avoid forcing resolution of lazy diagnostics to avoid cycles.
                            var code = d is DiagnosticWithInfo { HasLazyInfo: true, LazyInfo.Code: var lazyCode } ? lazyCode : d.Code;
                            if (code == (int)ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase &&
                                !(d.Arguments is [ParameterSymbol shadowedParameter] && shadowedParameter.Type.Equals(typeOrValue.Data.ValueExpression.Type, TypeCompareKind.AllIgnoreOptions))) // If the type and the name match, we would resolve to the same type rather than a value at the end.
                            {
                                Debug.Assert(d is not DiagnosticWithInfo { HasLazyInfo: true }, "Adjust the Arguments access to handle lazy diagnostics to avoid cycles.");
                                diagnostics.Add(d);
                            }
                        }

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

        private static BoundExpression GetValueExpressionIfTypeOrValueReceiver(BoundExpression receiver)
        {
            if ((object)receiver == null)
            {
                return null;
            }

            switch (receiver)
            {
                case BoundTypeOrValueExpression typeOrValueExpression:
                    return typeOrValueExpression.Data.ValueExpression;

                case BoundQueryClause queryClause:
                    // a query clause may wrap a TypeOrValueExpression.
                    return GetValueExpressionIfTypeOrValueReceiver(queryClause.Value);

                default:
                    return null;
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
                            var unboundArgument = (UnboundLambda)argument;

                            // If nested in other lambdas where type inference is involved,
                            // the target delegate type could be different each time.
                            // But if the lambda is explicitly typed, we can bind only once.
                            // https://github.com/dotnet/roslyn/issues/69093
                            if (unboundArgument.HasExplicitlyTypedParameterList &&
                                unboundArgument.HasExplicitReturnType(out _, out _) &&
                                unboundArgument.FunctionType is { } functionType &&
                                functionType.GetInternalDelegateType() is { } delegateType)
                            {
                                // Just assume we're not in an expression tree for the purposes of error recovery.
                                _ = unboundArgument.Bind(delegateType, isExpressionTree: false);
                            }
                            else
                            {
                                // bind the argument against each applicable parameter
                                foreach (var parameterList in parameterListList)
                                {
                                    var parameterType = GetCorrespondingParameterType(analyzedArguments, i, parameterList);
                                    if (parameterType?.Kind == SymbolKind.NamedType &&
                                        (object)parameterType.GetDelegateType() != null)
                                    {
                                        // Just assume we're not in an expression tree for the purposes of error recovery.
                                        var discarded = unboundArgument.Bind((NamedTypeSymbol)parameterType, isExpressionTree: false);
                                    }
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

        private bool TryBindNameofOperator(InvocationExpressionSyntax node, BindingDiagnosticBag diagnostics, out BoundExpression result)
        {
            if (node.MayBeNameofOperator())
            {
                var binder = this.GetBinder(node);
                if (binder.EnclosingNameofArgument == node.ArgumentList.Arguments[0].Expression)
                {
                    result = binder.BindNameofOperatorInternal(node, diagnostics);
                    return true;
                }
            }

            result = null;
            return false;
        }

        private BoundExpression BindNameofOperatorInternal(InvocationExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            CheckFeatureAvailability(node, MessageID.IDS_FeatureNameof, diagnostics);
            var argument = node.ArgumentList.Arguments[0].Expression;
            var boundArgument = BindExpression(argument, diagnostics);

            bool syntaxIsOk = CheckSyntaxForNameofArgument(argument, out string name, boundArgument.HasAnyErrors ? BindingDiagnosticBag.Discarded : diagnostics);
            if (!boundArgument.HasAnyErrors && syntaxIsOk && boundArgument.Kind == BoundKind.MethodGroup)
            {
                var methodGroup = (BoundMethodGroup)boundArgument;
                if (!methodGroup.TypeArgumentsOpt.IsDefaultOrEmpty)
                {
                    // method group with type parameters not allowed
                    diagnostics.Add(ErrorCode.ERR_NameofMethodGroupWithTypeParameters, argument.Location);
                }
                else
                {
                    EnsureNameofExpressionSymbols(methodGroup, diagnostics);
                }
            }

            if (boundArgument is BoundNamespaceExpression nsExpr)
            {
                diagnostics.AddAssembliesUsedByNamespaceReference(nsExpr.NamespaceSymbol);
            }

            boundArgument = BindToNaturalType(boundArgument, diagnostics, reportNoTargetType: false);
            return new BoundNameOfOperator(node, boundArgument, ConstantValue.Create(name), Compilation.GetSpecialType(SpecialType.System_String));
        }

        private void EnsureNameofExpressionSymbols(BoundMethodGroup methodGroup, BindingDiagnosticBag diagnostics)
        {
            // Check that the method group contains something applicable. Otherwise error.
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            var resolution = ResolveMethodGroup(methodGroup, analyzedArguments: null, isMethodGroupConversion: false, useSiteInfo: ref useSiteInfo);
            diagnostics.Add(methodGroup.Syntax, useSiteInfo);
            diagnostics.AddRange(resolution.Diagnostics);
            if (resolution.IsExtensionMethodGroup)
            {
                diagnostics.Add(ErrorCode.ERR_NameofExtensionMethod, methodGroup.Syntax.Location);
            }
        }

        /// <summary>
        /// Returns true if syntax form is OK (so no errors were reported)
        /// </summary>
        private bool CheckSyntaxForNameofArgument(ExpressionSyntax argument, out string name, BindingDiagnosticBag diagnostics, bool top = true)
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
        internal bool InvocableNameofInScope()
        {
            var lookupResult = LookupResult.GetInstance();
            const LookupOptions options = LookupOptions.AllMethodsOnArityZero | LookupOptions.MustBeInvocableIfMember;
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            this.LookupSymbolsWithFallback(lookupResult, SyntaxFacts.GetText(SyntaxKind.NameOfKeyword), useSiteInfo: ref discardedUseSiteInfo, arity: 0, options: options);

            var result = lookupResult.IsMultiViable;
            lookupResult.Free();
            return result;
        }

#nullable enable
        private BoundFunctionPointerInvocation BindFunctionPointerInvocation(SyntaxNode node, BoundExpression boundExpression, AnalyzedArguments analyzedArguments, BindingDiagnosticBag diagnostics)
        {
            boundExpression = BindToNaturalType(boundExpression, diagnostics);
            RoslynDebug.Assert(boundExpression.Type is FunctionPointerTypeSymbol);

            var funcPtr = (FunctionPointerTypeSymbol)boundExpression.Type;

            var overloadResolutionResult = OverloadResolutionResult<FunctionPointerMethodSymbol>.GetInstance();
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            var methodsBuilder = ArrayBuilder<FunctionPointerMethodSymbol>.GetInstance(1);
            methodsBuilder.Add(funcPtr.Signature);
            OverloadResolution.FunctionPointerOverloadResolution(
                methodsBuilder,
                analyzedArguments,
                overloadResolutionResult,
                ref useSiteInfo);

            diagnostics.Add(node, useSiteInfo);

            if (!overloadResolutionResult.Succeeded)
            {
                ImmutableArray<FunctionPointerMethodSymbol> methods = methodsBuilder.ToImmutableAndFree();
                overloadResolutionResult.ReportDiagnostics(
                    binder: this,
                    node.Location,
                    nodeOpt: null,
                    diagnostics,
                    name: null,
                    boundExpression,
                    boundExpression.Syntax,
                    analyzedArguments,
                    methods,
                    typeContainingConstructor: null,
                    delegateTypeBeingInvoked: null,
                    returnRefKind: funcPtr.Signature.RefKind);

                return new BoundFunctionPointerInvocation(
                    node,
                    boundExpression,
                    BuildArgumentsForErrorRecovery(analyzedArguments, StaticCast<MethodSymbol>.From(methods)),
                    analyzedArguments.RefKinds.ToImmutableOrNull(),
                    LookupResultKind.OverloadResolutionFailure,
                    funcPtr.Signature.ReturnType,
                    hasErrors: true);
            }

            methodsBuilder.Free();

            MemberResolutionResult<FunctionPointerMethodSymbol> methodResult = overloadResolutionResult.ValidResult;
            CheckAndCoerceArguments(methodResult, analyzedArguments, diagnostics, receiver: null, invokedAsExtensionMethod: false);

            var args = analyzedArguments.Arguments.ToImmutable();
            var refKinds = analyzedArguments.RefKinds.ToImmutableOrNull();

            bool hasErrors = ReportUnsafeIfNotAllowed(node, diagnostics);
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
