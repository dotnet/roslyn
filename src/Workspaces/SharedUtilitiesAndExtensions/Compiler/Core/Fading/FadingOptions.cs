// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Fading
{
    internal static class FadingOptions
    {
        public static readonly PerLanguageOption2<bool> FadeOutUnusedImports = new(
            nameof(FadingOptions), nameof(FadeOutUnusedImports), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(FadeOutUnusedImports)}"));

        public static readonly PerLanguageOption2<bool> FadeOutUnreachableCode = new(
            nameof(FadingOptions), nameof(FadeOutUnreachableCode), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(FadeOutUnreachableCode)}"));

        public static readonly ImmutableArray<IOption2> AllOptions = ImmutableArray.Create<IOption2>(
            FadeOutUnusedImports,
            FadeOutUnreachableCode);
    }
}
