' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
    Partial Friend Class EndConstructStatementVisitor
        Public Overrides Function VisitPropertyStatement(node As PropertyStatementSyntax) As AbstractEndConstructResult
            If node.Modifiers.Any(SyntaxKind.MustOverrideKeyword) Then
                Return Nothing
            End If

            Dim interfaceBlock = node.FirstAncestorOrSelf(Of InterfaceBlockSyntax)()
            If interfaceBlock IsNot Nothing Then
                Return Nothing
            End If

            Dim propertyBlock = node.GetAncestor(Of PropertyBlockSyntax)()

            If propertyBlock IsNot Nothing Then
                ' If we have an End, we don't have to spit
                If Not propertyBlock.EndPropertyStatement.IsMissing Then
                    Return Nothing
                End If
            Else
                ' We are an autoproperty, so we shouldn't spit. However, if we have parameters, then we aren't a valid
                ' autoproperty and we should spit
                If node.ParameterList Is Nothing OrElse node.ParameterList.Parameters.Count = 0 Then
                    Return Nothing
                End If
            End If

            ' We need to generate our accessors and the End Property
            Dim lines As New List(Of String)
            If NeedsGetAccessor(node) Then
                lines.AddRange(GenerateGetAccessor(node, _subjectBuffer.CurrentSnapshot))
            End If

            If NeedsSetAccessor(node) Then
                lines.AddRange(GenerateSetAccessor(node, _subjectBuffer.CurrentSnapshot))
            End If

            ' If we didn't need any accessors, that already means there's some accessor after us. Spitting
            ' End Property (if we have to) after that point would just make more broken code, so just bail
            If lines.Count = 0 Then
                Return Nothing
            End If

            ' If we are missing a End Property, then spit it
            If propertyBlock Is Nothing OrElse propertyBlock.EndPropertyStatement.IsMissing Then
                Dim aligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(node.SpanStart)
                lines.Add(aligningWhitespace & "End Property")
            End If

            Return New SpitLinesResult(lines)
        End Function

        Public Overrides Function VisitAccessorStatement(node As AccessorStatementSyntax) As AbstractEndConstructResult
            Dim propertyBlock = node.GetAncestor(Of PropertyBlockSyntax)()
            Dim methodBody = node.GetAncestor(Of AccessorBlockSyntax)()

            If propertyBlock Is Nothing OrElse methodBody Is Nothing Then
                ' We must have some accessor floating out in the middle of nowhere, so let's just ignore it
                Return Nothing
            End If

            If Not methodBody.EndBlockStatement.IsMissing Then
                Return Nothing
            End If

            Dim accessorAligningWhitespace = _subjectBuffer.CurrentSnapshot.GetAligningWhitespace(node.SpanStart)
            Dim lines As New List(Of String)
            Dim startOnCurrentLine = False

            If node.Kind = SyntaxKind.GetAccessorStatement Then
                If NeedsGetAccessor(propertyBlock.PropertyStatement, node.GetAncestor(Of AccessorBlockSyntax)()) Then
                    lines.Add("")
                    lines.Add(accessorAligningWhitespace & "End Get")
                Else
                    ' The user is hitting enter on an accessor they don't need, so we'll do nothing as a hint
                    Return Nothing
                End If

                If NeedsSetAccessor(propertyBlock.PropertyStatement) Then
                    lines.AddRange(GenerateSetAccessor(propertyBlock.PropertyStatement, _subjectBuffer.CurrentSnapshot))
                End If
            Else
                If NeedsSetAccessor(propertyBlock.PropertyStatement, node.GetAncestor(Of AccessorBlockSyntax)()) Then
                    ' If the user has typed just Set, we will be kind a spit the Set parameter for them
                    If node.ParameterList Is Nothing Then
                        lines.Add(GenerateSetAccessorArguments(propertyBlock.PropertyStatement))
                        startOnCurrentLine = True
                    End If

                    lines.Add("")
                    lines.Add(accessorAligningWhitespace & "End Set")
                Else
                    ' The user is hitting enter on an accessor they don't need, so we'll do nothing as a hint
                    Return Nothing
                End If

                If NeedsGetAccessor(propertyBlock.PropertyStatement) Then
                    lines.AddRange(GenerateGetAccessor(propertyBlock.PropertyStatement, _subjectBuffer.CurrentSnapshot))
                End If
            End If

            If propertyBlock.EndPropertyStatement.IsMissing Then
                lines.Add(_subjectBuffer.CurrentSnapshot.GetAligningWhitespace(propertyBlock.SpanStart) & "End Property")
            End If

            ' It's possible that in the end we might not have anything to spit. For example, the user might be trying
            ' another accessor that is invalid for the property, or is already duplicated. In that case, we shall spit
            ' nothing.

            If lines.Count = 0 Then
                Return Nothing
            Else
                Return New SpitLinesResult(lines, startOnCurrentLine)
            End If
        End Function

        ''' <summary>
        ''' Given a property declaration, determines if a Get accessor needs to be generated. This checks to see if any
        ''' getters already exist.
        ''' </summary>
        ''' <param name="accessorToIgnore">An existing getter to ignore. When we are checking for existing getters, we
        ''' might be in the middle of typing one that would be a false positive. </param>
        Private Shared Function NeedsGetAccessor(propertyDeclaration As PropertyStatementSyntax, Optional accessorToIgnore As AccessorBlockSyntax = Nothing) As Boolean
            If propertyDeclaration.Modifiers.Any(Function(m) m.IsKind(SyntaxKind.WriteOnlyKeyword)) Then
                Return False
            End If

            Dim propertyBlock = propertyDeclaration.GetAncestor(Of PropertyBlockSyntax)()
            If propertyBlock Is Nothing Then
                Return True
            End If

            For Each accessor In propertyBlock.Accessors
                If accessor IsNot accessorToIgnore And accessor.Kind = SyntaxKind.GetAccessorBlock Then
                    Return False
                End If
            Next

            Return True
        End Function

        Private Shared Function GenerateGetAccessor(propertyDeclaration As PropertyStatementSyntax, snapshot As ITextSnapshot) As String()
            Dim aligningWhitespace = snapshot.GetAligningWhitespace(propertyDeclaration.SpanStart) & "    "
            Return {aligningWhitespace & "Get",
                    "",
                    aligningWhitespace & "End Get"}
        End Function

        ''' <summary>
        ''' Given a property declaration, determines if a Set accessor needs to be generated. This checks to see if any
        ''' getters already exist.
        ''' </summary>
        ''' <param name="accessorToIgnore">An existing getter to ignore. When we are checking for existing getters, we
        ''' might be in the middle of typing one that would be a false positive. </param>
        Private Shared Function NeedsSetAccessor(propertyDeclaration As PropertyStatementSyntax, Optional accessorToIgnore As AccessorBlockSyntax = Nothing) As Boolean
            If propertyDeclaration.Modifiers.Any(Function(m) m.IsKind(SyntaxKind.ReadOnlyKeyword)) Then
                Return False
            End If

            Dim propertyBlock = propertyDeclaration.GetAncestor(Of PropertyBlockSyntax)()
            If propertyBlock Is Nothing Then
                Return True
            End If

            For Each accessor In propertyBlock.Accessors
                If accessor IsNot accessorToIgnore And accessor.Kind = SyntaxKind.SetAccessorBlock Then
                    Return False
                End If
            Next

            Return True
        End Function

        Private Shared Function GenerateSetAccessor(propertyDeclaration As PropertyStatementSyntax, snapshot As ITextSnapshot) As String()
            Dim aligningWhitespace = snapshot.GetAligningWhitespace(propertyDeclaration.SpanStart) & "    "
            Return {aligningWhitespace & "Set" & GenerateSetAccessorArguments(propertyDeclaration),
                    "",
                    aligningWhitespace & "End Set"}
        End Function

        Private Shared Function GenerateSetAccessorArguments(propertyDeclaration As PropertyStatementSyntax) As String
            Dim valueSuffix = ""
            If propertyDeclaration.AsClause IsNot Nothing Then
                valueSuffix = " " & propertyDeclaration.AsClause.ToString
            ElseIf propertyDeclaration.Identifier.Kind = SyntaxKind.IdentifierToken Then
                Dim identifier = propertyDeclaration.Identifier
                If identifier.GetTypeCharacter() <> TypeCharacter.None Then
                    valueSuffix = identifier.GetTypeCharacter().GetTypeCharacterString()
                End If
            End If

            Return "(value" & valueSuffix & ")"
        End Function
    End Class
End Namespace
