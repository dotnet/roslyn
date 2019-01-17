// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpGoToDefinition : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpGoToDefinition()
            : base(nameof(CSharpGoToDefinition))
        {
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.GoToDefinition)]
        public void GoToClassDeclaration()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddFile(project, "FileDef.cs");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "FileDef.cs");
            VisualStudioInstance.Editor.SetText(
@"class SomeClass
{
}");
            VisualStudioInstance.SolutionExplorer.AddFile(project, "FileConsumer.cs");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "FileConsumer.cs");
            VisualStudioInstance.Editor.SetText(
@"class SomeOtherClass
{
    SomeClass sc;
}");
            VisualStudioInstance.Editor.PlaceCaret("SomeClass");
            VisualStudioInstance.Editor.GoToDefinition();
            VisualStudioInstance.Editor.Verify.TextContains(@"class SomeClass$$", assertCaretPosition: true);
            Assert.IsFalse(VisualStudioInstance.Shell.IsActiveTabProvisional());
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.GoToDefinition)]
        public void GoToDefinitionOpensProvisionalTabIfDocumentNotAlreadyOpen()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddFile(project, "FileDef.cs");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "FileDef.cs");
            VisualStudioInstance.Editor.SetText(
@"class SomeClass
{
}
");
            VisualStudioInstance.SolutionExplorer.CloseFile(project, "FileDef.cs", saveFile: true);
            VisualStudioInstance.SolutionExplorer.AddFile(project, "FileConsumer.cs");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "FileConsumer.cs");
            VisualStudioInstance.Editor.SetText(
@"class SomeOtherClass
{
    SomeClass sc;
}");
            VisualStudioInstance.Editor.PlaceCaret("SomeClass");
            VisualStudioInstance.Editor.GoToDefinition();
            VisualStudioInstance.Editor.Verify.TextContains(@"class SomeClass$$", assertCaretPosition: true);
            Assert.IsTrue(VisualStudioInstance.Shell.IsActiveTabProvisional());
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.GoToDefinition)]
        public void GoToDefinitionWithMultipleResults()
        {
            SetUpEditor(
@"partial class /*Marker*/ $$PartialClass { }

partial class PartialClass { int i = 0; }");

            VisualStudioInstance.Editor.GoToDefinition();

            const string programReferencesCaption = "'PartialClass' declarations";
            var results = VisualStudioInstance.FindReferencesWindow.GetContents(programReferencesCaption);

            var activeWindowCaption = VisualStudioInstance.Shell.GetActiveWindowCaption();
            Assert.AreEqual(expected: programReferencesCaption, actual: activeWindowCaption);

            ExtendedAssert.Collection(
                results,
                new Action<Reference>[]
                {
                    reference =>
                    {
                        Assert.AreEqual(expected: "partial class /*Marker*/ PartialClass { }", actual: reference.Code);
                        Assert.AreEqual(expected: 0, actual: reference.Line);
                        Assert.AreEqual(expected: 25, actual: reference.Column);
                    },
                    reference =>
                    {
                        Assert.AreEqual(expected: "partial class PartialClass { int i = 0; }", actual: reference.Code);
                        Assert.AreEqual(expected: 2, actual: reference.Line);
                        Assert.AreEqual(expected: 14, actual: reference.Column);
                    }
                });
        }
    }
}
