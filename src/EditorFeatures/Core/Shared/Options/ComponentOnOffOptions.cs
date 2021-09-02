// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    /// <summary>
    /// options to indicate whether a certain component in Roslyn is enabled or not
    /// </summary>
    internal static class EditorComponentOnOffOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Components\";

        public static readonly Option2<bool> Adornment = new(nameof(EditorComponentOnOffOptions), nameof(Adornment), defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "Adornment"));

        public static readonly Option2<bool> Tagger = new(nameof(EditorComponentOnOffOptions), nameof(Tagger), defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "Tagger"));

        public static readonly Option2<bool> CodeRefactorings = new(nameof(EditorComponentOnOffOptions), nameof(CodeRefactorings), defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "Code Refactorings"));

        public static readonly Option2<bool> ShowCodeRefactoringsWhenQueriedForCodeFixes = new(
            nameof(EditorComponentOnOffOptions), nameof(ShowCodeRefactoringsWhenQueriedForCodeFixes), defaultValue: false,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(ShowCodeRefactoringsWhenQueriedForCodeFixes)));
    }

    [ExportOptionProvider, Shared]
    internal class EditorComponentOnOffOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditorComponentOnOffOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            EditorComponentOnOffOptions.Adornment,
            EditorComponentOnOffOptions.Tagger,
            EditorComponentOnOffOptions.CodeRefactorings,
            EditorComponentOnOffOptions.ShowCodeRefactoringsWhenQueriedForCodeFixes);
    }
}
