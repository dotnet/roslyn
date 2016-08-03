// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DocumentationComments.CodeFixes
{
    public class RemoveDocCommentNodeCodeFixProviderTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(null, new CSharpRemoveDocCommentNodeCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDuplicateParamTag)]
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
            var parseOptions = Options.Regular.WithDocumentationMode(DocumentationMode.Diagnose);
            await TestAsync(initial, expected, parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDuplicateParamTag)]
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
            var parseOptions = Options.Regular.WithDocumentationMode(DocumentationMode.Diagnose);
            await TestAsync(initial, expected, parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDuplicateParamTag)]
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
            var parseOptions = Options.Regular.WithDocumentationMode(DocumentationMode.Diagnose);
            await TestAsync(initial, expected, parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDuplicateParamTag)]
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
            var parseOptions = Options.Regular.WithDocumentationMode(DocumentationMode.Diagnose);
            await TestAsync(initial, expected, parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDuplicateParamTag)]
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
            var parseOptions = Options.Regular.WithDocumentationMode(DocumentationMode.Diagnose);
            await TestAsync(initial, expected, parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDuplicateParamTag)]
        public async Task RemovesDuplicateParamTag_BothParamTagsOnSameLine_NothingBetweenThem()
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
            var parseOptions = Options.Regular.WithDocumentationMode(DocumentationMode.Diagnose);
            await TestAsync(initial, expected, parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDuplicateParamTag)]
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
            var parseOptions = Options.Regular.WithDocumentationMode(DocumentationMode.Diagnose);
            await TestAsync(initial, expected, parseOptions);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDuplicateParamTag)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllTypeparamInDocument_DoesNotFixDuplicateParamTags()
        {
            var initial = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
class Program1
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;typeparam name=""T""&gt;&lt;/typeparam&gt;
    /// &lt;typeparam {|FixAllInDocument:name=""T""|}>&lt;/typeparam&gt;
    /// &lt;typeparam name=""U""&gt;&lt;/typeparam&gt;
    public void Fizz&lt;T, U&gt;(int value) {}

    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;typeparam name=""T""&gt;&lt;/typeparam&gt;
    /// &lt;typeparam name=""T""&gt;&lt;/typeparam&gt;
    /// &lt;typeparam name=""U""&gt;&lt;/typeparam&gt;
    /// &lt;returns&gt;&lt;/returns&gt;
    public int Buzz&lt;T, U&gt;(int value) { returns 0; }
}
        </Document>
        <Document>
class Program2
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
class Program3
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
class Program1
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;typeparam name=""T""&gt;&lt;/typeparam&gt;
    /// &lt;typeparam name=""U""&gt;&lt;/typeparam&gt;
    public void Fizz&lt;T, U&gt;(int value) {}

    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;typeparam name=""T""&gt;&lt;/typeparam&gt;
    /// &lt;typeparam name=""U""&gt;&lt;/typeparam&gt;
    /// &lt;returns&gt;&lt;/returns&gt;
    public int Buzz&lt;T, U&gt;(int value) { returns 0; }
}
        </Document>
        <Document>
class Program2
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
class Program3
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
    </Project>
</Workspace>";
            
            await TestAsync(initial, expected, compareTokens: false);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDuplicateParamTag)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInDocument()
        {
            var initial = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
class Program1
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param {|FixAllInDocument:name=""value""|}&gt;&lt;/param&gt;
    public void Fizz(int value) {}

    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;returns&gt;&lt;/returns&gt;
    public int Buzz(int value) { returns 0; }
}
        </Document>
        <Document>
class Program2
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
class Program3
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
class Program1
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}

    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;returns&gt;&lt;/returns&gt;
    public int Buzz(int value) { returns 0; }
}
        </Document>
        <Document>
class Program2
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
class Program3
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
    </Project>
</Workspace>";
            
            await TestAsync(initial, expected, compareTokens: false);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDuplicateParamTag)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInProject()
        {
            var initial = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
class Program1
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param {|FixAllInProject:name=""value""|}&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
        <Document>
class Program2
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
class Program3
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class Program1
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
        <Document>
class Program2
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
    </Project>
</Workspace>";
            
            await TestAsync(initial, expected, compareTokens: false);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDuplicateParamTag)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInSolution()
        {
            var initial = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
class Program1
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param {|FixAllInSolution:name=""value""|}&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
        <Document>
class Program2
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
class Program3
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
class Program1
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
        <Document>
class Program2
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
class Program3
{
    /// &lt;summary&gt;
    /// 
    /// &lt;/summary&gt;
    /// &lt;param name=""value""&gt;&lt;/param&gt;
    public void Fizz(int value) {}
}
        </Document>
    </Project>
</Workspace>";
            
            await TestAsync(initial, expected, compareTokens: false);
        }
    }
}
