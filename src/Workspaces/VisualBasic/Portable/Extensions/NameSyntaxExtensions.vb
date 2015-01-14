' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module NameSyntaxExtensions

        <Extension()>
        Public Function GetNameParts(nameSyntax As NameSyntax) As IList(Of NameSyntax)
            Return New NameSyntaxIterator(nameSyntax).ToList()
        End Function

        <Extension()>
        Public Function GetLastDottedName(nameSyntax As NameSyntax) As NameSyntax
            Dim parts = nameSyntax.GetNameParts()
            Return parts(parts.Count - 1)
        End Function

        <Extension>
        Public Function CanBeReplacedWithAnyName(nameSyntax As NameSyntax) As Boolean
            If nameSyntax.CheckParent(Of SimpleArgumentSyntax)(Function(a) a.IsNamed AndAlso a.NameColonEquals.Name Is nameSyntax) OrElse
               nameSyntax.CheckParent(Of HandlesClauseItemSyntax)(Function(h) h.EventMember Is nameSyntax) OrElse
               nameSyntax.CheckParent(Of InferredFieldInitializerSyntax)(Function(i) i.Expression Is nameSyntax) OrElse
               nameSyntax.CheckParent(Of NamedFieldInitializerSyntax)(Function(n) n.Name Is nameSyntax) OrElse
               nameSyntax.CheckParent(Of CatchStatementSyntax)(Function(c) c.IdentifierName Is nameSyntax) OrElse
               nameSyntax.CheckParent(Of RaiseEventStatementSyntax)(Function(r) r.Name Is nameSyntax) OrElse
               nameSyntax.CheckParent(Of QualifiedNameSyntax)(Function(q) q.Right Is nameSyntax) OrElse
               nameSyntax.CheckParent(Of MemberAccessExpressionSyntax)(Function(m) m.Name Is nameSyntax) Then
                Return False
            End If

            ' TODO(cyrusn): Add cases as appropriate as the language changes.
            Return True
        End Function
    End Module
End Namespace
