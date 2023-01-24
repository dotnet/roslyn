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

            Dim visitor = New VisualBasicCodeLensVisitor(Sub(node) builder.Add(node))
            visitor.Visit(root)

            Return builder.ToImmutableAndFree()

        End Function

        Private Class VisualBasicCodeLensVisitor
            Inherits VisualBasicSyntaxWalker

            Private ReadOnly _memberFoundAction As Action(Of CodeLensMember)

            Public Sub New(memberFoundAction As Action(Of CodeLensMember))
                _memberFoundAction = memberFoundAction
            End Sub

            Public Overrides Sub VisitClassStatement(node As ClassStatementSyntax)
                _memberFoundAction(New CodeLensMember(node, node.Identifier.Span))
                MyBase.VisitClassStatement(node)
            End Sub

            Public Overrides Sub VisitInterfaceStatement(node As InterfaceStatementSyntax)
                _memberFoundAction(New CodeLensMember(node, node.Identifier.Span))
                MyBase.VisitInterfaceStatement(node)
            End Sub

            Public Overrides Sub VisitEnumStatement(node As EnumStatementSyntax)
                _memberFoundAction(New CodeLensMember(node, node.Identifier.Span))
                MyBase.VisitEnumStatement(node)
            End Sub

            Public Overrides Sub VisitPropertyStatement(node As PropertyStatementSyntax)
                _memberFoundAction(New CodeLensMember(node, node.Identifier.Span))
                MyBase.VisitPropertyStatement(node)
            End Sub

            Public Overrides Sub VisitMethodStatement(node As MethodStatementSyntax)
                _memberFoundAction(New CodeLensMember(node, node.Identifier.Span))
                MyBase.VisitMethodStatement(node)
            End Sub

            Public Overrides Sub VisitStructureStatement(node As StructureStatementSyntax)
                _memberFoundAction(New CodeLensMember(node, node.Identifier.Span))
                MyBase.VisitStructureStatement(node)
            End Sub

            Public Overrides Sub VisitSubNewStatement(node As SubNewStatementSyntax)
                _memberFoundAction(New CodeLensMember(node, node.NewKeyword.Span))
                MyBase.VisitSubNewStatement(node)
            End Sub

            Public Overrides Sub VisitModuleStatement(node As ModuleStatementSyntax)
                _memberFoundAction(New CodeLensMember(node, node.Identifier.Span))
                MyBase.VisitModuleStatement(node)
            End Sub
        End Class
    End Class
End Namespace

