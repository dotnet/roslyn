// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A tuple of TypeParameterSymbol and DiagnosticInfo, created for errors
    /// reported from ConstraintsHelper rather than creating Diagnostics directly.
    /// This decouples constraints checking from syntax and Locations, and supports
    /// callers that may want to create Location instances lazily or not at all.
    /// </summary>
    internal struct TypeParameterDiagnosticInfo
    {
        public readonly TypeParameterSymbol TypeParameter;
        public readonly DiagnosticInfo DiagnosticInfo;

        public TypeParameterDiagnosticInfo(TypeParameterSymbol typeParameter, DiagnosticInfo diagnosticInfo)
        {
            this.TypeParameter = typeParameter;
            this.DiagnosticInfo = diagnosticInfo;
        }
    }

    /// <summary>
    /// Helper methods for generic type parameter constraints. There are two sets of methods: one
    /// set for resolving constraint "bounds" (that is, determining the effective base type, interface set,
    /// etc.), and another set for checking for constraint violations in type and method references.
    /// 
    /// Bounds are resolved by calling one of the ResolveBounds overloads. Typically bounds are
    /// resolved by each TypeParameterSymbol at, or before, one of the corresponding properties
    /// (BaseType, Interfaces, etc.) is accessed. Resolving bounds may result in errors (cycles,
    /// inconsistent constraints, etc.) and it is the responsibility of the caller to report any such
    /// errors as declaration errors or use-site errors (depending on whether the type parameter
    /// was from source or metadata) and to ensure bounds are resolved for source type parameters
    /// even if the corresponding properties are never accessed directly.
    /// 
    /// Constraints are checked by calling one of the CheckConstraints or CheckAllConstraints
    /// overloads for any generic type or method reference from source. In some circumstances,
    /// references are checked at the time the generic type or generic method is bound and constructed
    /// by the Binder. In those case, it is sufficient to call one of the CheckConstraints overloads
    /// since compound types (such as A&lt;T&gt;.B&lt;U&gt; or A&lt;B&lt;T&gt;&gt;) are checked
    /// incrementally as each part is bound. In other cases however, constraint checking needs to be
    /// delayed to prevent cycles where checking constraints requires binding the syntax that is currently
    /// being bound (such as the constraint in class C&lt;T&gt; where T : C&lt;T&gt;). In those cases,
    /// the caller must lazily check constraints, and since the types may be compound types, it is
    /// necessary to call CheckAllConstraints.
    /// </summary>
    internal static class ConstraintsHelper
    {
        /// <summary>
        /// Determine the effective base type, effective interface set, and set of type
        /// parameters (excluding cycles) from the type parameter constraints. Conflicts
        /// within the constraints and constraint types are returned as diagnostics.
        /// 'inherited' should be true if the type parameters are from an overridden
        /// generic method. In those cases, additional constraint checks are applied.
        /// </summary>
        public static TypeParameterBounds ResolveBounds(
            this TypeParameterSymbol typeParameter,
            AssemblySymbol corLibrary,
            ConsList<TypeParameterSymbol> inProgress,
            ImmutableArray<TypeSymbolWithAnnotations> constraintTypes,
            bool inherited,
            CSharpCompilation currentCompilation,
            DiagnosticBag diagnostics)
        {
            var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
            var bounds = typeParameter.ResolveBounds(corLibrary, inProgress, constraintTypes, inherited, currentCompilation, diagnosticsBuilder, ref useSiteDiagnosticsBuilder);

            if (useSiteDiagnosticsBuilder != null)
            {
                diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder);
            }

            foreach (var pair in diagnosticsBuilder)
            {
                diagnostics.Add(new CSDiagnostic(pair.DiagnosticInfo, pair.TypeParameter.Locations[0]));
            }

            diagnosticsBuilder.Free();
            return bounds;
        }

        // Based on SymbolLoader::ResolveBounds.
        public static TypeParameterBounds ResolveBounds(
            this TypeParameterSymbol typeParameter,
            AssemblySymbol corLibrary,
            ConsList<TypeParameterSymbol> inProgress,
            ImmutableArray<TypeSymbolWithAnnotations> constraintTypes,
            bool inherited,
            CSharpCompilation currentCompilation,
            ArrayBuilder<TypeParameterDiagnosticInfo> diagnosticsBuilder,
            ref ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder)
        {
            Debug.Assert(currentCompilation == null || typeParameter.IsFromCompilation(currentCompilation));

            ImmutableArray<NamedTypeSymbol> interfaces;

            NamedTypeSymbol effectiveBaseClass = corLibrary.GetSpecialType(typeParameter.HasValueTypeConstraint ? SpecialType.System_ValueType : SpecialType.System_Object);
            TypeSymbol deducedBaseType = effectiveBaseClass;
            DynamicTypeEraser dynamicEraser = null;

            if (constraintTypes.Length == 0)
            {
                interfaces = ImmutableArray<NamedTypeSymbol>.Empty;
            }
            else
            {
                var constraintTypesBuilder = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance();
                var interfacesBuilder = ArrayBuilder<NamedTypeSymbol>.GetInstance();
                var conversions = new TypeConversions(corLibrary);
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;

                // Resolve base types, determine the effective base class and
                // interfaces, and filter out any constraint types that cause cycles.
                foreach (var constraintType in constraintTypes)
                {
                    NamedTypeSymbol constraintEffectiveBase;
                    TypeSymbol constraintDeducedBase;

                    switch (constraintType.TypeKind)
                    {
                        case TypeKind.Dynamic:
                            Debug.Assert(inherited || currentCompilation == null);
                            continue;

                        case TypeKind.TypeParameter:
                            {
                                var containingSymbol = typeParameter.ContainingSymbol;
                                var constraintTypeParameter = (TypeParameterSymbol)constraintType.TypeSymbol;
                                ConsList<TypeParameterSymbol> constraintsInProgress;

                                if (constraintTypeParameter.ContainingSymbol == containingSymbol)
                                {
                                    // The constraint type parameter is from the same containing type or method.
                                    if (inProgress.ContainsReference(constraintTypeParameter))
                                    {
                                        // "Circular constraint dependency involving '{0}' and '{1}'"
                                        diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(constraintTypeParameter, new CSDiagnosticInfo(ErrorCode.ERR_CircularConstraint, constraintTypeParameter, typeParameter)));
                                        continue;
                                    }

                                    constraintsInProgress = inProgress;
                                }
                                else
                                {
                                    // The constraint type parameter is from a different containing symbol so no cycle.
                                    constraintsInProgress = ConsList<TypeParameterSymbol>.Empty;
                                }

                                // Use the calculated bounds from the constraint type parameter.
                                constraintEffectiveBase = constraintTypeParameter.GetEffectiveBaseClass(constraintsInProgress);
                                constraintDeducedBase = constraintTypeParameter.GetDeducedBaseType(constraintsInProgress);
                                AddInterfaces(interfacesBuilder, constraintTypeParameter.GetInterfaces(constraintsInProgress));

                                if (constraintTypeParameter.HasValueTypeConstraint && !inherited && currentCompilation != null && constraintTypeParameter.IsFromCompilation(currentCompilation))
                                {
                                    // "Type parameter '{1}' has the 'struct' constraint so '{1}' cannot be used as a constraint for '{0}'"
                                    diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new CSDiagnosticInfo(ErrorCode.ERR_ConWithValCon, typeParameter, constraintTypeParameter)));
                                    continue;
                                }
                            }
                            break;

                        case TypeKind.Interface:
                        case TypeKind.Class:
                        case TypeKind.Delegate:
                            NamedTypeSymbol erasedConstraintType;

                            if (inherited || currentCompilation == null)
                            {
                                // only inherited constraints may contain dynamic
                                if (dynamicEraser == null)
                                {
                                    dynamicEraser = new DynamicTypeEraser(corLibrary.GetSpecialType(SpecialType.System_Object));
                                }

                                erasedConstraintType = (NamedTypeSymbol)dynamicEraser.EraseDynamic(constraintType.TypeSymbol);
                            }
                            else
                            {
                                Debug.Assert(!constraintType.TypeSymbol.ContainsDynamic());
                                Debug.Assert(constraintType.TypeKind != TypeKind.Delegate);

                                erasedConstraintType = (NamedTypeSymbol)constraintType.TypeSymbol;
                            }

                            if (constraintType.TypeSymbol.IsInterfaceType())
                            {
                                AddInterface(interfacesBuilder, erasedConstraintType);
                                constraintTypesBuilder.Add(constraintType);
                                continue;
                            }
                            else
                            {
                                constraintEffectiveBase = erasedConstraintType;
                                constraintDeducedBase = constraintType.TypeSymbol;
                                break;
                            }

                        case TypeKind.Struct:
                            Debug.Assert(inherited || currentCompilation == null);
                            constraintEffectiveBase = corLibrary.GetSpecialType(SpecialType.System_ValueType);
                            constraintDeducedBase = constraintType.TypeSymbol;
                            break;

                        case TypeKind.Enum:
                            Debug.Assert(inherited || currentCompilation == null);
                            constraintEffectiveBase = corLibrary.GetSpecialType(SpecialType.System_Enum);
                            constraintDeducedBase = constraintType.TypeSymbol;
                            break;

                        case TypeKind.Array:
                            Debug.Assert(inherited || currentCompilation == null);
                            constraintEffectiveBase = corLibrary.GetSpecialType(SpecialType.System_Array);
                            constraintDeducedBase = constraintType.TypeSymbol;
                            break;

                        case TypeKind.Error:
                            constraintEffectiveBase = (NamedTypeSymbol)constraintType.TypeSymbol;
                            constraintDeducedBase = constraintType.TypeSymbol;
                            break;

                        case TypeKind.Submission:
                        default:
                            throw ExceptionUtilities.UnexpectedValue(constraintType.TypeKind);
                    }

                    CheckEffectiveAndDeducedBaseTypes(conversions, constraintEffectiveBase, constraintDeducedBase);

                    constraintTypesBuilder.Add(constraintType);

                    // Determine the more encompassed of the current effective base
                    // class and the previously computed effective base class.
                    if (!deducedBaseType.IsErrorType() && !constraintDeducedBase.IsErrorType())
                    {
                        if (!IsEncompassedBy(conversions, deducedBaseType, constraintDeducedBase, ref useSiteDiagnostics))
                        {
                            if (!IsEncompassedBy(conversions, constraintDeducedBase, deducedBaseType, ref useSiteDiagnostics))
                            {
                                // "Type parameter '{0}' inherits conflicting constraints '{1}' and '{2}'"
                                diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new CSDiagnosticInfo(ErrorCode.ERR_BaseConstraintConflict, typeParameter, constraintDeducedBase, deducedBaseType)));
                            }
                            else
                            {
                                deducedBaseType = constraintDeducedBase;
                                effectiveBaseClass = constraintEffectiveBase;
                            }
                        }
                    }
                }

                AppendUseSiteDiagnostics(useSiteDiagnostics, typeParameter, ref useSiteDiagnosticsBuilder);

                CheckEffectiveAndDeducedBaseTypes(conversions, effectiveBaseClass, deducedBaseType);

                constraintTypes = constraintTypesBuilder.ToImmutableAndFree();
                interfaces = interfacesBuilder.ToImmutableAndFree();
            }

            Debug.Assert((effectiveBaseClass.SpecialType == SpecialType.System_Object) || (deducedBaseType.SpecialType != SpecialType.System_Object));

            // Only create a TypeParameterBounds instance for this type
            // parameter if the bounds are not the default values.
            if ((constraintTypes.Length == 0) && (deducedBaseType.SpecialType == SpecialType.System_Object))
            {
                Debug.Assert(effectiveBaseClass.SpecialType == SpecialType.System_Object);
                Debug.Assert(interfaces.Length == 0);
                return null;
            }

            var bounds = new TypeParameterBounds(constraintTypes, interfaces, effectiveBaseClass, deducedBaseType);

            // Additional constraint checks for overrides.
            if (inherited)
            {
                CheckOverrideConstraints(typeParameter, bounds, diagnosticsBuilder);
            }

            return bounds;
        }

        internal static void CheckConstraintTypesVisibility(
            this Symbol containingSymbol,
            Location location,
            ImmutableArray<TypeParameterConstraintClause> constraintClauses,
            DiagnosticBag diagnostics)
        {
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            foreach (var clause in constraintClauses)
            {
                if (clause == null)
                {
                    continue;
                }

                foreach (var constraintType in clause.ConstraintTypes)
                {
                    if (!containingSymbol.IsNoMoreVisibleThan(constraintType, ref useSiteDiagnostics))
                    {
                        // "Inconsistent accessibility: constraint type '{1}' is less accessible than '{0}'"
                        diagnostics.Add(ErrorCode.ERR_BadVisBound, location, containingSymbol, constraintType.TypeSymbol);
                    }
                }
            }

            diagnostics.Add(location, useSiteDiagnostics);
        }

        // Based on SymbolLoader::SetOverrideConstraints.
        private static void CheckOverrideConstraints(
            TypeParameterSymbol typeParameter,
            TypeParameterBounds bounds,
            ArrayBuilder<TypeParameterDiagnosticInfo> diagnosticsBuilder)
        {
            var deducedBase = bounds.DeducedBaseType;
            var constraintTypes = bounds.ConstraintTypes;

            if (IsValueType(typeParameter, constraintTypes) && IsReferenceType(typeParameter, constraintTypes))
            {
                Debug.Assert(!deducedBase.IsValueType || typeParameter.HasReferenceTypeConstraint);
                diagnosticsBuilder.Add(GenerateConflictingConstraintsError(typeParameter, deducedBase, classConflict: deducedBase.IsValueType));
            }
            else if (deducedBase.IsNullableType() && (typeParameter.HasValueTypeConstraint || typeParameter.HasReferenceTypeConstraint))
            {
                diagnosticsBuilder.Add(GenerateConflictingConstraintsError(typeParameter, deducedBase, classConflict: typeParameter.HasReferenceTypeConstraint));
            }
        }

        /// <summary>
        /// Check all generic constraints on the given type and any containing types
        /// (such as A&lt;T&gt; in A&lt;T&gt;.B&lt;U&gt;). This includes checking constraints
        /// on generic types within the type (such as B&lt;T&gt; in A&lt;B&lt;T&gt;[]&gt;).
        /// </summary>
        public static void CheckAllConstraints(
            this TypeSymbol type,
            ConversionsBase conversions,
            Location location,
            DiagnosticBag diagnostics)
        {
            type.VisitType(s_checkConstraintsSingleTypeFunc, new CheckConstraintsArgs(type.DeclaringCompilation, conversions, location, diagnostics));
        }

        public static bool CheckAllConstraints(
            this TypeSymbol type,
            ConversionsBase conversions)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            type.CheckAllConstraints(conversions, NoLocation.Singleton, diagnostics);
            bool ok = !diagnostics.HasAnyErrors();
            diagnostics.Free();
            return ok;
        }

        private struct CheckConstraintsArgs
        {
            public readonly CSharpCompilation CurrentCompilation;
            public readonly ConversionsBase Conversions;
            public readonly Location Location;
            public readonly DiagnosticBag Diagnostics;

            public CheckConstraintsArgs(CSharpCompilation currentCompilation, ConversionsBase conversions, Location location, DiagnosticBag diagnostics)
            {
                this.CurrentCompilation = currentCompilation;
                this.Conversions = conversions;
                this.Location = location;
                this.Diagnostics = diagnostics;
            }
        }

        private static readonly Func<TypeSymbol, CheckConstraintsArgs, bool, bool> s_checkConstraintsSingleTypeFunc = (type, arg, unused) => CheckConstraintsSingleType(type, arg);

        private static bool CheckConstraintsSingleType(TypeSymbol type, CheckConstraintsArgs args)
        {
            if (type.Kind == SymbolKind.NamedType)
            {
                ((NamedTypeSymbol)type).CheckConstraints(args.CurrentCompilation, args.Conversions, args.Location, args.Diagnostics);
            }
            return false; // continue walking types
        }

        public static bool CheckConstraints(
            this NamedTypeSymbol type,
            ConversionsBase conversions,
            CSharpSyntaxNode typeSyntax,
            SeparatedSyntaxList<TypeSyntax> typeArgumentsSyntax, // may be omitted in synthesized invocations
            Compilation currentCompilation,
            ConsList<Symbol> basesBeingResolved,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(typeArgumentsSyntax.Count == 0 /*omitted*/ || typeArgumentsSyntax.Count == type.Arity);
            if (!RequiresChecking(type))
            {
                return true;
            }

            var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
            var result = !typeSyntax.HasErrors && CheckTypeConstraints(type, conversions, currentCompilation, diagnosticsBuilder, ref useSiteDiagnosticsBuilder);

            if (useSiteDiagnosticsBuilder != null)
            {
                diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder);
            }

            foreach (var pair in diagnosticsBuilder)
            {
                int ordinal = pair.TypeParameter.Ordinal;
                var location = new SourceLocation(ordinal < typeArgumentsSyntax.Count ? typeArgumentsSyntax[ordinal] : typeSyntax);
                diagnostics.Add(new CSDiagnostic(pair.DiagnosticInfo, location));
            }

            diagnosticsBuilder.Free();

            if (HasDuplicateInterfaces(type, basesBeingResolved))
            {
                result = false;
                diagnostics.Add(ErrorCode.ERR_BogusType, typeSyntax.Location, type);
            }

            return result;
        }

        public static bool CheckConstraints(
            this NamedTypeSymbol type,
            CSharpCompilation currentCompilation,
            ConversionsBase conversions,
            Location location,
            DiagnosticBag diagnostics)
        {
            if (!RequiresChecking(type))
            {
                return true;
            }

            var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
            var result = CheckTypeConstraints(type, conversions, currentCompilation, diagnosticsBuilder, ref useSiteDiagnosticsBuilder);

            if (useSiteDiagnosticsBuilder != null)
            {
                diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder);
            }

            foreach (var pair in diagnosticsBuilder)
            {
                diagnostics.Add(new CSDiagnostic(pair.DiagnosticInfo, location));
            }

            diagnosticsBuilder.Free();

            // we only check for distinct interfaces when the type is not from source, as we
            // trust that types that are from source have already been checked by the compiler
            // to prevent this from happening in the first place.
            if (!(currentCompilation != null && type.IsFromCompilation(currentCompilation)) && HasDuplicateInterfaces(type, null))
            {
                result = false;
                diagnostics.Add(ErrorCode.ERR_BogusType, location, type);
            }

            return result;
        }

        // C# does not let you declare a type in which it would be possible for distinct base interfaces
        // to unify under some instantiations.  But such ill-formed classes can come in through
        // metadata and be instantiated in C#.  We check to see if that's happened.
        private static bool HasDuplicateInterfaces(NamedTypeSymbol type, ConsList<Symbol> basesBeingResolved)
        {
            // PERF: avoid instantiating all interfaces here
            //       Ex: if class implements just IEnumerable<> and IComparable<> it cannot have conflicting implementations
            var array = type.OriginalDefinition.InterfacesNoUseSiteDiagnostics(basesBeingResolved);

            switch (array.Length)
            {
                case 0:
                case 1:
                    // less than 2 interfaces
                    return false;

                case 2:
                    if ((object)array[0].OriginalDefinition == array[1].OriginalDefinition)
                    {
                        break;
                    }

                    // two unrelated interfaces 
                    return false;

                default:
                    var set = new HashSet<NamedTypeSymbol>(ReferenceEqualityComparer.Instance);
                    foreach (var i in array)
                    {
                        if (!set.Add(i.OriginalDefinition))
                        {
                            goto hasRelatedInterfaces;
                        }
                    }

                    // all interfaces are unrelated
                    return false;
            }
            
            // very rare case. 
            // some implemented interfaces are related
            // will have to instantiate interfaces and check
            hasRelatedInterfaces:
            return type.InterfacesNoUseSiteDiagnostics(basesBeingResolved).HasDuplicates(TypeSymbol.EqualsIgnoringDynamicComparer);
        }

        public static bool CheckConstraints(
            this MethodSymbol method,
            ConversionsBase conversions,
            CSharpSyntaxNode syntaxNode,
            Compilation currentCompilation,
            DiagnosticBag diagnostics,
            BitVector skipParameters = default(BitVector))
        {
            if (!RequiresChecking(method))
            {
                return true;
            }

            var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
            var result = CheckMethodConstraints(method, conversions, currentCompilation, diagnosticsBuilder, ref useSiteDiagnosticsBuilder, skipParameters);

            if (useSiteDiagnosticsBuilder != null)
            {
                diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder);
            }

            foreach (var pair in diagnosticsBuilder)
            {
                var location = new SourceLocation(syntaxNode);
                diagnostics.Add(new CSDiagnostic(pair.DiagnosticInfo, location));
            }

            diagnosticsBuilder.Free();
            return result;
        }

        public static bool CheckConstraints(
            this MethodSymbol method,
            ConversionsBase conversions,
            Location location,
            Compilation currentCompilation,
            DiagnosticBag diagnostics)
        {
            if (!RequiresChecking(method))
            {
                return true;
            }

            var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
            var result = CheckMethodConstraints(method, conversions, currentCompilation, diagnosticsBuilder, ref useSiteDiagnosticsBuilder);

            if (useSiteDiagnosticsBuilder != null)
            {
                diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder);
            }

            foreach (var pair in diagnosticsBuilder)
            {
                diagnostics.Add(new CSDiagnostic(pair.DiagnosticInfo, location));
            }

            diagnosticsBuilder.Free();
            return result;
        }

        private static bool CheckTypeConstraints(
            NamedTypeSymbol type,
            ConversionsBase conversions,
            Compilation currentCompilation,
            ArrayBuilder<TypeParameterDiagnosticInfo> diagnosticsBuilder,
            ref ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder)
        {
            return CheckConstraints(
                type,
                conversions,
                type.TypeSubstitution,
                type.OriginalDefinition.TypeParameters,
                type.TypeArgumentsNoUseSiteDiagnostics,
                currentCompilation,
                diagnosticsBuilder,
                ref useSiteDiagnosticsBuilder);
        }

        private static bool CheckMethodConstraints(
            MethodSymbol method,
            ConversionsBase conversions,
            Compilation currentCompilation,
            ArrayBuilder<TypeParameterDiagnosticInfo> diagnosticsBuilder,
            ref ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder,
            BitVector skipParameters = default(BitVector))
        {
            return CheckConstraints(
                method,
                conversions,
                method.TypeSubstitution,
                ((MethodSymbol)method.OriginalDefinition).TypeParameters,
                method.TypeArguments,
                currentCompilation,
                diagnosticsBuilder,
                ref useSiteDiagnosticsBuilder,
                skipParameters);
        }

        /// <summary>
        /// Check type parameter constraints for the containing type or method symbol.
        /// </summary>
        /// <param name="containingSymbol">The generic type or method.</param>
        /// <param name="conversions">Conversions instance.</param>
        /// <param name="substitution">The map from type parameters to type arguments.</param>
        /// <param name="typeParameters">Containing symbol type parameters.</param>
        /// <param name="typeArguments">Containing symbol type arguments.</param>
        /// <param name="currentCompilation">Improves error message detail.</param>
        /// <param name="diagnosticsBuilder">Diagnostics.</param>
        /// <param name="skipParameters">Parameters to skip.</param>
        /// <param name="useSiteDiagnosticsBuilder"/>
        /// <returns>True if the constraints were satisfied, false otherwise.</returns>
        public static bool CheckConstraints(
            this Symbol containingSymbol,
            ConversionsBase conversions,
            TypeMap substitution,
            ImmutableArray<TypeParameterSymbol> typeParameters,
            ImmutableArray<TypeSymbolWithAnnotations> typeArguments,
            Compilation currentCompilation,
            ArrayBuilder<TypeParameterDiagnosticInfo> diagnosticsBuilder,
            ref ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder,
            BitVector skipParameters = default(BitVector))
        {
            Debug.Assert(typeParameters.Length == typeArguments.Length);
            Debug.Assert(typeParameters.Length > 0);

            int n = typeParameters.Length;
            bool succeeded = true;

            for (int i = 0; i < n; i++)
            {
                if (skipParameters[i])
                {
                    continue;
                }

                var typeArgument = typeArguments[i];
                var typeParameter = typeParameters[i];

                if (!CheckConstraints(containingSymbol, conversions, substitution, typeParameter, typeArgument, currentCompilation, diagnosticsBuilder, ref useSiteDiagnosticsBuilder))
                {
                    succeeded = false;
                }
            }

            return succeeded;
        }

        // See TypeBind::CheckSingleConstraint.
        private static bool CheckConstraints(
            Symbol containingSymbol,
            ConversionsBase conversions,
            TypeMap substitution,
            TypeParameterSymbol typeParameter,
            TypeSymbolWithAnnotations typeArgument,
            Compilation currentCompilation,
            ArrayBuilder<TypeParameterDiagnosticInfo> diagnosticsBuilder,
            ref ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder)
        {
            Debug.Assert(substitution != null);
            // The type parameters must be original definitions of type parameters from the containing symbol.
            Debug.Assert(ReferenceEquals(typeParameter.ContainingSymbol, containingSymbol.OriginalDefinition));

            if (typeArgument.TypeSymbol.IsErrorType())
            {
                return true;
            }

            if (typeArgument.IsPointerType() || typeArgument.IsRestrictedType() || typeArgument.SpecialType == SpecialType.System_Void)
            {
                // "The type '{0}' may not be used as a type argument"
                diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new CSDiagnosticInfo(ErrorCode.ERR_BadTypeArgument, typeArgument.TypeSymbol)));
                return false;
            }

            if (typeArgument.IsStatic)
            {
                // "'{0}': static types cannot be used as type arguments"
                diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new CSDiagnosticInfo(ErrorCode.ERR_GenericArgIsStaticClass, typeArgument.TypeSymbol)));
                return false;
            }

            if (typeParameter.HasReferenceTypeConstraint && !typeArgument.IsReferenceType)
            {
                // "The type '{2}' must be a reference type in order to use it as parameter '{1}' in the generic type or method '{0}'"
                diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new CSDiagnosticInfo(ErrorCode.ERR_RefConstraintNotSatisfied, containingSymbol.ConstructedFrom(), typeParameter, typeArgument.TypeSymbol)));
                return false;
            }

            if (typeParameter.HasValueTypeConstraint && !typeArgument.TypeSymbol.IsNonNullableValueType())
            {
                // "The type '{2}' must be a non-nullable value type in order to use it as parameter '{1}' in the generic type or method '{0}'"
                diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new CSDiagnosticInfo(ErrorCode.ERR_ValConstraintNotSatisfied, containingSymbol.ConstructedFrom(), typeParameter, typeArgument.TypeSymbol)));
                return false;
            }

            // The type parameters for a constructed type/method are the type parameters of
            // the ConstructedFrom type/method, so the constraint types are not substituted.
            // For instance with "class C<T, U> where T : U", the type parameter for T in "C<object, int>"
            // has constraint "U", not "int". We need to substitute the constraints from the
            // original definition of the type parameters using the map from the constructed symbol.
            var constraintTypes = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance();
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            substitution.SubstituteConstraintTypesDistinct(typeParameter.ConstraintTypesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics), constraintTypes);

            bool hasError = false;

            foreach (var constraintType in constraintTypes)
            {
                if (SatisfiesConstraintType(conversions, typeArgument, constraintType, ref useSiteDiagnostics))
                {
                    continue;
                }

                ErrorCode errorCode;
                if (typeArgument.IsReferenceType)
                {
                    errorCode = ErrorCode.ERR_GenericConstraintNotSatisfiedRefType;
                }
                else if (typeArgument.IsNullableType())
                {
                    errorCode = constraintType.TypeSymbol.IsInterfaceType() ? ErrorCode.ERR_GenericConstraintNotSatisfiedNullableInterface : ErrorCode.ERR_GenericConstraintNotSatisfiedNullableEnum;
                }
                else if (typeArgument.TypeKind == TypeKind.TypeParameter)
                {
                    errorCode = ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar;
                }
                else
                {
                    errorCode = ErrorCode.ERR_GenericConstraintNotSatisfiedValType;
                }

                SymbolDistinguisher distinguisher = new SymbolDistinguisher(currentCompilation, constraintType.TypeSymbol, typeArgument.TypeSymbol);
                diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new CSDiagnosticInfo(errorCode, containingSymbol.ConstructedFrom(), distinguisher.First, typeParameter, distinguisher.Second)));
                hasError = true;
            }

            if (AppendUseSiteDiagnostics(useSiteDiagnostics, typeParameter, ref useSiteDiagnosticsBuilder))
            {
                hasError = true;
            }

            constraintTypes.Free();

            // Check the constructor constraint.
            if (typeParameter.HasConstructorConstraint && !SatisfiesConstructorConstraint(typeArgument.TypeSymbol))
            {
                // "'{2}' must be a non-abstract type with a public parameterless constructor in order to use it as parameter '{1}' in the generic type or method '{0}'"
                diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new CSDiagnosticInfo(ErrorCode.ERR_NewConstraintNotSatisfied, containingSymbol.ConstructedFrom(), typeParameter, typeArgument.TypeSymbol)));
                return false;
            }

            return !hasError;
        }

        private static bool AppendUseSiteDiagnostics(
            HashSet<DiagnosticInfo> useSiteDiagnostics,
            TypeParameterSymbol typeParameter,
            ref ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder)
        {
            if (useSiteDiagnostics.IsNullOrEmpty())
            {
                return false;
            }

            if (useSiteDiagnosticsBuilder == null)
            {
                useSiteDiagnosticsBuilder = new ArrayBuilder<TypeParameterDiagnosticInfo>();
            }

            bool hasErrors = false;

            foreach (var info in useSiteDiagnostics)
            {
                if (info.Severity == DiagnosticSeverity.Error)
                {
                    hasErrors = true;
                }

                useSiteDiagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, info));
            }

            return hasErrors;
        }

        private static bool SatisfiesConstraintType(
            ConversionsBase conversions,
            TypeSymbolWithAnnotations typeArgument,
            TypeSymbolWithAnnotations constraintType,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (constraintType.TypeSymbol.IsErrorType())
            {
                return false;
            }

            // Spec 4.4.4 describes the valid conversions from
            // type argument A to constraint type C:

            // "An identity conversion (6.1.1).
            // An implicit reference conversion (6.1.6). ..."
            if (conversions.HasIdentityOrImplicitReferenceConversion(typeArgument.TypeSymbol, constraintType.TypeSymbol, ref useSiteDiagnostics))
            {
                return true;
            }

            // "... A boxing conversion (6.1.7), provided that type A is a non-nullable value type. ..."
            // NOTE: we extend this to allow, for example, a conversion from Nullable<T> to object.
            if (typeArgument.IsValueType &&
                conversions.HasBoxingConversion(typeArgument.TypeSymbol.IsNullableType() ? ((NamedTypeSymbol)typeArgument.TypeSymbol).ConstructedFrom : typeArgument.TypeSymbol, 
                                                constraintType.TypeSymbol, ref useSiteDiagnostics))
            {
                return true;
            }

            if (typeArgument.TypeKind == TypeKind.TypeParameter)
            {
                var typeParameter = (TypeParameterSymbol)typeArgument.TypeSymbol;

                // "... An implicit reference, boxing, or type parameter conversion
                // from type parameter A to C."
                if (conversions.HasImplicitTypeParameterConversion(typeParameter, constraintType.TypeSymbol, ref useSiteDiagnostics))
                {
                    return true;
                }

                // TypeBind::SatisfiesBound allows cases where one of the
                // type parameter constraints satisfies the constraint.
                foreach (var typeArgumentConstraint in typeParameter.ConstraintTypesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
                {
                    if (SatisfiesConstraintType(conversions, typeArgumentConstraint, constraintType, ref useSiteDiagnostics))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsReferenceType(TypeParameterSymbol typeParameter, ImmutableArray<TypeSymbolWithAnnotations> constraintTypes)
        {
            return typeParameter.HasReferenceTypeConstraint || TypeParameterSymbol.IsReferenceTypeFromConstraintTypes(constraintTypes);
        }

        private static bool IsValueType(TypeParameterSymbol typeParameter, ImmutableArray<TypeSymbolWithAnnotations> constraintTypes)
        {
            return typeParameter.HasValueTypeConstraint || TypeParameterSymbol.IsValueTypeFromConstraintTypes(constraintTypes);
        }

        private static TypeParameterDiagnosticInfo GenerateConflictingConstraintsError(TypeParameterSymbol typeParameter, TypeSymbol deducedBase, bool classConflict)
        {
            // "Type parameter '{0}' inherits conflicting constraints '{1}' and '{2}'"
            return new TypeParameterDiagnosticInfo(typeParameter, new CSDiagnosticInfo(ErrorCode.ERR_BaseConstraintConflict, typeParameter, deducedBase, classConflict ? "class" : "struct"));
        }

        private static void AddInterfaces(ArrayBuilder<NamedTypeSymbol> builder, ImmutableArray<NamedTypeSymbol> interfaces)
        {
            foreach (var @interface in interfaces)
            {
                AddInterface(builder, @interface);
            }
        }

        private static void AddInterface(ArrayBuilder<NamedTypeSymbol> builder, NamedTypeSymbol @interface)
        {
            if (!builder.Contains(@interface))
            {
                builder.Add(@interface);
            }
        }

        private static bool SatisfiesConstructorConstraint(TypeSymbol typeArgument)
        {
            switch (typeArgument.TypeKind)
            {
                case TypeKind.Struct:
                case TypeKind.Enum:
                case TypeKind.Dynamic:
                    return true;

                case TypeKind.Class:
                    return HasPublicParameterlessConstructor((NamedTypeSymbol)typeArgument) && !typeArgument.IsAbstract;

                case TypeKind.TypeParameter:
                    {
                        var typeParameter = (TypeParameterSymbol)typeArgument;
                        return typeParameter.HasConstructorConstraint || typeParameter.IsValueType;
                    }

                case TypeKind.Submission:
                    // submission can't be used as type argument
                    throw ExceptionUtilities.UnexpectedValue(typeArgument.TypeKind);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Return true if the class type has a public parameterless constructor.
        /// </summary>
        private static bool HasPublicParameterlessConstructor(NamedTypeSymbol type)
        {
            Debug.Assert(type.TypeKind == TypeKind.Class);
            foreach (var constructor in type.InstanceConstructors)
            {
                if (constructor.ParameterCount == 0)
                {
                    return constructor.DeclaredAccessibility == Accessibility.Public;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if type a is encompassed by type b (spec 6.4.3),
        /// and returns false otherwise.
        /// </summary>
        private static bool IsEncompassedBy(ConversionsBase conversions, TypeSymbol a, TypeSymbol b, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(IsValidEncompassedByArgument(a));
            Debug.Assert(IsValidEncompassedByArgument(b));

            return conversions.HasIdentityOrImplicitReferenceConversion(a, b, ref useSiteDiagnostics) || conversions.HasBoxingConversion(a, b, ref useSiteDiagnostics);
        }

        private static bool IsValidEncompassedByArgument(TypeSymbol type)
        {
            switch (type.TypeKind)
            {
                case TypeKind.Array:
                case TypeKind.Class:
                case TypeKind.Delegate:
                case TypeKind.Enum:
                case TypeKind.Struct:
                    return true;
                default:
                    return false;
            }
        }

        private static bool RequiresChecking(NamedTypeSymbol type)
        {
            if (type.Arity == 0)
            {
                return false;
            }

            // If type is the original definition, there is no need
            // to check constraints. In the following for instance:
            // class A<T> where T : struct
            // {
            //     A<T> F;
            // }
            if (ReferenceEquals(type.OriginalDefinition, type))
            {
                return false;
            }

            Debug.Assert(type.ConstructedFrom != type);
            return true;
        }

        private static bool RequiresChecking(MethodSymbol method)
        {
            if (!method.IsGenericMethod)
            {
                return false;
            }

            // If method is the original definition, there is no need
            // to check constraints. In the following for instance:
            // void M<T>() where T : class
            // {
            //     M<T>();
            // }
            if (ReferenceEquals(method.OriginalDefinition, method))
            {
                return false;
            }

            Debug.Assert(method.ConstructedFrom != method);
            return true;
        }

        [Conditional("DEBUG")]
        private static void CheckEffectiveAndDeducedBaseTypes(ConversionsBase conversions, TypeSymbol effectiveBase, TypeSymbol deducedBase)
        {
            Debug.Assert((object)deducedBase != null);
            Debug.Assert((object)effectiveBase != null);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            Debug.Assert(deducedBase.IsErrorType() ||
                effectiveBase.IsErrorType() ||
                conversions.HasIdentityOrImplicitReferenceConversion(deducedBase, effectiveBase, ref useSiteDiagnostics) ||
                conversions.HasBoxingConversion(deducedBase, effectiveBase, ref useSiteDiagnostics));
        }
    }
}
