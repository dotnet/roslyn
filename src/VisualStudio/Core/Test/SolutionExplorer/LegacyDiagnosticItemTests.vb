' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Microsoft.VisualStudio.LanguageServices.SolutionExplorer

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    Public Class LegacyDiagnosticItemTests
        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub Name()
            Dim descriptor = CreateDescriptor()

            Dim diagnostic = New LegacyDiagnosticItem(Nothing, descriptor, ReportDiagnostic.Error, LanguageNames.VisualBasic, Nothing)

            Assert.Equal(expected:="TST0001: A test diagnostic", actual:=diagnostic.Text)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub BrowseObject()
            Dim descriptor = CreateDescriptor()

            Dim diagnostic = New LegacyDiagnosticItem(Nothing, descriptor, ReportDiagnostic.Info, LanguageNames.VisualBasic, Nothing)
            Dim browseObject = DirectCast(diagnostic.GetBrowseObject(), LegacyDiagnosticItem.BrowseObject)

            Assert.Equal(expected:=SolutionExplorerShim.Diagnostic_Properties, actual:=browseObject.GetClassName())
            Assert.Equal(expected:="TST0001", actual:=browseObject.GetComponentName())
            Assert.Equal(expected:="TST0001", actual:=browseObject.Id)
            Assert.Equal(expected:="A test diagnostic", actual:=browseObject.Title)
            Assert.Equal(expected:="Test", actual:=browseObject.Category)
            Assert.Equal(expected:=SolutionExplorerShim.Error_, actual:=browseObject.DefaultSeverity)
            Assert.Equal(expected:=SolutionExplorerShim.Info, actual:=browseObject.EffectiveSeverity)
            Assert.Equal(expected:=True, actual:=browseObject.EnabledByDefault)

        End Sub

        Private Shared Function CreateDescriptor() As DiagnosticDescriptor
            Return New DiagnosticDescriptor(
                            id:="TST0001",
                            title:="A test diagnostic",
                            messageFormat:="A test message",
                            category:="Test",
                            defaultSeverity:=DiagnosticSeverity.Error,
                            isEnabledByDefault:=True)
        End Function
    End Class
End Namespace
