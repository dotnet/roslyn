' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Basic.Reference.Assemblies
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class VarianceConversions
        Inherits BasicTestBase

        <Fact>
        Public Sub SimpleTest_In_1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict On

Imports System

Module Module1

    Interface I123(Of In T)
        Interface I124
            Sub M2(x As T)
        End Interface

        Sub M1(x As T)
    End Interface

    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class B1
        Implements I123(Of Base), I123(Of Base).I124


        Public Sub M1(x As Base) Implements I123(Of Base).M1
            System.Console.WriteLine("B1.M1")
        End Sub

        Public Sub M2(x As Base) Implements I123(Of Base).I124.M2
            System.Console.WriteLine("B1.M2")
        End Sub
    End Class

    Structure S1
        Implements I123(Of Base), I123(Of Base).I124

        Public Sub M1(x As Base) Implements I123(Of Base).M1
            System.Console.WriteLine("S1.M1")
        End Sub

        Public Sub M2(x As Base) Implements I123(Of Base).I124.M2
            System.Console.WriteLine("S1.M2")
        End Sub
    End Structure

    Sub M3(Of T As I123(Of Base), S As I123(Of Base).I124)(x As T, y As S)
        Dim x1 As I123(Of Derived) = x
        Dim y1 As I123(Of Derived).I124 = y

        x1.M1(New Derived())
        y1.M2(New Derived())
    End Sub

    Sub M4(Of T As B1)(x As T)
        Dim x1 As I123(Of Derived) = x
        Dim y1 As I123(Of Derived).I124 = x

        x1.M1(New Derived())
        y1.M2(New Derived())
    End Sub

    MustInherit Class Base2(Of T)
        MustOverride Sub M5(Of S As T)(x As S)

    End Class

    Class Derived2
        Inherits Base2(Of S1)

        Public Overrides Sub M5(Of S As S1)(x As S)
            Dim x1 As I123(Of Derived) = x
            Dim y1 As I123(Of Derived).I124 = x

            x1.M1(New Derived())
            y1.M2(New Derived())
        End Sub
    End Class

    Sub Test(Of T As I123(Of Base), U As T, V As U)(x As V)
        System.Console.WriteLine("Test")
        Dim y As I123(Of Derived) = x
    End Sub

    Sub Main()

        Dim x As I123(Of Base) = New B1()
        Dim y As I123(Of Derived) = x
        y.M1(New Derived())
        y = New S1()
        y.M1(New Derived())
        y = New B1()
        y.M1(New Derived())

        Dim x1 As I123(Of Base).I124 = New B1()
        Dim y1 As I123(Of Derived).I124 = x1
        y1.M2(New Derived())
        y1 = New S1()
        y1.M2(New Derived())
        y1 = New B1()
        y1.M2(New Derived())

        M3(x, x1)
        M3(New S1(), New S1())
        M3(New B1(), New B1())

        M4(New B1())

        Dim z As New Derived2()
        z.M5(New S1())

        Test(Of B1, B1, B1)(New B1())
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
B1.M1
S1.M1
B1.M1
B1.M2
S1.M2
B1.M2
B1.M1
B1.M2
S1.M1
S1.M2
B1.M1
B1.M2
B1.M1
B1.M2
S1.M1
S1.M2
Test
]]>)

        End Sub

        <Fact>
        Public Sub SimpleTest_Out_1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict On

Imports System

Module Module1

    Interface I123(Of Out T)
        Interface I124
            Function M2() As T
        End Interface

        Function M1() As T
    End Interface

    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class B1
        Implements I123(Of Derived), I123(Of Derived).I124


        Public Function M1() As Derived Implements I123(Of Derived).M1
            System.Console.WriteLine("B1.M1")
            Return New Derived()
        End Function

        Public Function M2() As Derived Implements I123(Of Derived).I124.M2
            System.Console.WriteLine("B1.M2")
            Return New Derived()
        End Function
    End Class

    Structure S1
        Implements I123(Of Derived), I123(Of Derived).I124

        Public Function M1() As Derived Implements I123(Of Derived).M1
            System.Console.WriteLine("S1.M1")
            Return New Derived()
        End Function

        Public Function M2() As Derived Implements I123(Of Derived).I124.M2
            System.Console.WriteLine("S1.M2")
            Return New Derived()
        End Function
    End Structure

    Sub M3(Of T As I123(Of Derived), S As I123(Of Derived).I124)(x As T, y As S)
        Dim x1 As I123(Of Base) = x
        Dim y1 As I123(Of Base).I124 = y

        Dim z As Base = x1.M1()
        z = y1.M2()
    End Sub

    Sub M4(Of T As B1)(x As T)
        Dim x1 As I123(Of Base) = x
        Dim y1 As I123(Of Base).I124 = x

        Dim z As Base = x1.M1()
        z = y1.M2()
    End Sub

    MustInherit Class Base2(Of T)
        MustOverride Sub M5(Of S As T)(x As S)
    End Class

    Class Derived2
        Inherits Base2(Of S1)

        Public Overrides Sub M5(Of S As S1)(x As S)
            Dim x1 As I123(Of Base) = x
            Dim y1 As I123(Of Base).I124 = x

            Dim z As Base = x1.M1()
            z = y1.M2()
        End Sub
    End Class

    Sub Main()

        Dim x As I123(Of Derived) = New B1()
        Dim y As I123(Of Base) = x
        Dim z As Base = y.M1()
        y = New S1()
        z = y.M1()
        y = New B1()
        z = y.M1()

        Dim x1 As I123(Of Derived).I124 = New B1()
        Dim y1 As I123(Of Base).I124 = x1
        z = y1.M2()
        y1 = New S1()
        z = y1.M2()
        y1 = New B1()
        z = y1.M2()

        M3(x, x1)
        M3(New S1(), New S1())
        M3(New B1(), New B1())

        M4(New B1())

        Dim u As New Derived2()
        u.M5(New S1())
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
B1.M1
S1.M1
B1.M1
B1.M2
S1.M2
B1.M2
B1.M1
B1.M2
S1.M1
S1.M2
B1.M1
B1.M2
B1.M1
B1.M2
S1.M1
S1.M2]]>)

        End Sub

        <Fact>
        Public Sub NoVarianceConversion1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Interface I123(Of In T, Out S)
    End Interface

    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Structure Unrelated
    End Structure

    Sub Main()
        Dim x1 As I123(Of Base, Base) = Nothing
        Dim x2 As I123(Of Base, Derived) = Nothing
        Dim x3 As I123(Of Derived, Base) = Nothing
        Dim x4 As I123(Of Derived, Derived) = Nothing
        Dim x5 As I123(Of Base, Unrelated) = Nothing
        Dim x6 As I123(Of Derived, Unrelated) = Nothing
        Dim x7 As I123(Of Unrelated, Derived) = Nothing
        Dim x8 As I123(Of Unrelated, Base) = Nothing
        Dim x9 As I123(Of Unrelated, Unrelated) = Nothing

        x1 = x2
        x1 = x3
        x1 = x4
        x1 = x5
        x1 = x6
        x1 = x7
        x1 = x8
        x1 = x9

        x2 = x1
        x2 = x3
        x2 = x4
        x2 = x5
        x2 = x6
        x2 = x7
        x2 = x8
        x2 = x9

        x3 = x1
        x3 = x2
        x3 = x4
        x3 = x5
        x3 = x6
        x3 = x7
        x3 = x8
        x3 = x9

        x4 = x1
        x4 = x2
        x4 = x3
        x4 = x5
        x4 = x6
        x4 = x7
        x4 = x8
        x4 = x9

        x5 = x1
        x5 = x2
        x5 = x3
        x5 = x4
        x5 = x6
        x5 = x7
        x5 = x8
        x5 = x9

        x6 = x1
        x6 = x2
        x6 = x3
        x6 = x4
        x6 = x5
        x6 = x7
        x6 = x8
        x6 = x9

        x7 = x1
        x7 = x2
        x7 = x3
        x7 = x4
        x7 = x5
        x7 = x6
        x7 = x8
        x7 = x9

        x8 = x1
        x8 = x2
        x8 = x3
        x8 = x4
        x8 = x5
        x8 = x6
        x8 = x7
        x8 = x9

        x9 = x1
        x9 = x2
        x9 = x3
        x9 = x4
        x9 = x5
        x9 = x6
        x9 = x7
        x9 = x8
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Base)' to 'Module1.I123(Of Module1.Base, Module1.Base)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Derived', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x1 = x3
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Derived)' to 'Module1.I123(Of Module1.Base, Module1.Base)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Derived', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x1 = x4
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Unrelated)' to 'Module1.I123(Of Module1.Base, Module1.Base)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Base', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x1 = x5
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Unrelated)' to 'Module1.I123(Of Module1.Base, Module1.Base)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Base', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x1 = x6
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Derived)' to 'Module1.I123(Of Module1.Base, Module1.Base)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Unrelated', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x1 = x7
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Base)' to 'Module1.I123(Of Module1.Base, Module1.Base)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Unrelated', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x1 = x8
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Unrelated)' to 'Module1.I123(Of Module1.Base, Module1.Base)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Base', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x1 = x9
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Base)' to 'Module1.I123(Of Module1.Base, Module1.Derived)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Derived', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x2 = x1
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Base)' to 'Module1.I123(Of Module1.Base, Module1.Derived)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Derived', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x2 = x3
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Derived)' to 'Module1.I123(Of Module1.Base, Module1.Derived)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Derived', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x2 = x4
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Unrelated)' to 'Module1.I123(Of Module1.Base, Module1.Derived)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Derived', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x2 = x5
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Unrelated)' to 'Module1.I123(Of Module1.Base, Module1.Derived)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Derived', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x2 = x6
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Derived)' to 'Module1.I123(Of Module1.Base, Module1.Derived)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Unrelated', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x2 = x7
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Base)' to 'Module1.I123(Of Module1.Base, Module1.Derived)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Derived', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x2 = x8
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Unrelated)' to 'Module1.I123(Of Module1.Base, Module1.Derived)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Derived', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x2 = x9
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Unrelated)' to 'Module1.I123(Of Module1.Derived, Module1.Base)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Base', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x3 = x5
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Unrelated)' to 'Module1.I123(Of Module1.Derived, Module1.Base)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Base', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x3 = x6
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Derived)' to 'Module1.I123(Of Module1.Derived, Module1.Base)'; this conversion may fail because 'Module1.Derived' is not derived from 'Module1.Unrelated', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x3 = x7
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Base)' to 'Module1.I123(Of Module1.Derived, Module1.Base)'; this conversion may fail because 'Module1.Derived' is not derived from 'Module1.Unrelated', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x3 = x8
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Unrelated)' to 'Module1.I123(Of Module1.Derived, Module1.Base)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Base', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x3 = x9
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Base)' to 'Module1.I123(Of Module1.Derived, Module1.Derived)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Derived', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x4 = x1
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Base)' to 'Module1.I123(Of Module1.Derived, Module1.Derived)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Derived', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x4 = x3
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Unrelated)' to 'Module1.I123(Of Module1.Derived, Module1.Derived)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Derived', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x4 = x5
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Unrelated)' to 'Module1.I123(Of Module1.Derived, Module1.Derived)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Derived', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x4 = x6
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Derived)' to 'Module1.I123(Of Module1.Derived, Module1.Derived)'; this conversion may fail because 'Module1.Derived' is not derived from 'Module1.Unrelated', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x4 = x7
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Base)' to 'Module1.I123(Of Module1.Derived, Module1.Derived)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Derived', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x4 = x8
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Unrelated)' to 'Module1.I123(Of Module1.Derived, Module1.Derived)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Derived', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x4 = x9
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Base)' to 'Module1.I123(Of Module1.Base, Module1.Unrelated)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Unrelated', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x5 = x1
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Derived)' to 'Module1.I123(Of Module1.Base, Module1.Unrelated)'; this conversion may fail because 'Module1.Derived' is not derived from 'Module1.Unrelated', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x5 = x2
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Base)' to 'Module1.I123(Of Module1.Base, Module1.Unrelated)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Unrelated', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x5 = x3
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Derived)' to 'Module1.I123(Of Module1.Base, Module1.Unrelated)'; this conversion may fail because 'Module1.Derived' is not derived from 'Module1.Unrelated', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x5 = x4
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Unrelated)' to 'Module1.I123(Of Module1.Base, Module1.Unrelated)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Derived', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x5 = x6
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Derived)' to 'Module1.I123(Of Module1.Base, Module1.Unrelated)'; this conversion may fail because 'Module1.Derived' is not derived from 'Module1.Unrelated', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x5 = x7
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Base)' to 'Module1.I123(Of Module1.Base, Module1.Unrelated)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Unrelated', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x5 = x8
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Unrelated)' to 'Module1.I123(Of Module1.Base, Module1.Unrelated)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Unrelated', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x5 = x9
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Base)' to 'Module1.I123(Of Module1.Derived, Module1.Unrelated)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Unrelated', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x6 = x1
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Derived)' to 'Module1.I123(Of Module1.Derived, Module1.Unrelated)'; this conversion may fail because 'Module1.Derived' is not derived from 'Module1.Unrelated', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x6 = x2
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Base)' to 'Module1.I123(Of Module1.Derived, Module1.Unrelated)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Unrelated', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x6 = x3
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Derived)' to 'Module1.I123(Of Module1.Derived, Module1.Unrelated)'; this conversion may fail because 'Module1.Derived' is not derived from 'Module1.Unrelated', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x6 = x4
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Derived)' to 'Module1.I123(Of Module1.Derived, Module1.Unrelated)'; this conversion may fail because 'Module1.Derived' is not derived from 'Module1.Unrelated', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x6 = x7
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Base)' to 'Module1.I123(Of Module1.Derived, Module1.Unrelated)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Unrelated', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x6 = x8
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Unrelated)' to 'Module1.I123(Of Module1.Derived, Module1.Unrelated)'; this conversion may fail because 'Module1.Derived' is not derived from 'Module1.Unrelated', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x6 = x9
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Base)' to 'Module1.I123(Of Module1.Unrelated, Module1.Derived)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Derived', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x7 = x1
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Derived)' to 'Module1.I123(Of Module1.Unrelated, Module1.Derived)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Base', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x7 = x2
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Base)' to 'Module1.I123(Of Module1.Unrelated, Module1.Derived)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Derived', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x7 = x3
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Derived)' to 'Module1.I123(Of Module1.Unrelated, Module1.Derived)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Derived', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x7 = x4
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Unrelated)' to 'Module1.I123(Of Module1.Unrelated, Module1.Derived)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Derived', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x7 = x5
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Unrelated)' to 'Module1.I123(Of Module1.Unrelated, Module1.Derived)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Derived', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x7 = x6
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Base)' to 'Module1.I123(Of Module1.Unrelated, Module1.Derived)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Derived', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x7 = x8
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Unrelated)' to 'Module1.I123(Of Module1.Unrelated, Module1.Derived)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Derived', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x7 = x9
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Base)' to 'Module1.I123(Of Module1.Unrelated, Module1.Base)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Base', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x8 = x1
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Derived)' to 'Module1.I123(Of Module1.Unrelated, Module1.Base)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Base', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x8 = x2
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Base)' to 'Module1.I123(Of Module1.Unrelated, Module1.Base)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Derived', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x8 = x3
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Derived)' to 'Module1.I123(Of Module1.Unrelated, Module1.Base)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Derived', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x8 = x4
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Unrelated)' to 'Module1.I123(Of Module1.Unrelated, Module1.Base)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Base', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x8 = x5
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Unrelated)' to 'Module1.I123(Of Module1.Unrelated, Module1.Base)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Base', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x8 = x6
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Unrelated)' to 'Module1.I123(Of Module1.Unrelated, Module1.Base)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Base', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x8 = x9
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Base)' to 'Module1.I123(Of Module1.Unrelated, Module1.Unrelated)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Unrelated', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x9 = x1
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Derived)' to 'Module1.I123(Of Module1.Unrelated, Module1.Unrelated)'; this conversion may fail because 'Module1.Derived' is not derived from 'Module1.Unrelated', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x9 = x2
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Base)' to 'Module1.I123(Of Module1.Unrelated, Module1.Unrelated)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Unrelated', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x9 = x3
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Derived)' to 'Module1.I123(Of Module1.Unrelated, Module1.Unrelated)'; this conversion may fail because 'Module1.Derived' is not derived from 'Module1.Unrelated', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x9 = x4
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base, Module1.Unrelated)' to 'Module1.I123(Of Module1.Unrelated, Module1.Unrelated)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Base', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x9 = x5
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived, Module1.Unrelated)' to 'Module1.I123(Of Module1.Unrelated, Module1.Unrelated)'; this conversion may fail because 'Module1.Unrelated' is not derived from 'Module1.Derived', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T, Out S)'.
        x9 = x6
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Derived)' to 'Module1.I123(Of Module1.Unrelated, Module1.Unrelated)'; this conversion may fail because 'Module1.Derived' is not derived from 'Module1.Unrelated', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x9 = x7
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Unrelated, Module1.Base)' to 'Module1.I123(Of Module1.Unrelated, Module1.Unrelated)'; this conversion may fail because 'Module1.Base' is not derived from 'Module1.Unrelated', as required for the 'Out' generic parameter 'S' in 'Interface I123(Of In T, Out S)'.
        x9 = x8
             ~~
</expected>)
        End Sub

        <Fact>
        Public Sub NoVarianceConversion2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Interface I123(Of In T)
    End Interface

    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class B1
        Implements I123(Of Base)
    End Class

    Structure S1
        Implements I123(Of Base)
    End Structure

    Structure S2
        Implements I123(Of Derived)
    End Structure

    Sub Main()
        Dim x1 As I123(Of Derived) = Nothing
        Dim y1 As I123(Of Base) = Nothing
        Dim z1 As S1

        z1 = x1
        z1 = y1

        Dim z2 As S2

        z2 = x1
        z2 = y1
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.I123(Of Module1.Derived)' cannot be converted to 'Module1.S1'.
        z1 = x1
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Base)' to 'Module1.S1'.
        z1 = y1
             ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.Derived)' to 'Module1.S2'.
        z2 = x1
             ~~
BC30311: Value of type 'Module1.I123(Of Module1.Base)' cannot be converted to 'Module1.S2'.
        z2 = y1
             ~~
</expected>)
        End Sub

        <Fact>
        Public Sub NoVarianceConversion3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Interface I123(Of In T)
    End Interface

    Interface I124(Of Out T)
    End Interface

    Class B1
    End Class

    Structure S1
    End Structure

    Sub Main()
        Dim i1 As I123(Of Object) = Nothing
        Dim x1 As I123(Of B1) = i1
        Dim i2 As I124(Of B1) = Nothing
        Dim x2 As I124(Of Object) = i2

        Dim i3 As I123(Of Object) = Nothing
        Dim x3 As I123(Of S1) = i3
        Dim i4 As I124(Of S1) = Nothing
        Dim x4 As I124(Of Object) = i4
    End Sub

    MustInherit Class Base(Of T, S)
        MustOverride Sub Goo(Of U As T, V As S)()
    End Class

    Class Derived
        Inherits Base(Of B1, S1)

        Public Overrides Sub Goo(Of U As B1, V As S1)()
            Dim i1 As I123(Of Object) = Nothing
            Dim x1 As I123(Of U) = i1
            Dim i2 As I124(Of U) = Nothing
            Dim x2 As I124(Of Object) = i2

            Dim i3 As I123(Of Object) = Nothing
            Dim x3 As I123(Of V) = i3
            Dim i4 As I124(Of V) = Nothing
            Dim x4 As I124(Of Object) = i4
        End Sub
    End Class

    Class Derived2
        Inherits Base(Of B1, S1?)

        Public Overrides Sub Goo(Of U2 As B1, V2 As S1?)()
            Dim i3 As I123(Of Object) = Nothing
            Dim x3 As I123(Of V2) = i3
            Dim i4 As I124(Of V2) = Nothing
            Dim x4 As I124(Of Object) = i4
        End Sub
    End Class

End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Module1.I123(Of Object)' to 'Module1.I123(Of Module1.S1)'; this conversion may fail because 'Module1.S1' is not derived from 'Object', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T)'.
        Dim x3 As I123(Of S1) = i3
                                ~~
BC42016: Implicit conversion from 'Module1.I124(Of Module1.S1)' to 'Module1.I124(Of Object)'; this conversion may fail because 'Module1.S1' is not derived from 'Object', as required for the 'Out' generic parameter 'T' in 'Interface I124(Of Out T)'.
        Dim x4 As I124(Of Object) = i4
                                    ~~
BC42016: Implicit conversion from 'Module1.I123(Of Object)' to 'Module1.I123(Of V As Module1.S1)'; this conversion may fail because 'V' is not derived from 'Object', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T)'.
            Dim x3 As I123(Of V) = i3
                                   ~~
BC42016: Implicit conversion from 'Module1.I124(Of V As Module1.S1)' to 'Module1.I124(Of Object)'; this conversion may fail because 'V' is not derived from 'Object', as required for the 'Out' generic parameter 'T' in 'Interface I124(Of Out T)'.
            Dim x4 As I124(Of Object) = i4
                                        ~~
BC42016: Implicit conversion from 'Module1.I123(Of Object)' to 'Module1.I123(Of V2 As Module1.S1?)'; this conversion may fail because 'V2' is not derived from 'Object', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T)'.
            Dim x3 As I123(Of V2) = i3
                                    ~~
BC42016: Implicit conversion from 'Module1.I124(Of V2 As Module1.S1?)' to 'Module1.I124(Of Object)'; this conversion may fail because 'V2' is not derived from 'Object', as required for the 'Out' generic parameter 'T' in 'Interface I124(Of Out T)'.
            Dim x4 As I124(Of Object) = i4
                                        ~~
</expected>)

            compilation = compilation.WithOptions(compilation.Options.WithOptionStrict(OptionStrict.On))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36755: 'Module1.I123(Of Object)' cannot be converted to 'Module1.I123(Of Module1.S1)' because 'Module1.S1' is not derived from 'Object', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T)'.
        Dim x3 As I123(Of S1) = i3
                                ~~
BC36754: 'Module1.I124(Of Module1.S1)' cannot be converted to 'Module1.I124(Of Object)' because 'Module1.S1' is not derived from 'Object', as required for the 'Out' generic parameter 'T' in 'Interface I124(Of Out T)'.
        Dim x4 As I124(Of Object) = i4
                                    ~~
BC36755: 'Module1.I123(Of Object)' cannot be converted to 'Module1.I123(Of V As Module1.S1)' because 'V' is not derived from 'Object', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T)'.
            Dim x3 As I123(Of V) = i3
                                   ~~
BC36754: 'Module1.I124(Of V As Module1.S1)' cannot be converted to 'Module1.I124(Of Object)' because 'V' is not derived from 'Object', as required for the 'Out' generic parameter 'T' in 'Interface I124(Of Out T)'.
            Dim x4 As I124(Of Object) = i4
                                        ~~
BC36755: 'Module1.I123(Of Object)' cannot be converted to 'Module1.I123(Of V2 As Module1.S1?)' because 'V2' is not derived from 'Object', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T)'.
            Dim x3 As I123(Of V2) = i3
                                    ~~
BC36754: 'Module1.I124(Of V2 As Module1.S1?)' cannot be converted to 'Module1.I124(Of Object)' because 'V2' is not derived from 'Object', as required for the 'Out' generic parameter 'T' in 'Interface I124(Of Out T)'.
            Dim x4 As I124(Of Object) = i4
                                        ~~
</expected>)
        End Sub

        <Fact>
        Public Sub NoVarianceConversion4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Module Module1

    Interface I123(Of In T)
    End Interface

    Interface I124(Of Out T)
    End Interface

    Enum e1 As Integer
        a
    End Enum

    <extension()>
    Sub Goo1(i As I123(Of e1()))
    End Sub

    <extension()>
    Sub Goo2(i As I123(Of Integer()))
    End Sub

    Sub Goo3(i As I124(Of e1()))
    End Sub

    <extension()>
    Sub Goo4(i As I124(Of Integer()))
    End Sub

    Structure S1(Of T)
        Implements I123(Of T), I124(Of T)
    End Structure

    Sub Main()
        Dim i1 As I123(Of e1()) = Nothing
        Dim i2 As I123(Of Integer()) = i1
        i1 = i2

        Dim i3 As I124(Of e1()) = Nothing
        Dim i4 As I124(Of Integer()) = i3
        i3 = i4

        i2.Goo1()
        i1.Goo2()

        i4.Goo3()
        i3.Goo4()

        i1.Goo1()
        i2.Goo2()

        i3.Goo3()
        i4.Goo4()

        Dim s1 As New S1(Of e1())
        Dim s2 As New S1(Of Integer())

        s1.Goo1()
        s1.Goo2()
        s1.Goo3()
        s1.Goo4()

        s2.Goo1()
        s2.Goo2()
        s2.Goo3()
        s2.Goo4()
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef,
                                                                                     {Net40.References.SystemCore},
                                                                                     TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Module1.I123(Of Module1.e1())' to 'Module1.I123(Of Integer())'; this conversion may fail because 'Integer()' is not derived from 'Module1.e1()', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T)'.
        Dim i2 As I123(Of Integer()) = i1
                                       ~~
BC42016: Implicit conversion from 'Module1.I124(Of Integer())' to 'Module1.I124(Of Module1.e1())'; this conversion may fail because 'Integer()' is not derived from 'Module1.e1()', as required for the 'Out' generic parameter 'T' in 'Interface I124(Of Out T)'.
        i3 = i4
             ~~
BC30456: 'Goo1' is not a member of 'Module1.I123(Of Integer())'.
        i2.Goo1()
        ~~~~~~~
BC30456: 'Goo2' is not a member of 'Module1.I123(Of Module1.e1())'.
        i1.Goo2()
        ~~~~~~~
BC30456: 'Goo3' is not a member of 'Module1.I124(Of Integer())'.
        i4.Goo3()
        ~~~~~~~
BC30456: 'Goo4' is not a member of 'Module1.I124(Of Module1.e1())'.
        i3.Goo4()
        ~~~~~~~
BC30456: 'Goo3' is not a member of 'Module1.I124(Of Module1.e1())'.
        i3.Goo3()
        ~~~~~~~
BC30456: 'Goo2' is not a member of 'Module1.S1(Of Module1.e1())'.
        s1.Goo2()
        ~~~~~~~
BC30456: 'Goo3' is not a member of 'Module1.S1(Of Module1.e1())'.
        s1.Goo3()
        ~~~~~~~
BC30456: 'Goo4' is not a member of 'Module1.S1(Of Module1.e1())'.
        s1.Goo4()
        ~~~~~~~
BC30456: 'Goo1' is not a member of 'Module1.S1(Of Integer())'.
        s2.Goo1()
        ~~~~~~~
BC30456: 'Goo3' is not a member of 'Module1.S1(Of Integer())'.
        s2.Goo3()
        ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub VarianceConversionAmbiguity1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Interface I123(Of In T)
    End Interface

    Class B1
    End Class

    Class B2
        Inherits B1
    End Class

    Class B3
        Inherits B2
    End Class

    Class B4
        Implements I123(Of B1), I123(Of B3)
    End Class

    Class B5
        Implements I123(Of B3), I123(Of B1)
    End Class

    Class B6
        Implements I123(Of B2), I123(Of B3), I123(Of B1)
    End Class

    Class B7
        Implements I123(Of B3), I123(Of B2), I123(Of B1)
    End Class

    Class B8
        Implements I123(Of B3), I123(Of B1), I123(Of B2)
    End Class

    Class B9
        Implements I123(Of B1), I123(Of B2)
    End Class

    Class B10
        Implements I123(Of B2), I123(Of B3)
    End Class

    Sub Main()
        Dim x1 As I123(Of B2) = New B4()
        Dim x2 As I123(Of B2) = New B5()
        Dim x3 As I123(Of B2) = New B6()
        Dim x4 As I123(Of B2) = New B7()
        Dim x5 As I123(Of B2) = New B8()
        Dim x6 As I123(Of B3) = New B9()
        Dim x7 As I123(Of B1) = New B10()
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42333: Interface 'Module1.I123(Of Module1.B3)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B1)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B1), I123(Of B3)
                                ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B1)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B3)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B1)
                                ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B3)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B2)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B2), I123(Of B3), I123(Of B1)
                                ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B1)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B2)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B2), I123(Of B3), I123(Of B1)
                                             ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B1)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B3)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B2), I123(Of B3), I123(Of B1)
                                             ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B2)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B3)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B2), I123(Of B1)
                                ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B1)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B2)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B2), I123(Of B1)
                                             ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B1)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B3)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B2), I123(Of B1)
                                             ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B1)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B3)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B1), I123(Of B2)
                                ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B2)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B1)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B1), I123(Of B2)
                                             ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B2)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B3)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B1), I123(Of B2)
                                             ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B2)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B1)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B1), I123(Of B2)
                                ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B3)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B2)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B2), I123(Of B3)
                                ~~~~~~~~~~~
BC42016: Conversion from 'Module1.B4' to 'Module1.I123(Of Module1.B2)' may be ambiguous.
        Dim x1 As I123(Of B2) = New B4()
                                ~~~~~~~~
BC42016: Conversion from 'Module1.B5' to 'Module1.I123(Of Module1.B2)' may be ambiguous.
        Dim x2 As I123(Of B2) = New B5()
                                ~~~~~~~~
BC42016: Conversion from 'Module1.B9' to 'Module1.I123(Of Module1.B3)' may be ambiguous.
        Dim x6 As I123(Of B3) = New B9()
                                ~~~~~~~~
BC42016: Conversion from 'Module1.B10' to 'Module1.I123(Of Module1.B1)' may be ambiguous.
        Dim x7 As I123(Of B1) = New B10()
                                ~~~~~~~~~
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42333: Interface 'Module1.I123(Of Module1.B3)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B1)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B1), I123(Of B3)
                                ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B1)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B3)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B1)
                                ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B3)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B2)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B2), I123(Of B3), I123(Of B1)
                                ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B1)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B2)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B2), I123(Of B3), I123(Of B1)
                                             ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B1)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B3)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B2), I123(Of B3), I123(Of B1)
                                             ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B2)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B3)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B2), I123(Of B1)
                                ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B1)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B2)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B2), I123(Of B1)
                                             ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B1)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B3)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B2), I123(Of B1)
                                             ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B1)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B3)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B1), I123(Of B2)
                                ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B2)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B1)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B1), I123(Of B2)
                                             ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B2)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B3)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B1), I123(Of B2)
                                             ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B2)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B1)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B1), I123(Of B2)
                                ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B3)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B2)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B2), I123(Of B3)
                                ~~~~~~~~~~~
BC36737: Option Strict On does not allow implicit conversions from 'Module1.B4' to 'Module1.I123(Of Module1.B2)' because the conversion is ambiguous.
        Dim x1 As I123(Of B2) = New B4()
                                ~~~~~~~~
BC36737: Option Strict On does not allow implicit conversions from 'Module1.B5' to 'Module1.I123(Of Module1.B2)' because the conversion is ambiguous.
        Dim x2 As I123(Of B2) = New B5()
                                ~~~~~~~~
BC36737: Option Strict On does not allow implicit conversions from 'Module1.B9' to 'Module1.I123(Of Module1.B3)' because the conversion is ambiguous.
        Dim x6 As I123(Of B3) = New B9()
                                ~~~~~~~~
BC36737: Option Strict On does not allow implicit conversions from 'Module1.B10' to 'Module1.I123(Of Module1.B1)' because the conversion is ambiguous.
        Dim x7 As I123(Of B1) = New B10()
                                ~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub VarianceConversionAmbiguity2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Interface I123(Of In T)
    End Interface

    Class B1
    End Class

    Class B2
        Inherits B1
    End Class

    Class B3
        Inherits B2
    End Class

    Class B4
        Inherits B3
    End Class

    Class B5
        Implements I123(Of B3), I123(Of B1), I123(Of B4)
    End Class

    Interface I1
        Inherits I123(Of B1)
    End Interface

    Interface I2
        Inherits I123(Of B1)
    End Interface

    Sub Goo(Of T As {I1, I2})(x As T)
        Dim x22 As I123(Of B2) = x
    End Sub

    Sub Goo(Of T As I123(Of B1), S As {T, I123(Of B3)})(x As S)
        Dim x33 As I123(Of B2) = x
    End Sub

    Sub Main()
        Dim x2 As I123(Of B2) = New B5()
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42333: Interface 'Module1.I123(Of Module1.B1)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B3)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B1), I123(Of B4)
                                ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B4)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B1)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B1), I123(Of B4)
                                             ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B4)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B3)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B1), I123(Of B4)
                                             ~~~~~~~~~~~
BC42016: Conversion from 'S' to 'Module1.I123(Of Module1.B2)' may be ambiguous.
        Dim x33 As I123(Of B2) = x
                                 ~
BC42016: Conversion from 'Module1.B5' to 'Module1.I123(Of Module1.B2)' may be ambiguous.
        Dim x2 As I123(Of B2) = New B5()
                                ~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub VarianceConversionAmbiguity3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Interface I123(Of In T)
    End Interface

    Class B1
    End Class

    Class B2
        Inherits B1
    End Class

    Class B3
        Inherits B2
    End Class

    Class B4
        Implements I123(Of B3), I123(Of B1)
    End Class

    Interface I1(Of Out T)
    End Interface

    Interface I2(Of Out T, Out S)
    End Interface

    Structure S1
        Implements I123(Of B3), I123(Of B1)
    End Structure

    Sub Main()
        Dim x1 As I1(Of B4) = Nothing
        Dim x2 As I1(Of I123(Of B2)) = x1

        Dim x3 As B4 = Nothing
        Dim x4 As I123(Of B2) = x3

        Dim x5 As I2(Of B4, I123(Of B3)) = Nothing
        Dim x6 As I2(Of I123(Of B2), I123(Of B2)) = x5

        Dim x7 As I2(Of I123(Of B3), B4) = Nothing
        Dim x8 As I2(Of I123(Of B2), I123(Of B2)) = x7

        Dim y1 As B4() = Nothing
        Dim y2 As I123(Of B2)() = y1
        y1 = y2

        Dim y3 As I1(Of B4()) = Nothing
        Dim y4 As I1(Of I123(Of B2)()) = y3

        Dim y5 As I1(Of B4)() = Nothing
        Dim y6 As I1(Of I123(Of B2))() = y5

        Dim y7 As I123(Of B1)() = Nothing
        Dim y8 As System.Collections.Generic.IEnumerable(Of I123(Of B2)) = y7

        Dim y71 As B4() = Nothing
        Dim y81 As System.Collections.Generic.IEnumerable(Of I123(Of B2)) = y71

        Dim z1 As S1 = Nothing
        Dim z2 As I123(Of B2) = z1

    End Sub

    Sub Goo(Of T As {I123(Of B3), I123(Of B1)})(x As T)
        Dim p1 As I123(Of B2) = x
    End Sub

    Sub Goo(Of T As {I123(Of B3), I123(Of B1)})(x As T())
        Dim p2 As I123(Of B2)() = x
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42333: Interface 'Module1.I123(Of Module1.B1)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B3)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B1)
                                ~~~~~~~~~~~
BC42333: Interface 'Module1.I123(Of Module1.B1)' is ambiguous with another implemented interface 'Module1.I123(Of Module1.B3)' due to the 'In' and 'Out' parameters in 'Interface I123(Of In T)'.
        Implements I123(Of B3), I123(Of B1)
                                ~~~~~~~~~~~
BC42016: Conversion from 'Module1.I1(Of Module1.B4)' to 'Module1.I1(Of Module1.I123(Of Module1.B2))' may be ambiguous.
        Dim x2 As I1(Of I123(Of B2)) = x1
                                       ~~
BC42016: Conversion from 'Module1.B4' to 'Module1.I123(Of Module1.B2)' may be ambiguous.
        Dim x4 As I123(Of B2) = x3
                                ~~
BC42016: Implicit conversion from 'Module1.I2(Of Module1.B4, Module1.I123(Of Module1.B3))' to 'Module1.I2(Of Module1.I123(Of Module1.B2), Module1.I123(Of Module1.B2))'; this conversion may fail because 'Module1.I123(Of Module1.B3)' is not derived from 'Module1.I123(Of Module1.B2)', as required for the 'Out' generic parameter 'S' in 'Interface I2(Of Out T, Out S)'.
        Dim x6 As I2(Of I123(Of B2), I123(Of B2)) = x5
                                                    ~~
BC42016: Implicit conversion from 'Module1.I2(Of Module1.I123(Of Module1.B3), Module1.B4)' to 'Module1.I2(Of Module1.I123(Of Module1.B2), Module1.I123(Of Module1.B2))'; this conversion may fail because 'Module1.I123(Of Module1.B3)' is not derived from 'Module1.I123(Of Module1.B2)', as required for the 'Out' generic parameter 'T' in 'Interface I2(Of Out T, Out S)'.
        Dim x8 As I2(Of I123(Of B2), I123(Of B2)) = x7
                                                    ~~
BC42016: Conversion from 'Module1.B4()' to 'Module1.I123(Of Module1.B2)()' may be ambiguous.
        Dim y2 As I123(Of B2)() = y1
                                  ~~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.B2)()' to 'Module1.B4()'.
        y1 = y2
             ~~
BC42016: Conversion from 'Module1.I1(Of Module1.B4())' to 'Module1.I1(Of Module1.I123(Of Module1.B2)())' may be ambiguous.
        Dim y4 As I1(Of I123(Of B2)()) = y3
                                         ~~
BC42016: Conversion from 'Module1.I1(Of Module1.B4)()' to 'Module1.I1(Of Module1.I123(Of Module1.B2))()' may be ambiguous.
        Dim y6 As I1(Of I123(Of B2))() = y5
                                         ~~
BC42016: Conversion from 'Module1.B4()' to 'IEnumerable(Of Module1.I123(Of Module1.B2))' may be ambiguous.
        Dim y81 As System.Collections.Generic.IEnumerable(Of I123(Of B2)) = y71
                                                                            ~~~
BC42016: Conversion from 'Module1.S1' to 'Module1.I123(Of Module1.B2)' may be ambiguous.
        Dim z2 As I123(Of B2) = z1
                                ~~
BC42016: Conversion from 'T' to 'Module1.I123(Of Module1.B2)' may be ambiguous.
        Dim p1 As I123(Of B2) = x
                                ~
BC42016: Conversion from 'T()' to 'Module1.I123(Of Module1.B2)()' may be ambiguous.
        Dim p2 As I123(Of B2)() = x
                                  ~
</expected>)

        End Sub

        <Fact>
        Public Sub OverestimateNarrowingConversions1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Module1

    Interface I123(Of In T)
    End Interface

    Interface I124(Of Out T)
    End Interface

    Class B1
    End Class

    Class B2
        Inherits B1
    End Class

    Sub Goo1(Of T As {I123(Of B1), I123(Of S)}, S As Class, Q As {Class, I123(Of B1)})(x As T, y As S, z As I123(Of S), u As Q)
        Dim b2 As B2 = y
        y = b2
        Dim p1 As I123(Of B2) = x
        p1 = z
        p1 = u

        Dim a1 As T() = Nothing
        Dim a2 As Q() = Nothing
        Dim a3 As I123(Of B2)() = a1
        a3 = a2
    End Sub

    Sub Goo2(Of T As {I124(Of B2), I124(Of S)}, S As Class, Q As {Class, I124(Of B2)})(x As T, y As S, z As I124(Of S), u As Q)
        Dim b1 As B1 = y
        y = b1
        Dim p1 As I124(Of B1) = x
        p1 = z
        p1 = u

        Dim a1 As T() = Nothing
        Dim a2 As Q() = Nothing
        Dim a3 As I124(Of B1)() = a1
        a3 = a2
    End Sub

    Sub Goo3(Of T As {I123(Of B1()), I123(Of S())}, S As Class, Q As {Class, I123(Of B1())})(x As T, y As S(), z As I123(Of S()), u As Q)
        Dim b2 As B2() = y
        y = b2
        Dim p1 As I123(Of B2()) = x
        p1 = z
        p1 = u

        Dim a1 As T() = Nothing
        Dim a2 As Q() = Nothing
        Dim a3 As I123(Of B2())() = a1
        a3 = a2
    End Sub

    Sub Goo4(Of T As {I124(Of B2()), I124(Of S())}, S As Class, Q As {Class, I124(Of B2())})(x As T, y As S(), z As I124(Of S()), u As Q)
        Dim b1 As B1() = y
        y = b1
        Dim p1 As I124(Of B1()) = x
        p1 = z
        p1 = u

        Dim a1 As T() = Nothing
        Dim a2 As Q() = Nothing
        Dim a3 As I124(Of B1())() = a1
        a3 = a2
    End Sub

    Sub Goo5(Of T As {I123(Of IEnumerable(Of B1)), I123(Of IEnumerable(Of S))},
                S As Class,
                Q As {Class, I123(Of IEnumerable(Of B1))})(x As T, y As IEnumerable(Of S), z As I123(Of IEnumerable(Of S)), u As Q)
        Dim b2 As B2() = y
        y = b2
        Dim p1 As I123(Of B2()) = x
        p1 = z
        p1 = u

        Dim a1 As T() = Nothing
        Dim a2 As Q() = Nothing
        Dim a3 As I123(Of B2())() = a1
        a3 = a2
    End Sub

    MustInherit Class B3(Of U)
        MustOverride Sub Goo6(Of T As {I123(Of Q()), I123(Of S())}, S As Structure, Q As U, R As Q)()
    End Class

    Structure S1
    End Structure

    Class B4
        Inherits B3(Of S1)

        Public Overrides Sub Goo6(Of T As {I123(Of Q()), I123(Of S())}, S As Structure, Q As S1, R As Q)()
            Dim a As R() = Nothing
            Dim b As Q() = a

            Dim x As T = Nothing
            Dim z As I123(Of R()) = x

            Dim x1 As T() = Nothing
            Dim z1 As I123(Of R())() = x1
        End Sub
    End Class

    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'S' cannot be converted to 'Module1.B2'.
        Dim b2 As B2 = y
                       ~
BC30311: Value of type 'Module1.B2' cannot be converted to 'S'.
        y = b2
            ~~
BC42016: Conversion from 'T' to 'Module1.I123(Of Module1.B2)' may be ambiguous.
        Dim p1 As I123(Of B2) = x
                                ~
BC42016: Implicit conversion from 'Module1.I123(Of S As Class)' to 'Module1.I123(Of Module1.B2)'; this conversion may fail because 'Module1.B2' is not derived from 'S', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T)'.
        p1 = z
             ~
BC42016: Conversion from 'T()' to 'Module1.I123(Of Module1.B2)()' may be ambiguous.
        Dim a3 As I123(Of B2)() = a1
                                  ~~
BC30311: Value of type 'S' cannot be converted to 'Module1.B1'.
        Dim b1 As B1 = y
                       ~
BC30311: Value of type 'Module1.B1' cannot be converted to 'S'.
        y = b1
            ~~
BC42016: Conversion from 'T' to 'Module1.I124(Of Module1.B1)' may be ambiguous.
        Dim p1 As I124(Of B1) = x
                                ~
BC42016: Implicit conversion from 'Module1.I124(Of S As Class)' to 'Module1.I124(Of Module1.B1)'; this conversion may fail because 'S' is not derived from 'Module1.B1', as required for the 'Out' generic parameter 'T' in 'Interface I124(Of Out T)'.
        p1 = z
             ~
BC42016: Conversion from 'T()' to 'Module1.I124(Of Module1.B1)()' may be ambiguous.
        Dim a3 As I124(Of B1)() = a1
                                  ~~
BC30332: Value of type 'S()' cannot be converted to 'Module1.B2()' because 'S' is not derived from 'Module1.B2'.
        Dim b2 As B2() = y
                         ~
BC30332: Value of type 'Module1.B2()' cannot be converted to 'S()' because 'Module1.B2' is not derived from 'S'.
        y = b2
            ~~
BC42016: Conversion from 'T' to 'Module1.I123(Of Module1.B2())' may be ambiguous.
        Dim p1 As I123(Of B2()) = x
                                  ~
BC42016: Implicit conversion from 'Module1.I123(Of S())' to 'Module1.I123(Of Module1.B2())'; this conversion may fail because 'Module1.B2()' is not derived from 'S()', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T)'.
        p1 = z
             ~
BC42016: Conversion from 'T()' to 'Module1.I123(Of Module1.B2())()' may be ambiguous.
        Dim a3 As I123(Of B2())() = a1
                                    ~~
BC30332: Value of type 'S()' cannot be converted to 'Module1.B1()' because 'S' is not derived from 'Module1.B1'.
        Dim b1 As B1() = y
                         ~
BC30332: Value of type 'Module1.B1()' cannot be converted to 'S()' because 'Module1.B1' is not derived from 'S'.
        y = b1
            ~~
BC42016: Conversion from 'T' to 'Module1.I124(Of Module1.B1())' may be ambiguous.
        Dim p1 As I124(Of B1()) = x
                                  ~
BC42016: Implicit conversion from 'Module1.I124(Of S())' to 'Module1.I124(Of Module1.B1())'; this conversion may fail because 'S()' is not derived from 'Module1.B1()', as required for the 'Out' generic parameter 'T' in 'Interface I124(Of Out T)'.
        p1 = z
             ~
BC42016: Conversion from 'T()' to 'Module1.I124(Of Module1.B1())()' may be ambiguous.
        Dim a3 As I124(Of B1())() = a1
                                    ~~
BC42016: Implicit conversion from 'IEnumerable(Of S As Class)' to 'Module1.B2()'.
        Dim b2 As B2() = y
                         ~
BC42016: Implicit conversion from 'Module1.B2()' to 'IEnumerable(Of S As Class)'; this conversion may fail because 'Module1.B2' is not derived from 'S', as required for the 'Out' generic parameter 'T' in 'Interface IEnumerable(Of Out T)'.
        y = b2
            ~~
BC42016: Conversion from 'T' to 'Module1.I123(Of Module1.B2())' may be ambiguous.
        Dim p1 As I123(Of B2()) = x
                                  ~
BC42016: Implicit conversion from 'Module1.I123(Of IEnumerable(Of S))' to 'Module1.I123(Of Module1.B2())'; this conversion may fail because 'Module1.B2()' is not derived from 'IEnumerable(Of S As Class)', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T)'.
        p1 = z
             ~
BC42016: Conversion from 'T()' to 'Module1.I123(Of Module1.B2())()' may be ambiguous.
        Dim a3 As I123(Of B2())() = a1
                                    ~~
BC42016: Conversion from 'T' to 'Module1.I123(Of R())' may be ambiguous.
            Dim z As I123(Of R()) = x
                                    ~
BC42016: Conversion from 'T()' to 'Module1.I123(Of R())()' may be ambiguous.
            Dim z1 As I123(Of R())() = x1
                                       ~~
</expected>)

        End Sub

        <Fact>
        Public Sub OverestimateNarrowingConversions2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Module1

    Interface I123(Of In T)
    End Interface

    Public Sub Goo7(Of T As {I123(Of Q()), I123(Of S())}, S As Structure, Q, R As Q)()
        Dim a As R() = Nothing
        Dim b As Q() = a

        Dim x As T = Nothing
        Dim z As I123(Of R()) = x

        Dim x1 As T() = Nothing
        Dim z1 As I123(Of R())() = x1
    End Sub

    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'R()' to 'Q()'.
        Dim b As Q() = a
                       ~
BC42016: Conversion from 'T' to 'Module1.I123(Of R())' may be ambiguous.
        Dim z As I123(Of R()) = x
                                ~
BC42016: Conversion from 'T()' to 'Module1.I123(Of R())()' may be ambiguous.
        Dim z1 As I123(Of R())() = x1
                                   ~~
</expected>)

        End Sub

        <Fact>
        Public Sub OverestimateNarrowingConversions3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Module1

    Interface I123(Of In T)
    End Interface

    Public Sub Goo8(Of T As {I123(Of Q()), I123(Of S())}, S As Structure, Q, R As {Class, Q})()
        Dim a As R() = Nothing
        Dim b As Q() = a

        Dim x As T = Nothing
        Dim z As I123(Of R()) = x

        Dim x1 As T() = Nothing
        Dim z1 As I123(Of R())() = x1
    End Sub

    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'T()' to 'Module1.I123(Of R())()'.
        Dim z1 As I123(Of R())() = x1
                                   ~~
</expected>)

        End Sub

        <Fact>
        Public Sub OverestimateNarrowingConversions4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Module1

    Interface I123(Of In T)
    End Interface

    Public Sub Goo9(Of T As {I123(Of Q()), I123(Of S())}, S, Q, R As {Structure, Q})()
        Dim a As R() = Nothing
        Dim b As Q() = a

        Dim x As T = Nothing
        Dim z As I123(Of R()) = x

        Dim x1 As T() = Nothing
        Dim z1 As I123(Of R())() = x1
    End Sub

    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'R()' to 'Q()'.
        Dim b As Q() = a
                       ~
BC42016: Conversion from 'T' to 'Module1.I123(Of R())' may be ambiguous.
        Dim z As I123(Of R()) = x
                                ~
BC42016: Conversion from 'T()' to 'Module1.I123(Of R())()' may be ambiguous.
        Dim z1 As I123(Of R())() = x1
                                   ~~
</expected>)

        End Sub

        <Fact>
        Public Sub OverestimateNarrowingConversions5()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Module1

    Interface I123(Of In T)
    End Interface

    Class B5
        Implements I123(Of IEnumerable(Of Integer)), I123(Of UInteger())
    End Class

    Class B6
        Implements I123(Of IEnumerable(Of Integer)), 
                   I123(Of Byte()),
                   I123(Of SByte()),
                   I123(Of Boolean()),
                   I123(Of Int64()),
                   I123(Of UInt64())
    End Class

    Public Sub Main()
        Dim a As Integer() = Nothing
        Dim b As IEnumerable(Of Integer) = a

        Dim c As B5 = Nothing
        Dim d1 As I123(Of Integer()) = c
        Dim d2 As I123(Of TypeCode()) = c
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Conversion from 'Module1.B5' to 'Module1.I123(Of Integer())' may be ambiguous.
        Dim d1 As I123(Of Integer()) = c
                                       ~
BC42016: Conversion from 'Module1.B5' to 'Module1.I123(Of TypeCode())' may be ambiguous.
        Dim d2 As I123(Of TypeCode()) = c
                                        ~
</expected>)

        End Sub

        <Fact>
        Public Sub OverestimateNarrowingConversions6()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Module1

    Interface I123(Of In T)
    End Interface

    Class B5
        Implements I123(Of IEnumerable(Of Int64)), I123(Of UInt64())
    End Class

    Class B6
        Implements I123(Of IEnumerable(Of Int64)), 
                   I123(Of Byte()), 
                   I123(Of SByte()), 
                   I123(Of Boolean()), 
                   I123(Of Int32()), 
                   I123(Of UInt32())
    End Class

    Sub Main()
        Dim a As Integer() = Nothing
        Dim b As IEnumerable(Of Integer) = a

        Dim c As B5 = Nothing
        Dim d1 As I123(Of Int64()) = c

        Dim c2 As B6 = Nothing
        Dim d3 As I123(Of Int64()) = c2
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Conversion from 'Module1.B5' to 'Module1.I123(Of Long())' may be ambiguous.
        Dim d1 As I123(Of Int64()) = c
                                     ~
</expected>)

        End Sub

        <Fact>
        Public Sub OverestimateNarrowingConversions7()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Module1

    Interface I123(Of In T)
    End Interface

    Class B5
        Implements I123(Of IEnumerable(Of SByte)), I123(Of Byte())
    End Class

    Class B6
        Implements I123(Of IEnumerable(Of SByte)), 
                   I123(Of UInt64()), 
                   I123(Of Int64()), 
                   I123(Of Int32()), 
                   I123(Of UInt32())
    End Class

    Sub Main()
        Dim a As Integer() = Nothing
        Dim b As IEnumerable(Of Integer) = a

        Dim c As B5 = Nothing
        Dim d1 As I123(Of SByte()) = c

        Dim c2 As B6 = Nothing
        Dim d3 As I123(Of SByte()) = c2
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Conversion from 'Module1.B5' to 'Module1.I123(Of SByte())' may be ambiguous.
        Dim d1 As I123(Of SByte()) = c
                                     ~
</expected>)

        End Sub

        <Fact>
        Public Sub OverestimateNarrowingConversions8()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Module1

    Interface I123(Of In T)
    End Interface

    Class B5
        Implements I123(Of IEnumerable(Of Boolean)), I123(Of SByte())
    End Class

    Class B6
        Implements I123(Of IEnumerable(Of Boolean)), 
                   I123(Of UInt64()), 
                   I123(Of Int64()), 
                   I123(Of Int32()), 
                   I123(Of UInt32())
    End Class

    Sub Main()
        Dim a As Integer() = Nothing
        Dim b As IEnumerable(Of Integer) = a

        Dim c As B5 = Nothing
        Dim d1 As I123(Of Boolean()) = c

        Dim c2 As B6 = Nothing
        Dim d3 As I123(Of Boolean()) = c2
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Conversion from 'Module1.B5' to 'Module1.I123(Of Boolean())' may be ambiguous.
        Dim d1 As I123(Of Boolean()) = c
                                       ~
</expected>)

        End Sub

        <Fact>
        Public Sub OverestimateNarrowingConversions9()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Module1

    Interface I123(Of In T)
    End Interface

    Class B5(Of T)

        Class B6
            Implements I123(Of IEnumerable(Of Integer)), I123(Of T())
        End Class

        Sub Goo1()
            Dim x As B6 = Nothing
            Dim y As I123(Of Integer()) = x
        End Sub
    End Class

    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Conversion from 'Module1.B5(Of T).B6' to 'Module1.I123(Of Integer())' may be ambiguous.
            Dim y As I123(Of Integer()) = x
                                          ~
</expected>)

        End Sub

        <Fact>
        Public Sub OverestimateNarrowingConversions10()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Module1

    Interface I123(Of In T)
    End Interface

    Class B5(Of T)

        Class B6
            Implements I123(Of Integer()), I123(Of IEnumerable(Of T))
        End Class

        Sub Goo1()
            Dim x As B6 = Nothing
            Dim y As I123(Of T()) = x
        End Sub
    End Class

    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Conversion from 'Module1.B5(Of T).B6' to 'Module1.I123(Of T())' may be ambiguous.
            Dim y As I123(Of T()) = x
                                    ~
</expected>)

        End Sub

        <Fact>
        Public Sub Delegate1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class B1
    End Class

    Class B2
        Inherits B1
    End Class

    Delegate Function D(Of In T, Out S)(x As T) As S

    Sub Main()
        Dim x As D(Of B1, B2) = Function(p As B1) As B2
                                    System.Console.WriteLine("Function(p As B1) As B2 - {0}", p)
                                    Return New B2()
                                End Function
        Dim y As D(Of B2, B1) = x
        y(New B2())

        x = Nothing
        x = y
        x(New B1())
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
Function(p As B1) As B2 - Module1+B2
Function(p As B1) As B2 - Module1+B1
]]>)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
BC42016: Implicit conversion from 'Module1.D(Of Module1.B2, Module1.B1)' to 'Module1.D(Of Module1.B1, Module1.B2)'; this conversion may fail because 'Module1.B1' is not derived from 'Module1.B2', as required for the 'Out' generic parameter 'S' in 'Delegate Function Module1.D(Of In T, Out S)(x As T) As S'.
        x = y
            ~
</expected>)
        End Sub

        <Fact>
        Public Sub Dev10_820752()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Module2
    Class A
    End Class
    Class B
        Inherits A
    End Class
    Class C
        Inherits A
    End Class
    Public Sub Goo(ByVal a1 As A)
    End Sub
    Public Sub goo(ByVal a1 As Action(Of C))
        Console.WriteLine(TypeOf a1 Is 
        Action(Of B))
    End Sub
    Sub Main()
        Goo(New Action(Of A)(AddressOf Goo))
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
True
]]>)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
</expected>)

        End Sub

        <Fact>
        Public Sub Delegate2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Module1

    Class B1
    End Class

    Class B2
    End Class

    Delegate Sub D(Of In T)()
    Delegate Sub D(Of In T, In S)()


    Sub Main()
        Dim x As D(Of B1) = Nothing
        Dim y As D(Of B2) = x

        Dim x1 As D(Of B1, Integer) = Nothing
        Dim y1 As D(Of B2, System.IComparable) = x1
        x1 = y1

        Dim x2 As D(Of Integer, B1) = Nothing
        Dim y2 As D(Of System.IComparable, B2) = x2
        x2 = y2
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Module1.D(Of Module1.B1)' to 'Module1.D(Of Module1.B2)'; this conversion may fail because 'Module1.B2' is not derived from 'Module1.B1', as required for the 'In' generic parameter 'T' in 'Delegate Sub Module1.D(Of In T)()'.
        Dim y As D(Of B2) = x
                            ~
BC36755: 'Module1.D(Of Module1.B1, Integer)' cannot be converted to 'Module1.D(Of Module1.B2, IComparable)' because 'IComparable' is not derived from 'Integer', as required for the 'In' generic parameter 'S' in 'Delegate Sub Module1.D(Of In T, In S)()'.
        Dim y1 As D(Of B2, System.IComparable) = x1
                                                 ~~
BC36755: 'Module1.D(Of Module1.B2, IComparable)' cannot be converted to 'Module1.D(Of Module1.B1, Integer)' because 'Integer' is not derived from 'IComparable', as required for the 'In' generic parameter 'S' in 'Delegate Sub Module1.D(Of In T, In S)()'.
        x1 = y1
             ~~
BC36755: 'Module1.D(Of Integer, Module1.B1)' cannot be converted to 'Module1.D(Of IComparable, Module1.B2)' because 'Module1.B2' is not derived from 'Module1.B1', as required for the 'In' generic parameter 'S' in 'Delegate Sub Module1.D(Of In T, In S)()'.
        Dim y2 As D(Of System.IComparable, B2) = x2
                                                 ~~
BC36755: 'Module1.D(Of IComparable, Module1.B2)' cannot be converted to 'Module1.D(Of Integer, Module1.B1)' because 'Module1.B1' is not derived from 'Module1.B2', as required for the 'In' generic parameter 'S' in 'Delegate Sub Module1.D(Of In T, In S)()'.
        x2 = y2
             ~~
</expected>)

        End Sub

        <Fact>
        Public Sub OverestimateNarrowingConversions11()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Module1

    Delegate Sub D(Of In T)()

    Interface I123(Of In T)
    End Interface

    Public Sub Goo7(Of T As {I123(Of D(Of Q())), I123(Of D(Of S()))}, S As Structure, Q, R As Q)()
        Dim a As D(Of R()) = Nothing
        Dim b As D(Of Q()) = a

        Dim x As T = Nothing
        Dim z As I123(Of D(Of R())) = x

        Dim x1 As T() = Nothing
        Dim z1 As I123(Of D(Of R()))() = x1
    End Sub

    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Module1.D(Of R())' to 'Module1.D(Of Q())'; this conversion may fail because 'Q()' is not derived from 'R()', as required for the 'In' generic parameter 'T' in 'Delegate Sub Module1.D(Of In T)()'.
        Dim b As D(Of Q()) = a
                             ~
BC42016: Conversion from 'T' to 'Module1.I123(Of Module1.D(Of R()))' may be ambiguous.
        Dim z As I123(Of D(Of R())) = x
                                      ~
BC42016: Conversion from 'T()' to 'Module1.I123(Of Module1.D(Of R()))()' may be ambiguous.
        Dim z1 As I123(Of D(Of R()))() = x1
                                         ~~
</expected>)

        End Sub

        <Fact>
        Public Sub OverestimateNarrowingConversions12()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Module1

    Delegate Sub D(Of In T)()

    Interface I123(Of In T)
    End Interface

    Class B1
    End Class

    Class B2
        Inherits B1
    End Class

    Sub Goo1(Of T As {I123(Of D(Of B1)), I123(Of D(Of S))}, S, Q As {Class, I123(Of D(Of B1))})(x As T, y As S, z As I123(Of D(Of S)), u As Q)
        Dim p1 As I123(Of D(Of B2)) = x
        p1 = z
        p1 = u

        Dim a1 As T() = Nothing
        Dim a2 As Q() = Nothing
        Dim a3 As I123(Of D(Of B2))() = a1
        a3 = a2
    End Sub

    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Conversion from 'T' to 'Module1.I123(Of Module1.D(Of Module1.B2))' may be ambiguous.
        Dim p1 As I123(Of D(Of B2)) = x
                                      ~
BC42016: Implicit conversion from 'Module1.I123(Of Module1.D(Of S))' to 'Module1.I123(Of Module1.D(Of Module1.B2))'; this conversion may fail because 'Module1.D(Of Module1.B2)' is not derived from 'Module1.D(Of S)', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T)'.
        p1 = z
             ~
BC42016: Implicit conversion from 'Q' to 'Module1.I123(Of Module1.D(Of Module1.B2))'; this conversion may fail because 'Module1.D(Of Module1.B2)' is not derived from 'Module1.D(Of Module1.B1)', as required for the 'In' generic parameter 'T' in 'Interface I123(Of In T)'.
        p1 = u
             ~
BC42016: Conversion from 'T()' to 'Module1.I123(Of Module1.D(Of Module1.B2))()' may be ambiguous.
        Dim a3 As I123(Of D(Of B2))() = a1
                                        ~~
BC42016: Implicit conversion from 'Q()' to 'Module1.I123(Of Module1.D(Of Module1.B2))()'.
        a3 = a2
             ~~
</expected>)

        End Sub

        <Fact>
        Public Sub Delegate3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class B1
    End Class

    Class B2
        Inherits B1
    End Class

    Delegate Function D(Of T, S)(x As T) As S

    Sub Main()
        Dim x As D(Of B1, B2) = Function(p As B1) As B2
                                    System.Console.WriteLine("Function(p As B1) As B2 - {0}", p)
                                    Return New B2()
                                End Function
        Dim y As D(Of B2, B1) = x
        y(New B2())

        x = Nothing
        x = y
        x(New B1())
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36757: 'Module1.D(Of Module1.B1, Module1.B2)' cannot be converted to 'Module1.D(Of Module1.B2, Module1.B1)'. Consider changing the 'S' in the definition of 'Delegate Function Module1.D(Of T, S)(x As T) As S' to an Out type parameter, 'Out S'.
        Dim y As D(Of B2, B1) = x
                                ~
BC36757: 'Module1.D(Of Module1.B2, Module1.B1)' cannot be converted to 'Module1.D(Of Module1.B1, Module1.B2)'. Consider changing the 'T' in the definition of 'Delegate Function Module1.D(Of T, S)(x As T) As S' to an Out type parameter, 'Out T'.
        x = y
            ~
</expected>)
        End Sub

        <Fact>
        Public Sub Delegate4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class B1
    End Class

    Class B2
        Inherits B1
    End Class

    Delegate Function D(Of T, S)(x As T) As S

    Sub Main()
        Dim y As D(Of B1, B1) = Nothing
        Dim x As D(Of B1, B2) = y
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36758: 'Module1.D(Of Module1.B1, Module1.B1)' cannot be converted to 'Module1.D(Of Module1.B1, Module1.B2)'. Consider changing the 'S' in the definition of 'Delegate Function Module1.D(Of T, S)(x As T) As S' to an In type parameter, 'In S'.
        Dim x As D(Of B1, B2) = y
                                ~
</expected>)
        End Sub

        <Fact>
        Public Sub VarianceConversionSuggestion1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System
Imports System.Collections
Imports System.Collections.Generic

Module Module1

    Class B1
    End Class

    Class B2
        Inherits B1
    End Class

    Class B3
        Inherits List(Of B2)
    End Class

    Class B4
        Inherits List(Of B1)
    End Class

    Class B5
        Inherits System.Collections.ObjectModel.Collection(Of B2)
    End Class

    Class B6
        Inherits System.Collections.ObjectModel.Collection(Of B1)
    End Class

    Class B7
        Inherits System.Collections.ObjectModel.ReadOnlyCollection(Of B2)

        Sub New()
            MyBase.New(Nothing)
        End Sub
    End Class

    Class B8
        Inherits System.Collections.ObjectModel.ReadOnlyCollection(Of B1)

        Sub New()
            MyBase.New(Nothing)
        End Sub
    End Class

    Class B9(Of T)
        Implements IList(Of T)

        Public Sub Add(item As T) Implements ICollection(Of T).Add
        End Sub

        Public Sub Clear() Implements ICollection(Of T).Clear
        End Sub

        Public Function Contains(item As T) As Boolean Implements ICollection(Of T).Contains
            Return Nothing
        End Function

        Public Sub CopyTo(array() As T, arrayIndex As Integer) Implements ICollection(Of T).CopyTo
        End Sub

        Public ReadOnly Property Count As Integer Implements ICollection(Of T).Count
            Get
                Return Nothing
            End Get
        End Property

        Public ReadOnly Property IsReadOnly As Boolean Implements ICollection(Of T).IsReadOnly
            Get
                Return Nothing
            End Get
        End Property

        Public Function Remove(item As T) As Boolean Implements ICollection(Of T).Remove
            Return Nothing
        End Function

        Public Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
            Return Nothing
        End Function

        Public Function IndexOf(item As T) As Integer Implements IList(Of T).IndexOf
            Return Nothing
        End Function

        Public Sub Insert(index As Integer, item As T) Implements IList(Of T).Insert
        End Sub

        Default Public Property Item(index As Integer) As T Implements IList(Of T).Item
            Get
                Return Nothing
            End Get
            Set(value As T)
            End Set
        End Property

        Public Sub RemoveAt(index As Integer) Implements IList(Of T).RemoveAt
        End Sub

        Public Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
            Return Nothing
        End Function
    End Class

    Class B10(Of T)
        Implements ICollection(Of T)

        Public Sub Add(item As T) Implements ICollection(Of T).Add
        End Sub

        Public Sub Clear() Implements ICollection(Of T).Clear
        End Sub

        Public Function Contains(item As T) As Boolean Implements ICollection(Of T).Contains
            Return Nothing
        End Function

        Public Sub CopyTo(array() As T, arrayIndex As Integer) Implements ICollection(Of T).CopyTo
        End Sub

        Public ReadOnly Property Count As Integer Implements ICollection(Of T).Count
            Get
                Return Nothing
            End Get
        End Property

        Public ReadOnly Property IsReadOnly As Boolean Implements ICollection(Of T).IsReadOnly
            Get
                Return Nothing
            End Get
        End Property

        Public Function Remove(item As T) As Boolean Implements ICollection(Of T).Remove
            Return Nothing
        End Function

        Public Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
            Return Nothing
        End Function

        Public Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
            Return Nothing
        End Function
    End Class

    Class B11(Of T, U)
        Inherits B9(Of T)
        Implements ICollection(Of U)

        Public Sub Add1(item As U) Implements ICollection(Of U).Add
        End Sub

        Public Sub Clear1() Implements ICollection(Of U).Clear
        End Sub

        Public Function Contains1(item As U) As Boolean Implements ICollection(Of U).Contains
            Return Nothing
        End Function

        Public Sub CopyTo1(array() As U, arrayIndex As Integer) Implements ICollection(Of U).CopyTo
        End Sub

        Public ReadOnly Property Count1 As Integer Implements ICollection(Of U).Count
            Get
                Return Nothing
            End Get
        End Property

        Public ReadOnly Property IsReadOnly1 As Boolean Implements ICollection(Of U).IsReadOnly
            Get
                Return Nothing
            End Get
        End Property

        Public Function Remove1(item As U) As Boolean Implements ICollection(Of U).Remove
            Return Nothing
        End Function

        Public Function GetEnumerator2() As IEnumerator(Of U) Implements IEnumerable(Of U).GetEnumerator
            Return Nothing
        End Function
    End Class

    Sub Main()

        Dim a As List(Of B1) = New B3()
        Dim b As List(Of B2) = New B4()

        Dim c As System.Collections.ObjectModel.Collection(Of B1) = New B5()
        Dim d As System.Collections.ObjectModel.Collection(Of B2) = New B6()

        Dim e As System.Collections.ObjectModel.ReadOnlyCollection(Of B1) = New B7()
        Dim f As System.Collections.ObjectModel.ReadOnlyCollection(Of B2) = New B8()

        Dim g As IList(Of B1) = New B9(Of B2)()
        Dim h As IList(Of B2) = New B9(Of B1)()

        g = New B10(Of B2)()
        h = New B10(Of B1)()

        Dim i As ICollection(Of B1) = New B9(Of B2)()
        Dim j As ICollection(Of B2) = New B9(Of B1)()

        i = New B10(Of B2)()
        j = New B10(Of B1)()

        i = New B11(Of B2, B3)()
        j = New B11(Of B1, B3)()

        i = New B11(Of B2, B2)()
        j = New B11(Of B3, B3)()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36756: 'Module1.B3' cannot be converted to 'List(Of Module1.B1)'. Consider using 'IEnumerable(Of Module1.B1)' instead.
        Dim a As List(Of B1) = New B3()
                               ~~~~~~~~
BC30311: Value of type 'Module1.B4' cannot be converted to 'List(Of Module1.B2)'.
        Dim b As List(Of B2) = New B4()
                               ~~~~~~~~
BC36756: 'Module1.B5' cannot be converted to 'Collection(Of Module1.B1)'. Consider using 'IEnumerable(Of Module1.B1)' instead.
        Dim c As System.Collections.ObjectModel.Collection(Of B1) = New B5()
                                                                    ~~~~~~~~
BC30311: Value of type 'Module1.B6' cannot be converted to 'Collection(Of Module1.B2)'.
        Dim d As System.Collections.ObjectModel.Collection(Of B2) = New B6()
                                                                    ~~~~~~~~
BC36756: 'Module1.B7' cannot be converted to 'ReadOnlyCollection(Of Module1.B1)'. Consider using 'IEnumerable(Of Module1.B1)' instead.
        Dim e As System.Collections.ObjectModel.ReadOnlyCollection(Of B1) = New B7()
                                                                            ~~~~~~~~
BC30311: Value of type 'Module1.B8' cannot be converted to 'ReadOnlyCollection(Of Module1.B2)'.
        Dim f As System.Collections.ObjectModel.ReadOnlyCollection(Of B2) = New B8()
                                                                            ~~~~~~~~
BC42016: 'Module1.B9(Of Module1.B2)' cannot be converted to 'IList(Of Module1.B1)'. Consider using 'IEnumerable(Of Module1.B1)' instead.
        Dim g As IList(Of B1) = New B9(Of B2)()
                                ~~~~~~~~~~~~~~~
BC42016: Implicit conversion from 'Module1.B9(Of Module1.B1)' to 'IList(Of Module1.B2)'.
        Dim h As IList(Of B2) = New B9(Of B1)()
                                ~~~~~~~~~~~~~~~
BC42016: Implicit conversion from 'Module1.B10(Of Module1.B2)' to 'IList(Of Module1.B1)'.
        g = New B10(Of B2)()
            ~~~~~~~~~~~~~~~~
BC42016: Implicit conversion from 'Module1.B10(Of Module1.B1)' to 'IList(Of Module1.B2)'.
        h = New B10(Of B1)()
            ~~~~~~~~~~~~~~~~
BC42016: 'Module1.B9(Of Module1.B2)' cannot be converted to 'ICollection(Of Module1.B1)'. Consider using 'IEnumerable(Of Module1.B1)' instead.
        Dim i As ICollection(Of B1) = New B9(Of B2)()
                                      ~~~~~~~~~~~~~~~
BC42016: Implicit conversion from 'Module1.B9(Of Module1.B1)' to 'ICollection(Of Module1.B2)'.
        Dim j As ICollection(Of B2) = New B9(Of B1)()
                                      ~~~~~~~~~~~~~~~
BC42016: 'Module1.B10(Of Module1.B2)' cannot be converted to 'ICollection(Of Module1.B1)'. Consider using 'IEnumerable(Of Module1.B1)' instead.
        i = New B10(Of B2)()
            ~~~~~~~~~~~~~~~~
BC42016: Implicit conversion from 'Module1.B10(Of Module1.B1)' to 'ICollection(Of Module1.B2)'.
        j = New B10(Of B1)()
            ~~~~~~~~~~~~~~~~
BC42016: Implicit conversion from 'Module1.B11(Of Module1.B2, Module1.B3)' to 'ICollection(Of Module1.B1)'.
        i = New B11(Of B2, B3)()
            ~~~~~~~~~~~~~~~~~~~~
BC42016: Implicit conversion from 'Module1.B11(Of Module1.B1, Module1.B3)' to 'ICollection(Of Module1.B2)'.
        j = New B11(Of B1, B3)()
            ~~~~~~~~~~~~~~~~~~~~
BC42016: 'Module1.B11(Of Module1.B2, Module1.B2)' cannot be converted to 'ICollection(Of Module1.B1)'. Consider using 'IEnumerable(Of Module1.B1)' instead.
        i = New B11(Of B2, B2)()
            ~~~~~~~~~~~~~~~~~~~~
BC42016: Implicit conversion from 'Module1.B11(Of Module1.B3, Module1.B3)' to 'ICollection(Of Module1.B2)'.
        j = New B11(Of B3, B3)()
            ~~~~~~~~~~~~~~~~~~~~
</expected>)

            compilation = compilation.WithOptions(compilation.Options.WithOptionStrict(OptionStrict.On))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36756: 'Module1.B3' cannot be converted to 'List(Of Module1.B1)'. Consider using 'IEnumerable(Of Module1.B1)' instead.
        Dim a As List(Of B1) = New B3()
                               ~~~~~~~~
BC30311: Value of type 'Module1.B4' cannot be converted to 'List(Of Module1.B2)'.
        Dim b As List(Of B2) = New B4()
                               ~~~~~~~~
BC36756: 'Module1.B5' cannot be converted to 'Collection(Of Module1.B1)'. Consider using 'IEnumerable(Of Module1.B1)' instead.
        Dim c As System.Collections.ObjectModel.Collection(Of B1) = New B5()
                                                                    ~~~~~~~~
BC30311: Value of type 'Module1.B6' cannot be converted to 'Collection(Of Module1.B2)'.
        Dim d As System.Collections.ObjectModel.Collection(Of B2) = New B6()
                                                                    ~~~~~~~~
BC36756: 'Module1.B7' cannot be converted to 'ReadOnlyCollection(Of Module1.B1)'. Consider using 'IEnumerable(Of Module1.B1)' instead.
        Dim e As System.Collections.ObjectModel.ReadOnlyCollection(Of B1) = New B7()
                                                                            ~~~~~~~~
BC30311: Value of type 'Module1.B8' cannot be converted to 'ReadOnlyCollection(Of Module1.B2)'.
        Dim f As System.Collections.ObjectModel.ReadOnlyCollection(Of B2) = New B8()
                                                                            ~~~~~~~~
BC36756: 'Module1.B9(Of Module1.B2)' cannot be converted to 'IList(Of Module1.B1)'. Consider using 'IEnumerable(Of Module1.B1)' instead.
        Dim g As IList(Of B1) = New B9(Of B2)()
                                ~~~~~~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Module1.B9(Of Module1.B1)' to 'IList(Of Module1.B2)'.
        Dim h As IList(Of B2) = New B9(Of B1)()
                                ~~~~~~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Module1.B10(Of Module1.B2)' to 'IList(Of Module1.B1)'.
        g = New B10(Of B2)()
            ~~~~~~~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Module1.B10(Of Module1.B1)' to 'IList(Of Module1.B2)'.
        h = New B10(Of B1)()
            ~~~~~~~~~~~~~~~~
BC36756: 'Module1.B9(Of Module1.B2)' cannot be converted to 'ICollection(Of Module1.B1)'. Consider using 'IEnumerable(Of Module1.B1)' instead.
        Dim i As ICollection(Of B1) = New B9(Of B2)()
                                      ~~~~~~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Module1.B9(Of Module1.B1)' to 'ICollection(Of Module1.B2)'.
        Dim j As ICollection(Of B2) = New B9(Of B1)()
                                      ~~~~~~~~~~~~~~~
BC36756: 'Module1.B10(Of Module1.B2)' cannot be converted to 'ICollection(Of Module1.B1)'. Consider using 'IEnumerable(Of Module1.B1)' instead.
        i = New B10(Of B2)()
            ~~~~~~~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Module1.B10(Of Module1.B1)' to 'ICollection(Of Module1.B2)'.
        j = New B10(Of B1)()
            ~~~~~~~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Module1.B11(Of Module1.B2, Module1.B3)' to 'ICollection(Of Module1.B1)'.
        i = New B11(Of B2, B3)()
            ~~~~~~~~~~~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Module1.B11(Of Module1.B1, Module1.B3)' to 'ICollection(Of Module1.B2)'.
        j = New B11(Of B1, B3)()
            ~~~~~~~~~~~~~~~~~~~~
BC36756: 'Module1.B11(Of Module1.B2, Module1.B2)' cannot be converted to 'ICollection(Of Module1.B1)'. Consider using 'IEnumerable(Of Module1.B1)' instead.
        i = New B11(Of B2, B2)()
            ~~~~~~~~~~~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Module1.B11(Of Module1.B3, Module1.B3)' to 'ICollection(Of Module1.B2)'.
        j = New B11(Of B3, B3)()
            ~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub VarianceConversionSuggestion2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1

    Class B1
    End Class

    Class B2
        Inherits B1
    End Class

    Interface I123(Of T, S)
    End Interface

    Sub Main()
        Dim y As I123(Of B1, B1) = Nothing
        Dim x As I123(Of B1, B2) = y

        Dim y1 As I123(Of B2, B1) = Nothing
        Dim x1 As I123(Of B1, B2) = y1
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: 'Module1.I123(Of Module1.B1, Module1.B1)' cannot be converted to 'Module1.I123(Of Module1.B1, Module1.B2)'. Consider changing the 'S' in the definition of 'Interface I123(Of T, S)' to an In type parameter, 'In S'.
        Dim x As I123(Of B1, B2) = y
                                   ~
BC42016: 'Module1.I123(Of Module1.B2, Module1.B1)' cannot be converted to 'Module1.I123(Of Module1.B1, Module1.B2)'. Consider changing the 'T' in the definition of 'Interface I123(Of T, S)' to an Out type parameter, 'Out T'.
        Dim x1 As I123(Of B1, B2) = y1
                                    ~~
</expected>)
        End Sub

        <Fact>
        Public Sub InfiniteRecursion1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Interface W(Of In U) : End Interface
Interface X(Of T) : Inherits W(Of W(Of X(Of X(Of T)))) : End Interface
Class T2 : Implements X(Of Double) : End Class
Module Test2
    Sub Test()
        Dim t As X(Of Double) = New T2
        Dim u As W(Of X(Of String)) = t  ' BC36755/42106 underspecified: we don't know whether the CLR will allow this
        Dim v As W(Of X(Of String)) = CType(t, W(Of X(Of String)))
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'X(Of Double)' to 'W(Of X(Of String))'; this conversion may fail because 'X(Of String)' is not derived from 'W(Of X(Of X(Of Double)))', as required for the 'In' generic parameter 'U' in 'Interface W(Of In U)'.
        Dim u As W(Of X(Of String)) = t  ' BC36755/42106 underspecified: we don't know whether the CLR will allow this
                                      ~
</expected>)

            compilation = compilation.WithOptions(compilation.Options.WithOptionStrict(OptionStrict.On))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36755: 'X(Of Double)' cannot be converted to 'W(Of X(Of String))' because 'X(Of String)' is not derived from 'W(Of X(Of X(Of Double)))', as required for the 'In' generic parameter 'U' in 'Interface W(Of In U)'.
        Dim u As W(Of X(Of String)) = t  ' BC36755/42106 underspecified: we don't know whether the CLR will allow this
                                      ~
</expected>)
        End Sub

        <Fact>
        Public Sub ConstraintsOnGenericMethod()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict On
Imports System
Imports System.Collections.Generic
Module Module1
    Class B1
    End Class
    Class B2
        Inherits B1
    End Class
    Class Program
        Shared Function Goo(Of T As U, U)(ByVal arg As T) As U
            Console.WriteLine(TypeOf arg Is IEnumerable(Of B2))
            Return arg
        End Function

        Shared Sub Main()
            Dim btnList As IEnumerable(Of B2) = New List(Of B2)()
            'This is allowed because it satisfies the constraint IEnumerable(Of B1) AS IEnumerable(Of B2)
            Dim _ctrlCol As IEnumerable(Of B1) = Goo(Of IEnumerable(Of B2), IEnumerable(Of B1))(btnList)
        End Sub
    End Class
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom),
                             expectedOutput:=
            <![CDATA[
True
]]>)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
</expected>)

        End Sub

        <Fact>
        Public Sub UserDefinedConversions1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict On
Imports System
Module Module1
    Class A
    End Class
    Class B
        Inherits A
    End Class
    Class C
        Overloads Shared Widening Operator CType(ByVal arg As C) As Func(Of A)
            Return Function() New A
        End Operator
    End Class
    Class D
        Inherits C
        Overloads Shared Widening Operator CType(ByVal arg As D) As Func(Of B)
            Return Function() New B
        End Operator
    End Class
    Sub Main()
        'Variance ambiguity error, because there will be another widening path(D-->Func(Of B)--[Covariance]-->Func(Of A)
        Dim _func As Func(Of A) = New D()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Module1.D' cannot be converted to 'Func(Of Module1.A)'.
        Dim _func As Func(Of A) = New D()
                                  ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub UserDefinedConversions2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict On
Imports System
Module Module1
    Class A
    End Class
    Class B
        Inherits A
    End Class
    Class C
        Overloads Shared Narrowing Operator CType(ByVal arg As C) As Func(Of A)
            Return Function() New A
        End Operator

        Overloads Shared Widening Operator CType(ByVal arg As C) As Func(Of B)
            Console.WriteLine("T1->Func(Of B)")
            Return Function() New B
        End Operator
    End Class

    Sub Main()
        'The conversion will succeed using Variance->Func(Of B)-[Covariance]-Func(Of A)
        Dim _func As Func(Of A) = New C()
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On),
                      expectedOutput:=
            <![CDATA[
T1->Func(Of B)
]]>)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
</expected>)
        End Sub

        <WorkItem(545815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545815")>
        <Fact>
        Public Sub Bug14483()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict On
 
Imports System

Module Module1
 
    Sub Main()
 
        Dim ao As Action(Of Object) = Sub(a As Object)
                                      End Sub
        Dim ad As Action(Of D) = ao
        Console.WriteLine(ad.GetType)
 
        ' One Level Nesting
        ' ContraVariance nested in Variance
        Dim fao As Func(Of Action(Of Object)) = Function() ao
        Dim fad As Func(Of Action(Of D)) = fao
 
        Dim fac_1 As Func(Of Action(Of C)) = CType(fad, Func(Of Action(Of C)))
        Console.WriteLine(fac_1.GetType)
 
        ' ContraVariance nested in ContraVariance
        ' We only test this with Strict off, 
        ' since Dim aad As Action(Of Action(Of D)) = aao is narrowing conversion
 
        Dim aao As Action(Of Action(Of Object)) = ao
        Dim aad As Action(Of Action(Of D)) = CType(aao, Action(Of Action(Of D)))
        Dim aac As Action(Of Action(Of C)) = CType(aad, Action(Of Action(Of C)))
        Console.WriteLine(aac.GetType)
 
        ' Two Level Nesting
        ' ContraVariance Nested in Variance
        Dim ffao As Func(Of Func(Of Action(Of Object))) = Function() fao
        Dim ffad As Func(Of Func(Of Action(Of D))) = ffao
 
        Dim ffac_1 As Func(Of Func(Of Action(Of C))) = CType(ffad, Func(Of Func(Of Action(Of C))))
        Console.WriteLine(ffac_1.GetType)
    End Sub
 
    Sub Goo(ByVal f As C)
        Console.WriteLine(f.GetType)
    End Sub
 
    Class C : End Class
 
    Class D : End Class
 
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                      expectedOutput:=
            <![CDATA[
System.Action`1[System.Object]
System.Func`1[System.Action`1[System.Object]]
System.Action`1[System.Object]
System.Func`1[System.Func`1[System.Action`1[System.Object]]]
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

    End Class

End Namespace
