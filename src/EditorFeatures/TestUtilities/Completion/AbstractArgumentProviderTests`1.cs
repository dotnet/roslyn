// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

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

        protected abstract (SyntaxNode argumentList, ImmutableArray<SyntaxNode> arguments) GetArgumentList(SyntaxToken token);

        private protected async Task VerifyDefaultValueAsync(
            string markup,
            string? expectedDefaultValue,
            string? previousDefaultValue = null,
            OptionsCollection? options = null)
        {
            using var workspaceFixture = GetOrCreateWorkspaceFixture();

            var workspace = workspaceFixture.Target.GetWorkspace(markup, GetComposition());
            var code = workspaceFixture.Target.Code;
            var position = workspaceFixture.Target.Position;

            options?.SetGlobalOptions(workspace.GlobalOptions);

            var document = workspaceFixture.Target.UpdateDocument(code, SourceCodeKind.Regular);

            var provider = workspace.ExportProvider.GetExportedValues<ArgumentProvider>().Single();
            Assert.IsType(GetArgumentProviderType(), provider);

            var root = await document.GetRequiredSyntaxRootAsync(CancellationToken.None);
            var semanticModel = await document.GetRequiredSemanticModelAsync(CancellationToken.None);
            var parameter = GetParameterSymbolInfo(workspace, semanticModel, root, position, CancellationToken.None);
            Contract.ThrowIfNull(parameter);

            var context = new ArgumentContext(provider, semanticModel, position, parameter, previousDefaultValue, CancellationToken.None);
            await provider.ProvideArgumentAsync(context);

            Assert.Equal(expectedDefaultValue, context.DefaultValue);
        }

        private IParameterSymbol GetParameterSymbolInfo(Workspace workspace, SemanticModel semanticModel, SyntaxNode root, int position, CancellationToken cancellationToken)
        {
            var token = root.FindToken(position);
            var (argumentList, arguments) = GetArgumentList(token);
            var symbols = semanticModel.GetSymbolInfo(argumentList.GetRequiredParent(), cancellationToken).GetAllSymbols();

            // if more than one symbol is found, filter to only include symbols with a matching number of arguments
            if (symbols.Length > 1)
            {
                symbols = symbols.WhereAsArray(
                    symbol =>
                    {
                        var parameters = symbol.GetParameters();
                        if (arguments.Length < GetMinimumArgumentCount(parameters))
                            return false;

                        if (arguments.Length > GetMaximumArgumentCount(parameters))
                            return false;

                        return true;
                    });
            }

            var symbol = symbols.Single();
            var parameters = symbol.GetParameters();

            var syntaxFacts = workspace.Services.GetLanguageServices(root.Language).GetRequiredService<ISyntaxFactsService>();
            Contract.ThrowIfTrue(arguments.Any(argument => syntaxFacts.IsNamedArgument(argument)), "Named arguments are not currently supported by this test.");
            Contract.ThrowIfTrue(parameters.Any(parameter => parameter.IsParams), "'params' parameters are not currently supported by this test.");

            var index = arguments.Any()
                ? arguments.IndexOf(arguments.Single(argument => argument.FullSpan.Start <= position && argument.FullSpan.End >= position))
                : 0;

            return parameters[index];

            // Local functions
            static int GetMinimumArgumentCount(ImmutableArray<IParameterSymbol> parameters)
                => parameters.Count(parameter => !parameter.IsOptional && !parameter.IsParams);

            static int GetMaximumArgumentCount(ImmutableArray<IParameterSymbol> parameters)
                => parameters.Any(parameter => parameter.IsParams) ? int.MaxValue : parameters.Length;
        }
    }
}
