// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.MoveType
{
    public partial class MoveTypeTests : CSharpMoveTypeTestsBase
    {
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
    }
}