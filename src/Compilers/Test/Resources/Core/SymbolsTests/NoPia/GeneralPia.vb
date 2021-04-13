' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel
Imports System.ComponentModel.Composition
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

<Assembly: ImportedFromTypeLib("GeneralPIA.dll")> 
<Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>

Namespace SomeNamespace
    Public Structure NoAttributesStructure
        Public Goo1 As Integer
        Public Goo2 As Integer
    End Structure
End Namespace

Public Enum NoAttributesEnum
    Goo1
    Goo2
End Enum

Public Delegate Sub NoAttributesDelegate(ByVal x As String, ByVal y As Object)

<Guid("ee3c2bee-9dfb-4d1c-91de-4cd32ff13302")> _
Public Enum GooEnum
    Goo1
    __
    Goo2
    列挙識別子
    [Enum]
    COM
End Enum

<Guid("63370d76-3395-4560-92fd-b69ccfdaf461")> _
Public Structure GooStruct
    Public [Structure] As Integer

    Public NET As Decimal

    Public 構造メンバー As String
    
    Public Goo3 As Object()

    Public Goo4 As Double()
End Structure

'Public Structure GooPrivateStruct
'    Public Goo1 As Integer
'    Private Goo2 As Long
'End Structure
    
'Public Structure GooSharedStruct
'    Public Shared Field1 As Integer = 4
'End Structure

Public Structure GooConstStruct
    Public Const Field1 As String = "2"
End Structure

<Guid("bd62ff24-a97c-4a9c-a43e-09a31ff8b312")> _
<ComImport()> _
<InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _
Public Interface ISubFuncProp
    Sub Goo(ByVal p As Integer)

    Function Bar() As String

    Property Prop() As <MarshalAs(UnmanagedType.Currency)> Decimal
End Interface

<Guid("6a97be13-2611-4fc0-9d78-926dccec3243")> _
<ComImport()> _
<InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _
Public Interface IDuplicates
    Function DuplicateA(ByVal x As String) As Object
    Function DuplicateB(ByVal y As String) As Object

    Property PropDupeA() As ISubFuncProp
    Property PropDupeB() As ISubFuncProp
End Interface

Namespace Parameters
    <Guid("ed4bf792-06dd-449a-91d7-dde226e9f471")> _
    <ComImport()> _
    Public Interface IA
    End Interface

    <Guid("3b3fce76-b864-4a7c-93f6-87ec17339299")> _
    <ComImport()> _
    Public Interface IB
    End Interface

    <Guid("a6fa1a95-b29f-4f59-b457-29f8a55b3b6e")> _
    <ComImport()> _
    Public Interface IC
    End Interface

    <Guid("906b08a3-e478-4c52-ae30-ecb484ac01f0")> _
    <ComImport()> _
    Public Interface ID
    End Interface

    <Guid("bedd42c1-d023-4f53-8bfd-7c0b9de3aac7")> _
    <ComImport()> _
    Public Interface IByRef
        Function Goo(ByRef p1 As IA, ByRef p2 As IB, ByRef p3 As IC, ByRef p4 As Integer, ByRef p5 As Object) As ID
        Sub Bar(Optional ByRef p1 As IA = Nothing, Optional ByRef p2 As String() = Nothing, Optional ByRef p3 As Double() = Nothing)
    End Interface

    <Guid("1313c178-13f6-4450-8360-cf50d751a0f4")> _
    <ComImport()> _
    Public Interface IOptionalParam
        Function Goo(Optional ByVal p1 As IA = Nothing, Optional ByVal p2 As IB = Nothing, Optional ByVal p3 As IC = Nothing, Optional ByVal p4 As Integer = 5, Optional ByVal p5 As Object = Nothing) As ID
        Sub Bar(ByVal p1 As IA, ByVal p2 As String(), ByVal p3 As Double())
    End Interface
End Namespace

Namespace GeneralEventScenario

    <Guid("00000502-0000-6600-c000-000000000046")> _
    <ComImport()> _
    Public Interface IEventArgs2 : Inherits IEnumerable
        Property GetData() As Parameters.IA
    End Interface

    Public Delegate Sub EventHandler(ByVal o As Object, ByVal e As EventArgs)
    Public Delegate Sub EventHandler2(ByVal o As Object, ByVal e As IEventArgs2)

    ' Calling interface 
    <Guid("00000002-0000-0000-c000-000000000046")> _
    <ComImport()> _
    <InterfaceType(ComInterfaceType.InterfaceIsIDispatch)> _
    Public Interface _Arc
        <PreserveSig()> _
        Function SomeUnusedMethod() As Object

        <DispId(1558)> _
        <LCIDConversion(10)> _
        <PreserveSig()> _
        Function Cut(<MarshalAs(UnmanagedType.Bool), [In](), Out()> _
            ByVal x As Integer) As <MarshalAs(UnmanagedType.Bool)> Object

        <PreserveSig()> _
        Function SomeOtherUnusedMethod() As Object
        <PreserveSig()> _
        Function SomeLastMethod() As Object
    End Interface

    ' Class interface 
    <Guid("00000002-0000-0000-c000-000000000046")> _
    <ComImport()> _
    Public Interface Arc
        Inherits _Arc
        Inherits ArcEvent_Event
        Inherits ArcEvent_Event2
    End Interface


    ' Source interface 
    <Guid("00000002-0000-0000-c000-000000000047")> _
    <ComImport()> _
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
    Public Interface ArcEvent
        Function One() As Object

        <DispId(1558)> _
        Sub click()
        
        Sub move()
        Sub groove()
        Sub shake()
        Sub rattle()
    End Interface

    ' Events interface 
    <ComEventInterface(GetType(ArcEvent), GetType(Integer))> _
    Public Interface ArcEvent_Event
        Event click As EventHandler
    End Interface

    ' A few more events
    <ComEventInterface(GetType(ArcEvent), GetType(Integer))> _
    Public Interface ArcEvent_Event2
        Event move As EventHandler2
        Event groove As EventHandler
        Event shake As EventHandler
        Event rattle As EventHandler
    End Interface

End Namespace

Namespace Inheritance
    <Guid("27e3e649-994b-4f58-b3c6-f8089a5f2c6c")> _
    <ComImport()> _
    <InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _
    Public Interface IBase
        Sub IBaseSub(ByVal x As Integer)
        Function IBaseFunc() As Object

        Property IBaseProp() As String
    End Interface

    <Guid("bd60d4b3-f50b-478b-8ef2-e777df99d810")> _
    <ComImport()> _
    <InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _
    <CoClass(GetType(DerivedImpl))> _
    Public Interface IDerived
        Inherits IBase

        Shadows Sub IBaseSub(ByVal x As Integer)
        Shadows Function IBaseFunc() As Object

        Shadows Property IBaseProp() As String

        Sub IDerivedSub(ByVal x As Integer)
        Function IDerivedFunc() As Object

        Property IDerivedProp() As String
    End Interface

    <Guid("c9dcf748-b634-4504-a7ce-348cf7c61891")> _
    Public Class DerivedImpl

    End Class
End Namespace

Namespace InheritanceConflict

    <Guid("76a62998-7740-4ebd-a09f-e401ffff5c8c")> _
    <ComImport()> _
    Public Interface IBase
        Sub Goo()
        Function Bar() As Integer
    
        Sub ConflictMethod(ByVal x As Integer)
        
        Default Property IndexedProperty(x As Object) As String
    End Interface
    
    <Guid("d6c92e52-47f4-4075-8078-99a21c29c8fa")> _
    <ComImport()> _
    <CoClass(GetType(DerivedImpl))> _
    Public Interface IDerived
        Inherits IBase
        ' IBase methods
        Shadows Sub Goo()
        Shadows Function Bar() As Integer
            
        Shadows Sub ConflictMethod(ByVal x As Integer)
            
        Shadows Property IndexedProperty(x As Object) As String
        
        ' New methods    
        Shadows Function ConflictMethod(x As Integer, y As String) As Object
        
        Shadows Default Property IndexedPropertyDerived(x As Decimal) As Integer
    End Interface

    <Guid("7e12bd3c-1ad1-427f-bab8-af82e30d980d")> _
    Public Class DerivedImpl

    End Class
End Namespace

Namespace LateBound
    <Guid("43f3a25e-fccb-4b92-adc1-2fe84c922125")> _
    <ComImport()> _
    Public Interface INoPIAInterface
        Function Goo(ByVal x As INoPIAInterface) As String
        Function Bar(ByVal x As Integer, ByVal y As Integer) As String
        Function Moo() As Integer
    End Interface

    <Guid("ae390f6a-e7e4-47d7-a7b2-d95dfe14aac6")> _
    <ComImport()> _
    Public Interface INoPIAInterface2
        Property Blah() As INoPIAInterface
        Sub Fazz(ByVal x As INoPIAInterface)
    End Interface
End Namespace

Namespace NoPIACopyAttributes
    Class SomeOtherAttribute
        Inherits Attribute
    End Class

    ' from the below attributes, AutomationProxy and CLSCompliant are not preserved

    <AutomationProxy(True)> _
    <Guid("16063cdc-9759-461c-9ad7-56376771a8fb")> _
    <ComImport()> _
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
    <CoClass(GetType(HasAllSupportedAttributesCoClass))> _
    <BestFitMapping(True, ThrowOnUnmappableChar:=True)> _
    <TypeLibImportClass(GetType(HasAllSupportedAttributesCoClass))> _
    <CLSCompliant(True)> _
    <TypeLibType(TypeLibTypeFlags.FDual)> _
    Public Interface IHasAllSupportedAttributes
        Inherits IHasAllSupportedAttributes_Event

        ' return values have all supported attributes as well
        WriteOnly Property WOProp() As <MarshalAs(UnmanagedType.LPStr, SizeConst:=5)> String
        ReadOnly Property ROProp() As <MarshalAs(UnmanagedType.LPArray, arraysubtype:=UnmanagedType.Currency, SizeConst:=2)> Decimal()

        <TypeLibFunc(TypeLibFuncFlags.FBindable)>
        <LCIDConversion(10)>
        Function PropAndRetHasAllSupportedAttributes(<MarshalAs(UnmanagedType.LPArray, arraysubtype:=UnmanagedType.Error)> <[Optional]()> <Out()> <[In]()> <DefaultParameterValue(10)> <[ParamArray]()> ByVal x As Integer()) As String

        <SpecialName()> _
        <PreserveSig()> _
        Function Scenario11(ByRef str As String) As <MarshalAs(UnmanagedType.Bool)> Integer

        <SomeOtherAttribute()> _
        Function Scenario12(<CLSCompliant(False), ComAliasName("aoeu")> ByVal p1 As Integer, <CLSCompliant(True), MarshalAs(UnmanagedType.IUnknown)> ByVal p2 As Object, <SomeOtherAttribute()> ByVal p3 As String) As <CLSCompliant(False)> Char()
        Function Scenario13(<CLSCompliant(False), ComAliasName("aoeu")> ByVal p1 As Integer, <CLSCompliant(True)> ByVal p2 As Object, <SomeOtherAttribute()> ByVal p3 As String) As <CLSCompliant(False), MarshalAs(UnmanagedType.Error)> Integer

    End Interface

    <UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet:=CharSet.Auto), CLSCompliant(True), SomeOtherAttribute()>
    Public Delegate Sub Goo()

    <Guid("7b072a0e-065c-4266-89f4-bdb0de8df3b5")> _
    <ComImport()> _
    Public Interface IHasAllSupportedAttributesEvent

        Sub Scenario14()

    End Interface

    <ComEventInterface(GetType(IHasAllSupportedAttributesEvent), GetType(Integer)), CLSCompliant(True)> _
    Public Interface IHasAllSupportedAttributes_Event

        Event Scenario14 As Goo

    End Interface

    <Guid("ef307d1d-343d-44ef-a2f0-8235e214295e")> _
    Public Class HasAllSupportedAttributesCoClass

    End Class

    <Guid("7b072a0e-065c-4266-89f4-bdb0de8df2b5")> _
    <ComImport()> _
    <AutomationProxy(True)> _
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
    <SomeOtherAttribute()> _
    Public Interface IHasUnsupportedAttributes
        Function FuncHasUnsupportedAttributes() As <CLSCompliant(True)> Integer
        Sub Bar()
    End Interface

    ' from the below attributes, COMVisible and TypeLibType attributes are not preserved

    <ComVisible(True)> _
    <Guid("6f4eb02b-3469-424c-bbcc-2672f653e646")> _
    <BestFitMapping(False)> _
    <StructLayout(LayoutKind.Auto)> _
    <TypeLibType(TypeLibTypeFlags.FRestricted)> _
    <Flags()> _
    Public Enum EnumHasAllSupportedAttributes
        ID1
        ID2
    End Enum

    <CLSCompliant(True)> _
    <TypeLibType(TypeLibTypeFlags.FRestricted)> _
    <Guid("25f37bfe-5a80-42b8-9e35-af88676d7178")> _
    <SomeOtherAttribute()> _
    Public Enum EnumHasOnlyUnsupportedAttributes
        ID1
        ID2
    End Enum

    <ComVisible(True)> _
    <Guid("6f4eb02b-3469-424c-bbcc-2672f653e646")> _
    <BestFitMapping(False)> _
    <StructLayout(LayoutKind.Explicit, CharSet:=CharSet.Ansi, Pack:=4, Size:=2)> _
    <TypeLibType(TypeLibTypeFlags.FRestricted)> _
    <SomeOtherAttribute()> _
    Public Structure StructHasAllSupportedAttributes
        <FieldOffset(24)> _
        <MarshalAs(UnmanagedType.I4)> _
        <SomeOtherAttribute()> _
        Public a As Integer

        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=12)> _
        <DispId(2)> _
        <TypeLibVar(TypeLibVarFlags.FHidden)> _
        <CLSCompliant(False)> _
        <FieldOffset(0)> _
        Public FieldHasAllSupportedAttributes As Char()
    End Structure

    <CLSCompliant(True)> _
    <TypeLibType(TypeLibTypeFlags.FControl)> _
    <Guid("53de85e6-70eb-4b35-aff6-f34c0313637d")> _
    Public Structure StructHasOnlyUnsupportedAttributes
        ' leave the struct empty - see how it is handled by NoPIA
    End Structure

    ' Pseudo-custom attributes that weren't called out in the spec
    <Serializable()> _
    Public Structure OtherPseudoCustomAttributes
        '<NonSerialized()> _Verified we don't copy this guy (per spec)--not interesting to test, as it complicates verification logic
        Public Field1 As Date

        ' Scenarios no longer applicable, as compiler now generates error
        ' if structs contain anything except public instance fields
        ' Scenario is covered by Testcase #1422748
        '
        '<DllImport("blah", CharSet:=CharSet.Unicode)> _
        'Public Shared Sub Method1()
        'End Sub

        '<MethodImpl(MethodImplOptions.NoInlining Or MethodImplOptions.NoOptimization)> _
        'Public Sub Method2()
        '    Field1 = #1/1/1979#
        'End Sub
    End Structure

End Namespace

Namespace [Overloads]
    <ComImport()> _
    <Guid("f618f410-0331-487d-ade1-bd46289d9fe1")> _
    Public Interface IBase
        Sub Goo()
        Function Bar() As Integer
    End Interface

    <ComImport()> _
    <Guid("984c80ba-2aec-4abf-afdd-c2bce69daa83")> _
    Public Interface IDerived
        Inherits IBase
        Shadows Sub Goo()
        Shadows Function Bar() As Integer

        Shadows Function Goo(ByVal x As Integer) As <MarshalAs(UnmanagedType.LPStr, SizeConst:=10)> String
    End Interface

    <ComImport()> _
    <Guid("a314887a-b963-4c6c-98a0-ff42d942cc9b")> _
    Public Interface IInSameClass
        Overloads Function Over(ByVal x As Integer, <MarshalAs(UnmanagedType.IUnknown)> ByVal y As Object) As Integer

        Overloads Function Over(ByVal x As Integer) As Boolean
    End Interface
End Namespace

Namespace VTableGap
    <Guid("4cfdb6c3-ff27-4fc2-b477-07b914286230")> _
    <ComImport()> _
    Public Interface IScen1
        Sub g1()
        ReadOnly Property g2() As <MarshalAs(UnmanagedType.Bool)> Integer
    End Interface

    <Guid("6c5d196f-916c-433a-b62b-0bfc2f97675d")> _
    <ComImport()> _
    Public Interface IScen2
        Sub g000() ' use this one
        Sub g001()
        Sub g002()
        Sub g003()
        Sub g004()
        Sub g005()
        Sub g006()
        Sub g007()
        Sub g008()
        Sub g009()
        Sub g010()
        Sub g011()
        Sub g012()
        Sub g013()
        Sub g014()
        Sub g015()
        Sub g016()
        Sub g017()
        Sub g018()
        Sub g019()
        Sub g020()
        Sub g021()
        Sub g022()
        Sub g023()
        Sub g024()
        Sub g025()
        Sub g026()
        Sub g027()
        Sub g028()
        Sub g029()
        Sub g030()
        Sub g031()
        Sub g032()
        Sub g033()
        Sub g034()
        Sub g035()
        Sub g036()
        Sub g037()
        Sub g038()
        Sub g039()
        Sub g040()
        Sub g041()
        Sub g042()
        Sub g043()
        Sub g044()
        Sub g045()
        Sub g046()
        Sub g047()
        Sub g048()
        Sub g049()
        Sub g050()
        Sub g051()
        Sub g052()
        Sub g053()
        Sub g054()
        Sub g055()
        Sub g056()
        Sub g057()
        Sub g058()
        Sub g059()
        Sub g060()
        Sub g061()
        Sub g062()
        Sub g063()
        Sub g064()
        Sub g065()
        Sub g066()
        Sub g067()
        Sub g068()
        Sub g069()
        Sub g070()
        Sub g071()
        Sub g072()
        Sub g073()
        Sub g074()
        Sub g075()
        Sub g076()
        Sub g077()
        Sub g078()
        Sub g079()
        Sub g080()
        Sub g081()
        Sub g082()
        Sub g083()
        Sub g084()
        Sub g085()
        Sub g086()
        Sub g087()
        Sub g088()
        Sub g089()
        Sub g090()
        Sub g091()
        Sub g092()
        Sub g093()
        Sub g094()
        Sub g095()
        Sub g096()
        Sub g097()
        Sub g098()
        Sub g099()
        Sub g100()
        Sub g101() ' use this one
    End Interface

    <Guid("d0f4f3fc-6599-4c8f-b97b-8d0ba9a4a3c8")> _
    <ComImport()> _
    Public Interface IScen3
        Function M(<MarshalAs(UnmanagedType.LPArray)> <[In]()> ByVal y As Double()) As <MarshalAs(UnmanagedType.IUnknown)> Object
        Function g1(ByVal y As Integer) As <MarshalAs(UnmanagedType.LPStr, SizeConst:=10)> String
        Function g2(<Out()> ByRef y As String) As Integer
    End Interface

    <Guid("8feb04fb-8bb6-4121-b883-78b261936ae7")> _
    <ComImport()> _
    Public Interface IScen4
        Property P1() As String
        Sub g1()
        Sub g2(<[In]()> <Out()> ByVal x As String)
        Sub g3(<Out()> ByVal y As Integer)
        Property P2() As <MarshalAs(UnmanagedType.FunctionPtr)> Func(Of Integer)
    End Interface

    <Guid("858df621-87bb-40a0-99b5-617f85d04c88")> _
    <ComImport()> _
    Public Interface IScen5
        Function M1(<MarshalAs(UnmanagedType.LPArray)> <[In]()> ByVal y As Double()) As <MarshalAs(UnmanagedType.IUnknown)> Object
        Property g1() As Decimal
        Property g2() As Integer
        Function M2(<MarshalAs(UnmanagedType.LPArray)> <[In]()> ParamArray ByVal y As Integer()) As String
    End Interface

    <Guid("11e95f23-fc7a-4a45-a3b4-b968f0e2cb2c")> _
    <ComImport()> _
    Public Interface IScen6
        Property P1() As String
        ReadOnly Property g1() As String
        WriteOnly Property g2() As Integer
        ReadOnly Property g3() As Decimal
        WriteOnly Property g4() As Boolean
        Property P2() As <MarshalAs(UnmanagedType.FunctionPtr)> Func(Of Integer)
    End Interface

    <Guid("2f7b5524-4f8d-4471-95e9-ce319babf8d0")> _
    <ComImport()> _
    Public Interface IScen7
        Property g1() As String
        Sub M(ByVal a As IScen1, ByVal b As IScen2, ByVal c As IScen3)
        Function g2() As Object
        Function g3(ByVal x As IScen6) As Integer
    End Interface

    <Guid("e08004c7-a558-4b02-b5f4-146c4b94aaa2")> _
    <ComImport()> _
    <CoClass(GetType(IComplicatedVTableImpl))> _
    Public Interface IComplicatedVTable
        Default Property Goo(<MarshalAs(UnmanagedType.Bool)> ByVal index As Integer) As <MarshalAs(UnmanagedType.LPStr)> String
        Sub M1(<Out()> ByRef x As Integer)
        Function M2() As IScen1
        Property P1() As IScen2
        WriteOnly Property P3() As IComplicatedVTable
        Sub M3()
        ReadOnly Property P4() As ISubFuncProp
        Function M4() As <MarshalAs(UnmanagedType.IUnknown)> Object
    End Interface
    
    <Guid("c9dcf748-b634-4504-a7ce-348cf7c61891")> _
    Public Class IComplicatedVTableImpl
    End Class
    
End Namespace
Namespace InheritsFromEnum
    <ComImport()> _
    <Guid("ed9e4072-9bf4-4f6c-994d-8e415bcc6fd2")> _
    <CoClass(GetType(CoClassIfe))> _
    Public Interface IIfe
        Inherits IEnumerable

        Function Concat(ByVal x As Object) As IIfe
    End Interface

    <ComImport()> _
    <Guid("97792ef5-1377-46c1-9815-59c5828d034f")> _
    <CoClass(GetType(CoClassIfeScen2))> _
    Public Interface IIfeScen2
        Inherits IEnumerable(Of IIfeScen2)

        Function Concat(ByVal x As Object) As IIfeScen2
    End Interface

    <Guid("f7cdfd32-d2e6-4f3f-92f0-bd2c9dcfa923")> _
    Public Class CoClassIfe

    End Class

    <Guid("8a8c22bf-d666-4462-bc8d-2faf1940e041")> _
    Public Class CoClassIfeScen2 : End Class
End Namespace

Namespace LackingAttributes
    Public Interface INoAttributesOrMembers
    
    End Interface
    
    <ComImport()> _
    Public Interface IComImportButNoGuidOrMembers
    
    End Interface
    
    Public Interface INoAttributes
        Sub Goo(ByVal p As Integer)
    
        Function Bar() As String
    
        Property Prop() As <MarshalAs(UnmanagedType.Currency)> Decimal
    End Interface
    
    <ComImport()> _
    Public Interface IComImportButNoGuid
        Sub Goo(ByVal p As Integer)
    
        Function Bar() As String
    
        Property Prop() As <MarshalAs(UnmanagedType.Currency)> Decimal
    End Interface
End Namespace
