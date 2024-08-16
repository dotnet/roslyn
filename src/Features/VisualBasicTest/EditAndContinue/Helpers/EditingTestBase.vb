' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports Microsoft.CodeAnalysis.Differencing
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.Contracts.EditAndContinue
Imports Microsoft.CodeAnalysis.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EditAndContinue
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests

    Public MustInherit Class EditingTestBase
        Inherits BasicTestBase

        Public Shared ReadOnly ReloadableAttributeSrc As String = "
Imports System.Runtime.CompilerServices
Namespace System.Runtime.CompilerServices
    Class CreateNewOnMetadataUpdateAttribute
        Inherits Attribute
    End Class
End Namespace
"

        Friend Shared Function CreateAnalyzer() As VisualBasicEditAndContinueAnalyzer
            Return New VisualBasicEditAndContinueAnalyzer()
        End Function

        Public Enum MethodKind
            Regular
            Async
            Iterator
        End Enum

        Public Shared Function GetResource(keyword As String, symbolDisplayName As String) As String
            Dim resource = TryGetResource(keyword)

            If resource Is Nothing Then
                Throw ExceptionUtilities.UnexpectedValue(keyword)
            End If

            Return String.Format(FeaturesResources.member_kind_and_name, resource, symbolDisplayName)
        End Function

        Public Shared Function GetResource(keyword As String, symbolDisplayName As String, containerKeyword As String, containerDisplayName As String) As String
            Dim keywordResource = TryGetResource(keyword)
            If keywordResource Is Nothing Then
                Throw ExceptionUtilities.UnexpectedValue(keyword)
            End If

            Dim containerResource = TryGetResource(containerKeyword)
            If containerResource Is Nothing Then
                Throw ExceptionUtilities.UnexpectedValue(containerKeyword)
            End If

            Return String.Format(
                FeaturesResources.symbol_kind_and_name_of_member_kind_and_name,
                keywordResource,
                symbolDisplayName,
                containerResource,
                containerDisplayName)
        End Function

        Public Shared Function GetResource(keyword As String) As String
            Dim result = TryGetResource(keyword)
            If result Is Nothing Then
                Throw ExceptionUtilities.UnexpectedValue(keyword)
            End If
            Return result
        End Function

        Public Shared Function TryGetResource(keyword As String) As String
            Select Case keyword.ToLowerInvariant()
                Case "enum"
                    Return FeaturesResources.enum_
                Case "enum value"
                    Return FeaturesResources.enum_value
                Case "class"
                    Return FeaturesResources.class_
                Case "structure"
                    Return VBFeaturesResources.structure_
                Case "module"
                    Return VBFeaturesResources.module_
                Case "interface"
                    Return FeaturesResources.interface_
                Case "delegate"
                    Return FeaturesResources.delegate_
                Case "lambda"
                    Return VBFeaturesResources.Lambda
                Case "const field"
                    Return FeaturesResources.const_field
                Case "field"
                    Return FeaturesResources.field
                Case "auto-property"
                    Return FeaturesResources.auto_property
                Case "property"
                    Return FeaturesResources.property_
                Case "event"
                    Return FeaturesResources.event_
                Case "method"
                    Return FeaturesResources.method
                Case "constructor"
                    Return FeaturesResources.constructor
                Case "shared constructor"
                    Return VBFeaturesResources.Shared_constructor
                Case "parameter"
                    Return FeaturesResources.parameter
                Case "type parameter"
                    Return FeaturesResources.type_parameter
                Case "withevents field"
                    Return VBFeaturesResources.WithEvents_field
                Case Else
                    Return Nothing
            End Select
        End Function

        Friend Shared NoSemanticEdits As SemanticEditDescription() = Array.Empty(Of SemanticEditDescription)

        Friend Overloads Shared Function Diagnostic(rudeEditKind As RudeEditKind, squiggle As String, ParamArray arguments As String()) As RudeEditDiagnosticDescription
            Return New RudeEditDiagnosticDescription(rudeEditKind, squiggle, arguments, firstLine:=Nothing)
        End Function

        Friend Shared Function RuntimeRudeEdit(marker As Integer, rudeEditKind As RudeEditKind, position As (displayLine As Integer, displayColumn As Integer), ParamArray arguments As String()) As RuntimeRudeEditDescription
            Return New RuntimeRudeEditDescription(marker, rudeEditKind, New LinePosition(position.displayLine - 1, position.displayColumn - 1), arguments)
        End Function

        Friend Shared Function SemanticEdit(kind As SemanticEditKind,
                                            symbolProvider As Func(Of Compilation, ISymbol),
                                            syntaxMap As IEnumerable(Of (TextSpan, TextSpan)),
                                            Optional rudeEdits As IEnumerable(Of RuntimeRudeEditDescription) = Nothing,
                                            Optional partialType As String = Nothing,
                                            Optional deletedSymbolContainerProvider As Func(Of Compilation, ISymbol) = Nothing) As SemanticEditDescription
            Return New SemanticEditDescription(
                kind,
                symbolProvider,
                If(partialType Is Nothing, Nothing, Function(c As Compilation) CType(c.GetMember(partialType), ITypeSymbol)),
                syntaxMap,
                rudeEdits,
                hasSyntaxMap:=syntaxMap IsNot Nothing,
                deletedSymbolContainerProvider)
        End Function

        Friend Shared Function SemanticEdit(kind As SemanticEditKind,
                                            symbolProvider As Func(Of Compilation, ISymbol),
                                            Optional partialType As String = Nothing,
                                            Optional preserveLocalVariables As Boolean = False,
                                            Optional deletedSymbolContainerProvider As Func(Of Compilation, ISymbol) = Nothing) As SemanticEditDescription
            Return New SemanticEditDescription(
                kind,
                symbolProvider,
                If(partialType Is Nothing, Nothing, Function(c As Compilation) CType(c.GetMember(partialType), ITypeSymbol)),
                syntaxMap:=Nothing,
                rudeEdits:=Nothing,
                hasSyntaxMap:=preserveLocalVariables,
                deletedSymbolContainerProvider)
        End Function

        Friend Shared Function DeletedSymbolDisplay(kind As String, displayName As String) As String
            Return String.Format(FeaturesResources.member_kind_and_name, kind, displayName)
        End Function

        Friend Shared Function DocumentResults(
            Optional activeStatements As ActiveStatementsDescription = Nothing,
            Optional semanticEdits As SemanticEditDescription() = Nothing,
            Optional diagnostics As RudeEditDiagnosticDescription() = Nothing) As DocumentAnalysisResultsDescription
            Return New DocumentAnalysisResultsDescription(activeStatements, semanticEdits, lineEdits:=Nothing, diagnostics)
        End Function

        Private Shared Function GetDocumentFilePath(documentIndex As Integer) As String
            Return Path.Combine(TempRoot.Root, documentIndex.ToString() & ".vb")
        End Function

        Private Shared Function ParseSource(markedSource As String, Optional documentIndex As Integer = 0) As SyntaxTree
            Return SyntaxFactory.ParseSyntaxTree(
                SourceMarkers.Clear(markedSource),
                VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest),
                path:=GetDocumentFilePath(documentIndex))
        End Function

        Friend Shared Function GetTopEdits(src1 As String, src2 As String, Optional documentIndex As Integer = 0) As EditScript(Of SyntaxNode)
            Dim tree1 = ParseSource(src1, documentIndex)
            Dim tree2 = ParseSource(src2, documentIndex)

            tree1.GetDiagnostics().Verify()
            tree2.GetDiagnostics().Verify()

            Dim match = SyntaxComparer.TopLevel.ComputeMatch(tree1.GetRoot(), tree2.GetRoot())
            Return match.GetTreeEdits()
        End Function

        Public Shared Function GetTopEdits(methodEdits As EditScript(Of SyntaxNode)) As EditScript(Of SyntaxNode)
            Dim oldMethodSource = methodEdits.Match.OldRoot.ToFullString()
            Dim newMethodSource = methodEdits.Match.NewRoot.ToFullString()

            Return GetTopEdits(WrapMethodBodyWithClass(oldMethodSource), WrapMethodBodyWithClass(newMethodSource))
        End Function

        Friend Shared Function GetMethodEdits(src1 As String, src2 As String, Optional methodKind As MethodKind = MethodKind.Regular) As EditScript(Of SyntaxNode)
            Dim match = GetMethodMatch(src1, src2, methodKind)
            Return match.GetTreeEdits()
        End Function

        Friend Shared Function GetMethodMatch(src1 As String, src2 As String, Optional methodKind As MethodKind = MethodKind.Regular) As Match(Of SyntaxNode)
            Dim m1 = MakeMethodBody(src1, methodKind)
            Dim m2 = MakeMethodBody(src2, methodKind)

            Dim match = m1.ComputeSingleRootMatch(m2, knownMatches:=Nothing)

            Dim stateMachineInfo1 = m1.GetStateMachineInfo()
            Dim stateMachineInfo2 = m2.GetStateMachineInfo()
            Dim needsSyntaxMap = stateMachineInfo1.HasSuspensionPoints AndAlso stateMachineInfo2.HasSuspensionPoints

            Assert.Equal(methodKind <> MethodKind.Regular, needsSyntaxMap)

            Return match
        End Function

        Public Shared Function GetMethodMatches(src1 As String,
                                                src2 As String,
                                                Optional kind As MethodKind = MethodKind.Regular) As IEnumerable(Of KeyValuePair(Of SyntaxNode, SyntaxNode))
            Dim methodMatch = GetMethodMatch(src1, src2, kind)
            Return EditAndContinueTestVerifier.GetMethodMatches(CreateAnalyzer(), methodMatch)
        End Function

        Public Shared Function ToMatchingPairs(match As Match(Of SyntaxNode)) As MatchingPairs
            Return EditAndContinueTestVerifier.ToMatchingPairs(match)
        End Function

        Public Shared Function ToMatchingPairs(matches As IEnumerable(Of KeyValuePair(Of SyntaxNode, SyntaxNode))) As MatchingPairs
            Return EditAndContinueTestVerifier.ToMatchingPairs(matches)
        End Function

        Friend Shared Function MakeMethodBody(bodySource As String, Optional stateMachine As MethodKind = MethodKind.Regular) As MemberBody
            Dim source = WrapMethodBodyWithClass(bodySource, stateMachine)

            Dim tree = ParseSource(source)
            Dim root = tree.GetRoot()
            tree.GetDiagnostics().Verify()

            Dim declaration = DirectCast(DirectCast(root, CompilationUnitSyntax).Members(0), ClassBlockSyntax).Members(0)
            Return SyntaxUtilities.TryGetDeclarationBody(SyntaxFactory.SyntaxTree(declaration).GetRoot())
        End Function

        Private Shared Function WrapMethodBodyWithClass(bodySource As String, Optional kind As MethodKind = MethodKind.Regular) As String
            Select Case kind
                Case MethodKind.Iterator
                    Return "Class C" & vbLf & "Iterator Function F() As IEnumerable(Of Integer)" & vbLf & bodySource & " : End Function : End Class"

                Case MethodKind.Async
                    Return "Class C" & vbLf & "Async Function F() As Task(Of Integer)" & vbLf & bodySource & " : End Function : End Class"

                Case Else
                    Return "Class C" & vbLf & "Sub F()" & vbLf & bodySource & " : End Sub : End Class"
            End Select
        End Function

        Friend Shared Function GetActiveStatements(oldSource As String, newSource As String, Optional flags As ActiveStatementFlags() = Nothing, Optional documentIndex As Integer = 0) As ActiveStatementsDescription
            Return New ActiveStatementsDescription(oldSource, newSource, Function(source) SyntaxFactory.ParseSyntaxTree(source, path:=GetDocumentFilePath(documentIndex)), flags)
        End Function

        Friend Shared Function GetSyntaxMap(oldSource As String, newSource As String) As SyntaxMapDescription
            Return New SyntaxMapDescription(oldSource, newSource)
        End Function
    End Class
End Namespace
