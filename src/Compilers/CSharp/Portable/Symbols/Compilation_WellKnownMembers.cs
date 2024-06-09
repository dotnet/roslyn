// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RuntimeMembers;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.Symbols.TypeSymbolExtensions;

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class CSharpCompilation
    {
        private WellKnownMembersSignatureComparer? _lazyWellKnownMemberSignatureComparer;

        /// <summary>
        /// An array of cached well known types available for use in this Compilation.
        /// Lazily filled by GetWellKnownType method.
        /// </summary>
        private NamedTypeSymbol?[]? _lazyWellKnownTypes;

        /// <summary>
        /// Lazy cache of well known members.
        /// Not yet known value is represented by ErrorTypeSymbol.UnknownResultType
        /// </summary>
        private Symbol?[]? _lazyWellKnownTypeMembers;

        private bool _usesNullableAttributes;
        private int _needsGeneratedAttributes;
        private bool _needsGeneratedAttributes_IsFrozen;

        internal WellKnownMembersSignatureComparer WellKnownMemberSignatureComparer
            => InterlockedOperations.Initialize(ref _lazyWellKnownMemberSignatureComparer, static self => new WellKnownMembersSignatureComparer(self), this);

        /// <summary>
        /// Returns a value indicating which embedded attributes should be generated during emit phase.
        /// The value is set during binding the symbols that need those attributes, and is frozen on first trial to get it.
        /// Freezing is needed to make sure that nothing tries to modify the value after the value is read.
        /// </summary>
        internal EmbeddableAttributes GetNeedsGeneratedAttributes()
        {
            _needsGeneratedAttributes_IsFrozen = true;
            return (EmbeddableAttributes)_needsGeneratedAttributes;
        }

        private void SetNeedsGeneratedAttributes(EmbeddableAttributes attributes)
        {
            Debug.Assert(!_needsGeneratedAttributes_IsFrozen);
            ThreadSafeFlagOperations.Set(ref _needsGeneratedAttributes, (int)attributes);
        }

        internal bool GetUsesNullableAttributes()
        {
            _needsGeneratedAttributes_IsFrozen = true;
            return _usesNullableAttributes;
        }

        private void SetUsesNullableAttributes()
        {
            Debug.Assert(!_needsGeneratedAttributes_IsFrozen);
            _usesNullableAttributes = true;
        }

        /// <summary>
        /// Lookup member declaration in well known type used by this Compilation.
        /// </summary>
        /// <remarks>
        /// If a well-known member of a generic type instantiation is needed use this method to get the corresponding generic definition and 
        /// <see cref="MethodSymbol.AsMember"/> to construct an instantiation.
        /// </remarks>
        internal Symbol? GetWellKnownTypeMember(WellKnownMember member)
        {
            Debug.Assert(member >= 0 && member < WellKnownMember.Count);

            // Test hook: if a member is marked missing, then return null.
            if (IsMemberMissing(member)) return null;

            if (_lazyWellKnownTypeMembers == null || ReferenceEquals(_lazyWellKnownTypeMembers[(int)member], ErrorTypeSymbol.UnknownResultType))
            {
                if (_lazyWellKnownTypeMembers == null)
                {
                    var wellKnownTypeMembers = new Symbol[(int)WellKnownMember.Count];

                    for (int i = 0; i < wellKnownTypeMembers.Length; i++)
                    {
                        wellKnownTypeMembers[i] = ErrorTypeSymbol.UnknownResultType;
                    }

                    Interlocked.CompareExchange(ref _lazyWellKnownTypeMembers, wellKnownTypeMembers, null);
                }

                MemberDescriptor descriptor = WellKnownMembers.GetDescriptor(member);
                NamedTypeSymbol type = descriptor.IsSpecialTypeMember
                                            ? this.GetSpecialType(descriptor.DeclaringSpecialType)
                                            : this.GetWellKnownType(descriptor.DeclaringWellKnownType);
                Symbol? result = null;

                if (!type.IsErrorType())
                {
                    result = GetRuntimeMember(type, descriptor, WellKnownMemberSignatureComparer, accessWithinOpt: this.Assembly);
                }

                Interlocked.CompareExchange(ref _lazyWellKnownTypeMembers[(int)member], result, ErrorTypeSymbol.UnknownResultType);
            }

            return _lazyWellKnownTypeMembers[(int)member];
        }

        /// <summary>
        /// This method handles duplicate types in a few different ways:
        /// - for types before C# 7, the first candidate is returned with a warning
        /// - for types after C# 7, the type is considered missing
        /// - in both cases, when BinderFlags.IgnoreCorLibraryDuplicatedTypes is set, type from corlib will not count as a duplicate
        /// </summary>
        internal NamedTypeSymbol GetWellKnownType(WellKnownType type)
        {
            Debug.Assert(type.IsValid());

            bool ignoreCorLibraryDuplicatedTypes = this.Options.TopLevelBinderFlags.Includes(BinderFlags.IgnoreCorLibraryDuplicatedTypes);

            int index = (int)type - (int)WellKnownType.First;
            if (_lazyWellKnownTypes == null || _lazyWellKnownTypes[index] is null)
            {
                if (_lazyWellKnownTypes == null)
                {
                    Interlocked.CompareExchange(ref _lazyWellKnownTypes, new NamedTypeSymbol[(int)WellKnownTypes.Count], null);
                }

                string mdName = type.GetMetadataName();
                var warnings = DiagnosticBag.GetInstance();
                NamedTypeSymbol? result;
                (AssemblySymbol, AssemblySymbol) conflicts = default;

                if (IsTypeMissing(type))
                {
                    result = null;
                }
                else
                {
                    // well-known types introduced before CSharp7 allow lookup ambiguity and report a warning
                    DiagnosticBag? legacyWarnings = (type <= WellKnownType.CSharp7Sentinel) ? warnings : null;
                    result = this.Assembly.GetTypeByMetadataName(
                        mdName, includeReferences: true, useCLSCompliantNameArityEncoding: true, isWellKnownType: true, conflicts: out conflicts,
                        warnings: legacyWarnings, ignoreCorLibraryDuplicatedTypes: ignoreCorLibraryDuplicatedTypes);
                    Debug.Assert(result?.IsErrorType() != true);
                }

                if (result is null || result.IsExtension)
                {
                    MetadataTypeName emittedName = MetadataTypeName.FromFullName(mdName, useCLSCompliantNameArityEncoding: true);
                    if (type.IsValueTupleType())
                    {
                        CSDiagnosticInfo errorInfo;
                        if (conflicts.Item1 is null)
                        {
                            Debug.Assert(conflicts.Item2 is null);
                            errorInfo = new CSDiagnosticInfo(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, emittedName.FullName);
                        }
                        else
                        {
                            errorInfo = new CSDiagnosticInfo(ErrorCode.ERR_PredefinedValueTupleTypeAmbiguous3, emittedName.FullName, conflicts.Item1, conflicts.Item2);
                        }

                        result = new MissingMetadataTypeSymbol.TopLevel(this.Assembly.Modules[0], ref emittedName, type, errorInfo);
                    }
                    else
                    {
                        result = new MissingMetadataTypeSymbol.TopLevel(this.Assembly.Modules[0], ref emittedName, type);
                    }
                }

                if (Interlocked.CompareExchange(ref _lazyWellKnownTypes[index], result, null) is object)
                {
                    Debug.Assert(
                        TypeSymbol.Equals(result, _lazyWellKnownTypes[index], TypeCompareKind.ConsiderEverything2) || (_lazyWellKnownTypes[index]!.IsErrorType() && result.IsErrorType())
                    );
                }
                else
                {
                    AdditionalCodegenWarnings.AddRange(warnings);
                }

                warnings.Free();
            }

            return _lazyWellKnownTypes[index]!;
        }

        internal bool IsAttributeType(TypeSymbol type)
        {
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            return IsEqualOrDerivedFromWellKnownClass(type, WellKnownType.System_Attribute, ref discardedUseSiteInfo);
        }

        internal override bool IsAttributeType(ITypeSymbol type)
        {
            return IsAttributeType(type.EnsureCSharpSymbolOrNull(nameof(type)));
        }

        internal bool IsExceptionType(TypeSymbol type, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            return IsEqualOrDerivedFromWellKnownClass(type, WellKnownType.System_Exception, ref useSiteInfo);
        }

        internal bool IsReadOnlySpanType(TypeSymbol type)
        {
            return TypeSymbol.Equals(type.OriginalDefinition, GetWellKnownType(WellKnownType.System_ReadOnlySpan_T), TypeCompareKind.ConsiderEverything2);
        }

        internal bool IsEqualOrDerivedFromWellKnownClass(TypeSymbol type, WellKnownType wellKnownType, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(wellKnownType == WellKnownType.System_Attribute ||
                         wellKnownType == WellKnownType.System_Exception);

            if (type.Kind != SymbolKind.NamedType || type.TypeKind != TypeKind.Class)
            {
                return false;
            }

            var wkType = GetWellKnownType(wellKnownType);
            return type.Equals(wkType, TypeCompareKind.ConsiderEverything) || type.IsDerivedFrom(wkType, TypeCompareKind.ConsiderEverything, useSiteInfo: ref useSiteInfo);
        }

        internal override bool IsSystemTypeReference(ITypeSymbolInternal type)
        {
            return TypeSymbol.Equals((TypeSymbol)type, GetWellKnownType(WellKnownType.System_Type), TypeCompareKind.ConsiderEverything2);
        }

        internal override ISymbolInternal? CommonGetWellKnownTypeMember(WellKnownMember member)
        {
            return GetWellKnownTypeMember(member);
        }

        internal override ITypeSymbolInternal CommonGetWellKnownType(WellKnownType wellknownType)
        {
            return GetWellKnownType(wellknownType);
        }

        internal static Symbol? GetRuntimeMember(NamedTypeSymbol declaringType, in MemberDescriptor descriptor, SignatureComparer<MethodSymbol, FieldSymbol, PropertySymbol, TypeSymbol, ParameterSymbol> comparer, AssemblySymbol? accessWithinOpt)
        {
            var members = declaringType.GetMembers(descriptor.Name);
            return GetRuntimeMember(members, descriptor, comparer, accessWithinOpt);
        }

        internal static Symbol? GetRuntimeMember(ImmutableArray<Symbol> members, in MemberDescriptor descriptor, SignatureComparer<MethodSymbol, FieldSymbol, PropertySymbol, TypeSymbol, ParameterSymbol> comparer, AssemblySymbol? accessWithinOpt)
        {
            SymbolKind targetSymbolKind;
            MethodKind targetMethodKind = MethodKind.Ordinary;
            bool isStatic = (descriptor.Flags & MemberFlags.Static) != 0;

            Symbol? result = null;
            switch (descriptor.Flags & MemberFlags.KindMask)
            {
                case MemberFlags.Constructor:
                    targetSymbolKind = SymbolKind.Method;
                    targetMethodKind = MethodKind.Constructor;
                    //  static constructors are never called explicitly
                    Debug.Assert(!isStatic);
                    break;

                case MemberFlags.Method:
                    targetSymbolKind = SymbolKind.Method;
                    break;

                case MemberFlags.PropertyGet:
                    targetSymbolKind = SymbolKind.Method;
                    targetMethodKind = MethodKind.PropertyGet;
                    break;

                case MemberFlags.Field:
                    targetSymbolKind = SymbolKind.Field;
                    break;

                case MemberFlags.Property:
                    targetSymbolKind = SymbolKind.Property;
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(descriptor.Flags);
            }

            foreach (var member in members)
            {
                if (!member.Name.Equals(descriptor.Name))
                {
                    continue;
                }

                if (member.Kind != targetSymbolKind || member.IsStatic != isStatic ||
                    !(member.DeclaredAccessibility == Accessibility.Public || (accessWithinOpt is object && Symbol.IsSymbolAccessible(member, accessWithinOpt))))
                {
                    continue;
                }

                switch (targetSymbolKind)
                {
                    case SymbolKind.Method:
                        {
                            MethodSymbol method = (MethodSymbol)member;
                            MethodKind methodKind = method.MethodKind;
                            // Treat user-defined conversions and operators as ordinary methods for the purpose
                            // of matching them here.
                            if (methodKind == MethodKind.Conversion || methodKind == MethodKind.UserDefinedOperator)
                            {
                                methodKind = MethodKind.Ordinary;
                            }

                            if (method.Arity != descriptor.Arity || methodKind != targetMethodKind ||
                                ((descriptor.Flags & MemberFlags.Virtual) != 0) != (method.IsVirtual || method.IsOverride || method.IsAbstract))
                            {
                                continue;
                            }

                            if (!comparer.MatchMethodSignature(method, descriptor.Signature))
                            {
                                continue;
                            }
                        }

                        break;

                    case SymbolKind.Property:
                        {
                            PropertySymbol property = (PropertySymbol)member;
                            if (((descriptor.Flags & MemberFlags.Virtual) != 0) != (property.IsVirtual || property.IsOverride || property.IsAbstract))
                            {
                                continue;
                            }

                            if (!comparer.MatchPropertySignature(property, descriptor.Signature))
                            {
                                continue;
                            }
                        }

                        break;

                    case SymbolKind.Field:
                        if (!comparer.MatchFieldSignature((FieldSymbol)member, descriptor.Signature))
                        {
                            continue;
                        }

                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(targetSymbolKind);
                }

                // ambiguity
                if (result is object)
                {
                    result = null;
                    break;
                }

                result = member;
            }

            return result;
        }

        /// <summary>
        /// Synthesizes a custom attribute. 
        /// Returns null if the <paramref name="constructor"/> symbol is missing,
        /// or any of the members in <paramref name="namedArguments" /> are missing.
        /// The attribute is synthesized only if present.
        /// </summary>
        /// <param name="constructor">
        /// Constructor of the attribute. If it doesn't exist, the attribute is not created.
        /// </param>
        /// <param name="arguments">Arguments to the attribute constructor.</param>
        /// <param name="namedArguments">
        /// Takes a list of pairs of well-known members and constants. The constants
        /// will be passed to the field/property referenced by the well-known member.
        /// If the well-known member does not exist in the compilation then no attribute
        /// will be synthesized.
        /// </param>
        /// <param name="isOptionalUse">
        /// Indicates if this particular attribute application should be considered optional.
        /// </param>
        internal SynthesizedAttributeData? TrySynthesizeAttribute(
            WellKnownMember constructor,
            ImmutableArray<TypedConstant> arguments = default,
            ImmutableArray<KeyValuePair<WellKnownMember, TypedConstant>> namedArguments = default,
            bool isOptionalUse = false)
        {
            var ctorSymbol = (MethodSymbol)Binder.GetWellKnownTypeMember(this, constructor, useSiteInfo: out _, isOptional: true);

            if ((object)ctorSymbol == null)
            {
                // if this assert fails, UseSiteErrors for "member" have not been checked before emitting ...
                Debug.Assert(isOptionalUse || WellKnownMembers.IsSynthesizedAttributeOptional(constructor));
                return null;
            }

            if (arguments.IsDefault)
            {
                arguments = ImmutableArray<TypedConstant>.Empty;
            }

            ImmutableArray<KeyValuePair<string, TypedConstant>> namedStringArguments;
            if (namedArguments.IsDefault)
            {
                namedStringArguments = ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty;
            }
            else
            {
                var builder = new ArrayBuilder<KeyValuePair<string, TypedConstant>>(namedArguments.Length);
                foreach (var arg in namedArguments)
                {
                    var wellKnownMember = Binder.GetWellKnownTypeMember(this, arg.Key, useSiteInfo: out _, isOptional: true);
                    if (wellKnownMember == null || wellKnownMember is ErrorTypeSymbol)
                    {
                        // if this assert fails, UseSiteErrors for "member" have not been checked before emitting ...
                        Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(constructor));
                        return null;
                    }
                    else
                    {
                        builder.Add(new KeyValuePair<string, TypedConstant>(
                            wellKnownMember.Name, arg.Value));
                    }
                }
                namedStringArguments = builder.ToImmutableAndFree();
            }

            return SynthesizedAttributeData.Create(this, ctorSymbol, arguments, namedStringArguments);
        }

        internal SynthesizedAttributeData? TrySynthesizeAttribute(
            SpecialMember constructor,
            bool isOptionalUse = false)
        {
            var ctorSymbol = (MethodSymbol)this.GetSpecialTypeMember(constructor);

            if ((object)ctorSymbol == null)
            {
                Debug.Assert(isOptionalUse);
                return null;
            }

            return SynthesizedAttributeData.Create(
                this,
                ctorSymbol,
                arguments: ImmutableArray<TypedConstant>.Empty,
                namedArguments: ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
        }

        internal SynthesizedAttributeData? SynthesizeDecimalConstantAttribute(decimal value)
        {
            bool isNegative;
            byte scale;
            uint low, mid, high;
            value.GetBits(out isNegative, out scale, out low, out mid, out high);
            var systemByte = GetSpecialType(SpecialType.System_Byte);
            Debug.Assert(!systemByte.HasUseSiteError);

            var systemUnit32 = GetSpecialType(SpecialType.System_UInt32);
            Debug.Assert(!systemUnit32.HasUseSiteError);

            return TrySynthesizeAttribute(
                WellKnownMember.System_Runtime_CompilerServices_DecimalConstantAttribute__ctor,
                ImmutableArray.Create(
                    new TypedConstant(systemByte, TypedConstantKind.Primitive, scale),
                    new TypedConstant(systemByte, TypedConstantKind.Primitive, (byte)(isNegative ? 128 : 0)),
                    new TypedConstant(systemUnit32, TypedConstantKind.Primitive, high),
                    new TypedConstant(systemUnit32, TypedConstantKind.Primitive, mid),
                    new TypedConstant(systemUnit32, TypedConstantKind.Primitive, low)
                ));
        }

        internal SynthesizedAttributeData? SynthesizeDateTimeConstantAttribute(DateTime value)
        {
            var ticks = new TypedConstant(GetSpecialType(SpecialType.System_Int64), TypedConstantKind.Primitive, value.Ticks);

            return TrySynthesizeAttribute(
                WellKnownMember.System_Runtime_CompilerServices_DateTimeConstantAttribute__ctor,
                ImmutableArray.Create(ticks));
        }

        internal SynthesizedAttributeData? SynthesizeDebuggerBrowsableNeverAttribute()
        {
            if (Options.OptimizationLevel != OptimizationLevel.Debug)
            {
                return null;
            }

            return TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_DebuggerBrowsableAttribute__ctor,
                   ImmutableArray.Create(new TypedConstant(
                       GetWellKnownType(WellKnownType.System_Diagnostics_DebuggerBrowsableState),
                       TypedConstantKind.Enum,
                       DebuggerBrowsableState.Never)));
        }

        internal SynthesizedAttributeData? SynthesizeDebuggerStepThroughAttribute()
        {
            if (Options.OptimizationLevel != OptimizationLevel.Debug)
            {
                return null;
            }

            return TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_DebuggerStepThroughAttribute__ctor);
        }

        private void EnsureEmbeddableAttributeExists(EmbeddableAttributes attribute, BindingDiagnosticBag? diagnostics, Location location, bool modifyCompilation)
        {
            Debug.Assert(!modifyCompilation || !_needsGeneratedAttributes_IsFrozen);

            if (CheckIfAttributeShouldBeEmbedded(attribute, diagnostics, location) && modifyCompilation)
            {
                SetNeedsGeneratedAttributes(attribute);
            }

            if ((attribute & (EmbeddableAttributes.NullableAttribute | EmbeddableAttributes.NullableContextAttribute)) != 0 &&
                modifyCompilation)
            {
                SetUsesNullableAttributes();
            }
        }

        internal void EnsureIsReadOnlyAttributeExists(BindingDiagnosticBag? diagnostics, Location location, bool modifyCompilation)
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.IsReadOnlyAttribute, diagnostics, location, modifyCompilation);
        }

        internal void EnsureRequiresLocationAttributeExists(BindingDiagnosticBag? diagnostics, Location location, bool modifyCompilation)
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.RequiresLocationAttribute, diagnostics, location, modifyCompilation);
        }

        internal void EnsureParamCollectionAttributeExistsAndModifyCompilation(BindingDiagnosticBag? diagnostics, Location location)
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.ParamCollectionAttribute, diagnostics, location, modifyCompilation: true);
        }

        internal void EnsureIsByRefLikeAttributeExists(BindingDiagnosticBag? diagnostics, Location location, bool modifyCompilation)
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.IsByRefLikeAttribute, diagnostics, location, modifyCompilation);
        }

        internal void EnsureIsUnmanagedAttributeExists(BindingDiagnosticBag? diagnostics, Location location, bool modifyCompilation)
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.IsUnmanagedAttribute, diagnostics, location, modifyCompilation);
        }

        internal void EnsureNullableAttributeExists(BindingDiagnosticBag? diagnostics, Location location, bool modifyCompilation)
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.NullableAttribute, diagnostics, location, modifyCompilation);
        }

        internal void EnsureNullableContextAttributeExists(BindingDiagnosticBag? diagnostics, Location location, bool modifyCompilation)
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.NullableContextAttribute, diagnostics, location, modifyCompilation);
        }

        internal void EnsureNativeIntegerAttributeExists(BindingDiagnosticBag? diagnostics, Location location, bool modifyCompilation)
        {
            Debug.Assert(ShouldEmitNativeIntegerAttributes());
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.NativeIntegerAttribute, diagnostics, location, modifyCompilation);
        }

        internal void EnsureScopedRefAttributeExists(BindingDiagnosticBag? diagnostics, Location location, bool modifyCompilation)
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.ScopedRefAttribute, diagnostics, location, modifyCompilation);
        }

        internal bool CheckIfAttributeShouldBeEmbedded(EmbeddableAttributes attribute, BindingDiagnosticBag? diagnosticsOpt, Location locationOpt)
        {
            switch (attribute)
            {
                case EmbeddableAttributes.IsReadOnlyAttribute:
                    return CheckIfAttributeShouldBeEmbedded(
                        diagnosticsOpt,
                        locationOpt,
                        WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute,
                        WellKnownMember.System_Runtime_CompilerServices_IsReadOnlyAttribute__ctor);

                case EmbeddableAttributes.IsByRefLikeAttribute:
                    return CheckIfAttributeShouldBeEmbedded(
                        diagnosticsOpt,
                        locationOpt,
                        WellKnownType.System_Runtime_CompilerServices_IsByRefLikeAttribute,
                        WellKnownMember.System_Runtime_CompilerServices_IsByRefLikeAttribute__ctor);

                case EmbeddableAttributes.IsUnmanagedAttribute:
                    return CheckIfAttributeShouldBeEmbedded(
                        diagnosticsOpt,
                        locationOpt,
                        WellKnownType.System_Runtime_CompilerServices_IsUnmanagedAttribute,
                        WellKnownMember.System_Runtime_CompilerServices_IsUnmanagedAttribute__ctor);

                case EmbeddableAttributes.NullableAttribute:
                    // If the type exists, we'll check both constructors, regardless of which one(s) we'll eventually need.
                    return CheckIfAttributeShouldBeEmbedded(
                        diagnosticsOpt,
                        locationOpt,
                        WellKnownType.System_Runtime_CompilerServices_NullableAttribute,
                        WellKnownMember.System_Runtime_CompilerServices_NullableAttribute__ctorByte,
                        WellKnownMember.System_Runtime_CompilerServices_NullableAttribute__ctorTransformFlags);

                case EmbeddableAttributes.NullableContextAttribute:
                    return CheckIfAttributeShouldBeEmbedded(
                        diagnosticsOpt,
                        locationOpt,
                        WellKnownType.System_Runtime_CompilerServices_NullableContextAttribute,
                        WellKnownMember.System_Runtime_CompilerServices_NullableContextAttribute__ctor);

                case EmbeddableAttributes.NullablePublicOnlyAttribute:
                    return CheckIfAttributeShouldBeEmbedded(
                        diagnosticsOpt,
                        locationOpt,
                        WellKnownType.System_Runtime_CompilerServices_NullablePublicOnlyAttribute,
                        WellKnownMember.System_Runtime_CompilerServices_NullablePublicOnlyAttribute__ctor);

                case EmbeddableAttributes.NativeIntegerAttribute:
                    // If the type exists, we'll check both constructors, regardless of which one(s) we'll eventually need.
                    Debug.Assert(ShouldEmitNativeIntegerAttributes());
                    return CheckIfAttributeShouldBeEmbedded(
                        diagnosticsOpt,
                        locationOpt,
                        WellKnownType.System_Runtime_CompilerServices_NativeIntegerAttribute,
                        WellKnownMember.System_Runtime_CompilerServices_NativeIntegerAttribute__ctor,
                        WellKnownMember.System_Runtime_CompilerServices_NativeIntegerAttribute__ctorTransformFlags);

                case EmbeddableAttributes.ScopedRefAttribute:
                    return CheckIfAttributeShouldBeEmbedded(
                        diagnosticsOpt,
                        locationOpt,
                        WellKnownType.System_Runtime_CompilerServices_ScopedRefAttribute,
                        WellKnownMember.System_Runtime_CompilerServices_ScopedRefAttribute__ctor);

                case EmbeddableAttributes.RefSafetyRulesAttribute:
                    return CheckIfAttributeShouldBeEmbedded(
                        diagnosticsOpt,
                        locationOpt,
                        WellKnownType.System_Runtime_CompilerServices_RefSafetyRulesAttribute,
                        WellKnownMember.System_Runtime_CompilerServices_RefSafetyRulesAttribute__ctor);

                case EmbeddableAttributes.RequiresLocationAttribute:
                    return CheckIfAttributeShouldBeEmbedded(
                        diagnosticsOpt,
                        locationOpt,
                        WellKnownType.System_Runtime_CompilerServices_RequiresLocationAttribute,
                        WellKnownMember.System_Runtime_CompilerServices_RequiresLocationAttribute__ctor);

                case EmbeddableAttributes.ParamCollectionAttribute:
                    return CheckIfAttributeShouldBeEmbedded(
                        diagnosticsOpt,
                        locationOpt,
                        WellKnownType.System_Runtime_CompilerServices_ParamCollectionAttribute,
                        WellKnownMember.System_Runtime_CompilerServices_ParamCollectionAttribute__ctor);

                default:
                    throw ExceptionUtilities.UnexpectedValue(attribute);
            }
        }

        private bool CheckIfAttributeShouldBeEmbedded(BindingDiagnosticBag? diagnosticsOpt, Location? locationOpt, WellKnownType attributeType, WellKnownMember attributeCtor, WellKnownMember? secondAttributeCtor = null)
        {
            var userDefinedAttribute = GetWellKnownType(attributeType);

            if (userDefinedAttribute is MissingMetadataTypeSymbol)
            {
                if (Options.OutputKind == OutputKind.NetModule)
                {
                    if (diagnosticsOpt != null)
                    {
                        var errorReported = Binder.ReportUseSite(userDefinedAttribute, diagnosticsOpt, locationOpt);
                        Debug.Assert(errorReported);
                    }
                }
                else
                {
                    return true;
                }
            }
            else if (diagnosticsOpt != null)
            {
                // This should produce diagnostics if the member is missing or bad
                var member = Binder.GetWellKnownTypeMember(this, attributeCtor,
                                                           diagnosticsOpt, locationOpt);
                if (member != null && secondAttributeCtor != null)
                {
                    Binder.GetWellKnownTypeMember(this, secondAttributeCtor.Value, diagnosticsOpt, locationOpt);
                }
            }

            return false;
        }

        internal SynthesizedAttributeData? SynthesizeDebuggableAttribute()
        {
            TypeSymbol debuggableAttribute = GetWellKnownType(WellKnownType.System_Diagnostics_DebuggableAttribute);
            Debug.Assert((object)debuggableAttribute != null, "GetWellKnownType unexpectedly returned null");
            if (debuggableAttribute is MissingMetadataTypeSymbol)
            {
                return null;
            }

            TypeSymbol debuggingModesType = GetWellKnownType(WellKnownType.System_Diagnostics_DebuggableAttribute__DebuggingModes);
            RoslynDebug.Assert((object)debuggingModesType != null, "GetWellKnownType unexpectedly returned null");
            if (debuggingModesType is MissingMetadataTypeSymbol)
            {
                return null;
            }

            // IgnoreSymbolStoreDebuggingMode flag is checked by the CLR, it is not referred to by the debugger.
            // It tells the JIT that it doesn't need to load the PDB at the time it generates jitted code. 
            // The PDB would still be used by a debugger, or even by the runtime for putting source line information 
            // on exception stack traces. We always set this flag to avoid overhead of JIT loading the PDB. 
            // The theoretical scenario for not setting it would be a language compiler that wants their sequence points 
            // at specific places, but those places don't match what CLR's heuristics calculate when scanning the IL.
            var ignoreSymbolStoreDebuggingMode = (FieldSymbol?)GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggableAttribute_DebuggingModes__IgnoreSymbolStoreSequencePoints);
            if (ignoreSymbolStoreDebuggingMode is null || !ignoreSymbolStoreDebuggingMode.HasConstantValue)
            {
                return null;
            }

            int constantVal = ignoreSymbolStoreDebuggingMode.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false).Int32Value;

            // Since .NET 2.0 the combinations of None, Default and DisableOptimizations have the following effect:
            // 
            // None                                         JIT optimizations enabled
            // Default                                      JIT optimizations enabled
            // DisableOptimizations                         JIT optimizations enabled
            // Default | DisableOptimizations               JIT optimizations disabled
            if (_options.OptimizationLevel == OptimizationLevel.Debug)
            {
                var defaultDebuggingMode = (FieldSymbol?)GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggableAttribute_DebuggingModes__Default);
                if (defaultDebuggingMode is null || !defaultDebuggingMode.HasConstantValue)
                {
                    return null;
                }

                var disableOptimizationsDebuggingMode = (FieldSymbol?)GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggableAttribute_DebuggingModes__DisableOptimizations);
                if (disableOptimizationsDebuggingMode is null || !disableOptimizationsDebuggingMode.HasConstantValue)
                {
                    return null;
                }

                constantVal |= defaultDebuggingMode.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false).Int32Value;
                constantVal |= disableOptimizationsDebuggingMode.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false).Int32Value;
            }

            if (_options.EnableEditAndContinue)
            {
                var enableEncDebuggingMode = (FieldSymbol?)GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggableAttribute_DebuggingModes__EnableEditAndContinue);
                if (enableEncDebuggingMode is null || !enableEncDebuggingMode.HasConstantValue)
                {
                    return null;
                }

                constantVal |= enableEncDebuggingMode.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false).Int32Value;
            }

            var typedConstantDebugMode = new TypedConstant(debuggingModesType, TypedConstantKind.Enum, constantVal);

            return TrySynthesizeAttribute(
                WellKnownMember.System_Diagnostics_DebuggableAttribute__ctorDebuggingModes,
                ImmutableArray.Create(typedConstantDebugMode));
        }

        /// <summary>
        /// Given a type <paramref name="type"/>, which is either dynamic type OR is a constructed type with dynamic type present in it's type argument tree,
        /// returns a synthesized DynamicAttribute with encoded dynamic transforms array.
        /// </summary>
        /// <remarks>This method is port of AttrBind::CompileDynamicAttr from the native C# compiler.</remarks>
        internal SynthesizedAttributeData? SynthesizeDynamicAttribute(TypeSymbol type, int customModifiersCount, RefKind refKindOpt = RefKind.None)
        {
            RoslynDebug.Assert((object)type != null);
            Debug.Assert(type.ContainsDynamic());

            if (type.IsDynamic() && refKindOpt == RefKind.None && customModifiersCount == 0)
            {
                return TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_DynamicAttribute__ctor);
            }
            else
            {
                NamedTypeSymbol booleanType = GetSpecialType(SpecialType.System_Boolean);
                RoslynDebug.Assert((object)booleanType != null);
                var transformFlags = DynamicTransformsEncoder.Encode(type, refKindOpt, customModifiersCount, booleanType);
                var boolArray = ArrayTypeSymbol.CreateSZArray(booleanType.ContainingAssembly, TypeWithAnnotations.Create(booleanType));
                var arguments = ImmutableArray.Create(new TypedConstant(boolArray, transformFlags));
                return TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_DynamicAttribute__ctorTransformFlags, arguments);
            }
        }

        internal SynthesizedAttributeData? SynthesizeTupleNamesAttribute(TypeSymbol type)
        {
            RoslynDebug.Assert((object)type != null);
            Debug.Assert(type.ContainsTuple());

            var stringType = GetSpecialType(SpecialType.System_String);
            RoslynDebug.Assert((object)stringType != null);
            var names = TupleNamesEncoder.Encode(type, stringType);

            Debug.Assert(!names.IsDefault, "should not need the attribute when no tuple names");

            var stringArray = ArrayTypeSymbol.CreateSZArray(stringType.ContainingAssembly, TypeWithAnnotations.Create(stringType));
            var args = ImmutableArray.Create(new TypedConstant(stringArray, names));
            return TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_TupleElementNamesAttribute__ctorTransformNames, args);
        }

        internal SynthesizedAttributeData? SynthesizeExtensionErasureAttribute(TypeSymbol type)
        {
            Debug.Assert(type is not null);
            Debug.Assert(type.ContainsErasedExtensionType());

            var stringType = GetSpecialType(SpecialType.System_String);
            Debug.Assert(stringType is not null);

            var args = ImmutableArray.Create(new TypedConstant(stringType, TypedConstantKind.Primitive, type));
            return TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_ExtensionErasureAttribute__ctorEncodedType, args);
        }

        internal SynthesizedAttributeData? SynthesizeAttributeUsageAttribute(AttributeTargets targets, bool allowMultiple, bool inherited)
        {
            var attributeTargetsType = GetWellKnownType(WellKnownType.System_AttributeTargets);
            var boolType = GetSpecialType(SpecialType.System_Boolean);
            var arguments = ImmutableArray.Create(
                new TypedConstant(attributeTargetsType, TypedConstantKind.Enum, targets));
            var namedArguments = ImmutableArray.Create(
                new KeyValuePair<WellKnownMember, TypedConstant>(WellKnownMember.System_AttributeUsageAttribute__AllowMultiple, new TypedConstant(boolType, TypedConstantKind.Primitive, allowMultiple)),
                new KeyValuePair<WellKnownMember, TypedConstant>(WellKnownMember.System_AttributeUsageAttribute__Inherited, new TypedConstant(boolType, TypedConstantKind.Primitive, inherited)));
            return TrySynthesizeAttribute(WellKnownMember.System_AttributeUsageAttribute__ctor, arguments, namedArguments);
        }

        internal static class TupleNamesEncoder
        {
            public static ImmutableArray<string?> Encode(TypeSymbol type)
            {
                var namesBuilder = ArrayBuilder<string?>.GetInstance();

                if (!TryGetNames(type, namesBuilder))
                {
                    namesBuilder.Free();
                    return default;
                }

                return namesBuilder.ToImmutableAndFree();
            }

            public static ImmutableArray<TypedConstant> Encode(TypeSymbol type, TypeSymbol stringType)
            {
                var namesBuilder = ArrayBuilder<string?>.GetInstance();

                if (!TryGetNames(type, namesBuilder))
                {
                    namesBuilder.Free();
                    return default;
                }

                var names = namesBuilder.SelectAsArray((name, constantType) =>
                    new TypedConstant(constantType, TypedConstantKind.Primitive, name), stringType);
                namesBuilder.Free();
                return names;
            }

            internal static bool TryGetNames(TypeSymbol type, ArrayBuilder<string?> namesBuilder)
            {
                type.VisitType((t, builder, _, _) => AddNames(t, builder), namesBuilder);
                return namesBuilder.Any(name => name != null);
            }

            private static bool AddNames(TypeSymbol type, ArrayBuilder<string?> namesBuilder)
            {
                if (type.IsTupleType)
                {
                    if (type.TupleElementNames.IsDefaultOrEmpty)
                    {
                        // If none of the tuple elements have names, put
                        // null placeholders in.
                        // TODO(https://github.com/dotnet/roslyn/issues/12347):
                        // A possible optimization could be to emit an empty attribute
                        // if all the names are missing, but that has to be true
                        // recursively.
                        namesBuilder.AddMany(null, type.TupleElementTypesWithAnnotations.Length);
                    }
                    else
                    {
                        namesBuilder.AddRange(type.TupleElementNames);
                    }
                }
                // Always recur into nested types
                return false;
            }
        }

        /// <summary>
        /// Used to generate the dynamic attributes for the required typesymbol.
        /// </summary>
        internal static class DynamicTransformsEncoder
        {
            internal static ImmutableArray<TypedConstant> Encode(TypeSymbol type, RefKind refKind, int customModifiersCount, TypeSymbol booleanType)
            {
                var flagsBuilder = ArrayBuilder<bool>.GetInstance();
                Encode(type, customModifiersCount, refKind, flagsBuilder, addCustomModifierFlags: true);
                Debug.Assert(flagsBuilder.Any());
                Debug.Assert(flagsBuilder.Contains(true));

                var result = flagsBuilder.SelectAsArray((flag, constantType) => new TypedConstant(constantType, TypedConstantKind.Primitive, flag), booleanType);
                flagsBuilder.Free();
                return result;
            }

            internal static ImmutableArray<bool> Encode(TypeSymbol type, RefKind refKind, int customModifiersCount)
            {
                var builder = ArrayBuilder<bool>.GetInstance();
                Encode(type, customModifiersCount, refKind, builder, addCustomModifierFlags: true);
                return builder.ToImmutableAndFree();
            }

            internal static ImmutableArray<bool> EncodeWithoutCustomModifierFlags(TypeSymbol type, RefKind refKind)
            {
                var builder = ArrayBuilder<bool>.GetInstance();
                Encode(type, -1, refKind, builder, addCustomModifierFlags: false);
                return builder.ToImmutableAndFree();
            }

            internal static void Encode(TypeSymbol type, int customModifiersCount, RefKind refKind, ArrayBuilder<bool> transformFlagsBuilder, bool addCustomModifierFlags)
            {
                Debug.Assert(!transformFlagsBuilder.Any());

                if (refKind != RefKind.None)
                {
                    // Native compiler encodes an extra transform flag, always false, for ref/out parameters.
                    transformFlagsBuilder.Add(false);
                }

                if (addCustomModifierFlags)
                {
                    // Native compiler encodes an extra transform flag, always false, for each custom modifier.
                    HandleCustomModifiers(customModifiersCount, transformFlagsBuilder);
                    type.VisitType((typeSymbol, builder, isNested, isContainer) => AddFlags(typeSymbol, builder, isNested, addCustomModifierFlags: true), transformFlagsBuilder);
                }
                else
                {
                    type.VisitType((typeSymbol, builder, isNested, isContainer) => AddFlags(typeSymbol, builder, isNested, addCustomModifierFlags: false), transformFlagsBuilder);
                }
            }

            private static bool AddFlags(TypeSymbol type, ArrayBuilder<bool> transformFlagsBuilder, bool isNestedNamedType, bool addCustomModifierFlags)
            {
                // Encode transforms flag for this type and its custom modifiers (if any).
                switch (type.TypeKind)
                {
                    case TypeKind.Dynamic:
                        transformFlagsBuilder.Add(true);
                        break;

                    case TypeKind.Array:
                        if (addCustomModifierFlags)
                        {
                            HandleCustomModifiers(((ArrayTypeSymbol)type).ElementTypeWithAnnotations.CustomModifiers.Length, transformFlagsBuilder);
                        }

                        transformFlagsBuilder.Add(false);
                        break;

                    case TypeKind.Pointer:
                        if (addCustomModifierFlags)
                        {
                            HandleCustomModifiers(((PointerTypeSymbol)type).PointedAtTypeWithAnnotations.CustomModifiers.Length, transformFlagsBuilder);
                        }

                        transformFlagsBuilder.Add(false);
                        break;

                    case TypeKind.FunctionPointer:
                        Debug.Assert(!isNestedNamedType);
                        handleFunctionPointerType((FunctionPointerTypeSymbol)type, transformFlagsBuilder, addCustomModifierFlags);

                        // Function pointer types have nested custom modifiers and refkinds in line with types, and visit all their nested types
                        // as part of this call.
                        // We need a different way to indicate that we should not recurse for this type, but should continue walking for other
                        // types. https://github.com/dotnet/roslyn/issues/44160
                        return true;

                    default:
                        // Encode transforms flag for this type.
                        // For nested named types, a single flag (false) is encoded for the entire type name, followed by flags for all of the type arguments.
                        // For example, for type "A<T>.B<dynamic>", encoded transform flags are:
                        //      {
                        //          false,  // Type "A.B"
                        //          false,  // Type parameter "T"
                        //          true,   // Type parameter "dynamic"
                        //      }

                        if (!isNestedNamedType)
                        {
                            transformFlagsBuilder.Add(false);
                        }
                        break;
                }

                // Continue walking types
                return false;

                static void handleFunctionPointerType(FunctionPointerTypeSymbol funcPtr, ArrayBuilder<bool> transformFlagsBuilder, bool addCustomModifierFlags)
                {
                    TypePredicate<(ArrayBuilder<bool>, bool)> visitor =
                        (TypeSymbol type, (ArrayBuilder<bool> builder, bool addCustomModifierFlags) param, bool isNestedNamedType, bool isContainer) =>
                            AddFlags(type, param.builder, isNestedNamedType, param.addCustomModifierFlags);

                    // The function pointer type itself gets a false
                    transformFlagsBuilder.Add(false);

                    var sig = funcPtr.Signature;
                    handle(sig.RefKind, sig.RefCustomModifiers, sig.ReturnTypeWithAnnotations);

                    foreach (var param in sig.Parameters)
                    {
                        handle(param.RefKind, param.RefCustomModifiers, param.TypeWithAnnotations);
                    }

                    void handle(RefKind refKind, ImmutableArray<CustomModifier> customModifiers, TypeWithAnnotations twa)
                    {
                        if (addCustomModifierFlags)
                        {
                            HandleCustomModifiers(customModifiers.Length, transformFlagsBuilder);
                        }

                        if (refKind != RefKind.None)
                        {
                            transformFlagsBuilder.Add(false);
                        }

                        if (addCustomModifierFlags)
                        {
                            HandleCustomModifiers(twa.CustomModifiers.Length, transformFlagsBuilder);
                        }

                        twa.Type.VisitType(visitor, (transformFlagsBuilder, addCustomModifierFlags));
                    }
                }
            }

            private static void HandleCustomModifiers(int customModifiersCount, ArrayBuilder<bool> transformFlagsBuilder)
            {
                // Native compiler encodes an extra transforms flag, always false, for each custom modifier.
                transformFlagsBuilder.AddMany(false, customModifiersCount);
            }
        }

        internal static class NativeIntegerTransformsEncoder
        {
            internal static void Encode(ArrayBuilder<bool> builder, TypeSymbol type)
            {
                Debug.Assert(type.ContainingAssembly?.RuntimeSupportsNumericIntPtr != true);
                type.VisitType((typeSymbol, builder, isNested, isContainer) => AddFlags(typeSymbol, builder), builder);
            }

            private static bool AddFlags(TypeSymbol type, ArrayBuilder<bool> builder)
            {
                switch (type.SpecialType)
                {
                    case SpecialType.System_IntPtr:
                    case SpecialType.System_UIntPtr:
                        builder.Add(type.IsNativeIntegerWrapperType);
                        break;
                }
                // Continue walking types
                return false;
            }
        }

        internal class SpecialMembersSignatureComparer : SignatureComparer<MethodSymbol, FieldSymbol, PropertySymbol, TypeSymbol, ParameterSymbol>
        {
            // Fields
            public static readonly SpecialMembersSignatureComparer Instance = new SpecialMembersSignatureComparer();

            // Methods
            protected SpecialMembersSignatureComparer()
            {
            }

            protected override TypeSymbol? GetMDArrayElementType(TypeSymbol type)
            {
                if (type.Kind != SymbolKind.ArrayType)
                {
                    return null;
                }
                ArrayTypeSymbol array = (ArrayTypeSymbol)type;
                if (array.IsSZArray)
                {
                    return null;
                }
                return array.ElementType;
            }

            protected override TypeSymbol GetFieldType(FieldSymbol field)
            {
                return field.Type;
            }

            protected override TypeSymbol GetPropertyType(PropertySymbol property)
            {
                return property.Type;
            }

            protected override TypeSymbol? GetGenericTypeArgument(TypeSymbol type, int argumentIndex)
            {
                if (type.Kind != SymbolKind.NamedType)
                {
                    return null;
                }
                NamedTypeSymbol named = (NamedTypeSymbol)type;
                if (named.Arity <= argumentIndex)
                {
                    return null;
                }
                if ((object)named.ContainingType != null)
                {
                    return null;
                }
                return named.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[argumentIndex].Type;
            }

            protected override TypeSymbol? GetGenericTypeDefinition(TypeSymbol type)
            {
                if (type.Kind != SymbolKind.NamedType)
                {
                    return null;
                }
                NamedTypeSymbol named = (NamedTypeSymbol)type;
                if ((object)named.ContainingType != null)
                {
                    return null;
                }
                if (named.Arity == 0)
                {
                    return null;
                }
                return (NamedTypeSymbol)named.OriginalDefinition;
            }

            protected override ImmutableArray<ParameterSymbol> GetParameters(MethodSymbol method)
            {
                return method.Parameters;
            }

            protected override ImmutableArray<ParameterSymbol> GetParameters(PropertySymbol property)
            {
                return property.Parameters;
            }

            protected override TypeSymbol GetParamType(ParameterSymbol parameter)
            {
                return parameter.Type;
            }

            protected override TypeSymbol? GetPointedToType(TypeSymbol type)
            {
                return type.Kind == SymbolKind.PointerType ? ((PointerTypeSymbol)type).PointedAtType : null;
            }

            protected override TypeSymbol GetReturnType(MethodSymbol method)
            {
                return method.ReturnType;
            }

            protected override TypeSymbol? GetSZArrayElementType(TypeSymbol type)
            {
                if (type.Kind != SymbolKind.ArrayType)
                {
                    return null;
                }
                ArrayTypeSymbol array = (ArrayTypeSymbol)type;
                if (!array.IsSZArray)
                {
                    return null;
                }
                return array.ElementType;
            }

            protected override bool IsByRefParam(ParameterSymbol parameter)
            {
                return parameter.RefKind != RefKind.None;
            }

            protected override bool IsByRefMethod(MethodSymbol method)
            {
                return method.RefKind != RefKind.None;
            }

            protected override bool IsByRefProperty(PropertySymbol property)
            {
                return property.RefKind != RefKind.None;
            }

            protected override bool IsGenericMethodTypeParam(TypeSymbol type, int paramPosition)
            {
                if (type.Kind != SymbolKind.TypeParameter)
                {
                    return false;
                }
                TypeParameterSymbol typeParam = (TypeParameterSymbol)type;
                if (typeParam.ContainingSymbol.Kind != SymbolKind.Method)
                {
                    return false;
                }
                return (typeParam.Ordinal == paramPosition);
            }

            protected override bool IsGenericTypeParam(TypeSymbol type, int paramPosition)
            {
                if (type.Kind != SymbolKind.TypeParameter)
                {
                    return false;
                }
                TypeParameterSymbol typeParam = (TypeParameterSymbol)type;
                if (typeParam.ContainingSymbol.Kind != SymbolKind.NamedType)
                {
                    return false;
                }
                return (typeParam.Ordinal == paramPosition);
            }

            protected override bool MatchArrayRank(TypeSymbol type, int countOfDimensions)
            {
                if (type.Kind != SymbolKind.ArrayType)
                {
                    return false;
                }

                ArrayTypeSymbol array = (ArrayTypeSymbol)type;
                return (array.Rank == countOfDimensions);
            }

            protected override bool MatchTypeToTypeId(TypeSymbol type, int typeId)
            {
                if ((int)type.OriginalDefinition.ExtendedSpecialType == typeId)
                {
                    if (type.IsDefinition)
                    {
                        return true;
                    }

                    return type.Equals(type.OriginalDefinition, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);
                }

                return false;
            }
        }

        internal sealed class WellKnownMembersSignatureComparer : SpecialMembersSignatureComparer
        {
            private readonly CSharpCompilation _compilation;

            public WellKnownMembersSignatureComparer(CSharpCompilation compilation)
            {
                _compilation = compilation;
            }

            protected override bool MatchTypeToTypeId(TypeSymbol type, int typeId)
            {
                WellKnownType wellKnownId = (WellKnownType)typeId;
                if (wellKnownId.IsWellKnownType())
                {
                    return type.Equals(_compilation.GetWellKnownType(wellKnownId), TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);
                }

                return base.MatchTypeToTypeId(type, typeId);
            }
        }
    }
}
