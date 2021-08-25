// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Test.Utilities.Completion
{
    [UseExportProvider]
    public abstract class AbstractArgumentProviderTests<TWorkspaceFixture> : TestBase
        where TWorkspaceFixture : TestWorkspaceFixture, new()
    {
        private static readonly TestComposition s_baseComposition = EditorTestCompositions.EditorFeatures.AddExcludedPartTypes(typeof(ArgumentProvider));

        private readonly TestFixtureHelper<TWorkspaceFixture> _fixtureHelper = new();

        private ExportProvider? _lazyExportProvider;

        protected ExportProvider ExportProvider
            => _lazyExportProvider ??= GetComposition().ExportProviderFactory.CreateExportProvider();

        protected virtual TestComposition GetComposition()
            => s_baseComposition.AddParts(GetArgumentProviderType());

        private protected ReferenceCountedDisposable<TWorkspaceFixture> GetOrCreateWorkspaceFixture()
            => _fixtureHelper.GetOrCreateFixture();

        internal abstract Type GetArgumentProviderType();

        protected virtual OptionSet WithChangedOptions(OptionSet options) => options;

        private protected async Task VerifyDefaultValueAsync(
            string markup,
            string? expectedDefaultValue,
            string? previousDefaultValue = null)
        {
            using var workspaceFixture = GetOrCreateWorkspaceFixture();

            var workspace = workspaceFixture.Target.GetWorkspace(markup, ExportProvider);
            var code = workspaceFixture.Target.Code;
            var position = workspaceFixture.Target.Position;

            workspace.SetOptions(WithChangedOptions(workspace.Options));

            var document = workspaceFixture.Target.UpdateDocument(code, SourceCodeKind.Regular);

            var provider = workspace.ExportProvider.GetExportedValues<ArgumentProvider>().Single();
            Assert.IsType(GetArgumentProviderType(), provider);

            var root = await document.GetRequiredSyntaxRootAsync(CancellationToken.None);
            var token = root.FindToken(position - 2);
            var semanticModel = await document.GetRequiredSemanticModelAsync(CancellationToken.None);
            var symbolInfo = semanticModel.GetSymbolInfo(token.GetRequiredParent(), CancellationToken.None);
            var target = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.Single();
            Contract.ThrowIfNull(target);

            var parameter = target.GetParameters().Single();
            var context = new ArgumentContext(provider, semanticModel, position, parameter, previousDefaultValue, CancellationToken.None);
            await provider.ProvideArgumentAsync(context);

            Assert.Equal(expectedDefaultValue, context.DefaultValue);
        }
    }
}
