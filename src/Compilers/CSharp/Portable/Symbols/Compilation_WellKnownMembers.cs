// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RuntimeMembers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class CSharpCompilation
    {
        internal readonly WellKnownMembersSignatureComparer WellKnownMemberSignatureComparer;

        /// <summary>
        /// An array of cached well known types available for use in this Compilation.
        /// Lazily filled by GetWellKnownType method.
        /// </summary>
        private NamedTypeSymbol[] _lazyWellKnownTypes;

        /// <summary>
        /// Lazy cache of well known members.
        /// Not yet known value is represented by ErrorTypeSymbol.UnknownResultType
        /// </summary>
        private Symbol[] _lazyWellKnownTypeMembers;

        private bool _usesNullableAttributes;
        private int _needsGeneratedAttributes;
        private bool _needsGeneratedAttributes_IsFrozen;

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
        internal Symbol GetWellKnownTypeMember(WellKnownMember member)
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
                NamedTypeSymbol type = descriptor.DeclaringTypeId <= (int)SpecialType.Count
                                            ? this.GetSpecialType((SpecialType)descriptor.DeclaringTypeId)
                                            : this.GetWellKnownType((WellKnownType)descriptor.DeclaringTypeId);
                Symbol result = null;

                if (!type.IsErrorType())
                {
                    result = GetRuntimeMember(type, ref descriptor, WellKnownMemberSignatureComparer, accessWithinOpt: this.Assembly);
                }

                Interlocked.CompareExchange(ref _lazyWellKnownTypeMembers[(int)member], result, ErrorTypeSymbol.UnknownResultType);
            }

            return _lazyWellKnownTypeMembers[(int)member];
        }

        /// <summary>
        /// This method handles duplicate types in a few different ways:
        /// - for types before C# 7, the first candidate is returned with a warning
        /// - for types after C# 7, the type is considered missing
        /// - in both cases, when BinderFlags.IgnoreCorLibraryDuplicatedTypes is set, any duplicate coming from corlib will be ignored (ie not count as a duplicate)
        /// </summary>
        internal NamedTypeSymbol GetWellKnownType(WellKnownType type)
        {
            Debug.Assert(type.IsValid());

            bool ignoreCorLibraryDuplicatedTypes = this.Options.TopLevelBinderFlags.Includes(BinderFlags.IgnoreCorLibraryDuplicatedTypes);

            int index = (int)type - (int)WellKnownType.First;
            if (_lazyWellKnownTypes == null || (object)_lazyWellKnownTypes[index] == null)
            {
                if (_lazyWellKnownTypes == null)
                {
                    Interlocked.CompareExchange(ref _lazyWellKnownTypes, new NamedTypeSymbol[(int)WellKnownTypes.Count], null);
                }

                string mdName = type.GetMetadataName();
                var warnings = DiagnosticBag.GetInstance();
                NamedTypeSymbol result;
                (AssemblySymbol, AssemblySymbol) conflicts = default;

                if (IsTypeMissing(type))
                {
                    result = null;
                }
                else
                {
                    // well-known types introduced before CSharp7 allow lookup ambiguity and report a warning
                    DiagnosticBag legacyWarnings = (type <= WellKnownType.CSharp7Sentinel) ? warnings : null;
                    result = this.Assembly.GetTypeByMetadataName(
                        mdName, includeReferences: true, useCLSCompliantNameArityEncoding: true, isWellKnownType: true, conflicts: out conflicts,
                        warnings: legacyWarnings, ignoreCorLibraryDuplicatedTypes: ignoreCorLibraryDuplicatedTypes);
                }

                if ((object)result == null)
                {
                    // TODO: should GetTypeByMetadataName rather return a missing symbol?
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

                        result = new MissingMetadataTypeSymbol.TopLevelWithCustomErrorInfo(this.Assembly.Modules[0], ref emittedName, errorInfo, type);
                    }
                    else
                    {
                        result = new MissingMetadataTypeSymbol.TopLevel(this.Assembly.Modules[0], ref emittedName, type);
                    }
                }

                if ((object)Interlocked.CompareExchange(ref _lazyWellKnownTypes[index], result, null) != null)
                {
                    Debug.Assert(
                        TypeSymbol.Equals(result, _lazyWellKnownTypes[index], TypeCompareKind.ConsiderEverything2) || (_lazyWellKnownTypes[index].IsErrorType() && result.IsErrorType())
                    );
                }
                else
                {
                    AdditionalCodegenWarnings.AddRange(warnings);
                }

                warnings.Free();
            }

            return _lazyWellKnownTypes[index];
        }

        internal bool IsAttributeType(TypeSymbol type)
        {
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            return IsEqualOrDerivedFromWellKnownClass(type, WellKnownType.System_Attribute, ref useSiteDiagnostics);
        }

        internal override bool IsAttributeType(ITypeSymbol type)
        {
            return IsAttributeType((TypeSymbol)type);
        }

        internal bool IsExceptionType(TypeSymbol type, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            return IsEqualOrDerivedFromWellKnownClass(type, WellKnownType.System_Exception, ref useSiteDiagnostics);
        }

        internal bool IsReadOnlySpanType(TypeSymbol type)
        {
            return TypeSymbol.Equals(type.OriginalDefinition, GetWellKnownType(WellKnownType.System_ReadOnlySpan_T), TypeCompareKind.ConsiderEverything2);
        }

        internal bool IsEqualOrDerivedFromWellKnownClass(TypeSymbol type, WellKnownType wellKnownType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(wellKnownType == WellKnownType.System_Attribute ||
                         wellKnownType == WellKnownType.System_Exception);

            if (type.Kind != SymbolKind.NamedType || type.TypeKind != TypeKind.Class)
            {
                return false;
            }

            var wkType = GetWellKnownType(wellKnownType);
            return type.Equals(wkType, TypeCompareKind.ConsiderEverything) || type.IsDerivedFrom(wkType, TypeCompareKind.ConsiderEverything, useSiteDiagnostics: ref useSiteDiagnostics);
        }

        internal override bool IsSystemTypeReference(ITypeSymbol type)
        {
            return TypeSymbol.Equals((TypeSymbol)type, GetWellKnownType(WellKnownType.System_Type), TypeCompareKind.ConsiderEverything2);
        }

        internal override ISymbol CommonGetWellKnownTypeMember(WellKnownMember member)
        {
            return GetWellKnownTypeMember(member);
        }

        internal override ITypeSymbol CommonGetWellKnownType(WellKnownType wellknownType)
        {
            return GetWellKnownType(wellknownType);
        }

        internal static Symbol GetRuntimeMember(NamedTypeSymbol declaringType, ref MemberDescriptor descriptor, SignatureComparer<MethodSymbol, FieldSymbol, PropertySymbol, TypeSymbol, ParameterSymbol> comparer, AssemblySymbol accessWithinOpt)
        {
            Symbol result = null;
            SymbolKind targetSymbolKind;
            MethodKind targetMethodKind = MethodKind.Ordinary;
            bool isStatic = (descriptor.Flags & MemberFlags.Static) != 0;

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

            foreach (var member in declaringType.GetMembers(descriptor.Name))
            {
                Debug.Assert(member.Name.Equals(descriptor.Name));

                if (member.Kind != targetSymbolKind || member.IsStatic != isStatic ||
                    !(member.DeclaredAccessibility == Accessibility.Public || ((object)accessWithinOpt != null && Symbol.IsSymbolAccessible(member, accessWithinOpt))))
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
                if ((object)result != null)
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
        internal SynthesizedAttributeData TrySynthesizeAttribute(
            WellKnownMember constructor,
            ImmutableArray<TypedConstant> arguments = default(ImmutableArray<TypedConstant>),
            ImmutableArray<KeyValuePair<WellKnownMember, TypedConstant>> namedArguments = default(ImmutableArray<KeyValuePair<WellKnownMember, TypedConstant>>),
            bool isOptionalUse = false)
        {
            DiagnosticInfo diagnosticInfo;
            var ctorSymbol = (MethodSymbol)Binder.GetWellKnownTypeMember(this, constructor, out diagnosticInfo, isOptional: true);

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
                    var wellKnownMember = Binder.GetWellKnownTypeMember(this, arg.Key, out diagnosticInfo, isOptional: true);
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

            return new SynthesizedAttributeData(ctorSymbol, arguments, namedStringArguments);
        }

        internal SynthesizedAttributeData SynthesizeDecimalConstantAttribute(decimal value)
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

        internal SynthesizedAttributeData SynthesizeDebuggerBrowsableNeverAttribute()
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

        internal SynthesizedAttributeData SynthesizeDebuggerStepThroughAttribute()
        {
            if (Options.OptimizationLevel != OptimizationLevel.Debug)
            {
                return null;
            }

            return TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_DebuggerStepThroughAttribute__ctor);
        }

        private void EnsureEmbeddableAttributeExists(EmbeddableAttributes attribute, DiagnosticBag diagnostics, Location location, bool modifyCompilation)
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

        internal void EnsureIsReadOnlyAttributeExists(DiagnosticBag diagnostics, Location location, bool modifyCompilation)
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.IsReadOnlyAttribute, diagnostics, location, modifyCompilation);
        }

        internal void EnsureIsByRefLikeAttributeExists(DiagnosticBag diagnostics, Location location, bool modifyCompilation)
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.IsByRefLikeAttribute, diagnostics, location, modifyCompilation);
        }

        internal void EnsureIsUnmanagedAttributeExists(DiagnosticBag diagnostics, Location location, bool modifyCompilation)
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.IsUnmanagedAttribute, diagnostics, location, modifyCompilation);
        }

        internal void EnsureNullableAttributeExists(DiagnosticBag diagnostics, Location location, bool modifyCompilation)
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.NullableAttribute, diagnostics, location, modifyCompilation);
        }

        internal void EnsureNullableContextAttributeExists(DiagnosticBag diagnostics, Location location, bool modifyCompilation)
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.NullableContextAttribute, diagnostics, location, modifyCompilation);
        }

        internal bool CheckIfAttributeShouldBeEmbedded(EmbeddableAttributes attribute, DiagnosticBag diagnosticsOpt, Location locationOpt)
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
                    // Note: if the type exists, we'll check both constructors, regardless of which one(s) we'll eventually need
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

                default:
                    throw ExceptionUtilities.UnexpectedValue(attribute);
            }
        }

        private bool CheckIfAttributeShouldBeEmbedded(DiagnosticBag diagnosticsOpt, Location locationOpt, WellKnownType attributeType, WellKnownMember attributeCtor, WellKnownMember? secondAttributeCtor = null)
        {
            var userDefinedAttribute = GetWellKnownType(attributeType);

            if (userDefinedAttribute is MissingMetadataTypeSymbol)
            {
                if (Options.OutputKind == OutputKind.NetModule)
                {
                    if (diagnosticsOpt != null)
                    {
                        var errorReported = Binder.ReportUseSiteDiagnostics(userDefinedAttribute, diagnosticsOpt, locationOpt);
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
                var member = Binder.GetWellKnownTypeMember(this, attributeCtor, diagnosticsOpt, locationOpt);
                if (member != null && secondAttributeCtor != null)
                {
                    Binder.GetWellKnownTypeMember(this, secondAttributeCtor.Value, diagnosticsOpt, locationOpt);
                }
            }

            return false;
        }

        internal SynthesizedAttributeData SynthesizeDebuggableAttribute()
        {
            TypeSymbol debuggableAttribute = GetWellKnownType(WellKnownType.System_Diagnostics_DebuggableAttribute);
            Debug.Assert((object)debuggableAttribute != null, "GetWellKnownType unexpectedly returned null");
            if (debuggableAttribute is MissingMetadataTypeSymbol)
            {
                return null;
            }

            TypeSymbol debuggingModesType = GetWellKnownType(WellKnownType.System_Diagnostics_DebuggableAttribute__DebuggingModes);
            Debug.Assert((object)debuggingModesType != null, "GetWellKnownType unexpectedly returned null");
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
            var ignoreSymbolStoreDebuggingMode = (FieldSymbol)GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggableAttribute_DebuggingModes__IgnoreSymbolStoreSequencePoints);
            if ((object)ignoreSymbolStoreDebuggingMode == null || !ignoreSymbolStoreDebuggingMode.HasConstantValue)
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
                var defaultDebuggingMode = (FieldSymbol)GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggableAttribute_DebuggingModes__Default);
                if ((object)defaultDebuggingMode == null || !defaultDebuggingMode.HasConstantValue)
                {
                    return null;
                }

                var disableOptimizationsDebuggingMode = (FieldSymbol)GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggableAttribute_DebuggingModes__DisableOptimizations);
                if ((object)disableOptimizationsDebuggingMode == null || !disableOptimizationsDebuggingMode.HasConstantValue)
                {
                    return null;
                }

                constantVal |= defaultDebuggingMode.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false).Int32Value;
                constantVal |= disableOptimizationsDebuggingMode.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false).Int32Value;
            }

            if (_options.EnableEditAndContinue)
            {
                var enableEncDebuggingMode = (FieldSymbol)GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggableAttribute_DebuggingModes__EnableEditAndContinue);
                if ((object)enableEncDebuggingMode == null || !enableEncDebuggingMode.HasConstantValue)
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
        internal SynthesizedAttributeData SynthesizeDynamicAttribute(TypeSymbol type, int customModifiersCount, RefKind refKindOpt = RefKind.None)
        {
            Debug.Assert((object)type != null);
            Debug.Assert(type.ContainsDynamic());

            if (type.IsDynamic() && refKindOpt == RefKind.None && customModifiersCount == 0)
            {
                return TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_DynamicAttribute__ctor);
            }
            else
            {
                NamedTypeSymbol booleanType = GetSpecialType(SpecialType.System_Boolean);
                Debug.Assert((object)booleanType != null);
                var transformFlags = DynamicTransformsEncoder.Encode(type, refKindOpt, customModifiersCount, booleanType);
                var boolArray = ArrayTypeSymbol.CreateSZArray(booleanType.ContainingAssembly, TypeWithAnnotations.Create(booleanType));
                var arguments = ImmutableArray.Create<TypedConstant>(new TypedConstant(boolArray, transformFlags));
                return TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_DynamicAttribute__ctorTransformFlags, arguments);
            }
        }

        internal SynthesizedAttributeData SynthesizeTupleNamesAttribute(TypeSymbol type)
        {
            Debug.Assert((object)type != null);
            Debug.Assert(type.ContainsTuple());

            var stringType = GetSpecialType(SpecialType.System_String);
            Debug.Assert((object)stringType != null);
            var names = TupleNamesEncoder.Encode(type, stringType);

            Debug.Assert(!names.IsDefault, "should not need the attribute when no tuple names");

            var stringArray = ArrayTypeSymbol.CreateSZArray(stringType.ContainingAssembly, TypeWithAnnotations.Create(stringType));
            var args = ImmutableArray.Create(new TypedConstant(stringArray, names));
            return TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_TupleElementNamesAttribute__ctorTransformNames, args);
        }

        internal SynthesizedAttributeData SynthesizeAttributeUsageAttribute(AttributeTargets targets, bool allowMultiple, bool inherited)
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
            public static ImmutableArray<string> Encode(TypeSymbol type)
            {
                var namesBuilder = ArrayBuilder<string>.GetInstance();

                if (!TryGetNames(type, namesBuilder))
                {
                    namesBuilder.Free();
                    return default(ImmutableArray<string>);
                }

                return namesBuilder.ToImmutableAndFree();
            }

            public static ImmutableArray<TypedConstant> Encode(TypeSymbol type, TypeSymbol stringType)
            {
                var namesBuilder = ArrayBuilder<string>.GetInstance();

                if (!TryGetNames(type, namesBuilder))
                {
                    namesBuilder.Free();
                    return default(ImmutableArray<TypedConstant>);
                }

                var names = namesBuilder.SelectAsArray((name, constantType) =>
                    new TypedConstant(constantType, TypedConstantKind.Primitive, name), stringType);
                namesBuilder.Free();
                return names;
            }

            internal static bool TryGetNames(TypeSymbol type, ArrayBuilder<string> namesBuilder)
            {
                type.VisitType((t, builder, _ignore) => AddNames(t, builder), namesBuilder);
                return namesBuilder.Any(name => name != null);
            }

            private static bool AddNames(TypeSymbol type, ArrayBuilder<string> namesBuilder)
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
                    type.VisitType((typeSymbol, builder, isNested) => AddFlags(typeSymbol, builder, isNested, addCustomModifierFlags: true), transformFlagsBuilder);
                }
                else
                {
                    type.VisitType((typeSymbol, builder, isNested) => AddFlags(typeSymbol, builder, isNested, addCustomModifierFlags: false), transformFlagsBuilder);
                }
            }

            private static bool AddFlags(TypeSymbol type, ArrayBuilder<bool> transformFlagsBuilder, bool isNestedNamedType, bool addCustomModifierFlags)
            {
                // Encode transforms flag for this type and it's custom modifiers (if any).
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
            }

            private static void HandleCustomModifiers(int customModifiersCount, ArrayBuilder<bool> transformFlagsBuilder)
            {
                for (int i = 0; i < customModifiersCount; i++)
                {
                    // Native compiler encodes an extra transforms flag, always false, for each custom modifier.
                    transformFlagsBuilder.Add(false);
                }
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

            protected override TypeSymbol GetMDArrayElementType(TypeSymbol type)
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

            protected override TypeSymbol GetGenericTypeArgument(TypeSymbol type, int argumentIndex)
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

            protected override TypeSymbol GetGenericTypeDefinition(TypeSymbol type)
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

            protected override TypeSymbol GetPointedToType(TypeSymbol type)
            {
                return type.Kind == SymbolKind.PointerType ? ((PointerTypeSymbol)type).PointedAtType : null;
            }

            protected override TypeSymbol GetReturnType(MethodSymbol method)
            {
                return method.ReturnType;
            }

            protected override TypeSymbol GetSZArrayElementType(TypeSymbol type)
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
                if ((int)type.OriginalDefinition.SpecialType == typeId)
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
