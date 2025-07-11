// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.MoveType;

[Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
public sealed partial class MoveTypeTests : CSharpMoveTypeTestsBase
{
    [WpfFact]
    public Task TestMissing_OnMatchingFileName()
        => TestMissingInRegularAndScriptAsync(@"[||]class test1 { }");

    [WpfFact]
    public Task TestMissing_Nested_OnMatchingFileName_Simple()
        => TestMissingInRegularAndScriptAsync("""
            class outer
            { 
                [||]class test1 { }
            }
            """);

    [WpfFact]
    public Task TestMatchingFileName_CaseSensitive()
        => TestActionCountAsync(@"[||]class Test1 { }", count: 2);

    [WpfFact]
    public Task TestForSpans1()
        => TestActionCountAsync("""
            [|clas|]s Class1 { }
             class Class2 { }
            """, count: 3);

    [WpfFact]
    public Task TestForSpans2()
        => TestMoveTypeToNewFileAsync("""
            [||]class Class1 { }
             class Class2 { }
            """, @"class Class2 { }", "Class1.cs", """
            class Class1 { }

            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/14008")]
    public Task TestMoveToNewFileWithFolders()
        => TestMoveTypeToNewFileAsync(
            """

            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document Folders="A\B"> 
            [||]class Class1 { }
            class Class2 { }
                    </Document>
                </Project>
            </Workspace>
            """, """
            class Class2 { }
                    
            """, "Class1.cs",
            """
            class Class1 { }
                    
            """, destinationDocumentContainers: ["A", "B"]);

    [WpfFact]
    public Task TestForSpans3()
        => TestActionCountAsync("""
            [|class Class1|] { }
            class Class2 { }
            """, count: 3);

    [WpfFact]
    public Task TestForSpans4()
        => TestMoveTypeToNewFileAsync("""
            class Class1[||] { }
            class Class2 { }
            """, @"class Class2 { }", "Class1.cs", """
            class Class1 { }

            """);

    [WpfFact]
    public Task MoveTypeWithNoContainerNamespace()
        => TestMoveTypeToNewFileAsync("""
            [||]class Class1 { }
            class Class2 { }
            """, @"class Class2 { }", "Class1.cs", """
            class Class1 { }

            """);

    [WpfFact]
    public Task MoveTypeWithWithUsingsAndNoContainerNamespace()
        => TestMoveTypeToNewFileAsync("""
            // Banner Text
            using System;

            [||]class Class1 { }
            class Class2 { }
            """, """
            // Banner Text
            using System;
            class Class2 { }
            """, "Class1.cs", """
            // Banner Text
            class Class1 { }

            """);

    [WpfFact]
    public Task MoveTypeWithWithMembers()
        => TestMoveTypeToNewFileAsync("""
            // Banner Text
            using System;

            [||]class Class1 
            { 
                void Print(int x)
                {
                    Console.WriteLine(x);
                }
            }
            class Class2 { }
            """, """
            // Banner Text
            class Class2 { }
            """, "Class1.cs", """
            // Banner Text
            using System;

            class Class1 
            { 
                void Print(int x)
                {
                    Console.WriteLine(x);
                }
            }

            """);

    [WpfFact]
    public Task MoveTypeWithWithMembers2()
        => TestMoveTypeToNewFileAsync("""
            // Banner Text
            using System;

            [||]class Class1 
            { 
                void Print(int x)
                {
                    Console.WriteLine(x);
                }
            }

            class Class2
            { 
                void Print(int x)
                {
                    Console.WriteLine(x);
                }
            }
            """, """
            // Banner Text
            using System;

            class Class2
            { 
                void Print(int x)
                {
                    Console.WriteLine(x);
                }
            }
            """, "Class1.cs", """
            // Banner Text
            using System;

            class Class1 
            { 
                void Print(int x)
                {
                    Console.WriteLine(x);
                }
            }

            """);

    [WpfFact]
    public Task MoveAnInterface()
        => TestMoveTypeToNewFileAsync("""
            [||]interface IMoveType { }
            class Class2 { }
            """, @"class Class2 { }", "IMoveType.cs", """
            interface IMoveType { }

            """);

    [WpfFact]
    public Task MoveAStruct()
        => TestMoveTypeToNewFileAsync("""
            [||]struct MyStruct { }
            class Class2 { }
            """, @"class Class2 { }", "MyStruct.cs", """
            struct MyStruct { }

            """);

    [WpfFact]
    public Task MoveAnEnum()
        => TestMoveTypeToNewFileAsync("""
            [||]enum MyEnum { }
            class Class2 { }
            """, @"class Class2 { }", "MyEnum.cs", """
            enum MyEnum { }

            """);

    [WpfFact]
    public Task MoveTypeWithWithContainerNamespace()
        => TestMoveTypeToNewFileAsync("""
            namespace N1
            {
                [||]class Class1 { }
                class Class2 { }
            }
            """, """
            namespace N1
            {
                class Class2 { }
            }
            """, "Class1.cs", """
            namespace N1
            {
                class Class1 { }
            }
            """);

    [WpfFact]
    public Task MoveTypeWithWithFileScopedNamespace()
        => TestMoveTypeToNewFileAsync("""
            namespace N1;

            [||]class Class1 { }
            class Class2 { }

            """, """
            namespace N1;
            class Class2 { }

            """, "Class1.cs", """
            namespace N1;

            class Class1 { }

            """);

    [WpfFact]
    public Task MoveNestedTypeToNewFile_Simple()
        => TestMoveTypeToNewFileAsync("""
            namespace N1
            {
                class Class1 
                {
                    [||]class Class2 { }
                }
                
            }
            """, """
            namespace N1
            {
                partial class Class1 
                {
                }
                
            }
            """, "Class2.cs", """
            namespace N1
            {
                partial class Class1 
                {
                    class Class2 { }
                }
                
            }
            """);

    [WpfFact]
    public Task MoveNestedTypePreserveModifiers()
        => TestMoveTypeToNewFileAsync("""
            namespace N1
            {
                abstract class Class1 
                {
                    [||]class Class2 { }
                }
                
            }
            """, """
            namespace N1
            {
                abstract partial class Class1 
                {
                }
                
            }
            """, "Class2.cs", """
            namespace N1
            {
                abstract partial class Class1 
                {
                    class Class2 { }
                }
                
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/14004")]
    public Task MoveNestedTypeToNewFile_Attributes1()
        => TestMoveTypeToNewFileAsync("""
            namespace N1
            {
                [Outer]
                class Class1 
                {
                    [Inner]
                    [||]class Class2 { }
                }
                
            }
            """, """
            namespace N1
            {
                [Outer]
                partial class Class1 
                {
                }
                
            }
            """, "Class2.cs", """
            namespace N1
            {
                partial class Class1 
                {
                    [Inner]
                    class Class2 { }
                }
                
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/14484")]
    public Task MoveNestedTypeToNewFile_Comments1()
        => TestMoveTypeToNewFileAsync(
            """
            namespace N1
            {
                /// Outer doc comment.
                class Class1
                {
                    /// Inner doc comment
                    [||]class Class2
                    {
                    }
                }
            }
            """, """
            namespace N1
            {
                /// Outer doc comment.
                partial class Class1
                {
                }
            }
            """, "Class2.cs", """
            namespace N1
            {
                partial class Class1
                {
                    /// Inner doc comment
                    class Class2
                    {
                    }
                }
            }
            """);

    [WpfFact]
    public Task MoveNestedTypeToNewFile_Simple_DottedName()
        => TestMoveTypeToNewFileAsync("""
            namespace N1
            {
                class Class1 
                {
                    [||]class Class2 { }
                }
                
            }
            """, """
            namespace N1
            {
                partial class Class1 
                {
                }
                
            }
            """, "Class1.Class2.cs", """
            namespace N1
            {
                partial class Class1 
                {
                    class Class2 { }
                }
                
            }
            """, index: 1);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/72632")]
    public Task MoveNestedTypeToNewFile_Simple_DottedName_WithPrimaryConstructor()
        => TestMoveTypeToNewFileAsync("""
            internal class Outer()
            {
                private class Inner[||]
                {
                }
            }
            """, """
            internal partial class Outer()
            {
            }
            """, "Outer.Inner.cs", """
            internal partial class Outer
            {
                private class Inner
                {
                }
            }
            """, index: 1);

    [WpfFact]
    public Task MoveNestedTypeToNewFile_ParentHasOtherMembers()
        => TestMoveTypeToNewFileAsync("""
            namespace N1
            {
                class Class1 
                {
                    private int _field1;

                    [||]class Class2 { }

                    public void Method1() { }
                }
                
            }
            """, """
            namespace N1
            {
                partial class Class1 
                {
                    private int _field1;

                    public void Method1() { }
                }
                
            }
            """, "Class2.cs", """
            namespace N1
            {
                partial class Class1 
                {
                    class Class2 { }
                }
                
            }
            """);

    [WpfFact]
    public Task MoveNestedTypeToNewFile_HasOtherTopLevelMembers()
        => TestMoveTypeToNewFileAsync("""
            namespace N1
            {
                class Class1 
                {
                    private int _field1;

                    [||]class Class2 { }

                    public void Method1() { }
                }

                internal class Class3 
                {
                    private void Method1() { }
                }
            }
            """, """
            namespace N1
            {
                partial class Class1 
                {
                    private int _field1;

                    public void Method1() { }
                }

                internal class Class3 
                {
                    private void Method1() { }
                }
            }
            """, "Class2.cs", """
            namespace N1
            {
                partial class Class1 
                {
                    class Class2 { }
                }
            }
            """);

    [WpfFact]
    public Task MoveNestedTypeToNewFile_HasMembers()
        => TestMoveTypeToNewFileAsync("""
            namespace N1
            {
                class Class1 
                {
                    private int _field1;

                    [||]class Class2 
                    {
                        private string _field1;
                        public void InnerMethod() { }
                    }

                    public void Method1() { }
                }
            }
            """, """
            namespace N1
            {
                partial class Class1 
                {
                    private int _field1;

                    public void Method1() { }
                }
            }
            """, "Class2.cs", """
            namespace N1
            {
                partial class Class1 
                {
                    class Class2 
                    {
                        private string _field1;
                        public void InnerMethod() { }
                    }
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/13969")]
    public Task MoveTypeInFileWithComplexHierarchy()
        => TestMoveTypeToNewFileAsync("""
            namespace OuterN1.N1
            {
                namespace InnerN2.N2
                {
                    class OuterClass1
                    {
                        class InnerClass2
                        {
                        }
                    }
                }

                namespace InnerN3.N3
                {
                    class OuterClass2
                    {
                        [||]class InnerClass2 
                        {
                            class InnerClass3
                            {
                            }
                        }

                        class InnerClass4
                        {
                        }
                    }

                    class OuterClass3
                    {
                    }
                }
            }

            namespace OuterN2.N2
            {
                namespace InnerN3.N3
                {
                    class OuterClass5 {
                        class InnerClass6 {
                        }
                    }
                }
            }

            """, """
            namespace OuterN1.N1
            {
                namespace InnerN2.N2
                {
                    class OuterClass1
                    {
                        class InnerClass2
                        {
                        }
                    }
                }

                namespace InnerN3.N3
                {
                    partial class OuterClass2
                    {

                        class InnerClass4
                        {
                        }
                    }

                    class OuterClass3
                    {
                    }
                }
            }

            namespace OuterN2.N2
            {
                namespace InnerN3.N3
                {
                    class OuterClass5 {
                        class InnerClass6 {
                        }
                    }
                }
            }

            """, "InnerClass2.cs", """
            namespace OuterN1.N1
            {

                namespace InnerN3.N3
                {
                    partial class OuterClass2
                    {
                        class InnerClass2 
                        {
                            class InnerClass3
                            {
                            }
                        }
                    }
                }
            }

            """);

    [WpfFact]
    public Task MoveTypeUsings1()
        => TestMoveTypeToNewFileAsync("""

            // Only used by inner type.
            using System;

            // Unused by both types.
            using System.Collections;

            class Outer { 
                [||]class Inner {
                    DateTime d;
                }
            }
            """, """

            // Only used by inner type.

            // Unused by both types.
            using System.Collections;

            partial class Outer { 
            }
            """, "Inner.cs", """

            // Only used by inner type.
            using System;

            // Unused by both types.

            partial class Outer {
                class Inner {
                    DateTime d;
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16283")]
    public Task TestLeadingTrivia1()
        => TestMoveTypeToNewFileAsync(
            """

            class Outer
            {
                class Inner1
                {
                }

                [||]class Inner2
                {
                }
            }
            """, """

            partial class Outer
            {
                class Inner1
                {
                }
            }
            """, "Inner2.cs", """

            partial class Outer
            {
                class Inner2
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/17171")]
    public Task TestInsertFinalNewLine()
        => TestMoveTypeToNewFileAsync(
            """

            class Outer
            {
                class Inner1
                {
                }

                [||]class Inner2
                {
                }
            }
            """, """

            partial class Outer
            {
                class Inner1
                {
                }
            }
            """, "Inner2.cs", """

            partial class Outer
            {
                class Inner2
                {
                }
            }

            """,
            options: Option(FormattingOptions2.InsertFinalNewLine, true));

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/17171")]
    public Task TestInsertFinalNewLine2()
        => TestMoveTypeToNewFileAsync(
            """

            class Outer
            {
                class Inner1
                {
                }

                [||]class Inner2
                {
                }
            }
            """, """

            partial class Outer
            {
                class Inner1
                {
                }
            }
            """, "Inner2.cs", """

            partial class Outer
            {
                class Inner2
                {
                }
            }
            """,
            options: Option(FormattingOptions2.InsertFinalNewLine, false));

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16282")]
    public Task MoveTypeRemoveOuterInheritanceTypes()
        => TestMoveTypeToNewFileAsync(
            """

            class Outer : IComparable { 
                [||]class Inner : IWhatever {
                    DateTime d;
                }
            }
            """, """

            partial class Outer : IComparable { 
            }
            """, "Inner.cs", """

            partial class Outer
            {
                class Inner : IWhatever {
                    DateTime d;
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/17930")]
    public Task MoveTypeWithDirectives1()
        => TestMoveTypeToNewFileAsync(
            """
            using System;

            namespace N
            {
                class Program
                {
                    static void Main()
                    {
                    }
                }
            }

            #if true
            public class [||]Inner
            {

            }
            #endif
            """, """
            using System;

            namespace N
            {
                class Program
                {
                    static void Main()
                    {
                    }
                }
            }

            #if true
            #endif
            """, "Inner.cs", """

            #if true
            public class Inner
            {

            }
            #endif
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/17930")]
    public Task MoveTypeWithDirectives2()
        => TestMoveTypeToNewFileAsync(
            """
            using System;

            namespace N
            {
                class Program
                {
                    static void Main()
                    {
                    }

            #if true
                    public class [||]Inner
                    {

                    }
            #endif
                }
            }
            """, """
            using System;

            namespace N
            {
                partial class Program
                {
                    static void Main()
                    {
                    }

            #if true
            #endif
                }
            }
            """, "Inner.cs", """
            namespace N
            {
                partial class Program
                {
            #if true
                    public class Inner
                    {

                    }
            #endif
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/19613")]
    public Task MoveTypeWithDirectives3()
        => TestMoveTypeToNewFileAsync(
            """
            public class Goo
            {
                #region Region
                public class [||]Bar
                {
                }
                #endregion
            }
            """, """
            public partial class Goo
            {

                #region Region
                #endregion
            }
            """, "Bar.cs", """
            public partial class Goo
            {
                #region Region
                public class Bar
                {
                }
                #endregion
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/19613")]
    public Task MoveTypeWithDirectives4()
        => TestMoveTypeToNewFileAsync(
            """
            public class Goo
            {
                #region Region1
                public class NotBar
                {
                }
                #endregion

                #region Region2
                public class [||]Bar
                {
                }
                #endregion
            }
            """, """
            public partial class Goo
            {
                #region Region1
                public class NotBar
                {
                }

                #endregion
                #region Region2
                #endregion
            }
            """, "Bar.cs", """
            public partial class Goo
            {
                #region Region2
                public class Bar
                {
                }
                #endregion
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/21456")]
    public Task TestLeadingBlankLines1()
        => TestMoveTypeToNewFileAsync(
            """
            // Banner Text
            using System;

            [||]class Class1
            {
                void Foo()
                {
                    Console.WriteLine();
                }
            }

            class Class2
            {
                void Foo()
                {
                    Console.WriteLine();
                }
            }

            """, """
            // Banner Text
            using System;

            class Class2
            {
                void Foo()
                {
                    Console.WriteLine();
                }
            }

            """, "Class1.cs", """
            // Banner Text
            using System;

            class Class1
            {
                void Foo()
                {
                    Console.WriteLine();
                }
            }

            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/21456")]
    public Task TestLeadingBlankLines2()
        => TestMoveTypeToNewFileAsync(
            """
            // Banner Text
            using System;

            class Class1
            {
                void Foo()
                {
                    Console.WriteLine();
                }
            }

            [||]class Class2
            {
                void Foo()
                {
                    Console.WriteLine();
                }
            }

            """, """
            // Banner Text
            using System;

            class Class1
            {
                void Foo()
                {
                    Console.WriteLine();
                }
            }

            """, "Class2.cs", """
            // Banner Text
            using System;

            class Class2
            {
                void Foo()
                {
                    Console.WriteLine();
                }
            }

            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/31377")]
    public Task TestLeadingCommentInContainer()
        => TestMoveTypeToNewFileAsync(
            """
            // Banner Text
            using System;

            class Class1
            // Leading comment
            {
                class [||]Class2
                {
                }

                void Foo()
                {
                    Console.WriteLine();
                }

                public int I() => 5;
            }

            """, """
            // Banner Text
            using System;

            partial class Class1
            // Leading comment
            {

                void Foo()
                {
                    Console.WriteLine();
                }

                public int I() => 5;
            }

            """, "Class2.cs", """
            // Banner Text
            partial class Class1
            {
                class Class2
                {
                }
            }

            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/31377")]
    public Task TestLeadingCommentInContainer2()
        => TestMoveTypeToNewFileAsync(
            """
            // Banner Text
            using System;

            class Class1
            { // Leading comment
                class [||]Class2
                {
                }

                void Foo()
                {
                    Console.WriteLine();
                }

                public int I() => 5;
            }

            """, """
            // Banner Text
            using System;

            partial class Class1
            { // Leading comment

                void Foo()
                {
                    Console.WriteLine();
                }

                public int I() => 5;
            }

            """, "Class2.cs", """
            // Banner Text
            partial class Class1
            {
                class Class2
                {
                }
            }

            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/31377")]
    public Task TestTrailingCommentInContainer()
        => TestMoveTypeToNewFileAsync(
            """
            // Banner Text
            using System;

            class Class1
            {
                class [||]Class2
                {
                }

                void Foo()
                {
                    Console.WriteLine();
                }

                public int I() => 5;
                // End of class document
            }

            """, """
            // Banner Text
            using System;

            partial class Class1
            {

                void Foo()
                {
                    Console.WriteLine();
                }

                public int I() => 5;
                // End of class document
            }

            """, "Class2.cs", """
            // Banner Text
            partial class Class1
            {
                class Class2
                {
                }
            }

            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/31377")]
    public Task TestTrailingCommentInContainer2()
        => TestMoveTypeToNewFileAsync(
            """
            // Banner Text
            using System;

            class Class1
            {
                class [||]Class2
                {
                }

                void Foo()
                {
                    Console.WriteLine();
                }

                public int I() => 5;
            } // End of class document

            """, """
            // Banner Text
            using System;

            partial class Class1
            {

                void Foo()
                {
                    Console.WriteLine();
                }

                public int I() => 5;
            } // End of class document

            """, "Class2.cs", """
            // Banner Text
            partial class Class1
            {
                class Class2
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/50329")]
    public Task MoveRecordToNewFilePreserveUsings()
        => TestMoveTypeToNewFileAsync("""
            using System;

            [||]record CacheContext(String Message);

            class Program { }
            """, @"class Program { }", "CacheContext.cs", """
            using System;

            record CacheContext(String Message);

            """);

    [WpfFact]
    public Task MoveClassToNewFilePreserveUsings_PrimaryConstructor()
        => TestMoveTypeToNewFileAsync("""
            using System;

            [||]class CacheContext(String Message);

            class Program { }
            """, @"class Program { }", "CacheContext.cs", """
            using System;

            class CacheContext(String Message);

            """);

    [WpfFact]
    public Task MoveStructToNewFilePreserveUsings_PrimaryConstructor()
        => TestMoveTypeToNewFileAsync("""
            using System;

            [||]struct CacheContext(String Message);

            class Program { }
            """, @"class Program { }", "CacheContext.cs", """
            using System;

            struct CacheContext(String Message);

            """);

    [WpfFact]
    public Task MoveInterfaceToNewFilePreserveUsings_PrimaryConstructor()
        => TestMoveTypeToNewFileAsync("""
            using System;

            [||]interface CacheContext(String Message);

            class Program { }
            """, """
            using System;

            class Program { }
            """, "CacheContext.cs", """
            interface CacheContext(String Message);

            """);

    [WpfFact]
    public Task MoveClassInTopLevelStatements()
        => TestMoveTypeToNewFileAsync("""

            using ConsoleApp1;
            using System;

            var c = new C();
            Console.WriteLine(c.Hello);

            class [||]C
            {
                public string Hello => "Hello";
            }
            """, """

            using ConsoleApp1;
            using System;

            var c = new C();
            Console.WriteLine(c.Hello);

            """, "C.cs", """
            class C
            {
                public string Hello => "Hello";
            }
            """);

    [WpfFact]
    public Task MissingInTopLevelStatementsOnly()
        => TestMissingAsync("""

            using ConsoleApp1;
            using System;

            var c = new object();
            [||]Console.WriteLine(c.ToString());

            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55544")]
    public Task MoveInNamespace_WithAttributes1()
        => TestMoveTypeToNewFileAsync("""

            using Sytem.Reflection;

            [assembly: AssemblyCompany(")]
            namespace N
            {
                class A 
                {
                }

                class [||]B
                {
                }
            }
            """, """

            using Sytem.Reflection;

            [assembly: AssemblyCompany(")]
            namespace N
            {
                class A 
                {
                }
            }
            """, "B.cs", """
            namespace N
            {
                class B
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55544")]
    public Task MoveInNamespace_WithAttributes2()
        => TestMoveTypeToNewFileAsync("""

            using Sytem.Reflection;

            [assembly: AssemblyCompany(")]
            namespace N
            {
                class A 
                {
                }

                [Test]
                class [||]B
                {
                }
            }
            """, """

            using Sytem.Reflection;

            [assembly: AssemblyCompany(")]
            namespace N
            {
                class A 
                {
                }
            }
            """, "B.cs", """
            namespace N
            {
                [Test]
                class B
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55544")]
    public Task MoveInNamespace_WithAttributes3()
        => TestMoveTypeToNewFileAsync("""

            namespace N
            {
                class A 
                {
                }

                [Test]
                class [||]B
                {
                }
            }
            """, """

            namespace N
            {
                class A 
                {
                }
            }
            """, "B.cs", """

            namespace N
            {
                [Test]
                class B
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55544")]
    public Task MoveTopLevel_WithAttributes1()
        => TestMoveTypeToNewFileAsync("""

            [Test]
            class [||]A
            {
            }

            class B
            {
            }
            """, """

            class B
            {
            }
            """, "A.cs", """
            [Test]
            class A
            {
            }

            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55544")]
    public Task MoveTopLevel_WithAttributes2()
        => TestMoveTypeToNewFileAsync("""

            [Test]
            class [||]A
            {
            }

            [Test]
            class B
            {
            }
            """, """

            [Test]
            class B
            {
            }
            """, "A.cs", """
            [Test]
            class A
            {
            }

            """);

    [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/63114")]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("enum")]
    [InlineData("record")]
    public Task MoveNestedTypeFromInterface(string memberType)
        => TestMoveTypeToNewFileAsync($$"""

            interface I
            {
                {{memberType}} [||]Member
                {
                }
            }
            """, """

            partial interface I
            {
            }
            """, "Member.cs", $$"""

            partial interface I
            {
                {{memberType}} Member
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/74703")]
    public Task TestUpdateFileName()
        => TestMoveTypeToNewFileAsync(
            """

            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document Folders="A\B" FilePath="Goo.cs">// This is a banner referencing Goo.cs
            [||]class Class1 { }
            class Class2 { }
                    </Document>
                </Project>
            </Workspace>
            """, """
            // This is a banner referencing Goo.cs
            class Class2 { }
                    
            """, "Class1.cs",
            """
            // This is a banner referencing Class1.cs
            class Class1 { }
                    
            """, destinationDocumentContainers: ["A", "B"]);
}
