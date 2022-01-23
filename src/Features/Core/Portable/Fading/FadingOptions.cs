// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Fading
{
    internal sealed class FadingOptions
    {
        [ExportSolutionOptionProvider, Shared]
        internal sealed class Provider : IOptionProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Provider()
            {
            }

            ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
                FadeOutUnusedImports,
                FadeOutUnreachableCode);
        }

        private const string FeatureName = "FadingOptions";

        public static readonly PerLanguageOption2<bool> FadeOutUnusedImports = new(
            FeatureName, "FadeOutUnusedImports", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.FadeOutUnusedImports"));

        public static readonly PerLanguageOption2<bool> FadeOutUnreachableCode = new(
            FeatureName, "FadeOutUnreachableCode", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.FadeOutUnreachableCode"));
    }
}
