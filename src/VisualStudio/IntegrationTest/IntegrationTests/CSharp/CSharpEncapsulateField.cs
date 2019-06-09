// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpEncapsulateField : AbstractEditorTest
    {
        public CSharpEncapsulateField(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(CSharpEncapsulateField))
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

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/35701")]
        [Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void EncapsulateThroughCommand()
        {
            SetUpEditor(TestSource);
            var encapsulateField = VisualStudio.EncapsulateField;
            var dialog = VisualStudio.PreviewChangesDialog;
            encapsulateField.Invoke();
            dialog.VerifyOpen(encapsulateField.DialogName, timeout: Helper.HangMitigatingTimeout);
            dialog.ClickCancel(encapsulateField.DialogName);
            dialog.VerifyClosed(encapsulateField.DialogName);
            encapsulateField.Invoke();
            dialog.VerifyOpen(encapsulateField.DialogName, timeout: Helper.HangMitigatingTimeout);
            dialog.ClickApplyAndWaitForFeature(encapsulateField.DialogName, FeatureAttribute.EncapsulateField);
            VisualStudio.Editor.Verify.TextContains("public static int? Param { get => param; set => param = value; }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void EncapsulateThroughLightbulbIncludingReferences()
        {
            SetUpEditor(TestSource);
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Encapsulate field: 'param' (and use property)", applyFix: true, blockUntilComplete: true);
            VisualStudio.Editor.Verify.TextContains(@"
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void EncapsulateThroughLightbulbDefinitionsOnly()
        {
            SetUpEditor(TestSource);
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Encapsulate field: 'param' (but still use field)", applyFix: true, blockUntilComplete: true);
            VisualStudio.Editor.Verify.TextContains(@"
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
