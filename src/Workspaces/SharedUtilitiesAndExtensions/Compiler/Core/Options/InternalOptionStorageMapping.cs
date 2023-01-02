// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Options;

internal abstract class InternalOptionStorageMapping
{
    public IOption2 InternalOption { get; }

    public InternalOptionStorageMapping(IOption2 internalOption)
        => InternalOption = internalOption;

    public abstract object? ToPublicOptionValue(object? internalValue);
    public abstract object? UpdateInternalOptionValue(object? currentInternalValue, object? newPublicValue);
}
