﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Basic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicEncapsulateField : AbstractEditorTest
    {
        public BasicEncapsulateField(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicEncapsulateField))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        private const string TestSource = @"
Module Module1
        Public $$name As Integer? = 0
    Sub Main()
        name = 90
    End Sub
End Module";

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void EncapsulateThroughCommand()
        {
            SetUpEditor(TestSource);

            var encapsulateField = VisualStudio.EncapsulateField;
            var dialog = VisualStudio.PreviewChangesDialog;
            encapsulateField.Invoke();
            dialog.VerifyOpen(encapsulateField.DialogName);
            dialog.ClickCancel(encapsulateField.DialogName);
            dialog.VerifyClosed(encapsulateField.DialogName);
            encapsulateField.Invoke();
            dialog.VerifyOpen(encapsulateField.DialogName);
            dialog.ClickApplyAndWaitForFeature(encapsulateField.DialogName, FeatureAttribute.EncapsulateField);
            VisualStudio.Editor.Verify.TextContains(@"    Private _name As Integer? = 0

    Public Property Name As Integer?
        Get
            Return _name
        End Get
        Set(value As Integer?)
            _name = value
        End Set
    End Property");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void EncapsulateThroughLightbulbIncludingReferences()
        {
            SetUpEditor(TestSource);
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Encapsulate field: 'name' (and use property)", applyFix: true, blockUntilComplete: true);
            VisualStudio.Editor.Verify.TextContains(@"
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

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void EncapsulateThroughLightbulbDefinitionsOnly()
        {
            SetUpEditor(TestSource);
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Encapsulate field: 'name' (but still use field)", applyFix: true, blockUntilComplete: true);
            VisualStudio.Editor.Verify.TextContains(@"
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
