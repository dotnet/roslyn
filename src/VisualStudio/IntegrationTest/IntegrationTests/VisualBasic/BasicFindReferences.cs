// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicFindReferences : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicFindReferences() : base(nameof(BasicFindReferences)) { }

        [TestMethod, TestCategory(Traits.Features.FindReferences)]
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

            VisualStudioInstance.SendKeys.Send(Shift(VirtualKey.F12));

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
                        Assert.AreEqual(expected: "Dim local = 1", actual: reference.Code);
                        Assert.AreEqual(expected: 3, actual: reference.Line);
                        Assert.AreEqual(expected: 10, actual: reference.Column);
                    },
                    reference =>
                    {
                        Assert.AreEqual(expected: "Console.WriteLine(local)", actual: reference.Code);
                        Assert.AreEqual(expected: 4, actual: reference.Line);
                        Assert.AreEqual(expected: 24, actual: reference.Column);
                    }
                });
        }

        [TestMethod, TestCategory(Traits.Features.FindReferences)]
        public void FindReferencesToSharedField()
        {
            SetUpEditor(@"
Class Program
    Public Shared Alpha As Int32
End Class$$
");
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddFile(project, "File2.vb");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "File2.vb");

            SetUpEditor(@"
Class SomeOtherClass
    Sub M()
        Console.WriteLine(Program.$$Alpha)
    End Sub
End Class
");

            VisualStudioInstance.SendKeys.Send(Shift(VirtualKey.F12));

            const string alphaReferencesCaption = "'Alpha' references";
            var results = VisualStudioInstance.FindReferencesWindow.GetContents(alphaReferencesCaption);

            var activeWindowCaption = VisualStudioInstance.Shell.GetActiveWindowCaption();
            Assert.AreEqual(expected: alphaReferencesCaption, actual: activeWindowCaption);

            ExtendedAssert.Collection(
                results,
                new Action<Reference>[]
                {
                    reference =>
                    {
                        Assert.AreEqual(expected: "Public Shared Alpha As Int32", actual: reference.Code);
                        Assert.AreEqual(expected: 2, actual: reference.Line);
                        Assert.AreEqual(expected: 18, actual: reference.Column);
                    },
                    reference =>
                    {
                        Assert.AreEqual(expected: "Console.WriteLine(Program.Alpha)", actual: reference.Code);
                        Assert.AreEqual(expected: 3, actual: reference.Line);
                        Assert.AreEqual(expected: 34, actual: reference.Column);
                    }
                });
        }
    }
}
