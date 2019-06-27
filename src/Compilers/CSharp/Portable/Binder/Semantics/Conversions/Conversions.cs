// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class Conversions : ConversionsBase
    {
        private readonly Binder _binder;

        public Conversions(Binder binder)
            : this(binder, currentRecursionDepth: 0, includeNullability: false, otherNullabilityOpt: null)
        {
        }

        private Conversions(Binder binder, int currentRecursionDepth, bool includeNullability, Conversions otherNullabilityOpt)
            : base(binder.Compilation.Assembly.CorLibrary, currentRecursionDepth, includeNullability, otherNullabilityOpt)
        {
            _binder = binder;
        }

        protected override ConversionsBase CreateInstance(int currentRecursionDepth)
        {
            return new Conversions(_binder, currentRecursionDepth, IncludeNullability, otherNullabilityOpt: null);
        }

        private CSharpCompilation Compilation { get { return _binder.Compilation; } }

        protected override ConversionsBase WithNullabilityCore(bool includeNullability)
        {
            Debug.Assert(IncludeNullability != includeNullability);
            return new Conversions(_binder, currentRecursionDepth, includeNullability, this);
        }

        public override Conversion GetMethodGroupConversion(BoundMethodGroup source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // Must be a bona fide delegate type, not an expression tree type.
            if (!destination.IsDelegateType())
            {
                return Conversion.NoConversion;
            }

            var methodSymbol = GetDelegateInvokeMethodIfAvailable(destination);
            if ((object)methodSymbol == null)
            {
                return Conversion.NoConversion;
            }

            var resolution = ResolveDelegateMethodGroup(_binder, source, methodSymbol, ref useSiteDiagnostics);
            var conversion = (resolution.IsEmpty || resolution.HasAnyErrors) ?
                Conversion.NoConversion :
                ToConversion(resolution.OverloadResolutionResult, resolution.MethodGroup, (NamedTypeSymbol)destination);
            resolution.Free();
            return conversion;
        }

        protected override Conversion GetInterpolatedStringConversion(BoundInterpolatedString source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // An interpolated string expression may be converted to the types
            // System.IFormattable and System.FormattableString
            return (TypeSymbol.Equals(destination, Compilation.GetWellKnownType(WellKnownType.System_IFormattable), TypeCompareKind.ConsiderEverything2) ||
                    TypeSymbol.Equals(destination, Compilation.GetWellKnownType(WellKnownType.System_FormattableString), TypeCompareKind.ConsiderEverything2))
                ? Conversion.InterpolatedString : Conversion.NoConversion;
        }

        /// <summary>
        /// Resolve method group based on the optional delegate invoke method.
        /// If the invoke method is null, ignore arguments in resolution.
        /// </summary>
        private static MethodGroupResolution ResolveDelegateMethodGroup(Binder binder, BoundMethodGroup source, MethodSymbol delegateInvokeMethodOpt, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if ((object)delegateInvokeMethodOpt != null)
            {
                var analyzedArguments = AnalyzedArguments.GetInstance();
                GetDelegateArguments(source.Syntax, analyzedArguments, delegateInvokeMethodOpt.Parameters, binder.Compilation);
                var resolution = binder.ResolveMethodGroup(source, analyzedArguments, useSiteDiagnostics: ref useSiteDiagnostics, inferWithDynamic: true,
                    isMethodGroupConversion: true, returnRefKind: delegateInvokeMethodOpt.RefKind, returnType: delegateInvokeMethodOpt.ReturnType);
                analyzedArguments.Free();
                return resolution;
            }
            else
            {
                return binder.ResolveMethodGroup(source, analyzedArguments: null, isMethodGroupConversion: true, ref useSiteDiagnostics);
            }
        }

        /// <summary>
        /// Return the Invoke method symbol if the type is a delegate
        /// type and the Invoke method is available, otherwise null.
        /// </summary>
        private static MethodSymbol GetDelegateInvokeMethodIfAvailable(TypeSymbol type)
        {
            var delegateType = type.GetDelegateType();
            if ((object)delegateType == null)
            {
                return null;
            }

            MethodSymbol methodSymbol = delegateType.DelegateInvokeMethod;
            if ((object)methodSymbol == null || methodSymbol.HasUseSiteError)
            {
                return null;
            }

            return methodSymbol;
        }

        public static bool ReportDelegateMethodGroupDiagnostics(Binder binder, BoundMethodGroup expr, TypeSymbol targetType, DiagnosticBag diagnostics)
        {
            var invokeMethodOpt = GetDelegateInvokeMethodIfAvailable(targetType);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var resolution = ResolveDelegateMethodGroup(binder, expr, invokeMethodOpt, ref useSiteDiagnostics);
            diagnostics.Add(expr.Syntax, useSiteDiagnostics);

            bool hasErrors = resolution.HasAnyErrors;

            diagnostics.AddRange(resolution.Diagnostics);

            // SPEC VIOLATION: Unfortunately, we cannot exactly implement the specification for
            // the scenario in which an extension method that extends a value type is converted
            // to a delegate. The code we generate that captures a delegate to a static method
            // that is "partially evaluated" with the bound-to-the-delegate first argument
            // requires that the first argument be of reference type.
            //
            // SPEC VIOLATION: Similarly, we cannot capture a method of Nullable<T>, because
            // boxing a Nullable<T> gives a T, not a boxed Nullable<T>.
            //
            // We give special error messages in these situations.

            if (resolution.MethodGroup != null)
            {
                var result = resolution.OverloadResolutionResult;
                if (result != null)
                {
                    if (result.Succeeded)
                    {
                        var method = result.BestResult.Member;
                        Debug.Assert((object)method != null);
                        if (resolution.MethodGroup.IsExtensionMethodGroup)
                        {
                            Debug.Assert(method.IsExtensionMethod);

                            var thisParameter = method.Parameters[0];
                            if (!thisParameter.Type.IsReferenceType)
                            {
                                // Extension method '{0}' defined on value type '{1}' cannot be used to create delegates
                                diagnostics.Add(
                                    ErrorCode.ERR_ValueTypeExtDelegate,
                                    expr.Syntax.Location,
                                    method,
                                    thisParameter.Type);
                                hasErrors = true;
                            }
                        }
                        else if (method.ContainingType.IsNullableType() && !method.IsOverride)
                        {
                            // CS1728: Cannot bind delegate to '{0}' because it is a member of 'System.Nullable<T>'
                            diagnostics.Add(
                                ErrorCode.ERR_DelegateOnNullable,
                                expr.Syntax.Location,
                                method);
                            hasErrors = true;
                        }
                    }
                    else if (!hasErrors &&
                            !resolution.IsEmpty &&
                            resolution.ResultKind == LookupResultKind.Viable)
                    {
                        var overloadDiagnostics = DiagnosticBag.GetInstance();

                        result.ReportDiagnostics(
                            binder: binder, location: expr.Syntax.Location, nodeOpt: expr.Syntax, diagnostics: overloadDiagnostics,
                            name: expr.Name,
                            receiver: resolution.MethodGroup.Receiver, invokedExpression: expr.Syntax, arguments: resolution.AnalyzedArguments,
                            memberGroup: resolution.MethodGroup.Methods.ToImmutable(),
                            typeContainingConstructor: null, delegateTypeBeingInvoked: null,
                            isMethodGroupConversion: true, returnRefKind: invokeMethodOpt?.RefKind, delegateType: targetType);

                        if (!overloadDiagnostics.IsEmptyWithoutResolution)
                        {
                            hasErrors = overloadDiagnostics.HasAnyErrors();
                            diagnostics.AddRange(overloadDiagnostics);
                        }

                        overloadDiagnostics.Free();
                    }
                }
            }

            resolution.Free();
            return hasErrors;
        }

        public Conversion MethodGroupConversion(SyntaxNode syntax, MethodGroup methodGroup, NamedTypeSymbol delegateType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var analyzedArguments = AnalyzedArguments.GetInstance();
            var result = OverloadResolutionResult<MethodSymbol>.GetInstance();
            var delegateInvokeMethod = delegateType.DelegateInvokeMethod;

            Debug.Assert((object)delegateInvokeMethod != null && !delegateInvokeMethod.HasUseSiteError,
                         "This method should only be called for valid delegate types");
            GetDelegateArguments(syntax, analyzedArguments, delegateInvokeMethod.Parameters, Compilation);
            _binder.OverloadResolution.MethodInvocationOverloadResolution(
                methods: methodGroup.Methods,
                typeArguments: methodGroup.TypeArguments,
                receiver: methodGroup.Receiver,
                arguments: analyzedArguments,
                result: result,
                useSiteDiagnostics: ref useSiteDiagnostics,
                isMethodGroupConversion: true,
                returnRefKind: delegateInvokeMethod.RefKind,
                returnType: delegateInvokeMethod.ReturnType);
            var conversion = ToConversion(result, methodGroup, delegateType);

            analyzedArguments.Free();
            result.Free();
            return conversion;
        }

        public static void GetDelegateArguments(SyntaxNode syntax, AnalyzedArguments analyzedArguments, ImmutableArray<ParameterSymbol> delegateParameters, CSharpCompilation compilation)
        {
            foreach (var p in delegateParameters)
            {
                ParameterSymbol parameter = p;

                // In ExpressionBinder::BindGrpConversion, the native compiler substitutes object in place of dynamic.  This is
                // necessary because conversions from expressions of type dynamic always succeed, whereas conversions from the
                // type generally fail (modulo identity conversions).  This is not reflected in the C# 4 spec, but will be
                // incorporated going forward.  See DevDiv #742345 for additional details.
                // NOTE: Dev11 does a deep substitution (e.g. C<C<C<dynamic>>> -> C<C<C<object>>>), but that seems redundant.
                if (parameter.Type.IsDynamic())
                {
                    // If we don't have System.Object, then we'll get an error type, which will cause overload resolution to fail, 
                    // which will cause some error to be reported.  That's sufficient (i.e. no need to specifically report its absence here).
                    parameter = new SignatureOnlyParameterSymbol(
                        TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Object), customModifiers: parameter.TypeWithAnnotations.CustomModifiers), parameter.RefCustomModifiers, parameter.IsParams, parameter.RefKind);
                }

                analyzedArguments.Arguments.Add(new BoundParameter(syntax, parameter) { WasCompilerGenerated = true });
                analyzedArguments.RefKinds.Add(parameter.RefKind);
            }
        }

        private static Conversion ToConversion(OverloadResolutionResult<MethodSymbol> result, MethodGroup methodGroup, NamedTypeSymbol delegateType)
        {
            // 6.6 An implicit conversion (6.1) exists from a method group (7.1) to a compatible
            // delegate type. Given a delegate type D and an expression E that is classified as
            // a method group, an implicit conversion exists from E to D if E contains at least
            // one method that is applicable in its normal form (7.5.3.1) to an argument list
            // constructed by use of the parameter types and modifiers of D...

            // SPEC VIOLATION: Unfortunately, we cannot exactly implement the specification for
            // the scenario in which an extension method that extends a value type is converted
            // to a delegate. The code we generate that captures a delegate to a static method
            // that is "partially evaluated" with the bound-to-the-delegate first argument
            // requires that the first argument be of reference type.

            // SPEC VIOLATION: Similarly, we cannot capture a method of Nullable<T>, because
            // boxing a Nullable<T> gives a T, not a boxed Nullable<T>. (We can capture methods
            // of object on a nullable receiver, but not GetValueOrDefault.)

            if (!result.Succeeded)
            {
                return Conversion.NoConversion;
            }

            MethodSymbol method = result.BestResult.Member;

            if (methodGroup.IsExtensionMethodGroup && !method.Parameters[0].Type.IsReferenceType)
            {
                return Conversion.NoConversion;
            }

            //cannot capture stack-only types.
            if (method.RequiresInstanceReceiver && methodGroup.Receiver?.Type?.IsRestrictedType() == true)
            {
                return Conversion.NoConversion;
            }

            if (method.ContainingType.IsNullableType() && !method.IsOverride)
            {
                return Conversion.NoConversion;
            }

            // NOTE: Section 6.6 will be slightly updated:
            //
            //   - The candidate methods considered are only those methods that are applicable in their
            //     normal form (§7.5.3.1), and do not omit any optional parameters. Thus, candidate methods
            //     are ignored if they are applicable only in their expanded form, or if one or more of their
            //     optional parameters do not have a corresponding parameter in the targeted delegate type.
            //   
            // Therefore, we shouldn't get here unless the parameter count matches.

            // NOTE: Delegate type compatibility is important, but is not part of the existence check.

            Debug.Assert(method.ParameterCount == delegateType.DelegateInvokeMethod.ParameterCount + (methodGroup.IsExtensionMethodGroup ? 1 : 0));

            return new Conversion(ConversionKind.MethodGroup, method, methodGroup.IsExtensionMethodGroup);
        }

        public override Conversion GetStackAllocConversion(BoundStackAllocArrayCreation sourceExpression, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (sourceExpression.Syntax.IsLocalVariableDeclarationInitializationForPointerStackalloc())
            {
                Debug.Assert((object)sourceExpression.Type == null);
                Debug.Assert((object)sourceExpression.ElementType != null);

                var sourceAsPointer = new PointerTypeSymbol(TypeWithAnnotations.Create(sourceExpression.ElementType));
                var pointerConversion = ClassifyImplicitConversionFromType(sourceAsPointer, destination, ref useSiteDiagnostics);

                if (pointerConversion.IsValid)
                {
                    return Conversion.MakeStackAllocToPointerType(pointerConversion);
                }
                else
                {
                    var spanType = _binder.GetWellKnownType(WellKnownType.System_Span_T, ref useSiteDiagnostics);
                    if (spanType.TypeKind == TypeKind.Struct && spanType.IsRefLikeType)
                    {
                        var spanType_T = spanType.Construct(sourceExpression.ElementType);
                        var spanConversion = ClassifyImplicitConversionFromType(spanType_T, destination, ref useSiteDiagnostics);

                        if (spanConversion.Exists)
                        {
                            return Conversion.MakeStackAllocToSpanType(spanConversion);
                        }
                    }
                }
            }

            return Conversion.NoConversion;
        }
    }
}
