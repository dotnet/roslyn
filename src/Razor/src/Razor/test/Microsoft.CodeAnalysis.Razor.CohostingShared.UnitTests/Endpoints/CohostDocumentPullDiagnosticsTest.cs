// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public partial class CohostDocumentPullDiagnosticsTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public Task NoDiagnostics()
        => VerifyDiagnosticsAsync("""
            <div></div>

            @code
            {
                public void IJustMetYou()
                {
                }
            }
            """);

    [Fact]
    public Task CSharp()
        => VerifyDiagnosticsAsync("""
            <div></div>

            @code
            {
                public void IJustMetYou()
                {
                    {|CS0103:CallMeMaybe|}();
                }
            }
            """);

    [Fact]
    public Task Razor()
        => VerifyDiagnosticsAsync("""
            <div>

            {|RZ10012:<NonExistentComponent />|}

            </div>
            """);

    [Fact]
    public Task CSharpAndRazor_MiscellaneousFile()
        => VerifyDiagnosticsAsync("""
            <div>

            {|RZ10012:<NonExistentComponent />|}

            </div>

            @code
            {
                public void IJustMetYou()
                {
                    {|CS0103:CallMeMaybe|}();
                }
            }
            """,
            miscellaneousFile: true);

    [Fact]
    public Task CombinedAndNestedDiagnostics()
        => VerifyDiagnosticsAsync("""
            @using System.Threading.Tasks;

            <div>

            {|RZ10012:<NonExistentComponent />|}

            @code
            {
                public void IJustMetYou()
                {
                    {|CS0103:CallMeMaybe|}();
                }
            }

            <div>
                @{
                    {|CS4033:await Task.{|CS1501:Delay|}()|};
                }

                {|RZ9980:<p>|}
            </div>

            </div>
            """);

    [Fact]
    public Task CSharpUnusedUsings()
       => VerifyDiagnosticsAsync("""
            {|RZ0005:@using System|}
            @using System.Text

            <div></div>

            @code
            {
                public void BuildsStrings(StringBuilder b)
                {
                }
            }
            """);

    [Fact]
    public Task RazorUsingAlsoPresentInImports()
       => VerifyDiagnosticsAsync("""
            @using System.Text
            {|RZ0005:@using Microsoft.AspNetCore.Components.Forms|}

            <div></div>

            <PageTitle></PageTitle>

            @code
            {
                public void BuildsStrings(StringBuilder b)
                {
                }
            }
            """);

    [Fact]
    public Task RazorUsedUsings()
        => VerifyDiagnosticsAsync(
           input: """
                @using System.Text
                @using My.Fun.Namespace

                <div></div>

                <PageTitle></PageTitle>

                <Component />

                @code
                {
                    public void BuildsStrings(StringBuilder b)
                    {
                    }
                }
                """,
           additionalFiles: [
               (FilePath("Component.razor"), """
               @namespace My.Fun.Namespace

               <div></div>
               """)]);

    [Fact]
    public Task RazorUnusedUsings()
        => VerifyDiagnosticsAsync(
            input: """
                @using System.Text
                {|RZ0005:@using My.Fun.Namespace|}
                     
                <div></div>

                <PageTitle></PageTitle>

                @code
                {
                    public void BuildsStrings(StringBuilder b)
                    {
                    }
                }
                """,
            additionalFiles: [
                 (FilePath("Component.razor"), """
                     @namespace My.Fun.Namespace

                     <div></div>
                     """)]);

    [Fact]
    public Task LegacyUnusedAddTagHelperDirective()
        => VerifyDiagnosticsAsync(
            input: """
                {|RZ0005:@addTagHelper *, SomeProject|}
                {|RZ0005:@using System.Text|}
                {|RZ0005:@using System.Text.RegularExpressions|}

                <div></div>
                """,
            additionalFiles:
            [
                (FilePath("AboutBoxTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("dw:about-box")]
                    public class AboutBoxTagHelper : TagHelper
                    {
                    }
                    """)
            ],
            fileKind: RazorFileKind.Legacy);

    [Fact]
    public Task LegacyUsedAddTagHelperDirective_Control()
        => VerifyDiagnosticsAsync(
            input: """
                @addTagHelper *, SomeProject

                <dw:about-box />

                @functions
                {
                    public void M()
                    {
                    }
                }
                """,
            additionalFiles:
            [
                (FilePath("AboutBoxTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("dw:about-box")]
                    public class AboutBoxTagHelper : TagHelper
                    {
                    }
                    """)
            ],
            fileKind: RazorFileKind.Legacy);

    [Fact]
    public Task LegacySpecificAddTagHelperDirectives_MixedUsage()
        => VerifyDiagnosticsAsync(
            input: """
                @addTagHelper AboutBoxTagHelper, SomeProject
                {|RZ0005:@addTagHelper FancyBoxTagHelper, SomeProject|}

                <dw:about-box />
                """,
            additionalFiles:
            [
                (FilePath("AboutBoxTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("dw:about-box")]
                    public class AboutBoxTagHelper : TagHelper
                    {
                    }
                    """),
                (FilePath("FancyBoxTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("dw:fancy-box")]
                    public class FancyBoxTagHelper : TagHelper
                    {
                    }
                    """)
            ],
            fileKind: RazorFileKind.Legacy);
}
