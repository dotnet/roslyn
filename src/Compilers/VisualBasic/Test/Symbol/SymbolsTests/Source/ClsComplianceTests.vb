' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class ClsComplianceTests
        Inherits BasicTestBase
        <Fact>
        Public Sub NoAssemblyLevelAttributeRequired()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<CLSCompliant(True)>
Public Class A
End Class

<CLSCompliant(False)>
Public Class B
End Class
                        ]]>
                    </file>
                </compilation>

            ' In C#, an assembly-level attribute is required.  In VB, that is not the case.
            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub AssemblyLevelAttributeAllowed()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 
<Module: CLSCompliant(True)> 

<CLSCompliant(True)>
Public Class A
End Class

<CLSCompliant(False)>
Public Class B
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub AttributeAllowedOnPrivate()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<CLSCompliant(True)>
Public Class Outer
    <CLSCompliant(True)>
    Private Class A
    End Class

    <CLSCompliant(True)>
    Friend Class B
    End Class

    <CLSCompliant(True)>
    Protected Class C
    End Class

    <CLSCompliant(True)>
    Friend Protected Class D
    End Class

    <CLSCompliant(True)>
    Public Class E
    End Class
End Class
                        ]]>
                    </file>
                </compilation>

            ' C# warns about putting the attribute on members not visible outside the assembly.
            ' VB does not.
            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub AttributeAllowedOnParameters()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<CLSCompliant(True)>
Public Class C

    Public Function M(<CLSCompliant(True)> x As Integer) As Integer
        Return 0
    End Function

    Public ReadOnly Property P(<CLSCompliant(True)> x As Integer) As Integer
        Get
            Return 0
        End Get
    End Property
End Class
                        ]]>
                    </file>
                </compilation>

            ' C# warns about putting the attribute on parameters.  VB does not.
            ' C# also warns about putting the attribute on return types, but VB
            ' does not support the "return" attribute target.
            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub WRN_CLSMemberInNonCLSType3_Explicit()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<CLSCompliant(False)>
Public Class Kinds

    <CLSCompliant(True)>
    Public Sub M()
    End Sub

    <CLSCompliant(True)>
    Public Property P As Integer

    <CLSCompliant(True)>
    Public Event E As ND

    <CLSCompliant(True)>
    Public F As Integer

    <CLSCompliant(True)>
    Public Class NC
    End Class

    <CLSCompliant(True)>
    Public Interface NI
    End Interface

    <CLSCompliant(True)>
    Public Structure NS
    End Structure

    <CLSCompliant(True)>
    Public Delegate Sub ND()

End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40030: sub 'Public Sub M()' cannot be marked CLS-compliant because its containing type 'Kinds' is not CLS-compliant.
    Public Sub M()
               ~
BC40030: property 'Public Property P As Integer' cannot be marked CLS-compliant because its containing type 'Kinds' is not CLS-compliant.
    Public Property P As Integer
                    ~
BC40030: event 'Public Event E As Kinds.ND' cannot be marked CLS-compliant because its containing type 'Kinds' is not CLS-compliant.
    Public Event E As ND
                 ~
BC40030: variable 'Public F As Integer' cannot be marked CLS-compliant because its containing type 'Kinds' is not CLS-compliant.
    Public F As Integer
           ~
BC40030: class 'Kinds.NC' cannot be marked CLS-compliant because its containing type 'Kinds' is not CLS-compliant.
    Public Class NC
                 ~~
BC40030: interface 'Kinds.NI' cannot be marked CLS-compliant because its containing type 'Kinds' is not CLS-compliant.
    Public Interface NI
                     ~~
BC40030: structure 'Kinds.NS' cannot be marked CLS-compliant because its containing type 'Kinds' is not CLS-compliant.
    Public Structure NS
                     ~~
BC40030: delegate Class 'Kinds.ND' cannot be marked CLS-compliant because its containing type 'Kinds' is not CLS-compliant.
    Public Delegate Sub ND()
                        ~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_CLSMemberInNonCLSType3_Implicit()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly:CLSCompliant(False)>

Public Class Kinds

    <CLSCompliant(True)>
    Public Sub M()
    End Sub

    <CLSCompliant(True)>
    Public Property P As Integer

    <CLSCompliant(True)>
    Public Event E As ND

    <CLSCompliant(True)>
    Public F As Integer

    <CLSCompliant(True)>
    Public Class NC
    End Class

    <CLSCompliant(True)>
    Public Interface NI
    End Interface

    <CLSCompliant(True)>
    Public Structure NS
    End Structure

    <CLSCompliant(True)>
    Public Delegate Sub ND()

End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40030: sub 'Public Sub M()' cannot be marked CLS-compliant because its containing type 'Kinds' is not CLS-compliant.
    Public Sub M()
               ~
BC40030: property 'Public Property P As Integer' cannot be marked CLS-compliant because its containing type 'Kinds' is not CLS-compliant.
    Public Property P As Integer
                    ~
BC40030: event 'Public Event E As Kinds.ND' cannot be marked CLS-compliant because its containing type 'Kinds' is not CLS-compliant.
    Public Event E As ND
                 ~
BC40030: variable 'Public F As Integer' cannot be marked CLS-compliant because its containing type 'Kinds' is not CLS-compliant.
    Public F As Integer
           ~
BC40030: class 'Kinds.NC' cannot be marked CLS-compliant because its containing type 'Kinds' is not CLS-compliant.
    Public Class NC
                 ~~
BC40030: interface 'Kinds.NI' cannot be marked CLS-compliant because its containing type 'Kinds' is not CLS-compliant.
    Public Interface NI
                     ~~
BC40030: structure 'Kinds.NS' cannot be marked CLS-compliant because its containing type 'Kinds' is not CLS-compliant.
    Public Structure NS
                     ~~
BC40030: delegate Class 'Kinds.ND' cannot be marked CLS-compliant because its containing type 'Kinds' is not CLS-compliant.
    Public Delegate Sub ND()
                        ~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_CLSMemberInNonCLSType3_Alternating()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<CLSCompliant(True)>
Public Class A
    <CLSCompliant(False)>
    Public Class B
        <CLSCompliant(True)>
        Public Class C
            <CLSCompliant(False)>
            Public Class D
                <CLSCompliant(True)>
                Public Class E

                End Class
            End Class
        End Class
    End Class
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40030: class 'A.B.C' cannot be marked CLS-compliant because its containing type 'A.B' is not CLS-compliant.
        Public Class C
                     ~
BC40030: class 'A.B.C.D.E' cannot be marked CLS-compliant because its containing type 'A.B.C.D' is not CLS-compliant.
                Public Class E
                             ~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_BaseClassNotCLSCompliant2()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class A
    Inherits Bad
End Class

<CLSCompliant(False)>
Public Class Bad
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40026: 'A' is not CLS-compliant because it derives from 'Bad', which is not CLS-compliant.
Public Class A
             ~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_BaseClassNotCLSCompliant2_OtherAssemblies()
            Dim lib1Source =
                <compilation name="lib1">
                    <file name="a.vb">
                        <![CDATA[
Public Class Bad1
End Class
                        ]]>
                    </file>
                </compilation>

            Dim lib2Source =
                <compilation name="lib2">
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)>

<CLSCompliant(False)>
Public Class Bad2
End Class

                        ]]>
                    </file>
                </compilation>

            Dim lib3Source =
                <compilation name="lib3">
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(False)>

Public Class Bad3
End Class
                        ]]>
                    </file>
                </compilation>

            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)>

Public Class A1
    Inherits Bad1
End Class

Public Class A2
    Inherits Bad2
End Class

Public Class A3
    Inherits Bad3
End Class
                        ]]>
                    </file>
                </compilation>

            Dim lib1Ref = CreateCompilationWithMscorlib40(lib1Source).EmitToImageReference()
            Dim lib2Ref = CreateCompilationWithMscorlib40(lib2Source).EmitToImageReference()
            Dim lib3Ref = CreateCompilationWithMscorlib40(lib3Source).EmitToImageReference()

            CreateCompilationWithMscorlib40AndReferences(source, {lib1Ref, lib2Ref, lib3Ref}).AssertTheseDiagnostics(<errors><![CDATA[
BC40026: 'A1' is not CLS-compliant because it derives from 'Bad1', which is not CLS-compliant.
Public Class A1
             ~~
BC40026: 'A2' is not CLS-compliant because it derives from 'Bad2', which is not CLS-compliant.
Public Class A2
             ~~
BC40026: 'A3' is not CLS-compliant because it derives from 'Bad3', which is not CLS-compliant.
Public Class A3
             ~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_InheritedInterfaceNotCLSCompliant2_Interface()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[

Imports System

<Assembly: CLSCompliant(True)> 

Public Interface A
    Inherits Bad
End Interface

Public Interface B
    Inherits Bad, Good
End Interface

Public Interface C
    Inherits Good, Bad
End Interface

<CLSCompliant(True)>
Public Interface Good
End Interface

<CLSCompliant(False)>
Public Interface Bad
End Interface
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40029: 'A' is not CLS-compliant because the interface 'Bad' it inherits from is not CLS-compliant.
Public Interface A
                 ~
BC40029: 'B' is not CLS-compliant because the interface 'Bad' it inherits from is not CLS-compliant.
Public Interface B
                 ~
BC40029: 'C' is not CLS-compliant because the interface 'Bad' it inherits from is not CLS-compliant.
Public Interface C
                 ~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_InheritedInterfaceNotCLSCompliant2_Class()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[

Imports System

<Assembly: CLSCompliant(True)> 

Public Class A
    Implements Bad
End Class

Public Class B
    Implements Bad, Good
End Class

Public Class C
    Implements Good, Bad
End Class

<CLSCompliant(True)>
Public Interface Good
End Interface

<CLSCompliant(False)>
Public Interface Bad
End Interface
                        ]]>
                    </file>
                </compilation>

            ' Implemented interfaces are not required to be compliant - only inherited ones.
            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub WRN_NonCLSMemberInCLSInterface1()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Interface A
    Function M1() As Bad

    <CLSCompliant(False)>
    Sub M2()
End Interface

Public Interface Kinds
    <CLSCompliant(False)>
    Sub M()

    <CLSCompliant(False)>
    Property P()

    <CLSCompliant(False)>
    Event E As Action
End Interface

<CLSCompliant(False)>
Public Class Bad
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40027: Return type of function 'M1' is not CLS-compliant.
    Function M1() As Bad
             ~~
BC40033: Non CLS-compliant 'Sub M2()' is not allowed in a CLS-compliant interface.
    Sub M2()
        ~~
BC40033: Non CLS-compliant 'Sub M()' is not allowed in a CLS-compliant interface.
    Sub M()
        ~
BC40033: Non CLS-compliant 'Property P As Object' is not allowed in a CLS-compliant interface.
    Property P()
             ~
BC40033: Non CLS-compliant 'Event E As Action' is not allowed in a CLS-compliant interface.
    Event E As Action
          ~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_NonCLSMustOverrideInCLSType1()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public MustInherit Class A
    Public MustOverride Function M1() As Bad

    <CLSCompliant(False)>
    Public MustOverride Sub M2()
End Class

Public MustInherit Class Kinds
    <CLSCompliant(False)>
    Public MustOverride Sub M()

    <CLSCompliant(False)>
    Public MustOverride Property P()

    ' VB doesn't support generic events

    Public MustInherit Class C
    End Class
End Class

<CLSCompliant(False)>
Public Class Bad
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40027: Return type of function 'M1' is not CLS-compliant.
    Public MustOverride Function M1() As Bad
                                 ~~
BC40034: Non CLS-compliant 'MustOverride' member is not allowed in CLS-compliant type 'A'.
    Public MustOverride Sub M2()
                            ~~
BC40034: Non CLS-compliant 'MustOverride' member is not allowed in CLS-compliant type 'Kinds'.
    Public MustOverride Sub M()
                            ~
BC40034: Non CLS-compliant 'MustOverride' member is not allowed in CLS-compliant type 'Kinds'.
    Public MustOverride Property P()
                                 ~
]]></errors>)
        End Sub

        <Fact>
        Public Sub AbstractInCompliant_NoAssemblyAttribute()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<CLSCompliant(True)> Public Interface IFace
    <CLSCompliant(False)> Property Prop1() As Long
    <CLSCompliant(False)> Function F2() As Integer
    <CLSCompliant(False)> Event EV3(ByVal i3 As Integer)
    <CLSCompliant(False)> Sub Sub4()
End Interface

<CLSCompliant(True)> Public MustInherit Class QuiteCompliant
    <CLSCompliant(False)> Public MustOverride Sub Sub1()
    <CLSCompliant(False)> Protected MustOverride Function Fun2() As Integer
    <CLSCompliant(False)> Protected Friend MustOverride Sub Sub3()
    <CLSCompliant(False)> Friend MustOverride Function Fun4(ByVal x As Long) As Long
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40033: Non CLS-compliant 'Property Prop1 As Long' is not allowed in a CLS-compliant interface.
    <CLSCompliant(False)> Property Prop1() As Long
                                   ~~~~~
BC40033: Non CLS-compliant 'Function F2() As Integer' is not allowed in a CLS-compliant interface.
    <CLSCompliant(False)> Function F2() As Integer
                                   ~~
BC40033: Non CLS-compliant 'Event EV3(i3 As Integer)' is not allowed in a CLS-compliant interface.
    <CLSCompliant(False)> Event EV3(ByVal i3 As Integer)
                                ~~~
BC40033: Non CLS-compliant 'Sub Sub4()' is not allowed in a CLS-compliant interface.
    <CLSCompliant(False)> Sub Sub4()
                              ~~~~
BC40034: Non CLS-compliant 'MustOverride' member is not allowed in CLS-compliant type 'QuiteCompliant'.
    <CLSCompliant(False)> Public MustOverride Sub Sub1()
                                                  ~~~~
BC40034: Non CLS-compliant 'MustOverride' member is not allowed in CLS-compliant type 'QuiteCompliant'.
    <CLSCompliant(False)> Protected MustOverride Function Fun2() As Integer
                                                          ~~~~
BC40034: Non CLS-compliant 'MustOverride' member is not allowed in CLS-compliant type 'QuiteCompliant'.
    <CLSCompliant(False)> Protected Friend MustOverride Sub Sub3()
                                                            ~~~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_GenericConstraintNotCLSCompliant1()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C1(Of t As {Good, Bad}, u As {Bad, Good})
End Class

<CLSCompliant(False)>
Public Class C2(Of t As {Good, Bad}, u As {Bad, Good})
End Class

Public Delegate Sub D1(Of t As {Good, Bad}, u As {Bad, Good})()

<CLSCompliant(False)>
Public Delegate Sub D2(Of t As {Good, Bad}, u As {Bad, Good})()

Public Class C
    Public Sub M1(Of t As {Good, Bad}, u As {Bad, Good})()
    End Sub

    <CLSCompliant(False)>
    Public Sub M2(Of t As {Good, Bad}, u As {Bad, Good})()
    End Sub
End Class

<CLSCompliant(True)>
Public Interface Good
End Interface

<CLSCompliant(False)>
Public Interface Bad
End Interface
                        ]]>
                    </file>
                </compilation>

            ' NOTE: Dev11 squiggles the problematic constraint, but we don't have enough info.
            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40040: Generic parameter constraint type 'Bad' is not CLS-compliant.
Public Class C1(Of t As {Good, Bad}, u As {Bad, Good})
                   ~
BC40040: Generic parameter constraint type 'Bad' is not CLS-compliant.
Public Class C1(Of t As {Good, Bad}, u As {Bad, Good})
                                     ~
BC40040: Generic parameter constraint type 'Bad' is not CLS-compliant.
Public Delegate Sub D1(Of t As {Good, Bad}, u As {Bad, Good})()
                          ~
BC40040: Generic parameter constraint type 'Bad' is not CLS-compliant.
Public Delegate Sub D1(Of t As {Good, Bad}, u As {Bad, Good})()
                                            ~
BC40040: Generic parameter constraint type 'Bad' is not CLS-compliant.
    Public Sub M1(Of t As {Good, Bad}, u As {Bad, Good})()
                     ~
BC40040: Generic parameter constraint type 'Bad' is not CLS-compliant.
    Public Sub M1(Of t As {Good, Bad}, u As {Bad, Good})()
                                       ~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_FieldNotCLSCompliant1()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class Kinds1
    Public F1 As Bad
    Private F2 As Bad
End Class

<CLSCompliant(False)>
Public Class Kinds2
    Public F3 As Bad
    Private F4 As Bad
End Class

<CLSCompliant(False)>
Public Interface Bad
End Interface
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40025: Type of member 'F1' is not CLS-compliant.
    Public F1 As Bad
           ~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_ProcTypeNotCLSCompliant1_Method()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C1
    Public Function M1() As Bad
        Throw New Exception()
    End Function

    Public Function M2() As Generic(Of Bad)
        Throw New Exception()
    End Function

    Public Function M3() As Generic(Of Generic(Of Bad))
        Throw New Exception()
    End Function

    Public Function M4() As Bad()
        Throw New Exception()
    End Function

    Public Function M5() As Bad()()
        Throw New Exception()
    End Function

    Public Function M6() As Bad(,)
        Throw New Exception()
    End Function
End Class

<CLSCompliant(False)>
Public Class C2
    Public Function N1() As Bad
        Throw New Exception()
    End Function

    Public Function N2() As Generic(Of Bad)
        Throw New Exception()
    End Function

    Public Function N3() As Generic(Of Generic(Of Bad))
        Throw New Exception()
    End Function

    Public Function N4() As Bad()
        Throw New Exception()
    End Function

    Public Function N5() As Bad()()
        Throw New Exception()
    End Function

    Public Function N6() As Bad(,)
        Throw New Exception()
    End Function
End Class

Public Class Generic(Of T)
End Class

<CLSCompliant(False)>
Public Interface Bad
End Interface
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40027: Return type of function 'M1' is not CLS-compliant.
    Public Function M1() As Bad
                    ~~
BC40041: Type 'Bad' is not CLS-compliant.
    Public Function M2() As Generic(Of Bad)
                    ~~
BC40041: Type 'Bad' is not CLS-compliant.
    Public Function M3() As Generic(Of Generic(Of Bad))
                    ~~
BC40027: Return type of function 'M4' is not CLS-compliant.
    Public Function M4() As Bad()
                    ~~
BC40027: Return type of function 'M5' is not CLS-compliant.
    Public Function M5() As Bad()()
                    ~~
BC40027: Return type of function 'M6' is not CLS-compliant.
    Public Function M6() As Bad(,)
                    ~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_ProcTypeNotCLSCompliant1_Property()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C1
    Public Property P1() As Bad
    Public Property P2() As Generic(Of Bad)
    Public Property P3() As Generic(Of Generic(Of Bad))
    Public Property P4() As Bad()
    Public Property P5() As Bad()()
    Public Property P6() As Bad(,)
End Class

<CLSCompliant(False)>
Public Class C2
    Public Property Q1() As Bad
    Public Property Q2() As Generic(Of Bad)
    Public Property Q3() As Generic(Of Generic(Of Bad))
    Public Property Q4() As Bad()
    Public Property Q5() As Bad()()
    Public Property Q6() As Bad(,)
End Class

Public Class Generic(Of T)
End Class

<CLSCompliant(False)>
Public Interface Bad
End Interface
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40027: Return type of function 'P1' is not CLS-compliant.
    Public Property P1() As Bad
                    ~~
BC40041: Type 'Bad' is not CLS-compliant.
    Public Property P2() As Generic(Of Bad)
                    ~~
BC40041: Type 'Bad' is not CLS-compliant.
    Public Property P3() As Generic(Of Generic(Of Bad))
                    ~~
BC40027: Return type of function 'P4' is not CLS-compliant.
    Public Property P4() As Bad()
                    ~~
BC40027: Return type of function 'P5' is not CLS-compliant.
    Public Property P5() As Bad()()
                    ~~
BC40027: Return type of function 'P6' is not CLS-compliant.
    Public Property P6() As Bad(,)
                    ~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_ProcTypeNotCLSCompliant1_Delegate()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C1
    Public Delegate Function M1() As Bad
    Public Delegate Function M2() As Generic(Of Bad)
    Public Delegate Function M3() As Generic(Of Generic(Of Bad))
    Public Delegate Function M4() As Bad()
    Public Delegate Function M5() As Bad()()
    Public Delegate Function M6() As Bad(,)
End Class

<CLSCompliant(False)>
Public Class C2
    Public Delegate Function N1() As Bad
    Public Delegate Function N2() As Generic(Of Bad)
    Public Delegate Function N3() As Generic(Of Generic(Of Bad))
    Public Delegate Function N4() As Bad()
    Public Delegate Function N5() As Bad()()
    Public Delegate Function N6() As Bad(,)
End Class

Public Class Generic(Of T)
End Class

<CLSCompliant(False)>
Public Interface Bad
End Interface
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40027: Return type of function 'Invoke' is not CLS-compliant.
    Public Delegate Function M1() As Bad
                             ~~
BC40041: Type 'Bad' is not CLS-compliant.
    Public Delegate Function M2() As Generic(Of Bad)
                             ~~
BC40041: Type 'Bad' is not CLS-compliant.
    Public Delegate Function M3() As Generic(Of Generic(Of Bad))
                             ~~
BC40027: Return type of function 'Invoke' is not CLS-compliant.
    Public Delegate Function M4() As Bad()
                             ~~
BC40027: Return type of function 'Invoke' is not CLS-compliant.
    Public Delegate Function M5() As Bad()()
                             ~~
BC40027: Return type of function 'Invoke' is not CLS-compliant.
    Public Delegate Function M6() As Bad(,)
                             ~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_ParamNotCLSCompliant1()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C1
    Public Function M1(p As Bad)
        Throw New Exception()
    End Function

    Public Function M2(p As Generic(Of Bad))
        Throw New Exception()
    End Function

    Public Function M3(p As Generic(Of Generic(Of Bad)))
        Throw New Exception()
    End Function

    Public Function M4(p As Bad())
        Throw New Exception()
    End Function

    Public Function M5(p As Bad()())
        Throw New Exception()
    End Function

    Public Function M6(p As Bad(,))
        Throw New Exception()
    End Function
End Class

<CLSCompliant(False)>
Public Class C2
    Public Function N1(p As Bad)
        Throw New Exception()
    End Function

    Public Function N2(p As Generic(Of Bad))
        Throw New Exception()
    End Function

    Public Function N3(p As Generic(Of Generic(Of Bad)))
        Throw New Exception()
    End Function

    Public Function N4(p As Bad())
        Throw New Exception()
    End Function

    Public Function N5(p As Bad()())
        Throw New Exception()
    End Function

    Public Function N6(p As Bad(,))
        Throw New Exception()
    End Function
End Class

Public Class Generic(Of T)
End Class

<CLSCompliant(False)>
Public Interface Bad
End Interface
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40028: Type of parameter 'p' is not CLS-compliant.
    Public Function M1(p As Bad)
                       ~
BC40041: Type 'Bad' is not CLS-compliant.
    Public Function M2(p As Generic(Of Bad))
                       ~
BC40041: Type 'Bad' is not CLS-compliant.
    Public Function M3(p As Generic(Of Generic(Of Bad)))
                       ~
BC40028: Type of parameter 'p' is not CLS-compliant.
    Public Function M4(p As Bad())
                       ~
BC40028: Type of parameter 'p' is not CLS-compliant.
    Public Function M5(p As Bad()())
                       ~
BC40028: Type of parameter 'p' is not CLS-compliant.
    Public Function M6(p As Bad(,))
                       ~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_ParamNotCLSCompliant1_Kinds()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Interface I1
    Sub M(x As Bad)
    Property P(x As Bad) As Integer
    Delegate Sub D(x As Bad)
End Interface

<CLSCompliant(False)>
Public Interface I2
    Sub M(x As Bad)
    Property P(x As Bad) As Integer
    Delegate Sub D(x As Bad)
End Interface

<CLSCompliant(False)>
Public Interface Bad
End Interface
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40028: Type of parameter 'x' is not CLS-compliant.
    Sub M(x As Bad)
          ~
BC40028: Type of parameter 'x' is not CLS-compliant.
    Property P(x As Bad) As Integer
               ~
BC40028: Type of parameter 'x' is not CLS-compliant.
    Delegate Sub D(x As Bad)
                   ~
]]></errors>)
        End Sub

        ' From LegacyTest\CSharp\Source\csharp\Source\ClsCompliance\generics\Rule_E_01.cs
        <Fact>
        Public Sub WRN_ParamNotCLSCompliant1_ConstructedTypeAccessibility()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C(Of T)
    Protected Class N
    End Class

    ' Not CLS-compliant since C(Of Integer).N is not accessible within C(Of T) in all languages.
    Protected Sub M1(n As C(Of Integer).N)
    End Sub

    ' Fine
    Protected Sub M2(n As C(Of T).N)
    End Sub

    Protected Class N2
        ' Not CLS-compliant
        Protected Sub M3(n As C(Of ULong).N)
        End Sub
    End Class
End Class

Public Class D
    Inherits C(Of Long)

    ' Not CLS-compliant
    Protected Sub M4(n As C(Of Integer).N)
    End Sub

    ' Fine
    Protected Sub M5(n As C(Of Long).N)
    End Sub
End Class
                        ]]>
                    </file>
                </compilation>

            ' Dev11 produces error BC30508 for M1 and M3
            ' Dev11 produces error BC30389 for M4 and M5
            ' Roslyn dropped these errors (since they weren't helpful) and, instead, reports CLS warnings.
            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub WRN_ParamNotCLSCompliant1_ProtectedContainer()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C1(Of T)
    Protected Class C2(Of U)
        Public Class C3(Of V)
            Public Sub M(Of W)(p As C1(Of Integer).C2(Of U))
            End Sub
            Public Sub M(Of W)(p As C1(Of Integer).C2(Of U).C3(Of V))
            End Sub
        End Class
    End Class
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub AttributeConstructorsWithArrayParameters()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class EmptyAttribute
    Inherits Attribute
End Class

Public Class PublicAttribute
    Inherits Attribute

    ' Not accessible
    Friend Sub New()
    End Sub

    ' Not compliant
    <CLSCompliant(False)>
    Public Sub New(x As Integer)
    End Sub

    ' Array argument
    Public Sub New(a As Integer(,))
    End Sub

    ' Array argument
    Public Sub New(ParamArray a As Char())
    End Sub
End Class

Friend Class InternalAttribute
    Inherits Attribute

    ' Not accessible
    Public Sub New()
    End Sub
End Class

<CLSCompliant(False)>
Public Class BadAttribute
    Inherits Attribute

    ' Fine, since type isn't compliant.
    Public Sub New(array As Integer())
    End Sub
End Class

Public Class NotAnAttribute
    ' Fine, since not an attribute type.
    Public Sub New(array As Integer())
    End Sub
End Class
                        ]]>
                    </file>
                </compilation>

            ' NOTE: C# requires that compliant attributes have at least one
            ' accessible constructor with no attribute parameters.
            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub AttributeConstructorsWithNonPredefinedParameters()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class MyAttribute
    Inherits Attribute

    Public Sub New(m As MyAttribute)
    End Sub
End Class
                        ]]>
                    </file>
                </compilation>

            ' CLS only allows System.Type, string, char, bool, byte, short, int, long, float, double, and enums,
            ' but dev11 does not enforce this.
            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub ArrayArgumentToAttribute()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class ArrayAttribute
    Inherits Attribute

    Public Sub New(array As Integer())
    End Sub
End Class

Friend Class InternalArrayAttribute
    Inherits Attribute

    Public Sub New(array As Integer())
    End Sub
End Class

Public Class ObjectAttribute
    Inherits Attribute

    Public Sub New(array As Object)
    End Sub
End Class

Public Class NamedArgumentAttribute
    Inherits Attribute

    Public Property O As Object
End Class

<Array({1})>
Public Class A
End Class

<[Object]({1})>
Public Class B
End Class

<InternalArray({1})>
Public Class C
End Class

<NamedArgument(O:={1})>
Public Class D
End Class
                        ]]>
                    </file>
                </compilation>

            ' CLS only allows System.Type, string, char, bool, byte, short, int, long, float, double, and enums,
            ' but dev11 does not enforce this.
            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub WRN_NameNotCLSCompliant1()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class _A
End Class

Public Class B_
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40031: Name '_A' is not CLS-compliant.
Public Class _A
             ~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_NameNotCLSCompliant1_Kinds()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class Kinds

    Public Sub _M()
    End Sub

    Public Property _P As Integer

    Public Event _E As _ND

    Public _F As Integer

    Public Class _NC
    End Class

    Public Interface _NI
    End Interface

    Public Structure _NS
    End Structure

    Public Delegate Sub _ND()

    Private _Private As Integer

    <CLSCompliant(False)>
    Public _NonCompliant As Integer

End Class

Namespace _NS1
End Namespace

Namespace NS1._NS2
End Namespace
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40031: Name '_M' is not CLS-compliant.
    Public Sub _M()
               ~~
BC40031: Name '_P' is not CLS-compliant.
    Public Property _P As Integer
                    ~~
BC40031: Name '_E' is not CLS-compliant.
    Public Event _E As _ND
                 ~~
BC40031: Name '_F' is not CLS-compliant.
    Public _F As Integer
           ~~
BC40031: Name '_NC' is not CLS-compliant.
    Public Class _NC
                 ~~~
BC40031: Name '_NI' is not CLS-compliant.
    Public Interface _NI
                     ~~~
BC40031: Name '_NS' is not CLS-compliant.
    Public Structure _NS
                     ~~~
BC40031: Name '_ND' is not CLS-compliant.
    Public Delegate Sub _ND()
                        ~~~
BC40031: Name '_NS1' is not CLS-compliant.
Namespace _NS1
          ~~~~
BC40031: Name '_NS2' is not CLS-compliant.
Namespace NS1._NS2
              ~~~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_NameNotCLSCompliant1_Overrides()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class Base
    Public Overridable Sub _M()
    End Sub
End Class

Public Class Derived
    Inherits Base

    Public Overrides Sub _M()
    End Sub
End Class
                        ]]>
                    </file>
                </compilation>

            ' NOTE: C# doesn't report this warning on overrides.
            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40031: Name '_M' is not CLS-compliant.
    Public Overridable Sub _M()
                           ~~
BC40031: Name '_M' is not CLS-compliant.
    Public Overrides Sub _M()
                         ~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_NameNotCLSCompliant1_NotReferencable()
            Dim il = <![CDATA[
.class public abstract auto ansi B
{
  .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = {bool(true)}

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  _getter() cil managed
  {
  }

  .property instance int32 P()
  {
    .get instance int32 B::_getter()
  }
}
]]>

            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C
    Inherits B

    Public Overrides ReadOnly Property P As Integer
        Get
            Return 0
        End Get
    End Property
End Class
                        ]]>
                    </file>
                </compilation>

            Dim comp = CreateCompilationWithCustomILSource(source, il)
            comp.AssertNoDiagnostics()

            Dim accessor = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C").GetMember(Of PropertySymbol)("P").GetMethod
            Assert.True(accessor.MetadataName.StartsWith("_", StringComparison.Ordinal))
        End Sub

        <Fact>
        Public Sub WRN_NameNotCLSCompliant1_Parameter()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class B
    Public Sub M(_p As Integer)
    End Sub
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub ModuleLevel_NoAssemblyLevel()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Module: CLSCompliant(True)>
                        ]]>
                    </file>
                </compilation>

            ' C# warns.
            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()

            source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Module: CLSCompliant(False)>
                        ]]>
                    </file>
                </compilation>

            ' C# warns.
            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub ModuleLevel_DisagreesWithAssemblyLevel()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(False)>
<Module: CLSCompliant(True)>
                        ]]>
                    </file>
                </compilation>

            ' C# warns.
            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()

            source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)>
<Module: CLSCompliant(False)>
                        ]]>
                    </file>
                </compilation>

            ' C# warns.
            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub DroppedAttributes()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System
<Assembly: CLSCompliant(False)>
<Module: CLSCompliant(True)>
                        ]]>
                    </file>
                </compilation>

            Dim validator = Function(expectAssemblyLevel As Boolean, expectModuleLevel As Boolean) _
                                Sub(m As ModuleSymbol)
                                    Dim predicate = Function(attr As VisualBasicAttributeData) attr.AttributeClass.Name = "CLSCompliantAttribute"

                                    If expectModuleLevel Then
                                        AssertEx.Any(m.GetAttributes(), predicate)
                                    Else
                                        AssertEx.None(m.GetAttributes(), predicate)
                                    End If

                                    If expectAssemblyLevel Then
                                        AssertEx.Any(m.ContainingAssembly.GetAttributes(), predicate)
                                    ElseIf m.ContainingAssembly IsNot Nothing Then
                                        AssertEx.None(m.ContainingAssembly.GetAttributes(), predicate)
                                    End If
                                End Sub

            CompileAndVerify(source, options:=TestOptions.ReleaseDll, sourceSymbolValidator:=validator(True, True), symbolValidator:=validator(True, False))
            CompileAndVerify(source, options:=TestOptions.ReleaseModule, sourceSymbolValidator:=validator(True, True), symbolValidator:=validator(False, True), verify:=Verification.Fails) ' PEVerify doesn't like netmodules
        End Sub

        <Fact>
        Public Sub ConflictingAssemblyLevelAttributes_ModuleVsAssembly()
            Dim source =
                <compilation name="A">
                    <file name="a.vb">
                        <![CDATA[
<Assembly: System.CLSCompliant(False)>
                        ]]>
                    </file>
                </compilation>

            Dim moduleComp = CreateCSharpCompilation("[assembly:System.CLSCompliant(true)]", compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.NetModule), assemblyName:="A")
            Dim moduleRef = moduleComp.EmitToImageReference()

            CreateCompilationWithMscorlib40AndReferences(source, {moduleRef}).AssertTheseDiagnostics(<errors><![CDATA[
BC36978: Attribute 'CLSCompliantAttribute' in 'A.netmodule' cannot be applied multiple times.
]]></errors>)

        End Sub

        <Fact>
        Public Sub ConflictingAssemblyLevelAttributes_ModuleVsModule()
            Dim source =
                <compilation name="A">
                    <file name="a.vb">
                    </file>
                </compilation>

            Dim moduleComp1 = CreateCSharpCompilation("[assembly:System.CLSCompliant(true)]", compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.NetModule), assemblyName:="A")
            Dim moduleRef1 = moduleComp1.EmitToImageReference()

            Dim moduleComp2 = CreateCSharpCompilation("[assembly:System.CLSCompliant(false)]", compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.NetModule), assemblyName:="B")
            Dim moduleRef2 = moduleComp2.EmitToImageReference()

            CreateCompilationWithMscorlib40AndReferences(source, {moduleRef1, moduleRef2}).AssertTheseDiagnostics(<errors><![CDATA[
BC36978: Attribute 'CLSCompliantAttribute' in 'A.netmodule' cannot be applied multiple times.
]]></errors>)

        End Sub

        <Fact>
        Public Sub AssemblyIgnoresModuleAttribute()
            Dim source =
                <compilation name="A">
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Module: Clscompliant(True)> 

<CLSCompliant(True)>
Public Class Test
    Inherits Bad
End Class

' Doesn't inherit True from module, so not compliant.
Public Class Bad
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40026: 'Test' is not CLS-compliant because it derives from 'Bad', which is not CLS-compliant.
Public Class Test
             ~~~~
]]></errors>)

        End Sub

        <Fact>
        Public Sub ModuleIgnoresAssemblyAttribute()
            Dim source =
                <compilation name="A">
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: Clscompliant(True)> 

<CLSCompliant(True)>
Public Class Test
    Inherits Bad
End Class

' Doesn't inherit True from assembly, so not compliant.
Public Class Bad
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source, OutputKind.NetModule).AssertTheseDiagnostics(<errors><![CDATA[
BC40026: 'Test' is not CLS-compliant because it derives from 'Bad', which is not CLS-compliant.
Public Class Test
             ~~~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub IgnoreModuleAttributeInReferencedAssembly()
            Dim source =
                <compilation name="A">
                    <file name="a.vb"><![CDATA[
Imports System

<CLSCompliant(True)>
Public Class Test
    Inherits Bad
End Class
                    ]]></file>
                </compilation>

            Dim assemblyLevelLibSource = <![CDATA[
[assembly:System.CLSCompliant(true)]
public class Bad { }
]]>

            Dim moduleLevelLibSource = <![CDATA[
[module:System.CLSCompliant(true)]
public class Bad { }
]]>

            Dim assemblyLevelLibRef = CreateCSharpCompilation(assemblyLevelLibSource).EmitToImageReference()
            Dim moduleLevelLibRef = CreateCSharpCompilation(moduleLevelLibSource).EmitToImageReference(Nothing) ' suppress warning

            ' Attribute respected.
            CreateCompilationWithMscorlib40AndReferences(source, {assemblyLevelLibRef}).AssertNoDiagnostics()

            ' Attribute not respected.
            CreateCompilationWithMscorlib40AndReferences(source, {moduleLevelLibRef}).AssertTheseDiagnostics(<errors><![CDATA[
BC40026: 'Test' is not CLS-compliant because it derives from 'Bad', which is not CLS-compliant.
Public Class Test
             ~~~~
]]></errors>)

        End Sub

        <Fact>
        Public Sub WRN_EnumUnderlyingTypeNotCLS1()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Enum E1 As UInteger
    A
End Enum

Friend Enum E2 As UInteger
    A
End Enum

<CLSCompliant(False)>
Friend Enum E3 As UInteger
    A
End Enum
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40032: Underlying type 'UInteger' of Enum is not CLS-compliant.
Public Enum E1 As UInteger
            ~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_TypeNotCLSCompliant1()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(False)> 

Public Class Bad1
End Class

Public Class Bad2
End Class

Public Class BadGeneric(Of T, U)
End Class

<CLSCompliant(True)>
Public Class Good
End Class

<CLSCompliant(True)>
Public Class GoodGeneric(Of T, U)
End Class

<CLSCompliant(True)>
Public Class Test
    ' Reported within compliant generic types.
    Public x1 As GoodGeneric(Of Good, Good) ' Fine
    Public x2 As GoodGeneric(Of Good, Bad1)
    Public x3 As GoodGeneric(Of Bad1, Good)
    Public x4 As GoodGeneric(Of Bad1, Bad2) ' Both reported

    ' Reported within non-compliant generic types.
    Public Property y1 As BadGeneric(Of Good, Good)
    Public Property y2 As BadGeneric(Of Good, Bad1)
    Public Property y3 As BadGeneric(Of Bad1, Good)
    Public Property y4 As BadGeneric(Of Bad1, Bad2) ' Both reported

    Public z1 As GoodGeneric(Of GoodGeneric(Of Bad1, Good), GoodGeneric(Of Bad1, Good))
    Public z2 As GoodGeneric(Of BadGeneric(Of Bad1, Good), BadGeneric(Of Bad1, Good)) ' Reported at multiple levels

End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40041: Type 'Bad1' is not CLS-compliant.
    Public x2 As GoodGeneric(Of Good, Bad1)
           ~~
BC40041: Type 'Bad1' is not CLS-compliant.
    Public x3 As GoodGeneric(Of Bad1, Good)
           ~~
BC40041: Type 'Bad1' is not CLS-compliant.
    Public x4 As GoodGeneric(Of Bad1, Bad2) ' Both reported
           ~~
BC40041: Type 'Bad2' is not CLS-compliant.
    Public x4 As GoodGeneric(Of Bad1, Bad2) ' Both reported
           ~~
BC40027: Return type of function 'y1' is not CLS-compliant.
    Public Property y1 As BadGeneric(Of Good, Good)
                    ~~
BC40027: Return type of function 'y2' is not CLS-compliant.
    Public Property y2 As BadGeneric(Of Good, Bad1)
                    ~~
BC40041: Type 'Bad1' is not CLS-compliant.
    Public Property y2 As BadGeneric(Of Good, Bad1)
                    ~~
BC40027: Return type of function 'y3' is not CLS-compliant.
    Public Property y3 As BadGeneric(Of Bad1, Good)
                    ~~
BC40041: Type 'Bad1' is not CLS-compliant.
    Public Property y3 As BadGeneric(Of Bad1, Good)
                    ~~
BC40027: Return type of function 'y4' is not CLS-compliant.
    Public Property y4 As BadGeneric(Of Bad1, Bad2) ' Both reported
                    ~~
BC40041: Type 'Bad1' is not CLS-compliant.
    Public Property y4 As BadGeneric(Of Bad1, Bad2) ' Both reported
                    ~~
BC40041: Type 'Bad2' is not CLS-compliant.
    Public Property y4 As BadGeneric(Of Bad1, Bad2) ' Both reported
                    ~~
BC40041: Type 'Bad1' is not CLS-compliant.
    Public z1 As GoodGeneric(Of GoodGeneric(Of Bad1, Good), GoodGeneric(Of Bad1, Good))
           ~~
BC40041: Type 'Bad1' is not CLS-compliant.
    Public z1 As GoodGeneric(Of GoodGeneric(Of Bad1, Good), GoodGeneric(Of Bad1, Good))
           ~~
BC40041: Type 'Bad1' is not CLS-compliant.
    Public z2 As GoodGeneric(Of BadGeneric(Of Bad1, Good), BadGeneric(Of Bad1, Good)) ' Reported at multiple levels
           ~~
BC40041: Type 'Bad1' is not CLS-compliant.
    Public z2 As GoodGeneric(Of BadGeneric(Of Bad1, Good), BadGeneric(Of Bad1, Good)) ' Reported at multiple levels
           ~~
BC40041: Type 'BadGeneric(Of Bad1, Good)' is not CLS-compliant.
    Public z2 As GoodGeneric(Of BadGeneric(Of Bad1, Good), BadGeneric(Of Bad1, Good)) ' Reported at multiple levels
           ~~
BC40041: Type 'BadGeneric(Of Bad1, Good)' is not CLS-compliant.
    Public z2 As GoodGeneric(Of BadGeneric(Of Bad1, Good), BadGeneric(Of Bad1, Good)) ' Reported at multiple levels
           ~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_RootNamespaceNotCLSCompliant1()
            Dim source1 =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(False)>
                        ]]>
                    </file>
                </compilation>

            ' Nothing reported since the namespace inherits CLSCompliant(False) from the assembly.
            CreateCompilationWithMscorlib40(source1, options:=TestOptions.ReleaseDll.WithRootNamespace("_A")).AssertNoDiagnostics()

            Dim source2 =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)>
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source2, options:=TestOptions.ReleaseDll.WithRootNamespace("_A")).AssertTheseDiagnostics(<errors><![CDATA[
  BC40038: Root namespace '_A' is not CLS-compliant.
]]></errors>)

            Dim source3 =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Public Class Test
End Class
                        ]]>
                    </file>
                </compilation>

            Dim moduleRef = CreateCompilationWithMscorlib40(source3, options:=TestOptions.ReleaseModule).EmitToImageReference()

            CreateCompilationWithMscorlib40AndReferences(source2, {moduleRef}, options:=TestOptions.ReleaseDll.WithRootNamespace("_A").WithConcurrentBuild(False)).AssertTheseDiagnostics(<errors><![CDATA[
  BC40038: Root namespace '_A' is not CLS-compliant.
]]></errors>)

            Dim source4 =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System
<Module: CLSCompliant(True)>
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40AndReferences(source4, {moduleRef}, options:=TestOptions.ReleaseModule.WithRootNamespace("_A").WithConcurrentBuild(True)).AssertTheseDiagnostics(<errors><![CDATA[
  BC40038: Root namespace '_A' is not CLS-compliant.
]]></errors>)

            CreateCompilationWithMscorlib40AndReferences(source2, {moduleRef}, options:=TestOptions.ReleaseModule.WithRootNamespace("_A")).AssertTheseDiagnostics()
        End Sub

        <Fact>
        Public Sub WRN_RootNamespaceNotCLSCompliant2()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)>
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll.WithRootNamespace("_A.B.C")).AssertTheseDiagnostics(<errors><![CDATA[
BC40039: Name '_A' in the root namespace '_A.B.C' is not CLS-compliant.
]]></errors>)

            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll.WithRootNamespace("A._B.C")).AssertTheseDiagnostics(<errors><![CDATA[
BC40039: Name '_B' in the root namespace 'A._B.C' is not CLS-compliant.
]]></errors>)

            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll.WithRootNamespace("A.B._C")).AssertTheseDiagnostics(<errors><![CDATA[
BC40039: Name '_C' in the root namespace 'A.B._C' is not CLS-compliant.
]]></errors>)

            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll.WithRootNamespace("_A.B._C")).AssertTheseDiagnostics(<errors><![CDATA[
BC40039: Name '_A' in the root namespace '_A.B._C' is not CLS-compliant.
BC40039: Name '_C' in the root namespace '_A.B._C' is not CLS-compliant.
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_OptionalValueNotCLSCompliant1()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C
    Public Sub M(Optional x00 As Object = SByte.MaxValue,
                 Optional x01 As Object = Byte.MaxValue,
                 Optional x02 As Object = Short.MaxValue,
                 Optional x03 As Object = UShort.MaxValue,
                 Optional x04 As Object = Integer.MaxValue,
                 Optional x05 As Object = UInteger.MaxValue,
                 Optional x06 As Object = Long.MaxValue,
                 Optional x07 As Object = ULong.MaxValue,
                 Optional x08 As Object = Char.MaxValue,
                 Optional x09 As Object = True,
                 Optional x10 As Object = Single.MaxValue,
                 Optional x11 As Object = Double.MaxValue,
                 Optional x12 As Object = Decimal.MaxValue,
                 Optional x13 As Object = "ABC",
                 Optional x14 As Object = #1/1/2001#)

    End Sub
End Class
                        ]]>
                    </file>
                </compilation>

            ' As in dev11, this only applies to int8, uint16, uint32, and uint64
            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40042: Type of optional value for optional parameter 'x00' is not CLS-compliant.
    Public Sub M(Optional x00 As Object = SByte.MaxValue,
                          ~~~
BC40042: Type of optional value for optional parameter 'x03' is not CLS-compliant.
                 Optional x03 As Object = UShort.MaxValue,
                          ~~~
BC40042: Type of optional value for optional parameter 'x05' is not CLS-compliant.
                 Optional x05 As Object = UInteger.MaxValue,
                          ~~~
BC40042: Type of optional value for optional parameter 'x07' is not CLS-compliant.
                 Optional x07 As Object = ULong.MaxValue,
                          ~~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_OptionalValueNotCLSCompliant1_ParameterTypeNonCompliant()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C
    Public Sub M(Optional x00 As SByte = SByte.MaxValue)

    End Sub
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40028: Type of parameter 'x00' is not CLS-compliant.
    Public Sub M(Optional x00 As SByte = SByte.MaxValue)
                          ~~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_CLSAttrInvalidOnGetSet_True()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<CLSCompliant(True)>
Public Class C
    <CLSCompliant(False)>
    Public Property P1 As UInteger
        <CLSCompliant(True)>
        Get
            Return 0
        End Get
        <CLSCompliant(True)>
        Set(value As UInteger)
        End Set
    End Property

    <CLSCompliant(False)>
    Public ReadOnly Property P2 As UInteger
        <CLSCompliant(True)>
        Get
            Return 0
        End Get
    End Property

    <CLSCompliant(False)>
    Public WriteOnly Property P3 As UInteger
        <CLSCompliant(True)>
        Set(value As UInteger)
        End Set
    End Property
End Class
                        ]]>
                    </file>
                </compilation>

            ' NOTE: No warnings about non-compliant type UInteger.
            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40043: System.CLSCompliantAttribute cannot be applied to property 'Get' or 'Set'.
        <CLSCompliant(True)>
         ~~~~~~~~~~~~~~~~~~
BC40043: System.CLSCompliantAttribute cannot be applied to property 'Get' or 'Set'.
        <CLSCompliant(True)>
         ~~~~~~~~~~~~~~~~~~
BC40043: System.CLSCompliantAttribute cannot be applied to property 'Get' or 'Set'.
        <CLSCompliant(True)>
         ~~~~~~~~~~~~~~~~~~
BC40043: System.CLSCompliantAttribute cannot be applied to property 'Get' or 'Set'.
        <CLSCompliant(True)>
         ~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_CLSAttrInvalidOnGetSet_False()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<CLSCompliant(True)>
Public Class C
    <CLSCompliant(True)>
    Public Property P1 As UInteger
        <CLSCompliant(False)>
        Get
            Return 0
        End Get
        <CLSCompliant(False)>
        Set(value As UInteger)
        End Set
    End Property

    <CLSCompliant(True)>
    Public ReadOnly Property P2 As UInteger
        <CLSCompliant(False)>
        Get
            Return 0
        End Get
    End Property

    <CLSCompliant(True)>
    Public WriteOnly Property P3 As UInteger
        <CLSCompliant(False)>
        Set(value As UInteger)
        End Set
    End Property
End Class
                        ]]>
                    </file>
                </compilation>

            ' NOTE: See warnings about non-compliant type UInteger.
            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40027: Return type of function 'P1' is not CLS-compliant.
    Public Property P1 As UInteger
                    ~~
BC40043: System.CLSCompliantAttribute cannot be applied to property 'Get' or 'Set'.
        <CLSCompliant(False)>
         ~~~~~~~~~~~~~~~~~~~
BC40043: System.CLSCompliantAttribute cannot be applied to property 'Get' or 'Set'.
        <CLSCompliant(False)>
         ~~~~~~~~~~~~~~~~~~~
BC40027: Return type of function 'P2' is not CLS-compliant.
    Public ReadOnly Property P2 As UInteger
                             ~~
BC40043: System.CLSCompliantAttribute cannot be applied to property 'Get' or 'Set'.
        <CLSCompliant(False)>
         ~~~~~~~~~~~~~~~~~~~
BC40027: Return type of function 'P3' is not CLS-compliant.
    Public WriteOnly Property P3 As UInteger
                              ~~
BC40043: System.CLSCompliantAttribute cannot be applied to property 'Get' or 'Set'.
        <CLSCompliant(False)>
         ~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_CLSEventMethodInNonCLSType3()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<CLSCompliant(False)>
Public Class C

    Public Custom Event E1 As Action(Of UInteger)
        <CLSCompliant(True)>
        AddHandler(value As Action(Of UInteger))
        End AddHandler

        <CLSCompliant(True)>
        RemoveHandler(value As Action(Of UInteger))
        End RemoveHandler

        <CLSCompliant(True)>
        RaiseEvent()
        End RaiseEvent
    End Event

    <CLSCompliant(False)>
    Public Custom Event E2 As Action(Of UInteger)
        <CLSCompliant(True)>
        AddHandler(value As Action(Of UInteger))
        End AddHandler

        <CLSCompliant(True)>
        RemoveHandler(value As Action(Of UInteger))
        End RemoveHandler

        <CLSCompliant(True)>
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
                        ]]>
                    </file>
                </compilation>

            ' NOTE: No warnings about non-compliant type UInteger.
            ' NOTE: No warnings about RaiseEvent accessors.
            ' NOTE: CLSCompliant(False) on event doesn't suppress warnings.
            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40053: 'AddHandler' method for event 'E1' cannot be marked CLS compliant because its containing type 'C' is not CLS compliant.
        <CLSCompliant(True)>
         ~~~~~~~~~~~~~~~~~~
BC40053: 'RemoveHandler' method for event 'E1' cannot be marked CLS compliant because its containing type 'C' is not CLS compliant.
        <CLSCompliant(True)>
         ~~~~~~~~~~~~~~~~~~
BC40053: 'AddHandler' method for event 'E2' cannot be marked CLS compliant because its containing type 'C' is not CLS compliant.
        <CLSCompliant(True)>
         ~~~~~~~~~~~~~~~~~~
BC40053: 'RemoveHandler' method for event 'E2' cannot be marked CLS compliant because its containing type 'C' is not CLS compliant.
        <CLSCompliant(True)>
         ~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub EventAccessors()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<CLSCompliant(True)>
Public Class C

    <CLSCompliant(False)>
    Public Custom Event E1 As Action(Of UInteger)
        <CLSCompliant(True)>
        AddHandler(value As Action(Of UInteger))
        End AddHandler

        <CLSCompliant(True)>
        RemoveHandler(value As Action(Of UInteger))
        End RemoveHandler

        <CLSCompliant(True)>
        RaiseEvent()
        End RaiseEvent
    End Event

    <CLSCompliant(True)>
    Public Custom Event E2 As Action(Of UInteger)
        <CLSCompliant(False)>
        AddHandler(value As Action(Of UInteger))
        End AddHandler

        <CLSCompliant(False)>
        RemoveHandler(value As Action(Of UInteger))
        End RemoveHandler

        <CLSCompliant(False)>
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
                        ]]>
                    </file>
                </compilation>

            ' NOTE: As in dev11, we do not warn that we are ignoring CLSCompliantAttribute on event accessors.
            ' NOTE: See warning about non-compliant type UInteger only for E2.
            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40041: Type 'UInteger' is not CLS-compliant.
    Public Custom Event E2 As Action(Of UInteger)
                        ~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub WRN_EventDelegateTypeNotCLSCompliant2()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

Namespace Q
    <CLSCompliant(False)>
    Public Delegate Sub Bad()
End Namespace

<CLSCompliant(True)>
Public Class C
    Public Custom Event E1 As Q.Bad
        AddHandler(value As Q.Bad)
        End AddHandler

        RemoveHandler(value As Q.Bad)
        End RemoveHandler

        RaiseEvent()
        End RaiseEvent
    End Event

    Public Event E2 As Q.Bad

    Public Event E3(x As UInteger)

    <CLSCompliant(False)>
    Public Custom Event E4 As Q.Bad
        AddHandler(value As Q.Bad)
        End AddHandler

        RemoveHandler(value As Q.Bad)
        End RemoveHandler

        RaiseEvent()
        End RaiseEvent
    End Event

    <CLSCompliant(False)>
    Public Event E5 As Q.Bad

    <CLSCompliant(False)>
    Public Event E6(x As UInteger)
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40050: Delegate type 'Bad' of event 'E1' is not CLS-compliant.
    Public Custom Event E1 As Q.Bad
                        ~~
BC40050: Delegate type 'Bad' of event 'E2' is not CLS-compliant.
    Public Event E2 As Q.Bad
                 ~~
BC40028: Type of parameter 'x' is not CLS-compliant.
    Public Event E3(x As UInteger)
                    ~
]]></errors>)
        End Sub

        <Fact>
        Public Sub TopLevelMethod_NoAssemblyAttribute()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

Public Sub M()
End Sub
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC30001: Statement is not valid in a namespace.
Public Sub M()
~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub TopLevelMethod_AttributeTrue()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Sub M()
End Sub
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC30001: Statement is not valid in a namespace.
Public Sub M()
~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub TopLevelMethod_AttributeFalse()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Sub M()
End Sub
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC30001: Statement is not valid in a namespace.
Public Sub M()
~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub NonCompliantInaccessible()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C
    Private Function M(b As Bad) As Bad
        Return b
    End Function

    Private Property P(b As Bad) As Bad
        Get
            Return b
        End Get
        Set(value As Bad)
        End Set
    End Property
End Class

<CLSCompliant(False)>
Public Class Bad
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub NonCompliantAbstractInNonCompliantType()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

<CLSCompliant(False)>
Public MustInherit Class Bad
    Public MustOverride Function M(b As Bad) As Bad
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub SpecialTypes()
            Dim sourceTemplate = <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C
    Public Sub M(p As {0})

    End Sub
End Class
]]>.Value.Replace(vbCr, vbCrLf)

            Dim helper = CreateCompilationWithMscorlib40({""}, Nothing)
            Dim integerType = helper.GetSpecialType(SpecialType.System_Int32)

            For Each st As SpecialType In [Enum].GetValues(GetType(SpecialType))
                Select Case (st)
                    Case SpecialType.None, SpecialType.System_Void, SpecialType.System_Runtime_CompilerServices_IsVolatile
                        Continue For
                End Select

                Dim type = helper.GetSpecialType(st)
                If type.Arity > 0 Then
                    type = type.Construct(ArrayBuilder(Of TypeSymbol).GetInstance(type.Arity, integerType).ToImmutableAndFree())
                End If
                Dim qualifiedName = type.ToTestDisplayString()

                Dim source = String.Format(sourceTemplate, qualifiedName)
                Dim comp = CreateCompilationWithMscorlib40({source}, Nothing)

                Select Case (st)
                    Case SpecialType.System_SByte, SpecialType.System_UInt16, SpecialType.System_UInt32, SpecialType.System_UInt64, SpecialType.System_UIntPtr, SpecialType.System_TypedReference
                        Assert.Equal(ERRID.WRN_ParamNotCLSCompliant1, DirectCast(comp.GetDeclarationDiagnostics().Single().Code, ERRID))
                End Select
            Next
        End Sub

        <WorkItem(697178, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/697178")>
        <Fact>
        Public Sub ConstructedSpecialTypes()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System
Imports System.Collections.Generic

<Assembly: CLSCompliant(True)> 

Public Class C
    Public Sub M(p As IEnumerable(Of UInteger))
    End Sub
End Class
                        ]]>
                    </file>
                </compilation>

            ' Native C# misses this diagnostic
            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40041: Type 'UInteger' is not CLS-compliant.
    Public Sub M(p As IEnumerable(Of UInteger))
                 ~
]]></errors>)
        End Sub

        <Fact>
        Public Sub MissingAttributeType()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

<Missing>
Public Class C
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC30002: Type 'Missing' is not defined.
<Missing>
 ~~~~~~~
]]></errors>)
        End Sub

        <WorkItem(709317, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/709317")>
        <Fact>
        Public Sub Repro709317()
            Dim libSource =
                <compilation name="Lib">
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C
End Class
                        ]]>
                    </file>
                </compilation>

            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class D
    Public Function M() As C
        Return Nothing
    End Function
End Class
                        ]]>
                    </file>
                </compilation>

            Dim libRef = CreateCompilationWithMscorlib40(libSource).EmitToImageReference()
            Dim comp = CreateCompilationWithMscorlib40AndReferences(source, {libRef})
            Dim tree = comp.SyntaxTrees.Single()
            comp.GetDiagnosticsForSyntaxTree(CompilationStage.Declare, tree)
        End Sub

        <WorkItem(709317, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/709317")>
        <Fact>
        Public Sub FilterTree()
            Dim sourceTemplate = <![CDATA[
Imports System

Namespace N{0}

    <CLSCompliant(False)>
    Public Class NonCompliant
    End Class

    <CLSCompliant(False)>
    Public Interface INonCompliant
    End Interface

    <CLSCompliant(True)> 
    Public Class Compliant
        Inherits NonCompliant
        Implements INonCompliant

        Public Function M(Of T As NonCompliant)(n As NonCompliant)
            Throw New Exception
        End Function

        Public F As NonCompliant

        Public Property P As NonCompliant
    End Class

End Namespace
]]>.Value.Replace(vbCr, vbCrLf)

            Dim tree1 = VisualBasicSyntaxTree.ParseText(String.Format(sourceTemplate, 1), path:="a.vb")
            Dim tree2 = VisualBasicSyntaxTree.ParseText(String.Format(sourceTemplate, 2), path:="b.vb")
            Dim comp = CreateCompilationWithMscorlib40({tree1, tree2}, options:=TestOptions.ReleaseDll)

            ' Two copies of each diagnostic - one from each file.
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC40026: 'Compliant' is not CLS-compliant because it derives from 'NonCompliant', which is not CLS-compliant.
    Public Class Compliant
                 ~~~~~~~~~
BC40040: Generic parameter constraint type 'NonCompliant' is not CLS-compliant.
        Public Function M(Of T As NonCompliant)(n As NonCompliant)
                             ~
BC40028: Type of parameter 'n' is not CLS-compliant.
        Public Function M(Of T As NonCompliant)(n As NonCompliant)
                                                ~
BC40025: Type of member 'F' is not CLS-compliant.
        Public F As NonCompliant
               ~
BC40027: Return type of function 'P' is not CLS-compliant.
        Public Property P As NonCompliant
                        ~
BC40026: 'Compliant' is not CLS-compliant because it derives from 'NonCompliant', which is not CLS-compliant.
    Public Class Compliant
                 ~~~~~~~~~
BC40040: Generic parameter constraint type 'NonCompliant' is not CLS-compliant.
        Public Function M(Of T As NonCompliant)(n As NonCompliant)
                             ~
BC40028: Type of parameter 'n' is not CLS-compliant.
        Public Function M(Of T As NonCompliant)(n As NonCompliant)
                                                ~
BC40025: Type of member 'F' is not CLS-compliant.
        Public F As NonCompliant
               ~
BC40027: Return type of function 'P' is not CLS-compliant.
        Public Property P As NonCompliant
                        ~
]]></errors>)

            CompilationUtils.AssertTheseDiagnostics(comp.GetDiagnosticsForSyntaxTree(CompilationStage.Declare, tree1, filterSpanWithinTree:=Nothing, includeEarlierStages:=False),
                                               <errors><![CDATA[
BC40026: 'Compliant' is not CLS-compliant because it derives from 'NonCompliant', which is not CLS-compliant.
    Public Class Compliant
                 ~~~~~~~~~
BC40040: Generic parameter constraint type 'NonCompliant' is not CLS-compliant.
        Public Function M(Of T As NonCompliant)(n As NonCompliant)
                             ~
BC40028: Type of parameter 'n' is not CLS-compliant.
        Public Function M(Of T As NonCompliant)(n As NonCompliant)
                                                ~
BC40025: Type of member 'F' is not CLS-compliant.
        Public F As NonCompliant
               ~
BC40027: Return type of function 'P' is not CLS-compliant.
        Public Property P As NonCompliant
                        ~
]]></errors>)
        End Sub

        <WorkItem(718503, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718503")>
        <Fact>
        Public Sub ErrorTypeAccessibility()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C
    Implements IError
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC30002: Type 'IError' is not defined.
    Implements IError
               ~~~~~~
]]></errors>)
        End Sub

        ' Make sure nothing blows up when a protected symbol has no containing type.
        <Fact>
        Public Sub ProtectedTopLevelType()

            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Protected Class C
    Public F As UInteger
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC31047: Protected types can only be declared inside of a class.
Protected Class C
                ~
]]></errors>)
        End Sub

        <Fact>
        Public Sub ProtectedMemberOfSealedType()

            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public NotInheritable Class C
    Protected F As UInteger ' No warning, since not accessible outside assembly.
End Class
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub InheritedCompliance1()

            Dim libSource =
                <compilation name="Lib">
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(False)> 

<CLSCompliant(True)>
Public Class Base
End Class

Public Class Derived
    Inherits Base
End Class
                        ]]>
                    </file>
                </compilation>

            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C
    Public B as Base
    Public D as Derived
End Class
                        ]]>
                    </file>
                </compilation>

            ' NOTE: As in dev11, we ignore the fact that Derived inherits CLSCompliant(True) from Base.
            Dim libRef = CreateCompilationWithMscorlib40(libSource).EmitToImageReference()
            CreateCompilationWithMscorlib40AndReferences(source, {libRef}).AssertTheseDiagnostics(<errors><![CDATA[
BC40025: Type of member 'D' is not CLS-compliant.
    Public D as Derived
           ~
]]></errors>)
        End Sub

        <Fact>
        Public Sub InheritedCompliance2()

            Dim il = <![CDATA[.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly a
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
  .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = {bool(true)}
}
.module a.dll

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = {bool(false)}
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit Derived
       extends Base
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}]]>

            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C
    Public B as Base
    Public D as Derived
End Class
                        ]]>
                    </file>
                </compilation>

            ' NOTE: As in dev11, we consider the fact that Derived inherits CLSCompliant(False) from Base
            ' (since it is not from the current assembly).
            Dim libRef = CompileIL(il.Value, prependDefaultHeader:=False)
            CreateCompilationWithMscorlib40AndReferences(source, {libRef}).AssertTheseDiagnostics(<errors><![CDATA[
BC40025: Type of member 'B' is not CLS-compliant.
    Public B as Base
           ~
BC40025: Type of member 'D' is not CLS-compliant.
    Public D as Derived
           ~
]]></errors>)
        End Sub

        <Fact>
        Public Sub AllAttributeConstructorsRequireArrays()

            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class MyAttribute
    Inherits Attribute

    Public Sub New(a As Integer())
    End Sub
End Class
                        ]]>
                    </file>
                </compilation>

            ' C# would warn.
            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub ApplyAttributeWithArrayArgument()

            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class ObjectAttribute
    Inherits Attribute

    Public Sub New(o As Object)
    End Sub
End Class

Public Class ArrayAttribute
    Inherits Attribute

    Public Sub New(o As Integer())
    End Sub
End Class

Public Class ParamsAttribute
    Inherits Attribute

    Public Sub New(ParamArray o As Integer())
    End Sub
End Class

<[Object]({1})>
<Array({1})>
<Params(1)>
Public Class C
End Class
                        ]]>
                    </file>
                </compilation>

            ' C# would warn.
            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub ApplyAttributeWithNonCompliantArgument()

            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class ObjectAttribute
    Inherits Attribute

    Public Sub New(o As Object)
    End Sub
End Class

<[Object](1ui)>
Public Class C
End Class
                        ]]>
                    </file>
                </compilation>

            ' C# would warn.
            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub Overloading_ArrayRank()
            Dim source =
    <compilation>
        <file name="a.vb">
            <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class Compliant
    Public Sub M1(x As Integer())
    End Sub
    Public Sub M1(x As Integer(,)) 'BC40035
    End Sub

    Public Sub M2(x As Integer(,,))
    End Sub
    Public Sub M2(x As Integer(,)) 'BC40035
    End Sub

    Public Sub M3(x As Integer())
    End Sub
    Private Sub M3(x As Integer(,)) ' Fine, since inaccessible.
    End Sub

    Public Sub M4(x As Integer())
    End Sub
    <CLSCompliant(False)>
    Private Sub M4(x As Integer(,)) ' Fine, since flagged.
    End Sub
End Class

Friend Class Internal
    Public Sub M1(x As Integer())
    End Sub
    Public Sub M1(x As Integer(,)) ' Fine, since inaccessible.
    End Sub
End Class

<CLSCompliant(False)>
Public Class NonCompliant
    Public Sub M1(x As Integer())
    End Sub
    Public Sub M1(x As Integer(,)) ' Fine, since tagged.
    End Sub
End Class
                        ]]>
        </file>
    </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40035: 'Public Sub M1(x As Integer(*,*))' is not CLS-compliant because it overloads 'Public Sub M1(x As Integer())' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Sub M1(x As Integer(,)) 'BC40035
               ~~
BC40035: 'Public Sub M2(x As Integer(*,*))' is not CLS-compliant because it overloads 'Public Sub M2(x As Integer(*,*,*))' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Sub M2(x As Integer(,)) 'BC40035
               ~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub Overloading_RefKind()
            Dim source =
    <compilation>
        <file name="a.vb">
            <![CDATA[
Imports System

<Assembly: CLSCompliant(False)> 

Public Class Compliant
    Public Sub M1(x As Integer())
    End Sub
    Public Sub M1(ByRef x As Integer()) 'BC30345
    End Sub
End Class
                        ]]>
        </file>
    </compilation>

            ' NOTE: Illegal, even without compliance checking.
            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC30345: 'Public Sub M1(x As Integer())' and 'Public Sub M1(ByRef x As Integer())' cannot overload each other because they differ only by parameters declared 'ByRef' or 'ByVal'.
    Public Sub M1(x As Integer())
               ~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub Overloading_ArrayOfArrays()
            Dim source =
    <compilation>
        <file name="a.vb">
            <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class Compliant
    Public Sub M1(x As Long()())
    End Sub
    Public Sub M1(x As Char()()) 'BC40035
    End Sub

    Public Sub M2(x As Integer()()())
    End Sub
    Public Sub M2(x As Integer()()) 'BC40035
    End Sub

    Public Sub M3(x As Integer()())
    End Sub
    Public Sub M3(x As Integer()) 'Fine (C# warns)
    End Sub

    Public Sub M4(x As Integer(,)(,))
    End Sub
    Public Sub M4(x As Integer()(,)) 'BC40035
    End Sub

    Public Sub M5(x As Integer(,)(,))
    End Sub
    Public Sub M5(x As Integer(,)()) 'BC40035
    End Sub

    Public Sub M6(x As Long()())
    End Sub
    Private Sub M6(x As Char()()) ' Fine, since inaccessible.
    End Sub

    Public Sub M7(x As Long()())
    End Sub
    <CLSCompliant(False)>
    Public Sub M7(x As Char()()) ' Fine, since tagged (dev11 reports BC40035)
    End Sub
End Class

Friend Class Internal
    Public Sub M1(x As Long()())
    End Sub
    Public Sub M1(x As Char()()) ' Fine, since inaccessible.
    End Sub
End Class

<CLSCompliant(False)>
Public Class NonCompliant
    Public Sub M1(x As Long()())
    End Sub
    Public Sub M1(x As Char()()) ' Fine, since tagged.
    End Sub
End Class
                        ]]>
        </file>
    </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[

BC40035: 'Public Sub M1(x As Char()())' is not CLS-compliant because it overloads 'Public Sub M1(x As Long()())' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Sub M1(x As Char()()) 'BC40035
               ~~
BC40035: 'Public Sub M2(x As Integer()())' is not CLS-compliant because it overloads 'Public Sub M2(x As Integer()()())' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Sub M2(x As Integer()()) 'BC40035
               ~~
BC40035: 'Public Sub M4(x As Integer()(*,*))' is not CLS-compliant because it overloads 'Public Sub M4(x As Integer(*,*)(*,*))' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Sub M4(x As Integer()(,)) 'BC40035
               ~~
BC40035: 'Public Sub M5(x As Integer(*,*)())' is not CLS-compliant because it overloads 'Public Sub M5(x As Integer(*,*)(*,*))' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Sub M5(x As Integer(,)()) 'BC40035
               ~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub Overloading_Properties()
            Dim source =
    <compilation>
        <file name="a.vb">
            <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class Compliant
    Public Property P1(x As Long()()) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
    Public Property P1(x As Char()()) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    Public Property P2(x As String()) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
    Public Property P2(x As String(,)) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
                        ]]>
        </file>
    </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40035: 'Public Property P1(x As Char()()) As Integer' is not CLS-compliant because it overloads 'Public Property P1(x As Long()()) As Integer' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Property P1(x As Char()()) As Integer
                    ~~
BC40035: 'Public Property P2(x As String(*,*)) As Integer' is not CLS-compliant because it overloads 'Public Property P2(x As String()) As Integer' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Property P2(x As String(,)) As Integer
                    ~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub Overloading_MethodKinds()
            Dim source =
    <compilation>
        <file name="a.vb">
            <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C
    Public Sub New(p As Long()())
    End Sub
    Public Sub New(p As Char()())
    End Sub

    Public Shared Widening Operator CType(p As Long()) As C
        Return Nothing
    End Operator
    Public Shared Widening Operator CType(p As Long(,)) As C ' Not reported by dev11.
        Return Nothing
    End Operator

    Public Shared Narrowing Operator CType(p As String(,,)) As C
        Return Nothing
    End Operator
    Public Shared Narrowing Operator CType(p As String(,)) As C ' Not reported by dev11.
        Return Nothing
    End Operator

    ' Static constructors can't be overloaded
    ' Destructors can't be overloaded.
    ' Accessors are tested separately.
End Class
                        ]]>
        </file>
    </compilation>

            ' BREAK : Dev11 doesn't report BC40035 for operators.
            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40035: 'Public Sub New(p As Char()())' is not CLS-compliant because it overloads 'Public Sub New(p As Long()())' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Sub New(p As Char()())
               ~~~
BC40035: 'Public Shared Widening Operator CType(p As Long(*,*)) As C' is not CLS-compliant because it overloads 'Public Shared Widening Operator CType(p As Long()) As C' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Shared Widening Operator CType(p As Long(,)) As C ' Not reported by dev11.
                                    ~~~~~
BC40035: 'Public Shared Narrowing Operator CType(p As String(*,*)) As C' is not CLS-compliant because it overloads 'Public Shared Narrowing Operator CType(p As String(*,*,*)) As C' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Shared Narrowing Operator CType(p As String(,)) As C ' Not reported by dev11.
                                     ~~~~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub Overloading_Operators()
            Dim source =
    <compilation>
        <file name="a.vb">
            <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C
    Public Shared Widening Operator CType(p As C) As Integer
        Return Nothing
    End Operator
    Public Shared Widening Operator CType(p As C) As Long
        Return Nothing
    End Operator
End Class
                        ]]>
        </file>
    </compilation>

            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub Overloading_TypeParameterArray()
            Dim source =
    <compilation>
        <file name="a.vb">
            <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C(Of T)
    Public Sub M1(t As T())
    End Sub
    Public Sub M1(t As Integer())
    End Sub

    Public Sub M2(Of U)(t As U())
    End Sub
    Public Sub M2(Of U)(t As Integer())
    End Sub
End Class
                        ]]>
        </file>
    </compilation>

            CreateCompilationWithMscorlib40(source).AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub Overloading_InterfaceMember()
            Dim source =
    <compilation>
        <file name="a.vb">
            <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Interface I
    Sub M(p As Long()())
End Interface

Public Class ImplementWithSameName
    Implements I

    Public Sub M(p()() As Long) Implements I.M
    End Sub

    Public Sub M(p()() As Char) 'BC40035 (twice, in roslyn)
    End Sub
End Class

Public Class ImplementWithOtherName
    Implements I

    Public Sub I_M(p()() As Long) Implements I.M
    End Sub

    Public Sub M(p()() As String) 'BC40035 (roslyn only)
    End Sub
End Class

Public Class Base
    Implements I

    Public Sub I_M(p()() As Long) Implements I.M
    End Sub
End Class

Public Class Derived1
    Inherits Base
    Implements I

    Public Sub M(p()() As Boolean) 'BC40035 (roslyn only)
    End Sub
End Class

Public Class Derived2
    Inherits Base

    Public Sub M(p()() As Short) 'Mimic (C#) dev11 bug - don't report conflict with interface member.
    End Sub
End Class
                        ]]>
        </file>
    </compilation>

            ' BREAK : Dev11 doesn't report BC40035 for interface members.
            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40035: 'Public Sub M(p As Char()())' is not CLS-compliant because it overloads 'Public Sub M(p As Long()())' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Sub M(p()() As Char) 'BC40035 (twice, in roslyn)
               ~
BC40035: 'Public Sub M(p As Char()())' is not CLS-compliant because it overloads 'Sub M(p As Long()())' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Sub M(p()() As Char) 'BC40035 (twice, in roslyn)
               ~
BC40035: 'Public Sub M(p As String()())' is not CLS-compliant because it overloads 'Sub M(p As Long()())' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Sub M(p()() As String) 'BC40035 (roslyn only)
               ~
BC40035: 'Public Sub M(p As Boolean()())' is not CLS-compliant because it overloads 'Sub M(p As Long()())' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Sub M(p()() As Boolean) 'BC40035 (roslyn only)
               ~
            ]]></errors>)
        End Sub

        <Fact>
        Public Sub Overloading_BaseMember()
            Dim source =
    <compilation>
        <file name="a.vb">
            <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class Base
    Public Overridable Sub M(p As Long()())
    End Sub

    Public Overridable WriteOnly Property P(q As Long()())
        Set(value)
        End Set
    End Property
End Class

Public Class Derived_Overload
    Inherits Base

    Public Overloads Sub M(p As Char()())
    End Sub

    Public Overloads WriteOnly Property P(q As Char()())
        Set(value)
        End Set
    End Property
End Class

Public Class Derived_Hide
    Inherits Base

    Public Shadows Sub M(p As Long()())
    End Sub

    Public Shadows WriteOnly Property P(q As Long()())
        Set(value)
        End Set
    End Property
End Class

Public Class Derived_Override
    Inherits Base

    Public Overrides Sub M(p As Long()())
    End Sub

    Public Overrides WriteOnly Property P(q As Long()())
        Set(value)
        End Set
    End Property
End Class

Public Class Derived1
    Inherits Base
End Class

Public Class Derived2
    Inherits Derived1

    Public Overloads Sub M(p As String()())
    End Sub

    Public Overloads WriteOnly Property P(q As String()())
        Set(value)
        End Set
    End Property
End Class
                        ]]>
        </file>
    </compilation>

            ' BREAK : Dev11 doesn't report BC40035 for base type members.
            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40035: 'Public Overloads Sub M(p As Char()())' is not CLS-compliant because it overloads 'Public Overridable Sub M(p As Long()())' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Overloads Sub M(p As Char()())
                         ~
BC40035: 'Public Overloads WriteOnly Property P(q As Char()()) As Object' is not CLS-compliant because it overloads 'Public Overridable WriteOnly Property P(q As Long()()) As Object' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Overloads WriteOnly Property P(q As Char()())
                                        ~
BC40035: 'Public Overloads Sub M(p As String()())' is not CLS-compliant because it overloads 'Public Overridable Sub M(p As Long()())' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Overloads Sub M(p As String()())
                         ~
BC40035: 'Public Overloads WriteOnly Property P(q As String()()) As Object' is not CLS-compliant because it overloads 'Public Overridable WriteOnly Property P(q As Long()()) As Object' which differs from it only by array of array parameter types or by the rank of the array parameter types.
    Public Overloads WriteOnly Property P(q As String()())
                                        ~
            ]]></errors>)
        End Sub

        <Fact>
        Public Sub WithEventsWarning()
            Dim source =
    <compilation>
        <file name="a.vb">
            <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 

Public Class C
    Public WithEvents F As Bad
End Class

<CLSCompliant(False)>
Public Class Bad
End Class
                        ]]>
        </file>
    </compilation>

            ' Make sure we don't produce a bunch of spurious warnings for synthesized members.
            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40027: Return type of function 'F' is not CLS-compliant.
    Public WithEvents F As Bad
                      ~
            ]]></errors>)
        End Sub

        <WorkItem(749432, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/749432")>
        <Fact>
        Public Sub InvalidAttributeArgument()
            Dim source =
    <compilation>
        <file name="a.vb">
            <![CDATA[
Imports System
Public Module VBCoreHelperFunctionality
    <CLSCompliant((New With {.anonymousField = False}).anonymousField)>
    Public Function Len(ByVal Expression As SByte) As Integer
        Return (New With {.anonymousField = 1}).anonymousField
    End Function
    <CLSCompliant((New With {.anonymousField = False}).anonymousField)>
    Public Function Len(ByVal Expression As UInt16) As Integer
        Return (New With {.anonymousField = 2}).anonymousField
    End Function
End Module
                        ]]>
        </file>
    </compilation>

            CreateCompilationWithMscorlib40AndVBRuntime(source).AssertTheseDiagnostics(<errors><![CDATA[
BC30059: Constant expression is required.
    <CLSCompliant((New With {.anonymousField = False}).anonymousField)>
                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
    <CLSCompliant((New With {.anonymousField = False}).anonymousField)>
                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ]]></errors>)
        End Sub

        <WorkItem(749352, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/749352")>
        <Fact>
        Public Sub Repro749352()
            Dim source =
    <compilation>
        <file name="a.vb">
            <![CDATA[
Imports System
 
Namespace ClsCompClass001f
 
    <CLSCompliant(False)> 
    Public Class ContainerClass
 
        'COMPILEWARNING: BC40030, "Scen6"
        <CLSCompliant(True)> 
        Public Event Scen6(ByVal x As Integer)
 
    End Class
End Namespace
                        ]]>
        </file>
    </compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<errors><![CDATA[
BC40030: event 'Public Event Scen6(x As Integer)' cannot be marked CLS-compliant because its containing type 'ContainerClass' is not CLS-compliant.
        Public Event Scen6(ByVal x As Integer)
                     ~~~~~
            ]]></errors>)
        End Sub

        <Fact, WorkItem(1026453, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1026453")>
        Public Sub Bug1026453()
            Dim source1 =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Namespace N1
    Public Class A
    End Class
End Namespace
                        ]]>
                    </file>
                </compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(source1, options:=TestOptions.ReleaseModule)

            Dim source2 =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)> 
<Module: CLSCompliant(True)> 

Namespace N1
    Public Class B
    End Class
End Namespace
                        ]]>
                    </file>
                </compilation>

            Dim comp2 = CreateCompilationWithMscorlib40AndReferences(source2, {comp1.EmitToImageReference()}, TestOptions.ReleaseDll.WithConcurrentBuild(False))
            comp2.AssertNoDiagnostics()
            comp2.WithOptions(TestOptions.ReleaseDll.WithConcurrentBuild(True)).AssertNoDiagnostics()

            Dim comp3 = comp2.WithOptions(TestOptions.ReleaseModule.WithConcurrentBuild(False))
            comp3.AssertNoDiagnostics()
            comp3.WithOptions(TestOptions.ReleaseModule.WithConcurrentBuild(True)).AssertNoDiagnostics()
        End Sub

        <Fact, WorkItem(9719, "https://github.com/dotnet/roslyn/issues/9719")>
        Public Sub Bug9719()
            ' repro was simpler than what's on the github issue - before any fixes, the below snippit triggered the crash
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System

<Assembly: CLSCompliant(True)>

Public Class C
    Public Sub Problem(item As DummyModule)
    End Sub
End Class

Public Module DummyModule

End Module
                        ]]>
                    </file>
                </compilation>

            CreateCompilationWithMscorlib45AndVBRuntime(source).AssertTheseDiagnostics(<errors><![CDATA[
BC30371: Module 'DummyModule' cannot be used as a type.
    Public Sub Problem(item As DummyModule)
                               ~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub TupleDefersClsComplianceToUnderlyingType()
            Dim libCompliant_vb = "
Namespace System
    <CLSCompliant(True)>
    Public Structure ValueTuple(Of T1, T2)
        Public Sub New(item1 As T1, item2 As T2)
        End Sub
    End Structure
End Namespace
"

            Dim libNotCompliant_vb = "
Namespace System
    <CLSCompliant(False)>
    Public Structure ValueTuple(Of T1, T2)
        Public Sub New(item1 As T1, item2 As T2)
        End Sub
    End Structure
End Namespace
"
            Dim source = "
Imports System

<assembly:CLSCompliant(true)>
Public Class C
    Public Function Method() As (Integer, Integer)
        Throw New Exception()
    End Function
    Public Function Method2() As (Bad, Bad)
        Throw New Exception()
    End Function
End Class

<CLSCompliant(false)>
Public Class Bad
End Class
"
            Dim libCompliant = CreateCompilationWithMscorlib40({libCompliant_vb}, options:=TestOptions.ReleaseDll).EmitToImageReference()
            Dim compCompliant = CreateCompilationWithMscorlib40({source}, {libCompliant}, TestOptions.ReleaseDll)
            compCompliant.AssertTheseDiagnostics(
                <errors>
BC40041: Type 'Bad' is not CLS-compliant.
    Public Function Method2() As (Bad, Bad)
                    ~~~~~~~
BC40041: Type 'Bad' is not CLS-compliant.
    Public Function Method2() As (Bad, Bad)
                    ~~~~~~~
                </errors>)

            Dim libNotCompliant = CreateCompilationWithMscorlib40({libNotCompliant_vb}, options:=TestOptions.ReleaseDll).EmitToImageReference()
            Dim compNotCompliant = CreateCompilationWithMscorlib40({source}, {libNotCompliant}, TestOptions.ReleaseDll)
            compNotCompliant.AssertTheseDiagnostics(
                <errors>
BC40027: Return type of function 'Method' is not CLS-compliant.
    Public Function Method() As (Integer, Integer)
                    ~~~~~~
BC40027: Return type of function 'Method2' is not CLS-compliant.
    Public Function Method2() As (Bad, Bad)
                    ~~~~~~~
BC40041: Type 'Bad' is not CLS-compliant.
    Public Function Method2() As (Bad, Bad)
                    ~~~~~~~
BC40041: Type 'Bad' is not CLS-compliant.
    Public Function Method2() As (Bad, Bad)
                    ~~~~~~~
                </errors>)
        End Sub

    End Class
End Namespace
