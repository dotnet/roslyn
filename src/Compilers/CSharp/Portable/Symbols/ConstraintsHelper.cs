// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A tuple of TypeParameterSymbol and DiagnosticInfo, created for errors
    /// reported from ConstraintsHelper rather than creating Diagnostics directly.
    /// This decouples constraints checking from syntax and Locations, and supports
    /// callers that may want to create Location instances lazily or not at all.
    /// </summary>
    internal readonly struct TypeParameterDiagnosticInfo
    {
        public readonly TypeParameterSymbol TypeParameter;
        public readonly UseSiteInfo<AssemblySymbol> UseSiteInfo;

        public TypeParameterDiagnosticInfo(TypeParameterSymbol typeParameter, UseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            this.TypeParameter = typeParameter;
            this.UseSiteInfo = useSiteInfo;
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
            ImmutableArray<TypeWithAnnotations> constraintTypes,
            bool inherited,
            CSharpCompilation currentCompilation,
            BindingDiagnosticBag diagnostics)
        {
            var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
            var bounds = typeParameter.ResolveBounds(corLibrary, inProgress, constraintTypes, inherited, currentCompilation, diagnosticsBuilder, ref useSiteDiagnosticsBuilder,
                                                     template: new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, currentCompilation.Assembly));

            if (useSiteDiagnosticsBuilder != null)
            {
                diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder);
            }

            foreach (var pair in diagnosticsBuilder)
            {
                diagnostics.Add(pair.UseSiteInfo, pair.TypeParameter.GetFirstLocation());
            }

            diagnosticsBuilder.Free();
            return bounds;
        }

        // Based on SymbolLoader::ResolveBounds.
        public static TypeParameterBounds ResolveBounds(
            this TypeParameterSymbol typeParameter,
            AssemblySymbol corLibrary,
            ConsList<TypeParameterSymbol> inProgress,
            ImmutableArray<TypeWithAnnotations> constraintTypes,
            bool inherited,
            CSharpCompilation currentCompilation,
            ArrayBuilder<TypeParameterDiagnosticInfo> diagnosticsBuilder,
            ref ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder,
            CompoundUseSiteInfo<AssemblySymbol> template)
        {
            Debug.Assert(currentCompilation == null || typeParameter.IsFromCompilation(currentCompilation));

            ImmutableArray<NamedTypeSymbol> interfaces;

            NamedTypeSymbol effectiveBaseClass = corLibrary.GetSpecialType(typeParameter.HasValueTypeConstraint ? SpecialType.System_ValueType : SpecialType.System_Object);
            TypeSymbol deducedBaseType = effectiveBaseClass;

            if (constraintTypes.Length == 0)
            {
                interfaces = ImmutableArray<NamedTypeSymbol>.Empty;
            }
            else
            {
                var constraintTypesBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance();
                var interfacesBuilder = ArrayBuilder<NamedTypeSymbol>.GetInstance();
                var conversions = new TypeConversions(corLibrary);
                var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(template);

                // Resolve base types, determine the effective base class and
                // interfaces, and filter out any constraint types that cause cycles.
                foreach (var constraintType in constraintTypes)
                {
                    Debug.Assert(!constraintType.Type.ContainsDynamic());

                    NamedTypeSymbol constraintEffectiveBase;
                    TypeSymbol constraintDeducedBase;

                    switch (constraintType.TypeKind)
                    {
                        case TypeKind.TypeParameter:
                            {
                                var constraintTypeParameter = (TypeParameterSymbol)constraintType.Type;
                                ConsList<TypeParameterSymbol> constraintsInProgress;

                                if (constraintTypeParameter.ContainingSymbol == typeParameter.ContainingSymbol)
                                {
                                    // The constraint type parameter is from the same containing type or method.
                                    if (inProgress.ContainsReference(constraintTypeParameter))
                                    {
                                        // "Circular constraint dependency involving '{0}' and '{1}'"
                                        diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(constraintTypeParameter, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_CircularConstraint, constraintTypeParameter, typeParameter))));
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

                                if (!inherited && currentCompilation != null && constraintTypeParameter.IsFromCompilation(currentCompilation))
                                {
                                    ErrorCode errorCode;
                                    if (constraintTypeParameter.HasUnmanagedTypeConstraint)
                                    {
                                        errorCode = ErrorCode.ERR_ConWithUnmanagedCon;
                                    }
                                    else if (constraintTypeParameter.HasValueTypeConstraint)
                                    {
                                        errorCode = ErrorCode.ERR_ConWithValCon;
                                    }
                                    else
                                    {
                                        break;
                                    }

                                    // "Type parameter '{1}' has the '?' constraint so '{1}' cannot be used as a constraint for '{0}'"
                                    diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(errorCode, typeParameter, constraintTypeParameter))));
                                    continue;
                                }
                            }
                            break;

                        case TypeKind.Interface:
                        case TypeKind.Class:
                        case TypeKind.Delegate:

                            Debug.Assert(inherited || currentCompilation == null || constraintType.TypeKind != TypeKind.Delegate);

                            if (constraintType.Type.IsInterfaceType())
                            {
                                AddInterface(interfacesBuilder, (NamedTypeSymbol)constraintType.Type);
                                constraintTypesBuilder.Add(constraintType);
                                continue;
                            }
                            else
                            {
                                constraintEffectiveBase = (NamedTypeSymbol)constraintType.Type;
                                constraintDeducedBase = constraintType.Type;
                                break;
                            }

                        case TypeKind.Struct:
                            if (constraintType.IsNullableType())
                            {
                                var underlyingType = constraintType.Type.GetNullableUnderlyingType();
                                if (underlyingType.TypeKind == TypeKind.TypeParameter)
                                {
                                    var underlyingTypeParameter = (TypeParameterSymbol)underlyingType;
                                    if (underlyingTypeParameter.ContainingSymbol == typeParameter.ContainingSymbol)
                                    {
                                        // The constraint type parameter is from the same containing type or method.
                                        if (inProgress.ContainsReference(underlyingTypeParameter))
                                        {
                                            // "Circular constraint dependency involving '{0}' and '{1}'"
                                            diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(underlyingTypeParameter, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_CircularConstraint, underlyingTypeParameter, typeParameter))));
                                            continue;
                                        }
                                    }
                                }
                            }
                            Debug.Assert(inherited || currentCompilation == null);
                            constraintEffectiveBase = corLibrary.GetSpecialType(SpecialType.System_ValueType);
                            constraintDeducedBase = constraintType.Type;
                            break;

                        case TypeKind.Enum:
                            Debug.Assert(inherited || currentCompilation == null);
                            constraintEffectiveBase = corLibrary.GetSpecialType(SpecialType.System_Enum);
                            constraintDeducedBase = constraintType.Type;
                            break;

                        case TypeKind.Array:
                            Debug.Assert(inherited || currentCompilation == null);
                            constraintEffectiveBase = corLibrary.GetSpecialType(SpecialType.System_Array);
                            constraintDeducedBase = constraintType.Type;
                            break;

                        case TypeKind.Error:
                            constraintEffectiveBase = (NamedTypeSymbol)constraintType.Type;
                            constraintDeducedBase = constraintType.Type;
                            break;

                        case TypeKind.Pointer:
                        case TypeKind.FunctionPointer:
                            // Such a constraint can only be introduced by type substitution,
                            // in which case it is already reported elsewhere, so we ignore this constraint.
                            continue;

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
                        if (!IsEncompassedBy(conversions, deducedBaseType, constraintDeducedBase, ref useSiteInfo))
                        {
                            if (!IsEncompassedBy(conversions, constraintDeducedBase, deducedBaseType, ref useSiteInfo))
                            {
                                // "Type parameter '{0}' inherits conflicting constraints '{1}' and '{2}'"
                                diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_BaseConstraintConflict, typeParameter, constraintDeducedBase, deducedBaseType))));
                            }
                            else
                            {
                                deducedBaseType = constraintDeducedBase;
                                effectiveBaseClass = constraintEffectiveBase;
                            }
                        }
                    }
                }

                AppendUseSiteDiagnostics(useSiteInfo, typeParameter, ref useSiteDiagnosticsBuilder);

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

        internal static ImmutableArray<ImmutableArray<TypeWithAnnotations>> MakeTypeParameterConstraintTypes(
            this MethodSymbol containingSymbol,
            Binder withTypeParametersBinder,
            ImmutableArray<TypeParameterSymbol> typeParameters,
            TypeParameterListSyntax typeParameterList,
            SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            BindingDiagnosticBag diagnostics)
        {
            if (typeParameters.Length == 0 || constraintClauses.Count == 0)
            {
                return ImmutableArray<ImmutableArray<TypeWithAnnotations>>.Empty;
            }

            // Wrap binder from factory in a generic constraints specific binder
            // to avoid checking constraints when binding type names.
            Debug.Assert(!withTypeParametersBinder.Flags.Includes(BinderFlags.GenericConstraintsClause));
            withTypeParametersBinder = withTypeParametersBinder.WithAdditionalFlags(BinderFlags.GenericConstraintsClause | BinderFlags.SuppressConstraintChecks);

            ImmutableArray<TypeParameterConstraintClause> clauses;
            clauses = withTypeParametersBinder.BindTypeParameterConstraintClauses(containingSymbol, typeParameters, typeParameterList, constraintClauses,
                                                                diagnostics, performOnlyCycleSafeValidation: false);

            if (clauses.All(clause => clause.ConstraintTypes.IsEmpty))
            {
                return ImmutableArray<ImmutableArray<TypeWithAnnotations>>.Empty;
            }

            return clauses.SelectAsArray(clause => clause.ConstraintTypes);
        }

        internal static ImmutableArray<TypeParameterConstraintKind> MakeTypeParameterConstraintKinds(
            this MethodSymbol containingSymbol,
            Binder withTypeParametersBinder,
            ImmutableArray<TypeParameterSymbol> typeParameters,
            TypeParameterListSyntax typeParameterList,
            SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses)
        {
            if (typeParameters.Length == 0)
            {
                return ImmutableArray<TypeParameterConstraintKind>.Empty;
            }

            ImmutableArray<TypeParameterConstraintClause> clauses;

            if (constraintClauses.Count == 0)
            {
                clauses = withTypeParametersBinder.GetDefaultTypeParameterConstraintClauses(typeParameterList);
            }
            else
            {
                // Wrap binder from factory in a generic constraints specific binder
                // Also, suppress type argument binding in constraint types, this helps to avoid cycles while we figure out constraint kinds. 
                // to avoid checking constraints when binding type names.
                Debug.Assert(!withTypeParametersBinder.Flags.Includes(BinderFlags.GenericConstraintsClause));
                withTypeParametersBinder = withTypeParametersBinder.WithAdditionalFlags(BinderFlags.GenericConstraintsClause | BinderFlags.SuppressConstraintChecks | BinderFlags.SuppressTypeArgumentBinding);

                // We will recompute this diagnostics more accurately later, when binding without BinderFlags.SuppressTypeArgumentBinding  
                clauses = withTypeParametersBinder.BindTypeParameterConstraintClauses(containingSymbol, typeParameters, typeParameterList, constraintClauses,
                                                                    BindingDiagnosticBag.Discarded, performOnlyCycleSafeValidation: true);

                clauses = AdjustConstraintKindsBasedOnConstraintTypes(typeParameters, clauses);
            }

            if (clauses.All(clause => clause.Constraints == TypeParameterConstraintKind.None))
            {
                return ImmutableArray<TypeParameterConstraintKind>.Empty;
            }

            return clauses.SelectAsArray(clause => clause.Constraints);
        }

        internal static ImmutableArray<TypeParameterConstraintClause> AdjustConstraintKindsBasedOnConstraintTypes(ImmutableArray<TypeParameterSymbol> typeParameters, ImmutableArray<TypeParameterConstraintClause> constraintClauses)
        {
            int arity = typeParameters.Length;

            Debug.Assert(constraintClauses.Length == arity);

            SmallDictionary<TypeParameterSymbol, bool> isValueTypeMap = TypeParameterConstraintClause.BuildIsValueTypeMap(typeParameters, constraintClauses);
            SmallDictionary<TypeParameterSymbol, bool> isReferenceTypeFromConstraintTypesMap = TypeParameterConstraintClause.BuildIsReferenceTypeFromConstraintTypesMap(typeParameters, constraintClauses);
            ArrayBuilder<TypeParameterConstraintClause> builder = null;

            for (int i = 0; i < arity; i++)
            {
                var constraint = constraintClauses[i];
                var typeParameter = typeParameters[i];
                TypeParameterConstraintKind constraintKind = constraint.Constraints;

                Debug.Assert((constraintKind & (TypeParameterConstraintKind.ValueTypeFromConstraintTypes | TypeParameterConstraintKind.ReferenceTypeFromConstraintTypes)) == 0);

                if ((constraintKind & TypeParameterConstraintKind.AllValueTypeKinds) == 0 && isValueTypeMap[typeParameter])
                {
                    constraintKind |= TypeParameterConstraintKind.ValueTypeFromConstraintTypes;
                }

                if (isReferenceTypeFromConstraintTypesMap[typeParameter])
                {
                    constraintKind |= TypeParameterConstraintKind.ReferenceTypeFromConstraintTypes;
                }

                if (constraint.Constraints != constraintKind)
                {
                    if (builder == null)
                    {
                        builder = ArrayBuilder<TypeParameterConstraintClause>.GetInstance(constraintClauses.Length);
                        builder.AddRange(constraintClauses);
                    }

                    builder[i] = TypeParameterConstraintClause.Create(constraintKind, constraint.ConstraintTypes);
                }
            }

            if (builder != null)
            {
                constraintClauses = builder.ToImmutableAndFree();
            }

            return constraintClauses;
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
            CSharpCompilation compilation,
            ConversionsBase conversions,
            Location location,
            BindingDiagnosticBag diagnostics)
        {
            bool includeNullability = compilation.IsFeatureEnabled(MessageID.IDS_FeatureNullableReferenceTypes);
            var boxedArgs = CheckConstraintsArgsBoxed.Allocate(compilation, conversions, includeNullability, location, diagnostics);
            type.CheckAllConstraints(boxedArgs);
            boxedArgs.Free();
        }

        public static bool CheckAllConstraints(
            this TypeSymbol type,
            CSharpCompilation compilation,
            ConversionsBase conversions)
        {
            var diagnostics = new BindingDiagnosticBag(DiagnosticBag.GetInstance());

            // Nullability checks can only add warnings here so skip them for this check as we are only
            // concerned with errors.
            var boxedArgs = CheckConstraintsArgsBoxed.Allocate(compilation, conversions, includeNullability: false, NoLocation.Singleton, diagnostics);
            type.CheckAllConstraints(boxedArgs);
            bool ok = !diagnostics.HasAnyErrors();
            boxedArgs.Free();
            diagnostics.Free();
            return ok;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckAllConstraints(this TypeSymbol type, CheckConstraintsArgsBoxed args)
        {
            type.VisitType(s_checkConstraintsSingleTypeFunc, args);
        }

        internal readonly struct CheckConstraintsArgs
        {
            public readonly CSharpCompilation CurrentCompilation;
            public readonly ConversionsBase Conversions;
            public readonly bool IncludeNullability;
            public readonly Location Location;
            public readonly BindingDiagnosticBag Diagnostics;
            public readonly CompoundUseSiteInfo<AssemblySymbol> Template;

            public CheckConstraintsArgs(CSharpCompilation currentCompilation, ConversionsBase conversions, Location location, BindingDiagnosticBag diagnostics) :
                this(currentCompilation, conversions, currentCompilation.IsFeatureEnabled(MessageID.IDS_FeatureNullableReferenceTypes), location, diagnostics)
            {
            }

            public CheckConstraintsArgs(CSharpCompilation currentCompilation, ConversionsBase conversions, bool includeNullability, Location location, BindingDiagnosticBag diagnostics) :
                this(currentCompilation, conversions, includeNullability, location, diagnostics, template: new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, currentCompilation.Assembly))
            {
            }

            public CheckConstraintsArgs(CSharpCompilation currentCompilation, ConversionsBase conversions, bool includeNullability, Location location, BindingDiagnosticBag diagnostics, CompoundUseSiteInfo<AssemblySymbol> template)
            {
                this.CurrentCompilation = currentCompilation;
                this.Conversions = conversions;
                this.IncludeNullability = includeNullability;
                this.Location = location;
                this.Diagnostics = diagnostics;
                this.Template = template;
            }
        }

        private static readonly ObjectPool<CheckConstraintsArgsBoxed> s_checkConstraintsArgsBoxedPool = new ObjectPool<CheckConstraintsArgsBoxed>(static () => new CheckConstraintsArgsBoxed());

        internal sealed class CheckConstraintsArgsBoxed
        {
            public CheckConstraintsArgs Args;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static CheckConstraintsArgsBoxed Allocate(CSharpCompilation currentCompilation, ConversionsBase conversions, Location location, BindingDiagnosticBag diagnostics)
            {
                var boxedArgs = s_checkConstraintsArgsBoxedPool.Allocate();
                boxedArgs.Args = new CheckConstraintsArgs(currentCompilation, conversions, location, diagnostics);
                return boxedArgs;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static CheckConstraintsArgsBoxed Allocate(CSharpCompilation currentCompilation, ConversionsBase conversions, bool includeNullability, Location location, BindingDiagnosticBag diagnostics)
            {
                var boxedArgs = s_checkConstraintsArgsBoxedPool.Allocate();
                boxedArgs.Args = new CheckConstraintsArgs(currentCompilation, conversions, includeNullability, location, diagnostics);
                return boxedArgs;
            }

            public void Free()
            {
                s_checkConstraintsArgsBoxedPool.Free(this);
            }
        }

        private static readonly Func<TypeSymbol, CheckConstraintsArgsBoxed, bool, bool> s_checkConstraintsSingleTypeFunc = (type, arg, unused) => CheckConstraintsSingleType(type, in arg.Args);

        private static bool CheckConstraintsSingleType(TypeSymbol type, in CheckConstraintsArgs args)
        {
            if (type.Kind == SymbolKind.NamedType)
            {
                ((NamedTypeSymbol)type).CheckConstraints(args);
            }
            else if (type.Kind == SymbolKind.PointerType)
            {
                Binder.CheckManagedAddr(args.CurrentCompilation, ((PointerTypeSymbol)type).PointedAtType, args.Location, args.Diagnostics);
            }
            return false; // continue walking types
        }

        public static void CheckConstraints(
            this NamedTypeSymbol tuple,
            in CheckConstraintsArgs args,
            SyntaxNode typeSyntax,
            ImmutableArray<Location> elementLocations,
            BindingDiagnosticBag nullabilityDiagnosticsOpt)
        {
            Debug.Assert(tuple.IsTupleType);
            if (!RequiresChecking(tuple))
            {
                return;
            }

            if (typeSyntax.HasErrors)
            {
                return;
            }

            var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            var nullabilityDiagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            var underlyingTupleTypeChain = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            NamedTypeSymbol.GetUnderlyingTypeChain(tuple, underlyingTupleTypeChain);

            int offset = 0;
            foreach (var underlyingTuple in underlyingTupleTypeChain)
            {
                ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
                CheckTypeConstraints(
                    underlyingTuple,
                    in args,
                    diagnosticsBuilder,
                    nullabilityDiagnosticsBuilderOpt: (nullabilityDiagnosticsOpt is null) ? null : nullabilityDiagnosticsBuilder,
                    ref useSiteDiagnosticsBuilder);

                if (useSiteDiagnosticsBuilder != null)
                {
                    diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder);
                }

                populateDiagnosticsAndClear(diagnosticsBuilder, args.Diagnostics);
                populateDiagnosticsAndClear(nullabilityDiagnosticsBuilder, nullabilityDiagnosticsOpt);

                offset += NamedTypeSymbol.ValueTupleRestIndex;

                void populateDiagnosticsAndClear(ArrayBuilder<TypeParameterDiagnosticInfo> builder, BindingDiagnosticBag bag)
                {
                    if (bag is null)
                    {
                        builder.Clear();
                        return;
                    }

                    foreach (var pair in builder)
                    {
                        var ordinal = pair.TypeParameter.Ordinal;

                        // If this is the TRest type parameter, we report it on 
                        // the entire type syntax as it does not map to any tuple element.
                        var location = ordinal == NamedTypeSymbol.ValueTupleRestIndex ? typeSyntax.Location : elementLocations[ordinal + offset];
                        bag.Add(pair.UseSiteInfo, location);
                    }

                    builder.Clear();
                }
            }

            underlyingTupleTypeChain.Free();
            diagnosticsBuilder.Free();
            nullabilityDiagnosticsBuilder.Free();
        }

        public static bool CheckConstraintsForNamedType(
            this NamedTypeSymbol type,
            in CheckConstraintsArgs args,
            SyntaxNode typeSyntax,
            SeparatedSyntaxList<TypeSyntax> typeArgumentsSyntax, // may be omitted in synthesized invocations
            ConsList<TypeSymbol> basesBeingResolved)
        {
            Debug.Assert(typeArgumentsSyntax.Count == 0 /*omitted*/ || typeArgumentsSyntax.Count == type.Arity);

            if (!RequiresChecking(type))
            {
                return true;
            }

            var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
            var result = !typeSyntax.HasErrors && CheckTypeConstraints(type, in args, diagnosticsBuilder, nullabilityDiagnosticsBuilderOpt: args.IncludeNullability ? diagnosticsBuilder : null,
                                                                       ref useSiteDiagnosticsBuilder);

            if (useSiteDiagnosticsBuilder != null)
            {
                diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder);
            }

            foreach (var pair in diagnosticsBuilder)
            {
                int ordinal = pair.TypeParameter.Ordinal;
                var location = ordinal < typeArgumentsSyntax.Count ? typeArgumentsSyntax[ordinal].Location : args.Location;
                args.Diagnostics.Add(pair.UseSiteInfo, location);
            }

            diagnosticsBuilder.Free();

            if (HasDuplicateInterfaces(type, basesBeingResolved))
            {
                result = false;
                args.Diagnostics.Add(ErrorCode.ERR_BogusType, args.Location, type);
            }

            return result;
        }

        public static bool CheckConstraints(this NamedTypeSymbol type, in CheckConstraintsArgs args)
        {
            Debug.Assert(args.CurrentCompilation is object);

            if (!RequiresChecking(type))
            {
                return true;
            }

            var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
            var result = CheckTypeConstraints(type, in args, diagnosticsBuilder, nullabilityDiagnosticsBuilderOpt: args.IncludeNullability ? diagnosticsBuilder : null,
                                              ref useSiteDiagnosticsBuilder);

            if (useSiteDiagnosticsBuilder != null)
            {
                diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder);
            }

            foreach (var pair in diagnosticsBuilder)
            {
                args.Diagnostics.Add(pair.UseSiteInfo, args.Location);
            }

            diagnosticsBuilder.Free();

            // we only check for distinct interfaces when the type is not from source, as we
            // trust that types that are from source have already been checked by the compiler
            // to prevent this from happening in the first place.
            if (!(args.CurrentCompilation != null && type.IsFromCompilation(args.CurrentCompilation)) && HasDuplicateInterfaces(type, null))
            {
                result = false;
                args.Diagnostics.Add(ErrorCode.ERR_BogusType, args.Location, type);
            }

            return result;
        }

        // C# does not let you declare a type in which it would be possible for distinct base interfaces
        // to unify under some instantiations.  But such ill-formed classes can come in through
        // metadata and be instantiated in C#.  We check to see if that's happened.
        private static bool HasDuplicateInterfaces(NamedTypeSymbol type, ConsList<TypeSymbol> basesBeingResolved)
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
                    var set = PooledHashSet<object>.GetInstance();
                    foreach (var i in array)
                    {
                        if (!set.Add(i.OriginalDefinition))
                        {
                            set.Free();
                            goto hasRelatedInterfaces;
                        }
                    }

                    // all interfaces are unrelated
                    set.Free();
                    return false;
            }

// very rare case. 
// some implemented interfaces are related
// will have to instantiate interfaces and check
hasRelatedInterfaces:
            return type.InterfacesNoUseSiteDiagnostics(basesBeingResolved).HasDuplicates(Symbols.SymbolEqualityComparer.IgnoringDynamicTupleNamesAndNullability);
        }

        public static bool CheckConstraints(
            this MethodSymbol method,
            in CheckConstraintsArgs args)
        {
            if (!RequiresChecking(method))
            {
                return true;
            }

            var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
            var result = CheckMethodConstraints(method, in args, diagnosticsBuilder, nullabilityDiagnosticsBuilderOpt: null,
                                                ref useSiteDiagnosticsBuilder);

            if (useSiteDiagnosticsBuilder != null)
            {
                diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder);
            }

            foreach (var pair in diagnosticsBuilder)
            {
                args.Diagnostics.Add(pair.UseSiteInfo, args.Location);
            }

            diagnosticsBuilder.Free();
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckTypeConstraints(
            NamedTypeSymbol type,
            in CheckConstraintsArgs args,
            ArrayBuilder<TypeParameterDiagnosticInfo> diagnosticsBuilder,
            ArrayBuilder<TypeParameterDiagnosticInfo> nullabilityDiagnosticsBuilderOpt,
            ref ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder)
        {
            return CheckConstraints(
                type,
                in args,
                type.TypeSubstitution,
                type.OriginalDefinition.TypeParameters,
                type.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics,
                diagnosticsBuilder,
                nullabilityDiagnosticsBuilderOpt,
                ref useSiteDiagnosticsBuilder);
        }

        public static bool CheckMethodConstraints(
            MethodSymbol method,
            in CheckConstraintsArgs args,
            ArrayBuilder<TypeParameterDiagnosticInfo> diagnosticsBuilder,
            ArrayBuilder<TypeParameterDiagnosticInfo> nullabilityDiagnosticsBuilderOpt,
            ref ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder,
            BitVector skipParameters = default(BitVector))
        {
            return CheckConstraints(
                method,
                in args,
                method.TypeSubstitution,
                ((MethodSymbol)method.OriginalDefinition).TypeParameters,
                method.TypeArgumentsWithAnnotations,
                diagnosticsBuilder,
                nullabilityDiagnosticsBuilderOpt,
                ref useSiteDiagnosticsBuilder,
                skipParameters);
        }

        /// <summary>
        /// Check type parameter constraints for the containing type or method symbol.
        /// </summary>
        /// <param name="containingSymbol">The generic type or method.</param>
        /// <param name="args">Arguments for constraints checking.</param>
        /// <param name="substitution">The map from type parameters to type arguments.</param>
        /// <param name="typeParameters">Containing symbol type parameters.</param>
        /// <param name="typeArguments">Containing symbol type arguments.</param>
        /// <param name="diagnosticsBuilder">Diagnostics.</param>
        /// <param name="nullabilityDiagnosticsBuilderOpt">Nullability warnings.</param>
        /// <param name="skipParameters">Parameters to skip.</param>
        /// <param name="useSiteDiagnosticsBuilder"/>
        /// <param name="ignoreTypeConstraintsDependentOnTypeParametersOpt">If an original form of a type constraint 
        /// depends on a type parameter from this set, do not verify this type constraint.</param>
        /// <returns>True if the constraints were satisfied, false otherwise.</returns>
        public static bool CheckConstraints(
            this Symbol containingSymbol,
            in CheckConstraintsArgs args,
            TypeMap substitution,
            ImmutableArray<TypeParameterSymbol> typeParameters,
            ImmutableArray<TypeWithAnnotations> typeArguments,
            ArrayBuilder<TypeParameterDiagnosticInfo> diagnosticsBuilder,
            ArrayBuilder<TypeParameterDiagnosticInfo> nullabilityDiagnosticsBuilderOpt,
            ref ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder,
            BitVector skipParameters = default(BitVector),
            HashSet<TypeParameterSymbol> ignoreTypeConstraintsDependentOnTypeParametersOpt = null)
        {
            Debug.Assert(typeParameters.Length == typeArguments.Length);
            Debug.Assert(typeParameters.Length > 0);
            Debug.Assert(!args.Conversions.IncludeNullability || (nullabilityDiagnosticsBuilderOpt != null));

            int n = typeParameters.Length;
            bool succeeded = true;

            for (int i = 0; i < n; i++)
            {
                if (skipParameters[i])
                {
                    continue;
                }

                if (!CheckConstraints(containingSymbol, in args, substitution, typeParameters[i], typeArguments[i], diagnosticsBuilder, nullabilityDiagnosticsBuilderOpt,
                                      ref useSiteDiagnosticsBuilder,
                                      ignoreTypeConstraintsDependentOnTypeParametersOpt))
                {
                    succeeded = false;
                }
            }

            return succeeded;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool CheckBasicConstraints(
            Symbol containingSymbol,
            in CheckConstraintsArgs args,
            TypeParameterSymbol typeParameter,
            TypeWithAnnotations typeArgument,
            ArrayBuilder<TypeParameterDiagnosticInfo> diagnosticsBuilder,
            ArrayBuilder<TypeParameterDiagnosticInfo> nullabilityDiagnosticsBuilderOpt,
            ref ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder)
        {
            if (typeArgument.Type.IsPointerOrFunctionPointer() || typeArgument.IsRestrictedType() || typeArgument.IsVoidType())
            {
                // "The type '{0}' may not be used as a type argument"
                diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_BadTypeArgument, typeArgument.Type))));
                return false;
            }

            if (typeArgument.IsStatic)
            {
                // "'{0}': static types cannot be used as type arguments"
                diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_GenericArgIsStaticClass, typeArgument.Type))));
                return false;
            }

            if (typeParameter.HasReferenceTypeConstraint)
            {
                if (!typeArgument.Type.IsReferenceType)
                {
                    // "The type '{2}' must be a reference type in order to use it as parameter '{1}' in the generic type or method '{0}'"
                    diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_RefConstraintNotSatisfied, containingSymbol.ConstructedFrom(), typeParameter, typeArgument.Type))));
                    return false;
                }
            }

            CheckNullability(containingSymbol, typeParameter, typeArgument, nullabilityDiagnosticsBuilderOpt);

            if (typeParameter.HasUnmanagedTypeConstraint)
            {
                var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(args.Template);
                var managedKind = typeArgument.Type.GetManagedKind(ref useSiteInfo);
                AppendUseSiteDiagnostics(useSiteInfo, typeParameter, ref useSiteDiagnosticsBuilder);

                if (managedKind == ManagedKind.Managed || !typeArgument.Type.IsNonNullableValueType())
                {
                    // "The type '{2}' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter '{1}' in the generic type or method '{0}'"
                    diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, containingSymbol.ConstructedFrom(), typeParameter, typeArgument.Type))));
                    return false;
                }
                else if (managedKind == ManagedKind.UnmanagedWithGenerics)
                {
                    // When there is no compilation, we are being invoked through the API IMethodSymbol.ReduceExtensionMethod(...).
                    // In that case we consider the unmanaged constraint to be satisfied as if we were compiling with the latest
                    // language version.  The net effect of this is that in some IDE scenarios completion might consider an
                    // extension method to be applicable, but then when you try to use it the IDE tells you to upgrade your language version.
                    if (!(args.CurrentCompilation is null))
                    {
                        var csDiagnosticInfo = MessageID.IDS_FeatureUnmanagedConstructedTypes.GetFeatureAvailabilityDiagnosticInfo(args.CurrentCompilation);
                        if (csDiagnosticInfo != null)
                        {
                            diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new UseSiteInfo<AssemblySymbol>(csDiagnosticInfo)));
                            return false;
                        }
                    }
                }
            }

            if (typeParameter.HasValueTypeConstraint && !typeArgument.Type.IsNonNullableValueType())
            {
                // "The type '{2}' must be a non-nullable value type in order to use it as parameter '{1}' in the generic type or method '{0}'"
                diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_ValConstraintNotSatisfied, containingSymbol.ConstructedFrom(), typeParameter, typeArgument.Type))));
                return false;
            }

            return true;
        }

        // See TypeBind::CheckSingleConstraint.
        // Any new locals added to this method are likely going to cause EndToEndTests.Constraints to overflow. Break new locals out into
        // another function.
        private static bool CheckConstraints(
            Symbol containingSymbol,
            in CheckConstraintsArgs args,
            TypeMap substitution,
            TypeParameterSymbol typeParameter,
            TypeWithAnnotations typeArgument,
            ArrayBuilder<TypeParameterDiagnosticInfo> diagnosticsBuilder,
            ArrayBuilder<TypeParameterDiagnosticInfo> nullabilityDiagnosticsBuilderOpt,
            ref ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder,
            HashSet<TypeParameterSymbol> ignoreTypeConstraintsDependentOnTypeParametersOpt)
        {
            Debug.Assert(substitution != null);

            // The type parameters must be original definitions of type parameters from the containing symbol.
            Debug.Assert(ReferenceEquals(typeParameter.ContainingSymbol, containingSymbol.OriginalDefinition));

            if (typeArgument.Type.IsErrorType())
            {
                return true;
            }

            if (!CheckBasicConstraints(containingSymbol, in args, typeParameter, typeArgument, diagnosticsBuilder, nullabilityDiagnosticsBuilderOpt, ref useSiteDiagnosticsBuilder))
            {
                return false;
            }

            // The type parameters for a constructed type/method are the type parameters of
            // the ConstructedFrom type/method, so the constraint types are not substituted.
            // For instance with "class C<T, U> where T : U", the type parameter for T in "C<object, int>"
            // has constraint "U", not "int". We need to substitute the constraints from the
            // original definition of the type parameters using the map from the constructed symbol.
            var constraintTypes = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(args.Template);
            ImmutableArray<TypeWithAnnotations> originalConstraintTypes = typeParameter.ConstraintTypesWithDefinitionUseSiteDiagnostics(ref useSiteInfo);
            substitution.SubstituteConstraintTypesDistinctWithoutModifiers(typeParameter, originalConstraintTypes, constraintTypes,
                                                                           ignoreTypeConstraintsDependentOnTypeParametersOpt);
            bool hasError = false;

            if (typeArgument.Type is NamedTypeSymbol { IsInterface: true } iface && SelfOrBaseHasStaticAbstractMember(iface, ref useSiteInfo, out Symbol member))
            {
                diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter,
                    new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_GenericConstraintNotSatisfiedInterfaceWithStaticAbstractMembers, iface, member))));
                hasError = true;
            }

            foreach (var constraintType in constraintTypes)
            {
                CheckConstraintType(containingSymbol, in args, typeParameter, typeArgument, diagnosticsBuilder, nullabilityDiagnosticsBuilderOpt, ref useSiteInfo, constraintType, ref hasError);
            }
            constraintTypes.Free();

            if (AppendUseSiteDiagnostics(useSiteInfo, typeParameter, ref useSiteDiagnosticsBuilder))
            {
                hasError = true;
            }

            // Check the constructor constraint.
            if (typeParameter.HasConstructorConstraint && errorIfNotSatisfiesConstructorConstraint(containingSymbol, typeParameter, typeArgument, diagnosticsBuilder))
            {
                return false;
            }

            return !hasError;

            [MethodImpl(MethodImplOptions.NoInlining)]
            static bool errorIfNotSatisfiesConstructorConstraint(Symbol containingSymbol, TypeParameterSymbol typeParameter, TypeWithAnnotations typeArgument, ArrayBuilder<TypeParameterDiagnosticInfo> diagnosticsBuilder)
            {
                var error = SatisfiesConstructorConstraint(typeArgument.Type);

                switch (error)
                {
                    case ConstructorConstraintError.None:
                        return false;
                    case ConstructorConstraintError.NoPublicParameterlessConstructorOrAbstractType:
                        // "'{2}' must be a non-abstract type with a public parameterless constructor in order to use it as parameter '{1}' in the generic type or method '{0}'"
                        diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_NewConstraintNotSatisfied, containingSymbol.ConstructedFrom(), typeParameter, typeArgument.Type))));
                        return true;
                    case ConstructorConstraintError.HasRequiredMembers:
                        // '{2}' cannot satisfy the 'new()' constraint on parameter '{1}' in the generic type or or method '{0}' because '{2}' has required members.
                        diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_NewConstraintCannotHaveRequiredMembers, containingSymbol.ConstructedFrom(), typeParameter, typeArgument.Type))));
                        return true;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(error);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CheckNullability(
            Symbol containingSymbol,
            TypeParameterSymbol typeParameter,
            TypeWithAnnotations typeArgument,
            ArrayBuilder<TypeParameterDiagnosticInfo> nullabilityDiagnosticsBuilderOpt)
        {
            if (nullabilityDiagnosticsBuilderOpt != null)
            {
                if (typeParameter.HasNotNullConstraint && typeArgument.GetValueNullableAnnotation().IsAnnotated() && !typeArgument.Type.IsNonNullableValueType())
                {
                    nullabilityDiagnosticsBuilderOpt.Add(new TypeParameterDiagnosticInfo(typeParameter, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, containingSymbol.ConstructedFrom(), typeParameter, typeArgument))));
                }

                if (typeParameter.HasReferenceTypeConstraint &&
                    typeParameter.ReferenceTypeConstraintIsNullable == false &&
                    typeArgument.GetValueNullableAnnotation().IsAnnotated())
                {
                    nullabilityDiagnosticsBuilderOpt.Add(new TypeParameterDiagnosticInfo(typeParameter, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, containingSymbol.ConstructedFrom(), typeParameter, typeArgument))));
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CheckConstraintType(
            Symbol containingSymbol,
            in CheckConstraintsArgs args,
            TypeParameterSymbol typeParameter,
            TypeWithAnnotations typeArgument,
            ArrayBuilder<TypeParameterDiagnosticInfo> diagnosticsBuilder,
            ArrayBuilder<TypeParameterDiagnosticInfo> nullabilityDiagnosticsBuilderOpt,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
            TypeWithAnnotations constraintType,
            ref bool hasError)
        {
            if (SatisfiesConstraintType(args.Conversions.WithNullability(false), typeArgument, constraintType, ref useSiteInfo))
            {
                if (nullabilityDiagnosticsBuilderOpt != null)
                {
                    if (!SatisfiesConstraintType(args.Conversions.WithNullability(true), typeArgument, constraintType, ref useSiteInfo) ||
                        !constraintTypeAllows(constraintType, getTypeArgumentState(typeArgument)))
                    {
                        nullabilityDiagnosticsBuilderOpt.Add(new TypeParameterDiagnosticInfo(typeParameter, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.WRN_NullabilityMismatchInTypeParameterConstraint, containingSymbol.ConstructedFrom(), constraintType, typeParameter, typeArgument))));
                    }
                }
                return;
            }

            ErrorCode errorCode;
            if (typeArgument.Type.IsReferenceType)
            {
                errorCode = ErrorCode.ERR_GenericConstraintNotSatisfiedRefType;
            }
            else if (typeArgument.IsNullableType())
            {
                errorCode = constraintType.Type.IsInterfaceType() ? ErrorCode.ERR_GenericConstraintNotSatisfiedNullableInterface : ErrorCode.ERR_GenericConstraintNotSatisfiedNullableEnum;
            }
            else if (typeArgument.TypeKind == TypeKind.TypeParameter)
            {
                errorCode = ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar;
            }
            else
            {
                errorCode = ErrorCode.ERR_GenericConstraintNotSatisfiedValType;
            }

            object constraintTypeErrorArgument;
            object typeArgumentErrorArgument;

            if (constraintType.Type.Equals(typeArgument.Type, TypeCompareKind.AllIgnoreOptions))
            {
                constraintTypeErrorArgument = constraintType.Type;
                typeArgumentErrorArgument = typeArgument.Type;
            }
            else
            {
                SymbolDistinguisher distinguisher = new SymbolDistinguisher(args.CurrentCompilation, constraintType.Type, typeArgument.Type);
                constraintTypeErrorArgument = distinguisher.First;
                typeArgumentErrorArgument = distinguisher.Second;
            }

            diagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(errorCode, containingSymbol.ConstructedFrom(), constraintTypeErrorArgument, typeParameter, typeArgumentErrorArgument))));
            hasError = true;

            static NullableFlowState getTypeArgumentState(in TypeWithAnnotations typeWithAnnotations)
            {
                var type = typeWithAnnotations.Type;
                if (type is null)
                {
                    return NullableFlowState.NotNull;
                }
                if (type.IsValueType)
                {
                    return type.IsNullableTypeOrTypeParameter() ? NullableFlowState.MaybeNull : NullableFlowState.NotNull;
                }
                switch (typeWithAnnotations.NullableAnnotation)
                {
                    case NullableAnnotation.Annotated:
                        return type.IsTypeParameterDisallowingAnnotationInCSharp8() ? NullableFlowState.MaybeDefault : NullableFlowState.MaybeNull;
                    case NullableAnnotation.Oblivious:
                        return NullableFlowState.NotNull;
                }
                var typeParameter = type as TypeParameterSymbol;
                if (typeParameter is null || typeParameter.IsNotNullable == true)
                {
                    return NullableFlowState.NotNull;
                }
                NullableFlowState? result = null;
                foreach (var constraintType in typeParameter.ConstraintTypesNoUseSiteDiagnostics)
                {
                    var constraintState = getTypeArgumentState(constraintType);
                    if (result == null)
                    {
                        result = constraintState;
                    }
                    else
                    {
                        result = result.Value.Meet(constraintState);
                    }
                }
                return result ?? NullableFlowState.MaybeNull;
            }

            static bool constraintTypeAllows(in TypeWithAnnotations typeWithAnnotations, NullableFlowState state)
            {
                if (state == NullableFlowState.NotNull)
                {
                    return true;
                }
                var type = typeWithAnnotations.Type;
                if (type is null || type.IsValueType)
                {
                    return true;
                }
                switch (typeWithAnnotations.NullableAnnotation)
                {
                    case NullableAnnotation.Oblivious:
                    case NullableAnnotation.Annotated:
                        return true;
                }
                var typeParameter = type as TypeParameterSymbol;
                if (typeParameter is null || typeParameter.IsNotNullable == true)
                {
                    return false;
                }
                foreach (var constraintType in typeParameter.ConstraintTypesNoUseSiteDiagnostics)
                {
                    if (!constraintTypeAllows(constraintType, state))
                    {
                        return false;
                    }
                }
                return state == NullableFlowState.MaybeNull;
            }
        }

        private static bool AppendUseSiteDiagnostics(
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
            TypeParameterSymbol typeParameter,
            ref ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder)
        {
            if (!(useSiteInfo.AccumulatesDiagnostics && useSiteInfo.HasErrors) && useSiteInfo.AccumulatesDependencies && !useSiteInfo.Dependencies.IsNullOrEmpty())
            {
                ensureUseSiteDiagnosticsBuilder(ref useSiteDiagnosticsBuilder).Add(new TypeParameterDiagnosticInfo(typeParameter,
                                                                              useSiteInfo.Dependencies.Count == 1 ?
                                                                                  new UseSiteInfo<AssemblySymbol>(useSiteInfo.Dependencies.Single()) :
                                                                                  new UseSiteInfo<AssemblySymbol>(useSiteInfo.Dependencies.ToImmutableHashSet())));
            }

            if (!useSiteInfo.AccumulatesDiagnostics)
            {
                return false;
            }

            var useSiteDiagnostics = useSiteInfo.Diagnostics;
            if (useSiteDiagnostics.IsNullOrEmpty())
            {
                return false;
            }

            ensureUseSiteDiagnosticsBuilder(ref useSiteDiagnosticsBuilder);

            bool hasErrors = false;

            foreach (var info in useSiteDiagnostics)
            {
                if (info.Severity == DiagnosticSeverity.Error)
                {
                    hasErrors = true;
                }

                useSiteDiagnosticsBuilder.Add(new TypeParameterDiagnosticInfo(typeParameter, new UseSiteInfo<AssemblySymbol>(info)));
            }

            return hasErrors;

            static ArrayBuilder<TypeParameterDiagnosticInfo> ensureUseSiteDiagnosticsBuilder(ref ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder)
            {
                return useSiteDiagnosticsBuilder ??= new ArrayBuilder<TypeParameterDiagnosticInfo>();
            }
        }

        private static bool SatisfiesConstraintType(
            ConversionsBase conversions,
            TypeWithAnnotations typeArgument,
            TypeWithAnnotations constraintType,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            if (constraintType.Type.IsErrorType())
            {
                return false;
            }

            // Spec 4.4.4 describes the valid conversions from
            // type argument A to constraint type C:

            // "An identity conversion (6.1.1).
            // An implicit reference conversion (6.1.6). ..."

            if (conversions.HasIdentityOrImplicitReferenceConversion(typeArgument.Type, constraintType.Type, ref useSiteInfo))
            {
                return true;
            }

            // "... A boxing conversion (6.1.7), provided that type A is a non-nullable value type. ..."
            // NOTE: we extend this to allow, for example, a conversion from Nullable<T> to object.
            if (typeArgument.Type.IsValueType &&
                conversions.HasBoxingConversion(typeArgument.Type.IsNullableType() ? ((NamedTypeSymbol)typeArgument.Type).ConstructedFrom : typeArgument.Type,
                                                constraintType.Type, ref useSiteInfo))
            {
                return true;
            }

            if (typeArgument.TypeKind == TypeKind.TypeParameter)
            {
                var typeParameter = (TypeParameterSymbol)typeArgument.Type;

                // "... An implicit reference, boxing, or type parameter conversion
                // from type parameter A to C."
                if (conversions.HasImplicitTypeParameterConversion(typeParameter, constraintType.Type, ref useSiteInfo))
                {
                    return true;
                }

                // TypeBind::SatisfiesBound allows cases where one of the
                // type parameter constraints satisfies the constraint.
                foreach (var typeArgumentConstraint in typeParameter.ConstraintTypesWithDefinitionUseSiteDiagnostics(ref useSiteInfo))
                {
                    if (SatisfiesConstraintType(conversions, typeArgumentConstraint, constraintType, ref useSiteInfo))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool SelfOrBaseHasStaticAbstractMember(NamedTypeSymbol iface, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo, out Symbol memberWithoutImplementation)
        {
            Debug.Assert(iface.IsInterfaceType());

            foreach (Symbol m in iface.GetMembers())
            {
                if (m.IsStatic && m.IsImplementableInterfaceMember() && iface.FindImplementationForInterfaceMember(m) is null)
                {
                    memberWithoutImplementation = m;
                    return true;
                }
            }

            foreach (var baseInterface in iface.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Keys)
            {
                foreach (Symbol m in baseInterface.GetMembers())
                {
                    if (m.IsStatic && m.IsImplementableInterfaceMember() && iface.FindImplementationForInterfaceMember(m) is null)
                    {
                        memberWithoutImplementation = m;
                        return true;
                    }
                }

                baseInterface.OriginalDefinition.AddUseSiteInfo(ref useSiteInfo);
            }

            memberWithoutImplementation = null;
            return false;
        }

        private static bool IsReferenceType(TypeParameterSymbol typeParameter, ImmutableArray<TypeWithAnnotations> constraintTypes)
        {
            return typeParameter.HasReferenceTypeConstraint || TypeParameterSymbol.CalculateIsReferenceTypeFromConstraintTypes(constraintTypes);
        }

        private static bool IsValueType(TypeParameterSymbol typeParameter, ImmutableArray<TypeWithAnnotations> constraintTypes)
        {
            return typeParameter.HasValueTypeConstraint || TypeParameterSymbol.CalculateIsValueTypeFromConstraintTypes(constraintTypes);
        }

        private static TypeParameterDiagnosticInfo GenerateConflictingConstraintsError(TypeParameterSymbol typeParameter, TypeSymbol deducedBase, bool classConflict)
        {
            // "Type parameter '{0}' inherits conflicting constraints '{1}' and '{2}'"
            return new TypeParameterDiagnosticInfo(typeParameter, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_BaseConstraintConflict, typeParameter, deducedBase, classConflict ? "class" : "struct")));
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

        private enum ConstructorConstraintError
        {
            None,
            NoPublicParameterlessConstructorOrAbstractType,
            HasRequiredMembers,
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ConstructorConstraintError SatisfiesConstructorConstraint(TypeSymbol typeArgument)
        {
            switch (typeArgument.TypeKind)
            {
                case TypeKind.Struct:
                    return SatisfiesPublicParameterlessConstructor((NamedTypeSymbol)typeArgument, synthesizedIfMissing: true);

                case TypeKind.Enum:
                case TypeKind.Dynamic:
                    return ConstructorConstraintError.None;

                case TypeKind.Class:
                    if (typeArgument.IsAbstract)
                    {
                        return ConstructorConstraintError.NoPublicParameterlessConstructorOrAbstractType;
                    }

                    return SatisfiesPublicParameterlessConstructor((NamedTypeSymbol)typeArgument, synthesizedIfMissing: false);

                case TypeKind.TypeParameter:
                    {
                        var typeParameter = (TypeParameterSymbol)typeArgument;
                        return typeParameter.HasConstructorConstraint || typeParameter.IsValueType ? ConstructorConstraintError.None : ConstructorConstraintError.NoPublicParameterlessConstructorOrAbstractType;
                    }

                case TypeKind.Submission:
                    // submission can't be used as type argument
                    throw ExceptionUtilities.UnexpectedValue(typeArgument.TypeKind);

                default:
                    return ConstructorConstraintError.NoPublicParameterlessConstructorOrAbstractType;
            }
        }

        private static ConstructorConstraintError SatisfiesPublicParameterlessConstructor(NamedTypeSymbol type, bool synthesizedIfMissing)
        {
            Debug.Assert(type.TypeKind is TypeKind.Class or TypeKind.Struct);

            bool hasAnyRequiredMembers = type.HasAnyRequiredMembers;

            foreach (var constructor in type.InstanceConstructors)
            {
                if (constructor.ParameterCount == 0)
                {
                    if (constructor.DeclaredAccessibility != Accessibility.Public)
                    {
                        return ConstructorConstraintError.NoPublicParameterlessConstructorOrAbstractType;
                    }
                    else if (hasAnyRequiredMembers && constructor.ShouldCheckRequiredMembers())
                    {
                        return ConstructorConstraintError.HasRequiredMembers;
                    }
                    else
                    {
                        return ConstructorConstraintError.None;
                    }
                }
            }

            return (synthesizedIfMissing, hasAnyRequiredMembers) switch
            {
                (false, _) => ConstructorConstraintError.NoPublicParameterlessConstructorOrAbstractType,
                (true, true) => ConstructorConstraintError.HasRequiredMembers,
                (true, false) => ConstructorConstraintError.None,
            };
        }

        /// <summary>
        /// Returns true if type a is encompassed by type b (spec 6.4.3),
        /// and returns false otherwise.
        /// </summary>
        private static bool IsEncompassedBy(ConversionsBase conversions, TypeSymbol a, TypeSymbol b, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(IsValidEncompassedByArgument(a));
            Debug.Assert(IsValidEncompassedByArgument(b));

            // IncludeNullability should not be used when calculating EffectiveBaseType or EffectiveInterfaceSet.
            Debug.Assert(!conversions.IncludeNullability);

            return conversions.HasIdentityOrImplicitReferenceConversion(a, b, ref useSiteInfo) || conversions.HasBoxingConversion(a, b, ref useSiteInfo);
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

        public static bool RequiresChecking(NamedTypeSymbol type)
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

            Debug.Assert(!type.ConstructedFrom.Equals(type, TypeCompareKind.ConsiderEverything));
            return true;
        }

        public static bool RequiresChecking(MethodSymbol method)
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
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            Debug.Assert(deducedBase.IsErrorType() ||
                effectiveBase.IsErrorType() ||
                conversions.HasIdentityOrImplicitReferenceConversion(deducedBase, effectiveBase, ref discardedUseSiteInfo) ||
                conversions.HasBoxingConversion(deducedBase, effectiveBase, ref discardedUseSiteInfo));
        }

        internal static TypeWithAnnotations ConstraintWithMostSignificantNullability(TypeWithAnnotations type1, TypeWithAnnotations type2)
        {
            switch (type2.NullableAnnotation)
            {
                case NullableAnnotation.Annotated:
                    return type1;
                case NullableAnnotation.NotAnnotated:
                    return type2;
                case NullableAnnotation.Oblivious:
                    if (type1.NullableAnnotation.IsNotAnnotated())
                    {
                        return type1;
                    }

                    return type2;
                default:
                    throw ExceptionUtilities.UnexpectedValue(type2.NullableAnnotation);
            }
        }

        internal static bool IsObjectConstraint(TypeWithAnnotations type, ref TypeWithAnnotations bestObjectConstraint)
        {
            if (type.SpecialType == SpecialType.System_Object)
            {
                switch (type.NullableAnnotation)
                {
                    case NullableAnnotation.Annotated:
                        break;
                    default:
                        if (!bestObjectConstraint.HasType)
                        {
                            bestObjectConstraint = type;
                        }
                        else
                        {
                            bestObjectConstraint = ConstraintWithMostSignificantNullability(bestObjectConstraint, type);
                        }
                        break;
                }

                return true;
            }

            return false;
        }

        internal static bool IsObjectConstraintSignificant(bool? isNotNullable, TypeWithAnnotations objectConstraint)
        {
            switch (isNotNullable)
            {
                case true:
                    return false;
                case null:
                    if (objectConstraint.NullableAnnotation.IsOblivious())
                    {
                        return false;
                    }

                    break;
            }

            return true;
        }
    }
}
