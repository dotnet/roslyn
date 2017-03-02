// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Windows.Automation;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicChangeSignatureDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        private const string ChangeSignatureDialogAutomationId = "ChangeSignatureDialog";

        public BasicChangeSignatureDialog(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicChangeSignatureDialog))
        {
        }
        
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/17393"),
         Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void VerifyCodeRefactoringOffered()
        {
            SetUpEditor(@"
Class C
    Sub Method$$(a As Integer, b As Integer)
    End Sub
End Class");

            InvokeCodeActionList();
            VerifyCodeAction("Change signature...", applyFix: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void VerifyRefactoringCancelled()
        {

            SetUpEditor(@"
Class C
    Sub Method$$(a As Integer, b As String)
    End Sub
End Class");

            InvokeChangeSignature();
            VerifyChangeSignatureDialog(isOpen: true);
            ClickCancel();
            VerifyChangeSignatureDialog(isOpen: false);

            VerifyTextContains(@"
Class C
    Sub Method(a As Integer, b As String)
    End Sub
End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void VerifyReorderParameters()
        {
            SetUpEditor(@"
Class C
    Sub Method$$(a As Integer, b As String)
    End Sub
End Class");

            InvokeChangeSignature();
            VerifyChangeSignatureDialog(isOpen: true);
            SelectParameter("Integer a");
            ClickDownButton();
            ClickOK();
            VerifyChangeSignatureDialog(isOpen: false);

            VerifyTextContains(@"
Class C
    Sub Method(b As String, a As Integer)
    End Sub
End Class");
        }

        private void InvokeChangeSignature()
        {
            Editor.SendKeys(Ctrl(VirtualKey.R), Ctrl(VirtualKey.V));
        }

        private void VerifyChangeSignatureDialog(bool isOpen)
        {
            VerifyDialog(ChangeSignatureDialogAutomationId, isOpen);
        }

        private void ClickOK()
        {
            PressDialogButton(ChangeSignatureDialogAutomationId, "OKButton");
        }

        private void ClickCancel()
        {
            PressDialogButton(ChangeSignatureDialogAutomationId, "CancelButton");
        }

        private void ClickDownButton()
        {
            PressDialogButton(ChangeSignatureDialogAutomationId, "DownButton");
        }

        private void ClickUpButton()
        {
            PressDialogButton(ChangeSignatureDialogAutomationId, "UpButton");
        }

        private void SelectParameter(string parameterName)
        {
            var dialogAutomationElement = GetDialog(ChangeSignatureDialogAutomationId);

            Condition propertyCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, "MemberSelectionList");
            var grid = dialogAutomationElement.FindFirst(TreeScope.Descendants, propertyCondition);

            var gridPattern = grid.GetCurrentPattern(GridPattern.Pattern) as GridPattern;
            var rowCount = (int)grid.GetCurrentPropertyValue(GridPattern.RowCountProperty);
            var columnToSelect = 2;
            int i = 0;
            for (; i < rowCount; i++)
            {
                // Modifier | Type | Parameter | Default
                var item = gridPattern.GetItem(i, columnToSelect);
                var name = item.GetCurrentPropertyValue(AutomationElement.NameProperty) as string;
                if (name == parameterName)
                {
                    // The parent of a cell is of DataItem control type, which support SelectionItemPattern.
                    TreeWalker walker = TreeWalker.ControlViewWalker;
                    var parent = walker.GetParent(item);
                    object pattern;
                    if (parent.TryGetCurrentPattern(SelectionItemPattern.Pattern, out pattern))
                    {
                        (pattern as SelectionItemPattern).Select();
                    }
                    else
                    {
                        AssertEx.Fail("Unexpected error. Item's parent is expected to support SelectionItemPattern.");
                    }

                    return;
                }
            }

            if (i == rowCount)
            {
                AssertEx.Fail($"Unable to find the parameter {parameterName}");
            }
        }
    }
}
