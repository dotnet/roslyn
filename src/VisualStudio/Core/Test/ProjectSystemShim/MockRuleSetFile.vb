' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    Public Class MockRuleSetFile
        Implements IRuleSetFile

        Private ReadOnly _generalOption As ReportDiagnostic
        Private ReadOnly _specificOptions As ImmutableDictionary(Of String, ReportDiagnostic)

        Public Sub New(generalOption As ReportDiagnostic, specificOptions As ImmutableDictionary(Of String, ReportDiagnostic))
            _generalOption = generalOption
            _specificOptions = specificOptions
        End Sub

        Public ReadOnly Property FilePath As String Implements IRuleSetFile.FilePath
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Event UpdatedOnDisk As EventHandler Implements IRuleSetFile.UpdatedOnDisk

        Public Function GetException() As Exception Implements IRuleSetFile.GetException
            Throw New NotImplementedException()
        End Function

        Public Function GetGeneralDiagnosticOption() As ReportDiagnostic Implements IRuleSetFile.GetGeneralDiagnosticOption
            Return _generalOption
        End Function

        Public Function GetSpecificDiagnosticOptions() As ImmutableDictionary(Of String, ReportDiagnostic) Implements IRuleSetFile.GetSpecificDiagnosticOptions
            Return _specificOptions
        End Function
    End Class
End Namespace
