﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Reflection
Imports System.Threading
Imports Microsoft.CodeAnalysis.Scripting.Hosting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Scripting.VisualBasic
    Friend NotInheritable Class VisualBasicScriptCompiler
        Inherits ScriptCompiler

        Public Shared ReadOnly Instance As ScriptCompiler = New VisualBasicScriptCompiler()

        Private Shared ReadOnly s_defaultInteractive As VisualBasicParseOptions = New VisualBasicParseOptions(languageVersion:=LanguageVersion.VisualBasic11, kind:=SourceCodeKind.Interactive)
        Private Shared ReadOnly s_defaultScript As VisualBasicParseOptions = New VisualBasicParseOptions(languageVersion:=LanguageVersion.VisualBasic11, kind:=SourceCodeKind.Script)
        Private Shared ReadOnly s_vbRuntimeReference As MetadataReference = MetadataReference.CreateFromAssemblyInternal(GetType(CompilerServices.NewLateBinding).GetTypeInfo().Assembly)

        Private Sub New()
        End Sub

        Public Overrides ReadOnly Property DiagnosticFormatter As DiagnosticFormatter
            Get
                Return VisualBasicDiagnosticFormatter.Instance
            End Get
        End Property

        Public Overrides ReadOnly Property IdentifierComparer As StringComparer
            Get
                Return CaseInsensitiveComparison.Comparer
            End Get
        End Property

        Public Overrides Function IsCompleteSubmission(tree As SyntaxTree) As Boolean
            ' TODO: https://github.com/dotnet/roslyn/issues/5235
            Return True
        End Function

        Public Overrides Function ParseSubmission(text As SourceText, cancellationToken As CancellationToken) As SyntaxTree
            Return SyntaxFactory.ParseSyntaxTree(text, s_defaultInteractive, cancellationToken:=cancellationToken)
        End Function

        Private Shared Function GetGlobalImportsForCompilation(script As Script) As IEnumerable(Of GlobalImport)
            ' TODO: remember these per options instance so we don't need to reparse each submission
            ' TODO: get imports out of compilation???
            Return script.Options.Namespaces.Select(Function(n) GlobalImport.Parse(n))
        End Function

        Public Overrides Function CreateSubmission(script As Script) As Compilation
            Dim previousSubmission As Compilation = Nothing
            If script.Previous IsNot Nothing Then
                previousSubmission = script.Previous.GetCompilation()
            End If

            Dim diagnostics = DiagnosticBag.GetInstance()
            Dim references = script.GetReferencesForCompilation(MessageProvider.Instance, diagnostics, s_vbRuntimeReference)

            '  TODO report Diagnostics
            diagnostics.Free()

            ' parse:
            Dim parseOptions = If(script.Options.IsInteractive, s_defaultInteractive, s_defaultScript)
            Dim tree = VisualBasicSyntaxTree.ParseText(script.Code, parseOptions, script.Options.Path)

            ' create compilation:
            Dim assemblyName As String = Nothing
            Dim submissionTypeName As String = Nothing
            script.Builder.GenerateSubmissionId(assemblyName, submissionTypeName)

            Dim globalImports = GetGlobalImportsForCompilation(script)

            Dim submission = VisualBasicCompilation.CreateSubmission(
                assemblyName,
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
                    xmlReferenceResolver:=Nothing, ' don't support XML file references in interactive (permissions & doc comment includes)
                    sourceReferenceResolver:=SourceFileResolver.Default,
                    metadataReferenceResolver:=script.Options.MetadataResolver,
                    assemblyIdentityComparer:=DesktopAssemblyIdentityComparer.Default),
                previousSubmission,
                script.ReturnType,
                script.GlobalsType)

            Return submission
        End Function
    End Class
End Namespace