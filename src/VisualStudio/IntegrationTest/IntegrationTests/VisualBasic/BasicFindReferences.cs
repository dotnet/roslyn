// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

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

        [Fact, Trait(Traits.Feature, Traits.Features.FindReferences)]
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

            SendKeys(Shift(VirtualKey.F12));

            const string localReferencesCaption = "'local' references";
            var results = VisualStudio.Instance.FindReferencesWindow.GetContents(localReferencesCaption);

            var activeWindowCaption = VisualStudio.Instance.Shell.GetActiveWindowCaption();
            Assert.Equal(expected: localReferencesCaption, actual: activeWindowCaption);

            Assert.Collection(
                results,
                new Action<Reference>[]
                {
                    reference =>
                    {
                        Assert.Equal(expected: "Console.WriteLine(local)", actual: reference.Code);
                        Assert.Equal(expected: 4, actual: reference.Line);
                        Assert.Equal(expected: 24, actual: reference.Column);
                    }
                });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public void FindReferencesToSharedField()
        {
            SetUpEditor(@"
Class Program
    Public Shared Alpha As Int32
End Class$$
");

            VisualStudio.Instance.SolutionExplorer.AddFile(ProjectName, "File2.vb");
            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "File2.vb");

            SetUpEditor(@"
Class SomeOtherClass
    Sub M()
        Console.WriteLine(Program.$$Alpha)
    End Sub
End Class
");

            SendKeys(Shift(VirtualKey.F12));

            const string alphaReferencesCaption = "'Alpha' references";
            var results = VisualStudio.Instance.FindReferencesWindow.GetContents(alphaReferencesCaption);

            var activeWindowCaption = VisualStudio.Instance.Shell.GetActiveWindowCaption();
            Assert.Equal(expected: alphaReferencesCaption, actual: activeWindowCaption);

            Assert.Collection(
                results,
                new Action<Reference>[]
                {
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
