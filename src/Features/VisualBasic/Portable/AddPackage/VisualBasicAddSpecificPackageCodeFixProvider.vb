' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.AddPackage
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.AddPackage
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddPackage), [Shared]>
    Friend Class VisualBasicAddSpecificPackageCodeFixProvider
        Inherits AbstractAddSpecificPackageCodeFixProvider

        Private Const BC37267 As String = NameOf(BC37267) ' Predefined type 'ValueTuple(Of ,)' is not defined or imported.

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC37267)
            End Get
        End Property

        Protected Overrides Function GetAssemblyName(id As String) As String
            Select Case id
                Case BC37267 : Return "System.ValueTuple"
            End Select

            Return Nothing
        End Function
    End Class
End Namespace
