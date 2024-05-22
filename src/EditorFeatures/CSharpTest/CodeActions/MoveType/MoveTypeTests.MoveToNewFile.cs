// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.MoveType
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
    public partial class MoveTypeTests : CSharpMoveTypeTestsBase
    {
        [WpfFact]
        public async Task TestMissing_OnMatchingFileName()
        {
            var code =
@"[||]class test1 { }";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact]
        public async Task TestMissing_Nested_OnMatchingFileName_Simple()
        {
            var code =
@"class outer
{ 
    [||]class test1 { }
}";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact]
        public async Task TestMatchingFileName_CaseSensitive()
        {
            var code =
@"[||]class Test1 { }";

            await TestActionCountAsync(code, count: 2);
        }

        [WpfFact]
        public async Task TestForSpans1()
        {
            var code =
@"[|clas|]s Class1 { }
 class Class2 { }";

            await TestActionCountAsync(code, count: 3);
        }

        [WpfFact]
        public async Task TestForSpans2()
        {
            var code =
@"[||]class Class1 { }
 class Class2 { }";
            var codeAfterMove = @"class Class2 { }";

            var expectedDocumentName = "Class1.cs";
            var destinationDocumentText = @"class Class1 { }
";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/14008")]
        public async Task TestMoveToNewFileWithFolders()
        {
            var code =
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document Folders=""A\B""> 
[||]class Class1 { }
class Class2 { }
        </Document>
    </Project>
</Workspace>";
            var codeAfterMove = @"class Class2 { }
        ";

            var expectedDocumentName = "Class1.cs";
            var destinationDocumentText = @"class Class1 { }
        ";

            await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName,
                destinationDocumentText, destinationDocumentContainers: ImmutableArray.Create("A", "B"));
        }

        [WpfFact]
        public async Task TestForSpans3()
        {
            var code =
@"[|class Class1|] { }
class Class2 { }";

            await TestActionCountAsync(code, count: 3);
        }

        [WpfFact]
        public async Task TestForSpans4()
        {
            var code =
@"class Class1[||] { }
class Class2 { }";
            var codeAfterMove = @"class Class2 { }";

            var expectedDocumentName = "Class1.cs";
            var destinationDocumentText = @"class Class1 { }
";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveTypeWithNoContainerNamespace()
        {
            var code =
@"[||]class Class1 { }
class Class2 { }";
            var codeAfterMove = @"class Class2 { }";

            var expectedDocumentName = "Class1.cs";
            var destinationDocumentText = @"class Class1 { }
";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveTypeWithWithUsingsAndNoContainerNamespace()
        {
            var code =
@"// Banner Text
using System;

[||]class Class1 { }
class Class2 { }";

            var codeAfterMove =
@"// Banner Text
using System;
class Class2 { }";

            var expectedDocumentName = "Class1.cs";
            var destinationDocumentText =
@"// Banner Text
class Class1 { }
";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveTypeWithWithMembers()
        {
            var code =
@"// Banner Text
using System;

[||]class Class1 
{ 
    void Print(int x)
    {
        Console.WriteLine(x);
    }
}
class Class2 { }";

            var codeAfterMove =
@"// Banner Text
class Class2 { }";

            var expectedDocumentName = "Class1.cs";
            var destinationDocumentText =
@"// Banner Text
using System;

class Class1 
{ 
    void Print(int x)
    {
        Console.WriteLine(x);
    }
}
";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveTypeWithWithMembers2()
        {
            var code =
@"// Banner Text
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
}";

            var codeAfterMove =
@"// Banner Text
using System;

class Class2
{ 
    void Print(int x)
    {
        Console.WriteLine(x);
    }
}";

            var expectedDocumentName = "Class1.cs";
            var destinationDocumentText =
@"// Banner Text
using System;

class Class1 
{ 
    void Print(int x)
    {
        Console.WriteLine(x);
    }
}
";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveAnInterface()
        {
            var code =
@"[||]interface IMoveType { }
class Class2 { }";
            var codeAfterMove = @"class Class2 { }";

            var expectedDocumentName = "IMoveType.cs";
            var destinationDocumentText = @"interface IMoveType { }
";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveAStruct()
        {
            var code =
@"[||]struct MyStruct { }
class Class2 { }";
            var codeAfterMove = @"class Class2 { }";

            var expectedDocumentName = "MyStruct.cs";
            var destinationDocumentText = @"struct MyStruct { }
";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveAnEnum()
        {
            var code =
@"[||]enum MyEnum { }
class Class2 { }";
            var codeAfterMove = @"class Class2 { }";

            var expectedDocumentName = "MyEnum.cs";
            var destinationDocumentText = @"enum MyEnum { }
";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveTypeWithWithContainerNamespace()
        {
            var code =
@"namespace N1
{
    [||]class Class1 { }
        class Class2 { }
}";

            var codeAfterMove =
@"namespace N1
{
    class Class2 { }
}";

            var expectedDocumentName = "Class1.cs";

            var destinationDocumentText =
@"namespace N1
{
    class Class1 { }
}";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveTypeWithWithFileScopedNamespace()
        {
            var code =
@"namespace N1;

[||]class Class1 { }
class Class2 { }
";

            var codeAfterMove =
@"namespace N1;
class Class2 { }
";

            var expectedDocumentName = "Class1.cs";

            var destinationDocumentText =
@"namespace N1;

class Class1 { }
";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveNestedTypeToNewFile_Simple()
        {
            var code =
@"namespace N1
{
    class Class1 
    {
        [||]class Class2 { }
    }
    
}";

            var codeAfterMove =
@"namespace N1
{
    partial class Class1 
    {
    }
    
}";

            var expectedDocumentName = "Class2.cs";

            var destinationDocumentText =
@"namespace N1
{
    partial class Class1 
    {
        class Class2 { }
    }
    
}";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveNestedTypePreserveModifiers()
        {
            var code =
@"namespace N1
{
    abstract class Class1 
    {
        [||]class Class2 { }
    }
    
}";

            var codeAfterMove =
@"namespace N1
{
    abstract partial class Class1 
    {
    }
    
}";

            var expectedDocumentName = "Class2.cs";

            var destinationDocumentText =
@"namespace N1
{
    abstract partial class Class1 
    {
        class Class2 { }
    }
    
}";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/14004")]
        public async Task MoveNestedTypeToNewFile_Attributes1()
        {
            var code =
@"namespace N1
{
    [Outer]
    class Class1 
    {
        [Inner]
        [||]class Class2 { }
    }
    
}";

            var codeAfterMove =
@"namespace N1
{
    [Outer]
    partial class Class1 
    {
    }
    
}";

            var expectedDocumentName = "Class2.cs";

            var destinationDocumentText =
@"namespace N1
{
    partial class Class1 
    {
        [Inner]
        class Class2 { }
    }
    
}";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/14484")]
        public async Task MoveNestedTypeToNewFile_Comments1()
        {
            var code =
@"namespace N1
{
    /// Outer doc comment.
    class Class1
    {
        /// Inner doc comment
        [||]class Class2
        {
        }
    }
}";

            var codeAfterMove =
@"namespace N1
{
    /// Outer doc comment.
    partial class Class1
    {
    }
}";

            var expectedDocumentName = "Class2.cs";

            var destinationDocumentText =
@"namespace N1
{
    partial class Class1
    {
        /// Inner doc comment
        class Class2
        {
        }
    }
}";
            await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveNestedTypeToNewFile_Simple_DottedName()
        {
            var code =
@"namespace N1
{
    class Class1 
    {
        [||]class Class2 { }
    }
    
}";

            var codeAfterMove =
@"namespace N1
{
    partial class Class1 
    {
    }
    
}";

            var expectedDocumentName = "Class1.Class2.cs";

            var destinationDocumentText =
@"namespace N1
{
    partial class Class1 
    {
        class Class2 { }
    }
    
}";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText, index: 1);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/72632")]
        public async Task MoveNestedTypeToNewFile_Simple_DottedName_WithPrimaryConstructor()
        {
            var code =
@"internal class Outer()
{
    private class Inner[||]
    {
    }
}";

            var codeAfterMove =
@"internal partial class Outer()
{
}";

            var expectedDocumentName = "Outer.Inner.cs";

            var destinationDocumentText =
@"internal partial class Outer
{
    private class Inner
    {
    }
}";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText, index: 1);
        }

        [WpfFact]
        public async Task MoveNestedTypeToNewFile_ParentHasOtherMembers()
        {
            var code =
@"namespace N1
{
    class Class1 
    {
        private int _field1;

        [||]class Class2 { }

        public void Method1() { }
    }
    
}";

            var codeAfterMove =
@"namespace N1
{
    partial class Class1 
    {
        private int _field1;

        public void Method1() { }
    }
    
}";

            var expectedDocumentName = "Class2.cs";

            var destinationDocumentText =
@"namespace N1
{
    partial class Class1 
    {
        class Class2 { }
    }
    
}";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveNestedTypeToNewFile_HasOtherTopLevelMembers()
        {
            var code =
@"namespace N1
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
}";

            var codeAfterMove =
@"namespace N1
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
}";

            var expectedDocumentName = "Class2.cs";

            var destinationDocumentText =
@"namespace N1
{
    partial class Class1 
    {
        class Class2 { }
    }
}";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveNestedTypeToNewFile_HasMembers()
        {
            var code =
@"namespace N1
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
}";

            var codeAfterMove =
@"namespace N1
{
    partial class Class1 
    {
        private int _field1;

        public void Method1() { }
    }
}";

            var expectedDocumentName = "Class2.cs";

            var destinationDocumentText =
@"namespace N1
{
    partial class Class1 
    {
        class Class2 
        {
            private string _field1;
            public void InnerMethod() { }
        }
    }
}";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/13969")]
        public async Task MoveTypeInFileWithComplexHierarchy()
        {
            var code =
@"namespace OuterN1.N1
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
";

            var codeAfterMove =
@"namespace OuterN1.N1
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
";

            var expectedDocumentName = "InnerClass2.cs";

            var destinationDocumentText =
@"namespace OuterN1.N1
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
";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveTypeUsings1()
        {
            var code =
@"
// Only used by inner type.
using System;

// Unused by both types.
using System.Collections;

class Outer { 
    [||]class Inner {
        DateTime d;
    }
}";
            var codeAfterMove = @"
// Only used by inner type.

// Unused by both types.
using System.Collections;

partial class Outer { 
}";

            var expectedDocumentName = "Inner.cs";
            var destinationDocumentText =
@"
// Only used by inner type.
using System;

// Unused by both types.

partial class Outer {
    class Inner {
        DateTime d;
    }
}";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16283")]
        public async Task TestLeadingTrivia1()
        {
            var code =
@"
class Outer
{
    class Inner1
    {
    }

    [||]class Inner2
    {
    }
}";
            var codeAfterMove = @"
partial class Outer
{
    class Inner1
    {
    }
}";

            var expectedDocumentName = "Inner2.cs";
            var destinationDocumentText = @"
partial class Outer
{
    class Inner2
    {
    }
}";

            await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/17171")]
        public async Task TestInsertFinalNewLine()
        {
            var code =
@"
class Outer
{
    class Inner1
    {
    }

    [||]class Inner2
    {
    }
}";
            var codeAfterMove = @"
partial class Outer
{
    class Inner1
    {
    }
}";

            var expectedDocumentName = "Inner2.cs";
            var destinationDocumentText = @"
partial class Outer
{
    class Inner2
    {
    }
}
";

            await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName, destinationDocumentText,
                options: Option(FormattingOptions2.InsertFinalNewLine, true));
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/17171")]
        public async Task TestInsertFinalNewLine2()
        {
            var code =
@"
class Outer
{
    class Inner1
    {
    }

    [||]class Inner2
    {
    }
}";
            var codeAfterMove = @"
partial class Outer
{
    class Inner1
    {
    }
}";

            var expectedDocumentName = "Inner2.cs";
            var destinationDocumentText = @"
partial class Outer
{
    class Inner2
    {
    }
}";

            await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName, destinationDocumentText,
                options: Option(FormattingOptions2.InsertFinalNewLine, false));
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16282")]
        public async Task MoveTypeRemoveOuterInheritanceTypes()
        {
            var code =
@"
class Outer : IComparable { 
    [||]class Inner : IWhatever {
        DateTime d;
    }
}";
            var codeAfterMove =
@"
partial class Outer : IComparable { 
}";

            var expectedDocumentName = "Inner.cs";
            var destinationDocumentText =
@"
partial class Outer
{
    class Inner : IWhatever {
        DateTime d;
    }
}";

            await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/17930")]
        public async Task MoveTypeWithDirectives1()
        {
            var code =
@"using System;

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
#endif";
            var codeAfterMove =
            @"using System;

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
#endif";

            var expectedDocumentName = "Inner.cs";
            var destinationDocumentText =
@"
#if true
public class Inner
{

}
#endif";

            await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/17930")]
        public async Task MoveTypeWithDirectives2()
        {
            var code =
@"using System;

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
}";
            var codeAfterMove =
            @"using System;

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
}";

            var expectedDocumentName = "Inner.cs";
            var destinationDocumentText =
@"namespace N
{
    partial class Program
    {
#if true
        public class Inner
        {

        }
#endif
    }
}";

            await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/21456")]
        public async Task TestLeadingBlankLines1()
        {
            var code =
@"// Banner Text
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
";
            var codeAfterMove = @"// Banner Text
using System;

class Class2
{
    void Foo()
    {
        Console.WriteLine();
    }
}
";

            var expectedDocumentName = "Class1.cs";
            var destinationDocumentText = @"// Banner Text
using System;

class Class1
{
    void Foo()
    {
        Console.WriteLine();
    }
}
";

            await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/21456")]
        public async Task TestLeadingBlankLines2()
        {
            var code =
@"// Banner Text
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
";
            var codeAfterMove = @"// Banner Text
using System;

class Class1
{
    void Foo()
    {
        Console.WriteLine();
    }
}
";

            var expectedDocumentName = "Class2.cs";
            var destinationDocumentText = @"// Banner Text
using System;

class Class2
{
    void Foo()
    {
        Console.WriteLine();
    }
}
";

            await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/31377")]
        public async Task TestLeadingCommentInContainer()
        {
            var code =
@"// Banner Text
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
";
            var codeAfterMove = @"// Banner Text
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
";

            var expectedDocumentName = "Class2.cs";
            var destinationDocumentText = @"// Banner Text
partial class Class1
{
    class Class2
    {
    }
}
";

            await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/31377")]
        public async Task TestLeadingCommentInContainer2()
        {
            var code =
@"// Banner Text
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
";
            var codeAfterMove = @"// Banner Text
using System;

partial class Class1
{ // Leading comment

    void Foo()
    {
        Console.WriteLine();
    }

    public int I() => 5;
}
";

            var expectedDocumentName = "Class2.cs";
            var destinationDocumentText = @"// Banner Text
partial class Class1
{
    class Class2
    {
    }
}
";

            await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/31377")]
        public async Task TestTrailingCommentInContainer()
        {
            var code =
@"// Banner Text
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
";
            var codeAfterMove = @"// Banner Text
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
";

            var expectedDocumentName = "Class2.cs";
            var destinationDocumentText = @"// Banner Text
partial class Class1
{
    class Class2
    {
    }
}
";

            await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/31377")]
        public async Task TestTrailingCommentInContainer2()
        {
            var code =
@"// Banner Text
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
";
            var codeAfterMove = @"// Banner Text
using System;

partial class Class1
{

    void Foo()
    {
        Console.WriteLine();
    }

    public int I() => 5;
} // End of class document
";

            var expectedDocumentName = "Class2.cs";
            var destinationDocumentText = @"// Banner Text
partial class Class1
{
    class Class2
    {
    }
}";

            await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/50329")]
        public async Task MoveRecordToNewFilePreserveUsings()
        {
            var code =
@"using System;

[||]record CacheContext(String Message);

class Program { }";
            var codeAfterMove = @"class Program { }";

            var expectedDocumentName = "CacheContext.cs";
            var destinationDocumentText = @"using System;

record CacheContext(String Message);
";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveClassToNewFilePreserveUsings_PrimaryConstructor()
        {
            var code =
@"using System;

[||]class CacheContext(String Message);

class Program { }";
            var codeAfterMove = @"class Program { }";

            var expectedDocumentName = "CacheContext.cs";
            var destinationDocumentText = @"using System;

class CacheContext(String Message);
";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveStructToNewFilePreserveUsings_PrimaryConstructor()
        {
            var code =
@"using System;

[||]struct CacheContext(String Message);

class Program { }";
            var codeAfterMove = @"class Program { }";

            var expectedDocumentName = "CacheContext.cs";
            var destinationDocumentText = @"using System;

struct CacheContext(String Message);
";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveInterfaceToNewFilePreserveUsings_PrimaryConstructor()
        {
            var code =
@"using System;

[||]interface CacheContext(String Message);

class Program { }";
            var codeAfterMove = @"using System;

class Program { }";

            var expectedDocumentName = "CacheContext.cs";
            var destinationDocumentText = @"interface CacheContext(String Message);
";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MoveClassInTopLevelStatements()
        {
            var code = @"
using ConsoleApp1;
using System;

var c = new C();
Console.WriteLine(c.Hello);

class [||]C
{
    public string Hello => ""Hello"";
}";

            var codeAfterMove = @"
using ConsoleApp1;
using System;

var c = new C();
Console.WriteLine(c.Hello);
";

            var expectedDocumentName = "C.cs";
            var destinationDocumentText = @"class C
{
    public string Hello => ""Hello"";
}";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact]
        public async Task MissingInTopLevelStatementsOnly()
        {
            var code = @"
using ConsoleApp1;
using System;

var c = new object();
[||]Console.WriteLine(c.ToString());
";

            await TestMissingAsync(code);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55544")]
        public async Task MoveInNamespace_WithAttributes1()
        {
            var code = @"
using Sytem.Reflection;

[assembly: AssemblyCompany("")]
namespace N
{
    class A 
    {
    }

    class [||]B
    {
    }
}";

            var codeAfterMove = @"
using Sytem.Reflection;

[assembly: AssemblyCompany("")]
namespace N
{
    class A 
    {
    }
}";

            var expectedDocumentName = "B.cs";
            var destinationDocumentText = @"namespace N
{
    class B
    {
    }
}";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55544")]
        public async Task MoveInNamespace_WithAttributes2()
        {
            var code = @"
using Sytem.Reflection;

[assembly: AssemblyCompany("")]
namespace N
{
    class A 
    {
    }

    [Test]
    class [||]B
    {
    }
}";

            var codeAfterMove = @"
using Sytem.Reflection;

[assembly: AssemblyCompany("")]
namespace N
{
    class A 
    {
    }
}";

            var expectedDocumentName = "B.cs";
            var destinationDocumentText = @"namespace N
{
    [Test]
    class B
    {
    }
}";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55544")]
        public async Task MoveInNamespace_WithAttributes3()
        {
            var code = @"
namespace N
{
    class A 
    {
    }

    [Test]
    class [||]B
    {
    }
}";

            var codeAfterMove = @"
namespace N
{
    class A 
    {
    }
}";

            var expectedDocumentName = "B.cs";
            var destinationDocumentText = @"
namespace N
{
    [Test]
    class B
    {
    }
}";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55544")]
        public async Task MoveTopLevel_WithAttributes1()
        {
            var code = @"
[Test]
class [||]A
{
}

class B
{
}";

            var codeAfterMove = @"
class B
{
}";

            var expectedDocumentName = "A.cs";
            var destinationDocumentText = @"[Test]
class A
{
}
";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/55544")]
        public async Task MoveTopLevel_WithAttributes2()
        {
            var code = @"
[Test]
class [||]A
{
}

[Test]
class B
{
}";

            var codeAfterMove = @"
[Test]
class B
{
}";

            var expectedDocumentName = "A.cs";
            var destinationDocumentText = @"[Test]
class A
{
}
";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/63114")]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("interface")]
        [InlineData("enum")]
        [InlineData("record")]
        public async Task MoveNestedTypeFromInterface(string memberType)
        {
            var code = $@"
interface I
{{
    {memberType} [||]Member
    {{
    }}
}}";
            var codeAfterMove = @"
partial interface I
{
}";
            var expectedDocumentName = "Member.cs";
            var destinationDocumentText = $@"
partial interface I
{{
    {memberType} Member
    {{
    }}
}}";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }
    }
}
