// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportGlobalOptionProvider, Shared]
    internal sealed class VisualStudioNavigationOptions : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioNavigationOptions()
        {
        }

        ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
            NavigateToObjectBrowser);

        public static readonly PerLanguageOption2<bool> NavigateToObjectBrowser = new(nameof(VisualStudioNavigationOptions), nameof(NavigateToObjectBrowser), defaultValue: false,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.NavigateToObjectBrowser"));
    }
}
