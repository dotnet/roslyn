// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal static class NavigationBarOptions
    {
        public static readonly PerLanguageOption<bool> ShowNavigationBar = new PerLanguageOption<bool>(nameof(NavigationBarOptions), nameof(ShowNavigationBar), defaultValue: true);
    }

    [ExportOptionProvider, Shared]
    internal class NavigationBarOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NavigationBarOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            NavigationBarOptions.ShowNavigationBar);
    }
}
