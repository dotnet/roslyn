// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicGenerateTypeDialog : AbstractEditorTest
    {
        private const string GenerateTypeDialogID = "GenerateTypeDialog";

        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicGenerateTypeDialog(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicGenerateTypeDialog))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public void BasicToCSharp()
        {
            VisualStudio.Instance.SolutionExplorer.AddProject("CSProj", WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp);

            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "Class1.vb");

            SetUpEditor(@"
Class C
    Sub Method()
        $$Dim _A As A
    End Sub
End Class
");
            VerifyCodeAction("Generate new type...",
                applyFix: true,
                blockUntilComplete: false);

            VerifyDialog(GenerateTypeDialogID, isOpen: true);

            // Set access to Public
            Editor.DialogSelectComboBoxItem(GenerateTypeDialogID, "AccessList", "Public");

            // Set kind to structure
            Editor.DialogSelectComboBoxItem(GenerateTypeDialogID, "KindList", "Structure");

            // Set project to "CSProj"
            Editor.DialogSelectComboBoxItem(GenerateTypeDialogID, "ProjectList", "CSProj");

            // Choose new file
            Editor.DialogSelectRadioButton(GenerateTypeDialogID, "CreateNewFileRadioButton");

            // Set file name to "GenerateTypeTest"
            Editor.DialogSetElementValue(GenerateTypeDialogID, "CreateNewFileComboBox", "GenerateTypeTest.cs");

            // Click OK
            Editor.PressDialogButtonWithName(GenerateTypeDialogID, "OK");

            WaitForAsyncOperations(FeatureAttribute.LightBulb);

            VerifyDialog(GenerateTypeDialogID, isOpen: false);

            VerifyTextContains(@"Imports CSProj

Class C
    Sub Method()
        Dim _A As A
    End Sub
End Class
");

            VisualStudio.Instance.SolutionExplorer.OpenFile("CSProj", "GenerateTypeTest.cs");
            VerifyTextContains(@"namespace CSProj
{
    public struct A
    {
    }
}");
        }
    }
}
