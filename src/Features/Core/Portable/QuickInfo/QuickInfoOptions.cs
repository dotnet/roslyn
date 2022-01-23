// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal readonly record struct QuickInfoOptions(
        bool ShowRemarksInQuickInfo,
        bool IncludeNavigationHintsInQuickInfo)
    {
        [ExportSolutionOptionProvider, Shared]
        internal sealed class Metadata : IOptionProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Metadata()
            {
            }

            public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
                ShowRemarksInQuickInfo,
                IncludeNavigationHintsInQuickInfo);

            private const string FeatureName = "QuickInfoOptions";

            public static readonly PerLanguageOption2<bool> ShowRemarksInQuickInfo = new(
                FeatureName, "ShowRemarksInQuickInfo", defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowRemarks"));

            public static readonly Option2<bool> IncludeNavigationHintsInQuickInfo = new(
                FeatureName, "IncludeNavigationHintsInQuickInfo", defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.Specific.IncludeNavigationHintsInQuickInfo"));
        }

        public static readonly QuickInfoOptions Default
          = new(
              ShowRemarksInQuickInfo: Metadata.ShowRemarksInQuickInfo.DefaultValue,
              IncludeNavigationHintsInQuickInfo: Metadata.IncludeNavigationHintsInQuickInfo.DefaultValue);

        public static QuickInfoOptions From(Project project)
           => From(project.Solution.Options, project.Language);

        public static QuickInfoOptions From(OptionSet options, string? language)
          => new(
              ShowRemarksInQuickInfo: options.GetOption(Metadata.ShowRemarksInQuickInfo, language),
              IncludeNavigationHintsInQuickInfo: options.GetOption(Metadata.IncludeNavigationHintsInQuickInfo));

    }
}
