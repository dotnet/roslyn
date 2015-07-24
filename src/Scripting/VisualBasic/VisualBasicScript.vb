' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Scripting.VisualBasic

    ''' <summary>
    ''' A factory for creating and running Visual Basic scripts.
    ''' </summary>
    Public Module VisualBasicScript

        ''' <summary>
        ''' Create a new Visual Basic script.
        ''' </summary>
        Public Function Create(Of T)(code As String, options As ScriptOptions) As Script(Of T)
            Return New VisualBasicScript(Of T)(code, Nothing, options, Nothing, Nothing, Nothing)
        End Function

        ''' <summary>
        ''' Create a new Visual Basic script.
        ''' </summary>
        Public Function Create(code As String, options As ScriptOptions) As Script(Of Object)
            Return New VisualBasicScript(Of Object)(code, Nothing, options, Nothing, Nothing, Nothing)
        End Function

        ''' <summary>
        ''' Create a new Visual Basic script.
        ''' </summary>
        Public Function Create(code As String) As Script(Of Object)
            Return New VisualBasicScript(Of Object)(code, Nothing, Nothing, Nothing, Nothing, Nothing)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script.
        ''' </summary>
        Public Function RunAsync(Of T)(code As String, options As ScriptOptions, globals As Object, Optional cancellationToken As CancellationToken = Nothing) As ScriptState(Of T)
            Return Create(Of T)(code, options).RunAsync(globals, cancellationToken)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script.
        ''' </summary>
        Public Function RunAsync(code As String, options As ScriptOptions, globals As Object, Optional cancellationToken As CancellationToken = Nothing) As ScriptState(Of Object)
            Return RunAsync(Of Object)(code, options, globals, cancellationToken)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script.
        ''' </summary>
        Public Function RunAsync(code As String, options As ScriptOptions, Optional cancellationToken As CancellationToken = Nothing) As ScriptState(Of Object)
            Return RunAsync(Of Object)(code, options, Nothing, cancellationToken)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script.
        ''' </summary>
        Public Function RunAsync(code As String, globals As Object, Optional cancellationToken As CancellationToken = Nothing) As ScriptState(Of Object)
            Return RunAsync(Of Object)(code, Nothing, globals, cancellationToken)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script.
        ''' </summary>
        Public Function RunAsync(code As String, Optional cancellationToken As CancellationToken = Nothing) As ScriptState(Of Object)
            Return RunAsync(Of Object)(code, Nothing, Nothing, cancellationToken)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script and return its resulting value.
        ''' </summary>
        Public Function EvaluateAsync(Of T)(code As String, options As ScriptOptions, globals As Object, Optional cancellationToken As CancellationToken = Nothing) As Task(Of T)
            Return RunAsync(Of T)(code, options, globals, cancellationToken).ReturnValue
        End Function

        ''' <summary>
        ''' Run a Visual Basic script and return its resulting value.
        ''' </summary>
        Public Function EvaluateAsync(code As String, options As ScriptOptions, globals As Object, Optional cancellationToken As CancellationToken = Nothing) As Task(Of Object)
            Return EvaluateAsync(Of Object)(code, options, globals, cancellationToken)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script and return its resulting value.
        ''' </summary>
        Public Function EvaluateAsync(code As String, options As ScriptOptions, Optional cancellationToken As CancellationToken = Nothing) As Task(Of Object)
            Return EvaluateAsync(Of Object)(code, options, Nothing, cancellationToken)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script and return its resulting value.
        ''' </summary>
        Public Function EvaluateAsync(code As String, globals As Object, Optional cancellationToken As CancellationToken = Nothing) As Task(Of Object)
            Return EvaluateAsync(Of Object)(code, Nothing, globals, cancellationToken)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script and return its resulting value.
        ''' </summary>
        Public Function EvaluateAsync(code As String, Optional cancellationToken As CancellationToken = Nothing) As Task(Of Object)
            Return EvaluateAsync(Of Object)(code, Nothing, Nothing, cancellationToken)
        End Function

    End Module

    Friend NotInheritable Class VisualBasicScript(Of T)
        Inherits Script(Of T)

        Friend Sub New(code As String, path As String, options As ScriptOptions, globalsType As Type, builder As ScriptBuilder, previous As Script)
            MyBase.New(code, path, options, globalsType, builder, previous)
        End Sub

        Friend Overrides Function Make(code As String, path As String, options As ScriptOptions, globalsType As Type, builder As ScriptBuilder, previous As Script) As Script(Of T)
            Return New VisualBasicScript(Of T)(code, path, options, globalsType, builder, previous)
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

