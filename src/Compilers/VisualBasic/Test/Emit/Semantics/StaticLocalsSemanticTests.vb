' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class StaticLocalsSemanticTests
        Inherits BasicTestBase

        <Fact, WorkItem(15925, "DevDiv_Projects/Roslyn")>
        Public Sub Semantic_StaticLocalDeclarationInSub()
            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Public Class Module1
    Public Shared Sub Main()
        StaticLocalInSub()
        StaticLocalInSub()
    End Sub

    Shared Sub StaticLocalInSub()
        Static SLItem1 = 1
        Console.WriteLine("StaticLocalInSub")
        Console.WriteLine(SLItem1.GetType.ToString) 'Type Inferred
        Console.WriteLine(SLItem1.ToString) 'Value
        SLItem1 += 1
    End Sub
End Class
    </file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[StaticLocalInSub
System.Int32
1
StaticLocalInSub
System.Int32
2]]>)
        End Sub

        <Fact, WorkItem(15925, "DevDiv_Projects/Roslyn")>
        Public Sub Semantic_StaticLocalDeclarationInSubModule()
            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
    Public Sub Main()
        StaticLocalInSub()
        StaticLocalInSub()
    End Sub

    Sub StaticLocalInSub()
        Static SLItem1 = 1
        Console.WriteLine("StaticLocalInSub")
        Console.WriteLine(SLItem1.GetType.ToString) 'Type Inferred
        Console.WriteLine(SLItem1.ToString) 'Value
        SLItem1 += 1
    End Sub
End Module 
    </file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[StaticLocalInSub
System.Int32
1
StaticLocalInSub
System.Int32
2]]>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalDeclarationInFunction()
            'Using different Type as well

            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
        Public Sub Main()
            Dim x1 = StaticLocalInFunction()
            x1 = StaticLocalInFunction()
        End Sub

        Function StaticLocalInFunction() As Long
            Static SLItem1 As Long = 1  'Type Character
            Console.WriteLine("StaticLocalInFunction")
            Console.WriteLine(SLItem1.GetType.ToString) 'Type Inferred
            Console.WriteLine(SLItem1.ToString) 'Value
            SLItem1 += 1
            Return SLItem1
        End Function
End Module
    </file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[StaticLocalInFunction
System.Int64
1
StaticLocalInFunction
System.Int64
2]]>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalDeclarationReferenceType()
            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Module Module1
        Sub Main()
            StaticLocalRefType()
            StaticLocalRefType()
            StaticLocalRefType()
        End Sub

       Sub StaticLocalRefType()
            Static SLItem1 As String = ""
            SLItem1 &amp;= "*"
            Console.WriteLine("StaticLocalRefType")
            Console.WriteLine(SLItem1.GetType.ToString) 'Type Inferred
            Console.WriteLine(SLItem1.ToString) 'Value
        End Sub
End Module
    </file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[StaticLocalRefType
System.String
*
StaticLocalRefType
System.String
**
StaticLocalRefType
System.String
***]]>)

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalDeclarationUserDefinedClass()
            'With a user defined reference type (class) this should only initialize on initial invocation and then
            'increment each time
            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">        
Imports System

Public Module Module1
    Public Sub Main()
        StaticLocalUserDefinedType()
        StaticLocalUserDefinedType()
        StaticLocalUserDefinedType()
    End Sub

    Sub StaticLocalUserDefinedType()
        Static SLi As Integer = 1
        Static SLItem1 As TestUDClass = New TestUDClass With {.ABC = SLi}

        SLItem1.ABC = SLi
        SLi += 1
        Console.WriteLine("StaticLocalUserDefinedType")
        Console.WriteLine(SLItem1.GetType.ToString) 'Type Inferred
        Console.WriteLine(SLItem1.ToString) 'Value
        Console.WriteLine(SLItem1.ABC.ToString) 'Value
    End Sub
End Module

Class TestUDClass
    Public Property ABC As Integer = 1
End Class
    </file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[StaticLocalUserDefinedType
TestUDClass
TestUDClass
1
StaticLocalUserDefinedType
TestUDClass
TestUDClass
2
StaticLocalUserDefinedType
TestUDClass
TestUDClass
3
]]>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalDeclaration_InGenericType()
            'Can declare in generic type, just not in generic method

            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
    Public Sub Main()
        Dim x As New UDTest(Of Integer)
        x.Goo()
        x.Goo()
        x.Goo()
    End Sub
End Module

Public Class UDTest(Of t)
    Public Sub Goo()
        Static SLItem As Integer = 1
        Console.WriteLine(SLItem.ToString)
        SLItem += 1
    End Sub
End Class
    </file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1
2
3]]>)

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalDeclaration_Keyword_NameClashInType()
            'declare Escaped identifier called static 

            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
        <compilation>
            <file name="a.vb">
Imports System

Public Module Module1
    Public Sub Main()
        AvoidingNameConflicts()
        AvoidingNameConflicts()
        AvoidingNameConflicts()
    End Sub

    Sub AvoidingNameConflicts()
        Static [Static] As Integer = 1
        Console.WriteLine([Static])
        [Static] += 1
    End Sub
End Module

    </file>
        </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1
2
3]]>)

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalDeclaration_Keyword_NameClashEscaped()
            'declare identifier and type called static both of which need to be escaped along with static
            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
    Public Sub Main()
        AvoidingNameConflicts()
        AvoidingNameConflicts()
        AvoidingNameConflicts()
    End Sub

    Sub AvoidingNameConflicts()
        Static [Static] As [Static] = New [Static] With {.ABC = 1}
        Console.WriteLine([Static].ABC)
        [Static].ABC += 1
    End Sub
End Module

Class [Static]
    Public Property ABC As Integer
End Class
    </file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1
2
3]]>)
        End Sub


        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalDeclaration_Keyword_NameClash_Property_NoEscapingRequired()
            'declare Property called static doesnt need escaping because of preceding .
            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
    Public Sub Main()
        AvoidingNameConflicts()
        AvoidingNameConflicts()
        AvoidingNameConflicts()
    End Sub

    Sub AvoidingNameConflicts()
        Static S1 As [Static] = New [Static] With {.Static = 1}
        Console.WriteLine(S1.Static)
        S1.Static += 1
    End Sub
End Module

Class [Static]
    Public Property [Static] As Integer
End Class
    </file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1
2
3]]>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalDeclaration_LateBound()
            ' test late bind
            ' call ToString() on object defeat the purpose
            Dim currCulture = Threading.Thread.CurrentThread.CurrentCulture
            Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture
            Try
                'Declare static local which is late bound
                Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
    Public Sub Main()
        WithTypeObject(1)
        WithTypeObject(2)
        WithTypeObject(3)
    End Sub

    Sub WithTypeObject(x As Integer)
        Static sl1 As Object = 1

        Console.WriteLine("Prior:" &amp; sl1)
        Select Case x
            Case 1
                sl1 = 1
            Case 2
                sl1 = "Test"
            Case Else
                sl1 = 5.5
        End Select

        Console.WriteLine("After:" &amp; sl1)
    End Sub
End Module
    </file>
    </compilation>, TestOptions.ReleaseExe)

                CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[Prior:1
After:1
Prior:1
After:Test
Prior:Test
After:5.5]]>)

            Catch ex As Exception
                Assert.Null(ex)
            Finally
                Threading.Thread.CurrentThread.CurrentCulture = currCulture
            End Try
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalDeclaration_WithTypeCharacters()
            'Declare static local using type identifier        
            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
    Public Sub Main()
        WithTypeCharacters()
        WithTypeCharacters()
        WithTypeCharacters()
    End Sub

    Sub WithTypeCharacters()
        Static sl1% = 1 'integer
        Static sl2&amp; = 1 'Long
        Static sl3@ = 1 'Decimal
        Static sl4! = 1 'Single
        Static sl5# = 1 'Double
        Static sl6$ = "" 'String

        Console.WriteLine(sl1)
        Console.WriteLine(sl2)
        Console.WriteLine(sl3)
        Console.WriteLine(sl4.ToString(System.Globalization.CultureInfo.InvariantCulture))
        Console.WriteLine(sl5.ToString(System.Globalization.CultureInfo.InvariantCulture))
        Console.WriteLine(sl6)

        sl1 +=1
        sl2 +=1
        sl3 +=1
        sl4 +=0.5
        sl5 +=0.5
        sl6 +="*"
    End Sub
End Module
    </file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1
1
1
1
1

2
2
2
1.5
1.5
*
3
3
3
2
2
**]]>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalDeclaration_WithArrayTypes()
            'Declare static local with array types

            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
    Public Sub Main()
        WithArrayType()
        WithArrayType()

    End Sub

    Sub WithArrayType()
        Static Dim sl1 As Integer() = {1, 2, 3} 'integer

        'Show Values
        Console.WriteLine(sl1.Length)
        For Each i In sl1
            Console.Write(i.ToString &amp; " ")
        Next
        Console.WriteLine("")

        sl1 = {11, 12}
    End Sub
End Module
    </file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[3
1 2 3 
2
11 12]]>)

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalDeclaration_WithCollectionInitializer()
            'Declare static local using collection types / extension methods and the Add would be invoked each time,

            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
        Imports System

'Used my own attribute for Extension attribute based upon necessary signature rather than adding a specific reference to 
'System.Core which contains this normally

        Namespace System.Runtime.CompilerServices
    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method, AllowMultiple:=False, Inherited:=False)&gt; Class ExtensionAttribute
        Inherits Attribute
    End Class
End Namespace

        Public Module Module1
            Public Sub Main()
                Dim x As New System.Collections.Generic.Stack(Of Integer) From {11, 21, 31}
            End Sub

            &lt;System.Runtime.CompilerServices.Extension&gt; Public Sub Add(x As System.Collections.Generic.Stack(Of Integer), y As Integer)
                Static Dim sl1 As Integer = 0

                sl1 += 1
                Console.WriteLine(sl1.ToString &amp; "   Value:" &amp; y.ToString)
                x.Push(y)
            End Sub
        End Module
    </file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1   Value:11
2   Value:21
3   Value:31]]>)

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalDeclaration_WithDim()
            'Declare static local in conjunction with an Dim keyword 
            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
    Public Sub Main()
        WithTypeCharacters()
        WithTypeCharacters()
        WithTypeCharacters()
    End Sub

    Sub WithTypeCharacters()
        Static Dim sl1 As Integer = 1 'integer
        Console.WriteLine(sl1)
        sl1 += 1
    End Sub
End Module
    </file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1
2
3]]>)

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalDeclaration_WithAttribute()
            'This is important because the static local can have an attribute on it whereas a normal local cannot

            ParseAndVerify(<![CDATA[
                    Imports System

                    Public Module Module1
                        Public Sub Main()
                            Goo()
                            Goo()
                            Goo()
                        End Sub

                        Sub Goo()
                            <Test> Static a1 As Integer = 1
                            a1 += 1
                            Console.WriteLine(a1.ToString)
                        End Sub
                    End Module

                    <AttributeUsage(AttributeTargets.All)>
                    Class TestAttribute
                       Inherits Attribute
                    End Class
                         ]]>)

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalInTryCatchBlock()
            'The Use of Static Locals within Try/Catch/Finally Blocks
            'Simple Usage
            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
    Public Sub Main()
        CatchBlock()
        CatchBlock()

        FinallyBlock()
        FinallyBlock()

        NotCalledCatchBlock()
        NotCalledCatchBlock()
    End Sub

    Sub CatchBlock()
        Try
            Throw New Exception
        Catch ex As Exception
            Static a As Integer = 1
            Console.WriteLine(a.ToString)
            a += 1
        End Try
    End Sub

    Sub FinallyBlock()
        Try
            Throw New Exception
        Catch ex As Exception
        Finally
            Static a As Integer = 1
            Console.WriteLine(a.ToString)
            a += 1
        End Try
    End Sub

    Sub NotCalledCatchBlock()
        Try

        Catch ex As Exception
            Static a As Integer = 1
            Console.WriteLine(a.ToString)
            a += 1
        End Try
    End Sub
End Module
    </file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1
2
1
2]]>)

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalExceptionInInitialization()

            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
    Public ExceptionThrow As Boolean = False

    Public Sub Main()
        ExceptionThrow = False
        test(True) 'First Time Exception thrown so it will result in static local initialized to default for second call
        If ExceptionThrow Then Console.WriteLine("Exception Thrown") Else Console.WriteLine("No Exception Thrown")

        ExceptionThrow = False
        test(True) 'This should result in value of default value +1 and no exception thrown on second invocation
        If ExceptionThrow Then Console.WriteLine("Exception Thrown") Else Console.WriteLine("No Exception Thrown")

        ExceptionThrow = False
        test(True) 'This should result in value of default value +1 and no exception thrown on second invocation
        If ExceptionThrow Then Console.WriteLine("Exception Thrown") Else Console.WriteLine("No Exception Thrown")
    End Sub

    Sub test(BlnThrowException As Boolean)
        Try
            Static sl As Integer = throwException(BlnThrowException) 'Something to cause exception            
            sl += 1
            Console.WriteLine(sl.ToString)
        Catch ex As Exception
            ExceptionThrow = True
        Finally
        End Try
    End Sub

    Function throwException(x As Boolean) As Integer
        If x = True Then
            Throw New Exception
        Else
            Return 1
        End If
    End Function
End Module    </file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[
Exception Thrown
1
No Exception Thrown
2
No Exception Thrown]]>)






            'SemanticInfoTypeTestForeach(compilation1, 1, "String()", "System.Collections.IEnumerable")

            'AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="s", ReadInsideSymbol:="arr, s", ReadOutsideSymbol:="arr",
            '                                 WrittenInsideSymbol:="s", WrittenOutsideSymbol:="arr",
            '                                 AlwaysAssignedSymbol:="", DataFlowsInSymbol:="arr", DataFlowsOutSymbol:="")
            'AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
            '                                    EndPointIsReachable:=True)
            'ClassfiConversionTestForeach(compilation1)
            'VerifyForeachSemanticInfo(compilation1)

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalInTryCatchBlock_21()
            'The Use of Static Locals within Try/Catch/Finally Blocks

            Dim compilationDef =
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
    Public Sub Main()
        test(False)
        test(True)
        test(False)
    End Sub

    Sub test(ThrowException As Boolean)
        Static sl As Integer = 1
        Try
            If ThrowException Then
                Throw New Exception
            End If
        Catch ex As Exception
            sl += 1
        End Try

        Console.WriteLine(sl.ToString)
    End Sub
End Module
End Module
</file>
    </compilation>

            'Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndVBRuntime(compilationDef)
            'compilation.VerifyDiagnostics()

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalInTryCatchBlock_3()
            'The Use of Static Locals within Try/Catch/Finally Blocks

            Dim compilationDef =
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
        Public Sub Main()
            test(True)
            test(False)
            test(True)
        End Sub

        Sub test(ThrowException As Boolean)
            Static sl As Integer = 1
            Try
                If ThrowException Then
                    Throw New Exception
                End If
            Catch ex As Exception
                sl += 1
            End Try

            Console.WriteLine(sl.ToString)
        End Sub
    End Module
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics()
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalInTryCatchFinallyBlock()
            'The Use of Static Locals within Try/Catch/Finally Blocks
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
    Public Sub Main()
        CatchBlock()
        CatchBlock()

        FinallyBlock()
        FinallyBlock()

        NotCalledCatchBlock()
        NotCalledCatchBlock()
    End Sub

    Sub CatchBlock()
        Try
            Throw New Exception
        Catch ex As Exception
            Static a As Integer = 1
            Console.WriteLine(a.ToString)
            a += 1
        End Try
    End Sub

    Sub FinallyBlock()
        Try
            Throw New Exception
        Catch ex As Exception
        Finally
            Static a As Integer = 1
            Console.WriteLine(a.ToString)
            a += 1
        End Try
    End Sub

    Sub NotCalledCatchBlock()
        Try

        Catch ex As Exception
            Static a As Integer = 1
            Console.WriteLine(a.ToString)
            a += 1
        End Try
    End Sub
End Module
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1
2
1
2]]>)

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalInTryCatchBlock_2()
            'The Use of Static Locals within Try/Catch/Finally Blocks

            Dim compilationDef =
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
    Public Sub Main()
        test(False)
        test(True)
        test(False)
    End Sub

    Sub test(ThrowException As Boolean)
        Static sl As Integer = 1
        Try
            If ThrowException Then
                Throw New Exception
            End If
        Catch ex As Exception
            sl += 1
        End Try

        Console.WriteLine(sl.ToString)
    End Sub
End Module
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1
2
2]]>)
        End Sub

        Public Sub Semantic_SameNameInDifferentMethods()
            'The Use of Static Locals within shared methods with same name as static local in each method

            Dim compilationDef =
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
    Public Sub Main()
        Test1()
        Test2()
        Test1()
        Test2()
    End Sub

    Sub Test1()
        Static sl As Integer = 1
        Console.WriteLine(sl.ToString)
        sl += 1
    End Sub

    Sub Test2()
        Static sl As Integer = 1
        Console.WriteLine(sl.ToString)
        sl += 1
    End Sub
End Module
</file>
    </compilation>

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1
    1
    2
    2]]>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_SameNameInDifferentOverloads()
            'The Use of Static Locals within shared methods with same name as static local in each method

            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
    Public Sub Main()
        Test()
        Test(1)
        Test()
        Test(1)
    End Sub

    Sub Test()
        Static sl As Integer = 1
        Console.WriteLine(sl.ToString)
        sl += 1
    End Sub

    Sub Test(x As Integer)
        Static sl As Integer = 1
        Console.WriteLine(sl.ToString)
        sl += 1
    End Sub
End Module
</file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1
1
2
2]]>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_SharedMethods()
            'The Use of Static Locals within shared methods with same name as static local in each method

            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Public Class C1
    Public Shared Sub Main()
        testMethod()
        testMethod2()
        testMethod()
        testMethod2()
        testMethod()
        testMethod2()
    End Sub

    Shared Sub testMethod()
        Static sl As Integer = 1
        sl += 1
        Console.WriteLine(sl.ToString)
    End Sub

    Shared Sub testMethod2()
        Static sl As Integer = 1
        sl += 1
        Console.WriteLine(sl.ToString)
    End Sub
End Class
</file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[2
2
3
3
4
4]]>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_OverriddenMethod()
            'The Use of Static Locals in both a base and derived class with overridden method

            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
    Public Sub Main()
        Dim Obj As New Base
        Obj.Goo()
        Obj.Goo()
        Obj.Goo()

        Dim ObjD As New Derived
        ObjD.goo()
        ObjD.goo()
        ObjD.goo()
    End Sub

End Module

Class Base
    Overridable Sub Goo()
        Static sl As Integer = 1
        Console.WriteLine(sl.ToString)
        sl += 1
    End Sub
End Class

Class Derived
    Sub goo()
        Static sl As Integer = 10
        Console.WriteLine(sl.ToString)
        sl += 1
    End Sub
End Class
</file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1
2
3
10
11
12]]>)
        End Sub



        Public Sub Semantic_InheritenceConstructor()
            'The Use of Static Locals in both a base and derived class constructor - instance method

            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">

Imports System

Public Module Module1
    Public Sub Main()
        Dim Obj As New Base
        Dim ObjD As New Derived
    End Sub
End Module

Class Base
    Sub New()
        Static sl As Integer = 1
        Console.WriteLine(sl.ToString)
        sl += 1
    End Sub
End Class

Class Derived
    Inherits Base

    Sub New()
        Static sl As Integer = 10
        Console.WriteLine(sl.ToString)
        sl += 1
    End Sub    
End Class
</file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1
    1
    10]]>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_WithFields()
            'The Use of Static Locals within shared methods with same name as static local in each method

            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">

Imports System

Public Module Module1
    Public sl = 10

    Public Sub Main()
        'These should touch the static locals
        testMethod()
        testMethod2(True)
        testMethod()
        testMethod2(True)
        testMethod()
        testMethod2(True)

        'These should touch the field - sl out of scope in method
        testMethod2(False)
        testMethod2(False)
        testMethod2(False)

        'These should touch the static locals as SL declaration moved ins cope for both code blocks
        testMethod3(True)
        testMethod3(False)
        testMethod3(True)
        testMethod3(False)
        testMethod3(True)
        testMethod3(False)
    End Sub

    Sub testMethod()
        Static sl As Integer = 1
        sl += 1
        Console.WriteLine(sl.ToString)
    End Sub

    Sub testMethod2(x As Boolean)
        'Only true in Scope for Static Local, False is field
        If x = True Then
            Static sl As Integer = 1
            sl += 1
            Console.WriteLine(sl.ToString)
        Else
            sl += 1
            Console.WriteLine(sl.ToString)
        End If
    End Sub

    Sub testMethod3(x As Boolean)
        'Both Code Blocks in Scope for Static Local
        Static sl As Integer = 1
        If sl = True Then
            sl += 1
            Console.WriteLine(sl.ToString)
        Else
            sl += 1
            Console.WriteLine(sl.ToString)
        End If
    End Sub
End Module
</file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[2
2
3
3
4
4
11
12
13
2
3
4
5
6
7]]>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_WithProperty()
            'The Use of Static Locals within shared methods with same name as static local in each method

            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">

Imports System

Public Module Module1
    Public Property sl = 10

    Public Sub Main()
        'These should touch the static locals
        testMethod()
        testMethod2(True)
        testMethod()
        testMethod2(True)
        testMethod()
        testMethod2(True)

        'These should touch the field - sl out of scope in method
        testMethod2(False)
        testMethod2(False)
        testMethod2(False)

        'These should touch the static locals as SL declaration moved ins cope for both code blocks
        testMethod3(True)
        testMethod3(False)
        testMethod3(True)
        testMethod3(False)
        testMethod3(True)
        testMethod3(False)
    End Sub

    Sub testMethod()
        Static sl As Integer = 1
        sl += 1
        Console.WriteLine(sl.ToString)
    End Sub

    Sub testMethod2(x As Boolean)
        'Only true in Scope for Static Local, False is field
        If x = True Then
            Static sl As Integer = 1
            sl += 1
            Console.WriteLine(sl.ToString)
        Else
            sl += 1
            Console.WriteLine(sl.ToString)
        End If
    End Sub

    Sub testMethod3(x As Boolean)
        'Both Code Blocks in Scope for Static Local
        Static sl As Integer = 1
        If sl = True Then
            sl += 1
            Console.WriteLine(sl.ToString)
        Else
            sl += 1
            Console.WriteLine(sl.ToString)
        End If
    End Sub
End Module
</file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[2
2
3
3
4
4
11
12
13
2
3
4
5
6
7]]>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_InPropertySetter()
            'The Use of Static Locals within shared methods with same name as static local in each method

            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">

Imports System

Public Module Module1
     Public Sub Main()
        'Each time I set property sl should increment

        Dim obj1 As New Goo
        obj1.sl = 1
        obj1.sl = 2
        obj1.sl = 3

        'Different Object
        Dim Obj2 As New Goo With {.sl = 1}
        Obj2.sl = 2

    End Sub

    Class Goo
        Public _field As Integer = 0
        Public Property sl As Integer
            Set(value As Integer)
                Static sl As Integer = 1
                Console.WriteLine(sl.ToString)
                sl += 1
                _field = value
            End Set
            Get
                Return _field
            End Get
        End Property
    End Class
End Module
</file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1
2
3
1
2]]>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_InConstructor()
            'The Use of Static Locals within Constructor

            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">

Imports System

Public Module Module1
    Public Sub Main()
        Dim obj1 As New Goo
        Dim obj2 As New Goo
        Dim obj3 As New Goo

    End Sub

    Class Goo
        Sub New()
            Static sl As Integer = 1
            Console.WriteLine(sl.ToString)
            sl += 1
        End Sub
    End Class
End Module
</file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1
1
1]]>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_InSharedConstructor()
            'The Use of Static Locals within Shared Constructor - Only called Once
            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
    Public Sub Main()
        Dim obj1 As New Goo
        Dim obj2 As New Goo
        Dim obj3 As New Goo
    End Sub

    Class Goo
        Shared Sub New()
            Static sl As Integer = 1
            Console.WriteLine(sl.ToString)
            sl += 1
        End Sub
    End Class
End Module
</file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1]]>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub Semantic_InFinalizer()
            'The Use of Static Locals within Finalizer - No Problems
            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">

Imports System

Public Module Module1
    Public Sub Main()
        Dim x As New TestClass
    End Sub
End Module

Class TestClass
    Sub New()
        Static SLConstructor As Integer = 1
    End Sub

    Protected Overrides Sub Finalize()
        Static SLFinalize As Integer = 1
        Console.WriteLine(SLFinalize.ToString)
        MyBase.Finalize()
    End Sub
End Class
</file>
    </compilation>, TestOptions.ReleaseExe)

            compilationDef.VerifyDiagnostics()

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_MaximumLength_StaticLocalIdentifier()
            'The Use of Static Locals with an identifier at maxmimum length to ensure functionality
            'works and generated backing field is correctly supported.
            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">

Imports System

Public Module Module1
    Public Sub Main()
        MaximumLengthIdentifierIn2012()
        MaximumLengthIdentifierIn2012()
        MaximumLengthIdentifierIn2012()
    End Sub

    Sub MaximumLengthIdentifierIn2012()
        Static abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijk As Integer = 1

        Console.WriteLine(abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijk.ToString)
        abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijk += 1
    End Sub
End Module
</file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1
2
3]]>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Semantic_StaticLocalPartialClasses()
            'Ensure that the code generated field is correctly generated in Partial Class / Partial Private scenarios
            Dim compilationDef = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

Public Module Module1
    Public Sub Main()
        Test()
    End Sub

    Sub test()
        Dim x As New P1
        x.Caller()
        x.Caller()
        x.Caller()
    End Sub
End Module

Partial Class P1
    Public Sub Caller()
        Goo()
    End Sub

    Partial Private Sub Goo()
    End Sub
End Class

Partial Class P1
    Private Sub Goo()
        Static i As Integer = 1
        Console.WriteLine(i.ToString)
        i += 1
    End Sub
End Class

    </file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilationDef, expectedOutput:=<![CDATA[1
2
3]]>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub semanticInfo_StaticKeywordOnly_IsStatic()
            Dim source =
    <compilation>
        <file name="a.vb">
    Imports System
Public Module Module1
    Public Sub Main()
        Goo()
        Goo()
    End Sub

    Sub Goo()
        Static x As Long = 2         
        Console.WriteLine(x.ToString)
        x += 1 'BIND:"x"        
    End Sub
End Module

    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            Dim tree = compilation.SyntaxTrees(0)
            Dim treeModel = compilation.GetSemanticModel(tree)

            Dim cDecl = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.LocalDeclarationStatement, 1).AsNode(), LocalDeclarationStatementSyntax)
            Dim cTypeSymbol = treeModel.GetSemanticInfoSummary(DirectCast(cDecl.Declarators(0).AsClause, SimpleAsClauseSyntax).Type).Type

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            Dim iSymbol = DirectCast(semanticInfo.Symbol, LocalSymbol)
            Assert.True(iSymbol.IsStatic)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub semanticInfo_StaticAndDimKeyword_IsStatic()
            Dim source =
    <compilation>
        <file name="a.vb">
    Imports System
Public Module Module1
    Public Sub Main()
        Goo()
        Goo()
    End Sub

    Sub Goo()
        Static Dim x As Long = 2 
        Console.WriteLine(x.ToString)
        x += 1 'BIND:"x"        
    End Sub
End Module

    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            Dim tree = compilation.SyntaxTrees(0)
            Dim treeModel = compilation.GetSemanticModel(tree)

            Dim cDecl = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.LocalDeclarationStatement, 1).AsNode(), LocalDeclarationStatementSyntax)
            Dim cTypeSymbol = treeModel.GetSemanticInfoSummary(DirectCast(cDecl.Declarators(0).AsClause, SimpleAsClauseSyntax).Type).Type

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            Dim iSymbol = DirectCast(semanticInfo.Symbol, LocalSymbol)
            Assert.True(iSymbol.IsStatic)

            source =
    <compilation>
        <file name="a.vb">
    Imports System
Public Module Module1
    Public Sub Main()
        Goo()
        Goo()
    End Sub

    Sub Goo()
        Dim Static x As Long = 2 
        Console.WriteLine(x.ToString)
        x += 1 'BIND:"x"        
    End Sub
End Module

    </file>
    </compilation>

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            tree = compilation.SyntaxTrees(0)
            treeModel = compilation.GetSemanticModel(tree)

            cDecl = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.LocalDeclarationStatement, 1).AsNode(), LocalDeclarationStatementSyntax)
            cTypeSymbol = treeModel.GetSemanticInfoSummary(DirectCast(cDecl.Declarators(0).AsClause, SimpleAsClauseSyntax).Type).Type

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            iSymbol = DirectCast(semanticInfo.Symbol, LocalSymbol)
            Assert.True(iSymbol.IsStatic)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub semanticInfo_StaticDimOnly_IsStatic()
            Dim source =
    <compilation>
        <file name="a.vb">
    Imports System
Public Module Module1
    Public Sub Main()
        Goo()
        Goo()
    End Sub

    Sub Goo()
        Dim x As Long = 2 
        Console.WriteLine(x.ToString)
        x += 1 'BIND:"x"        
    End Sub
End Module

    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            Dim tree = compilation.SyntaxTrees(0)
            Dim treeModel = compilation.GetSemanticModel(tree)

            Dim cDecl = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.LocalDeclarationStatement, 1).AsNode(), LocalDeclarationStatementSyntax)
            Dim cTypeSymbol = treeModel.GetSemanticInfoSummary(DirectCast(cDecl.Declarators(0).AsClause, SimpleAsClauseSyntax).Type).Type

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            Dim iSymbol = DirectCast(semanticInfo.Symbol, LocalSymbol)
            Assert.False(iSymbol.IsStatic)
        End Sub
    End Class

End Namespace
