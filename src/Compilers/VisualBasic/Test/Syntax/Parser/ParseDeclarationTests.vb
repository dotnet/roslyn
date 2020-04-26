' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

<CLSCompliant(False)>
Public Class ParseDeclarations
    Inherits BasicTestBase

    <WorkItem(865836, "DevDiv/Personal")>
    <WorkItem(927366, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseModuleDeclaration()
        ParseAndVerify(<![CDATA[
            Module Module1
            End Module
        ]]>).
        VerifyOccurrenceCount(SyntaxKind.EndOfFileToken, 1)

        ParseAndVerify(<![CDATA[
            Module Module1 : End Module
        ]]>).
        VerifyOccurrenceCount(SyntaxKind.EndOfFileToken, 1)

        ParseAndVerify(<![CDATA[Module Module1
            End Module]]>).
        VerifyOccurrenceCount(SyntaxKind.EndOfFileToken, 1)

    End Sub

    <Fact>
    Public Sub ParseNamespaceDeclaration()
        ParseAndVerify(<![CDATA[
             Namespace N1
             End Namespace
        ]]>)

        ParseAndVerify(<![CDATA[
            Namespace N1 : End Namespace
        ]]>)
    End Sub

    <Fact>
    Public Sub ParseNamespaceDeclarationWithGlobal()
        ParseAndVerify(<![CDATA[
             Namespace Global.N1
             End Namespace
        ]]>)
    End Sub

    <Fact>
    Public Sub ParseClassDeclaration()
        ParseAndVerify(<![CDATA[
            Class C1
            End Class
        ]]>)

        ParseAndVerify(<![CDATA[
            Class C1 : End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub ParseStructureDeclaration()
        ParseAndVerify(<![CDATA[
            Structure S1
            End Structure
        ]]>)

        ParseAndVerify(<![CDATA[
            Structure S1 : End Structure
        ]]>)
    End Sub

    <Fact>
    Public Sub ParseInterfaceDeclaration()
        ParseAndVerify(<![CDATA[
            Interface I1
            End Interface
        ]]>)

        ParseAndVerify(<![CDATA[
            Interface I1 :  End Interface
        ]]>)
    End Sub

    <Fact>
    Public Sub ParseClassInheritsDeclaration()
        ParseAndVerify(<![CDATA[
                Class C1
                End Class
                Class C2
                    Inherits C1
                End Class
            ]]>)
    End Sub

    <WorkItem(927384, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseClassInheritsStatementWithSeparator()
        ParseAndVerify(<![CDATA[
                Class C1
                End Class
                Class C2 : Inherits C1
                End Class
                Class C3 : 
                     Inherits C1
                End Class
                Class C4
                     : Inherits C1
                End Class
                Class C4 :::
                     :::: Inherits C1
                     :::: Implements QQQ
                End Class
                Interface I1 : End Interface
                Interface I2:
                   Inherits I1
                End Interface
                Interface I3
                   :Inherits I1
                End Interface
                Interface I3::
                   :Inherits I1 ::
                    :: Inherits I2 ::
                End Interface
                Structure S1
                       :Implements I1
                End Structure
                Structure S2:
                       Implements I1
                End Structure
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseClassImplementsDeclaration()
        ParseAndVerify(<![CDATA[
               Interface I1
                End Interface
                Class C2
                    Implements I1
                End Class
           ]]>)
    End Sub

    <Fact>
    Public Sub ParseGenericClassDeclaration()
        ParseAndVerify(<![CDATA[
                Class C1(of T1, T2)
                End Class
             ]]>)

        ParseAndVerify(<![CDATA[
                Class C1(
                            of 
                                T1, 
                                T2
                        )
                End Class
             ]]>)
    End Sub

    <Fact>
    Public Sub ParseGenericClassAsNewDeclaration()
        ParseAndVerify(<![CDATA[
                Class Base
                End Class
                Class C1(of T1 as new)
                End Class
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseGenericClassAsTypeDeclaration()
        ParseAndVerify(<![CDATA[
                Class Base
                End Class
                Class C1(of T1 as Base)
                End Class
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseGenericClassAsMultipleDeclaration()
        ParseAndVerify(<![CDATA[
                Class Base
                End Class
                Class C1(of T1 as {new, Base})
                End Class
            ]]>)

        ParseAndVerify(<![CDATA[
                Class Base  
                End Class
                Class C1(of 
                            T1 as {
                                    new, 
                                    Base
                                  })
                End Class
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseEnum()
        ParseAndVerify(<![CDATA[
                Module Module1
                    enum e1
                        member1
                        member2
                        member3 = 100
                        member4
                   end enum
                End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub Bug8037()
        ParseAndVerify(<![CDATA[
Enum Y
    ::A ::B
End Enum
            ]]>)

        ParseAndVerify(<![CDATA[
Enum Y::
    ::A::B:::
End Enum
            ]]>)

        ParseAndVerify(<![CDATA[
Enum Y
    A::B:::  ' A: is parsed as a label
End Enum
            ]]>,
                Diagnostic(ERRID.ERR_InvInsideEnum, "A:"))

        ParseAndVerify(<![CDATA[
Enum Y:::::
    A::B ' A: is parsed as a label
End Enum
            ]]>,
                Diagnostic(ERRID.ERR_InvInsideEnum, "A:"))
    End Sub

    <Fact>
    Public Sub Bug862490()
        ParseAndVerify(<![CDATA[
                Interface I1 
                End Interface
                Interface I2
                    Inherits I1
                End Interface
            ]]>)
    End Sub

    <Fact>
    Public Sub Bug863541()
        ParseAndVerify(<![CDATA[
                Class Class1(Of Type)
                End Class
            ]]>)
    End Sub

    <Fact>
    Public Sub Bug866559()
        ParseAndVerify(<![CDATA[
                Interface IVariance1(Of Out)
                End Interface
            ]]>)
    End Sub

    <Fact>
    Public Sub Bug866616()
        ParseAndVerify(<![CDATA[
            Module Module1
            Dim x = from i in ""


            End Module
        ]]>).
        VerifyOccurrenceCount(SyntaxKind.EmptyStatement, 0)
    End Sub

    <Fact>
    Public Sub Bug867063()
        ParseAndVerify(<![CDATA[
                Interface IVariance(Of In T, Out R) : : End Interface
            ]]>)
    End Sub

    <Fact>
    Public Sub Bug868402()
        'Tree does not round-trip for generic type parameter list with newlines
        ParseAndVerify(<![CDATA[
                Interface IVariance1(Of Out
                )
                End Interface
            ]]>)
    End Sub

    <WorkItem(873467, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseInterfaceBasesDeclaration()
        ParseAndVerify(<![CDATA[
                Interface I1 : End Interface
                Interface I2 : End Interface
                Interface I3
                    Inherits I1, I2
                End Interface
            ]]>)

        ParseAndVerify(<![CDATA[
                Interface I1 : End Interface
                Interface I2 : End Interface
                Class C1
                    Implements I1, I2
                End Class
            ]]>)

        ParseAndVerify(<![CDATA[
                Interface I1 : End Interface
                Interface I2 : End Interface
                Structure S1
                    Implements I1, I2
                End Structure
            ]]>)
    End Sub

    <WorkItem(882976, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLambdaInFieldInitializer()
        ParseAndVerify(<![CDATA[
                Class Class1
                    Dim x = Sub() Console.WriteLine()
                    Dim y As Integer = 3
                End Class
            ]]>)
    End Sub

    <WorkItem(869140, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseInterfaceSingleLine()
        ParseAndVerify(<![CDATA[
            Interface IVariance(Of Out T) : Function Goo() As T : End Interface
        ]]>)
    End Sub

    <WorkItem(889005, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseErrorsInvalidEndFunctionAndExecutableAsDeclaration()
        ParseAndVerify(<![CDATA[
                     Class Class1
                        Dim x32 As Object = Sub() Call Function()
                                     Return New Exception
                                     Return New Exception
                                   End Function
                        Dim x43 As Object = CType(Function()
                                  Return Nothing
                               End Function, Action(Of Long))
                     End Class
            ]]>)
    End Sub

    <WorkItem(917197, "DevDiv/Personal")>
    <Fact>
    Public Sub TraverseEmptyBlocks()
        ParseAndVerify(
            "Module M1" & vbCrLf &
            "Sub Goo" & vbCrLf &
            "Try" & vbCrLf &
            "Catch" & vbCrLf &
            "Finally" & vbCrLf &
            "End Try" & vbCrLf &
            "End Sub" & vbCrLf &
            "End Module"
        ).
        TraverseAllNodes()
    End Sub

    <WorkItem(527076, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527076")>
    <Fact>
    Public Sub ParseMustOverrideInsideModule()
        ParseAndVerify(<![CDATA[
Module M1
Mustoverride Sub Goo()
End Sub
End Module
        ]]>, <errors>
                 <error id="30429" message="'End Sub' must be preceded by a matching 'Sub'." start="34" end="41"/>
             </errors>)
    End Sub

    <Fact>
    Public Sub ParseVariousOrderingOfDeclModifiers()
        ParseAndVerify(<![CDATA[
Public MustInherit Class A
    MustInherit Private Class B
        Public MustOverride Function Func() As Integer
        MustOverride Protected Property Prop As String
    End Class
End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub ParseIncompleteMemberBecauseOfAttributeAndOrModifiers()
        ParseAndVerify(<![CDATA[
Public Class C1
    <Obsolete1()> 
    goo

    <Obsolete2()> 
    if true then :

    Public ' 1

    <Obsolete3()> 
    Public ' 2

    <Obsolete4()>

    Public Shared with

    Public Shared with if SyncLock

    Public Shared Sub Main() 
    End Sub
End Class
        ]]>, <errors>
                 <error id="32035"/>
                 <error id="32035"/>
                 <error id="30203"/>
                 <error id="30203"/>
                 <error id="32035"/>
                 <error id="30183"/>
                 <error id="30183"/>
             </errors>)
    End Sub

    <WorkItem(543607, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543607")>
    <Fact()>
    Public Sub ParseInheritsAtInvalidLocation()
        ParseAndVerify(<![CDATA[
Class Scen24
    Dim i = Sub(a as Integer, b as Long)
Inherits Scen23
    End Sub
End Class
        ]]>, <errors>
                 <error id="30024"/>
             </errors>)
    End Sub

#Region "Parser Error Tests"

    <WorkItem(866455, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30001ERR_NoParseError()
        ParseAndVerify(<![CDATA[
                Property Goo As Integer
             
                Namespace Namespace1
                End Namespace
            ]]>)
    End Sub

    <WorkItem(863086, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30025ERR_EndProp()
        ParseAndVerify(<![CDATA[
                Structure Struct1
                    Default Public Property Goo(ByVal x) As Integer
                    End Function
                End Structure
            ]]>,
             <errors>
                 <error id="30430"/>
                 <error id="30634"/>
                 <error id="30025"/>
             </errors>)
    End Sub

    <WorkItem(527022, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527022")>
    <Fact()>
    Public Sub BC30037ERR_IllegalChar_TypeParamMissingAsCommaOrRParen()
        ParseAndVerify(<![CDATA[
                     Class Class1(of ])
                     End Class
            ]]>,
            Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
            Diagnostic(ERRID.ERR_TypeParamMissingAsCommaOrRParen, ""),
            Diagnostic(ERRID.ERR_IllegalChar, "]"))
    End Sub

    <WorkItem(888046, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30184ERR_InvalidEndEnum()
        ParseAndVerify(<![CDATA[
                        Enum myenum
                         x
                         Shared Narrowing Operator CType(ByVal x As Integer) As c2
                            Return New c2
                         End Operator
                        End Enum
            ]]>,
                Diagnostic(ERRID.ERR_MissingEndEnum, "Enum myenum"),
                Diagnostic(ERRID.ERR_InvInsideEndsEnum, "Shared Narrowing Operator CType(ByVal x As Integer) As c2"),
                Diagnostic(ERRID.ERR_InvalidEndEnum, "End Enum"))
    End Sub

    <WorkItem(869144, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30185ERR_MissingEndEnum()
        ' Tree loses text when access modifier used on Enum member
        ParseAndVerify(<![CDATA[
                Enum Access1
                    Orange
                    Public Red = 2
                End Enum
            ]]>,
            <errors>
                <error id="30185"/>
                <error id="30619"/>
                <error id="30184"/>
            </errors>)
    End Sub

    <WorkItem(863528, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30188ERR_ExpectedDeclaration()
        ParseAndVerify(<![CDATA[
             VERSION 1.0 CLASS
                BEGIN
                    MultiUse = -1 'True
                    Persistable = 0 'NotPersistable
                    DataBindingBehavior = 0 'vbNone
                    DataSourceBehavior = 0 'vbNone
                    MTSTransactionMode = 0 'NotAnMTSObject
                END
                Attribute VB_Name = "Class1"
                Attribute VB_GlobalNameSpace = False
                Attribute VB_Creatable = True
                Attribute VB_PredeclaredId = False
                Attribute VB_Exposed = True
                Public v As Variant
            ]]>,
                Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "VERSION 1.0 CLASS"),
                Diagnostic(ERRID.ERR_ObsoleteArgumentsNeedParens, "1.0 CLASS"),
                Diagnostic(ERRID.ERR_ArgumentSyntax, "CLASS"),
                Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "BEGIN"),
                Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "MultiUse = -1"),
                Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Persistable = 0"),
                Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "DataBindingBehavior = 0"),
                Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "DataSourceBehavior = 0"),
                Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "MTSTransactionMode = 0"),
                Diagnostic(ERRID.ERR_UnrecognizedEnd, "END"),
                Diagnostic(ERRID.ERR_ExpectedSpecifier, "VB_Name = ""Class1"""),
                Diagnostic(ERRID.ERR_ExpectedDeclaration, "Attribute"),
                Diagnostic(ERRID.ERR_ExpectedSpecifier, "VB_GlobalNameSpace = False"),
                Diagnostic(ERRID.ERR_ExpectedDeclaration, "Attribute"),
                Diagnostic(ERRID.ERR_ExpectedSpecifier, "VB_Creatable = True"),
                Diagnostic(ERRID.ERR_ExpectedDeclaration, "Attribute"),
                Diagnostic(ERRID.ERR_ExpectedSpecifier, "VB_PredeclaredId = False"),
                Diagnostic(ERRID.ERR_ExpectedDeclaration, "Attribute"),
                Diagnostic(ERRID.ERR_ExpectedSpecifier, "VB_Exposed = True"),
                Diagnostic(ERRID.ERR_ExpectedDeclaration, "Attribute"),
                Diagnostic(ERRID.ERR_ObsoleteObjectNotVariant, "Variant"))
    End Sub

    <Fact>
    Public Sub BC30193ERR_SpecifiersInvalidOnInheritsImplOpt()
        ParseAndVerify(<![CDATA[
Class C
    <A> Inherits B
End Class
]]>,
            <errors>
                <error id="30193"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    <A> Implements I
End Class
]]>,
            <errors>
                <error id="30193"/>
            </errors>)
    End Sub

    <WorkItem(863112, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30193ERR_SpecifiersInvalidOnInheritsImplOpt_2()
        ParseAndVerify(<![CDATA[
            <System.Runtime.CompilerServices.Extension()> Namespace ExtensionAttributeNamespace01
            End Namespace
        ]]>,
        <errors>
            <error id="30193"/>
        </errors>)
    End Sub

    <WorkItem(887502, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30193_MismatchEndNoNamespace()
        ParseAndVerify(<![CDATA[
                         <System.Runtime.CompilerServices.CompilationRelaxations(Runtime.CompilerServices.CompilationRelaxations.NoStringInterning)> _
                         Namespace CompilerRelaxations02
                         End Namespace
            ]]>,
            <errors>
                <error id="30193"/>
            </errors>)
    End Sub

    <Fact>
    Public Sub BC30193_ParseInterfaceInherits()
        ParseAndVerify(<![CDATA[
                interface i
                public inherits i2
                end interface
            ]]>,
            <errors>
                <error id="30193"/>
            </errors>)
    End Sub

    <WorkItem(889075, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30201ERR_ExpectedExpression_InvInsideEndsEnumAndMissingEndEnum()
        ParseAndVerify(<![CDATA[
                     Module Module1
                      Enum byteenum As Byte
                        a = 200
                        b = 
                      End Enum
                     Enum sbyteenum As SByte
                        c
                        d
                     End Enum
                     End Module
            ]]>,
            <errors>
                <error id="30201"/>
            </errors>)
    End Sub

    <WorkItem(889301, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30203ERR_ExpectedIdentifier_ParasExtraError30213()
        ' Expected error 30203: Identifier expected.
        ' Was also reporting 30213.
        ParseAndVerify(<![CDATA[
            Friend Class cTest
               Sub Sub1 (0 To 10)
               End Sub
            End Class
        ]]>,
        <errors>
            <error id="30203"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub BC30205ERR_ExpectedEOS_NamespaceDeclarationWithGeneric()
        ParseAndVerify(<![CDATA[
             Namespace N1.N2(of T)
             End Namespace
        ]]>,
        <errors>
            <error id="30205"/>
        </errors>)

    End Sub

    <WorkItem(894067, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30213ERR_InvalidParameterSyntax_DollarAutoProp()
        ParseAndVerify(<![CDATA[
Property Scen4(
p1 as vb$anonymous1
) a
]]>,
            Diagnostic(ERRID.ERR_InvalidParameterSyntax, "anonymous1"),
            Diagnostic(ERRID.ERR_AutoPropertyCantHaveParams, <![CDATA[(
p1 as vb$anonymous1
)]]>))
    End Sub

    <Fact>
    Public Sub BC30363ERR_NewInInterface_InterfaceWithSubNew()
        ParseAndVerify(<![CDATA[
        Interface i
            Sub new ()

        End Interface
        ]]>,
        <errors>
            <error id="30363"/>
        </errors>)
    End Sub

    <WorkItem(887748, "DevDiv/Personal")>
    <WorkItem(889062, "DevDiv/Personal")>
    <WorkItem(538919, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538919")>
    <Fact>
    Public Sub BC30602ERR_InterfaceMemberSyntax_TypeStatement()
        ParseAndVerify(<![CDATA[
            interface i1
                dim i as integer
            End interface

            Interface Scen1
               type dd
            End Interface

            Interface il
               _loc as object
            End Interface

]]>,
    <errors>
        <error id="30188"/>
        <error id="30602"/>
        <error id="30802"/>
    </errors>)
    End Sub

    <WorkItem(887508, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30603ERR_InvInsideInterface_ParseInterfaceWithSubWithEndSub()
        ParseAndVerify(<![CDATA[
            Interface i1
              Sub goo()
              end sub
            End Interface
]]>,
        <errors>
            <error id="30603"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub BC30618ERR_NamespaceNotAtNamespace_ModuleNamespaceClassDeclaration()
        ParseAndVerify(<![CDATA[
            Namespace N1
                Module M1
                    Class A
                    End Class
               End Module
            End Namespace
        ]]>)

        ParseAndVerify(<![CDATA[
            Module Module1
                Namespace N1
                    Class A
                    End Class
                End Namespace
            End Module
        ]]>,
        <errors>
            <error id=<%= CInt(ERRID.ERR_ExpectedEndModule) %>/>
            <error id=<%= CInt(ERRID.ERR_NamespaceNotAtNamespace) %>/>
            <error id=<%= CInt(ERRID.ERR_EndModuleNoModule) %>/>
        </errors>)
    End Sub

    <WorkItem(862508, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30624ERR_ExpectedEndStructure()
        ParseAndVerify(<![CDATA[
                Structure Struct1
            ]]>,
            <errors>
                <error id="30624"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30625ERR_ExpectedEndModule()
        ParseAndVerify(<![CDATA[
                Module M1
            ]]>,
            <errors>
                <error id="30625"/>
            </errors>)
    End Sub

    <Fact>
    Public Sub BC30626ERR_ExpectedEndNamespace_ModuleNamespaceClassMissingEnd1()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Namespace N1
                        Class A
                End Module
            ]]>,
            <errors>
                <error id=<%= CInt(ERRID.ERR_ExpectedEndModule) %>/>
                <error id=<%= CInt(ERRID.ERR_NamespaceNotAtNamespace) %>/>
                <error id=<%= CInt(ERRID.ERR_ExpectedEndNamespace) %>/>
                <error id=<%= CInt(ERRID.ERR_ExpectedEndClass) %>/>
                <error id=<%= CInt(ERRID.ERR_EndModuleNoModule) %>/>
            </errors>)
    End Sub

    <Fact>
    Public Sub NamespaceOutOfPlace()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Class C1
                        Namespace N1
            ]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'." start="17" end="31"/>
                <error id="30481" message="'Class' statement must end with a matching 'End Class'." start="52" end="60"/>
                <error id="30618" message="'Namespace' statements can occur only at file or namespace level." start="85" end="97"/>
                <error id="30626" message="'Namespace' statement must end with a matching 'End Namespace'." start="85" end="97"/>
            </errors>)

        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub S1()
                        Namespace N1
            ]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'." start="17" end="31"/>
                <error id="30026" message="'End Sub' expected." start="52" end="60"/>
                <error id="30289" message="Statement cannot appear within a method body. End of method assumed." start="85" end="97"/>
                <error id="30626" message="'Namespace' statement must end with a matching 'End Namespace'." start="85" end="97"/>
            </errors>)
    End Sub

    <WorkItem(888556, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30984ERR_ExpectedAssignmentOperatorInInit_AndExpectedRbrace()
        ParseAndVerify(<![CDATA[
                        Module Module1
                         Sub Goo()
                            Dim scen2b = New With {.prop = 10, .321prop = 9, .567abc = -10}
                            Dim scen3 = New With {.$123prop=1}
                         End Sub
                        End Module
            ]]>,
            <errors>
                <error id="36576"/>
                <error id="36576"/>
                <error id="30203"/>
                <error id="30984"/>
                <error id="30201"/>
                <error id="30037"/>
            </errors>)
    End Sub

    <WorkItem(888560, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30987ERR_ExpectedLbrace_EndNoModuleAndModuleNotAtNamespaceAndExpectedEndClass()
        ParseAndVerify(<![CDATA[
                           Namespace scen8
                              Class customer
                              End Class
                              Class c1
                                  public x as new customer with
                              End Class
                              Module m
                                Sub test()
                                End Sub
                              End Module
                           End Namespace
            ]]>,
            <errors>
                <error id="30987"/>
            </errors>)
    End Sub

    <WorkItem(889038, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31111ERR_ExitEventMemberNotInvalid_ExpectedExitKindAndSubOfFuncAndExpectedExitKind()
        ParseAndVerify(<![CDATA[
                     Friend Class cTest
                       Friend Delegate Sub fir()
                        Friend Custom Event e As fir
                       AddHandler(ByVal value As fir)
                             exit addhandler
                        End AddHandler
                       RemoveHandler(ByVal value As fir)
                              exit removehandler
                       End RemoveHandler
                       RaiseEvent()
                               exit raiseevent
                       End RaiseEvent
                       End Event
                     End Class
            ]]>,
            <errors>
                <error id="31111"/>
                <error id="31111"/>
                <error id="31111"/>
            </errors>)
    End Sub

    <WorkItem(536278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536278")>
    <Fact>
    Public Sub BC31140ERR_InvalidUseOfCustomModifier_ExpectedSpecifierAndInvalidEndSub()
        ParseAndVerify(<![CDATA[
                      Module Module1
                       Custom sub goo()
                       End Sub
                      End Module

            ]]>,
            <errors>
                <error id="30195"/>
                <error id="31140"/>
                <error id="30429"/>
            </errors>)
    End Sub

    <Fact>
    Public Sub BC32100ERR_TypeParamMissingAsCommaOrRParen_GenericClassMissingComma()
        ParseAndVerify(<![CDATA[
            class B(of a b)
            end class
        ]]>,
        <errors>
            <error id="32100"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub Bug863497()
        ParseAndVerify(<![CDATA[
                Namespace $safeitemname$
                    Public Module $safeitemname$
                    End Module
                End Namespace
            ]]>,
            <errors>
                <error id="30203"/>
                <error id="30037"/>
                <error id="30203"/>
                <error id="30037"/>
            </errors>)
    End Sub

    <WorkItem(538990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538990")>
    <Fact>
    Public Sub Bug4770()
        ParseAndVerify(<![CDATA[
         interface i1
            dim i as integer
         End interface
            ]]>, <errors>
                     <error id="30602"/>
                 </errors>)
    End Sub

    <WorkItem(539509, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539509")>
    <Fact>
    Public Sub EnumsWithGenericParameter()
        ' Enums should recover the same was as other declarations that do not allow generics.
        Dim t = ParseAndVerify(<![CDATA[
       enum e(Of T1, T2)
            red
       end enum

        Module Program(Of T1, T2)
        End Module

        Class C
            Sub New(Of T)()
            End Sub
            Property P(Of T)
            Event e(Of T)
            Shared Operator +(Of T)(x As C1, y As C1) As C1
            End Operator
        End Class

            ]]>,
            <errors>
                <error id="32065" message="Type parameters cannot be specified on this declaration." start="14" end="25"/>
                <error id="32073" message="Modules cannot be generic." start="81" end="92"/>
                <error id="32065" message="Type parameters cannot be specified on this declaration." start="148" end="154"/>
                <error id="32065" message="Type parameters cannot be specified on this declaration." start="199" end="205"/>
                <error id="32065" message="Type parameters cannot be specified on this declaration." start="225" end="231"/>
                <error id="32065" message="Type parameters cannot be specified on this declaration." start="261" end="267"/>
            </errors>)

        Dim root = t.GetCompilationUnitRoot()

        Assert.Equal("(Of T1, T2)" + vbLf, DirectCast(root.Members(0), EnumBlockSyntax).EnumStatement.Identifier.TrailingTrivia.Node.ToFullString)
        Assert.Equal("(Of T1, T2)" + vbLf, DirectCast(root.Members(1), TypeBlockSyntax).BlockStatement.Identifier.TrailingTrivia.Node.ToFullString)

        Dim c = DirectCast(root.Members(2), TypeBlockSyntax)
        Assert.Equal("(Of T)", DirectCast(c.Members(0), ConstructorBlockSyntax).SubNewStatement.NewKeyword.TrailingTrivia.Node.ToFullString)
        Assert.Equal("(Of T)" + vbLf, DirectCast(c.Members(1), PropertyStatementSyntax).Identifier.TrailingTrivia.Node.ToFullString)
        Assert.Equal("(Of T)" + vbLf, DirectCast(c.Members(2), EventStatementSyntax).Identifier.TrailingTrivia.Node.ToFullString)
        Assert.Equal("(Of T)", DirectCast(c.Members(3), OperatorBlockSyntax).OperatorStatement.OperatorToken.TrailingTrivia.Node.ToFullString)
    End Sub

    <Fact>
    Public Sub ERR_NamespaceNotAllowedInScript()
        Const source = "
Namespace N
End Namespace
"

        Parse(source, TestOptions.Script).AssertTheseDiagnostics(<errors><![CDATA[
BC36965: You cannot declare Namespace in script code
Namespace N
~~~~~~~~~
]]></errors>)
    End Sub

#End Region

End Class
