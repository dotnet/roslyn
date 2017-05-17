// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Typescript
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class TypescriptCompletionTest : AbstractEditorTest
    {
        public TypescriptCompletionTest(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(TypescriptCompletionTest), WellKnownProjectTemplates.TypescriptNodeConsoleApp)
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "Lib.ts", contents:
@"export function foo(): string {
    return ""Hello World""
}");

            // Wait for current file to become part of the typescript project
            // and not miscellaneous files. Otherwise intellisense will not work.
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        protected override string LanguageName => "TypeScript";

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

        [Fact]
        public void CompleteFromNodeStdLib()
        {
            VisualStudio.Editor.SetText(@"import * as fs from 'fs';
fs.");
            VisualStudio.Editor.PlaceCaret("fs.", charsOffset: 1);
            VisualStudio.Editor.InvokeCompletionList();
            VisualStudio.Editor.Verify.CompletionItemsExist("chown");
        }
    }
}
