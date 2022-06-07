﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

#if CODE_STYLE
using System.Collections.Immutable;
#endif

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Internal base option type that is available in both the Workspaces layer and CodeStyle layer.
    /// Its definition in Workspaces layer sub-types "IOption" and its definition in CodeStyle layer
    /// explicitly defines all the members from "IOption" type as "IOption" is not available in CodeStyle layer.
    /// This ensures that all the sub-types of <see cref="IOption2"/> in either layer see an identical
    /// set of interface members.
    /// </summary>
    internal interface IOption2 : IEquatable<IOption2?>
#if !CODE_STYLE
        , IOption
#endif
    {
        OptionDefinition OptionDefinition { get; }

#if CODE_STYLE
        string Feature { get; }
        string Name { get; }
        Type Type { get; }
        object? DefaultValue { get; }
        bool IsPerLanguage { get; }

        ImmutableArray<OptionStorageLocation2> StorageLocations { get; }
#endif
    }
}
