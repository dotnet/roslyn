// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A representation of the program region in which a 'ref' can be used,
    /// which is limited to what is expressible in C#.
    /// </summary>
    /// <remarks>
    /// For example, in this design, all lifetimes have a known relationship to all other lifetimes.
    /// </remarks>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal readonly struct Lifetime
    {
        private const uint CallingMethodRaw = 0;
        private const uint ReturnOnlyRaw = 1;
        private const uint CurrentMethodRaw = 2;

        /// <summary>
        /// For the purpose of escape verification we operate with the depth of local scopes.
        /// The depth is a uint, with smaller number representing shallower/wider scopes.
        /// Since sibling scopes do not intersect and a value cannot escape from one to another without
        /// escaping to a wider scope, we can use simple depth numbering without ambiguity.
        /// </summary>
        private readonly uint _value;
        private Lifetime(uint value) => _value = value;

        /// <summary>
        /// The "calling method" scope that is outside of the containing method/lambda.
        /// If something can escape to this scope, it can escape to any scope in a given method through a ref parameter or return.
        /// </summary>
        public static Lifetime CallingMethod => new Lifetime(CallingMethodRaw);

        /// <summary>
        /// The "return-only" scope that is outside of the containing method/lambda.
        /// If something can escape to this scope, it can escape to any scope in a given method or can be returned, but it can't escape through a ref parameter.
        /// </summary>
        public static Lifetime ReturnOnly => new Lifetime(ReturnOnlyRaw);

        /// <summary>
        /// The "current method" scope that is just inside the containing method/lambda.
        /// If something can escape to this scope, it can escape to any scope in a given method, but cannot be returned.
        /// </summary>
        public static Lifetime CurrentMethod => new Lifetime(CurrentMethodRaw);

        /// <summary>
        /// Gets a lifetime which is "empty". i.e. which refers to a variable whose storage is never allocated.
        ///</summary>
        public static Lifetime Empty => new Lifetime(uint.MaxValue);

        /// <summary>
        /// Gets a lifetime which is narrower than the given lifetime.
        /// Used to "enter" a nested local scope.
        /// </summary>
        public Lifetime Narrower()
        {
            var result = new Lifetime(this._value + 1);
            // Narrower() operator should always result in a local lifetime
            Debug.Assert(!result.IsReturnable);
            return result;
        }

        /// <summary>
        /// Gets a lifetime which is wider than the given lifetime.
        /// Used to "exit" a nested local scope.
        /// </summary>
        public Lifetime Wider()
        {
            // Wider() operator should always start from a local lifetime
            Debug.Assert(!this.IsReturnable);
            return new Lifetime(this._value - 1);
        }

        public bool IsCallingMethod => _value == CallingMethodRaw;
        public bool IsReturnOnly => _value == ReturnOnlyRaw;
        public bool IsReturnable => _value is CallingMethodRaw or ReturnOnlyRaw;

        /// <summary>Returns true if a 'ref' with this lifetime can be converted to the 'other' lifetime. Otherwise, returns false.</summary>
        /// <remarks>Generally, a wider lifetime is convertible to a narrower lifetime.</remarks>
        public bool IsConvertibleTo(Lifetime other)
            => this._value <= other._value;

        /// <summary>
        /// Returns the narrower of two lifetimes.
        /// </summary>
        /// <remarks>
        /// In other words, this method returns the widest lifetime which 'this' and 'other' are both convertible to.
        /// If in future we added the concept of unrelated lifetimes (e.g. to implement 'ref scoped'), this method would perhaps return a Nullable,
        /// for the case that no lifetime exists which both input lifetimes are convertible to.
        /// </remarks>
        public Lifetime Intersect(Lifetime other)
            => this.IsConvertibleTo(other) ? other : this;

        /// <summary>
        /// Returns the wider of two lifetimes.
        /// </summary>
        /// <remarks>In other words, this method returns the narrowest lifetime which can be converted to both 'this' and 'other'.</remarks>
        public Lifetime Union(Lifetime other)
            => this.IsConvertibleTo(other) ? this : other;

        /// <summary>Returns true if this lifetime is the same as 'other' (i.e. for invariant nested conversion).</summary>
        public bool Equals(Lifetime other)
            => this._value == other._value;

        public override bool Equals(object? obj)
            => obj is Lifetime other && this.Equals(other);

        public override int GetHashCode()
            => unchecked((int)_value);

        public static bool operator ==(Lifetime lhs, Lifetime rhs)
            => lhs._value == rhs._value;

        public static bool operator !=(Lifetime lhs, Lifetime rhs)
            => lhs._value != rhs._value;

        private string GetDebuggerDisplay()
            => _value switch
            {
                CallingMethodRaw => "Lifetime<CallingMethod>",
                ReturnOnlyRaw => "Lifetime<ReturnOnly>",
                CurrentMethodRaw => "Lifetime<CurrentMethod>",
                _ => $"Lifetime<{_value}>"
            };
    }
}
