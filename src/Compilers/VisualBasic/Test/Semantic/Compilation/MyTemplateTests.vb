﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.ObjectModel
Imports System.Globalization
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class MyTemplateTests
        Inherits BasicTestBase

        Friend Shared Function GetMyTemplateTree(compilation As VisualBasicCompilation) As SyntaxTree
            Dim MyTemplate As SyntaxTree = Nothing

            For Each tree In compilation.AllSyntaxTrees
                If tree.IsMyTemplate Then
                    ' should be only one My template
                    Assert.Null(MyTemplate)
                    MyTemplate = tree
                End If
            Next

            Return MyTemplate
        End Function

        <Fact()>
        Public Sub LoadMyTemplate()


            Dim sources = <compilation>
                              <file name="c.vb"><![CDATA[
Module M1
    Sub Main
    End Sub
End Class

    ]]></file>
                          </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndReferences(sources,
                references:={MsvbRef},
                options:=TestOptions.ReleaseDll)

            Dim MyTemplate = GetMyTemplateTree(compilation)

            Assert.NotNull(MyTemplate)

            Dim text = MyTemplate.GetText.ToString
            Assert.Contains("Private ReadOnly m_Context As New Global.Microsoft.VisualBasic.MyServices.Internal.ContextValue(Of T)", text, StringComparison.Ordinal)

        End Sub

        <Fact()>
        Public Sub LoadMyTemplateNoRuntime()


            Dim sources = <compilation>
                              <file name="c.vb"><![CDATA[
Module M1
    Sub Main
    End Sub
End Class

    ]]></file>
                          </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndReferences(sources,
                references:={SystemCoreRef},
                options:=TestOptions.ReleaseDll)

            Dim MyTemplate = GetMyTemplateTree(compilation)

            Dim text = MyTemplate.GetText.ToString
            Assert.Contains("Private ReadOnly m_Context As New Global.Microsoft.VisualBasic.MyServices.Internal.ContextValue(Of T)", text, StringComparison.Ordinal)
        End Sub

        <Fact()>
        Public Sub LoadMyTemplateRuntimeNotFile()


            Dim sources = <compilation>
                              <file name="c.vb"><![CDATA[
Module M1
    Sub Main
    End Sub
End Class

    ]]></file>
                          </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndReferences(sources,
                references:={SystemCoreRef, MsvbRef},
                options:=TestOptions.ReleaseDll)

            Dim MyTemplate = GetMyTemplateTree(compilation)

            Dim text = MyTemplate.GetText.ToString
            Assert.Contains("Private ReadOnly m_Context As New Global.Microsoft.VisualBasic.MyServices.Internal.ContextValue(Of T)", text, StringComparison.Ordinal)
        End Sub

        <Fact()>
        Public Sub MyConsoleApp()


            Dim sources = <compilation>
                              <file name="c.vb"><![CDATA[

Imports System

Module Module1

    Sub Main()
        Console.WriteLine(My.Application.IsNetworkDeployed)
    End Sub

End Module


    ]]></file>
                          </compilation>



            Dim defines = PredefinedPreprocessorSymbols.AddPredefinedPreprocessorSymbols(OutputKind.ConsoleApplication)
            defines = defines.Add(KeyValuePair.Create("_MyType", CObj("Console")))

            Dim parseOptions = New VisualBasicParseOptions(preprocessorSymbols:=defines)
            Dim compilationOptions = TestOptions.ReleaseExe.WithParseOptions(parseOptions)

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(sources, options:=compilationOptions)

            CompileAndVerify(compilation, expectedOutput:="False")

        End Sub

        <ConditionalFact(GetType(HasValidFonts))>
        Public Sub MyWinformApp()
            Dim sources = <compilation>
                              <file name="c.vb"><![CDATA[

Imports System

Module m1
    Function Test As String
        Return My.Forms.Form1.Text
    End Function
End Module

Public Class Form1
    Private Sub Form1_Load( sender As Object,  e As EventArgs) Handles MyBase.Load
        Console.WriteLine(Test)
        Me.Close
    End Sub
End Class

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.SuspendLayout
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6!, 13!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(292, 273)
        Me.Name = "Form1"
        Me.Text = "HelloWinform"
        Me.WindowState = System.Windows.Forms.FormWindowState.Minimized
        Me.ResumeLayout(false)

End Sub

End Class


    ]]></file>
                          </compilation>



            Dim defines = PredefinedPreprocessorSymbols.AddPredefinedPreprocessorSymbols(OutputKind.WindowsApplication)
            defines = defines.Add(KeyValuePair.Create("_MyType", CObj("WindowsForms")))

            Dim parseOptions = New VisualBasicParseOptions(preprocessorSymbols:=defines)
            Dim compilationOptions = TestOptions.ReleaseExe.WithOutputKind(OutputKind.WindowsApplication).WithParseOptions(parseOptions).WithMainTypeName("Form1")

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(sources, {SystemWindowsFormsRef, SystemDrawingRef}, compilationOptions)
            compilation.VerifyDiagnostics()

            CompileAndVerify(compilation, expectedOutput:="HelloWinform")
        End Sub

        <Fact()>
        Public Sub MyApplicationSemanticInfo()

            Dim sources = <compilation>
                              <file name="a.vb"><![CDATA[
Imports System

Module Module1

    Sub Main()
        Console.WriteLine(My.Application.IsNetworkDeployed)'BIND:"Application"
    End Sub

End Module
    ]]></file>
                          </compilation>



            Dim defines = PredefinedPreprocessorSymbols.AddPredefinedPreprocessorSymbols(OutputKind.ConsoleApplication)
            defines = defines.Add(KeyValuePair.Create("_MyType", CObj("Console")))

            Dim parseOptions = New VisualBasicParseOptions(preprocessorSymbols:=defines)
            Dim compilationOptions = TestOptions.ReleaseExe.WithParseOptions(parseOptions)

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(sources, options:=compilationOptions)


            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("My.MyApplication", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("My.MyApplication", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)
            Assert.Equal(0, semanticSummary.MemberGroup.Length)
            Assert.False(semanticSummary.ConstantValue.HasValue)

            Dim sym = semanticSummary.Symbol

            Assert.IsType(Of MyTemplateLocation)(sym.Locations(0))
            Assert.True(sym.DeclaringSyntaxReferences.IsEmpty)

        End Sub

        <Fact()>
        Public Sub MySettingExtraMember()

            Dim sources = <compilation>
                              <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
        My.Application.Foo()'BIND:"Foo"
    End Sub
End Module

Namespace My
    Partial Class MyApplication
        Public Function Foo() As Integer
            Return 1
        End Function
    End Class
End Namespace

    ]]></file>
                          </compilation>



            Dim defines = PredefinedPreprocessorSymbols.AddPredefinedPreprocessorSymbols(OutputKind.ConsoleApplication)
            defines = defines.Add(KeyValuePair.Create("_MyType", CObj("Console")))

            Dim parseOptions = New VisualBasicParseOptions(preprocessorSymbols:=defines)
            Dim compilationOptions = TestOptions.ReleaseExe.WithParseOptions(parseOptions)

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(sources, options:=compilationOptions)


            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Function My.MyApplication.Foo() As System.Int32", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function My.MyApplication.Foo() As System.Int32", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)

            Dim sym = semanticSummary.Symbol

            Assert.IsType(Of SourceLocation)(sym.Locations(0))
            Assert.Equal("Public Function Foo() As Integer", sym.DeclaringSyntaxReferences(0).GetSyntax().ToString())

            Dim parent = sym.ContainingType
            Assert.Equal(1, parent.Locations.OfType(Of SourceLocation).Count)
            Assert.Equal(1, parent.Locations.OfType(Of MyTemplateLocation).Count)

            Assert.Equal("Partial Class MyApplication", parent.DeclaringSyntaxReferences.Single.GetSyntax.ToString)

        End Sub

    End Class
End Namespace
