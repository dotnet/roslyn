// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class CSharpEESymbolProvider : EESymbolProvider<TypeSymbol, LocalSymbol>
    {
        private readonly MetadataDecoder _metadataDecoder;
        private readonly SourceAssemblySymbol _sourceAssembly;
        private readonly PEMethodSymbol _method;

        public CSharpEESymbolProvider(SourceAssemblySymbol sourceAssembly, PEModuleSymbol module, PEMethodSymbol method)
        {
            _metadataDecoder = new MetadataDecoder(module, method);
            _sourceAssembly = sourceAssembly;
            _method = method;
        }

        public override LocalSymbol GetLocalVariable(
            string? name,
            int slotIndex,
            LocalInfo<TypeSymbol> info,
            ImmutableArray<bool> dynamicFlagsOpt,
            ImmutableArray<string?> tupleElementNamesOpt)
        {
            var isPinned = info.IsPinned;

            LocalDeclarationKind kind;
            RefKind refKind;
            TypeSymbol type;
            if (info.IsByRef && isPinned)
            {
                kind = LocalDeclarationKind.FixedVariable;
                refKind = RefKind.None;
                type = new PointerTypeSymbol(TypeWithAnnotations.Create(info.Type));
            }
            else
            {
                kind = LocalDeclarationKind.RegularVariable;
                refKind = info.IsByRef ? RefKind.Ref : RefKind.None;
                type = info.Type;
            }

            // Custom modifiers can be dropped since binding ignores custom
            // modifiers from locals and since we only need to preserve
            // the type of the original local in the generated method.
            type = IncludeDynamicAndTupleElementNamesIfAny(type, refKind, dynamicFlagsOpt, tupleElementNamesOpt);
            return new EELocalSymbol(_method, EELocalSymbol.NoLocations, name, slotIndex, kind, type, refKind, isPinned, isCompilerGenerated: false, canScheduleToStack: false);
        }

        public override LocalSymbol GetLocalConstant(
            string name,
            TypeSymbol type,
            ConstantValue value,
            ImmutableArray<bool> dynamicFlagsOpt,
            ImmutableArray<string?> tupleElementNamesOpt)
        {
            type = IncludeDynamicAndTupleElementNamesIfAny(type, RefKind.None, dynamicFlagsOpt, tupleElementNamesOpt);
            return new EELocalConstantSymbol(_method, name, type, value);
        }

        /// <exception cref="BadImageFormatException"></exception>
        /// <exception cref="UnsupportedSignatureContent"></exception>
        public override TypeSymbol DecodeLocalVariableType(ImmutableArray<byte> signature)
        {
            return _metadataDecoder.DecodeLocalVariableTypeOrThrow(signature);
        }

        public override TypeSymbol GetTypeSymbolForSerializedType(string typeName)
        {
            return _metadataDecoder.GetTypeSymbolForSerializedType(typeName);
        }

        /// <exception cref="BadImageFormatException"></exception>
        /// <exception cref="UnsupportedSignatureContent"></exception>
        public override void DecodeLocalConstant(ref BlobReader reader, out TypeSymbol type, out ConstantValue value)
        {
            _metadataDecoder.DecodeLocalConstantBlobOrThrow(ref reader, out type, out value);
        }

        /// <exception cref="BadImageFormatException"></exception>
        public override IAssemblySymbolInternal GetReferencedAssembly(AssemblyReferenceHandle handle)
        {
            int index = _metadataDecoder.Module.GetAssemblyReferenceIndexOrThrow(handle);
            var assembly = _metadataDecoder.ModuleSymbol.GetReferencedAssemblySymbol(index);
            if (assembly == null)
            {
                throw new BadImageFormatException();
            }
            return assembly;
        }

        /// <exception cref="UnsupportedSignatureContent"></exception>
        public override TypeSymbol GetType(EntityHandle handle)
        {
            bool isNoPiaLocalType;
            return _metadataDecoder.GetSymbolForTypeHandleOrThrow(handle, out isNoPiaLocalType, allowTypeSpec: true, requireShortForm: false);
        }

        private TypeSymbol IncludeDynamicAndTupleElementNamesIfAny(
            TypeSymbol type,
            RefKind refKind,
            ImmutableArray<bool> dynamicFlagsOpt,
            ImmutableArray<string?> tupleElementNamesOpt)
        {
            if (!dynamicFlagsOpt.IsDefault)
            {
                type = DynamicTypeDecoder.TransformTypeWithoutCustomModifierFlags(type, _sourceAssembly, refKind, dynamicFlagsOpt, checkLength: false);
            }
            return TupleTypeDecoder.DecodeTupleTypesIfApplicable(type, tupleElementNamesOpt);
        }
    }
}
