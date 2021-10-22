// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.InlineHints
{
    [ExportGlobalOptionProvider, Shared]
    internal sealed class InlineHintsViewOptions : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineHintsViewOptions()
        {
        }

        ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
            DisplayAllHintsWhilePressingAltF1,
            ColorHints);

        private const string FeatureName = "InlineHintsOptions";

        public static readonly Option2<bool> DisplayAllHintsWhilePressingAltF1 = new(
            FeatureName, "DisplayAllHintsWhilePressingAltF1", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.Specific.DisplayAllHintsWhilePressingAltF1"));

        public static readonly PerLanguageOption2<bool> ColorHints = new(
            FeatureName, "ColorHints", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ColorHints"));
    }
}
