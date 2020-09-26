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

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicFindReferences : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicFindReferences(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicFindReferences))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public void FindReferencesToLocals()
        {
            SetUpEditor(@"
Class Program
  Sub Main()
      Dim local = 1
      Console.WriteLine(loca$$l)
  End Sub
End Class
");

            VisualStudio.SendKeys.Send(Shift(VirtualKey.F12));

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
                        Assert.Equal(expected: "Dim local = 1", actual: reference.Code);
                        Assert.Equal(expected: 3, actual: reference.Line);
                        Assert.Equal(expected: 10, actual: reference.Column);
                    },
                    reference =>
                    {
                        Assert.Equal(expected: "Console.WriteLine(local)", actual: reference.Code);
                        Assert.Equal(expected: 4, actual: reference.Line);
                        Assert.Equal(expected: 24, actual: reference.Column);
                    }
                });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public void FindReferencesToSharedField()
        {
            SetUpEditor(@"
Class Program
    Public Shared Alpha As Int32
End Class$$
");
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "File2.vb");
            VisualStudio.SolutionExplorer.OpenFile(project, "File2.vb");

            SetUpEditor(@"
Class SomeOtherClass
    Sub M()
        Console.WriteLine(Program.$$Alpha)
    End Sub
End Class
");

            VisualStudio.SendKeys.Send(Shift(VirtualKey.F12));

            const string alphaReferencesCaption = "'Alpha' references";
            var results = VisualStudio.FindReferencesWindow.GetContents(alphaReferencesCaption);

            var activeWindowCaption = VisualStudio.Shell.GetActiveWindowCaption();
            Assert.Equal(expected: alphaReferencesCaption, actual: activeWindowCaption);

            Assert.Collection(
                results,
                new Action<Reference>[]
                {
                    reference =>
                    {
                        Assert.Equal(expected: "Public Shared Alpha As Int32", actual: reference.Code);
                        Assert.Equal(expected: 2, actual: reference.Line);
                        Assert.Equal(expected: 18, actual: reference.Column);
                    },
                    reference =>
                    {
                        Assert.Equal(expected: "Console.WriteLine(Program.Alpha)", actual: reference.Code);
                        Assert.Equal(expected: 3, actual: reference.Line);
                        Assert.Equal(expected: 34, actual: reference.Column);
                    }
                });
        }
    }
}
