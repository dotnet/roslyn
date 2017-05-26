// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Typescript
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class TypescriptSignatureHelperTest : AbstractTypescriptIntegrationTest
    {
        public TypescriptSignatureHelperTest(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(TypescriptCompletionTest))
        {
        }

        [Fact]
        public void ShowSignature()
        {
            VisualStudio.Editor.SetText(@"var v = 0;
v.toExponential(");

            VisualStudio.Editor.InvokeSignatureHelp();
            var currentSignature = VisualStudio.Editor.GetCurrentSignature();
            Assert.StartsWith("toExponential", currentSignature.Content);
        }
    }
}
