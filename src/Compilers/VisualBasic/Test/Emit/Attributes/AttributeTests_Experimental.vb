' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class AttributeTests_Experimental
        Inherits BasicTestBase

        Private Shared ReadOnly DeprecatedAndExperimentalAttributeSource As XElement =
<compilation>
    <file><![CDATA[
Imports System
Namespace Windows.Foundation.Metadata
    <AttributeUsage(
        AttributeTargets.Class Or AttributeTargets.Struct Or AttributeTargets.Enum Or AttributeTargets.Constructor Or AttributeTargets.Method Or AttributeTargets.Property Or AttributeTargets.Field Or AttributeTargets.Event Or AttributeTargets.Interface Or AttributeTargets.Delegate,
        AllowMultiple:=True)>
    Public NotInheritable Class DeprecatedAttribute
        Inherits Attribute
        Public Sub New(message As String, type As DeprecationType, version As UInteger)
        End Sub
    End Class
    Public Enum DeprecationType
        Deprecate
        Remove
    End Enum
End Namespace
]]>
    </file>
    <file><![CDATA[
Imports System
Namespace Windows.Foundation.Metadata
    <AttributeUsage(
        AttributeTargets.Class Or AttributeTargets.Struct Or AttributeTargets.Enum Or AttributeTargets.Interface Or AttributeTargets.Delegate,
        AllowMultiple:=False)>
    Public NotInheritable Class ExperimentalAttribute
        Inherits Attribute
    End Class
End Namespace
]]>
    </file>
</compilation>

        <Fact()>
        Public Sub TestExperimentalAttribute()
            Dim comp0 = CreateCompilationWithMscorlib(DeprecatedAndExperimentalAttributeSource)
            comp0.AssertNoDiagnostics()
            Dim ref0 = comp0.EmitToImageReference()

            Dim source1 =
<compilation>
    <file><![CDATA[
Imports Windows.Foundation.Metadata
Namespace N
    <Experimental>
    Public Structure S
    End Structure
    <Experimental>
    Public Delegate Sub D(Of T)()
    Public Class A(Of T)
        <Experimental>
        Public Class B
        End Class
        Private Shared Sub M()
            Dim o = New B()
            Dim d As D(Of Integer) = Nothing
            d()
        End Sub
    End Class
    <Experimental>
    Public Enum E
        A
    End Enum
End Namespace
]]>
    </file>
</compilation>
            Dim comp1 = CreateCompilationWithMscorlib(source1, references:={ref0})
            comp1.AssertTheseDiagnostics(<errors>
BC42380: 'N.A(Of T).B' is for evaluation purposes only and is subject to change or removal in future updates.
            Dim o = New B()
                        ~
BC42380: 'N.D(Of Integer)' is for evaluation purposes only and is subject to change or removal in future updates.
            Dim d As D(Of Integer) = Nothing
                     ~~~~~~~~~~~~~
     </errors>)

            Dim source2 =
<compilation>
    <file><![CDATA[
Imports N
Imports B = N.A(Of Integer).B
Class C
    Shared Sub F()
        Dim o As Object = New B()
        o = New S()
        Dim e As E
        e = E.A
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim comp2A = CreateCompilationWithMscorlib(source2, references:={ref0, comp1.EmitToImageReference()})
            comp2A.AssertTheseDiagnostics(<errors>
BC42380: 'N.A(Of Integer).B' is for evaluation purposes only and is subject to change or removal in future updates.
Imports B = N.A(Of Integer).B
            ~~~~~~~~~~~~~~~~~
BC42380: 'N.S' is for evaluation purposes only and is subject to change or removal in future updates.
        o = New S()
                ~
BC42380: 'N.E' is for evaluation purposes only and is subject to change or removal in future updates.
        Dim e As E
                 ~
BC42380: 'N.E' is for evaluation purposes only and is subject to change or removal in future updates.
        e = E.A
            ~
     </errors>)

            Dim comp2B = CreateCompilationWithMscorlib(source2, references:={ref0, New VisualBasicCompilationReference(comp1)})
            comp2B.AssertTheseDiagnostics(<errors>
BC42380: 'N.A(Of Integer).B' is for evaluation purposes only and is subject to change or removal in future updates.
Imports B = N.A(Of Integer).B
            ~~~~~~~~~~~~~~~~~
BC42380: 'N.S' is for evaluation purposes only and is subject to change or removal in future updates.
        o = New S()
                ~
BC42380: 'N.E' is for evaluation purposes only and is subject to change or removal in future updates.
        Dim e As E
                 ~
BC42380: 'N.E' is for evaluation purposes only and is subject to change or removal in future updates.
        e = E.A
            ~
     </errors>)
        End Sub

        ' <Experimental> applied to members even though
        ' AttributeUsage is types only.
        <Fact()>
        Public Sub TestExperimentalMembers()
            Dim source0 =
".class public Windows.Foundation.Metadata.ExperimentalAttribute extends [mscorlib]System.Attribute
{
  .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = ( 01 00 1C 14 00 00 01 00 54 02 0D 41 6C 6C 6F 77   // ........T..Allow
                                                                                                                         4D 75 6C 74 69 70 6C 65 00 )                      // Multiple.
}
.class public E extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype E A = int32(0x00000000)
  .custom instance void Windows.Foundation.Metadata.ExperimentalAttribute::.ctor() = ( 01 00 00 00 ) 
  .field public static literal valuetype E B = int32(0x00000001)
}
.class interface public I
{
  .method public abstract virtual instance void F()
  {
    .custom instance void Windows.Foundation.Metadata.ExperimentalAttribute::.ctor() = ( 01 00 00 00 ) 
  }
}
.class public C implements I
{
  .method public virtual final instance void F() { ret }
}"
            Dim ref0 = CompileIL(source0)
            Dim source1 =
<compilation>
    <file><![CDATA[
Class Program
    Shared Sub Main()
        Dim e As E
        e = E.A              ' BC42380: 'A' is for evaluation purposes only
        e = E.B
        Dim o As C = Nothing
        o.F()
        DirectCast(o, I).F() ' BC42380: 'Sub F()' is for evaluation purposes only
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim comp1 = CreateCompilationWithMscorlib(source1, references:={ref0})
            comp1.AssertTheseDiagnostics(<errors>
BC42380: 'A' is for evaluation purposes only and is subject to change or removal in future updates.
        e = E.A              ' BC42380: 'A' is for evaluation purposes only
            ~~~
BC42380: 'Sub F()' is for evaluation purposes only and is subject to change or removal in future updates.
        DirectCast(o, I).F() ' BC42380: 'Sub F()' is for evaluation purposes only
        ~~~~~~~~~~~~~~~~~~~~
     </errors>)

            Dim source2 =
<compilation>
    <file><![CDATA[
Imports N
Imports B = N.A(Of Integer).B
Class C
    Shared Sub F()
        Dim o As Object = New B()
        o = New S()
        Dim e As E
        e = E.A
    End Sub
End Class
]]>
    </file>
</compilation>
        End Sub

        <Fact()>
        Public Sub TestExperimentalTypeWithDeprecatedAndObsoleteMembers()
            Dim comp0 = CreateCompilationWithMscorlib(DeprecatedAndExperimentalAttributeSource)
            comp0.AssertNoDiagnostics()
            Dim ref0 = comp0.EmitToImageReference()

            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Imports Windows.Foundation.Metadata
<Experimental>
Class A
    Friend Sub F0()
    End Sub
    <Deprecated("", DeprecationType.Deprecate, 0)>
    Friend Sub F1()
    End Sub
    <Deprecated("", DeprecationType.Remove, 0)>
    Friend Sub F2()
    End Sub
    <Obsolete("", False)>
    Friend Sub F3()
    End Sub
    <Obsolete("", True)>
    Friend Sub F4()
    End Sub
    <Experimental>
    Friend Class B
    End Class
End Class
Class C
    Shared Sub F(a As A)
        a.F0()
        a.F1()
        a.F2()
        a.F3()
        a.F4()
        Dim b = New A.B()
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib(source, references:={ref0})
            comp.AssertTheseDiagnostics(<errors>
BC42380: 'A' is for evaluation purposes only and is subject to change or removal in future updates.
    Shared Sub F(a As A)
                      ~
BC40008: 'Friend Sub F1()' is obsolete.
        a.F1()
        ~~~~~~
BC31075: 'Friend Sub F2()' is obsolete.
        a.F2()
        ~~~~~~
BC40008: 'Friend Sub F3()' is obsolete.
        a.F3()
        ~~~~~~
BC31075: 'Friend Sub F4()' is obsolete.
        a.F4()
        ~~~~~~
BC42380: 'A' is for evaluation purposes only and is subject to change or removal in future updates.
        Dim b = New A.B()
                    ~
BC42380: 'A.B' is for evaluation purposes only and is subject to change or removal in future updates.
        Dim b = New A.B()
                    ~~~
     </errors>)
        End Sub

        ' Diagnostics for <Obsolete> members
        ' are not suppressed in <Experimental> types.
        <Fact()>
        Public Sub TestObsoleteMembersInExperimentalType()
            Dim comp0 = CreateCompilationWithMscorlib(DeprecatedAndExperimentalAttributeSource)
            comp0.AssertNoDiagnostics()
            Dim ref0 = comp0.EmitToImageReference()

            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Imports Windows.Foundation.Metadata
Class A
    Friend Sub F0()
    End Sub
    <Deprecated("", DeprecationType.Deprecate, 0)>
    Friend Sub F1()
    End Sub
    <Deprecated("", DeprecationType.Remove, 0)>
    Friend Sub F2()
    End Sub
    <Obsolete("", False)>
    Friend Sub F3()
    End Sub
    <Obsolete("", True)>
    Friend Sub F4()
    End Sub
    <Experimental>
    Friend Class B
    End Class
End Class
<Experimental>
Class C
    Shared Sub F(a As A)
        a.F0()
        a.F1()
        a.F2()
        a.F3()
        a.F4()
        Dim b = New A.B()
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib(source, references:={ref0})
            comp.AssertTheseDiagnostics(<errors>
BC40008: 'Friend Sub F1()' is obsolete.
        a.F1()
        ~~~~~~
BC31075: 'Friend Sub F2()' is obsolete.
        a.F2()
        ~~~~~~
BC40008: 'Friend Sub F3()' is obsolete.
        a.F3()
        ~~~~~~
BC31075: 'Friend Sub F4()' is obsolete.
        a.F4()
        ~~~~~~
BC42380: 'A.B' is for evaluation purposes only and is subject to change or removal in future updates.
        Dim b = New A.B()
                    ~~~
     </errors>)
        End Sub

        ' Diagnostics for <Experimental> types
        ' are not suppressed in <Obsolete> members.
        <Fact()>
        Public Sub TestExperimentalTypeInObsoleteMember()
            Dim comp0 = CreateCompilationWithMscorlib(DeprecatedAndExperimentalAttributeSource)
            comp0.AssertNoDiagnostics()
            Dim ref0 = comp0.EmitToImageReference()

            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Imports Windows.Foundation.Metadata
<Experimental>
Class A
End Class
Class C
    Shared Function F0() As Object
        Return If(New A(), 0)
    End Function
    <Obsolete("", False)>
    Shared Function F1() As Object
        Return If(New A(), 1)
    End Function
End Class
]]>
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib(source, references:={ref0})
            comp.AssertTheseDiagnostics(<errors>
BC42380: 'A' is for evaluation purposes only and is subject to change or removal in future updates.
        Return If(New A(), 0)
                      ~
     </errors>)
        End Sub

        ' Combinations of attributes.
        <Fact()>
        Public Sub TestDeprecatedAndExperimentalAndObsoleteAttributes()
            Dim comp0 = CreateCompilationWithMscorlib(DeprecatedAndExperimentalAttributeSource)
            comp0.AssertNoDiagnostics()
            Dim ref0 = comp0.EmitToImageReference()

            Dim source1 =
<compilation>
    <file><![CDATA[
Imports System
Imports Windows.Foundation.Metadata
<Obsolete("OA", False),                          Deprecated("DA", DeprecationType.Deprecate, 0)>
Public Structure SA : End Structure
<Obsolete("OB", False),                          Deprecated("DB", DeprecationType.Remove, 0)>   Public Structure SB : End Structure
<Obsolete("OC", False),                          Experimental>                                  Public Structure SC : End Structure
<Obsolete("OD", True),                           Deprecated("DC", DeprecationType.Deprecate, 0)>Public Structure SD : End Structure
<Obsolete("OE", True),                           Deprecated("DD", DeprecationType.Remove, 0)>   Public Structure SE : End Structure
<Obsolete("OF", True),                           Experimental>                                  Public Structure SF : End Structure
<Deprecated("DG", DeprecationType.Deprecate, 0), Obsolete("OG", False)>                         Public Interface IG : End Interface
<Deprecated("DH", DeprecationType.Deprecate, 0), Obsolete("OH", True)>                          Public Interface IH : End Interface
<Deprecated("DI", DeprecationType.Deprecate, 0), Experimental>                                  Public Interface II : End Interface
<Deprecated("DJ", DeprecationType.Remove, 0),    Obsolete("OJ", False)>                         Public Interface IJ : End Interface
<Deprecated("DK", DeprecationType.Remove, 0),    Obsolete("OK", True)>                          Public Interface IK : End Interface
<Deprecated("DL", DeprecationType.Remove, 0),    Experimental>                                  Public Interface IL : End Interface
<Experimental,                                   Obsolete("OM", False)>                         Public Class CM : End Class
<Experimental,                                   Obsolete("ON", True)>                          Public Class CN : End Class
<Experimental,                                   Deprecated("DO", DeprecationType.Deprecate, 0)>Public Class CO : End Class
<Experimental,                                   Deprecated("DP", DeprecationType.Remove, 0)>   Public Class CP : End Class
]]>
    </file>
</compilation>
            Dim comp1 = CreateCompilationWithMscorlib(source1, references:={ref0})
            comp1.AssertTheseDiagnostics(<errors/>)

            Dim source2 =
<compilation>
    <file><![CDATA[
Class C
    Shared Sub F(o As Object)
    End Sub
    Shared Sub Main()
        F(New SA())
        F(New SB())
        F(New SC())
        F(New SD())
        F(New SE())
        F(New SF())
        F(GetType(IG))
        F(GetType(IH))
        F(GetType(II))
        F(GetType(IJ))
        F(GetType(IK))
        F(New CM())
        F(New CN())
        F(New CO())
        F(New CP())
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim comp2 = CreateCompilationWithMscorlib(source2, references:={ref0, comp1.EmitToImageReference()})
            comp2.AssertTheseDiagnostics(<errors>
BC40000: 'SA' is obsolete: 'DA'.
        F(New SA())
              ~~
BC30668: 'SB' is obsolete: 'DB'.
        F(New SB())
              ~~
BC40000: 'SC' is obsolete: 'OC'.
        F(New SC())
              ~~
BC40000: 'SD' is obsolete: 'DC'.
        F(New SD())
              ~~
BC30668: 'SE' is obsolete: 'DD'.
        F(New SE())
              ~~
BC30668: 'SF' is obsolete: 'OF'.
        F(New SF())
              ~~
BC40000: 'IG' is obsolete: 'DG'.
        F(GetType(IG))
                  ~~
BC40000: 'IH' is obsolete: 'DH'.
        F(GetType(IH))
                  ~~
BC40000: 'II' is obsolete: 'DI'.
        F(GetType(II))
                  ~~
BC30668: 'IJ' is obsolete: 'DJ'.
        F(GetType(IJ))
                  ~~
BC30668: 'IK' is obsolete: 'DK'.
        F(GetType(IK))
                  ~~
BC40000: 'CM' is obsolete: 'OM'.
        F(New CM())
              ~~
BC30668: 'CN' is obsolete: 'ON'.
        F(New CN())
              ~~
BC40000: 'CO' is obsolete: 'DO'.
        F(New CO())
              ~~
BC30668: 'CP' is obsolete: 'DP'.
        F(New CP())
              ~~
     </errors>)
        End Sub

    End Class

End Namespace

