// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// We need a CCI representation for local constants because they are emitted as locals in
    /// PDB scopes to improve the debugging experience (see LocalScopeProvider.GetConstantsInScope).
    /// </summary>
    internal sealed class LocalConstantDefinition : Cci.ILocalDefinition
    {
        public LocalConstantDefinition(
            string name,
            Location location,
            MetadataConstant compileTimeValue,
            ImmutableArray<bool> dynamicTransformFlags,
            ImmutableArray<string> tupleElementNames)
        {
            RoslynDebug.Assert(!RoslynString.IsNullOrEmpty(name));
            RoslynDebug.Assert(compileTimeValue != null);

            Name = name;
            Location = location;
            CompileTimeValue = compileTimeValue;
            DynamicTransformFlags = dynamicTransformFlags.NullToEmpty();
            TupleElementNames = tupleElementNames.NullToEmpty();
        }

        public string Name { get; }

        public Location Location { get; }

        public MetadataConstant CompileTimeValue { get; }

        public Cci.ITypeReference Type => CompileTimeValue.Type;

        public bool IsConstant => true;

        public ImmutableArray<Cci.ICustomModifier> CustomModifiers
            => ImmutableArray<Cci.ICustomModifier>.Empty;

        public bool IsModified => false;

        public bool IsPinned => false;

        public bool IsReference => false;

        public LocalSlotConstraints Constraints => LocalSlotConstraints.None;

        public LocalVariableAttributes PdbAttributes => LocalVariableAttributes.None;

        public ImmutableArray<bool> DynamicTransformFlags { get; }

        public ImmutableArray<string> TupleElementNames { get; }

        public int SlotIndex => -1;

        public byte[]? Signature => null;

        public LocalSlotDebugInfo SlotInfo
            => new LocalSlotDebugInfo(SynthesizedLocalKind.UserDefined, LocalDebugId.None);
    }
}
