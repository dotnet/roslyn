// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// <para>
    /// C# 4.0 §10.6: The name, the type parameter list and the formal parameter list of a method define
    /// the signature (§3.6) of the method. Specifically, the signature of a method consists of its
    /// name, the number of type parameters and the number, modifiers, and types of its formal
    /// parameters. For these purposes, any type parameter of the method that occurs in the type of
    /// a formal parameter is identified not by its name, but by its ordinal position in the type
    /// argument list of the method. The return type is not part of a method's signature, nor are
    /// the names of the type parameters or the formal parameters.
    /// </para>
    /// <para>
    /// C# 4.0 §3.6: For the purposes of signatures, the types object and dynamic are considered the
    /// same. 
    /// </para>
    /// <para>
    /// C# 4.0 §3.6: We implement the rules for ref/out by mapping both to ref. The caller (i.e.
    /// checking for proper overrides or partial methods, etc) should check that ref/out are
    /// consistent.
    /// </para>
    /// </summary>
    internal class MemberSignatureComparer : IEqualityComparer<Symbol>
    {
        /// <summary>
        /// This instance is used when trying to determine if one member explicitly implements another,
        /// according the C# definition.
        /// The member signatures are compared without regard to name (including the interface part, if any)
        /// and the return types must match.
        /// </summary>
        public static readonly MemberSignatureComparer ExplicitImplementationComparer = new MemberSignatureComparer(
            considerName: false,
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: true,
            considerTypeConstraints: false,
            considerRefOutDifference: true,
            considerCallingConvention: true,
            considerCustomModifiers: false);

        /// <summary>
        /// This instance is used when trying to determine if one member implicitly implements another,
        /// according to the C# definition.
        /// The member names, parameters, and (return) types must match. Custom modifiers are ignored.
        /// </summary>
        /// <remarks>
        /// One would expect this comparer to have requireSourceMethod = true, but it doesn't because (for source types)
        /// we allow inexact matching of custom modifiers when computing implicit member implementations. Consider the
        /// following scenario: interface I has a method M with custom modifiers C1, source type ST includes I in its
        /// interface list but has no method M, and metadata type MT has a method M with custom modifiers C2.
        /// In this scenario, we want to compare I.M to MT.M without regard to custom modifiers, because if C1 != C2,
        /// we can just synthesize an explicit implementation of I.M in ST that calls MT.M.
        /// </remarks>
        public static readonly MemberSignatureComparer CSharpImplicitImplementationComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: true,
            considerReturnType: true,
            considerTypeConstraints: false, // constraints are checked by caller instead
            considerCallingConvention: true,
            considerRefOutDifference: true,
            considerCustomModifiers: false);

        /// <summary>
        /// This instance is used as a fallback when it is determined that one member does not implicitly implement
        /// another. It applies a looser check to determine whether the proposed implementation should be reported
        /// as "close".
        /// </summary>
        public static readonly MemberSignatureComparer CSharpCloseImplicitImplementationComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: true,
            considerReturnType: false,
            considerTypeConstraints: false,
            considerCallingConvention: false,
            considerRefOutDifference: true,
            considerCustomModifiers: false); //shouldn't actually matter for source members

        /// <summary>
        /// This instance is used to determine if two C# member declarations in source conflict with each other.
        /// Names, arities, and parameter types are considered.
        /// Return types, type parameter constraints, custom modifiers, and parameter ref kinds, etc are ignored.
        /// </summary>
        /// <remarks>
        /// This does the same comparison that MethodSignature used to do.
        /// </remarks>
        public static readonly MemberSignatureComparer DuplicateSourceComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: true,
            considerReturnType: false,
            considerTypeConstraints: false,
            considerCallingConvention: false,
            considerRefOutDifference: false,
            considerCustomModifiers: false); //shouldn't actually matter for source members

        /// <summary>
        /// This instance is used to check whether one member overrides another, according to the C# definition.
        /// </summary>
        public static readonly MemberSignatureComparer CSharpOverrideComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: false,
            considerTypeConstraints: false,
            considerCallingConvention: false, //ignore static-ness
            considerRefOutDifference: true,
            considerCustomModifiers: false);

        /// <summary>
        /// This instance is used to check whether one property or event overrides another, according to the C# definition.
        /// <para>NOTE: C# ignores accessor member names.</para>
        /// <para>CAVEAT: considers return types so that getters and setters will be treated the same.</para>
        /// </summary>
        public static readonly MemberSignatureComparer CSharpAccessorOverrideComparer = new MemberSignatureComparer(
            considerName: false,
            considerExplicitlyImplementedInterfaces: false, //Bug: DevDiv #15775
            considerReturnType: true,
            considerTypeConstraints: false,
            considerCallingConvention: false, //ignore static-ness
            considerRefOutDifference: true,
            considerCustomModifiers: false);

        /// <summary>
        /// Same as <see cref="CSharpOverrideComparer"/> except that it pays attention to custom modifiers and return type.  
        /// Normally, the return type isn't considered during overriding, but this comparer is actually used to find
        /// exact matches (i.e. before tie-breaking takes place amongst close matches).
        /// </summary>
        public static readonly MemberSignatureComparer CSharpCustomModifierOverrideComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: true,
            considerTypeConstraints: false,
            considerCallingConvention: false, //ignore static-ness
            considerRefOutDifference: true,
            considerCustomModifiers: true);

        /// <summary>
        /// If this returns false, then the real override comparer (whichever one is appropriate for the scenario)
        /// will also return false.
        /// </summary>
        internal static readonly MemberSignatureComparer SloppyOverrideComparer = new MemberSignatureComparer(
            considerName: false,
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: false,
            considerTypeConstraints: false,
            considerCallingConvention: false, //ignore static-ness
            considerRefOutDifference: false,
            considerCustomModifiers: false);

        /// <summary>
        /// This instance is intended to reflect the definition of signature equality used by the runtime 
        /// (<a href="http://www.ecma-international.org/publications/files/ECMA-ST/ECMA-335.pdf">ECMA-335</a>, Partition I, §8.6.1.6 Signature Matching).
        /// It considers return type, name, parameters, calling convention, and custom modifiers, but ignores
        /// the difference between <see cref="RefKind.Out"/> and <see cref="RefKind.Ref"/>.
        /// </summary>
        public static readonly MemberSignatureComparer RuntimeSignatureComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: true,
            considerTypeConstraints: false,
            considerCallingConvention: true,
            considerRefOutDifference: false,
            considerCustomModifiers: true);

        /// <summary>
        /// Same as <see cref="RuntimeSignatureComparer"/>, but distinguishes between <c>ref</c> and <c>out</c>. During override resolution,
        /// if we find two methods that match except for <c>ref</c>/<c>out</c>, we want to prefer the one that matches, even
        /// if the runtime doesn't.
        /// </summary>
        public static readonly MemberSignatureComparer RuntimePlusRefOutSignatureComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: true,
            considerTypeConstraints: false,
            considerCallingConvention: true,
            considerRefOutDifference: true,
            considerCustomModifiers: true);

        /// <summary>
        /// This instance is the same as RuntimeSignatureComparer.
        /// CONSIDER: just use RuntimeSignatureComparer?
        /// </summary>
        public static readonly MemberSignatureComparer RuntimeImplicitImplementationComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: true,
            considerReturnType: true,
            considerTypeConstraints: false, // constraints are checked by caller instead
            considerCallingConvention: true,
            considerRefOutDifference: false,
            considerCustomModifiers: true);

        // NOTE: Not used anywhere. Do we still need to keep it?
        /// <summary>
        /// This instance is used to search for members that have the same name, parameters, (return) type, and constraints (if any)
        /// according to the C# definition. Custom modifiers are ignored.
        /// </summary>
        public static readonly MemberSignatureComparer CSharpSignatureAndConstraintsAndReturnTypeComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: true,
            considerReturnType: true,
            considerTypeConstraints: true,
            considerCallingConvention: true,
            considerRefOutDifference: true,
            considerCustomModifiers: false); //intended for source types

        /// <summary>
        /// This instance is used to search for members that have identical signatures in every regard.
        /// </summary>
        public static readonly MemberSignatureComparer RetargetedExplicitImplementationComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: false, //we'll be comparing interface members anyway
            considerReturnType: true,
            considerTypeConstraints: false,
            considerCallingConvention: true,
            considerRefOutDifference: true,
            considerCustomModifiers: true); //if it was a true explicit impl, we expect it to remain so after retargeting

        /// <summary>
        /// This instance is used for performing approximate overload resolution of documentation
        /// comment <c>cref</c> attributes. It ignores the name, because the candidates were all found by lookup.
        /// </summary>
        public static readonly MemberSignatureComparer CrefComparer = new MemberSignatureComparer(
            considerName: false, //handled by lookup
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: false,
            considerTypeConstraints: false,
            considerCallingConvention: false, //ignore static-ness
            considerRefOutDifference: true,
            considerCustomModifiers: false);

        /// <summary>
        /// This instance is used as a key in the lambda return type inference.
        /// We basically only interested in parameters since inference will set the return type to null.
        /// </summary>
        public static readonly MemberSignatureComparer LambdaReturnInferenceCacheComparer = new MemberSignatureComparer(
            considerName: false,                // valid invoke is always called "Invoke"
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: false,          // do not care
            considerTypeConstraints: false,     // valid invoke is never generic
            considerCallingConvention: false,   // valid invoke is never static
            considerRefOutDifference: true,
            considerCustomModifiers: true,
            ignoreDynamic: false);

        // Compare the "unqualified" part of the member name (no explicit part)
        private readonly bool _considerName;

        // Compare the interfaces implemented (as symbols, to avoid ambiguous representations)
        private readonly bool _considerExplicitlyImplementedInterfaces;

        // Compare the type symbols of the return types
        private readonly bool _considerReturnType;

        // Compare the type constraints
        private readonly bool _considerTypeConstraints;

        // Compare the full calling conventions.  Still compares varargs if false.
        private readonly bool _considerCallingConvention;

        // True to consider RefKind.Ref and RefKind.Out different, false to consider them the same.
        private readonly bool _considerRefOutDifference;

        // Consider custom modifiers on/in parameters and return types (if return is considered).
        private readonly bool _considerCustomModifiers;

        // Ignore Object vs. Dynamic difference 
        private readonly bool _ignoreDynamic;

        private MemberSignatureComparer(
            bool considerName,
            bool considerExplicitlyImplementedInterfaces,
            bool considerReturnType,
            bool considerTypeConstraints,
            bool considerCallingConvention,
            bool considerRefOutDifference,
            bool considerCustomModifiers,
            bool ignoreDynamic = true)
        {
            Debug.Assert(!considerExplicitlyImplementedInterfaces || considerName, "Doesn't make sense to consider interfaces separately from name.");

            _considerName = considerName;
            _considerExplicitlyImplementedInterfaces = considerExplicitlyImplementedInterfaces;
            _considerReturnType = considerReturnType;
            _considerTypeConstraints = considerTypeConstraints;
            _considerCallingConvention = considerCallingConvention;
            _considerRefOutDifference = considerRefOutDifference;
            _considerCustomModifiers = considerCustomModifiers;
            _ignoreDynamic = ignoreDynamic;
        }

        #region IEqualityComparer<Symbol> Members

        public bool Equals(Symbol member1, Symbol member2)
        {
            if (ReferenceEquals(member1, member2))
            {
                return true;
            }

            if ((object)member1 == null || (object)member2 == null || member1.Kind != member2.Kind)
            {
                return false;
            }

            bool sawInterfaceInName1 = false;
            bool sawInterfaceInName2 = false;

            if (_considerName)
            {
                string name1 = ExplicitInterfaceHelpers.GetMemberNameWithoutInterfaceName(member1.Name);
                string name2 = ExplicitInterfaceHelpers.GetMemberNameWithoutInterfaceName(member2.Name);

                sawInterfaceInName1 = name1 != member1.Name;
                sawInterfaceInName2 = name2 != member2.Name;

                if (name1 != name2)
                {
                    return false;
                }
            }

            // NB: up to, and including, this check, we have not actually forced the (type) parameters
            // to be expanded - we're only using the counts.
            if ((member1.GetMemberArity() != member2.GetMemberArity()) ||
                (member1.GetParameterCount() != member2.GetParameterCount()))
            {
                return false;
            }

            var typeMap1 = GetTypeMap(member1);
            var typeMap2 = GetTypeMap(member2);

            if (_considerReturnType && !HaveSameReturnTypes(member1, typeMap1, member2, typeMap2, _considerCustomModifiers, _ignoreDynamic))
            {
                return false;
            }

            if (member1.GetParameterCount() > 0 && !HaveSameParameterTypes(member1.GetParameters(), typeMap1, member2.GetParameters(), typeMap2,
                                                                           _considerRefOutDifference, _considerCustomModifiers, _ignoreDynamic))
            {
                return false;
            }

            if (_considerCallingConvention)
            {
                if (GetCallingConvention(member1) != GetCallingConvention(member2))
                {
                    return false;
                }
            }
            else
            {
                if (IsVarargMethod(member1) != IsVarargMethod(member2))
                {
                    return false;
                }
            }

            if (_considerExplicitlyImplementedInterfaces)
            {
                if (sawInterfaceInName1 != sawInterfaceInName2)
                {
                    return false;
                }

                // The purpose of this check is to determine whether the interface parts of the member names agree,
                // but to do so using robust symbolic checks, rather than syntactic ones.  Therefore, if neither member
                // name contains an interface name, this check is not relevant.  
                // Phrased differently, the explicitly implemented interface is not part of the signature unless it's
                // part of the name.
                if (sawInterfaceInName1)
                {
                    Debug.Assert(sawInterfaceInName2);

                    // May avoid realizing interface members.
                    if (member1.IsExplicitInterfaceImplementation() != member2.IsExplicitInterfaceImplementation())
                    {
                        return false;
                    }

                    // By comparing symbols, rather than syntax, we gain the flexibility of ignoring whitespace
                    // and gracefully accepting multiple names for the same (or equivalent) types (e.g. "I<int>.M"
                    // vs "I<System.Int32>.M"), but we lose the connection with the name.  For example, in metadata,
                    // a method name "I.M" could have nothing to do with "I" but explicitly implement interface "I2".
                    // We will behave as if the method was really named "I2.M".  Furthermore, in metadata, a method
                    // can explicitly implement more than one interface method, in which case it doesn't really
                    // make sense to pretend that all of them are part of the signature.

                    var explicitInterfaceImplementations1 = member1.GetExplicitInterfaceImplementations();
                    var explicitInterfaceImplementations2 = member2.GetExplicitInterfaceImplementations();

                    if (!explicitInterfaceImplementations1.SetEquals(explicitInterfaceImplementations2, EqualityComparer<Symbol>.Default))
                    {
                        return false;
                    }
                }
            }

            return !_considerTypeConstraints || HaveSameConstraints(member1, typeMap1, member2, typeMap2);
        }

        public int GetHashCode(Symbol member)
        {
            int hash = 1;
            if ((object)member != null)
            {
                hash = Hash.Combine(hash, (int)member.Kind);

                if (_considerName)
                {
                    hash = Hash.Combine(ExplicitInterfaceHelpers.GetMemberNameWithoutInterfaceName(member.Name), hash);
                    // CONSIDER: could use interface type, but that might be quite expensive
                }

                if (_considerReturnType && member.GetMemberArity() == 0 && !_considerCustomModifiers) // If it is generic, then type argument might be in return type.
                {
                    hash = Hash.Combine(member.GetTypeOrReturnType(), hash);
                }

                // CONSIDER: modify hash for constraints?

                hash = Hash.Combine(hash, member.GetMemberArity());
                hash = Hash.Combine(hash, member.GetParameterCount());
            }
            return hash;
        }

        #endregion

        public static bool HaveSameReturnTypes(MethodSymbol member1, MethodSymbol member2, bool considerCustomModifiers)
        {
            return HaveSameReturnTypes(member1, GetTypeMap(member1), member2, GetTypeMap(member2), considerCustomModifiers, ignoreDynamic: true);
        }

        private static bool HaveSameReturnTypes(Symbol member1, TypeMap typeMap1, Symbol member2, TypeMap typeMap2, bool considerCustomModifiers, bool ignoreDynamic)
        {
            TypeSymbolWithAnnotations unsubstitutedReturnType1 = member1.GetTypeOrReturnType();
            TypeSymbolWithAnnotations unsubstitutedReturnType2 = member2.GetTypeOrReturnType();

            // short-circuit type map building in the easiest cases
            var isVoid1 = unsubstitutedReturnType1.SpecialType == SpecialType.System_Void;
            var isVoid2 = unsubstitutedReturnType2.SpecialType == SpecialType.System_Void;

            if (isVoid1 != isVoid2)
            {
                return false;
            }

            if (isVoid1)
            {
                if (!considerCustomModifiers || 
                    (unsubstitutedReturnType1.CustomModifiers.IsEmpty && unsubstitutedReturnType2.CustomModifiers.IsEmpty))
                {
                    return true;
                }
            }

            var returnType1 = SubstituteType(typeMap1, unsubstitutedReturnType1);
            var returnType2 = SubstituteType(typeMap2, unsubstitutedReturnType2);

            // the runtime compares custom modifiers using (effectively) SequenceEqual
            return considerCustomModifiers ?
                returnType1.TypeSymbol.Equals(returnType2.TypeSymbol, ignoreDynamic: ignoreDynamic) && returnType1.CustomModifiers.SequenceEqual(returnType2.CustomModifiers) :
                returnType1.TypeSymbol.Equals(returnType2.TypeSymbol, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: ignoreDynamic);
        }

        private static TypeMap GetTypeMap(Symbol member)
        {
            var typeParameters = member.GetMemberTypeParameters();
            return typeParameters.IsEmpty ?
                null :
                new TypeMap(
                    typeParameters,
                    IndexedTypeParameterSymbol.Take(member.GetMemberArity()),
                    true);
        }

        private static bool HaveSameConstraints(Symbol member1, TypeMap typeMap1, Symbol member2, TypeMap typeMap2)
        {
            Debug.Assert(member1.GetMemberArity() == member2.GetMemberArity());

            int arity = member1.GetMemberArity();
            if (arity == 0)
            {
                return true;
            }

            var typeParameters1 = member1.GetMemberTypeParameters();
            var typeParameters2 = member2.GetMemberTypeParameters();
            return HaveSameConstraints(typeParameters1, typeMap1, typeParameters2, typeMap2);
        }

        public static bool HaveSameConstraints(ImmutableArray<TypeParameterSymbol> typeParameters1, TypeMap typeMap1, ImmutableArray<TypeParameterSymbol> typeParameters2, TypeMap typeMap2)
        {
            Debug.Assert(typeParameters1.Length == typeParameters2.Length);

            int arity = typeParameters1.Length;
            for (int i = 0; i < arity; i++)
            {
                if (!HaveSameConstraints(typeParameters1[i], typeMap1, typeParameters2[i], typeMap2))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool HaveSameConstraints(TypeParameterSymbol typeParameter1, TypeMap typeMap1, TypeParameterSymbol typeParameter2, TypeMap typeMap2)
        {
            // Spec 13.4.3: Implementation of generic methods.

            if ((typeParameter1.HasConstructorConstraint != typeParameter2.HasConstructorConstraint) ||
                (typeParameter1.HasReferenceTypeConstraint != typeParameter2.HasReferenceTypeConstraint) ||
                (typeParameter1.HasValueTypeConstraint != typeParameter2.HasValueTypeConstraint) ||
                (typeParameter1.Variance != typeParameter2.Variance))
            {
                return false;
            }

            // Check that constraintTypes1 is a subset of constraintTypes2 and
            // also that constraintTypes2 is a subset of constraintTypes1
            // (see SymbolPreparer::CheckImplicitImplConstraints).

            var constraintTypes1 = typeParameter1.ConstraintTypesNoUseSiteDiagnostics;
            var constraintTypes2 = typeParameter2.ConstraintTypesNoUseSiteDiagnostics;

            // The two sets of constraints may differ in size but still be considered
            // the same (duplicated constraints, ignored "object" constraints), but
            // if both are zero size, the sets must be equal.
            if ((constraintTypes1.Length == 0) && (constraintTypes2.Length == 0))
            {
                return true;
            }

            var substitutedTypes1 = new HashSet<TypeSymbol>(TypeSymbol.EqualsIgnoringDynamicComparer);
            var substitutedTypes2 = new HashSet<TypeSymbol>(TypeSymbol.EqualsIgnoringDynamicComparer);

            SubstituteConstraintTypes(constraintTypes1, typeMap1, substitutedTypes1);
            SubstituteConstraintTypes(constraintTypes2, typeMap2, substitutedTypes2);

            return AreConstraintTypesSubset(substitutedTypes1, substitutedTypes2, typeParameter2) &&
                AreConstraintTypesSubset(substitutedTypes2, substitutedTypes1, typeParameter1);
        }

        /// <summary>
        /// Returns true if the first set of constraint types
        /// is a subset of the second set.
        /// </summary>
        private static bool AreConstraintTypesSubset(HashSet<TypeSymbol> constraintTypes1, HashSet<TypeSymbol> constraintTypes2, TypeParameterSymbol typeParameter2)
        {
            foreach (var constraintType in constraintTypes1)
            {
                // Skip object type (spec. 13.4.3).
                if (constraintType.SpecialType == SpecialType.System_Object)
                {
                    continue;
                }

                if (constraintTypes2.Contains(constraintType))
                {
                    continue;
                }

                // The struct constraint implies a System.ValueType constraint
                // type which may be explicit in the other type parameter
                // constraints (through type substitution in derived types).
                if ((constraintType.SpecialType == SpecialType.System_ValueType) &&
                    typeParameter2.HasValueTypeConstraint)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static void SubstituteConstraintTypes(ImmutableArray<TypeSymbolWithAnnotations> types, TypeMap typeMap, HashSet<TypeSymbol> result)
        {
            foreach (var type in types)
            {
                result.Add(typeMap.SubstituteType(type).TypeSymbol);
            }
        }

        private static bool HaveSameParameterTypes(ImmutableArray<ParameterSymbol> params1, TypeMap typeMap1, ImmutableArray<ParameterSymbol> params2, TypeMap typeMap2,
                                                   bool considerRefOutDifference, bool considerCustomModifiers, bool ignoreDynamic)
        {
            Debug.Assert(params1.Length == params2.Length);

            var numParams = params1.Length;

            for (int i = 0; i < numParams; i++)
            {
                var param1 = params1[i];
                var param2 = params2[i];

                var type1 = SubstituteType(typeMap1, param1.Type);
                var type2 = SubstituteType(typeMap2, param2.Type);

                // the runtime compares custom modifiers using (effectively) SequenceEqual
                if (considerCustomModifiers)
                {
                    if (!type1.TypeSymbol.Equals(type2.TypeSymbol, ignoreDynamic: ignoreDynamic) ||
                        !type1.CustomModifiers.SequenceEqual(type2.CustomModifiers) || 
                        (param1.CountOfCustomModifiersPrecedingByRef != param2.CountOfCustomModifiersPrecedingByRef))
                    {
                        return false;
                    }
                }
                else if (!type1.TypeSymbol.Equals(type2.TypeSymbol, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: ignoreDynamic))
                {
                    return false;
                }

                var refKind1 = param1.RefKind;
                var refKind2 = param2.RefKind;

                // Metadata signatures don't distinguish ref/out, but C# does - even when comparing metadata method signatures.
                if (considerRefOutDifference)
                {
                    if (refKind1 != refKind2)
                    {
                        return false;
                    }
                }
                else
                {
                    if ((refKind1 == RefKind.None) != (refKind2 == RefKind.None))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static TypeSymbolWithAnnotations SubstituteType(TypeMap typeMap, TypeSymbolWithAnnotations typeSymbol)
        {
            return typeMap == null ? typeSymbol : typeSymbol.SubstituteType(typeMap);
        }

        private static Cci.CallingConvention GetCallingConvention(Symbol member)
        {
            switch (member.Kind)
            {
                case SymbolKind.Method:
                    return ((MethodSymbol)member).CallingConvention;
                case SymbolKind.Property: //NOTE: Not using PropertySymbol.CallingConvention
                case SymbolKind.Event:
                    return member.IsStatic ? 0 : Cci.CallingConvention.HasThis;
                default:
                    throw ExceptionUtilities.UnexpectedValue(member.Kind);
            }
        }

        private static bool IsVarargMethod(Symbol member)
        {
            return member.Kind == SymbolKind.Method && ((MethodSymbol)member).IsVararg;
        }
    }
}
