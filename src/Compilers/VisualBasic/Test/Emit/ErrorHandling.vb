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

    Public Class ErrorHandlingTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub ErrorHandler_WithValidLabel_No_Resume()
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
Imports System
Module Module1
    Sub Main()
        Dim sPath As String = ""
        sPath = "Test1"
        On Error GoTo foo
        Error 5
        Console.WriteLine(sPath)
        Exit Sub
foo:
        sPath &amp;= "foo"
        Console.WriteLine(sPath)
    End Sub
End Module
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)
            CompileAndVerify(compilation)
        End Sub

        <Fact()>
        Public Sub ErrorHandler_WithGotoMinus1andMatchingLabel()
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
Imports System

Module Module1    
    Public Sub Main()        
        On Error GoTo -1
        Error 5
        exit sub
foo:
        Resume 
    End Sub
End Module 
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)
            CompileAndVerify(compilation)
        End Sub

        <Fact()>
        Public Sub Error_ErrorHandler_WithGoto0andNoMatchingLabel()
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
Imports System

Module Module1    
    Public Sub Main()
        On Error GoTo 0
        Error 5
        exit sub
Foo:
        Resume 
    End Sub
End Module 
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)
            CompileAndVerify(compilation)
            'compilation.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub Error_ErrorHandler_WithResumeNext()
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
Imports System

Module Module1    
    Public Sub Main()
        On Error Resume Next
        Error 5
        exit sub
    End Sub
End Module 
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)
            CompileAndVerify(compilation)
            'compilation.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub ErrorHandler_WithValidLabelMatchingKeywordsEscaped()
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
Imports System

Module Module1    
    Public Sub Main()        
        On Error GoTo [On]
        On Error GoTo [goto]  'Doesn't matter if case mismatch (Didn't pretty list correctly)
        On Error GoTo [Error]

        exit sub
[Goto]:
        Resume [on]

[On]:
        Resume [Error]

[Error]:
        Resume [Goto]
    End Sub
End Module 
</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)
            CompileAndVerify(compilation)
            ' compilation.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub Error_ErrorHandler_WithValidLabelMatchingKeywordsNotEscaped()
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
    Module Module1    
        Public Sub Main()        
            On Error GoTo On
            On Error GoTo goto  
            On Error GoTo Error

    [Goto]:
            Resume [on]

    [On]:
            Resume [Error]

    [Error]:
            Resume [Goto]
        End Sub
    End Module 
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
        Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
        Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
        Diagnostic(ERRID.ERR_LabelNotDefined1, "").WithArguments(""),
        Diagnostic(ERRID.ERR_LabelNotDefined1, "").WithArguments(""),
        Diagnostic(ERRID.ERR_LabelNotDefined1, "").WithArguments(""))
        End Sub


        <Fact()>
        Public Sub Error_ErrorHandler_WithInValidLabelMatchingKeywordsEscaped()
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
    Module Module1    
        Public Sub Main()        
            On Error GoTo [On]
            On Error GoTo [goto]  'Doesn't matter if case mismatch (Didn't pretty list correctly)
            On Error GoTo [Error]

    Goto:
            Resume on

    On:
            Resume Error

    Error:
            Resume Goto
        End Sub
    End Module 
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
        Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
        Diagnostic(ERRID.ERR_ObsoleteOnGotoGosub, ""),
        Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
        Diagnostic(ERRID.ERR_ExpectedExpression, ""),
        Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
        Diagnostic(ERRID.ERR_LabelNotDefined1, "[On]").WithArguments("On"),
        Diagnostic(ERRID.ERR_LabelNotDefined1, "[goto]").WithArguments("goto"),
        Diagnostic(ERRID.ERR_LabelNotDefined1, "[Error]").WithArguments("Error"),
        Diagnostic(ERRID.ERR_LabelNotDefined1, "").WithArguments(""),
        Diagnostic(ERRID.ERR_LabelNotDefined1, "").WithArguments(""),
        Diagnostic(ERRID.ERR_LabelNotDefined1, "").WithArguments(""),
        Diagnostic(ERRID.ERR_LabelNotDefined1, "").WithArguments(""),
        Diagnostic(ERRID.ERR_LabelNotDefined1, "").WithArguments(""))
        End Sub

        <Fact()>
        Public Sub Error_ErrorHandler_WithGoto0andMatchingLabel()
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
    Imports System

    Module Module1    
        Public Sub Main()
            On Error GoTo 0
            Error 5
            exit sub
    0:
            Resume 
        End Sub
    End Module 
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            Dim compilationVerifier = CompileAndVerify(compilation)
            compilationVerifier.VerifyIL("Module1.Main", <![CDATA[
    {
      // Code size      155 (0x9b)
      .maxstack  3
      .locals init (Integer V_0,
      Integer V_1,
      Integer V_2,
      Integer V_3)
      .try
    {
      IL_0000:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
      IL_0005:  ldc.i4.0
      IL_0006:  stloc.0
      IL_0007:  ldc.i4.2
      IL_0008:  stloc.2
      IL_0009:  ldc.i4.5
      IL_000a:  call       "Function Microsoft.VisualBasic.CompilerServices.ProjectData.CreateProjectError(Integer) As System.Exception"
      IL_000f:  throw
      IL_0010:  ldc.i4.0
      IL_0011:  stloc.3
      IL_0012:  ldc.i4.5
      IL_0013:  stloc.2
      IL_0014:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
      IL_0019:  ldloc.1
      IL_001a:  brtrue.s   IL_0029
      IL_001c:  ldc.i4     0x800a0014
      IL_0021:  call       "Function Microsoft.VisualBasic.CompilerServices.ProjectData.CreateProjectError(Integer) As System.Exception"
      IL_0026:  throw
      IL_0027:  leave.s    IL_0092
      IL_0029:  ldloc.1
      IL_002a:  br.s       IL_002f
      IL_002c:  ldloc.1
      IL_002d:  ldc.i4.1
      IL_002e:  add
      IL_002f:  ldc.i4.0
      IL_0030:  stloc.1
      IL_0031:  switch    (
      IL_0052,
      IL_0000,
      IL_0007,
      IL_0027,
      IL_0010,
      IL_0012,
      IL_0027)
      IL_0052:  leave.s    IL_0087
      IL_0054:  ldloc.2
      IL_0055:  stloc.1
      IL_0056:  ldloc.0
      IL_0057:  switch    (
      IL_0064,
      IL_002c)
      IL_0064:  leave.s    IL_0087
    }
      filter
    {
      IL_0066:  isinst     "System.Exception"
      IL_006b:  ldnull
      IL_006c:  cgt.un
      IL_006e:  ldloc.0
      IL_006f:  ldc.i4.0
      IL_0070:  cgt.un
      IL_0072:  and
      IL_0073:  ldloc.1
      IL_0074:  ldc.i4.0
      IL_0075:  ceq
      IL_0077:  and
      IL_0078:  endfilter
    }  // end filter
    {  // handler
      IL_007a:  castclass  "System.Exception"
      IL_007f:  ldloc.3
      IL_0080:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception, Integer)"
      IL_0085:  leave.s    IL_0054
    }
      IL_0087:  ldc.i4     0x800a0033
      IL_008c:  call       "Function Microsoft.VisualBasic.CompilerServices.ProjectData.CreateProjectError(Integer) As System.Exception"
      IL_0091:  throw
      IL_0092:  ldloc.1
      IL_0093:  brfalse.s  IL_009a
      IL_0095:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
      IL_009a:  ret
    }
    ]]>)
        End Sub

        <Fact()>
        Public Sub Error_ErrorHandler_WithGoto1andMatchingLabel()
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
    Imports System

    Module Module1    
        Public Sub Main()        
            On Error GoTo 1
            Console.writeline("Start")        
            Error 5
            Console.writeline("2")
            exit sub
    1:
    Console.writeline("1")
            Resume Next
        End Sub
    End Module 
    </file>
    </compilation>


            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[Start
1
2]]>)
        End Sub

        <Fact()>
        Public Sub Error_ErrorHandler_WithMissingOrIncorrectLabels()
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
    Module Module1   
        sing Labels
        Public Sub Main()           
        End Sub

        Sub Goto_MissingLabel()
            'Error - label is not present
            On Error GoTo foo

        End Sub

        Sub GotoLabelInDifferentMethod()
            'Error - no label in this method, in a different so it will fail
            On Error GoTo diffMethodLabel
        End Sub

        Sub GotoLabelInDifferentMethod()
            'Error - no label in this method - trying to fully qualify will fail
            On Error GoTo DifferentMethod.diffMethodLabel
        End Sub

        Sub DifferentMethod()
    DiffMethodLabel:
        End Sub
    End Module 
    </file>
    </compilation>


            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedSpecifier, "Labels"),
                                          Diagnostic(ERRID.ERR_ExpectedDeclaration, "sing"),
                                          Diagnostic(ERRID.ERR_ExpectedEOS, "."),
                                          Diagnostic(ERRID.ERR_DuplicateProcDef1, "GotoLabelInDifferentMethod").WithArguments("Public Sub GotoLabelInDifferentMethod()"),
                                          Diagnostic(ERRID.ERR_LabelNotDefined1, "foo").WithArguments("foo"),
                                          Diagnostic(ERRID.ERR_LabelNotDefined1, "diffMethodLabel").WithArguments("diffMethodLabel"),
                                          Diagnostic(ERRID.ERR_LabelNotDefined1, "DifferentMethod").WithArguments("DifferentMethod"))
        End Sub



        <Fact()>
        Public Sub Error_ErrorHandler_BothTypesOfErrorHandling()
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
    Imports System

    Module Module1   
        Sub Main
            TryAndOnErrorInSameMethod
            OnErrorAndTryInSameMethod
        End Sub

        Sub TryAndOnErrorInSameMethod()
            'Nested
            Try
                On Error GoTo foo
    foo:
            Catch ex As Exception
            End Try
        End Sub

        Sub OnErrorAndTryInSameMethod()
            'Sequential
            On Error GoTo foo
    foo:
            Try
            Catch ex As Exception
            End Try
        End Sub
    End Module 
    </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim ExpectedOutput = <![CDATA[Try
                On Error GoTo foo
    foo:
            Catch ex As Exception
            End Try]]>

            Dim ExpectedOutput2 = <![CDATA[Try
            Catch ex As Exception
            End Try]]>

            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_TryAndOnErrorDoNotMix, ExpectedOutput),
                                          Diagnostic(ERRID.ERR_TryAndOnErrorDoNotMix, "On Error GoTo foo"),
                                          Diagnostic(ERRID.ERR_TryAndOnErrorDoNotMix, "On Error GoTo foo"),
                                          Diagnostic(ERRID.ERR_TryAndOnErrorDoNotMix, ExpectedOutput2)
    )
        End Sub

        <Fact()>
        Public Sub Error_ErrorHandler_InVBCore()
            'Old Style handling not supported in VBCore
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
    Module Module1   
        Public Sub Main        
            On Error GoTo foo
    foo:
        End Sub
    End Module 
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(compilationDef,
                                                                         references:={MscorlibRef, SystemRef, SystemCoreRef},
                                                                         options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))

            Dim ExpectedOutput = <![CDATA[Public Sub Main        
            On Error GoTo foo
    foo:
        End Sub]]>


            compilation.VerifyEmitDiagnostics(Diagnostic(ERRID.ERR_PlatformDoesntSupport, ExpectedOutput).WithArguments("Unstructured exception handling").WithLocation(2, 9))
        End Sub

        <Fact()>
        Public Sub Error_ErrorHandler_InVBCore_LateBound1()
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
Module Module1
    Dim a As Object

    Sub Main()
        a = New ABC
        a = a + 1
        
        a = a &amp; "test"
    End Sub
End Module

Class ABC

End Class
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(compilationDef,
                                                                         references:={MscorlibRef, SystemRef, SystemCoreRef},
                                                                         options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))


            compilation.VerifyEmitDiagnostics(Diagnostic(ERRID.ERR_PlatformDoesntSupport, "a + 1").WithArguments("Late binding").WithLocation(6, 13),
                                          Diagnostic(ERRID.ERR_PlatformDoesntSupport, "a & ""test""").WithArguments("Late binding").WithLocation(8, 13)
                                          )

        End Sub

        <Fact()>
        Public Sub Error_ErrorHandler_InVBCore_LikeOperator()
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
Module Module1

    Sub Main()
        Dim testCheck As Boolean      
        testCheck = "F" Like "F"
    End Sub
End Module
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(compilationDef,
                                                                         references:={MscorlibRef, SystemRef, SystemCoreRef},
                                                                         options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))

            Dim ExpectedOutput = <![CDATA["F" Like "F"]]>

            compilation.VerifyEmitDiagnostics(Diagnostic(ERRID.ERR_PlatformDoesntSupport, ExpectedOutput).WithArguments("Like operator").WithLocation(5, 21))
        End Sub

        <Fact()>
        Public Sub Error_ErrorHandler_InVBCore_ErrObject()
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
        Module Module1

            Sub Main()
                 Error 1
            End Sub
        End Module
            </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(compilationDef,
                                                                         references:={MscorlibRef, SystemRef, SystemCoreRef},
                                                                         options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))

            compilation.VerifyEmitDiagnostics(Diagnostic(ERRID.ERR_PlatformDoesntSupport, "Error 1").WithArguments("Unstructured exception handling").WithLocation(4, 18))
        End Sub

        <Fact()>
        Public Sub Error_ErrorHandler_InVBCore_AnonymousType()
            Dim source =
    <compilation>
        <file name="a.vb">
Module Module1
    Dim a As Object

    Sub Main()
        a = "1"
        Dim x = New With {.a = a, .b = a + 1}
    End Sub

End Module

            </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(source,
                                                                         references:={MscorlibRef, SystemRef, SystemCoreRef},
                                                                         options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))

            compilation.VerifyEmitDiagnostics(Diagnostic(ERRID.ERR_PlatformDoesntSupport, "a + 1").WithArguments("Late binding").WithLocation(6, 40))
        End Sub


        <Fact(), WorkItem(545772, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545772")>
        Public Sub VbCoreMyNamespace()
            Dim source =
<compilation>
    <file name="a.vb">
Module Module1
        Public Sub Main()
            My.Computer.FileSystem.WriteAllText("Test.txt","abc")
        End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(source,
                                                                         references:={MscorlibRef, SystemRef, SystemCoreRef},
                                                                         options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))

            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_PlatformDoesntSupport, "My").WithArguments("My").WithLocation(3, 13))
        End Sub

        <Fact()>
        Public Sub Error_ErrorHandler_OutsideOfMethodBody()
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
    Module Module1   
        Sub Main

        End Sub

        'Error Outside of Method Body
        On Error Goto foo

        Sub Foo
        End Sub
    End Module 
    </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "On Error Goto foo"))
        End Sub

        <Fact()>
        Public Sub ErrorHandler_In_Different_Types()
            'Basic Validation that this is permissible in Class/Structure/(Module Tested elsewhere)
            'Generic
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
    Imports System

    Module Module1   
        Sub Main        
        End Sub

      Class Foo
            Sub Method()
                On Error GoTo CLassMethodLabel

    CLassMethodLabel:
            End Sub

            Public Property ABC As String
                Set(value As String)
                    On Error GoTo setLabel
    setLabel:
                End Set
                Get
                    On Error GoTo getLabel
    getLabel:

                End Get
            End Property
        End Class


    Structure Foo_Struct
            Sub Method()
                On Error GoTo StructMethodLabel

    StructMethodLabel:
            End Sub

            Public Property ABC As String
                Set(value As String)
                    On Error GoTo SetLabel
    setLabel:
                End Set
                Get
                    On Error GoTo getLabel
    getLabel:

                End Get
            End Property
        End Structure 

        Class GenericFoo(Of t)
            Sub Method()
                'Normal Method In Generic Class
                On Error GoTo CLassMethodLabel

    CLassMethodLabel:

            End Sub

            Sub GenericMethod(Of u)(x As u)
                'Generic Method In Generic Class
                On Error GoTo CLassMethodLabel

    CLassMethodLabel:

            End Sub
            Public Property ABC As String
                Set(value As String)
                    On Error GoTo setLabel
    setLabel:
                End Set
                Get
                    On Error GoTo getLabel
    getLabel:

                End Get
            End Property
        End Class
    End Module 
    </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            compilation.AssertNoDiagnostics()
        End Sub

        <Fact()>
        Public Sub ErrorHandler_Other_Constructor_Dispose()
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
    Imports System

    Module Module1   
        Sub Main        
            Dim X As New TestInConstructor

            Dim X2 As New TestInDisposeAndFinalize
            X2.Dispose()
        End Sub
    End Module

        Class TestInConstructor
            Sub New()
                On Error GoTo constructorError
    ConstructorError:

            End Sub
        End Class

        Class TestInDisposeAndFinalize
            Implements IDisposable

            Sub New()
            End Sub

    #Region "IDisposable Support"
            Private disposedValue As Boolean ' To detect redundant calls

            ' IDisposable
            Protected Overridable Sub Dispose(disposing As Boolean)
                On Error GoTo ConstructorError

                If Not Me.disposedValue Then
                    If disposing Then
                        ' TODO: dispose managed state (managed objects).
                    End If

                    ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                    ' TODO: set large fields to null.
                End If
                Me.disposedValue = True

    ConstructorError:

            End Sub

            ' TODO: override Finalize() only if Dispose(ByVal disposing As Boolean) above has code to free unmanaged resources.
            Protected Overrides Sub Finalize()
                On Error GoTo FInalizeError
                ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
                Dispose(False)
                MyBase.Finalize()
    FInalizeError:

            End Sub

            ' This code added by Visual Basic to correctly implement the disposable pattern.
            Public Sub Dispose() Implements IDisposable.Dispose
                ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
                Dispose(True)
                GC.SuppressFinalize(Me)
            End Sub
    #End Region
        End Class
    </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            compilation.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub Error_InvalidTypes_ImplicitConversions()

            Dim compilationDef =
    <compilation>
        <file name="a.vb">
    Imports System

    Module Module1   
        Sub Main        
            Error 1L    
            Error 2S
            Error &quot;3&quot;
            Error 4!      
            Error 5%
        End Sub
        End Module
    </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            compilation.AssertNoDiagnostics()
        End Sub

        <Fact()>
        Public Sub Error_InvalidTypes_InvalidTypes_StrictOn()
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
    Option Strict On

    Module Module1   
        Sub Main        
            Error 1L    
            Error 2S
            Error &quot;3&quot;
            Error 4!      
            Error 5%
        End Sub
        End Module
    </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, """3""").WithArguments("String", "Integer"),
                                          Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "4!").WithArguments("Single", "Integer"))
        End Sub

        <Fact()>
        Public Sub ErrorHandler_Error_InSyncLockBlock()
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
    Class LockClass
    End Class

    Module Module1
        Sub Main()
            Dim lock As New LockClass

            On Error GoTo handler

            SyncLock lock
                On Error GoTo foo

    foo:
                Resume Next
            End SyncLock
            Exit Sub
        End Sub
    End Module
    </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_LabelNotDefined1, "handler").WithArguments("handler"),
                                          Diagnostic(ERRID.ERR_OnErrorInSyncLock, "On Error GoTo foo"))
        End Sub

        <Fact()>
        Public Sub ErrorHandler_Error_InMethodWithSyncLockBlock()
            'Method has a Error Handler and Error Occurs within SyncLock
            'resume next will occur outside of the SyncLock Block
            Dim compilationDef =
    <compilation>
        <file name="a.vb">
    Imports System

    Class LockClass
    End Class

    Module Module1
        Sub Main()
            Dim lock As New LockClass
            Console.WriteLine("Start")
            On Error GoTo handler

            SyncLock lock
                Console.WriteLine("In SyncLock")
                Error 1
                Console.WriteLine("After Error In SyncLock")
            End SyncLock

            Console.WriteLine("End")
            Exit Sub
    handler:
            Console.WriteLine("Handler")
            Resume Next
        End Sub
    End Module
    </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            compilation.VerifyDiagnostics()
            Dim CompilationVerifier = CompileAndVerify(compilation, expectedOutput:=<![CDATA[Start
In SyncLock
Handler
End]]>)

            CompilationVerifier.VerifyIL("Module1.Main", <![CDATA[
{
  // Code size      248 (0xf8)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                Integer V_2,
                LockClass V_3, //lock
                Object V_4,
                Boolean V_5)
  .try
  {
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.2
    IL_0002:  newobj     "Sub LockClass..ctor()"
    IL_0007:  stloc.3
    IL_0008:  ldc.i4.2
    IL_0009:  stloc.2
    IL_000a:  ldstr      "Start"
    IL_000f:  call       "Sub System.Console.WriteLine(String)"
    IL_0014:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0019:  ldc.i4.2
    IL_001a:  stloc.0
    IL_001b:  ldc.i4.4
    IL_001c:  stloc.2
    IL_001d:  ldloc.3
    IL_001e:  stloc.s    V_4
    IL_0020:  ldc.i4.0
    IL_0021:  stloc.s    V_5
    .try
    {
      IL_0023:  ldloc.s    V_4
      IL_0025:  ldloca.s   V_5
      IL_0027:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
      IL_002c:  ldstr      "In SyncLock"
      IL_0031:  call       "Sub System.Console.WriteLine(String)"
      IL_0036:  ldc.i4.1
      IL_0037:  call       "Function Microsoft.VisualBasic.CompilerServices.ProjectData.CreateProjectError(Integer) As System.Exception"
      IL_003c:  throw
    }
    finally
    {
      IL_003d:  ldloc.s    V_5
      IL_003f:  brfalse.s  IL_0048
      IL_0041:  ldloc.s    V_4
      IL_0043:  call       "Sub System.Threading.Monitor.Exit(Object)"
      IL_0048:  endfinally
    }
    IL_0049:  ldc.i4.5
    IL_004a:  stloc.2
    IL_004b:  ldstr      "End"
    IL_0050:  call       "Sub System.Console.WriteLine(String)"
    IL_0055:  br.s       IL_0078
    IL_0057:  ldc.i4.7
    IL_0058:  stloc.2
    IL_0059:  ldstr      "Handler"
    IL_005e:  call       "Sub System.Console.WriteLine(String)"
    IL_0063:  ldc.i4.8
    IL_0064:  stloc.2
    IL_0065:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_006a:  ldloc.1
    IL_006b:  brtrue.s   IL_007a
    IL_006d:  ldc.i4     0x800a0014
    IL_0072:  call       "Function Microsoft.VisualBasic.CompilerServices.ProjectData.CreateProjectError(Integer) As System.Exception"
    IL_0077:  throw
    IL_0078:  leave.s    IL_00ef
    IL_007a:  ldloc.1
    IL_007b:  ldc.i4.1
    IL_007c:  add
    IL_007d:  ldc.i4.0
    IL_007e:  stloc.1
    IL_007f:  switch    (
        IL_00ac,
        IL_0000,
        IL_0008,
        IL_0014,
        IL_001b,
        IL_0049,
        IL_0078,
        IL_0057,
        IL_0063,
        IL_0078)
    IL_00ac:  leave.s    IL_00e4
    IL_00ae:  ldloc.2
    IL_00af:  stloc.1
    IL_00b0:  ldloc.0
    IL_00b1:  switch    (
        IL_00c2,
        IL_007a,
        IL_0057)
    IL_00c2:  leave.s    IL_00e4
  }
  filter
  {
    IL_00c4:  isinst     "System.Exception"
    IL_00c9:  ldnull
    IL_00ca:  cgt.un
    IL_00cc:  ldloc.0
    IL_00cd:  ldc.i4.0
    IL_00ce:  cgt.un
    IL_00d0:  and
    IL_00d1:  ldloc.1
    IL_00d2:  ldc.i4.0
    IL_00d3:  ceq
    IL_00d5:  and
    IL_00d6:  endfilter
  }  // end filter
  {  // handler
    IL_00d8:  castclass  "System.Exception"
    IL_00dd:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00e2:  leave.s    IL_00ae
  }
  IL_00e4:  ldc.i4     0x800a0033
  IL_00e9:  call       "Function Microsoft.VisualBasic.CompilerServices.ProjectData.CreateProjectError(Integer) As System.Exception"
  IL_00ee:  throw
  IL_00ef:  ldloc.1
  IL_00f0:  brfalse.s  IL_00f7
  IL_00f2:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_00f7:  ret
}
]]>)
        End Sub

    End Class

End Namespace
