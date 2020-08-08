// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpFindReferences : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpFindReferences(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpFindReferences))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public void FindReferencesToCtor()
        {
            SetUpEditor(@"
class Program
{
}$$
");
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "File2.cs");
            VisualStudio.SolutionExplorer.OpenFile(project, "File2.cs");

            SetUpEditor(@"
class SomeOtherClass
{
    void M()
    {
        Program p = new Progr$$am();
    }
}
");

            VisualStudio.Editor.SendKeys(Shift(VirtualKey.F12));

            const string programReferencesCaption = "'Program' references";
            var results = VisualStudio.FindReferencesWindow.GetContents(programReferencesCaption);

            var activeWindowCaption = VisualStudio.Shell.GetActiveWindowCaption();
            Assert.Equal(expected: programReferencesCaption, actual: activeWindowCaption);

            Assert.Collection(
                results,
                new Action<Reference>[]
                {
                    reference =>
                    {
                        Assert.Equal(expected: "class Program", actual: reference.Code);
                        Assert.Equal(expected: 1, actual: reference.Line);
                        Assert.Equal(expected: 6, actual: reference.Column);
                    },
                    reference =>
                    {
                        Assert.Equal(expected: "Program p = new Program();", actual: reference.Code);
                        Assert.Equal(expected: 5, actual: reference.Line);
                        Assert.Equal(expected: 24, actual: reference.Column);
                    }
                });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public void FindReferencesToLocals()
        {
            using var telemetry = VisualStudio.EnableTestTelemetryChannel();
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

            VisualStudio.Editor.SendKeys(Shift(VirtualKey.F12));

            const string localReferencesCaption = "'local' references";
            var results = VisualStudio.FindReferencesWindow.GetContents(localReferencesCaption);

            var activeWindowCaption = VisualStudio.Shell.GetActiveWindowCaption();
            Assert.Equal(expected: localReferencesCaption, actual: activeWindowCaption);

            Assert.Collection(
                results,
                new Action<Reference>[]
                {
                    reference =>
                    {
                        Assert.Equal(expected: "int local = 1;", actual: reference.Code);
                        Assert.Equal(expected: 5, actual: reference.Line);
                        Assert.Equal(expected: 12, actual: reference.Column);
                    },
                    reference =>
                    {
                        Assert.Equal(expected: "Console.WriteLine(local);", actual: reference.Code);
                        Assert.Equal(expected: 6, actual: reference.Line);
                        Assert.Equal(expected: 26, actual: reference.Column);
                    }
                });

            telemetry.VerifyFired("vs/platform/findallreferences/search", "vs/ide/vbcs/commandhandler/findallreference");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)]
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

            VisualStudio.Editor.SendKeys(Shift(VirtualKey.F12));

            const string findReferencesCaption = "'\"1\"' references";
            var results = VisualStudio.FindReferencesWindow.GetContents(findReferencesCaption);

            var activeWindowCaption = VisualStudio.Shell.GetActiveWindowCaption();
            Assert.Equal(expected: findReferencesCaption, actual: activeWindowCaption);

            Assert.Collection(
                results,
                new Action<Reference>[]
                {
                    reference =>
                    {
                        Assert.Equal(expected: "string local = \"1\";", actual: reference.Code);
                        Assert.Equal(expected: 5, actual: reference.Line);
                        Assert.Equal(expected: 24, actual: reference.Column);
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

            // because the solution cache directory is stored in the user temp folder, 
            // closing the solution has no effect on what is returned.
            Assert.NotNull(VisualStudio.Workspace.GetWorkingFolder());
        }
    }
}
