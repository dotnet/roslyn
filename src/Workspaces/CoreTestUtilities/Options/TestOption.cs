// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.UnitTests;

internal class TestOption : IOption
{
    public string Feature { get; set; } = "test";
    public string Name { get; set; } = "test";
    public Type Type { get; set; } = typeof(int);
    public object? DefaultValue { get; set; } = 1;
    public bool IsPerLanguage { get; set; }
    public ImmutableArray<OptionStorageLocation> StorageLocations { get; set; }
}

#pragma warning disable RS0030 // Do not used banned APIs

internal class TestOption<T> : Option<T>
{
    public TestOption(string feature = "test", string name = "test", T? defaultValue = default, OptionStorageLocation[]? storageLocations = null)
        : base(feature, name, defaultValue!, storageLocations ?? [])
    {
    }
}

internal class PerLanguageTestOption<T> : PerLanguageOption<T>
{
    public PerLanguageTestOption(string feature = "test", string name = "test", T? defaultValue = default, OptionStorageLocation[]? storageLocations = null)
        : base(feature, name, defaultValue!, storageLocations ?? [])
    {
    }
}
#pragma warning restore
