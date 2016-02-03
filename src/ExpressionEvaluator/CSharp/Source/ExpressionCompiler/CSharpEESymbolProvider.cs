// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.ExpressionEvaluator;

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

        private TypeSymbol GetDynamicType(TypeSymbol type, RefKind refKind, ImmutableArray<bool> dynamicFlags)
        {
            return DynamicTypeDecoder.TransformTypeWithoutCustomModifierFlags(type, _sourceAssembly, refKind, dynamicFlags);
        }

        public override LocalSymbol GetLocalVariable(string name, int slotIndex, LocalInfo<TypeSymbol> info, ImmutableArray<bool> dynamicFlagsOpt)
        {
            var isPinned = info.IsPinned;

            LocalDeclarationKind kind;
            RefKind refKind;
            TypeSymbol type;
            if (info.IsByRef && isPinned)
            {
                kind = LocalDeclarationKind.FixedVariable;
                refKind = RefKind.None;
                type = new PointerTypeSymbol(info.Type);
            }
            else
            {
                kind = LocalDeclarationKind.RegularVariable;
                refKind = info.IsByRef ? RefKind.Ref : RefKind.None;
                type = info.Type;
            }

            if (!dynamicFlagsOpt.IsDefault)
            {
                type = GetDynamicType(type, refKind, dynamicFlagsOpt);
            }

            // Custom modifiers can be dropped since binding ignores custom
            // modifiers from locals and since we only need to preserve
            // the type of the original local in the generated method.
            return new EELocalSymbol(_method, EELocalSymbol.NoLocations, name, slotIndex, kind, type, refKind, isPinned, isCompilerGenerated: false, canScheduleToStack: false);
        }

        public override LocalSymbol GetLocalConstant(string name, TypeSymbol type, ConstantValue value, ImmutableArray<bool> dynamicFlagsOpt)
        {
            if (!dynamicFlagsOpt.IsDefault)
            {
                type = GetDynamicType(type, RefKind.None, dynamicFlagsOpt);
            }

            return new EELocalConstantSymbol(_method, name, type, value);
        }

        /// <exception cref="BadImageFormatException"></exception>
        /// <exception cref="UnsupportedSignatureContent"></exception>
        public override TypeSymbol DecodeLocalVariableType(ImmutableArray<byte> signature)
        {
            return _metadataDecoder.DecodeLocalVariableTypeOrThrow(signature);
        }
    }
}
