' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic

    <ExportLanguageService(GetType(ICommandLineParserService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicCommandLineParserService
        Implements ICommandLineParserService

        <ImportingConstructor>
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

