// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.RuntimeMembers;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    // TODO: (tomat) translated 1:1 from VB, might need adjustments
    // TODO: (tomat) can we share more with VB?

    partial class CSharpCompilation
    {
        private readonly WellKnownMembersSignatureComparer wellKnownMemberSignatureComparer;

        /// <summary>
        /// An array of cached well known types available for use in this Compilation.
        /// Lazily filled by GetWellKnownType method.
        /// </summary>
        private NamedTypeSymbol[] lazyWellKnownTypes;

        /// <summary>
        /// Lazy cache of well known members.
        /// Not yet known value is represented by ErrorTypeSymbol.UnknownResultType
        /// </summary>
        private Symbol[] lazyWellKnownTypeMembers;

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

            if (lazyWellKnownTypeMembers == null || ReferenceEquals(lazyWellKnownTypeMembers[(int)member], ErrorTypeSymbol.UnknownResultType))
            {
                if (lazyWellKnownTypeMembers == null)
                {
                    var wellKnownTypeMembers = new Symbol[(int)WellKnownMember.Count];

                    for (int i = 0; i < wellKnownTypeMembers.Length; i++)
                    {
                        wellKnownTypeMembers[i] = ErrorTypeSymbol.UnknownResultType;
                    }

                    Interlocked.CompareExchange(ref lazyWellKnownTypeMembers, wellKnownTypeMembers, null);
                }

                MemberDescriptor descriptor = WellKnownMembers.GetDescriptor(member);
                NamedTypeSymbol type = descriptor.DeclaringTypeId <= (int)SpecialType.Count
                                            ? this.GetSpecialType((SpecialType)descriptor.DeclaringTypeId)
                                            : this.GetWellKnownType((WellKnownType)descriptor.DeclaringTypeId);
                Symbol result = null;

                if (!type.IsErrorType())
                {
                    result = GetRuntimeMember(type, ref descriptor, wellKnownMemberSignatureComparer, accessWithinOpt: this.Assembly);
                }

                Interlocked.CompareExchange(ref lazyWellKnownTypeMembers[(int)member], result, ErrorTypeSymbol.UnknownResultType);
            }

            return lazyWellKnownTypeMembers[(int)member];
        }

        internal NamedTypeSymbol GetWellKnownType(WellKnownType type)
        {
            Debug.Assert(type >= WellKnownType.First && type <= WellKnownType.Last);

            int index = (int)type - (int)WellKnownType.First;
            if (lazyWellKnownTypes == null || (object)lazyWellKnownTypes[index] == null)
            {
                if (lazyWellKnownTypes == null)
                {
                    Interlocked.CompareExchange(ref lazyWellKnownTypes, new NamedTypeSymbol[(int)WellKnownTypes.Count], null);
                }

                string mdName = type.GetMetadataName();
                var warnings = DiagnosticBag.GetInstance();
                NamedTypeSymbol result = this.Assembly.GetTypeByMetadataName(
                    mdName, includeReferences: true, useCLSCompliantNameArityEncoding: true, isWellKnownType: true, warnings: warnings);

                if ((object)result == null)
                {
                    // TODO: should GetTypeByMetadataName rather return a missing symbol?
                    MetadataTypeName emittedName = MetadataTypeName.FromFullName(mdName, useCLSCompliantNameArityEncoding: true);
                    result = new MissingMetadataTypeSymbol.TopLevel(this.Assembly.Modules[0], ref emittedName, type);
                }

                if ((object)Interlocked.CompareExchange(ref lazyWellKnownTypes[index], result, null) != null)
                {
                    Debug.Assert(
                        result == lazyWellKnownTypes[index] || (lazyWellKnownTypes[index].IsErrorType() && result.IsErrorType())
                    );
                }
                else
                {
                    AdditionalCodegenWarnings.AddRange(warnings);
                }

                warnings.Free();
            }

            return lazyWellKnownTypes[index];
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

        internal bool IsExceptionType(TypeSymbol type)
        {
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
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
        /// Returns null if the <paramref name="constructor"/> symbol is missing and the attribute is synthesized only if present.
        /// </summary>
        internal SynthesizedAttributeData SynthesizeAttribute(
            WellKnownMember constructor,
            ImmutableArray<TypedConstant> arguments = default(ImmutableArray<TypedConstant>),
            ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments = default(ImmutableArray<KeyValuePair<string, TypedConstant>>))
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

            if (namedArguments.IsDefault)
            {
                namedArguments = ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty;
            }

            return new SynthesizedAttributeData(ctorSymbol, arguments, namedArguments);
        }

        internal SynthesizedAttributeData SynthesizeDecimalConstantAttribute(decimal value)
        {
            var decimalBits = new ConstantValueUtils.DecimalValue(value);
            var systemByte = GetSpecialType(SpecialType.System_Byte);
            Debug.Assert(!systemByte.HasUseSiteError);

            var systemUnit32 = GetSpecialType(SpecialType.System_UInt32);
            Debug.Assert(!systemUnit32.HasUseSiteError);

            return SynthesizeAttribute(
                WellKnownMember.System_Runtime_CompilerServices_DecimalConstantAttribute__ctor,
                ImmutableArray.Create(
                    new TypedConstant(systemByte, TypedConstantKind.Primitive, decimalBits.Scale),
                    new TypedConstant(systemByte, TypedConstantKind.Primitive, decimalBits.IsNegative ? (byte)128 : (byte)0),
                    new TypedConstant(systemUnit32, TypedConstantKind.Primitive, decimalBits.High),
                    new TypedConstant(systemUnit32, TypedConstantKind.Primitive, decimalBits.Mid),
                    new TypedConstant(systemUnit32, TypedConstantKind.Primitive, decimalBits.Low)
                ));
        }

        internal SynthesizedAttributeData SynthesizeDebuggerBrowsableNeverAttribute()
        {
            if (Options.DebugInformationKind != DebugInformationKind.Full)
            {
                return null;
            }

            return SynthesizeAttribute(WellKnownMember.System_Diagnostics_DebuggerBrowsableAttribute__ctor,
                   ImmutableArray.Create(new TypedConstant(
                       GetWellKnownType(WellKnownType.System_Diagnostics_DebuggerBrowsableState),
                       TypedConstantKind.Enum,
                       DebuggerBrowsableState.Never)));
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

            var defaulDebuggingMode = (FieldSymbol)GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggableAttribute_DebuggingModes__Default);
            if ((object)defaulDebuggingMode == null || !defaulDebuggingMode.HasConstantValue)
            {
                return null;
            }

            var disableOptsDebuggingMode = (FieldSymbol)GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggableAttribute_DebuggingModes__DisableOptimizations);
            if ((object)disableOptsDebuggingMode == null || !disableOptsDebuggingMode.HasConstantValue)
            {
                return null;
            }

            var enableENCDebuggingMode = (FieldSymbol)GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggableAttribute_DebuggingModes__EnableEditAndContinue);
            if ((object)enableENCDebuggingMode == null || !enableENCDebuggingMode.HasConstantValue)
            {
                return null;
            }

            var ignorePDBDebuggingMode = (FieldSymbol)GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggableAttribute_DebuggingModes__IgnoreSymbolStoreSequencePoints);
            if ((object)ignorePDBDebuggingMode == null || !ignorePDBDebuggingMode.HasConstantValue)
            {
                return null;
            }

            Debug.Assert(options.DebugInformationKind.IsValid() && options.DebugInformationKind != DebugInformationKind.None);

            bool emittingFullDebugInfo = options.DebugInformationKind == DebugInformationKind.Full;
            bool optimizationsDisabled = !options.Optimize;

            int constantVal = ignorePDBDebuggingMode.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false).Int32Value;

            if (emittingFullDebugInfo)
            {
                constantVal |= defaulDebuggingMode.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false).Int32Value;
            }

            if (optimizationsDisabled)
            {
                constantVal |= disableOptsDebuggingMode.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false).Int32Value;
            }

            if (options.EnableEditAndContinue)
            {
                constantVal |= enableENCDebuggingMode.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false).Int32Value;
            }

            var typedConstantDebugMode = new TypedConstant(debuggingModesType, TypedConstantKind.Enum, constantVal);

            return SynthesizeAttribute(
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
                return SynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_DynamicAttribute__ctor);
            }
            else
            {
                NamedTypeSymbol booleanType = GetSpecialType(SpecialType.System_Boolean);
                Debug.Assert((object)booleanType != null);
                var transformFlags = DynamicTransformsEncoder.Encode(type, booleanType, customModifiersCount, refKindOpt);
                var boolArray = new ArrayTypeSymbol(booleanType.ContainingAssembly, booleanType, customModifiers: ImmutableArray<CustomModifier>.Empty);
                var arguments = ImmutableArray.Create<TypedConstant>(new TypedConstant(boolArray, transformFlags));
                return SynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_DynamicAttribute__ctorTransformFlags, arguments);
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

                type.VisitType(EncodeDynamicTransform, transformFlagsBuilder);
            }

            private static readonly Func<TypeSymbol, ArrayBuilder<bool>, bool, bool> EncodeDynamicTransform = (type, transformFlagsBuilder, isNestedNamedType) =>
            {
                // Encode transforms flag for this type and it's custom modifiers (if any).
                switch (type.TypeKind)
                {
                    case TypeKind.DynamicType:
                        transformFlagsBuilder.Add(true);
                        break;

                    case TypeKind.ArrayType:
                        HandleCustomModifiers(((ArrayTypeSymbol)type).CustomModifiers.Length, transformFlagsBuilder);
                        transformFlagsBuilder.Add(false);
                        break;

                    case TypeKind.PointerType:
                        HandleCustomModifiers(((PointerTypeSymbol)type).CustomModifiers.Length, transformFlagsBuilder);
                        transformFlagsBuilder.Add(false);
                        break;

                    default:
                        // Encode transforms flag for this type.
                        // For nested named types, a single flag (false) is encoded for the entire type name, followed by flags for all of the type arguments.
                        // For example, for type "A<T>.B<dynamic>", encoded transform flags are:
                        //      {
                        //          false,  // Type "A.B"
                        //          false,  // Type parameter "T"
                        //          true,   // Type parmeter "dynamic"
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

            protected override TypeSymbol GetArrayElementType(TypeSymbol type)
            {
                if (type.Kind != SymbolKind.ArrayType)
                {
                    return null;
                }
                ArrayTypeSymbol array = (ArrayTypeSymbol)type;
                if (array.Rank < 2)
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
                return named.TypeArgumentsNoUseSiteDiagnostics[argumentIndex];
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
                if (array.Rank != 1)
                {
                    return null;
                }
                return array.ElementType;
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
                if (countOfDimensions == 1)
                {
                    return false;
                }
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
            private readonly CSharpCompilation compilation;

            public WellKnownMembersSignatureComparer(CSharpCompilation compilation)
            {
                this.compilation = compilation;
            }

            protected override bool MatchTypeToTypeId(TypeSymbol type, int typeId)
            {
                WellKnownType wellKnownId = (WellKnownType)typeId;
                if (wellKnownId >= WellKnownType.First && wellKnownId <= WellKnownType.Last)
                {
                    return (type == this.compilation.GetWellKnownType(wellKnownId));
                }

                return base.MatchTypeToTypeId(type, typeId);
            }
        }
    }
}
