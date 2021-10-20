// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests
{
    internal static class TestOptionService
    {
        public static IGlobalOptionService GetGlobalOptionService(HostWorkspaceServices services, IOptionProvider? optionProvider = null, IOptionPersisterProvider? optionPersisterProvider = null)
        {
            var mefHostServices = (IMefHostExportProvider)services.HostServices;
            var workspaceThreadingService = mefHostServices.GetExportedValues<IWorkspaceThreadingService>().SingleOrDefault();
            return new GlobalOptionService(
                workspaceThreadingService,
                new[]
                {
                    new Lazy<IOptionProvider, LanguageMetadata>(() => optionProvider ??= new TestOptionsProvider(), new LanguageMetadata(LanguageNames.CSharp))
                },
                new[]
                {
                    new Lazy<IOptionPersisterProvider>(() => optionPersisterProvider ??= new TestOptionsPersisterProvider())
                });
        }

        public static OptionServiceFactory.OptionService GetService(Workspace workspace, IOptionProvider? optionProvider = null, IOptionPersisterProvider? optionPersisterProvider = null)
            => new OptionServiceFactory.OptionService(GetGlobalOptionService(workspace.Services, optionProvider, optionPersisterProvider), workspaceServices: workspace.Services);

        internal class TestOptionsProvider : IOptionProvider
        {
            public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
                new Option<bool>("Test Feature", "Test Name", false));
        }

        internal sealed class TestOptionsPersisterProvider : IOptionPersisterProvider
        {
            private readonly ValueTask<IOptionPersister> _optionPersisterTask;

            public TestOptionsPersisterProvider(IOptionPersister? optionPersister = null)
                => _optionPersisterTask = new(optionPersister ?? new TestOptionsPersister());

            public ValueTask<IOptionPersister> GetOrCreatePersisterAsync(CancellationToken cancellationToken)
                => _optionPersisterTask;
        }

        internal sealed class TestOptionsPersister : IOptionPersister
        {
            private ImmutableDictionary<OptionKey, object?> _options = ImmutableDictionary<OptionKey, object?>.Empty;

            public bool TryFetch(OptionKey optionKey, out object? value)
                => _options.TryGetValue(optionKey, out value);

            public bool TryPersist(OptionKey optionKey, object? value)
            {
                _options = _options.SetItem(optionKey, value);
                return true;
            }
        }
    }
}
