// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.GenerateDefaultConstructors;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.GenerateDefaultConstructors;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateDefaultConstructors;

using VerifyCodeFix = CSharpCodeFixVerifier<
    EmptyDiagnosticAnalyzer,
    CSharpGenerateDefaultConstructorsCodeFixProvider>;

using VerifyRefactoring = CSharpCodeRefactoringVerifier<
    GenerateDefaultConstructorsCodeRefactoringProvider>;

[UseExportProvider]
public class GenerateDefaultConstructorsTests
{
    private static async Task TestRefactoringAsync(string source, string fixedSource, int index = 0)
    {
        await TestRefactoringOnlyAsync(source, fixedSource, index);
        await TestCodeFixMissingAsync(source);
    }

    private static async Task TestRefactoringOnlyAsync(string source, string fixedSource, int index = 0)
    {
        await new VerifyRefactoring.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionIndex = index,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    private static async Task TestCodeFixAsync(string source, string fixedSource, int index = 0)
    {
        await new VerifyCodeFix.Test
        {
            TestCode = source.Replace("[||]", ""),
            FixedCode = fixedSource,
            CodeActionIndex = index,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();

        await TestRefactoringMissingAsync(source);
    }

    private static async Task TestRefactoringMissingAsync(string source)
    {
        await new VerifyRefactoring.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    private static async Task TestCodeFixMissingAsync(string source)
    {
        source = source.Replace("[||]", "");
        await new VerifyCodeFix.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    public async Task TestProtectedBase()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    public async Task TestPublicBase()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    public async Task TestInternalBase()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    public async Task TestPrivateBase()
    {
        await TestRefactoringMissingAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    public async Task TestRefOutParams()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    public async Task TestFix1()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    public async Task TestFix2()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    public async Task TestRefactoring1()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    public async Task TestFixAll1()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    public async Task TestFixAll2()
    {
        await TestRefactoringAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    public async Task TestFixAll_WithTuples()
    {
        await TestRefactoringAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    public async Task TestMissing1()
    {
        await TestRefactoringMissingAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/889349")]
    public async Task TestDefaultConstructorGeneration_1()
    {
        await TestRefactoringAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/889349")]
    public async Task TestDefaultConstructorGeneration_2()
    {
        await TestRefactoringAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544070")]
    public async Task TestException1()
    {
        await TestRefactoringAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    public async Task TestException2()
    {
        await TestRefactoringAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    public async Task TestException3()
    {
        await TestRefactoringAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    public async Task TestException4()
    {
        await TestRefactoringAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors), CompilerTrait(CompilerFeature.Tuples)]
    public async Task Tuple()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors), CompilerTrait(CompilerFeature.Tuples)]
    public async Task TupleWithNames()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
    [WorkItem("https://github.com/dotnet/Roslyn/issues/6541")]
    public async Task TestGenerateFromDerivedClass()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
    [WorkItem("https://github.com/dotnet/Roslyn/issues/6541")]
    public async Task TestGenerateFromDerivedClass2()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/19953")]
    public async Task TestNotOnEnum()
    {
        await TestRefactoringMissingAsync(
            """
            enum [||]E
            {
            }
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public async Task TestGenerateConstructorFromProtectedConstructor()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public async Task TestGenerateConstructorFromProtectedConstructor2()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/48318")]
    public async Task TestGenerateConstructorFromProtectedConstructorCursorAtTypeOpening()
    {
        await TestRefactoringOnlyAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/48318")]
    public async Task TestGenerateConstructorFromProtectedConstructorCursorBetweenTypeMembers()
    {
        await TestRefactoringOnlyAsync(
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
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/35208")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public async Task TestGenerateConstructorInAbstractClassFromPublicConstructor()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public async Task TestGenerateConstructorFromPublicConstructor2()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public async Task TestGenerateConstructorFromInternalConstructor()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public async Task TestGenerateConstructorFromInternalConstructor2()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public async Task TestGenerateConstructorFromProtectedInternalConstructor()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public async Task TestGenerateConstructorFromProtectedInternalConstructor2()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public async Task TestGenerateConstructorFromPrivateProtectedConstructor()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/25238")]
    public async Task TestGenerateConstructorFromPrivateProtectedConstructor2()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/40586")]
    public async Task TestGeneratePublicConstructorInSealedClassForProtectedBase()
    {
        await TestRefactoringAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/40586")]
    public async Task TestGenerateInternalConstructorInSealedClassForProtectedOrInternalBase()
    {
        await TestRefactoringAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/40586")]
    public async Task TestGenerateInternalConstructorInSealedClassForProtectedAndInternalBase()
    {
        await TestRefactoringAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
    public async Task TestRecord()
    {
        await TestCodeFixAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/58593")]
    public async Task TestStructWithFieldInitializer()
    {
        var source = """
            struct [||]{|CS8983:S|}
            {
                object X = 1;
            }
            """;
        var fixedSource = """
            struct S
            {
                object X = 1;

                public S()
                {
                }
            }
            """;

        await new VerifyCodeFix.Test
        {
            TestCode = source.Replace("[||]", ""),
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

        await TestRefactoringMissingAsync(source);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/58593")]
    public async Task TestMissingInStructWithoutFieldInitializer()
    {
        var source = """
            struct [||]S
            {
                object X;
            }
            """;

        await TestCodeFixMissingAsync(source);
        await TestRefactoringMissingAsync(source);
    }
}
