// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    [ExportGlobalOptionProvider, Shared]
    internal sealed class NavigationBarViewOptions : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NavigationBarViewOptions()
        {
        }

        ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
            ShowNavigationBar);

        private const string FeatureName = "NavigationBarOptions";

        public static readonly PerLanguageOption<bool> ShowNavigationBar = new(FeatureName, nameof(ShowNavigationBar), defaultValue: true);
    }
}
