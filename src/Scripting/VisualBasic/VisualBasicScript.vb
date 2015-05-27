' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Scripting.VisualBasic

    Public NotInheritable Class VisualBasicScript
        Inherits Script

        Private Sub New(code As String, path As String, options As ScriptOptions, globalsType As Type, returnType As Type, builder As ScriptBuilder, previous As Script)
            MyBase.New(code, path, options, globalsType, returnType, builder, previous)
        End Sub

        Friend Overrides Function Make(code As String, path As String, options As ScriptOptions, globalsType As Type, returnType As Type, builder As ScriptBuilder, previous As Script) As Script
            Return New VisualBasicScript(code, path, options, globalsType, returnType, builder, previous)
        End Function

        ''' <summary>
        ''' Create a new Visual Basic script.
        ''' </summary>
        Public Shared Function Create(code As String, options As ScriptOptions) As Script
            Return New VisualBasicScript(code, Nothing, options, Nothing, Nothing, Nothing, Nothing)
        End Function

        ''' <summary>
        ''' Create a new Visual Basic script.
        ''' </summary>
        Public Shared Function Create(code As String) As Script
            Return New VisualBasicScript(code, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script.
        ''' </summary>
        Public Overloads Shared Function Run(code As String, options As ScriptOptions, globals As Object) As ScriptState
            Return Create(code, options).Run(globals)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script.
        ''' </summary>
        Public Overloads Shared Function Run(code As String, options As ScriptOptions) As ScriptState
            Return Create(code, options).Run(Nothing)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script.
        ''' </summary>
        Public Overloads Shared Function Run(code As String, globals As Object) As ScriptState
            Return Create(code, Nothing).Run(globals)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script.
        ''' </summary>
        Public Overloads Shared Function Run(code As String) As ScriptState
            Return Create(code, Nothing).Run(Nothing)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script and return its resulting value.
        ''' </summary>
        Public Shared Function Eval(code As String, options As ScriptOptions, globals As Object) As Object
            Return Run(code, options, globals).ReturnValue
        End Function

        ''' <summary>
        ''' Run a Visual Basic script and return its resulting value.
        ''' </summary>
        Public Shared Function Eval(code As String, options As ScriptOptions) As Object
            Return Run(code, options).ReturnValue
        End Function

        ''' <summary>
        ''' Run a Visual Basic script and return its resulting value.
        ''' </summary>
        Public Shared Function Eval(code As String, globals As Object) As Object
            Return Run(code, globals).ReturnValue
        End Function

        ''' <summary>
        ''' Run a Visual Basic script and return its resulting value.
        ''' </summary>
        Public Shared Function Eval(code As String) As Object
            Return Run(code).ReturnValue
        End Function

#Region "Compilation"
        Private Shared ReadOnly s_defaultInteractive As VisualBasicParseOptions = New VisualBasicParseOptions(languageVersion:=LanguageVersion.VisualBasic11, kind:=SourceCodeKind.Interactive)
        Private Shared ReadOnly s_defaultScript As VisualBasicParseOptions = New VisualBasicParseOptions(languageVersion:=LanguageVersion.VisualBasic11, kind:=SourceCodeKind.Script)

        Protected Overrides Function CreateCompilation() As Compilation

            Dim previousSubmission As Compilation = Nothing
            If Me.Previous IsNot Nothing Then
                previousSubmission = Me.Previous.GetCompilation()
            End If

            Dim references = Me.GetReferencesForCompilation()

            Dim globalImports = Me.GetGlobalImportsForCompilation()

            ' parse:
            Dim parseOptions = If(Me.Options.IsInteractive, s_defaultInteractive, s_defaultScript)
            Dim tree = VisualBasicSyntaxTree.ParseText(Me.Code, parseOptions, Me.Path)

            ' create compilation:
            Dim assemblyName As String = Nothing
            Dim submissionTypeName As String = Nothing
            Me.Builder.GenerateSubmissionId(assemblyName, submissionTypeName)

            Dim submission = VisualBasicCompilation.CreateSubmission(assemblyName,
                tree,
                references,
                New VisualBasicCompilationOptions(
                    outputKind:=OutputKind.DynamicallyLinkedLibrary,
                    mainTypeName:=Nothing,
                    scriptClassName:=submissionTypeName,
                    globalImports:=globalImports,
                    rootNamespace:="",
                    optionStrict:=OptionStrict.Off,
                    optionInfer:=True,
                    optionExplicit:=True,
                    optionCompareText:=False,
                    embedVbCoreRuntime:=False,
                    checkOverflow:=False,
                    metadataReferenceResolver:=Me.Options.ReferenceResolver,
                    assemblyIdentityComparer:=DesktopAssemblyIdentityComparer.Default),
                previousSubmission,
                Me.ReturnType,
                Me.GlobalsType)

            Return submission
        End Function

        Private Function GetGlobalImportsForCompilation() As IEnumerable(Of GlobalImport)
            ' TODO: remember these per options instance so we don't need to reparse each submission
            ' TODO: get imports out of compilation???
            Return Me.Options.Namespaces.Select(Function(n) GlobalImport.Parse(n))
        End Function

        Protected Overrides Function FormatDiagnostic(diagnostic As Diagnostic, culture As CultureInfo) As String
            Return VisualBasicDiagnosticFormatter.Instance.Format(diagnostic, culture)
        End Function

#End Region

    End Class
End Namespace

