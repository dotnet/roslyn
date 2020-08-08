// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.UnitTests
{
    internal static class TestOptionService
    {
        public static OptionServiceFactory.OptionService GetService(Workspace workspace, IOptionProvider? optionProvider = null)
        {
            return new OptionServiceFactory.OptionService(new GlobalOptionService(new[]
                {
                    new Lazy<IOptionProvider, LanguageMetadata>(() => optionProvider ??= new TestOptionsProvider(), new LanguageMetadata(LanguageNames.CSharp))
                },
                Enumerable.Empty<Lazy<IOptionPersister>>()), workspaceServices: workspace.Services);
        }

        internal class TestOptionsProvider : IOptionProvider
        {
            public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
                new Option<bool>("Test Feature", "Test Name", false));
        }
    }
}
