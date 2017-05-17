// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Typescript
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class TypescriptSignatureHelperTest : AbstractEditorTest
    {
        public TypescriptSignatureHelperTest(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(TypescriptCompletionTest), WellKnownProjectTemplates.TypescriptNodeConsoleApp)
        {
            // Wait for current file to become part of the typescript project
            // and not miscellaneous files. Otherwise intellisense will not work.
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        protected override string LanguageName => "TypeScript";

        [Fact]
        public void ShowSignature()
        {
            VisualStudio.Editor.SetText(@"var v = 0;
v.toExponential(");

            VisualStudio.Editor.InvokeSignatureHelp();
            VisualStudio.Editor.Verify.CurrentSignature(
                "toExponential([fractionDigits?: number]): string\r\n" +
                "Returns a string containing a number represented in exponential notation.");
        }
    }
}
