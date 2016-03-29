// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// This subclass of MetadataDecoder is specifically for finding
    /// method symbols corresponding to method MemberRefs.  The parent 
    /// implementation is unsuitable because it requires a PEMethodSymbol
    /// for context when decoding method type parameters and no such
    /// context is available because it is precisely what we are trying
    /// to find.  Since we know in advance that there will be no context
    /// and that signatures decoded with this class will only be used
    /// for comparison (when searching through the methods of a known
    /// TypeSymbol), we can return indexed type parameters instead.
    /// </summary>
    internal sealed class MemberRefMetadataDecoder : MetadataDecoder
    {
        /// <summary>
        /// Type context for resolving generic type arguments.
        /// </summary>
        private readonly TypeSymbol _containingType;

        public MemberRefMetadataDecoder(
            PEModuleSymbol moduleSymbol,
            TypeSymbol containingType) :
            base(moduleSymbol, containingType as PENamedTypeSymbol)
        {
            Debug.Assert((object)containingType != null);
            _containingType = containingType;
        }

        /// <summary>
        /// We know that we'll never have a method context because that's what we're
        /// trying to find.  Instead, just return an indexed type parameter that will
        /// make comparison easier.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        protected override TypeSymbol GetGenericMethodTypeParamSymbol(int position)
        {
            // Note: technically this is a source symbol, but we only care about the position
            return IndexedTypeParameterSymbol.GetTypeParameter(position);
        }

        /// <summary>
        /// This override changes two things:
        ///     1) Return type arguments instead of type parameters.
        ///     2) Handle non-PE types.
        /// </summary>
        protected override TypeSymbol GetGenericTypeParamSymbol(int position)
        {
            PENamedTypeSymbol peType = _containingType as PENamedTypeSymbol;
            if ((object)peType != null)
            {
                while ((object)peType != null && (peType.MetadataArity - peType.Arity) > position)
                {
                    peType = peType.ContainingSymbol as PENamedTypeSymbol;
                }

                if ((object)peType == null || peType.MetadataArity <= position)
                {
                    return new UnsupportedMetadataTypeSymbol(); // position of type parameter too large
                }

                position -= peType.MetadataArity - peType.Arity;
                Debug.Assert(position >= 0 && position < peType.Arity);

                return peType.TypeArgumentsNoUseSiteDiagnostics[position].TypeSymbol; //NB: args, not params
            }

            NamedTypeSymbol namedType = _containingType as NamedTypeSymbol;
            if ((object)namedType != null)
            {
                int cumulativeArity;
                TypeSymbol typeArgument;
                GetGenericTypeArgumentSymbol(position, namedType, out cumulativeArity, out typeArgument);
                if ((object)typeArgument != null)
                {
                    return typeArgument;
                }
                else
                {
                    Debug.Assert(cumulativeArity <= position);
                    return new UnsupportedMetadataTypeSymbol(); // position of type parameter too large
                }
            }

            return new UnsupportedMetadataTypeSymbol(); // associated type does not have type parameters
        }

        private static void GetGenericTypeArgumentSymbol(int position, NamedTypeSymbol namedType, out int cumulativeArity, out TypeSymbol typeArgument)
        {
            cumulativeArity = namedType.Arity;
            typeArgument = null;

            int arityOffset = 0;

            var containingType = namedType.ContainingType;
            if ((object)containingType != null)
            {
                int containingTypeCumulativeArity;
                GetGenericTypeArgumentSymbol(position, containingType, out containingTypeCumulativeArity, out typeArgument);
                cumulativeArity += containingTypeCumulativeArity;
                arityOffset = containingTypeCumulativeArity;
            }

            if (arityOffset <= position && position < cumulativeArity)
            {
                Debug.Assert((object)typeArgument == null);

                typeArgument = namedType.TypeArgumentsNoUseSiteDiagnostics[position - arityOffset].TypeSymbol;
            }
        }

        /// <summary>
        /// Search through the members of a given type symbol to find the method that matches a particular
        /// signature.
        /// </summary>
        /// <param name="targetTypeSymbol">Type containing the desired method symbol.</param>
        /// <param name="memberRef">A MemberRef handle that can be used to obtain the name and signature of the method</param>
        /// <param name="methodsOnly">True to only return a method.</param>
        /// <returns>The matching method symbol, or null if the inputs do not correspond to a valid method.</returns>
        internal Symbol FindMember(TypeSymbol targetTypeSymbol, MemberReferenceHandle memberRef, bool methodsOnly)
        {
            if ((object)targetTypeSymbol == null)
            {
                return null;
            }

            try
            {
                string memberName = Module.GetMemberRefNameOrThrow(memberRef);
                BlobHandle signatureHandle = Module.GetSignatureOrThrow(memberRef);

                SignatureHeader signatureHeader;
                BlobReader signaturePointer = this.DecodeSignatureHeaderOrThrow(signatureHandle, out signatureHeader);

                switch (signatureHeader.RawValue & SignatureHeader.CallingConventionOrKindMask)
                {
                    case (byte)SignatureCallingConvention.Default:
                    case (byte)SignatureCallingConvention.VarArgs:
                        int typeParamCount;
                        ParamInfo<TypeSymbol>[] targetParamInfo = this.DecodeSignatureParametersOrThrow(ref signaturePointer, signatureHeader, out typeParamCount, allowByRefReturn: true);
                        return FindMethodBySignature(targetTypeSymbol, memberName, signatureHeader, typeParamCount, targetParamInfo);

                    case (byte)SignatureKind.Field:
                        if (methodsOnly)
                        {
                            // skip:
                            return null;
                        }

                        ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers;
                        bool isVolatile;
                        TypeSymbol type = this.DecodeFieldSignature(ref signaturePointer, out isVolatile, out customModifiers);
                        return FindFieldBySignature(targetTypeSymbol, memberName, customModifiers, type);

                    default:
                        // error: unexpected calling convention
                        return null;
                }
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }

        private static FieldSymbol FindFieldBySignature(TypeSymbol targetTypeSymbol, string targetMemberName, ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers, TypeSymbol type)
        {
            foreach (Symbol member in targetTypeSymbol.GetMembers(targetMemberName))
            {
                var field = member as FieldSymbol;
                TypeSymbolWithAnnotations fieldType;

                if ((object)field != null &&
                    (fieldType = field.Type).TypeSymbol == type &&
                    CustomModifiersMatch(fieldType.CustomModifiers, customModifiers))
                {
                    // Behavior in the face of multiple matching signatures is
                    // implementation defined - we'll just pick the first one.
                    return field;
                }
            }

            return null;
        }

        private static MethodSymbol FindMethodBySignature(TypeSymbol targetTypeSymbol, string targetMemberName, SignatureHeader targetMemberSignatureHeader, int targetMemberTypeParamCount, ParamInfo<TypeSymbol>[] targetParamInfo)
        {
            foreach (Symbol member in targetTypeSymbol.GetMembers(targetMemberName))
            {
                var method = member as MethodSymbol;
                if ((object)method != null &&
                    ((byte)method.CallingConvention == targetMemberSignatureHeader.RawValue) &&
                    (targetMemberTypeParamCount == method.Arity) &&
                    MethodSymbolMatchesParamInfo(method, targetParamInfo))
                {
                    // Behavior in the face of multiple matching signatures is
                    // implementation defined - we'll just pick the first one.
                    return method;
                }
            }

            return null;
        }

        private static bool MethodSymbolMatchesParamInfo(MethodSymbol candidateMethod, ParamInfo<TypeSymbol>[] targetParamInfo)
        {
            int numParams = targetParamInfo.Length - 1; //don't count return type

            if (candidateMethod.ParameterCount != numParams)
            {
                return false;
            }

            // IndexedTypeParameterSymbol is not going to be exposed anywhere,
            // so we'll cheat and use it here for comparison purposes.
            TypeMap candidateMethodTypeMap = new TypeMap(
                candidateMethod.TypeParameters,
                IndexedTypeParameterSymbol.Take(candidateMethod.Arity), true);

            if (!ReturnTypesMatch(candidateMethod, candidateMethodTypeMap, ref targetParamInfo[0]))
            {
                return false;
            }

            for (int i = 0; i < numParams; i++)
            {
                if (!ParametersMatch(candidateMethod.Parameters[i], candidateMethodTypeMap, ref targetParamInfo[i + 1 /*for return type*/]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ParametersMatch(ParameterSymbol candidateParam, TypeMap candidateMethodTypeMap, ref ParamInfo<TypeSymbol> targetParam)
        {
            // This could be combined into a single return statement with a more complicated expression, but that would
            // be harder to debug.

            if ((candidateParam.RefKind != RefKind.None) != targetParam.IsByRef || candidateParam.CountOfCustomModifiersPrecedingByRef != targetParam.CountOfCustomModifiersPrecedingByRef)
            {
                return false;
            }

            // CONSIDER: Do we want to add special handling for error types?  Right now, we expect they'll just fail to match.
            var substituted = candidateParam.Type.SubstituteType(candidateMethodTypeMap);
            if (substituted.TypeSymbol != targetParam.Type)
            {
                return false;
            }

            if (!CustomModifiersMatch(substituted.CustomModifiers, targetParam.CustomModifiers))
            {
                return false;
            }

            return true;
        }

        private static bool ReturnTypesMatch(MethodSymbol candidateMethod, TypeMap candidateMethodTypeMap, ref ParamInfo<TypeSymbol> targetReturnParam)
        {
            TypeSymbolWithAnnotations candidateMethodType = candidateMethod.ReturnType;
            TypeSymbol targetReturnType = targetReturnParam.Type;

            // CONSIDER: Do we want to add special handling for error types?  Right now, we expect they'll just fail to match.
            var substituted = candidateMethodType.SubstituteType(candidateMethodTypeMap);
            if (substituted.TypeSymbol != targetReturnType)
            {
                return false;
            }

            if (!CustomModifiersMatch(substituted.CustomModifiers, targetReturnParam.CustomModifiers))
            {
                return false;
            }

            return true;
        }

        private static bool CustomModifiersMatch(ImmutableArray<CustomModifier> candidateCustomModifiers, ImmutableArray<ModifierInfo<TypeSymbol>> targetCustomModifiers)
        {
            if (targetCustomModifiers.IsDefault || targetCustomModifiers.IsEmpty)
            {
                return candidateCustomModifiers.IsDefault || candidateCustomModifiers.IsEmpty;
            }
            else if (candidateCustomModifiers.IsDefault)
            {
                return false;
            }

            var n = candidateCustomModifiers.Length;
            if (targetCustomModifiers.Length != n)
            {
                return false;
            }

            for (int i = 0; i < n; i++)
            {
                var targetCustomModifier = targetCustomModifiers[i];
                CustomModifier candidateCustomModifier = candidateCustomModifiers[i];

                if (targetCustomModifier.IsOptional != candidateCustomModifier.IsOptional ||
                    !object.Equals(targetCustomModifier.Modifier, candidateCustomModifier.Modifier))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
