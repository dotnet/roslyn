' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.FullyQualify
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.FullyQualify
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.FullyQualify), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.AddImport)>
    Friend NotInheritable Class VisualBasicFullyQualifyCodeFixProvider
        Inherits AbstractFullyQualifyCodeFixProvider

        ''' <summary>
        ''' Type xxx is not defined
        ''' </summary>
        Private Const BC30002 = "BC30002"

        ''' <summary>
        ''' Error 'x' is not declared
        ''' </summary>
        Private Const BC30451 = "BC30451"

        ''' <summary>
        ''' 'reference' is an ambiguous reference between 'identifier' and 'identifier'
        ''' </summary>
        Private Const BC30561 = "BC30561"

        ''' <summary>
        ''' Namespace or type specified in imports cannot be found
        ''' </summary>
        Private Const BC40056 = "BC40056"

        ''' <summary>
        ''' 'A' has no type parameters and so cannot have type arguments.
        ''' </summary>
        Private Const BC32045 = "BC32045"

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create(BC30002, IDEDiagnosticIds.UnboundIdentifierId, BC30451, BC30561, BC40056, BC32045)
    End Class
End Namespace
