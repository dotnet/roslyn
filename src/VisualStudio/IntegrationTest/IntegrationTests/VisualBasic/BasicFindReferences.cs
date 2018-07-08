// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicFindReferences : AbstractIdeEditorTest
    {
        public BasicFindReferences()
            : base(nameof(BasicFindReferences))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public async Task FindReferencesToLocalsAsync()
        {
            await SetUpEditorAsync(@"
Class Program
  Sub Main()
      Dim local = 1
      Console.WriteLine(loca$$l)
  End Sub
End Class
");

            await VisualStudio.SendKeys.SendAsync(Shift(VirtualKey.F12));

            const string localReferencesCaption = "'local' references";
            var results = await VisualStudio.FindReferencesWindow.GetContentsAsync(localReferencesCaption);

            var activeWindowCaption = await VisualStudio.Shell.GetActiveWindowCaptionAsync();
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

        [IdeFact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public async Task FindReferencesToSharedFieldAsync()
        {
            await SetUpEditorAsync(@"
Class Program
    Public Shared Alpha As Int32
End Class$$
");
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "File2.vb");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "File2.vb");

            await SetUpEditorAsync(@"
Class SomeOtherClass
    Sub M()
        Console.WriteLine(Program.$$Alpha)
    End Sub
End Class
");

            await VisualStudio.SendKeys.SendAsync(Shift(VirtualKey.F12));

            const string alphaReferencesCaption = "'Alpha' references";
            var results = await VisualStudio.FindReferencesWindow.GetContentsAsync(alphaReferencesCaption);

            var activeWindowCaption = await VisualStudio.Shell.GetActiveWindowCaptionAsync();
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
