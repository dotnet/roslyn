// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
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
        public void Locals()
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

            VisualStudioWorkspaceOutOfProc.WaitForAsyncOperations(FeatureAttribute.FindReferences);

            var activeWindowCaption = VisualStudio.Instance.Shell.GetActiveWindowCaption();

            const string localReferencesCaption = "'local' references";
            Assert.Equal(expected: localReferencesCaption, actual: activeWindowCaption);

            var findReferencesResults = VisualStudio.Instance.FindReferencesWindow.GetContents(localReferencesCaption);

            var reference = findReferencesResults.Single();
            Assert.Equal(expected: "Console.WriteLine(local)", actual: reference.Code);
            Assert.Equal(expected: 4, actual: reference.Line);
            Assert.Equal(expected: 24, actual: reference.Column);
        }
    }
}
