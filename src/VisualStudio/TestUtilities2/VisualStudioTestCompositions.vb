' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Notification
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.UnitTests.Fakes
Imports Microsoft.VisualStudio.LanguageServices.CSharp
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    Friend NotInheritable Class VisualStudioTestCompositions
        Private Sub New()
        End Sub

        Public Shared ReadOnly LanguageServices As TestComposition = EditorTestCompositions.EditorFeaturesWpf.
            AddAssemblies(
                GetType(ServicesVSResources).Assembly,
                GetType(CSharpVSResources).Assembly,
                GetType(BasicVSResources).Assembly).
            RemoveParts(
                GetType(StubStreamingFindUsagesPresenter)).
            AddParts(
                GetType(MockWorkspaceEventListenerProvider)). ' avoid running Solution Crawler
            AddExcludedPartTypes(
                GetType(HACK_ThemeColorFixer),
                GetType(INotificationService),      ' EditorNotificationServiceFactory is used 
                GetType(VisualStudioWaitIndicator)) ' TestWaitIndicator is used instead
    End Class
End Namespace
