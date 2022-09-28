// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

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

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/38301"), Trait(Traits.Feature, Traits.Features.QuickInfo)]
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
                "class System.String\r\nRepresents text as a sequence of UTF-16 code units.To browse the .NET Framework source code for this type, see the Reference Source.",
                VisualStudio.Editor.GetQuickInfo());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo), Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
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
            Assert.Equal("class Program\r\nHello!", VisualStudio.Editor.GetQuickInfo());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo), Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
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
            Assert.Equal(@"class العربية123
This is an XML doc comment defined in code.", VisualStudio.Editor.GetQuickInfo());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo), Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
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
            var expected = "(awaitable) Task<int> C.M()\r\n\r\nExceptions:\r\n  Exception";
            Assert.Equal(expected, VisualStudio.Editor.GetQuickInfo());
        }
    }
}
