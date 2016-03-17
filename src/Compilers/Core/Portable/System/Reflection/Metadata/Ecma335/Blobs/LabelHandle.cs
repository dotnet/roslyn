// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

#if SRM
namespace System.Reflection.Metadata.Ecma335.Blobs
#else
namespace Roslyn.Reflection.Metadata.Ecma335.Blobs
#endif
{
#if SRM
    public
#endif
    struct LabelHandle : IEquatable<LabelHandle>
    {
        // 1-based
        internal readonly int Id;

        internal LabelHandle(int id)
        {
            Debug.Assert(id >= 1);
            Id = id;
        }

        public bool IsNil => Id == 0;

        public bool Equals(LabelHandle other) => Id == other.Id;
        public override bool Equals(object obj) => obj is LabelHandle && Equals((LabelHandle)obj);
        public override int GetHashCode() => Id.GetHashCode();

        public static bool operator ==(LabelHandle left, LabelHandle right) => left.Equals(right);
        public static bool operator !=(LabelHandle left, LabelHandle right) => !left.Equals(right);
    }
}
