' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Reflection
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Scripting.VisualBasic
    Friend Class VisualBasicScriptCompiler
        Inherits ScriptCompiler

        Public Shared ReadOnly Instance As ScriptCompiler = New VisualBasicScriptCompiler()

        Private Shared ReadOnly s_defaultInteractive As VisualBasicParseOptions = New VisualBasicParseOptions(languageVersion:=LanguageVersion.VisualBasic11, kind:=SourceCodeKind.Interactive)
        Private Shared ReadOnly s_defaultScript As VisualBasicParseOptions = New VisualBasicParseOptions(languageVersion:=LanguageVersion.VisualBasic11, kind:=SourceCodeKind.Script)

        Private Sub New()
        End Sub

        Private Shared Function GetGlobalImportsForCompilation(script As Script) As IEnumerable(Of GlobalImport)
            ' TODO: remember these per options instance so we don't need to reparse each submission
            ' TODO: get imports out of compilation???
            Return script.Options.Namespaces.Select(Function(n) GlobalImport.Parse(n))
        End Function

        Public Overrides Function CreateSubmission(script As Script) As Compilation
            Dim references = script.GetReferencesForCompilation()

            Dim previousSubmission As Compilation = Nothing

            If script.Previous IsNot Nothing Then
                previousSubmission = script.Previous.GetCompilation()
            Else
                references = references.Add(MetadataReference.CreateFromAssemblyInternal(GetType(CompilerServices.NewLateBinding).GetTypeInfo().Assembly))
            End If

            Dim globalImports = GetGlobalImportsForCompilation(script)

            ' parse:
            Dim parseOptions = If(script.Options.IsInteractive, s_defaultInteractive, s_defaultScript)
            Dim tree = VisualBasicSyntaxTree.ParseText(script.Code, parseOptions, script.Options.Path)

            ' create compilation:
            Dim assemblyName As String = Nothing
            Dim submissionTypeName As String = Nothing
            script.Builder.GenerateSubmissionId(assemblyName, submissionTypeName)

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
                    metadataReferenceResolver:=script.Options.ReferenceResolver,
                    assemblyIdentityComparer:=DesktopAssemblyIdentityComparer.Default),
                previousSubmission,
                script.ReturnType,
                script.GlobalsType)

            Return submission
        End Function

        Public Overrides ReadOnly Property DiagnosticFormatter As DiagnosticFormatter
            Get
                Return VisualBasicDiagnosticFormatter.Instance
            End Get
        End Property
    End Class
End Namespace