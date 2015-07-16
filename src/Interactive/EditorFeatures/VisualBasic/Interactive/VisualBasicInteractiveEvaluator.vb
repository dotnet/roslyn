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

    Public NotInheritable Class VisualBasicInteractiveEvaluator
        Inherits InteractiveEvaluator

        Private Shared ReadOnly s_parseOptions As ParseOptions = New VisualBasicParseOptions(languageVersion:=LanguageVersion.VisualBasic11, kind:=SourceCodeKind.Interactive)

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
                       GetType(VisualBasicRepl))
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
                Return VisualBasicCommandLineParser.Interactive
            End Get
        End Property

        Protected Overrides Function GetSubmissionCompilationOptions(name As String, metadataReferenceResolver As MetadataReferenceResolver) As CompilationOptions
            Return New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                            scriptClassName:=name,
                                            metadataReferenceResolver:=metadataReferenceResolver,
                                            assemblyIdentityComparer:=DesktopAssemblyIdentityComparer.Default)
        End Function

        Public Overrides Function CanExecuteCode(text As String) As Boolean
            If MyBase.CanExecuteCode(text) Then
                Return True
            End If

            ' TODO (tomat): Return Syntax.IsCompleteSubmission(SyntaxTree.ParseCompilationUnit(text, options:=ParseOptions))
            Return True
        End Function
    End Class
End Namespace

