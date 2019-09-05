' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEndConstruct
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.GenerateEndConstruct), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.FixIncorrectExitContinue)>
    Friend Class GenerateEndConstructCodeFixProvider
        Inherits CodeFixProvider

        Friend Const BC30025 As String = "BC30025" ' error BC30025: Property missing 'End Property'.
        Friend Const BC30026 As String = "BC30026" ' error BC30026: 'End Sub' expected.
        Friend Const BC30027 As String = "BC30027" ' error BC30027: 'End Function' expected.
        Friend Const BC30081 As String = "BC30081" ' error BC30081: 'If' must end with a matching 'End If'
        Friend Const BC30082 As String = "BC30082" ' error BC30082: 'While' must end with a matching 'End While'.
        Friend Const BC30083 As String = "BC30083" ' error BC30083: 'Do' must end with a matching 'Loop'.
        Friend Const BC30084 As String = "BC30084" ' error BC30084: 'For' must end with a matching 'Next'.
        Friend Const BC30085 As String = "BC30085" ' error BC30085: 'With' must end with a matching 'End With'.
        Friend Const BC30185 As String = "BC30185" ' error BC30185: 'Enum' must end with a matching 'End Enum'.
        Friend Const BC30253 As String = "BC30253" ' error BC30253: 'Interface' must end with a matching 'End Interface'.
        Friend Const BC30384 As String = "BC30384" ' error BC30384: 'Try' must end with a matching 'End Try'.
        Friend Const BC30481 As String = "BC30481" ' error BC30481: 'Class' statement must end with a matching 'End Class'.
        Friend Const BC30624 As String = "BC30624" ' error BC30624: 'Structure' statement must end with a matching 'End Structure'.
        Friend Const BC30625 As String = "BC30625" ' error BC30625: 'Module' statement must end with a matching 'End Module'.
        Friend Const BC30626 As String = "BC30626" ' error BC30626: 'Namespace' statement must end with a matching 'End Namespace'.
        Friend Const BC30631 As String = "BC30631" ' error BC30631: 'Get' statement must end with a matching 'End Get'.
        Friend Const BC30633 As String = "BC30633" ' error BC30633: 'Set' statement must end with a matching 'End Set'.
        Friend Const BC30675 As String = "BC30675" ' error BC30675: 'SyncLock' statement must end with a matching 'End SyncLock'.
        Friend Const BC31114 As String = "BC31114" ' error BC31114: 'Custom Event' must end with a matching 'End Event'.
        Friend Const BC31115 As String = "BC31115" ' error BC31115: 'AddHandler' declaration must end with a matching 'End AddHandler'.
        Friend Const BC31116 As String = "BC31116" ' error BC31116: 'RemoveHandler' declaration must end with a matching 'End RemoveHandler'.
        Friend Const BC31117 As String = "BC31117" ' error BC31117: 'RaiseEvent' declaration must end with a matching 'End RaiseEvent'.
        Friend Const BC36008 As String = "BC36008" ' error BC36008: 'Using' must end with a matching 'End Using'.

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30025, BC30026, BC30027, BC30081, BC30082, BC30083, BC30084, BC30085, BC30185, BC30253, BC30384, BC30481, BC30624, BC30625,
                    BC30626, BC30631, BC30633, BC30675, BC31114, BC31115, BC31116, BC31117, BC36008)
            End Get
        End Property

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            ' Fix All is not supported by this code fix
            ' https://github.com/dotnet/roslyn/issues/34473
            Return Nothing
        End Function

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)

            Dim token = root.FindToken(context.Span.Start)
            If Not token.Span.IntersectsWith(context.Span) Then
                Return
            End If

            Dim beginStatement = token.GetAncestors(Of SyntaxNode) _
                                      .FirstOrDefault(Function(c) c.Span.IntersectsWith(context.Span) AndAlso IsCandidate(c))
            If beginStatement Is Nothing OrElse beginStatement.Parent Is Nothing Then
                Return
            End If

            Dim endStatement = GetEndStatement(beginStatement.Parent)
            If endStatement Is Nothing OrElse Not endStatement.IsMissing Then
                Return
            End If

            If endStatement.Parent.Kind = SyntaxKind.PropertyBlock Then
                context.RegisterCodeFix(
                    New MyCodeAction(
                        VBFeaturesResources.Insert_the_missing_End_Property_statement,
                        Function(c) GeneratePropertyEndConstructAsync(context.Document, DirectCast(endStatement.Parent, PropertyBlockSyntax), c)),
                    context.Diagnostics)
                Return
            End If

            If endStatement.Kind = SyntaxKind.EndGetStatement OrElse endStatement.Kind = SyntaxKind.EndSetStatement Then
                If endStatement?.Parent?.Parent.Kind = SyntaxKind.PropertyBlock Then
                    context.RegisterCodeFix(
                        New MyCodeAction(
                            VBFeaturesResources.Insert_the_missing_End_Property_statement,
                            Function(c) GeneratePropertyEndConstructAsync(context.Document, DirectCast(endStatement.Parent.Parent, PropertyBlockSyntax), c)),
                        context.Diagnostics)
                    Return
                End If
            End If

            context.RegisterCodeFix(
                New MyCodeAction(
                    GetDescription(endStatement),
                    Function(c) GenerateEndConstructAsync(context.Document, endStatement, c)),
                context.Diagnostics)
        End Function

        Private Shared Function IsCandidate(node As SyntaxNode) As Boolean
            If node Is Nothing Then
                Return False
            End If

            Dim begin = GetBeginStatement(node.Parent)
            If begin Is Nothing Then
                Return False
            End If

            Return begin.Span.Contains(node.Span)
        End Function

        Private Shared Function GetBeginStatement(node As SyntaxNode) As SyntaxNode
            Return node.TypeSwitch(
                (Function(n As MultiLineIfBlockSyntax) n.IfStatement),
                (Function(n As UsingBlockSyntax) n.UsingStatement),
                (Function(n As StructureBlockSyntax) n.BlockStatement),
                (Function(n As ModuleBlockSyntax) n.BlockStatement),
                (Function(n As NamespaceBlockSyntax) n.NamespaceStatement),
                (Function(n As ClassBlockSyntax) n.BlockStatement),
                (Function(n As InterfaceBlockSyntax) n.BlockStatement),
                (Function(n As EnumBlockSyntax) n.EnumStatement),
                (Function(n As WhileBlockSyntax) n.WhileStatement),
                (Function(n As WithBlockSyntax) n.WithStatement),
                (Function(n As SyncLockBlockSyntax) n.SyncLockStatement),
                (Function(n As DoLoopBlockSyntax) n.DoStatement),
                (Function(n As ForOrForEachBlockSyntax) n.ForOrForEachStatement),
                (Function(n As TryBlockSyntax) DirectCast(n, SyntaxNode)),
                (Function(n As MethodBlockBaseSyntax) n.BlockStatement),
                (Function(n As PropertyBlockSyntax) n.PropertyStatement))
        End Function

        Private Shared Function GetEndStatement(node As SyntaxNode) As SyntaxNode
            Return node.TypeSwitch(
                (Function(n As MultiLineIfBlockSyntax) DirectCast(n.EndIfStatement, SyntaxNode)),
                (Function(n As UsingBlockSyntax) n.EndUsingStatement),
                (Function(n As StructureBlockSyntax) n.EndBlockStatement),
                (Function(n As ModuleBlockSyntax) n.EndBlockStatement),
                (Function(n As NamespaceBlockSyntax) n.EndNamespaceStatement),
                (Function(n As ClassBlockSyntax) n.EndBlockStatement),
                (Function(n As InterfaceBlockSyntax) n.EndBlockStatement),
                (Function(n As EnumBlockSyntax) n.EndEnumStatement),
                (Function(n As WhileBlockSyntax) n.EndWhileStatement),
                (Function(n As WithBlockSyntax) n.EndWithStatement),
                (Function(n As SyncLockBlockSyntax) n.EndSyncLockStatement),
                (Function(n As DoLoopBlockSyntax) n.LoopStatement),
                (Function(n As ForOrForEachBlockSyntax) n.NextStatement),
                (Function(n As TryBlockSyntax) n.EndTryStatement),
                (Function(n As MethodBlockBaseSyntax) n.EndBlockStatement),
                (Function(n As PropertyBlockSyntax) n.EndPropertyStatement))
        End Function

        Private Async Function GeneratePropertyEndConstructAsync(document As Document, node As PropertyBlockSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            ' Make sure the PropertyBlock has End Property
            Dim updatedProperty = node
            If node.EndPropertyStatement.IsMissing Then
                updatedProperty = node.WithEndPropertyStatement(SyntaxFactory.EndPropertyStatement())
            End If

            ' Make sure any existing setters or getters have their End
            Dim getter = updatedProperty.Accessors.FirstOrDefault(Function(a) a.Kind = SyntaxKind.GetAccessorBlock)
            If getter IsNot Nothing AndAlso getter.EndBlockStatement.IsMissing Then
                updatedProperty = updatedProperty.ReplaceNode(getter, getter.WithEndBlockStatement(SyntaxFactory.EndGetStatement()))
            End If

            Dim setter = updatedProperty.Accessors.FirstOrDefault(Function(a) a.Kind = SyntaxKind.SetAccessorBlock)
            If setter IsNot Nothing AndAlso setter.EndBlockStatement.IsMissing Then
                updatedProperty = updatedProperty.ReplaceNode(setter, setter.WithEndBlockStatement(SyntaxFactory.EndSetStatement()))
            End If

            Dim gen = document.GetLanguageService(Of SyntaxGenerator)()

            If getter Is Nothing AndAlso Not updatedProperty.PropertyStatement.Modifiers.Any(SyntaxKind.WriteOnlyKeyword) Then
                updatedProperty = DirectCast(gen.WithGetAccessorStatements(updatedProperty, Array.Empty(Of SyntaxNode)()), PropertyBlockSyntax)
            End If

            If setter Is Nothing AndAlso Not updatedProperty.PropertyStatement.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) Then
                updatedProperty = DirectCast(gen.WithSetAccessorStatements(updatedProperty, Array.Empty(Of SyntaxNode)()), PropertyBlockSyntax)
            End If

            Dim updatedDocument = Await document.ReplaceNodeAsync(node, updatedProperty.WithAdditionalAnnotations(Formatter.Annotation), cancellationToken).ConfigureAwait(False)
            Return updatedDocument
        End Function

        Public Function GetDescription(node As SyntaxNode) As String
            Dim endBlockSyntax = TryCast(node, EndBlockStatementSyntax)
            If endBlockSyntax IsNot Nothing Then
                Return String.Format(VBFeaturesResources.Insert_the_missing_0, "End " + SyntaxFacts.GetText(endBlockSyntax.BlockKeyword.Kind))
            End If

            Dim loopStatement = TryCast(node, LoopStatementSyntax)
            If loopStatement IsNot Nothing Then
                Return String.Format(VBFeaturesResources.Insert_the_missing_0, SyntaxFacts.GetText(SyntaxKind.LoopKeyword))
            End If

            Return String.Format(VBFeaturesResources.Insert_the_missing_0, SyntaxFacts.GetText(SyntaxKind.NextKeyword))
        End Function

        Private Async Function GenerateEndConstructAsync(document As Document, endStatement As SyntaxNode, cancellationToken As CancellationToken) As Task(Of Document)
            If endStatement.Kind = SyntaxKind.EndEnumStatement Then
                ' InvInsideEndsEnum
                Dim nextNode = endStatement.Parent.GetLastToken().GetNextToken().Parent

                If nextNode IsNot Nothing Then
                    Dim diagnostics = nextNode.GetDiagnostics()
                    If diagnostics.SingleOrDefault(Function(d) d.Id = "BC30619") IsNot Nothing Then
                        Dim updatedParent = DirectCast(endStatement.Parent, EnumBlockSyntax).WithEndEnumStatement(SyntaxFactory.EndEnumStatement())
                        Dim updatedDocument = Await document.ReplaceNodeAsync(endStatement.Parent, updatedParent.WithAdditionalAnnotations(Formatter.Annotation), cancellationToken).ConfigureAwait(False)
                        Return updatedDocument
                    End If
                End If
            End If

            Return Await InsertEndConstructAsync(document, endStatement, cancellationToken).ConfigureAwait(False)
        End Function

        Private Async Function InsertEndConstructAsync(document As Document, endStatement As SyntaxNode, cancellationToken As CancellationToken) As Task(Of Document)
            Dim text = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)

            Dim stringToAppend As String = Nothing

            Dim endBlock = TryCast(endStatement, EndBlockStatementSyntax)
            If endBlock IsNot Nothing Then
                stringToAppend = vbCrLf + "End " + SyntaxFacts.GetText(endBlock.BlockKeyword.Kind) + vbCrLf
            End If

            Dim nextStatement = TryCast(endStatement, NextStatementSyntax)
            If nextStatement IsNot Nothing Then
                stringToAppend = vbCrLf & SyntaxFacts.GetText(nextStatement.NextKeyword.Kind) & vbCrLf
            End If

            Dim loopStatement = TryCast(endStatement, LoopStatementSyntax)
            If loopStatement IsNot Nothing Then
                stringToAppend = vbCrLf & SyntaxFacts.GetText(loopStatement.LoopKeyword.Kind) & vbCrLf
            End If

            Dim insertionPoint = GetBeginStatement(endStatement.Parent).FullSpan.End
            Dim updatedText = text.WithChanges(New TextChange(TextSpan.FromBounds(insertionPoint, insertionPoint), stringToAppend))
            Dim updatedDocument = document.WithText(updatedText)

            Dim tree = Await updatedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            updatedDocument = Await updatedDocument.ReplaceNodeAsync(tree, tree.WithAdditionalAnnotations(Formatter.Annotation), cancellationToken).ConfigureAwait(False)

            Return updatedDocument
        End Function

        Private Class MyCodeAction
            Inherits CodeAction.DocumentChangeAction

            Public Sub New(title As String, createChangedDocument As Func(Of CancellationToken, Task(Of Document)))
                MyBase.New(title, createChangedDocument)
            End Sub
        End Class
    End Class
End Namespace
