// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpGenerateFromUsage : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpGenerateFromUsage( )
            : base( nameof(CSharpGenerateFromUsage))
        {
        }

        [TestMethod, TestCategory(Traits.Features.CodeActionsGenerateLocal)]
        public void GenerateLocal()
        {
            SetUpEditor(
@"class Program
{
    static void Main(string[] args)
    {
        string s = $$xyz;
    }
}");
            VisualStudioInstance.Editor.Verify.CodeAction("Generate local 'xyz'", applyFix: true);
            VisualStudioInstance.Editor.Verify.TextContains(
@"class Program
{
    static void Main(string[] args)
    {
        string xyz = null;
        string s = xyz;
    }
}");
        }
    }
}
