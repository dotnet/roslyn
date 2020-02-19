// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;

#if CODE_STYLE
namespace Microsoft.CodeAnalysis.Internal.Options
#else
namespace Microsoft.CodeAnalysis.Options
#endif
{
    public interface IOption
    {
        string Feature { get; }
        string Name { get; }
        Type Type { get; }
        object? DefaultValue { get; }
        bool IsPerLanguage { get; }

        ImmutableArray<OptionStorageLocation> StorageLocations { get; }
    }
}
