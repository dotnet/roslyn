// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.MoveType
{
    public partial class MoveTypeTests : CSharpMoveTypeTestsBase
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task TestMissing_OnMatchingFileName()
        {
            var code =
@"[||]class test1 { }";

            await TestMissingAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task TestMissing_Nested_OnMatchingFileName_Simple()
        {
            var code =
@"class outer
{ 
    [||]class test1 { }
}";

            await TestMissingAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task TestMatchingFileName_CaseSensitive()
        {
            var code =
@"[||]class Test1 { }";

            await TestActionCountAsync(code, count: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task TestForSpans1()
        {
            var code =
@"[|clas|]s Class1 { }
class Class2 { }";
            var codeAfterMove = @"class Class2 { }";

            var expectedDocumentName = "Class1.cs";
            var destinationDocumentText = @"class Class1 { }";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        [WorkItem(14008, "https://github.com/dotnet/roslyn/issues/14008")]
        public async Task TestMoveToNewFileWithFolders()
        {
            var code =
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document Folders=""A\B""> 
[|class|] Class1 { }
class Class2 { }
        </Document>
    </Project>
</Workspace>";
            var codeAfterMove = @"class Class2 { }";

            var expectedDocumentName = "Class1.cs";
            var destinationDocumentText = @"class Class1 { }";

            await TestMoveTypeToNewFileAsync(
                code, codeAfterMove, expectedDocumentName, 
                destinationDocumentText, destinationDocumentContainers: new [] {"A", "B"});
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task TestForSpans2()
        {
            var code =
@"[|class Class1|] { }
class Class2 { }";
            var codeAfterMove = @"class Class2 { }";

            var expectedDocumentName = "Class1.cs";
            var destinationDocumentText = @"class Class1 { }";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task TestForSpans3()
        {
            var code =
@"class Class1[||] { }
class Class2 { }";
            var codeAfterMove = @"class Class2 { }";

            var expectedDocumentName = "Class1.cs";
            var destinationDocumentText = @"class Class1 { }";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task MoveTypeWithNoContainerNamespace()
        {
            var code = 
@"[||]class Class1 { }
class Class2 { }";
            var codeAfterMove = @"class Class2 { }";

            var expectedDocumentName = "Class1.cs";
            var destinationDocumentText = @"class Class1 { }";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
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
@"class Class1 { }";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
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
@"
using System;
class Class1 
{ 
    void Print(int x)
    {
        Console.WriteLine(x);
    }
}";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
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
@"
using System;
class Class1 
{ 
    void Print(int x)
    {
        Console.WriteLine(x);
    }
}";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task MoveAnInterface()
        {
            var code =
@"[||]interface IMoveType { }
class Class2 { }";
            var codeAfterMove = @"class Class2 { }";

            var expectedDocumentName = "IMoveType.cs";
            var destinationDocumentText = @"interface IMoveType { }";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task MoveAStruct()
        {
            var code =
@"[||]struct MyStruct { }
class Class2 { }";
            var codeAfterMove = @"class Class2 { }";

            var expectedDocumentName = "MyStruct.cs";
            var destinationDocumentText = @"struct MyStruct { }";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task MoveAnEnum()
        {
            var code =
@"[||]enum MyEnum { }
class Class2 { }";
            var codeAfterMove = @"class Class2 { }";

            var expectedDocumentName = "MyEnum.cs";
            var destinationDocumentText = @"enum MyEnum { }";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
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
        class Class2
        {
        }
    }
}";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        [WorkItem(14004, "https://github.com/dotnet/roslyn/issues/14004")]
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
        class Class2
        {
        }
    }
}";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        [WorkItem(14484, "https://github.com/dotnet/roslyn/issues/14484")]
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
                code, codeAfterMove, expectedDocumentName, destinationDocumentText,
                compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
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
        class Class2
        {
        }
    }
}";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText, index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
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
        class Class2
        {
        }
    }
}";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
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
        class Class2
        {
        }
    }
}";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        [WorkItem(13969, "https://github.com/dotnet/roslyn/issues/13969")]
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
}";

            var expectedDocumentName = "InnerClass2.cs";

            var destinationDocumentText =
@"
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
}";
            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
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
// Unused by both types.
using System.Collections;

partial class Outer {
}";

            var expectedDocumentName = "Inner.cs";
            var destinationDocumentText =
@"
// Only used by inner type.
using System;

partial class Outer {
    class Inner { 
        DateTime d;
    }
}";

            await TestMoveTypeToNewFileAsync(code, codeAfterMove, expectedDocumentName, destinationDocumentText);
        }
    }
}