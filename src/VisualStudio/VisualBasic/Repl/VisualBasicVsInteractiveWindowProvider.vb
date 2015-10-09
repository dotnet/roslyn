' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.IO
Imports Microsoft.CodeAnalysis.Editor.Interactive
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Interactive
Imports Microsoft.CodeAnalysis.Internal.Log
Imports Microsoft.VisualStudio.LanguageServices.Interactive
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Text.Classification
Imports Microsoft.VisualStudio.Utilities
Imports Microsoft.VisualStudio.InteractiveWindow
Imports Microsoft.VisualStudio.InteractiveWindow.Commands
Imports Microsoft.VisualStudio.InteractiveWindow.Shell
Imports LanguageServiceGuids = Microsoft.VisualStudio.LanguageServices.Guids

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Interactive

    <Export(GetType(VisualBasicVsInteractiveWindowProvider))>
    Friend NotInheritable Class VisualBasicVsInteractiveWindowProvider
        Inherits VsInteractiveWindowProvider

        <ImportingConstructor>
        Public Sub New(serviceProvider As SVsServiceProvider,
                       interactiveWindowFactory As IVsInteractiveWindowFactory,
                       classifierAggregator As IViewClassifierAggregatorService,
                       contentTypeRegistry As IContentTypeRegistryService,
                       commandsFactory As IInteractiveWindowCommandsFactory,
                       <ImportMany> commands As IInteractiveWindowCommand(),
                       workspace As VisualStudioWorkspace)

            MyBase.New(serviceProvider, interactiveWindowFactory, classifierAggregator, contentTypeRegistry, commandsFactory, commands, workspace)
        End Sub

        Protected Overrides ReadOnly Property LanguageServiceGuid As Guid
            Get
                Return LanguageServiceGuids.VisualBasicLanguageServiceId
            End Get
        End Property

        Protected Overrides ReadOnly Property Id As Guid
            Get
                Return VisualBasicVsInteractiveWindowPackage.Id
            End Get
        End Property

        Protected Overrides ReadOnly Property Title As String
            Get
                ' Note: intentionally left unlocalized (we treat these words as if they were unregistered trademarks)
                Return "Visual Basic Interactive"
            End Get
        End Property

        Protected Overrides Function CreateInteractiveEvaluator(serviceProvider As SVsServiceProvider,
                                                                classifierAggregator As IViewClassifierAggregatorService,
                                                                contentTypeRegistry As IContentTypeRegistryService,
                                                                workspace As VisualStudioWorkspace) As InteractiveEvaluator
            Return New VisualBasicInteractiveEvaluator(
                workspace.Services.HostServices,
                classifierAggregator,
                CommandsFactory,
                Commands,
                contentTypeRegistry,
                Path.GetDirectoryName(GetType(VisualBasicVsInteractiveWindowPackage).Assembly.Location),
                CommonVsUtils.GetWorkingDirectory())
        End Function

        Protected Overrides ReadOnly Property InteractiveWindowFunctionId As FunctionId
            Get
                Return FunctionId.VisualBasic_Interactive_Window
            End Get
        End Property
    End Class
End Namespace

