' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class StaticLocalDeclarationTests
        Inherits BasicTestBase

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Valid_BasicParsingWithDim()
            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
Imports System

Module Module1
    Sub Main()
        Goo()
        Goo()
    End Sub

    Sub Goo()
        Static Dim x As Integer = 1
        Console.WriteLine(x)
        x = x + 1
    End Sub    
End Module 
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics()
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Valid_BasicParsingWithoutDim()
            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
Imports System

Module Module1
    Sub Main()
        Goo()
        Goo()
    End Sub

    Sub Goo()
        Static x As Integer = 1
        Console.WriteLine(x)
        x = x + 1
    End Sub    
End Module 
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics()
        End Sub

        <Fact>
        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        Public Sub Error_StaticLocal_DuplicationDeclarations_InSameScopes()
            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
Imports System

Module Module1
    Sub Main()
        StaticLocal_DuplicationDeclarations_InSameScopes()
        StaticLocal_DuplicationDeclarations_InSameScopes()
    End Sub

    Sub StaticLocal_DuplicationDeclarations_InSameScopes()
        Static x As Integer = 1
        Console.WriteLine(x)
        Static x As Integer = 2   'Err
        Console.WriteLine(x)
    End Sub    
End Module 
</file>
    </compilation>

            'This should present a single error BC31401: Static local variable 'x' is already declared.

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31401: Static local variable 'x' is already declared.
        Static x As Integer = 2   'Err
               ~

</expected>)
        End Sub

        <Fact>
        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        Public Sub Error_StaticLocal_DuplicationDeclarationsConflictWithLocal1()
            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
Imports System

Module Module1
    Sub Main()
        StaticLocal_ConflictDeclarations()
        StaticLocal_ConflictDeclarations()
    End Sub

    Sub StaticLocal_ConflictDeclarations()
        Static x As Integer = 1 'Err
        Console.WriteLine(x)
        Dim x As Integer = 2   
        Console.WriteLine(x)
    End Sub    
End Module 
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30288: Local variable 'x' is already declared in the current block.
        Dim x As Integer = 2   
            ~ 

</expected>)
        End Sub

        <Fact>
        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        Public Sub Error_StaticLocal_DuplicationDeclarationsConflictWithLocal2()
            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
            Imports System

            Module Module1
                Sub Main()
                    StaticLocal_ConflictDeclarations()
                    StaticLocal_ConflictDeclarations()
                End Sub

                Sub StaticLocal_ConflictDeclarations()
                    Dim x As Integer = 1 
                    Console.WriteLine(x)
                    Static x As Integer = 2 'Err  
                    Console.WriteLine(x)
                End Sub    
            End Module 
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30288: Local variable 'x' is already declared in the current block.
                    Static x As Integer = 2 'Err  
                           ~  
</expected>)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Error_StaticLocal_DuplicationDeclarations_InDifferentScopes_tryCatch()
            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
Imports System

Module Module1
    Sub Main()
        StaticLocal_DuplicationDeclarations_InDifferentScopes_tryCatch()
        StaticLocal_DuplicationDeclarations_InDifferentScopes_tryCatch()
    End Sub

    Sub StaticLocal_DuplicationDeclarations_InDifferentScopes_tryCatch()
        Try
            Dim y As Integer = 1
            Static x As Integer = 1
        Catch ex As Exception
            Dim y As Integer = 2
            Static x As Integer = 2   'Error
        End Try
    End Sub
End Module
</file>
    </compilation>

            'This should present a single error BC31401: Static local variable 'x' is already declared.
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_DuplicateLocalStatic1, "x").WithArguments("x"))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Error_StaticLocal_DuplicationDeclarations_InDifferentScopesSelectCase()
            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
Module Module1
    Sub Main()
        StaticLocal_DuplicationDeclarations_InDifferentScopesSelectCase(1)
        StaticLocal_DuplicationDeclarations_InDifferentScopesSelectCase(2)
    End Sub

    Sub StaticLocal_DuplicationDeclarations_InDifferentScopesSelectCase(a As Integer)
        Select Case a
            Case 1
                Dim y As Integer = 1
                Static x As Integer = 1
            Case 2
                Dim y As Integer = 1
                Static x As Integer = 1 'Error
        End Select
    End Sub    
End Module
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_DuplicateLocalStatic1, "x").WithArguments("x"))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Error_StaticLocal_DuplicationDeclarations_InDifferentScopes_If()
            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
Module Module1
    Sub Main()
        StaticLocal_DuplicationDeclarations_InDifferentScopes_If()
    End Sub

    Sub StaticLocal_DuplicationDeclarations_InDifferentScopes_If()
        If True Then
            Dim y As Integer = 1
            Static x As Integer = 1
        Else
            Dim y As Integer = 2
            Static x As Integer = 2   'Error
        End If
    End Sub
End Module    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_DuplicateLocalStatic1, "x").WithArguments("x"))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Error_StaticLocal_DuplicationDeclarations_InDifferentScopes_For()
            'Show differences between static local and normal local
            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
Module Module1
    Sub Main()
        StaticLocal_DuplicationDeclarations_InDifferentScopes_For()
        StaticLocal_DuplicationDeclarations_InDifferentScopes_For()
    End Sub

    Sub StaticLocal_DuplicationDeclarations_InDifferentScopes_For()
        Dim y As Integer = 1
        Static x As Integer = 1 'Warning Hide in enclosing block

        For i = 1 To 2
            Dim y As Integer = 2 'Warning Hide in enclosing block
            Static x As Integer = 3 'Error
        Next
    End Sub
End Module
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_BlockLocalShadowing1, "y").WithArguments("y"),
                                          Diagnostic(ERRID.ERR_BlockLocalShadowing1, "x").WithArguments("x"),
                                          Diagnostic(ERRID.ERR_DuplicateLocalStatic1, "x").WithArguments("x"))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Error_StaticLocalDeclaration_Negative_InGeneric()
            'Cannot declare in generic method
            Dim compilationDef =
        <compilation name="StaticLocaltest">
            <file name="a.vb">
Module Module1
        Sub Main()
            Dim x as new UDTest()
            x.Goo(of Integer)()
            x.Goo(of Integer)()
        End Sub
End Module

        Public Class UDTest
            Public Sub Goo(of t)
                Static SLItem as integer = 1
                SLItem +=1
            End Sub            
        End Class
</file>
        </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_BadStaticLocalInGenericMethod, "Static"))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Error_StaticLocalDeclaration_Negative_InStructure()
            'Cannot declare in Structure Type

            Dim compilationDef =
        <compilation name="StaticLocaltest">
            <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As New UDTest()
        x.Goo()
        x.Goo()
    End Sub
End Module

Public Structure UDTest
    Public Sub Goo()
        Static SLItem As Integer = 1
        SLItem += 1
    End Sub
End Structure
</file>
        </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_BadStaticLocalInStruct, "Static"))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Error_StaticLocalDeclaration_Negative_WithModifiers()
            'Errors in conjunction with access modifiers
            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
Module Module1
    Sub Main()
        AccessModifiers()        
    End Sub

    Sub AccessModifiers()
        'These are prettylisted with Access Modified beforehand
        Public Static SLItem1 As String = ""
        Private Static SLItem2 As String = ""
        Protected Static SLItem3 As String = ""
        Friend Static SLItem4 As String = ""
        Protected Friend Static SLItem5 As String = ""
        Static Shared SLItem6 

        Static Dim SLItem_Valid1 As String = "" 'Valid
    End Sub
End Module
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_BadLocalDimFlags1, "Public").WithArguments("Public"),
                                          Diagnostic(ERRID.ERR_BadLocalDimFlags1, "Private").WithArguments("Private"),
                                          Diagnostic(ERRID.ERR_BadLocalDimFlags1, "Protected").WithArguments("Protected"),
                                          Diagnostic(ERRID.ERR_BadLocalDimFlags1, "Friend").WithArguments("Friend"),
                                          Diagnostic(ERRID.ERR_BadLocalDimFlags1, "Protected").WithArguments("Protected"),
                                          Diagnostic(ERRID.ERR_BadLocalDimFlags1, "Friend").WithArguments("Friend"),
                                          Diagnostic(ERRID.ERR_BadLocalDimFlags1, "Shared").WithArguments("Shared"),
                                          Diagnostic(ERRID.WRN_UnusedLocal, "SLItem6").WithArguments("SLItem6"))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Error_StaticLocalDeclaration_Negative_OutsideOfMethod()
            'Static Locals outside of method bodies
            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
Module Module1
    Sub Main()
        AccessModifiers()
        AccessModifiers()
    End Sub

    Static SLItem_Valid1 As String = "" 'Invalid

    Sub AccessModifiers()
        Static SLItem_Valid1 As String = "" 'Valid
    End Sub
End Module
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_BadDimFlags1, "Static").WithArguments("Static"))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Error_TryingToAccessStaticLocalFromOutsideMethod()
            'trying to access SL from oUtside method not possible
            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
Module Module1
    Sub Main()
        'trying to access SL from oUtside method not possible
        StaticLocalInSub()
        StaticLocalInSub.slItem = 2 'ERROR

        StaticLocalInSub2()
        StaticLocalInSub2.slItem = 2 'ERROR
    End Sub

    Sub StaticLocalInSub()
        Static SLItem1 = 1
        SLItem1 += 1
    End Sub

    Public Sub StaticLocalInSub2()
        'With Method Accessibility set to Public
        Static SLItem1 = 1
        SLItem1 += 1
    End Sub
End Module</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_VoidValue, "StaticLocalInSub"),
                                          Diagnostic(ERRID.ERR_VoidValue, "StaticLocalInSub2"))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Error_StaticLocalDeclaration_Negative_HideLocalInCatchBlock()
            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
Imports System

Module Module1
    Sub Main()
        'trying to access SL from oUtside method not possible
        Test4_Err()
    End Sub

    Public Sub Test4_Err()
        Static sl1 As String = ""
        Try
            Throw New Exception("Test")
        Catch sl1 As Exception 'Err
            sl1 &amp;= "InCatch" 'Err - this is the exception instance not the static local            
        Finally
        End Try
    End Sub
End Module
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_BlockLocalShadowing1, "sl1").WithArguments("sl1"),
                                          Diagnostic(ERRID.ERR_BinaryOperands3, "sl1 &= ""InCatch""").WithArguments("&", "System.Exception", "String"))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Error_StaticLocalDeclaration_Keyword_NameClashInIdentifier()
            'declare UnEscaped identifier called static 

            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
Module Module1
    Sub Main()
        AvoidingNameConflicts1()        
    End Sub

    Sub AvoidingNameConflicts1()
        Static Static as double = 1 'Error
    End Sub
End Module

</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "as"),
                                          Diagnostic(ERRID.ERR_DuplicateSpecifier, "Static"))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Error_StaticLocalDeclaration_Keyword_NameTypeClash()
            'declare escaped identifier and type called static along with static
            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
Module Module1
    Sub Main()
        AvoidingNameConflicts()        
    End Sub

    Sub AvoidingNameConflicts()
        Static [Static] As New [Static]
    End Sub
End Module

Class Static 'Error Clash With Keyword

End Class
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "Static"))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Error_StaticLocalDeclaration_Negative_InLambda_SingleLine()
            'Single Line Lambda
            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
Module Module1
    Sub Main()
        InSingleLineLambda()        
    End Sub

    Sub InSingleLineLambda()
        Static sl1 As Integer = 0

        'Declaring Static in Single Line Lambda
        Dim l1 = Sub() static x1 As Integer = 0 'Error

        Dim l2 = Function() static x2 As Integer = 0 'Error

        'Using Lifted Locals in Lambda's
        Dim l3 = Sub() sl1 += 1

        Dim l4 = Function() (sl1 + 1)
    End Sub
End Module
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedExpression, ""),
                                          Diagnostic(ERRID.ERR_SubDisallowsStatement, "static x1 As Integer = 0"))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Error_StaticLocalDeclaration_Negative_InLambda_MultiLine()
            'Multi-Line Lambda

            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
Module Module1
    Sub Main()
        InMultiLineLambda()        
    End Sub


    Sub InMultiLineLambda()
        Static sl1 As Integer = 0

        'Declaring Static in MultiLine
        Dim l1 = Sub()
                     static x1 As Integer = 0 'Error
                 End Sub

        Dim l2 = Function()
                     static x2 As Integer = 0 'Error
                     Return x2
                 End Function

        'Using Lifted Locals in Lambda's
        Dim l3 = Sub()
                     sl1 += 1
                 End Sub

        Dim l4 = Function()
                     sl1 += 1
                     Return sl1
                 End Function

    End Sub
End Module
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_StaticInLambda, "static"),
                                          Diagnostic(ERRID.ERR_StaticInLambda, "static"))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Error_StaticLocalInTryCatchBlockScope()
            'The Use of Static Locals within Try/Catch/Finally Blocks

            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
Imports System

Module Module1
    Sub Main()
        test(False)
        test(True)
    End Sub

    Sub test(ThrowException As Boolean)
        Try
            If ThrowException Then
                Throw New Exception
            End If
        Catch ex As Exception
            Static sl As Integer = 1
            sl += 1
        End Try

        Console.WriteLine(SL.tostring) 'Error Outside Scope
    End Sub
End Module</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_NameNotDeclared1, "SL").WithArguments("SL"))

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Error_StaticLocal_SpecialType_ArgIterator()
            Dim compilationDef =
        <compilation name="StaticLocaltest">
            <file name="a.vb">
Imports System

Module Module1
    Sub Main()
        Goo()
        Goo()
    End Sub

    Sub Goo()
        Static SLItem2 As ArgIterator
    End Sub
End Module</file>
        </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_RestrictedType1, "ArgIterator").WithArguments("System.ArgIterator"),
                                          Diagnostic(ERRID.WRN_UnusedLocal, "SLItem2").WithArguments("SLItem2"))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Error_StaticLocal_SpecialType_TypedReference()
            Dim compilationDef =
        <compilation name="StaticLocaltest">
            <file name="a.vb">
Imports System

Module Module1
    Sub Main()
        Goo()
        Goo()
    End Sub

    Sub Goo()
        Static SLItem2 As TypedReference = Nothing
    End Sub
End Module
</file>
        </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_RestrictedType1, "TypedReference").WithArguments("System.TypedReference"))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Error_StaticLocalWithRangeVariables()
            'The Use of Static Locals within Try/Catch/Finally Blocks

            Dim compilationDef =
    <compilation name="StaticLocaltest">
        <file name="a.vb">
Imports System

Module Module1
    Sub Main()
        test()
        test()
    End Sub

    Sub test()
        Static sl As Integer = 1

        Dim x = From sl In {1, 2, 3} Select sl 'Error Same Scope

        Console.WriteLine(sl.ToString) 
        sl += 1
    End Sub
End Module</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedQueryableSource, "{1, 2, 3}").WithArguments("Integer()"),
                                          Diagnostic(ERRID.ERR_IterationVariableShadowLocal1, "sl").WithArguments("sl"),
                                          Diagnostic(ERRID.ERR_IterationVariableShadowLocal1, "sl").WithArguments("sl"))
        End Sub
    End Class

End Namespace
