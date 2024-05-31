// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;

internal static class OptionsCollectionExtensions
{
    public static OptionSet ToOptionSet(this OptionsCollection collection)
        => new TestOptionSet(collection.Options.ToImmutableDictionary(entry => new OptionKey(entry.Key.Option, entry.Key.Language), entry => entry.Value));

    public static void SetGlobalOptions(this OptionsCollection collection, IGlobalOptionService globalOptions)
    {
        foreach (var (optionKey, value) in collection.Options)
        {
            globalOptions.SetGlobalOption(optionKey, value);
        }
    }
}
