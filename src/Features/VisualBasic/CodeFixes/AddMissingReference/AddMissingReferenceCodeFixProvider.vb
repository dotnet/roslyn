' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.AddMissingReference
Imports Microsoft.CodeAnalysis.CodeFixes

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.AddMissingReference

    <ExportCodeFixProviderAttribute(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddMissingReference), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.SimplifyNames)>
    Friend Class AddMissingReferenceCodeFixProvider
        Inherits CodeFixProvider

        Friend Const BC30005 As String = "BC30005" ' ERR_UnreferencedAssemblyEvent3
        Friend Const BC30007 As String = "BC30007" ' ERR_UnreferencedAssemblyBase3
        Friend Const BC30009 As String = "BC30009" ' ERR_UnreferencedAssemblyImplements3
        Friend Const BC30652 As String = "BC30652" ' ERR_UnreferencedAssembly3

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30005, BC30007, BC30009, BC30652)
            End Get
        End Property

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim assemblyIdentities = New HashSet(Of AssemblyIdentity)
            For Each diagnostic In context.Diagnostics
                Dim assemblyIdentity = HACK_GetAssemblyIdentity(diagnostic.GetMessage())

                If Not assemblyIdentities.Contains(assemblyIdentity) Then
                    assemblyIdentities.Add(assemblyIdentity)
                    context.RegisterCodeFix(
                        Await AddMissingReferenceCodeAction.CreateAsync(context.Document.Project, assemblyIdentity, context.CancellationToken).ConfigureAwait(False),
                        diagnostic)
                End If
            Next
        End Function

        Private Function HACK_GetAssemblyIdentity(message As String) As AssemblyIdentity
            ' EVIL SLIMY HACK: right now we don't have a way to properly get
            ' the AssemblyIdentity.
            message = message.Split("'"c)(1)

            Dim identity As AssemblyIdentity = Nothing
            Contract.ThrowIfFalse(AssemblyIdentity.TryParseDisplayName(message, identity))

            Return identity
        End Function
    End Class
End Namespace
