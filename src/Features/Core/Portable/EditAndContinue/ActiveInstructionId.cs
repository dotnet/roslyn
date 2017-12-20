// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct ActiveInstructionId : IEquatable<ActiveInstructionId>
    {
        public readonly Guid ModuleId;
        public readonly int MethodToken;
        public readonly int MethodVersion;
        public readonly int ILOffset;

        public ActiveInstructionId(Guid moduleId, int methodToken, int methodVersion, int ilOffset)
        {
            ModuleId = moduleId;
            MethodToken = methodToken;
            MethodVersion = methodVersion;
            ILOffset = ilOffset;
        }

        public override bool Equals(object obj)
            => obj is ActiveInstructionId && Equals((ActiveInstructionId)obj);

        public bool Equals(ActiveInstructionId other)
            => ModuleId.Equals(other.ModuleId) &&
               MethodToken == other.MethodToken &&
               MethodVersion == other.MethodVersion &&
               ILOffset == other.ILOffset;

        public override int GetHashCode()
            => Hash.Combine(ModuleId.GetHashCode(), Hash.Combine(MethodToken, Hash.Combine(MethodVersion, ILOffset)));

        public static bool operator ==(ActiveInstructionId left, ActiveInstructionId right) => left.Equals(right);
        public static bool operator !=(ActiveInstructionId left, ActiveInstructionId right) => !left.Equals(right);
    }
}
