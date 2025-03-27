// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A representation of the program region in which the *referent* of a `ref` is *live*.
    /// Limited to what is expressible in C#.
    /// See also:
    /// - https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/low-level-struct-improvements.md#detailed-design
    /// - https://github.com/dotnet/csharpstandard/blob/draft-v8/standard/variables.md#972-ref-safe-contexts
    /// - https://github.com/dotnet/csharpstandard/blob/draft-v8/standard/structs.md#16412-safe-context-constraint
    /// </summary>
    /// <remarks>
    /// - A *referent* is the variable being referenced by a `ref`.
    /// - Informally, a variable is *live* if it has storage allocated for it (either on heap or stack).
    /// - In this design, all SafeContexts have a known relationship to all other SafeContexts.
    /// </remarks>
    internal readonly struct SafeContext
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
        private SafeContext(uint value) => _value = value;

        /// <summary>
        /// The "calling method" scope that is outside of the containing method/lambda.
        /// If something can escape to this scope, it can escape to any scope in a given method through a ref parameter or return.
        /// </summary>
        public static readonly SafeContext CallingMethod = new SafeContext(CallingMethodRaw);

        /// <summary>
        /// The "return-only" scope that is outside of the containing method/lambda.
        /// If something can escape to this scope, it can escape to any scope in a given method or can be returned, but it can't escape through a ref parameter.
        /// </summary>
        public static readonly SafeContext ReturnOnly = new SafeContext(ReturnOnlyRaw);

        /// <summary>
        /// The "current method" scope that is just inside the containing method/lambda.
        /// If something can escape to this scope, it can escape to any scope in a given method, but cannot be returned.
        /// </summary>
        public static readonly SafeContext CurrentMethod = new SafeContext(CurrentMethodRaw);

        /// <summary>
        /// Gets a SafeContext which is "empty". i.e. which refers to a variable whose storage is never allocated.
        /// </summary>
        public static readonly SafeContext Empty = new SafeContext(uint.MaxValue);

        /// <summary>
        /// Gets a SafeContext which is narrower than the given SafeContext.
        /// Used to "enter" a nested local scope.
        /// </summary>
        public SafeContext Narrower()
        {
            Debug.Assert(_value >= ReturnOnlyRaw);
            return new SafeContext(_value + 1);
        }

        /// <summary>
        /// Gets a SafeContext which is wider than the given SafeContext.
        /// Used to "exit" a nested local scope.
        /// </summary>
        public SafeContext Wider()
        {
            Debug.Assert(_value >= CurrentMethodRaw);
            return new SafeContext(_value - 1);
        }

        public bool IsCallingMethod => _value == CallingMethodRaw;
        public bool IsReturnOnly => _value == ReturnOnlyRaw;
        public bool IsReturnable => _value is CallingMethodRaw or ReturnOnlyRaw;

        /// <summary>Returns true if a 'ref' with this SafeContext can be converted to the 'other' SafeContext. Otherwise, returns false.</summary>
        /// <remarks>Generally, a wider SafeContext is convertible to a narrower SafeContext.</remarks>
        public bool IsConvertibleTo(SafeContext other)
            => this._value <= other._value;

        /// <summary>
        /// Returns the narrower of two SafeContexts.
        /// </summary>
        /// <remarks>
        /// In other words, this method returns the widest SafeContext which 'this' and 'other' are both convertible to.
        /// If in future we added the concept of unrelated SafeContexts (e.g. to implement 'ref scoped'), this method would perhaps return a Nullable,
        /// for the case that no SafeContext exists which both input SafeContexts are convertible to.
        /// </remarks>
        public SafeContext Intersect(SafeContext other)
            => this.IsConvertibleTo(other) ? other : this;

        /// <summary>
        /// Returns the wider of two SafeContexts.
        /// </summary>
        /// <remarks>In other words, this method returns the narrowest SafeContext which can be converted to both 'this' and 'other'.</remarks>
        public SafeContext Union(SafeContext other)
            => this.IsConvertibleTo(other) ? this : other;

        /// <summary>Returns true if this SafeContext is the same as 'other' (i.e. for invariant nested conversion).</summary>
        public bool Equals(SafeContext other)
            => this._value == other._value;

        public override bool Equals(object? obj)
            => obj is SafeContext other && this.Equals(other);

        public override int GetHashCode()
            => unchecked((int)_value);

        public static bool operator ==(SafeContext lhs, SafeContext rhs)
            => lhs._value == rhs._value;

        public static bool operator !=(SafeContext lhs, SafeContext rhs)
            => lhs._value != rhs._value;

        public override string ToString()
            => _value switch
            {
                CallingMethodRaw => "SafeContext<CallingMethod>",
                ReturnOnlyRaw => "SafeContext<ReturnOnly>",
                CurrentMethodRaw => "SafeContext<CurrentMethod>",
                _ => $"SafeContext<{_value}>"
            };
    }
}
