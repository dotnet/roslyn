// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Roslyn.VisualStudio.IntegrationTests.Basic
{
    [TestClass]
    public class BasicEncapsulateField : AbstractEditorTest
    {
        public BasicEncapsulateField() : base(nameof(BasicEncapsulateField)) { }

        protected override string LanguageName => LanguageNames.VisualBasic;

        private const string TestSource = @"
Module Module1
        Public $$name As Integer? = 0
    Sub Main()
        name = 90
    End Sub
End Module";

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/19816")]
        [TestCategory(Traits.Features.EncapsulateField)]
        public void EncapsulateThroughCommand()
        {
            SetUpEditor(TestSource);

            var encapsulateField = VisualStudioInstance.EncapsulateField;
            var dialog = VisualStudioInstance.PreviewChangesDialog;
            encapsulateField.Invoke();
            dialog.VerifyOpen(encapsulateField.DialogName, timeout: Helper.HangMitigatingTimeout);
            dialog.ClickCancel(encapsulateField.DialogName);
            dialog.VerifyClosed(encapsulateField.DialogName);
            encapsulateField.Invoke();
            dialog.VerifyOpen(encapsulateField.DialogName, timeout: Helper.HangMitigatingTimeout);
            dialog.ClickApplyAndWaitForFeature(encapsulateField.DialogName, FeatureAttribute.EncapsulateField);
            VisualStudioInstance.Editor.Verify.TextContains(@"    Private _name As Integer? = 0

    Public Property Name As Integer?
        Get
            Return _name
        End Get
        Set(value As Integer?)
            _name = value
        End Set
    End Property");
        }

        [TestMethod, TestCategory(Traits.Features.EncapsulateField)]
        public void EncapsulateThroughLightbulbIncludingReferences()
        {
            SetUpEditor(TestSource);
            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction("Encapsulate field: 'name' (and use property)", applyFix: true, blockUntilComplete: true);
            VisualStudioInstance.Editor.Verify.TextContains(@"
Module Module1
    Private _name As Integer? = 0

    Public Property Name As Integer?
        Get
            Return _name
        End Get
        Set(value As Integer?)
            _name = value
        End Set
    End Property

    Sub Main()
        Name = 90
    End Sub
End Module");
        }

        [TestMethod, TestCategory(Traits.Features.EncapsulateField)]
        public void EncapsulateThroughLightbulbDefinitionsOnly()
        {
            SetUpEditor(TestSource);
            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction("Encapsulate field: 'name' (but still use field)", applyFix: true, blockUntilComplete: true);
            VisualStudioInstance.Editor.Verify.TextContains(@"
Module Module1
    Private _name As Integer? = 0

    Public Property Name As Integer?
        Get
            Return _name
        End Get
        Set(value As Integer?)
            _name = value
        End Set
    End Property

    Sub Main()
        name = 90
    End Sub
End Module");
        }
    }
}
