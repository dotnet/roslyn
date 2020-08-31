' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Editor
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets.SnippetFunctions
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.TextManager.Interop
Imports MSXML
Imports VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets
    Friend NotInheritable Class SnippetExpansionClient
        Inherits AbstractSnippetExpansionClient

        Public Sub New(threadingContext As IThreadingContext, languageServiceId As Guid, textView As ITextView, subjectBuffer As ITextBuffer, editorAdaptersFactoryService As IVsEditorAdaptersFactoryService)
            MyBase.New(threadingContext, languageServiceId, textView, subjectBuffer, editorAdaptersFactoryService)
        End Sub

        Public Shared Function GetSnippetExpansionClient(threadingContext As IThreadingContext, textView As ITextView, subjectBuffer As ITextBuffer, editorAdaptersFactoryService As IVsEditorAdaptersFactoryService) As AbstractSnippetExpansionClient

            Dim expansionClient As AbstractSnippetExpansionClient = Nothing

            If Not textView.Properties.TryGetProperty(GetType(AbstractSnippetExpansionClient), expansionClient) Then
                expansionClient = New SnippetExpansionClient(threadingContext, Guids.VisualBasicDebuggerLanguageId, textView, subjectBuffer, editorAdaptersFactoryService)
                textView.Properties.AddProperty(GetType(AbstractSnippetExpansionClient), expansionClient)
            End If

            Return expansionClient
        End Function

        Protected Overrides Function InsertEmptyCommentAndGetEndPositionTrackingSpan() As ITrackingSpan
            Dim endSpanInSurfaceBuffer(1) As VsTextSpan
            If ExpansionSession.GetEndSpan(endSpanInSurfaceBuffer) <> VSConstants.S_OK Then
                Return Nothing
            End If

            Dim endSpan As SnapshotSpan = Nothing
            If Not TryGetSubjectBufferSpan(endSpanInSurfaceBuffer(0), endSpan) Then
                Return Nothing
            End If

            Dim endPositionLine = SubjectBuffer.CurrentSnapshot.GetLineFromPosition(endSpan.Start.Position)
            Dim endLineText = endPositionLine.GetText()

            If endLineText.Trim() = String.Empty Then
                Dim commentString = "'"
                SubjectBuffer.Insert(endSpan.Start.Position, commentString)

                Dim commentSpan = New Span(endSpan.Start.Position, commentString.Length)
                Return SubjectBuffer.CurrentSnapshot.CreateTrackingSpan(commentSpan, SpanTrackingMode.EdgeExclusive)
            End If

            Return Nothing
        End Function

        Public Overrides Function GetExpansionFunction(xmlFunctionNode As IXMLDOMNode, bstrFieldName As String, ByRef pFunc As IVsExpansionFunction) As Integer
            Dim snippetFunctionName As String = Nothing
            Dim param As String = Nothing

            If Not TryGetSnippetFunctionInfo(xmlFunctionNode, snippetFunctionName, param) Then
                pFunc = Nothing
                Return VSConstants.E_INVALIDARG
            End If

            Select Case snippetFunctionName
                Case "SimpleTypeName"
                    pFunc = New SnippetFunctionSimpleTypeName(Me, SubjectBuffer, bstrFieldName, param)
                    Return VSConstants.S_OK
                Case "ClassName"
                    pFunc = New SnippetFunctionClassName(Me, SubjectBuffer, bstrFieldName)
                    Return VSConstants.S_OK
                Case "GenerateSwitchCases"
                    pFunc = New SnippetFunctionGenerateSwitchCases(Me, SubjectBuffer, bstrFieldName, param)
                    Return VSConstants.S_OK
                Case Else
                    pFunc = Nothing
                    Return VSConstants.E_INVALIDARG
            End Select
        End Function

        Friend Overrides Function AddImports(document As Document,
                                             position As Integer,
                                             snippetNode As XElement,
                                             placeSystemNamespaceFirst As Boolean,
                                             cancellationToken As CancellationToken) As Document
            Dim importsNode = snippetNode.Element(XName.Get("Imports", snippetNode.Name.NamespaceName))
            If importsNode Is Nothing OrElse
               Not importsNode.HasElements() Then
                Return document
            End If

            Dim newImportsStatements = GetImportsStatementsToAdd(document, snippetNode, importsNode, cancellationToken)
            If Not newImportsStatements.Any() Then
                Return document
            End If

            ' In Venus/Razor, inserting imports statements into the subject buffer does not work.
            ' Instead, we add the imports through the contained language host.

            Dim memberImportsNamespaces = newImportsStatements.SelectMany(Function(s) s.ImportsClauses).OfType(Of SimpleImportsClauseSyntax).Select(Function(c) c.Name.ToString())
            If TryAddImportsToContainedDocument(document, memberImportsNamespaces) Then
                Return document
            End If

            Dim root = document.GetSyntaxRootSynchronously(cancellationToken)

            Dim newRoot = CType(root, CompilationUnitSyntax).AddImportsStatements(newImportsStatements, placeSystemNamespaceFirst)
            Dim newDocument = document.WithSyntaxRoot(newRoot)

            Dim formattedDocument = Formatter.FormatAsync(newDocument, Formatter.Annotation, cancellationToken:=cancellationToken).WaitAndGetResult(cancellationToken)
            document.Project.Solution.Workspace.ApplyDocumentChanges(formattedDocument, cancellationToken)

            Return formattedDocument
        End Function

        Private Shared Function GetImportsStatementsToAdd(document As Document, snippetNode As XElement, importsNode As XElement, cancellationToken As CancellationToken) As IList(Of ImportsStatementSyntax)
            Dim root = document.GetSyntaxRootSynchronously(cancellationToken)
            Dim localImportsClauses = CType(root, CompilationUnitSyntax).Imports.SelectMany(Function(x) x.ImportsClauses)
            Dim compilation = document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult(cancellationToken)
            Dim options = CType(compilation.Options, VisualBasicCompilationOptions)
            Dim globalImportsClauses = options.GlobalImports.Select(Function(g) g.Clause)

            Dim membersImports = From clause In localImportsClauses.Union(globalImportsClauses).OfType(Of SimpleImportsClauseSyntax)
                                 Where clause.Alias Is Nothing

            Dim aliasImports = From clause In localImportsClauses.Union(globalImportsClauses).OfType(Of SimpleImportsClauseSyntax)
                               Where clause.Alias IsNot Nothing

            Dim xmlNamespaceImports = localImportsClauses.Union(globalImportsClauses).OfType(Of XmlNamespaceImportsClauseSyntax)

            Dim namespaceXmlName = XName.Get("Namespace", snippetNode.Name.NamespaceName)
            Dim ordinalIgnoreCaseStringComparer = StringComparer.OrdinalIgnoreCase
            Dim importsToAdd = New List(Of ImportsStatementSyntax)

            For Each import In importsNode.Elements(XName.Get("Import", snippetNode.Name.NamespaceName))
                Dim namespaceElement = import.Element(namespaceXmlName)
                If namespaceElement Is Nothing Then
                    Continue For
                End If

                Dim namespaceToImport = namespaceElement.Value.Trim()

                If String.IsNullOrEmpty(namespaceToImport) Then
                    Continue For
                End If

                AddUniqueClausesOfImport(namespaceToImport, importsToAdd, membersImports, aliasImports, xmlNamespaceImports, ordinalIgnoreCaseStringComparer)
            Next

            Return importsToAdd
        End Function

        Private Shared Sub AddUniqueClausesOfImport(
           namespaceToImport As String,
           importsToAdd As List(Of ImportsStatementSyntax),
           membersImports As IEnumerable(Of SimpleImportsClauseSyntax),
           aliasImports As IEnumerable(Of SimpleImportsClauseSyntax),
           xmlNamespaceImports As IEnumerable(Of XmlNamespaceImportsClauseSyntax),
           ordinalIgnoreCaseStringComparer As StringComparer)

            Dim importsStatement = TryCast(SyntaxFactory.ParseExecutableStatement("Imports " + namespaceToImport), ImportsStatementSyntax)
            If importsStatement Is Nothing Then
                Return
            End If

            Dim usableClauses = GetUniqueImportsClauses(importsStatement, membersImports, aliasImports, xmlNamespaceImports, ordinalIgnoreCaseStringComparer)
            If Not usableClauses.Any() Then
                Return
            End If

            Dim filteredImportsStatement = SyntaxFactory.ImportsStatement(
                SyntaxFactory.Token(SyntaxKind.ImportsKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                SyntaxFactory.SeparatedList(usableClauses))

            importsToAdd.Add(filteredImportsStatement.WithAdditionalAnnotations(Formatter.Annotation) _
                .WithAppendedTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed))
        End Sub

        Private Shared Function GetUniqueImportsClauses(
           importsStatement As ImportsStatementSyntax,
           membersImports As IEnumerable(Of SimpleImportsClauseSyntax),
           aliasImports As IEnumerable(Of SimpleImportsClauseSyntax),
           xmlNamespaceImports As IEnumerable(Of XmlNamespaceImportsClauseSyntax),
           ordinalIgnoreCaseStringComparer As StringComparer) As IEnumerable(Of ImportsClauseSyntax)

            Dim uniqueClauses = New List(Of ImportsClauseSyntax)

            For Each clause In importsStatement.ImportsClauses
                Dim simpleImportsClause = TryCast(clause, SimpleImportsClauseSyntax)
                If simpleImportsClause IsNot Nothing Then
                    If simpleImportsClause.Alias Is Nothing Then
                        If Not membersImports.Any(Function(c) ordinalIgnoreCaseStringComparer.Equals(c.Name.ToString(), simpleImportsClause.Name.ToString())) Then
                            uniqueClauses.Add(clause)
                        End If
                    Else
                        If Not aliasImports.Any(Function(a) ordinalIgnoreCaseStringComparer.Equals(a.Alias.Identifier.ToString(), simpleImportsClause.Alias.Identifier.ToString()) AndAlso
                                                        ordinalIgnoreCaseStringComparer.Equals(a.Name.ToString(), simpleImportsClause.Name.ToString())) Then
                            uniqueClauses.Add(clause)
                        End If
                    End If

                    Continue For
                End If

                Dim xmlNamespaceImportsClause = TryCast(clause, XmlNamespaceImportsClauseSyntax)
                If xmlNamespaceImportsClause IsNot Nothing Then
                    If Not xmlNamespaceImports.Any(Function(x) ordinalIgnoreCaseStringComparer.Equals(x.XmlNamespace.Name.ToString(), xmlNamespaceImportsClause.XmlNamespace.Name.ToString()) AndAlso
                                                               ordinalIgnoreCaseStringComparer.Equals(x.XmlNamespace.Value.ToString(), xmlNamespaceImportsClause.XmlNamespace.Value.ToString())) Then
                        uniqueClauses.Add(clause)
                    End If
                    Continue For
                End If
            Next

            Return uniqueClauses
        End Function
    End Class
End Namespace

