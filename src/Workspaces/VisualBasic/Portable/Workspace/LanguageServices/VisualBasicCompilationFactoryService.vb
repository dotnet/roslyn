' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageService(GetType(ICompilationFactoryService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicCompilationFactoryService
        Implements ICompilationFactoryService

        Private Shared ReadOnly s_defaultOptions As New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, concurrentBuild:=False)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overloads Function CreateCompilation(
            assemblyName As String,
            options As CompilationOptions) As Compilation Implements ICompilationFactoryService.CreateCompilation

            Return VisualBasicCompilation.Create(
                assemblyName,
                options:=If(DirectCast(options, VisualBasicCompilationOptions), s_defaultOptions))
        End Function

        Public Function CreateSubmissionCompilation(
            assemblyName As String,
            options As CompilationOptions,
            hostObjectType As Type) As Compilation Implements ICompilationFactoryService.CreateSubmissionCompilation

#If TODO Then ' https://github.com/dotnet/roslyn/issues/9063
            Return VisualBasicCompilation.CreateScriptCompilation(
                assemblyName,
                options:=DirectCast(options, VisualBasicCompilationOptions),
                globalsType:=hostObjectType)
#Else
            Throw New NotImplementedException()
#End If
        End Function

        Public Function GetDefaultCompilationOptions() As CompilationOptions Implements ICompilationFactoryService.GetDefaultCompilationOptions
            Return s_defaultOptions
        End Function

        Public Function TryParsePdbCompilationOptions(metadata As IReadOnlyDictionary(Of String, String)) As CompilationOptions Implements ICompilationFactoryService.TryParsePdbCompilationOptions
            Dim outputKindString As String = Nothing
            Dim outputKind As OutputKind

            If Not metadata.TryGetValue("output-kind", outputKindString) OrElse
               Not [Enum].TryParse(outputKindString, outputKind) Then
                Return Nothing
            End If

            Return New VisualBasicCompilationOptions(outputKind)
        End Function

        Public Function CreateGeneratorDriver(
                parseOptions As ParseOptions,
                generators As ImmutableArray(Of ISourceGenerator),
                optionsProvider As AnalyzerConfigOptionsProvider,
                additionalTexts As ImmutableArray(Of AdditionalText),
                generatedFilesBaseDirectory As String,
                projectName As String) As GeneratorDriver Implements ICompilationFactoryService.CreateGeneratorDriver
            Return VisualBasicGeneratorDriver.Create(generators, additionalTexts, DirectCast(parseOptions, VisualBasicParseOptions), optionsProvider, New GeneratorDriverOptions(baseDirectory:=generatedFilesBaseDirectory, projectName:=projectName))
        End Function
    End Class
End Namespace
