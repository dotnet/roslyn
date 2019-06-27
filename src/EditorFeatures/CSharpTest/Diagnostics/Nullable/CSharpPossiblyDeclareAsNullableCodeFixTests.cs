// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.PossiblyDeclareAsNullable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.PossiblyDeclareAsNullable
{
    // Consider adding support for:
    // symbol in another document
    // symbol in another project
    // a.b.c.s$$ == null
    // somet$$hing?.x
    // (object)x == null
    // switch(...) { case null: ...
    // e switch { null => ...
    // ReferenceEquals(x, null) and other equality methods

    [Trait(Traits.Feature, Traits.Features.CodeActionsDeclareAsNullable)]
    public class CSharpPossiblyDeclareAsNullableCodeFixTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpPossiblyDeclareAsNullableDiagnosticAnalyzer(), new CSharpPossiblyDeclareAsNullableCodeFixProvider());

        private static readonly TestParameters s_nullableFeature =
            new TestParameters(parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));

        private readonly string NullableEnable = @"
#nullable enable
";

        [Fact]
        public async Task ParameterEqualsNull()
        {
            var code = NullableEnable + @"
class C
{
    static void M(string s)
    {
        if (s [|==|] null)
        {
            return;
        }
    }
}";

            var expected = NullableEnable + @"
class C
{
    static void M(string? s)
    {
        if (s == null)
        {
            return;
        }
    }
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FieldFromAnotherDocument()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
#nullable enable
partial class C
{
    void M()
    {
        if (s [|==|] null)
        {
            return;
        }
    }
}
        </Document>
        <Document>
#nullable enable
partial class C
{
    string s;
}
        </Document>
    </Project>
</Workspace>";

            // Fix not offered when in another document, at the moment
            await TestMissingInRegularAndScriptAsync(input, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FieldFromAnotherProject()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
#nullable enable
class C
{
    void M(D d)
    {
        if (d.s [|==|] null)
        {
            return;
        }
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
#nullable enable
public class D
{
    public string s;
}
        </Document>
    </Project>
</Workspace>";

            // Fix not offered when in another project, at the moment
            await TestMissingInRegularAndScriptAsync(input, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task ConditionalAccess()
        {
            var code = NullableEnable + @"
class C
{
    static void M(string s)
    {
        if ([|s|]?.Length == 0)
        {
            return;
        }
    }
}";

            var expected = NullableEnable + @"
class C
{
    static void M(string? s)
    {
        if (s?.Length == 0)
        {
            return;
        }
    }
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task LocalIsNull()
        {
            var code = NullableEnable + @"
class C
{
    static void M()
    {
        string s = """";
        if (s [|is|] null)
        {
            return;
        }
    }
}";

            var expected = NullableEnable + @"
class C
{
    static void M()
    {
        string? s = """";
        if (s is null)
        {
            return;
        }
    }
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task AlreadyNullableStringType()
        {
            var code = @"
class C
{
    static void M(string? s)
    {
        if (s [|==|] null)
        {
            return;
        }
    }
}";

            await TestMissingInRegularAndScriptAsync(code, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task NullableValueType()
        {
            var code = @"
class C
{
    static void M(int? s)
    {
        if (s [|==|] null)
        {
            return;
        }
    }
}";

            await TestMissingInRegularAndScriptAsync(code, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task CursorOnEquals()
        {
            var code = @"
class C
{
    static void M(string s)
    {
        if (s ==[||] null)
        {
            return;
        }
    }
}";

            var expected = @"
class C
{
    static void M(string? s)
    {
        if (s == null)
        {
            return;
        }
    }
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task CursorOnNull()
        {
            var code = @"
class C
{
    static void M(string s)
    {
        if (s == [||]null)
        {
            return;
        }
    }
}";

            var expected = @"
class C
{
    static void M(string? s)
    {
        if (s == null)
        {
            return;
        }
    }
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task ParameterNotEqualsNull()
        {
            var code = @"
class C
{
    static void M(string s)
    {
        if (s [|!=|] null)
        {
            return;
        }
    }
}";

            var expected = @"
class C
{
    static void M(string? s)
    {
        if (s != null)
        {
            return;
        }
    }
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task NullEqualsParameter()
        {
            var code = @"
class C
{
    static void M(string s)
    {
        if (null [|==|] s)
        {
            return;
        }
    }
}";

            var expected = @"
class C
{
    static void M(string? s)
    {
        if (null == s)
        {
            return;
        }
    }
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task LocalEqualsNull()
        {
            var code = @"
class C
{
    static void M()
    {
        string s = M2();
        if (s [|==|] null)
        {
            return;
        }
    }
    static string M2() => throw null;
}";

            var expected = @"
class C
{
    static void M()
    {
        string? s = M2();
        if (s == null)
        {
            return;
        }
    }
    static string M2() => throw null;
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FieldEqualsNull()
        {
            var code = @"
class C
{
    string field;
    static void M(C c)
    {
        if (c.field [|==|] null)
        {
            return;
        }
    }
}";

            var expected = @"
class C
{
    string? field;
    static void M(C c)
    {
        if (c.field == null)
        {
            return;
        }
    }
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task MultiLocalEqualsNull()
        {
            var code = @"
class C
{
    static void M()
    {
        string s = M2(), y = M2();
        if (s [|==|] null)
        {
            return;
        }
    }
    static string M2() => throw null;
}";

            await TestMissingInRegularAndScriptAsync(code, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task PropertyEqualsNull()
        {
            var code = @"
class C
{
    string s { get; set; }
    static void M()
    {
        if (s [|==|] null)
        {
            return;
        }
    }
}";

            var expected = @"
class C
{
    string? s { get; set; }
    static void M()
    {
        if (s == null)
        {
            return;
        }
    }
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task MethodEqualsNull()
        {
            var code = @"
class C
{
    static void M()
    {
        if (M2() [|==|] null)
        {
            return;
        }
    }
    static string M2() => throw null;
}";

            var expected = @"
class C
{
    static void M()
    {
        if (M2() == null)
        {
            return;
        }
    }
    static string? M2() => throw null;
}";

            await TestInRegularAndScript1Async(code, expected, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task GenericMethodEqualsNull()
        {
            var code = @"
class C
{
    static void M()
    {
        if (M2<string>() [|==|] null)
        {
            return;
        }
    }
    static T M2<T>() => throw null;
}";

            await TestMissingInRegularAndScriptAsync(code, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task PartialMethodEqualsNull()
        {
            var code = @"
partial class C
{
    void M()
    {
        if (M2() [|==|] null)
        {
            return;
        }
    }
    partial string M2();
}
partial class C
{
    partial string M2() => throw null;
}";

            await TestMissingInRegularAndScriptAsync(code, parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixAll()
        {
            await TestMissingInRegularAndScriptAsync(
NullableEnable + @"
class Program
{
class C
{
    static void M(string s)
    {
        return ;
        if (s {|FixAllInDocument:==|}] null)
        {
            return;
        }
    }
}");
        }
    }
}
