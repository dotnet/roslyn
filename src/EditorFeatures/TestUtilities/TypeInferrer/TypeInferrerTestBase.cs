// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.TypeInferrer
{
    [UseExportProvider]
    public abstract class TypeInferrerTestBase<TWorkspaceFixture> : TestBase, IClassFixture<TWorkspaceFixture>, IDisposable
        where TWorkspaceFixture : TestWorkspaceFixture, new()
    {
        protected readonly TWorkspaceFixture fixture;

        protected TypeInferrerTestBase(TWorkspaceFixture workspaceFixture)
        {
            this.fixture = workspaceFixture;
        }

        public override void Dispose()
        {
            this.fixture.DisposeAfterTest();
            base.Dispose();
        }

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
            MarkupTestFile.GetSpan(text.NormalizeLineEndings(), out text, out var textSpan);

            var document = fixture.UpdateDocument(text, sourceCodeKind);
            await TestWorkerAsync(document, textSpan, expectedType, mode);

            if (await CanUseSpeculativeSemanticModelAsync(document, textSpan.Start))
            {
                var document2 = fixture.UpdateDocument(text, sourceCodeKind, cleanBeforeUpdate: false);
                await TestWorkerAsync(document2, textSpan, expectedType, mode);
            }
        }

        protected abstract Task TestWorkerAsync(Document document, TextSpan textSpan, string expectedType, TestMode mode);
    }
}
