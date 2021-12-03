' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class InitOnlyMemberTests
        Inherits BasicTestBase

        Protected Const IsExternalInitTypeDefinition As String = "
namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit
    {
    }
}
"

        <Fact>
        Public Sub EvaluationInitOnlySetter_01()

            Dim csSource =
"
public class C : System.Attribute
{
    public int Property0 { init { System.Console.Write(value + "" 0 ""); } }
    public int Property1 { init { System.Console.Write(value + "" 1 ""); } }
    public int Property2 { init { System.Console.Write(value + "" 2 ""); } }
    public int Property3 { init { System.Console.Write(value + "" 3 ""); } }
    public int Property4 { init { System.Console.Write(value + "" 4 ""); } }
    public int Property5 { init { System.Console.Write(value + "" 5 ""); } }
    public int Property6 { init { System.Console.Write(value + "" 6 ""); } }
    public int Property7 { init { System.Console.Write(value + "" 7 ""); } }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim x = new B() With { .Property1 = 42, .Property2 = 43}
    End Sub
End Class

<C(Property7:= 48)>
Class B
    Inherits C

    Public Sub New()
        Property0 = 41
        Me.Property3 = 44
        MyBase.Property4 = 45
        MyClass.Property5 = 46

        With Me
            .Property6 = 47
        End With

        Me.GetType().GetCustomAttributes(False)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:="41 0 44 3 45 4 46 5 47 6 48 7 42 1 43 2 ").VerifyDiagnostics()

            Assert.True(DirectCast(comp1.GetMember(Of PropertySymbol)("C.Property0").SetMethod, IMethodSymbol).IsInitOnly)

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(
<expected><![CDATA[
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Dim x = new B() With { .Property1 = 42, .Property2 = 43}
                               ~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Dim x = new B() With { .Property1 = 42, .Property2 = 43}
                                                ~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
<C(Property7:= 48)>
   ~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Property0 = 41
        ~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Me.Property3 = 44
        ~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        MyBase.Property4 = 45
        ~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        MyClass.Property5 = 46
        ~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            .Property6 = 47
            ~~~~~~~~~~~~~~~
]]></expected>)

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim x = new C()
        x.Property1 = 42

        With New B()
            .Property2 = 43
        End With

        Dim y As New B() With { .F = Sub()
                                         .Property3 = 44
                                     End Sub}
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Dim y = new B()

        With y
            .Property4 = 45
        End With

        With Me
            With y
                .Property6 = 47
            End With
        End With

        Dim x as New B()
        x.Property0 = 41

        Dim z = Sub()
                  Property5 = 46  
                End Sub
    End Sub

    Public F As System.Action
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilation(source3, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected3 =
<expected>
BC37311: Init-only property 'Property1' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x.Property1 = 42
        ~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property2' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Property2 = 43
            ~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property3' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                                         .Property3 = 44
                                         ~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property4' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Property4 = 45
            ~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property6' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                .Property6 = 47
                ~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property0' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x.Property0 = 41
        ~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property5' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                  Property5 = 46  
                  ~~~~~~~~~~~~~~
</expected>
            comp3.AssertTheseDiagnostics(expected3)

            Dim comp4 = CreateCompilation(source3, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp4.AssertTheseDiagnostics(expected3)

            Dim source5 =
<compilation>
    <file name="c.vb"><![CDATA[
Class B
    Inherits C

    Public Sub Test()
        Property0 = 41
        Me.Property3 = 44
        MyBase.Property4 = 45
        MyClass.Property5 = 46

        With Me
            .Property6 = 47
        End With
    End Sub
End Class

]]></file>
</compilation>

            Dim comp5 = CreateCompilation(source5, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected5 =
<expected>
BC37311: Init-only property 'Property0' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Property0 = 41
        ~~~~~~~~~~~~~~
BC37311: Init-only property 'Property3' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Me.Property3 = 44
        ~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property4' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        MyBase.Property4 = 45
        ~~~~~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property5' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        MyClass.Property5 = 46
        ~~~~~~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property6' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Property6 = 47
            ~~~~~~~~~~~~~~~
</expected>
            comp5.AssertTheseDiagnostics(expected5)

            Dim comp6 = CreateCompilation(source5, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp6.AssertTheseDiagnostics(expected5)
        End Sub

        <Fact>
        Public Sub EvaluationInitOnlySetter_02()

            Dim csSource =
"
public class C : System.Attribute
{
    public int Property0 { init; get; }
    public int Property1 { init; get; }
    public int Property2 { init; get; }
    public int Property3 { init; get; }
    public int Property4 { init; get; }
    public int Property5 { init; get; }
    public int Property6 { init; get; }
    public int Property7 { init; get; }
    public int Property8 { init; get; }
    public int Property9 { init => throw new System.InvalidOperationException(); get => 0; }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim b = new B() With { .Property1 = 42 }

        System.Console.Write(b.Property0)
        System.Console.Write(" "c)
        System.Console.Write(b.Property1)
        System.Console.Write(" "c)
        System.Console.Write(b.Property2)
        System.Console.Write(" "c)
        System.Console.Write(b.Property3)
        System.Console.Write(" "c)
        System.Console.Write(b.Property4)
        System.Console.Write(" "c)
        System.Console.Write(b.Property5)
        System.Console.Write(" "c)
        System.Console.Write(b.Property6)
        System.Console.Write(" "c)
        System.Console.Write(DirectCast(b.GetType().GetCustomAttributes(False)(0), C).Property7)
        System.Console.Write(" "c)
        System.Console.Write(b.Property8)

        B.Init(b.Property9, 492)
        B.Init((b.Property9), 493)
    End Sub
End Class

<C(Property7:= 48)>
Class B
    Inherits C

    Public Sub New()
        Property0 = 41
        Me.Property3 = 44
        MyBase.Property4 = 45
        MyClass.Property5 = 46

        With Me
            .Property6 = 47
        End With

        Init(Property2, 43)
        Init((Property2), 430)

        With Me
            Init(.Property8, 49)
            Init((.Property9), 494)
        End With

        Dim b = Me
        Init(b.Property9, 490)
        Init((b.Property9), 491)

        With b
            Init(.Property9, 499)
            Init((.Property9), 450)
        End With

        Test()

        Dim d = Sub()
                    Init(Property9, 600)
                    Init((Property9), 601)
                End Sub

        d()
    End Sub

    Public Sub Test()
        With Me
            Init(.Property9, 495)
            Init((.Property9), 496)
        End With

        Init(Property9, 497)
        Init((Property9), 498)

        Dim b = Me

        With b
            Init(.Property9, 451)
            Init((.Property9), 452)
        End With
    End Sub

    Public Shared Sub Init(ByRef p as Integer, val As Integer)
        p = val
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16_9, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:="41 42 43 44 45 46 47 48 49").VerifyDiagnostics()

            Assert.True(DirectCast(comp1.GetMember(Of PropertySymbol)("C.Property0").SetMethod, IMethodSymbol).IsInitOnly)

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(
<expected><![CDATA[
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Dim b = new B() With { .Property1 = 42 }
                               ~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        B.Init(b.Property9, 492)
               ~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
<C(Property7:= 48)>
   ~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Property0 = 41
        ~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Me.Property3 = 44
        ~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        MyBase.Property4 = 45
        ~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        MyClass.Property5 = 46
        ~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            .Property6 = 47
            ~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Init(Property2, 43)
             ~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            Init(.Property8, 49)
                 ~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Init(b.Property9, 490)
             ~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            Init(.Property9, 499)
                 ~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
                    Init(Property9, 600)
                         ~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            Init(.Property9, 495)
                 ~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Init(Property9, 497)
             ~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            Init(.Property9, 451)
                 ~~~~~~~~~~
]]></expected>)

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim x = new C()
        x.Property1 = 42

        With New B()
            .Property2 = 43
        End With

        Dim y As New B() With { .F = Sub()
                                         .Property3 = 44
                                     End Sub}
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Dim y = new B()

        With y
            .Property4 = 45
        End With

        With Me
            With y
                .Property6 = 47
            End With
        End With

        Dim x as New B()
        x.Property0 = 41

        Dim z = Sub()
                  Property5 = 46  
                End Sub
    End Sub

    Public F As System.Action
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilation(source3, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected3 =
<expected>
BC37311: Init-only property 'Property1' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x.Property1 = 42
        ~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property2' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Property2 = 43
            ~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property3' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                                         .Property3 = 44
                                         ~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property4' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Property4 = 45
            ~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property6' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                .Property6 = 47
                ~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property0' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x.Property0 = 41
        ~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property5' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                  Property5 = 46  
                  ~~~~~~~~~~~~~~
</expected>
            comp3.AssertTheseDiagnostics(expected3)

            Dim comp4 = CreateCompilation(source3, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp4.AssertTheseDiagnostics(expected3)

            Dim source5 =
<compilation>
    <file name="c.vb"><![CDATA[
Class B
    Inherits C

    Public Sub Test()
        Property0 = 41
        Me.Property3 = 44
        MyBase.Property4 = 45
        MyClass.Property5 = 46

        With Me
            .Property6 = 47
        End With
    End Sub
End Class

]]></file>
</compilation>

            Dim comp5 = CreateCompilation(source5, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected5 =
<expected>
BC37311: Init-only property 'Property0' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Property0 = 41
        ~~~~~~~~~~~~~~
BC37311: Init-only property 'Property3' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Me.Property3 = 44
        ~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property4' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        MyBase.Property4 = 45
        ~~~~~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property5' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        MyClass.Property5 = 46
        ~~~~~~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property6' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Property6 = 47
            ~~~~~~~~~~~~~~~
</expected>
            comp5.AssertTheseDiagnostics(expected5)

            Dim comp6 = CreateCompilation(source5, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp6.AssertTheseDiagnostics(expected5)
        End Sub

        <Fact>
        Public Sub EvaluationInitOnlySetter_03()

            Dim csSource =
"
public class C
{
    public int this[int x] { init { System.Console.Write(value + "" ""); } }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim x = new B()
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Item(0) = 40
        Me.Item(0) = 41
        MyBase.Item(0) = 42
        MyClass.Item(0) = 43

        Me(0) = 44

        With Me
            .Item(0) = 45
        End With
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:="40 41 42 43 44 45 ").VerifyDiagnostics()

            Assert.True(DirectCast(comp1.GetMember(Of PropertySymbol)("C.Item").SetMethod, IMethodSymbol).IsInitOnly)

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(
<expected><![CDATA[
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Item(0) = 40
        ~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Me.Item(0) = 41
        ~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        MyBase.Item(0) = 42
        ~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        MyClass.Item(0) = 43
        ~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Me(0) = 44
        ~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            .Item(0) = 45
            ~~~~~~~~~~~~~
]]></expected>)

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim x = new C()
        x(0) = 40
        x.Item(0) = 41

        With New B()
            .Item(0) = 42
        End With

        Dim y As New B() With { .F = Sub()
                                         .Item(0) = 43
                                     End Sub}
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Dim y = new B()

        With y
            .Item(0) = 44
        End With

        With Me
            With y
                .Item(0) = 45
            End With
        End With

        Dim x as New B()
        x(0) = 46
        x.Item(0) = 47

        Dim z = Sub()
                  Item(0) = 48 
                  Me(0) = 49
                End Sub
    End Sub

    Public F As System.Action
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilation(source3, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected3 =
<expected>
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x(0) = 40
        ~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x.Item(0) = 41
        ~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Item(0) = 42
            ~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                                         .Item(0) = 43
                                         ~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Item(0) = 44
            ~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                .Item(0) = 45
                ~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x(0) = 46
        ~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x.Item(0) = 47
        ~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                  Item(0) = 48 
                  ~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                  Me(0) = 49
                  ~~~~~~~~~~
</expected>
            comp3.AssertTheseDiagnostics(expected3)

            Dim comp4 = CreateCompilation(source3, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp4.AssertTheseDiagnostics(expected3)

            Dim source5 =
<compilation>
    <file name="c.vb"><![CDATA[
Class B
    Inherits C

    Public Sub Test()
        Item(0) = 40
        Me(0) = 41
        Me.Item(0) = 42
        MyBase.Item(0) = 43
        MyClass.Item(0) = 44

        With Me
            .Item(0) = 45
        End With
    End Sub
End Class

]]></file>
</compilation>

            Dim comp5 = CreateCompilation(source5, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected5 =
<expected>
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Item(0) = 40
        ~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Me(0) = 41
        ~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Me.Item(0) = 42
        ~~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        MyBase.Item(0) = 43
        ~~~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        MyClass.Item(0) = 44
        ~~~~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Item(0) = 45
            ~~~~~~~~~~~~~
</expected>
            comp5.AssertTheseDiagnostics(expected5)

            Dim comp6 = CreateCompilation(source5, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp6.AssertTheseDiagnostics(expected5)
        End Sub

        <Fact>
        Public Sub EvaluationInitOnlySetter_04()

            Dim csSource =
"
public class C : System.Attribute
{
    private int[] _item = new int[36];
    public int this[int x]
    {
        init
        {
            if (x > 8)
            {
                throw new System.InvalidOperationException();
            }

            _item[x] = value;
        }

        get => _item[x];
    }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim b = new B()

        B.Init(b(9), 49)
        B.Init((b(19)), 59)
        B.Init(b.Item(10), 50)
        B.Init((b.Item(20)), 60)

        With b
            B.Init(.Item(11), 51)
            B.Init((.Item(21)), 61)
        End With

        for i as Integer = 0 To 35
            System.Console.Write(b(i))
            System.Console.Write(" "c)
        Next
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Item(0) = 40
        Me(1) = 41
        Me.Item(2) = 42
        MyBase.Item(3) = 43
        MyClass.Item(4) = 44

        With Me
            .Item(5) = 45
        End With

        Init(Item(6), 46)
        Init((Item(22)), 62)
        Init(Me(7), 47)
        Init((Me(23)), 63)

        Dim b = Me
        Init(b(12), 52)
        Init((b(24)), 64)
        Init(b.Item(13), 53)
        Init((b.Item(25)), 65)

        With Me
            Init(.Item(8), 48)
            Init((.Item(26)), 66)
        End With

        With b
            Init(.Item(14), 54)
            Init((.Item(27)), 67)
        End With

        Test()

        Dim d = Sub()
                    Init(Item(32), 72)
                    Init((Item(33)), 73)
                    Init(Me(34), 74)
                    Init((Me(35)), 75)
                End Sub

        d()

    End Sub

    Public Sub Test()
        With Me
            Init(.Item(15), 55)
            Init((.Item(28)), 68)
        End With

        Init(Me(16), 56)
        Init((Me(29)), 69)
        Init(Item(17), 57)
        Init((Item(30)), 70)

        Dim b = Me

        With b
            Init(.Item(18), 58)
            Init((.Item(31)), 71)
        End With
    End Sub


    Public Shared Sub Init(ByRef p as Integer, val As Integer)
        p = val
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16_9, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:="40 41 42 43 44 45 46 47 48 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0").VerifyDiagnostics()

            Assert.True(DirectCast(comp1.GetMember(Of PropertySymbol)("C.Item").SetMethod, IMethodSymbol).IsInitOnly)

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(
<expected><![CDATA[
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        B.Init(b(9), 49)
               ~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        B.Init(b.Item(10), 50)
               ~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            B.Init(.Item(11), 51)
                   ~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Item(0) = 40
        ~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Me(1) = 41
        ~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Me.Item(2) = 42
        ~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        MyBase.Item(3) = 43
        ~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        MyClass.Item(4) = 44
        ~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            .Item(5) = 45
            ~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Init(Item(6), 46)
             ~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Init(Me(7), 47)
             ~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Init(b(12), 52)
             ~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Init(b.Item(13), 53)
             ~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            Init(.Item(8), 48)
                 ~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            Init(.Item(14), 54)
                 ~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
                    Init(Item(32), 72)
                         ~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
                    Init(Me(34), 74)
                         ~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            Init(.Item(15), 55)
                 ~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Init(Me(16), 56)
             ~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Init(Item(17), 57)
             ~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            Init(.Item(18), 58)
                 ~~~~~~~~~
]]></expected>)

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim x = new C()
        x(0) = 40
        x.Item(1) = 41

        With New B()
            .Item(2) = 42
        End With

        Dim y As New B() With { .F = Sub()
                                         .Item(3) = 43
                                     End Sub}
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Dim y = new B()

        With y
            .Item(4) = 44
        End With

        With Me
            With y
                .Item(5) = 45
            End With
        End With

        Dim x as New B()
        x(6) = 46
        x.Item(7) = 47

        Dim z = Sub()
                  Item(8) = 48  
                  Me(9) = 49  
                End Sub
    End Sub

    Public F As System.Action
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilation(source3, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected3 =
<expected>
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x(0) = 40
        ~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x.Item(1) = 41
        ~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Item(2) = 42
            ~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                                         .Item(3) = 43
                                         ~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Item(4) = 44
            ~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                .Item(5) = 45
                ~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x(6) = 46
        ~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x.Item(7) = 47
        ~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                  Item(8) = 48  
                  ~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                  Me(9) = 49  
                  ~~~~~~~~~~
</expected>
            comp3.AssertTheseDiagnostics(expected3)

            Dim comp4 = CreateCompilation(source3, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp4.AssertTheseDiagnostics(expected3)

            Dim source5 =
<compilation>
    <file name="c.vb"><![CDATA[
Class B
    Inherits C

    Public Sub Test()
        Item(0) = 40
        Me(1) = 41
        Me.Item(2) = 42
        MyBase.Item(3) = 43
        MyClass.Item(4) = 44

        With Me
            .Item(5) = 45
        End With
    End Sub
End Class

]]></file>
</compilation>

            Dim comp5 = CreateCompilation(source5, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected5 =
<expected>
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Item(0) = 40
        ~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Me(1) = 41
        ~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Me.Item(2) = 42
        ~~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        MyBase.Item(3) = 43
        ~~~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        MyClass.Item(4) = 44
        ~~~~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Item(5) = 45
            ~~~~~~~~~~~~~
</expected>
            comp5.AssertTheseDiagnostics(expected5)

            Dim comp6 = CreateCompilation(source5, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp6.AssertTheseDiagnostics(expected5)
        End Sub

        <Fact>
        Public Sub EvaluationInitOnlySetter_05()

            Dim csSource =
"
public class C
{
    public int Property0 { get => 0; init { System.Console.Write(value + "" 0 ""); } }
    public int Property1 { get => 0; init { System.Console.Write(value + "" 1 ""); } }
    public int Property2 { get => 0; init { System.Console.Write(value + "" 2 ""); } }
    public int Property3 { get => 0; init { System.Console.Write(value + "" 3 ""); } }
    public int Property4 { get => 0; init { System.Console.Write(value + "" 4 ""); } }
    public int Property5 { get => 0; init { System.Console.Write(value + "" 5 ""); } }
    public int Property6 { get => 0; init { System.Console.Write(value + "" 6 ""); } }
    public int Property7 { get => 0; init { System.Console.Write(value + "" 7 ""); } }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim x as New B()
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Property0 += 41
        Me.Property3 += 44
        MyBase.Property4 += 45
        MyClass.Property5 += 46

        With Me
            .Property6 += 47
        End With
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:="41 0 44 3 45 4 46 5 47 6 ").VerifyDiagnostics()

            Assert.True(DirectCast(comp1.GetMember(Of PropertySymbol)("C.Property0").SetMethod, IMethodSymbol).IsInitOnly)

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(
<expected><![CDATA[
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Property0 += 41
        ~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Me.Property3 += 44
        ~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        MyBase.Property4 += 45
        ~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        MyClass.Property5 += 46
        ~~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            .Property6 += 47
            ~~~~~~~~~~~~~~~~
]]></expected>)

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim x = new C()
        x.Property1 += 42

        With New B()
            .Property2 += 43
        End With

        Dim y As New B() With { .F = Sub()
                                         .Property3 += 44
                                     End Sub}
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Dim y = new B()

        With y
            .Property4 += 45
        End With

        With Me
            With y
                .Property6 += 47
            End With
        End With

        Dim x as New B()
        x.Property0 += 41

        Dim z = Sub()
                  Property5 += 46  
                End Sub
    End Sub

    Public F As System.Action
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilation(source3, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected3 =
<expected>
BC37311: Init-only property 'Property1' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x.Property1 += 42
        ~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property2' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Property2 += 43
            ~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property3' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                                         .Property3 += 44
                                         ~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property4' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Property4 += 45
            ~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property6' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                .Property6 += 47
                ~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property0' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x.Property0 += 41
        ~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property5' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                  Property5 += 46  
                  ~~~~~~~~~~~~~~~
</expected>
            comp3.AssertTheseDiagnostics(expected3)

            Dim comp4 = CreateCompilation(source3, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp4.AssertTheseDiagnostics(expected3)

            Dim source5 =
<compilation>
    <file name="c.vb"><![CDATA[
Class B
    Inherits C

    Public Sub Test()
        Property0 += 41
        Me.Property3 += 44
        MyBase.Property4 += 45
        MyClass.Property5 += 46

        With Me
            .Property6 += 47
        End With
    End Sub
End Class

]]></file>
</compilation>

            Dim comp5 = CreateCompilation(source5, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected5 =
<expected>
BC37311: Init-only property 'Property0' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Property0 += 41
        ~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property3' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Me.Property3 += 44
        ~~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property4' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        MyBase.Property4 += 45
        ~~~~~~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property5' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        MyClass.Property5 += 46
        ~~~~~~~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property6' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Property6 += 47
            ~~~~~~~~~~~~~~~~
</expected>
            comp5.AssertTheseDiagnostics(expected5)

            Dim comp6 = CreateCompilation(source5, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp6.AssertTheseDiagnostics(expected5)
        End Sub

        <Fact>
        Public Sub EvaluationInitOnlySetter_06()

            Dim csSource =
"
public class C
{
    public int this[int x] { get => 0; init { System.Console.Write(value + "" ""); } }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim x = new B()
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Item(0) += 40
        Me.Item(0) += 41
        MyBase.Item(0) += 42
        MyClass.Item(0) += 43

        Me(0) += 44

        With Me
            .Item(0) += 45
        End With
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:="40 41 42 43 44 45 ").VerifyDiagnostics()

            Assert.True(DirectCast(comp1.GetMember(Of PropertySymbol)("C.Item").SetMethod, IMethodSymbol).IsInitOnly)

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(
<expected><![CDATA[
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Item(0) += 40
        ~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Me.Item(0) += 41
        ~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        MyBase.Item(0) += 42
        ~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        MyClass.Item(0) += 43
        ~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Me(0) += 44
        ~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            .Item(0) += 45
            ~~~~~~~~~~~~~~
]]></expected>)

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim x = new C()
        x(0) += 40
        x.Item(0) += 41

        With New B()
            .Item(0) += 42
        End With

        Dim y As New B() With { .F = Sub()
                                         .Item(0) += 43
                                     End Sub}
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Dim y = new B()

        With y
            .Item(0) += 44
        End With

        With Me
            With y
                .Item(0) += 45
            End With
        End With

        Dim x as New B()
        x(0) += 46
        x.Item(0) += 47

        Dim z = Sub()
                  Item(0) += 48  
                  Me(0) += 49  
                End Sub
    End Sub

    Public F As System.Action
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilation(source3, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected3 =
<expected>
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x(0) += 40
        ~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x.Item(0) += 41
        ~~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Item(0) += 42
            ~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                                         .Item(0) += 43
                                         ~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Item(0) += 44
            ~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                .Item(0) += 45
                ~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x(0) += 46
        ~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x.Item(0) += 47
        ~~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                  Item(0) += 48  
                  ~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                  Me(0) += 49  
                  ~~~~~~~~~~~
</expected>
            comp3.AssertTheseDiagnostics(expected3)

            Dim comp4 = CreateCompilation(source3, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp4.AssertTheseDiagnostics(expected3)

            Dim source5 =
<compilation>
    <file name="c.vb"><![CDATA[
Class B
    Inherits C

    Public Sub Test()
        Item(0) += 40
        Me(0) += 41
        Me.Item(0) += 42
        MyBase.Item(0) += 43
        MyClass.Item(0) += 44

        With Me
            .Item(0) += 45
        End With
    End Sub
End Class

]]></file>
</compilation>

            Dim comp5 = CreateCompilation(source5, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected5 =
<expected>
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Item(0) += 40
        ~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Me(0) += 41
        ~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Me.Item(0) += 42
        ~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        MyBase.Item(0) += 43
        ~~~~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        MyClass.Item(0) += 44
        ~~~~~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Item(0) += 45
            ~~~~~~~~~~~~~~
</expected>
            comp5.AssertTheseDiagnostics(expected5)

            Dim comp6 = CreateCompilation(source5, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp6.AssertTheseDiagnostics(expected5)
        End Sub

        <Fact>
        Public Sub EvaluationInitOnlySetter_07()

            Dim csSource =
"
public interface I
{
    public int Property1 { init; }
    public int Property2 { init; }
    public int Property3 { init; }
    public int Property4 { init; }
    public int Property5 { init; }
}

public class C : I
{
    public int Property1 { init { System.Console.Write(value + "" 1 ""); } }
    public int Property2 { init { System.Console.Write(value + "" 2 ""); } }
    public int Property3 { init { System.Console.Write(value + "" 3 ""); } }
    public int Property4 { init { System.Console.Write(value + "" 4 ""); } }
    public int Property5 { init { System.Console.Write(value + "" 5 ""); } }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        M1(Of C)()
        M2(Of B)()
    End Sub

    Shared Sub M1(OF T As {New, I})()
        Dim x = new T() With { .Property1 = 42 }
    End Sub

    Shared Sub M2(OF T As {New, C})()
        Dim x = new T() With { .Property2 = 43 }
    End Sub
End Class

Class B
    Inherits C
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:="42 1 43 2 ").VerifyDiagnostics()

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(
<expected><![CDATA[
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Dim x = new T() With { .Property1 = 42 }
                               ~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Dim x = new T() With { .Property2 = 43 }
                               ~~~~~~~~~~~~~~~
]]></expected>)

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
    End Sub

    Shared Sub M1(Of T As {New, I})()
        Dim x = New T()
        x.Property1 = 42

        With New T()
            .Property2 = 43
        End With
    End Sub

    Shared Sub M2(Of T As {New, C})()
        Dim x = New T()
        x.Property3 = 44

        With New T()
            .Property4 = 45
        End With
    End Sub

    Shared Sub M3(x As I)
        x.Property5 = 46
    End Sub
End Class

Class B
    Inherits C
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilation(source3, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected3 =
<expected>
BC37311: Init-only property 'Property1' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x.Property1 = 42
        ~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property2' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Property2 = 43
            ~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property3' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x.Property3 = 44
        ~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property4' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Property4 = 45
            ~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property5' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x.Property5 = 46
        ~~~~~~~~~~~~~~~~
</expected>
            comp3.AssertTheseDiagnostics(expected3)

            Dim comp4 = CreateCompilation(source3, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp4.AssertTheseDiagnostics(expected3)
        End Sub

        <Fact>
        Public Sub EvaluationInitOnlySetter_08()

            Dim csSource =
"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58277"")]
[CoClass(typeof(C))]
public interface I
{
    public int Property1 { init; }
    public int Property2 { init; }
}

[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")]
public class C : I
{
    int I.Property1 { init { System.Console.Write(value + "" 1 ""); } }
    int I.Property2 { init { System.Console.Write(value + "" 2 ""); } }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim x = new I() With { .Property1 = 42 }
    End Sub
End Class

Class B
    Inherits C
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:="42 1 ").VerifyDiagnostics()

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(
<expected><![CDATA[
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Dim x = new I() With { .Property1 = 42 }
                               ~~~~~~~~~~~~~~~
]]></expected>)

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        With New I()
            .Property2 = 43
        End With
    End Sub
End Class

Class B
    Inherits C
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilation(source3, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected3 =
<expected>
BC37311: Init-only property 'Property2' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Property2 = 43
            ~~~~~~~~~~~~~~~
</expected>
            comp3.AssertTheseDiagnostics(expected3)

            Dim comp4 = CreateCompilation(source3, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp4.AssertTheseDiagnostics(expected3)
        End Sub

        <Fact>
        Public Sub EvaluationInitOnlySetter_09()

            Dim csSource =
"
public class C
{
    public int Property1 { init { System.Console.Write(value + "" 1 ""); } }
    public int Property2 { init { System.Console.Write(value + "" 2 ""); } }
    public int Property3 { init { System.Console.Write(value + "" 3 ""); } }
    public int Property4 { init { System.Console.Write(value + "" 4 ""); } }
    public int Property5 { init { System.Console.Write(value + "" 5 ""); } }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Class B
    Inherits C

    Shared Sub New()
        Property1 = 41
        Me.Property2 = 42
        MyBase.Property3 = 43
        MyClass.Property4 = 44

        With Me
            .Property5 = 45
        End With
    End Sub
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilation(source3, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected3 =
<expected>
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
        Property1 = 41
        ~~~~~~~~~
BC30043: 'Me' is valid only within an instance method.
        Me.Property2 = 42
        ~~
BC30043: 'MyBase' is valid only within an instance method.
        MyBase.Property3 = 43
        ~~~~~~
BC30043: 'MyClass' is valid only within an instance method.
        MyClass.Property4 = 44
        ~~~~~~~
BC30043: 'Me' is valid only within an instance method.
        With Me
             ~~
BC37311: Init-only property 'Property5' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .Property5 = 45
            ~~~~~~~~~~~~~~~
</expected>
            comp3.AssertTheseDiagnostics(expected3)

            Dim comp4 = CreateCompilation(source3, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp4.AssertTheseDiagnostics(expected3)
        End Sub

        <Fact>
        Public Sub EvaluationInitOnlySetter_10()

            Dim csSource =
"
public class C
{
    public int P2 { init { System.Console.Write(value + "" 2 ""); } }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim x = new B()
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        With (Me)
            .P2 = 42
        End With
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:="42 2 ").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub Overriding_01()

            Dim csSource =
"
public class C
{
    public virtual int P0 { init { } }
    public virtual int P1 { init; get; }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class B1
    Inherits C

    Public Overrides WriteOnly Property P0 As Integer 
        Set
        End Set
    End Property 

    Public Overrides Property P1 As Integer 
End Class

Class B2
    Inherits C

    Public Overrides Property P0 As Integer

    Public Overrides ReadOnly Property P1 As Integer 
End Class

Class B3
    Inherits C

    Public Overrides ReadOnly Property P0 As Integer

    Public Overrides WriteOnly Property P1 As Integer 
        Set
        End Set
    End Property 
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected1 =
<expected>
BC37312: 'Public Overrides WriteOnly Property P0 As Integer' cannot override init-only 'Public Overridable Overloads WriteOnly Property P0 As Integer'.
    Public Overrides WriteOnly Property P0 As Integer 
                                        ~~
BC37312: 'Public Overrides Property P1 As Integer' cannot override init-only 'Public Overridable Overloads Property P1 As Integer'.
    Public Overrides Property P1 As Integer 
                              ~~
BC30362: 'Public Overrides Property P0 As Integer' cannot override 'Public Overridable Overloads WriteOnly Property P0 As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
    Public Overrides Property P0 As Integer
                              ~~
BC30362: 'Public Overrides ReadOnly Property P1 As Integer' cannot override 'Public Overridable Overloads Property P1 As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
    Public Overrides ReadOnly Property P1 As Integer 
                                       ~~
BC30362: 'Public Overrides ReadOnly Property P0 As Integer' cannot override 'Public Overridable Overloads WriteOnly Property P0 As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
    Public Overrides ReadOnly Property P0 As Integer
                                       ~~
BC30362: 'Public Overrides WriteOnly Property P1 As Integer' cannot override 'Public Overridable Overloads Property P1 As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
    Public Overrides WriteOnly Property P1 As Integer 
                                        ~~
</expected>
            comp1.AssertTheseDiagnostics(expected1)

            Dim p0Set = comp1.GetMember(Of PropertySymbol)("B1.P0").SetMethod
            Assert.False(p0Set.IsInitOnly)
            Assert.True(p0Set.OverriddenMethod.IsInitOnly)
            Dim p1Set = comp1.GetMember(Of PropertySymbol)("B1.P1").SetMethod
            Assert.False(p1Set.IsInitOnly)
            Assert.True(p1Set.OverriddenMethod.IsInitOnly)
            Assert.False(comp1.GetMember(Of PropertySymbol)("B2.P0").SetMethod.IsInitOnly)

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(expected1)
        End Sub

        <Fact>
        Public Sub Overriding_02()

            Dim csSource =
"
public class C<T>
{
    public virtual T this[int x] { init { } }
    public virtual T this[short x] { init {} get => throw null; }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class B1
    Inherits C(Of Integer)

    Public Overrides WriteOnly Property Item(x as Integer) As Integer 
        Set
        End Set
    End Property 

    Public Overrides Property Item(x as Short) As Integer 
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property 
End Class

Class B2
    Inherits C(Of Integer)

    Public Overrides Property Item(x as Integer) As Integer
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property 

    Public Overrides ReadOnly Property Item(x as Short) As Integer 
        Get
            Return Nothing
        End Get
    End Property 
End Class

Class B3
    Inherits C(Of Integer)

    Public Overrides ReadOnly Property Item(x as Integer) As Integer
        Get
            Return Nothing
        End Get
    End Property 

    Public Overrides WriteOnly Property Item(x as Short) As Integer 
        Set
        End Set
    End Property 
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected1 =
<expected>
BC37312: 'Public Overrides WriteOnly Property Item(x As Integer) As Integer' cannot override init-only 'Public Overridable Overloads WriteOnly Default Property Item(x As Integer) As Integer'.
    Public Overrides WriteOnly Property Item(x as Integer) As Integer 
                                        ~~~~
BC37312: 'Public Overrides Property Item(x As Short) As Integer' cannot override init-only 'Public Overridable Overloads Default Property Item(x As Short) As Integer'.
    Public Overrides Property Item(x as Short) As Integer 
                              ~~~~
BC30362: 'Public Overrides Property Item(x As Integer) As Integer' cannot override 'Public Overridable Overloads WriteOnly Default Property Item(x As Integer) As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
    Public Overrides Property Item(x as Integer) As Integer
                              ~~~~
BC30362: 'Public Overrides ReadOnly Property Item(x As Short) As Integer' cannot override 'Public Overridable Overloads Default Property Item(x As Short) As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
    Public Overrides ReadOnly Property Item(x as Short) As Integer 
                                       ~~~~
BC30362: 'Public Overrides ReadOnly Property Item(x As Integer) As Integer' cannot override 'Public Overridable Overloads WriteOnly Default Property Item(x As Integer) As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
    Public Overrides ReadOnly Property Item(x as Integer) As Integer
                                       ~~~~
BC30362: 'Public Overrides WriteOnly Property Item(x As Short) As Integer' cannot override 'Public Overridable Overloads Default Property Item(x As Short) As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
    Public Overrides WriteOnly Property Item(x as Short) As Integer 
                                        ~~~~
</expected>
            comp1.AssertTheseDiagnostics(expected1)

            Dim p0Set = comp1.GetTypeByMetadataName("B1").GetMembers("Item").OfType(Of PropertySymbol).First().SetMethod
            Assert.False(p0Set.IsInitOnly)
            Assert.True(p0Set.OverriddenMethod.IsInitOnly)
            Assert.True(DirectCast(p0Set.OverriddenMethod, IMethodSymbol).IsInitOnly)

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(expected1)
        End Sub

        <Fact>
        Public Sub Overriding_03()

            Dim csSource =
"
public class A
{
    public virtual int P1 { init; get; }
    public virtual int P2 { init; get; }
}

public class B : A
{
    public override int P1 { get => throw null; }
    public override int P2 { init {} }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class C1
    Inherits B

    Public Overrides WriteOnly Property P1 As Integer 
        Set
        End Set
    End Property 

    Public Overrides WriteOnly Property P2 As Integer 
        Set
        End Set
    End Property 
End Class

Class C2
    Inherits B

    Public Overrides Property P1 As Integer

    Public Overrides Property P2 As Integer 
End Class

Class C3
    Inherits B

    Public Overrides ReadOnly Property P1 As Integer

    Public Overrides ReadOnly Property P2 As Integer 
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected1 =
<expected>
BC30362: 'Public Overrides WriteOnly Property P1 As Integer' cannot override 'Public Overrides ReadOnly Property P1 As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
    Public Overrides WriteOnly Property P1 As Integer 
                                        ~~
BC37312: 'Public Overrides WriteOnly Property P2 As Integer' cannot override init-only 'Public Overrides WriteOnly Property P2 As Integer'.
    Public Overrides WriteOnly Property P2 As Integer 
                                        ~~
BC30362: 'Public Overrides Property P1 As Integer' cannot override 'Public Overrides ReadOnly Property P1 As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
    Public Overrides Property P1 As Integer
                              ~~
BC30362: 'Public Overrides Property P2 As Integer' cannot override 'Public Overrides WriteOnly Property P2 As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
    Public Overrides Property P2 As Integer 
                              ~~
BC30362: 'Public Overrides ReadOnly Property P2 As Integer' cannot override 'Public Overrides WriteOnly Property P2 As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
    Public Overrides ReadOnly Property P2 As Integer 
                                       ~~
</expected>
            comp1.AssertTheseDiagnostics(expected1)

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(expected1)
        End Sub

        <Fact()>
        Public Sub Overriding_04()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1
       extends System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      IL_0000: ldarg.0
      IL_0001: call instance void System.Object::.ctor()
      IL_0006: ret
    }

    .method public hidebysig newslot virtual
            instance int32 get_P() cil managed
    {
      ldc.i4.s   123
      ret
    } 

    .method public hidebysig newslot virtual
            instance void set_P(int32 x) cil managed
    {
      ret
    } 

    .property instance int32 P()
    {
      .get instance int32 CL1::get_P()
      .set instance void CL1::set_P(int32)
    } 

    .method public hidebysig newslot virtual
            instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) get_P() cil managed
    {
      ldc.i4.s   123
      ret
    } 

    .method public hidebysig newslot virtual
            instance void modreq(System.Runtime.CompilerServices.IsExternalInit) set_P(int32 x) cil managed
    {
      ret
    } 

    .property instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) P()
    {
      .get instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) CL1::get_P()
      .set instance void modreq(System.Runtime.CompilerServices.IsExternalInit) CL1::set_P(int32)
    } 
} // end of class CL1

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test
    Inherits CL1

    Overrides Property P As Integer
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseDll)
            CompileAndVerify(compilation).VerifyDiagnostics()

            Dim p = compilation.GetMember(Of PropertySymbol)("Test.P")
            Dim pSet = p.SetMethod
            Assert.False(pSet.IsInitOnly)
            Assert.False(pSet.OverriddenMethod.IsInitOnly)
            Assert.Empty(pSet.OverriddenMethod.ReturnTypeCustomModifiers)
            Assert.Empty(p.GetMethod.OverriddenMethod.ReturnTypeCustomModifiers)
            Assert.Empty(p.OverriddenProperty.TypeCustomModifiers)
        End Sub

        <Fact()>
        Public Sub Overriding_05()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1
       extends System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      IL_0000: ldarg.0
      IL_0001: call instance void System.Object::.ctor()
      IL_0006: ret
    }

    .method public hidebysig newslot virtual
            instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) get_P() cil managed
    {
      ldc.i4.s   123
      ret
    } 

    .method public hidebysig newslot virtual
            instance void modreq(System.Runtime.CompilerServices.IsExternalInit) set_P(int32 x) cil managed
    {
      ret
    } 

    .property instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) P()
    {
      .get instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) CL1::get_P()
      .set instance void modreq(System.Runtime.CompilerServices.IsExternalInit) CL1::set_P(int32)
    } 

    .method public hidebysig newslot virtual
            instance int32 get_P() cil managed
    {
      ldc.i4.s   123
      ret
    } 

    .method public hidebysig newslot virtual
            instance void set_P(int32 x) cil managed
    {
      ret
    } 

    .property instance int32 P()
    {
      .get instance int32 CL1::get_P()
      .set instance void CL1::set_P(int32)
    } 
} // end of class CL1

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test
    Inherits CL1

    Overrides Property P As Integer
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseDll)

            CompileAndVerify(compilation).VerifyDiagnostics()

            Dim p = compilation.GetMember(Of PropertySymbol)("Test.P")
            Dim pSet = p.SetMethod
            Assert.False(pSet.IsInitOnly)
            Assert.False(pSet.OverriddenMethod.IsInitOnly)
            Assert.Empty(pSet.OverriddenMethod.ReturnTypeCustomModifiers)
            Assert.Empty(p.GetMethod.OverriddenMethod.ReturnTypeCustomModifiers)
            Assert.Empty(p.OverriddenProperty.TypeCustomModifiers)
        End Sub

        <Fact()>
        Public Sub Overriding_06()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1
       extends System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      IL_0000: ldarg.0
      IL_0001: call instance void System.Object::.ctor()
      IL_0006: ret
    }

    .method public hidebysig newslot virtual
            instance int32 get_P() cil managed
    {
      ldc.i4.s   123
      ret
    } 

    .method public hidebysig newslot virtual
            instance void set_P(int32 x) cil managed
    {
      ret
    } 

    .property instance int32 modopt(CL1) P()
    {
      .get instance int32 CL1::get_P()
      .set instance void CL1::set_P(int32)
    } 

    .method public hidebysig newslot virtual
            instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) get_P() cil managed
    {
      ldc.i4.s   123
      ret
    } 

    .method public hidebysig newslot virtual
            instance void modreq(System.Runtime.CompilerServices.IsExternalInit) set_P(int32 x) cil managed
    {
      ret
    } 

    .property instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) P()
    {
      .get instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) CL1::get_P()
      .set instance void modreq(System.Runtime.CompilerServices.IsExternalInit) CL1::set_P(int32)
    } 
} // end of class CL1

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test
    Inherits CL1

    Overrides Property P As Integer
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(
<expected>
BC30935: Member 'Public Overridable Overloads Property P As Integer' that matches this signature cannot be overridden because the class 'CL1' contains multiple members with this same name and signature: 
   'Public Overridable Overloads Property P As Integer'
   'Public Overridable Overloads Property P As Integer'
    Overrides Property P As Integer
                       ~
</expected>)

            Dim p = compilation.GetMember(Of PropertySymbol)("Test.P")
            Dim pSet = p.SetMethod
            Assert.False(pSet.IsInitOnly)
            Assert.False(pSet.OverriddenMethod.IsInitOnly)
            Assert.Empty(pSet.OverriddenMethod.ReturnTypeCustomModifiers)
            Assert.Empty(p.GetMethod.OverriddenMethod.ReturnTypeCustomModifiers)
        End Sub

        <Fact()>
        Public Sub Overriding_07()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1
       extends System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      IL_0000: ldarg.0
      IL_0001: call instance void System.Object::.ctor()
      IL_0006: ret
    }

    .method public hidebysig newslot virtual
            instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) get_P() cil managed
    {
      ldc.i4.s   123
      ret
    } 

    .method public hidebysig newslot virtual
            instance void modreq(System.Runtime.CompilerServices.IsExternalInit) set_P(int32 x) cil managed
    {
      ret
    } 

    .property instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) P()
    {
      .get instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) CL1::get_P()
      .set instance void modreq(System.Runtime.CompilerServices.IsExternalInit) CL1::set_P(int32)
    } 

    .method public hidebysig newslot virtual
            instance int32 get_P() cil managed
    {
      ldc.i4.s   123
      ret
    } 

    .method public hidebysig newslot virtual
            instance void set_P(int32 x) cil managed
    {
      ret
    } 

    .property instance int32 modopt(CL1) P()
    {
      .get instance int32 CL1::get_P()
      .set instance void CL1::set_P(int32)
    } 
} // end of class CL1

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test
    Inherits CL1

    Overrides Property P As Integer
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(
<expected>
BC30935: Member 'Public Overridable Overloads Property P As Integer' that matches this signature cannot be overridden because the class 'CL1' contains multiple members with this same name and signature: 
   'Public Overridable Overloads Property P As Integer'
   'Public Overridable Overloads Property P As Integer'
    Overrides Property P As Integer
                       ~
</expected>)

            Dim p = compilation.GetMember(Of PropertySymbol)("Test.P")
            Dim pSet = p.SetMethod
            Assert.False(pSet.IsInitOnly)
            Assert.True(pSet.OverriddenMethod.IsInitOnly)
            Assert.NotEmpty(pSet.OverriddenMethod.ReturnTypeCustomModifiers)
            Assert.NotEmpty(p.GetMethod.OverriddenMethod.ReturnTypeCustomModifiers)
        End Sub

        <Fact()>
        Public Sub Overriding_08()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit CL1
       extends System.Object
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      IL_0000: ldarg.0
      IL_0001: call instance void System.Object::.ctor()
      IL_0006: ret
    }

    .method public hidebysig newslot virtual
            instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) get_P1() cil managed
    {
      ldc.i4.s   123
      ret
    } 

    .method public hidebysig newslot virtual
            instance void modreq(System.Runtime.CompilerServices.IsExternalInit) set_P1(int32 x) cil managed
    {
      ret
    } 

    .property instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) P1()
    {
      .get instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) CL1::get_P1()
      .set instance void modreq(System.Runtime.CompilerServices.IsExternalInit) CL1::set_P1(int32)
    } 

    .method public hidebysig newslot virtual
            instance int32 get_P1() cil managed
    {
      ldc.i4.s   123
      ret
    } 

    .method public hidebysig newslot virtual
            instance void set_P1(int32 x) cil managed
    {
      ret
    } 

    .property instance int32 P1()
    {
      .get instance int32 CL1::get_P1()
      .set instance void CL1::set_P1(int32)
    } 

    .method public hidebysig newslot virtual
            instance int32 get_P2() cil managed
    {
      ldc.i4.s   123
      ret
    } 

    .method public hidebysig newslot virtual
            instance void set_P2(int32 x) cil managed
    {
      ret
    } 

    .property instance int32 P2()
    {
      .get instance int32 CL1::get_P2()
      .set instance void CL1::set_P2(int32)
    } 

    .method public hidebysig newslot virtual
            instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) get_P2() cil managed
    {
      ldc.i4.s   123
      ret
    } 

    .method public hidebysig newslot virtual
            instance void modreq(System.Runtime.CompilerServices.IsExternalInit) set_P2(int32 x) cil managed
    {
      ret
    } 

    .property instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) P2()
    {
      .get instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) CL1::get_P2()
      .set instance void modreq(System.Runtime.CompilerServices.IsExternalInit) CL1::set_P2(int32)
    } 
} // end of class CL1

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}

.class public auto ansi beforefieldinit CL2
       extends CL1
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      IL_0000: ldarg.0
      IL_0001: call instance void CL1::.ctor()
      IL_0006: ret
    }

    .method public hidebysig virtual
            instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) get_P1() cil managed
    {
      ldc.i4.s   123
      ret
    } 

    .method public hidebysig virtual
            instance void modreq(System.Runtime.CompilerServices.IsExternalInit) set_P1(int32 x) cil managed
    {
      ret
    } 

    .property instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) P1()
    {
      .get instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) CL2::get_P1()
      .set instance void modreq(System.Runtime.CompilerServices.IsExternalInit) CL2::set_P1(int32)
    } 

    .method public hidebysig virtual
            instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) get_P2() cil managed
    {
      ldc.i4.s   123
      ret
    } 

    .method public hidebysig virtual
            instance void modreq(System.Runtime.CompilerServices.IsExternalInit) set_P2(int32 x) cil managed
    {
      ret
    } 

    .property instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) P2()
    {
      .get instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) CL2::get_P2()
      .set instance void modreq(System.Runtime.CompilerServices.IsExternalInit) CL2::set_P2(int32)
    } 
}

.class public auto ansi beforefieldinit CL3
       extends CL1
{
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      IL_0000: ldarg.0
      IL_0001: call instance void CL1::.ctor()
      IL_0006: ret
    }

    .method public hidebysig virtual
            instance int32 get_P1() cil managed
    {
      ldc.i4.s   123
      ret
    } 

    .method public hidebysig virtual
            instance void set_P1(int32 x) cil managed
    {
      ret
    } 

    .property instance int32 P1()
    {
      .get instance int32 CL3::get_P1()
      .set instance void CL3::set_P1(int32)
    } 

    .method public hidebysig virtual
            instance int32 get_P2() cil managed
    {
      ldc.i4.s   123
      ret
    } 

    .method public hidebysig virtual
            instance void set_P2(int32 x) cil managed
    {
      ret
    } 

    .property instance int32 P2()
    {
      .get instance int32 CL3::get_P2()
      .set instance void CL3::set_P2(int32)
    } 
}
]]>.Value

            Dim vbSource =
<compilation>
    <file name="c.vb"><![CDATA[
]]></file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseDll)

            Dim cl2p1 = compilation.GetMember(Of PropertySymbol)("CL2.P1")
            Assert.NotEmpty(cl2p1.SetMethod.OverriddenMethod.ReturnTypeCustomModifiers)
            Assert.NotEmpty(cl2p1.GetMethod.OverriddenMethod.ReturnTypeCustomModifiers)
            Assert.NotEmpty(cl2p1.OverriddenProperty.TypeCustomModifiers)

            Dim cl2p2 = compilation.GetMember(Of PropertySymbol)("CL2.P2")
            Assert.NotEmpty(cl2p2.SetMethod.OverriddenMethod.ReturnTypeCustomModifiers)
            Assert.NotEmpty(cl2p2.GetMethod.OverriddenMethod.ReturnTypeCustomModifiers)
            Assert.NotEmpty(cl2p2.OverriddenProperty.TypeCustomModifiers)

            Dim cl3p1 = compilation.GetMember(Of PropertySymbol)("CL3.P1")
            Assert.Empty(cl3p1.SetMethod.OverriddenMethod.ReturnTypeCustomModifiers)
            Assert.Empty(cl3p1.GetMethod.OverriddenMethod.ReturnTypeCustomModifiers)
            Assert.Empty(cl3p1.OverriddenProperty.TypeCustomModifiers)

            Dim cl3p2 = compilation.GetMember(Of PropertySymbol)("CL3.P2")
            Assert.Empty(cl3p2.SetMethod.OverriddenMethod.ReturnTypeCustomModifiers)
            Assert.Empty(cl3p2.GetMethod.OverriddenMethod.ReturnTypeCustomModifiers)
            Assert.Empty(cl3p2.OverriddenProperty.TypeCustomModifiers)
        End Sub

        <Fact>
        Public Sub Implementing_01()

            Dim csSource =
"
public interface I
{
    int P0 { init; }
    int P1 { init; get; }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class B1
    Implements I

    Public WriteOnly Property P0 As Integer Implements I.P0
        Set
        End Set
    End Property 

    Public Property P1 As Integer Implements I.P1 
End Class

Class B2
    Implements I

    Public Property P0 As Integer Implements I.P0

    Public ReadOnly Property P1 As Integer Implements I.P1 
End Class

Class B3
    Implements I

    Public ReadOnly Property P0 As Integer Implements I.P0

    Public WriteOnly Property P1 As Integer Implements I.P1 
        Set
        End Set
    End Property 
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected1 =
<expected>
BC37313: Init-only 'WriteOnly Property P0 As Integer' cannot be implemented.
    Public WriteOnly Property P0 As Integer Implements I.P0
                                                       ~~~~
BC37313: Init-only 'Property P1 As Integer' cannot be implemented.
    Public Property P1 As Integer Implements I.P1 
                                             ~~~~
BC37313: Init-only 'WriteOnly Property P0 As Integer' cannot be implemented.
    Public Property P0 As Integer Implements I.P0
                                             ~~~~
BC31444: 'Property P1 As Integer' cannot be implemented by a ReadOnly property.
    Public ReadOnly Property P1 As Integer Implements I.P1 
                                                      ~~~~
BC31444: 'WriteOnly Property P0 As Integer' cannot be implemented by a ReadOnly property.
    Public ReadOnly Property P0 As Integer Implements I.P0
                                                      ~~~~
BC31444: 'Property P1 As Integer' cannot be implemented by a WriteOnly property.
    Public WriteOnly Property P1 As Integer Implements I.P1 
                                                       ~~~~
BC37313: Init-only 'Property P1 As Integer' cannot be implemented.
    Public WriteOnly Property P1 As Integer Implements I.P1 
                                                       ~~~~
</expected>
            comp1.AssertTheseDiagnostics(expected1)

            Dim p0Set = comp1.GetMember(Of PropertySymbol)("B1.P0").SetMethod
            Assert.False(p0Set.IsInitOnly)
            Dim p1Set = comp1.GetMember(Of PropertySymbol)("B1.P1").SetMethod
            Assert.False(p1Set.IsInitOnly)
            Assert.False(comp1.GetMember(Of PropertySymbol)("B2.P0").SetMethod.IsInitOnly)

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(expected1)
        End Sub

        <Fact>
        Public Sub Implementing_02()

            Dim csSource =
"
public interface I
{
    int this[int x] { init; }
    int this[short x] { init; get; }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class B1
    Implements I

    Public WriteOnly Property Item(x As Integer) As Integer Implements I.Item
        Set
        End Set
    End Property 

    Public Property Item(x As Short) As Integer Implements I.Item 
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property 
End Class

Class B2
    Implements I

    Public Property Item(x As Integer) As Integer Implements I.Item
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property 

    Public ReadOnly Property Item(x As Short) As Integer Implements I.Item 
        Get
            Return Nothing
        End Get
    End Property 
End Class

Class B3
    Implements I

    Public ReadOnly Property Item(x As Integer) As Integer Implements I.Item
        Get
            Return Nothing
        End Get
    End Property 

    Public WriteOnly Property Item(x As Short) As Integer Implements I.Item 
        Set
        End Set
    End Property 
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected1 =
<expected>
BC37313: Init-only 'WriteOnly Default Property Item(x As Integer) As Integer' cannot be implemented.
    Public WriteOnly Property Item(x As Integer) As Integer Implements I.Item
                                                                       ~~~~~~
BC37313: Init-only 'Default Property Item(x As Short) As Integer' cannot be implemented.
    Public Property Item(x As Short) As Integer Implements I.Item 
                                                           ~~~~~~
BC37313: Init-only 'WriteOnly Default Property Item(x As Integer) As Integer' cannot be implemented.
    Public Property Item(x As Integer) As Integer Implements I.Item
                                                             ~~~~~~
BC31444: 'Default Property Item(x As Short) As Integer' cannot be implemented by a ReadOnly property.
    Public ReadOnly Property Item(x As Short) As Integer Implements I.Item 
                                                                    ~~~~~~
BC31444: 'WriteOnly Default Property Item(x As Integer) As Integer' cannot be implemented by a ReadOnly property.
    Public ReadOnly Property Item(x As Integer) As Integer Implements I.Item
                                                                      ~~~~~~
BC31444: 'Default Property Item(x As Short) As Integer' cannot be implemented by a WriteOnly property.
    Public WriteOnly Property Item(x As Short) As Integer Implements I.Item 
                                                                     ~~~~~~
BC37313: Init-only 'Default Property Item(x As Short) As Integer' cannot be implemented.
    Public WriteOnly Property Item(x As Short) As Integer Implements I.Item 
                                                                     ~~~~~~
</expected>
            comp1.AssertTheseDiagnostics(expected1)

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(expected1)
        End Sub

        <Fact>
        Public Sub Implementing_03()

            Dim csSource =
"
public interface I
{
    int P0 { set; get; }
    int P1 { init; get; }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class B2
    Implements I

    Public Property P0 As Integer Implements I.P0, I.P1 
End Class

Class B3
    Implements I

    Public Property P0 As Integer Implements I.P1, I.P0 
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected1 =
<expected>
BC37313: Init-only 'Property P1 As Integer' cannot be implemented.
    Public Property P0 As Integer Implements I.P0, I.P1 
                                                   ~~~~
BC37313: Init-only 'Property P1 As Integer' cannot be implemented.
    Public Property P0 As Integer Implements I.P1, I.P0 
                                             ~~~~
</expected>
            comp1.AssertTheseDiagnostics(expected1)

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(expected1)
        End Sub

        <Fact()>
        Public Sub Implementing_04()
            Dim ilSource = <![CDATA[
.class interface public abstract auto ansi CL1
{
    .method public hidebysig newslot specialname abstract virtual 
            instance int32 get_P() cil managed
    {
    } 

    .method public hidebysig newslot specialname abstract virtual 
            instance void set_P(int32 x) cil managed
    {
    } 

    .property instance int32 P()
    {
      .get instance int32 CL1::get_P()
      .set instance void CL1::set_P(int32)
    } 

    .method public hidebysig newslot specialname abstract virtual 
            instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) get_P() cil managed
    {
    } 

    .method public hidebysig newslot specialname abstract virtual 
            instance void modreq(System.Runtime.CompilerServices.IsExternalInit) set_P(int32 x) cil managed
    {
    } 

    .property instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) P()
    {
      .get instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) CL1::get_P()
      .set instance void modreq(System.Runtime.CompilerServices.IsExternalInit) CL1::set_P(int32)
    } 
} // end of class CL1

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test
    Implements CL1

    Property P As Integer Implements CL1.P
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(
<expected>
BC30149: Class 'Test' must implement 'Property P As Integer' for interface 'CL1'.
    Implements CL1
               ~~~
BC30937: Member 'CL1.P' that matches this signature cannot be implemented because the interface 'CL1' contains multiple members with this same name and signature:
   'Property P As Integer'
   'Property P As Integer'
    Property P As Integer Implements CL1.P
                                     ~~~~~
</expected>)

            Dim p = compilation.GetMember(Of PropertySymbol)("Test.P")
            Dim pSet = p.SetMethod
            Assert.False(pSet.IsInitOnly)
            Assert.False(pSet.ExplicitInterfaceImplementations.Single().IsInitOnly)
            Assert.Empty(pSet.ExplicitInterfaceImplementations.Single().ReturnTypeCustomModifiers)
            Assert.Empty(p.GetMethod.ExplicitInterfaceImplementations.Single().ReturnTypeCustomModifiers)
            Assert.Empty(p.ExplicitInterfaceImplementations.Single().TypeCustomModifiers)
        End Sub

        <Fact()>
        Public Sub Implementing_05()
            Dim ilSource = <![CDATA[
.class interface public abstract auto ansi CL1
{
    .method public hidebysig newslot specialname abstract virtual 
            instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) get_P() cil managed
    {
    } 

    .method public hidebysig newslot specialname abstract virtual 
            instance void modreq(System.Runtime.CompilerServices.IsExternalInit) set_P(int32 x) cil managed
    {
    } 

    .property instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) P()
    {
      .get instance int32 modopt(System.Runtime.CompilerServices.IsExternalInit) CL1::get_P()
      .set instance void modreq(System.Runtime.CompilerServices.IsExternalInit) CL1::set_P(int32)
    } 

    .method public hidebysig newslot specialname abstract virtual 
            instance int32 get_P() cil managed
    {
    } 

    .method public hidebysig newslot specialname abstract virtual 
            instance void set_P(int32 x) cil managed
    {
    } 

    .property instance int32 P()
    {
      .get instance int32 CL1::get_P()
      .set instance void CL1::set_P(int32)
    } 
} // end of class CL1

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test
    Implements CL1

    Property P As Integer Implements CL1.P
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(
<expected>
BC30149: Class 'Test' must implement 'Property P As Integer' for interface 'CL1'.
    Implements CL1
               ~~~
BC30937: Member 'CL1.P' that matches this signature cannot be implemented because the interface 'CL1' contains multiple members with this same name and signature:
   'Property P As Integer'
   'Property P As Integer'
    Property P As Integer Implements CL1.P
                                     ~~~~~
</expected>)

            Dim p = compilation.GetMember(Of PropertySymbol)("Test.P")
            Dim pSet = p.SetMethod
            Assert.False(pSet.IsInitOnly)
            Assert.True(pSet.ExplicitInterfaceImplementations.Single().IsInitOnly)
            Assert.NotEmpty(pSet.ExplicitInterfaceImplementations.Single().ReturnTypeCustomModifiers)
            Assert.NotEmpty(p.GetMethod.ExplicitInterfaceImplementations.Single().ReturnTypeCustomModifiers)
            Assert.NotEmpty(p.ExplicitInterfaceImplementations.Single().TypeCustomModifiers)
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        <WorkItem(56665, "https://github.com/dotnet/roslyn/issues/56665")>
        Public Sub LateBound_01()

            Dim csSource =
"
public class C
{
    public int P0 { init; get; }
    public int P1 { init; get; }
    public int P2 { init; get; }
    public int P3 { init; get; }
    private int[] _item = new int[10];
    public int this[int x] { init => _item[x] = value; get => _item[x]; }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim b = new B()
        Dim ob As Object = b
        Dim x = new C()
        Dim ox As Object = x

        Try
            ox.P0 = -40
        Catch ex As System.MissingMemberException
            System.Console.Write(x.P0)
            System.Console.Write(" ")
        End Try

        b.Init(ox.P1, -41)
        ob.Init(x.P2, -42)
        ob.Init(ox.P3, -43)

        System.Console.Write(x.P1)
        System.Console.Write(" ")
        System.Console.Write(x.P2)
        System.Console.Write(" ")
        System.Console.Write(x.P3)

        Try
            ox(0) = 40
        Catch ex As System.MissingMemberException
            System.Console.Write(" ")
            System.Console.Write(x(0))
        End Try

        Try
            ox.Item(1) = 41
        Catch ex As System.MissingMemberException
            System.Console.Write(" ")
            System.Console.Write(x(1))
        End Try

        b.Init(ox(2), 42)
        ob.Init(x(3), 43)
        b.Init(ox.Item(4), 44)
        ob.Init(x.Item(5), 45)
        ob.Init(ox(6), 46)
        ob.Init(ox.Item(7), 47)

        For i as Integer = 2 To 7
            System.Console.Write(" ")
            System.Console.Write(x(i))
        Next
    End Sub
End Class

Class B
    Public Sub Init(ByRef p as Integer, val As Integer)
        p = val
    End Sub
End Class
]]></file>
</compilation>

            Dim expectedOutput As String = "0 0 0 0 0 0 0 0 0 0 0 0"
            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=expectedOutput).VerifyDiagnostics()

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp2, expectedOutput:=expectedOutput).VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(CoreClrOnly))>
        <WorkItem(56665, "https://github.com/dotnet/roslyn/issues/56665")>
        Public Sub LateBound_02()

            Dim csSource =
"
public class C
{
    private int[] _item = new int[12];
    public int this[int x] { init => _item[x] = value; get => _item[x]; }
    public int this[short x] { init => throw null; get => throw null; }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim b = new B()
        Dim ob As Object = b
        Dim x = new C()
        Dim ox As Object = x

        Try
            ox(0) = 40
        Catch ex As System.MissingMemberException
            System.Console.Write(x(0))
        End Try

        Try
            ox.Item(1) = 41
        Catch ex As System.MissingMemberException
            System.Console.Write(" ")
            System.Console.Write(x(1))
        End Try

        Try
            x(CObj(2)) = 42
        Catch ex As System.MissingMemberException
            System.Console.Write(" ")
            System.Console.Write(x(2))
        End Try

        Try
            x.Item(CObj(3)) = 43
        Catch ex As System.MissingMemberException
            System.Console.Write(" ")
            System.Console.Write(x(3))
        End Try

        b.Init(ox(4), 44)
        ob.Init(ox(5), 45)
        b.Init(ox.Item(6), 46)
        ob.Init(ox.Item(7), 47)
        b.Init(x(CObj(8)), 48)
        ob.Init(x(CObj(9)), 49)
        b.Init(x.Item(CObj(10)), 50)
        ob.Init(x.Item(CObj(11)), 51)

        For i as Integer = 4 To 11
            System.Console.Write(" ")
            System.Console.Write(x(i))
        Next
    End Sub
End Class

Class B
    Public Sub Init(ByRef p as Integer, val As Integer)
        p = val
    End Sub
End Class
]]></file>
</compilation>

            Dim expectedOutput As String = "0 0 0 0 0 0 0 0 0 0 0 0"
            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=expectedOutput).VerifyDiagnostics()

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp2, expectedOutput:=expectedOutput).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub Redim_01()

            Dim csSource =
"
public class C
{
    public int[] Property0 { init; get; }
    public int[] Property1 { init; get; }
    public int[] Property2 { init; get; }
    public int[] Property3 { init; get; }
    public int[] Property4 { init; get; }
    public int[] Property5 { init; get; }
    public int[] Property6 { init; get; }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim b = new B()

        System.Console.Write(b.Property0.Length)
        System.Console.Write(" "c)
        System.Console.Write(b.Property3.Length)
        System.Console.Write(" "c)
        System.Console.Write(b.Property4.Length)
        System.Console.Write(" "c)
        System.Console.Write(b.Property5.Length)
        System.Console.Write(" "c)
        System.Console.Write(b.Property6.Length)
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        ReDim Property0(41)
        Redim Me.Property3(44), MyBase.Property4(45), MyClass.Property5(46)

        With Me
            Redim .Property6(47)
        End With
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16_9, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:="42 45 46 47 48").VerifyDiagnostics()

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(
<expected><![CDATA[
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        ReDim Property0(41)
              ~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Redim Me.Property3(44), MyBase.Property4(45), MyClass.Property5(46)
              ~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Redim Me.Property3(44), MyBase.Property4(45), MyClass.Property5(46)
                                ~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Redim Me.Property3(44), MyBase.Property4(45), MyClass.Property5(46)
                                                      ~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            Redim .Property6(47)
                  ~~~~~~~~~~
]]></expected>)

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim x = new C()
        ReDim x.Property1(42)

        With New B()
            Redim .Property2(43)
        End With

        Dim y As New B() With { .F = Sub()
                                         ReDim .Property3(44)
                                     End Sub}
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Dim y = new B()

        With y
            Redim .Property4(45)
        End With

        With Me
            With y
                Redim .Property6(47)
            End With
        End With

        Dim x as New B()
        Redim x.Property0(41)

        Dim z = Sub()
                  Redim Property5(46)
                End Sub
    End Sub

    Public F As System.Action
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilation(source3, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected3 =
<expected>
BC37311: Init-only property 'Property1' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        ReDim x.Property1(42)
              ~~~~~~~~~~~
BC37311: Init-only property 'Property2' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            Redim .Property2(43)
                  ~~~~~~~~~~
BC37311: Init-only property 'Property3' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                                         ReDim .Property3(44)
                                               ~~~~~~~~~~
BC37311: Init-only property 'Property4' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            Redim .Property4(45)
                  ~~~~~~~~~~
BC37311: Init-only property 'Property6' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                Redim .Property6(47)
                      ~~~~~~~~~~
BC37311: Init-only property 'Property0' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Redim x.Property0(41)
              ~~~~~~~~~~~
BC37311: Init-only property 'Property5' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                  Redim Property5(46)
                        ~~~~~~~~~
</expected>
            comp3.AssertTheseDiagnostics(expected3)

            Dim comp4 = CreateCompilation(source3, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp4.AssertTheseDiagnostics(expected3)

            Dim source5 =
<compilation>
    <file name="c.vb"><![CDATA[
Class B
    Inherits C

    Public Sub Test()
        ReDim Property0(41)
        ReDim Me.Property3(44), MyBase.Property4(45), MyClass.Property5(46)

        With Me
            ReDim .Property6(47)
        End With
    End Sub
End Class

]]></file>
</compilation>

            Dim comp5 = CreateCompilation(source5, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected5 =
<expected>
BC37311: Init-only property 'Property0' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        ReDim Property0(41)
              ~~~~~~~~~
BC37311: Init-only property 'Property3' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        ReDim Me.Property3(44), MyBase.Property4(45), MyClass.Property5(46)
              ~~~~~~~~~~~~
BC37311: Init-only property 'Property4' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        ReDim Me.Property3(44), MyBase.Property4(45), MyClass.Property5(46)
                                ~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property5' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        ReDim Me.Property3(44), MyBase.Property4(45), MyClass.Property5(46)
                                                      ~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property6' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            ReDim .Property6(47)
                  ~~~~~~~~~~
</expected>
            comp5.AssertTheseDiagnostics(expected5)

            Dim comp6 = CreateCompilation(source5, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp6.AssertTheseDiagnostics(expected5)
        End Sub

        <Fact>
        Public Sub Redim_02()

            Dim csSource =
"
public class C
{
    private int[][] _item = new int[6][];
    public int[] this[int x]
    {
        init
        {
            _item[x] = value;
        }

        get => _item[x];
    }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim b = new B()
        for i as Integer = 0 To 5
            System.Console.Write(b(i).Length)
            System.Console.Write(" "c)
        Next
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        ReDim Item(0)(40)
        ReDim Me(1)(41),  Me.Item(2)(42), MyBase.Item(3)(43), MyClass.Item(4)(44)

        With Me
            ReDim .Item(5)(45)
        End With
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16_9, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:="41 42 43 44 45 46").VerifyDiagnostics()

            Assert.True(DirectCast(comp1.GetMember(Of PropertySymbol)("C.Item").SetMethod, IMethodSymbol).IsInitOnly)

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(
<expected><![CDATA[
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        ReDim Item(0)(40)
              ~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        ReDim Me(1)(41),  Me.Item(2)(42), MyBase.Item(3)(43), MyClass.Item(4)(44)
              ~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        ReDim Me(1)(41),  Me.Item(2)(42), MyBase.Item(3)(43), MyClass.Item(4)(44)
                          ~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        ReDim Me(1)(41),  Me.Item(2)(42), MyBase.Item(3)(43), MyClass.Item(4)(44)
                                          ~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        ReDim Me(1)(41),  Me.Item(2)(42), MyBase.Item(3)(43), MyClass.Item(4)(44)
                                                              ~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            ReDim .Item(5)(45)
                  ~~~~~~~~
]]></expected>)

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim x = new C()
        ReDim x(0)(40)
        ReDim x.Item(1)(41)

        With New B()
            ReDim .Item(2)(42)
        End With

        Dim y As New B() With { .F = Sub()
                                         ReDim .Item(3)(43)
                                     End Sub}
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Dim y = new B()

        With y
            ReDim .Item(4)(44)
        End With

        With Me
            With y
                ReDim .Item(5)(45)
            End With
        End With

        Dim x as New B()
        ReDim x(6)(46)
        ReDim x.Item(7)(47)

        Dim z = Sub()
                  ReDim Item(8)(48)
                  ReDim Me(9)(49)
                End Sub
    End Sub

    Public F As System.Action
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilation(source3, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected3 =
<expected>
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        ReDim x(0)(40)
              ~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        ReDim x.Item(1)(41)
              ~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            ReDim .Item(2)(42)
                  ~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                                         ReDim .Item(3)(43)
                                               ~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            ReDim .Item(4)(44)
                  ~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                ReDim .Item(5)(45)
                      ~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        ReDim x(6)(46)
              ~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        ReDim x.Item(7)(47)
              ~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                  ReDim Item(8)(48)
                        ~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                  ReDim Me(9)(49)
                        ~~~~~
</expected>
            comp3.AssertTheseDiagnostics(expected3)

            Dim comp4 = CreateCompilation(source3, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp4.AssertTheseDiagnostics(expected3)

            Dim source5 =
<compilation>
    <file name="c.vb"><![CDATA[
Class B
    Inherits C

    Public Sub Test()
        ReDim Item(0)(40)
        ReDim Me(1)(41), Me.Item(2)(42), MyBase.Item(3)(43), MyClass.Item(4)(44)

        With Me
            ReDim .Item(5)(45)
        End With
    End Sub
End Class

]]></file>
</compilation>

            Dim comp5 = CreateCompilation(source5, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected5 =
<expected>
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        ReDim Item(0)(40)
              ~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        ReDim Me(1)(41), Me.Item(2)(42), MyBase.Item(3)(43), MyClass.Item(4)(44)
              ~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        ReDim Me(1)(41), Me.Item(2)(42), MyBase.Item(3)(43), MyClass.Item(4)(44)
                         ~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        ReDim Me(1)(41), Me.Item(2)(42), MyBase.Item(3)(43), MyClass.Item(4)(44)
                                         ~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        ReDim Me(1)(41), Me.Item(2)(42), MyBase.Item(3)(43), MyClass.Item(4)(44)
                                                             ~~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            ReDim .Item(5)(45)
                  ~~~~~~~~
</expected>
            comp5.AssertTheseDiagnostics(expected5)

            Dim comp6 = CreateCompilation(source5, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp6.AssertTheseDiagnostics(expected5)
        End Sub

        <Fact>
        Public Sub Erase_01()

            Dim csSource =
"
public class C
{
    public int[] Property0 { init; get; } = new int[] {};
    public int[] Property1 { init; get; } = new int[] {};
    public int[] Property2 { init; get; } = new int[] {};
    public int[] Property3 { init; get; } = new int[] {};
    public int[] Property4 { init; get; } = new int[] {};
    public int[] Property5 { init; get; } = new int[] {};
    public int[] Property6 { init; get; } = new int[] {};
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim b = new B()

        System.Console.Write(b.Property0 Is Nothing)
        System.Console.Write(" "c)
        System.Console.Write(b.Property3 Is Nothing)
        System.Console.Write(" "c)
        System.Console.Write(b.Property4 Is Nothing)
        System.Console.Write(" "c)
        System.Console.Write(b.Property5 Is Nothing)
        System.Console.Write(" "c)
        System.Console.Write(b.Property6 Is Nothing)
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Erase Property0
        Erase Me.Property3, MyBase.Property4, MyClass.Property5

        With Me
            Erase .Property6
        End With
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16_9, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:="True True True True True").VerifyDiagnostics()

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(
<expected><![CDATA[
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Erase Property0
              ~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Erase Me.Property3, MyBase.Property4, MyClass.Property5
              ~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Erase Me.Property3, MyBase.Property4, MyClass.Property5
                            ~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Erase Me.Property3, MyBase.Property4, MyClass.Property5
                                              ~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            Erase .Property6
                  ~~~~~~~~~~
]]></expected>)

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim x = new C()
        Erase x.Property1

        With New B()
            Erase .Property2
        End With

        Dim y As New B() With { .F = Sub()
                                         Erase .Property3
                                     End Sub}
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Dim y = new B()

        With y
            Erase .Property4
        End With

        With Me
            With y
                Erase .Property6
            End With
        End With

        Dim x as New B()
        Erase x.Property0

        Dim z = Sub()
                  Erase Property5
                End Sub
    End Sub

    Public F As System.Action
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilation(source3, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected3 =
<expected>
BC37311: Init-only property 'Property1' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Erase x.Property1
              ~~~~~~~~~~~
BC37311: Init-only property 'Property2' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            Erase .Property2
                  ~~~~~~~~~~
BC37311: Init-only property 'Property3' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                                         Erase .Property3
                                               ~~~~~~~~~~
BC37311: Init-only property 'Property4' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            Erase .Property4
                  ~~~~~~~~~~
BC37311: Init-only property 'Property6' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                Erase .Property6
                      ~~~~~~~~~~
BC37311: Init-only property 'Property0' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Erase x.Property0
              ~~~~~~~~~~~
BC37311: Init-only property 'Property5' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                  Erase Property5
                        ~~~~~~~~~
</expected>
            comp3.AssertTheseDiagnostics(expected3)

            Dim comp4 = CreateCompilation(source3, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp4.AssertTheseDiagnostics(expected3)

            Dim source5 =
<compilation>
    <file name="c.vb"><![CDATA[
Class B
    Inherits C

    Public Sub Test()
        Erase Property0
        Erase Me.Property3, MyBase.Property4, MyClass.Property5

        With Me
            Erase .Property6
        End With
    End Sub
End Class

]]></file>
</compilation>

            Dim comp5 = CreateCompilation(source5, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected5 =
<expected>
BC37311: Init-only property 'Property0' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Erase Property0
              ~~~~~~~~~
BC37311: Init-only property 'Property3' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Erase Me.Property3, MyBase.Property4, MyClass.Property5
              ~~~~~~~~~~~~
BC37311: Init-only property 'Property4' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Erase Me.Property3, MyBase.Property4, MyClass.Property5
                            ~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property5' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Erase Me.Property3, MyBase.Property4, MyClass.Property5
                                              ~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'Property6' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            Erase .Property6
                  ~~~~~~~~~~
</expected>
            comp5.AssertTheseDiagnostics(expected5)

            Dim comp6 = CreateCompilation(source5, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp6.AssertTheseDiagnostics(expected5)
        End Sub

        <Fact>
        Public Sub Erase_02()

            Dim csSource =
"
public class C
{
    private int[][] _item = new int[6][] {new int[]{}, new int[]{}, new int[]{}, new int[]{}, new int[]{}, new int[]{}};
    public int[] this[int x]
    {
        init
        {
            _item[x] = value;
        }

        get => _item[x];
    }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim b = new B()
        for i as Integer = 0 To 5
            System.Console.Write(b(i) Is Nothing)
            System.Console.Write(" "c)
        Next
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Erase Item(0)
        Erase Me(1),  Me.Item(2), MyBase.Item(3), MyClass.Item(4)

        With Me
            Erase .Item(5)
        End With
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16_9, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:="True True True True True True ").VerifyDiagnostics()

            Assert.True(DirectCast(comp1.GetMember(Of PropertySymbol)("C.Item").SetMethod, IMethodSymbol).IsInitOnly)

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(
<expected><![CDATA[
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Erase Item(0)
              ~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Erase Me(1),  Me.Item(2), MyBase.Item(3), MyClass.Item(4)
              ~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Erase Me(1),  Me.Item(2), MyBase.Item(3), MyClass.Item(4)
                      ~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Erase Me(1),  Me.Item(2), MyBase.Item(3), MyClass.Item(4)
                                  ~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Erase Me(1),  Me.Item(2), MyBase.Item(3), MyClass.Item(4)
                                                  ~~~~~~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            Erase .Item(5)
                  ~~~~~~~~
]]></expected>)

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim x = new C()
        Erase x(0)
        Erase x.Item(1)

        With New B()
            Erase .Item(2)
        End With

        Dim y As New B() With { .F = Sub()
                                         Erase .Item(3)
                                     End Sub}
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Dim y = new B()

        With y
            Erase .Item(4)
        End With

        With Me
            With y
                Erase .Item(5)
            End With
        End With

        Dim x as New B()
        Erase x(6)
        Erase x.Item(7)

        Dim z = Sub()
                  Erase Item(8)
                  Erase Me(9)
                End Sub
    End Sub

    Public F As System.Action
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilation(source3, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected3 =
<expected>
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Erase x(0)
              ~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Erase x.Item(1)
              ~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            Erase .Item(2)
                  ~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                                         Erase .Item(3)
                                               ~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            Erase .Item(4)
                  ~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                Erase .Item(5)
                      ~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Erase x(6)
              ~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Erase x.Item(7)
              ~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                  Erase Item(8)
                        ~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                  Erase Me(9)
                        ~~~~~
</expected>
            comp3.AssertTheseDiagnostics(expected3)

            Dim comp4 = CreateCompilation(source3, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp4.AssertTheseDiagnostics(expected3)

            Dim source5 =
<compilation>
    <file name="c.vb"><![CDATA[
Class B
    Inherits C

    Public Sub Test()
        Erase Item(0)
        Erase Me(1), Me.Item(2), MyBase.Item(3), MyClass.Item(4)

        With Me
            Erase .Item(5)
        End With
    End Sub
End Class

]]></file>
</compilation>

            Dim comp5 = CreateCompilation(source5, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected5 =
<expected>
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Erase Item(0)
              ~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Erase Me(1), Me.Item(2), MyBase.Item(3), MyClass.Item(4)
              ~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Erase Me(1), Me.Item(2), MyBase.Item(3), MyClass.Item(4)
                     ~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Erase Me(1), Me.Item(2), MyBase.Item(3), MyClass.Item(4)
                                 ~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Erase Me(1), Me.Item(2), MyBase.Item(3), MyClass.Item(4)
                                                 ~~~~~~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            Erase .Item(5)
                  ~~~~~~~~
</expected>
            comp5.AssertTheseDiagnostics(expected5)

            Dim comp6 = CreateCompilation(source5, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp6.AssertTheseDiagnostics(expected5)
        End Sub

        <Fact>
        Public Sub DictionaryAccess_01()

            Dim csSource =
"
public class C
{
    private int[] _item = new int[36];
    public int this[string id]
    {
        init
        {
            int x = int.Parse(id.Substring(1, id.Length - 1));

            if (x != 1 && x != 5 && x != 7 && x != 8)
            {
                throw new System.InvalidOperationException();
            }

            _item[x] = value;
        }

        get
        {
            int x = int.Parse(id.Substring(1, id.Length - 1));
            return _item[x];
        }
    }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim b = new B()

        B.Init(b!c9, 49)
        B.Init((b!c19), 59)

        With b
            B.Init(!c11, 51)
            B.Init((!c21), 61)
        End With

        for i as Integer = 0 To 35
            System.Console.Write(b("c" & i))
            System.Console.Write(" "c)
        Next
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Me!c1 = 41

        With Me
            !c5 = 45
        End With

        Init(Me!c7, 47)
        Init((Me!c23), 63)

        Dim b = Me
        Init(b!c12, 52)
        Init((b!c24), 64)

        With Me
            Init(!c8, 48)
            Init((!c26), 66)
        End With

        With b
            Init(!c14, 54)
            Init((!c27), 67)
        End With

        Test()

        Dim d = Sub()
                    Init(Me!c34, 74)
                    Init((Me!c35), 75)
                End Sub

        d()

    End Sub

    Public Sub Test()
        With Me
            Init(!c15, 55)
            Init((!c28), 68)
        End With

        Init(Me!c16, 56)
        Init((Me!c29), 69)

        Dim b = Me

        With b
            Init(!c18, 58)
            Init((!c31), 71)
        End With
    End Sub


    Public Shared Sub Init(ByRef p as Integer, val As Integer)
        p = val
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16_9, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:="0 41 0 0 0 45 0 47 48 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0").VerifyDiagnostics()

            Dim comp2 = CreateCompilation(source1, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp2.AssertTheseDiagnostics(
<expected><![CDATA[
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        B.Init(b!c9, 49)
               ~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            B.Init(!c11, 51)
                   ~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Me!c1 = 41
        ~~~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            !c5 = 45
            ~~~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Init(Me!c7, 47)
             ~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Init(b!c12, 52)
             ~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            Init(!c8, 48)
                 ~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            Init(!c14, 54)
                 ~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
                    Init(Me!c34, 74)
                         ~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            Init(!c15, 55)
                 ~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
        Init(Me!c16, 56)
             ~~~~~~
BC36716: Visual Basic 16 does not support assigning to or passing 'ByRef' properties with init-only setters.
            Init(!c18, 58)
                 ~~~~
]]></expected>)

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim x = new C()
        x!c0 = 40

        With New B()
            !c2 = 42
        End With

        Dim y As New B() With { .F = Sub()
                                         !c3 = 43
                                     End Sub}
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Dim y = new B()

        With y
            !c4 = 44
        End With

        With Me
            With y
                !c5 = 45
            End With
        End With

        Dim x as New B()
        x!c6 = 46

        Dim z = Sub()
                  Me!c9 = 49  
                End Sub
    End Sub

    Public F As System.Action
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilation(source3, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected3 =
<expected>
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x!c0 = 40
        ~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            !c2 = 42
            ~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                                         !c3 = 43
                                         ~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            !c4 = 44
            ~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                !c5 = 45
                ~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        x!c6 = 46
        ~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
                  Me!c9 = 49  
                  ~~~~~~~~~~
</expected>
            comp3.AssertTheseDiagnostics(expected3)

            Dim comp4 = CreateCompilation(source3, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp4.AssertTheseDiagnostics(expected3)

            Dim source5 =
<compilation>
    <file name="c.vb"><![CDATA[
Class B
    Inherits C

    Public Sub Test()
        Me!c1 = 41

        With Me
            !c5 = 45
        End With
    End Sub
End Class

]]></file>
</compilation>

            Dim comp5 = CreateCompilation(source5, parseOptions:=TestOptions.RegularLatest, references:={csCompilation})
            Dim expected5 =
<expected>
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        Me!c1 = 41
        ~~~~~~~~~~
BC37311: Init-only property 'Item' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            !c5 = 45
            ~~~~~~~~
</expected>
            comp5.AssertTheseDiagnostics(expected5)

            Dim comp6 = CreateCompilation(source5, parseOptions:=TestOptions.Regular16, references:={csCompilation})
            comp6.AssertTheseDiagnostics(expected5)
        End Sub

        <Fact()>
        <WorkItem(50327, "https://github.com/dotnet/roslyn/issues/50327")>
        Public Sub ModReqOnSetAccessorParameter()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig specialname newslot virtual instance void set_Property1 ( int32 modreq(System.Runtime.CompilerServices.IsExternalInit) 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance int32 Property1()
    {
        .set instance void C::set_Property1(int32 modreq(System.Runtime.CompilerServices.IsExternalInit))
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test
    Inherits C

    Public Overrides WriteOnly Property Property1 As Integer
        Set
        End Set
    End Property

    Sub M(c As C)
        c.Property1 = 42
        c.set_Property1(43)
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(
<expected>
BC30657: 'Property1' has a return type that is not supported or parameter types that are not supported.
        Set
        ~~~
BC30657: 'Property1' has a return type that is not supported or parameter types that are not supported.
        c.Property1 = 42
        ~~~~~~~~~~~
BC30456: 'set_Property1' is not a member of 'C'.
        c.set_Property1(43)
        ~~~~~~~~~~~~~~~
</expected>)

            Dim p = compilation.GetMember(Of PropertySymbol)("C.Property1")
            Dim pSet = p.SetMethod
            Assert.False(pSet.IsInitOnly)
            Assert.NotNull(pSet.GetUseSiteErrorInfo())
            Assert.True(pSet.HasUnsupportedMetadata)
            Assert.Null(p.GetUseSiteErrorInfo())
            Assert.False(p.HasUnsupportedMetadata)
        End Sub

        <Fact()>
        Public Sub ModReqOnSetAccessorParameter_AndProperty()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig specialname newslot virtual instance void set_Property1 ( int32 modreq(System.Runtime.CompilerServices.IsExternalInit) 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance int32 modreq(System.Runtime.CompilerServices.IsExternalInit) Property1()
    {
        .set instance void C::set_Property1(int32 modreq(System.Runtime.CompilerServices.IsExternalInit))
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test
    Inherits C

    Public Overrides WriteOnly Property Property1 As Integer
        Set
        End Set
    End Property

    Sub M(c As C)
        c.Property1 = 42
        c.set_Property1(43)
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(
<expected>
BC30643: Property 'C.Property1' is of an unsupported type.
    Public Overrides WriteOnly Property Property1 As Integer
                                        ~~~~~~~~~
BC30643: Property 'C.Property1' is of an unsupported type.
        c.Property1 = 42
          ~~~~~~~~~
BC30456: 'set_Property1' is not a member of 'C'.
        c.set_Property1(43)
        ~~~~~~~~~~~~~~~
</expected>)

            Dim p = compilation.GetMember(Of PropertySymbol)("C.Property1")
            Dim pSet = p.SetMethod
            Assert.False(pSet.IsInitOnly)
            Assert.NotNull(pSet.GetUseSiteErrorInfo())
            Assert.True(pSet.HasUnsupportedMetadata)
            Assert.NotNull(p.GetUseSiteErrorInfo())
            Assert.True(p.HasUnsupportedMetadata)
        End Sub

        <Fact()>
        Public Sub ModReqOnStaticMethod()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig static void modreq(System.Runtime.CompilerServices.IsExternalInit) M () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test
    Sub M()
        C.M()
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(
<expected>
BC30657: 'M' has a return type that is not supported or parameter types that are not supported.
        C.M()
          ~
</expected>)

            Dim m = compilation.GetMember(Of MethodSymbol)("C.M")
            Assert.False(m.IsInitOnly)
            Assert.NotNull(m.GetUseSiteErrorInfo())
            Assert.True(m.HasUnsupportedMetadata)
        End Sub

        <Fact()>
        Public Sub ModReqOnInstanceMethod()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig instance void modreq(System.Runtime.CompilerServices.IsExternalInit) M () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test
    Sub M(c As C)
        c.M()
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(
<expected>
BC30657: 'M' has a return type that is not supported or parameter types that are not supported.
        c.M()
          ~
</expected>)

            Dim m = compilation.GetMember(Of MethodSymbol)("C.M")
            Assert.False(m.IsInitOnly)
            Assert.NotNull(m.GetUseSiteErrorInfo())
            Assert.True(m.HasUnsupportedMetadata)
        End Sub

        <Fact()>
        Public Sub ModReqOnStaticSet()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig newslot specialname
            static void modreq(System.Runtime.CompilerServices.IsExternalInit) set_P(int32 x) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    } 

    .property instance int32 P()
    {
      .set void modreq(System.Runtime.CompilerServices.IsExternalInit) C::set_P(int32)
    } 

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test
    Sub M()
        C.P = 2
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(
<expected>
BC30657: 'P' has a return type that is not supported or parameter types that are not supported.
        C.P = 2
        ~~~
</expected>)

            Dim p = compilation.GetMember(Of PropertySymbol)("C.P")
            Dim pSet = p.SetMethod
            Assert.False(pSet.IsInitOnly)
            Assert.NotNull(pSet.GetUseSiteErrorInfo())
            Assert.True(pSet.HasUnsupportedMetadata)
            Assert.Null(p.GetUseSiteErrorInfo())
            Assert.False(p.HasUnsupportedMetadata)
        End Sub

        <Fact()>
        Public Sub ModReqOnSetterOfRefProperty()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig specialname newslot virtual instance int32& get_Property1 () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname newslot virtual instance void set_Property1 ( int32& modreq(System.Runtime.CompilerServices.IsExternalInit) 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance int32& Property1()
    {
        .get instance int32& C::get_Property1()
        .set instance void C::set_Property1(int32& modreq(System.Runtime.CompilerServices.IsExternalInit))
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test
    Sub M(c As C, ByRef i as Integer)
        Dim x1 = c.get_Property1()
        c.set_Property(i)

        Dim x2 = c.Property1
        c.Property1 = i
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(
<expected>
BC30456: 'get_Property1' is not a member of 'C'.
        Dim x1 = c.get_Property1()
                 ~~~~~~~~~~~~~~~
BC30456: 'set_Property' is not a member of 'C'.
        c.set_Property(i)
        ~~~~~~~~~~~~~~
</expected>)

            Dim p = compilation.GetMember(Of PropertySymbol)("C.Property1")
            Dim pSet = p.SetMethod
            Assert.False(pSet.IsInitOnly)
            Assert.NotNull(pSet.GetUseSiteErrorInfo())
            Assert.True(pSet.HasUnsupportedMetadata)
            Assert.Null(p.GetUseSiteErrorInfo())
            Assert.False(p.HasUnsupportedMetadata)
        End Sub

        <Fact()>
        Public Sub ModReqOnRefProperty_OnRefReturn()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig specialname newslot virtual instance int32& modreq(System.Runtime.CompilerServices.IsExternalInit) get_Property1 () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname newslot virtual instance void set_Property1 ( int32& modreq(System.Runtime.CompilerServices.IsExternalInit) 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance int32& modreq(System.Runtime.CompilerServices.IsExternalInit) Property1()
    {
        .get instance int32& modreq(System.Runtime.CompilerServices.IsExternalInit) C::get_Property1()
        .set instance void C::set_Property1(int32& modreq(System.Runtime.CompilerServices.IsExternalInit))
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test
    Sub M(c As C, ByRef i as Integer)
        Dim x1 = c.get_Property1()
        c.set_Property(i)

        Dim x2 = c.Property1
        c.Property1 = i
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(
<expected>
BC30456: 'get_Property1' is not a member of 'C'.
        Dim x1 = c.get_Property1()
                 ~~~~~~~~~~~~~~~
BC30456: 'set_Property' is not a member of 'C'.
        c.set_Property(i)
        ~~~~~~~~~~~~~~
BC30643: Property 'C.Property1' is of an unsupported type.
        Dim x2 = c.Property1
                   ~~~~~~~~~
BC30643: Property 'C.Property1' is of an unsupported type.
        c.Property1 = i
          ~~~~~~~~~
</expected>)

            Dim p = compilation.GetMember(Of PropertySymbol)("C.Property1")
            Dim pSet = p.SetMethod
            Assert.False(pSet.IsInitOnly)
            Assert.NotNull(pSet.GetUseSiteErrorInfo())
            Assert.True(pSet.HasUnsupportedMetadata)
            Dim pGet = p.GetMethod
            Assert.False(pGet.IsInitOnly)
            Assert.NotNull(pGet.GetUseSiteErrorInfo())
            Assert.True(pGet.HasUnsupportedMetadata)

            Assert.NotNull(p.GetUseSiteErrorInfo())
            Assert.True(p.HasUnsupportedMetadata)
        End Sub

        <Fact()>
        Public Sub ModReqOnRefProperty_OnReturn()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig specialname newslot virtual instance int32 modreq(System.Runtime.CompilerServices.IsExternalInit)& get_Property1 () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname newslot virtual instance void set_Property1 ( int32 modreq(System.Runtime.CompilerServices.IsExternalInit)& 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance int32 modreq(System.Runtime.CompilerServices.IsExternalInit)& Property1()
    {
        .get instance int32 modreq(System.Runtime.CompilerServices.IsExternalInit)& C::get_Property1()
        .set instance void C::set_Property1(int32 modreq(System.Runtime.CompilerServices.IsExternalInit)&)
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test
    Sub M(c As C, ByRef i as Integer)
        Dim x1 = c.get_Property1()
        c.set_Property(i)

        Dim x2 = c.Property1
        c.Property1 = i
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(
<expected>
BC30456: 'get_Property1' is not a member of 'C'.
        Dim x1 = c.get_Property1()
                 ~~~~~~~~~~~~~~~
BC30456: 'set_Property' is not a member of 'C'.
        c.set_Property(i)
        ~~~~~~~~~~~~~~
BC30643: Property 'C.Property1' is of an unsupported type.
        Dim x2 = c.Property1
                   ~~~~~~~~~
BC30643: Property 'C.Property1' is of an unsupported type.
        c.Property1 = i
          ~~~~~~~~~
</expected>)

            Dim p = compilation.GetMember(Of PropertySymbol)("C.Property1")
            Dim pSet = p.SetMethod
            Assert.False(pSet.IsInitOnly)
            Assert.NotNull(pSet.GetUseSiteErrorInfo())
            Assert.True(pSet.HasUnsupportedMetadata)
            Dim pGet = p.GetMethod
            Assert.False(pGet.IsInitOnly)
            Assert.NotNull(pGet.GetUseSiteErrorInfo())
            Assert.True(pGet.HasUnsupportedMetadata)

            Assert.NotNull(p.GetUseSiteErrorInfo())
            Assert.True(p.HasUnsupportedMetadata)
        End Sub

        <Fact()>
        <WorkItem(50327, "https://github.com/dotnet/roslyn/issues/50327")>
        Public Sub ModReqOnGetAccessorReturnValue()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig specialname newslot virtual instance int32 modreq(System.Runtime.CompilerServices.IsExternalInit) get_Property1 () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname newslot virtual instance void set_Property1 ( int32 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance int32 Property1()
    {
        .get instance int32 modreq(System.Runtime.CompilerServices.IsExternalInit) C::get_Property1()
        .set instance void C::set_Property1(int32)
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test
    Inherits C

    Overrides Property Property1 As Integer

    Sub M(c As C)
        Dim x1 = c.get_Property1()
        c.set_Property(1)

        Dim x2 = c.Property1
        c.Property1 = 2
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(
<expected>
BC30657: 'Property1' has a return type that is not supported or parameter types that are not supported.
    Overrides Property Property1 As Integer
                       ~~~~~~~~~
BC30456: 'get_Property1' is not a member of 'C'.
        Dim x1 = c.get_Property1()
                 ~~~~~~~~~~~~~~~
BC30456: 'set_Property' is not a member of 'C'.
        c.set_Property(1)
        ~~~~~~~~~~~~~~
BC30657: 'Property1' has a return type that is not supported or parameter types that are not supported.
        Dim x2 = c.Property1
                 ~~~~~~~~~~~
</expected>)

            Dim p = compilation.GetMember(Of PropertySymbol)("C.Property1")
            Dim pSet = p.SetMethod
            Assert.False(pSet.IsInitOnly)
            Assert.Null(pSet.GetUseSiteErrorInfo())
            Assert.False(pSet.HasUnsupportedMetadata)
            Dim pGet = p.GetMethod
            Assert.False(pGet.IsInitOnly)
            Assert.NotNull(pGet.GetUseSiteErrorInfo())
            Assert.True(pGet.HasUnsupportedMetadata)

            Assert.Null(p.GetUseSiteErrorInfo())
            Assert.False(p.HasUnsupportedMetadata)
        End Sub

        <Fact()>
        Public Sub ModReqOnPropertyAndGetAccessorReturnValue()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig specialname newslot virtual instance int32 modreq(System.Runtime.CompilerServices.IsExternalInit) get_Property1 () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname newslot virtual instance void set_Property1 ( int32 'value' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance int32 modreq(System.Runtime.CompilerServices.IsExternalInit) Property1()
    {
        .get instance int32 modreq(System.Runtime.CompilerServices.IsExternalInit) C::get_Property1()
        .set instance void C::set_Property1(int32)
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test
    Inherits C

    Overrides Property Property1 As Integer

    Sub M(c As C)
        Dim x1 = c.get_Property1()
        c.set_Property(1)

        Dim x2 = c.Property1
        c.Property1 = 2
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(
<expected>
BC30643: Property 'C.Property1' is of an unsupported type.
    Overrides Property Property1 As Integer
                       ~~~~~~~~~
BC30456: 'get_Property1' is not a member of 'C'.
        Dim x1 = c.get_Property1()
                 ~~~~~~~~~~~~~~~
BC30456: 'set_Property' is not a member of 'C'.
        c.set_Property(1)
        ~~~~~~~~~~~~~~
BC30643: Property 'C.Property1' is of an unsupported type.
        Dim x2 = c.Property1
                   ~~~~~~~~~
BC30643: Property 'C.Property1' is of an unsupported type.
        c.Property1 = 2
          ~~~~~~~~~
</expected>)

            Dim p = compilation.GetMember(Of PropertySymbol)("C.Property1")
            Dim pSet = p.SetMethod
            Assert.False(pSet.IsInitOnly)
            Assert.Null(pSet.GetUseSiteErrorInfo())
            Assert.False(pSet.HasUnsupportedMetadata)
            Dim pGet = p.GetMethod
            Assert.False(pGet.IsInitOnly)
            Assert.NotNull(pGet.GetUseSiteErrorInfo())
            Assert.True(pGet.HasUnsupportedMetadata)

            Assert.NotNull(p.GetUseSiteErrorInfo())
            Assert.True(p.HasUnsupportedMetadata)
        End Sub

        <Fact()>
        Public Sub ModOptOnSet()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig newslot specialname
            instance void modopt(System.Runtime.CompilerServices.IsExternalInit) set_P(int32 x) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    } 

    .property instance int32 P()
    {
      .set instance void modopt(System.Runtime.CompilerServices.IsExternalInit) C::set_P(int32)
    } 

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test
    Shared Sub M(c As C)
        c.P = 2
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.ReleaseDll)
            CompileAndVerify(compilation).VerifyDiagnostics()

            Dim p = compilation.GetMember(Of PropertySymbol)("C.P")
            Dim pSet = p.SetMethod
            Assert.False(pSet.IsInitOnly)
            Assert.Null(pSet.GetUseSiteErrorInfo())
            Assert.False(pSet.HasUnsupportedMetadata)
            Assert.Null(p.GetUseSiteErrorInfo())
            Assert.False(p.HasUnsupportedMetadata)
        End Sub

        <Theory,
         InlineData("Runtime.CompilerServices.IsExternalInit"),
         InlineData("CompilerServices.IsExternalInit"),
         InlineData("IsExternalInit"),
         InlineData("ns.System.Runtime.CompilerServices.IsExternalInit"),
         InlineData("system.Runtime.CompilerServices.IsExternalInit"),
         InlineData("System.runtime.CompilerServices.IsExternalInit"),
         InlineData("System.Runtime.compilerServices.IsExternalInit"),
         InlineData("System.Runtime.CompilerServices.isExternalInit")
        >
        Public Sub IsExternalInitCheck(modifierName As String)
            Dim ilSource = "
.class public auto ansi beforefieldinit C extends System.Object
{
    .method public hidebysig newslot specialname
            instance void modreq(" + modifierName + ") set_P(int32 x) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    } 

    .property instance int32 P()
    {
      .set instance void modreq(" + modifierName + ") C::set_P(int32)
    } 

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}

.class public auto ansi sealed beforefieldinit " + modifierName + " extends System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
"

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test
    Sub M(c As C)
        c.P = 2
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(
<expected>
BC30657: 'P' has a return type that is not supported or parameter types that are not supported.
        c.P = 2
        ~~~
</expected>)

            Dim p = compilation.GetMember(Of PropertySymbol)("C.P")
            Dim pSet = p.SetMethod
            Assert.False(pSet.IsInitOnly)
            Assert.NotNull(pSet.GetUseSiteErrorInfo())
            Assert.True(pSet.HasUnsupportedMetadata)
            Assert.Null(p.GetUseSiteErrorInfo())
            Assert.False(p.HasUnsupportedMetadata)
        End Sub

        <Fact()>
        Public Sub IsInitOnlyValue()
            Dim vbSource1 =
                <compilation>
                    <file name="c.vb"><![CDATA[
]]>
                    </file>
                </compilation>

            Dim compilation1 = CreateCompilation(vbSource1, options:=TestOptions.ReleaseDll)

            Dim vbSource2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Test1(Of T)
    Public Shared Sub M1()
        Dim x as Integer = 0
        x.DoSomething()
    End Sub

    Public Function M2() As System.Action
        return Sub() 
               End Sub
    End Function

    Public Property P As Integer
End Class

Class Test2
    Inherits Test1(Of Integer)
End Class

Delegate Sub D()

Module Ext
    <System.Runtime.CompilerServices.Extension>
    Sub DoSomething(x As Integer)
    End Sub
End Module
]]>
                    </file>
                </compilation>

            Dim compilation2 = CreateCompilation(vbSource2, references:={compilation1.ToMetadataReference()}, options:=TestOptions.ReleaseDll)

            Dim tree = compilation2.SyntaxTrees.Single()
            Dim model = compilation2.GetSemanticModel(tree)
            Dim lambda = tree.GetRoot.DescendantNodes().OfType(Of LambdaExpressionSyntax)().Single()

            Assert.False(DirectCast(model.GetSymbolInfo(lambda).Symbol, MethodSymbol).IsInitOnly)

            Dim invocation = tree.GetRoot.DescendantNodes().OfType(Of InvocationExpressionSyntax)().Single()
            Assert.False(DirectCast(model.GetSymbolInfo(invocation).Symbol, MethodSymbol).IsInitOnly)

            Dim verify = Sub(compilation As VisualBasicCompilation)
                             Dim test1 = compilation.GetTypeByMetadataName("Test1`1")
                             Dim p = test1.GetMember(Of PropertySymbol)("P")
                             Assert.False(p.SetMethod.IsInitOnly)
                             Assert.False(p.GetMethod.IsInitOnly)

                             Assert.False(test1.GetMember(Of MethodSymbol)("M1").IsInitOnly)
                             Assert.False(test1.GetMember(Of MethodSymbol)("M2").IsInitOnly)

                             Dim test1Constructed = compilation.GetTypeByMetadataName("Test2").BaseTypeNoUseSiteDiagnostics
                             p = test1Constructed.GetMember(Of PropertySymbol)("P")
                             Assert.False(p.SetMethod.IsInitOnly)
                             Assert.False(p.GetMethod.IsInitOnly)

                             Assert.False(test1Constructed.GetMember(Of MethodSymbol)("M1").IsInitOnly)
                             Assert.False(test1Constructed.GetMember(Of MethodSymbol)("M2").IsInitOnly)

                             Dim d = compilation.GetTypeByMetadataName("D")
                             For Each m As MethodSymbol In d.GetMembers()
                                 Assert.False(m.IsInitOnly)
                             Next
                         End Sub

            verify(compilation2)

            Dim compilation3 = CreateCompilation(vbSource1, references:={compilation2.ToMetadataReference()}, options:=TestOptions.ReleaseDll)
            verify(compilation3)
        End Sub

        <Fact>
        Public Sub ReferenceConversion_01()

            Dim csSource =
"
public interface I1 { int P1 { get; init; } }
public interface I2 {}

public class C : I1, I2
{
    public int P0 { init; get; }
    int I1.P1
    {
        get => P0;
        init => P0 = value;
    }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim b = new B()
        System.Console.Write(b.P0)
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        DirectCast(Me, I1).P1 = 41
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37311: Init-only property 'P1' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        DirectCast(Me, I1).P1 = 41
        ~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ReferenceConversion_02()

            Dim csSource =
"
public interface I1 { int P1 { get; init; } }
public interface I2 {}

public class C : I1, I2
{
    public int P0 { init; get; }
    int I1.P1
    {
        get => P0;
        init => P0 = value;
    }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim b = new B()
        System.Console.Write(b.P0)
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        With CType(Me, I1)
            .P1 = 41
        End With
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37311: Init-only property 'P1' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .P1 = 41
            ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ReferenceConversion_03()

            Dim csSource =
"
public interface I1 { int P1 { get; init; } }

public class C : I1
{
    public int P0 { init; get; }
    int I1.P1
    {
        get => P0;
        init => P0 = value;
    }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim b = new B()
        System.Console.Write(b.P0)
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        TryCast(Me, I1).P1 = 41
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37311: Init-only property 'P1' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        TryCast(Me, I1).P1 = 41
        ~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ReferenceConversion_04()

            Dim csSource =
"
public interface I1 { int P1 { get; init; } }

public class C : I1
{
    public int P0 { init; get; }
    int I1.P1
    {
        get => P0;
        init => P0 = value;
    }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim b = new B()
        System.Console.Write(b.P0)
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        With TryCast(Me, I1)
            .P1 = 41
        End With
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37311: Init-only property 'P1' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .P1 = 41
            ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ReferenceConversion_05()

            Dim csSource =
"
public interface I1 { int P1 { get; init; } }

public class C : I1
{
    public int P0 { init; get; }
    int I1.P1
    {
        get => P0;
        init => P0 = value;
    }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim b = new B()
        System.Console.Write(b.P0)
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        CType(MyBase, I1).P1 = 41
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37311: Init-only property 'P1' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        CType(MyBase, I1).P1 = 41
        ~~~~~~~~~~~~~~~~~~~~~~~~~
BC32027: 'MyBase' must be followed by '.' and an identifier.
        CType(MyBase, I1).P1 = 41
              ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ReferenceConversion_06()

            Dim csSource =
"
public interface I1 { int P1 { get; init; } }

public class C : I1
{
    public int P0 { init; get; }
    int I1.P1
    {
        get => P0;
        init => P0 = value;
    }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim b = new B()
        System.Console.Write(b.P0)
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        DirectCast(MyClass, I1).P1 = 41

        With MyClass
            .P0 = 1
        End With

        With MyBase
            .P0 = 2
        End With
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37311: Init-only property 'P1' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        DirectCast(MyClass, I1).P1 = 41
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32028: 'MyClass' must be followed by '.' and an identifier.
        DirectCast(MyClass, I1).P1 = 41
                   ~~~~~~~
BC32028: 'MyClass' must be followed by '.' and an identifier.
        With MyClass
             ~~~~~~~
BC32027: 'MyBase' must be followed by '.' and an identifier.
        With MyBase
             ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ReferenceConversion_07()

            Dim csSource =
"
public class C
{
    public int P0 { init; get; }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        Me.P0 = 41
    End Sub
End Class

Class D
    Public Shared Widening Operator CType(x As D) As B
        Return Nothing
    End Operator

    Public Sub New()
        CType(Me, B).P0 = 42
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37311: Init-only property 'P0' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        CType(Me, B).P0 = 42
        ~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ReferenceConversion_08()

            Dim csSource =
"
public class C
{
    public int P0 { init; get; }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim b = new B()
        System.Console.Write(b.P0)
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        DirectCast(Me, B).P0 = 41
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37311: Init-only property 'P0' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        DirectCast(Me, B).P0 = 41
        ~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ReferenceConversion_09()

            Dim csSource =
"
public class C
{
    public int P0 { init; get; }
}
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
        Dim b = new B()
        System.Console.Write(b.P0)
    End Sub
End Class

Class B
    Inherits C

    Public Sub New()
        With CType(Me, B)
            .P0 = 41
        End With
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37311: Init-only property 'P0' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
            .P0 = 41
            ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ReferenceConversion_10()

            Dim csSource =
"
public interface I1 { int P1 { get; init; } }
"
            Dim csCompilation = CreateCSharpCompilation(csSource + IsExternalInitTypeDefinition).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test
    Shared Sub Main()
    End Sub
End Class

Structure B
    Implements I1

    Public Sub New(x As Integer)
        DirectCast(Me, I1).P1 = 41
        CType(Me, I1).P1 = 42
        DirectCast(CObj(Me), I1).P1 = 43
    End Sub
End Structure
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, parseOptions:=TestOptions.RegularLatest, options:=TestOptions.DebugExe, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC30149: Structure 'B' must implement 'Property P1 As Integer' for interface 'I1'.
    Implements I1
               ~~
BC37311: Init-only property 'P1' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        DirectCast(Me, I1).P1 = 41
        ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'P1' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        CType(Me, I1).P1 = 42
        ~~~~~~~~~~~~~~~~~~~~~
BC37311: Init-only property 'P1' can only be assigned by an object member initializer, or on 'Me', 'MyClass` or 'MyBase' in an instance constructor.
        DirectCast(CObj(Me), I1).P1 = 43
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

    End Class
End Namespace
