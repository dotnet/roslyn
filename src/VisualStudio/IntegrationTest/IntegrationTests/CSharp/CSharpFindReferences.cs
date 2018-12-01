// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpFindReferences : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpFindReferences( )
            : base(nameof(CSharpFindReferences))
        {
        }

        [TestMethod, TestCategory(Traits.Features.FindReferences)]
        public void FindReferencesToCtor()
        {
            SetUpEditor(@"
class Program
{
}$$
");
            var project = new ProjectUtils.Project(ProjectName); ;
            VisualStudioInstance.SolutionExplorer.AddFile(project, "File2.cs");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "File2.cs");

            SetUpEditor(@"
class SomeOtherClass
{
    void M()
    {
        Program p = new Progr$$am();
    }
}
");

            VisualStudioInstance.Editor.SendKeys(Shift(VirtualKey.F12));

            const string programReferencesCaption = "'Program' references";
            var results = VisualStudioInstance.FindReferencesWindow.GetContents(programReferencesCaption);

            var activeWindowCaption = VisualStudioInstance.Shell.GetActiveWindowCaption();
            Assert.AreEqual(expected: programReferencesCaption, actual: activeWindowCaption);

            ExtendedAssert.Collection(
                results,
                new Action<Reference>[]
                {
                    reference =>
                    {
                        Assert.AreEqual(expected: "class Program", actual: reference.Code);
                        Assert.AreEqual(expected: 1, actual: reference.Line);
                        Assert.AreEqual(expected: 6, actual: reference.Column);
                    },
                    reference =>
                    {
                        Assert.AreEqual(expected: "Program p = new Program();", actual: reference.Code);
                        Assert.AreEqual(expected: 5, actual: reference.Line);
                        Assert.AreEqual(expected: 24, actual: reference.Column);
                    }
                });
        }

        [TestMethod, TestCategory(Traits.Features.FindReferences)]
        public void FindReferencesToLocals()
        {
            using (var telemetry = VisualStudioInstance.EnableTestTelemetryChannel())
            {
                SetUpEditor(@"
class Program
{
    static void Main()
    {
        int local = 1;
        Console.WriteLine(local$$);
    }
}
");

                VisualStudioInstance.Editor.SendKeys(Shift(VirtualKey.F12));

                const string localReferencesCaption = "'local' references";
                var results = VisualStudioInstance.FindReferencesWindow.GetContents(localReferencesCaption);

                var activeWindowCaption = VisualStudioInstance.Shell.GetActiveWindowCaption();
                Assert.AreEqual(expected: localReferencesCaption, actual: activeWindowCaption);

                ExtendedAssert.Collection(
                    results,
                    new Action<Reference>[]
                    {
                    reference =>
                    {
                        Assert.AreEqual(expected: "int local = 1;", actual: reference.Code);
                        Assert.AreEqual(expected: 5, actual: reference.Line);
                        Assert.AreEqual(expected: 12, actual: reference.Column);
                    },
                    reference =>
                    {
                        Assert.AreEqual(expected: "Console.WriteLine(local);", actual: reference.Code);
                        Assert.AreEqual(expected: 6, actual: reference.Line);
                        Assert.AreEqual(expected: 26, actual: reference.Column);
                    }
                    });

                telemetry.VerifyFired("vs/platform/findallreferences/search", "vs/ide/vbcs/commandhandler/findallreference");
            }
        }

        [TestMethod, TestCategory(Traits.Features.FindReferences)]
        public void FindReferencesToString()
        {
            SetUpEditor(@"
class Program
{
    static void Main()
    {
         string local = ""1""$$;
    }
}
");

            VisualStudioInstance.Editor.SendKeys(Shift(VirtualKey.F12));

            const string findReferencesCaption = "'\"1\"' references";
            var results = VisualStudioInstance.FindReferencesWindow.GetContents(findReferencesCaption);

            var activeWindowCaption = VisualStudioInstance.Shell.GetActiveWindowCaption();
            Assert.AreEqual(expected: findReferencesCaption, actual: activeWindowCaption);

            ExtendedAssert.Collection(
                results,
                new Action<Reference>[]
                {
                    reference =>
                    {
                        Assert.AreEqual(expected: "string local = \"1\";", actual: reference.Code);
                        Assert.AreEqual(expected: 5, actual: reference.Line);
                        Assert.AreEqual(expected: 24, actual: reference.Column);
                    }
                });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public void VerifyWorkingFolder()
        {
            SetUpEditor(@"class EmptyContent {$$}");

            // verify working folder has set
            Assert.NotNull(VisualStudio.Workspace.GetWorkingFolder());

            VisualStudio.SolutionExplorer.CloseSolution();

            // verify working folder has not set
            Assert.Null(VisualStudio.Workspace.GetWorkingFolder());
        }
    }
}
