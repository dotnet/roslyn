﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            this.fixture.CloseTextView();
            base.Dispose();
        }

        private static async Task<bool> CanUseSpeculativeSemanticModelAsync(Document document, int position)
        {
            var service = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var node = (await document.GetSyntaxRootAsync()).FindToken(position).Parent;

            return !service.GetMemberBodySpanForSpeculativeBinding(node).IsEmpty;
        }

        protected async Task TestAsync(string text, string expectedType, bool testNode = true, bool testPosition = true)
        {
            MarkupTestFile.GetSpan(text.NormalizeLineEndings(), out text, out var textSpan);

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
            var document = fixture.UpdateDocument(text, SourceCodeKind.Regular);
            await TestWorkerAsync(document, textSpan, expectedType, useNodeStartPosition);

            if (await CanUseSpeculativeSemanticModelAsync(document, textSpan.Start))
            {
                var document2 = fixture.UpdateDocument(text, SourceCodeKind.Regular, cleanBeforeUpdate: false);
                await TestWorkerAsync(document2, textSpan, expectedType, useNodeStartPosition);
            }
        }

        protected abstract Task TestWorkerAsync(Document document, TextSpan textSpan, string expectedType, bool useNodeStartPosition);
    }
}
