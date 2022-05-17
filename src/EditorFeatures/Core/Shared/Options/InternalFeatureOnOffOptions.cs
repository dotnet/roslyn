// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    [ExportGlobalOptionProvider, Shared]
    internal sealed class InternalFeatureOnOffOptions : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InternalFeatureOnOffOptions()
        {
        }

        ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
            BraceMatching,
            Classification,
            SemanticColorizer,
            SyntacticColorizer,
            AutomaticLineEnder,
            SmartIndenter,
            Squiggles,
            FormatOnSave,
            RenameTracking,
            EventHookup,
            Snippets,
            BackgroundAnalysisMemoryMonitor);

        private const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Features\";
        private const string FeatureName = "InternalFeatureOnOffOptions";

        public static readonly Option2<bool> BraceMatching = new(FeatureName, "BraceMatching", defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "Brace Matching"));

        public static readonly Option2<bool> Classification = new(FeatureName, "Classification", defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "Classification"));

        public static readonly Option2<bool> SemanticColorizer = new(FeatureName, "SemanticColorizer", defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "Semantic Colorizer"));

        public static readonly Option2<bool> SyntacticColorizer = new(FeatureName, "SyntacticColorizer", defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "Syntactic Colorizer"));

        public static readonly Option2<bool> AutomaticLineEnder = new(FeatureName, "AutomaticLineEnder", defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "Automatic Line Ender"));

        public static readonly Option2<bool> SmartIndenter = new(FeatureName, "SmartIndenter", defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "Smart Indenter"));

        public static readonly Option2<bool> Squiggles = new(FeatureName, "Squiggles", defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "Squiggles"));

        public static readonly Option2<bool> FormatOnSave = new(FeatureName, "FormatOnSave", defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "FormatOnSave"));

        public static readonly Option2<bool> RenameTracking = new(FeatureName, "RenameTracking", defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "Rename Tracking"));

        public static readonly Option2<bool> EventHookup = new(FeatureName, "EventHookup", defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "Event Hookup"));

        public static readonly Option2<bool> Snippets = new(FeatureName, "Snippets", defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "Snippets2"));

        public static readonly Option2<bool> BackgroundAnalysisMemoryMonitor = new(FeatureName, "FullSolutionAnalysisMemoryMonitor", defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "Full Solution Analysis Memory Monitor"));
    }
}
