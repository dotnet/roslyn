// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal struct ActiveMethodId : IEquatable<ActiveMethodId>
    {
        public readonly Guid ModuleId;
        public readonly int MethodToken;
        public readonly int MethodVersion;

        public ActiveMethodId(Guid moduleId, int methodToken, int methodVersion)
        {
            ModuleId = moduleId;
            MethodToken = methodToken;
            MethodVersion = methodVersion;
        }

        public override bool Equals(object obj)
            => obj is ActiveMethodId && Equals((ActiveMethodId)obj);

        public bool Equals(ActiveMethodId other)
            => MethodToken == other.MethodToken &&
               MethodVersion == other.MethodVersion &&
               ModuleId.Equals(other.ModuleId);

        public override int GetHashCode()
            => Hash.Combine(ModuleId.GetHashCode(), Hash.Combine(MethodToken, MethodVersion));

        public static bool operator ==(ActiveMethodId left, ActiveMethodId right) => left.Equals(right);
        public static bool operator !=(ActiveMethodId left, ActiveMethodId right) => !left.Equals(right);

        internal string GetDebuggerDisplay()
            => $"mvid={ModuleId} 0x{MethodToken:X8} v{MethodVersion}";
    }
}
