' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageService(GetType(ICommandLineArgumentsFactoryService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicCommandLineArgumentsFactoryService
        Implements ICommandLineArgumentsFactoryService

        Public Function CreateCommandLineArguments(arguments As IEnumerable(Of String), baseDirectory As String, isInteractive As Boolean) As CommandLineArguments Implements ICommandLineArgumentsFactoryService.CreateCommandLineArguments
            Dim parser = If(isInteractive, VisualBasicCommandLineParser.Interactive, VisualBasicCommandLineParser.Default)
            Return parser.Parse(arguments, baseDirectory, RuntimeEnvironment.GetRuntimeDirectory())
        End Function
    End Class
End Namespace
