// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.TypeInferrer;

[UseExportProvider]
public abstract class TypeInferrerTestBase<TWorkspaceFixture> : TestBase
    where TWorkspaceFixture : TestWorkspaceFixture, new()
{
    private readonly TestFixtureHelper<TWorkspaceFixture> _fixtureHelper = new();

    private protected ReferenceCountedDisposable<TWorkspaceFixture> GetOrCreateWorkspaceFixture()
        => _fixtureHelper.GetOrCreateFixture();

    private static async Task<bool> CanUseSpeculativeSemanticModelAsync(Document document, int position)
    {
        var service = document.GetLanguageService<ISyntaxFactsService>();
        var node = (await document.GetSyntaxRootAsync()).FindToken(position).Parent;

        return !service.GetMemberBodySpanForSpeculativeBinding(node).IsEmpty;
    }

    /// <summary>
    /// Specifies which overload of the <see cref="ITypeInferenceService"/> will be tested.
    /// </summary>
    public enum TestMode
    {
        /// <summary>
        /// Specifies the test is going to call into <see cref="ITypeInferenceService.InferTypes(SemanticModel, SyntaxNode, string, System.Threading.CancellationToken)"/>.
        /// </summary>
        Node,

        /// <summary>
        /// Specifies the test is going to call into <see cref="ITypeInferenceService.InferTypes(SemanticModel, int, string, System.Threading.CancellationToken)"/>.
        /// </summary>
        Position
    }

    protected async Task TestAsync(string text, string expectedType, TestMode mode,
        SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();

        MarkupTestFile.GetSpan(text.NormalizeLineEndings(), out text, out var textSpan);

        var document = workspaceFixture.Target.UpdateDocument(text, sourceCodeKind);
        await TestWorkerAsync(document, textSpan, expectedType, mode);

        if (await CanUseSpeculativeSemanticModelAsync(document, textSpan.Start))
        {
            var document2 = workspaceFixture.Target.UpdateDocument(text, sourceCodeKind, cleanBeforeUpdate: false);
            await TestWorkerAsync(document2, textSpan, expectedType, mode);
        }
    }

    protected abstract Task TestWorkerAsync(Document document, TextSpan textSpan, string expectedType, TestMode mode);
}
