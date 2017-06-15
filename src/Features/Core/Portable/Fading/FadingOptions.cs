// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Fading
{
    internal static class FadingOptions
    {
        public static readonly PerLanguageOption<bool> FadeOutUnusedImports = new PerLanguageOption<bool>(
            nameof(FadingOptions), nameof(FadeOutUnusedImports), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(FadeOutUnusedImports)}"));

        public static readonly PerLanguageOption<bool> FadeOutUnnecessaryCasts = new PerLanguageOption<bool>(
            nameof(FadingOptions), nameof(FadeOutUnnecessaryCasts), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(FadeOutUnnecessaryCasts)}"));

        public static readonly PerLanguageOption<bool> FadeOutUnreachableCode = new PerLanguageOption<bool>(
            nameof(FadingOptions), nameof(FadeOutUnreachableCode), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(FadeOutUnreachableCode)}"));

        public static readonly PerLanguageOption<bool> FadeOutNullChecksThatCanBeSimplified = new PerLanguageOption<bool>(
            nameof(FadingOptions), nameof(FadeOutNullChecksThatCanBeSimplified), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(FadeOutNullChecksThatCanBeSimplified)}"));

        public static readonly PerLanguageOption<bool> FadeOutInitializersThatCanBeSimplified = new PerLanguageOption<bool>(
            nameof(FadingOptions), nameof(FadeOutInitializersThatCanBeSimplified), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(FadeOutInitializersThatCanBeSimplified)}"));

        public static readonly PerLanguageOption<bool> FadeOutDefaultExpressionsThatCanBeSimplified = new PerLanguageOption<bool>(
            nameof(FadingOptions), nameof(FadeOutDefaultExpressionsThatCanBeSimplified), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(FadeOutDefaultExpressionsThatCanBeSimplified)}"));
    }
}