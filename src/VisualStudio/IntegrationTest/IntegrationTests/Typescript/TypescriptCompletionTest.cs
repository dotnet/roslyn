// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Typescript
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class TypescriptCompletionTest : AbstractTypescriptIntegrationTest
    {
        public TypescriptCompletionTest(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(TypescriptCompletionTest))
        {
        }

        [Fact]
        public void CompleteNumberMethod()
        {
            VisualStudio.Editor.SetText(@"var v = 0;
v");

            VisualStudio.SendKeys.Send(".");
            VisualStudio.Editor.Verify.CompletionItemsExist("toExponential");
            VisualStudio.SendKeys.Send("toe", VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("v.toExponential$$", assertCaretPosition: true);
        }

        [Fact]
        public void CompleteImportPath()
        {
            VisualStudio.Editor.SetText("import * as lib from '.';");
            VisualStudio.Editor.PlaceCaret(".", charsOffset: 1);

            VisualStudio.SendKeys.Send("/");
            VisualStudio.Editor.Verify.CompletionItemsExist("Lib");
        }

        [Fact]
        public void CompleteExportedFunction()
        {
            VisualStudio.Editor.SetText(@"import * as lib from './Lib';
lib");

            VisualStudio.SendKeys.Send(".");
            VisualStudio.Editor.Verify.CompletionItemsExist("foo");
        }
    }
}
