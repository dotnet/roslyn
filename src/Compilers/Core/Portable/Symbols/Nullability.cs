// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Enumeration of the possible nullability states for types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Generally, <see cref="Nullability"/> is used both for declared nullability
    /// and for inferred nullability. The declared nullability of a symbol is what
    /// the code declared when the variable was defined. For example:
    /// </para>
    /// <para>
    /// <code>
    /// string s;
    /// </code>
    /// This declares a variable of type <code>String</code> with nullability <see cref="NonNull"/>,
    /// assuming that this is in C# 8 code with a <code>NonNullTypes</code> attribute.
    /// </para>
    /// <para>
    /// <code>
    /// string? s;
    /// </code>
    /// This declares a variable of type <code>String</code> with nullability <see cref="MaybeNull"/>.
    /// </para>
    /// <para>
    /// The compiler also tracks inferred nullability. For example:
    /// <code>
    /// object? o1 = "Hello world!";
    /// object? o2 = null;
    /// o1.ToString();
    /// o2.ToString(); // Warning reported
    /// if (o2 != null) o2.ToString(); // No warning reported
    /// </code>
    /// <code>o1</code> is inferred to be <see cref="NonNull"/>, despite being declared
    /// <see cref="MaybeNull"/>, and no warning is reported when it is dereferenced.
    /// However, the first dereference of <code>o2</code> has not been inferred to
    /// be <see cref="NonNull"/>, so a warning is reported. The second dereference
    /// occurs inside an <code>if</code> statement check for <see langword="null"/>, so
    /// the compiler can infer that <code>o2</code> must not be <see langword="null"/> 
    /// inside the <code>if</code> statement.
    /// </para>
    /// <para>
    /// The states of value types and nullable value types are not dynamically tracked by
    /// the compiler, so they will always be their initial state. Value types will always
    /// have a nullability of <see cref="NonNull"/>, and nullable value types will always
    /// have a nullability of <see cref="MaybeNull"/>.
    /// </para>
    /// <para>
    /// Unconstrained type parameters are a unique superposition of both <see cref="NonNull"/>
    /// and <see cref="MaybeNull"/>. They must be checked for <see langword="null"/> before
    /// being dereferenced, but are treated as <see cref="NonNull"/> for the purposes of
    /// assignment and return type warnings. As an example:
    /// <code>
    /// T M&lt;T&gt;(T t1)
    /// {
    ///     t1.ToString(); // Warning reported for dereferencing t1
    ///     t1?.ToString(); // No warning reported for using ?. operator
    ///     T t2 = default(T2); // Warning reported for assigning possibly null into t2
    ///     return t2; // Warning reported for returning possibly null
    /// }
    /// </code>
    /// This is because both <code>string</code> and <code>string?</code> can be substituted
    /// for <code>T</code>, so the user must account for the strictest possible scenario
    /// in all cases.
    /// </para>
    /// </remarks>
    public enum Nullability
    {
        /// <summary>
        /// There is no information on the current nullable state.
        /// </summary>
        /// <remarks>
        /// This is used for legacy code scenarios, where a legacy API that does
        /// not provide nullability information is being used. Expressions that
        /// have unknown nullability are also referred to as oblivious expressions,
        /// and they generally do not provide warnings when converting to 
        /// <see langword="null"/>, or when dereferencing them.
        /// </remarks>
        Unknown = 0,
        /// <summary>
        /// The variable is either known or declared to be non-null.
        /// </summary>
        /// <remarks>
        /// It is possible for a variable to have a declared nullability of NonNull,
        /// but to then be inferred to be null later. As an example:
        /// <code>
        /// object o = null; // Warning for assigning null to a NonNull object
        /// o.ToString(); // Warning for dereferencing null
        /// </code>
        /// In this example, the <see cref="ILocalSymbol.DeclaredNullability"/> will 
        /// be <see cref="NonNull"/>, but the <see cref="TypeInfo.Nullability"/> of
        /// <code>o</code> in the <code>o.ToString()</code> call will be 
        /// <see cref="MaybeNull"/>, because the compiler tracks null state regardless
        /// of the declared nullability.
        /// </remarks>
        NonNull = 1,
        /// <summary>
        /// The variable is either known or declared to possibly be null.
        /// </summary>
        /// <remarks>
        /// Nullable value types will always be considered to be <see cref="MaybeNull"/>.
        /// </remarks>
        MaybeNull = 2
    }
}
