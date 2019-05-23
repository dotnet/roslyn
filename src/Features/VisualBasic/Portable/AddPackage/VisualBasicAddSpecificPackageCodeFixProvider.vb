' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.AddPackage
Imports Microsoft.CodeAnalysis.CodeFixes

Namespace Microsoft.CodeAnalysis.VisualBasic.AddPackage
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicAddSpecificPackageCodeFixProvider
        Inherits AbstractAddSpecificPackageCodeFixProvider

        Private Const BC37267 As String = NameOf(BC37267) ' Predefined type 'ValueTuple(Of ,)' is not defined or imported.

        <ImportingConstructor>
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
