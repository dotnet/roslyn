' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Microsoft.VisualStudio.LanguageServices.SolutionExplorer
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    Public Class DiagnosticItemTests
        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub Name()
            Dim descriptor = CreateDescriptor()

            Dim diagnostic = New DiagnosticItem(Nothing, descriptor, ReportDiagnostic.Error, Nothing)

            Assert.Equal(expected:="TST0001: A test diagnostic", actual:=diagnostic.Text)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub BrowseObject()
            Dim descriptor = CreateDescriptor()

            Dim diagnostic = New DiagnosticItem(Nothing, descriptor, ReportDiagnostic.Info, Nothing)
            Dim browseObject = DirectCast(diagnostic.GetBrowseObject(), DiagnosticItem.BrowseObject)

            Assert.Equal(expected:=SolutionExplorerShim.DiagnosticItem_PropertyWindowClassName, actual:=browseObject.GetClassName())
            Assert.Equal(expected:="TST0001", actual:=browseObject.GetComponentName())
            Assert.Equal(expected:="TST0001", actual:=browseObject.Id)
            Assert.Equal(expected:="A test diagnostic", actual:=browseObject.Title)
            Assert.Equal(expected:="Test", actual:=browseObject.Category)
            Assert.Equal(expected:=SolutionExplorerShim.Severity_Error, actual:=browseObject.DefaultSeverity)
            Assert.Equal(expected:=SolutionExplorerShim.Severity_Info, actual:=browseObject.EffectiveSeverity)
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
