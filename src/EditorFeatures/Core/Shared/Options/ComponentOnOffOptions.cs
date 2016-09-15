// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    /// <summary>
    /// options to indicate whether a certain component in Roslyn is enabled or not
    /// </summary>
    internal static class EditorComponentOnOffOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Components\";

        [ExportOption]
        public static readonly Option<bool> Adornment = new Option<bool>(nameof(EditorComponentOnOffOptions), nameof(Adornment), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Adornment"));

        [ExportOption]
        public static readonly Option<bool> Tagger = new Option<bool>(nameof(EditorComponentOnOffOptions), nameof(Tagger), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Tagger"));

        [ExportOption]
        public static readonly Option<bool> CodeRefactorings = new Option<bool>(nameof(EditorComponentOnOffOptions), nameof(CodeRefactorings), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Code Refactorings"));
    }
}
