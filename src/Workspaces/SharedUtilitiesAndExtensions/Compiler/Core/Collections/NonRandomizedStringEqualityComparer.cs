// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using Internal.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    // NonRandomizedStringEqualityComparer is the comparer used by default with the Dictionary<string,...>
    // We use NonRandomizedStringEqualityComparer as default comparer as it doesnt use the randomized string hashing which
    // keeps the performance not affected till we hit collision threshold and then we switch to the comparer which is using
    // randomized string hashing.
    [Serializable] // Required for compatibility with .NET Core 2.0 as we exposed the NonRandomizedStringEqualityComparer inside the serialization blob
    // Needs to be public to support binary serialization compatibility
    public class NonRandomizedStringEqualityComparer : IEqualityComparer<string?>, IInternalStringEqualityComparer, ISerializable
    {
        // Dictionary<...>.Comparer and similar methods need to return the original IEqualityComparer
        // that was passed in to the ctor. The caller chooses one of these singletons so that the
        // GetUnderlyingEqualityComparer method can return the correct value.

        internal static readonly NonRandomizedStringEqualityComparer WrappedAroundDefaultComparer = new OrdinalComparer(EqualityComparer<string?>.Default);
        internal static readonly NonRandomizedStringEqualityComparer WrappedAroundStringComparerOrdinal = new OrdinalComparer(StringComparer.Ordinal);
        internal static readonly NonRandomizedStringEqualityComparer WrappedAroundStringComparerOrdinalIgnoreCase = new OrdinalIgnoreCaseComparer(StringComparer.OrdinalIgnoreCase);

        private readonly IEqualityComparer<string?> _underlyingComparer;

        private NonRandomizedStringEqualityComparer(IEqualityComparer<string?> underlyingComparer)
        {
            Debug.Assert(underlyingComparer != null);
            _underlyingComparer = underlyingComparer;
        }

        // This is used by the serialization engine.
        protected NonRandomizedStringEqualityComparer(SerializationInfo information, StreamingContext context)
            : this(EqualityComparer<string?>.Default)
        {
        }

        public virtual bool Equals(string? x, string? y)
        {
            // This instance may have been deserialized into a class that doesn't guarantee
            // these parameters are non-null. Can't short-circuit the null checks.

            return string.Equals(x, y);
        }

        public virtual int GetHashCode(string? obj)
        {
            // This instance may have been deserialized into a class that doesn't guarantee
            // these parameters are non-null. Can't short-circuit the null checks.

            return obj?.GetNonRandomizedHashCode() ?? 0;
        }

        internal virtual RandomizedStringEqualityComparer GetRandomizedEqualityComparer()
        {
            return RandomizedStringEqualityComparer.Create(_underlyingComparer, ignoreCase: false);
        }

        // Gets the comparer that should be returned back to the caller when querying the
        // ICollection.Comparer property. Also used for serialization purposes.
        public virtual IEqualityComparer<string?> GetUnderlyingEqualityComparer() => _underlyingComparer;

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // We are doing this to stay compatible with .NET Framework.
            // Our own collection types will never call this (since this type is a wrapper),
            // but perhaps third-party collection types could try serializing an instance
            // of this.
            info.SetType(typeof(GenericEqualityComparer<string>));
        }

        private sealed class OrdinalComparer : NonRandomizedStringEqualityComparer
        {
            internal OrdinalComparer(IEqualityComparer<string?> wrappedComparer)
                : base(wrappedComparer)
            {
            }

            public override bool Equals(string? x, string? y) => string.Equals(x, y);

            public override int GetHashCode(string? obj)
            {
                Debug.Assert(obj != null, "This implementation is only called from first-party collection types that guarantee non-null parameters.");
                return obj.GetNonRandomizedHashCode();
            }

        }

        private sealed class OrdinalIgnoreCaseComparer : NonRandomizedStringEqualityComparer
        {
            internal OrdinalIgnoreCaseComparer(IEqualityComparer<string?> wrappedComparer)
                : base(wrappedComparer)
            {
            }

            public override bool Equals(string? x, string? y) => string.EqualsOrdinalIgnoreCase(x, y);

            public override int GetHashCode(string? obj)
            {
                Debug.Assert(obj != null, "This implementation is only called from first-party collection types that guarantee non-null parameters.");
                return obj.GetNonRandomizedHashCodeOrdinalIgnoreCase();
            }

            internal override RandomizedStringEqualityComparer GetRandomizedEqualityComparer()
            {
                return RandomizedStringEqualityComparer.Create(_underlyingComparer, ignoreCase: true);
            }
        }
    }
}
