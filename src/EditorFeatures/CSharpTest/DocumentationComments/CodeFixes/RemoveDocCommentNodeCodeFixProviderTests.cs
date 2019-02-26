// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DocumentationComments.CodeFixes
{
    public class RemoveDocCommentNodeCodeFixProviderTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpRemoveDocCommentNodeCodeFixProvider());

        private async Task TestAsync(string initial, string expected)
        {
            var parseOptions = Options.Regular.WithDocumentationMode(DocumentationMode.Diagnose);
            await TestAsync(initial, expected, parseOptions: parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
        public async Task RemovesDuplicateParamTag()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param [|name=""value""|]></param>
    public void Fizz(int value) {}
}
";

            var expected =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
        public async Task RemovesDuplicateParamTag_OnlyParamTags()
        {
            var initial =
@"class Program
{
    /// <param name=""value""></param>
    /// <param [|name=""value""|]></param>
    public void Fizz(int value) {}
}
";

            var expected =
@"class Program
{
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
        public async Task RemovesDuplicateParamTag_TagBelowOffendingParamTag()
        {
            var initial =
@"class Program
{
    /// <param name=""value""></param>
    /// <param [|name=""value""|]></param>
    /// <returns></returns>
    public int Fizz(int value) { return 0; }
}
";

            var expected =
@"class Program
{
    /// <param name=""value""></param>
    /// <returns></returns>
    public int Fizz(int value) { return 0; }
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
        public async Task RemovesDuplicateParamTag_BothParamTagsOnSameLine_DocCommentTagBetweenThem()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>    /// <param [|name=""value""|]></param>
    public void Fizz(int value) {}
}
";

            var expected =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
        public async Task RemovesDuplicateParamTag_BothParamTagsOnSameLine_WhitespaceBetweenThem()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>     <param [|name=""value""|]></param>
    public void Fizz(int value) {}
}
";

            var expected =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
        public async Task RemovesDuplicateParamTag_BothParamTagsOnSameLine_NothingBetweenThem1()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param><param [|name=""value""|]></param>
    public void Fizz(int value) {}
}
";

            var expected =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}
";
            await TestAsync(initial, expected);
        }

        [WorkItem(13436, "https://github.com/dotnet/roslyn/issues/13436")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
        public async Task RemovesTag_BothParamTagsOnSameLine_NothingBetweenThem2()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param [|name=""a""|]></param><param name=""value""></param>
    public void Fizz(int value) {}
}
";

            var expected =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}
";
            await TestAsync(initial, expected);
        }

        [WorkItem(13436, "https://github.com/dotnet/roslyn/issues/13436")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
        public async Task RemovesTag_TrailingTextAfterTag()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param [|name=""a""|]></param> a
    public void Fizz(int value) {}
}
";

            var expected =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    ///  a
    public void Fizz(int value) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
        public async Task RemovesDuplicateParamTag_RawTextBeforeAndAfterNode()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// some comment<param [|name=""value""|]></param>out of the XML nodes
    public void Fizz(int value) {}
}
";

            var expected =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// some commentout of the XML nodes
    public void Fizz(int value) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
        public async Task RemovesDuplicateTypeparamTag()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name=""T""></typeparam>
    /// <typeparam [|name=""T""|]></typeparam>
    public void Fizz<T>() { }
}
";

            var expected =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name=""T""></typeparam>
    public void Fizz<T>() { }
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
        public async Task RemovesParamTagWithNoMatchingParameter()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""[|val|]""></param>
    public void Fizz(int value) {}
}
";

            var expected =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    public void Fizz(int value) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
        public async Task RemovesParamTag_NestedInSummaryTag()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// <param name=""value""></param>
    /// <param [|name=""value""|]></param>
    /// </summary>
    public void Fizz(int value) {}
}
";

            var expected =
@"class Program
{
    /// <summary>
    /// <param name=""value""></param>
    /// </summary>
    public void Fizz(int value) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
        public async Task RemovesParamTag_NestedInSummaryTag_WithChildren()
        {
            var initial =
@"class Program
{
    /// <summary>
    ///   <param name=""value""></param>
    ///   <param [|name=""value""|]>
    ///     <xmlnode></xmlnode>
    ///   </param>
    /// </summary>
    public void Fizz(int value) {}
}
";

            var expected =
@"class Program
{
    /// <summary>
    ///   <param name=""value""></param>
    /// </summary>
    public void Fizz(int value) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllTypeparamInDocument_DoesNotFixDuplicateParamTags()
        {
            var initial = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
<![CDATA[
class Program1
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param name=""value""></param>
    /// <typeparam name=""T""></typeparam>
    /// <typeparam {|FixAllInDocument:name=""T""|}></typeparam>
    /// <typeparam name=""U""></typeparam>
    public void Fizz<T, U>(int value) {}

    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param name=""value""></param>
    /// <typeparam name=""T""></typeparam>
    /// <typeparam name=""T""></typeparam>
    /// <typeparam name=""U""></typeparam>
    /// <returns></returns>
    public int Buzz<T, U>(int value) { returns 0; }
}]]>
        </Document>
        <Document>
<![CDATA[
class Program2
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}]]>
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
<![CDATA[
class Program3
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}]]>
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
<![CDATA[
class Program1
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param name=""value""></param>
    /// <typeparam name=""T""></typeparam>
    /// <typeparam name=""U""></typeparam>
    public void Fizz<T, U>(int value) {}

    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param name=""value""></param>
    /// <typeparam name=""T""></typeparam>
    /// <typeparam name=""U""></typeparam>
    /// <returns></returns>
    public int Buzz<T, U>(int value) { returns 0; }
}]]>
        </Document>
        <Document>
<![CDATA[
class Program2
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}]]>
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
<![CDATA[
class Program3
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}]]>
        </Document>
    </Project>
</Workspace>";

            await TestAsync(initial, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInDocument()
        {
            var initial = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
<![CDATA[
class Program1
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param {|FixAllInDocument:name=""value""|}></param>
    public void Fizz(int value) {}

    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param name=""value""></param>
    /// <returns></returns>
    public int Buzz(int value) { returns 0; }
}]]>
        </Document>
        <Document>
<![CDATA[
class Program2
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}]]>
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
<![CDATA[
class Program3
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}]]>
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
<![CDATA[
class Program1
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    public void Fizz(int value) {}

    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <returns></returns>
    public int Buzz(int value) { returns 0; }
}]]>
        </Document>
        <Document>
<![CDATA[
class Program2
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}]]>
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
<![CDATA[
class Program3
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}]]>
        </Document>
    </Project>
</Workspace>";

            await TestAsync(initial, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInProject()
        {
            var initial = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
<![CDATA[
class Program1
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param {|FixAllInProject:name=""value""|}></param>
    public void Fizz(int value) {}
}]]>
        </Document>
        <Document>
<![CDATA[
class Program2
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}]]>
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
<![CDATA[
class Program3
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}]]>
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
<![CDATA[
class Program1
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}]]>
        </Document>
        <Document>
<![CDATA[
class Program2
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}]]>
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
<![CDATA[
class Program3
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}]]>
        </Document>
    </Project>
</Workspace>";

            await TestAsync(initial, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInSolution()
        {
            var initial = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
<![CDATA[
class Program1
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param {|FixAllInSolution:name=""value""|}></param>
    public void Fizz(int value) {}
}]]>
        </Document>
        <Document>
<![CDATA[
class Program2
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}]]>
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
<![CDATA[
class Program3
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}]]>
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
<![CDATA[
class Program1
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}]]>
        </Document>
        <Document>
<![CDATA[
class Program2
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}]]>
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
<![CDATA[
class Program3
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""value""></param>
    public void Fizz(int value) {}
}]]>
        </Document>
    </Project>
</Workspace>";

            await TestAsync(initial, expected);
        }
    }
}
