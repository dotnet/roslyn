// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Utilities;
using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Emit
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal struct EncLocalInfo : IEquatable<EncLocalInfo>
    {
        // Offset of local declarator in collection of locals
        // from syntax in the containing method.
        public readonly int Offset;
        public readonly ITypeReference Type;
        public readonly LocalSlotConstraints Constraints;
        public readonly int TempKind;
        public readonly byte[] Signature;

        public EncLocalInfo(byte[] signature)
        {
            Debug.Assert(signature != null);
            Debug.Assert(signature.Length > 0);

            this.Offset = -1;
            this.Type = null;
            this.Constraints = default(LocalSlotConstraints);
            this.TempKind = 0;
            this.Signature = signature;
        }

        public EncLocalInfo(int offset, ITypeReference type, LocalSlotConstraints constraints, int tempKind, byte[] signature)
        {
            Debug.Assert(type != null);

            this.Offset = offset;
            this.Type = type;
            this.Constraints = constraints;
            this.TempKind = tempKind;
            this.Signature = signature;
        }

        public bool IsDefault
        {
            get { return (this.Type == null) && (this.Signature == null); }
        }

        public bool IsInvalid
        {
            get { return this.Offset < 0; }
        }

        public bool Equals(EncLocalInfo other)
        {
            Debug.Assert(this.Type != null);
            Debug.Assert(other.Type != null);

            return (this.Offset == other.Offset) &&
                (this.TempKind == other.TempKind) &&
                this.Type.Equals(other.Type) &&
                (this.Constraints == other.Constraints);
        }

        public override bool Equals(object obj)
        {
            return Equals((EncLocalInfo)obj);
        }

        public override int GetHashCode()
        {
            Debug.Assert(this.Type != null);

            int result = this.Offset.GetHashCode();
            result = Hash.Combine(result, this.Type.GetHashCode());
            result = Hash.Combine(result, this.Constraints.GetHashCode());
            result = Hash.Combine(result, this.TempKind.GetHashCode());
            return result;
        }

        private string GetDebuggerDisplay()
        {
            if (this.IsDefault)
            {
                return "[default]";
            }

            return string.Format("[Offset={0}, Type={1}, Constraints={2}, TempKind={3}]", this.Offset, this.Type, this.Constraints, this.TempKind);
        }
    }
}
