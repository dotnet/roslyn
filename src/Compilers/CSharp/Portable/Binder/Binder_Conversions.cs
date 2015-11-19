﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        protected BoundExpression CreateConversion(
            BoundExpression source,
            TypeSymbol destination,
            DiagnosticBag diagnostics)
        {
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var conversion = Conversions.ClassifyConversionFromExpression(source, destination, ref useSiteDiagnostics);

            diagnostics.Add(source.Syntax, useSiteDiagnostics);
            return CreateConversion(source.Syntax, source, conversion, isCast: false, destination: destination, diagnostics: diagnostics);
        }

        protected BoundExpression CreateConversion(
            BoundExpression source,
            Conversion conversion,
            TypeSymbol destination,
            DiagnosticBag diagnostics)
        {
            return CreateConversion(source.Syntax, source, conversion, isCast: false, destination: destination, diagnostics: diagnostics);
        }

        protected BoundExpression CreateConversion(
            CSharpSyntaxNode syntax,
            BoundExpression source,
            Conversion conversion,
            bool isCast,
            TypeSymbol destination,
            DiagnosticBag diagnostics)
        {
            return CreateConversion(syntax, source, conversion, isCast, source.WasCompilerGenerated, destination, diagnostics);
        }

        protected BoundExpression CreateConversion(
            CSharpSyntaxNode syntax,
            BoundExpression source,
            Conversion conversion,
            bool isCast,
            bool wasCompilerGenerated,
            TypeSymbol destination,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(source != null);
            Debug.Assert((object)destination != null);

            // We need to preserve any conversion that changes the type (even identity conversions, like object->dynamic),
            // or that was explicitly written in code (so that GetSemanticInfo can find the syntax in the bound tree).
            if (conversion.Kind == ConversionKind.Identity && !isCast && source.Type == destination)
            {
                return source;
            }

            ReportDiagnosticsIfObsolete(diagnostics, conversion, syntax, hasBaseReceiver: false);

            if (conversion.IsMethodGroup)
            {
                return CreateMethodGroupConversion(syntax, source, conversion, isCast, destination, diagnostics);
            }

            if (conversion.IsAnonymousFunction && source.Kind == BoundKind.UnboundLambda)
            {
                return CreateAnonymousFunctionConversion(syntax, source, conversion, isCast, destination, diagnostics);
            }

            if (conversion.IsUserDefined)
            {
                return CreateUserDefinedConversion(syntax, source, conversion, isCast, destination, diagnostics);
            }

            ConstantValue constantValue = this.FoldConstantConversion(syntax, source, conversion, destination, diagnostics);
            return new BoundConversion(
                syntax,
                source,
                conversion,
                IsCheckedConversion(source.Type, destination),
                explicitCastInCode: isCast && !wasCompilerGenerated,
                constantValueOpt: constantValue,
                type: destination)
            { WasCompilerGenerated = wasCompilerGenerated };
        }

        private bool IsCheckedConversion(TypeSymbol source, TypeSymbol target)
        {
            Debug.Assert((object)target != null);

            if ((object)source == null || !CheckOverflowAtRuntime)
            {
                return false;
            }

            if (source.IsDynamic())
            {
                return true;
            }

            SpecialType sourceST = source.StrippedType().EnumUnderlyingType().SpecialType;
            SpecialType targetST = target.StrippedType().EnumUnderlyingType().SpecialType;

            // integral to double or float is never checked, but float/double to integral 
            // may be checked.
            bool sourceIsNumeric = SpecialType.System_Char <= sourceST && sourceST <= SpecialType.System_Double;
            bool targetIsNumeric = SpecialType.System_Char <= targetST && targetST <= SpecialType.System_UInt64;

            return
                sourceIsNumeric && (targetIsNumeric || target.IsPointerType()) ||
                targetIsNumeric && source.IsPointerType();
        }

        protected BoundExpression CreateUserDefinedConversion(CSharpSyntaxNode syntax, BoundExpression source, Conversion conversion, bool isCast, TypeSymbol destination, DiagnosticBag diagnostics)
        {
            if (!conversion.IsValid)
            {
                GenerateImplicitConversionError(diagnostics, syntax, conversion, source, destination);

                return new BoundConversion(
                    syntax,
                    source,
                    conversion,
                    CheckOverflowAtRuntime,
                    explicitCastInCode: isCast,
                    constantValueOpt: ConstantValue.NotAvailable,
                    type: destination,
                    hasErrors: true)
                { WasCompilerGenerated = source.WasCompilerGenerated };
            }

            // Due to an oddity in the way we create a non-lifted user-defined conversion from A to D? 
            // (required backwards compatibility with the native compiler) we can end up in a situation 
            // where we have:
            // a standard conversion from A to B?
            // then a standard conversion from B? to B
            // then a user-defined  conversion from B to C
            // then a standard conversion from C to C? 
            // then a standard conversion from C? to D?
            //
            // In that scenario, the "from type" of the conversion will be B? and the "from conversion" will be 
            // from A to B?. Similarly the "to type" of the conversion will be C? and the "to conversion"
            // of the conversion will be from C? to D?.
            //
            // Therefore, we might need to introduce an extra conversion on the source side, from B? to B.
            // Now, you might think we should also introduce an extra conversion on the destination side,
            // from C to C?. But that then gives us the following bad situation: If we in fact bind this as
            //
            // (D?)(C?)(C)(B)(B?)(A)x 
            //
            // then what we are in effect doing is saying "convert C? to D? by checking for null, unwrapping,
            // converting C to D, and then wrapping". But we know that the C? will never be null. In this case
            // we should actually generate
            //
            // (D?)(C)(B)(B?)(A)x
            //
            // And thereby skip the unnecessary nullable conversion.

            // Original expression --> conversion's "from" type
            BoundExpression convertedOperand = CreateConversion(
                syntax: source.Syntax,
                source: source,
                conversion: conversion.UserDefinedFromConversion,
                isCast: false,
                wasCompilerGenerated: true,
                destination: conversion.BestUserDefinedConversionAnalysis.FromType,
                diagnostics: diagnostics);

            TypeSymbol conversionParameterType = conversion.BestUserDefinedConversionAnalysis.Operator.ParameterTypes[0];
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            if (conversion.BestUserDefinedConversionAnalysis.Kind == UserDefinedConversionAnalysisKind.ApplicableInNormalForm &&
                conversion.BestUserDefinedConversionAnalysis.FromType != conversionParameterType)
            {
                // Conversion's "from" type --> conversion method's parameter type.
                convertedOperand = CreateConversion(
                    syntax: syntax,
                    source: convertedOperand,
                    conversion: Conversions.ClassifyStandardConversion(null, convertedOperand.Type, conversionParameterType, ref useSiteDiagnostics),
                    isCast: false,
                    wasCompilerGenerated: true,
                    destination: conversionParameterType,
                    diagnostics: diagnostics);
            }

            BoundExpression userDefinedConversion;

            TypeSymbol conversionReturnType = conversion.BestUserDefinedConversionAnalysis.Operator.ReturnType;
            TypeSymbol conversionToType = conversion.BestUserDefinedConversionAnalysis.ToType;
            Conversion toConversion = conversion.UserDefinedToConversion;

            if (conversion.BestUserDefinedConversionAnalysis.Kind == UserDefinedConversionAnalysisKind.ApplicableInNormalForm &&
                conversionToType != conversionReturnType)
            {
                // Conversion method's parameter type --> conversion method's return type
                // NB: not calling CreateConversion here because this is the recursive base case.
                userDefinedConversion = new BoundConversion(
                    syntax,
                    convertedOperand,
                    conversion,
                    @checked: false, // There are no checked user-defined conversions, but the conversions on either side might be checked.
                    explicitCastInCode: isCast,
                    constantValueOpt: ConstantValue.NotAvailable,
                    type: conversionReturnType)
                { WasCompilerGenerated = true };

                if (conversionToType.IsNullableType() && conversionToType.GetNullableUnderlyingType() == conversionReturnType)
                {
                    // Skip introducing the conversion from C to C?.  The "to" conversion is now wrong though,
                    // because it will still assume converting C? to D?. 

                    toConversion = Conversions.ClassifyConversion(conversionReturnType, destination, ref useSiteDiagnostics);
                    Debug.Assert(toConversion.Exists);
                }
                else
                {
                    // Conversion method's return type --> conversion's "to" type
                    userDefinedConversion = CreateConversion(
                        syntax: syntax,
                        source: userDefinedConversion,
                        conversion: Conversions.ClassifyStandardConversion(null, conversionReturnType, conversion.BestUserDefinedConversionAnalysis.ToType, ref useSiteDiagnostics),
                        isCast: false,
                        wasCompilerGenerated: true,
                        destination: conversion.BestUserDefinedConversionAnalysis.ToType,
                        diagnostics: diagnostics);
                }
            }
            else
            {
                // Conversion method's parameter type --> conversion method's "to" type
                // NB: not calling CreateConversion here because this is the recursive base case.
                userDefinedConversion = new BoundConversion(
                    syntax,
                    convertedOperand,
                    conversion,
                    @checked: false,
                    explicitCastInCode: isCast,
                    constantValueOpt: ConstantValue.NotAvailable,
                    type: conversion.BestUserDefinedConversionAnalysis.ToType)
                { WasCompilerGenerated = true };
            }

            diagnostics.Add(syntax, useSiteDiagnostics);

            // Conversion's "to" type --> final type
            BoundExpression finalConversion = CreateConversion(
                syntax: syntax,
                source: userDefinedConversion,
                conversion: toConversion,
                isCast: false,
                wasCompilerGenerated: true, // NOTE: doesn't necessarily set flag on resulting bound expression.
                destination: destination,
                diagnostics: diagnostics);

            finalConversion.ResetCompilerGenerated(source.WasCompilerGenerated);

            return finalConversion;
        }

        private static BoundExpression CreateAnonymousFunctionConversion(CSharpSyntaxNode syntax, BoundExpression source, Conversion conversion, bool isCast, TypeSymbol destination, DiagnosticBag diagnostics)
        {
            // We have a successful anonymous function conversion; rather than producing a node
            // which is a conversion on top of an unbound lambda, replace it with the bound
            // lambda.

            // UNDONE: Figure out what to do about the error case, where a lambda
            // UNDONE: is converted to a delegate that does not match. What to surface then?

            var unboundLambda = (UnboundLambda)source;
            var boundLambda = unboundLambda.Bind((NamedTypeSymbol)destination);
            diagnostics.AddRange(boundLambda.Diagnostics);

            return new BoundConversion(
                syntax,
                boundLambda,
                conversion,
                @checked: false,
                explicitCastInCode: isCast,
                constantValueOpt: ConstantValue.NotAvailable,
                type: destination)
            { WasCompilerGenerated = source.WasCompilerGenerated };
        }

        private BoundExpression CreateMethodGroupConversion(CSharpSyntaxNode syntax, BoundExpression source, Conversion conversion, bool isCast, TypeSymbol destination, DiagnosticBag diagnostics)
        {
            BoundMethodGroup group = FixMethodGroupWithTypeOrValue((BoundMethodGroup)source, conversion, diagnostics);
            BoundExpression receiverOpt = group.ReceiverOpt;
            MethodSymbol method = conversion.Method;
            bool hasErrors = false;
            if (receiverOpt != null && receiverOpt.Kind == BoundKind.BaseReference && method.IsAbstract)
            {
                Error(diagnostics, ErrorCode.ERR_AbstractBaseCall, syntax, method);
                hasErrors = true;
            }

            NamedTypeSymbol delegateType = (NamedTypeSymbol)destination;
            if (MethodGroupConversionHasErrors(syntax, conversion, group.ReceiverOpt, conversion.IsExtensionMethod, delegateType, diagnostics))
            {
                hasErrors = true;
            }

            return new BoundConversion(syntax, group, conversion, @checked: false, explicitCastInCode: isCast, constantValueOpt: ConstantValue.NotAvailable, type: destination, hasErrors: hasErrors) { WasCompilerGenerated = source.WasCompilerGenerated };
        }

        private static bool IsMethodGroupWithTypeOrValueReceiver(BoundNode node)
        {
            if (node.Kind != BoundKind.MethodGroup)
            {
                return false;
            }

            BoundNode receiverOpt = ((BoundMethodGroup)node).ReceiverOpt;
            return receiverOpt != null && receiverOpt.Kind == BoundKind.TypeOrValueExpression;
        }


        private BoundMethodGroup FixMethodGroupWithTypeOrValue(BoundMethodGroup group, Conversion conversion, DiagnosticBag diagnostics)
        {
            if (!IsMethodGroupWithTypeOrValueReceiver(group))
            {
                return group;
            }

            BoundExpression receiverOpt = group.ReceiverOpt;
            Debug.Assert(receiverOpt != null);
            Debug.Assert((object)conversion.Method != null);
            receiverOpt = ReplaceTypeOrValueReceiver(receiverOpt, conversion.Method.IsStatic && !conversion.IsExtensionMethod, diagnostics);
            return group.Update(
                group.TypeArgumentsOpt,
                group.Name,
                group.Methods,
                group.LookupSymbolOpt,
                group.LookupError,
                group.Flags,
                receiverOpt, //only change
                group.ResultKind);
        }

        /// <summary>
        /// This method implements the algorithm in spec section 7.6.5.1.
        /// 
        /// For method group conversions, there are situations in which the conversion is
        /// considered to exist ("Otherwise the algorithm produces a single best method M having
        /// the same number of parameters as D and the conversion is considered to exist"), but
        /// application of the conversion fails.  These are the "final validation" steps of
        /// overload resolution.
        /// </summary>
        /// <returns>
        /// True if there is any error.
        /// </returns>
        private bool MemberGroupFinalValidation(BoundExpression receiverOpt, MethodSymbol methodSymbol, CSharpSyntaxNode node, DiagnosticBag diagnostics, bool invokedAsExtensionMethod)
        {
            if (MemberGroupFinalValidationAccessibilityChecks(receiverOpt, methodSymbol, node, diagnostics, invokedAsExtensionMethod))
            {
                return true;
            }

            // SPEC: If the best method is a generic method, the type arguments (supplied or inferred) are checked against the constraints 
            // SPEC: declared on the generic method. If any type argument does not satisfy the corresponding constraint(s) on
            // SPEC: the type parameter, a binding-time error occurs.

            // The portion of the overload resolution spec quoted above is subtle and somewhat
            // controversial. The upshot of this is that overload resolution does not consider
            // constraints to be a part of the signature. Overload resolution matches arguments to
            // parameter lists; it does not consider things which are outside of the parameter list.
            // If the best match from the arguments to the formal parameters is not viable then we
            // give an error rather than falling back to a worse match. 
            //
            // Consider the following:
            //
            // void M<T>(T t) where T : Reptile {}
            // void M(object x) {}
            // ...
            // M(new Giraffe());
            //
            // The correct analysis is to determine that the applicable candidates are
            // M<Giraffe>(Giraffe) and M(object). Overload resolution then chooses the former
            // because it is an exact match, over the latter which is an inexact match. Only after
            // the best method is determined do we check the constraints and discover that the
            // constraint on T has been violated.
            // 
            // Note that this is different from the rule that says that during type inference, if an
            // inference violates a constraint then inference fails. For example:
            // 
            // class C<T> where T : struct {}
            // ...
            // void M<U>(U u, C<U> c){}
            // void M(object x, object y) {}
            // ...
            // M("hello", null);
            //
            // Type inference determines that U is string, but since C<string> is not a valid type
            // because of the constraint, type inference fails. M<string> is never added to the
            // applicable candidate set, so the applicable candidate set consists solely of
            // M(object, object) and is therefore the best match.

            return !methodSymbol.CheckConstraints(this.Conversions, node, this.Compilation, diagnostics);
        }

        /// <summary>
        /// Performs the following checks:
        /// 
        /// Spec 7.6.5: Invocation expressions (definition of Final Validation) 
        ///   The method is validated in the context of the method group: If the best method is a static method, 
        ///   the method group must have resulted from a simple-name or a member-access through a type. If the best 
        ///   method is an instance method, the method group must have resulted from a simple-name, a member-access
        ///   through a variable or value, or a base-access. If neither of these requirements is true, a binding-time
        ///   error occurs.
        ///   (Note that the spec omits to mention, in the case of an instance method invoked through a simple name, that
        ///   the invocation must appear within the body of an instance method)
        ///
        /// Spec 7.5.4: Compile-time checking of dynamic overload resolution 
        ///   If F is a static method, the method group must have resulted from a simple-name, a member-access through a type, 
        ///   or a member-access whose receiver can't be classified as a type or value until after overload resolution (see §7.6.4.1). 
        ///   If F is an instance method, the method group must have resulted from a simple-name, a member-access through a variable or value,
        ///   or a member-access whose receiver can't be classified as a type or value until after overload resolution (see §7.6.4.1).
        /// </summary>
        /// <returns>
        /// True if there is any error.
        /// </returns>
        private bool MemberGroupFinalValidationAccessibilityChecks(BoundExpression receiverOpt, Symbol memberSymbol, CSharpSyntaxNode node, DiagnosticBag diagnostics, bool invokedAsExtensionMethod)
        {
            // Perform final validation of the method to be invoked.

            Debug.Assert(memberSymbol.Kind != SymbolKind.Method || memberSymbol.CanBeReferencedByName); //should be true since the caller has LookupOptions.MustBeReferenceableByName set
            //note that the same assert does not hold for all properties. Some properties and (all indexers) are not referenceable by name, yet
            //their binding brings them through here, perhaps needlessly.

            if (receiverOpt != null && receiverOpt.Kind == BoundKind.TypeOrValueExpression)
            {
                // TypeOrValue expression isn't replaced only if the invocation is late bound, in which case it can't be extension method.
                // None of the checks below apply if the receiver can't be classified as a type or value. 
                Debug.Assert(!invokedAsExtensionMethod);
            }
            else if (memberSymbol.IsStatic)
            {
                Debug.Assert(!invokedAsExtensionMethod || (receiverOpt != null));
                if (!invokedAsExtensionMethod && !WasImplicitReceiver(receiverOpt) && IsMemberAccessedThroughVariableOrValue(receiverOpt))
                {
                    if (this.Flags.Includes(BinderFlags.CollectionInitializerAddMethod))
                    {
                        diagnostics.Add(ErrorCode.ERR_InitializerAddHasWrongSignature, node.Location, memberSymbol);
                    }
                    else if (node.Kind() == SyntaxKind.AwaitExpression && memberSymbol.Name == WellKnownMemberNames.GetAwaiter)
                    {
                        diagnostics.Add(ErrorCode.ERR_BadAwaitArg, node.Location, receiverOpt.Type);
                    }
                    else
                    {
                        diagnostics.Add(ErrorCode.ERR_ObjectProhibited, node.Location, memberSymbol);
                    }
                    return true;
                }
            }
            else if (IsMemberAccessedThroughType(receiverOpt))
            {
                diagnostics.Add(ErrorCode.ERR_ObjectRequired, node.Location, memberSymbol);
                return true;
            }
            else if (WasImplicitReceiver(receiverOpt))
            {
                if (InFieldInitializer && !ContainingType.IsScriptClass || InConstructorInitializer || InAttributeArgument)
                {
                    CSharpSyntaxNode errorNode = node;
                    if (node.Parent != null && node.Parent.Kind() == SyntaxKind.InvocationExpression)
                    {
                        errorNode = node.Parent;
                    }

                    ErrorCode code = InFieldInitializer ? ErrorCode.ERR_FieldInitRefNonstatic : ErrorCode.ERR_ObjectRequired;
                    diagnostics.Add(code, errorNode.Location, memberSymbol);
                    return true;
                }

                // If we could access the member through implicit "this" the receiver would be a BoundThisReference.
                // If it is null it means that the instance member is inaccessible.
                if (receiverOpt == null || ContainingMember().IsStatic)
                {
                    Error(diagnostics, ErrorCode.ERR_ObjectRequired, node, memberSymbol);
                    return true;
                }
            }

            var containingType = this.ContainingType;
            if ((object)containingType != null)
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                bool isAccessible = this.IsSymbolAccessibleConditional(memberSymbol.GetTypeOrReturnType(), containingType, ref useSiteDiagnostics);
                diagnostics.Add(node, useSiteDiagnostics);

                if (!isAccessible)
                {
                    // In the presence of non-transitive [InternalsVisibleTo] in source, or obnoxious symbols from metadata, it is possible
                    // to select a method through overload resolution in which the type is not accessible.  In this case a method cannot
                    // be called through normal IL, so we give an error.  Neither [InternalsVisibleTo] nor the need for this diagnostic is
                    // described by the language specification.
                    //
                    // Dev11 perform different access checks. See bug #530360 and tests AccessCheckTests.InaccessibleReturnType.
                    Error(diagnostics, ErrorCode.ERR_BadAccess, node, memberSymbol);
                    return true;
                }
            }

            return false;
        }

        private static bool IsMemberAccessedThroughVariableOrValue(BoundExpression receiverOpt)
        {
            if (receiverOpt == null)
            {
                return false;
            }

            return !IsMemberAccessedThroughType(receiverOpt);
        }

        private static bool IsMemberAccessedThroughType(BoundExpression receiverOpt)
        {
            if (receiverOpt == null)
            {
                return false;
            }

            while (receiverOpt.Kind == BoundKind.QueryClause)
            {
                receiverOpt = ((BoundQueryClause)receiverOpt).Value;
            }

            return receiverOpt.Kind == BoundKind.TypeExpression;
        }

        /// <summary>
        /// Was the receiver expression compiler-generated?
        /// </summary>
        private static bool WasImplicitReceiver(BoundExpression receiverOpt)
        {
            if (receiverOpt == null) return true;
            if (!receiverOpt.WasCompilerGenerated) return false;
            switch (receiverOpt.Kind)
            {
                case BoundKind.ThisReference:
                case BoundKind.HostObjectMemberReference:
                case BoundKind.PreviousSubmissionReference:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// This method implements the checks in spec section 15.2.
        /// </summary>
        private bool MethodGroupIsCompatibleWithDelegate(BoundExpression receiverOpt, bool isExtensionMethod, MethodSymbol method, NamedTypeSymbol delegateType, Location errorLocation, DiagnosticBag diagnostics)
        {
            Debug.Assert(delegateType.TypeKind == TypeKind.Delegate);
            Debug.Assert((object)delegateType.DelegateInvokeMethod != null && !delegateType.DelegateInvokeMethod.HasUseSiteError,
                         "This method should only be called for valid delegate types.");

            MethodSymbol delegateMethod = delegateType.DelegateInvokeMethod;

            Debug.Assert(!isExtensionMethod || (receiverOpt != null));

            // - Argument types "match", and
            var delegateParameters = delegateMethod.Parameters;
            var methodParameters = method.Parameters;
            int numParams = delegateParameters.Length;

            if (methodParameters.Length != numParams + (isExtensionMethod ? 1 : 0))
            {
                // This can happen if "method" has optional parameters.
                Debug.Assert(methodParameters.Length > numParams + (isExtensionMethod ? 1 : 0));
                Error(diagnostics, ErrorCode.ERR_MethDelegateMismatch, errorLocation, method, delegateType);
                return false;
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            // If this is an extension method delegate, the caller should have verified the
            // receiver is compatible with the "this" parameter of the extension method.
            Debug.Assert(!isExtensionMethod ||
                (Conversions.ConvertExtensionMethodThisArg(methodParameters[0].Type, receiverOpt.Type, ref useSiteDiagnostics).Exists && useSiteDiagnostics.IsNullOrEmpty()));

            useSiteDiagnostics = null;

            for (int i = 0; i < numParams; i++)
            {
                var delegateParameterType = delegateParameters[i].Type;
                var methodParameterType = methodParameters[isExtensionMethod ? i + 1 : i].Type;

                if (!Conversions.HasIdentityOrImplicitReferenceConversion(delegateParameterType, methodParameterType, ref useSiteDiagnostics))
                {
                    // No overload for '{0}' matches delegate '{1}'
                    Error(diagnostics, ErrorCode.ERR_MethDelegateMismatch, errorLocation, method, delegateType);
                    diagnostics.Add(errorLocation, useSiteDiagnostics);
                    return false;
                }
            }

            // - Return types "match"
            var returnsMatch =
                method.ReturnsVoid && delegateMethod.ReturnsVoid ||
                Conversions.HasIdentityOrImplicitReferenceConversion(method.ReturnType, delegateMethod.ReturnType, ref useSiteDiagnostics);
            if (!returnsMatch)
            {
                Error(diagnostics, ErrorCode.ERR_BadRetType, errorLocation, method, method.ReturnType);
                diagnostics.Add(errorLocation, useSiteDiagnostics);
                return false;
            }

            diagnostics.Add(errorLocation, useSiteDiagnostics);

            if (method.IsConditional)
            {
                // CS1618: Cannot create delegate with '{0}' because it has a Conditional attribute
                Error(diagnostics, ErrorCode.ERR_DelegateOnConditional, errorLocation, method);
                return false;
            }

            var sourceMethod = method as SourceMemberMethodSymbol;
            if ((object)sourceMethod != null && sourceMethod.IsPartialWithoutImplementation)
            {
                // CS0762: Cannot create delegate from method '{0}' because it is a partial method without an implementing declaration
                Error(diagnostics, ErrorCode.ERR_PartialMethodToDelegate, errorLocation, method);
                return false;
            }

            return true;
        }

        /// <summary>
        /// This method combines final validation (section 7.6.5.1) and delegate compatibility (section 15.2).
        /// </summary>
        /// <param name="syntax">CSharpSyntaxNode of the expression requiring method group conversion.</param>
        /// <param name="conversion">Conversion to be performed.</param>
        /// <param name="receiverOpt">Optional receiver.</param>
        /// <param name="isExtensionMethod">Method invoked as extension method.</param>
        /// <param name="delegateType">Target delegate type.</param>
        /// <param name="diagnostics">Where diagnostics should be added.</param>
        /// <returns>True if a diagnostic has been added.</returns>
        private bool MethodGroupConversionHasErrors(
            CSharpSyntaxNode syntax,
            Conversion conversion,
            BoundExpression receiverOpt,
            bool isExtensionMethod,
            NamedTypeSymbol delegateType,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(delegateType.TypeKind == TypeKind.Delegate);

            MethodSymbol selectedMethod = conversion.Method;

            if (MemberGroupFinalValidation(receiverOpt, selectedMethod, syntax, diagnostics, isExtensionMethod) ||
                !MethodGroupIsCompatibleWithDelegate(receiverOpt, isExtensionMethod, selectedMethod, delegateType, syntax.Location, diagnostics))
            {
                return true;
            }

            if (selectedMethod.HasUnsafeParameter() || selectedMethod.ReturnType.IsUnsafe())
            {
                return ReportUnsafeIfNotAllowed(syntax, diagnostics);
            }

            // No use site errors, but there could be use site warnings.
            // If there are use site warnings, they were reported during the overload resolution process
            // that chose selectedMethod.
            Debug.Assert(!selectedMethod.HasUseSiteError, "Shouldn't have reached this point if there were use site errors.");

            return false;
        }

        /// <summary>
        /// This method is a wrapper around MethodGroupConversionHasErrors.  As a preliminary step,
        /// it checks whether a conversion exists.
        /// </summary>
        private bool MethodGroupConversionDoesNotExistOrHasErrors(
            BoundMethodGroup boundMethodGroup,
            NamedTypeSymbol delegateType,
            Location delegateMismatchLocation,
            DiagnosticBag diagnostics,
            out Conversion conversion)
        {
            if (ReportDelegateInvokeUseSiteDiagnostic(diagnostics, delegateType, delegateMismatchLocation))
            {
                conversion = Conversion.NoConversion;
                return true;
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            conversion = Conversions.GetMethodGroupConversion(boundMethodGroup, delegateType, ref useSiteDiagnostics);
            diagnostics.Add(delegateMismatchLocation, useSiteDiagnostics);
            if (!conversion.Exists)
            {
                // No overload for '{0}' matches delegate '{1}'
                diagnostics.Add(ErrorCode.ERR_MethDelegateMismatch, delegateMismatchLocation, boundMethodGroup.Name, delegateType);
                return true;
            }
            else
            {
                Debug.Assert(conversion.IsValid); // i.e. if it exists, then it is valid.
                // Only cares about nullness and type of receiver, so no need to worry about BoundTypeOrValueExpression.
                return this.MethodGroupConversionHasErrors(boundMethodGroup.Syntax, conversion, boundMethodGroup.ReceiverOpt, conversion.IsExtensionMethod, delegateType, diagnostics);
            }
        }

        public ConstantValue FoldConstantConversion(
            CSharpSyntaxNode syntax,
            BoundExpression source,
            Conversion conversion,
            TypeSymbol destination,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(source != null);
            Debug.Assert((object)destination != null);

            // The diagnostics bag can be null in cases where we know ahead of time that the
            // conversion will succeed without error or warning. (For example, if we have a valid
            // implicit numeric conversion on a constant of numeric type.)

            // SPEC: A constant expression must be the null literal or a value with one of 
            // SPEC: the following types: sbyte, byte, short, ushort, int, uint, long, 
            // SPEC: ulong, char, float, double, decimal, bool, string, or any enumeration type.

            // SPEC: The following conversions are permitted in constant expressions:
            // SPEC: Identity conversions
            // SPEC: Numeric conversions
            // SPEC: Enumeration conversions
            // SPEC: Constant expression conversions
            // SPEC: Implicit and explicit reference conversions, provided that the source of the conversions 
            // SPEC: is a constant expression that evaluates to the null value.

            // SPEC VIOLATION: C# has always allowed the following, even though this does violate the rule that
            // SPEC VIOLATION: a constant expression must be either the null literal, or an expression of one 
            // SPEC VIOLATION: of the given types. 

            // SPEC VIOLATION: const C c = (C)null;

            // TODO: Some conversions can produce errors or warnings depending on checked/unchecked.
            // TODO: Fold conversions on enums and strings too.

            if (source.HasAnyErrors)
            {
                return null;
            }

            var sourceConstantValue = source.ConstantValue;
            if (sourceConstantValue == null || sourceConstantValue.IsBad)
            {
                return sourceConstantValue;
            }

            switch (conversion.Kind)
            {
                case ConversionKind.Identity:
                case ConversionKind.NullLiteral:
                    return sourceConstantValue;

                case ConversionKind.ImplicitConstant:
                    return FoldConstantNumericConversion(syntax, sourceConstantValue, destination, diagnostics);

                case ConversionKind.ExplicitNumeric:
                case ConversionKind.ImplicitNumeric:
                case ConversionKind.ExplicitEnumeration:
                case ConversionKind.ImplicitEnumeration:
                    // The C# specification categorizes conversion from literal zero to nullable enum as 
                    // an Implicit Enumeration Conversion. Such a thing should not be constant folded
                    // because nullable enums are never constants.

                    if (destination.IsNullableType())
                    {
                        return null;
                    }

                    return FoldConstantNumericConversion(syntax, sourceConstantValue, destination, diagnostics);

                case ConversionKind.ExplicitReference:
                case ConversionKind.ImplicitReference:
                    return sourceConstantValue.IsNull ? sourceConstantValue : null;
            }

            return null;
        }

        private ConstantValue FoldConstantNumericConversion(
            CSharpSyntaxNode syntax,
            ConstantValue sourceValue,
            TypeSymbol destination,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(sourceValue != null);
            Debug.Assert(!sourceValue.IsBad);

            SpecialType destinationType;
            if ((object)destination != null && destination.IsEnumType())
            {
                var underlyingType = ((NamedTypeSymbol)destination).EnumUnderlyingType;
                Debug.Assert((object)underlyingType != null);
                Debug.Assert(underlyingType.SpecialType != SpecialType.None);
                destinationType = underlyingType.SpecialType;
            }
            else
            {
                destinationType = destination.GetSpecialTypeSafe();
            }

            // In an unchecked context we ignore overflowing conversions on conversions from any
            // integral type, float and double to any integral type. "unchecked" actually does not
            // affect conversions from decimal to any integral type; if those are out of bounds then
            // we always give an error regardless.

            if (sourceValue.IsDecimal)
            {
                if (!CheckConstantBounds(destinationType, sourceValue))
                {
                    // NOTE: Dev10 puts a suffix, "M", on the constant value.
                    Error(diagnostics, ErrorCode.ERR_ConstOutOfRange, syntax, sourceValue.Value + "M", destination);

                    return ConstantValue.Bad;
                }
            }
            else if (destinationType == SpecialType.System_Decimal)
            {
                if (!CheckConstantBounds(destinationType, sourceValue))
                {
                    Error(diagnostics, ErrorCode.ERR_ConstOutOfRange, syntax, sourceValue.Value, destination);

                    return ConstantValue.Bad;
                }
            }
            else if (CheckOverflowAtCompileTime)
            {
                if (!CheckConstantBounds(destinationType, sourceValue))
                {
                    Error(diagnostics, ErrorCode.ERR_ConstOutOfRangeChecked, syntax, sourceValue.Value, destination);

                    return ConstantValue.Bad;
                }
            }

            return ConstantValue.Create(DoUncheckedConversion(destinationType, sourceValue), destinationType);
        }

        private static object DoUncheckedConversion(SpecialType destinationType, ConstantValue value)
        {
            // Note that we keep "single" floats as doubles internally to maintain higher precision. However,
            // we do not do so in an entirely "lossless" manner. When *converting* to a float, we do lose 
            // the precision lost due to the conversion. But when doing arithmetic, we do the arithmetic on
            // the double values.
            //
            // An example will help. Suppose we have:
            //
            // const float cf1 = 1.0f;
            // const float cf2 = 1.0e-15f;
            // const double cd3 = cf1 - cf2;
            //
            // We first take the double-precision values for 1.0 and 1.0e-15 and round them to floats,
            // and then turn them back into doubles. Then when we do the subtraction, we do the subtraction
            // in doubles, not in floats. Had we done the subtraction in floats, we'd get 1.0; but instead we
            // do it in doubles and get 0.99999999999999.
            //
            // Similarly, if we have
            //
            // const int i4 = int.MaxValue; // 2147483647
            // const float cf5 = int.MaxValue; //  2147483648.0
            // const double cd6 = cf5; // 2147483648.0
            //
            // The int is converted to float and stored internally as the double 214783648, even though the
            // fully precise int would fit into a double.

            unchecked
            {
                switch (value.Discriminator)
                {
                    case ConstantValueTypeDiscriminator.Byte:
                        byte byteValue = value.ByteValue;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)byteValue;
                            case SpecialType.System_Char: return (char)byteValue;
                            case SpecialType.System_UInt16: return (ushort)byteValue;
                            case SpecialType.System_UInt32: return (uint)byteValue;
                            case SpecialType.System_UInt64: return (ulong)byteValue;
                            case SpecialType.System_SByte: return (sbyte)byteValue;
                            case SpecialType.System_Int16: return (short)byteValue;
                            case SpecialType.System_Int32: return (int)byteValue;
                            case SpecialType.System_Int64: return (long)byteValue;
                            case SpecialType.System_Single:
                            case SpecialType.System_Double: return (double)byteValue;
                            case SpecialType.System_Decimal: return (decimal)byteValue;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.Char:
                        char charValue = value.CharValue;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)charValue;
                            case SpecialType.System_Char: return (char)charValue;
                            case SpecialType.System_UInt16: return (ushort)charValue;
                            case SpecialType.System_UInt32: return (uint)charValue;
                            case SpecialType.System_UInt64: return (ulong)charValue;
                            case SpecialType.System_SByte: return (sbyte)charValue;
                            case SpecialType.System_Int16: return (short)charValue;
                            case SpecialType.System_Int32: return (int)charValue;
                            case SpecialType.System_Int64: return (long)charValue;
                            case SpecialType.System_Single:
                            case SpecialType.System_Double: return (double)charValue;
                            case SpecialType.System_Decimal: return (decimal)charValue;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.UInt16:
                        ushort uint16Value = value.UInt16Value;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)uint16Value;
                            case SpecialType.System_Char: return (char)uint16Value;
                            case SpecialType.System_UInt16: return (ushort)uint16Value;
                            case SpecialType.System_UInt32: return (uint)uint16Value;
                            case SpecialType.System_UInt64: return (ulong)uint16Value;
                            case SpecialType.System_SByte: return (sbyte)uint16Value;
                            case SpecialType.System_Int16: return (short)uint16Value;
                            case SpecialType.System_Int32: return (int)uint16Value;
                            case SpecialType.System_Int64: return (long)uint16Value;
                            case SpecialType.System_Single:
                            case SpecialType.System_Double: return (double)uint16Value;
                            case SpecialType.System_Decimal: return (decimal)uint16Value;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.UInt32:
                        uint uint32Value = value.UInt32Value;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)uint32Value;
                            case SpecialType.System_Char: return (char)uint32Value;
                            case SpecialType.System_UInt16: return (ushort)uint32Value;
                            case SpecialType.System_UInt32: return (uint)uint32Value;
                            case SpecialType.System_UInt64: return (ulong)uint32Value;
                            case SpecialType.System_SByte: return (sbyte)uint32Value;
                            case SpecialType.System_Int16: return (short)uint32Value;
                            case SpecialType.System_Int32: return (int)uint32Value;
                            case SpecialType.System_Int64: return (long)uint32Value;
                            case SpecialType.System_Single: return (double)(float)uint32Value;
                            case SpecialType.System_Double: return (double)uint32Value;
                            case SpecialType.System_Decimal: return (decimal)uint32Value;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.UInt64:
                        ulong uint64Value = value.UInt64Value;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)uint64Value;
                            case SpecialType.System_Char: return (char)uint64Value;
                            case SpecialType.System_UInt16: return (ushort)uint64Value;
                            case SpecialType.System_UInt32: return (uint)uint64Value;
                            case SpecialType.System_UInt64: return (ulong)uint64Value;
                            case SpecialType.System_SByte: return (sbyte)uint64Value;
                            case SpecialType.System_Int16: return (short)uint64Value;
                            case SpecialType.System_Int32: return (int)uint64Value;
                            case SpecialType.System_Int64: return (long)uint64Value;
                            case SpecialType.System_Single: return (double)(float)uint64Value;
                            case SpecialType.System_Double: return (double)uint64Value;
                            case SpecialType.System_Decimal: return (decimal)uint64Value;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.SByte:
                        sbyte sbyteValue = value.SByteValue;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)sbyteValue;
                            case SpecialType.System_Char: return (char)sbyteValue;
                            case SpecialType.System_UInt16: return (ushort)sbyteValue;
                            case SpecialType.System_UInt32: return (uint)sbyteValue;
                            case SpecialType.System_UInt64: return (ulong)sbyteValue;
                            case SpecialType.System_SByte: return (sbyte)sbyteValue;
                            case SpecialType.System_Int16: return (short)sbyteValue;
                            case SpecialType.System_Int32: return (int)sbyteValue;
                            case SpecialType.System_Int64: return (long)sbyteValue;
                            case SpecialType.System_Single:
                            case SpecialType.System_Double: return (double)sbyteValue;
                            case SpecialType.System_Decimal: return (decimal)sbyteValue;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.Int16:
                        short int16Value = value.Int16Value;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)int16Value;
                            case SpecialType.System_Char: return (char)int16Value;
                            case SpecialType.System_UInt16: return (ushort)int16Value;
                            case SpecialType.System_UInt32: return (uint)int16Value;
                            case SpecialType.System_UInt64: return (ulong)int16Value;
                            case SpecialType.System_SByte: return (sbyte)int16Value;
                            case SpecialType.System_Int16: return (short)int16Value;
                            case SpecialType.System_Int32: return (int)int16Value;
                            case SpecialType.System_Int64: return (long)int16Value;
                            case SpecialType.System_Single:
                            case SpecialType.System_Double: return (double)int16Value;
                            case SpecialType.System_Decimal: return (decimal)int16Value;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.Int32:
                        int int32Value = value.Int32Value;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)int32Value;
                            case SpecialType.System_Char: return (char)int32Value;
                            case SpecialType.System_UInt16: return (ushort)int32Value;
                            case SpecialType.System_UInt32: return (uint)int32Value;
                            case SpecialType.System_UInt64: return (ulong)int32Value;
                            case SpecialType.System_SByte: return (sbyte)int32Value;
                            case SpecialType.System_Int16: return (short)int32Value;
                            case SpecialType.System_Int32: return (int)int32Value;
                            case SpecialType.System_Int64: return (long)int32Value;
                            case SpecialType.System_Single: return (double)(float)int32Value;
                            case SpecialType.System_Double: return (double)int32Value;
                            case SpecialType.System_Decimal: return (decimal)int32Value;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.Int64:
                        long int64Value = value.Int64Value;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)int64Value;
                            case SpecialType.System_Char: return (char)int64Value;
                            case SpecialType.System_UInt16: return (ushort)int64Value;
                            case SpecialType.System_UInt32: return (uint)int64Value;
                            case SpecialType.System_UInt64: return (ulong)int64Value;
                            case SpecialType.System_SByte: return (sbyte)int64Value;
                            case SpecialType.System_Int16: return (short)int64Value;
                            case SpecialType.System_Int32: return (int)int64Value;
                            case SpecialType.System_Int64: return (long)int64Value;
                            case SpecialType.System_Single: return (double)(float)int64Value;
                            case SpecialType.System_Double: return (double)int64Value;
                            case SpecialType.System_Decimal: return (decimal)int64Value;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.Single:
                    case ConstantValueTypeDiscriminator.Double:
                        // This code used to invoke CheckConstantBounds and return constant zero if the value is not within the target type.
                        // The C# spec says that in this case the result of the conversion is an unspecified value of the destination type.
                        // Zero is a perfectly valid unspecified value, so that behavior was formally correct.
                        // But it did not agree with the behavior of the native C# compiler, that apparently returned a value that
                        // would resulted from a runtime conversion with normal CLR overflow behavior.
                        // To avoid breaking programs that might accidentally rely on that unspecified behavior
                        // we now removed that check and just allow conversion to overflow.
                        double doubleValue = value.DoubleValue;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)doubleValue;
                            case SpecialType.System_Char: return (char)doubleValue;
                            case SpecialType.System_UInt16: return (ushort)doubleValue;
                            case SpecialType.System_UInt32: return (uint)doubleValue;
                            case SpecialType.System_UInt64: return (ulong)doubleValue;
                            case SpecialType.System_SByte: return (sbyte)doubleValue;
                            case SpecialType.System_Int16: return (short)doubleValue;
                            case SpecialType.System_Int32: return (int)doubleValue;
                            case SpecialType.System_Int64: return (long)doubleValue;
                            case SpecialType.System_Single: return (double)(float)doubleValue;
                            case SpecialType.System_Double: return (double)doubleValue;
                            case SpecialType.System_Decimal: return (value.Discriminator == ConstantValueTypeDiscriminator.Single) ? (decimal)(float)doubleValue : (decimal)doubleValue;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.Decimal:
                        decimal decimalValue = CheckConstantBounds(destinationType, value.DecimalValue) ? value.DecimalValue : 0m;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)decimalValue;
                            case SpecialType.System_Char: return (char)decimalValue;
                            case SpecialType.System_UInt16: return (ushort)decimalValue;
                            case SpecialType.System_UInt32: return (uint)decimalValue;
                            case SpecialType.System_UInt64: return (ulong)decimalValue;
                            case SpecialType.System_SByte: return (sbyte)decimalValue;
                            case SpecialType.System_Int16: return (short)decimalValue;
                            case SpecialType.System_Int32: return (int)decimalValue;
                            case SpecialType.System_Int64: return (long)decimalValue;
                            case SpecialType.System_Single: return (double)(float)decimalValue;
                            case SpecialType.System_Double: return (double)decimalValue;
                            case SpecialType.System_Decimal: return (decimal)decimalValue;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    default:
                        throw ExceptionUtilities.UnexpectedValue(value.Discriminator);
                }
            }

            // all cases should have been handled in the switch above.
            // return value.Value;
        }

        public static bool CheckConstantBounds(SpecialType destinationType, ConstantValue value)
        {
            if (value.IsBad)
            {
                //assume that the constant was intended to be in bounds
                return true;
            }

            // Compute whether the value fits into the bounds of the given destination type without
            // error. We know that the constant will fit into either a double or a decimal, so
            // convert it to one of those and then check the bounds on that.
            var canonicalValue = CanonicalizeConstant(value);
            return canonicalValue is decimal ?
                CheckConstantBounds(destinationType, (decimal)canonicalValue) :
                CheckConstantBounds(destinationType, (double)canonicalValue);
        }

        private static bool CheckConstantBounds(SpecialType destinationType, double value)
        {
            // Dev10 checks (minValue - 1) < value < (maxValue + 1).
            // See ExpressionBinder::isConstantInRange.
            switch (destinationType)
            {
                case SpecialType.System_Byte: return (byte.MinValue - 1D) < value && value < (byte.MaxValue + 1D);
                case SpecialType.System_Char: return (char.MinValue - 1D) < value && value < (char.MaxValue + 1D);
                case SpecialType.System_UInt16: return (ushort.MinValue - 1D) < value && value < (ushort.MaxValue + 1D);
                case SpecialType.System_UInt32: return (uint.MinValue - 1D) < value && value < (uint.MaxValue + 1D);
                case SpecialType.System_UInt64: return (ulong.MinValue - 1D) < value && value < (ulong.MaxValue + 1D);
                case SpecialType.System_SByte: return (sbyte.MinValue - 1D) < value && value < (sbyte.MaxValue + 1D);
                case SpecialType.System_Int16: return (short.MinValue - 1D) < value && value < (short.MaxValue + 1D);
                case SpecialType.System_Int32: return (int.MinValue - 1D) < value && value < (int.MaxValue + 1D);
                // Note: Using <= to compare the min value matches the native compiler.
                case SpecialType.System_Int64: return (long.MinValue - 1D) <= value && value < (long.MaxValue + 1D);
                case SpecialType.System_Decimal: return ((double)decimal.MinValue - 1D) < value && value < ((double)decimal.MaxValue + 1D);
            }

            return true;
        }

        private static bool CheckConstantBounds(SpecialType destinationType, decimal value)
        {
            // Dev10 checks (minValue - 1) < value < (MaxValue + 1) + 1).
            // See ExpressionBinder::isConstantInRange.
            switch (destinationType)
            {
                case SpecialType.System_Byte: return (byte.MinValue - 1M) < value && value < (byte.MaxValue + 1M);
                case SpecialType.System_Char: return (char.MinValue - 1M) < value && value < (char.MaxValue + 1M);
                case SpecialType.System_UInt16: return (ushort.MinValue - 1M) < value && value < (ushort.MaxValue + 1M);
                case SpecialType.System_UInt32: return (uint.MinValue - 1M) < value && value < (uint.MaxValue + 1M);
                case SpecialType.System_UInt64: return (ulong.MinValue - 1M) < value && value < (ulong.MaxValue + 1M);
                case SpecialType.System_SByte: return (sbyte.MinValue - 1M) < value && value < (sbyte.MaxValue + 1M);
                case SpecialType.System_Int16: return (short.MinValue - 1M) < value && value < (short.MaxValue + 1M);
                case SpecialType.System_Int32: return (int.MinValue - 1M) < value && value < (int.MaxValue + 1M);
                case SpecialType.System_Int64: return (long.MinValue - 1M) < value && value < (long.MaxValue + 1M);
            }

            return true;
        }

        // Takes in a constant of any kind and returns the constant as either a double or decimal
        private static object CanonicalizeConstant(ConstantValue value)
        {
            switch (value.Discriminator)
            {
                case ConstantValueTypeDiscriminator.SByte: return (decimal)value.SByteValue;
                case ConstantValueTypeDiscriminator.Int16: return (decimal)value.Int16Value;
                case ConstantValueTypeDiscriminator.Int32: return (decimal)value.Int32Value;
                case ConstantValueTypeDiscriminator.Int64: return (decimal)value.Int64Value;
                case ConstantValueTypeDiscriminator.Byte: return (decimal)value.ByteValue;
                case ConstantValueTypeDiscriminator.Char: return (decimal)value.CharValue;
                case ConstantValueTypeDiscriminator.UInt16: return (decimal)value.UInt16Value;
                case ConstantValueTypeDiscriminator.UInt32: return (decimal)value.UInt32Value;
                case ConstantValueTypeDiscriminator.UInt64: return (decimal)value.UInt64Value;
                case ConstantValueTypeDiscriminator.Single:
                case ConstantValueTypeDiscriminator.Double: return value.DoubleValue;
                case ConstantValueTypeDiscriminator.Decimal: return value.DecimalValue;
                default: throw ExceptionUtilities.UnexpectedValue(value.Discriminator);
            }

            // all cases handled in the switch, above.
        }
    }
}
