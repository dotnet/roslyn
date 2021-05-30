﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// If a virtual "clone" method is present in the base record, the synthesized "clone" method overrides it
    /// and the return type of the method is the current containing type if the "covariant returns" feature is
    /// supported and the override return type otherwise. An error is produced if the base record clone method
    /// is sealed. If a virtual "clone" method is not present in the base record, the return type of the clone
    /// method is the containing type and the method is virtual, unless the record is sealed or abstract.
    /// If the containing record is abstract, the synthesized clone method is also abstract.
    /// If the "clone" method is not abstract, it returns the result of a call to a copy constructor.
    /// </summary>
    internal sealed class SynthesizedRecordClone : SynthesizedRecordOrdinaryMethod
    {
        public SynthesizedRecordClone(
            SourceMemberContainerTypeSymbol containingType,
            int memberOffset,
            BindingDiagnosticBag diagnostics)
            : base(containingType, WellKnownMemberNames.CloneMethodName, hasBody: !containingType.IsAbstract, memberOffset, diagnostics)
        {
        }

        protected override DeclarationModifiers MakeDeclarationModifiers(DeclarationModifiers allowedModifiers, BindingDiagnosticBag diagnostics)
        {
            DeclarationModifiers result = DeclarationModifiers.Public;

            if (VirtualCloneInBase() is object)
            {
                result |= DeclarationModifiers.Override;
            }
            else
            {
                result |= ContainingType.IsSealed ? DeclarationModifiers.None : DeclarationModifiers.Virtual;
            }

            if (ContainingType.IsAbstract)
            {
                result &= ~DeclarationModifiers.Virtual;
                result |= DeclarationModifiers.Abstract;
            }

            Debug.Assert((result & ~allowedModifiers) == 0);
#if DEBUG
            Debug.Assert(modifiersAreValid(result));
#endif 
            return result;

#if DEBUG
            static bool modifiersAreValid(DeclarationModifiers modifiers)
            {
                if ((modifiers & DeclarationModifiers.AccessibilityMask) != DeclarationModifiers.Public)
                {
                    return false;
                }

                modifiers &= ~DeclarationModifiers.AccessibilityMask;

                switch (modifiers)
                {
                    case DeclarationModifiers.None:
                        return true;
                    case DeclarationModifiers.Abstract:
                        return true;
                    case DeclarationModifiers.Override:
                        return true;
                    case DeclarationModifiers.Abstract | DeclarationModifiers.Override:
                        return true;
                    case DeclarationModifiers.Virtual:
                        return true;
                    default:
                        return false;
                }
            }
#endif 
        }

        private MethodSymbol? VirtualCloneInBase()
        {
            NamedTypeSymbol baseType = ContainingType.BaseTypeNoUseSiteDiagnostics;

            if (!baseType.IsObjectType())
            {
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded; // This is reported when we bind bases
                return FindValidCloneMethod(baseType, ref discardedUseSiteInfo);
            }

            return null;
        }

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters, bool IsVararg, ImmutableArray<TypeParameterConstraintClause> DeclaredConstraintsForOverrideOrImplementation) MakeParametersAndBindReturnType(BindingDiagnosticBag diagnostics)
        {
            return (ReturnType: VirtualCloneInBase() is { } baseClone ?
                                     baseClone.ReturnTypeWithAnnotations : // Use covariant returns when available
                                     TypeWithAnnotations.Create(isNullableEnabled: true, ContainingType),
                    Parameters: ImmutableArray<ParameterSymbol>.Empty,
                    IsVararg: false,
                    DeclaredConstraintsForOverrideOrImplementation: ImmutableArray<TypeParameterConstraintClause>.Empty);
        }

        protected override int GetParameterCountFromSyntax() => 0;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(!IsAbstract);

            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);

            try
            {
                if (ReturnType.IsErrorType())
                {
                    F.CloseMethod(F.ThrowNull());
                    return;
                }

                var members = ContainingType.InstanceConstructors;
                foreach (var member in members)
                {
                    var ctor = (MethodSymbol)member;
                    if (ctor.ParameterCount == 1 && ctor.Parameters[0].RefKind == RefKind.None &&
                        ctor.Parameters[0].Type.Equals(ContainingType, TypeCompareKind.AllIgnoreOptions))
                    {
                        F.CloseMethod(F.Return(F.New(ctor, F.This())));
                        return;
                    }
                }

                throw ExceptionUtilities.Unreachable;
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }
        }

        internal static MethodSymbol? FindValidCloneMethod(TypeSymbol containingType, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            if (containingType.IsObjectType() || containingType is not NamedTypeSymbol containingNamedType)
            {
                return null;
            }

            // If this symbol is from metadata, getting all members can cause us to realize a lot of structures that we otherwise
            // don't have to. Optimize for the common case here of there not being a method named <Clone>$. If there is a method
            // with that name, it's most likely the one we're interested in, and we can't get around loading everything to find it.
            if (!containingNamedType.HasPossibleWellKnownCloneMethod())
            {
                return null;
            }

            MethodSymbol? candidate = null;

            foreach (var member in containingType.GetMembers(WellKnownMemberNames.CloneMethodName))
            {
                if (member is MethodSymbol
                    {
                        DeclaredAccessibility: Accessibility.Public,
                        IsStatic: false,
                        ParameterCount: 0,
                        Arity: 0
                    } method)
                {
                    if (candidate is object)
                    {
                        // An ambiguity case, can come from metadata, treat as an error for simplicity.
                        return null;
                    }

                    candidate = method;
                }
            }

            if (candidate is null ||
                !(containingType.IsSealed || candidate.IsOverride || candidate.IsVirtual || candidate.IsAbstract) ||
                !containingType.IsEqualToOrDerivedFrom(
                    candidate.ReturnType,
                    TypeCompareKind.AllIgnoreOptions,
                    ref useSiteInfo))
            {
                return null;
            }

            return candidate;
        }
    }
}
