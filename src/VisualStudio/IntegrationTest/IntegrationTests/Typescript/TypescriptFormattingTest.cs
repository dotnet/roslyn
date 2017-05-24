// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Typescript
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class TypescriptFormattingTest : AbstractTypescriptIntegrationTest
    {
        public TypescriptFormattingTest(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(TypescriptCompletionTest))
        {
        }

        [Fact]
        public void FormatAssignment()
        {
            VisualStudio.Editor.SetText("var   v =   0;");

            VisualStudio.SendKeys.Send(VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains("var v = 0;");
        }
    }
}
