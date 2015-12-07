' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports Microsoft.CodeAnalysis.Editor.Interactive
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Interactive
Imports Microsoft.VisualStudio.Text.Classification
Imports Microsoft.VisualStudio.Utilities
Imports Microsoft.VisualStudio.InteractiveWindow.Commands

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Interactive

    Friend NotInheritable Class VisualBasicInteractiveEvaluator
        Inherits InteractiveEvaluator

        Private Shared ReadOnly s_parseOptions As ParseOptions = New VisualBasicParseOptions(languageVersion:=LanguageVersion.VisualBasic11, kind:=SourceCodeKind.Script)

        Private Const s_interactiveResponseFile As String = "VisualBasicInteractive.rsp"

        Public Sub New(hostServices As HostServices,
                       classifierAggregator As IViewClassifierAggregatorService,
                       commandsFactory As IInteractiveWindowCommandsFactory,
                       commands As ImmutableArray(Of IInteractiveWindowCommand),
                       contentTypeRegistry As IContentTypeRegistryService,
                       responseFileDirectory As String,
                       initialWorkingDirectory As String)

            MyBase.New(contentTypeRegistry.GetContentType(ContentTypeNames.VisualBasicContentType),
                       hostServices,
                       classifierAggregator,
                       commandsFactory,
                       commands,
                       Path.Combine(responseFileDirectory, s_interactiveResponseFile),
                       initialWorkingDirectory,
                       GetType(InteractiveHostEntryPoint).Assembly.Location,
                       GetType(VisualBasicReplServiceProvider))
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Protected Overrides ReadOnly Property ParseOptions As ParseOptions
            Get
                Return s_parseOptions
            End Get
        End Property

        Protected Overrides ReadOnly Property CommandLineParser As CommandLineParser
            Get
                Return VisualBasicCommandLineParser.ScriptRunner
            End Get
        End Property

        Protected Overrides Function GetSubmissionCompilationOptions(name As String, metadataReferenceResolver As MetadataReferenceResolver, sourceReferenceResolver As SourceReferenceResolver, [imports] As ImmutableArray(Of String)) As CompilationOptions
            Dim globalImports = [imports].Select(Function(import) GlobalImport.Parse(import))
            Return New VisualBasicCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                scriptClassName:=name,
                globalImports:=globalImports,
                metadataReferenceResolver:=metadataReferenceResolver,
                sourceReferenceResolver:=sourceReferenceResolver,
                assemblyIdentityComparer:=DesktopAssemblyIdentityComparer.Default)
        End Function

        Public Overrides Function CanExecuteCode(text As String) As Boolean
            If MyBase.CanExecuteCode(text) Then
                Return True
            End If

            Return SyntaxFactory.IsCompleteSubmission(SyntaxFactory.ParseSyntaxTree(text, options:=ParseOptions))
        End Function
    End Class
End Namespace

