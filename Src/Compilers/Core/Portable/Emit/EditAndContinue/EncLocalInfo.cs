// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal struct EncLocalInfo : IEquatable<EncLocalInfo>
    {
        public readonly LocalDebugId Id;
        public readonly Cci.ITypeReference Type;
        public readonly LocalSlotConstraints Constraints;
        public readonly SynthesizedLocalKind Kind;
        public readonly byte[] Signature;
        public readonly bool isInvalid;

        public EncLocalInfo(byte[] signature)
        {
            Debug.Assert(signature != null);
            Debug.Assert(signature.Length > 0);

            this.Id = LocalDebugId.None;
            this.Type = null;
            this.Constraints = default(LocalSlotConstraints);
            this.Kind = SynthesizedLocalKind.EmitterTemp;
            this.Signature = signature;
            this.isInvalid = true;
        }

        public EncLocalInfo(LocalDebugId id, Cci.ITypeReference type, LocalSlotConstraints constraints, SynthesizedLocalKind synthesizedKind, byte[] signature)
        {
            Debug.Assert(type != null);

            this.Id = id;
            this.Type = type;
            this.Constraints = constraints;
            this.Kind = synthesizedKind;
            this.Signature = signature;
            this.isInvalid = false;
        }

        public bool IsDefault
        {
            get { return this.Type == null && this.Signature == null; }
        }

        public bool IsInvalid
        {
            get { return isInvalid; }
        }

        public bool Equals(EncLocalInfo other)
        {
            Debug.Assert(this.Type != null);
            Debug.Assert(other.Type != null);

            return this.Id.Equals(other.Id) &&
                   this.Kind == other.Kind &&
                   this.Type.Equals(other.Type) &&
                   this.Constraints == other.Constraints &&
                   this.isInvalid == other.isInvalid;
        }

        public override bool Equals(object obj)
        {
            return obj is EncLocalInfo && Equals((EncLocalInfo)obj);
        }

        public override int GetHashCode()
        {
            Debug.Assert(this.Type != null);

            return Hash.Combine(this.Id.GetHashCode(),
                   Hash.Combine(this.Type.GetHashCode(),
                   Hash.Combine((int)this.Constraints,
                   Hash.Combine(isInvalid, (int)this.Kind))));
        }

        private string GetDebuggerDisplay()
        {
            if (this.IsDefault)
            {
                return "[default]";
            }

            if (this.isInvalid)
            {
                return "[invalid]";
            }

            return string.Format("[Id={0}.{1}.{2}, Type={3}, Constraints={4}, SynthesizedKind={5}]", this.Id.SyntaxOffset, this.Id.Ordinal, this.Id.Subordinal, this.Type, this.Constraints, this.Kind);
        }
    }
}
