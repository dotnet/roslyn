' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Scripting
Imports Microsoft.CodeAnalysis.Scripting.Hosting

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting

    Friend Class VisualBasicReplServiceProvider
        Inherits ReplServiceProvider

        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property CommandLineParser As CommandLineParser
            Get
                Return VisualBasicCommandLineParser.Script
            End Get
        End Property

        Public Overrides ReadOnly Property DiagnosticFormatter As DiagnosticFormatter
            Get
                Return VisualBasicDiagnosticFormatter.Instance
            End Get
        End Property

        Public Overrides ReadOnly Property Logo As String
            Get
                Return String.Format(VBScriptingResources.LogoLine1, CommonCompiler.GetProductVersion(GetType(VisualBasicReplServiceProvider)))
            End Get
        End Property

        Public Overrides ReadOnly Property ObjectFormatter As ObjectFormatter = VisualBasicObjectFormatter.Instance

        Public Overrides Function CreateScript(Of T)(code As String, options As ScriptOptions, globalsTypeOpt As Type, assemblyLoader As InteractiveAssemblyLoader) As Script(Of T)
            Return VisualBasicScript.Create(Of T)(code, options, globalsTypeOpt, assemblyLoader)
        End Function
    End Class
End Namespace

