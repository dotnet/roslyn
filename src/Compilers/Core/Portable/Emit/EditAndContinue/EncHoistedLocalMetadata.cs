// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Emit
{
    internal struct EncHoistedLocalMetadata
    {
        public readonly string Name;
        public readonly Cci.ITypeReference Type;
        public readonly SynthesizedLocalKind SynthesizedKind;

        public EncHoistedLocalMetadata(string name, Cci.ITypeReference type, SynthesizedLocalKind synthesizedKind)
        {
            Debug.Assert(name != null);
            Debug.Assert(type != null);
            Debug.Assert(synthesizedKind.IsLongLived());

            this.Name = name;
            this.Type = type;
            this.SynthesizedKind = synthesizedKind;
        }
    }
}
