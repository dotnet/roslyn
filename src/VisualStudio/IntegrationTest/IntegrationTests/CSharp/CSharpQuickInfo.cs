// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpQuickInfo : AbstractIdeEditorTest
    {
        public CSharpQuickInfo()
            : base(nameof(CSharpQuickInfo))
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        [IdeFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task QuickInfo_MetadataDocumentationAsync()
        {
            await SetUpEditorAsync(@"
///<summary>Hello!</summary>
class Program
{
    static void Main(string$$[] args)
    {
    }
}");
            await VisualStudio.Editor.InvokeQuickInfoAsync();
            Assert.Equal(
                "class\u200e System\u200e.String\r\nRepresents text as a series of Unicode characters.To browse the .NET Framework source code for this type, see the Reference Source.",
                await VisualStudio.Editor.GetQuickInfoAsync());
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task QuickInfo_DocumentationAsync()
        {
            await SetUpEditorAsync(@"
///<summary>Hello!</summary>
class Program$$
{
    static void Main(string[] args)
    {
    }
}");
            await VisualStudio.Editor.InvokeQuickInfoAsync();
            Assert.Equal("class\u200e Program\r\nHello!", await VisualStudio.Editor.GetQuickInfoAsync());
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task InternationalAsync()
        {
            await SetUpEditorAsync(@"
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
            await VisualStudio.Editor.InvokeQuickInfoAsync();
            Assert.Equal(@"class" + '\u200e' + @" العربية123
This is an XML doc comment defined in code.", await VisualStudio.Editor.GetQuickInfoAsync());
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task SectionOrderingAsync()
        {
            await SetUpEditorAsync(@"
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

            await VisualStudio.Editor.InvokeQuickInfoAsync();
            var expected = "\u200e(awaitable\u200e)\u200e Task\u200e<int\u200e>\u200e C\u200e.M\u200e(\u200e)\u000d\u000a\u000d\u000aUsage:\u000d\u000a  int\u200e x\u200e \u200e=\u200e await\u200e M\u200e(\u200e\u200e)\u200e;\u000d\u000a\u000d\u000aExceptions:\u200e\u000d\u000a\u200e  Exception";
            Assert.Equal(expected, await VisualStudio.Editor.GetQuickInfoAsync());
        }
    }
}
