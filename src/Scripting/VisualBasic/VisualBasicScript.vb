' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Scripting
Imports Microsoft.CodeAnalysis.Scripting.Hosting
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting

    ''' <summary>
    ''' A factory for creating and running Visual Basic scripts.
    ''' </summary>
    Public NotInheritable Class VisualBasicScript
        Private Sub New()
        End Sub

        ''' <summary>
        ''' Create a new Visual Basic script.
        ''' </summary>
        Public Shared Function Create(Of T)(code As String,
                                            Optional options As ScriptOptions = Nothing,
                                            Optional globalsType As Type = Nothing,
                                            Optional assemblyLoader As InteractiveAssemblyLoader = Nothing) As Script(Of T)
            Return Script.CreateInitialScript(Of T)(VisualBasicScriptCompiler.Instance, SourceText.From(If(code, String.Empty)), options, globalsType, assemblyLoader)
        End Function

        ''' <summary>
        ''' Create a new Visual Basic script.
        ''' </summary>
        Public Shared Function Create(code As String,
                                      Optional options As ScriptOptions = Nothing,
                                      Optional globalsType As Type = Nothing,
                                      Optional assemblyLoader As InteractiveAssemblyLoader = Nothing) As Script(Of Object)
            Return Create(Of Object)(code, options, globalsType, assemblyLoader)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script.
        ''' </summary>
        Public Shared Function RunAsync(Of T)(code As String,
                                              Optional options As ScriptOptions = Nothing,
                                              Optional globals As Object = Nothing,
                                              Optional cancellationToken As CancellationToken = Nothing) As Task(Of ScriptState(Of T))
            Return Create(Of T)(code, options, globals?.GetType()).RunAsync(globals, cancellationToken)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script.
        ''' </summary>
        Public Shared Function RunAsync(code As String,
                                        Optional options As ScriptOptions = Nothing,
                                        Optional globals As Object = Nothing,
                                        Optional cancellationToken As CancellationToken = Nothing) As Task(Of ScriptState(Of Object))
            Return RunAsync(Of Object)(code, options, globals, cancellationToken)
        End Function

        ''' <summary>
        ''' Run a Visual Basic script and return its resulting value.
        ''' </summary>
        Public Shared Function EvaluateAsync(Of T)(code As String,
                                                   Optional options As ScriptOptions = Nothing,
                                                   Optional globals As Object = Nothing,
                                                   Optional cancellationToken As CancellationToken = Nothing) As Task(Of T)
            Return RunAsync(Of T)(code, options, globals, cancellationToken).GetEvaluationResultAsync()
        End Function

        ''' <summary>
        ''' Run a Visual Basic script and return its resulting value.
        ''' </summary>
        Public Shared Function EvaluateAsync(code As String,
                                             Optional options As ScriptOptions = Nothing,
                                             Optional globals As Object = Nothing,
                                             Optional cancellationToken As CancellationToken = Nothing) As Task(Of Object)
            Return EvaluateAsync(Of Object)(code, options, globals, cancellationToken)
        End Function
    End Class

End Namespace

