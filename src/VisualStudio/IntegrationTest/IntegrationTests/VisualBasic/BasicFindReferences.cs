// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            var findReferencesResults = VisualStudio.Instance.FindUsagesWindow.GetContents(localReferencesCaption);

            Assert.Equal(expected: 1, actual: findReferencesResults.Length);
            Assert.Equal(expected: "Console.WriteLine(local)", actual: findReferencesResults[0]);
        }
    }
}
