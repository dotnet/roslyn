// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.RuntimeMembers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    // TODO: (tomat) translated 1:1 from VB, might need adjustments
    // TODO: (tomat) can we share more with VB?

    public partial class CSharpCompilation
    {
        private readonly WellKnownMembersSignatureComparer _wellKnownMemberSignatureComparer;

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
                    result = GetRuntimeMember(type, ref descriptor, _wellKnownMemberSignatureComparer, accessWithinOpt: this.Assembly);
                }

                Interlocked.CompareExchange(ref _lazyWellKnownTypeMembers[(int)member], result, ErrorTypeSymbol.UnknownResultType);
            }

            return _lazyWellKnownTypeMembers[(int)member];
        }

        internal NamedTypeSymbol GetWellKnownType(WellKnownType type)
        {
            Debug.Assert(type >= WellKnownType.First && type <= WellKnownType.Last);

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

                if (IsTypeMissing(type))
                {
                    result = null;
                }
                else
                {
                    result = this.Assembly.GetTypeByMetadataName(
                        mdName, includeReferences: true, useCLSCompliantNameArityEncoding: true, isWellKnownType: true, warnings: warnings);
                }

                if ((object)result == null)
                {
                    // TODO: should GetTypeByMetadataName rather return a missing symbol?
                    MetadataTypeName emittedName = MetadataTypeName.FromFullName(mdName, useCLSCompliantNameArityEncoding: true);
                    result = new MissingMetadataTypeSymbol.TopLevel(this.Assembly.Modules[0], ref emittedName, type);
                }

                if ((object)Interlocked.CompareExchange(ref _lazyWellKnownTypes[index], result, null) != null)
                {
                    Debug.Assert(
                        result == _lazyWellKnownTypes[index] || (_lazyWellKnownTypes[index].IsErrorType() && result.IsErrorType())
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

        internal bool IsEqualOrDerivedFromWellKnownClass(TypeSymbol type, WellKnownType wellKnownType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(wellKnownType == WellKnownType.System_Attribute ||
                         wellKnownType == WellKnownType.System_Exception);

            if (type.Kind != SymbolKind.NamedType || type.TypeKind != TypeKind.Class)
            {
                return false;
            }

            var wkType = GetWellKnownType(wellKnownType);
            return type.Equals(wkType, ignoreDynamic: false) || type.IsDerivedFrom(wkType, ignoreDynamic: false, useSiteDiagnostics: ref useSiteDiagnostics);
        }

        internal override bool IsSystemTypeReference(ITypeSymbol type)
        {
            return (TypeSymbol)type == GetWellKnownType(WellKnownType.System_Type);
        }

        internal override ISymbol CommonGetWellKnownTypeMember(WellKnownMember member)
        {
            return GetWellKnownTypeMember(member);
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
        internal SynthesizedAttributeData TrySynthesizeAttribute(
            WellKnownMember constructor,
            ImmutableArray<TypedConstant> arguments = default(ImmutableArray<TypedConstant>),
            ImmutableArray<KeyValuePair<WellKnownMember, TypedConstant>> namedArguments = default(ImmutableArray<KeyValuePair<WellKnownMember, TypedConstant>>))
        {
            var ctorSymbol = (MethodSymbol)GetWellKnownTypeMember(constructor);
            if ((object)ctorSymbol == null)
            {
                // if this assert fails, UseSiteErrors for "member" have not been checked before emitting ...
                Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(constructor));
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
                    var wellKnownMember = GetWellKnownTypeMember(arg.Key);
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
                var transformFlags = DynamicTransformsEncoder.Encode(type, booleanType, customModifiersCount, refKindOpt);
                var boolArray = ArrayTypeSymbol.CreateSZArray(booleanType.ContainingAssembly, TypeSymbolWithAnnotations.Create(booleanType));
                var arguments = ImmutableArray.Create<TypedConstant>(new TypedConstant(boolArray, transformFlags));
                return TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_DynamicAttribute__ctorTransformFlags, arguments);
            }
        }

        /// <summary>
        /// Used to generate the dynamic attributes for the required typesymbol.
        /// </summary>
        internal static class DynamicTransformsEncoder
        {
            internal static ImmutableArray<TypedConstant> Encode(TypeSymbol type, TypeSymbol booleanType, int customModifiersCount, RefKind refKind)
            {
                var flagsBuilder = ArrayBuilder<bool>.GetInstance();
                EncodeInternal(type, customModifiersCount, refKind, flagsBuilder);
                Debug.Assert(flagsBuilder.Any());
                Debug.Assert(flagsBuilder.Contains(true));

                var constantsBuilder = ArrayBuilder<TypedConstant>.GetInstance(flagsBuilder.Count);
                foreach (bool flag in flagsBuilder)
                {
                    constantsBuilder.Add(new TypedConstant(booleanType, TypedConstantKind.Primitive, flag));
                }

                flagsBuilder.Free();
                return constantsBuilder.ToImmutableAndFree();
            }

            internal static ImmutableArray<bool> Encode(TypeSymbol type, int customModifiersCount, RefKind refKind)
            {
                var transformFlagsBuilder = ArrayBuilder<bool>.GetInstance();
                EncodeInternal(type, customModifiersCount, refKind, transformFlagsBuilder);
                return transformFlagsBuilder.ToImmutableAndFree();
            }

            internal static void EncodeInternal(TypeSymbol type, int customModifiersCount, RefKind refKind, ArrayBuilder<bool> transformFlagsBuilder)
            {
                Debug.Assert(!transformFlagsBuilder.Any());

                if (refKind != RefKind.None)
                {
                    // Native compiler encodes an extra transform flag, always false, for ref/out parameters.
                    transformFlagsBuilder.Add(false);
                }

                // Native compiler encodes an extra transform flag, always false, for each custom modifier.
                HandleCustomModifiers(customModifiersCount, transformFlagsBuilder);

                type.VisitType(s_encodeDynamicTransform, transformFlagsBuilder);
            }

            private static readonly Func<TypeSymbol, ArrayBuilder<bool>, bool, bool> s_encodeDynamicTransform = (type, transformFlagsBuilder, isNestedNamedType) =>
            {
                // Encode transforms flag for this type and it's custom modifiers (if any).
                switch (type.TypeKind)
                {
                    case TypeKind.Dynamic:
                        transformFlagsBuilder.Add(true);
                        break;

                    case TypeKind.Array:
                        HandleCustomModifiers(((ArrayTypeSymbol)type).ElementType.CustomModifiers.Length, transformFlagsBuilder);
                        transformFlagsBuilder.Add(false);
                        break;

                    case TypeKind.Pointer:
                        HandleCustomModifiers(((PointerTypeSymbol)type).PointedAtType.CustomModifiers.Length, transformFlagsBuilder);
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
            };

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
                return array.ElementType.TypeSymbol;
            }

            protected override TypeSymbol GetFieldType(FieldSymbol field)
            {
                return field.Type.TypeSymbol;
            }

            protected override TypeSymbol GetPropertyType(PropertySymbol property)
            {
                return property.Type.TypeSymbol;
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
                return named.TypeArgumentsNoUseSiteDiagnostics[argumentIndex].TypeSymbol;
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
                return parameter.Type.TypeSymbol;
            }

            protected override TypeSymbol GetPointedToType(TypeSymbol type)
            {
                return type.Kind == SymbolKind.PointerType ? ((PointerTypeSymbol)type).PointedAtType.TypeSymbol : null;
            }

            protected override TypeSymbol GetReturnType(MethodSymbol method)
            {
                return method.ReturnType.TypeSymbol;
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
                return array.ElementType.TypeSymbol;
            }

            protected override bool IsByRefParam(ParameterSymbol parameter)
            {
                return parameter.RefKind != RefKind.None;
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
                return (int)type.SpecialType == typeId;
            }
        }

        private class WellKnownMembersSignatureComparer : SpecialMembersSignatureComparer
        {
            private readonly CSharpCompilation _compilation;

            public WellKnownMembersSignatureComparer(CSharpCompilation compilation)
            {
                _compilation = compilation;
            }

            protected override bool MatchTypeToTypeId(TypeSymbol type, int typeId)
            {
                WellKnownType wellKnownId = (WellKnownType)typeId;
                if (wellKnownId >= WellKnownType.First && wellKnownId <= WellKnownType.Last)
                {
                    return (type == _compilation.GetWellKnownType(wellKnownId));
                }

                return base.MatchTypeToTypeId(type, typeId);
            }
        }
    }
}
