// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// This class groups together all of the functionality needed to check for error CS1961, ERR_UnexpectedVariance.
    /// Its functionality is accessible through the NamedTypeSymbol extension method CheckInterfaceVarianceSafety and
    /// the MethodSymbol extension method CheckMethodVarianceSafety (for checking delegate Invoke).
    /// </summary>
    internal static class VarianceSafety
    {
        #region Interface variance safety

        /// <summary>
        /// Accumulate diagnostics related to the variance safety of an interface.
        /// </summary>
        internal static void CheckInterfaceVarianceSafety(this NamedTypeSymbol interfaceType, DiagnosticBag diagnostics)
        {
            Debug.Assert(interfaceType is { IsInterface: true });

            foreach (NamedTypeSymbol baseInterface in interfaceType.InterfacesNoUseSiteDiagnostics())
            {
                IsVarianceUnsafe(
                    baseInterface,
                    requireOutputSafety: true,
                    requireInputSafety: false,
                    context: baseInterface,
                    locationProvider: i => null,
                    locationArg: baseInterface,
                    diagnostics: diagnostics);
            }

            foreach (Symbol member in interfaceType.GetMembersUnordered())
            {
                switch (member.Kind)
                {
                    case SymbolKind.Method:
                        if (!member.IsAccessor())
                        {
                            CheckMethodVarianceSafety((MethodSymbol)member, diagnostics);
                        }
                        break;
                    case SymbolKind.Property:
                        CheckPropertyVarianceSafety((PropertySymbol)member, diagnostics);
                        break;
                    case SymbolKind.Event:
                        CheckEventVarianceSafety((EventSymbol)member, diagnostics);
                        break;
                }
            }
        }

        /// <summary>
        /// Accumulate diagnostics related to the variance safety of a delegate.
        /// </summary>
        internal static void CheckDelegateVarianceSafety(this SourceDelegateMethodSymbol method, DiagnosticBag diagnostics)
        {
            method.CheckMethodVarianceSafety(
                returnTypeLocationProvider: m =>
                    {
                        var syntax = m.GetDeclaringSyntax<DelegateDeclarationSyntax>();
                        return (syntax == null) ? null : syntax.ReturnType.Location;
                    },
                diagnostics: diagnostics);
        }

        /// <summary>
        /// Accumulate diagnostics related to the variance safety of an interface method.
        /// </summary>
        private static void CheckMethodVarianceSafety(this MethodSymbol method, DiagnosticBag diagnostics)
        {
            method.CheckMethodVarianceSafety(
                returnTypeLocationProvider: m =>
                    {
                        var syntax = m.GetDeclaringSyntax<MethodDeclarationSyntax>();
                        return (syntax == null) ? null : syntax.ReturnType.Location;
                    },
                diagnostics: diagnostics);
        }

        private static void CheckMethodVarianceSafety(this MethodSymbol method, LocationProvider<MethodSymbol> returnTypeLocationProvider, DiagnosticBag diagnostics)
        {
            // Spec 13.2.1: "Furthermore, each class type constraint, interface type constraint and
            // type parameter constraint on any type parameter of the method must be input-safe."
            CheckTypeParametersVarianceSafety(method.TypeParameters, method, diagnostics);

            //spec only applies this to non-void methods, but it falls out of our impl anyway
            IsVarianceUnsafe(
                method.ReturnType,
                requireOutputSafety: true,
                requireInputSafety: method.RefKind != RefKind.None,
                context: method,
                locationProvider: returnTypeLocationProvider,
                locationArg: method,
                diagnostics: diagnostics);

            CheckParametersVarianceSafety(method.Parameters, method, diagnostics);
        }

        /// <summary>
        /// Accumulate diagnostics related to the variance safety of an interface property.
        /// </summary>
        private static void CheckPropertyVarianceSafety(PropertySymbol property, DiagnosticBag diagnostics)
        {
            bool hasGetter = (object)property.GetMethod != null;
            bool hasSetter = (object)property.SetMethod != null;
            if (hasGetter || hasSetter)
            {
                IsVarianceUnsafe(
                    property.Type,
                    requireOutputSafety: hasGetter,
                    requireInputSafety: hasSetter || !(property.GetMethod?.RefKind == RefKind.None),
                    context: property,
                    locationProvider: p =>
                        {
                            var syntax = p.GetDeclaringSyntax<BasePropertyDeclarationSyntax>();
                            return (syntax == null) ? null : syntax.Type.Location;
                        },
                    locationArg: property,
                    diagnostics: diagnostics);
            }

            CheckParametersVarianceSafety(property.Parameters, property, diagnostics);
        }

        /// <summary>
        /// Accumulate diagnostics related to the variance safety of an interface event.
        /// </summary>
        private static void CheckEventVarianceSafety(EventSymbol @event, DiagnosticBag diagnostics)
        {
            IsVarianceUnsafe(
                @event.Type,
                requireOutputSafety: false,
                requireInputSafety: true,
                context: @event,
                locationProvider: e => e.Locations[0],
                locationArg: @event,
                diagnostics: diagnostics);
        }

        /// <summary>
        /// Accumulate diagnostics related to the variance safety of an interface method/property parameter.
        /// </summary>
        private static void CheckParametersVarianceSafety(ImmutableArray<ParameterSymbol> parameters, Symbol context, DiagnosticBag diagnostics)
        {
            foreach (ParameterSymbol param in parameters)
            {
                IsVarianceUnsafe(
                    param.Type,
                    requireOutputSafety: param.RefKind != RefKind.None,
                    requireInputSafety: true,
                    context: context,
                    locationProvider: p =>
                        {
                            var syntax = p.GetDeclaringSyntax<ParameterSyntax>();
                            return (syntax == null) ? null : syntax.Type.Location;
                        },
                    locationArg: param,
                    diagnostics: diagnostics);
            }
        }

        /// <summary>
        /// Accumulate diagnostics related to the variance safety of an interface method type parameters.
        /// </summary>
        private static void CheckTypeParametersVarianceSafety(ImmutableArray<TypeParameterSymbol> typeParameters, MethodSymbol context, DiagnosticBag diagnostics)
        {
            foreach (TypeParameterSymbol typeParameter in typeParameters)
            {
                foreach (TypeWithAnnotations constraintType in typeParameter.ConstraintTypesNoUseSiteDiagnostics)
                {
                    IsVarianceUnsafe(constraintType.Type,
                        requireOutputSafety: false,
                        requireInputSafety: true,
                        context: context,
                        locationProvider: t => t.Locations[0],
                        locationArg: typeParameter,
                        diagnostics: diagnostics);
                }
            }
        }

        #endregion Interface variance safety

        #region Input- and output- unsafeness

        /// <summary>
        /// Returns true if the type is output-unsafe or input-unsafe, as defined in the C# spec.
        /// Roughly, a type is output-unsafe if it could not be the return type of a method and
        /// input-unsafe if it could not be a parameter type of a method.
        /// </summary>
        /// <remarks>
        /// This method is intended to match spec section 13.1.3.1 as closely as possible 
        /// (except that the output-unsafe and input-unsafe checks are merged).
        /// </remarks>
        private static bool IsVarianceUnsafe<T>(
            TypeSymbol type,
            bool requireOutputSafety,
            bool requireInputSafety,
            Symbol context,
            LocationProvider<T> locationProvider,
            T locationArg,
            DiagnosticBag diagnostics)
            where T : Symbol
        {
            Debug.Assert(requireOutputSafety || requireInputSafety);

            // A type T is "output-unsafe" ["input-unsafe"] if one of the following holds:
            switch (type.Kind)
            {
                case SymbolKind.TypeParameter:
                    // 1) T is a contravariant [covariant] type parameter
                    TypeParameterSymbol typeParam = (TypeParameterSymbol)type;
                    if (requireInputSafety && requireOutputSafety && typeParam.Variance != VarianceKind.None)
                    {
                        // This sub-case isn't mentioned in the spec, because it's not required for
                        // the definition.  It just allows us to give a better error message for
                        // type parameters that are both output-unsafe and input-unsafe.
                        diagnostics.AddVarianceError(typeParam, context, locationProvider, locationArg, MessageID.IDS_Invariantly);
                        return true;
                    }
                    else if (requireOutputSafety && typeParam.Variance == VarianceKind.In)
                    {
                        // The is output-unsafe case (1) from the spec.
                        diagnostics.AddVarianceError(typeParam, context, locationProvider, locationArg, MessageID.IDS_Covariantly);
                        return true;
                    }
                    else if (requireInputSafety && typeParam.Variance == VarianceKind.Out)
                    {
                        // The is input-unsafe case (1) from the spec.
                        diagnostics.AddVarianceError(typeParam, context, locationProvider, locationArg, MessageID.IDS_Contravariantly);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case SymbolKind.ArrayType:
                    // 2) T is an array type with an output-unsafe [input-unsafe] element type
                    return IsVarianceUnsafe(((ArrayTypeSymbol)type).ElementType, requireOutputSafety, requireInputSafety, context, locationProvider, locationArg, diagnostics);
                case SymbolKind.ErrorType:
                case SymbolKind.NamedType:
                    var namedType = (NamedTypeSymbol)type.TupleUnderlyingTypeOrSelf();
                    // 3) (see IsVarianceUnsafe(NamedTypeSymbol))
                    return IsVarianceUnsafe(namedType, requireOutputSafety, requireInputSafety, context, locationProvider, locationArg, diagnostics);
                default:
                    return false;
            }
        }

        /// <summary>
        /// 3) T is an interface, class, struct, enum, or delegate type <![CDATA[S<A_1, ..., A_k>]]> constructed
        /// from a generic type <![CDATA[S<X_1, ..., X_k>]]> where for at least one A_i one
        /// of the following holds:
        ///     a) X_i is covariant or invariant and A_i is output-unsafe [input-unsafe]
        ///     b) X_i is contravariant or invariant and A_i is input-unsafe [output-unsafe] (note: spec has "input-safe", but it's a typo)
        /// </summary>
        /// <remarks>
        /// Slight rewrite to make it more idiomatic for C#:
        ///     a) X_i is covariant and A_i is input-unsafe
        ///     b) X_i is contravariant and A_i is output-unsafe
        ///     c) X_i is invariant and A_i is input-unsafe or output-unsafe
        /// </remarks>
        private static bool IsVarianceUnsafe<T>(
            NamedTypeSymbol namedType,
            bool requireOutputSafety,
            bool requireInputSafety,
            Symbol context,
            LocationProvider<T> locationProvider,
            T locationArg,
            DiagnosticBag diagnostics)
            where T : Symbol
        {
            Debug.Assert(requireOutputSafety || requireInputSafety);

            switch (namedType.TypeKind)
            {
                case TypeKind.Class:
                case TypeKind.Struct:
                case TypeKind.Enum: // Can't be generic, but can be nested in generic.
                case TypeKind.Interface:
                case TypeKind.Delegate:
                case TypeKind.Error:
                    break;
                default:
                    return false;
            }

            while ((object)namedType != null)
            {
                for (int i = 0; i < namedType.Arity; i++)
                {
                    TypeParameterSymbol typeParam = namedType.TypeParameters[i];
                    TypeSymbol typeArg = namedType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[i].Type;

                    bool requireOut;
                    bool requireIn;

                    switch (typeParam.Variance)
                    {
                        case VarianceKind.Out:
                            // a) X_i is covariant and A_i is output-unsafe [input-unsafe]
                            requireOut = requireOutputSafety;
                            requireIn = requireInputSafety;
                            break;
                        case VarianceKind.In:
                            // b) X_i is contravariant and A_i is input-unsafe [output-unsafe]
                            requireOut = requireInputSafety;
                            requireIn = requireOutputSafety;
                            break;
                        case VarianceKind.None:
                            // c) X_i is invariant and A_i is output-unsafe or input-unsafe
                            requireIn = true;
                            requireOut = true;
                            break;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(typeParam.Variance);
                    }

                    if (IsVarianceUnsafe(typeArg, requireOut, requireIn, context, locationProvider, locationArg, diagnostics))
                    {
                        return true;
                    }
                }

                namedType = namedType.ContainingType;
            }

            return false;
        }

        #endregion Input- and output- unsafeness

        #region Adding diagnostics

        private delegate Location LocationProvider<T>(T arg);

        /// <summary>
        /// Add an ERR_UnexpectedVariance diagnostic to the diagnostic bag.
        /// </summary>
        /// <param name="diagnostics">Diagnostic bag.</param>
        /// <param name="unsafeTypeParameter">Type parameter that is not variance safe.</param>
        /// <param name="context">Context in which type is not variance safe (e.g. method).</param>
        /// <param name="locationProvider">Callback to provide location.</param>
        /// <param name="locationArg">Callback argument.</param>
        /// <param name="expectedVariance">Desired variance of type.</param>
        private static void AddVarianceError<T>(
            this DiagnosticBag diagnostics,
            TypeParameterSymbol unsafeTypeParameter,
            Symbol context,
            LocationProvider<T> locationProvider,
            T locationArg,
            MessageID expectedVariance)
            where T : Symbol
        {
            MessageID actualVariance;
            switch (unsafeTypeParameter.Variance)
            {
                case VarianceKind.In:
                    actualVariance = MessageID.IDS_Contravariant;
                    break;
                case VarianceKind.Out:
                    actualVariance = MessageID.IDS_Covariant;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(unsafeTypeParameter.Variance);
            }

            // Get a location that roughly represents the unsafe type parameter use.
            // (Typically, the locationProvider will return the location of the entire type
            // reference rather than the specific type parameter, for instance, returning
            // "C<T>[]" for "interface I<in T> { C<T>[] F(); }" rather than the type parameter
            // in "C<T>[]", but that is better than returning the location of T within "I<in T>".
            var location = locationProvider(locationArg) ?? unsafeTypeParameter.Locations[0];

            // CONSIDER: instead of using the same error code for all variance errors, we could use different codes for "requires input-safe", 
            // "requires output-safe", and "requires input-safe and output-safe".  This would make the error codes much easier to document and
            // much more actionable.
            // UNDONE: related location for use is much more useful
            diagnostics.Add(ErrorCode.ERR_UnexpectedVariance, location, context, unsafeTypeParameter, actualVariance.Localize(), expectedVariance.Localize());
        }

        private static T GetDeclaringSyntax<T>(this Symbol symbol) where T : SyntaxNode
        {
            var syntaxRefs = symbol.DeclaringSyntaxReferences;
            if (syntaxRefs.Length == 0)
            {
                return null;
            }
            return syntaxRefs[0].GetSyntax() as T;
        }

        #endregion Adding diagnostics
    }
}
