// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private BoundExpression BindMethodGroup(ExpressionSyntax node, bool invoked, bool indexed, DiagnosticBag diagnostics)
        {
            switch (node.Kind())
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
        /// <param name="allowUnexpandedForm">False to prevent selecting a params method in unexpanded form.</param>
        /// <returns>Synthesized method invocation expression.</returns>
        internal BoundExpression MakeInvocationExpression(
            CSharpSyntaxNode node,
            BoundExpression receiver,
            string methodName,
            ImmutableArray<BoundExpression> args,
            DiagnosticBag diagnostics,
            SeparatedSyntaxList<TypeSyntax> typeArgsSyntax = default(SeparatedSyntaxList<TypeSyntax>),
            ImmutableArray<TypeSymbolWithAnnotations> typeArgs = default(ImmutableArray<TypeSymbolWithAnnotations>),
            CSharpSyntaxNode queryClause = null,
            bool allowFieldsAndProperties = false,
            bool allowUnexpandedForm = true)
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

                    analyzedArguments.Arguments[i] = GenerateConversionForAssignment(objType, argument, diagnostics);
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
                result = BindDynamicInvocation(node, boundExpression, analyzedArguments, ImmutableArray<MethodSymbol>.Empty, diagnostics, queryClause);
            }
            else if (boundExpression.Kind == BoundKind.MethodGroup)
            {
                result = BindMethodGroupInvocation(node, expression, methodName, (BoundMethodGroup)boundExpression, analyzedArguments, diagnostics, queryClause, allowUnexpandedForm: allowUnexpandedForm);
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

                if ((methodGroup.LookupSymbolOpt as MethodSymbol)?.MethodKind == MethodKind.LocalFunction)
                {
                    diagnostics.Add(ErrorCode.ERR_DynamicLocalFunctionParameter, node.Location, methodGroup.Syntax);
                }

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
                                // the base constructor is called. We need to handle this as a type qualified static method call.
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
                result = BindInvocationExpressionContinued(node, expression, methodName, overloadResolutionResult, analyzedArguments, methodGroup, delegateType, diagnostics, false, queryClause);
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
            CSharpSyntaxNode queryClause,
            bool allowUnexpandedForm = true)
        {
            BoundExpression result;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var resolution = this.ResolveMethodGroup(
                methodGroup, expression, methodName, analyzedArguments, isMethodGroupConversion: false,
                useSiteDiagnostics: ref useSiteDiagnostics, allowUnexpandedForm: allowUnexpandedForm);
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
                    isDelegate: false,
                    extensionMethodsOfSameViabilityAreAvailable: resolution.ExtensionMethodsOfSameViabilityAreAvailable);
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
                        result = BindInvocationExpressionContinued(syntax, expression, methodName, resolution.OverloadResolutionResult, resolution.AnalyzedArguments, resolution.MethodGroup, null, discarded, 
                                                                   resolution.ExtensionMethodsOfSameViabilityAreAvailable, queryClause);
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
                        result = BindInvocationExpressionContinued(
                            syntax, expression, methodName, resolution.OverloadResolutionResult, resolution.AnalyzedArguments,
                            resolution.MethodGroup, null, diagnostics, resolution.ExtensionMethodsOfSameViabilityAreAvailable, queryClause);
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
                    //   or a member-access whose receiver can't be classified as a type or value until after overload resolution (see §7.6.4.1). 
                    // * If F is an instance method, the method group must have resulted from a simple-name, a member-access through a variable or value, 
                    //   or a member-access whose receiver can't be classified as a type or value until after overload resolution (see §7.6.4.1).

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
            Debug.Assert(diagnostics != null);

            // It is never legal to box a restricted type, even if we are boxing it as the receiver
            // of a method call. When must be box? We skip boxing when the method in question is defined
            // on the restricted type or overridden by the restricted type.
            switch (expression.Kind)
            {
                case BoundKind.Call:
                    {
                        var call = (BoundCall)expression;
                        if (!call.HasAnyErrors &&
                            call.ReceiverOpt != null &&
                            (object)call.ReceiverOpt.Type != null &&
                            call.ReceiverOpt.Type.IsRestrictedType() &&
                            call.Method.ContainingType != call.ReceiverOpt.Type)
                        {
                            // error CS0029: Cannot implicitly convert type 'TypedReference' to 'object'
                            SymbolDistinguisher distinguisher = new SymbolDistinguisher(compilation, call.ReceiverOpt.Type, call.Method.ContainingType);
                            Error(diagnostics, ErrorCode.ERR_NoImplicitConv, call.ReceiverOpt.Syntax, distinguisher.First, distinguisher.Second);
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
        /// <param name="extensionMethodsOfSameViabilityAreAvailable">
        /// Indicates that there are additional extension method candidates of the same lookup viability in cases when 
        /// none of the instance methods are applicable to the argument list.
        /// </param>
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
            bool extensionMethodsOfSameViabilityAreAvailable,
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
                    // Errors for arguments have already been reported, except for unbound lambdas.
                    // We report those now.
                    foreach (var argument in analyzedArguments.Arguments)
                    {
                        var unboundLambda = argument as UnboundLambda;
                        if (unboundLambda != null)
                        {
                            var boundWithErrors = unboundLambda.BindForErrorRecovery();
                            diagnostics.AddRange(boundWithErrors.Diagnostics);
                        }
                    }
                }
                else
                {
                    // Since there were no argument errors to report, we report an error on the invocation itself.
                    string name = (object)delegateTypeOpt == null ? methodName : null;
                    result.ReportDiagnostics(this, GetLocationForOverloadResolutionDiagnostic(node, expression), diagnostics, name,
                        methodGroup.Receiver, analyzedArguments, methodGroup.Methods.ToImmutable(),
                        typeContainingConstructor: null, delegateTypeBeingInvoked: delegateTypeOpt,
                        queryClause: queryClause);
                }

                return CreateBadCall(node, methodGroup.Name, invokedAsExtensionMethod && analyzedArguments.Arguments.Count > 0 && (object)methodGroup.Receiver == (object)analyzedArguments.Arguments[0] ? null : methodGroup.Receiver,
                    GetOriginalMethods(result), methodGroup.ResultKind, methodGroup.TypeArguments.ToImmutable(), analyzedArguments, invokedAsExtensionMethod: invokedAsExtensionMethod, isDelegate: ((object)delegateTypeOpt != null),
                    extensionMethodsOfSameViabilityAreAvailable: extensionMethodsOfSameViabilityAreAvailable);
            }

            // Otherwise, there were no dynamic arguments and overload resolution found a unique best candidate. 
            // We still have to determine if it passes final validation.

            var methodResult = result.ValidResult;
            var returnType = methodResult.Member.ReturnType.TypeSymbol;
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
                BoundExpression convertedReceiver = CreateConversion(receiver, methodResult.Result.ConversionForArg(0), method.Parameters[0].Type.TypeSymbol, diagnostics);
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

            Debug.Assert(args.IsDefaultOrEmpty || (object)receiver != (object)args[0]);

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

                if (!method.IsStatic)
                {
                    WarnOnAccessOfOffDefault(node.Kind() == SyntaxKind.InvocationExpression ?
                                                ((InvocationExpressionSyntax)node).Expression :
                                                node,
                                             receiver,
                                             diagnostics);
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
        /// or a value (if useType is false).  Any other node is unmodified.
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
            }

            return receiver;
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
            CSharpSyntaxNode node,
            string name,
            BoundExpression receiver,
            ImmutableArray<MethodSymbol> methods,
            LookupResultKind resultKind,
            ImmutableArray<TypeSymbol> typeArguments,
            AnalyzedArguments analyzedArguments,
            bool invokedAsExtensionMethod,
            bool isDelegate,
            bool extensionMethodsOfSameViabilityAreAvailable)
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

            if (methods.Length == 1 && !extensionMethodsOfSameViabilityAreAvailable)
            {
                // If there is only one method in the group and no additional extension methods with the same lookup viability,
                // we should attempt to bind to it.  That includes binding any lambdas in the argument list against
                // the method's parameter types.
                method = methods[0];
                args = BuildArgumentsForErrorRecovery(analyzedArguments, method.Parameters);
            }
            else
            {
                var returnType = GetCommonTypeOrReturnType(methods) ?? new ExtendedErrorTypeSymbol(this.Compilation, string.Empty, arity: 0, errorInfo: null);
                var methodContainer = (object)receiver != null && (object)receiver.Type != null
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

                if (argumentKind == BoundKind.UnboundLambda && i < parameterCount)
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
                                    NamedTypeSymbol parameterType = parameters[i].Type.TypeSymbol as NamedTypeSymbol;
                                    if ((object)parameterType != null)
                                    {
                                        newArguments[i] = ((UnboundLambda)oldArgument).Bind(parameterType);
                                    }
                                    break;
                            }
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
                TypeSymbol returnType = members[i].GetTypeOrReturnType().TypeSymbol;
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
            CheckFeatureAvailability(node.GetLocation(), MessageID.IDS_FeatureNameof, diagnostics);
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
    }
}
