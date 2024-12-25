// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Some options store their values in a type that's not accessible publicly.
/// The mapping provides translation between the two representations.
/// </summary>
internal abstract class OptionStorageMapping(IOption2 internalOption)
{
    /// <summary>
    /// The option that stores the value internally.
    /// </summary>
    public IOption2 InternalOption { get; } = internalOption;

    /// <summary>
    /// Converts internal option value representation to public.
    /// </summary>
    public abstract object? ToPublicOptionValue(object? internalValue);

    /// <summary>
    /// Returns a new internal value created by updating <paramref name="currentInternalValue"/> to <paramref name="newPublicValue"/>.
    /// </summary>
    public abstract object? UpdateInternalOptionValue(object? currentInternalValue, object? newPublicValue);
}
