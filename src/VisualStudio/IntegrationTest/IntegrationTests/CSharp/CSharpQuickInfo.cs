// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpQuickInfo : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpQuickInfo(VisualStudioInstanceFactory instanceFactory)
            : base(nameof(CSharpQuickInfo))
        {
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.QuickInfo)]
        public void QuickInfo_MetadataDocumentation()
        {
            SetUpEditor(@"
///<summary>Hello!</summary>
class Program
{
    static void Main(string$$[] args)
    {
    }
}");
            VisualStudioInstance.Editor.InvokeQuickInfo();
            Assert.AreEqual(
                "class System.String\r\nRepresents text as a sequence of UTF-16 code units.To browse the .NET Framework source code for this type, see the Reference Source.",
                VisualStudioInstance.Editor.GetQuickInfo());
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.QuickInfo)]
        public void QuickInfo_Documentation()
        {
            SetUpEditor(@"
///<summary>Hello!</summary>
class Program$$
{
    static void Main(string[] args)
    {
    }
}");
            VisualStudioInstance.Editor.InvokeQuickInfo();
            Assert.AreEqual("class Program\r\nHello!", VisualStudioInstance.Editor.GetQuickInfo());
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.QuickInfo)]
        public void International()
        {
            SetUpEditor(@"
/// <summary>
/// This is an XML doc comment defined in code.
/// </summary>
class العربية123
{
    static void Main()
    {
         العربية123$$ goo;
    }
}");
            VisualStudioInstance.Editor.InvokeQuickInfo();
            Assert.AreEqual(@"class العربية123
This is an XML doc comment defined in code.", VisualStudioInstance.Editor.GetQuickInfo());
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.QuickInfo)]
        public void SectionOrdering()
        {
            SetUpEditor(@"
using System;
using System.Threading.Tasks;

class C
{
    /// <exception cref=""Exception""></exception>
    async Task <int> M()
    {
                return await M$$();
            }
        }");

            VisualStudioInstance.Editor.InvokeQuickInfo();
            var expected = "(awaitable) Task<int> C.M()\r\n\r\nUsage:\r\n  int x = await M();\r\n\r\nExceptions:\r\n  Exception";
            Assert.AreEqual(expected, VisualStudioInstance.Editor.GetQuickInfo());
        }
    }
}
