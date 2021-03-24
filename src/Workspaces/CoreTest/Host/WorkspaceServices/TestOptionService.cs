// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests
{
    internal static class TestOptionService
    {
        public static OptionServiceFactory.OptionService GetService(Workspace workspace, IOptionProvider? optionProvider = null)
        {
            var mefHostServices = (IMefHostExportProvider)workspace.Services.HostServices;
            var workspaceThreadingService = mefHostServices.GetExportedValues<IWorkspaceThreadingService>().SingleOrDefault();
            return new OptionServiceFactory.OptionService(new GlobalOptionService(
                workspaceThreadingService,
                new[]
                {
                    new Lazy<IOptionProvider, LanguageMetadata>(() => optionProvider ??= new TestOptionsProvider(), new LanguageMetadata(LanguageNames.CSharp))
                },
                Enumerable.Empty<Lazy<IOptionPersisterProvider>>()), workspaceServices: workspace.Services);
        }

        internal class TestOptionsProvider : IOptionProvider
        {
            public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
                new Option<bool>("Test Feature", "Test Name", false));
        }
    }
}
