// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpQuickInfo : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpQuickInfo(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpQuickInfo))
        {
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/19914"), Trait(Traits.Feature, Traits.Features.QuickInfo)]
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
            VisualStudio.Editor.InvokeQuickInfo();
            Assert.Equal(
                "class\u200e System\u200e.String\r\nRepresents text as a sequence of UTF-16 code units.To browse the .NET Framework source code for this type, see the Reference Source.",
                VisualStudio.Editor.GetQuickInfo());
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/19914"), Trait(Traits.Feature, Traits.Features.QuickInfo)]
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
            VisualStudio.Editor.InvokeQuickInfo();
            Assert.Equal("class\u200e Program\r\nHello!", VisualStudio.Editor.GetQuickInfo());
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/19914"), Trait(Traits.Feature, Traits.Features.QuickInfo)]
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
            VisualStudio.Editor.InvokeQuickInfo();
            Assert.Equal(@"class" + '\u200e' + @" العربية123
This is an XML doc comment defined in code.", VisualStudio.Editor.GetQuickInfo());
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/19914"), Trait(Traits.Feature, Traits.Features.QuickInfo)]
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

            VisualStudio.Editor.InvokeQuickInfo();
            var expected = "\u200e(awaitable\u200e)\u200e Task\u200e<int\u200e>\u200e C\u200e.M\u200e(\u200e)\u000d\u000a\u000d\u000aUsage:\u000d\u000a  int\u200e x\u200e \u200e=\u200e await\u200e M\u200e(\u200e\u200e)\u200e;\u000d\u000a\u000d\u000aExceptions:\u200e\u000d\u000a\u200e  Exception";
            Assert.Equal(expected, VisualStudio.Editor.GetQuickInfo());
        }
    }
}
