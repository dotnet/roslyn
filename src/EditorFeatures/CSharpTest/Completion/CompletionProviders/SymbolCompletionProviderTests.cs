// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionSetSources;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed partial class SymbolCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(SymbolCompletionProvider);

    [Theory]
    [InlineData(SourceCodeKind.Regular)]
    [InlineData(SourceCodeKind.Script)]
    public async Task EmptyFile(SourceCodeKind sourceCodeKind)
    {
        await VerifyItemIsAbsentAsync(@"$$", @"String", expectedDescriptionOrNull: null, sourceCodeKind: sourceCodeKind);
        await VerifyItemExistsAsync(@"$$", @"System", expectedDescriptionOrNull: null, sourceCodeKind: sourceCodeKind);
    }

    [Theory]
    [InlineData(SourceCodeKind.Regular)]
    [InlineData(SourceCodeKind.Script)]
    public async Task EmptyFileWithUsing(SourceCodeKind sourceCodeKind)
    {
        await VerifyItemExistsAsync("""
            using System;
            $$
            """, @"String", expectedDescriptionOrNull: null, sourceCodeKind: sourceCodeKind);
        await VerifyItemExistsAsync("""
            using System;
            $$
            """, @"System", expectedDescriptionOrNull: null, sourceCodeKind: sourceCodeKind);
    }

    [Fact]
    public async Task NotAfterHashR()
        => await VerifyItemIsAbsentAsync(@"#r $$", "@System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);

    [Fact]
    public async Task NotAfterHashLoad()
        => await VerifyItemIsAbsentAsync(@"#load $$", "@System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);

    [Fact]
    public async Task UsingDirective()
    {
        await VerifyItemIsAbsentAsync(@"using $$", @"String");
        await VerifyItemIsAbsentAsync(@"using $$ = System", @"System");
        await VerifyItemExistsAsync(@"using $$", @"System");
        await VerifyItemExistsAsync(@"using T = $$", @"System");
    }

    [Fact]
    public async Task InactiveRegion()
    {
        await VerifyItemIsAbsentAsync("""
            class C {
            #if false 
            $$
            #endif
            """, @"String");
        await VerifyItemIsAbsentAsync("""
            class C {
            #if false 
            $$
            #endif
            """, @"System");
    }

    [Fact]
    public async Task ActiveRegion()
    {
        await VerifyItemIsAbsentAsync("""
            class C {
            #if true 
            $$
            #endif
            """, @"String");
        await VerifyItemExistsAsync("""
            class C {
            #if true 
            $$
            #endif
            """, @"System");
    }

    [Fact]
    public async Task InactiveRegionWithUsing()
    {
        await VerifyItemIsAbsentAsync("""
            using System;

            class C {
            #if false 
            $$
            #endif
            """, @"String");
        await VerifyItemIsAbsentAsync("""
            using System;

            class C {
            #if false 
            $$
            #endif
            """, @"System");
    }

    [Fact]
    public async Task ActiveRegionWithUsing()
    {
        await VerifyItemExistsAsync("""
            using System;

            class C {
            #if true 
            $$
            #endif
            """, @"String");
        await VerifyItemExistsAsync("""
            using System;

            class C {
            #if true 
            $$
            #endif
            """, @"System");
    }

    [Fact]
    public async Task SingleLineComment1()
    {
        await VerifyItemIsAbsentAsync("""
            using System;

            class C {
            // $$
            """, @"String");
        await VerifyItemIsAbsentAsync("""
            using System;

            class C {
            // $$
            """, @"System");
    }

    [Fact]
    public async Task SingleLineComment2()
    {
        await VerifyItemIsAbsentAsync("""
            using System;

            class C {
            // $$
            """, @"String");
        await VerifyItemIsAbsentAsync("""
            using System;

            class C {
            // $$
            """, @"System");
        await VerifyItemIsAbsentAsync("""
            using System;

            class C {
              // $$
            """, @"System");
    }

    [Fact]
    public async Task MultiLineComment()
    {
        await VerifyItemIsAbsentAsync("""
            using System;

            class C {
            /*  $$
            """, @"String");
        await VerifyItemIsAbsentAsync("""
            using System;

            class C {
            /*  $$
            """, @"System");
        await VerifyItemIsAbsentAsync("""
            using System;

            class C {
            /*  $$   */
            """, @"String");
        await VerifyItemIsAbsentAsync("""
            using System;

            class C {
            /*  $$   */
            """, @"System");
        await VerifyItemExistsAsync("""
            using System;

            class C {
            /*    */$$
            """, @"System");
        await VerifyItemExistsAsync("""
            using System;

            class C {
            /*    */$$
            """, @"System");
        await VerifyItemExistsAsync("""
            using System;

            class C {
              /*    */$$
            """, @"System");
    }

    [Fact]
    public async Task SingleLineXmlComment1()
    {
        await VerifyItemIsAbsentAsync("""
            using System;

            class C {
            /// $$
            """, @"String");
        await VerifyItemIsAbsentAsync("""
            using System;

            class C {
            /// $$
            """, @"System");
    }

    [Fact]
    public async Task SingleLineXmlComment2()
    {
        await VerifyItemIsAbsentAsync("""
            using System;

            class C {
            /// $$
            """, @"String");
        await VerifyItemIsAbsentAsync("""
            using System;

            class C {
            /// $$
            """, @"System");
        await VerifyItemIsAbsentAsync("""
            using System;

            class C {
              /// $$
            """, @"System");
    }

    [Fact]
    public async Task MultiLineXmlComment()
    {
        await VerifyItemIsAbsentAsync("""
            using System;

            class C {
            /**  $$   */
            """, @"String");
        await VerifyItemIsAbsentAsync("""
            using System;

            class C {
            /**  $$   */
            """, @"System");
        await VerifyItemExistsAsync("""
            using System;

            class C {
            /**     */$$
            """, @"System");
        await VerifyItemExistsAsync("""
            using System;

            class C {
            /**     */$$
            """, @"System");
        await VerifyItemExistsAsync("""
            using System;

            class C {
              /**     */$$
            """, @"System");
    }

    [Fact]
    public async Task OpenStringLiteral()
    {
        var code = AddUsingDirectives("using System;", AddInsideMethod("string s = \"$$"));
        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Absent("String"),
            ItemExpectation.Absent("System")
        ]);
    }

    [Fact]
    public Task OpenStringLiteralInDirective()
        => VerifyExpectedItemsAsync(
            "#r \"$$", [
                ItemExpectation.Absent("String"),
                ItemExpectation.Absent("System")
            ],
            sourceCodeKind: SourceCodeKind.Script);

    [Fact]
    public async Task StringLiteral()
    {
        var code = AddUsingDirectives("using System;", AddInsideMethod("string s = \"$$\";"));
        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Absent("String"),
            ItemExpectation.Absent("System")
        ]);
    }

    [Fact]
    public Task StringLiteralInDirective()
        => VerifyExpectedItemsAsync(
            """
            #r "$$"
            """, [
                ItemExpectation.Absent("String"),
                ItemExpectation.Absent("System")
            ],
            sourceCodeKind: SourceCodeKind.Script);

    [Fact]
    public async Task OpenCharLiteral()
    {
        var code = AddUsingDirectives("using System;", AddInsideMethod("char c = '$$"));
        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Absent("String"),
            ItemExpectation.Absent("System")
        ]);
    }

    [Fact]
    public Task AssemblyAttribute1()
        => VerifyExpectedItemsAsync(@"[assembly: $$]", [
            ItemExpectation.Absent("String"),
            ItemExpectation.Exists("System")
        ]);

    [Fact]
    public Task AssemblyAttribute2()
        => VerifyExpectedItemsAsync(
            AddUsingDirectives("using System;", @"[assembly: $$]"), [
            ItemExpectation.Exists("CLSCompliant"),
            ItemExpectation.Exists("System"),
            ItemExpectation.Absent("AttributeUsage")
        ]);

    [Fact]
    public Task SystemAttributeIsNotAnAttribute()
        => VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", """
            [$$]
            class CL {}
            """), @"Attribute");

    [Fact]
    public async Task TypeAttribute()
    {
        var content = """
            [$$]
            class CL {}
            """;

        await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"AttributeUsage");
        await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
    }

    [Fact]
    public Task TypeParamAttribute()
        => VerifyExpectedItemsAsync(
            AddUsingDirectives("using System;", @"class CL<[A$$]T> {}"), [
            ItemExpectation.Exists("CLSCompliant"),
            ItemExpectation.Exists("System"),
            ItemExpectation.Absent("AttributeUsage"),
        ]);

    [Fact]
    public Task MethodAttribute()
        => VerifyExpectedItemsAsync(AddUsingDirectives("using System;", """
            class CL {
                [$$]
                void Method() {}
            }
            """), [
            ItemExpectation.Exists("STAThread"),
            ItemExpectation.Exists("System"),
            ItemExpectation.Absent("AttributeUsage"),
        ]);

    [Fact]
    public Task MethodTypeParamAttribute()
        => VerifyExpectedItemsAsync(AddUsingDirectives("using System;", """
            class CL{
                void Method<[A$$]T> () {}
            }
            """), [
            ItemExpectation.Exists("CLSCompliant"),
            ItemExpectation.Exists("System"),
            ItemExpectation.Absent("AttributeUsage"),
        ]);

    [Fact]
    public Task MethodParamAttribute()
        => VerifyExpectedItemsAsync(AddUsingDirectives("using System;", """
            class CL{
                void Method ([$$]int i) {}
            }
            """), [
            ItemExpectation.Exists("ParamArray"),
            ItemExpectation.Exists("System"),
            ItemExpectation.Absent("AttributeUsage"),
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_AssemblyAttribute()
    {
        var code = """
            [assembly: $$]

            namespace TestNamespace
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                    }
                }
            }

            [System.AttributeUsage(System.AttributeTargets.Assembly)]
            public class AssemblyOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class ClassOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("AssemblyOnly"),
            ItemExpectation.Absent("ClassOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_ClassAttribute()
    {
        var code = """
            [$$]
            class TestClass
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Assembly)]
            public class AssemblyOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class ClassOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Absent("AssemblyOnly"),
            ItemExpectation.Exists("ClassOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_MethodAttribute()
    {
        var code = """
            class TestClass
            {
                [$$]
                void TestMethod()
                {
                }
            }

            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class MethodOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Property)]
            public class PropertyOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("MethodOnly"),
            ItemExpectation.Absent("PropertyOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_ExplicitTargetSpecifier()
    {
        var code = """
            class TestClass
            {
                [return: $$]
                int TestMethod()
                {
                    return 0;
                }
            }

            [System.AttributeUsage(System.AttributeTargets.ReturnValue)]
            public class ReturnValueOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class MethodOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("ReturnValueOnly"),
            ItemExpectation.Absent("MethodOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_StructAttribute()
    {
        var code = """
            [$$]
            struct TestStruct
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Struct)]
            public class StructOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class ClassOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("StructOnly"),
            ItemExpectation.Absent("ClassOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_InterfaceAttribute()
    {
        var code = """
            [$$]
            interface ITestInterface
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Interface)]
            public class InterfaceOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class ClassOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("InterfaceOnly"),
            ItemExpectation.Absent("ClassOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_EnumAttribute()
    {
        var code = """
            [$$]
            enum TestEnum
            {
                Value1
            }

            [System.AttributeUsage(System.AttributeTargets.Enum)]
            public class EnumOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class ClassOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("EnumOnly"),
            ItemExpectation.Absent("ClassOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_DelegateAttribute()
    {
        var code = """
            [$$]
            delegate void TestDelegate();

            [System.AttributeUsage(System.AttributeTargets.Delegate)]
            public class DelegateOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class ClassOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("DelegateOnly"),
            ItemExpectation.Absent("ClassOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_RecordClassAttribute()
    {
        var code = """
            [$$]
            record TestRecord
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class ClassOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Struct)]
            public class StructOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("ClassOnly"),
            ItemExpectation.Absent("StructOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_RecordStructAttribute()
    {
        var code = """
            [$$]
            record struct TestRecordStruct
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Struct)]
            public class StructOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class ClassOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("StructOnly"),
            ItemExpectation.Absent("ClassOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_ConstructorAttribute()
    {
        var code = """
            class TestClass
            {
                [$$]
                public TestClass()
                {
                }
            }

            [System.AttributeUsage(System.AttributeTargets.Constructor)]
            public class ConstructorOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class MethodOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("ConstructorOnly"),
            ItemExpectation.Absent("MethodOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_PropertyAttribute()
    {
        var code = """
            class TestClass
            {
                [$$]
                public int TestProperty { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Property)]
            public class PropertyOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class MethodOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("PropertyOnly"),
            ItemExpectation.Absent("MethodOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_FieldAttribute()
    {
        var code = """
            class TestClass
            {
                [$$]
                private int _testField;
            }

            [System.AttributeUsage(System.AttributeTargets.Field)]
            public class FieldOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Property)]
            public class PropertyOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("FieldOnly"),
            ItemExpectation.Absent("PropertyOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_EventAttribute()
    {
        var code = """
            class TestClass
            {
                [$$]
                public event System.EventHandler TestEvent;
            }

            [System.AttributeUsage(System.AttributeTargets.Event)]
            public class EventOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class MethodOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("EventOnly"),
            ItemExpectation.Absent("MethodOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_ParameterAttribute()
    {
        var code = """
            class TestClass
            {
                void TestMethod([$$] int parameter)
                {
                }
            }

            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public class ParameterOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class MethodOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("ParameterOnly"),
            ItemExpectation.Absent("MethodOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_TypeParameterAttribute()
    {
        var code = """
            class TestClass<[$$]T>
            {
            }

            [System.AttributeUsage(System.AttributeTargets.GenericParameter)]
            public class GenericParameterOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class ClassOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("GenericParameterOnly"),
            ItemExpectation.Absent("ClassOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_IndexerAttribute()
    {
        var code = """
            class TestClass
            {
                [$$]
                public int this[int index] => 0;
            }

            [System.AttributeUsage(System.AttributeTargets.Property)]
            public class PropertyOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class MethodOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("PropertyOnly"),
            ItemExpectation.Absent("MethodOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_ModuleTargetSpecifier()
    {
        var code = """
            [module: $$]

            class TestClass
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Module)]
            public class ModuleOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class ClassOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("ModuleOnly"),
            ItemExpectation.Absent("ClassOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_TypeTargetSpecifier()
    {
        var code = """
            [type: $$]
            class TestClass
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class ClassOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class MethodOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("ClassOnly"),
            ItemExpectation.Absent("MethodOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_MethodTargetSpecifier()
    {
        var code = """
            class TestClass
            {
                [method: $$]
                void TestMethod()
                {
                }
            }

            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class MethodOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Property)]
            public class PropertyOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("MethodOnly"),
            ItemExpectation.Absent("PropertyOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_FieldTargetSpecifier()
    {
        var code = """
            class TestClass
            {
                [field: $$]
                private int _field;
            }

            [System.AttributeUsage(System.AttributeTargets.Field)]
            public class FieldOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Property)]
            public class PropertyOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("FieldOnly"),
            ItemExpectation.Absent("PropertyOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_PropertyTargetSpecifier()
    {
        var code = """
            class TestClass
            {
                [property: $$]
                public int Property { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Property)]
            public class PropertyOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class MethodOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("PropertyOnly"),
            ItemExpectation.Absent("MethodOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_EventTargetSpecifier()
    {
        var code = """
            class TestClass
            {
                [event: $$]
                public event System.EventHandler TestEvent;
            }

            [System.AttributeUsage(System.AttributeTargets.Event)]
            public class EventOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class MethodOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("EventOnly"),
            ItemExpectation.Absent("MethodOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_ParamTargetSpecifier()
    {
        var code = """
            class TestClass
            {
                void TestMethod([param: $$] int parameter)
                {
                }
            }

            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public class ParameterOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class MethodOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("ParameterOnly"),
            ItemExpectation.Absent("MethodOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7640")]
    public async Task AttributeTargetFiltering_TypeVarTargetSpecifier()
    {
        var code = """
            class TestClass<[typevar: $$]T>
            {
            }

            [System.AttributeUsage(System.AttributeTargets.GenericParameter)]
            public class GenericParameterOnlyAttribute : System.Attribute
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class ClassOnlyAttribute : System.Attribute
            {
            }
            """;

        await VerifyExpectedItemsAsync(code, [
            ItemExpectation.Exists("GenericParameterOnly"),
            ItemExpectation.Absent("ClassOnly")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_EmptyNameSpan_TopLevel()
        => VerifyItemExistsAsync(@"namespace $$ { }", "System", sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_EmptyNameSpan_Nested()
        => VerifyItemExistsAsync("""
            ;
            namespace System
            {
                namespace $$ { }
            }
            """, "Runtime", sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_Unqualified_TopLevelNoPeers()
        => VerifyExpectedItemsAsync("""
            using System;

            namespace $$
            """,
            [
                ItemExpectation.Exists("System"),
                ItemExpectation.Absent("String")
            ],
            sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_Unqualified_TopLevelNoPeers_FileScopedNamespace()
        => VerifyExpectedItemsAsync("""
            using System;

            namespace $$;
            """,
            [
                ItemExpectation.Exists("System"),
                ItemExpectation.Absent("String")
            ],
            sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_Unqualified_TopLevelWithPeer()
        => VerifyItemExistsAsync("""
            namespace A { }

            namespace $$
            """, "A", sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_Unqualified_NestedWithNoPeers()
        => VerifyNoItemsExistAsync("""
            namespace A
            {
                namespace $$
            }
            """, sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_Unqualified_NestedWithPeer()
        => VerifyExpectedItemsAsync("""
            namespace A
            {
                namespace B { }

                namespace $$
            }
            """,
            [
                ItemExpectation.Absent("A"),
                ItemExpectation.Exists("B")
            ],
            sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_Unqualified_ExcludesCurrentDeclaration()
        => VerifyItemIsAbsentAsync(@"namespace N$$S", "NS", sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_Unqualified_WithNested()
        => VerifyExpectedItemsAsync("""
            namespace A
            {
                namespace $$
                {
                    namespace B { }
                }
            }
            """,
            [
                ItemExpectation.Absent("A"),
                ItemExpectation.Absent("B")
            ],
            sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_Unqualified_WithNestedAndMatchingPeer()
        => VerifyExpectedItemsAsync("""
            namespace A.B { }

            namespace A
            {
                namespace $$
                {
                    namespace B { }
                }
            }
            """,
            [
                ItemExpectation.Absent("A"),
                ItemExpectation.Exists("B")
            ],
            sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_Unqualified_InnerCompletionPosition()
        => VerifyExpectedItemsAsync(@"namespace Sys$$tem { }",
            [
                ItemExpectation.Exists("System"),
                ItemExpectation.Absent("Runtime")
            ],
            sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_Unqualified_IncompleteDeclaration()
        => VerifyExpectedItemsAsync(
            """
            namespace A
            {
                namespace B
                {
                    namespace $$
                    namespace C1 { }
                }
                namespace B.C2 { }
            }

            namespace A.B.C3 { }
            """, [
                // Ideally, all the C* namespaces would be recommended but, because of how the parser
                // recovers from the missing braces, they end up with the following qualified names...
                //
                //     C1 => A.B.?.C1
                //     C2 => A.B.B.C2
                //     C3 => A.A.B.C3
                //
                // ...none of which are found by the current algorithm.
                ItemExpectation.Absent("C1"),
                ItemExpectation.Absent("C2"),
                ItemExpectation.Absent("C3"),
                ItemExpectation.Absent("A"),

                // Because of the above, B does end up in the completion list
                // since A.B.B appears to be a peer of the new declaration
                ItemExpectation.Exists("B")
            ],
            SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_Qualified_NoPeers()
        => VerifyNoItemsExistAsync(@"namespace A.$$", sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_Qualified_TopLevelWithPeer()
        => VerifyItemExistsAsync("""
            namespace A.B { }

            namespace A.$$
            """, "B", sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_Qualified_TopLevelWithPeer_FileScopedNamespace()
        => VerifyItemExistsAsync("""
            namespace A.B { }

            namespace A.$$;
            """, "B", sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_Qualified_NestedWithPeer()
        => VerifyExpectedItemsAsync("""
            namespace A
            {
                namespace B.C { }

                namespace B.$$
            }
            """,
            [
                ItemExpectation.Absent("A"),
                ItemExpectation.Absent("B"),
                ItemExpectation.Exists("C")
            ],
            sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_Qualified_WithNested()
        => VerifyExpectedItemsAsync("""
            namespace A.$$
            {
                namespace B { }
            }
            """,
            [
                ItemExpectation.Absent("A"),
                ItemExpectation.Absent("B")
            ],
            sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_Qualified_WithNestedAndMatchingPeer()
        => VerifyExpectedItemsAsync("""
            namespace A.B { }

            namespace A.$$
            {
                namespace B { }
            }
            """,
            [
                ItemExpectation.Absent("A"),
                ItemExpectation.Exists("B")
            ],
            sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_Qualified_InnerCompletionPosition()
        => VerifyExpectedItemsAsync(@"namespace Sys$$tem.Runtime { }",
            [
                ItemExpectation.Exists("System"),
                ItemExpectation.Absent("Runtime")
            ],
            sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_OnKeyword()
        => VerifyItemExistsAsync(@"name$$space System { }", "System", sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_OnNestedKeyword()
        => VerifyExpectedItemsAsync("""
            namespace System
            {
                name$$space Runtime { }
            }
            """,
            [
                ItemExpectation.Absent("System"),
                ItemExpectation.Absent("Runtime")
            ],
            sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceName_Qualified_IncompleteDeclaration()
        => VerifyExpectedItemsAsync(
            """
            namespace A
            {
                namespace B
                {
                    namespace C.$$

                    namespace C.D1 { }
                }

                namespace B.C.D2 { }
            }

            namespace A.B.C.D3 { }
            """, [
                ItemExpectation.Absent("A"),
                ItemExpectation.Absent("B"),
                ItemExpectation.Absent("C"),

                // Ideally, all the D* namespaces would be recommended but, because of how the parser
                // recovers from the missing braces, they end up with the following qualified names...
                //
                //     D1 => A.B.C.C.?.D1
                //     D2 => A.B.B.C.D2
                //     D3 => A.A.B.C.D3
                //
                // ...none of which are found by the current algorithm.
                ItemExpectation.Absent("D1"),
                ItemExpectation.Absent("D2"),
                ItemExpectation.Absent("D3")
            ],
            SourceCodeKind.Regular);

    [Fact]
    public Task UnderNamespace()
        => VerifyExpectedItemsAsync(@"namespace NS { $$", [
            ItemExpectation.Absent("String"),
            ItemExpectation.Absent("System")
        ]);

    [Fact]
    public Task OutsideOfType1()
        => VerifyExpectedItemsAsync("""
            namespace NS {
            class CL {}
            $$
            """, [
            ItemExpectation.Absent("String"),
            ItemExpectation.Absent("System")
        ]);

    [Fact]
    public async Task OutsideOfType2()
    {
        var content = """
            namespace NS {
            class CL {}
            $$
            """;
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Absent("String"),
            ItemExpectation.Absent("System")
        ]);
    }

    [Fact]
    public Task CompletionInsideProperty()
        => VerifyExpectedItemsAsync("""
            class C
            {
                private string name;
                public string Name
                {
                    set
                    {
                        name = $$
            """, [
            ItemExpectation.Exists("value"),
            ItemExpectation.Exists("C")
        ]);

    [Fact]
    public async Task AfterDot()
    {
        var source = AddUsingDirectives("using System;", @"[assembly: A.$$");
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Absent("String"),
            ItemExpectation.Absent("System")
        ]);
    }

    [Fact]
    public async Task UsingAlias()
    {
        var source = AddUsingDirectives("using System;", @"using MyType = $$");
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Absent("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task IncompleteMember()
    {
        var content = """
            class CL {
                $$
            """;
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task IncompleteMemberAccessibility()
    {
        var content = """
            class CL {
                public $$
            """;
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task BadStatement()
    {
        var source = AddUsingDirectives("using System;", AddInsideMethod(@"var t = $$)c"));
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task TypeTypeParameter()
    {
        var source = AddUsingDirectives("using System;", @"class CL<$$");
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Absent("String"),
            ItemExpectation.Absent("System")
        ]);
    }

    [Fact]
    public async Task TypeTypeParameterList()
    {
        var source = AddUsingDirectives("using System;", @"class CL<T, $$");
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Absent("String"),
            ItemExpectation.Absent("System")
        ]);
    }

    [Fact]
    public async Task CastExpressionTypePart()
    {
        var source = AddUsingDirectives("using System;", AddInsideMethod(@"var t = ($$)c"));
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ObjectCreationExpression()
    {
        var source = AddUsingDirectives("using System;", AddInsideMethod(@"var t = new $$"));
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ArrayCreationExpression()
    {
        var source = AddUsingDirectives("using System;", AddInsideMethod(@"var t = new $$ ["));
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task StackAllocArrayCreationExpression()
    {
        var source = AddUsingDirectives("using System;", AddInsideMethod(@"var t = stackalloc $$"));
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task FromClauseTypeOptPart()
    {
        var source = AddUsingDirectives("using System;", AddInsideMethod(@"var t = from $$ c"));
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task JoinClause()
    {
        var source = AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join $$ j"));
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task DeclarationStatement()
    {
        var source = AddUsingDirectives("using System;", AddInsideMethod(@"$$ i ="));
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task VariableDeclaration()
    {
        var source = AddUsingDirectives("using System;", AddInsideMethod(@"fixed($$"));
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ForEachStatement()
    {
        var source = AddUsingDirectives("using System;", AddInsideMethod(@"foreach($$"));
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ForEachStatementNoToken()
    {
        var source = AddUsingDirectives("using System;", AddInsideMethod(@"foreach $$"));
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Absent("String"),
            ItemExpectation.Absent("System")
        ]);
    }

    [Fact]
    public async Task CatchDeclaration()
    {
        var source = AddUsingDirectives("using System;", AddInsideMethod(@"try {} catch($$"));
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task FieldDeclaration()
    {
        var content = """
            class CL {
                $$ i
            """;
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task EventFieldDeclaration()
    {
        var content = """
            class CL {
                event $$
            """;
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ConversionOperatorDeclaration()
    {
        var content = """
            class CL {
                explicit operator $$
            """;
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ConversionOperatorDeclarationNoToken()
    {
        var content = """
            class CL {
                explicit $$
            """;
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Absent("String"),
            ItemExpectation.Absent("System")
        ]);
    }

    [Fact]
    public async Task PropertyDeclaration()
    {
        var content = """
            class CL {
                $$ Prop {
            """;
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task EventDeclaration()
    {
        var content = """
            class CL {
                event $$ Event {
            """;
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task IndexerDeclaration()
    {
        var content = """
            class CL {
                $$ this
            """;
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task Parameter()
    {
        var content = """
            class CL {
                void Method($$
            """;
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ArrayType()
    {
        var content = """
            class CL {
                $$ [
            """;
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task PointerType()
    {
        var content = """
            class CL {
                $$ *
            """;
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task NullableType()
    {
        var content = """
            class CL {
                $$ ?
            """;
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task DelegateDeclaration()
    {
        var content = """
            class CL {
                delegate $$
            """;
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task MethodDeclaration()
    {
        var content = """
            class CL {
                $$ M(
            """;
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task OperatorDeclaration()
    {
        var content = """
            class CL {
                $$ operator
            """;
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ParenthesizedExpression()
    {
        var content = @"($$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task InvocationExpression()
    {
        var content = @"$$(";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ElementAccessExpression()
    {
        var content = @"$$[";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task Argument()
    {
        var content = @"i[$$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task CastExpressionExpressionPart()
    {
        var content = @"(c)$$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task FromClauseInPart()
    {
        var content = @"var t = from c in $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task LetClauseExpressionPart()
    {
        var content = @"var t = from c in C let n = $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task OrderingExpressionPart()
    {
        var content = @"var t = from c in C orderby $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task SelectClauseExpressionPart()
    {
        var content = @"var t = from c in C select $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ExpressionStatement()
    {
        var content = @"$$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ReturnStatement()
    {
        var content = @"return $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ThrowStatement()
    {
        var content = @"throw $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/760097")]
    public async Task YieldReturnStatement()
    {
        var content = @"yield return $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ForEachStatementExpressionPart()
    {
        var content = @"foreach(T t in $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task UsingStatementExpressionPart()
    {
        var content = @"using($$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task LockStatement()
    {
        var content = @"lock($$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task EqualsValueClause()
    {
        var content = @"var i = $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ForStatementInitializersPart()
    {
        var content = @"for($$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ForStatementConditionOptPart()
    {
        var content = @"for(i=0;$$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ForStatementIncrementorsPart()
    {
        var content = @"for(i=0;i>10;$$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task DoStatementConditionPart()
    {
        var content = @"do {} while($$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task WhileStatementConditionPart()
    {
        var content = @"while($$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ArrayRankSpecifierSizesPart()
    {
        var content = @"int [$$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task PrefixUnaryExpression()
    {
        var content = @"+$$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task PostfixUnaryExpression()
    {
        var content = @"$$++";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task BinaryExpressionLeftPart()
    {
        var content = @"$$ + 1";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task BinaryExpressionRightPart()
    {
        var content = @"1 + $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task AssignmentExpressionLeftPart()
    {
        var content = @"$$ = 1";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task AssignmentExpressionRightPart()
    {
        var content = @"1 = $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ConditionalExpressionConditionPart()
    {
        var content = @"$$? 1:";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ConditionalExpressionWhenTruePart()
    {
        var content = @"true? $$:";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task ConditionalExpressionWhenFalsePart()
    {
        var content = @"true? 1:$$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task JoinClauseInExpressionPart()
    {
        var content = @"var t = from c in C join p in $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task JoinClauseLeftExpressionPart()
    {
        var content = @"var t = from c in C join p in P on $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task JoinClauseRightExpressionPart()
    {
        var content = @"var t = from c in C join p in P on id equals $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task WhereClauseConditionPart()
    {
        var content = @"var t = from c in C where $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task GroupClauseGroupExpressionPart()
    {
        var content = @"var t = from c in C group $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task GroupClauseByExpressionPart()
    {
        var content = @"var t = from c in C group g by $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task IfStatement()
    {
        var content = @"if ($$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task SwitchStatement()
    {
        var content = @"switch($$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task SwitchLabelCase()
    {
        var content = @"switch(i) { case $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task SwitchPatternLabelCase()
    {
        var content = @"switch(i) { case $$ when";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33915")]
    public async Task SwitchExpressionFirstBranch()
    {
        var content = @"i switch { $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33915")]
    public async Task SwitchExpressionSecondBranch()
    {
        var content = @"i switch { 1 => true, $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33915")]
    public async Task PositionalPatternFirstPosition()
    {
        var content = @"i is ($$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33915")]
    public async Task PositionalPatternSecondPosition()
    {
        var content = @"i is (1, $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33915")]
    public async Task PropertyPatternFirstPosition()
    {
        var content = @"i is { P: $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33915")]
    public async Task PropertyPatternSecondPosition()
    {
        var content = @"i is { P1: 1, P2: $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task InitializerExpression()
    {
        var content = @"var t = new [] { $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TypeParameterConstraintClause()
        => VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL<T> where T : $$"), @"System");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TypeParameterConstraintClause_NotStaticClass()
        => VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> where T : $$"), @"Console");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TypeParameterConstraintClause_StillShowStaticClassWhenHaveInternalType()
        => VerifyItemExistsAsync(
            """
            static class Test
            {
                public interface IInterface {}
            }

            class CL<T> where T : $$
            """, @"Test");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TypeParameterConstraintClause_NotSealedClass()
        => VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> where T : $$"), @"String");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TypeParameterConstraintClause_StillShowSealedClassWhenHaveInternalType()
        => VerifyItemExistsAsync(
            """
            sealed class Test
            {
                public interface IInterface {}
            }

            class CL<T> where T : $$
            """, @"Test");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public async Task TypeParameterConstraintClause_StillShowStaticAndSealedTypesNotDirectlyInConstraint()
    {
        var content = @"class CL<T> where T : IList<$$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TypeParameterConstraintClauseList()
        => VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL<T> where T : A, $$"), @"System");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TypeParameterConstraintClauseList_NotStaticClass()
        => VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> where T : A, $$"), @"Console");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TypeParameterConstraintClauseList_StillShowStaticClassWhenHaveInternalType()
        => VerifyItemExistsAsync(
            """
            static class Test
            {
                public interface IInterface {}
            }

            class CL<T> where T : A, $$
            """, @"Test");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TypeParameterConstraintClauseList_NotSealedClass()
        => VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> where T : A, $$"), @"String");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TypeParameterConstraintClauseList_StillShowSealedClassWhenHaveInternalType()
        => VerifyItemExistsAsync(
            """
            sealed class Test
            {
                public interface IInterface {}
            }

            class CL<T> where T : A, $$
            """, @"Test");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public async Task TypeParameterConstraintClauseList_StillShowStaticAndSealedTypesNotDirectlyInConstraint()
    {
        var content = @"class CL<T> where T : A, IList<$$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task TypeParameterConstraintClauseAnotherWhere()
    {
        var content = @"class CL<T> where T : A where$$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Absent("String"),
            ItemExpectation.Absent("System")
        ]);
    }

    [Fact]
    public async Task TypeSymbolOfTypeParameterConstraintClause1()
    {
        await VerifyItemExistsAsync(@"class CL<T> where $$", @"T");
        await VerifyItemExistsAsync(@"class CL{ delegate void F<T>() where $$} ", @"T");
        await VerifyItemExistsAsync(@"class CL{ void F<T>() where $$", @"T");
    }

    [Fact]
    public async Task TypeSymbolOfTypeParameterConstraintClause2()
    {
        await VerifyItemIsAbsentAsync(@"class CL<T> where $$", @"System");
        await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> where $$"), @"String");
    }

    [Fact]
    public async Task TypeSymbolOfTypeParameterConstraintClause3()
    {
        await VerifyItemIsAbsentAsync(@"class CL<T1> { void M<T2> where $$", @"T1");
        await VerifyItemExistsAsync(@"class CL<T1> { void M<T2>() where $$", @"T2");
    }

    [Fact]
    public async Task BaseList1()
    {
        var content = @"class CL : $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task BaseList2()
    {
        var content = @"class CL : B, $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task BaseListWhere()
    {
        var content = @"class CL<T> : B where$$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Absent("String"),
            ItemExpectation.Absent("System")
        ]);
    }

    [Fact]
    public async Task AliasedName()
    {
        var content = @"global::$$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Absent("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task AliasedNamespace()
        => await VerifyItemExistsAsync(AddUsingDirectives("using S = System;", AddInsideMethod(@"S.$$")), @"String");

    [Fact]
    public async Task AliasedType()
        => await VerifyItemExistsAsync(AddUsingDirectives("using S = System.String;", AddInsideMethod(@"S.$$")), @"Empty");

    [Fact]
    public async Task ConstructorInitializer()
    {
        var content = @"class C { C() : $$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Absent("String"),
            ItemExpectation.Absent("System")
        ]);
    }

    [Fact]
    public async Task Typeof1()
    {
        var content = @"typeof($$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task Typeof2()
        => await VerifyItemIsAbsentAsync(AddInsideMethod(@"var x = 0; typeof($$"), @"x");

    [Fact]
    public async Task Sizeof1()
    {
        var content = @"sizeof($$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task Sizeof2()
        => await VerifyItemIsAbsentAsync(AddInsideMethod(@"var x = 0; sizeof($$"), @"x");

    [Fact]
    public async Task Default1()
    {
        var content = @"default($$";
        var source = AddUsingDirectives("using System;", content);
        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("System")
        ]);
    }

    [Fact]
    public async Task Default2()
        => await VerifyItemIsAbsentAsync(AddInsideMethod(@"var x = 0; default($$"), @"x");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543819")]
    public async Task Checked()
        => await VerifyItemExistsAsync(AddInsideMethod(@"var x = 0; checked($$"), @"x");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543819")]
    public async Task Unchecked()
        => await VerifyItemExistsAsync(AddInsideMethod(@"var x = 0; unchecked($$"), @"x");

    [Fact]
    public async Task Locals()
        => await VerifyItemExistsAsync(@"class c { void M() { string goo; $$", "goo");

    [Fact]
    public async Task Parameters_01()
        => await VerifyItemExistsAsync(@"class c { void M(string args) { $$", "args");

    [Theory]
    [InlineData("a")]
    [InlineData("ar")]
    [InlineData("arg")]
    [InlineData("args")]
    public Task Parameters_02(string prefix)
        => VerifyItemExistsAsync(prefix + "$$", "args", sourceCodeKind: SourceCodeKind.Regular);

    [Theory]
    [InlineData("a")]
    [InlineData("ar")]
    [InlineData("arg")]
    [InlineData("args")]
    public Task Parameters_03(string prefix)
        => VerifyItemIsAbsentAsync(prefix + "$$", "args", sourceCodeKind: SourceCodeKind.Script);

    [Theory]
    [InlineData("a")]
    [InlineData("ar")]
    [InlineData("arg")]
    [InlineData("args")]
    public Task Parameters_04(string prefix)
        => VerifyItemExistsAsync(prefix + """
            $$
            Systen.Console.WriteLine();
            """, "args", sourceCodeKind: SourceCodeKind.Regular);

    [Theory]
    [InlineData("a")]
    [InlineData("ar")]
    [InlineData("arg")]
    [InlineData("args")]
    public Task Parameters_05(string prefix)
        => VerifyItemExistsAsync("""
            Systen.Console.WriteLine();
            """ + prefix + "$$", "args", sourceCodeKind: SourceCodeKind.Regular);

    [Theory]
    [InlineData("a")]
    [InlineData("ar")]
    [InlineData("arg")]
    [InlineData("args")]
    public Task Parameters_06(string prefix)
        => VerifyItemExistsAsync("""
            Systen.Console.WriteLine();
            """ + prefix + """
            $$
            Systen.Console.WriteLine();
            """, "args", sourceCodeKind: SourceCodeKind.Regular);

    [Theory]
    [InlineData("a")]
    [InlineData("ar")]
    [InlineData("arg")]
    [InlineData("args")]
    public Task Parameters_07(string prefix)
        => VerifyItemExistsAsync("call(" + prefix + "$$)", "args", sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55969")]
    public async Task Parameters_TopLevelStatement_1()
        => await VerifyItemIsAbsentAsync(@"$$", "args", sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55969")]
    public async Task Parameters_TopLevelStatement_2()
        => await VerifyItemExistsAsync(
            """
            using System;
            Console.WriteLine();
            $$
            """, "args", sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55969")]
    public async Task Parameters_TopLevelStatement_3()
        => await VerifyItemIsAbsentAsync(
            """
            using System;
            $$
            """, "args", sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55969")]
    public async Task Parameters_TopLevelStatement_4()
        => await VerifyItemExistsAsync(@"string first = $$", "args", sourceCodeKind: SourceCodeKind.Regular);

    [Fact]
    public async Task LambdaDiscardParameters()
        => await VerifyItemIsAbsentAsync(@"class C { void M() { System.Func<int, string, int> f = (int _, string _) => 1 + $$", "_");

    [Fact]
    public async Task AnonymousMethodDiscardParameters()
        => await VerifyItemIsAbsentAsync(@"class C { void M() { System.Func<int, string, int> f = delegate(int _, string _) { return 1 + $$ }; } }", "_");

    [Fact]
    public async Task CommonTypesInNewExpressionContext()
        => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class c { void M() { new $$"), "Exception");

    [Fact]
    public async Task NoCompletionForUnboundTypes()
        => await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class c { void M() { goo.$$"), "Equals");

    [Fact]
    public async Task NoParametersInTypeOf()
        => await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class c { void M(int x) { typeof($$"), "x");

    [Fact]
    public async Task NoParametersInDefault()
        => await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class c { void M(int x) { default($$"), "x");

    [Fact]
    public async Task NoParametersInSizeOf()
        => await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"public class C { void M(int x) { unsafe { sizeof($$"), "x");

    [Fact]
    public async Task NoParametersInGenericParameterList()
        => await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"public class Generic<T> { void M(int x) { Generic<$$"), "x");

    [Fact]
    public async Task NoMembersAfterNullLiteral()
        => await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"public class C { void M() { null.$$"), "Equals");

    [Fact]
    public async Task MembersAfterTrueLiteral()
        => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { true.$$"), "Equals");

    [Fact]
    public async Task MembersAfterFalseLiteral()
        => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { false.$$"), "Equals");

    [Fact]
    public async Task MembersAfterCharLiteral()
        => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { 'c'.$$"), "Equals");

    [Fact]
    public async Task MembersAfterStringLiteral()
        => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { """".$$"), "Equals");

    [Fact]
    public async Task MembersAfterVerbatimStringLiteral()
        => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { @"""".$$"), "Equals");

    [Fact]
    public Task MembersAfterNumericLiteral()
        => VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { 2.$$"), "Equals");

    [Fact]
    public async Task NoMembersAfterParenthesizedNullLiteral()
        => await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"public class C { void M() { (null).$$"), "Equals");

    [Fact]
    public async Task MembersAfterParenthesizedTrueLiteral()
        => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { (true).$$"), "Equals");

    [Fact]
    public async Task MembersAfterParenthesizedFalseLiteral()
        => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { (false).$$"), "Equals");

    [Fact]
    public async Task MembersAfterParenthesizedCharLiteral()
        => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { ('c').$$"), "Equals");

    [Fact]
    public async Task MembersAfterParenthesizedStringLiteral()
        => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { ("""").$$"), "Equals");

    [Fact]
    public async Task MembersAfterParenthesizedVerbatimStringLiteral()
        => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { (@"""").$$"), "Equals");

    [Fact]
    public async Task MembersAfterParenthesizedNumericLiteral()
        => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { (2).$$"), "Equals");

    [Fact]
    public async Task MembersAfterArithmeticExpression()
        => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { (1 + 1).$$"), "Equals");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539332")]
    public async Task InstanceTypesAvailableInUsingAlias()
        => await VerifyItemExistsAsync(@"using S = System.$$", "String");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
    public Task InheritedMember1()
        => VerifyExpectedItemsAsync("""
            class A
            {
                private void Hidden() { }
                protected void Goo() { }
            }
            class B : A
            {
                void Bar()
                {
                    $$
                }
            }
            """, [
            ItemExpectation.Absent("Hidden"),
            ItemExpectation.Exists("Goo")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
    public Task InheritedMember2()
        => VerifyExpectedItemsAsync("""
            class A
            {
                private void Hidden() { }
                protected void Goo() { }
            }
            class B : A
            {
                void Bar()
                {
                    this.$$
                }
            }
            """, [
            ItemExpectation.Absent("Hidden"),
            ItemExpectation.Exists("Goo")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
    public Task InheritedMember3()
        => VerifyExpectedItemsAsync("""
            class A
            {
                private void Hidden() { }
                protected void Goo() { }
            }
            class B : A
            {
                void Bar()
                {
                    base.$$
                }
            }
            """, [
            ItemExpectation.Absent("Hidden"),
            ItemExpectation.Exists("Goo"),
            ItemExpectation.Absent("Bar"),
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
    public Task InheritedStaticMember1()
        => VerifyExpectedItemsAsync("""
            class A
            {
                private static void Hidden() { }
                protected static void Goo() { }
            }
            class B : A
            {
                void Bar()
                {
                    $$
                }
            }
            """, [
            ItemExpectation.Absent("Hidden"),
            ItemExpectation.Exists("Goo")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
    public Task InheritedStaticMember2()
        => VerifyExpectedItemsAsync("""
            class A
            {
                private static void Hidden() { }
                protected static void Goo() { }
            }
            class B : A
            {
                void Bar()
                {
                    B.$$
                }
            }
            """, [
            ItemExpectation.Absent("Hidden"),
            ItemExpectation.Exists("Goo")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
    public Task InheritedStaticMember3()
        => VerifyExpectedItemsAsync("""
            class A
            {
                 private static void Hidden() { }
                 protected static void Goo() { }
            }
            class B : A
            {
                void Bar()
                {
                    A.$$
                }
            }
            """, [
            ItemExpectation.Absent("Hidden"),
            ItemExpectation.Exists("Goo")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
    public Task InheritedInstanceAndStaticMembers()
        => VerifyExpectedItemsAsync("""
            class A
            {
                 private static void HiddenStatic() { }
                 protected static void GooStatic() { }

                 private void HiddenInstance() { }
                 protected void GooInstance() { }
            }
            class B : A
            {
                void Bar()
                {
                    $$
                }
            }
            """, [
            ItemExpectation.Absent("HiddenStatic"),
            ItemExpectation.Exists("GooStatic"),
            ItemExpectation.Absent("HiddenInstance"),
            ItemExpectation.Exists("GooInstance")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540155")]
    public Task ForLoopIndexer1()
        => VerifyItemExistsAsync("""
            class C
            {
                void M()
                {
                    for (int i = 0; $$
            """, "i");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540155")]
    public Task ForLoopIndexer2()
        => VerifyItemExistsAsync("""
            class C
            {
                void M()
                {
                    for (int i = 0; i < 10; $$
            """, "i");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
    public Task NoInstanceMembersAfterType1()
        => VerifyItemIsAbsentAsync("""
            class C
            {
                void M()
                {
                    System.IDisposable.$$
            """, "Dispose");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
    public Task NoInstanceMembersAfterType2()
        => VerifyItemIsAbsentAsync("""
            class C
            {
                void M()
                {
                    (System.IDisposable).$$
            """, "Dispose");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
    public Task NoInstanceMembersAfterType3()
        => VerifyItemIsAbsentAsync("""
            using System;
            class C
            {
                void M()
                {
                    IDisposable.$$
            """, "Dispose");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
    public Task NoInstanceMembersAfterType4()
        => VerifyItemIsAbsentAsync("""
            using System;
            class C
            {
                void M()
                {
                    (IDisposable).$$
            """, "Dispose");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
    public Task StaticMembersAfterType1()
        => VerifyItemExistsAsync("""
            class C
            {
                void M()
                {
                    System.IDisposable.$$
            """, "ReferenceEquals");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
    public Task StaticMembersAfterType2()
        => VerifyItemIsAbsentAsync("""
            class C
            {
                void M()
                {
                    (System.IDisposable).$$
            """, "ReferenceEquals");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
    public Task StaticMembersAfterType3()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                void M()
                {
                    IDisposable.$$
            """, "ReferenceEquals");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
    public Task StaticMembersAfterType4()
        => VerifyItemIsAbsentAsync("""
            using System;
            class C
            {
                void M()
                {
                    (IDisposable).$$
            """, "ReferenceEquals");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540197")]
    public Task TypeParametersInClass()
        => VerifyItemExistsAsync("""
            class C<T, R>
            {
                $$
            }
            """, "T");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540212")]
    public Task AfterRefInLambda_TypeOnly()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                void M(String parameter)
                {
                    Func<int, int> f = (ref $$
                }
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Absent("parameter")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540212")]
    public Task AfterOutInLambda_TypeOnly()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                void M(String parameter)
                {
                    Func<int, int> f = (out $$
                }
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Absent("parameter")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24326")]
    public Task AfterInInLambda_TypeOnly()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                void M(String parameter)
                {
                    Func<int, int> f = (in $$
                }
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Absent("parameter")
        ]);

    [Fact]
    public Task AfterRefInMethodDeclaration_TypeOnly()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                String field;
                void M(ref $$)
                {
                }
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Absent("field")
        ]);

    [Fact]
    public Task AfterOutInMethodDeclaration_TypeOnly()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                String field;
                void M(out $$)
                {
                }
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Absent("field")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24326")]
    public Task AfterInInMethodDeclaration_TypeOnly()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                String field;
                void M(in $$)
                {
                }
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Absent("field")
        ]);

    [Fact]
    public Task AfterRefInInvocation_TypeAndVariable()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                void M(ref String parameter)
                {
                    M(ref $$
                }
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("parameter")
        ]);

    [Fact]
    public Task AfterOutInInvocation_TypeAndVariable()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                void M(out String parameter)
                {
                    M(out $$
                }
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("parameter")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24326")]
    public Task AfterInInInvocation_TypeAndVariable()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                void M(in String parameter)
                {
                    M(in $$
                }
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("parameter")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25569")]
    public Task AfterRefExpression_TypeAndVariable()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                void M(String parameter)
                {
                    ref var x = ref $$
                }
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Exists("parameter")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25569")]
    public Task AfterRefInStatementContext_TypeOnly()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                void M(String parameter)
                {
                    ref $$
                }
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Absent("parameter")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25569")]
    public Task AfterRefReadonlyInStatementContext_TypeOnly()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                void M(String parameter)
                {
                    ref readonly $$
                }
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Absent("parameter")
        ]);

    [Fact]
    public Task AfterRefLocalDeclaration_TypeOnly()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                void M(String parameter)
                {
                    ref $$ int local;
                }
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Absent("parameter")
        ]);

    [Fact]
    public Task AfterRefReadonlyLocalDeclaration_TypeOnly()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                void M(String parameter)
                {
                    ref readonly $$ int local;
                }
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Absent("parameter")
        ]);

    [Fact]
    public Task AfterRefLocalFunction_TypeOnly()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                void M(String parameter)
                {
                    ref $$ int Function();
                }
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Absent("parameter")
        ]);

    [Fact]
    public Task AfterRefReadonlyLocalFunction_TypeOnly()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                void M(String parameter)
                {
                    ref readonly $$ int Function();
                }
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Absent("parameter")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35178")]
    public Task RefStructMembersEmptyByDefault()
        => VerifyNoItemsExistAsync("""
            ref struct Test {}
            class C
            {
                void M()
                {
                    var test = new Test();
                    test.$$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35178")]
    public Task RefStructMembersHasMethodIfItWasOverriden()
        => VerifyExpectedItemsAsync("""
            ref struct Test
            {
                public override string ToString() => string.Empty;
            }
            class C
            {
                void M()
                {
                    var test = new Test();
                    test.$$
                }
            }
            """, [
            ItemExpectation.Exists("ToString"),
            ItemExpectation.Absent("GetType"),
            ItemExpectation.Absent("Equals"),
            ItemExpectation.Absent("GetHashCode")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35178")]
    public Task RefStructMembersHasMethodsForNameof()
        => VerifyExpectedItemsAsync("""
            ref struct Test {}
            class C
            {
                void M()
                {
                    var test = new Test();
                    _ = nameof(test.$$);
                }
            }
            """, [
            ItemExpectation.Exists("ToString"),
            ItemExpectation.Exists("GetType"),
            ItemExpectation.Exists("Equals"),
            ItemExpectation.Exists("GetHashCode")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53585")]
    public Task AfterStaticLocalFunction_TypeOnly()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                void M(String parameter)
                {
                    static $$
                }
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Absent("parameter")
        ]);

    [Theory]
    [WorkItem("https://github.com/dotnet/roslyn/issues/53585")]
    [InlineData("extern")]
    [InlineData("static extern")]
    [InlineData("extern static")]
    [InlineData("unsafe")]
    [InlineData("static unsafe")]
    [InlineData("unsafe static")]
    [InlineData("unsafe extern")]
    [InlineData("extern unsafe")]
    public Task AfterLocalFunction_TypeOnly(string keyword)
        => VerifyExpectedItemsAsync($$"""
            using System;
            class C
            {
                void M(String parameter)
                {
                    {{keyword}} $$
                }
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Absent("parameter")
        ]);

    [Theory]
    [WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
    [InlineData("async")]
    [InlineData("static async")]
    [InlineData("async static")]
    [InlineData("async unsafe")]
    [InlineData("unsafe async")]
    [InlineData("extern unsafe async static")]
    public Task AfterLocalFunction_TypeOnly_Async(string keyword)
        => VerifyExpectedItemsAsync($$"""
            using System;
            class C
            {
                void M(String parameter)
                {
                    {{keyword}} $$
                }
            }
            """, [
            ItemExpectation.Absent("String"),
            ItemExpectation.Absent("parameter")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
    public Task AfterAsyncLocalFunctionWithTwoAsyncs()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                void M(string parameter)
                {
                    async async $$
                }
            }
            """, [
            ItemExpectation.Absent("String"),
            ItemExpectation.Absent("parameter")
        ]);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/53585")]
    [InlineData("void")]
    [InlineData("string")]
    [InlineData("String")]
    [InlineData("(int, int)")]
    [InlineData("async void")]
    [InlineData("async System.Threading.Tasks.Task")]
    [InlineData("int Function")]
    public Task NotAfterReturnTypeInLocalFunction(string returnType)
        => VerifyExpectedItemsAsync($$"""
            using System;
            class C
            {
                void M(String parameter)
                {
                    static {{returnType}} $$
                }
            }
            """, [
            ItemExpectation.Absent("String"),
            ItemExpectation.Absent("parameter")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25569")]
    public Task AfterRefInMemberContext_TypeOnly()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                String field;
                ref $$
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Absent("field")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25569")]
    public Task AfterRefReadonlyInMemberContext_TypeOnly()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                String field;
                ref readonly $$
            }
            """, [
            ItemExpectation.Exists("String"),
            ItemExpectation.Absent("field")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
    public Task NestedType1()
        => VerifyExpectedItemsAsync("""
            class Q
            {
                $$
                class R
                {

                }
            }
            """, [
            ItemExpectation.Exists("Q"),
            ItemExpectation.Exists("R")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
    public Task NestedType2()
        => VerifyExpectedItemsAsync("""
            class Q
            {
                class R
                {
                    $$
                }
            }
            """, [
            ItemExpectation.Exists("Q"),
            ItemExpectation.Exists("R")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
    public Task NestedType3()
        => VerifyExpectedItemsAsync("""
            class Q
            {
                class R
                {
                }
                $$
            }
            """, [
            ItemExpectation.Exists("Q"),
            ItemExpectation.Exists("R")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
    public async Task NestedType4_Regular()
    {
        var markup = """
            class Q
            {
                class R
                {
                }
            }
            $$
            """; // At EOF

        // Top-level statements are not allowed to follow classes, but we still offer it in completion for a few
        // reasons:
        //
        // 1. The code is simpler
        // 2. It's a relatively rare coding practice to define types outside of namespaces
        // 3. It allows the compiler to produce a better error message when users type things in the wrong order
        await VerifyItemExistsAsync(markup, "Q", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
        await VerifyItemIsAbsentAsync(markup, "R", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
    public async Task NestedType4_Script()
    {
        var markup = """
            class Q
            {
                class R
                {
                }
            }
            $$
            """; // At EOF
        await VerifyItemExistsAsync(markup, "Q", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        await VerifyItemIsAbsentAsync(markup, "R", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
    public Task NestedType5()
        => VerifyExpectedItemsAsync("""
            class Q
            {
                class R
                {
                }
                $$
            """, [
            ItemExpectation.Exists("Q"),
            ItemExpectation.Exists("R")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
    public Task NestedType6()
        => VerifyExpectedItemsAsync("""
            class Q
            {
                class R
                {
                    $$
            """, [
            ItemExpectation.Exists("Q"),
            ItemExpectation.Exists("R")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540574")]
    public Task AmbiguityBetweenTypeAndLocal()
        => VerifyItemExistsAsync("""
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                public void goo() {
                    int i = 5;
                    i.$$
                    List<string> ml = new List<string>();
                }
            }
            """, "CompareTo");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21596")]
    public Task AmbiguityBetweenExpressionAndLocalFunctionReturnType()
        => VerifyItemExistsAsync("""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    AwaitTest test = new AwaitTest();
                    test.Test1().Wait();
                }
            }

            class AwaitTest
            {
                List<string> stringList = new List<string>();

                public async Task<bool> Test1()
                {
                    stringList.$$

                    await Test2();

                    return true;
                }

                public async Task<bool> Test2()
                {
                    return true;
                }
            }
            """, "Add");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540750")]
    public Task CompletionAfterNewInScript()
        => VerifyItemExistsAsync("""
            using System;

            new $$
            """, "String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540933")]
    public Task ExtensionMethodsInScript()
        => VerifyItemExistsAsync("""
            using System.Linq;
            var a = new int[] { 1, 2 };
            a.$$
            """, "ElementAt", displayTextSuffix: "<>", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541019")]
    public Task ExpressionsInForLoopInitializer()
        => VerifyItemExistsAsync("""
            public class C
            {
                public void M()
                {
                    int count = 0;
                    for ($$
            """, "count");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541108")]
    public Task AfterLambdaExpression1()
        => VerifyItemIsAbsentAsync("""
            public class C
            {
                public void M()
                {
                    System.Func<int, int> f = arg => { arg = 2; return arg; }.$$
                }
            }
            """, "ToString");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541108")]
    public Task AfterLambdaExpression2()
        => VerifyExpectedItemsAsync("""
            public class C
            {
                public void M()
                {
                    ((System.Func<int, int>)(arg => { arg = 2; return arg; })).$$
                }
            }
            """, [
            ItemExpectation.Exists("ToString"),
            ItemExpectation.Exists("Invoke")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541216")]
    public Task InMultiLineCommentAtEndOfFile()
        => VerifyItemIsAbsentAsync("""
            using System;
            /*$$
            """, "Console", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541218")]
    public Task TypeParametersAtEndOfFile()
        => VerifyItemExistsAsync("""
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Outer<T>
            {
            class Inner<U>
            {
            static void F(T t, U u)
            {
            return;
            }
            public static void F(T t)
            {
            Outer<$$
            """, "T");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552717")]
    public Task LabelInCaseSwitchAbsentForCase()
        => VerifyItemIsAbsentAsync("""
            class Program
            {
                static void Main()
                {
                    int x;
                    switch (x)
                    {
                        case 0:
                            goto $$
            """, "case 0:");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552717")]
    public Task LabelInCaseSwitchAbsentForDefaultWhenAbsent()
        => VerifyItemIsAbsentAsync("""
            class Program
            {
                static void Main()
                {
                    int x;
                    switch (x)
                    {
                        case 0:
                            goto $$
            """, "default:");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552717")]
    public Task LabelInCaseSwitchPresentForDefault()
        => VerifyItemExistsAsync("""
            class Program
            {
                static void Main()
                {
                    int x;
                    switch (x)
                    {
                        default:
                            goto $$
            """, "default");

    [Fact]
    public Task LabelAfterGoto1()
        => VerifyItemExistsAsync("""
            class Program
            {
                static void Main()
                {
                Goo:
                    int Goo;
                    goto $$
            """, "Goo");

    [Fact]
    public Task LabelAfterGoto2()
        => VerifyItemIsAbsentAsync("""
            class Program
            {
                static void Main()
                {
                Goo:
                    int Goo;
                    goto Goo $$
            """, "Goo");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
    public Task AttributeName()
        => VerifyExpectedItemsAsync("""
            using System;
            [$$
            """, [
            ItemExpectation.Exists("CLSCompliant"),
            ItemExpectation.Absent("CLSCompliantAttribute")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
    public Task AttributeNameAfterSpecifier()
        => VerifyExpectedItemsAsync("""
            using System;
            [assembly:$$
            """, [
            ItemExpectation.Exists("CLSCompliant"),
            ItemExpectation.Absent("CLSCompliantAttribute")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
    public Task AttributeNameInAttributeList()
        => VerifyExpectedItemsAsync("""
            using System;
            [CLSCompliant, $$
            """, [
            ItemExpectation.Exists("CLSCompliant"),
            ItemExpectation.Absent("CLSCompliantAttribute")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
    public Task AttributeNameBeforeClass()
        => VerifyExpectedItemsAsync("""
            using System;
            [$$
            class C { }
            """, [
            ItemExpectation.Exists("CLSCompliant"),
            ItemExpectation.Absent("CLSCompliantAttribute")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
    public Task AttributeNameAfterSpecifierBeforeClass()
        => VerifyExpectedItemsAsync("""
            using System;
            [assembly:$$
            class C { }
            """, [
            ItemExpectation.Exists("CLSCompliant"),
            ItemExpectation.Absent("CLSCompliantAttribute")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
    public Task AttributeNameInAttributeArgumentList()
        => VerifyExpectedItemsAsync("""
            using System;
            [CLSCompliant($$
            class C { }
            """, [
            ItemExpectation.Exists("CLSCompliantAttribute"),
            ItemExpectation.Absent("CLSCompliant")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
    public Task AttributeNameInsideClass()
        => VerifyExpectedItemsAsync("""
            using System;
            class C { $$ }
            """, [
            ItemExpectation.Exists("CLSCompliantAttribute"),
            ItemExpectation.Absent("CLSCompliant")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542954")]
    public Task NamespaceAliasInAttributeName1()
        => VerifyItemExistsAsync("""
            using Alias = System;

            [$$
            class C { }
            """, "Alias");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542954")]
    public Task NamespaceAliasInAttributeName2()
        => VerifyItemIsAbsentAsync("""
            using Alias = Goo;

            namespace Goo { }

            [$$
            class C { }
            """, "Alias");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542954")]
    public Task NamespaceAliasInAttributeName3()
        => VerifyItemExistsAsync("""
            using Alias = Goo;

            namespace Goo { class A : System.Attribute { } }

            [$$
            class C { }
            """, "Alias");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545121")]
    public Task AttributeNameAfterNamespace()
        => VerifyExpectedItemsAsync("""
            namespace Test
            {
                class MyAttribute : System.Attribute { }
                [Test.$$
                class Program { }
            }
            """, [
            ItemExpectation.Exists("My"),
            ItemExpectation.Absent("MyAttribute")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545121")]
    public Task AttributeNameAfterNamespace2()
        => VerifyExpectedItemsAsync("""
            namespace Test
            {
                namespace Two
                {
                    class MyAttribute : System.Attribute { }
                    [Test.Two.$$
                    class Program { }
                }
            }
            """, [
            ItemExpectation.Exists("My"),
            ItemExpectation.Absent("MyAttribute")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545121")]
    public Task AttributeNameWhenSuffixlessFormIsKeyword()
        => VerifyExpectedItemsAsync("""
            namespace Test
            {
                class namespaceAttribute : System.Attribute { }
                [$$
                class Program { }
            }
            """, [
            ItemExpectation.Exists("namespaceAttribute"),
            ItemExpectation.Absent("namespace"),
            ItemExpectation.Absent("@namespace")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545121")]
    public Task AttributeNameAfterNamespaceWhenSuffixlessFormIsKeyword()
        => VerifyExpectedItemsAsync("""
            namespace Test
            {
                class namespaceAttribute : System.Attribute { }
                [Test.$$
                class Program { }
            }
            """, [
            ItemExpectation.Exists("namespaceAttribute"),
            ItemExpectation.Absent("namespace"),
            ItemExpectation.Absent("@namespace")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545348")]
    public Task KeywordsUsedAsLocals()
        => VerifyExpectedItemsAsync("""
            class C
            {
                void M()
                {
                    var error = 0;
                    var method = 0;
                    var @int = 0;
                    Console.Write($$
                }
            }
            """, [
            // preprocessor keyword
            ItemExpectation.Exists("error"),
            ItemExpectation.Absent("@error"),

            // contextual keyword
            ItemExpectation.Exists("method"),
            ItemExpectation.Absent("@method"),

            // full keyword
            ItemExpectation.Exists("@int"),
            ItemExpectation.Absent("int")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545348")]
    public Task QueryContextualKeywords1()
        => VerifyExpectedItemsAsync("""
            class C
            {
                void M()
                {
                    var from = new[]{1,2,3};
                    var r = from x in $$
                }
            }
            """, [
            ItemExpectation.Exists("@from"),
            ItemExpectation.Absent("from")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545348")]
    public Task QueryContextualKeywords2()
        => VerifyExpectedItemsAsync("""
            class C
            {
                void M()
                {
                    var where = new[] { 1, 2, 3 };
                    var x = from @from in @where
                            where $$ == @where.Length
                            select @from;
                }
            }
            """, [
            ItemExpectation.Exists("@from"),
            ItemExpectation.Absent("from"),
            ItemExpectation.Exists("@where"),
            ItemExpectation.Absent("where")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545348")]
    public Task QueryContextualKeywords3()
        => VerifyExpectedItemsAsync("""
            class C
            {
                void M()
                {
                    var where = new[] { 1, 2, 3 };
                    var x = from @from in @where
                            where @from == @where.Length
                            select $$;
                }
            }
            """, [
            ItemExpectation.Exists("@from"),
            ItemExpectation.Absent("from"),
            ItemExpectation.Exists("@where"),
            ItemExpectation.Absent("where")
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545121")]
    public Task AttributeNameAfterGlobalAlias()
        => VerifyExpectedItemsAsync(
            """
            class MyAttribute : System.Attribute { }
            [global::$$
            class Program { }
            """, [
                ItemExpectation.Exists("My"),
                ItemExpectation.Absent("MyAttribute")
            ],
            SourceCodeKind.Regular);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545121")]
    public Task AttributeNameAfterGlobalAliasWhenSuffixlessFormIsKeyword()
        => VerifyExpectedItemsAsync(
            """
            class namespaceAttribute : System.Attribute { }
            [global::$$
            class Program { }
            """, [
                ItemExpectation.Exists("namespaceAttribute"),
                ItemExpectation.Absent("namespace"),
                ItemExpectation.Absent("@namespace")
            ],
            SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25589")]
    public Task AttributeSearch_NamespaceWithNestedAttribute1()
        => VerifyItemExistsAsync("""
            namespace Namespace1
            {
                namespace Namespace2 { class NonAttribute { } }
                namespace Namespace3.Namespace4 { class CustomAttribute : System.Attribute { } }
            }

            [$$]
            """, "Namespace1");

    [Fact]
    public Task AttributeSearch_NamespaceWithNestedAttribute2()
        => VerifyExpectedItemsAsync("""
            namespace Namespace1
            {
                namespace Namespace2 { class NonAttribute { } }
                namespace Namespace3.Namespace4 { class CustomAttribute : System.Attribute { } }
            }

            [Namespace1.$$]
            """, [
            ItemExpectation.Absent("Namespace2"),
            ItemExpectation.Exists("Namespace3"),
        ]);

    [Fact]
    public Task AttributeSearch_NamespaceWithNestedAttribute3()
        => VerifyItemExistsAsync("""
            namespace Namespace1
            {
                namespace Namespace2 { class NonAttribute { } }
                namespace Namespace3.Namespace4 { class CustomAttribute : System.Attribute { } }
            }

            [Namespace1.Namespace3.$$]
            """, "Namespace4");

    [Fact]
    public Task AttributeSearch_NamespaceWithNestedAttribute4()
        => VerifyItemExistsAsync("""
            namespace Namespace1
            {
                namespace Namespace2 { class NonAttribute { } }
                namespace Namespace3.Namespace4 { class CustomAttribute : System.Attribute { } }
            }

            [Namespace1.Namespace3.Namespace4.$$]
            """, "Custom");

    [Fact]
    public Task AttributeSearch_NamespaceWithNestedAttribute_NamespaceAlias()
        => VerifyExpectedItemsAsync("""
            using Namespace1Alias = Namespace1;
            using Namespace2Alias = Namespace1.Namespace2;
            using Namespace3Alias = Namespace1.Namespace3;
            using Namespace4Alias = Namespace1.Namespace3.Namespace4;

            namespace Namespace1
            {
                namespace Namespace2 { class NonAttribute { } }
                namespace Namespace3.Namespace4 { class CustomAttribute : System.Attribute { } }
            }

            [$$]
            """, [
            ItemExpectation.Exists("Namespace1Alias"),
            ItemExpectation.Absent("Namespace2Alias"),
            ItemExpectation.Exists("Namespace3Alias"),
            ItemExpectation.Exists("Namespace4Alias"),
        ]);

    [Fact]
    public Task AttributeSearch_NamespaceWithoutNestedAttribute()
        => VerifyItemIsAbsentAsync("""
            namespace Namespace1
            {
                namespace Namespace2 { class NonAttribute { } }
                namespace Namespace3.Namespace4 { class NonAttribute : System.NonAttribute { } }
            }

            [$$]
            """, "Namespace1");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542230")]
    public Task RangeVariableInQuerySelect()
        => VerifyItemExistsAsync("""
            using System.Linq;
            class P
            {
                void M()
                {
                    var src = new string[] { "Goo", "Bar" };
                    var q = from x in src
                            select x.$$
            """, "Length");

    [Fact]
    public Task ConstantsInIsExpression()
        => VerifyItemExistsAsync("""
            class C
            {
                public const int MAX_SIZE = 10;
                void M()
                {
                    int i = 10;
                    if (i is $$ int
            """, "MAX_SIZE");

    [Fact]
    public Task ConstantsInIsPatternExpression()
        => VerifyItemExistsAsync("""
            class C
            {
                public const int MAX_SIZE = 10;
                void M()
                {
                    int i = 10;
                    if (i is $$ 1
            """, "MAX_SIZE");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
    public Task ConstantsInSwitchCase()
        => VerifyItemExistsAsync("""
            class C
            {
                public const int MAX_SIZE = 10;
                void M()
                {
                    int i = 10;
                    switch (i)
                    {
                        case $$
            """, "MAX_SIZE");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25084#issuecomment-370148553")]
    public Task ConstantsInSwitchPatternCase()
        => VerifyItemExistsAsync("""
            class C
            {
                public const int MAX_SIZE = 10;
                void M()
                {
                    int i = 10;
                    switch (i)
                    {
                        case $$ when
            """, "MAX_SIZE");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
    public Task ConstantsInSwitchGotoCase()
        => VerifyItemExistsAsync("""
            class C
            {
                public const int MAX_SIZE = 10;
                void M()
                {
                    int i = 10;
                    switch (i)
                    {
                        case MAX_SIZE:
                            break;
                        case GOO:
                            goto case $$
            """, "MAX_SIZE");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
    public Task ConstantsInEnumMember()
        => VerifyItemExistsAsync("""
            class C
            {
                public const int GOO = 0;
                enum E
                {
                    A = $$
            """, "GOO");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
    public Task ConstantsInAttribute1()
        => VerifyItemExistsAsync("""
            class C
            {
                public const int GOO = 0;
                [System.AttributeUsage($$
            """, "GOO");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
    public Task ConstantsInAttribute2()
        => VerifyItemExistsAsync("""
            class C
            {
                public const int GOO = 0;
                [System.AttributeUsage(GOO, $$
            """, "GOO");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
    public Task ConstantsInAttribute3()
        => VerifyItemExistsAsync("""
            class C
            {
                public const int GOO = 0;
                [System.AttributeUsage(validOn: $$
            """, "GOO");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
    public Task ConstantsInAttribute4()
        => VerifyItemExistsAsync("""
            class C
            {
                public const int GOO = 0;
                [System.AttributeUsage(AllowMultiple = $$
            """, "GOO");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
    public Task ConstantsInParameterDefaultValue()
        => VerifyItemExistsAsync("""
            class C
            {
                public const int GOO = 0;
                void M(int x = $$
            """, "GOO");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
    public Task ConstantsInConstField()
        => VerifyItemExistsAsync("""
            class C
            {
                public const int GOO = 0;
                const int BAR = $$
            """, "GOO");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
    public Task ConstantsInConstLocal()
        => VerifyItemExistsAsync("""
            class C
            {
                public const int GOO = 0;
                void M()
                {
                    const int BAR = $$
            """, "GOO");

    [Fact]
    public Task DescriptionWith1Overload()
        => VerifyItemExistsAsync("""
            class C
            {
                void M(int i) { }
                void M()
                {
                    $$
            """, "M", expectedDescriptionOrNull: $"void C.M(int i) (+ 1 {FeaturesResources.overload})");

    [Fact]
    public Task DescriptionWith2Overloads()
        => VerifyItemExistsAsync("""
            class C
            {
                void M(int i) { }
                void M(out int i) { }
                void M()
                {
                    $$
            """, "M", expectedDescriptionOrNull: $"void C.M(int i) (+ 2 {FeaturesResources.overloads_})");

    [Fact]
    public Task DescriptionWith1GenericOverload()
        => VerifyItemExistsAsync("""
            class C
            {
                void M<T>(T i) { }
                void M<T>()
                {
                    $$
            """, "M", displayTextSuffix: "<>", expectedDescriptionOrNull: $"void C.M<T>(T i) (+ 1 {FeaturesResources.generic_overload})");

    [Fact]
    public Task DescriptionWith2GenericOverloads()
        => VerifyItemExistsAsync("""
            class C
            {
                void M<T>(int i) { }
                void M<T>(out int i) { }
                void M<T>()
                {
                    $$
            """, "M", displayTextSuffix: "<>", expectedDescriptionOrNull: $"void C.M<T>(int i) (+ 2 {FeaturesResources.generic_overloads})");

    [Fact]
    public Task DescriptionNamedGenericType()
        => VerifyItemExistsAsync("""
            class C<T>
            {
                void M()
                {
                    $$
            """, "C", displayTextSuffix: "<>", expectedDescriptionOrNull: "class C<T>");

    [Fact]
    public Task DescriptionParameter()
        => VerifyItemExistsAsync("""
            class C<T>
            {
                void M(T goo)
                {
                    $$
            """, "goo", expectedDescriptionOrNull: $"({FeaturesResources.parameter}) T goo");

    [Fact]
    public Task DescriptionGenericTypeParameter()
        => VerifyItemExistsAsync("""
            class C<T>
            {
                void M()
                {
                    $$
            """, "T", expectedDescriptionOrNull: $"T {FeaturesResources.in_} C<T>");

    [Fact]
    public Task DescriptionAnonymousType()
        => VerifyItemExistsAsync("""
            class C
            {
                void M()
                {
                    var a = new { };
                    $$
            """, "a", $$"""
            ({{FeaturesResources.local_variable}}) 'a a

            {{FeaturesResources.Types_colon}}
                'a {{FeaturesResources.is_}} new {  }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543288")]
    public Task AfterNewInAnonymousType()
        => VerifyItemExistsAsync("""
            class Program {
                string field = 0;
                static void Main()     {
                    var an = new {  new $$  }; 
                }
            }
            """, "Program");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543601")]
    public Task NoInstanceFieldsInStaticMethod()
        => VerifyItemIsAbsentAsync("""
            class C
            {
                int x = 0;
                static void M()
                {
                    $$
                }
            }
            """, "x");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543601")]
    public Task NoInstanceFieldsInStaticFieldInitializer()
        => VerifyItemIsAbsentAsync("""
            class C
            {
                int x = 0;
                static int y = $$
            }
            """, "x");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543601")]
    public Task StaticFieldsInStaticMethod()
        => VerifyItemExistsAsync("""
            class C
            {
                static int x = 0;
                static void M()
                {
                    $$
                }
            }
            """, "x");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543601")]
    public Task StaticFieldsInStaticFieldInitializer()
        => VerifyItemExistsAsync("""
            class C
            {
                static int x = 0;
                static int y = $$
            }
            """, "x");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543680")]
    public Task NoInstanceFieldsFromOuterClassInInstanceMethod()
        => VerifyItemIsAbsentAsync("""
            class outer
            {
                int i;
                class inner
                {
                    void M()
                    {
                        $$
                    }
                }
            }
            """, "i");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543680")]
    public Task StaticFieldsFromOuterClassInInstanceMethod()
        => VerifyItemExistsAsync("""
            class outer
            {
                static int i;
                class inner
                {
                    void M()
                    {
                        $$
                    }
                }
            }
            """, "i");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543104")]
    public Task OnlyEnumMembersInEnumMemberAccess()
        => VerifyExpectedItemsAsync("""
            class C
            {
                enum x {a,b,c}
                void M()
                {
                    x.$$
                }
            }
            """, [
            ItemExpectation.Exists("a"),
            ItemExpectation.Exists("b"),
            ItemExpectation.Exists("c"),
            ItemExpectation.Absent("Equals"),
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543104")]
    public Task NoEnumMembersInEnumLocalAccess()
        => VerifyExpectedItemsAsync("""
            class C
            {
                enum x {a,b,c}
                void M()
                {
                    var y = x.a;
                    y.$$
                }
            }
            """, [
            ItemExpectation.Absent("a"),
            ItemExpectation.Absent("b"),
            ItemExpectation.Absent("c"),
            ItemExpectation.Exists("Equals"),
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529138")]
    public Task AfterLambdaParameterDot()
        => VerifyItemExistsAsync("""
            using System;
            using System.Linq;
            class A
            {
                public event Func<String, String> E;
            }

            class Program
            {
                static void Main(string[] args)
                {
                    new A().E += ss => ss.$$
                }
            }
            """, "Substring");

    [Fact, WorkItem(61343, "https://github.com/dotnet/roslyn/issues/61343")]
    public async Task LambdaParameterMemberAccessOverloads()
    {
        var markup = """
            using System.Linq;

            public class C
            {
                public void M() { }
                public void M(int i) { }
                public int P { get; }

                void Test()
                {
                    new C[0].Select(x => x.$$)
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "M", expectedDescriptionOrNull: $"void C.M() (+ 1 {FeaturesResources.overload})");
        await VerifyItemExistsAsync(markup, "P", expectedDescriptionOrNull: "int C.P { get; }");
    }

    [Fact]
    public Task ValueNotAtRoot_Interactive()
        => VerifyItemIsAbsentAsync(
@"$$",
"value",
expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);

    [Fact]
    public Task ValueNotAfterClass_Interactive()
        => VerifyItemIsAbsentAsync(
            """
            class C { }
            $$
            """,
            "value",
            expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);

    [Fact]
    public Task ValueNotAfterGlobalStatement_Interactive()
        => VerifyItemIsAbsentAsync(
            """
            System.Console.WriteLine();
            $$
            """,
            "value",
            expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);

    [Fact]
    public Task ValueNotAfterGlobalVariableDeclaration_Interactive()
        => VerifyItemIsAbsentAsync(
            """
            int i = 0;
            $$
            """,
            "value",
            expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);

    [Fact]
    public Task ValueNotInUsingAlias()
        => VerifyItemIsAbsentAsync(
@"using Goo = $$",
"value");

    [Fact]
    public Task ValueNotInEmptyStatement()
        => VerifyItemIsAbsentAsync(AddInsideMethod(
@"$$"),
"value");

    [Fact]
    public Task ValueInsideSetter()
        => VerifyItemExistsAsync(
            """
            class C {
                int Goo {
                  set {
                    $$
            """,
            "value");

    [Fact]
    public Task ValueInsideAdder()
        => VerifyItemExistsAsync(
            """
            class C {
                event int Goo {
                  add {
                    $$
            """,
            "value");

    [Fact]
    public Task ValueInsideRemover()
        => VerifyItemExistsAsync(
            """
            class C {
                event int Goo {
                  remove {
                    $$
            """,
            "value");

    [Fact]
    public Task ValueNotAfterDot()
        => VerifyItemIsAbsentAsync(
            """
            class C {
                int Goo {
                  set {
                    this.$$
            """,
            "value");

    [Fact]
    public Task ValueNotAfterArrow()
        => VerifyItemIsAbsentAsync(
            """
            class C {
                int Goo {
                  set {
                    a->$$
            """,
            "value");

    [Fact]
    public Task ValueNotAfterColonColon()
        => VerifyItemIsAbsentAsync(
            """
            class C {
                int Goo {
                  set {
                    a::$$
            """,
            "value");

    [Fact]
    public Task ValueNotInGetter()
        => VerifyItemIsAbsentAsync(
            """
            class C {
                int Goo {
                  get {
                    $$
            """,
            "value");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
    public Task NotAfterNullableType()
        => VerifyItemIsAbsentAsync(
            """
            class C {
                void M() {
                    int goo = 0;
                    C? $$
            """,
            "goo");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
    public Task NotAfterNullableTypeAlias()
        => VerifyItemIsAbsentAsync(
            """
            using A = System.Int32;
            class C {
                void M() {
                    int goo = 0;
                    A? $$
            """,
            "goo");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
    public Task NotAfterNullableTypeAndPartialIdentifier()
        => VerifyItemIsAbsentAsync(
            """
            class C {
                void M() {
                    int goo = 0;
                    C? f$$
            """,
            "goo");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
    public Task AfterQuestionMarkInConditional()
        => VerifyItemExistsAsync(
            """
            class C {
                void M() {
                    bool b = false;
                    int goo = 0;
                    b? $$
            """,
            "goo");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
    public Task AfterQuestionMarkAndPartialIdentifierInConditional()
        => VerifyItemExistsAsync(
            """
            class C {
                void M() {
                    bool b = false;
                    int goo = 0;
                    b? f$$
            """,
            "goo");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
    public Task NotAfterPointerType()
        => VerifyItemIsAbsentAsync(
            """
            class C {
                void M() {
                    int goo = 0;
                    C* $$
            """,
            "goo");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
    public Task NotAfterPointerTypeAlias()
        => VerifyItemIsAbsentAsync(
            """
            using A = System.Int32;
            class C {
                void M() {
                    int goo = 0;
                    A* $$
            """,
            "goo");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
    public Task NotAfterPointerTypeAndPartialIdentifier()
        => VerifyItemIsAbsentAsync(
            """
            class C {
                void M() {
                    int goo = 0;
                    C* f$$
            """,
            "goo");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
    public Task AfterAsteriskInMultiplication()
        => VerifyItemExistsAsync(
            """
            class C {
                void M() {
                    int i = 0;
                    int goo = 0;
                    i* $$
            """,
            "goo");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
    public Task AfterAsteriskAndPartialIdentifierInMultiplication()
        => VerifyItemExistsAsync(
            """
            class C {
                void M() {
                    int i = 0;
                    int goo = 0;
                    i* f$$
            """,
            "goo");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543868")]
    public Task AfterEventFieldDeclaredInSameType()
        => VerifyItemExistsAsync(
            """
            class C {
                public event System.EventHandler E;
                void M() {
                    E.$$
            """,
            "Invoke");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543868")]
    public Task NotAfterFullEventDeclaredInSameType()
        => VerifyItemIsAbsentAsync(
            """
            class C {
                    public event System.EventHandler E { add { } remove { } }
                void M() {
                    E.$$
            """,
            "Invoke");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543868")]
    public Task NotAfterEventDeclaredInDifferentType()
        => VerifyItemIsAbsentAsync(
            """
            class C {
                void M() {
                    System.Console.CancelKeyPress.$$
            """,
            "Invoke");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
    public Task NotInObjectInitializerMemberContext()
        => VerifyItemIsAbsentAsync("""
            class C
            {
                public int x, y;
                void M()
                {
                    var c = new C { x = 2, y = 3, $$
            """,
"x");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
    public Task AfterPointerMemberAccess()
        => VerifyItemExistsAsync("""
            struct MyStruct
            {
                public int MyField;
            }

            class Program
            {
                static unsafe void Main(string[] args)
                {
                    MyStruct s = new MyStruct();
                    MyStruct* ptr = &s;
                    ptr->$$
                }}
            """,
"MyField");

    // After @ both X and XAttribute are legal. We think this is an edge case in the language and
    // are not fixing the bug 11931. This test captures that XAttribute doesn't show up indeed.
    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem(11931, "DevDiv_Projects/Roslyn")]
    public async Task VerbatimAttributes()
    {
        var code = """
            using System;
            public class X : Attribute
            { }

            public class XAttribute : Attribute
            { }


            [@X$$]
            class Class3 { }
            """;
        await VerifyItemExistsAsync(code, "X");
        await Assert.ThrowsAsync<Xunit.Sdk.TrueException>(() => VerifyItemExistsAsync(code, "XAttribute"));
    }

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544928")]
    public Task InForLoopIncrementor1()
        => VerifyItemExistsAsync("""
            using System;

            class Program
            {
                static void Main()
                {
                    for (; ; $$
                }
            }
            """, "Console");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544928")]
    public Task InForLoopIncrementor2()
        => VerifyItemExistsAsync("""
            using System;

            class Program
            {
                static void Main()
                {
                    for (; ; Console.WriteLine(), $$
                }
            }
            """, "Console");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544931")]
    public Task InForLoopInitializer1()
        => VerifyItemExistsAsync("""
            using System;

            class Program
            {
                static void Main()
                {
                    for ($$
                }
            }
            """, "Console");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544931")]
    public Task InForLoopInitializer2()
        => VerifyItemExistsAsync("""
            using System;

            class Program
            {
                static void Main()
                {
                    for (Console.WriteLine(), $$
                }
            }
            """, "Console");

    [Fact, WorkItem(10572, "DevDiv_Projects/Roslyn")]
    public Task LocalVariableInItsDeclaration()
        => VerifyItemExistsAsync("""
            class Program
            {
                void M()
                {
                    int goo = $$
                }
            }
            """, "goo");

    [Fact, WorkItem(10572, "DevDiv_Projects/Roslyn")]
    public Task LocalVariableInItsDeclarator()
        => VerifyItemExistsAsync("""
            class Program
            {
                void M()
                {
                    int goo = 0, int bar = $$, int baz = 0;
                }
            }
            """, "bar");

    [Fact, WorkItem(10572, "DevDiv_Projects/Roslyn")]
    public Task LocalVariableNotBeforeDeclaration()
        => VerifyItemIsAbsentAsync("""
            class Program
            {
                void M()
                {
                    $$
                    int goo = 0;
                }
            }
            """, "goo");

    [Fact, WorkItem(10572, "DevDiv_Projects/Roslyn")]
    public Task LocalVariableNotBeforeDeclarator()
        => VerifyItemIsAbsentAsync("""
            class Program
            {
                void M()
                {
                    int goo = $$, bar = 0;
                }
            }
            """, "bar");

    [Fact, WorkItem(10572, "DevDiv_Projects/Roslyn")]
    public Task LocalVariableAfterDeclarator()
        => VerifyItemExistsAsync("""
            class Program
            {
                void M()
                {
                    int goo = 0, int bar = $$
                }
            }
            """, "goo");

    [Fact, WorkItem(10572, "DevDiv_Projects/Roslyn")]
    public Task LocalVariableAsOutArgumentInInitializerExpression()
        => VerifyItemExistsAsync("""
            class Program
            {
                void M()
                {
                    int goo = Bar(out $$
                }
                int Bar(out int x)
                {
                    x = 3;
                    return 5;
                }
            }
            """, "goo");

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Method_BrowsableStateAlways()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    Goo.$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                public static void Bar() 
                {
                }
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Method_BrowsableStateNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    Goo.$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public static void Bar() 
                {
                }
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Method_BrowsableStateAdvanced()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    Goo.$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
                public static void Bar() 
                {
                }
            }
            """;
        HideAdvancedMembers = false;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = true;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Method_Overloads_BothBrowsableAlways()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    Goo.$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                public static void Bar() 
                {
                }

                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                public static void Bar(int x) 
                {
                }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 2,
            expectedSymbolsMetadataReference: 2,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Method_Overloads_OneBrowsableAlways_OneBrowsableNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    Goo.$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                public static void Bar() 
                {
                }

                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public static void Bar(int x) 
                {
                }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 2,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Method_Overloads_BothBrowsableNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    Goo.$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public static void Bar() 
                {
                }

                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public static void Bar(int x) 
                {
                }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 2,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_ExtensionMethod_BrowsableAlways()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
            }

            public static class GooExtensions
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                public static void Bar(this Goo goo, int x)
                {
                }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_ExtensionMethod_BrowsableNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
            }

            public static class GooExtensions
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public static void Bar(this Goo goo, int x)
                {
                }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_ExtensionMethod_BrowsableAdvanced()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
            }

            public static class GooExtensions
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
                public static void Bar(this Goo goo, int x)
                {
                }
            }
            """;

        HideAdvancedMembers = false;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = true;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_ExtensionMethod_BrowsableMixed()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
            }

            public static class GooExtensions
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                public static void Bar(this Goo goo, int x)
                {
                }

                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public static void Bar(this Goo goo, int x, int y)
                {
                }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 2,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_OverloadExtensionMethodAndMethod_BrowsableAlways()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                public void Bar(int x)
                {
                }
            }

            public static class GooExtensions
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                public static void Bar(this Goo goo, int x, int y)
                {
                }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 2,
            expectedSymbolsMetadataReference: 2,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_OverloadExtensionMethodAndMethod_BrowsableMixed()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Bar(int x)
                {
                }
            }

            public static class GooExtensions
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                public static void Bar(this Goo goo, int x, int y)
                {
                }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 2,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_SameSigExtensionMethodAndMethod_InstanceMethodBrowsableNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Bar(int x)
                {
                }
            }

            public static class GooExtensions
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                public static void Bar(this Goo goo, int x)
                {
                }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 2,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task OverriddenSymbolsFilteredFromCompletionList()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    D d = new D();
                    d.$$
                }
            }
            """;

        var referencedCode = """
            public class B
            {
                public virtual void Goo(int original) 
                {
                }
            }

            public class D : B
            {
                public override void Goo(int derived) 
                {
                }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverClass()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    C c = new C();
                    c.$$
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public class C
            {
                public void Goo() 
                {
                }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverBaseClass()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    D d = new D();
                    d.$$
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public class B
            {
                public void Goo() 
                {
                }
            }

            public class D : B
            {
                public void Goo(int x)
                {
                }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 2,
            expectedSymbolsMetadataReference: 2,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_BrowsableStateNeverMethodsInBaseClass()
    {
        var markup = """
            class Program : B
            {
                void M()
                {
                    $$
                }
            }
            """;

        var referencedCode = """
            public class B
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Goo() 
                {
                }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableAlways()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    var ci = new C<int>();
                    ci.$$
                }
            }
            """;

        var referencedCode = """
            public class C<T>
            {
                public void Goo(T t) { }
                public void Goo(int i) { }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 2,
            expectedSymbolsMetadataReference: 2,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed1()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    var ci = new C<int>();
                    ci.$$
                }
            }
            """;

        var referencedCode = """
            public class C<T>
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Goo(T t) { }
                public void Goo(int i) { }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 2,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed2()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    var ci = new C<int>();
                    ci.$$
                }
            }
            """;

        var referencedCode = """
            public class C<T>
            {
                public void Goo(T t) { }
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Goo(int i) { }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 2,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    var ci = new C<int>();
                    ci.$$
                }
            }
            """;

        var referencedCode = """
            public class C<T>
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Goo(T t) { }
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Goo(int i) { }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 2,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableAlways()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    var cii = new C<int, int>();
                    cii.$$
                }
            }
            """;

        var referencedCode = """
            public class C<T, U>
            {
                public void Goo(T t) { }
                public void Goo(U u) { }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 2,
            expectedSymbolsMetadataReference: 2,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericType2CausingMethodSignatureEquality_BrowsableMixed()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    var cii = new C<int, int>();
                    cii.$$
                }
            }
            """;

        var referencedCode = """
            public class C<T, U>
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Goo(T t) { }
                public void Goo(U u) { }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 2,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    var cii = new C<int, int>();
                    cii.$$
                }
            }
            """;

        var referencedCode = """
            public class C<T, U>
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Goo(T t) { }
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public void Goo(U u) { }
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 2,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Field_BrowsableStateNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public int bar;
            }
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Field_BrowsableStateAlways()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                public int bar;
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Field_BrowsableStateAdvanced()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
                public int bar;
            }
            """;
        HideAdvancedMembers = true;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = false;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/522440")]
    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674611")]
    public async Task EditorBrowsable_Property_BrowsableStateNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public int Bar {get; set;}
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Property_IgnoreBrowsabilityOfGetSetMethods()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                public int Bar {
                    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                    get { return 5; }
                    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                    set { }
                }
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Property_BrowsableStateAlways()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                public int Bar {get; set;}
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Property_BrowsableStateAdvanced()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
                public int Bar {get; set;}
            }
            """;
        HideAdvancedMembers = true;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = false;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Constructor_BrowsableStateNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new $$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public Goo()
                {
                }
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Constructor_BrowsableStateAlways()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new $$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                public Goo()
                {
                }
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Constructor_BrowsableStateAdvanced()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new $$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
                public Goo()
                {
                }
            }
            """;
        HideAdvancedMembers = true;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = false;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Constructor_MixedOverloads1()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new $$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public Goo()
                {
                }

                public Goo(int x)
                {
                }
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Constructor_MixedOverloads2()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new $$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public Goo()
                {
                }

                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public Goo(int x)
                {
                }
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Event_BrowsableStateNever()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new C().$$
                }
            }
            """;

        var referencedCode = """
            public delegate void Handler();

            public class C
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public event Handler Changed;
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Changed",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Event_BrowsableStateAlways()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new C().$$
                }
            }
            """;

        var referencedCode = """
            public delegate void Handler();

            public class C
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
                public event Handler Changed;
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Changed",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Event_BrowsableStateAdvanced()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new C().$$
                }
            }
            """;

        var referencedCode = """
            public delegate void Handler();

            public class C
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
                public event Handler Changed;
            }
            """;

        HideAdvancedMembers = false;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Changed",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = true;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Changed",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Delegate_BrowsableStateNever()
    {
        var markup = """
            class Program
            {
                public event $$
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public delegate void Handler();
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Handler",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Delegate_BrowsableStateAlways()
    {
        var markup = """
            class Program
            {
                public event $$
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
            public delegate void Handler();
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Handler",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Delegate_BrowsableStateAdvanced()
    {
        var markup = """
            class Program
            {
                public event $$
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
            public delegate void Handler();
            """;

        HideAdvancedMembers = false;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Handler",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = true;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Handler",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Class_BrowsableStateNever_DeclareLocal()
    {
        var markup = """
            class Program
            {
                public void M()
                {
                    $$    
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public class Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Class_BrowsableStateNever_DeriveFrom()
    {
        var markup = """
            class Program : $$
            {
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public class Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Class_BrowsableStateNever_FullyQualifiedInUsing()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    using (var x = new NS.$$
                }
            }
            """;

        var referencedCode = """
            namespace NS
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public class Goo : System.IDisposable
                {
                    public void Dispose()
                    {
                    }
                }
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Class_BrowsableStateAlways_DeclareLocal()
    {
        var markup = """
            class Program
            {
                public void M()
                {
                    $$    
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
            public class Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Class_BrowsableStateAlways_DeriveFrom()
    {
        var markup = """
            class Program : $$
            {
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
            public class Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Class_BrowsableStateAlways_FullyQualifiedInUsing()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    using (var x = new NS.$$
                }
            }
            """;

        var referencedCode = """
            namespace NS
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
                public class Goo : System.IDisposable
                {
                    public void Dispose()
                    {
                    }
                }
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Class_BrowsableStateAdvanced_DeclareLocal()
    {
        var markup = """
            class Program
            {
                public void M()
                {
                    $$    
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
            public class Goo
            {
            }
            """;
        HideAdvancedMembers = false;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = true;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Class_BrowsableStateAdvanced_DeriveFrom()
    {
        var markup = """
            class Program : $$
            {
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
            public class Goo
            {
            }
            """;

        HideAdvancedMembers = false;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = true;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Class_BrowsableStateAdvanced_FullyQualifiedInUsing()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    using (var x = new NS.$$
                }
            }
            """;

        var referencedCode = """
            namespace NS
            {
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
                public class Goo : System.IDisposable
                {
                    public void Dispose()
                    {
                    }
                }
            }
            """;
        HideAdvancedMembers = false;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = true;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Class_IgnoreBaseClassBrowsableNever()
    {
        var markup = """
            class Program
            {
                public void M()
                {
                    $$    
                }
            }
            """;

        var referencedCode = """
            public class Goo : Bar
            {
            }

            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public class Bar
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Struct_BrowsableStateNever_DeclareLocal()
    {
        var markup = """
            class Program
            {
                public void M()
                {
                    $$    
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public struct Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Struct_BrowsableStateNever_DeriveFrom()
    {
        var markup = """
            class Program : $$
            {
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public struct Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Struct_BrowsableStateAlways_DeclareLocal()
    {
        var markup = """
            class Program
            {
                public void M()
                {
                    $$
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
            public struct Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Struct_BrowsableStateAlways_DeriveFrom()
    {
        var markup = """
            class Program : $$
            {
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
            public struct Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Struct_BrowsableStateAdvanced_DeclareLocal()
    {
        var markup = """
            class Program
            {
                public void M()
                {
                    $$    
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
            public struct Goo
            {
            }
            """;
        HideAdvancedMembers = false;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = true;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Struct_BrowsableStateAdvanced_DeriveFrom()
    {
        var markup = """
            class Program : $$
            {
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
            public struct Goo
            {
            }
            """;
        HideAdvancedMembers = false;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = true;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Enum_BrowsableStateNever()
    {
        var markup = """
            class Program
            {
                public void M()
                {
                    $$
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public enum Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Enum_BrowsableStateAlways()
    {
        var markup = """
            class Program
            {
                public void M()
                {
                    $$
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
            public enum Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Enum_BrowsableStateAdvanced()
    {
        var markup = """
            class Program
            {
                public void M()
                {
                    $$
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
            public enum Goo
            {
            }
            """;
        HideAdvancedMembers = false;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = true;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Interface_BrowsableStateNever_DeclareLocal()
    {
        var markup = """
            class Program
            {
                public void M()
                {
                    $$    
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public interface Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Interface_BrowsableStateNever_DeriveFrom()
    {
        var markup = """
            class Program : $$
            {
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public interface Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Interface_BrowsableStateAlways_DeclareLocal()
    {
        var markup = """
            class Program
            {
                public void M()
                {
                    $$
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
            public interface Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Interface_BrowsableStateAlways_DeriveFrom()
    {
        var markup = """
            class Program : $$
            {
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
            public interface Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Interface_BrowsableStateAdvanced_DeclareLocal()
    {
        var markup = """
            class Program
            {
                public void M()
                {
                    $$    
                }
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
            public interface Goo
            {
            }
            """;
        HideAdvancedMembers = false;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = true;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_Interface_BrowsableStateAdvanced_DeriveFrom()
    {
        var markup = """
            class Program : $$
            {
            }
            """;

        var referencedCode = """
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
            public interface Goo
            {
            }
            """;
        HideAdvancedMembers = false;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);

        HideAdvancedMembers = true;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_CrossLanguage_CStoVB_Always()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    $$
                }
            }
            """;

        var referencedCode = """
            <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
            Public Class Goo
            End Class
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.VisualBasic);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_CrossLanguage_CStoVB_Never()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    $$
                }
            }
            """;

        var referencedCode = """
            <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
            Public Class Goo
            End Class
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 0,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.VisualBasic);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_TypeLibType_NotHidden()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new $$
                }
            }
            """;

        var referencedCode = """
            [System.Runtime.InteropServices.TypeLibType(System.Runtime.InteropServices.TypeLibTypeFlags.FLicensed)]
            public class Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_TypeLibType_Hidden()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new $$
                }
            }
            """;

        var referencedCode = """
            [System.Runtime.InteropServices.TypeLibType(System.Runtime.InteropServices.TypeLibTypeFlags.FHidden)]
            public class Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_TypeLibType_HiddenAndOtherFlags()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new $$
                }
            }
            """;

        var referencedCode = """
            [System.Runtime.InteropServices.TypeLibType(System.Runtime.InteropServices.TypeLibTypeFlags.FHidden | System.Runtime.InteropServices.TypeLibTypeFlags.FLicensed)]
            public class Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_TypeLibType_NotHidden_Int16Constructor()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new $$
                }
            }
            """;

        var referencedCode = """
            [System.Runtime.InteropServices.TypeLibType((short)System.Runtime.InteropServices.TypeLibTypeFlags.FLicensed)]
            public class Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_TypeLibType_Hidden_Int16Constructor()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new $$
                }
            }
            """;

        var referencedCode = """
            [System.Runtime.InteropServices.TypeLibType((short)System.Runtime.InteropServices.TypeLibTypeFlags.FHidden)]
            public class Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_TypeLibType_HiddenAndOtherFlags_Int16Constructor()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new $$
                }
            }
            """;

        var referencedCode = """
            [System.Runtime.InteropServices.TypeLibType((short)(System.Runtime.InteropServices.TypeLibTypeFlags.FHidden | System.Runtime.InteropServices.TypeLibTypeFlags.FLicensed))]
            public class Goo
            {
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Goo",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_TypeLibFunc_NotHidden()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.Runtime.InteropServices.TypeLibFunc(System.Runtime.InteropServices.TypeLibFuncFlags.FReplaceable)]
                public void Bar()
                {
                }
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_TypeLibFunc_Hidden()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.Runtime.InteropServices.TypeLibFunc(System.Runtime.InteropServices.TypeLibFuncFlags.FHidden)]
                public void Bar()
                {
                }
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_TypeLibFunc_HiddenAndOtherFlags()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.Runtime.InteropServices.TypeLibFunc(System.Runtime.InteropServices.TypeLibFuncFlags.FHidden | System.Runtime.InteropServices.TypeLibFuncFlags.FReplaceable)]
                public void Bar()
                {
                }
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_TypeLibFunc_NotHidden_Int16Constructor()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.Runtime.InteropServices.TypeLibFunc((short)System.Runtime.InteropServices.TypeLibFuncFlags.FReplaceable)]
                public void Bar()
                {
                }
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_TypeLibFunc_Hidden_Int16Constructor()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.Runtime.InteropServices.TypeLibFunc((short)System.Runtime.InteropServices.TypeLibFuncFlags.FHidden)]
                public void Bar()
                {
                }
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_TypeLibFunc_HiddenAndOtherFlags_Int16Constructor()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.Runtime.InteropServices.TypeLibFunc((short)(System.Runtime.InteropServices.TypeLibFuncFlags.FHidden | System.Runtime.InteropServices.TypeLibFuncFlags.FReplaceable))]
                public void Bar()
                {
                }
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "Bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_TypeLibVar_NotHidden()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.Runtime.InteropServices.TypeLibVar(System.Runtime.InteropServices.TypeLibVarFlags.FReplaceable)]
                public int bar;
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_TypeLibVar_Hidden()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.Runtime.InteropServices.TypeLibVar(System.Runtime.InteropServices.TypeLibVarFlags.FHidden)]
                public int bar;
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_TypeLibVar_HiddenAndOtherFlags()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.Runtime.InteropServices.TypeLibVar(System.Runtime.InteropServices.TypeLibVarFlags.FHidden | System.Runtime.InteropServices.TypeLibVarFlags.FReplaceable)]
                public int bar;
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_TypeLibVar_NotHidden_Int16Constructor()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.Runtime.InteropServices.TypeLibVar((short)System.Runtime.InteropServices.TypeLibVarFlags.FReplaceable)]
                public int bar;
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_TypeLibVar_Hidden_Int16Constructor()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.Runtime.InteropServices.TypeLibVar((short)System.Runtime.InteropServices.TypeLibVarFlags.FHidden)]
                public int bar;
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
    public async Task EditorBrowsable_TypeLibVar_HiddenAndOtherFlags_Int16Constructor()
    {
        var markup = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var referencedCode = """
            public class Goo
            {
                [System.Runtime.InteropServices.TypeLibVar((short)(System.Runtime.InteropServices.TypeLibVarFlags.FHidden | System.Runtime.InteropServices.TypeLibVarFlags.FReplaceable))]
                public int bar;
            }
            """;
        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "bar",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 0,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.CSharp);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545557")]
    public Task TestColorColor1()
        => VerifyExpectedItemsAsync("""
            class A
            {
                static void Goo() { }
                void Bar() { }

                static void Main()
                {
                    A A = new A();
                    A.$$
                }
            }
            """, [
            ItemExpectation.Exists("Goo"),
            ItemExpectation.Exists("Bar"),
        ]);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545647")]
    public Task TestLaterLocalHidesType1()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                public static void Main()
                {
                    $$
                    Console.WriteLine();
                }
            }
            """, "Console");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545647")]
    public Task TestLaterLocalHidesType2()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                public static void Main()
                {
                    C$$
                    Console.WriteLine();
                }
            }
            """, "Console");

    [Fact]
    public async Task TestIndexedProperty()
    {
        var markup = """
            class Program
            {
                void M()
                {
                        CCC c = new CCC();
                        c.$$
                }
            }
            """;

        // Note that <COMImport> is required by compiler.  Bug 17013 tracks enabling indexed property for non-COM types.
        var referencedCode = """
            Imports System.Runtime.InteropServices

            <ComImport()>
            <GuidAttribute(CCC.ClassId)>
            Public Class CCC

            #Region "COM GUIDs"
                Public Const ClassId As String = "9d965fd2-1514-44f6-accd-257ce77c46b0"
                Public Const InterfaceId As String = "a9415060-fdf0-47e3-bc80-9c18f7f39cf6"
                Public Const EventsId As String = "c6a866a5-5f97-4b53-a5df-3739dc8ff1bb"
            # End Region

                        ''' <summary>
                ''' An index property from VB
                ''' </summary>
                ''' <param name="p1">p1 is an integer index</param>
                ''' <returns>A string</returns>
                Public Property IndexProp(ByVal p1 As Integer, Optional ByVal p2 As Integer = 0) As String
                    Get
                        Return Nothing
                    End Get
                    Set(ByVal value As String)

                    End Set
                End Property
            End Class
            """;

        await VerifyItemInEditorBrowsableContextsAsync(
            markup: markup,
            referencedCode: referencedCode,
            item: "IndexProp",
            expectedSymbolsSameSolution: 1,
            expectedSymbolsMetadataReference: 1,
            sourceLanguage: LanguageNames.CSharp,
            referencedLanguage: LanguageNames.VisualBasic);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546841")]
    public Task TestDeclarationAmbiguity()
        => VerifyItemExistsAsync("""
            using System;

            class Program
            {
                void Main()
                {
                    Environment.$$
                    var v;
                }
            }
            """, "CommandLine");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12781")]
    public Task TestFieldDeclarationAmbiguity()
        => VerifyItemExistsAsync("""
            using System;
            Environment.$$
            var v;
            }
            """, "CommandLine", sourceCodeKind: SourceCodeKind.Script);

    [Fact]
    public Task TestCursorOnClassCloseBrace()
        => VerifyItemExistsAsync("""
            using System;

            class Outer
            {
                class Inner { }

            $$}
            """, "Inner");

    [Fact]
    public Task AfterAsync1()
        => VerifyItemExistsAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async $$
            }
            """, "Task");

    [Fact]
    public Task AfterAsync2()
        => VerifyItemExistsAsync("""
            using System.Threading.Tasks;
            class Program
            {
                public async T$$
            }
            """, "Task");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
    public Task AfterAsync3()
        => VerifyItemExistsAsync("""
            using System.Threading.Tasks;
            class Program
            {
                public async $$

                public void M() {}
            }
            """, "Task");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
    public Task AfterAsync4()
        => VerifyExpectedItemsAsync("""
            using System;
            using System.Threading.Tasks;
            class Program
            {
                public async $$
            }
            """, [
            ItemExpectation.Exists("Task"),
            ItemExpectation.Absent("Console"),
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
    public Task AfterAsync5()
        => VerifyExpectedItemsAsync("""
            using System.Threading.Tasks;
            class Program
            {
                public async $$
            }

            class Test {}
            """, [
            ItemExpectation.Exists("Task"),
            ItemExpectation.Absent("Test"),
        ]);

    [Fact]
    public Task NotAfterAsyncInMethodBody()
        => VerifyItemIsAbsentAsync("""
            using System.Threading.Tasks;
            class Program
            {
                void goo()
                {
                    var x = async $$
                }
            }
            """, "Task");

    [Fact]
    public Task NotAwaitable1()
        => VerifyItemWithMscorlib45Async("""
            class Program
            {
                void goo()
                {
                    $$
                }
            }
            """, "goo", "void Program.goo()", "C#");

    [Fact]
    public Task NotAwaitable2()
        => VerifyItemWithMscorlib45Async("""
            class Program
            {
                async void goo()
                {
                    $$
                }
            }
            """, "goo", "void Program.goo()", "C#");

    [Fact]
    public Task Awaitable1()
        => VerifyItemWithMscorlib45Async("""
            using System.Threading;
            using System.Threading.Tasks;

            class Program
            {
                async Task goo()
                {
                    $$
                }
            }
            """, "goo", $@"({CSharpFeaturesResources.awaitable}) Task Program.goo()", "C#");

    [Fact]
    public Task Awaitable2()
        => VerifyItemWithMscorlib45Async("""
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> goo()
                {
                    $$
                }
            }
            """, "goo", $@"({CSharpFeaturesResources.awaitable}) Task<int> Program.goo()", "C#");

    [Fact]
    public Task AwaitableDotsLikeRangeExpression()
        => VerifyItemExistsAsync("""
            using System.IO;
            using System.Threading.Tasks;

            namespace N
            {
                class C
                {
                    async Task M()
                    {
                        var request = new Request();
                        var m = await request.$$.ReadAsStreamAsync();
                    }
                }

                class Request
                {
                    public Task<Stream> ReadAsStreamAsync() => null;
                }
            }
            """, "ReadAsStreamAsync");

    [Fact]
    public Task AwaitableDotsLikeRangeExpressionWithParentheses()
        => VerifyExpectedItemsAsync("""
            using System.IO;
            using System.Threading.Tasks;

            namespace N
            {
                class C
                {
                    async Task M()
                    {
                        var request = new Request();
                        var m = (await request).$$.ReadAsStreamAsync();
                    }
                }

                class Request
                {
                    public Task<Stream> ReadAsStreamAsync() => null;
                }
            }
            """, [
            ItemExpectation.Absent("Result"),
            ItemExpectation.Absent("ReadAsStreamAsync"),
        ]);

    [Fact]
    public Task AwaitableDotsLikeRangeExpressionWithTaskAndParentheses()
        => VerifyExpectedItemsAsync("""
            using System.IO;
            using System.Threading.Tasks;

            namespace N
            {
                class C
                {
                    async Task M()
                    {
                        var request = new Task<Request>();
                        var m = (await request).$$.ReadAsStreamAsync();
                    }
                }

                class Request
                {
                    public Task<Stream> ReadAsStreamAsync() => null;
                }
            }
            """, [
            ItemExpectation.Absent("Result"),
            ItemExpectation.Exists("ReadAsStreamAsync"),
        ]);

    [Fact]
    public Task ObsoleteItem()
        => VerifyItemExistsAsync("""
            using System;

            class Program
            {
                [Obsolete]
                public void goo()
                {
                    $$
                }
            }
            """, "goo", $"[{CSharpFeaturesResources.deprecated}] void Program.goo()");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568986")]
    public Task NoMembersOnDottingIntoUnboundType()
        => VerifyNoItemsExistAsync("""
            class Program
            {
                RegistryKey goo;

                static void Main(string[] args)
                {
                    goo.$$
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/550717")]
    public Task TypeArgumentsInConstraintAfterBaselist()
        => VerifyItemExistsAsync("""
            public class Goo<T> : System.Object where $$
            {
            }
            """, "T");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/647175")]
    public Task NoDestructor()
        => VerifyItemIsAbsentAsync("""
            class C
            {
                ~C()
                {
                    $$
            """, "Finalize");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/669624")]
    public Task ExtensionMethodOnCovariantInterface()
        => VerifyItemExistsAsync("""
            class Schema<T> { }

            interface ISet<out T> { }

            static class SetMethods
            {
                public static void ForSchemaSet<T>(this ISet<Schema<T>> set) { }
            }

            class Context
            {
                public ISet<T> Set<T>() { return null; }
            }

            class CustomSchema : Schema<int> { }

            class Program
            {
                static void Main(string[] args)
                {
                    var set = new Context().Set<CustomSchema>();

                    set.$$
            """, "ForSchemaSet", displayTextSuffix: "<>", sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667752")]
    public Task ForEachInsideParentheses()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                void M()
                {
                    foreach($$)
            """, "String");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/766869")]
    public Task TestFieldInitializerInP2P()
        => VerifyItemWithProjectReferenceAsync("""
            class Class
            {
                int i = Consts.$$;
            }
            """, """
            public static class Consts
            {
                public const int C = 1;
            }
            """, "C", 1, LanguageNames.CSharp, LanguageNames.CSharp);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/834605")]
    public Task ShowWithEqualsSign()
        => VerifyNoItemsExistAsync("""
            class c { public int value {set; get; }}

            class d
            {
                void goo()
                {
                   c goo = new c { value$$=
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/825661")]
    public Task NothingAfterThisDotInStaticContext()
        => VerifyNoItemsExistAsync("""
            class C
            {
                void M1() { }

                static void M2()
                {
                    this.$$
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/825661")]
    public Task NothingAfterBaseDotInStaticContext()
        => VerifyNoItemsExistAsync("""
            class C
            {
                void M1() { }

                static void M2()
                {
                    base.$$
                }
            }
            """);

    [Fact, WorkItem("http://github.com/dotnet/roslyn/issues/7648")]
    public async Task NothingAfterBaseDotInScriptContext()
        => await VerifyItemIsAbsentAsync(@"base.$$", @"ToString", sourceCodeKind: SourceCodeKind.Script);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858086")]
    public Task NoNestedTypeWhenDisplayingInstance()
        => VerifyItemIsAbsentAsync("""
            class C
            {
                class D
                {
                }

                void M2()
                {
                    new C().$$
                }
            }
            """, "D");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/876031")]
    public Task CatchVariableInExceptionFilter()
        => VerifyItemExistsAsync("""
            class C
            {
                void M()
                {
                    try
                    {
                    }
                    catch (System.Exception myExn) when ($$
            """, "myExn");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849698")]
    public Task CompletionAfterExternAlias()
        => VerifyItemExistsAsync("""
            class C
            {
                void goo()
                {
                    global::$$
                }
            }
            """, "System", usePreviousCharAsTrigger: true);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849698")]
    public Task ExternAliasSuggested()
        => VerifyItemWithAliasedMetadataReferencesAsync("""
            extern alias Bar;
            class C
            {
                void goo()
                {
                    $$
                }
            }
            """, "Bar", "Bar", 1, "C#", "C#");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/635957")]
    public async Task ClassDestructor()
    {
        var markup = """
            class C
            {
                class N
                {
                ~$$
                }
            }
            """;
        await VerifyItemExistsAsync(markup, "N");
        await VerifyItemIsAbsentAsync(markup, "C");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/635957")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    public async Task TildeOutsideClass()
    {
        var markup = """
            class C
            {
                class N
                {
                }
            }
            ~$$
            """;
        await VerifyItemExistsAsync(markup, "C");
        await VerifyItemIsAbsentAsync(markup, "N");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/635957")]
    public Task StructDestructor()
        => VerifyItemIsAbsentAsync("""
            struct C
            {
               ~$$
            }
            """, "C");

    [Theory]
    [InlineData("record")]
    [InlineData("record class")]
    public Task RecordDestructor(string record)
        => VerifyItemExistsAsync($$"""
            {{record}} C
            {
               ~$$
            }
            """, "C");

    [Fact]
    public Task RecordStructDestructor()
        => VerifyItemIsAbsentAsync($$"""
            record struct C
            {
               ~$$
            }
            """, "C");

    [Fact]
    public Task FieldAvailableInBothLinkedFiles()
        => VerifyItemInLinkedFilesAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            class C
            {
                int x;
                void goo()
                {
                    $$
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
            </Workspace>
            """, "x", $"({FeaturesResources.field}) int C.x");

    [Fact]
    public Task FieldUnavailableInOneLinkedFile()
        => VerifyItemInLinkedFilesAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            class C
            {
            #if GOO
                int x;
            #endif
                void goo()
                {
                    $$
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
            </Workspace>
            """, "x", $"""
            ({FeaturesResources.field}) int C.x

                {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}
                {string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}

            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
            """);

    [Fact]
    public Task FieldUnavailableInTwoLinkedFiles()
        => VerifyItemInLinkedFilesAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            class C
            {
            #if GOO
                int x;
            #endif
                void goo()
                {
                    $$
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj3">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
            </Workspace>
            """, "x", $"""
            ({FeaturesResources.field}) int C.x

                {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}
                {string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}
                {string.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}

            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
            """);

    [Fact]
    public Task ExcludeFilesWithInactiveRegions()
        => VerifyItemInLinkedFilesAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO,BAR">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            class C
            {
            #if GOO
                int x;
            #endif

            #if BAR
                void goo()
                {
                    $$
                }
            #endif
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs" />
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj3" PreprocessorSymbols="BAR">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
            </Workspace>
            """, "x", $"""
            ({FeaturesResources.field}) int C.x

                {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}
                {string.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}

            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
            """);

    [Fact]
    public Task UnionOfItemsFromBothContexts()
        => VerifyItemInLinkedFilesAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            class C
            {
            #if GOO
                int x;
            #endif

            #if BAR
                class G
                {
                    public void DoGStuff() {}
                }
            #endif
                void goo()
                {
                    new G().$$
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2" PreprocessorSymbols="BAR">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj3">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
            </Workspace>
            """, "DoGStuff", $"""
            void G.DoGStuff()

                {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Not_Available)}
                {string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Available)}
                {string.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}

            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020944")]
    public Task LocalsValidInLinkedDocuments()
        => VerifyItemInLinkedFilesAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            class C
            {
                void M()
                {
                    int xyz;
                    $$
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
            </Workspace>
            """, "xyz", $"({FeaturesResources.local_variable}) int xyz");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020944")]
    public Task LocalWarningInLinkedDocuments()
        => VerifyItemInLinkedFilesAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="PROJ1">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            class C
            {
                void M()
                {
            #if PROJ1
                    int xyz;
            #endif
                    $$
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
            </Workspace>
            """, "xyz", $"""
            ({FeaturesResources.local_variable}) int xyz

                {string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}
                {string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}

            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020944")]
    public Task LabelsValidInLinkedDocuments()
        => VerifyItemInLinkedFilesAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            class C
            {
                void M()
                {
            LABEL:  int xyz;
                    goto $$
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
            </Workspace>
            """, "LABEL", $"({FeaturesResources.label}) LABEL");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020944")]
    public Task RangeVariablesValidInLinkedDocuments()
        => VerifyItemInLinkedFilesAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            using System.Linq;
            class C
            {
                void M()
                {
                    var x = from y in new[] { 1, 2, 3 } select $$
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
            </Workspace>
            """, "y", $"({FeaturesResources.range_variable}) ? y");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1063403")]
    public Task MethodOverloadDifferencesIgnored()
        => VerifyItemInLinkedFilesAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="ONE">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            class C
            {
            #if ONE
                void Do(int x){}
            #endif
            #if TWO
                void Do(string x){}
            #endif

                void Shared()
                {
                    $$
                }

            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2" PreprocessorSymbols="TWO">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
            </Workspace>
            """, "Do", $"void C.Do(int x)");

    [Fact]
    public Task MethodOverloadDifferencesIgnored_ExtensionMethod()
        => VerifyItemInLinkedFilesAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="ONE">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            class C
            {
            #if ONE
                void Do(int x){}
            #endif

                void Shared()
                {
                    this.$$
                }

            }

            public static class Extensions
            {
            #if TWO
                public static void Do (this C c, string x)
                {
                }
            #endif
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2" PreprocessorSymbols="TWO">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
            </Workspace>
            """, "Do", $"void C.Do(int x)");

    [Fact]
    public Task MethodOverloadDifferencesIgnored_ExtensionMethod2()
        => VerifyItemInLinkedFilesAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="TWO">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            class C
            {
            #if ONE
                void Do(int x){}
            #endif

                void Shared()
                {
                    this.$$
                }

            }

            public static class Extensions
            {
            #if TWO
                public static void Do (this C c, string x)
                {
                }
            #endif
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2" PreprocessorSymbols="ONE">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
            </Workspace>
            """, "Do", $"({CSharpFeaturesResources.extension}) void C.Do(string x)");

    [Fact]
    public Task MethodOverloadDifferencesIgnored_ContainingType()
        => VerifyItemInLinkedFilesAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="ONE">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            class C
            {
                void Shared()
                {
                    var x = GetThing();
                    x.$$
                }

            #if ONE
                private Methods1 GetThing()
                {
                    return new Methods1();
                }
            #endif

            #if TWO
                private Methods2 GetThing()
                {
                    return new Methods2();
                }
            #endif
            }

            #if ONE
            public class Methods1
            {
                public void Do(string x) { }
            }
            #endif

            #if TWO
            public class Methods2
            {
                public void Do(string x) { }
            }
            #endif
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2" PreprocessorSymbols="TWO">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
            </Workspace>
            """, "Do", $"void Methods1.Do(string x)");

    [Fact]
    public Task SharedProjectFieldAndPropertiesTreatedAsIdentical()
        => VerifyItemInLinkedFilesAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="ONE">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            class C
            {
            #if ONE
                public int x;
            #endif
            #if TWO
                public int x {get; set;}
            #endif
                void goo()
                {
                    x$$
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2" PreprocessorSymbols="TWO">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
            </Workspace>
            """, "x", $"({FeaturesResources.field}) int C.x");

    [Fact]
    public Task SharedProjectFieldAndPropertiesTreatedAsIdentical2()
        => VerifyItemInLinkedFilesAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="ONE">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            class C
            {
            #if TWO
                public int x;
            #endif
            #if ONE
                public int x {get; set;}
            #endif
                void goo()
                {
                    x$$
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2" PreprocessorSymbols="TWO">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                </Project>
            </Workspace>
            """, "x", "int C.x { get; set; }");

    [Fact]
    public Task ConditionalAccessWalkUp()
        => VerifyExpectedItemsAsync("""
            public class B
            {
                public A BA;
                public B BB;
            }

            class A
            {
                public A AA;
                public A AB;
                public int? x;

                public void goo()
                {
                    A a = null;
                    var q = a?.$$AB.BA.AB.BA;
                }
            }
            """, [
            ItemExpectation.Exists("AA"),
            ItemExpectation.Exists("AB"),
        ]);

    [Fact]
    public Task ConditionalAccessNullableIsUnwrapped()
        => VerifyExpectedItemsAsync("""
            public struct S
            {
                public int? i;
            }

            class A
            {
                public S? s;

                public void goo()
                {
                    A a = null;
                    var q = a?.s?.$$;
                }
            }
            """, [
            ItemExpectation.Exists("i"),
            ItemExpectation.Absent("Value"),
        ]);

    [Fact]
    public Task ConditionalAccessNullableIsUnwrapped2()
        => VerifyExpectedItemsAsync("""
            public struct S
            {
                public int? i;
            }

            class A
            {
                public S? s;

                public void goo()
                {
                    var q = s?.$$i?.ToString();
                }
            }
            """, [
            ItemExpectation.Exists("i"),
            ItemExpectation.Absent("Value"),
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/54361")]
    public Task ConditionalAccessNullableIsUnwrappedOnParameter()
        => VerifyExpectedItemsAsync("""
            class A
            {
                void M(System.DateTime? dt)
                {
                    dt?.$$
                }
            }
            """, [
            ItemExpectation.Exists("Day"),
            ItemExpectation.Absent("Value"),
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/54361")]
    public Task NullableIsNotUnwrappedOnParameter()
        => VerifyExpectedItemsAsync("""
            class A
            {
                void M(System.DateTime? dt)
                {
                    dt.$$
                }
            }
            """, [
            ItemExpectation.Exists("Value"),
            ItemExpectation.Absent("Day"),
        ]);

    [Fact]
    public Task CompletionAfterConditionalIndexing()
        => VerifyItemExistsAsync("""
            public struct S
            {
                public int? i;
            }

            class A
            {
                public S[] s;

                public void goo()
                {
                    A a = null;
                    var q = a?.s?[$$;
                }
            }
            """, "System");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109319")]
    public Task WithinChainOfConditionalAccesses1()
        => VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    A a;
                    var x = a?.$$b?.c?.d.e;
                }
            }

            class A { public B b; }
            class B { public C c; }
            class C { public D d; }
            class D { public int e; }
            """, "b");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109319")]
    public Task WithinChainOfConditionalAccesses2()
        => VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    A a;
                    var x = a?.b?.$$c?.d.e;
                }
            }

            class A { public B b; }
            class B { public C c; }
            class C { public D d; }
            class D { public int e; }
            """, "c");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109319")]
    public Task WithinChainOfConditionalAccesses3()
        => VerifyItemExistsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    A a;
                    var x = a?.b?.c?.$$d.e;
                }
            }

            class A { public B b; }
            class B { public C c; }
            class C { public D d; }
            class D { public int e; }
            """, "d");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/843466")]
    public Task NestedAttributeAccessibleOnSelf()
        => VerifyItemExistsAsync("""
            using System;
            [My]
            class X
            {
                [My$$]
                class MyAttribute : Attribute
                {

                }
            }
            """, "My");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/843466")]
    public Task NestedAttributeAccessibleOnOuterType()
        => VerifyItemExistsAsync("""
            using System;

            [My]
            class Y
            {

            }

            [$$]
            class X
            {
                [My]
                class MyAttribute : Attribute
                {

                }
            }
            """, "My");

    [Fact]
    public Task InstanceMembersFromBaseOuterType()
        => VerifyItemExistsAsync("""
            abstract class Test
            {
              private int _field;

              public sealed class InnerTest : Test 
              {

                public void SomeTest() 
                {
                    $$
                }
              }
            }
            """, "_field");

    [Fact]
    public Task InstanceMembersFromBaseOuterType2()
        => VerifyItemExistsAsync("""
            class C<T>
            {
                void M() { }
                class N : C<int>
                {
                    void Test()
                    {
                        $$ // M recommended and accessible
                    }

                    class NN
                    {
                        void Test2()
                        {
                            // M inaccessible and not recommended
                        }
                    }
                }
            }
            """, "M");

    [Fact]
    public Task InstanceMembersFromBaseOuterType3()
        => VerifyItemIsAbsentAsync("""
            class C<T>
            {
                void M() { }
                class N : C<int>
                {
                    void Test()
                    {
                        M(); // M recommended and accessible
                    }

                    class NN
                    {
                        void Test2()
                        {
                            $$ // M inaccessible and not recommended
                        }
                    }
                }
            }
            """, "M");

    [Fact]
    public Task InstanceMembersFromBaseOuterType4()
        => VerifyItemExistsAsync("""
            class C<T>
            {
                void M() { }
                class N : C<int>
                {
                    void Test()
                    {
                        M(); // M recommended and accessible
                    }

                    class NN : N
                    {
                        void Test2()
                        {
                            $$ // M accessible and recommended.
                        }
                    }
                }
            }
            """, "M");

    [Fact]
    public Task InstanceMembersFromBaseOuterType5()
        => VerifyItemIsAbsentAsync("""
            class D
            {
                public void Q() { }
            }
            class C<T> : D
            {
                class N
                {
                    void Test()
                    {
                        $$
                    }
                }
            }
            """, "Q");

    [Fact]
    public Task InstanceMembersFromBaseOuterType6()
        => VerifyItemIsAbsentAsync("""
            class Base<T>
            {
                public int X;
            }

            class Derived : Base<int>
            {
                class Nested
                {
                    void Test()
                    {
                        $$
                    }
                }
            }
            """, "X");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/983367")]
    public Task NoTypeParametersDefinedInCrefs()
        => VerifyItemIsAbsentAsync("""
            using System;

            /// <see cref="Program{T$$}"/>
            class Program<T> { }
            """, "T");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/988025")]
    public Task ShowTypesInGenericMethodTypeParameterList1()
        => VerifyItemExistsAsync("""
            class Class1<T, D>
            {
                public static Class1<T, D> Create() { return null; }
            }
            static class Class2
            {
                public static void Test<T,D>(this Class1<T, D> arg)
                {
                }
            }
            class Program
            {
                static void Main(string[] args)
                {
                    Class1<string, int>.Create().Test<$$
                }
            }
            """, "Class1", displayTextSuffix: "<>", sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/988025")]
    public Task ShowTypesInGenericMethodTypeParameterList2()
        => VerifyItemExistsAsync("""
            class Class1<T, D>
            {
                public static Class1<T, D> Create() { return null; }
            }
            static class Class2
            {
                public static void Test<T,D>(this Class1<T, D> arg)
                {
                }
            }
            class Program
            {
                static void Main(string[] args)
                {
                    Class1<string, int>.Create().Test<string,$$
                }
            }
            """, "Class1", displayTextSuffix: "<>", sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991466")]
    public Task DescriptionInAliasedType()
        => VerifyItemExistsAsync("""
            using IAlias = IGoo;
            ///<summary>summary for interface IGoo</summary>
            interface IGoo {  }
            class C 
            { 
                I$$
            }
            """, "IAlias", expectedDescriptionOrNull: """
            interface IGoo
            summary for interface IGoo
            """);

    [Fact]
    public Task WithinNameOf()
        => VerifyAnyItemExistsAsync("""
            class C 
            { 
                void goo()
                {
                    var x = nameof($$)
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/997410")]
    public Task InstanceMemberInNameOfInStaticContext()
        => VerifyItemExistsAsync("""
            class C
            {
              int y1 = 15;
              static int y2 = 1;
              static string x = nameof($$
            """, "y1");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/997410")]
    public Task StaticMemberInNameOfInStaticContext()
        => VerifyItemExistsAsync("""
            class C
            {
              int y1 = 15;
              static int y2 = 1;
              static string x = nameof($$
            """, "y2");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/883293")]
    public Task IncompleteDeclarationExpressionType()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
              void goo()
                {
                    var x = Console.$$
                    var y = 3;
                }
            }
            """, "WriteLine");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1024380")]
    public Task StaticAndInstanceInNameOf()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                class D
                {
                    public int x;
                    public static int y;   
                }

              void goo()
                {
                    var z = nameof(C.D.$$
                }
            }
            """, [
            ItemExpectation.Exists("x"),
            ItemExpectation.Exists("y"),
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1663")]
    public Task NameOfMembersListedForLocals()
        => VerifyItemExistsAsync("""
            class C
            {
                void M()
                {
                    var x = nameof(T.z.$$)
                }
            }

            public class T
            {
                public U z; 
            }

            public class U
            {
                public int nope;
            }
            """, "nope");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1029522")]
    public Task NameOfMembersListedForNamespacesAndTypes2()
        => VerifyItemExistsAsync("""
            class C
            {
                void M()
                {
                    var x = nameof(U.$$)
                }
            }

            public class T
            {
                public U z; 
            }

            public class U
            {
                public int nope;
            }
            """, "nope");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1029522")]
    public Task NameOfMembersListedForNamespacesAndTypes3()
        => VerifyItemExistsAsync("""
            class C
            {
                void M()
                {
                    var x = nameof(N.$$)
                }
            }

            namespace N
            {
            public class U
            {
                public int nope;
            }
            }
            """, "U");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1029522")]
    public Task NameOfMembersListedForNamespacesAndTypes4()
        => VerifyItemExistsAsync("""
            using z = System;
            class C
            {
                void M()
                {
                    var x = nameof(z.$$)
                }
            }
            """, "Console");

    [Fact]
    public Task InterpolatedStrings1()
        => VerifyItemExistsAsync("""
            class C
            {
                void M()
                {
                    var a = "Hello";
                    var b = "World";
                    var c = $"{$$
            """, "a");

    [Fact]
    public Task InterpolatedStrings2()
        => VerifyItemExistsAsync("""
            class C
            {
                void M()
                {
                    var a = "Hello";
                    var b = "World";
                    var c = $"{$$}";
                }
            }
            """, "a");

    [Fact]
    public Task InterpolatedStrings3()
        => VerifyItemExistsAsync("""
            class C
            {
                void M()
                {
                    var a = "Hello";
                    var b = "World";
                    var c = $"{a}, {$$
            """, "b");

    [Fact]
    public Task InterpolatedStrings4()
        => VerifyItemExistsAsync("""
            class C
            {
                void M()
                {
                    var a = "Hello";
                    var b = "World";
                    var c = $"{a}, {$$}";
                }
            }
            """, "b");

    [Fact]
    public Task InterpolatedStrings5()
        => VerifyItemExistsAsync("""
            class C
            {
                void M()
                {
                    var a = "Hello";
                    var b = "World";
                    var c = $@"{a}, {$$
            """, "b");

    [Fact]
    public Task InterpolatedStrings6()
        => VerifyItemExistsAsync("""
            class C
            {
                void M()
                {
                    var a = "Hello";
                    var b = "World";
                    var c = $@"{a}, {$$}";
                }
            }
            """, "b");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
    public Task NotBeforeFirstStringHole()
        => VerifyNoItemsExistAsync(AddInsideMethod(
            """
            var x = "\{0}$$\{1}\{2}"
            """));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
    public Task NotBetweenStringHoles()
        => VerifyNoItemsExistAsync(AddInsideMethod(
            """
            var x = "\{0}\{1}$$\{2}"
            """));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
    public Task NotAfterStringHoles()
        => VerifyNoItemsExistAsync(AddInsideMethod(
            """
            var x = "\{0}\{1}\{2}$$"
            """));

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087171")]
    public Task CompletionAfterTypeOfGetType()
        => VerifyItemExistsAsync(AddInsideMethod(
"typeof(int).GetType().$$"), "GUID");

    [Fact]
    public Task UsingDirectives1()
        => VerifyExpectedItemsAsync("""
            using $$

            class A { }
            static class B { }

            namespace N
            {
                class C { }
                static class D { }

                namespace M { }
            }
            """, [
            ItemExpectation.Absent("A"),
            ItemExpectation.Absent("B"),
            ItemExpectation.Exists("N"),
        ]);

    [Fact]
    public Task UsingDirectives2()
        => VerifyExpectedItemsAsync("""
            using N.$$

            class A { }
            static class B { }

            namespace N
            {
                class C { }
                static class D { }

                namespace M { }
            }
            """, [
            ItemExpectation.Absent("C"),
            ItemExpectation.Absent("D"),
            ItemExpectation.Exists("M"),
        ]);

    [Fact]
    public Task UsingDirectives3()
        => VerifyExpectedItemsAsync("""
            using G = $$

            class A { }
            static class B { }

            namespace N
            {
                class C { }
                static class D { }

                namespace M { }
            }
            """, [
            ItemExpectation.Exists("A"),
            ItemExpectation.Exists("B"),
            ItemExpectation.Exists("N"),
        ]);

    [Fact]
    public Task UsingDirectives4()
        => VerifyExpectedItemsAsync("""
            using G = N.$$

            class A { }
            static class B { }

            namespace N
            {
                class C { }
                static class D { }

                namespace M { }
            }
            """, [
            ItemExpectation.Exists("C"),
            ItemExpectation.Exists("D"),
            ItemExpectation.Exists("M"),
        ]);

    [Fact]
    public Task UsingDirectives5()
        => VerifyExpectedItemsAsync("""
            using static $$

            class A { }
            static class B { }

            namespace N
            {
                class C { }
                static class D { }

                namespace M { }
            }
            """, [
            ItemExpectation.Exists("A"),
            ItemExpectation.Exists("B"),
            ItemExpectation.Exists("N"),
        ]);

    [Fact]
    public Task UsingDirectives6()
        => VerifyExpectedItemsAsync("""
            using static N.$$

            class A { }
            static class B { }

            namespace N
            {
                class C { }
                static class D { }

                namespace M { }
            }
            """, [
            ItemExpectation.Exists("C"),
            ItemExpectation.Exists("D"),
            ItemExpectation.Exists("M"),
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67985")]
    public Task UsingDirectives7()
        => VerifyExpectedItemsAsync("""
            using static unsafe $$

            class A { }
            static class B { }

            namespace N
            {
                class C { }
                static class D { }

                namespace M { }
            }
            """, [
            ItemExpectation.Exists("A"),
            ItemExpectation.Exists("B"),
            ItemExpectation.Exists("N"),
        ]);

    [Fact]
    public Task UsingStaticDoesNotShowDelegates1()
        => VerifyExpectedItemsAsync("""
            using static $$

            class A { }
            delegate void B();

            namespace N
            {
                class C { }
                static class D { }

                namespace M { }
            }
            """, [
            ItemExpectation.Exists("A"),
            ItemExpectation.Absent("B"),
            ItemExpectation.Exists("N"),
        ]);

    [Fact]
    public Task UsingStaticDoesNotShowDelegates2()
        => VerifyExpectedItemsAsync("""
            using static N.$$

            class A { }
            static class B { }

            namespace N
            {
                class C { }
                delegate void D();

                namespace M { }
            }
            """, [
            ItemExpectation.Exists("C"),
            ItemExpectation.Absent("D"),
            ItemExpectation.Exists("M"),
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67985")]
    public Task UsingStaticDoesNotShowDelegates3()
        => VerifyExpectedItemsAsync("""
            using static unsafe $$

            class A { }
            delegate void B();

            namespace N
            {
                class C { }
                static class D { }

                namespace M { }
            }
            """, [
            ItemExpectation.Exists("A"),
            ItemExpectation.Absent("B"),
            ItemExpectation.Exists("N"),
        ]);

    [Fact]
    public Task UsingStaticShowInterfaces1()
        => VerifyExpectedItemsAsync("""
            using static N.$$

            class A { }
            static class B { }

            namespace N
            {
                class C { }
                interface I { }

                namespace M { }
            }
            """, [
            ItemExpectation.Exists("C"),
            ItemExpectation.Exists("I"),
            ItemExpectation.Exists("M"),
        ]);

    [Fact]
    public Task UsingStaticShowInterfaces2()
        => VerifyExpectedItemsAsync("""
            using static $$

            class A { }
            interface I { }

            namespace N
            {
                class C { }
                static class D { }

                namespace M { }
            }
            """, [
            ItemExpectation.Exists("A"),
            ItemExpectation.Exists("I"),
            ItemExpectation.Exists("N"),
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67985")]
    public Task UsingStaticShowInterfaces3()
        => VerifyExpectedItemsAsync("""
            using static unsafe $$

            class A { }
            interface I { }

            namespace N
            {
                class C { }
                static class D { }

                namespace M { }
            }
            """, [
            ItemExpectation.Exists("A"),
            ItemExpectation.Exists("I"),
            ItemExpectation.Exists("N"),
        ]);

    [Fact]
    public Task UsingStaticAndExtensionMethods1()
        => VerifyExpectedItemsAsync("""
            using static A;
            using static B;

            static class A
            {
                public static void Goo(this string s) { }
            }

            static class B
            {
                public static void Bar(this string s) { }
            }

            class C
            {
                void M()
                {
                    $$
                }
            }
            """, [
            ItemExpectation.Absent("Goo"),
            ItemExpectation.Absent("Bar"),
        ]);

    [Fact]
    public Task UsingStaticAndExtensionMethods2()
        => VerifyExpectedItemsAsync("""
            using N;

            namespace N
            {
                static class A
                {
                    public static void Goo(this string s) { }
                }

                static class B
                {
                    public static void Bar(this string s) { }
                }
            }

            class C
            {
                void M()
                {
                    $$
                }
            }
            """, [
            ItemExpectation.Absent("Goo"),
            ItemExpectation.Absent("Bar"),
        ]);

    [Fact]
    public Task UsingStaticAndExtensionMethods3()
        => VerifyExpectedItemsAsync("""
            using N;

            namespace N
            {
                static class A
                {
                    public static void Goo(this string s) { }
                }

                static class B
                {
                    public static void Bar(this string s) { }
                }
            }

            class C
            {
                void M()
                {
                    string s;
                    s.$$
                }
            }
            """, [
            ItemExpectation.Exists("Goo"),
            ItemExpectation.Exists("Bar"),
        ]);

    [Fact]
    public Task UsingStaticAndExtensionMethods4()
        => VerifyExpectedItemsAsync("""
            using static N.A;
            using static N.B;

            namespace N
            {
                static class A
                {
                    public static void Goo(this string s) { }
                }

                static class B
                {
                    public static void Bar(this string s) { }
                }
            }

            class C
            {
                void M()
                {
                    string s;
                    s.$$
                }
            }
            """, [
            ItemExpectation.Exists("Goo"),
            ItemExpectation.Exists("Bar"),
        ]);

    [Fact]
    public Task UsingStaticAndExtensionMethods5()
        => VerifyExpectedItemsAsync("""
            using static N.A;

            namespace N
            {
                static class A
                {
                    public static void Goo(this string s) { }
                }

                static class B
                {
                    public static void Bar(this string s) { }
                }
            }

            class C
            {
                void M()
                {
                    string s;
                    s.$$
                }
            }
            """, [
            ItemExpectation.Exists("Goo"),
            ItemExpectation.Absent("Bar"),
        ]);

    [Fact]
    public Task UsingStaticAndExtensionMethods6()
        => VerifyExpectedItemsAsync("""
            using static N.B;

            namespace N
            {
                static class A
                {
                    public static void Goo(this string s) { }
                }

                static class B
                {
                    public static void Bar(this string s) { }
                }
            }

            class C
            {
                void M()
                {
                    string s;
                    s.$$
                }
            }
            """, [
            ItemExpectation.Absent("Goo"),
            ItemExpectation.Exists("Bar"),
        ]);

    [Fact]
    public Task UsingStaticAndExtensionMethods7()
        => VerifyExpectedItemsAsync("""
            using N;
            using static N.B;

            namespace N
            {
                static class A
                {
                    public static void Goo(this string s) { }
                }

                static class B
                {
                    public static void Bar(this string s) { }
                }
            }

            class C
            {
                void M()
                {
                    string s;
                    s.$$;
                }
            }
            """, [
            ItemExpectation.Exists("Goo"),
            ItemExpectation.Exists("Bar"),
        ]);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7932")]
    public Task ExtensionMethodWithinSameClassOfferedForCompletion()
        => VerifyItemExistsAsync("""
            public static class Test
            {
                static void TestB()
                {
                    $$
                }
                static void TestA(this string s) { }
            }
            """, "TestA");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7932")]
    public Task ExtensionMethodWithinParentClassOfferedForCompletion()
        => VerifyItemExistsAsync("""
            public static class Parent
            {
                static void TestA(this string s) { }
                static void TestC(string s) { }
                public static class Test
                {
                    static void TestB()
                    {
                        $$
                    }
                }
            }
            """, "TestA");

    [Fact]
    public Task ExceptionFilter1()
        => VerifyItemExistsAsync("""
            using System;

            class C
            {
                void M(bool x)
                {
                    try
                    {
                    }
                    catch when ($$
            """, "x");

    [Fact]
    public Task ExceptionFilter1_NotBeforeOpenParen()
        => VerifyNoItemsExistAsync("""
            using System;

            class C
            {
                void M(bool x)
                {
                    try
                    {
                    }
                    catch when $$
            """);

    [Fact]
    public Task ExceptionFilter2()
        => VerifyItemExistsAsync("""
            using System;

            class C
            {
                void M(bool x)
                {
                    try
                    {
                    }
                    catch (Exception ex) when ($$
            """, "x");

    [Fact]
    public Task ExceptionFilter2_NotBeforeOpenParen()
        => VerifyNoItemsExistAsync("""
            using System;

            class C
            {
                void M(bool x)
                {
                    try
                    {
                    }
                    catch (Exception ex) when $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25084")]
    public Task SwitchCaseWhenClause1()
        => VerifyItemExistsAsync("""
            class C
            {
                void M(bool x)
                {
                    switch (1)
                    {
                        case 1 when $$
            """, "x");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25084")]
    public Task SwitchCaseWhenClause2()
        => VerifyItemExistsAsync("""
            class C
            {
                void M(bool x)
                {
                    switch (1)
                    {
                        case int i when $$
            """, "x");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/717")]
    public Task ExpressionContextCompletionWithinCast()
        => VerifyItemExistsAsync("""
            class Program
            {
                void M()
                {
                    for (int i = 0; i < 5; i++)
                    {
                        var x = ($$)
                        var y = 1;
                    }
                }
            }
            """, "i");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1277")]
    public Task NoInstanceMembersInPropertyInitializer()
        => VerifyItemIsAbsentAsync("""
            class A {
                int abc;
                int B { get; } = $$
            }
            """, "abc");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1277")]
    public Task StaticMembersInPropertyInitializer()
        => VerifyItemExistsAsync("""
            class A {
                static Action s_abc;
                event Action B = $$
            }
            """, "s_abc");

    [Fact]
    public Task NoInstanceMembersInFieldLikeEventInitializer()
        => VerifyItemIsAbsentAsync("""
            class A {
                Action abc;
                event Action B = $$
            }
            """, "abc");

    [Fact]
    public Task StaticMembersInFieldLikeEventInitializer()
        => VerifyItemExistsAsync("""
            class A {
                static Action s_abc;
                event Action B = $$
            }
            """, "s_abc");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5069")]
    public Task InstanceMembersInTopLevelFieldInitializer()
        => VerifyItemExistsAsync("""
            int aaa = 1;
            int bbb = $$
            """, "aaa", sourceCodeKind: SourceCodeKind.Script);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5069")]
    public Task InstanceMembersInTopLevelFieldLikeEventInitializer()
        => VerifyItemExistsAsync("""
            Action aaa = null;
            event Action bbb = $$
            """, "aaa", sourceCodeKind: SourceCodeKind.Script);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33")]
    public Task NoConditionalAccessCompletionOnTypes1()
        => VerifyNoItemsExistAsync("""
            using A = System
            class C
            {
                A?.$$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33")]
    public Task NoConditionalAccessCompletionOnTypes2()
        => VerifyNoItemsExistAsync("""
            class C
            {
                System?.$$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33")]
    public Task NoConditionalAccessCompletionOnTypes3()
        => VerifyNoItemsExistAsync("""
            class C
            {
                System.Console?.$$
            }
            """);

    [Fact]
    public Task CompletionInIncompletePropertyDeclaration()
        => VerifyItemExistsAsync("""
            class Class1
            {
                public string Property1 { get; set; }
            }

            class Class2
            {
                public string Property { get { return this.Source.$$
                public Class1 Source { get; set; }
            }
            """, "Property1");

    [Fact]
    public async Task NoCompletionInShebangComments()
    {
        await VerifyNoItemsExistAsync("#!$$", sourceCodeKind: SourceCodeKind.Script);
        await VerifyNoItemsExistAsync("#! S$$", sourceCodeKind: SourceCodeKind.Script, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public Task CompoundNameTargetTypePreselection()
        => VerifyItemExistsAsync("""
            class Class1
            {
                void goo()
                {
                    int x = 3;
                    string y = x.$$
                }
            }
            """, "ToString", matchPriority: SymbolMatchPriority.PreferEventOrMethod);

    [Fact]
    public Task TargetTypeInCollectionInitializer1()
        => VerifyItemExistsAsync("""
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    int z;
                    string q;
                    List<int> x = new List<int>() { $$  }
                }
            }
            """, "z", matchPriority: SymbolMatchPriority.PreferLocalOrParameterOrRangeVariable);

    [Fact]
    public Task TargetTypeInCollectionInitializer2()
        => VerifyItemExistsAsync("""
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    int z;
                    string q;
                    List<int> x = new List<int>() { 1, $$  }
                }
            }
            """, "z", matchPriority: SymbolMatchPriority.PreferLocalOrParameterOrRangeVariable);

    [Fact]
    public Task TargeTypeInObjectInitializer1()
        => VerifyItemExistsAsync("""
            class C
            {
                public int X { get; set; }
                public int Y { get; set; }

                void goo()
                {
                    int i;
                    var c = new C() { X = $$ }
                }
            }
            """, "i", matchPriority: SymbolMatchPriority.PreferLocalOrParameterOrRangeVariable);

    [Fact]
    public Task TargeTypeInObjectInitializer2()
        => VerifyItemExistsAsync("""
            class C
            {
                public int X { get; set; }
                public int Y { get; set; }

                void goo()
                {
                    int i;
                    var c = new C() { X = 1, Y = $$ }
                }
            }
            """, "i", matchPriority: SymbolMatchPriority.PreferLocalOrParameterOrRangeVariable);

    [Fact]
    public async Task TupleElements()
    {
        var markup = """
            class C
            {
                void goo()
                {
                    var t = (Alice: 1, Item2: 2, ITEM3: 3, 4, 5, 6, 7, 8, Bob: 9);
                    t.$$
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs;

        await VerifyExpectedItemsAsync(markup, [
            ItemExpectation.Exists("Alice"),
            ItemExpectation.Exists("Bob"),
            ItemExpectation.Exists("CompareTo"),
            ItemExpectation.Exists("Equals"),
            ItemExpectation.Exists("GetHashCode"),
            ItemExpectation.Exists("GetType"),
            ItemExpectation.Exists("Item2"),
            ItemExpectation.Exists("ITEM3"),
            ItemExpectation.Exists("Item4"),
            ItemExpectation.Exists("Item5"),
            ItemExpectation.Exists("Item6"),
            ItemExpectation.Exists("Item7"),
            ItemExpectation.Exists("Item8"),
            ItemExpectation.Exists("ToString"),

            ItemExpectation.Absent("Item1"),
            ItemExpectation.Absent("Item9"),
            ItemExpectation.Absent("Rest"),
            ItemExpectation.Absent("Item3")
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14546")]
    public async Task TupleElementsCompletionOffMethodGroup()
    {
        var markup = """
            class C
            {
                void goo()
                {
                    new object().ToString.$$
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs;

        // should not crash
        await VerifyNoItemsExistAsync(markup);
    }

    [Fact]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/13480")]
    public Task NoCompletionInLocalFuncGenericParamList()
        => VerifyNoItemsExistAsync("""
            class C
            {
                void M()
                {
                    int Local<$$
            """);

    [Fact]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/13480")]
    public Task CompletionForAwaitWithoutAsync()
        => VerifyAnyItemExistsAsync("""
            class C
            {
                void M()
                {
                    await Local<$$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
    public Task TupleTypeAtMemberLevel1()
        => VerifyItemExistsAsync("""
            class C
            {
                ($$
            }
            """, "C");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
    public Task TupleTypeAtMemberLevel2()
        => VerifyItemExistsAsync("""
            class C
            {
                ($$)
            }
            """, "C");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
    public Task TupleTypeAtMemberLevel3()
        => VerifyItemExistsAsync("""
            class C
            {
                (C, $$
            }
            """, "C");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
    public Task TupleTypeAtMemberLevel4()
        => VerifyItemExistsAsync("""
            class C
            {
                (C, $$)
            }
            """, "C");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
    public Task TupleTypeInForeach()
        => VerifyItemExistsAsync("""
            class C
            {
                void M()
                {
                    foreach ((C, $$
                }
            }
            """, "C");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
    public Task TupleTypeInParameterList()
        => VerifyItemExistsAsync("""
            class C
            {
                void M((C, $$)
                {
                }
            }
            """, "C");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
    public Task TupleTypeInNameOf()
        => VerifyItemExistsAsync("""
            class C
            {
                void M()
                {
                    var x = nameof((C, $$
                }
            }
            """, "C");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14163")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task LocalFunctionDescription()
        => VerifyItemExistsAsync("""
            class C
            {
                void M()
                {
                    void Local() { }

                    $$
                }
            }
            """, "Local", "void Local()");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14163")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task LocalFunctionDescription2()
        => VerifyItemExistsAsync("""
            using System;
            class C
            {
                class var { }
                void M()
                {
                    Action<int> Local(string x, ref var @class, params Func<int, string> f)
                    {
                        return () => 0;
                    }

                    $$
                }
            }
            """, "Local", "Action<int> Local(string x, ref var @class, params Func<int, string> f)");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18359")]
    public async Task EnumMemberAfterDot()
    {
        var markup =
            """
            namespace ConsoleApplication253
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        M(E.$$)
                    }

                    static void M(E e) { }
                }

                enum E
                {
                    A,
                    B,
                }
            }
            """;
        // VerifyItemExistsAsync also tests with the item typed.
        await VerifyItemExistsAsync(markup, "A");
        await VerifyItemExistsAsync(markup, "B");
    }

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/8321")]
    public Task NotOnMethodGroup1()
        => VerifyNoItemsExistAsync("""
            namespace ConsoleApp
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        Main.$$
                    }
                }
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/8321")]
    public Task NotOnMethodGroup2()
        => VerifyNoItemsExistAsync("""
            class C {
                void M<T>() {M<C>.$$ }
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/8321")]
    public Task NotOnMethodGroup3()
        => VerifyNoItemsExistAsync("""
            class C {
                void M() {M.$$}
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=420697&_a=edit")]
    public Task DoNotCrashInExtensionMethoWithExpressionBodiedMember()
        => VerifyItemExistsAsync("""
            public static class Extensions { public static T Get<T>(this object o) => $$}
            """, "o");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task EnumConstraint()
        => VerifyItemExistsAsync("""
            public class X<T> where T : System.$$
            """, "Enum");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task DelegateConstraint()
        => VerifyItemExistsAsync("""
            public class X<T> where T : System.$$
            """, "Delegate");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task MulticastDelegateConstraint()
        => VerifyItemExistsAsync("""
            public class X<T> where T : System.$$
            """, "MulticastDelegate");

    private static string CreateThenIncludeTestCode(string lambdaExpressionString, string methodDeclarationString)
    {
        var template = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Linq.Expressions;

            namespace ThenIncludeIntellisenseBug
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        var registrations = new List<Registration>().AsQueryable();
                        var reg = registrations.Include(r => r.Activities).ThenInclude([1]);
                    }
                }

                internal class Registration
                {
                    public ICollection<Activity> Activities { get; set; }
                }

                public class Activity
                {
                    public Task Task { get; set; }
                }

                public class Task
                {
                    public string Name { get; set; }
                }

                public interface IIncludableQueryable<out TEntity, out TProperty> : IQueryable<TEntity>
                {
                }

                public static class EntityFrameworkQuerybleExtensions
                {
                    public static IIncludableQueryable<TEntity, TProperty> Include<TEntity, TProperty>(
                        this IQueryable<TEntity> source,
                        Expression<Func<TEntity, TProperty>> navigationPropertyPath)
                        where TEntity : class
                    {
                        return default(IIncludableQueryable<TEntity, TProperty>);
                    }

                    [2]
                }
            }
            """;

        return template.Replace("[1]", lambdaExpressionString).Replace("[2]", methodDeclarationString);
    }

    [Fact]
    public async Task ThenInclude()
    {
        var markup = CreateThenIncludeTestCode("b => b.$$",
            """
            public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
                this IIncludableQueryable<TEntity, ICollection<TPreviousProperty>> source,
                Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
            {
                return default(IIncludableQueryable<TEntity, TProperty>);
            }

            public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
                this IIncludableQueryable<TEntity, TPreviousProperty> source,
                Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
            {
                return default(IIncludableQueryable<TEntity, TProperty>);
            }
            """);

        await VerifyItemExistsAsync(markup, "Task");
        await VerifyItemExistsAsync(markup, "FirstOrDefault", displayTextSuffix: "<>");
    }

    [Fact]
    public async Task ThenIncludeNoExpression()
    {
        var markup = CreateThenIncludeTestCode("b => b.$$",
            """
            public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
                this IIncludableQueryable<TEntity, ICollection<TPreviousProperty>> source,
                Func<TPreviousProperty, TProperty> navigationPropertyPath) where TEntity : class
            {
                return default(IIncludableQueryable<TEntity, TProperty>);
            }

            public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
                this IIncludableQueryable<TEntity, TPreviousProperty> source,
                Func<TPreviousProperty, TProperty> navigationPropertyPath) where TEntity : class
            {
                return default(IIncludableQueryable<TEntity, TProperty>);
            }
            """);

        await VerifyItemExistsAsync(markup, "Task");
        await VerifyItemExistsAsync(markup, "FirstOrDefault", displayTextSuffix: "<>");
    }

    [Fact]
    public async Task ThenIncludeSecondArgument()
    {
        var markup = CreateThenIncludeTestCode("0, b => b.$$",
            """
            public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
                this IIncludableQueryable<TEntity, ICollection<TPreviousProperty>> source,
                int a,
                Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
            {
                return default(IIncludableQueryable<TEntity, TProperty>);
            }

            public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
                this IIncludableQueryable<TEntity, TPreviousProperty> source,
                int a,
                Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
            {
                return default(IIncludableQueryable<TEntity, TProperty>);
            }
            """);

        await VerifyItemExistsAsync(markup, "Task");
        await VerifyItemExistsAsync(markup, "FirstOrDefault", displayTextSuffix: "<>");
    }

    [Fact]
    public async Task ThenIncludeSecondArgumentAndMultiArgumentLambda()
    {
        var markup = CreateThenIncludeTestCode("0, (a,b,c) => c.$$)",
            """
            public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
                this IIncludableQueryable<TEntity, ICollection<TPreviousProperty>> source,
                int a,
                Expression<Func<string, string, TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
            {
                return default(IIncludableQueryable<TEntity, TProperty>);
            }

            public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
                this IIncludableQueryable<TEntity, TPreviousProperty> source,
                int a,
                Expression<Func<string, string, TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
            {
                return default(IIncludableQueryable<TEntity, TProperty>);
            }
            """);

        await VerifyItemExistsAsync(markup, "Task");
        await VerifyItemExistsAsync(markup, "FirstOrDefault", displayTextSuffix: "<>");
    }

    [Fact]
    public async Task ThenIncludeSecondArgumentNoOverlap()
    {
        var markup = CreateThenIncludeTestCode("b => b.Task, b =>b.$$",
            """
            public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
                this IIncludableQueryable<TEntity, ICollection<TPreviousProperty>> source,
                Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath,
                Expression<Func<TPreviousProperty, TProperty>> anotherNavigationPropertyPath) where TEntity : class
                {
                    return default(IIncludableQueryable<TEntity, TProperty>);
                }

                public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
                   this IIncludableQueryable<TEntity, TPreviousProperty> source,
                   Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
                {
                    return default(IIncludableQueryable<TEntity, TProperty>);
                }
            """);

        await VerifyItemExistsAsync(markup, "Task");
        await VerifyItemIsAbsentAsync(markup, "FirstOrDefault", displayTextSuffix: "<>");
    }

    [Fact]
    public async Task ThenIncludeSecondArgumentAndMultiArgumentLambdaWithNoLambdaOverlap()
    {
        var markup = CreateThenIncludeTestCode("0, (a,b,c) => c.$$",
            """
            public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
                this IIncludableQueryable<TEntity, ICollection<TPreviousProperty>> source,
                int a,
                Expression<Func<string, TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
            {
                return default(IIncludableQueryable<TEntity, TProperty>);
            }

            public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
                this IIncludableQueryable<TEntity, TPreviousProperty> source,
                int a,
                Expression<Func<string, string, TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
            {
                return default(IIncludableQueryable<TEntity, TProperty>);
            }
            """);

        await VerifyItemIsAbsentAsync(markup, "Task");
        await VerifyItemExistsAsync(markup, "FirstOrDefault", displayTextSuffix: "<>");
    }

    [Fact]
    public async Task ThenIncludeGenericAndNoGenericOverloads()
    {
        var markup = CreateThenIncludeTestCode("c => c.$$",
            """
            public static IIncludableQueryable<Registration, Task> ThenInclude(
                       this IIncludableQueryable<Registration, ICollection<Activity>> source,
                       Func<Activity, Task> navigationPropertyPath)
            {
                return default(IIncludableQueryable<Registration, Task>);
            }

            public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
                this IIncludableQueryable<TEntity, TPreviousProperty> source,
                Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
            {
                return default(IIncludableQueryable<TEntity, TProperty>);
            }
            """);

        await VerifyItemExistsAsync(markup, "Task");
        await VerifyItemExistsAsync(markup, "FirstOrDefault", displayTextSuffix: "<>");
    }

    [Fact]
    public async Task ThenIncludeNoGenericOverloads()
    {
        var markup = CreateThenIncludeTestCode("c => c.$$",
            """
            public static IIncludableQueryable<Registration, Task> ThenInclude(
                this IIncludableQueryable<Registration, ICollection<Activity>> source,
                Func<Activity, Task> navigationPropertyPath)
            {
                return default(IIncludableQueryable<Registration, Task>);
            }

            public static IIncludableQueryable<Registration, Activity> ThenInclude(
                this IIncludableQueryable<Registration, ICollection<Activity>> source,
                Func<ICollection<Activity>, Activity> navigationPropertyPath) 
            {
                return default(IIncludableQueryable<Registration, Activity>);
            }
            """);

        await VerifyItemExistsAsync(markup, "Task");
        await VerifyItemExistsAsync(markup, "FirstOrDefault", displayTextSuffix: "<>");
    }

    [Fact]
    public Task CompletionForLambdaWithOverloads()
        => VerifyExpectedItemsAsync("""
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Linq;
            using System.Linq.Expressions;

            namespace ClassLibrary1
            {
                class SomeItem
                {
                    public string A;
                    public int B;
                }
                class SomeCollection<T> : List<T>
                {
                    public virtual SomeCollection<T> Include(string path) => null;
                }

                static class Extensions
                {
                    public static IList<T> Include<T, TProperty>(this IList<T> source, Expression<Func<T, TProperty>> path)
                        => null;

                    public static IList Include(this IList source, string path) => null;

                    public static IList<T> Include<T>(this IList<T> source, string path) => null;
                }

                class Program 
                {
                    void M(SomeCollection<SomeItem> c)
                    {
                        var a = from m in c.Include(t => t.$$);
                    }
                }
            }
            """, [
            ItemExpectation.Absent("Substring"),
            ItemExpectation.Exists("A"),
            ItemExpectation.Exists("B"),
        ]);

    [Fact, WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1056325")]
    public async Task CompletionForLambdaWithOverloads2()
    {
        var markup = """
            using System;

            class C
            {
                void M(Action<int> a) { }
                void M(string s) { }

                void Test()
                {
                    M(p => p.$$);
                }
            }
            """;

        await VerifyItemIsAbsentAsync(markup, "Substring");
        await VerifyItemExistsAsync(markup, "GetTypeCode");
    }

    [Fact, WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1056325")]
    public async Task CompletionForLambdaWithOverloads3()
    {
        var markup = """
            using System;

            class C
            {
                void M(Action<int> a) { }
                void M(Action<string> a) { }

                void Test()
                {
                    M((int p) => p.$$);
                }
            }
            """;

        await VerifyItemIsAbsentAsync(markup, "Substring");
        await VerifyItemExistsAsync(markup, "GetTypeCode");
    }

    [Fact, WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1056325")]
    public async Task CompletionForLambdaWithOverloads4()
    {
        var markup = """
            using System;

            class C
            {
                void M(Action<int> a) { }
                void M(Action<string> a) { }

                void Test()
                {
                    M(p => p.$$);
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "Substring");
        await VerifyItemExistsAsync(markup, "GetTypeCode");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42997")]
    public Task CompletionForLambdaWithTypeParameters()
        => VerifyItemExistsAsync("""
            using System;
            using System.Collections.Generic;

            class Program
            {
                static void M()
                {
                    Create(new List<Product>(), arg => arg.$$);
                }

                static void Create<T>(List<T> list, Action<T> expression) { }
            }

            class Product { public void MyProperty() { } }
            """, "MyProperty");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42997")]
    public Task CompletionForLambdaWithTypeParametersAndOverloads()
        => VerifyExpectedItemsAsync("""
            using System;
            using System.Collections.Generic;

            class Program
            {
                static void M()
                {
                    Create(new Dictionary<Product1, Product2>(), arg => arg.$$);
                }

                static void Create<T, U>(Dictionary<T, U> list, Action<T> expression) { }
                static void Create<T, U>(Dictionary<U, T> list, Action<T> expression) { }
            }

            class Product1 { public void MyProperty1() { } }
            class Product2 { public void MyProperty2() { } }
            """, [
            ItemExpectation.Exists("MyProperty1"),
            ItemExpectation.Exists("MyProperty2"),
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42997")]
    public Task CompletionForLambdaWithTypeParametersAndOverloads2()
        => VerifyExpectedItemsAsync("""
            using System;
            using System.Collections.Generic;

            class Program
            {
                static void M()
                {
                    Create(new Dictionary<Product1,Product2>(),arg => arg.$$);
                }

                static void Create<T, U>(Dictionary<T, U> list, Action<T> expression) { }
                static void Create<T, U>(Dictionary<U, T> list, Action<T> expression) { }
                static void Create(Dictionary<Product1, Product2> list, Action<Product3> expression) { }
            }

            class Product1 { public void MyProperty1() { } }
            class Product2 { public void MyProperty2() { } }
            class Product3 { public void MyProperty3() { } }
            """, [
            ItemExpectation.Exists("MyProperty1"),
            ItemExpectation.Exists("MyProperty2"),
            ItemExpectation.Exists("MyProperty3")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42997")]
    public async Task CompletionForLambdaWithTypeParametersFromClass()
    {
        var markup = """
            using System;

            class Program<T>
            {
                static void M()
                {
                    Create(arg => arg.$$);
                }

                static void Create(Action<T> expression) { }
            }

            class Product { public void MyProperty() { } }
            """;

        await VerifyItemExistsAsync(markup, "GetHashCode");
        await VerifyItemIsAbsentAsync(markup, "MyProperty");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42997")]
    public async Task CompletionForLambdaWithTypeParametersFromClassWithConstraintOnType()
    {
        var markup = """
            using System;

            class Program<T> where T : Product
            {
                static void M()
                {
                    Create(arg => arg.$$);
                }

                static void Create(Action<T> expression) { }
            }

            class Product { public void MyProperty() { } }
            """;

        await VerifyItemExistsAsync(markup, "GetHashCode");
        await VerifyItemExistsAsync(markup, "MyProperty");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42997")]
    public async Task CompletionForLambdaWithTypeParametersFromClassWithConstraintOnMethod()
    {
        var markup = """
            using System;

            class Program
            {
                static void M()
                {
                    Create(arg => arg.$$);
                }

                static void Create<T>(Action<T> expression) where T : Product { }
            }

            class Product { public void MyProperty() { } }
            """;

        await VerifyItemExistsAsync(markup, "GetHashCode");
        await VerifyItemExistsAsync(markup, "MyProperty");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40216")]
    public Task CompletionForLambdaPassedAsNamedArgumentAtDifferentPositionFromCorrespondingParameter1()
        => VerifyItemExistsAsync("""
            using System;

            class C
            {
                void Test()
                {
                    X(y: t => Console.WriteLine(t.$$));
                }

                void X(int x = 7, Action<string> y = null) { }
            }
            """, "Length");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40216")]
    public Task CompletionForLambdaPassedAsNamedArgumentAtDifferentPositionFromCorrespondingParameter2()
        => VerifyItemExistsAsync("""
            using System;

            class C
            {
                void Test()
                {
                    X(y: t => Console.WriteLine(t.$$));
                }

                void X(int x, int z, Action<string> y) { }
            }
            """, "Length");

    [Fact]
    public Task CompletionForLambdaPassedAsArgumentInReducedExtensionMethod_NonInteractive()
        => VerifyItemExistsAsync("""
            using System;

            static class CExtensions
            {
                public static void X(this C x, Action<string> y) { }
            }

            class C
            {
                void Test()
                {
                    new C().X(t => Console.WriteLine(t.$$));
                }
            }
            """, "Length", sourceCodeKind: SourceCodeKind.Regular);

    [Fact]
    public Task CompletionForLambdaPassedAsArgumentInReducedExtensionMethod_Interactive()
        => VerifyItemExistsAsync("""
            using System;

            public static void X(this C x, Action<string> y) { }

            public class C
            {
                void Test()
                {
                    new C().X(t => Console.WriteLine(t.$$));
                }
            }
            """, "Length", sourceCodeKind: SourceCodeKind.Script);

    [Fact]
    public Task CompletionInsideMethodsWithNonFunctionsAsArguments()
        => VerifyExpectedItemsAsync("""
            using System;
            class c
            {
                void M()
                {
                    Goo(builder =>
                    {
                        builder.$$
                    });
                }

                void Goo(Action<Builder> configure)
                {
                    var builder = new Builder();
                    configure(builder);
                }
            }
            class Builder
            {
                public int Something { get; set; }
            }
            """, [
            ItemExpectation.Exists("Something"),
            ItemExpectation.Absent("BeginInvoke"),
            ItemExpectation.Absent("Clone"),
            ItemExpectation.Absent("Method"),
            ItemExpectation.Absent("Target")
        ]);

    [Fact]
    public Task CompletionInsideMethodsWithDelegatesAsArguments()
        => VerifyExpectedItemsAsync("""
            using System;

            class Program
            {
                public delegate void Delegate1(Uri u);
                public delegate void Delegate2(Guid g);

                public void M(Delegate1 d) { }
                public void M(Delegate2 d) { }

                public void Test()
                {
                    M(d => d.$$)
                }
            }
            """, [
            // Guid
            ItemExpectation.Exists("ToByteArray"),

            // Uri
            ItemExpectation.Exists("AbsoluteUri"),
            ItemExpectation.Exists("Fragment"),
            ItemExpectation.Exists("Query"),

            // Should not appear for Delegate
            ItemExpectation.Absent("BeginInvoke"),
            ItemExpectation.Absent("Clone"),
            ItemExpectation.Absent("Method"),
            ItemExpectation.Absent("Target")
        ]);

    [Fact]
    public Task CompletionInsideMethodsWithDelegatesAndReversingArguments()
        => VerifyExpectedItemsAsync("""
            using System;

            class Program
            {
                public delegate void Delegate1<T1,T2>(T2 t2, T1 t1);
                public delegate void Delegate2<T1,T2>(T2 t2, int g, T1 t1);

                public void M(Delegate1<Uri,Guid> d) { }
                public void M(Delegate2<Uri,Guid> d) { }

                public void Test()
                {
                    M(d => d.$$)
                }
            }
            """, [
            // Guid
            ItemExpectation.Exists("ToByteArray"),

            // Should not appear for Uri
            ItemExpectation.Absent("AbsoluteUri"),
            ItemExpectation.Absent("Fragment"),
            ItemExpectation.Absent("Query"),

            // Should not appear for Delegate
            ItemExpectation.Absent("BeginInvoke"),
            ItemExpectation.Absent("Clone"),
            ItemExpectation.Absent("Method"),
            ItemExpectation.Absent("Target")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36029")]
    public Task CompletionInsideMethodWithParamsBeforeParams()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                void M()
                {
                    Goo(builder =>
                    {
                        builder.$$
                    });
                }

                void Goo(Action<Builder> action, params Action<AnotherBuilder>[] otherActions)
                {
                }
            }
            class Builder
            {
                public int Something { get; set; }
            };

            class AnotherBuilder
            {
                public int AnotherSomething { get; set; }
            }
            """, [
            ItemExpectation.Absent("AnotherSomething"),
            ItemExpectation.Absent("FirstOrDefault"),
            ItemExpectation.Exists("Something")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36029")]
    public Task CompletionInsideMethodWithParamsInParams()
        => VerifyExpectedItemsAsync("""
            using System;
            class C
            {
                void M()
                {
                    Goo(b0 => { }, b1 => {}, b2 => { b2.$$ });
                }

                void Goo(Action<Builder> action, params Action<AnotherBuilder>[] otherActions)
                {
                }
            }
            class Builder
            {
                public int Something { get; set; }
            };

            class AnotherBuilder
            {
                public int AnotherSomething { get; set; }
            }
            """, [
            ItemExpectation.Absent("Something"),
            ItemExpectation.Absent("FirstOrDefault"),
            ItemExpectation.Exists("AnotherSomething")
        ]);

    [Fact, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)]
    public async Task TestTargetTypeFilterWithExperimentEnabled()
    {
        ShowTargetTypedCompletionFilter = true;
        await VerifyItemExistsAsync(
            """
            public class C
            {
                int intField;
                void M(int x)
                {
                    M($$);
                }
            }
            """, "intField",
            matchingFilters: [FilterSet.FieldFilter, FilterSet.TargetTypedFilter]);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)]
    public async Task TestNoTargetTypeFilterWithExperimentDisabled()
    {
        ShowTargetTypedCompletionFilter = false;
        await VerifyItemExistsAsync(
            """
            public class C
            {
                int intField;
                void M(int x)
                {
                    M($$);
                }
            }
            """, "intField",
            matchingFilters: [FilterSet.FieldFilter]);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)]
    public async Task TestTargetTypeFilter_NotOnObjectMembers()
    {
        ShowTargetTypedCompletionFilter = true;
        await VerifyItemExistsAsync(
            """
            public class C
            {
                void M(int x)
                {
                    M($$);
                }
            }
            """, "GetHashCode",
            matchingFilters: [FilterSet.MethodFilter]);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)]
    public async Task TestTargetTypeFilter_NotNamedTypes()
    {
        ShowTargetTypedCompletionFilter = true;

        var markup =
            """
            public class C
            {
                void M(C c)
                {
                    M($$);
                }
            }
            """;
        await VerifyItemExistsAsync(
            markup, "c",
            matchingFilters: [FilterSet.LocalAndParameterFilter, FilterSet.TargetTypedFilter]);

        await VerifyItemExistsAsync(
            markup, "C",
            matchingFilters: [FilterSet.ClassFilter]);
    }

    [Fact]
    public Task CompletionShouldNotProvideExtensionMethodsIfTypeConstraintDoesNotMatch()
        => VerifyExpectedItemsAsync("""
            public static class Ext
            {
                public static void DoSomething<T>(this T thing, string s) where T : class, I
                { 
                }
            }

            public interface I 
            {
            }

            public class C
            {
                public void M(string s)
                {
                    this.$$
                }
            }
            """, [
            ItemExpectation.Exists("M"),
            ItemExpectation.Exists("Equals"),
            ItemExpectation.Absent("DoSomething") with
            {
                DisplayTextSuffix = "<>"
            },
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38074")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task LocalFunctionInStaticMethod()
        => VerifyItemExistsAsync("""
            class C
            {
                static void M()
                {
                    void Local() { }

                    $$
                }
            }
            """, "Local");

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1152109")]
    public Task NoItemWithEmptyDisplayName()
        => VerifyItemIsAbsentAsync(
            """
            class C
            {
                static void M()
                {
                    int$$
                }
            }
            """, "",
            matchingFilters: [FilterSet.LocalAndParameterFilter]);

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public Task CompletionWithCustomizedCommitCharForMethod(char commitChar)
        => VerifyProviderCommitAsync("""
            class Program
            {
                private void Bar()
                {
                    F$$
                }

                private void Foo(int i)
                {
                }

                private void Foo(int i, int c)
                {
                }
            }
            """, "Foo", $$"""
            class Program
            {
                private void Bar()
                {
                    Foo(){{commitChar}}
                }

                private void Foo(int i)
                {
                }

                private void Foo(int i, int c)
                {
                }
            }
            """, commitChar: commitChar);

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public Task CompletionWithSemicolonInNestedMethod(char commitChar)
        => VerifyProviderCommitAsync("""
            class Program
            {
                private void Bar()
                {
                    Foo(F$$);
                }

                private int Foo(int i)
                {
                    return 1;
                }
            }
            """, "Foo", $$"""
            class Program
            {
                private void Bar()
                {
                    Foo(Foo(){{commitChar}});
                }

                private int Foo(int i)
                {
                    return 1;
                }
            }
            """, commitChar: commitChar);

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public Task CompletionWithCustomizedCommitCharForDelegateInferredType(char commitChar)
        => VerifyProviderCommitAsync("""
            using System;
            class Program
            {
                private void Bar()
                {
                    Bar2(F$$);
                }

                private void Foo()
                {
                }

                void Bar2(Action t) { }
            }
            """, "Foo", $$"""
            using System;
            class Program
            {
                private void Bar()
                {
                    Bar2(Foo{{commitChar}});
                }

                private void Foo()
                {
                }

                void Bar2(Action t) { }
            }
            """, commitChar: commitChar);

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public Task CompletionWithCustomizedCommitCharForConstructor(char commitChar)
        => VerifyProviderCommitAsync("""
            class Program
            {
                private static void Bar()
                {
                    var o = new P$$
                }
            }
            """, "Program", $$"""
            class Program
            {
                private static void Bar()
                {
                    var o = new Program(){{commitChar}}
                }
            }
            """, commitChar: commitChar);

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public Task CompletionWithCustomizedCharForTypeUnderNonObjectCreationContext(char commitChar)
        => VerifyProviderCommitAsync("""
            class Program
            {
                private static void Bar()
                {
                    var o = P$$
                }
            }
            """, "Program", $$"""
            class Program
            {
                private static void Bar()
                {
                    var o = Program{{commitChar}}
                }
            }
            """, commitChar: commitChar);

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public Task CompletionWithCustomizedCommitCharForAliasConstructor(char commitChar)
        => VerifyProviderCommitAsync("""
            using String2 = System.String;
            namespace Bar1
            {
                class Program
                {
                    private static void Bar()
                    {
                        var o = new S$$
                    }
                }
            }
            """, "String2", $$"""
            using String2 = System.String;
            namespace Bar1
            {
                class Program
                {
                    private static void Bar()
                    {
                        var o = new String2(){{commitChar}}
                    }
                }
            }
            """, commitChar: commitChar);

    [Fact]
    public Task CompletionWithSemicolonUnderNameofContext()
        => VerifyProviderCommitAsync("""
            namespace Bar1
            {
                class Program
                {
                    private static void Bar()
                    {
                        var o = nameof(B$$)
                    }
                }
            }
            """, "Bar", """
            namespace Bar1
            {
                class Program
                {
                    private static void Bar()
                    {
                        var o = nameof(Bar;)
                    }
                }
            }
            """, commitChar: ';');

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49072")]
    public Task EnumMemberAfterPatternMatch()
        => VerifyExpectedItemsAsync("""
            namespace N
            {
            	enum RankedMusicians
            	{
            		BillyJoel,
            		EveryoneElse
            	}

            	class C
            	{
            		void M(RankedMusicians m)
            		{
            			if (m is RankedMusicians.$$
            		}
            	}
            }
            """, [
            ItemExpectation.Exists("BillyJoel"),
            ItemExpectation.Exists("EveryoneElse"),
            ItemExpectation.Absent("Equals"),
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49072")]
    public Task EnumMemberAfterPatternMatchWithDeclaration()
        => VerifyExpectedItemsAsync("""
            namespace N
            {
            	enum RankedMusicians
            	{
            		BillyJoel,
            		EveryoneElse
            	}

            	class C
            	{
            		void M(RankedMusicians m)
            		{
            			if (m is RankedMusicians.$$ r)
                        {
                        }
            		}
            	}
            }
            """, [
            ItemExpectation.Exists("BillyJoel"),
            ItemExpectation.Exists("EveryoneElse"),
            ItemExpectation.Absent("Equals"),
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49072")]
    public Task EnumMemberAfterPropertyPatternMatch()
        => VerifyExpectedItemsAsync("""
            namespace N
            {
            	enum RankedMusicians
            	{
            		BillyJoel,
            		EveryoneElse
            	}

            	class C
            	{
                    public RankedMusicians R;

            		void M(C m)
            		{
            			if (m is { R: RankedMusicians.$$
            		}
            	}
            }
            """, [
            ItemExpectation.Exists("BillyJoel"),
            ItemExpectation.Exists("EveryoneElse"),
            ItemExpectation.Absent("Equals"),
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49072")]
    public async Task ChildClassAfterPatternMatch()
    {
        var markup =
            """
            namespace N
            {
            	public class D { public class E { } }

            	class C
            	{
            		void M(object m)
            		{
            			if (m is D.$$
            		}
            	}
            }
            """;
        // VerifyItemExistsAsync also tests with the item typed.
        await VerifyItemExistsAsync(markup, "E");
        await VerifyItemIsAbsentAsync(markup, "Equals");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49072")]
    public Task EnumMemberAfterBinaryExpression()
        => VerifyExpectedItemsAsync("""
            namespace N
            {
            	enum RankedMusicians
            	{
            		BillyJoel,
            		EveryoneElse
            	}

            	class C
            	{
            		void M(RankedMusicians m)
            		{
            			if (m == RankedMusicians.$$
            		}
            	}
            }
            """, [
            ItemExpectation.Exists("BillyJoel"),
            ItemExpectation.Exists("EveryoneElse"),
            ItemExpectation.Absent("Equals"),
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49072")]
    public Task EnumMemberAfterBinaryExpressionWithDeclaration()
        => VerifyExpectedItemsAsync("""
            namespace N
            {
            	enum RankedMusicians
            	{
            		BillyJoel,
            		EveryoneElse
            	}

            	class C
            	{
            		void M(RankedMusicians m)
            		{
            			if (m == RankedMusicians.$$ r)
                        {
                        }
            		}
            	}
            }
            """, [
            ItemExpectation.Exists("BillyJoel"),
            ItemExpectation.Exists("EveryoneElse"),
            ItemExpectation.Absent("Equals"),
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49609")]
    public Task ObsoleteOverloadsAreSkippedIfNonObsoleteOverloadIsAvailable()
        => VerifyItemExistsAsync("""
            public class C
            {
                [System.Obsolete]
                public void M() { }

                public void M(int i) { }

                public void Test()
                {
                    this.$$
                }
            }
            """, "M", expectedDescriptionOrNull: $"void C.M(int i) (+ 1 {FeaturesResources.overload})");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49609")]
    public Task FirstObsoleteOverloadIsUsedIfAllOverloadsAreObsolete()
        => VerifyItemExistsAsync("""
            public class C
            {
                [System.Obsolete]
                public void M() { }

                [System.Obsolete]
                public void M(int i) { }

                public void Test()
                {
                    this.$$
                }
            }
            """, "M", expectedDescriptionOrNull: $"[{CSharpFeaturesResources.deprecated}] void C.M() (+ 1 {FeaturesResources.overload})");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49609")]
    public Task IgnoreCustomObsoleteAttribute()
        => VerifyItemExistsAsync("""
            public class ObsoleteAttribute: System.Attribute
            {
            }

            public class C
            {
                [Obsolete]
                public void M() { }

                public void M(int i) { }

                public void Test()
                {
                    this.$$
                }
            }
            """, "M", expectedDescriptionOrNull: $"void C.M() (+ 1 {FeaturesResources.overload})");

    [InlineData("int", "")]
    [InlineData("int[]", "int a")]
    [Theory, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)]
    public async Task TestTargetTypeCompletionDescription(string targetType, string expectedParameterList)
    {
        // Check the description displayed is based on symbol matches targeted type
        ShowTargetTypedCompletionFilter = true;
        await VerifyItemExistsAsync(
            $$"""
            public class C
            {
                bool Bar(int a, int b) => false;
                int Bar() => 0;
                int[] Bar(int a) => null;

                bool N({{targetType}} x) => true;

                void M(C c)
                {
                    N(c.$$);
                }
            }
            """, "Bar",
            expectedDescriptionOrNull: $"{targetType} C.Bar({expectedParameterList}) (+{NonBreakingSpaceString}2{NonBreakingSpaceString}{FeaturesResources.overloads_})",
            matchingFilters: [FilterSet.MethodFilter, FilterSet.TargetTypedFilter]);
    }

    [InlineData("IGoo", new string[] { "Goo", "GooDerived", "GooGeneric" })]
    [InlineData("IGoo[]", new string[] { "IGoo", "IGooGeneric", "Goo", "GooAbstract", "GooDerived", "GooGeneric" })]
    [InlineData("IGooGeneric<int>", new string[] { "GooGeneric" })]
    [InlineData("IGooGeneric<int>[]", new string[] { "IGooGeneric", "GooGeneric" })]
    [InlineData("IOther", new string[] { })]
    [InlineData("Goo", new string[] { "Goo" })]
    [InlineData("GooAbstract", new string[] { "GooDerived" })]
    [InlineData("GooDerived", new string[] { "GooDerived" })]
    [InlineData("GooGeneric<int>", new string[] { "GooGeneric" })]
    [InlineData("object", new string[] { "C", "Goo", "GooDerived", "GooGeneric" })]
    [Theory, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)]
    public async Task TestTargetTypeCompletionInCreationContext(string targetType, string[] expectedItems)
    {
        ShowTargetTypedCompletionFilter = true;

        var markup =
            $$"""
            interface IGoo { }
            interface IGooGeneric<T> : IGoo { }
            interface IOther { }
            class Goo : IGoo { }
            abstract class GooAbstract : IGoo { }
            class GooDerived : GooAbstract { }
            class GooGeneric<T> : IGooGeneric<T> { }
            
            class C
            {
                void M1({{targetType}} arg) { }

                void M2()
                    => M1(new $$);
            }
            """;

        (string Name, bool IsClass, string? DisplaySuffix)[] types = [
            ("IGoo", false, null),
            ("IGooGeneric", false, "<>"),
            ("IOther", false, null),
            ("Goo", true, null),
            ("GooAbstract", true, null),
            ("GooDerived", true, null),
            ("GooGeneric", true, "<>"),
            ("C", true, null)
        ];

        foreach (var item in types.Where(t => t.IsClass && expectedItems.Contains(t.Name)))
            await VerifyItemExistsAsync(markup, item.Name, matchingFilters: [FilterSet.ClassFilter, FilterSet.TargetTypedFilter], displayTextSuffix: item.DisplaySuffix);

        foreach (var item in types.Where(t => t.IsClass && !expectedItems.Contains(t.Name)))
            await VerifyItemExistsAsync(markup, item.Name, matchingFilters: [FilterSet.ClassFilter], displayTextSuffix: item.DisplaySuffix);

        foreach (var item in types.Where(t => !t.IsClass && expectedItems.Contains(t.Name)))
            await VerifyItemExistsAsync(markup, item.Name, matchingFilters: [FilterSet.InterfaceFilter, FilterSet.TargetTypedFilter], displayTextSuffix: item.DisplaySuffix);

        foreach (var item in types.Where(t => !t.IsClass && !expectedItems.Contains(t.Name)))
            await VerifyItemExistsAsync(markup, item.Name, matchingFilters: [FilterSet.InterfaceFilter], displayTextSuffix: item.DisplaySuffix);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestTypesNotSuggestedInDeclarationDeconstruction()
        => VerifyItemIsAbsentAsync("""
            class C
            {
                int M()
                {
                    var (x, $$) = (0, 0);
                }
            }
            """, "C");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestTypesSuggestedInMixedDeclarationAndAssignmentInDeconstruction()
        => VerifyItemExistsAsync("""
            class C
            {
                int M()
                {
                    (x, $$) = (0, 0);
                }
            }
            """, "C");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestLocalDeclaredBeforeDeconstructionSuggestedInMixedDeclarationAndAssignmentInDeconstruction()
        => VerifyItemExistsAsync("""
            class C
            {
                int M()
                {
                    int y;
                    (var x, $$) = (0, 0);
                }
            }
            """, "y");

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/53930")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/64733")]
    public Task TestTypeParameterConstrainedToInterfaceWithStatics()
        => VerifyExpectedItemsAsync("""
            interface I1
            {
                static void M0();
                static abstract void M1();
                abstract static int P1 { get; set; }
                abstract static event System.Action E1;
            }

            interface I2
            {
                static abstract void M2();
                static virtual void M3() { }
            }

            class Test
            {
                void M<T>(T x) where T : I1, I2
                {
                    T.$$
                }
            }
            """, [
            ItemExpectation.Absent("M0"),

            ItemExpectation.Exists("M1"),
            ItemExpectation.Exists("M2"),
            ItemExpectation.Exists("M3"),
            ItemExpectation.Exists("P1"),
            ItemExpectation.Exists("E1")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58081")]
    public Task CompletionOnPointerParameter()
        => VerifyExpectedItemsAsync("""
            struct TestStruct
            {
                public int X;
                public int Y { get; }
                public void Method() { }
            }

            unsafe class Test
            {
                void TestMethod(TestStruct* a)
                {
                    a->$$
                }
            }
            """, [
            ItemExpectation.Exists("X"),
            ItemExpectation.Exists("Y"),
            ItemExpectation.Exists("Method"),
            ItemExpectation.Exists("ToString")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58081")]
    public Task CompletionOnAwaitedPointerParameter()
        => VerifyExpectedItemsAsync("""
            struct TestStruct
            {
                public int X;
                public int Y { get; }
                public void Method() { }
            }

            unsafe class Test
            {
                async void TestMethod(TestStruct* a)
                {
                    await a->$$
                }
            }
            """, [
            ItemExpectation.Exists("X"),
            ItemExpectation.Exists("Y"),
            ItemExpectation.Exists("Method"),
            ItemExpectation.Exists("ToString")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58081")]
    public Task CompletionOnLambdaPointerParameter()
        => VerifyExpectedItemsAsync("""
            struct TestStruct
            {
                public int X;
                public int Y { get; }
                public void Method() { }
            }

            unsafe class Test
            {
                delegate void TestLambda(TestStruct* a);

                TestLambda TestMethod()
                {
                    return a => a->$$
                }
            }
            """, [
            ItemExpectation.Exists("X"),
            ItemExpectation.Exists("Y"),
            ItemExpectation.Exists("Method"),
            ItemExpectation.Exists("ToString")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58081")]
    public Task CompletionOnOverloadedLambdaPointerParameter()
        => VerifyExpectedItemsAsync("""
            struct TestStruct1
            {
                public int X;
            }

            struct TestStruct2
            {
                public int Y;
            }

            unsafe class Test
            {
                delegate void TestLambda1(TestStruct1* a);
                delegate void TestLambda2(TestStruct2* a);

                void Overloaded(TestLambda1 lambda)
                {
                }

                void Overloaded(TestLambda2 lambda)
                {
                }

                void TestMethod()
                    => Overloaded(a => a->$$);
            }
            """, [
            ItemExpectation.Exists("X"),
            ItemExpectation.Exists("Y")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58081")]
    public Task CompletionOnOverloadedLambdaPointerParameterWithExplicitType()
        => VerifyExpectedItemsAsync("""
            struct TestStruct1
            {
                public int X;
            }

            struct TestStruct2
            {
                public int Y;
            }

            unsafe class Test
            {
                delegate void TestLambda1(TestStruct1* a);
                delegate void TestLambda2(TestStruct2* a);

                void Overloaded(TestLambda1 lambda)
                {
                }

                void Overloaded(TestLambda2 lambda)
                {
                }

                void TestMethod()
                    => Overloaded((TestStruct1* a) => a->$$);
            }
            """, [
            ItemExpectation.Exists("X"),
            ItemExpectation.Absent("Y")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58081")]
    public Task CompletionOnPointerParameterWithSimpleMemberAccess()
        => VerifyItemIsAbsentAsync("""
            struct TestStruct
            {
                public int X;
                public int Y { get; }
                public void Method() { }
            }

            unsafe class Test
            {
                void TestMethod(TestStruct* a)
                {
                    a.$$
                }
            }
            """, "X");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58081")]
    public Task CompletionOnOverloadedLambdaPointerParameterWithSimpleMemberAccess()
        => VerifyExpectedItemsAsync("""
            struct TestStruct1
            {
                public int X;
            }

            struct TestStruct2
            {
                public int Y;
            }

            unsafe class Test
            {
                delegate void TestLambda1(TestStruct1* a);
                delegate void TestLambda2(TestStruct2* a);

                void Overloaded(TestLambda1 lambda)
                {
                }

                void Overloaded(TestLambda2 lambda)
                {
                }

                void TestMethod()
                    => Overloaded(a => a.$$);
            }
            """, [
            ItemExpectation.Absent("X"),
            ItemExpectation.Absent("Y")
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58081")]
    public Task CompletionOnOverloadedLambdaPointerParameterWithSimpleMemberAccessAndExplicitType()
        => VerifyExpectedItemsAsync("""
            struct TestStruct1
            {
                public int X;
            }

            struct TestStruct2
            {
                public int Y;
            }

            unsafe class Test
            {
                delegate void TestLambda1(TestStruct1* a);
                delegate void TestLambda2(TestStruct2* a);

                void Overloaded(TestLambda1 lambda)
                {
                }

                void Overloaded(TestLambda2 lambda)
                {
                }

                void TestMethod()
                    => Overloaded((TestStruct1* a) => a.$$);
            }
            """, [
            ItemExpectation.Absent("X"),
            ItemExpectation.Absent("Y")
        ]);

    [InlineData("m.MyObject?.$$MyValue!!()")]
    [InlineData("m.MyObject?.$$MyObject!.MyValue!!()")]
    [InlineData("m.MyObject?.MyObject!.$$MyValue!!()")]
    [Theory]
    [WorkItem("https://github.com/dotnet/roslyn/issues/59714")]
    public Task OptionalExclamationsAfterConditionalAccessShouldBeHandled(string conditionalAccessExpression)
        => VerifyItemExistsAsync($$"""
            class MyClass
            {
                public MyClass? MyObject { get; set; }
                public MyClass? MyValue() => null;

                public static void F()
                {
                    var m = new MyClass();
                    {{conditionalAccessExpression}};
                }
            }
            """, "MyValue");

    [Fact]
    public async Task TopLevelSymbolsAvailableAtTopLevel()
    {
        var source = $$"""
            int goo;

            void Bar()
            {
            }

            $$

            class MyClass
            {
                public static void F()
                {
                }
            }
            """;
        await VerifyItemExistsAsync(source, "goo");
        await VerifyItemExistsAsync(source, "Bar");
    }

    [Fact]
    public async Task TopLevelSymbolsAvailableInsideTopLevelFunction()
    {
        var source = $$"""
            int goo;

            void Bar()
            {
                $$
            }

            class MyClass
            {
                public static void F()
                {
                }
            }
            """;
        await VerifyItemExistsAsync(source, "goo");
        await VerifyItemExistsAsync(source, "Bar");
    }

    [Fact]
    public async Task TopLevelSymbolsNotAvailableInOtherTypes()
    {
        var source = $$"""
            int goo;

            void Bar()
            {
            }

            class MyClass
            {
                public static void F()
                {
                    $$
                }
            }
            """;
        await VerifyItemIsAbsentAsync(source, "goo");
        await VerifyItemIsAbsentAsync(source, "Bar");
    }

    [Fact]
    public async Task ParameterAvailableInMethodAttributeNameof()
    {
        var source = """
            class C
            {
                [Some(nameof(p$$))]
                void M(int parameter) { }
            }
            """;
        await VerifyItemExistsAsync(MakeMarkup(source), "parameter");

        await VerifyItemExistsAsync(MakeMarkup(source, languageVersion: LanguageVersion.CSharp10), "parameter");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60812")]
    public Task ParameterNotAvailableInMethodAttributeNameofWithNoArgument()
        => VerifyItemExistsAsync(MakeMarkup("""
            class C
            {
                [Some(nameof($$))]
                void M(int parameter) { }
            }
            """), "parameter");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66982")]
    public Task CapturedParameters1()
        => VerifyItemIsAbsentAsync(MakeMarkup("""
            class C
            {
                void M(string args)
                {
                    static void LocalFunc()
                    {
                        Console.WriteLine($$);
                    }
                }
            }
            """), "args");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66982")]
    public Task CapturedParameters2()
        => VerifyItemExistsAsync(MakeMarkup("""
            class C
            {
                void M(string args)
                {
                    static void LocalFunc()
                    {
                        Console.WriteLine(nameof($$));
                    }
                }
            }
            """), "args");

    [Fact]
    public Task ParameterAvailableInMethodParameterAttributeNameof()
        => VerifyItemExistsAsync(MakeMarkup("""
            class C
            {
                void M([Some(nameof(p$$))] int parameter) { }
            }
            """), "parameter");

    [Fact]
    public async Task ParameterAvailableInLocalFunctionAttributeNameof()
    {
        var source = """
            class C
            {
                void M()
                {
                    [Some(nameof(p$$))]
                    void local(int parameter) { }
                }
            }
            """;
        await VerifyItemExistsAsync(MakeMarkup(source), "parameter");

        await VerifyItemExistsAsync(MakeMarkup(source, languageVersion: LanguageVersion.CSharp10), "parameter");
    }

    [Fact]
    public async Task ParameterAvailableInLocalFunctionParameterAttributeNameof()
    {
        var source = """
            class C
            {
                void M()
                {
                    void local([Some(nameof(p$$))] int parameter) { }
                }
            }
            """;
        await VerifyItemExistsAsync(MakeMarkup(source), "parameter");

        await VerifyItemExistsAsync(MakeMarkup(source, languageVersion: LanguageVersion.CSharp10), "parameter");
    }

    [Fact]
    public async Task ParameterAvailableInLambdaAttributeNameof()
    {
        var source = """
            class C
            {
                void M()
                {
                    _ = [Some(nameof(p$$))] void(int parameter) => { };
                }
            }
            """;
        await VerifyItemExistsAsync(MakeMarkup(source), "parameter");

        await VerifyItemExistsAsync(MakeMarkup(source, languageVersion: LanguageVersion.CSharp10), "parameter");
    }

    [Fact]
    public async Task ParameterAvailableInLambdaParameterAttributeNameof()
    {
        var source = """
            class C
            {
                void M()
                {
                    _ = void([Some(nameof(p$$))] int parameter) => { };
                }
            }
            """;
        await VerifyItemExistsAsync(MakeMarkup(source), "parameter");

        await VerifyItemExistsAsync(MakeMarkup(source, languageVersion: LanguageVersion.CSharp10), "parameter");
    }

    [Fact]
    public async Task ParameterAvailableInDelegateAttributeNameof()
    {
        var source = """
            [Some(nameof(p$$))]
            delegate void MyDelegate(int parameter);
            """;
        await VerifyItemExistsAsync(MakeMarkup(source), "parameter");

        await VerifyItemExistsAsync(MakeMarkup(source, languageVersion: LanguageVersion.CSharp10), "parameter");
    }

    [Fact]
    public async Task ParameterAvailableInDelegateParameterAttributeNameof()
    {
        var source = """
            delegate void MyDelegate([Some(nameof(p$$))] int parameter);
            """;
        await VerifyItemExistsAsync(MakeMarkup(source), "parameter");

        await VerifyItemExistsAsync(MakeMarkup(source, languageVersion: LanguageVersion.CSharp10), "parameter");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64585")]
    public Task AfterRequired()
        => VerifyAnyItemExistsAsync("""
            class C
            {
                required $$
            }
            """);

    [Theory, CombinatorialData]
    public async Task AfterScopedInsideMethod(bool useRef)
    {
        var refKeyword = useRef ? "ref " : "";
        await VerifyItemExistsAsync(MakeMarkup($$"""
            class C
            {
                void M()
                {
                    scoped {{refKeyword}}$$
                }
            }

            ref struct MyRefStruct { }
            """), "MyRefStruct");
    }

    [Theory, CombinatorialData]
    public async Task AfterScopedGlobalStatement_FollowedByRefStruct(bool useRef)
    {
        var refKeyword = useRef ? "ref " : "";
        await VerifyItemExistsAsync(MakeMarkup($$"""
            scoped {{refKeyword}}$$

            ref struct MyRefStruct { }
            """), "MyRefStruct");
    }

    [Theory, CombinatorialData]
    public async Task AfterScopedGlobalStatement_FollowedByStruct(bool useRef)
    {
        var refKeyword = useRef ? "ref " : "";
        await VerifyItemExistsAsync(MakeMarkup($$"""
            using System;

            scoped {{refKeyword}}$$

            struct S { }
            """), "ReadOnlySpan", displayTextSuffix: "<>");
    }

    [Theory, CombinatorialData]
    public async Task AfterScopedGlobalStatement_FollowedByPartialStruct(bool useRef)
    {
        var refKeyword = useRef ? "ref " : "";
        await VerifyItemExistsAsync(MakeMarkup($$"""
            using System;

            scoped {{refKeyword}}$$

            partial struct S { }
            """), "ReadOnlySpan", displayTextSuffix: "<>");
    }

    [Theory, CombinatorialData]
    public async Task AfterScopedGlobalStatement_NotFollowedByType(bool useRef)
    {
        var refKeyword = useRef ? "ref " : "";
        await VerifyItemExistsAsync(MakeMarkup($"""
            using System;

            scoped {refKeyword}$$
            """), "ReadOnlySpan", displayTextSuffix: "<>");
    }

    [Fact]
    public Task AfterScopedInParameter()
        => VerifyItemExistsAsync(MakeMarkup("""
            class C
            {
                void M(scoped $$)
                {
                }
            }

            ref struct MyRefStruct { }
            """), "MyRefStruct");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65020")]
    public Task DoNotProvideMemberOnSystemVoid()
        => VerifyItemIsAbsentAsync(MakeMarkup("""
            class C
            {
                void M1(){}
                void M2()
                {
                    this.M1().$$
                }
            }

            public static class Extension
            {
                public static bool ExtMethod(this object x) => false;
            }
            """), "ExtMethod");

    [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
    public Task EnumBaseList1(string underlyingType)
        => VerifyExpectedItemsAsync("enum E : $$", [
            ItemExpectation.Exists("System"),

            // Not accessible in the given context
            ItemExpectation.Absent(underlyingType),
        ]);

    [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
    public async Task EnumBaseList2(string underlyingType)
    {
        var source = """
            enum E : $$

            class System
            {
            }
            """;

        // class `System` shadows the namespace in regular source
        await VerifyItemIsAbsentAsync(source, "System", sourceCodeKind: SourceCodeKind.Regular);

        // Not accessible in the given context
        await VerifyItemIsAbsentAsync(source, underlyingType);
    }

    [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
    public Task EnumBaseList3(string underlyingType)
        => VerifyExpectedItemsAsync("""
            using System;

            enum E : $$
            """, [
            ItemExpectation.Exists("System"),

            ItemExpectation.Exists(underlyingType),

            // Verify that other things from `System` namespace are not present
            ItemExpectation.Absent("Console"),
            ItemExpectation.Absent("Action"),
            ItemExpectation.Absent("DateTime")
        ]);

    [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
    public Task EnumBaseList4(string underlyingType)
        => VerifyExpectedItemsAsync("""
            namespace MyNamespace
            {
            }

            enum E : global::$$
            """, [
            ItemExpectation.Absent("E"),

            ItemExpectation.Exists("System"),
            ItemExpectation.Absent("MyNamespace"),

            // Not accessible in the given context
            ItemExpectation.Absent(underlyingType)
        ]);

    [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
    public Task EnumBaseList5(string underlyingType)
        => VerifyExpectedItemsAsync("enum E : System.$$", [
            ItemExpectation.Absent("System"),

            ItemExpectation.Exists(underlyingType),

            // Verify that other things from `System` namespace are not present
            ItemExpectation.Absent("Console"),
            ItemExpectation.Absent("Action"),
            ItemExpectation.Absent("DateTime")
        ]);

    [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
    public Task EnumBaseList6(string underlyingType)
        => VerifyExpectedItemsAsync("enum E : global::System.$$", [
            ItemExpectation.Absent("System"),

            ItemExpectation.Exists(underlyingType),

            // Verify that other things from `System` namespace are not present
            ItemExpectation.Absent("Console"),
            ItemExpectation.Absent("Action"),
            ItemExpectation.Absent("DateTime")
        ]);

    [Fact]
    public Task EnumBaseList7()
        => VerifyNoItemsExistAsync("enum E : System.Collections.Generic.$$");

    [Fact]
    public Task EnumBaseList8()
        => VerifyNoItemsExistAsync("""
            namespace MyNamespace
            {
                namespace System {}
                public struct Byte {}
                public struct SByte {}
                public struct Int16 {}
                public struct UInt16 {}
                public struct Int32 {}
                public struct UInt32 {}
                public struct Int64 {}
                public struct UInt64 {}
            }

            enum E : MyNamespace.$$
            """);

    [Fact]
    public Task EnumBaseList9()
        => VerifyItemExistsAsync("""
            using MySystem = System;

            enum E : $$
            """, "MySystem");

    [Fact]
    public Task EnumBaseList10()
        => VerifyItemIsAbsentAsync("""
            using MySystem = System;

            enum E : global::$$
            """, "MySystem");

    [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
    public Task EnumBaseList11(string underlyingType)
        => VerifyExpectedItemsAsync("""
            using MySystem = System;

            enum E : MySystem.$$
            """, [
            ItemExpectation.Absent("System"),
            ItemExpectation.Absent("MySystem"),

            ItemExpectation.Exists(underlyingType),

            // Verify that other things from `System` namespace are not present
            ItemExpectation.Absent("Console"),
            ItemExpectation.Absent("Action"),
            ItemExpectation.Absent("DateTime")
        ]);

    [Fact]
    public Task EnumBaseList12()
        => VerifyNoItemsExistAsync("""
            using MySystem = System;

            enum E : global::MySystem.$$
            """);

    [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
    public Task EnumBaseList13(string underlyingType)
        => VerifyItemExistsAsync($"""
            using My{underlyingType} = System.{underlyingType};

            enum E : $$
            """, $"My{underlyingType}");

    [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
    public Task EnumBaseList14(string underlyingType)
        => VerifyItemIsAbsentAsync($"""
            using My{underlyingType} = System.{underlyingType};

            enum E : global::$$
            """, $"My{underlyingType}");

    [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
    public Task EnumBaseList15(string underlyingType)
        => VerifyItemIsAbsentAsync($"""
            using My{underlyingType} = System.{underlyingType};

            enum E : System.$$
            """, $"My{underlyingType}");

    [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
    public Task EnumBaseList16(string underlyingType)
        => VerifyItemIsAbsentAsync($"""
            using MySystem = System;
            using My{underlyingType} = System.{underlyingType};

            enum E : MySystem.$$
            """, $"My{underlyingType}");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66903")]
    public Task InRangeExpression()
        => VerifyExpectedItemsAsync("""
            class C
            {
                const int Test = 1;

                void M(string s)
                {
                    var endIndex = 1;
                    var substr = s[1..$$];
                }
            }
            """, [
            ItemExpectation.Exists("endIndex"),
            ItemExpectation.Exists("Test"),
            ItemExpectation.Exists("C"),
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66903")]
    public Task InRangeExpression_WhitespaceAfterDotDotToken()
        => VerifyExpectedItemsAsync("""
            class C
            {
                const int Test = 1;

                void M(string s)
                {
                    var endIndex = 1;
                    var substr = s[1.. $$];
                }
            }
            """, [
            ItemExpectation.Exists("endIndex"),
            ItemExpectation.Exists("Test"),
            ItemExpectation.Exists("C"),
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25572")]
    public Task PropertyAndGenericExtensionMethodCandidates()
        => VerifyExpectedItemsAsync("""
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    int foo;
                    List<int> list;
                    if (list.Count < $$)
                    {
                    }
                }
            }
            """, [
            ItemExpectation.Exists("foo"),
            ItemExpectation.Exists("M"),
            ItemExpectation.Exists("System"),
            ItemExpectation.Absent("Int32"),
        ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25572")]
    public Task GenericWithNonGenericOverload()
        => VerifyExpectedItemsAsync("""
            class C
            {
                void M(C other)
                {
                    if (other.A < $$)
                    {
                    }
                }

                void A() { }
                void A<T>() { }
            }
            """, [
            ItemExpectation.Exists("System"),
            ItemExpectation.Exists("C"),
            ItemExpectation.Absent("other"),
        ]);

    public static readonly IEnumerable<object[]> PatternMatchingPrecedingPatterns = new object[][]
    {
        ["is"],
        ["is ("],
        ["is not"],
        ["is (not"],
        ["is not ("],
        ["is Constants.A and"],
        ["is (Constants.A and"],
        ["is Constants.A and ("],
        ["is Constants.A and not"],
        ["is (Constants.A and not"],
        ["is Constants.A and (not"],
        ["is Constants.A and not ("],
        ["is Constants.A or"],
        ["is (Constants.A or"],
        ["is Constants.A or ("],
        ["is Constants.A or not"],
        ["is (Constants.A or not"],
        ["is Constants.A or (not"],
        ["is Constants.A or not ("],
    };

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/70226")]
    [MemberData(nameof(PatternMatchingPrecedingPatterns))]
    public async Task PatternMatching_01(string precedingPattern)
    {
        var expression = $"return input {precedingPattern} Constants.$$";
        var source = WrapPatternMatchingSource(expression);

        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("A"),
            ItemExpectation.Exists("B"),
            ItemExpectation.Exists("C"),
            ItemExpectation.Absent("D"),
            ItemExpectation.Absent("M"),
            ItemExpectation.Exists("R"),
        ]);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/70226")]
    [MemberData(nameof(PatternMatchingPrecedingPatterns))]
    public async Task PatternMatching_02(string precedingPattern)
    {
        var expression = $"return input {precedingPattern} Constants.R.$$";
        var source = WrapPatternMatchingSource(expression);

        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("A"),
            ItemExpectation.Exists("B"),
            ItemExpectation.Absent("C"),
            ItemExpectation.Absent("D"),
            ItemExpectation.Absent("M"),
            ItemExpectation.Absent("R"),
        ]);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/70226")]
    [MemberData(nameof(PatternMatchingPrecedingPatterns))]
    public async Task PatternMatching_03(string precedingPattern)
    {
        var expression = $"return input {precedingPattern} $$";
        var source = WrapPatternMatchingSource(expression);

        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("C"),
            ItemExpectation.Exists("Constants"),
            ItemExpectation.Exists("System"),
        ]);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/70226")]
    [MemberData(nameof(PatternMatchingPrecedingPatterns))]
    public async Task PatternMatching_04(string precedingPattern)
    {
        var expression = $"return input {precedingPattern} global::$$";
        var source = WrapPatternMatchingSource(expression);

        // In scripts, we also get a Script class containing our defined types
        await VerifyExpectedItemsAsync(source,
            [
                ItemExpectation.Exists("C"),
                ItemExpectation.Exists("Constants"),
            ],
            sourceCodeKind: SourceCodeKind.Regular);
        await VerifyItemExistsAsync(source, "System");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70226")]
    public async Task PatternMatching_05()
    {
        var expression = $"return $$ is Constants.A";
        var source = WrapPatternMatchingSource(expression);

        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("input"),
            ItemExpectation.Exists("Constants"),
            ItemExpectation.Exists("C"),
            ItemExpectation.Exists("M"),
        ]);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/70226")]
    [MemberData(nameof(PatternMatchingPrecedingPatterns))]
    public async Task PatternMatching_06(string precedingPattern)
    {
        var expression = $"return input {precedingPattern} Constants.$$.A";
        var source = WrapPatternMatchingSource(expression);

        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("A"),
            ItemExpectation.Exists("B"),
            ItemExpectation.Exists("C"),
            ItemExpectation.Absent("D"),
            ItemExpectation.Absent("M"),
            ItemExpectation.Exists("R"),
        ]);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/70226")]
    [MemberData(nameof(PatternMatchingPrecedingPatterns))]
    public async Task PatternMatching_07(string precedingPattern)
    {
        var expression = $"return input {precedingPattern} Enum.$$";
        var source = WrapPatternMatchingSource(expression);

        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("A"),
            ItemExpectation.Exists("B"),
            ItemExpectation.Exists("C"),
            ItemExpectation.Exists("D"),
            ItemExpectation.Absent("M"),
            ItemExpectation.Absent("R"),
        ]);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/70226")]
    [MemberData(nameof(PatternMatchingPrecedingPatterns))]
    public async Task PatternMatching_08(string precedingPattern)
    {
        var expression = $"return input {precedingPattern} nameof(Constants.$$";
        var source = WrapPatternMatchingSource(expression);

        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("A"),
            ItemExpectation.Exists("B"),
            ItemExpectation.Exists("C"),
            ItemExpectation.Exists("D"),
            ItemExpectation.Exists("E"),
            ItemExpectation.Exists("M"),
            ItemExpectation.Exists("R"),
            ItemExpectation.Exists("ToString"),
        ]);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/70226")]
    [MemberData(nameof(PatternMatchingPrecedingPatterns))]
    public async Task PatternMatching_09(string precedingPattern)
    {
        var expression = $"return input {precedingPattern} nameof(Constants.R.$$";
        var source = WrapPatternMatchingSource(expression);

        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("A"),
            ItemExpectation.Exists("B"),
            ItemExpectation.Exists("D"),
            ItemExpectation.Exists("E"),
            ItemExpectation.Exists("M"),
        ]);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/70226")]
    [MemberData(nameof(PatternMatchingPrecedingPatterns))]
    public async Task PatternMatching_10(string precedingPattern)
    {
        var expression = $"return input {precedingPattern} nameof(Constants.$$.A";
        var source = WrapPatternMatchingSource(expression);

        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("A"),
            ItemExpectation.Exists("B"),
            ItemExpectation.Exists("C"),
            ItemExpectation.Exists("D"),
            ItemExpectation.Exists("E"),
            ItemExpectation.Exists("M"),
            ItemExpectation.Exists("R"),
        ]);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/70226")]
    [MemberData(nameof(PatternMatchingPrecedingPatterns))]
    public async Task PatternMatching_11(string precedingPattern)
    {
        var expression = $"return input {precedingPattern} [Constants.R(Constants.$$, nameof(Constants.R))]";
        var source = WrapPatternMatchingSource(expression);

        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("A"),
            ItemExpectation.Exists("B"),
            ItemExpectation.Exists("C"),
            ItemExpectation.Absent("D"),
            ItemExpectation.Absent("M"),
            ItemExpectation.Exists("R"),
        ]);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/70226")]
    [MemberData(nameof(PatternMatchingPrecedingPatterns))]
    public async Task PatternMatching_12(string precedingPattern)
    {
        var expression = $"return input {precedingPattern} [Constants.R(Constants.$$), nameof(Constants.R)]";
        var source = WrapPatternMatchingSource(expression);

        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("A"),
            ItemExpectation.Exists("B"),
            ItemExpectation.Exists("C"),
            ItemExpectation.Absent("D"),
            ItemExpectation.Absent("M"),
            ItemExpectation.Exists("R"),
        ]);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/70226")]
    [MemberData(nameof(PatternMatchingPrecedingPatterns))]
    public async Task PatternMatching_13(string precedingPattern)
    {
        var expression = $"return input {precedingPattern} [Constants.R(Constants.A) {{ P.$$: Constants.A, InstanceProperty: 153 }}, nameof(Constants.R)]";
        var source = WrapPatternMatchingSource(expression);

        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("Length"),
            ItemExpectation.Absent("Constants"),
        ]);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/70226")]
    [MemberData(nameof(PatternMatchingPrecedingPatterns))]
    public async Task PatternMatching_14(string precedingPattern)
    {
        var expression = $"return input {precedingPattern} [Constants.R(Constants.A) {{ P: $$ }}, nameof(Constants.R)]";
        var source = WrapPatternMatchingSource(expression);

        await VerifyExpectedItemsAsync(source, [
            ItemExpectation.Exists("Constants"),
            ItemExpectation.Absent("InstanceProperty"),
            ItemExpectation.Absent("P"),
        ]);
    }

    private static string WrapPatternMatchingSource(string returnedExpression)
    {
        return $$"""
            class C
            {
                bool M(string input)
                {
            {{returnedExpression}}
                }
            }

            public static class Constants
            {
                public const string
                    A = "a",
                    B = "b",
                    C = "c";

                public static readonly string D = "d";
                public static string E => "e";

                public static void M() { }

                public record R(string P)
                {
                    public const string
                        A = "a",
                        B = "b";

                    public static readonly string D = "d";
                    public static string E => "e";

                    public int InstanceProperty { get; set; }

                    public static void M() { }
                }
            }

            public enum Enum { A, B, C, D }
            """;
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72457")]
    public async Task ConstrainedGenericExtensionMethods_01()
    {
        var markup = """
            using System.Collections.Generic;
            using System.Linq;

            namespace Extensions;

            public static class GenericExtensions
            {
                public static string FirstOrDefaultOnHashSet<T>(this T s)
                    where T : HashSet<string>
                {
                    return s.FirstOrDefault();
                }
                public static string FirstOrDefaultOnList<T>(this T s)
                    where T : List<string>
                {
                    return s.FirstOrDefault();
                }
            }

            class C
            {
                void M()
                {
                    var list = new List<string>();
                    list.$$
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "FirstOrDefaultOnList", displayTextSuffix: "<>");
        await VerifyItemIsAbsentAsync(markup, "FirstOrDefaultOnHashSet", displayTextSuffix: "<>");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72457")]
    public async Task ConstrainedGenericExtensionMethods_02()
    {
        var markup = """
            using System.Collections.Generic;
            using System.Linq;

            namespace Extensions;

            public static class GenericExtensions
            {
                public static string FirstOrDefaultOnHashSet<T>(this T s)
                    where T : HashSet<string>
                {
                    return s.FirstOrDefault();
                }
                public static string FirstOrDefaultOnList<T>(this T s)
                    where T : List<string>
                {
                    return s.FirstOrDefault();
                }

                public static bool HasFirstNonNullItemOnList<T>(this T s)
                    where T : List<string>
                {
                    return s.$$
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "FirstOrDefaultOnList", displayTextSuffix: "<>");
        await VerifyItemIsAbsentAsync(markup, "FirstOrDefaultOnHashSet", displayTextSuffix: "<>");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72457")]
    public async Task ConstrainedGenericExtensionMethods_SelfGeneric01()
    {
        var markup = """
            using System.Collections.Generic;
            using System.Linq;

            namespace Extensions;

            public static class GenericExtensions
            {
                public static T FirstOrDefaultOnHashSet<T>(this T s)
                    where T : HashSet<T>
                {
                    return s.FirstOrDefault();
                }
                public static T FirstOrDefaultOnList<T>(this T s)
                    where T : List<T>
                {
                    return s.FirstOrDefault();
                }
            }

            public class ListExtension<T> : List<ListExtension<T>>
                where T : List<T>
            {
                public void Method()
                {
                    this.$$
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "FirstOrDefaultOnList", displayTextSuffix: "<>");
        await VerifyItemIsAbsentAsync(markup, "FirstOrDefaultOnHashSet", displayTextSuffix: "<>");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72457")]
    public async Task ConstrainedGenericExtensionMethods_SelfGeneric02()
    {
        var markup = """
            using System.Collections.Generic;
            using System.Linq;

            namespace Extensions;

            public static class GenericExtensions
            {
                public static T FirstOrDefaultOnHashSet<T>(this T s)
                    where T : HashSet<T>
                {
                    return s.FirstOrDefault();
                }
                public static T FirstOrDefaultOnList<T>(this T s)
                    where T : List<T>
                {
                    return s.FirstOrDefault();
                }

                public static bool HasFirstNonNullItemOnList<T>(this T s)
                    where T : List<T>
                {
                    return s.$$
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "FirstOrDefaultOnList", displayTextSuffix: "<>");
        await VerifyItemIsAbsentAsync(markup, "FirstOrDefaultOnHashSet", displayTextSuffix: "<>");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72457")]
    public async Task ConstrainedGenericExtensionMethods_SelfGeneric03()
    {
        var markup = """
            namespace Extensions;

            public interface IBinaryInteger<T>
            {
                public static T AdditiveIdentity { get; }
            }

            public static class GenericExtensions
            {
                public static T AtLeastAdditiveIdentity<T>(this T s)
                    where T : IBinaryInteger<T>
                {
                    return T.AdditiveIdentity > s ? s : T.AdditiveIdentity;
                }

                public static T Method<T>(this T s)
                    where T : IBinaryInteger<T>
                {
                    return s.$$
                }
            }
            """;

        await VerifyItemExistsAsync(markup, "AtLeastAdditiveIdentity", displayTextSuffix: "<>");
        await VerifyItemExistsAsync(markup, "Method", displayTextSuffix: "<>");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75350")]
    public async Task SwitchExpressionEnumColorColor_01()
    {
        //lang=c#-test
        await VerifyExpectedItemsAsync("""
            public sealed record OrderModel(int Id, Status Status)
            {
                public string StatusDisplay
                {
                    get
                    {
                        return Status switch
                        {
                            Status.$$
                        };
                    }
                }
            }

            public enum Status
            {
                Undisclosed,
                Open,
                Closed,
            }
            """, [
            ItemExpectation.Exists("Undisclosed"),
            ItemExpectation.Absent("ToString"),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75350")]
    public async Task SwitchExpressionEnumColorColor_02()
    {
        //lang=c#-test
        await VerifyExpectedItemsAsync("""
            public sealed record OrderModel(int Id, Status Status)
            {
                public string StatusDisplay
                {
                    get
                    {
                        return this switch
                        {
                            { Status: Status.$$ }
                        };
                    }
                }
            }

            public enum Status
            {
                Undisclosed,
                Open,
                Closed,
            }
            """, [
            ItemExpectation.Exists("Undisclosed"),
            ItemExpectation.Absent("ToString"),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75350")]
    public async Task SwitchExpressionEnumColorColor_03()
    {
        //lang=c#-test
        await VerifyExpectedItemsAsync("""
            namespace Status;

            public sealed record OrderModel(int Id, StatusEn Status)
            {
                public string StatusDisplay
                {
                    get
                    {
                        return this switch
                        {
                            { Status: Status.$$ }
                        };
                    }
                }
            }

            public enum StatusEn
            {
                Undisclosed,
                Open,
                Closed,
            }
            """, [
            ItemExpectation.Exists("StatusEn"),
            ItemExpectation.Absent("ToString"),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75350")]
    public async Task SwitchExpressionEnumColorColor_04()
    {
        //lang=c#-test
        await VerifyExpectedItemsAsync("""
            using Status = StatusEn;

            public sealed record OrderModel(int Id, StatusEn Status)
            {
                public string StatusDisplay
                {
                    get
                    {
                        return this switch
                        {
                            { Status: Status.$$ }
                        };
                    }
                }
            }

            public enum StatusEn
            {
                Undisclosed,
                Open,
                Closed,
            }
            """, [
            ItemExpectation.Exists("Undisclosed"),
            ItemExpectation.Absent("ToString"),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75350")]
    public async Task SwitchExpressionEnumColorColor_05()
    {
        //lang=c#-test
        await VerifyExpectedItemsAsync("""
            using Status = StatusEn;

            public sealed record OrderModel(int Id, StatusEn Status)
            {
                public string StatusDisplay
                {
                    get
                    {
                        const StatusEn Status = StatusEn.Undisclosed;
                        return this switch
                        {
                            { Status: Status.$$ }
                        };
                    }
                }
            }

            public enum StatusEn
            {
                Undisclosed,
                Open,
                Closed,
            }
            """, [
            ItemExpectation.Exists("Undisclosed"),
            ItemExpectation.Absent("ToString"),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75350")]
    public async Task ConstantPatternExpressionEnumColorColor_01()
    {
        //lang=c#-test
        await VerifyExpectedItemsAsync("""
            public sealed record OrderModel(int Id, Status Status)
            {
                public string StatusDisplay
                {
                    get
                    {
                        if (Status is Status.$$)
                            ;
                    }
                }
            }

            public enum Status
            {
                Undisclosed,
                Open,
                Closed,
            }
            """, [
            ItemExpectation.Exists("Undisclosed"),
            ItemExpectation.Absent("ToString"),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75350")]
    public async Task ConstantPatternExpressionEnumColorColor_02()
    {
        //lang=c#-test
        await VerifyExpectedItemsAsync("""
            public sealed record OrderModel(int Id, Status Status)
            {
                public string StatusDisplay
                {
                    get
                    {
                        if (Status is (Status.$$)
                            ;
                    }
                }
            }

            public enum Status
            {
                Undisclosed,
                Open,
                Closed,
            }
            """, [
            ItemExpectation.Exists("Undisclosed"),
            ItemExpectation.Absent("ToString"),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75350")]
    public async Task ConstantPatternExpressionEnumColorColor_03()
    {
        //lang=c#-test
        await VerifyExpectedItemsAsync("""
            namespace Status;

            public sealed record OrderModel(int Id, StatusEn Status)
            {
                public string StatusDisplay
                {
                    get
                    {
                        if (Status is (Status.$$)
                            ;
                    }
                }
            }

            public enum StatusEn
            {
                Undisclosed,
                Open,
                Closed,
            }
            """, [
            ItemExpectation.Exists("StatusEn"),
            ItemExpectation.Absent("ToString"),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75350")]
    public async Task ConstantPatternExpressionEnumColorColor_04()
    {
        //lang=c#-test
        await VerifyExpectedItemsAsync("""
            using Status = StatusEn;

            public sealed record OrderModel(int Id, StatusEn Status)
            {
                public string StatusDisplay
                {
                    get
                    {
                        if (Status is (Status.$$)
                            ;
                    }
                }
            }

            public enum StatusEn
            {
                Undisclosed,
                Open,
                Closed,
            }
            """, [
            ItemExpectation.Exists("Undisclosed"),
            ItemExpectation.Absent("ToString"),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75350")]
    public async Task ConstantPatternExpressionEnumColorColor_05()
    {
        //lang=c#-test
        await VerifyExpectedItemsAsync("""
            using Status = StatusEn;

            public sealed record OrderModel(int Id, StatusEn Status)
            {
                public string StatusDisplay
                {
                    get
                    {
                        const StatusEn Status = StatusEn.Undisclosed;
                        if (Status is (Status.$$)
                            ;
                    }
                }
            }

            public enum StatusEn
            {
                Undisclosed,
                Open,
                Closed,
            }
            """, [
            ItemExpectation.Exists("Undisclosed"),
            ItemExpectation.Absent("ToString"),
        ]);
    }

    #region Collection expressions

    [Fact]
    public async Task TestInCollectionExpressions_BeforeFirstElementToVar()
    {
        var source = AddInsideMethod(
            """
            const int val = 3;
            var x = [$$
            """);

        await VerifyItemExistsAsync(source, "val");
    }

    [Fact]
    public async Task TestInCollectionExpressions_BeforeFirstElementToReturn()
    {
        var source =
            """
            using System;

            class C
            {
                private readonly string field = string.Empty;

                IEnumerable<string> M() => [$$
            }
            """;

        await VerifyItemExistsAsync(source, "String");
        await VerifyItemExistsAsync(source, "System");
        await VerifyItemExistsAsync(source, "field");
    }

    [Fact]
    public async Task TestInCollectionExpressions_AfterFirstElementToVar()
    {
        var source = AddInsideMethod(
            """
            const int val = 3;
            var x = [val, $$
            """);

        await VerifyItemExistsAsync(source, "val");
    }

    [Fact]
    public async Task TestInCollectionExpressions_AfterFirstElementToReturn()
    {
        var source =
            """
            using System;

            class C
            {
                private readonly string field = string.Empty;
            
                IEnumerable<string> M() => [string.Empty, $$
            }
            """;

        await VerifyItemExistsAsync(source, "String");
        await VerifyItemExistsAsync(source, "System");
        await VerifyItemExistsAsync(source, "field");
    }

    [Fact]
    public async Task TestInCollectionExpressions_SpreadBeforeFirstElementToReturn()
    {
        var source =
            """
            class C
            {
                private static readonly string[] strings = [string.Empty, "", "hello"];
            
                IEnumerable<string> M() => [.. $$
            }
            """;

        await VerifyItemExistsAsync(source, "System");
        await VerifyItemExistsAsync(source, "strings");
    }

    [Fact]
    public async Task TestInCollectionExpressions_SpreadAfterFirstElementToReturn()
    {
        var source =
            """
            class C
            {
                private static readonly string[] strings = [string.Empty, "", "hello"];
            
                IEnumerable<string> M() => [string.Empty, .. $$
            }
            """;

        await VerifyItemExistsAsync(source, "System");
        await VerifyItemExistsAsync(source, "strings");
    }

    [Fact]
    public async Task TestInCollectionExpressions_ParenAtFirstElementToReturn()
    {
        var source =
            """
            using System;

            class C
            {
                private readonly string field = string.Empty;

                IEnumerable<string> M() => [($$
            }
            """;

        await VerifyItemExistsAsync(source, "String");
        await VerifyItemExistsAsync(source, "System");
        await VerifyItemExistsAsync(source, "field");
    }

    [Fact]
    public async Task TestInCollectionExpressions_ParenAfterFirstElementToReturn()
    {
        var source =
            """
            using System;

            class C
            {
                private readonly string field = string.Empty;

                IEnumerable<string> M() => [string.Empty, ($$
            }
            """;

        await VerifyItemExistsAsync(source, "String");
        await VerifyItemExistsAsync(source, "System");
        await VerifyItemExistsAsync(source, "field");
    }

    [Fact]
    public async Task TestInCollectionExpressions_ParenSpreadAtFirstElementToReturn()
    {
        var source =
            """
            class C
            {
                private static readonly string[] strings = [string.Empty, "", "hello"];
            
                IEnumerable<string> M() => [.. ($$
            }
            """;

        await VerifyItemExistsAsync(source, "System");
        await VerifyItemExistsAsync(source, "strings");
    }

    [Fact]
    public async Task TestInCollectionExpressions_ParenSpreadAfterFirstElementToReturn()
    {
        var source =
            """
            class C
            {
                private static readonly string[] strings = [string.Empty, "", "hello"];
            
                IEnumerable<string> M() => [string.Empty, .. ($$
            }
            """;

        await VerifyItemExistsAsync(source, "System");
        await VerifyItemExistsAsync(source, "strings");
    }

    #endregion

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/74327")]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("record class")]
    [InlineData("record struct")]
    public Task RecommendedPrimaryConstructorParameters01(string typeKind)
        => VerifyExpectedItemsAsync($$"""
            namespace PrimaryConstructor;

            public {{typeKind}} Point(int X, int Y)
            {
                public static Point Parse(string line)
                {
                    $$
                }
            }
            """, [
            ItemExpectation.Absent("X"),
            ItemExpectation.Absent("Y"),
        ]);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/74327")]
    [InlineData("class")]
    [InlineData("record class")]
    public Task RecommendedPrimaryConstructorParameters02(string typeKind)
        => VerifyExpectedItemsAsync($$"""
            namespace PrimaryConstructor;

            public abstract {{typeKind}} BasePoint(int X);

            public {{typeKind}} Point(int X, int Y)
                : BasePoint(X)
            {
                public static Point Parse(string line)
                {
                    $$
                }
            }
            """, [
            ItemExpectation.Absent("X"),
            ItemExpectation.Absent("Y"),
        ]);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/74327")]
    [InlineData("class")]
    [InlineData("record class")]
    public Task RecommendedPrimaryConstructorParameters03(string typeKind)
        => VerifyExpectedItemsAsync($$"""
            namespace PrimaryConstructor;

            public abstract {{typeKind}} BasePoint(int X);

            public {{typeKind}} Point(int X, int Y)
                : BasePoint(X)
            {
                public int Y { get; init; } = Y;

                public static Point Parse(string line)
                {
                    $$
                }
            }
            """, [
            ItemExpectation.Absent("X"),
            ItemExpectation.Absent("Y"),
        ]);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/74327")]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("record class")]
    [InlineData("record struct")]
    public Task RecommendedPrimaryConstructorParameters04(string typeKind)
        => VerifyExpectedItemsAsync($$"""
            namespace PrimaryConstructor;

            public {{typeKind}} Point(int X, int Y)
            {
                public static Point Parse(string line)
                {
                    var n = nameof($$
                }
            }
            """, [
            ItemExpectation.Exists("X"),
            ItemExpectation.Exists("Y"),
        ]);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/74327")]
    [InlineData("record class")]
    [InlineData("class")]
    public Task RecommendedPrimaryConstructorParameters05(string typeKind)
        => VerifyExpectedItemsAsync($$"""
            namespace PrimaryConstructor;

            public abstract {{typeKind}} BasePoint(int X);

            public {{typeKind}} Point(int X, int Y)
                : BasePoint(X)
            {
                public static Point Parse(string line)
                {
                    var n = nameof($$
                }
            }
            """, [
            ItemExpectation.Exists("X"),
            ItemExpectation.Exists("Y"),
        ]);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/74327")]
    [InlineData("record")]
    [InlineData("class")]
    public Task RecommendedPrimaryConstructorParameters06(string typeKind)
        => VerifyExpectedItemsAsync($$"""
            namespace PrimaryConstructor;

            public abstract {{typeKind}} BasePoint(int X);

            public {{typeKind}} Point(int X, int Y)
                : BasePoint(X)
            {
                public int Y { get; init; } = Y;

                public static Point Parse(string line)
                {
                    var n = nameof($$
                }
            }
            """, [
            ItemExpectation.Exists("X"),
            ItemExpectation.Exists("Y"),
        ]);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/74327")]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("record class")]
    [InlineData("record struct")]
    public Task RecommendedPrimaryConstructorParameters07(string typeKind)
        => VerifyExpectedItemsAsync($$"""
            namespace PrimaryConstructor;

            public {{typeKind}} Point(int X, int Y)
            {
                public static int Y { get; } = 0;

                public static Point Parse(string line)
                {
                    $$
                }
            }
            """, [
            ItemExpectation.Absent("X"),
            ItemExpectation.Exists("Y"),
        ]);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/74327")]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("record class")]
    [InlineData("record struct")]
    public Task RecommendedPrimaryConstructorParameters08(string typeKind)
        => VerifyExpectedItemsAsync($$"""
            namespace PrimaryConstructor;

            public {{typeKind}} Point(int X, int Y)
            {
                public static int Y { get; } = 0;

                public static Point Parse(string line)
                {
                    var n = nameof($$
                }
            }
            """, [
            ItemExpectation.Exists("X"),
            ItemExpectation.Exists("Y"),
        ]);

    [Theory, CombinatorialData]
    public Task PartialPropertyOrConstructor(
        [CombinatorialValues("class", "struct", "record", "record class", "record struct", "interface")] string typeKind,
        [CombinatorialValues("", "public", "private", "static", "extern")] string modifiers)
        => VerifyExpectedItemsAsync($$"""
            partial {{typeKind}} C
            {
                {{modifiers}} partial $$
            }
            """, [
            ItemExpectation.Exists("C"),
        ]);

    [Fact]
    public Task ModernExtensionMethod1()
        => VerifyItemExistsAsync(
            MakeMarkup("""
            static class C
            {
                extension(string s)
                {
                    public bool IsNullOrEmpty() => false;
                }

                void M(string s)
                {
                    s.$$
                }
            }
            """),
            "IsNullOrEmpty",
            sourceCodeKind: SourceCodeKind.Regular,
            glyph: Glyph.ExtensionMethodPublic);

    [Theory]
    [InlineData("""public int Number => 0;""",
                Glyph.PropertyPublic,
                """
                extension(TestClass testclass)
                {
                    public int Number()  => 0;
                }
                """,
                Glyph.ExtensionMethodPublic)]
    [InlineData("""public int Number => 0;""",
                Glyph.PropertyPublic,
                """public static int Number(this TestClass testclass)  => 0;""",
                Glyph.ExtensionMethodPublic)]
    [InlineData("""public int Number() => 0;""",
                Glyph.MethodPublic,
                """
                extension(TestClass testclass)
                {
                    public int Number  => 0;
                }
                """,
                Glyph.PropertyPublic)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/70537")]
    internal Task TestIdenticalNameWithExtensionMembersOfDifferentKind(string member, Glyph memberGlyph, string extension, Glyph extensionGlyph)
        => VerifyExpectedItemsAsync(
            MakeMarkup($$"""
            public class TestClass
            {
                {{member}}
            }

            public static class Extensions
            {
                {{extension}}
            }

            internal class Program
            {
                static void Main(string[] args)
                {
                    var t = new TestClass();
                    var x = t.$$
                }
            }
            """, LanguageVersion.CSharp14),
            results: [
            new ItemExpectation(Name: "Number", IsAbsent: false, Glyph: memberGlyph),
            new ItemExpectation(Name: "Number", IsAbsent: false, Glyph: extensionGlyph),
            ],
            sourceCodeKind: SourceCodeKind.Regular);

    private static string MakeMarkup(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string source,
        LanguageVersion languageVersion = LanguageVersion.Preview)
    {
        return $$"""
<Workspace>
    <Project Language="C#" AssemblyName="Assembly" CommonReferencesNet6="true" LanguageVersion="{{languageVersion.ToDisplayString()}}">
        <Document FilePath="Test.cs">
{{source}}
        </Document>
    </Project>
</Workspace>
""";
    }

    public static IEnumerable<object[]> ValidEnumUnderlyingTypeNames()
        => [["Byte"], ["SByte"], ["Int16"], ["UInt16"], ["Int32"], ["UInt32"], ["Int64"], ["UInt64"]];

    [Theory, CombinatorialData]
    public Task CompletionInObjectCreationContextForTypeWithNestedTypes(
        [CombinatorialValues(".", ";")] char commitChar,
        [CombinatorialValues("public", "private", "internal", "")] string modifier)
    {
        // always add "()" if committed via ";", or when nested types are inaccessible if via "."
        var parens = commitChar is ';'
            ? "()"
            : modifier is "private" or ""
                ? "()"
                : "";

        return VerifyProviderCommitAsync($$"""
            class Program
            {
                void M()
                {
                    var a = new Ba$$
                }
            }

            class Bar
            {
                {{modifier}} class Foo1 { }
                class Foo2 { }
            }
            """, "Bar", $$"""
            class Program
            {
                void M()
                {
                    var a = new Bar{{parens}}{{commitChar}}
                }
            }

            class Bar
            {
                {{modifier}} class Foo1 { }
                class Foo2 { }
            }
            """, commitChar: commitChar, sourceCodeKind: SourceCodeKind.Regular);
    }
}
