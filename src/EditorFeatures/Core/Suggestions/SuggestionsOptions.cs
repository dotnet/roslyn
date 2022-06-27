// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    [ExportGlobalOptionProvider, Shared]
    internal sealed class SuggestionsOptions : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SuggestionsOptions()
        {
        }

        ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
            AsynchronousQuickActionsDisableFeatureFlag);

        private const string FeatureName = "SuggestionsOptions";

        public static readonly Option2<bool?> Asynchronous = new(FeatureName, nameof(Asynchronous), defaultValue: null,
            new RoamingProfileStorageLocation("TextEditor.Specific.Suggestions.Asynchronous4"));

        public static readonly Option2<bool> AsynchronousQuickActionsDisableFeatureFlag = new(FeatureName, nameof(AsynchronousQuickActionsDisableFeatureFlag), defaultValue: false,
            new FeatureFlagStorageLocation("Roslyn.AsynchronousQuickActionsDisable2"));
    }
}
