' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel
    Partial Friend Class VisualBasicCodeModelService

        Protected Overrides Function CreateNodeLocator() As AbstractNodeLocator
            Return New NodeLocator()
        End Function

        Private Class NodeLocator
            Inherits AbstractNodeLocator

            Protected Overrides ReadOnly Property LanguageName As String
                Get
                    Return LanguageNames.VisualBasic
                End Get
            End Property

            Protected Overrides ReadOnly Property DefaultPart As EnvDTE.vsCMPart
                Get
                    Return EnvDTE.vsCMPart.vsCMPartWhole
                End Get
            End Property

            Protected Overrides Function GetStartPoint(text As SourceText, options As LineFormattingOptions, node As SyntaxNode, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Select Case node.Kind
                    Case SyntaxKind.ClassBlock,
                         SyntaxKind.InterfaceBlock,
                         SyntaxKind.ModuleBlock,
                         SyntaxKind.StructureBlock
                        Return GetTypeBlockStartPoint(text, options, DirectCast(node, TypeBlockSyntax), part)
                    Case SyntaxKind.EnumBlock
                        Return GetEnumBlockStartPoint(text, options, DirectCast(node, EnumBlockSyntax), part)
                    Case SyntaxKind.ClassStatement,
                         SyntaxKind.InterfaceStatement,
                         SyntaxKind.ModuleStatement,
                         SyntaxKind.StructureStatement
                        Return GetTypeBlockStartPoint(text, options, DirectCast(node.Parent, TypeBlockSyntax), part)
                    Case SyntaxKind.EnumStatement
                        Return GetEnumBlockStartPoint(text, options, DirectCast(node.Parent, EnumBlockSyntax), part)
                    Case SyntaxKind.ConstructorBlock,
                         SyntaxKind.FunctionBlock,
                         SyntaxKind.OperatorBlock,
                         SyntaxKind.SubBlock,
                         SyntaxKind.GetAccessorBlock,
                         SyntaxKind.SetAccessorBlock,
                         SyntaxKind.AddHandlerAccessorBlock,
                         SyntaxKind.RemoveHandlerAccessorBlock,
                         SyntaxKind.RaiseEventAccessorBlock
                        Return GetMethodBlockStartPoint(text, options, DirectCast(node, MethodBlockBaseSyntax), part)
                    Case SyntaxKind.SubNewStatement,
                         SyntaxKind.OperatorStatement,
                         SyntaxKind.GetAccessorStatement,
                         SyntaxKind.SetAccessorStatement,
                         SyntaxKind.AddHandlerStatement,
                         SyntaxKind.RemoveHandlerStatement,
                         SyntaxKind.RaiseEventStatement
                        Return GetMethodBlockStartPoint(text, options, DirectCast(node.Parent, MethodBlockBaseSyntax), part)
                    Case SyntaxKind.FunctionStatement,
                         SyntaxKind.SubStatement
                        If TypeOf node.Parent Is MethodBlockBaseSyntax Then
                            Return GetMethodBlockStartPoint(text, options, DirectCast(node.Parent, MethodBlockBaseSyntax), part)
                        Else
                            Return GetMethodStatementStartPoint(text, DirectCast(node, MethodStatementSyntax), part)
                        End If

                    Case SyntaxKind.DeclareFunctionStatement,
                         SyntaxKind.DeclareSubStatement
                        Return GetDeclareStatementStartPoint(text, DirectCast(node, DeclareStatementSyntax), part)
                    Case SyntaxKind.PropertyBlock
                        Return GetPropertyBlockStartPoint(text, DirectCast(node, PropertyBlockSyntax), part)
                    Case SyntaxKind.PropertyStatement
                        Return GetPropertyStatementStartPoint(text, DirectCast(node, PropertyStatementSyntax), part)

                    Case SyntaxKind.EventBlock
                        Return GetEventBlockStartPoint(text, options, DirectCast(node, EventBlockSyntax), part)
                    Case SyntaxKind.EventStatement
                        Return GetEventStatementStartPoint(text, DirectCast(node, EventStatementSyntax), part)

                    Case SyntaxKind.DelegateFunctionStatement,
                         SyntaxKind.DelegateSubStatement
                        Return GetDelegateStatementStartPoint(text, DirectCast(node, DelegateStatementSyntax), part)

                    Case SyntaxKind.NamespaceBlock
                        Return GetNamespaceBlockStartPoint(text, options, DirectCast(node, NamespaceBlockSyntax), part)
                    Case SyntaxKind.NamespaceStatement
                        Return GetNamespaceBlockStartPoint(text, options, DirectCast(node.Parent, NamespaceBlockSyntax), part)
                    Case SyntaxKind.ModifiedIdentifier
                        Return GetVariableStartPoint(text, DirectCast(node, ModifiedIdentifierSyntax), part)
                    Case SyntaxKind.EnumMemberDeclaration
                        Return GetVariableStartPoint(text, DirectCast(node, EnumMemberDeclarationSyntax), part)
                    Case SyntaxKind.Parameter
                        Return GetParameterStartPoint(text, DirectCast(node, ParameterSyntax), part)

                    Case SyntaxKind.Attribute
                        Return GetAttributeStartPoint(text, DirectCast(node, AttributeSyntax), part)
                    Case SyntaxKind.SimpleArgument,
                         SyntaxKind.OmittedArgument
                        Return GetAttributeArgumentStartPoint(text, DirectCast(node, ArgumentSyntax), part)

                    Case SyntaxKind.SimpleImportsClause
                        Return GetImportsStatementStartPoint(text, DirectCast(node.Parent, ImportsStatementSyntax), part)
                    Case SyntaxKind.OptionStatement
                        Return GetOptionStatementStartPoint(text, DirectCast(node, OptionStatementSyntax), part)
                    Case SyntaxKind.InheritsStatement
                        Return GetInheritsStatementStartPoint(text, DirectCast(node, InheritsStatementSyntax), part)
                    Case SyntaxKind.ImplementsStatement
                        Return GetImplementsStatementStartPoint(text, DirectCast(node, ImplementsStatementSyntax), part)
                    Case Else
                        Debug.Fail(String.Format("Unsupported node kind: {0}", CType(node.Kind, SyntaxKind)))
                        Throw New NotSupportedException()
                End Select
            End Function

            Protected Overrides Function GetEndPoint(text As SourceText, options As LineFormattingOptions, node As SyntaxNode, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Select Case node.Kind
                    Case SyntaxKind.ClassBlock,
                         SyntaxKind.InterfaceBlock,
                         SyntaxKind.ModuleBlock,
                         SyntaxKind.StructureBlock
                        Return GetTypeBlockEndPoint(text, DirectCast(node, TypeBlockSyntax), part)
                    Case SyntaxKind.EnumBlock
                        Return GetEnumBlockEndPoint(text, DirectCast(node, EnumBlockSyntax), part)
                    Case SyntaxKind.ClassStatement,
                         SyntaxKind.InterfaceStatement,
                         SyntaxKind.ModuleBlock,
                         SyntaxKind.StructureBlock
                        Return GetTypeBlockEndPoint(text, DirectCast(node.Parent, TypeBlockSyntax), part)
                    Case SyntaxKind.EnumStatement
                        Return GetEnumBlockEndPoint(text, DirectCast(node.Parent, EnumBlockSyntax), part)
                    Case SyntaxKind.ConstructorBlock,
                         SyntaxKind.FunctionBlock,
                         SyntaxKind.OperatorBlock,
                         SyntaxKind.SubBlock,
                         SyntaxKind.GetAccessorBlock,
                         SyntaxKind.SetAccessorBlock,
                         SyntaxKind.AddHandlerAccessorBlock,
                         SyntaxKind.RemoveHandlerAccessorBlock,
                         SyntaxKind.RaiseEventAccessorBlock
                        Return GetMethodBlockEndPoint(text, DirectCast(node, MethodBlockBaseSyntax), part)
                    Case SyntaxKind.SubNewStatement,
                         SyntaxKind.OperatorStatement,
                         SyntaxKind.GetAccessorStatement,
                         SyntaxKind.SetAccessorStatement,
                         SyntaxKind.AddHandlerStatement,
                         SyntaxKind.RemoveHandlerStatement,
                         SyntaxKind.RaiseEventStatement
                        Return GetMethodBlockEndPoint(text, DirectCast(node.Parent, MethodBlockBaseSyntax), part)
                    Case SyntaxKind.FunctionStatement,
                         SyntaxKind.SubStatement
                        If TypeOf node.Parent Is MethodBlockBaseSyntax Then
                            Return GetMethodBlockEndPoint(text, DirectCast(node.Parent, MethodBlockBaseSyntax), part)
                        Else
                            Return GetMethodStatementEndPoint(text, DirectCast(node, MethodStatementSyntax), part)
                        End If

                    Case SyntaxKind.DeclareFunctionStatement,
                         SyntaxKind.DeclareSubStatement
                        Return GetDeclareStatementEndPoint(text, DirectCast(node, DeclareStatementSyntax), part)
                    Case SyntaxKind.PropertyBlock
                        Return GetPropertyBlockEndPoint(text, DirectCast(node, PropertyBlockSyntax), part)
                    Case SyntaxKind.PropertyStatement
                        Return GetPropertyStatementEndPoint(text, DirectCast(node, PropertyStatementSyntax), part)

                    Case SyntaxKind.EventBlock
                        Return GetEventBlockEndPoint(text, DirectCast(node, EventBlockSyntax), part)
                    Case SyntaxKind.EventStatement
                        Return GetEventStatementEndPoint(text, DirectCast(node, EventStatementSyntax), part)

                    Case SyntaxKind.DelegateFunctionStatement,
                         SyntaxKind.DelegateSubStatement
                        Return GetDelegateStatementEndPoint(text, DirectCast(node, DelegateStatementSyntax), part)

                    Case SyntaxKind.NamespaceBlock
                        Return GetNamespaceBlockEndPoint(text, DirectCast(node, NamespaceBlockSyntax), part)
                    Case SyntaxKind.NamespaceStatement
                        Return GetNamespaceBlockEndPoint(text, DirectCast(node.Parent, NamespaceBlockSyntax), part)
                    Case SyntaxKind.ModifiedIdentifier
                        Return GetVariableEndPoint(text, DirectCast(node, ModifiedIdentifierSyntax), part)
                    Case SyntaxKind.EnumMemberDeclaration
                        Return GetVariableEndPoint(text, DirectCast(node, EnumMemberDeclarationSyntax), part)
                    Case SyntaxKind.Parameter
                        Return GetParameterEndPoint(text, DirectCast(node, ParameterSyntax), part)

                    Case SyntaxKind.Attribute
                        Return GetAttributeEndPoint(text, DirectCast(node, AttributeSyntax), part)
                    Case SyntaxKind.SimpleArgument,
                         SyntaxKind.OmittedArgument
                        Return GetAttributeArgumentEndPoint(text, DirectCast(node, ArgumentSyntax), part)

                    Case SyntaxKind.SimpleImportsClause
                        Return GetImportsStatementEndPoint(text, DirectCast(node.Parent, ImportsStatementSyntax), part)
                    Case SyntaxKind.OptionStatement
                        Return GetOptionStatementEndPoint(text, DirectCast(node, OptionStatementSyntax), part)
                    Case SyntaxKind.InheritsStatement
                        Return GetInheritsStatementEndPoint(text, DirectCast(node, InheritsStatementSyntax), part)
                    Case SyntaxKind.ImplementsStatement
                        Return GetImplementsStatementEndPoint(text, DirectCast(node, ImplementsStatementSyntax), part)
                    Case Else
                        Debug.Fail(String.Format("Unsupported node kind: {0}", CType(node.Kind, SyntaxKind)))
                        Throw New NotImplementedException()
                End Select
            End Function

            Private Shared Function GetAttributesStartPoint(text As SourceText, attributes As SyntaxList(Of AttributeListSyntax), part As EnvDTE.vsCMPart) As VirtualTreePoint?
                ' VB has a Code Model bug that causes vsCMPartAttributes and vsCMPartAttributesWithDelimiters to return the same value.
                ' You can see the issue clearly in vb\Language\VsPackage\CodeModelHelpers.cpp, CodeModelLocations::GetAttributeSpecifierListLocation.
                ' Essentially, the old code tries to do the right thing for vsCMPartAttributes, but then deliberately falls through to the
                ' vsCMPartAttributesWithDelimiter case, where it overwrites the calculation it just made. Sigh...

                If attributes.Count = 0 Then
                    Return Nothing
                End If

                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        startPosition = attributes.First().LessThanToken.SpanStart

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartName,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter
                        Return Nothing

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(attributes.First().SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetAttributesEndPoint(text As SourceText, attributes As SyntaxList(Of AttributeListSyntax), part As EnvDTE.vsCMPart) As VirtualTreePoint?
                ' VB has a Code Model bug that causes vsCMPartAttributes and vsCMPartAttributesWithDelimiters to return the same value.
                ' See GetAttributesStartPoint for the details

                If attributes.Count = 0 Then
                    Return Nothing
                End If

                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        startPosition = attributes.Last().GreaterThanToken.Span.End

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartName,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter
                        Return Nothing

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(attributes.Last().SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetHeaderStartPosition(typeBlock As TypeBlockSyntax) As Integer
                If typeBlock.BlockStatement.Modifiers.Count > 0 Then
                    Return typeBlock.BlockStatement.Modifiers.First().SpanStart
                Else
                    Return typeBlock.BlockStatement.DeclarationKeyword.SpanStart
                End If
            End Function

            Private Shared Function GetHeaderStartPosition(enumBlock As EnumBlockSyntax) As Integer
                If enumBlock.EnumStatement.Modifiers.Count > 0 Then
                    Return enumBlock.EnumStatement.Modifiers.First().SpanStart
                Else
                    Return enumBlock.EnumStatement.EnumKeyword.SpanStart
                End If
            End Function

            Private Shared Function GetTypeBlockStartPoint(text As SourceText, options As LineFormattingOptions, typeBlock As TypeBlockSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        startPosition = typeBlock.BlockStatement.Identifier.SpanStart
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return GetAttributesStartPoint(text, typeBlock.BlockStatement.AttributeLists, part)
                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes
                        startPosition = typeBlock.SpanStart
                    Case EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole
                        startPosition = GetHeaderStartPosition(typeBlock)
                    Case EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        Dim statement As StatementSyntax = typeBlock.BlockStatement
                        While statement IsNot Nothing
                            Dim [next] = statement.GetNextNonEmptyStatement()
                            If [next] IsNot Nothing AndAlso
                              ([next].Kind = SyntaxKind.InheritsStatement OrElse
                               [next].Kind = SyntaxKind.ImplementsStatement) Then
                                statement = [next]
                                Continue While
                            Else
                                Exit While
                            End If
                        End While

                        Debug.Assert(statement IsNot Nothing)

                        Dim statementLine = text.Lines.GetLineFromPosition(statement.SpanStart)

                        ' statement points to either the last Inherits/Implements or to the type declaration itself.
                        Dim nextStatement = statement.GetNextNonEmptyStatement()
                        Dim nextStatementLine As Nullable(Of TextLine) = If(nextStatement IsNot Nothing, text.Lines.GetLineFromPosition(nextStatement.SpanStart), Nothing)

                        ' If the next statement is on the same line as the current one, set body start
                        ' position to the end of the current statement
                        If nextStatementLine IsNot Nothing AndAlso nextStatementLine.Value.LineNumber = statementLine.LineNumber Then
                            startPosition = statement.Span.End
                        Else
                            ' Otherwise, use the beginning of the next line.
                            startPosition = text.Lines(statementLine.LineNumber + 1).Start
                        End If

                        If part = EnvDTE.vsCMPart.vsCMPartNavigate Then
                            Return NavigationPointHelpers.GetNavigationPoint(text, options.TabSize, typeBlock.BlockStatement, statementLine.LineNumber + 1)
                        End If

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(typeBlock.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetTypeBlockEndPoint(text As SourceText, typeBlock As TypeBlockSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        startPosition = typeBlock.BlockStatement.Identifier.Span.End
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return GetAttributesEndPoint(text, typeBlock.BlockStatement.AttributeLists, part)
                    Case EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes
                        startPosition = typeBlock.BlockStatement.Span.End
                    Case EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes
                        startPosition = typeBlock.Span.End
                    Case EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter
                        startPosition = typeBlock.EndBlockStatement.SpanStart
                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(typeBlock.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetEnumBlockStartPoint(text As SourceText, options As LineFormattingOptions, enumBlock As EnumBlockSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        startPosition = enumBlock.EnumStatement.Identifier.SpanStart
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return GetAttributesStartPoint(text, enumBlock.EnumStatement.AttributeLists, part)
                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes
                        startPosition = enumBlock.SpanStart
                    Case EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole
                        startPosition = GetHeaderStartPosition(enumBlock)
                    Case EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        Dim statement As StatementSyntax = enumBlock.EnumStatement
                        Dim statementLine = text.Lines.GetLineFromPosition(statement.SpanStart)

                        Dim nextStatement = statement.GetNextNonEmptyStatement()
                        Dim nextStatementLine As Nullable(Of TextLine) = If(nextStatement IsNot Nothing, text.Lines.GetLineFromPosition(nextStatement.SpanStart), Nothing)

                        ' If the next statement is on the same line as the current one, set body start
                        ' position to the end of the current statement
                        If nextStatementLine IsNot Nothing AndAlso nextStatementLine.Value.LineNumber = statementLine.LineNumber Then
                            startPosition = statement.Span.End
                        Else
                            ' Otherwise, use the beginning of the next line.
                            startPosition = text.Lines(statementLine.LineNumber + 1).Start
                        End If

                        If part = EnvDTE.vsCMPart.vsCMPartNavigate Then
                            Return NavigationPointHelpers.GetNavigationPoint(text, options.TabSize, enumBlock.EnumStatement, statementLine.LineNumber + 1)
                        End If

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(enumBlock.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetEnumBlockEndPoint(text As SourceText, enumBlock As EnumBlockSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        startPosition = enumBlock.EnumStatement.Identifier.Span.End
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return GetAttributesEndPoint(text, enumBlock.EnumStatement.AttributeLists, part)
                    Case EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes
                        startPosition = enumBlock.EnumStatement.Span.End
                    Case EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes
                        startPosition = enumBlock.Span.End
                    Case EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter
                        startPosition = enumBlock.EndEnumStatement.SpanStart
                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(enumBlock.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetMethodBlockStartPoint(text As SourceText, options As LineFormattingOptions, methodBlock As MethodBlockBaseSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        Select Case methodBlock.BlockStatement.Kind
                            Case SyntaxKind.SubNewStatement
                                startPosition = DirectCast(methodBlock.BlockStatement, SubNewStatementSyntax).NewKeyword.SpanStart
                            Case SyntaxKind.FunctionStatement,
                                 SyntaxKind.SubStatement
                                startPosition = DirectCast(methodBlock.BlockStatement, MethodStatementSyntax).Identifier.SpanStart
                            Case SyntaxKind.OperatorStatement
                                startPosition = DirectCast(methodBlock.BlockStatement, OperatorStatementSyntax).OperatorToken.SpanStart
                            Case SyntaxKind.GetAccessorStatement,
                                 SyntaxKind.SetAccessorStatement
                                ' For properties accessors, use the name of property block
                                Dim propertyBlock = methodBlock.FirstAncestorOrSelf(Of PropertyBlockSyntax)()
                                If propertyBlock Is Nothing Then
                                    Throw Exceptions.ThrowEFail()
                                End If

                                Return GetPropertyBlockStartPoint(text, propertyBlock, part)
                            Case SyntaxKind.AddHandlerAccessorStatement,
                                 SyntaxKind.RemoveHandlerAccessorStatement,
                                 SyntaxKind.RaiseEventAccessorStatement
                                ' For event accessors, use the name of event block
                                Dim eventBlock = methodBlock.FirstAncestorOrSelf(Of EventBlockSyntax)()
                                If eventBlock Is Nothing Then
                                    Throw Exceptions.ThrowEFail()
                                End If

                                Return GetEventBlockStartPoint(text, options, eventBlock, part)
                            Case Else
                                Throw Exceptions.ThrowEFail()
                        End Select
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return GetAttributesStartPoint(text, methodBlock.BlockStatement.AttributeLists, part)
                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes
                        startPosition = methodBlock.SpanStart
                    Case EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole
                        startPosition = NavigationPointHelpers.GetHeaderStartPosition(methodBlock)
                    Case EnvDTE.vsCMPart.vsCMPartNavigate
                        Return NavigationPointHelpers.GetNavigationPoint(text, options.TabSize, methodBlock)
                    Case EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        Dim startLine = text.Lines.GetLineFromPosition(NavigationPointHelpers.GetHeaderStartPosition(methodBlock))
                        startPosition = text.Lines(startLine.LineNumber + 1).Start

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(methodBlock.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetDeclareStatementStartPoint(text As SourceText, declareStatement As DeclareStatementSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        startPosition = declareStatement.Identifier.SpanStart

                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter

                        Return GetAttributesStartPoint(text, declareStatement.AttributeLists, part)

                    Case EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        If declareStatement.AttributeLists.Count > 0 Then
                            startPosition = declareStatement.AttributeLists.Last().GetLastToken().GetNextToken().SpanStart
                        Else
                            startPosition = declareStatement.SpanStart
                        End If

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes

                        startPosition = declareStatement.SpanStart

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(declareStatement.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetDeclareStatementEndPoint(text As SourceText, declareStatement As DeclareStatementSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim endPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        endPosition = declareStatement.Identifier.Span.End

                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter

                        Return GetAttributesEndPoint(text, declareStatement.AttributeLists, part)

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        endPosition = declareStatement.Span.End

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(declareStatement.SyntaxTree, text, endPosition)
            End Function

            Private Shared Function GetMethodStatementStartPoint(text As SourceText, methodStatement As MethodStatementSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        startPosition = methodStatement.Identifier.SpanStart

                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter

                        Return GetAttributesStartPoint(text, methodStatement.AttributeLists, part)

                    Case EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        If methodStatement.AttributeLists.Count > 0 Then
                            startPosition = methodStatement.AttributeLists.Last().GetLastToken().GetNextToken().SpanStart
                        Else
                            startPosition = methodStatement.SpanStart
                        End If

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes

                        startPosition = methodStatement.SpanStart

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(methodStatement.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetMethodBlockEndPoint(text As SourceText, methodBlock As MethodBlockBaseSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        Select Case methodBlock.BlockStatement.Kind
                            Case SyntaxKind.SubNewStatement
                                startPosition = DirectCast(methodBlock.BlockStatement, SubNewStatementSyntax).NewKeyword.Span.End
                            Case SyntaxKind.FunctionStatement,
                                 SyntaxKind.SubStatement
                                startPosition = DirectCast(methodBlock.BlockStatement, MethodStatementSyntax).Identifier.Span.End
                            Case SyntaxKind.OperatorStatement
                                startPosition = DirectCast(methodBlock.BlockStatement, OperatorStatementSyntax).OperatorToken.Span.End
                            Case SyntaxKind.GetAccessorStatement,
                                 SyntaxKind.SetAccessorStatement
                                ' For properties accessors, use the name of property block
                                Dim propertyBlock = methodBlock.FirstAncestorOrSelf(Of PropertyBlockSyntax)()
                                If propertyBlock Is Nothing Then
                                    Throw Exceptions.ThrowEFail()
                                End If

                                Return GetPropertyBlockEndPoint(text, propertyBlock, part)
                            Case SyntaxKind.AddHandlerAccessorStatement,
                                 SyntaxKind.RemoveHandlerAccessorStatement,
                                 SyntaxKind.RaiseEventAccessorStatement
                                ' For event accessors, use the name of event block
                                Dim eventBlock = methodBlock.FirstAncestorOrSelf(Of EventBlockSyntax)()
                                If eventBlock Is Nothing Then
                                    Throw Exceptions.ThrowEFail()
                                End If

                                Return GetEventBlockEndPoint(text, eventBlock, part)
                            Case Else
                                Throw Exceptions.ThrowEFail()
                        End Select
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return GetAttributesEndPoint(text, methodBlock.BlockStatement.AttributeLists, part)
                    Case EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes
                        startPosition = methodBlock.BlockStatement.Span.End
                    Case EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes
                        startPosition = methodBlock.Span.End
                    Case EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter
                        startPosition = methodBlock.EndBlockStatement.SpanStart
                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(methodBlock.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetMethodStatementEndPoint(text As SourceText, methodStatement As MethodStatementSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim endPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        endPosition = methodStatement.Identifier.Span.End

                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter

                        Return GetAttributesEndPoint(text, methodStatement.AttributeLists, part)

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        endPosition = methodStatement.Span.End

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(methodStatement.SyntaxTree, text, endPosition)
            End Function

            Private Shared Function GetPropertyBlockStartPoint(text As SourceText, propertyBlock As PropertyBlockSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Return GetPropertyStatementStartPoint(text, propertyBlock.PropertyStatement, part)
            End Function

            Private Shared Function GetPropertyStatementStartPoint(text As SourceText, propertyStatement As PropertyStatementSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        startPosition = propertyStatement.Identifier.SpanStart
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return GetAttributesStartPoint(text, propertyStatement.AttributeLists, part)
                    Case EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole
                        If propertyStatement.AttributeLists.Count > 0 Then
                            startPosition = propertyStatement.AttributeLists.Last().GetLastToken().GetNextToken().SpanStart
                        Else
                            startPosition = propertyStatement.SpanStart
                        End If
                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes
                        startPosition = propertyStatement.SpanStart
                    Case EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        Dim beginLine = text.Lines.IndexOf(propertyStatement.Span.End)

                        Dim lineNumber = beginLine + 1
                        startPosition = text.Lines(lineNumber).Start

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(propertyStatement.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetPropertyBlockEndPoint(text As SourceText, propertyBlock As PropertyBlockSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Return GetPropertyStatementEndPoint(text, propertyBlock.PropertyStatement, part)
            End Function

            Private Shared Function GetPropertyStatementEndPoint(text As SourceText, propertyStatement As PropertyStatementSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName

                        startPosition = propertyStatement.Identifier.Span.End

                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter

                        Return GetAttributesEndPoint(text, propertyStatement.AttributeLists, part)

                    Case EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes

                        startPosition = propertyStatement.Span.End

                    Case EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes

                        startPosition = If(propertyStatement.IsParentKind(SyntaxKind.PropertyBlock),
                                           DirectCast(propertyStatement.Parent, PropertyBlockSyntax).EndPropertyStatement.Span.End,
                                           propertyStatement.Span.End)

                    Case EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                         EnvDTE.vsCMPart.vsCMPartNavigate

                        ' Oddity of VB code model: this only happens if it isn't an auto-property. Otherwise, the start of the file is returned.
                        startPosition = If(propertyStatement.IsParentKind(SyntaxKind.PropertyBlock),
                                           DirectCast(propertyStatement.Parent, PropertyBlockSyntax).EndPropertyStatement.SpanStart,
                                           0)

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(propertyStatement.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetEventBlockStartPoint(text As SourceText, options As LineFormattingOptions, eventBlock As EventBlockSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName

                        startPosition = eventBlock.EventStatement.Identifier.SpanStart

                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter

                        Return GetAttributesStartPoint(text, eventBlock.EventStatement.AttributeLists, part)

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes

                        startPosition = eventBlock.SpanStart

                    Case EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole

                        startPosition = NavigationPointHelpers.GetHeaderStartPosition(eventBlock)

                    Case EnvDTE.vsCMPart.vsCMPartNavigate

                        Return NavigationPointHelpers.GetNavigationPoint(text, options.TabSize, eventBlock)

                    Case EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        Dim startLine = text.Lines.GetLineFromPosition(NavigationPointHelpers.GetHeaderStartPosition(eventBlock))
                        startPosition = text.Lines(startLine.LineNumber + 1).Start

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(eventBlock.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetEventStatementStartPoint(text As SourceText, eventStatement As EventStatementSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName

                        startPosition = eventStatement.Identifier.SpanStart

                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter

                        Return GetAttributesStartPoint(text, eventStatement.AttributeLists, part)

                    Case EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        If eventStatement.AttributeLists.Count > 0 Then
                            startPosition = eventStatement.AttributeLists.Last().GetLastToken().GetNextToken().SpanStart
                        Else
                            startPosition = eventStatement.SpanStart
                        End If

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes

                        startPosition = eventStatement.SpanStart

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(eventStatement.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetEventBlockEndPoint(text As SourceText, eventBlock As EventBlockSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Return GetEventStatementEndPoint(text, eventBlock.EventStatement, part)
            End Function

            Private Shared Function GetEventStatementEndPoint(text As SourceText, eventStatement As EventStatementSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part

                    Case EnvDTE.vsCMPart.vsCMPartName

                        startPosition = eventStatement.Identifier.Span.End

                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter

                        Return GetAttributesEndPoint(text, eventStatement.AttributeLists, part)

                    Case EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes

                        startPosition = eventStatement.Span.End

                    Case EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes

                        startPosition = If(eventStatement.IsParentKind(SyntaxKind.EventBlock),
                                           DirectCast(eventStatement.Parent, EventBlockSyntax).EndEventStatement.Span.End,
                                           eventStatement.Span.End)

                    Case EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                         EnvDTE.vsCMPart.vsCMPartNavigate

                        startPosition = If(eventStatement.IsParentKind(SyntaxKind.EventBlock),
                                           DirectCast(eventStatement.Parent, EventBlockSyntax).EndEventStatement.SpanStart,
                                           eventStatement.Span.End)

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(eventStatement.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetDelegateStatementStartPoint(text As SourceText, delegateStatement As DelegateStatementSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        startPosition = delegateStatement.Identifier.SpanStart

                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter

                        Return GetAttributesStartPoint(text, delegateStatement.AttributeLists, part)

                    Case EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        If delegateStatement.AttributeLists.Count > 0 Then
                            startPosition = delegateStatement.AttributeLists.Last().GetLastToken().GetNextToken().SpanStart
                        Else
                            startPosition = delegateStatement.SpanStart
                        End If

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes

                        startPosition = delegateStatement.SpanStart

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(delegateStatement.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetDelegateStatementEndPoint(text As SourceText, delegateStatement As DelegateStatementSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim endPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        endPosition = delegateStatement.Identifier.Span.End

                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter

                        Return GetAttributesEndPoint(text, delegateStatement.AttributeLists, part)

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        endPosition = delegateStatement.Span.End

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(delegateStatement.SyntaxTree, text, endPosition)
            End Function

            Private Shared Function GetTrailingColonTrivia(statement As StatementSyntax) As SyntaxTrivia?
                If Not statement.HasTrailingTrivia Then
                    Return Nothing
                End If

                Return statement _
                    .GetTrailingTrivia() _
                    .FirstOrNull(Function(t) t.Kind = SyntaxKind.ColonTrivia)
            End Function

            Private Shared Function GetNamespaceBlockStartPoint(text As SourceText, options As LineFormattingOptions, namespaceBlock As NamespaceBlockSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        startPosition = namespaceBlock.NamespaceStatement.Name.SpanStart

                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return Nothing

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWhole
                        startPosition = namespaceBlock.NamespaceStatement.SpanStart

                    Case EnvDTE.vsCMPart.vsCMPartNavigate
                        Dim beginStatement = namespaceBlock.NamespaceStatement

                        Dim beginLine = text.Lines.IndexOf(beginStatement.SpanStart)
                        Dim lineNumber = beginLine + 1

                        ' If the begin statement has trailing colon trivia, we assume the body starts at the colon.
                        Dim colonTrivia = GetTrailingColonTrivia(beginStatement)
                        If colonTrivia IsNot Nothing Then
                            lineNumber = text.Lines.IndexOf(colonTrivia.Value.SpanStart)
                        End If

                        Return NavigationPointHelpers.GetNavigationPoint(text, options.TabSize, namespaceBlock.NamespaceStatement, lineNumber)

                    Case EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        Dim beginStatement = namespaceBlock.NamespaceStatement
                        Dim endStatement = namespaceBlock.EndNamespaceStatement

                        ' Handle case where begin statement has trailing colon trivia, e.g. Namespace N : End Namespace
                        Dim colonTrivia = GetTrailingColonTrivia(beginStatement)
                        If colonTrivia IsNot Nothing Then
                            startPosition = colonTrivia.Value.SpanStart
                            Exit Select
                        End If

                        Dim beginLine = text.Lines.IndexOf(beginStatement.SpanStart)

                        Dim lineNumber = beginLine + 1
                        startPosition = text.Lines(lineNumber).Start

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(namespaceBlock.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetNamespaceBlockEndPoint(text As SourceText, namespaceBlock As NamespaceBlockSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim endPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        endPosition = namespaceBlock.NamespaceStatement.Name.Span.End

                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return Nothing

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader
                        endPosition = namespaceBlock.NamespaceStatement.Span.End

                    Case EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWhole
                        endPosition = namespaceBlock.EndNamespaceStatement.Span.End

                    Case EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                         EnvDTE.vsCMPart.vsCMPartNavigate

                        endPosition = namespaceBlock.EndNamespaceStatement.SpanStart
                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(namespaceBlock.SyntaxTree, text, endPosition)
            End Function

            Private Shared Function GetVariableStartPoint(text As SourceText, variable As ModifiedIdentifierSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim fieldDeclaration = variable.FirstAncestorOrSelf(Of FieldDeclarationSyntax)()
                Debug.Assert(fieldDeclaration IsNot Nothing)

                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        startPosition = variable.SpanStart
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return GetAttributesStartPoint(text, fieldDeclaration.AttributeLists, part)
                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes
                        startPosition = fieldDeclaration.SpanStart
                    Case EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter
                        If fieldDeclaration.Modifiers.Count > 0 Then
                            startPosition = fieldDeclaration.Modifiers.First().SpanStart
                        Else
                            startPosition = fieldDeclaration.Declarators.First().SpanStart
                        End If
                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(variable.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetVariableStartPoint(text As SourceText, enumMember As EnumMemberDeclarationSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        startPosition = enumMember.Identifier.SpanStart
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return GetAttributesStartPoint(text, enumMember.AttributeLists, part)
                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes
                        startPosition = enumMember.SpanStart
                    Case EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter
                        startPosition = enumMember.Identifier.SpanStart
                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(enumMember.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetVariableEndPoint(text As SourceText, variable As ModifiedIdentifierSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim fieldDeclaration = variable.FirstAncestorOrSelf(Of FieldDeclarationSyntax)()
                Debug.Assert(fieldDeclaration IsNot Nothing)

                Dim endPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        endPosition = variable.Span.End
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return GetAttributesEndPoint(text, fieldDeclaration.AttributeLists, part)
                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter
                        endPosition = fieldDeclaration.Span.End
                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(variable.SyntaxTree, text, endPosition)
            End Function

            Private Shared Function GetVariableEndPoint(text As SourceText, enumMember As EnumMemberDeclarationSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim endPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        endPosition = enumMember.Identifier.Span.End
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return GetAttributesEndPoint(text, enumMember.AttributeLists, part)
                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter
                        endPosition = enumMember.Span.End
                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(enumMember.SyntaxTree, text, endPosition)
            End Function

            Private Shared Function GetParameterStartPoint(text As SourceText, parameter As ParameterSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        startPosition = parameter.Identifier.SpanStart
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return GetAttributesStartPoint(text, parameter.AttributeLists, part)
                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes
                        startPosition = parameter.SpanStart
                    Case EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        If parameter.Modifiers.Any() Then
                            startPosition = parameter.Modifiers.First().SpanStart
                        Else
                            startPosition = parameter.Identifier.SpanStart
                        End If
                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(parameter.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetParameterEndPoint(text As SourceText, parameter As ParameterSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim endPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName
                        endPosition = parameter.Identifier.Span.End
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return GetAttributesEndPoint(text, parameter.AttributeLists, part)
                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter
                        endPosition = parameter.Span.End
                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(parameter.SyntaxTree, text, endPosition)
            End Function

            Private Shared Function GetImportsStatementStartPoint(text As SourceText, importsStatement As ImportsStatementSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName,
                         EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return Nothing

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        startPosition = importsStatement.SpanStart

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(importsStatement.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetImportsStatementEndPoint(text As SourceText, importsStatement As ImportsStatementSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim endPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName,
                         EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return Nothing

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        endPosition = importsStatement.Span.End

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(importsStatement.SyntaxTree, text, endPosition)
            End Function

            Private Shared Function GetOptionStatementStartPoint(text As SourceText, optionStatement As OptionStatementSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName,
                         EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return Nothing

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        startPosition = optionStatement.SpanStart

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(optionStatement.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetOptionStatementEndPoint(text As SourceText, optionStatement As OptionStatementSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim endPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName,
                         EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return Nothing

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        endPosition = optionStatement.Span.End

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(optionStatement.SyntaxTree, text, endPosition)
            End Function

            Private Shared Function GetInheritsStatementStartPoint(text As SourceText, inheritsStatement As InheritsStatementSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName,
                         EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return Nothing

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        startPosition = inheritsStatement.SpanStart

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(inheritsStatement.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetInheritsStatementEndPoint(text As SourceText, inheritsStatement As InheritsStatementSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim endPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName,
                         EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return Nothing

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        endPosition = inheritsStatement.Span.End

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(inheritsStatement.SyntaxTree, text, endPosition)
            End Function

            Private Shared Function GetImplementsStatementStartPoint(text As SourceText, implementsStatement As ImplementsStatementSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName,
                         EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return Nothing

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        startPosition = implementsStatement.SpanStart

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(implementsStatement.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetImplementsStatementEndPoint(text As SourceText, implementsStatement As ImplementsStatementSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim endPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartName,
                         EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return Nothing

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        endPosition = implementsStatement.Span.End

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(implementsStatement.SyntaxTree, text, endPosition)
            End Function

            Private Shared Function GetAttributeStartPoint(text As SourceText, attribute As AttributeSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return Nothing

                    Case EnvDTE.vsCMPart.vsCMPartName
                        startPosition = attribute.Name.SpanStart

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartName,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        startPosition = attribute.SpanStart

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(attribute.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetAttributeEndPoint(text As SourceText, attribute As AttributeSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim endPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return Nothing

                    Case EnvDTE.vsCMPart.vsCMPartName
                        endPosition = attribute.Name.Span.End

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        endPosition = attribute.Span.End

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(attribute.SyntaxTree, text, endPosition)
            End Function

            Private Shared Function GetAttributeArgumentStartPoint(text As SourceText, argument As ArgumentSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim startPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return Nothing

                    Case EnvDTE.vsCMPart.vsCMPartName
                        If Not argument.IsNamed Then
                            Return Nothing
                        End If

                        startPosition = DirectCast(argument, SimpleArgumentSyntax).NameColonEquals.Name.SpanStart

                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartName,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        startPosition = If(argument.Kind = SyntaxKind.OmittedArgument,
                                           argument.SpanStart - 1,
                                           argument.SpanStart)

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(argument.SyntaxTree, text, startPosition)
            End Function

            Private Shared Function GetAttributeArgumentEndPoint(text As SourceText, argument As ArgumentSyntax, part As EnvDTE.vsCMPart) As VirtualTreePoint?
                Dim endPosition As Integer

                Select Case part
                    Case EnvDTE.vsCMPart.vsCMPartAttributes,
                         EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter
                        Return Nothing

                    Case EnvDTE.vsCMPart.vsCMPartName
                        If Not argument.IsNamed Then

                            Return Nothing

                        End If

                        endPosition = DirectCast(argument, SimpleArgumentSyntax).NameColonEquals.Name.Span.End
                    Case EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartHeader,
                         EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                         EnvDTE.vsCMPart.vsCMPartWhole,
                         EnvDTE.vsCMPart.vsCMPartNavigate,
                         EnvDTE.vsCMPart.vsCMPartName,
                         EnvDTE.vsCMPart.vsCMPartBody,
                         EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter

                        endPosition = argument.Span.End

                    Case Else
                        Throw Exceptions.ThrowEFail()
                End Select

                Return New VirtualTreePoint(argument.SyntaxTree, text, endPosition)
            End Function

        End Class
    End Class
End Namespace
