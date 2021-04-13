' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic

    <ExportLanguageService(GetType(ICommandLineParserService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicCommandLineParserService
        Implements ICommandLineParserService

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Function Parse(arguments As IEnumerable(Of String), baseDirectory As String, isInteractive As Boolean, sdkDirectory As String) As CommandLineArguments Implements ICommandLineParserService.Parse
#If SCRIPTING Then
            Dim parser = If(isInteractive, VisualBasicCommandLineParser.Interactive, VisualBasicCommandLineParser.Default)
#Else
            Dim parser = VisualBasicCommandLineParser.Default
#End If
            Return parser.Parse(arguments, baseDirectory, sdkDirectory)
        End Function
    End Class
End Namespace

