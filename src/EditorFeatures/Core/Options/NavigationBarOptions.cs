// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
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
        public NavigationBarOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            NavigationBarOptions.ShowNavigationBar);
    }
}
