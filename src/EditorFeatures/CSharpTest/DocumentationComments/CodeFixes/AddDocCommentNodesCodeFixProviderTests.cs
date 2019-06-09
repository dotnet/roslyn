// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DocumentationComments.CodeFixes
{
    public class AddDocCommentNodesCodesFixProviderTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpAddDocCommentNodesCodeFixProvider());

        private async Task TestAsync(string initial, string expected)
        {
            var parseOptions = Options.Regular.WithDocumentationMode(DocumentationMode.Diagnose);
            await TestAsync(initial, expected, parseOptions: parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
        public async Task AddsParamTag_NoNodesBefore()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""j""></param>
    public void Fizz(int [|i|], int j, int k) {}
}
";

            var expected =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    public void Fizz(int i, int j, int k) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
        public async Task AddsParamTag_NoNodesAfter()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""j""></param>
    public void Fizz(int i, int j, int [|k|]) {}
}
";

            var expected =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    public void Fizz(int i, int j, int k) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
        public async Task AddsParamTag_NodesBeforeAndAfter()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""i""></param>
    /// <param name=""k""></param>
    public void Fizz(int i, int [|j|], int k) {}
}
";

            var expected =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    public void Fizz(int i, int j, int k) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
        public async Task AddsParamTag_NodesBeforeAndAfter_RawTextInComment()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""i""></param>
    /// text
    /// <param name=""k""></param>
    public void Fizz(int i, int [|j|], int k) {}
}
";

            var expected =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    /// text
    /// <param name=""k""></param>
    public void Fizz(int i, int j, int k) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
        public async Task AddsParamTag_NodesBeforeAndAfter_WithContent()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""i"">Parameter <paramref name=""i""/> does something</param>
    /// <param name=""k"">Parameter <paramref name=""k""/> does something else</param>
    public void Fizz(int i, int [|j|], int k) {}
}
";

            var expected =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""i"">Parameter <paramref name=""i""/> does something</param>
    /// <param name=""j""></param>
    /// <param name=""k"">Parameter <paramref name=""k""/> does something else</param>
    public void Fizz(int i, int j, int k) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
        public async Task AddsParamTag_NestedInSummaryTag()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// <param name=""j""></param>
    /// </summary>
    public void Fizz(int i, int j, int [|k|]) {}
}
";

            var expected =
@"class Program
{
    /// <summary>
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    /// </summary>
    public void Fizz(int i, int j, int k) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
        public async Task AddsParamTag_BeforeNode_EverythingOnOneLine()
        {
            var initial =
@"class Program
{
    /// <summary></summary> <param name=""j""></param>
    public void Fizz(int [|i|], int j, int k) {}
}
";

            var expected =
@"class Program
{
    /// <summary></summary>
    /// <param name=""i""></param> <param name=""j""></param>
    /// <param name=""k""></param>
    public void Fizz(int i, int j, int k) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
        public async Task AddsParamTag_AfterNode_EverythingOnOneLine()
        {
            var initial =
@"class Program
{
    /// <summary></summary> <param name=""j""></param>
    public void Fizz(int i, int j, int [|k|]) {}
}
";

            var expected =
@"class Program
{
    /// <summary></summary>
    /// <param name=""i""></param> <param name=""j""></param>
    /// <param name=""k""></param>
    public void Fizz(int i, int j, int k) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
        public async Task AddsParamTag_BeforeNode_JustParamNode()
        {
            var initial =
@"class Program
{
    /// <param name=""j""></param>
    public void Fizz(int [|i|], int j, int k) {}
}
";

            var expected =
@"class Program
{
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    public void Fizz(int i, int j, int k) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
        public async Task AddsParamTag_AfterNode_JustParamNode()
        {
            var initial =
@"class Program
{
    /// <param name=""j""></param>
    public void Fizz(int i, int j, int [|k|]) {}
}
";

            var expected =
@"class Program
{
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    public void Fizz(int i, int j, int k) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
        public async Task AddsParamTag_MultipleDocComments()
        {
            var initial =
@"class Program
{
    /// <summary></summary>
    // ...
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""j""></param>
    public void Fizz(int [|i|], int j, int k) {}
}
";

            var expected =
@"class Program
{
    /// <summary></summary>
    // ...
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    public void Fizz(int i, int j, int k) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
        public async Task AddsParamTag_Ctor()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""j""></param>
    public Program(int [|i|], int j, int k) {}
}
";

            var expected =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    public Program(int i, int j, int k) {}
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
        public async Task AddsParamTag_Delegate()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""j""></param>
    public delegate int Goo(int [|i|], int j, int k);
}
";

            var expected =
@"class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    public delegate int Goo(int [|i|], int j, int k);
}
";
            await TestAsync(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
        public async Task AddsParamTag_Operator()
        {
            var initial =
@"public struct MyStruct
{
    public int Val { get; }

    public MyStruct(int val)
    {
        Val = val;
    }
        
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""s1""></param>
    /// <returns></returns>
    public static MyStruct operator +(MyStruct s1, MyStruct [|s2|])
    {
        return new MyStruct(s1.Val + s2.Val);
    }
}
";

            var expected =
@"public struct MyStruct
{
    public int Val { get; }

    public MyStruct(int val)
    {
        Val = val;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name=""s1""></param>
    /// <param name=""s2""></param>
    /// <returns></returns>
    public static MyStruct operator +(MyStruct s1, MyStruct s2)
    {
        return new MyStruct(s1.Val + s2.Val);
    }
}
";
            await TestAsync(initial, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInDocument_MultipleParamNodesInVariousPlaces()
        {
            var initial =
@"class Program
{
    /// <summary>
    /// <param name=""i""></param>
    /// </summary>
    /// <param name=""k""></param>
    public void Fizz(int i, int {|FixAllInDocument:j|}, int k, int l) {}
}";

            var expected =
@"class Program
{
    /// <summary>
    /// <param name=""i""></param>
    /// </summary>
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    /// <param name=""l""></param>
    public void Fizz(int i, int j, int k, int l) {}
}";

            await TestAsync(initial, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
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
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    public void Fizz(int i, int j, {|FixAllInDocument:int k|}) {}

    /// <summary>
    /// 
    /// </summary>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    /// <returns></returns>
    public int Buzz(int i, int j, int k) { returns 0; }
}]]>
        </Document>
        <Document>
<![CDATA[
class Program2
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    public void Fizz(int i, int j, int k) {}
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
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    public void Fizz(int i, int j, int k) {}
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
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    public void Fizz(int i, int j, int k) {}

    /// <summary>
    /// 
    /// </summary>
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    /// <returns></returns>
    public int Buzz(int i, int j, int k) { returns 0; }
}]]>
        </Document>
        <Document>
<![CDATA[
class Program2
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    public void Fizz(int i, int j, int k) {}
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
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    public void Fizz(int i, int j, int k) {}
}]]>
        </Document>
    </Project>
</Workspace>";

            await TestAsync(initial, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
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
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    public void Fizz(int i, int j, {|FixAllInProject:int k|}) {}
}]]>
        </Document>
        <Document>
<![CDATA[
class Program2
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    /// <returns></returns>
    public int Buzz(int i, int j, int k) { returns 0; }
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
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    /// <returns></returns>
    public int Buzz(int i, int j, int k) { returns 0; }
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
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    public void Fizz(int i, int j, int k) {}
}]]>
        </Document>
        <Document>
<![CDATA[
class Program2
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    /// <returns></returns>
    public int Buzz(int i, int j, int k) { returns 0; }
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
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    /// <returns></returns>
    public int Buzz(int i, int j, int k) { returns 0; }
}]]>
        </Document>
    </Project>
</Workspace>";

            await TestAsync(initial, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
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
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    public void Fizz(int i, int j, {|FixAllInSolution:int k|}) {}
}]]>
        </Document>
        <Document>
<![CDATA[
class Program2
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""j""></param>
    public void Fizz(int i, int j, int k) {}
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
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    public void Fizz(int i, int j, int k) {}
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
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    public void Fizz(int i, int j, int k) {}
}]]>
        </Document>
        <Document>
<![CDATA[
class Program2
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    public void Fizz(int i, int j, int k) {}
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
    /// <param name=""i""></param>
    /// <param name=""j""></param>
    /// <param name=""k""></param>
    public void Fizz(int i, int j, int k) {}
}]]>
        </Document>
    </Project>
</Workspace>";

            await TestAsync(initial, expected);
        }
    }
}
