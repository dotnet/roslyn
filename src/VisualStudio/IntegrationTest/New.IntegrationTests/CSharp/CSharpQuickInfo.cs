// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.QuickInfo)]
    public class CSharpQuickInfo : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpQuickInfo()
            : base(nameof(CSharpQuickInfo))
        {
        }

        [IdeFact]
        public async Task QuickInfo_MetadataDocumentation()
        {
            await SetUpEditorAsync(@"
///<summary>Hello!</summary>
class Program
{
    static void Main(string$$[] args)
    {
    }
}", HangMitigatingCancellationToken);
            await TestServices.Editor.InvokeQuickInfoAsync(HangMitigatingCancellationToken);
            var quickInfo = await TestServices.Editor.GetQuickInfoAsync(HangMitigatingCancellationToken);
            Assert.Equal(
                "class System.String\r\nRepresents text as a sequence of UTF-16 code units.To browse the .NET Framework source code for this type, see the Reference Source.",
                quickInfo);
        }

        [IdeFact, Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
        public async Task QuickInfo_Documentation()
        {
            await SetUpEditorAsync(@"
///<summary>Hello!</summary>
class Program$$
{
    static void Main(string[] args)
    {
    }
}", HangMitigatingCancellationToken);
            await TestServices.Editor.InvokeQuickInfoAsync(HangMitigatingCancellationToken);
            var quickInfo = await TestServices.Editor.GetQuickInfoAsync(HangMitigatingCancellationToken);
            Assert.Equal("class Program\r\nHello!", quickInfo);
        }

        [IdeFact, Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
        public async Task International()
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
}", HangMitigatingCancellationToken);
            await TestServices.Editor.InvokeQuickInfoAsync(HangMitigatingCancellationToken);
            var quickInfo = await TestServices.Editor.GetQuickInfoAsync(HangMitigatingCancellationToken);
            Assert.Equal(@"class العربية123
This is an XML doc comment defined in code.", quickInfo);
        }

        [IdeFact, Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
        public async Task SectionOrdering()
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
        }", HangMitigatingCancellationToken);

            await TestServices.Editor.InvokeQuickInfoAsync(HangMitigatingCancellationToken);
            var quickInfo = await TestServices.Editor.GetQuickInfoAsync(HangMitigatingCancellationToken);
            var expected = "(awaitable) Task<int> C.M()\r\n\r\nExceptions:\r\n  Exception";
            Assert.Equal(expected, quickInfo);
        }
    }
}
