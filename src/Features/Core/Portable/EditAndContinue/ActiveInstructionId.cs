// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
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
            => ILOffset == other.ILOffset &&
               MethodToken == other.MethodToken &&
               MethodVersion == other.MethodVersion &&
               ModuleId.Equals(other.ModuleId);

        public override int GetHashCode()
            => Hash.Combine(ModuleId.GetHashCode(), Hash.Combine(MethodToken, Hash.Combine(MethodVersion, ILOffset)));

        public static bool operator ==(ActiveInstructionId left, ActiveInstructionId right) => left.Equals(right);
        public static bool operator !=(ActiveInstructionId left, ActiveInstructionId right) => !left.Equals(right);

        internal string GetDebuggerDisplay()
            => $"mvid={ModuleId} 0x{MethodToken:X8} v{MethodVersion} IL_{ILOffset:X4}";

        public ActiveMethodId MethodId => new ActiveMethodId(ModuleId, MethodToken, MethodVersion);
    }
}
