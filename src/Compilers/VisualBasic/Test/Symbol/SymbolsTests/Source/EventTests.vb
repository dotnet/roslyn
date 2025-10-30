' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Reflection
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class EventSymbolTests
        Inherits BasicTestBase

        <WorkItem(20335, "https://github.com/dotnet/roslyn/issues/20335")>
        <Fact()>
        Public Sub IlEventVisibility()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit A
{
  .method assembly hidebysig newslot specialname virtual instance void 
          add_E(class [mscorlib]System.Action`1<int32> 'value') cil managed
  {
    ret
  }
  .method public hidebysig newslot specialname virtual instance void 
          remove_E(class [mscorlib]System.Action`1<int32> 'value') cil managed
  {
    ret
  }
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
  .event class [mscorlib]System.Action`1<int32> E
  {
    .addon instance void A::add_E(class [mscorlib]System.Action`1<int32>)
    .removeon instance void A::remove_E(class [mscorlib]System.Action`1<int32>)
  }
}]]>
            Dim vbSource = <compilation name="F">
                               <file name="F.vb">
Class B
    Inherits A
    Sub M()
        AddHandler E, Nothing
        RemoveHandler E, Nothing
        AddHandler MyBase.E, Nothing
        RemoveHandler MyBase.E, Nothing
    End Sub 
End Class
                               </file>
                           </compilation>

            Dim comp1 = CreateCompilationWithCustomILSource(vbSource, ilSource.Value, TestOptions.DebugDll)
            CompilationUtils.AssertTheseCompileDiagnostics(comp1,
<Expected>
BC30390: 'A.Friend Overridable Overloads AddHandler Event E(value As Action(Of Integer))' is not accessible in this context because it is 'Friend'.
        AddHandler E, Nothing
                   ~
BC30390: 'A.Friend Overridable Overloads AddHandler Event E(value As Action(Of Integer))' is not accessible in this context because it is 'Friend'.
        AddHandler MyBase.E, Nothing
                   ~~~~~~~~
</Expected>)

        End Sub

        <WorkItem(20335, "https://github.com/dotnet/roslyn/issues/20335")>
        <Fact()>
        Public Sub CustomEventVisibility()
            Dim source = <compilation name="F">
                             <file name="F.vb">
Imports System

Public Class C
    Protected Custom Event Click As EventHandler
        AddHandler(ByVal value As EventHandler)
            Console.Write("add")
        End AddHandler

        RemoveHandler(ByVal value As EventHandler)
			Console.Write("remove")
        End RemoveHandler

        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
			Console.Write("raise")
        End RaiseEvent
    End Event
End Class

Public Class D
	Inherits C

	Public Sub F()
		AddHandler Click, Nothing
		RemoveHandler Click, Nothing
		AddHandler MyBase.Click, Nothing
		RemoveHandler MyBase.Click, Nothing
	End Sub
End Class
                             </file>
                         </compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertTheseCompileDiagnostics(comp, <Expected></Expected>)
        End Sub

        <WorkItem(20335, "https://github.com/dotnet/roslyn/issues/20335")>
        <Fact()>
        Public Sub ProtectedHandlerDefinedInCSharp()
            Dim csharpCompilation = CreateCSharpCompilation("
public class C {
	protected delegate void Handle();
    protected event Handle MyEvent;
}

public class D: C {
  public D() {
    MyEvent += () => {};
  }
}
")
            Dim source = Parse("
Public Class E
    Inherits C
    Public Sub S()
        AddHandler MyBase.MyEvent, Nothing
    End Sub
End Class
")
            Dim vbCompilation = CompilationUtils.CreateCompilationWithMscorlib461AndVBRuntime(
                source:={source},
                references:={csharpCompilation.EmitToImageReference()},
                options:=TestOptions.DebugDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertTheseCompileDiagnostics(vbCompilation, <Expected></Expected>)

        End Sub

        <WorkItem(20335, "https://github.com/dotnet/roslyn/issues/20335")>
        <Fact()>
        Public Sub EventVisibility()
            Dim source = <compilation name="F">
                             <file name="F.vb">
 Public Class Form1
    Protected Event EventA As System.Action
    Private Event EventB As System.Action
    Friend Event EventC As System.Action
End Class

Public Class Form2
    Inherits Form1

    Public Sub New()
        AddHandler MyBase.EventA, Nothing
        RemoveHandler MyBase.EventA, Nothing
        AddHandler EventA, Nothing
        RemoveHandler EventA, Nothing

        AddHandler MyBase.EventB, Nothing
        RemoveHandler MyBase.EventB, Nothing
        AddHandler EventB, Nothing
        RemoveHandler EventB, Nothing

        AddHandler MyBase.EventC, Nothing
        RemoveHandler MyBase.EventC, Nothing
        AddHandler EventC, Nothing
        RemoveHandler EventC, Nothing
    End Sub
End Class
                             </file>
                         </compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertTheseCompileDiagnostics(comp,
<Expected>
BC30389: 'Form1.EventB' is not accessible in this context because it is 'Private'.
        AddHandler MyBase.EventB, Nothing
                   ~~~~~~~~~~~~~
BC30389: 'Form1.EventB' is not accessible in this context because it is 'Private'.
        RemoveHandler MyBase.EventB, Nothing
                      ~~~~~~~~~~~~~
BC30389: 'Form1.EventB' is not accessible in this context because it is 'Private'.
        AddHandler EventB, Nothing
                   ~~~~~~
BC30389: 'Form1.EventB' is not accessible in this context because it is 'Private'.
        RemoveHandler EventB, Nothing
                      ~~~~~~
</Expected>)
        End Sub

        <WorkItem(542806, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542806")>
        <Fact()>
        Public Sub EmptyCustomEvent()
            Dim source = <compilation name="F">
                             <file name="F.vb">
Class C
    Public Custom Event Goo
End Class

                             </file>
                         </compilation>

            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertTheseParseDiagnostics(comp2,
<expected>
BC31122: 'Custom' modifier is not valid on events declared without explicit delegate types.
    Public Custom Event Goo
    ~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(542891, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542891")>
        <Fact()>
        Public Sub InterfaceImplements()
            Dim source = <compilation name="F">
                             <file name="F.vb">
Imports System.ComponentModel 

Class C    
    Implements INotifyPropertyChanged 
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
End Class
                             </file>
                         </compilation>

            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Off))
            CompilationUtils.AssertNoErrors(comp1)
            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertNoErrors(comp2)
        End Sub

        <Fact()>
        Public Sub RaiseBaseEventedFromDerivedNestedTypes()
            Dim source =
<compilation>
    <file name="filename.vb">
Module Program
    Sub Main()
    End Sub
End Module
Class C1
    Event HelloWorld
    Class C2
        Inherits C1
        Sub t
            RaiseEvent HelloWorld
        End Sub
    End Class
End Class
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub MultipleInterfaceImplements()
            Dim source =
            <compilation>
                <file name="filename.vb">
Option Infer On
Imports System
Imports System.Collections.Generic
Interface I
    Event E As Action(Of Integer)
    Event E2 As Action(Of String)
End Interface
Interface I2
    Event E As Action(Of Integer)
    Event E2 As Action(Of String)
End Interface


Class Base
    Implements I
    Implements I2
    Event E2(x As Integer) Implements I.E, I2.E

    Dim eventsList As List(Of Action(Of String)) = New List(Of Action(Of String))
    Public Custom Event E As Action(Of String) Implements I.E2, I2.E2
        AddHandler(e As Action(Of String))
            Console.Write("Add E|")
            eventsList.Add(e)
        End AddHandler

        RemoveHandler(e As Action(Of String))
            Console.Write("Remove E|")
            eventsList.Remove(e)
        End RemoveHandler

        RaiseEvent()
            Dim x As String = Nothing
            Console.Write("Raise E|")
            For Each ev In eventsList
                ev(x)
            Next
        End RaiseEvent
    End Event
    Sub R
        RaiseEvent E
    End Sub
End Class
Module Module1
    Sub Main(args As String())
        Dim b = New Base
        Dim a As Action(Of String) = Sub(x)
                                         Console.Write("Added from Base|")
                                     End Sub
        AddHandler b.E, a

        Dim i_1 As I = b
        Dim i_2 As I2 = b

        RemoveHandler i_1.E2, a

        AddHandler i_2.E2, Sub(x)
                               Console.Write("Added from I2|")
                           End Sub

        b.R
    End Sub
End Module
    </file>
            </compilation>
            CompileAndVerify(source,
                             expectedOutput:=
            <![CDATA[Add E|Remove E|Add E|Raise E|Added from I2|]]>.Value
)

        End Sub

        <WorkItem(543309, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543309")>
        <Fact()>
        Public Sub EventSyntheticDelegateShadows()
            Dim source = <compilation name="F">
                             <file name="F.vb">
Public MustInherit Class GameLauncher    
    Public Event Empty()
End Class 

Public Class MissileLauncher    
    Inherits GameLauncher 
    Public Shadows Event Empty()
End Class
                             </file>
                         </compilation>

            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Off))
            CompilationUtils.AssertNoErrors(comp1)
            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertNoErrors(comp2)
        End Sub

        <Fact()>
        Public Sub EventNoShadows()
            Dim source = <compilation name="F">
                             <file name="F.vb">
                                 <![CDATA[       
Public MustInherit Class GameLauncher    
    Public Event Empty()
End Class 

Public Class MissileLauncher    
    Inherits GameLauncher 
    Public Event Empty()
End Class
]]>
                             </file>
                         </compilation>

            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertTheseDiagnostics(comp2,
<expected>
    <![CDATA[   
BC40004: event 'Empty' conflicts with event 'Empty' in the base class 'GameLauncher' and should be declared 'Shadows'.
    Public Event Empty()
                 ~~~~~
]]>
</expected>)
        End Sub

        <Fact()>
        Public Sub EventAutoPropShadows()
            Dim source = <compilation name="F">
                             <file name="F.vb">
                                 <![CDATA[       
Public MustInherit Class GameLauncher    
    Public Event _Empty()
End Class

Public Class MissileLauncher
    Inherits GameLauncher
    Public Property EmptyEventhandler As Integer
End Class

Public MustInherit Class GameLauncher1
    Public Property EmptyEventhandler As Integer
End Class

Public Class MissileLauncher1
    Inherits GameLauncher1
    Public Event _Empty()
End Class
]]>
                             </file>
                         </compilation>

            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertTheseDiagnostics(comp2,
<expected>
    <![CDATA[   
BC40018: property 'EmptyEventhandler' implicitly declares '_EmptyEventhandler', which conflicts with a member implicitly declared for event '_Empty' in the base class 'GameLauncher'. property should be declared 'Shadows'.
    Public Property EmptyEventhandler As Integer
                    ~~~~~~~~~~~~~~~~~
]]>
</expected>)
        End Sub

        <Fact()>
        Public Sub EventAutoPropClash()
            Dim source = <compilation name="F">
                             <file name="F.vb">
                                 <![CDATA[       
Public Class MissileLauncher1
    Public Event _Empty()
    Public Property EmptyEventhandler As Integer
End Class

Public Class MissileLauncher2
    Public Property EmptyEventhandler As Integer
    Public Event _Empty()
End Class


]]>
                             </file>
                         </compilation>

            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertTheseDiagnostics(comp2,
<expected>
BC31059: event '_Empty' implicitly defines '_EmptyEventHandler', which conflicts with a member implicitly declared for property 'EmptyEventhandler' in class 'MissileLauncher1'.
    Public Event _Empty()
                 ~~~~~~
BC31059: event '_Empty' implicitly defines '_EmptyEventHandler', which conflicts with a member implicitly declared for property 'EmptyEventhandler' in class 'MissileLauncher2'.
    Public Event _Empty()
                 ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub EventNoShadows1()
            Dim source = <compilation name="F">
                             <file name="F.vb">
                                 <![CDATA[       
Public MustInherit Class GameLauncher    
    Public EmptyEventHandler as integer
End Class 

Public Class MissileLauncher    
    Inherits GameLauncher 
    Public Event Empty()
End Class
]]>
                             </file>
                         </compilation>

            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertTheseDiagnostics(comp2,
<expected>
    <![CDATA[   
BC40012: event 'Empty' implicitly declares 'EmptyEventHandler', which conflicts with a member in the base class 'GameLauncher', and so the event should be declared 'Shadows'.
    Public Event Empty()
                 ~~~~~
]]>
</expected>)
        End Sub

        <Fact()>
        Public Sub EventsAreNotValues()
            Dim source = <compilation name="F">
                             <file name="F.vb">
                                 <![CDATA[       
Class cls1
    Event e1()
    Event e2()

    Sub goo()
        System.Console.WriteLine(e1)
        System.Console.WriteLine(e1 + (e2))
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertTheseDiagnostics(comp2,
<expected>
    <![CDATA[   
BC32022: 'Public Event e1()' is an event, and cannot be called directly. Use a 'RaiseEvent' statement to raise an event.
        System.Console.WriteLine(e1)
                                 ~~
BC32022: 'Public Event e1()' is an event, and cannot be called directly. Use a 'RaiseEvent' statement to raise an event.
        System.Console.WriteLine(e1 + (e2))
                                 ~~
BC32022: 'Public Event e2()' is an event, and cannot be called directly. Use a 'RaiseEvent' statement to raise an event.
        System.Console.WriteLine(e1 + (e2))
                                       ~~
]]>
</expected>)
        End Sub

        <Fact()>
        Public Sub EventImplementsInInterfaceAndModule()
            Dim source = <compilation name="F">
                             <file name="F.vb">
                                 <![CDATA[       
Interface I1
    Event e1()
End Interface

Interface I2
    Inherits I1

    Event e2() Implements I1.e1
End Interface

Module m1
    Event e2() Implements I1.e1
End Module
]]>
                             </file>
                         </compilation>

            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertTheseDiagnostics(comp2,
<expected>
    <![CDATA[   
BC30688: Events in interfaces cannot be declared 'Implements'.
    Event e2() Implements I1.e1
               ~~~~~~~~~~
BC31083: Members in a Module cannot implement interface members.
    Event e2() Implements I1.e1
               ~~~~~~~~~~
]]>
</expected>)
        End Sub

        <Fact()>
        Public Sub AttributesInapplicable()
            Dim source = <compilation name="F">
                             <file name="F.vb">
                                 <![CDATA[       
Imports System
                        
Class cls0
    <System.ParamArray()>
    Event RegularEvent()

    <System.ParamArray()>
    Custom Event CustomEvent As Action
        AddHandler(value As Action)

        End AddHandler

        RemoveHandler(value As Action)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event
End Class
]]>
                             </file>
                         </compilation>

            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertTheseDiagnostics(comp2,
<expected>
    <![CDATA[   
BC30662: Attribute 'ParamArrayAttribute' cannot be applied to 'RegularEvent' because the attribute is not valid on this declaration type.
    <System.ParamArray()>
     ~~~~~~~~~~~~~~~~~
BC30662: Attribute 'ParamArrayAttribute' cannot be applied to 'CustomEvent' because the attribute is not valid on this declaration type.
    <System.ParamArray()>
     ~~~~~~~~~~~~~~~~~
]]>
</expected>)
        End Sub

        <Fact()>
        Public Sub AttributesApplicable()
            Dim source = <compilation name="F">
                             <file name="F.vb">
                                 <![CDATA[       
Imports System
                        
Class cls0
    <Obsolete>
    Event RegularEvent()

    <Obsolete>
    Custom Event CustomEvent As Action
        AddHandler(value As Action)

        End AddHandler

        RemoveHandler(value As Action)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event
End Class
]]>
                             </file>
                         </compilation>

            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertNoErrors(comp2)

            Dim attributeValidatorSource = Sub(m As ModuleSymbol)

                                               ' Event should have an Obsolete attribute
                                               Dim type = DirectCast(m.GlobalNamespace.GetMember("cls0"), NamedTypeSymbol)
                                               Dim member = type.GetMember("RegularEvent")
                                               Dim attrs = member.GetAttributes()
                                               Assert.Equal(1, attrs.Length)
                                               Assert.Equal("System.ObsoleteAttribute", attrs(0).AttributeClass.ToDisplayString)

                                               ' additional synthetic members (field, accessors and such) should not
                                               member = type.GetMember("RegularEventEvent")
                                               attrs = member.GetAttributes()
                                               Assert.Equal(0, attrs.Length)

                                               member = type.GetMember("RegularEventEventHandler")
                                               attrs = member.GetAttributes()
                                               Assert.Equal(0, attrs.Length)

                                               member = type.GetMember("add_RegularEvent")
                                               attrs = member.GetAttributes()
                                               Assert.Equal(0, attrs.Length)

                                               member = type.GetMember("remove_RegularEvent")
                                               attrs = member.GetAttributes()
                                               Assert.Equal(0, attrs.Length)

                                               ' Event should have an Obsolete attribute
                                               member = type.GetMember("CustomEvent")
                                               attrs = member.GetAttributes()
                                               Assert.Equal(1, attrs.Length)
                                               Assert.Equal("System.ObsoleteAttribute", attrs(0).AttributeClass.ToDisplayString)

                                               ' additional synthetic members (field, accessors and such) should not
                                               member = type.GetMember("add_CustomEvent")
                                               attrs = member.GetAttributes()
                                               Assert.Equal(0, attrs.Length)

                                               member = type.GetMember("remove_CustomEvent")
                                               attrs = member.GetAttributes()
                                               Assert.Equal(0, attrs.Length)

                                               member = type.GetMember("raise_CustomEvent")
                                               attrs = member.GetAttributes()
                                               Assert.Equal(0, attrs.Length)
                                           End Sub

            ' metadata verifier excludes private members as those are not loaded.
            Dim attributeValidatorMetadata = Sub(m As ModuleSymbol)

                                                 ' Event should have an Obsolete attribute
                                                 Dim type = DirectCast(m.GlobalNamespace.GetMember("cls0"), NamedTypeSymbol)
                                                 Dim member = type.GetMember("RegularEvent")
                                                 Dim attrs = member.GetAttributes()
                                                 Assert.Equal(1, attrs.Length)
                                                 Assert.Equal("System.ObsoleteAttribute", attrs(0).AttributeClass.ToDisplayString)

                                                 ' additional synthetic members (field, accessors and such) should not
                                                 'member = type.GetMember("RegularEventEvent")
                                                 'attrs = member.GetAttributes()
                                                 'Assert.Equal(0, attrs.Count)

                                                 member = type.GetMember("RegularEventEventHandler")
                                                 attrs = member.GetAttributes()
                                                 Assert.Equal(0, attrs.Length)

                                                 member = type.GetMember("add_RegularEvent")
                                                 attrs = member.GetAttributes()
                                                 Assert.Equal(1, attrs.Length)
                                                 Assert.Equal("CompilerGeneratedAttribute", attrs(0).AttributeClass.Name)

                                                 member = type.GetMember("remove_RegularEvent")
                                                 attrs = member.GetAttributes()
                                                 Assert.Equal(1, attrs.Length)
                                                 Assert.Equal("CompilerGeneratedAttribute", attrs(0).AttributeClass.Name)

                                                 ' Event should have an Obsolete attribute
                                                 member = type.GetMember("CustomEvent")
                                                 attrs = member.GetAttributes()
                                                 Assert.Equal(1, attrs.Length)
                                                 Assert.Equal("System.ObsoleteAttribute", attrs(0).AttributeClass.ToDisplayString)

                                                 ' additional synthetic members (field, accessors and such) should not
                                                 member = type.GetMember("add_CustomEvent")
                                                 attrs = member.GetAttributes()
                                                 Assert.Equal(0, attrs.Length)

                                                 member = type.GetMember("remove_CustomEvent")
                                                 attrs = member.GetAttributes()
                                                 Assert.Equal(0, attrs.Length)

                                                 'member = type.GetMember("raise_CustomEvent")
                                                 'attrs = member.GetAttributes()
                                                 'Assert.Equal(0, attrs.Count)
                                             End Sub

            ' Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(source, sourceSymbolValidator:=attributeValidatorSource,
                             symbolValidator:=attributeValidatorMetadata)

        End Sub

        <WorkItem(543321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543321")>
        <Fact()>
        Public Sub DeclareEventWithArgument()
            CompileAndVerify(
    <compilation name="DeclareEventWithArgument">
        <file name="a.vb">
Class Test
    Public Event Percent(ByVal Percent1 As Single)
    Public Shared Sub Main()
    End Sub
End Class
    </file>
    </compilation>)
        End Sub

        <WorkItem(543366, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543366")>
        <Fact()>
        Public Sub UseEventDelegateType()
            CompileAndVerify(
    <compilation name="DeclareEventWithArgument">
        <file name="a.vb">
Class C
    Event Hello()
End Class
Module Program
    Sub Main(args As String())
        Dim cc As C = New C
        Dim a As C.HelloEventHandler = AddressOf Handler
        AddHandler cc.Hello, a
    End Sub
    Sub Handler()
    End Sub
End Module
    </file>
    </compilation>)
        End Sub

        <WorkItem(543372, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543372")>
        <Fact()>
        Public Sub AddHandlerWithoutAddressOf()
            Dim source = <compilation name="F">
                             <file name="F.vb">
Class C
    Event Hello()
End Class

Module Program
    Sub Goo()
    End Sub
    Sub Main(args As String())
        Dim x As C
        AddHandler x.Hello, Goo
    End Sub
End Module

                             </file>
                         </compilation>

            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertTheseDiagnostics(comp2,
<expected>
BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.
        AddHandler x.Hello, Goo
                   ~
BC30491: Expression does not produce a value.
        AddHandler x.Hello, Goo
                            ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub EventPrivateAccessor()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit ClassLibrary1.Class1
       extends [mscorlib]System.Object
{
  .field private class [mscorlib]System.Action E1
  .method private hidebysig specialname instance void 
          add_E1(class [mscorlib]System.Action 'value') cil managed
  {
    // Code size       41 (0x29)
    .maxstack  3
    .locals init (class [mscorlib]System.Action V_0,
             class [mscorlib]System.Action V_1,
             class [mscorlib]System.Action V_2)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class [mscorlib]System.Action ClassLibrary1.Class1::E1
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  stloc.1
    IL_0009:  ldloc.1
    IL_000a:  ldarg.1
    IL_000b:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate,
                                                                                            class [mscorlib]System.Delegate)
    IL_0010:  castclass  [mscorlib]System.Action
    IL_0015:  stloc.2
    IL_0016:  ldarg.0
    IL_0017:  ldflda     class [mscorlib]System.Action ClassLibrary1.Class1::E1
    IL_001c:  ldloc.2
    IL_001d:  ldloc.1
    IL_001e:  call       !!0 [mscorlib]System.Threading.Interlocked::CompareExchange<class [mscorlib]System.Action>(!!0&,
                                                                                                                    !!0,
                                                                                                                    !!0)
    IL_0023:  stloc.0
    IL_0024:  ldloc.0
    IL_0025:  ldloc.1
    IL_0026:  bne.un.s   IL_0007

    IL_0028:  ret
  } // end of method Class1::add_E1

  .method public hidebysig specialname instance void 
          remove_E1(class [mscorlib]System.Action 'value') cil managed
  {
    // Code size       41 (0x29)
    .maxstack  3
    .locals init (class [mscorlib]System.Action V_0,
             class [mscorlib]System.Action V_1,
             class [mscorlib]System.Action V_2)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class [mscorlib]System.Action ClassLibrary1.Class1::E1
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  stloc.1
    IL_0009:  ldloc.1
    IL_000a:  ldarg.1
    IL_000b:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate,
                                                                                           class [mscorlib]System.Delegate)
    IL_0010:  castclass  [mscorlib]System.Action
    IL_0015:  stloc.2
    IL_0016:  ldarg.0
    IL_0017:  ldflda     class [mscorlib]System.Action ClassLibrary1.Class1::E1
    IL_001c:  ldloc.2
    IL_001d:  ldloc.1
    IL_001e:  call       !!0 [mscorlib]System.Threading.Interlocked::CompareExchange<class [mscorlib]System.Action>(!!0&,
                                                                                                                    !!0,
                                                                                                                    !!0)
    IL_0023:  stloc.0
    IL_0024:  ldloc.0
    IL_0025:  ldloc.1
    IL_0026:  bne.un.s   IL_0007

    IL_0028:  ret
  } // end of method Class1::remove_E1

  .method public hidebysig instance void 
          Raise(int32 x) cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class [mscorlib]System.Action ClassLibrary1.Class1::E1
    IL_0006:  callvirt   instance void [mscorlib]System.Action::Invoke()
    IL_000b:  ret
  } // end of method Class1::Raise

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Class1::.ctor

  .event [mscorlib]System.Action E1
  {
    .addon instance void ClassLibrary1.Class1::add_E1(class [mscorlib]System.Action)
    .removeon instance void ClassLibrary1.Class1::remove_E1(class [mscorlib]System.Action)
  } // end of event Class1::E1
} // end of class ClassLibrary1.Class1

]]>

            Dim vbSource =
<compilation name="PublicParameterlessConstructorInMetadata_Private">
    <file name="a.vb">
Class Program
    Sub Main()
        Dim x = New ClassLibrary1.Class1

        Dim h as System.Action = Sub() System.Console.WriteLine("hello")

        AddHandler x.E1, h
        RemoveHandler x.E1, h

        x.Raise(1)
    End Sub
End Class
    </file>
</compilation>

            Dim comp2 = CreateCompilationWithCustomILSource(vbSource, ilSource.Value, TestOptions.ReleaseDll)

            CompilationUtils.AssertTheseDiagnostics(comp2,
<expected>
    <![CDATA[   
BC30456: 'E1' is not a member of 'Class1'.
        AddHandler x.E1, h
                   ~~~~
BC30456: 'E1' is not a member of 'Class1'.
        RemoveHandler x.E1, h
                      ~~~~
]]>
</expected>)
        End Sub

        <Fact>
        Public Sub EventProtectedAccessor()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit ClassLibrary1.Class1
       extends [mscorlib]System.Object
{
  .field private class [mscorlib]System.Action E1
  .method public hidebysig specialname instance void 
          add_E1(class [mscorlib]System.Action 'value') cil managed
  {
    // Code size       41 (0x29)
    .maxstack  3
    .locals init (class [mscorlib]System.Action V_0,
             class [mscorlib]System.Action V_1,
             class [mscorlib]System.Action V_2)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class [mscorlib]System.Action ClassLibrary1.Class1::E1
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  stloc.1
    IL_0009:  ldloc.1
    IL_000a:  ldarg.1
    IL_000b:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate,
                                                                                            class [mscorlib]System.Delegate)
    IL_0010:  castclass  [mscorlib]System.Action
    IL_0015:  stloc.2
    IL_0016:  ldarg.0
    IL_0017:  ldflda     class [mscorlib]System.Action ClassLibrary1.Class1::E1
    IL_001c:  ldloc.2
    IL_001d:  ldloc.1
    IL_001e:  call       !!0 [mscorlib]System.Threading.Interlocked::CompareExchange<class [mscorlib]System.Action>(!!0&,
                                                                                                                    !!0,
                                                                                                                    !!0)
    IL_0023:  stloc.0
    IL_0024:  ldloc.0
    IL_0025:  ldloc.1
    IL_0026:  bne.un.s   IL_0007

    IL_0028:  ret
  } // end of method Class1::add_E1

  .method family hidebysig specialname instance void 
          remove_E1(class [mscorlib]System.Action 'value') cil managed
  {
    // Code size       41 (0x29)
    .maxstack  3
    .locals init (class [mscorlib]System.Action V_0,
             class [mscorlib]System.Action V_1,
             class [mscorlib]System.Action V_2)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class [mscorlib]System.Action ClassLibrary1.Class1::E1
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  stloc.1
    IL_0009:  ldloc.1
    IL_000a:  ldarg.1
    IL_000b:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate,
                                                                                           class [mscorlib]System.Delegate)
    IL_0010:  castclass  [mscorlib]System.Action
    IL_0015:  stloc.2
    IL_0016:  ldarg.0
    IL_0017:  ldflda     class [mscorlib]System.Action ClassLibrary1.Class1::E1
    IL_001c:  ldloc.2
    IL_001d:  ldloc.1
    IL_001e:  call       !!0 [mscorlib]System.Threading.Interlocked::CompareExchange<class [mscorlib]System.Action>(!!0&,
                                                                                                                    !!0,
                                                                                                                    !!0)
    IL_0023:  stloc.0
    IL_0024:  ldloc.0
    IL_0025:  ldloc.1
    IL_0026:  bne.un.s   IL_0007

    IL_0028:  ret
  } // end of method Class1::remove_E1

  .method public hidebysig instance void 
          Raise(int32 x) cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class [mscorlib]System.Action ClassLibrary1.Class1::E1
    IL_0006:  callvirt   instance void [mscorlib]System.Action::Invoke()
    IL_000b:  ret
  } // end of method Class1::Raise

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Class1::.ctor

  .event [mscorlib]System.Action E1
  {
    .addon instance void ClassLibrary1.Class1::add_E1(class [mscorlib]System.Action)
    .removeon instance void ClassLibrary1.Class1::remove_E1(class [mscorlib]System.Action)
  } // end of event Class1::E1
} // end of class ClassLibrary1.Class1

]]>

            Dim vbSource =
<compilation name="PublicParameterlessConstructorInMetadata_Private">
    <file name="a.vb">
Class Program
    Sub Main()
        Dim x = New ClassLibrary1.Class1

        Dim h as System.Action = Sub() System.Console.WriteLine("hello")

        AddHandler x.E1, h
        RemoveHandler x.E1, h

        x.Raise(1)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource.Value, TestOptions.ReleaseDll)

            compilation.AssertTheseDiagnostics(
<expected>
BC30390: 'Class1.Protected Overloads RemoveHandler Event E1(value As Action)' is not accessible in this context because it is 'Protected'.
        RemoveHandler x.E1, h
                      ~~~~
</expected>)
        End Sub

        ' Check that both errors are reported
        <WorkItem(543504, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543504")>
        <Fact()>
        Public Sub TestEventWithParamArray()
            Dim source =
<compilation name="TestEventWithParamArray">
    <file name="a.vb">
        <![CDATA[
Class A
    Event E1(paramarray o() As object)
    Delegate Sub d(paramarray o() As object)
End Class

Module Program
    Sub Main(args As String())
    End Sub
End Module
]]>
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_ParamArrayIllegal1, "paramarray").WithArguments("Event"),
                                   Diagnostic(ERRID.ERR_ParamArrayIllegal1, "paramarray").WithArguments("Delegate"))
        End Sub

        'import abstract class with abstract event and attempt to override the event
        <Fact()>
        Public Sub EventOverridingAndInterop()

            Dim ilSource = <![CDATA[
// =============== CLASS MEMBERS DECLARATION ===================

.class public abstract auto ansi beforefieldinit AbsEvent
       extends [mscorlib]System.Object
{
  .field private class [mscorlib]System.Action E
  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  br.s       IL_0008

    IL_0008:  ret
  } // end of method AbsEvent::.ctor

  .method public hidebysig newslot specialname abstract virtual 
          instance void  add_E(class [mscorlib]System.Action 'value') cil managed
  {
  } // end of method AbsEvent::add_E

  .method public hidebysig newslot specialname abstract virtual 
          instance void  remove_E(class [mscorlib]System.Action 'value') cil managed
  {
  } // end of method AbsEvent::remove_E

  .event [mscorlib]System.Action E
  {
    .addon instance void AbsEvent::add_E(class [mscorlib]System.Action)
    .removeon instance void AbsEvent::remove_E(class [mscorlib]System.Action)
  } // end of event AbsEvent::E
} // end of class AbsEvent


]]>

            Dim vbSource =
<compilation>
    <file name="b.vb">
Class B
    Inherits AbsEvent
    Overrides Public Event E As System.Action
End Class
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithCustomILSource(vbSource, ilSource)

            AssertTheseDiagnostics(comp,
<expected>
BC31499: 'Public MustOverride Event E As Action' is a MustOverride event in the base class 'AbsEvent'. Visual Basic does not support event overriding. You must either provide an implementation for the event in the base class, or make class 'B' MustInherit.
Class B
      ~
BC30243: 'Overrides' is not valid on an event declaration.
    Overrides Public Event E As System.Action
    ~~~~~~~~~
BC40004: event 'E' conflicts with event 'E' in the base class 'AbsEvent' and should be declared 'Shadows'.
    Overrides Public Event E As System.Action
                           ~
</expected>)
        End Sub

        <WorkItem(529772, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529772")>
        <Fact>
        Public Sub Bug529772_ReproSteps()
            Dim csCompilation = CreateCSharpCompilation("
using System;
namespace AbstEvent
{

    public abstract class Base
    {
        public abstract event EventHandler AnEvent;
        public abstract void method();
        public event EventHandler AnotherEvent;
    }
    public class base1 : Base
    {
        public override event EventHandler AnEvent;
        public override void method() { }
    }

    public abstract class base2 : Base
    {
        public override void method() { }
    }

    public abstract class GenBase<T>
    {
        public abstract event EventHandler AnEvent;
    }

}",
                assemblyName:="AbstEvent",
                referencedAssemblies:={MscorlibRef})

            Dim vbCompilation = CreateCompilationWithMscorlib45AndVBRuntime(
                <compilation>
                    <file name="App.vb">
Imports AbstEvent

Module Module1
    Sub Main()
    End Sub

    ' Expect compiler catch that Goo1 does not implement AnEvent or method()

    Class Goo1
        Inherits Base
    End Class

    ' Expect compiler catch Goo2 does not implement AnEvent

    Class Goo2
        Inherits Base
        Public Overrides Sub method()
        End Sub
    End Class

    ' Expect compiler catch that Goo3 does not implement AnEvent

    Class Goo3
        Inherits base2
    End Class

    ' Expect no compiler error

    Class Goo4
        Inherits base1
    End Class

    ' Expect no compiler error, since both Goo5 and base2 are abstract

    MustInherit Class Goo5
        Inherits base2
    End Class

    '
    ' Testing Type Parameter Printing
    '
    Class GenGoo1(Of T)
        Inherits GenBase(Of T)
    End Class

    Class GenGoo2
        Inherits GenBase(Of Integer)
    End Class

    MustInherit Class Goo6
        Inherits base2
        Shadows Public AnEvent As Integer
    End Class
End Module
                    </file>
                </compilation>,
                references:={csCompilation.EmitToImageReference()})

            vbCompilation.AssertTheseDiagnostics(<errors>
BC30610: Class 'Goo1' must either be declared 'MustInherit' or override the following inherited 'MustOverride' member(s): 
    Base: Public MustOverride Overloads Sub method().
    Class Goo1
          ~~~~
BC31499: 'Public MustOverride Event AnEvent As EventHandler' is a MustOverride event in the base class 'AbstEvent.Base'. Visual Basic does not support event overriding. You must either provide an implementation for the event in the base class, or make class 'Goo1' MustInherit.
    Class Goo1
          ~~~~
BC31499: 'Public MustOverride Event AnEvent As EventHandler' is a MustOverride event in the base class 'AbstEvent.Base'. Visual Basic does not support event overriding. You must either provide an implementation for the event in the base class, or make class 'Goo2' MustInherit.
    Class Goo2
          ~~~~
BC31499: 'Public MustOverride Event AnEvent As EventHandler' is a MustOverride event in the base class 'AbstEvent.Base'. Visual Basic does not support event overriding. You must either provide an implementation for the event in the base class, or make class 'Goo3' MustInherit.
    Class Goo3
          ~~~~
BC31499: 'Public MustOverride Event AnEvent As EventHandler' is a MustOverride event in the base class 'AbstEvent.GenBase(Of T)'. Visual Basic does not support event overriding. You must either provide an implementation for the event in the base class, or make class 'GenGoo1' MustInherit.
    Class GenGoo1(Of T)
          ~~~~~~~
BC31499: 'Public MustOverride Event AnEvent As EventHandler' is a MustOverride event in the base class 'AbstEvent.GenBase(Of Integer)'. Visual Basic does not support event overriding. You must either provide an implementation for the event in the base class, or make class 'GenGoo2' MustInherit.
    Class GenGoo2
          ~~~~~~~
BC31404: 'Public AnEvent As Integer' cannot shadow a method declared 'MustOverride'.
        Shadows Public AnEvent As Integer
                       ~~~~~~~
</errors>)

        End Sub

        <WorkItem(529772, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529772")>
        <Fact>
        Public Sub Bug529772_ReproStepsWithILSource()

            Dim ilSource = "
.class public abstract auto ansi beforefieldinit AbstEvent.Base extends [mscorlib]System.Object
{
  .field private class [mscorlib]System.EventHandler AnotherEvent
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig newslot specialname abstract virtual instance void add_AnEvent(class [mscorlib]System.EventHandler 'value')
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  }

  .method public hidebysig newslot specialname abstract virtual instance void remove_AnEvent(class [mscorlib]System.EventHandler 'value')
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  }

  .method public hidebysig newslot abstract virtual instance void 'method'()
  {
  }

  .method public hidebysig specialname instance void add_AnotherEvent(class [mscorlib]System.EventHandler 'value')
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
     ret
  }

  .method public hidebysig specialname instance void remove_AnotherEvent(class [mscorlib]System.EventHandler 'value')
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
     ret
  }

  .method family hidebysig specialname rtspecialname instance void .ctor()
  {
     ret
  }

  .event [mscorlib]System.EventHandler AnEvent
  {
    .addon instance void AbstEvent.Base::add_AnEvent(class [mscorlib]System.EventHandler)
    .removeon instance void AbstEvent.Base::remove_AnEvent(class [mscorlib]System.EventHandler)
  }

  .event [mscorlib]System.EventHandler AnotherEvent
  {
    .addon instance void AbstEvent.Base::add_AnotherEvent(class [mscorlib]System.EventHandler)
    .removeon instance void AbstEvent.Base::remove_AnotherEvent(class [mscorlib]System.EventHandler)
  }
}

.class public auto ansi beforefieldinit AbstEvent.base1 extends AbstEvent.Base
{
  .field private class [mscorlib]System.EventHandler AnEvent
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig specialname virtual instance void add_AnEvent(class [mscorlib]System.EventHandler 'value')
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
     ret
  }

  .method public hidebysig specialname virtual instance void remove_AnEvent(class [mscorlib]System.EventHandler 'value')
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
     ret
  }

  .method public hidebysig virtual instance void 'method'()
  {
     ret
  }

  .method public hidebysig specialname rtspecialname instance void .ctor()
  {
     ret
  }

  .event [mscorlib]System.EventHandler AnEvent
  {
    .addon instance void AbstEvent.base1::add_AnEvent(class [mscorlib]System.EventHandler)
    .removeon instance void AbstEvent.base1::remove_AnEvent(class [mscorlib]System.EventHandler)
  }
}

.class public abstract auto ansi beforefieldinit AbstEvent.base2 extends AbstEvent.Base
{
  .method public hidebysig virtual instance void 'method'()
  {
     ret
  }

  .method family hidebysig specialname rtspecialname instance void .ctor()
  {
     ret
  }
}

.class public abstract auto ansi beforefieldinit AbstEvent.GenBase`1<T> extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname abstract virtual instance void add_AnEvent(class [mscorlib]System.EventHandler 'value')
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  }

  .method public hidebysig newslot specialname abstract virtual instance void remove_AnEvent(class [mscorlib]System.EventHandler 'value')
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  }

  .method family hidebysig specialname rtspecialname instance void .ctor()
  {
     ret
  }

  .event [mscorlib]System.EventHandler AnEvent
  {
    .addon instance void AbstEvent.GenBase`1::add_AnEvent(class [mscorlib]System.EventHandler)
    .removeon instance void AbstEvent.GenBase`1::remove_AnEvent(class [mscorlib]System.EventHandler)
  }
}"

            Dim vbSource =
<compilation>
    <file name="App.vb">
Imports AbstEvent

Module Module1
    Sub Main()
    End Sub

    ' Expect compiler catch that Goo1 does not implement AnEvent or method()

    Class Goo1
        Inherits Base
    End Class

    ' Expect compiler catch Goo2 does not implement AnEvent

    Class Goo2
        Inherits Base
        Public Overrides Sub method()
        End Sub
    End Class

    ' Expect compiler catch that Goo3 does not implement AnEvent

    Class Goo3
        Inherits base2
    End Class

    ' Expect no compiler error

    Class Goo4
        Inherits base1
    End Class

    ' Expect no compiler error, since both Goo5 and base2 are abstract

    MustInherit Class Goo5
        Inherits base2
    End Class

    '
    ' Testing Type Parameter Printing
    '
    Class GenGoo1(Of T)
        Inherits GenBase(Of T)
    End Class

    Class GenGoo2
        Inherits GenBase(Of Integer)
    End Class

    MustInherit Class Goo6
        Inherits base2
        Shadows Public AnEvent As Integer
    End Class
End Module
                    </file>
</compilation>

            Dim vbCompilation = CreateCompilationWithCustomILSource(vbSource, ilSource, includeVbRuntime:=True)

            vbCompilation.AssertTheseDiagnostics(<errors>
BC30610: Class 'Goo1' must either be declared 'MustInherit' or override the following inherited 'MustOverride' member(s): 
    Base: Public MustOverride Overloads Sub method().
    Class Goo1
          ~~~~
BC31499: 'Public MustOverride Event AnEvent As EventHandler' is a MustOverride event in the base class 'AbstEvent.Base'. Visual Basic does not support event overriding. You must either provide an implementation for the event in the base class, or make class 'Goo1' MustInherit.
    Class Goo1
          ~~~~
BC31499: 'Public MustOverride Event AnEvent As EventHandler' is a MustOverride event in the base class 'AbstEvent.Base'. Visual Basic does not support event overriding. You must either provide an implementation for the event in the base class, or make class 'Goo2' MustInherit.
    Class Goo2
          ~~~~
BC31499: 'Public MustOverride Event AnEvent As EventHandler' is a MustOverride event in the base class 'AbstEvent.Base'. Visual Basic does not support event overriding. You must either provide an implementation for the event in the base class, or make class 'Goo3' MustInherit.
    Class Goo3
          ~~~~
BC31499: 'Public MustOverride Event AnEvent As EventHandler' is a MustOverride event in the base class 'AbstEvent.GenBase(Of T)'. Visual Basic does not support event overriding. You must either provide an implementation for the event in the base class, or make class 'GenGoo1' MustInherit.
    Class GenGoo1(Of T)
          ~~~~~~~
BC31499: 'Public MustOverride Event AnEvent As EventHandler' is a MustOverride event in the base class 'AbstEvent.GenBase(Of Integer)'. Visual Basic does not support event overriding. You must either provide an implementation for the event in the base class, or make class 'GenGoo2' MustInherit.
    Class GenGoo2
          ~~~~~~~
BC31404: 'Public AnEvent As Integer' cannot shadow a method declared 'MustOverride'.
        Shadows Public AnEvent As Integer
                       ~~~~~~~
</errors>)

        End Sub

        <Fact()>
        Public Sub EventInGenericTypes()
            Dim vbSource =
<compilation>
    <file name="filename.vb">
Class A(Of T)
    Public Event E1(arg As T)
    Public Event E2 As System.Action(Of T, T)
End Class

Class B
    Sub S
        Dim x = New A(Of String)
        Dim a = New A(Of String).E1EventHandler(Sub(arg)
                                                End Sub)
        AddHandler x.E1, a
        AddHandler x.E2, Sub(a1, a2)
                         End Sub
    End Sub
End Class
    </file>
</compilation>
            CompileAndVerify(vbSource,
                             sourceSymbolValidator:=Sub(moduleSymbol As ModuleSymbol)
                                                        Dim tA = DirectCast(moduleSymbol.GlobalNamespace.GetMember("A"), NamedTypeSymbol)
                                                        Dim tB = DirectCast(moduleSymbol.GlobalNamespace.GetMember("B"), NamedTypeSymbol)
                                                        Dim member = tA.GetMember("E1Event")
                                                        Assert.NotNull(member)
                                                        Dim delegateTypeMember = DirectCast(tA.GetMember("E1EventHandler"), SynthesizedEventDelegateSymbol)
                                                        Assert.NotNull(delegateTypeMember)
                                                        Assert.Equal(delegateTypeMember.AssociatedSymbol, DirectCast(tA.GetMember("E1"), EventSymbol))
                                                    End Sub)

        End Sub
        <Fact()>
        Public Sub BindOnRegularEventParams()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class C
    Event E(arg1 As Integer, arg2 As String)'BIND:"Integer"
End Class

Module Program
    Sub Main(args As String())

    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of PredefinedTypeSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Int32", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("System.Int32", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub BindOnEventHandlerAddHandler()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Event E
End Class

Module Program
    Sub Main(args As String())
        Dim x = New C
        AddHandler x.E, Sub()'BIND:"E"
                        End Sub
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("C.EEventHandler", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticSummary.Type.TypeKind)
            Assert.Equal("C.EEventHandler", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Event C.E()", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Event, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub BindOnEventPrivateField()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Event E
End Class

Module Program
    Sub Main(args As String())
        Dim x = New C
        AddHandler x.EEvent, Sub()'BIND:"EEvent"
                             End Sub
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("C.EEventHandler", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticSummary.Type.TypeKind)
            Assert.Equal("C.EEventHandler", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("C.EEvent As C.EEventHandler", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Field, sortedCandidates(0).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(543447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543447")>
        <Fact()>
        Public Sub BindOnFieldOfRegularEventHandlerType()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Dim ev As EEventHandler
    Event E
    Sub T
        ev = Nothing'BIND:"ev"

    End Sub
End Class

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("C.EEventHandler", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticSummary.Type.TypeKind)
            Assert.Equal("C.EEventHandler", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("C.ev As C.EEventHandler", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Field, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(543725, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543725")>
        <Fact()>
        Public Sub SynthesizedEventDelegateSymbolImplicit()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C
    Event E()
End Class
    ]]></file>
</compilation>)

            Dim typeC = DirectCast(compilation.SourceModule.GlobalNamespace.GetMembers("C").SingleOrDefault(), NamedTypeSymbol)
            Dim mems = typeC.GetMembers().OrderBy(Function(s) s.ToDisplayString()).Select(Function(s) s)
            'Event Delegate Symbol
            Assert.Equal(TypeKind.Delegate, DirectCast(mems(0), NamedTypeSymbol).TypeKind)
            Assert.True(mems(0).IsImplicitlyDeclared)
            Assert.Equal("C.EEventHandler", mems(0).ToDisplayString())
            'Event Backing Field
            Assert.Equal(SymbolKind.Field, mems(1).Kind)
            Assert.True(mems(1).IsImplicitlyDeclared)
            Assert.Equal("Private EEvent As C.EEventHandler", mems(1).ToDisplayString())

            ' Source Event Symbol
            Assert.Equal(SymbolKind.Event, mems(3).Kind)
            Assert.False(mems(3).IsImplicitlyDeclared)
            Assert.Equal("Public Event E()", mems(3).ToDisplayString())

            ' Add Accessor
            Assert.Equal(MethodKind.EventAdd, DirectCast(mems(2), MethodSymbol).MethodKind)
            Assert.True(mems(2).IsImplicitlyDeclared)
            Assert.Equal("Public AddHandler Event E(obj As C.EEventHandler)", mems(2).ToDisplayString())
            'Remove Accessor
            Assert.Equal(MethodKind.EventRemove, DirectCast(mems(4), MethodSymbol).MethodKind)
            Assert.True(mems(4).IsImplicitlyDeclared)
            Assert.Equal("Public RemoveHandler Event E(obj As C.EEventHandler)", mems(4).ToDisplayString())

        End Sub

        <WorkItem(545200, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545200")>
        <Fact()>
        Public Sub TestBadlyFormattedEventCode()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System<Serializable>Class c11    <NonSerialized()>
    Public Event Start(ByVal sender As Object, ByVal e As EventArgs)
    <NonSerialized>    Dim x As LongEnd Class
    ]]></file>
</compilation>)

            Dim typeMembers = compilation.SourceModule.GlobalNamespace.GetMembers().OfType(Of TypeSymbol)()
            Assert.Equal(1, typeMembers.Count)
            Dim implicitClass = typeMembers.First

            Assert.True(DirectCast(implicitClass, NamedTypeSymbol).IsImplicitClass)
            Assert.False(implicitClass.CanBeReferencedByName)

            Dim classMembers = implicitClass.GetMembers()
            Assert.Equal(7, classMembers.Length)

            Dim eventDelegate = classMembers.OfType(Of SynthesizedEventDelegateSymbol)().Single
            Assert.Equal("StartEventHandler", eventDelegate.Name)
        End Sub

        <WorkItem(545221, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545221")>
        <Fact()>
        Public Sub TestBadlyFormattedCustomEvent()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Partial Class c1
    Private Custom Event E1 as
        AddHandler()
        End AddHandler
    End Event
    Partial Private Sub M(i As Integer) Handles Me.E1'BIND:"E1"
    End Sub
    Sub Raise()
        RaiseEvent E1(1)
    End Sub
    Shared Sub Main()
        Call New c1().Raise()
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Event c1.E1 As ?", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Event, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        ''' <summary>
        ''' Avoid redundant errors from handlers when
        ''' a custom event type has errors.
        ''' </summary>
        <Fact>
        <WorkItem(101185, "https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems?_a=edit&id=101185")>
        <WorkItem(530406, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530406")>
        Public Sub CustomEventTypeDuplicateErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Public Custom Event E As D
        AddHandler(value As D)
        End AddHandler
        RemoveHandler(value As D)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
    Private Delegate Sub D()
End Class
   ]]></file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30508: 'E' cannot expose type 'C.D' in namespace '<Default>' through class 'C'.
    Public Custom Event E As D
                        ~
BC30508: 'value' cannot expose type 'C.D' in namespace '<Default>' through class 'C'.
        AddHandler(value As D)
                            ~
BC30508: 'value' cannot expose type 'C.D' in namespace '<Default>' through class 'C'.
        RemoveHandler(value As D)
                               ~
     ]]></errors>)
        End Sub

        <Fact()>
        Public Sub MissingSystemTypes_Event()
            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Interface I
    Event E As Object
End Interface
   ]]></file>
</compilation>, references:=Nothing)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30002: Type 'System.Void' is not defined.
    Event E As Object
          ~
BC30002: Type 'System.Object' is not defined.
    Event E As Object
               ~~~~~~
BC31044: Events declared with an 'As' clause must have a delegate type.
    Event E As Object
               ~~~~~~
     ]]></errors>)
        End Sub

        <Fact()>
        Public Sub MissingSystemTypes_WithEvents()
            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(
<compilation name="C">
    <file name="a.vb"><![CDATA[
Class C
    WithEvents F As Object
End Class
   ]]></file>
</compilation>, references:=Nothing)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30002: Type 'System.Void' is not defined.
Class C
~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'C.dll' failed.
Class C
      ~
BC30002: Type 'System.Void' is not defined.
    WithEvents F As Object
               ~
BC35000: Requested operation is not available because the runtime library function 'System.Runtime.CompilerServices.AccessedThroughPropertyAttribute..ctor' is not defined.
    WithEvents F As Object
               ~
BC30002: Type 'System.Object' is not defined.
    WithEvents F As Object
                    ~~~~~~
     ]]></errors>)
        End Sub

        <WorkItem(780993, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/780993")>
        <Fact()>
        Public Sub EventInMemberNames()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Event X As EventHandler
End Class

    ]]></file>
</compilation>)

            Dim typeMembers = compilation.SourceModule.GlobalNamespace.GetMembers().OfType(Of NamedTypeSymbol)()
            Assert.Equal(1, typeMembers.Count)
            Dim c = typeMembers.First

            Dim classMembers = c.MemberNames
            Assert.Equal(1, classMembers.Count)

            Assert.Equal("X", classMembers(0))
        End Sub

        <Fact, WorkItem(1027568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027568"), WorkItem(528573, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528573")>
        Public Sub MissingCompareExchange_01()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Event X As System.EventHandler
End Class
    ]]></file>
</compilation>)

            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Interlocked__CompareExchange_T)
            compilation.MakeMemberMissing(SpecialMember.System_Delegate__Combine)
            compilation.MakeMemberMissing(SpecialMember.System_Delegate__Remove)

            AssertTheseEmitDiagnostics(compilation,
<expected>
BC35000: Requested operation is not available because the runtime library function 'System.Delegate.Combine' is not defined.
    Event X As System.EventHandler
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Delegate.Remove' is not defined.
    Event X As System.EventHandler
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact, WorkItem(1027568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027568"), WorkItem(528573, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528573")>
        Public Sub MissingCompareExchange_02()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
public delegate sub E1()

class C
    public event e As E1

    public shared sub Main()
        Dim v = new C()
        System.Console.Write(v.eEvent Is Nothing) 
        Addhandler v.e, AddressOf Main
        System.Console.Write(v.eEvent Is Nothing) 
        Removehandler v.e, AddressOf Main
        System.Console.Write(v.eEvent Is Nothing) 
    End Sub
End Class
    ]]></file>
</compilation>, options:=TestOptions.DebugExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="TrueFalseTrue",
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim c = m.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                                                                 Dim e = c.GetMember(Of EventSymbol)("e")

                                                                 Dim addMethod = e.AddMethod
                                                                 Assert.True((addMethod.ImplementationAttributes And System.Reflection.MethodImplAttributes.Synchronized) = 0)

                                                                 Dim removeMethod = e.RemoveMethod
                                                                 Assert.True((removeMethod.ImplementationAttributes And System.Reflection.MethodImplAttributes.Synchronized) = 0)
                                                             End Sub).VerifyDiagnostics()

            verifier.VerifyIL("C.add_e",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  3
  .locals init (E1 V_0,
                E1 V_1,
                E1 V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "C.eEvent As E1"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  stloc.1
  IL_0009:  ldloc.1
  IL_000a:  ldarg.1
  IL_000b:  call       "Function System.Delegate.Combine(System.Delegate, System.Delegate) As System.Delegate"
  IL_0010:  castclass  "E1"
  IL_0015:  stloc.2
  IL_0016:  ldarg.0
  IL_0017:  ldflda     "C.eEvent As E1"
  IL_001c:  ldloc.2
  IL_001d:  ldloc.1
  IL_001e:  call       "Function System.Threading.Interlocked.CompareExchange(Of E1)(ByRef E1, E1, E1) As E1"
  IL_0023:  stloc.0
  IL_0024:  ldloc.0
  IL_0025:  ldloc.1
  IL_0026:  bne.un.s   IL_0007
  IL_0028:  ret
}
]]>)

            verifier.VerifyIL("C.remove_e",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  3
  .locals init (E1 V_0,
                E1 V_1,
                E1 V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "C.eEvent As E1"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  stloc.1
  IL_0009:  ldloc.1
  IL_000a:  ldarg.1
  IL_000b:  call       "Function System.Delegate.Remove(System.Delegate, System.Delegate) As System.Delegate"
  IL_0010:  castclass  "E1"
  IL_0015:  stloc.2
  IL_0016:  ldarg.0
  IL_0017:  ldflda     "C.eEvent As E1"
  IL_001c:  ldloc.2
  IL_001d:  ldloc.1
  IL_001e:  call       "Function System.Threading.Interlocked.CompareExchange(Of E1)(ByRef E1, E1, E1) As E1"
  IL_0023:  stloc.0
  IL_0024:  ldloc.0
  IL_0025:  ldloc.1
  IL_0026:  bne.un.s   IL_0007
  IL_0028:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(1027568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027568"), WorkItem(528573, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528573")>
        Public Sub MissingCompareExchange_03()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
public delegate sub E1()

Structure C
    public event e As E1

    public shared sub Main()
        Dim v = new C()
        System.Console.Write(v.eEvent Is Nothing) 
        Addhandler v.e, AddressOf Main
        System.Console.Write(v.eEvent Is Nothing) 
        Removehandler v.e, AddressOf Main
        System.Console.Write(v.eEvent Is Nothing) 
    End Sub
End Structure
    ]]></file>
</compilation>, options:=TestOptions.DebugExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="TrueFalseTrue",
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim c = m.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                                                                 Dim e = c.GetMember(Of EventSymbol)("e")

                                                                 Dim addMethod = e.AddMethod
                                                                 Assert.True((addMethod.ImplementationAttributes And System.Reflection.MethodImplAttributes.Synchronized) = 0)

                                                                 Dim removeMethod = e.RemoveMethod
                                                                 Assert.True((removeMethod.ImplementationAttributes And System.Reflection.MethodImplAttributes.Synchronized) = 0)
                                                             End Sub).VerifyDiagnostics()

            verifier.VerifyIL("C.add_e",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  3
  .locals init (E1 V_0,
                E1 V_1,
                E1 V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "C.eEvent As E1"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  stloc.1
  IL_0009:  ldloc.1
  IL_000a:  ldarg.1
  IL_000b:  call       "Function System.Delegate.Combine(System.Delegate, System.Delegate) As System.Delegate"
  IL_0010:  castclass  "E1"
  IL_0015:  stloc.2
  IL_0016:  ldarg.0
  IL_0017:  ldflda     "C.eEvent As E1"
  IL_001c:  ldloc.2
  IL_001d:  ldloc.1
  IL_001e:  call       "Function System.Threading.Interlocked.CompareExchange(Of E1)(ByRef E1, E1, E1) As E1"
  IL_0023:  stloc.0
  IL_0024:  ldloc.0
  IL_0025:  ldloc.1
  IL_0026:  bne.un.s   IL_0007
  IL_0028:  ret
}
]]>)

            verifier.VerifyIL("C.remove_e",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  3
  .locals init (E1 V_0,
                E1 V_1,
                E1 V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "C.eEvent As E1"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  stloc.1
  IL_0009:  ldloc.1
  IL_000a:  ldarg.1
  IL_000b:  call       "Function System.Delegate.Remove(System.Delegate, System.Delegate) As System.Delegate"
  IL_0010:  castclass  "E1"
  IL_0015:  stloc.2
  IL_0016:  ldarg.0
  IL_0017:  ldflda     "C.eEvent As E1"
  IL_001c:  ldloc.2
  IL_001d:  ldloc.1
  IL_001e:  call       "Function System.Threading.Interlocked.CompareExchange(Of E1)(ByRef E1, E1, E1) As E1"
  IL_0023:  stloc.0
  IL_0024:  ldloc.0
  IL_0025:  ldloc.1
  IL_0026:  bne.un.s   IL_0007
  IL_0028:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(1027568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027568"), WorkItem(528573, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528573")>
        Public Sub MissingCompareExchange_04()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
public delegate sub E1()

class C
    public event e As E1

    public shared sub Main()
        Dim v = new C()
        System.Console.Write(v.eEvent Is Nothing) 
        Addhandler v.e, AddressOf Main
        System.Console.Write(v.eEvent Is Nothing) 
        Removehandler v.e, AddressOf Main
        System.Console.Write(v.eEvent Is Nothing) 
    End Sub
End Class
    ]]></file>
</compilation>, options:=TestOptions.DebugExe)

            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Interlocked__CompareExchange_T)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="TrueFalseTrue",
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim c = m.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                                                                 Dim e = c.GetMember(Of EventSymbol)("e")

                                                                 Dim addMethod = e.AddMethod
                                                                 Assert.False((addMethod.ImplementationAttributes And System.Reflection.MethodImplAttributes.Synchronized) = 0)

                                                                 Dim removeMethod = e.RemoveMethod
                                                                 Assert.False((removeMethod.ImplementationAttributes And System.Reflection.MethodImplAttributes.Synchronized) = 0)
                                                             End Sub).VerifyDiagnostics()

            verifier.VerifyIL("C.add_e",
            <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldfld      "C.eEvent As E1"
  IL_0007:  ldarg.1
  IL_0008:  call       "Function System.Delegate.Combine(System.Delegate, System.Delegate) As System.Delegate"
  IL_000d:  castclass  "E1"
  IL_0012:  stfld      "C.eEvent As E1"
  IL_0017:  ret
}
]]>)

            verifier.VerifyIL("C.remove_e",
            <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldfld      "C.eEvent As E1"
  IL_0007:  ldarg.1
  IL_0008:  call       "Function System.Delegate.Remove(System.Delegate, System.Delegate) As System.Delegate"
  IL_000d:  castclass  "E1"
  IL_0012:  stfld      "C.eEvent As E1"
  IL_0017:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(1027568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027568"), WorkItem(528573, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528573")>
        Public Sub MissingCompareExchange_05()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
public delegate sub E1()

Structure C
    public event e As E1

    public shared sub Main()
        Dim v = new C()
        System.Console.Write(v.eEvent Is Nothing) 
        Addhandler v.e, AddressOf Main
        System.Console.Write(v.eEvent Is Nothing) 
        Removehandler v.e, AddressOf Main
        System.Console.Write(v.eEvent Is Nothing) 
    End Sub
End Structure
    ]]></file>
</compilation>, options:=TestOptions.DebugExe)

            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Interlocked__CompareExchange_T)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="TrueFalseTrue",
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim c = m.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                                                                 Dim e = c.GetMember(Of EventSymbol)("e")

                                                                 Dim addMethod = e.AddMethod
                                                                 Assert.True((addMethod.ImplementationAttributes And System.Reflection.MethodImplAttributes.Synchronized) = 0)

                                                                 Dim removeMethod = e.RemoveMethod
                                                                 Assert.True((removeMethod.ImplementationAttributes And System.Reflection.MethodImplAttributes.Synchronized) = 0)
                                                             End Sub).VerifyDiagnostics()

            verifier.VerifyIL("C.add_e",
            <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldfld      "C.eEvent As E1"
  IL_0007:  ldarg.1
  IL_0008:  call       "Function System.Delegate.Combine(System.Delegate, System.Delegate) As System.Delegate"
  IL_000d:  castclass  "E1"
  IL_0012:  stfld      "C.eEvent As E1"
  IL_0017:  ret
}
]]>)

            verifier.VerifyIL("C.remove_e",
            <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldfld      "C.eEvent As E1"
  IL_0007:  ldarg.1
  IL_0008:  call       "Function System.Delegate.Remove(System.Delegate, System.Delegate) As System.Delegate"
  IL_000d:  castclass  "E1"
  IL_0012:  stfld      "C.eEvent As E1"
  IL_0017:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(3448, "https://github.com/dotnet/roslyn/issues/3448")>
        Public Sub HandlesInAnInterface()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Interface I
    Event E()
    Sub M() Handles Me.E
End Interface
    ]]></file>
</compilation>, options:=TestOptions.DebugDll)

            Dim expected = <expected>
BC30270: 'Handles' is not valid on an interface method declaration.
    Sub M() Handles Me.E
            ~~~~~~~~~~~~
                           </expected>

            compilation.AssertTheseDiagnostics(expected)
            compilation.AssertTheseEmitDiagnostics(expected)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(n) n.Identifier.ValueText = "E").Single()

            Assert.Equal("Me.E", node.Parent.ToString())

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim symbolInfo = semanticModel.GetSymbolInfo(node)
            Assert.Equal("Event I.E()", symbolInfo.Symbol.ToTestDisplayString())
        End Sub

        <Fact, WorkItem(3448, "https://github.com/dotnet/roslyn/issues/3448")>
        Public Sub HandlesInAStruct()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Structure S
    Event E()
    Sub M() Handles Me.E
    End Sub
End Structure
    ]]></file>
</compilation>, options:=TestOptions.DebugDll)

            Dim expected = <expected>
BC30728: Methods declared in structures cannot have 'Handles' clauses.
    Sub M() Handles Me.E
        ~
                           </expected>

            compilation.AssertTheseDiagnostics(expected)
            compilation.AssertTheseEmitDiagnostics(expected)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(n) n.Identifier.ValueText = "E").Single()

            Assert.Equal("Me.E", node.Parent.ToString())

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim symbolInfo = semanticModel.GetSymbolInfo(node)
            Assert.Equal("Event S.E()", symbolInfo.Symbol.ToTestDisplayString())
        End Sub

        <Fact, WorkItem(3448, "https://github.com/dotnet/roslyn/issues/3448")>
        Public Sub HandlesInAnEnum()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Enum E1
    'Event E()
    Sub M() Handles Me.E
    End Sub
End Enum
    ]]></file>
</compilation>, options:=TestOptions.DebugDll)

            Dim expected = <expected>
BC30185: 'Enum' must end with a matching 'End Enum'.
Enum E1
~~~~~~~
BC30280: Enum 'E1' must contain at least one member.
Enum E1
     ~~
BC30619: Statement cannot appear within an Enum body. End of Enum assumed.
    Sub M() Handles Me.E
    ~~~~~~~~~~~~~~~~~~~~
BC30590: Event 'E' cannot be found.
    Sub M() Handles Me.E
                       ~
BC30184: 'End Enum' must be preceded by a matching 'Enum'.
End Enum
~~~~~~~~
                           </expected>

            compilation.AssertTheseDiagnostics(expected)
            compilation.AssertTheseEmitDiagnostics(expected)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(n) n.Identifier.ValueText = "E").Single()

            Assert.Equal("Me.E", node.Parent.ToString())

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim symbolInfo = semanticModel.GetSymbolInfo(node)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
        End Sub

        <WorkItem(9400, "https://github.com/dotnet/roslyn/issues/9400")>
        <Fact()>
        Public Sub HandlesNoMyBase()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Interface I
    Sub F() Handles MyBase.E
End Interface
]]></file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC30270: 'Handles' is not valid on an interface method declaration.
    Sub F() Handles MyBase.E
            ~~~~~~~~~~~~~~~~
BC30590: Event 'E' cannot be found.
    Sub F() Handles MyBase.E
                           ~
                           </expected>)
        End Sub

        <WorkItem(14364, "https://github.com/dotnet/roslyn/issues/14364")>
        <Fact()>
        Public Sub SemanticModelOnParameters_01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class A

    Public Event E1(x As Integer) 

End Class]]></file>
</compilation>, options:=TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim x = tree.GetRoot().DescendantNodes().OfType(Of ParameterSyntax)().Single().Identifier

            Dim model = compilation.GetSemanticModel(tree)
            Dim xSym = model.GetDeclaredSymbol(x)
            Assert.Equal("x As System.Int32", xSym.ToTestDisplayString())
            Assert.False(xSym.IsImplicitlyDeclared)
            Assert.Equal("x As Integer", xSym.DeclaringSyntaxReferences.Single().GetSyntax().ToString())

            Dim e1EventHandler = compilation.GetTypeByMetadataName("A+E1EventHandler").DelegateInvokeMethod
            Assert.Same(e1EventHandler.ContainingType, DirectCast(e1EventHandler.ContainingType.AssociatedSymbol, EventSymbol).Type)
            Assert.True(e1EventHandler.IsImplicitlyDeclared)
            Assert.True(e1EventHandler.ContainingType.IsImplicitlyDeclared)
            Assert.Same(e1EventHandler, xSym.ContainingSymbol)
            Assert.Same(xSym, e1EventHandler.Parameters.First())
        End Sub

        <WorkItem(14364, "https://github.com/dotnet/roslyn/issues/14364")>
        <Fact()>
        Public Sub SemanticModelOnParameters_02()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Interface I1
    Delegate Sub D (z As Integer)
    Event E1 As D
End Interface

Class A
    Implements I1
    Public Event E1(x As Integer) Implements I1.E1
End Class]]></file>
</compilation>, options:=TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            Dim a = compilation.GetTypeByMetadataName("A")
            Dim e1 = a.GetMember(Of EventSymbol)("E1")
            Assert.Equal("I1.D", e1.Type.ToTestDisplayString())

            Dim tree = compilation.SyntaxTrees.Single()
            Dim x = tree.GetRoot().DescendantNodes().OfType(Of ParameterSyntax)().ElementAt(1).Identifier

            Dim model = compilation.GetSemanticModel(tree)
            Dim xSym = model.GetDeclaredSymbol(x)
            Assert.Null(xSym)
        End Sub

        <WorkItem(14364, "https://github.com/dotnet/roslyn/issues/14364")>
        <Fact()>
        Public Sub SemanticModelOnParameters_03()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Interface I1
End Interface

Class A
    Implements I1
    Public Event E1(x As Integer) Implements I1.E1
End Class]]></file>
</compilation>, options:=TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(
<expected>
BC30401: 'E1' cannot implement 'E1' because there is no matching event on interface 'I1'.
    Public Event E1(x As Integer) Implements I1.E1
                                             ~~~~~
</expected>)

            Dim a = compilation.GetTypeByMetadataName("A")
            Dim e1 = a.GetMember(Of EventSymbol)("E1")
            Assert.Equal("Event A.E1(x As System.Int32)", e1.ToTestDisplayString())

            Assert.Equal("A.E1EventHandler", e1.Type.ToTestDisplayString())
            Assert.True(e1.Type.IsDelegateType())
            Assert.DoesNotContain(e1.Type, a.GetMembers())
            Assert.Same(a, e1.Type.ContainingType)
            Assert.True(e1.Type.IsImplicitlyDeclared)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim x = tree.GetRoot().DescendantNodes().OfType(Of ParameterSyntax)().Single().Identifier

            Dim model = compilation.GetSemanticModel(tree)
            Dim xSym = model.GetDeclaredSymbol(x)

            Assert.Equal("x As System.Int32", xSym.ToTestDisplayString())
            Assert.False(xSym.IsImplicitlyDeclared)
            Assert.Equal("x As Integer", xSym.DeclaringSyntaxReferences.Single().GetSyntax().ToString())
            Assert.True(xSym.ContainingSymbol.IsImplicitlyDeclared)
            Assert.Same(e1.Type, xSym.ContainingType)
            Assert.Same(xSym, xSym.ContainingType.DelegateInvokeMethod.Parameters.First())
        End Sub

        <WorkItem(14364, "https://github.com/dotnet/roslyn/issues/14364")>
        <Fact()>
        Public Sub SemanticModelOnParameters_04()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Interface I1
End Interface

Class A
    Implements I1
    Public Event E1(x As Integer) Implements I1.E1

    class E1EventHandler
    End Class
End Class]]></file>
</compilation>, options:=TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(
<expected>
BC30401: 'E1' cannot implement 'E1' because there is no matching event on interface 'I1'.
    Public Event E1(x As Integer) Implements I1.E1
                                             ~~~~~
</expected>)

            Dim e1EventHandler = compilation.GetTypeByMetadataName("A+E1EventHandler")
            Assert.False(e1EventHandler.IsImplicitlyDeclared)
            Assert.Equal("A.E1EventHandler", e1EventHandler.ToTestDisplayString())
            Assert.Null(e1EventHandler.AssociatedSymbol)

            Dim a = compilation.GetTypeByMetadataName("A")
            Dim e1 = a.GetMember(Of EventSymbol)("E1")
            Assert.Equal("Event A.E1(x As System.Int32)", e1.ToTestDisplayString())

            Assert.Equal("A.E1EventHandler", e1.Type.ToTestDisplayString())
            Assert.True(e1.Type.IsDelegateType())
            Assert.DoesNotContain(e1.Type, a.GetMembers())
            Assert.Same(a, e1.Type.ContainingType)
            Assert.True(e1.Type.IsImplicitlyDeclared)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim x = tree.GetRoot().DescendantNodes().OfType(Of ParameterSyntax)().Single().Identifier

            Dim model = compilation.GetSemanticModel(tree)
            Dim xSym = model.GetDeclaredSymbol(x)

            Assert.Equal("x As System.Int32", xSym.ToTestDisplayString())
            Assert.False(xSym.IsImplicitlyDeclared)
            Assert.Equal("x As Integer", xSym.DeclaringSyntaxReferences.Single().GetSyntax().ToString())
            Assert.True(xSym.ContainingSymbol.IsImplicitlyDeclared)
            Assert.Same(e1.Type, xSym.ContainingType)
            Assert.Same(xSym, xSym.ContainingType.DelegateInvokeMethod.Parameters.First())
        End Sub

        <Fact()>
        Public Sub CompilerLoweringPreserveAttribute_01()
            Dim source1 = "
Imports System
Imports System.Runtime.CompilerServices

<CompilerLoweringPreserve>
<AttributeUsage(AttributeTargets.Field Or AttributeTargets.Event)>
Public Class Preserve1Attribute
    Inherits Attribute
End Class

<CompilerLoweringPreserve>
<AttributeUsage(AttributeTargets.Event)>
Public Class Preserve2Attribute
    Inherits Attribute
End Class

<AttributeUsage(AttributeTargets.Field Or AttributeTargets.Event)>
Public Class Preserve3Attribute
    Inherits Attribute
End Class
"
            Dim source2 = "
Class Test1
    <Preserve1>
    <Preserve2>
    <Preserve3>
    Event E1 As System.Action
End Class
"

            Dim validate = Sub(m As ModuleSymbol)
                               AssertEx.SequenceEqual(
                                   {
                                       "Preserve1Attribute",
                                       "System.Runtime.CompilerServices.CompilerGeneratedAttribute",
                                       "System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)"
                                   },
                                   m.GlobalNamespace.GetMember("Test1.E1Event").GetAttributes().Select(Function(a) a.ToString()))
                           End Sub

            Dim comp1 = CreateCompilation(
                {source1, source2, CompilerLoweringPreserveAttributeDefinition},
                options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            CompileAndVerify(comp1, symbolValidator:=validate).VerifyDiagnostics()

            Dim comp2 = CreateCompilation([source2], references:={comp1.ToMetadataReference()}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            CompileAndVerify(comp2, symbolValidator:=validate).VerifyDiagnostics()

            Dim comp3 = CreateCompilation(source2, references:={comp1.EmitToImageReference()}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            CompileAndVerify(comp3, symbolValidator:=validate).VerifyDiagnostics()
        End Sub

    End Class
End Namespace
