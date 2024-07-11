' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.ImplementInterface
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ImplementInterface
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.ImplementInterface), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.ImplementAbstractClass)>
    Friend NotInheritable Class VisualBasicImplementInterfaceCodeFixProvider
        Inherits AbstractImplementInterfaceCodeFixProvider(Of TypeSyntax)

        Friend Const BC30149 As String = "BC30149" ' Class 'bar' must implement 'Sub goo()' for interface 'igoo'.

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(BC30149)

        Protected Overrides Function IsTypeInInterfaceBaseList(type As TypeSyntax) As Boolean
            Return TypeOf type.Parent Is ImplementsStatementSyntax
        End Function
    End Class
End Namespace
