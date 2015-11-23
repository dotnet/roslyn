' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Scripting
Imports Microsoft.CodeAnalysis.Scripting.Hosting

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
            Return Script.CreateInitialScript(Of T)(VisualBasicScriptCompiler.Instance, code, options, globalsType, assemblyLoader)
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
            Return EvaluateAsync(Of Object)(code, Nothing, globals, cancellationToken)
        End Function
    End Class

End Namespace

