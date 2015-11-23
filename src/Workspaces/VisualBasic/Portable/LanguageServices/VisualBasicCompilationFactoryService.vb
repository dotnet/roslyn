' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageService(GetType(ICompilationFactoryService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicCompilationFactoryService
        Implements ICompilationFactoryService

        Private Shared ReadOnly s_defaultOptions As New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, concurrentBuild:=False)

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
            ' https://github.com/dotnet/roslyn/issues/5944
            Return VisualBasicCompilation.CreateScriptCompilation(
                assemblyName,
                options:=DirectCast(options, VisualBasicCompilationOptions),
                globalsType:=hostObjectType)
        End Function

        Private Function ICompilationFactoryService_GetCompilationFromCompilationReference(reference As MetadataReference) As Compilation Implements ICompilationFactoryService.GetCompilationFromCompilationReference
            Return GetCompilationFromCompilationReference(reference)
        End Function

        Private Overloads Function GetCompilationFromCompilationReference(reference As MetadataReference) As Compilation
            Dim cref = TryCast(reference, CompilationReference)

            If cref IsNot Nothing Then
                Return cref.Compilation
            End If

            Return Nothing
        End Function

        Public Function IsCompilationReference(reference As MetadataReference) As Boolean Implements ICompilationFactoryService.IsCompilationReference
            Return TypeOf reference Is CompilationReference
        End Function

        Public Function GetDefaultCompilationOptions() As CompilationOptions Implements ICompilationFactoryService.GetDefaultCompilationOptions
            Return s_defaultOptions
        End Function
    End Class
End Namespace
