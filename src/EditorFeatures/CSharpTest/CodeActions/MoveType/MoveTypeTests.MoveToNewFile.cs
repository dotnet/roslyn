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
    public async Task TestMissing_OnMatchingFileName()
    {
        await TestMissingInRegularAndScriptAsync(@"[||]class test1 { }");
    }

    [WpfFact]
    public async Task TestMissing_Nested_OnMatchingFileName_Simple()
    {
        await TestMissingInRegularAndScriptAsync("""
            class outer
            { 
                [||]class test1 { }
            }
            """);
    }

    [WpfFact]
    public async Task TestMatchingFileName_CaseSensitive()
    {
        await TestActionCountAsync(@"[||]class Test1 { }", count: 2);
    }

    [WpfFact]
    public async Task TestForSpans1()
    {
        await TestActionCountAsync("""
            [|clas|]s Class1 { }
             class Class2 { }
            """, count: 3);
    }

    [WpfFact]
    public async Task TestForSpans2()
    {
        await TestMoveTypeToNewFileAsync("""
            [||]class Class1 { }
             class Class2 { }
            """, @"class Class2 { }", "Class1.cs", """
            class Class1 { }

            """);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/14008")]
    public async Task TestMoveToNewFileWithFolders()
    {
        await TestMoveTypeToNewFileAsync(
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
    }

    [WpfFact]
    public async Task TestForSpans3()
    {
        await TestActionCountAsync("""
            [|class Class1|] { }
            class Class2 { }
            """, count: 3);
    }

    [WpfFact]
    public async Task TestForSpans4()
    {
        await TestMoveTypeToNewFileAsync("""
            class Class1[||] { }
            class Class2 { }
            """, @"class Class2 { }", "Class1.cs", """
            class Class1 { }

            """);
    }

    [WpfFact]
    public async Task MoveTypeWithNoContainerNamespace()
    {
        await TestMoveTypeToNewFileAsync("""
            [||]class Class1 { }
            class Class2 { }
            """, @"class Class2 { }", "Class1.cs", """
            class Class1 { }

            """);
    }

    [WpfFact]
    public async Task MoveTypeWithWithUsingsAndNoContainerNamespace()
    {
        await TestMoveTypeToNewFileAsync("""
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
    }

    [WpfFact]
    public async Task MoveTypeWithWithMembers()
    {
        await TestMoveTypeToNewFileAsync("""
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
    }

    [WpfFact]
    public async Task MoveTypeWithWithMembers2()
    {
        await TestMoveTypeToNewFileAsync("""
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
    }

    [WpfFact]
    public async Task MoveAnInterface()
    {
        await TestMoveTypeToNewFileAsync("""
            [||]interface IMoveType { }
            class Class2 { }
            """, @"class Class2 { }", "IMoveType.cs", """
            interface IMoveType { }

            """);
    }

    [WpfFact]
    public async Task MoveAStruct()
    {
        await TestMoveTypeToNewFileAsync("""
            [||]struct MyStruct { }
            class Class2 { }
            """, @"class Class2 { }", "MyStruct.cs", """
            struct MyStruct { }

            """);
    }

    [WpfFact]
    public async Task MoveAnEnum()
    {
        await TestMoveTypeToNewFileAsync("""
            [||]enum MyEnum { }
            class Class2 { }
            """, @"class Class2 { }", "MyEnum.cs", """
            enum MyEnum { }

            """);
    }

    [WpfFact]
    public async Task MoveTypeWithWithContainerNamespace()
    {
        await TestMoveTypeToNewFileAsync("""
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
    }

    [WpfFact]
    public async Task MoveTypeWithWithFileScopedNamespace()
    {
        await TestMoveTypeToNewFileAsync("""
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
    }

    [WpfFact]
    public async Task MoveNestedTypeToNewFile_Simple()
    {
        await TestMoveTypeToNewFileAsync("""
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
    }

    [WpfFact]
    public async Task MoveNestedTypePreserveModifiers()
    {
        await TestMoveTypeToNewFileAsync("""
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/14004")]
    public async Task MoveNestedTypeToNewFile_Attributes1()
    {
        await TestMoveTypeToNewFileAsync("""
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/14484")]
    public async Task MoveNestedTypeToNewFile_Comments1()
    {
        await TestMoveTypeToNewFileAsync(
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
    }

    [WpfFact]
    public async Task MoveNestedTypeToNewFile_Simple_DottedName()
    {
        await TestMoveTypeToNewFileAsync("""
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/72632")]
    public async Task MoveNestedTypeToNewFile_Simple_DottedName_WithPrimaryConstructor()
    {
        await TestMoveTypeToNewFileAsync("""
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
    }

    [WpfFact]
    public async Task MoveNestedTypeToNewFile_ParentHasOtherMembers()
    {
        await TestMoveTypeToNewFileAsync("""
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
    }

    [WpfFact]
    public async Task MoveNestedTypeToNewFile_HasOtherTopLevelMembers()
    {
        await TestMoveTypeToNewFileAsync("""
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
    }

    [WpfFact]
    public async Task MoveNestedTypeToNewFile_HasMembers()
    {
        await TestMoveTypeToNewFileAsync("""
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/13969")]
    public async Task MoveTypeInFileWithComplexHierarchy()
    {
        await TestMoveTypeToNewFileAsync("""
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
    }

    [WpfFact]
    public async Task MoveTypeUsings1()
    {
        await TestMoveTypeToNewFileAsync("""

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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16283")]
    public async Task TestLeadingTrivia1()
    {
        await TestMoveTypeToNewFileAsync(
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/17171")]
    public async Task TestInsertFinalNewLine()
    {
        await TestMoveTypeToNewFileAsync(
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/17171")]
    public async Task TestInsertFinalNewLine2()
    {
        await TestMoveTypeToNewFileAsync(
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16282")]
    public async Task MoveTypeRemoveOuterInheritanceTypes()
    {
        await TestMoveTypeToNewFileAsync(
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/17930")]
    public async Task MoveTypeWithDirectives1()
    {
        await TestMoveTypeToNewFileAsync(
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/17930")]
    public async Task MoveTypeWithDirectives2()
    {
        await TestMoveTypeToNewFileAsync(
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/19613")]
    public async Task MoveTypeWithDirectives3()
    {
        await TestMoveTypeToNewFileAsync(
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/19613")]
    public async Task MoveTypeWithDirectives4()
    {
        await TestMoveTypeToNewFileAsync(
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/21456")]
    public async Task TestLeadingBlankLines1()
    {
        await TestMoveTypeToNewFileAsync(
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/21456")]
    public async Task TestLeadingBlankLines2()
    {
        await TestMoveTypeToNewFileAsync(
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/31377")]
    public async Task TestLeadingCommentInContainer()
    {
        await TestMoveTypeToNewFileAsync(
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/31377")]
    public async Task TestLeadingCommentInContainer2()
    {
        await TestMoveTypeToNewFileAsync(
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/31377")]
    public async Task TestTrailingCommentInContainer()
    {
        await TestMoveTypeToNewFileAsync(
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/31377")]
    public async Task TestTrailingCommentInContainer2()
    {
        await TestMoveTypeToNewFileAsync(
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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/50329")]
    public async Task MoveRecordToNewFilePreserveUsings()
    {
        await TestMoveTypeToNewFileAsync("""
            using System;

            [||]record CacheContext(String Message);

            class Program { }
            """, @"class Program { }", "CacheContext.cs", """
            using System;

            record CacheContext(String Message);

            """);
    }

    [WpfFact]
    public async Task MoveClassToNewFilePreserveUsings_PrimaryConstructor()
    {
        await TestMoveTypeToNewFileAsync("""
            using System;

            [||]class CacheContext(String Message);

            class Program { }
            """, @"class Program { }", "CacheContext.cs", """
            using System;

            class CacheContext(String Message);

            """);
    }

    [WpfFact]
    public async Task MoveStructToNewFilePreserveUsings_PrimaryConstructor()
    {
        await TestMoveTypeToNewFileAsync("""
            using System;

            [||]struct CacheContext(String Message);

            class Program { }
            """, @"class Program { }", "CacheContext.cs", """
            using System;

            struct CacheContext(String Message);

            """);
    }

    [WpfFact]
    public async Task MoveInterfaceToNewFilePreserveUsings_PrimaryConstructor()
    {
        await TestMoveTypeToNewFileAsync("""
            using System;

            [||]interface CacheContext(String Message);

            class Program { }
            """, """
            using System;

            class Program { }
            """, "CacheContext.cs", """
            interface CacheContext(String Message);

            """);
    }

    [WpfFact]
    public async Task MoveClassInTopLevelStatements()
    {
        await TestMoveTypeToNewFileAsync("""

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
    }

    [WpfFact]
    public async Task MissingInTopLevelStatementsOnly()
    {
        await TestMissingAsync("""

            using ConsoleApp1;
            using System;

            var c = new object();
            [||]Console.WriteLine(c.ToString());

            """);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55544")]
    public async Task MoveInNamespace_WithAttributes1()
    {
        await TestMoveTypeToNewFileAsync("""

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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55544")]
    public async Task MoveInNamespace_WithAttributes2()
    {
        await TestMoveTypeToNewFileAsync("""

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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55544")]
    public async Task MoveInNamespace_WithAttributes3()
    {
        await TestMoveTypeToNewFileAsync("""

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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55544")]
    public async Task MoveTopLevel_WithAttributes1()
    {
        await TestMoveTypeToNewFileAsync("""

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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55544")]
    public async Task MoveTopLevel_WithAttributes2()
    {
        await TestMoveTypeToNewFileAsync("""

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
    }

    [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/63114")]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("enum")]
    [InlineData("record")]
    public async Task MoveNestedTypeFromInterface(string memberType)
    {
        await TestMoveTypeToNewFileAsync($$"""

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
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/74703")]
    public async Task TestUpdateFileName()
    {
        await TestMoveTypeToNewFileAsync(
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
}
