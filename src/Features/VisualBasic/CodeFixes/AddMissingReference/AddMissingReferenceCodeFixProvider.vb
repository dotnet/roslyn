' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.AddMissingReference
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.AddMissingReference

    <ExportCodeFixProviderAttribute(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddMissingReference), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.SimplifyNames)>
    Friend Class AddMissingReferenceCodeFixProvider
        Inherits AbstractAddMissingReferenceCodeFixProvider(Of IdentifierNameSyntax)

        Friend Const BC30005 As String = "BC30005" ' ERR_UnreferencedAssemblyEvent3
        Friend Const BC30007 As String = "BC30007" ' ERR_UnreferencedAssemblyBase3
        Friend Const BC30009 As String = "BC30009" ' ERR_UnreferencedAssemblyImplements3
        Friend Const BC30652 As String = "BC30652" ' ERR_UnreferencedAssembly3

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30005, BC30007, BC30009, BC30652)
            End Get
        End Property
    End Class
End Namespace
