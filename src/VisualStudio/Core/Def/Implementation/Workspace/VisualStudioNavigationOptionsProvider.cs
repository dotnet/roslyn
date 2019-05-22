// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportOptionProvider, Shared]
    internal class VisualStudioNavigationOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public VisualStudioNavigationOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            VisualStudioNavigationOptions.NavigateToObjectBrowser);
    }
}
