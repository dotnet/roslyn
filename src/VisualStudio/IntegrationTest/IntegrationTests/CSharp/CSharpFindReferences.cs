// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

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

        [Fact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public void FindReferencesToCtor()
        {
            SetUpEditor(@"
class Program
{
}$$
");

            VisualStudio.Instance.SolutionExplorer.AddFile(ProjectName, "File2.cs");
            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "File2.cs");

            SetUpEditor(@"
class SomeOtherClass
{
    void M()
    {
        Program p = new Progr$$am();
    }
}
");

            SendKeys(Shift(VirtualKey.F12));

            VisualStudioWorkspaceOutOfProc.WaitForAsyncOperations(FeatureAttribute.FindReferences);

            const string programReferencesCaption = "'Program' references";
            var activeWindowCaption = VisualStudio.Instance.Shell.GetActiveWindowCaption();
            Assert.Equal(expected: programReferencesCaption, actual: activeWindowCaption);

            var results = VisualStudio.Instance.FindReferencesWindow.GetContents(programReferencesCaption);

            var reference = results.Single();
            Assert.Equal(expected: "Program p = new Program();", actual: reference.Code);
            Assert.Equal(expected: 5, actual: reference.Line);
            Assert.Equal(expected: 24, actual: reference.Column);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public void FindReferencesToLocals()
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

            SendKeys(Shift(VirtualKey.F12));

            VisualStudioWorkspaceOutOfProc.WaitForAsyncOperations(FeatureAttribute.FindReferences);

            const string localReferencesCaption = "'local' references";
            var activeWindowCaption = VisualStudio.Instance.Shell.GetActiveWindowCaption();
            Assert.Equal(expected: localReferencesCaption, actual: activeWindowCaption);

            var results = VisualStudio.Instance.FindReferencesWindow.GetContents(localReferencesCaption);

            var reference = results.Single();
            Assert.Equal(expected: "Console.WriteLine(local);", actual: reference.Code);
            Assert.Equal(expected: 6, actual: reference.Line);
            Assert.Equal(expected: 26, actual: reference.Column);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.FindReferences)]
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

            SendKeys(Shift(VirtualKey.F12));

            VisualStudioWorkspaceOutOfProc.WaitForAsyncOperations(FeatureAttribute.FindReferences);

            const string findReferencesCaption = "Find References";
            var activeWindowCaption = VisualStudio.Instance.Shell.GetActiveWindowCaption();
            Assert.Equal(expected: findReferencesCaption, actual: activeWindowCaption);

            var results = VisualStudio.Instance.FindReferencesWindow.GetContents(findReferencesCaption);

            var reference = results.Single();
            Assert.Equal(expected: "Search found no results", actual: reference.Code);
        }
    }
}
