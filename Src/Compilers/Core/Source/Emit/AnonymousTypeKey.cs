// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.Emit
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal struct AnonymousTypeKey : IEquatable<AnonymousTypeKey>
    {
        public readonly bool IsDelegate;
        public readonly ImmutableArray<string> Names;

        public AnonymousTypeKey(ImmutableArray<string> names, bool isDelegate = false)
        {
            this.IsDelegate = isDelegate;
            this.Names = names;
        }

        public bool Equals(AnonymousTypeKey other)
        {
            return (this.IsDelegate == other.IsDelegate) && this.Names.SequenceEqual(other.Names);
        }

        public override bool Equals(object obj)
        {
            return this.Equals((AnonymousTypeKey)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.IsDelegate.GetHashCode(), Hash.CombineValues(this.Names));
        }

        private string GetDebuggerDisplay()
        {
            return this.Names.Aggregate((a, b) => a + "|" + b);
        }
    }
}
