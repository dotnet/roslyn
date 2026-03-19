' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.CSharp
Imports Microsoft.VisualStudio.LanguageServices.Implementation
Imports Microsoft.VisualStudio.LanguageServices.Remote
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    Friend NotInheritable Class VisualStudioTestCompositions
        Private Sub New()
        End Sub

        Public Shared ReadOnly LanguageServices As TestComposition = EditorTestCompositions.EditorFeatures.
            AddAssemblies(
                GetType(ServicesVSResources).Assembly,
                GetType(CSharpVSResources).Assembly,
                GetType(BasicVSResources).Assembly).
            AddParts(
                GetType(StubVsEditorAdaptersFactoryService)).
            AddExcludedPartTypes(
                GetType(VisualStudioRemoteHostClientProvider.Factory), ' Do not use ServiceHub in VS unit tests, run services locally.
                GetType(IStreamingFindUsagesPresenter),                ' TODO: should we be using the actual implementation (https://github.com/dotnet/roslyn/issues/46380)?
                GetType(HACK_ThemeColorFixer),
                GetType(Notification.VSNotificationServiceFactory),
                GetType(Options.VisualStudioOptionPersisterProvider),
                GetType(VisualStudioWorkspaceStatusServiceFactory),  ' Depends on other packages being loaded, and it's not really clear how it would work in unit tests anyways
                GetType(VisualStudioDocumentTrackingServiceFactory)) ' Depends on IVsMonitorSelection, and removing it falls back to the default no-op implementation.
    End Class
End Namespace
