// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// We need a CCI representation for local constants because they are emitted as locals in
    /// PDB scopes to improve the debugging experience (see LocalScopeProvider.GetConstantsInScope).
    /// </summary>
    internal sealed class LocalConstantDefinition : Cci.ILocalDefinition
    {
        private readonly string _name;
        private readonly Location _location;
        private readonly Cci.IMetadataConstant _compileTimeValue;
        private readonly bool _isDynamic;

        //Gives the synthesized dynamic attributes of the local definition
        private readonly ImmutableArray<TypedConstant> _dynamicTransformFlags;

        public LocalConstantDefinition(string name, Location location, Cci.IMetadataConstant compileTimeValue, bool isDynamic = false,
            ImmutableArray<TypedConstant> dynamicTransformFlags = default(ImmutableArray<TypedConstant>))
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Debug.Assert(compileTimeValue != null);

            _name = name;
            _location = location;
            _compileTimeValue = compileTimeValue;
            _isDynamic = isDynamic;
            _dynamicTransformFlags = dynamicTransformFlags;
        }

        public string Name => _name;

        public Location Location => _location;

        public Cci.IMetadataConstant CompileTimeValue => _compileTimeValue;

        public Cci.ITypeReference Type => _compileTimeValue.Type;

        public bool IsConstant => true;

        public ImmutableArray<Cci.ICustomModifier> CustomModifiers
            => ImmutableArray<Cci.ICustomModifier>.Empty;

        public bool IsModified => false;

        public bool IsPinned => false;

        public bool IsReference => false;

        public LocalSlotConstraints Constraints => LocalSlotConstraints.None;

        public bool IsDynamic => _isDynamic;

        public LocalVariableAttributes PdbAttributes => LocalVariableAttributes.None;

        public ImmutableArray<TypedConstant> DynamicTransformFlags => _dynamicTransformFlags;

        public int SlotIndex => -1;

        public byte[] Signature => null;

        public LocalSlotDebugInfo SlotInfo
            => new LocalSlotDebugInfo(SynthesizedLocalKind.UserDefined, LocalDebugId.None);
    }
}
