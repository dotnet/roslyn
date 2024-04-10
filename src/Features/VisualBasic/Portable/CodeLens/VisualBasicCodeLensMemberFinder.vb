' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeLens
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeLens

    <ExportLanguageService(GetType(ICodeLensMemberFinder), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicCodeLensMemberFinder
        Implements ICodeLensMemberFinder

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Async Function GetCodeLensMembersAsync(document As Document, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of CodeLensMember)) Implements ICodeLensMemberFinder.GetCodeLensMembersAsync
            Dim root = Await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim builder = ArrayBuilder(Of CodeLensMember).GetInstance()

            Dim visitor = New VisualBasicCodeLensVisitor(builder)
            visitor.Visit(root)

            Return builder.ToImmutableAndFree()
        End Function

        Private NotInheritable Class VisualBasicCodeLensVisitor
            Inherits VisualBasicSyntaxWalker

            Private ReadOnly _memberBuilder As ArrayBuilder(Of CodeLensMember)

            Public Sub New(memberBuilder As ArrayBuilder(Of CodeLensMember))
                _memberBuilder = memberBuilder
            End Sub

            Public Overrides Sub VisitClassStatement(node As ClassStatementSyntax)
                _memberBuilder.Add(New CodeLensMember(node, node.Identifier.Span))
                MyBase.VisitClassStatement(node)
            End Sub

            Public Overrides Sub VisitInterfaceStatement(node As InterfaceStatementSyntax)
                _memberBuilder.Add(New CodeLensMember(node, node.Identifier.Span))
                MyBase.VisitInterfaceStatement(node)
            End Sub

            Public Overrides Sub VisitEnumStatement(node As EnumStatementSyntax)
                _memberBuilder.Add(New CodeLensMember(node, node.Identifier.Span))
                MyBase.VisitEnumStatement(node)
            End Sub

            Public Overrides Sub VisitPropertyStatement(node As PropertyStatementSyntax)
                _memberBuilder.Add(New CodeLensMember(node, node.Identifier.Span))
            End Sub

            Public Overrides Sub VisitMethodStatement(node As MethodStatementSyntax)
                _memberBuilder.Add(New CodeLensMember(node, node.Identifier.Span))
            End Sub

            Public Overrides Sub VisitStructureStatement(node As StructureStatementSyntax)
                _memberBuilder.Add(New CodeLensMember(node, node.Identifier.Span))
                MyBase.VisitStructureStatement(node)
            End Sub

            Public Overrides Sub VisitSubNewStatement(node As SubNewStatementSyntax)
                _memberBuilder.Add(New CodeLensMember(node, node.NewKeyword.Span))
            End Sub

            Public Overrides Sub VisitModuleStatement(node As ModuleStatementSyntax)
                _memberBuilder.Add(New CodeLensMember(node, node.Identifier.Span))
                MyBase.VisitModuleStatement(node)
            End Sub

            Public Overrides Sub VisitDelegateStatement(node As DelegateStatementSyntax)
                _memberBuilder.Add(New CodeLensMember(node, node.Identifier.Span))
            End Sub

            Public Overrides Sub VisitEnumMemberDeclaration(node As EnumMemberDeclarationSyntax)
                _memberBuilder.Add(New CodeLensMember(node, node.Identifier.Span))
            End Sub

            Public Overrides Sub VisitFieldDeclaration(node As FieldDeclarationSyntax)
                For Each variable In node.Declarators
                    For Each name In variable.Names
                        _memberBuilder.Add(New CodeLensMember(name, name.Identifier.Span))
                    Next
                Next
            End Sub

            Public Overrides Sub VisitEventStatement(node As EventStatementSyntax)
                _memberBuilder.Add(New CodeLensMember(node, node.Identifier.Span))
            End Sub
        End Class
    End Class
End Namespace

