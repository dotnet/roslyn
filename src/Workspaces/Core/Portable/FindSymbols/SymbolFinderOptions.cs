// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FindSymbols
{
    /// <summary>
    /// Options for <see cref="SymbolFinder"/> operations.
    /// </summary>
    internal sealed class SymbolFinderOptions
    {
        public static SymbolFinderOptions Default { get; } = new SymbolFinderOptions();

        /// <summary>
        /// <para>
        /// Determines whether searches for an accessor are replaced with searches for its
        /// containing member.
        /// </para>
        /// <para>
        /// For example, if you search for an <see cref="IMethodSymbol"/> which happens
        /// to be a property accessor, you will get results as if you had searched for
        /// the containing <see cref="IPropertySymbol"/>.
        /// </para>
        /// </summary>
        public bool SearchAccessorsAsContainingMember { get; }

        /// <summary>
        /// <para>
        /// Determines whether searches for accessors (<see cref="IMethodSymbol"/>s)
        /// include results for usages of the containing member.
        /// </para>
        /// <para>
        /// For example, this would cause <c>set_SomeProperty</c> to be found in the syntax
        /// <c>SomeProperty = 42</c> even though only the property (not the setter itself)
        /// is syntactically referenced.
        /// (A hypothetical accessor reference, as opposed to containing member reference,
        /// would look something like <c>new Action&lt;int&gt;(SomeProperty.set)</c>.)
        /// </para>
        /// <para>
        /// This is different than <see cref="SearchAccessorsAsContainingMember"/>. Cascading
        /// returns no results keyed to the accessor <see cref="IMethodSymbol"/> for which
        /// you actually searched. Cascading instead returns results keyed to the containing
        /// member (say,<see cref="IPropertySymbol"/>) as though you had searched for the
        /// property instead of the accessor. Those results would include implicit usages
        /// of all the property’s accessors, not just the accessor for which you searched.
        /// </para>
        /// </summary>
        public bool IncludeImplicitAccessorUsages { get; }

        /// <param name="searchAccessorsAsContainingMember">See <see cref="SearchAccessorsAsContainingMember" />.</param>
        /// <param name="includeImplicitAccessorUsages">See <see cref="IncludeImplicitAccessorUsages" />.</param>
        public SymbolFinderOptions(bool searchAccessorsAsContainingMember = true, bool includeImplicitAccessorUsages = false)
        {
            SearchAccessorsAsContainingMember = searchAccessorsAsContainingMember;
            IncludeImplicitAccessorUsages = includeImplicitAccessorUsages;
        }
    }
}
