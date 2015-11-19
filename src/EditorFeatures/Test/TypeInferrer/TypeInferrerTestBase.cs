// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.TypeInferrer
{
    public abstract class TypeInferrerTestBase<TWorkspaceFixture> : TestBase, IClassFixture<TWorkspaceFixture>, IDisposable
        where TWorkspaceFixture : TestWorkspaceFixture, new()
    {
        protected TWorkspaceFixture fixture;

        protected TypeInferrerTestBase(TWorkspaceFixture workspaceFixture)
        {
            this.fixture = workspaceFixture;
        }

        public override void Dispose()
        {
            this.fixture.CloseTextViewAsync().Wait();
            base.Dispose();
        }

        private static bool CanUseSpeculativeSemanticModel(Document document, int position)
        {
            var service = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var node = document.GetSyntaxRootAsync().Result.FindToken(position).Parent;

            return !service.GetMemberBodySpanForSpeculativeBinding(node).IsEmpty;
        }

        protected async Task TestAsync(string text, string expectedType, bool testNode = true, bool testPosition = true)
        {
            TextSpan textSpan;
            MarkupTestFile.GetSpan(text.NormalizeLineEndings(), out text, out textSpan);

            if (testNode)
            {
                await TestWithAndWithoutSpeculativeSemanticModelAsync(text, textSpan, expectedType, useNodeStartPosition: false);
            }

            if (testPosition)
            {
                await TestWithAndWithoutSpeculativeSemanticModelAsync(text, textSpan, expectedType, useNodeStartPosition: true);
            }
        }

        private async Task TestWithAndWithoutSpeculativeSemanticModelAsync(
            string text,
            TextSpan textSpan,
            string expectedType,
            bool useNodeStartPosition)
        {
            var document = await fixture.UpdateDocumentAsync(text, SourceCodeKind.Regular);
            TestWorker(document, textSpan, expectedType, useNodeStartPosition);

            if (CanUseSpeculativeSemanticModel(document, textSpan.Start))
            {
                var document2 = await fixture.UpdateDocumentAsync(text, SourceCodeKind.Regular, cleanBeforeUpdate: false);
                TestWorker(document2, textSpan, expectedType, useNodeStartPosition);
            }
        }

        protected abstract void TestWorker(Document document, TextSpan textSpan, string expectedType, bool useNodeStartPosition);
    }
}
