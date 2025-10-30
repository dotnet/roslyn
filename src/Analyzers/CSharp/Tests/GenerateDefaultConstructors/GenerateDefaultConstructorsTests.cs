// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.GenerateConstructors;
using Microsoft.CodeAnalysis.CSharp.GenerateDefaultConstructors;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateDefaultConstructors;

using VerifyCodeFix = CSharpCodeFixVerifier<
    EmptyDiagnosticAnalyzer,
    CSharpGenerateDefaultConstructorsCodeFixProvider>;

#if !CODE_STYLE
using VerifyRefactoring = CSharpCodeRefactoringVerifier<
    CSharpGenerateConstructorsCodeRefactoringProvider>;
#endif

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
public sealed class GenerateDefaultConstructorsTests
{
#if !CODE_STYLE
    private static async Task TestRefactoringAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string source,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string fixedSource,
        int index = 0)
    {
        await TestRefactoringOnlyAsync(source, fixedSource, index);
        await TestCodeFixMissingAsync(source);
    }

    private static Task TestRefactoringOnlyAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string source,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string fixedSource,
        int index = 0)
        => new VerifyRefactoring.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionIndex = index,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
#endif

    private static Task TestCodeFixAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string source,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string fixedSource,
        int index = 0)
        => new VerifyCodeFix.Test
        {
            TestCode = source.Replace("[||]", ""),
            FixedCode = fixedSource,
            CodeActionIndex = index,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();

#if !CODE_STYLE

    private static Task TestRefactoringMissingAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string source)
        => new VerifyRefactoring.Test
        {
            TestCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
#endif

    private static async Task TestCodeFixMissingAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string source)
    {
        source = source.Replace("[||]", "");
        await new VerifyCodeFix.Test
        {
            TestCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact]
    public Task TestProtectedBase()
        => TestCodeFixAsync(
            """
            class {|CS7036:C|} : [||]B
            {
            }

            class B
            {
                protected B(int x)
                {
                }
            }
            """,
            """
            class C : B
            {
                protected C(int x) : base(x)
                {
                }
            }

            class B
            {
                protected B(int x)
                {
                }
            }
            """);

    [Fact]
    public Task TestPublicBase()
        => TestCodeFixAsync(
            """
            class {|CS7036:C|} : [||]B
            {
            }

            class B
            {
                public B(int x)
                {
                }
            }
            """,
            """
            class C : B
            {
                public C(int x) : base(x)
                {
                }
            }

            class B
            {
                public B(int x)
                {
                }
            }
            """);

    [Fact]
    public Task TestInternalBase()
        => TestCodeFixAsync(
            """
            class {|CS7036:C|} : [||]B
            {
            }

            class B
            {
                internal B(int x)
                {
                }
            }
            """,
            """
            class C : B
            {
                internal C(int x) : base(x)
                {
                }
            }

            class B
            {
                internal B(int x)
                {
                }
            }
            """);

    [Fact]
    public Task TestRefOutParams()
        => TestCodeFixAsync(
            """
            class {|CS7036:C|} : [||]B
            {
            }

            class B
            {
                internal B(ref int x, out string s, params bool[] b)
                {
                    s = null;
                }
            }
            """,
            """
            class C : B
            {
                internal C(ref int x, out string s, params bool[] b) : base(ref x, out s, b)
                {
                }
            }

            class B
            {
                internal B(ref int x, out string s, params bool[] b)
                {
                    s = null;
                }
            }
            """);

    [Fact]
    public Task TestFix1()
        => TestCodeFixAsync(
            """
            class {|CS1729:C|} : [||]B
            {
            }

            class B
            {
                internal B(int x)
                {
                }

                protected B(string x)
                {
                }

                public B(bool x)
                {
                }
            }
            """,
            """
            class C : B
            {
                internal C(int x) : base(x)
                {
                }
            }

            class B
            {
                internal B(int x)
                {
                }

                protected B(string x)
                {
                }

                public B(bool x)
                {
                }
            }
            """);

    [Fact]
    public Task TestFix2()
        => TestCodeFixAsync(
            """
            class {|CS1729:C|} : [||]B
            {
            }

            class B
            {
                internal B(int x)
                {
                }

                protected B(string x)
                {
                }

                public B(bool x)
                {
                }
            }
            """,
            """
            class C : B
            {
                protected C(string x) : base(x)
                {
                }
            }

            class B
            {
                internal B(int x)
                {
                }

                protected B(string x)
                {
                }

                public B(bool x)
                {
                }
            }
            """,
            index: 1);

    [Fact]
    public Task TestRefactoring1()
        => TestCodeFixAsync(
            """
            class {|CS1729:C|} : [||]B
            {
            }

            class B
            {
                internal B(int x)
                {
                }

                protected B(string x)
                {
                }

                public B(bool x)
                {
                }
            }
            """,
            """
            class C : B
            {
                public C(bool x) : base(x)
                {
                }
            }

            class B
            {
                internal B(int x)
                {
                }

                protected B(string x)
                {
                }

                public B(bool x)
                {
                }
            }
            """,
            index: 2);

    [Fact]
    public Task TestFixAll1()
        => TestCodeFixAsync(
            """
            class {|CS1729:C|} : [||]B
            {
            }

            class B
            {
                internal B(int x)
                {
                }

                protected B(string x)
                {
                }

                public B(bool x)
                {
                }
            }
            """,
            """
            class C : B
            {
                public C(bool x) : base(x)
                {
                }

                protected C(string x) : base(x)
                {
                }

                internal C(int x) : base(x)
                {
                }
            }

            class B
            {
                internal B(int x)
                {
                }

                protected B(string x)
                {
                }

                public B(bool x)
                {
                }
            }
            """,
            index: 3);

    [Fact, CompilerTrait(CompilerFeature.Tuples)]
    public Task Tuple()
        => TestCodeFixAsync(
            """
            class {|CS7036:C|} : [||]B
            {
            }

            class B
            {
                public B((int, string) x)
                {
                }
            }
            """,
            """
            class C : B
            {
                public C((int, string) x) : base(x)
                {
                }
            }

            class B
            {
                public B((int, string) x)
                {
                }
            }
            """);

    [Fact, CompilerTrait(CompilerFeature.Tuples)]
    public Task TupleWithNames()
        => TestCodeFixAsync(
            """
            class {|CS7036:C|} : [||]B
            {
            }

            class B
            {
                public B((int a, string b) x)
                {
                }
            }
            """,
            """
            class C : B
            {
                public C((int a, string b) x) : base(x)
                {
                }
            }

            class B
            {
                public B((int a, string b) x)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/Roslyn/issues/6541")]
    public Task TestGenerateFromDerivedClass()
        => TestCodeFixAsync(
            """
            class Base
            {
                public Base(string value)
                {
                }
            }

            class [||]{|CS7036:Derived|} : Base
            {
            }
            """,
            """
            class Base
            {
                public Base(string value)
                {
                }
            }

            class Derived : Base
            {
                public Derived(string value) : base(value)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/Roslyn/issues/6541")]
    public Task TestGenerateFromDerivedClass2()
        => TestCodeFixAsync(
            """
            class Base
            {
                public Base(int a, string value = null)
                {
                }
            }

            class [||]{|CS7036:Derived|} : Base
            {
            }
            """,
            """
            class Base
            {
                public Base(int a, string value = null)
                {
                }
            }

            class Derived : Base
            {
                public Derived(int a, string value = null) : base(a, value)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public Task TestGenerateConstructorFromProtectedConstructor()
        => TestCodeFixAsync(
            """
            abstract class {|CS7036:C|} : [||]B
            {
            }

            abstract class B
            {
                protected B(int x)
                {
                }
            }
            """,
            """
            abstract class C : B
            {
                protected C(int x) : base(x)
                {
                }
            }

            abstract class B
            {
                protected B(int x)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public Task TestGenerateConstructorFromProtectedConstructor2()
        => TestCodeFixAsync(
            """
            class {|CS7036:C|} : [||]B
            {
            }

            abstract class B
            {
                protected B(int x)
                {
                }
            }
            """,
            """
            class C : B
            {
                public C(int x) : base(x)
                {
                }
            }

            abstract class B
            {
                protected B(int x)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35208")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public Task TestGenerateConstructorInAbstractClassFromPublicConstructor()
        => TestCodeFixAsync(
            """
            abstract class {|CS7036:C|} : [||]B
            {
            }

            abstract class B
            {
                public B(int x)
                {
                }
            }
            """,
            """
            abstract class C : B
            {
                protected C(int x) : base(x)
                {
                }
            }

            abstract class B
            {
                public B(int x)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public Task TestGenerateConstructorFromPublicConstructor2()
        => TestCodeFixAsync(
            """
            class {|CS7036:C|} : [||]B
            {
            }

            abstract class B
            {
                public B(int x)
                {
                }
            }
            """,
            """
            class C : B
            {
                public C(int x) : base(x)
                {
                }
            }

            abstract class B
            {
                public B(int x)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public Task TestGenerateConstructorFromInternalConstructor()
        => TestCodeFixAsync(
            """
            abstract class {|CS7036:C|} : [||]B
            {
            }

            abstract class B
            {
                internal B(int x)
                {
                }
            }
            """,
            """
            abstract class C : B
            {
                internal C(int x) : base(x)
                {
                }
            }

            abstract class B
            {
                internal B(int x)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public Task TestGenerateConstructorFromInternalConstructor2()
        => TestCodeFixAsync(
            """
            class {|CS7036:C|} : [||]B
            {
            }

            abstract class B
            {
                internal B(int x)
                {
                }
            }
            """,
            """
            class C : B
            {
                public C(int x) : base(x)
                {
                }
            }

            abstract class B
            {
                internal B(int x)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public Task TestGenerateConstructorFromProtectedInternalConstructor()
        => TestCodeFixAsync(
            """
            abstract class {|CS7036:C|} : [||]B
            {
            }

            abstract class B
            {
                protected internal B(int x)
                {
                }
            }
            """,
            """
            abstract class C : B
            {
                protected internal C(int x) : base(x)
                {
                }
            }

            abstract class B
            {
                protected internal B(int x)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public Task TestGenerateConstructorFromProtectedInternalConstructor2()
        => TestCodeFixAsync(
            """
            class {|CS7036:C|} : [||]B
            {
            }

            abstract class B
            {
                protected internal B(int x)
                {
                }
            }
            """,
            """
            class C : B
            {
                public C(int x) : base(x)
                {
                }
            }

            abstract class B
            {
                protected internal B(int x)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public Task TestGenerateConstructorFromPrivateProtectedConstructor()
        => TestCodeFixAsync(
            """
            abstract class {|CS7036:C|} : [||]B
            {
            }

            abstract class B
            {
                private protected B(int x)
                {
                }
            }
            """,
            """
            abstract class C : B
            {
                private protected C(int x) : base(x)
                {
                }
            }

            abstract class B
            {
                private protected B(int x)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public Task TestGenerateConstructorFromPrivateProtectedConstructor2()
        => TestCodeFixAsync(
            """
            class {|CS7036:C|} : [||]B
            {
            }

            abstract class B
            {
                private protected internal {|CS0107:B|}(int x)
                {
                }
            }
            """,
            """
            class C : B
            {
                public C(int x) : base(x)
                {
                }
            }

            abstract class B
            {
                private protected internal {|CS0107:B|}(int x)
                {
                }
            }
            """);

    [Fact]
    public Task TestRecord()
        => TestCodeFixAsync(
            """
            record {|CS1729:C|} : [||]B
            {
            }

            record B
            {
                public B(int x)
                {
                }
            }
            """,
            """
            record C : B
            {
                public C(int x) : base(x)
                {
                }
            }

            record B
            {
                public B(int x)
                {
                }
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58593")]
    public async Task TestStructWithFieldInitializer()
    {
        var source = """
            struct [||]{|CS8983:S|}
            {
                object X = 1;
            }
            """;
        await new VerifyCodeFix.Test
        {
            TestCode = source.Replace("[||]", ""),
            FixedCode = """
            struct S
            {
                object X = 1;

                public S()
                {
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

#if !CODE_STYLE
        await TestRefactoringMissingAsync(source);
#endif
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58593")]
    public async Task TestMissingInStructWithoutFieldInitializer()
    {
        var source = """
            struct [||]S
            {
                object X;
            }
            """;
        await TestCodeFixMissingAsync(source);

#if !CODE_STYLE
        await TestRefactoringMissingAsync(source);
#endif
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/19611")]
    [InlineData("public")]
    [InlineData("protected")]
    public Task TestAttributeReferenceInBaseType1(string accessibility)
        => TestCodeFixAsync(
            $$"""
            using System;

            namespace TestApp.Data
            {
                public class Base
                {
                    public Base([Bar] string goo)
                    {

                    }

                    [AttributeUsage(AttributeTargets.Parameter)]
                    {{accessibility}} class BarAttribute : Attribute
                    {

                    }
                }

                public class {|CS7036:Derived|} : [||]Base
                {

                }
            }
            """,
            $$"""
            using System;
            
            namespace TestApp.Data
            {
                public class Base
                {
                    public Base([Bar] string goo)
                    {
            
                    }
            
                    [AttributeUsage(AttributeTargets.Parameter)]
                    {{accessibility}} class BarAttribute : Attribute
                    {
            
                    }
                }
            
                public class Derived : Base
                {
                    public Derived([Bar] string goo) : base(goo)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19611")]
    public Task TestAttributeReferenceInBaseType2()
        => TestCodeFixAsync(
            """
            using System;

            namespace TestApp.Data
            {
                public class Base
                {
                    public Base([Bar] string goo)
                    {

                    }

                    [AttributeUsage(AttributeTargets.Parameter)]
                    private class BarAttribute : Attribute
                    {

                    }
                }

                public class {|CS7036:Derived|} : [||]Base
                {

                }
            }
            """,
            """
            using System;
            
            namespace TestApp.Data
            {
                public class Base
                {
                    public Base([Bar] string goo)
                    {
            
                    }
            
                    [AttributeUsage(AttributeTargets.Parameter)]
                    private class BarAttribute : Attribute
                    {
            
                    }
                }
            
                public class Derived : Base
                {
                    public Derived(string goo) : base(goo)
                    {
                    }
                }
            }
            """);

#if !CODE_STYLE

    [Fact]
    public Task TestPrivateBase()
        => TestRefactoringMissingAsync(
            """
            class {|CS1729:C|} : [||]B
            {
            }

            class B
            {
                private B(int x)
                {
                }
            }
            """);

    [Fact]
    public Task TestFixAll2()
        => TestRefactoringAsync(
            """
            class C : [||]B
            {
                public {|CS1729:C|}(bool x)
                {
                }
            }

            class B
            {
                internal B(int x)
                {
                }

                protected B(string x)
                {
                }

                public B(bool x)
                {
                }
            }
            """,
            """
            class C : B
            {
                public {|CS1729:C|}(bool x)
                {
                }

                protected C(string x) : base(x)
                {
                }

                internal C(int x) : base(x)
                {
                }
            }

            class B
            {
                internal B(int x)
                {
                }

                protected B(string x)
                {
                }

                public B(bool x)
                {
                }
            }
            """,
            index: 2);

    [Fact]
    public Task TestFixAll_WithTuples()
        => TestRefactoringAsync(
            """
            class C : [||]B
            {
                public {|CS1729:C|}((bool, bool) x)
                {
                }
            }

            class B
            {
                internal B((int, int) x)
                {
                }

                protected B((string, string) x)
                {
                }

                public B((bool, bool) x)
                {
                }
            }
            """,
            """
            class C : B
            {
                public {|CS1729:C|}((bool, bool) x)
                {
                }

                protected C((string, string) x) : base(x)
                {
                }

                internal C((int, int) x) : base(x)
                {
                }
            }

            class B
            {
                internal B((int, int) x)
                {
                }

                protected B((string, string) x)
                {
                }

                public B((bool, bool) x)
                {
                }
            }
            """,
            index: 2);

    [Fact]
    public Task TestMissing1()
        => TestRefactoringMissingAsync(
            """
            class C : [||]B
            {
                public {|CS7036:C|}(int x)
                {
                }
            }

            class B
            {
                internal B(int x)
                {
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/889349")]
    public Task TestDefaultConstructorGeneration_1()
        => TestRefactoringAsync(
            """
            class C : [||]B
            {
                public {|CS7036:C|}(int y)
                {
                }
            }

            class B
            {
                internal B(int x)
                {
                }
            }
            """,
            """
            class C : B
            {
                public {|CS7036:C|}(int y)
                {
                }

                internal {|CS0111:C|}(int x) : base(x)
                {
                }
            }

            class B
            {
                internal B(int x)
                {
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/889349")]
    public Task TestDefaultConstructorGeneration_2()
        => TestRefactoringAsync(
            """
            class C : [||]B
            {
                private {|CS7036:C|}(int y)
                {
                }
            }

            class B
            {
                internal B(int x)
                {
                }
            }
            """,
            """
            class C : B
            {
                internal C(int x) : base(x)
                {
                }

                private {|CS0111:{|CS7036:C|}|}(int y)
                {
                }
            }

            class B
            {
                internal B(int x)
                {
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544070")]
    public Task TestException1()
        => TestRefactoringAsync(
            """
            using System;
            class Program : Excep[||]tion
            {
            }
            """,
            """
            using System;
            using System.Runtime.Serialization;
            class Program : Exception
            {
                public Program()
                {
                }

                public Program(string message) : base(message)
                {
                }

                public Program(string message, Exception innerException) : base(message, innerException)
                {
                }

                protected Program(SerializationInfo info, StreamingContext context) : base(info, context)
                {
                }
            }
            """,
            index: 4);

    [Fact]
    public Task TestException2()
        => TestRefactoringAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program : [||]Exception
            {
                public Program()
                {
                }

                static void Main(string[] args)
                {
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Runtime.Serialization;

            class Program : Exception
            {
                public Program()
                {
                }

                public Program(string message) : base(message)
                {
                }

                public Program(string message, Exception innerException) : base(message, innerException)
                {
                }

                protected Program(SerializationInfo info, StreamingContext context) : base(info, context)
                {
                }

                static void Main(string[] args)
                {
                }
            }
            """,
            index: 3);

    [Fact]
    public Task TestException3()
        => TestRefactoringAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program : [||]Exception
            {
                public Program(string message) : base(message)
                {
                }

                public Program(string message, Exception innerException) : base(message, innerException)
                {
                }

                protected Program(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
                {
                }

                static void Main(string[] args)
                {
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program : Exception
            {
                public Program()
                {
                }

                public Program(string message) : base(message)
                {
                }

                public Program(string message, Exception innerException) : base(message, innerException)
                {
                }

                protected Program(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
                {
                }

                static void Main(string[] args)
                {
                }
            }
            """);

    [Fact]
    public Task TestException4()
        => TestRefactoringAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program : [||]Exception
            {
                public Program(string message, Exception innerException) : base(message, innerException)
                {
                }

                protected Program(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
                {
                }

                static void Main(string[] args)
                {
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program : Exception
            {
                public Program()
                {
                }

                public Program(string message) : base(message)
                {
                }

                public Program(string message, Exception innerException) : base(message, innerException)
                {
                }

                protected Program(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
                {
                }

                static void Main(string[] args)
                {
                }
            }
            """,
            index: 2);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19953")]
    public Task TestNotOnEnum()
        => TestRefactoringMissingAsync(
            """
            enum [||]E
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48318")]
    public Task TestGenerateConstructorFromProtectedConstructorCursorAtTypeOpening()
        => TestRefactoringOnlyAsync(
            """
            class {|CS7036:C|} : B
            {

            [||]

            }

            abstract class B
            {
                protected B(int x)
                {
                }
            }
            """,
            """
            class C : B
            {
                public C(int x) : base(x)
                {
                }
            }

            abstract class B
            {
                protected B(int x)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48318")]
    public Task TestGenerateConstructorFromProtectedConstructorCursorBetweenTypeMembers()
        => TestRefactoringOnlyAsync(
            """
            class {|CS7036:C|} : B
            {
                int X;
            [||]
                int Y;
            }

            abstract class B
            {
                protected B(int x)
                {
                }
            }
            """,
            """
            class C : B
            {
                int X;

                int Y;

                public C(int x) : base(x)
                {
                }
            }

            abstract class B
            {
                protected B(int x)
                {
                }
            }
            """,
            index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40586")]
    public Task TestGenerateInternalConstructorInSealedClassForProtectedOrInternalBase()
        => TestRefactoringAsync(
            """
            class Base
            {
                protected internal Base()
                {
                }
            }

            sealed class Program : [||]Base
            {
            }
            """,
            """
            class Base
            {
                protected internal Base()
                {
                }
            }

            sealed class Program : Base
            {
                internal Program()
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40586")]
    public Task TestGenerateInternalConstructorInSealedClassForProtectedAndInternalBase()
        => TestRefactoringAsync(
            """
            class Base
            {
                private protected Base()
                {
                }
            }

            sealed class Program : [||]Base
            {
            }
            """,
            """
            class Base
            {
                private protected Base()
                {
                }
            }

            sealed class Program : Base
            {
                internal Program()
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40586")]
    public Task TestGeneratePublicConstructorInSealedClassForProtectedBase()
        => TestRefactoringAsync(
            """
            class Base
            {
                protected Base()
                {
                }
            }

            sealed class Program : [||]Base
            {
            }
            """,
            """
            class Base
            {
                protected Base()
                {
                }
            }

            sealed class Program : Base
            {
                public Program()
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51049")]
    public Task TestGenerateDefaultConstructorPreserveBinaryCompat1()
        => TestRefactoringAsync(
            """
            class Base
            {
                protected Base()
                {
                }

                protected Base(int i)
                {
                }
            }

            sealed class Program : [||]Base
            {
            }
            """,
            """
            class Base
            {
                protected Base()
                {
                }
            
                protected Base(int i)
                {
                }
            }

            sealed class Program : Base
            {
                public Program()
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51049")]
    public Task TestGenerateDefaultConstructorPreserveBinaryCompat2()
        => TestRefactoringAsync(
            """
            class Base
            {
                protected Base()
                {
                }

                protected Base(int i)
                {
                }
            }

            sealed class Program : [||]Base
            {
            }
            """,
            """
            class Base
            {
                protected Base()
                {
                }
            
                protected Base(int i)
                {
                }
            }

            sealed class Program : Base
            {
                public Program()
                {
                }

                public Program(int i) : base(i)
                {
                }
            }
            """,
            index: 1);

#endif
}
