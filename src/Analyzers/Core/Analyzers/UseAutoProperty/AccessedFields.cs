// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.UseAutoProperty;

/// <param name="TrivialField">The single field accessed, when the get/set-accessor is of a trivial form similar to
/// <c>get => fieldName;</c> or <c>set => fieldName = value;</c>. If we see these forms, we'll want to convert them to
/// <c>get;/set;</c>.</param>
/// <param name="NonTrivialFields">Any fields we saw accessed in more complex expressions. These can be converted to use
/// the <c>field</c> expression form if we think we can still convert this field/property pair to an auto-prop.</param>
internal readonly record struct AccessedFields(
    IFieldSymbol? TrivialField,
    ImmutableArray<IFieldSymbol> NonTrivialFields)
{
    public static readonly AccessedFields Empty = new(null, []);

    public AccessedFields(IFieldSymbol? trivialField) : this(trivialField, [])
    {
    }

    public int Count => (TrivialField != null ? 1 : 0) + NonTrivialFields.Length;
    public bool IsEmpty => Count == 0;

    public AccessedFields Where<TArg>(Func<IFieldSymbol, TArg, bool> predicate, TArg arg)
        => new(TrivialField != null && predicate(TrivialField, arg) ? TrivialField : null,
               NonTrivialFields.WhereAsArray(predicate, arg));

    public bool Contains(IFieldSymbol field)
        => Equals(TrivialField, field) || NonTrivialFields.Contains(field);
}
