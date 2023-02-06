// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Emit
{
    internal readonly struct EncHoistedLocalMetadata
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
