// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpEncapsulateField : AbstractEditorTest
    {
        public CSharpEncapsulateField( )
            : base( nameof(CSharpEncapsulateField))
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        private const string TestSource = @"
namespace myNamespace
{
    class Program
    {
        private static int? $$param = 0;
        static void Main(string[] args)
        {
            param = 80;
        }
    }
}";

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
            VisualStudioInstance.Editor.Verify.TextContains("public static int? Param { get => param; set => param = value; }");
        }

        [TestMethod, TestCategory(Traits.Features.EncapsulateField)]
        public void EncapsulateThroughLightbulbIncludingReferences()
        {
            SetUpEditor(TestSource);
            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction("Encapsulate field: 'param' (and use property)", applyFix: true, blockUntilComplete: true);
            VisualStudioInstance.Editor.Verify.TextContains(@"
namespace myNamespace
{
    class Program
    {
        private static int? param = 0;

        public static int? Param { get => param; set => param = value; }

        static void Main(string[] args)
        {
            Param = 80;
        }
    }
}");
        }

        [TestMethod, TestCategory(Traits.Features.EncapsulateField)]
        public void EncapsulateThroughLightbulbDefinitionsOnly()
        {
            SetUpEditor(TestSource);
            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction("Encapsulate field: 'param' (but still use field)", applyFix: true, blockUntilComplete: true);
            VisualStudioInstance.Editor.Verify.TextContains(@"
namespace myNamespace
{
    class Program
    {
        private static int? param = 0;

        public static int? Param { get => param; set => param = value; }

        static void Main(string[] args)
        {
            param = 80;
        }
    }
}");
        }
    }
}
