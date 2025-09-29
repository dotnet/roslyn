// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    internal sealed class MemberSignatureComparer : IEqualityComparer<Symbol>
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
            refKindCompareMode: RefKindCompareMode.ConsiderDifferences | RefKindCompareMode.AllowRefReadonlyVsInMismatch,
            considerCallingConvention: true,
            typeComparison: TypeCompareKind.AllIgnoreOptions);

        /// <summary>
        /// If this returns false, then the real explicit implementation comparer will also return false.
        /// Skips checking whether the return type is equal.
        /// </summary>
        public static readonly MemberSignatureComparer ExplicitImplementationWithoutReturnTypeComparer = new MemberSignatureComparer(
            considerName: false,
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: false,
            refKindCompareMode: RefKindCompareMode.ConsiderDifferences | RefKindCompareMode.AllowRefReadonlyVsInMismatch,
            considerCallingConvention: true,
            typeComparison: TypeCompareKind.AllIgnoreOptions);

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
            considerCallingConvention: true,
            refKindCompareMode: RefKindCompareMode.ConsiderDifferences | RefKindCompareMode.AllowRefReadonlyVsInMismatch,
            typeComparison: TypeCompareKind.AllIgnoreOptions);

        /// <summary>
        /// This instance is used as a fallback when it is determined that one member does not implicitly implement
        /// another. It applies a looser check to determine whether the proposed implementation should be reported
        /// as "close".
        /// </summary>
        public static readonly MemberSignatureComparer CSharpCloseImplicitImplementationComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: true,
            considerReturnType: false,
            considerCallingConvention: false,
            refKindCompareMode: RefKindCompareMode.ConsiderDifferences | RefKindCompareMode.AllowRefReadonlyVsInMismatch,
            typeComparison: TypeCompareKind.AllIgnoreOptions); //shouldn't actually matter for source members

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
            considerCallingConvention: false,
            refKindCompareMode: RefKindCompareMode.RefOutInRefReadonlyMatch,
            typeComparison: TypeCompareKind.AllIgnoreOptions);

        /// <summary>
        /// This instance is used to determine if some API specific to records is explicitly declared.
        /// It is the same as <see cref="DuplicateSourceComparer"/> except it considers ref kinds as well.
        /// </summary>
        public static readonly MemberSignatureComparer RecordAPISignatureComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: true,
            considerReturnType: false,
            considerCallingConvention: false,
            refKindCompareMode: RefKindCompareMode.ConsiderDifferences,
            typeComparison: TypeCompareKind.AllIgnoreOptions);

        /// <summary>
        /// This instance is used to determine if a partial method implementation matches the definition.
        /// It is the same as <see cref="DuplicateSourceComparer"/> except it considers ref kinds as well.
        /// </summary>
        public static readonly MemberSignatureComparer PartialMethodsComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: true,
            considerReturnType: false,
            considerCallingConvention: false,
            refKindCompareMode: RefKindCompareMode.ConsiderDifferences,
            typeComparison: TypeCompareKind.AllIgnoreOptions);

        /// <summary>
        /// This instance is used to determine if a partial method implementation matches the definition,
        /// including differences ignored by the runtime.
        /// </summary>
        public static readonly MemberSignatureComparer PartialMethodsStrictComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: true,
            considerReturnType: true,
            considerCallingConvention: false,
            refKindCompareMode: RefKindCompareMode.ConsiderDifferences,
            typeComparison: TypeCompareKind.ObliviousNullableModifierMatchesAny);

        /// <summary>
        /// Determines if an interceptor has a compatible signature with an interceptable method.
        /// NB: when a classic extension method is intercepting an instance method call, a normalization to 'ReducedExtensionMethodSymbol' must be performed first.
        /// </summary>
        public static readonly MemberSignatureComparer InterceptorsComparer = new MemberSignatureComparer(
            considerName: false,
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: true,
            considerCallingConvention: false,
            refKindCompareMode: RefKindCompareMode.ConsiderDifferences,
            considerArity: false,
            typeComparison: TypeCompareKind.AllIgnoreOptions);

        /// <summary>
        /// Determines if an interceptor has a compatible signature with an interceptable method.
        /// If methods are considered equal by <see cref="InterceptorsComparer"/>, but not equal by this comparer, a warning is reported.
        /// NB: when a classic extension method is intercepting an instance method call, a normalization to 'ReducedExtensionMethodSymbol' must be performed first.
        /// </summary>
        public static readonly MemberSignatureComparer InterceptorsStrictComparer = new MemberSignatureComparer(
            considerName: false,
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: true,
            considerCallingConvention: false,
            refKindCompareMode: RefKindCompareMode.ConsiderDifferences,
            considerArity: false,
            typeComparison: TypeCompareKind.AllNullableIgnoreOptions);

        /// <summary>
        /// This instance is used to check whether one member overrides another, according to the C# definition.
        /// </summary>
        public static readonly MemberSignatureComparer CSharpOverrideComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: false,
            considerCallingConvention: false, //ignore static-ness
            refKindCompareMode: RefKindCompareMode.ConsiderDifferences | RefKindCompareMode.AllowRefReadonlyVsInMismatch,
            typeComparison: TypeCompareKind.AllIgnoreOptions);

        /// <summary>
        /// This instance checks whether two signatures match including tuples names, in both return type and parameters.
        /// It is used to detect tuple-name-only differences.
        /// </summary>
        private static readonly MemberSignatureComparer CSharpWithTupleNamesComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: true,
            considerCallingConvention: false, //ignore static-ness
            refKindCompareMode: RefKindCompareMode.RefOutInRefReadonlyMatch,
            typeComparison: TypeCompareKind.AllIgnoreOptions & ~TypeCompareKind.IgnoreTupleNames);

        /// <summary>
        /// This instance checks whether two signatures match excluding tuples names, in both return type and parameters.
        /// It is used to detect tuple-name-only differences.
        /// </summary>
        private static readonly MemberSignatureComparer CSharpWithoutTupleNamesComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: true,
            considerCallingConvention: false, //ignore static-ness
            refKindCompareMode: RefKindCompareMode.RefOutInRefReadonlyMatch,
            typeComparison: TypeCompareKind.AllIgnoreOptions);

        /// <summary>
        /// This instance is used to check whether one property or event overrides another, according to the C# definition.
        /// <para>NOTE: C# ignores accessor member names.</para>
        /// </summary>
        public static readonly MemberSignatureComparer CSharpAccessorOverrideComparer = new MemberSignatureComparer(
            considerName: false,
            considerExplicitlyImplementedInterfaces: false, //Bug: DevDiv #15775
            considerReturnType: false,
            considerCallingConvention: false, //ignore static-ness
            refKindCompareMode: RefKindCompareMode.ConsiderDifferences | RefKindCompareMode.AllowRefReadonlyVsInMismatch,
            typeComparison: TypeCompareKind.AllIgnoreOptions);

        /// <summary>
        /// Same as <see cref="CSharpOverrideComparer"/> except that it pays attention to custom modifiers and return type.  
        /// Normally, the return type isn't considered during overriding, but this comparer is actually used to find
        /// exact matches (i.e. before tie-breaking takes place amongst close matches).
        /// </summary>
        public static readonly MemberSignatureComparer CSharpCustomModifierOverrideComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: true,
            considerCallingConvention: false, //ignore static-ness
            refKindCompareMode: RefKindCompareMode.ConsiderDifferences | RefKindCompareMode.AllowRefReadonlyVsInMismatch,
            typeComparison: TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes | TypeCompareKind.IgnoreNativeIntegers);

        /// <summary>
        /// If this returns false, then the real override comparer (whichever one is appropriate for the scenario)
        /// will also return false.
        /// </summary>
        internal static readonly MemberSignatureComparer SloppyOverrideComparer = new MemberSignatureComparer(
            considerName: false,
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: false,
            considerCallingConvention: false, //ignore static-ness
            refKindCompareMode: RefKindCompareMode.RefOutInRefReadonlyMatch,
            typeComparison: TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes | TypeCompareKind.IgnoreDynamicAndTupleNames);

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
            considerCallingConvention: true,
            refKindCompareMode: RefKindCompareMode.RefOutInRefReadonlyMatch,
            typeComparison: TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes | TypeCompareKind.IgnoreNativeIntegers);

        /// <summary>
        /// Same as <see cref="RuntimeSignatureComparer"/>, but in addition ignores name.
        /// </summary>
        public static readonly MemberSignatureComparer RuntimeExplicitImplementationSignatureComparer = new MemberSignatureComparer(
            considerName: false,
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: true,
            considerCallingConvention: true,
            refKindCompareMode: RefKindCompareMode.RefOutInRefReadonlyMatch,
            typeComparison: TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes | TypeCompareKind.IgnoreNativeIntegers);

        /// <summary>
        /// Same as <see cref="RuntimeSignatureComparer"/>, but distinguishes between <c>ref</c> and <c>out</c>. During override resolution,
        /// if we find two methods that match except for <c>ref</c>/<c>out</c>, we want to prefer the one that matches, even
        /// if the runtime doesn't.
        /// </summary>
        public static readonly MemberSignatureComparer RuntimePlusRefOutSignatureComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: true,
            considerCallingConvention: true,
            refKindCompareMode: RefKindCompareMode.ConsiderDifferences | RefKindCompareMode.AllowRefReadonlyVsInMismatch,
            typeComparison: TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes | TypeCompareKind.IgnoreNativeIntegers);

        /// <summary>
        /// This instance is the same as RuntimeSignatureComparer.
        /// CONSIDER: just use RuntimeSignatureComparer?
        /// </summary>
        public static readonly MemberSignatureComparer RuntimeImplicitImplementationComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: true,
            considerReturnType: true,
            considerCallingConvention: true,
            refKindCompareMode: RefKindCompareMode.RefOutInRefReadonlyMatch,
            typeComparison: TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes | TypeCompareKind.IgnoreNativeIntegers);

        /// <summary>
        /// This instance is used to search for members that have identical signatures in every regard.
        /// </summary>
        public static readonly MemberSignatureComparer RetargetedExplicitImplementationComparer = new MemberSignatureComparer(
            considerName: true,
            considerExplicitlyImplementedInterfaces: false, //we'll be comparing interface members anyway
            considerReturnType: true,
            considerCallingConvention: true,
            refKindCompareMode: RefKindCompareMode.ConsiderDifferences | RefKindCompareMode.AllowRefReadonlyVsInMismatch,
            typeComparison: TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes | TypeCompareKind.IgnoreNativeIntegers); //if it was a true explicit impl, we expect it to remain so after retargeting

        /// <summary>
        /// This instance is used for performing approximate overload resolution of documentation
        /// comment <c>cref</c> attributes. It ignores the name, because the candidates were all found by lookup.
        /// </summary>
        public static readonly MemberSignatureComparer CrefComparer = new MemberSignatureComparer(
            considerName: false, //handled by lookup
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: false,
            considerCallingConvention: false, //ignore static-ness
            refKindCompareMode: RefKindCompareMode.ConsiderDifferences,
            typeComparison: TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes | TypeCompareKind.IgnoreDynamicAndTupleNames);

        /// <summary>
        /// Compare signatures of methods from a method group (only used in logic for older language version).
        /// </summary>
        internal static readonly MemberSignatureComparer CSharp10MethodGroupSignatureComparer = new MemberSignatureComparer(
            considerName: false,
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: true,
            refKindCompareMode: RefKindCompareMode.ConsiderDifferences,
            considerCallingConvention: false,
            considerArity: true,
            considerDefaultValues: true,
            typeComparison: TypeCompareKind.AllIgnoreOptions);

        /// <summary>
        /// Compare signatures of methods from a method group.
        /// </summary>
        internal static readonly MemberSignatureComparer MethodGroupSignatureComparer = new MemberSignatureComparer(
            considerName: false,
            considerExplicitlyImplementedInterfaces: false,
            considerReturnType: true,
            refKindCompareMode: RefKindCompareMode.ConsiderDifferences,
            considerCallingConvention: false,
            considerArity: false,
            considerDefaultValues: true,
            typeComparison: TypeCompareKind.AllIgnoreOptions);

        // Compare the "unqualified" part of the member name (no explicit part)
        private readonly bool _considerName;

        // Compare the interfaces implemented (as symbols, to avoid ambiguous representations)
        private readonly bool _considerExplicitlyImplementedInterfaces;

        // Compare the type symbols of the return types
        private readonly bool _considerReturnType;

        // Compare the arity (type parameter count)
        private readonly bool _considerArity;

        // Compare the full calling conventions.  Still compares varargs if false.
        private readonly bool _considerCallingConvention;

        // Compare explicit default values
        private readonly bool _considerDefaultValues;

        private readonly RefKindCompareMode _refKindCompareMode;

        // Equality options for parameter types and return types (if return is considered).
        private readonly TypeCompareKind _typeComparison;

        private MemberSignatureComparer(
            bool considerName,
            bool considerExplicitlyImplementedInterfaces,
            bool considerReturnType,
            bool considerCallingConvention,
            RefKindCompareMode refKindCompareMode,
            bool considerArity = true,
            bool considerDefaultValues = false,
            TypeCompareKind typeComparison = TypeCompareKind.IgnoreDynamic | TypeCompareKind.IgnoreNativeIntegers)
        {
            Debug.Assert(!considerExplicitlyImplementedInterfaces || considerName, "Doesn't make sense to consider interfaces separately from name.");

            _considerName = considerName;
            _considerExplicitlyImplementedInterfaces = considerExplicitlyImplementedInterfaces;
            _considerReturnType = considerReturnType;
            _considerCallingConvention = considerCallingConvention;
            _refKindCompareMode = refKindCompareMode;
            _considerArity = considerArity;
            _considerDefaultValues = considerDefaultValues;
            _typeComparison = typeComparison;
            Debug.Assert((_typeComparison & TypeCompareKind.FunctionPointerRefOutInRefReadonlyMatch) == 0,
                         $"Rely on the {nameof(refKindCompareMode)} flag to set this to ensure all cases are handled.");
            Debug.Assert(_refKindCompareMode == RefKindCompareMode.RefOutInRefReadonlyMatch ||
                (_refKindCompareMode & RefKindCompareMode.ConsiderDifferences) != 0,
                $"Cannot set {nameof(RefKindCompareMode)} flags without {nameof(RefKindCompareMode.ConsiderDifferences)}.");
            if ((refKindCompareMode & RefKindCompareMode.ConsiderDifferences) == 0)
            {
                _typeComparison |= TypeCompareKind.FunctionPointerRefOutInRefReadonlyMatch;
            }
        }

        #region IEqualityComparer<Symbol> Members

        public bool Equals(Symbol? member1, Symbol? member2)
        {
            if (ReferenceEquals(member1, member2))
            {
                return true;
            }

            if (member1 is null || member2 is null || member1.Kind != member2.Kind)
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
            if (_considerArity && (member1.GetMemberArity() != member2.GetMemberArity()))
            {
                return false;
            }

            if (member1.GetParameterCount() != member2.GetParameterCount())
            {
                return false;
            }

            TypeMap? typeMap1 = GetTypeMap(member1);
            TypeMap? typeMap2 = GetTypeMap(member2);

            if (_considerReturnType && !HaveSameReturnTypes(member1, typeMap1, member2, typeMap2, _typeComparison))
            {
                return false;
            }

            if (member1.GetParameterCount() > 0 && !HaveSameParameterTypes(member1.GetParameters().AsSpan(), typeMap1, member2.GetParameters().AsSpan(), typeMap2,
                                                                           _refKindCompareMode, considerDefaultValues: _considerDefaultValues, _typeComparison))
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

                    if (!explicitInterfaceImplementations1.SetEquals(explicitInterfaceImplementations2, SymbolEqualityComparer.ConsiderEverything))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public int GetHashCode(Symbol? member)
        {
            int hash = 1;
            if (member is not null)
            {
                hash = Hash.Combine((int)member.Kind, hash);

                if (_considerName)
                {
                    hash = Hash.Combine(ExplicitInterfaceHelpers.GetMemberNameWithoutInterfaceName(member.Name), hash);
                    // CONSIDER: could use interface type, but that might be quite expensive
                }

                if (_considerReturnType && member.GetMemberArity() == 0 &&
                    (_typeComparison & TypeCompareKind.AllIgnoreOptions) == 0) // If it is generic, then type argument might be in return type.
                {
                    hash = Hash.Combine(member.GetTypeOrReturnType().GetHashCode(), hash);
                }

                // CONSIDER: modify hash for constraints?

                if (member.Kind != SymbolKind.Field)
                {
                    if (_considerArity)
                    {
                        hash = Hash.Combine(member.GetMemberArity(), hash);
                    }

                    hash = Hash.Combine(member.GetParameterCount(), hash);
                }
            }
            return hash;
        }

        #endregion

        public static bool HaveSameReturnTypes(Symbol member1, TypeMap? typeMap1, Symbol member2, TypeMap? typeMap2, TypeCompareKind typeComparison)
        {
            RefKind refKind1;
            TypeWithAnnotations unsubstitutedReturnType1;
            ImmutableArray<CustomModifier> refCustomModifiers1;
            member1.GetTypeOrReturnType(out refKind1, out unsubstitutedReturnType1, out refCustomModifiers1);

            RefKind refKind2;
            TypeWithAnnotations unsubstitutedReturnType2;
            ImmutableArray<CustomModifier> refCustomModifiers2;
            member2.GetTypeOrReturnType(out refKind2, out unsubstitutedReturnType2, out refCustomModifiers2);

            // short-circuit type map building in the easiest cases
            if (refKind1 != refKind2)
            {
                return false;
            }

            var isVoid1 = unsubstitutedReturnType1.IsVoidType();
            var isVoid2 = unsubstitutedReturnType2.IsVoidType();

            if (isVoid1 != isVoid2)
            {
                return false;
            }

            if (isVoid1)
            {
                if ((typeComparison & TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds) != 0 ||
                    (unsubstitutedReturnType1.CustomModifiers.IsEmpty && unsubstitutedReturnType2.CustomModifiers.IsEmpty))
                {
                    return true;
                }
            }

            var returnType1 = SubstituteType(typeMap1, unsubstitutedReturnType1);
            var returnType2 = SubstituteType(typeMap2, unsubstitutedReturnType2);
            if (!returnType1.Equals(returnType2, typeComparison))
            {
                return false;
            }

            if (((typeComparison & TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds) == 0) &&
                !HaveSameCustomModifiers(refCustomModifiers1, typeMap1, refCustomModifiers2, typeMap2))
            {
                return false;
            }

            return true;
        }

        internal static TypeMap? GetTypeMap(Symbol member)
        {
            var typeParameters = member.GetMemberTypeParameters();
            return typeParameters.IsEmpty ?
                null :
                new TypeMap(
                    typeParameters,
                    IndexedTypeParameterSymbol.Take(member.GetMemberArity()),
                    true);
        }

        public static bool HaveSameConstraints(ImmutableArray<TypeParameterSymbol> typeParameters1, TypeMap? typeMap1, ImmutableArray<TypeParameterSymbol> typeParameters2, TypeMap? typeMap2, TypeCompareKind typeComparison)
        {
            Debug.Assert(typeParameters1.Length == typeParameters2.Length);

            int arity = typeParameters1.Length;
            for (int i = 0; i < arity; i++)
            {
                if (!HaveSameConstraints(typeParameters1[i], typeMap1, typeParameters2[i], typeMap2, typeComparison))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool HaveSameConstraints(TypeParameterSymbol typeParameter1, TypeMap? typeMap1, TypeParameterSymbol typeParameter2, TypeMap? typeMap2, TypeCompareKind typeComparison)
        {
            // Spec 13.4.3: Implementation of generic methods.

            if ((typeParameter1.HasConstructorConstraint != typeParameter2.HasConstructorConstraint) ||
                (typeParameter1.HasReferenceTypeConstraint != typeParameter2.HasReferenceTypeConstraint) ||
                (typeParameter1.HasValueTypeConstraint != typeParameter2.HasValueTypeConstraint) ||
                (typeParameter1.AllowsRefLikeType != typeParameter2.AllowsRefLikeType) ||
                (typeParameter1.HasUnmanagedTypeConstraint != typeParameter2.HasUnmanagedTypeConstraint) ||
                (typeParameter1.Variance != typeParameter2.Variance))
            {
                return false;
            }

            return HaveSameTypeConstraints(typeParameter1, typeMap1, typeParameter2, typeMap2, SymbolEqualityComparer.Create(typeComparison));
        }

        private static bool HaveSameTypeConstraints(TypeParameterSymbol typeParameter1, TypeMap? typeMap1, TypeParameterSymbol typeParameter2, TypeMap? typeMap2, IEqualityComparer<TypeSymbol> comparer)
        {
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

            var substitutedTypes1 = new HashSet<TypeSymbol>(comparer);
            var substitutedTypes2 = new HashSet<TypeSymbol>(comparer);

            SubstituteConstraintTypes(constraintTypes1, typeMap1, substitutedTypes1);
            SubstituteConstraintTypes(constraintTypes2, typeMap2, substitutedTypes2);

            return AreConstraintTypesSubset(substitutedTypes1, substitutedTypes2, typeParameter2) &&
                AreConstraintTypesSubset(substitutedTypes2, substitutedTypes1, typeParameter1);
        }

        public static bool HaveSameNullabilityInConstraints(TypeParameterSymbol typeParameter1, TypeMap typeMap1, TypeParameterSymbol typeParameter2, TypeMap typeMap2)
        {
            if (!typeParameter1.IsValueType)
            {
                bool? isNotNullable1 = typeParameter1.IsNotNullable;
                bool? isNotNullable2 = typeParameter2.IsNotNullable;
                if (isNotNullable1.HasValue && isNotNullable2.HasValue &&
                    isNotNullable1.GetValueOrDefault() != isNotNullable2.GetValueOrDefault())
                {
                    return false;
                }
            }

            return HaveSameTypeConstraints(typeParameter1, typeMap1, typeParameter2, typeMap2, SymbolEqualityComparer.AllIgnoreOptionsPlusNullableWithUnknownMatchesAny);
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

        private static void SubstituteConstraintTypes(ImmutableArray<TypeWithAnnotations> types, TypeMap? typeMap, HashSet<TypeSymbol> result)
        {
            foreach (var type in types)
            {
                result.Add(SubstituteType(typeMap, type).Type);
            }
        }

        internal static bool HaveSameParameterTypes(
            ReadOnlySpan<ParameterSymbol> params1,
            TypeMap? typeMap1,
            ReadOnlySpan<ParameterSymbol> params2,
            TypeMap? typeMap2,
            RefKindCompareMode refKindCompareMode,
            bool considerDefaultValues,
            TypeCompareKind typeComparison)
        {
            Debug.Assert(params1.Length == params2.Length);

            var numParams = params1.Length;

            for (int i = 0; i < numParams; i++)
            {
                if (!HaveSameParameterType(params1[i], typeMap1, params2[i], typeMap2, refKindCompareMode, considerDefaultValues, typeComparison))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool HaveSameParameterType(
            ParameterSymbol param1,
            TypeMap? typeMap1,
            ParameterSymbol param2,
            TypeMap? typeMap2,
            RefKindCompareMode refKindCompareMode,
            bool considerDefaultValues,
            TypeCompareKind typeComparison)
        {
            var type1 = SubstituteType(typeMap1, param1.TypeWithAnnotations);
            var type2 = SubstituteType(typeMap2, param2.TypeWithAnnotations);

            if (!type1.Equals(type2, typeComparison))
            {
                return false;
            }

            if (considerDefaultValues && param1.ExplicitDefaultConstantValue != param2.ExplicitDefaultConstantValue)
            {
                return false;
            }

            if ((typeComparison & TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds) == 0 &&
                !HaveSameCustomModifiers(param1.RefCustomModifiers, typeMap1, param2.RefCustomModifiers, typeMap2))
            {
                return false;
            }

            var refKind1 = param1.RefKind;
            var refKind2 = param2.RefKind;

            // Metadata signatures don't distinguish ref/out, but C# does - even when comparing metadata method signatures.
            if (refKindCompareMode != RefKindCompareMode.IgnoreRefKind)
            {
                if ((refKindCompareMode & RefKindCompareMode.ConsiderDifferences) != 0)
                {
                    if (!areRefKindsCompatible(refKindCompareMode, refKind1, refKind2))
                    {
                        return false;
                    }
                }
                else
                {
                    Debug.Assert(refKindCompareMode == RefKindCompareMode.RefOutInRefReadonlyMatch);
                    if ((refKind1 == RefKind.None) != (refKind2 == RefKind.None))
                    {
                        return false;
                    }
                }
            }

            return true;

            static bool areRefKindsCompatible(RefKindCompareMode refKindCompareMode, RefKind refKind1, RefKind refKind2)
            {
                if (refKind1 == refKind2)
                {
                    return true;
                }

                if ((refKindCompareMode & RefKindCompareMode.AllowRefReadonlyVsInMismatch) != 0)
                {
                    return (refKind1, refKind2) is (RefKind.RefReadOnlyParameter, RefKind.In) or (RefKind.In, RefKind.RefReadOnlyParameter);
                }

                return false;
            }
        }

        internal static TypeWithAnnotations SubstituteType(TypeMap? typeMap, TypeWithAnnotations typeSymbol)
        {
            return typeMap == null ? typeSymbol : typeSymbol.SubstituteType(typeMap);
        }

        private static bool HaveSameCustomModifiers(ImmutableArray<CustomModifier> customModifiers1, TypeMap? typeMap1, ImmutableArray<CustomModifier> customModifiers2, TypeMap? typeMap2)
        {
            // the runtime compares custom modifiers using (effectively) SequenceEqual
            return SubstituteModifiers(typeMap1, customModifiers1).SequenceEqual(SubstituteModifiers(typeMap2, customModifiers2));
        }

        private static ImmutableArray<CustomModifier> SubstituteModifiers(TypeMap? typeMap, ImmutableArray<CustomModifier> customModifiers)
        {
            return typeMap == null ? customModifiers : typeMap.SubstituteCustomModifiers(customModifiers);
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

        /// <summary>
        /// Do the members differ in terms of tuple names (both in their return type and parameters), but would match ignoring names?
        ///
        /// We'll look at the result of equality without tuple names (1) and with tuple names (2).
        /// The question is whether there is a change in tuple element names only (3).
        ///
        /// member1                       vs. member2                        | (1) | (2) |    (3)    |
        /// <c>(int a, int b) M()</c>     vs. <c>(int a, int b) M()</c>      | yes | yes |   match   |
        /// <c>(int a, int b) M()</c>     vs. <c>(int x, int y) M()</c>      | yes | no  | different |
        /// <c>void M((int a, int b))</c> vs. <c>void M((int x, int y))</c>  | yes | no  | different |
        /// <c>int M()</c>                vs. <c>string M()</c>              | no  | no  |   match   |
        ///
        /// </summary>
        internal static bool ConsideringTupleNamesCreatesDifference(Symbol member1, Symbol member2)
        {
            return !CSharpWithTupleNamesComparer.Equals(member1, member2) &&
                CSharpWithoutTupleNamesComparer.Equals(member1, member2);
        }

        [Flags]
        internal enum RefKindCompareMode
        {
            /// <summary>
            /// All ref modifiers are considered equivalent.
            /// </summary>
            RefOutInRefReadonlyMatch = 0,

            /// <summary>
            /// Parameters with different ref modifiers are considered different.
            /// </summary>
            ConsiderDifferences = 1 << 0,

            /// <summary>
            /// 'in'/'ref readonly' modifiers are considered equivalent.
            /// </summary>
            AllowRefReadonlyVsInMismatch = 1 << 1,

            /// <summary>
            /// Ignore ref kind differences.
            /// </summary>
            IgnoreRefKind = 1 << 2,
        }
    }
}
