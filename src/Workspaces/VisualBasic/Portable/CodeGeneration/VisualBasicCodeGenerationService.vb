' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Class VisualBasicCodeGenerationService
        Inherits AbstractCodeGenerationService

        Public Sub New(provider As HostLanguageServices)
            MyBase.New(provider.GetService(Of ISymbolDeclarationService)(),
                       provider.WorkspaceServices.Workspace)
        End Sub

        Public Overloads Overrides Function GetDestination(containerNode As SyntaxNode) As CodeGenerationDestination
            Return VisualBasicCodeGenerationHelpers.GetDestination(containerNode)
        End Function

        Protected Overrides Function GetMemberComparer() As IComparer(Of SyntaxNode)
            Return VisualBasicDeclarationComparer.WithoutNamesInstance
        End Function

        Protected Overrides Function GetAvailableInsertionIndices(destination As SyntaxNode, cancellationToken As CancellationToken) As IList(Of Boolean)
            ' NOTE(cyrusn): We know that the destination overlaps some hidden regions.
            If TypeOf destination Is TypeBlockSyntax Then
                Return DirectCast(destination, TypeBlockSyntax).GetInsertionIndices(cancellationToken)
            End If

            If TypeOf destination Is CompilationUnitSyntax Then
                Return GetAvailableInsertionIndices(DirectCast(destination, CompilationUnitSyntax), cancellationToken)
            End If

            ' TODO(cyrusn): This will exclude all non-type-blocks if they overlap a hidden region.
            ' For example, for enums or namespaces.  We could consider relaxing that and actually
            ' attempting to determine where in the destination are viable, if we think it's worth
            ' it.
            Return Nothing
        End Function

        Private Overloads Function GetAvailableInsertionIndices(destination As CompilationUnitSyntax, cancellationToken As CancellationToken) As IList(Of Boolean)
            Dim members = destination.Members

            Dim indices = New List(Of Boolean)
            If members.Count >= 1 Then
                ' First, see if we can insert between the start of the typeblock, and it's first
                ' member.
                indices.Add(Not destination.OverlapsHiddenPosition(TextSpan.FromBounds(0, destination.Members.First.SpanStart), cancellationToken))

                ' Now, walk between each member and see if something can be inserted between it and
                ' the next member
                For i = 0 To members.Count - 2
                    Dim member1 = members(i)
                    Dim member2 = members(i + 1)

                    indices.Add(Not destination.OverlapsHiddenPosition(member1, member2, cancellationToken))
                Next

                ' Last, see if we can insert between the last member and the end of the typeblock
                indices.Add(Not destination.OverlapsHiddenPosition(
                    TextSpan.FromBounds(destination.Members.Last.Span.End, destination.EndOfFileToken.SpanStart), cancellationToken))
            End If

            Return indices
        End Function

        Protected Overrides Function AddEvent(Of TDeclarationNode As SyntaxNode)(
                destinationType As TDeclarationNode,
                [event] As IEventSymbol,
                options As CodeGenerationOptions,
                availableIndices As IList(Of Boolean)) As TDeclarationNode
            CheckDeclarationNode(Of TypeBlockSyntax)(destinationType)
            Return Cast(Of TDeclarationNode)(AddEventTo(Cast(Of TypeBlockSyntax)(destinationType), [event], options, availableIndices))
        End Function

        Protected Overrides Function AddField(Of TDeclarationNode As SyntaxNode)(
                destinationType As TDeclarationNode,
                field As IFieldSymbol,
                options As CodeGenerationOptions,
                availableIndices As IList(Of Boolean)) As TDeclarationNode
            CheckDeclarationNode(Of EnumBlockSyntax, TypeBlockSyntax, CompilationUnitSyntax)(destinationType)
            If TypeOf destinationType Is EnumBlockSyntax Then
                Return Cast(Of TDeclarationNode)(EnumMemberGenerator.AddEnumMemberTo(Cast(Of EnumBlockSyntax)(destinationType), field, options))
            ElseIf TypeOf destinationType Is TypeBlockSyntax Then
                Return Cast(Of TDeclarationNode)(FieldGenerator.AddFieldTo(Cast(Of TypeBlockSyntax)(destinationType), field, options, availableIndices))
            Else
                Return Cast(Of TDeclarationNode)(FieldGenerator.AddFieldTo(Cast(Of CompilationUnitSyntax)(destinationType), field, options, availableIndices))
            End If
        End Function

        Protected Overrides Function AddProperty(Of TDeclarationNode As SyntaxNode)(
                destinationType As TDeclarationNode,
                [property] As IPropertySymbol,
                options As CodeGenerationOptions,
                availableIndices As IList(Of Boolean)) As TDeclarationNode
            CheckDeclarationNode(Of TypeBlockSyntax, CompilationUnitSyntax)(destinationType)

            If TypeOf destinationType Is TypeBlockSyntax Then
                Return Cast(Of TDeclarationNode)(PropertyGenerator.AddPropertyTo(Cast(Of TypeBlockSyntax)(destinationType), [property], options, availableIndices))
            Else
                Return Cast(Of TDeclarationNode)(PropertyGenerator.AddPropertyTo(Cast(Of CompilationUnitSyntax)(destinationType), [property], options, availableIndices))
            End If
        End Function

        Protected Overrides Function AddMethod(Of TDeclarationNode As SyntaxNode)(
                destination As TDeclarationNode,
                method As IMethodSymbol,
                options As CodeGenerationOptions,
                availableIndices As IList(Of Boolean)) As TDeclarationNode
            CheckDeclarationNode(Of TypeBlockSyntax, CompilationUnitSyntax, NamespaceBlockSyntax)(destination)

            ' Synthesized methods for properties/events are not things we actually generate 
            ' declarations for.
            If method.AssociatedSymbol IsNot Nothing Then
                Return destination
            End If

            Dim typeDeclaration As TypeBlockSyntax = TryCast(destination, TypeBlockSyntax)
            If typeDeclaration IsNot Nothing Then
                If method.IsConstructor() Then
                    Return Cast(Of TDeclarationNode)(ConstructorGenerator.AddConstructorTo(typeDeclaration, method, options, availableIndices))
                End If

                If method.MethodKind = MethodKind.UserDefinedOperator Then
                    Return Cast(Of TDeclarationNode)(OperatorGenerator.AddOperatorTo(typeDeclaration, method, options, availableIndices))
                End If

                If method.MethodKind = MethodKind.Conversion Then
                    Return Cast(Of TDeclarationNode)(ConversionGenerator.AddConversionTo(typeDeclaration, method, options, availableIndices))
                End If

                Return Cast(Of TDeclarationNode)(MethodGenerator.AddMethodTo(typeDeclaration, method, options, availableIndices))
            End If

            If method.IsConstructor() Then
                Return destination
            End If

            Dim compilationUnit = TryCast(destination, CompilationUnitSyntax)
            If compilationUnit IsNot Nothing Then
                Return Cast(Of TDeclarationNode)(MethodGenerator.AddMethodTo(compilationUnit, method, options, availableIndices))
            End If

            Dim ns = Cast(Of NamespaceBlockSyntax)(destination)
            Return Cast(Of TDeclarationNode)(MethodGenerator.AddMethodTo(ns, method, options, availableIndices))
        End Function

        Protected Overloads Overrides Function AddNamedType(Of TDeclarationNode As SyntaxNode)(
                destination As TDeclarationNode,
                namedType As INamedTypeSymbol,
                options As CodeGenerationOptions,
                availableIndices As IList(Of Boolean),
                cancellationToken As CancellationToken) As TDeclarationNode
            CheckDeclarationNode(Of TypeBlockSyntax, NamespaceBlockSyntax, CompilationUnitSyntax)(destination)
            options = If(options, CodeGenerationOptions.Default)
            If TypeOf destination Is TypeBlockSyntax Then
                Return Cast(Of TDeclarationNode)(NamedTypeGenerator.AddNamedTypeTo(Me, Cast(Of TypeBlockSyntax)(destination), namedType, options, availableIndices, cancellationToken))
            ElseIf TypeOf destination Is NamespaceBlockSyntax Then
                Return Cast(Of TDeclarationNode)(NamedTypeGenerator.AddNamedTypeTo(Me, Cast(Of NamespaceBlockSyntax)(destination), namedType, options, availableIndices, cancellationToken))
            Else
                Return Cast(Of TDeclarationNode)(NamedTypeGenerator.AddNamedTypeTo(Me, Cast(Of CompilationUnitSyntax)(destination), namedType, options, availableIndices, cancellationToken))
            End If
        End Function

        Protected Overrides Function AddNamespace(Of TDeclarationNode As SyntaxNode)(
                destination As TDeclarationNode,
                [namespace] As INamespaceSymbol,
                options As CodeGenerationOptions,
                availableIndices As IList(Of Boolean),
                cancellationToken As CancellationToken) As TDeclarationNode
            CheckDeclarationNode(Of CompilationUnitSyntax, NamespaceBlockSyntax)(destination)

            If TypeOf destination Is CompilationUnitSyntax Then
                Return Cast(Of TDeclarationNode)(NamespaceGenerator.AddNamespaceTo(Me, Cast(Of CompilationUnitSyntax)(destination), [namespace], options, availableIndices, cancellationToken))
            Else
                Return Cast(Of TDeclarationNode)(NamespaceGenerator.AddNamespaceTo(Me, Cast(Of NamespaceBlockSyntax)(destination), [namespace], options, availableIndices, cancellationToken))
            End If
        End Function

        Public Overrides Function AddParameters(Of TDeclarationNode As SyntaxNode)(
                destinationMember As TDeclarationNode,
                parameters As IEnumerable(Of IParameterSymbol),
                options As CodeGenerationOptions,
                cancellationToken As CancellationToken) As TDeclarationNode
            Dim methodBlock = TryCast(destinationMember, MethodBlockBaseSyntax)
            Dim methodStatement = If(methodBlock IsNot Nothing,
                                     methodBlock.BlockStatement,
                                     TryCast(destinationMember, MethodBaseSyntax))

            If methodStatement IsNot Nothing Then
                Select Case methodStatement.Kind
                    Case SyntaxKind.GetAccessorStatement, SyntaxKind.SetAccessorStatement
                        ' Don't allow adding parameters to Property Getter/Setter
                        Return destinationMember
                    Case Else
                        Return AddParametersToMethod(Of TDeclarationNode)(methodStatement, methodBlock, parameters, options)
                End Select
            Else
                Dim propertyBlock = TryCast(destinationMember, PropertyBlockSyntax)
                If propertyBlock IsNot Nothing Then
                    Return AddParametersToProperty(Of TDeclarationNode)(propertyBlock, parameters, options)
                End If
            End If

            Return destinationMember
        End Function

        Protected Overrides Function AddMembers(Of TDeclarationNode As SyntaxNode)(destination As TDeclarationNode, members As IEnumerable(Of SyntaxNode)) As TDeclarationNode
            CheckDeclarationNode(Of EnumBlockSyntax, TypeBlockSyntax, NamespaceBlockSyntax, CompilationUnitSyntax)(destination)
            If TypeOf destination Is EnumBlockSyntax Then
                Return Cast(Of TDeclarationNode)(Cast(Of EnumBlockSyntax)(destination).AddMembers(members.Cast(Of EnumMemberDeclarationSyntax).ToArray()))
            ElseIf TypeOf destination Is TypeBlockSyntax Then
                Return Cast(Of TDeclarationNode)(Cast(Of TypeBlockSyntax)(destination).AddMembers(members.Cast(Of StatementSyntax).ToArray()))
            ElseIf TypeOf destination Is NamespaceBlockSyntax Then
                Return Cast(Of TDeclarationNode)(Cast(Of NamespaceBlockSyntax)(destination).AddMembers(members.Cast(Of StatementSyntax).ToArray()))
            Else
                Return Cast(Of TDeclarationNode)(Cast(Of CompilationUnitSyntax)(destination).AddMembers(members.Cast(Of StatementSyntax).ToArray()))
            End If
        End Function

        Private Overloads Shared Function AddParametersToMethod(Of TDeclarationNode As SyntaxNode)(methodStatement As MethodBaseSyntax,
                                                                methodBlock As MethodBlockBaseSyntax,
                                                                parameters As IEnumerable(Of IParameterSymbol),
                                                                options As CodeGenerationOptions) As TDeclarationNode
            Dim newParameterList = AddParameters(methodStatement.ParameterList, parameters, options)
            Dim finalStatement = methodStatement.WithParameterList(newParameterList)

            Dim result As Object

            If methodBlock IsNot Nothing Then
                Select Case methodBlock.Kind
                    Case SyntaxKind.SubBlock,
                        SyntaxKind.FunctionBlock
                        result = DirectCast(methodBlock, MethodBlockSyntax).WithBlockStatement(DirectCast(finalStatement, MethodStatementSyntax))

                    Case SyntaxKind.ConstructorBlock
                        result = DirectCast(methodBlock, ConstructorBlockSyntax).WithBlockStatement(DirectCast(finalStatement, SubNewStatementSyntax))

                    Case SyntaxKind.GetAccessorBlock,
                        SyntaxKind.SetAccessorBlock,
                        SyntaxKind.AddHandlerAccessorBlock,
                        SyntaxKind.RemoveHandlerAccessorBlock,
                        SyntaxKind.RaiseEventAccessorBlock
                        result = DirectCast(methodBlock, AccessorBlockSyntax).WithBlockStatement(DirectCast(finalStatement, AccessorStatementSyntax))

                    Case Else
                        result = DirectCast(methodBlock, OperatorBlockSyntax).WithBlockStatement(DirectCast(finalStatement, OperatorStatementSyntax))
                End Select
            Else
                result = finalStatement
            End If

            Return DirectCast(DirectCast(result, Object), TDeclarationNode)
        End Function

        Private Overloads Shared Function AddParametersToProperty(Of TDeclarationNode As SyntaxNode)(
                                                                propertyBlock As PropertyBlockSyntax,
                                                                parameters As IEnumerable(Of IParameterSymbol),
                                                                options As CodeGenerationOptions) As TDeclarationNode
            Dim propertyStatement = propertyBlock.PropertyStatement
            Dim newParameterList = AddParameters(propertyStatement.ParameterList, parameters, options)
            Dim newPropertyStatement = propertyStatement.WithParameterList(newParameterList)
            Dim newPropertyBlock As SyntaxNode = propertyBlock.WithPropertyStatement(newPropertyStatement)
            Return DirectCast(newPropertyBlock, TDeclarationNode)
        End Function

        Private Overloads Shared Function AddParameters(parameterList As ParameterListSyntax, parameters As IEnumerable(Of IParameterSymbol), options As CodeGenerationOptions) As ParameterListSyntax
            Dim nodesAndTokens = If(parameterList IsNot Nothing,
                    New List(Of SyntaxNodeOrToken)(parameterList.Parameters.GetWithSeparators()),
                    New List(Of SyntaxNodeOrToken))

            Dim currentParamsCount = If(parameterList IsNot Nothing, parameterList.Parameters.Count, 0)
            Dim seenOptional = currentParamsCount > 0 AndAlso parameterList.Parameters(currentParamsCount - 1).Default IsNot Nothing

            For Each parameter In parameters
                If nodesAndTokens.Count > 0 AndAlso nodesAndTokens.Last().Kind() <> SyntaxKind.CommaToken Then
                    nodesAndTokens.Add(SyntaxFactory.Token(SyntaxKind.CommaToken))
                End If

                Dim parameterSyntax = ParameterGenerator.GenerateParameter(parameter, seenOptional, options)
                nodesAndTokens.Add(parameterSyntax)
                seenOptional = seenOptional OrElse parameterSyntax.Default IsNot Nothing
            Next

            Return If(parameterList IsNot Nothing,
                       SyntaxFactory.ParameterList(parameterList.OpenParenToken, SyntaxFactory.SeparatedList(Of ParameterSyntax)(nodesAndTokens), parameterList.CloseParenToken),
                       SyntaxFactory.ParameterList(parameters:=SyntaxFactory.SeparatedList(Of ParameterSyntax)(nodesAndTokens)))
        End Function

        Public Overrides Function AddAttributes(Of TDeclarationNode As SyntaxNode)(
                    destination As TDeclarationNode,
                    attributes As IEnumerable(Of AttributeData),
                    target As SyntaxToken?,
                    options As CodeGenerationOptions,
                    cancellationToken As CancellationToken) As TDeclarationNode

            If target.HasValue AndAlso Not target.Value.IsValidAttributeTarget() Then
                Throw New ArgumentException(NameOf(target))
            End If

            Dim attributeSyntaxList = AttributeGenerator.GenerateAttributeBlocks(attributes.ToImmutableArray(), options, target)

            ' Handle most cases
            Dim member = TryCast(destination, StatementSyntax)
            If member IsNot Nothing Then
                Return Cast(Of TDeclarationNode)(member.AddAttributeLists(attributeSyntaxList.ToArray()))
            End If

            ' Handle global attributes
            Dim compilationUnit = TryCast(destination, CompilationUnitSyntax)
            If compilationUnit IsNot Nothing Then
                Return Cast(Of TDeclarationNode)(compilationUnit.AddAttributes(SyntaxFactory.AttributesStatement(attributeSyntaxList)))
            End If

            ' Handle parameters
            Dim parameter = TryCast(destination, ParameterSyntax)
            If parameter IsNot Nothing Then
                Return Cast(Of TDeclarationNode)(parameter.AddAttributeLists(attributeSyntaxList.ToArray()))
            End If

            Return destination
        End Function

        Public Overrides Function RemoveAttribute(Of TDeclarationNode As SyntaxNode)(destination As TDeclarationNode, attributeToRemove As AttributeData, options As CodeGenerationOptions, cancellationToken As CancellationToken) As TDeclarationNode
            If attributeToRemove.ApplicationSyntaxReference Is Nothing Then
                Throw New ArgumentException(NameOf(attributeToRemove))
            End If

            Dim attributeSyntaxToRemove = attributeToRemove.ApplicationSyntaxReference.GetSyntax(cancellationToken)
            Return RemoveAttribute(destination, attributeSyntaxToRemove, options, cancellationToken)
        End Function

        Public Overrides Function RemoveAttribute(Of TDeclarationNode As SyntaxNode)(destination As TDeclarationNode, attributeToRemove As SyntaxNode, options As CodeGenerationOptions, cancellationToken As CancellationToken) As TDeclarationNode
            If attributeToRemove Is Nothing Then
                Throw New ArgumentException(NameOf(attributeToRemove))
            End If

            ' Removed node could be AttributeSyntax or AttributeListSyntax.
            Dim attributeRemoved As Boolean = False
            Dim positionOfRemovedNode As Integer = -1
            Dim triviaOfRemovedNode As SyntaxTriviaList = Nothing

            ' Handle most cases
            Dim member = TryCast(destination, StatementSyntax)
            If member IsNot Nothing Then
                Dim newAttributeLists = RemoveAttributeFromAttributeLists(member.GetAttributes(), attributeToRemove, options, attributeRemoved, positionOfRemovedNode, triviaOfRemovedNode)
                VerifyAttributeRemoved(attributeRemoved)
                Dim newMember = member.WithAttributeLists(newAttributeLists)
                Return Cast(Of TDeclarationNode)(AppendTriviaAtPosition(newMember, positionOfRemovedNode - destination.FullSpan.Start, triviaOfRemovedNode))
            End If

            ' Handle global attributes
            Dim compilationUnit = TryCast(destination, CompilationUnitSyntax)
            If compilationUnit IsNot Nothing Then
                Dim attributeStatements = compilationUnit.Attributes
                Dim newAttributeStatements = RemoveAttributeFromAttributeStatements(attributeStatements, attributeToRemove, options, attributeRemoved, positionOfRemovedNode, triviaOfRemovedNode)
                VerifyAttributeRemoved(attributeRemoved)
                Dim newCompilationUnit = compilationUnit.WithAttributes(newAttributeStatements)
                Return Cast(Of TDeclarationNode)(AppendTriviaAtPosition(newCompilationUnit, positionOfRemovedNode - destination.FullSpan.Start, triviaOfRemovedNode))
            End If

            ' Handle parameters
            Dim parameter = TryCast(destination, ParameterSyntax)
            If parameter IsNot Nothing Then
                Dim newAttributeLists = RemoveAttributeFromAttributeLists(parameter.AttributeLists, attributeToRemove, options, attributeRemoved, positionOfRemovedNode, triviaOfRemovedNode)
                VerifyAttributeRemoved(attributeRemoved)
                Dim newParameter = parameter.WithAttributeLists(newAttributeLists)
                Return Cast(Of TDeclarationNode)(AppendTriviaAtPosition(newParameter, positionOfRemovedNode - destination.FullSpan.Start, triviaOfRemovedNode))
            End If

            Return destination
        End Function

        Private Shared Function RemoveAttributeFromAttributeLists(attributeLists As SyntaxList(Of AttributeListSyntax), attributeToRemove As SyntaxNode, options As CodeGenerationOptions,
                                                                  <Out> ByRef attributeRemoved As Boolean, <Out> ByRef positionOfRemovedNode As Integer, <Out> ByRef triviaOfRemovedNode As SyntaxTriviaList) As SyntaxList(Of AttributeListSyntax)
            For Each attributeList In attributeLists
                Dim attributes = attributeList.Attributes
                If attributes.Any(Function(a) a Is attributeToRemove) Then
                    attributeRemoved = True
                    Dim trivia As IEnumerable(Of SyntaxTrivia) = Nothing
                    Dim newAttributeLists As IEnumerable(Of AttributeListSyntax) = Nothing
                    If attributes.Count = 1 Then
                        ' Remove the entire attribute list.
                        ComputePositionAndTriviaForRemoveAttributeList(attributeList, Function(t As SyntaxTrivia) t.IsKind(SyntaxKind.EndOfLineTrivia), positionOfRemovedNode, trivia)
                        newAttributeLists = attributeLists.Where(Function(aList) aList IsNot attributeList)
                    Else
                        ' Remove just the given attribute from the attribute list.
                        ComputePositionAndTriviaForRemoveAttributeFromAttributeList(attributeToRemove, Function(t As SyntaxToken) t.IsKind(SyntaxKind.CommaToken), positionOfRemovedNode, trivia)
                        Dim newAttributes = SyntaxFactory.SeparatedList(attributes.Where(Function(a) a IsNot attributeToRemove))
                        Dim newAttributeList = attributeList.WithAttributes(newAttributes)
                        newAttributeLists = attributeLists.Select(Function(attrList) If(attrList Is attributeList, newAttributeList, attrList))
                    End If

                    triviaOfRemovedNode = trivia.ToSyntaxTriviaList()
                    Return SyntaxFactory.List(newAttributeLists)
                End If
            Next

            attributeRemoved = False
            Return attributeLists
        End Function

        Private Shared Function RemoveAttributeFromAttributeStatements(attributeStatements As SyntaxList(Of AttributesStatementSyntax), attributeToRemove As SyntaxNode, options As CodeGenerationOptions,
                                                                       <Out> ByRef attributeRemoved As Boolean, <Out> ByRef positionOfRemovedNode As Integer, <Out> ByRef triviaOfRemovedNode As SyntaxTriviaList) As SyntaxList(Of AttributesStatementSyntax)
            For Each attributeStatement In attributeStatements
                Dim attributeLists = attributeStatement.AttributeLists
                Dim newAttributeLists = RemoveAttributeFromAttributeLists(attributeLists, attributeToRemove, options, attributeRemoved, positionOfRemovedNode, triviaOfRemovedNode)
                If attributeRemoved Then
                    Dim newAttributeStatement = attributeStatement.WithAttributeLists(newAttributeLists)
                    Return SyntaxFactory.List(attributeStatements.Select(Function(attrStatement) If(attrStatement Is attributeStatement, newAttributeStatement, attrStatement)))
                End If
            Next

            attributeRemoved = False
            Return attributeStatements
        End Function

        Private Shared Sub VerifyAttributeRemoved(attributeRemoved As Boolean)
            If Not attributeRemoved Then
                Throw New ArgumentException("attributeToRemove")
            End If
        End Sub

        Public Overrides Function AddStatements(Of TDeclarationNode As SyntaxNode)(
                destinationMember As TDeclarationNode,
                statements As IEnumerable(Of SyntaxNode),
                options As CodeGenerationOptions,
                cancellationToken As CancellationToken) As TDeclarationNode

            Dim methodBlock = TryCast(destinationMember, MethodBlockBaseSyntax)
            If methodBlock IsNot Nothing Then
                Dim allStatements = methodBlock.Statements.Concat(StatementGenerator.GenerateStatements(statements))

                Dim result As Object

                Select Case methodBlock.Kind
                    Case SyntaxKind.SubBlock,
                        SyntaxKind.FunctionBlock
                        result = DirectCast(methodBlock, MethodBlockSyntax).WithStatements(SyntaxFactory.List(allStatements))

                    Case SyntaxKind.ConstructorBlock
                        result = DirectCast(methodBlock, ConstructorBlockSyntax).WithStatements(SyntaxFactory.List(allStatements))

                    Case SyntaxKind.OperatorBlock
                        result = DirectCast(methodBlock, OperatorBlockSyntax).WithStatements(SyntaxFactory.List(allStatements))

                    Case Else
                        result = DirectCast(methodBlock, AccessorBlockSyntax).WithStatements(SyntaxFactory.List(allStatements))
                End Select

                Return DirectCast(DirectCast(result, Object), TDeclarationNode)
            Else
                Return AddStatementsWorker(destinationMember, statements, options, cancellationToken)
            End If
        End Function

        Private Function AddStatementsWorker(Of TDeclarationNode As SyntaxNode)(
                destinationMember As TDeclarationNode,
                statements As IEnumerable(Of SyntaxNode),
                options As CodeGenerationOptions,
                cancellationToken As CancellationToken) As TDeclarationNode
            Dim location = options.BestLocation
            CheckLocation(destinationMember, location)

            Dim token = location.FindToken(cancellationToken)
            Dim oldBlock = token.Parent.GetContainingMultiLineExecutableBlocks().First
            Dim oldBlockStatements = oldBlock.GetExecutableBlockStatements()
            Dim oldBlockStatementsSet = oldBlockStatements.ToSet()

            Dim oldStatement = token.Parent.GetAncestorsOrThis(Of StatementSyntax)().First(AddressOf oldBlockStatementsSet.Contains)
            Dim oldStatementIndex = oldBlockStatements.IndexOf(oldStatement)

            Dim statementArray = statements.OfType(Of StatementSyntax).ToArray()
            Dim newBlock As SyntaxNode
            If options.BeforeThisLocation IsNot Nothing Then
                Dim strippedTrivia As ImmutableArray(Of SyntaxTrivia) = Nothing
                Dim newStatement = VisualBasicSyntaxFactsService.Instance.GetNodeWithoutLeadingBannerAndPreprocessorDirectives(
                    oldStatement, strippedTrivia)

                statementArray(0) = statementArray(0).WithLeadingTrivia(strippedTrivia)

                newBlock = oldBlock.ReplaceNode(oldStatement, newStatement)
                newBlock = newBlock.ReplaceStatements(newBlock.GetExecutableBlockStatements().InsertRange(oldStatementIndex, statementArray))
            Else
                newBlock = oldBlock.ReplaceStatements(oldBlockStatements.InsertRange(oldStatementIndex + 1, statementArray))
            End If

            Return destinationMember.ReplaceNode(oldBlock, newBlock)
        End Function

        Public Overrides Function CreateMethodDeclaration(method As IMethodSymbol,
                                                          destination As CodeGenerationDestination,
                                                          options As CodeGenerationOptions) As SyntaxNode
            ' Synthesized methods for properties/events are not things we actually generate 
            ' declarations for.
            If method.AssociatedSymbol IsNot Nothing Then
                Return Nothing
            End If

            If method.IsConstructor() Then
                Return ConstructorGenerator.GenerateConstructorDeclaration(method, destination, options)
            ElseIf method.IsUserDefinedOperator() Then
                Return OperatorGenerator.GenerateOperatorDeclaration(method, destination, options)
            ElseIf method.IsConversion() Then
                Return ConversionGenerator.GenerateConversionDeclaration(method, destination, options)
            Else
                Return MethodGenerator.GenerateMethodDeclaration(method, destination, options)
            End If
        End Function

        Public Overrides Function CreateEventDeclaration([event] As IEventSymbol,
                                                         destination As CodeGenerationDestination,
                                                         options As CodeGenerationOptions) As SyntaxNode
            Return EventGenerator.GenerateEventDeclaration([event], destination, options)
        End Function

        Public Overrides Function CreateFieldDeclaration(field As IFieldSymbol,
                                                         destination As CodeGenerationDestination,
                                                         options As CodeGenerationOptions) As SyntaxNode
            If destination = CodeGenerationDestination.EnumType Then
                Return EnumMemberGenerator.GenerateEnumMemberDeclaration(field, Nothing, options)
            Else
                Return FieldGenerator.GenerateFieldDeclaration(field, destination, options)
            End If
        End Function

        Public Overrides Function CreatePropertyDeclaration([property] As IPropertySymbol,
                                                            destination As CodeGenerationDestination,
                                                            options As CodeGenerationOptions) As SyntaxNode
            Return PropertyGenerator.GeneratePropertyDeclaration([property], destination, options)
        End Function

        Public Overrides Function CreateNamedTypeDeclaration(namedType As INamedTypeSymbol,
                                                             destination As CodeGenerationDestination,
                                                             options As CodeGenerationOptions,
                                                             cancellationToken As CancellationToken) As SyntaxNode
            Return NamedTypeGenerator.GenerateNamedTypeDeclaration(Me, namedType, options, cancellationToken)
        End Function

        Public Overrides Function CreateNamespaceDeclaration([namespace] As INamespaceSymbol,
                                                             destination As CodeGenerationDestination,
                                                             options As CodeGenerationOptions,
                                                             cancellationToken As CancellationToken) As SyntaxNode
            Return NamespaceGenerator.GenerateNamespaceDeclaration(Me, [namespace], options, cancellationToken)
        End Function

        Private Overloads Shared Function UpdateDeclarationModifiers(Of TDeclarationNode As SyntaxNode)(declaration As TDeclarationNode, computeNewModifiersList As Func(Of SyntaxTokenList, SyntaxTokenList), options As CodeGenerationOptions, cancellationToken As CancellationToken) As TDeclarationNode
            ' Handle type declarations
            Dim typeStatementSyntax = TryCast(declaration, TypeStatementSyntax)
            If typeStatementSyntax IsNot Nothing Then
                Return Cast(Of TDeclarationNode)(typeStatementSyntax.WithModifiers(computeNewModifiersList(typeStatementSyntax.Modifiers)))
            End If

            ' Handle enum declarations
            Dim enumStatementSyntax = TryCast(declaration, EnumStatementSyntax)
            If enumStatementSyntax IsNot Nothing Then
                Return Cast(Of TDeclarationNode)(enumStatementSyntax.WithModifiers(computeNewModifiersList(enumStatementSyntax.Modifiers)))
            End If

            ' Handle methods
            Dim methodBaseSyntax = TryCast(declaration, MethodBaseSyntax)
            If methodBaseSyntax IsNot Nothing Then
                Return Cast(Of TDeclarationNode)(methodBaseSyntax.WithModifiers(computeNewModifiersList(methodBaseSyntax.Modifiers)))
            End If

            ' Handle Incomplete Members
            Dim incompleteMemberSyntax = TryCast(declaration, IncompleteMemberSyntax)
            If incompleteMemberSyntax IsNot Nothing Then
                Return Cast(Of TDeclarationNode)(incompleteMemberSyntax.WithModifiers(computeNewModifiersList(incompleteMemberSyntax.Modifiers)))
            End If

            ' Handle fields
            Dim fieldDeclarationSyntax = TryCast(declaration, FieldDeclarationSyntax)
            If fieldDeclarationSyntax IsNot Nothing Then
                Return Cast(Of TDeclarationNode)(fieldDeclarationSyntax.WithModifiers(computeNewModifiersList(fieldDeclarationSyntax.Modifiers)))
            End If

            ' Handle parameters
            Dim parameterSyntax = TryCast(declaration, ParameterSyntax)
            If parameterSyntax IsNot Nothing Then
                Return Cast(Of TDeclarationNode)(parameterSyntax.WithModifiers(computeNewModifiersList(parameterSyntax.Modifiers)))
            End If

            ' Handle local declarations
            Dim localDeclarationStatementSyntax = TryCast(declaration, LocalDeclarationStatementSyntax)
            If localDeclarationStatementSyntax IsNot Nothing Then
                Return Cast(Of TDeclarationNode)(localDeclarationStatementSyntax.WithModifiers(computeNewModifiersList(localDeclarationStatementSyntax.Modifiers)))
            End If

            Return declaration
        End Function

        Public Overrides Function UpdateDeclarationModifiers(Of TDeclarationNode As SyntaxNode)(declaration As TDeclarationNode, newModifiers As IEnumerable(Of SyntaxToken), options As CodeGenerationOptions, cancellationToken As CancellationToken) As TDeclarationNode
            Dim computeNewModifiersList As Func(Of SyntaxTokenList, SyntaxTokenList) = Function(modifiersList As SyntaxTokenList)
                                                                                           Return SyntaxFactory.TokenList(newModifiers)
                                                                                       End Function
            Return UpdateDeclarationModifiers(declaration, computeNewModifiersList, options, cancellationToken)
        End Function

        Public Overrides Function UpdateDeclarationAccessibility(Of TDeclarationNode As SyntaxNode)(declaration As TDeclarationNode, newAccessibility As Accessibility, options As CodeGenerationOptions, cancellationToken As CancellationToken) As TDeclarationNode
            Dim computeNewModifiersList As Func(Of SyntaxTokenList, SyntaxTokenList) = Function(modifiersList As SyntaxTokenList)
                                                                                           Return UpdateDeclarationAccessibility(modifiersList, newAccessibility, options)
                                                                                       End Function
            Return UpdateDeclarationModifiers(declaration, computeNewModifiersList, options, cancellationToken)
        End Function

        Private Overloads Shared Function UpdateDeclarationAccessibility(modifiersList As SyntaxTokenList, newAccessibility As Accessibility, options As CodeGenerationOptions) As SyntaxTokenList
            Dim newModifierTokens = New List(Of SyntaxToken)()
            VisualBasicCodeGenerationHelpers.AddAccessibilityModifiers(newAccessibility, newModifierTokens, CodeGenerationDestination.Unspecified, options, Accessibility.NotApplicable)
            If newModifierTokens.Count = 0 Then
                Return modifiersList
            End If

            Return SyntaxFactory.TokenList(GetUpdatedDeclarationAccessibilityModifiers(newModifierTokens, modifiersList, Function(modifier As SyntaxToken) SyntaxFacts.IsAccessibilityModifier(modifier.Kind())))
        End Function

        Private Function UpdateSimpleAsClause(asClause As SimpleAsClauseSyntax, newType As ITypeSymbol) As SimpleAsClauseSyntax
            Dim newTypeSyntax = newType.GenerateTypeSyntax().
                WithLeadingTrivia(asClause.GetLeadingTrivia()).
                WithTrailingTrivia(asClause.GetTrailingTrivia())

            Return DirectCast(asClause, SimpleAsClauseSyntax).WithType(newTypeSyntax)
        End Function

        Private Function UpdateAsClause(asClause As AsClauseSyntax, newType As ITypeSymbol) As AsClauseSyntax
            Dim newTypeSyntax = newType.GenerateTypeSyntax().
                WithLeadingTrivia(asClause.GetLeadingTrivia()).
                WithTrailingTrivia(asClause.GetTrailingTrivia())

            Select Case asClause.Kind
                Case SyntaxKind.SimpleAsClause
                    Return DirectCast(asClause, SimpleAsClauseSyntax).WithType(newTypeSyntax)
                Case Else
                    Dim asNewClause = DirectCast(asClause, AsNewClauseSyntax)
                    Dim newExpression = asNewClause.NewExpression
                    Dim updatedNewExpression As NewExpressionSyntax
                    Select Case newExpression.Kind
                        Case SyntaxKind.ArrayCreationExpression
                            updatedNewExpression = DirectCast(newExpression, ArrayCreationExpressionSyntax).WithType(newTypeSyntax)
                        Case SyntaxKind.ObjectCreationExpression
                            updatedNewExpression = DirectCast(newExpression, ObjectCreationExpressionSyntax).WithType(newTypeSyntax)
                        Case Else
                            Return asClause
                    End Select
                    Return asNewClause.WithNewExpression(updatedNewExpression)
            End Select
        End Function

        Public Overrides Function UpdateDeclarationType(Of TDeclarationNode As SyntaxNode)(declaration As TDeclarationNode, newType As ITypeSymbol, options As CodeGenerationOptions, cancellationToken As CancellationToken) As TDeclarationNode
            Dim syntaxNode = TryCast(declaration, VisualBasicSyntaxNode)
            If syntaxNode Is Nothing Then
                Return declaration
            End If

            Select Case syntaxNode.Kind
                Case SyntaxKind.SubStatement, SyntaxKind.FunctionStatement
                    Dim methodStatementSyntax = DirectCast(syntaxNode, MethodStatementSyntax)
                    Dim newAsClause = UpdateSimpleAsClause(methodStatementSyntax.AsClause, newType)
                    Return Cast(Of TDeclarationNode)(methodStatementSyntax.WithAsClause(newAsClause))

                Case SyntaxKind.DeclareSubStatement, SyntaxKind.DeclareFunctionStatement
                    Dim declareStatementSyntax = DirectCast(syntaxNode, DeclareStatementSyntax)
                    Dim newAsClause = UpdateSimpleAsClause(declareStatementSyntax.AsClause, newType)
                    Return Cast(Of TDeclarationNode)(declareStatementSyntax.WithAsClause(newAsClause))

                Case SyntaxKind.DelegateSubStatement, SyntaxKind.DelegateFunctionStatement
                    Dim delegateStatementSyntax = DirectCast(syntaxNode, DelegateStatementSyntax)
                    Dim newAsClause = UpdateSimpleAsClause(delegateStatementSyntax.AsClause, newType)
                    Return Cast(Of TDeclarationNode)(delegateStatementSyntax.WithAsClause(newAsClause))

                Case SyntaxKind.EventStatement
                    Dim eventStatementSyntax = DirectCast(syntaxNode, EventStatementSyntax)
                    Dim newAsClause = UpdateSimpleAsClause(eventStatementSyntax.AsClause, newType)
                    Return Cast(Of TDeclarationNode)(eventStatementSyntax.WithAsClause(newAsClause))

                Case SyntaxKind.OperatorStatement
                    Dim operatorStatementSyntax = DirectCast(syntaxNode, OperatorStatementSyntax)
                    Dim newAsClause = UpdateSimpleAsClause(operatorStatementSyntax.AsClause, newType)
                    Return Cast(Of TDeclarationNode)(operatorStatementSyntax.WithAsClause(newAsClause))

                Case SyntaxKind.PropertyStatement
                    Dim propertyStatementSyntax = DirectCast(syntaxNode, PropertyStatementSyntax)
                    Dim newAsClause = UpdateAsClause(propertyStatementSyntax.AsClause, newType)
                    Return Cast(Of TDeclarationNode)(propertyStatementSyntax.WithAsClause(newAsClause))

                Case SyntaxKind.VariableDeclarator
                    Dim variableDeclaratorSyntax = DirectCast(syntaxNode, VariableDeclaratorSyntax)
                    Dim newAsClause = UpdateAsClause(variableDeclaratorSyntax.AsClause, newType)
                    Return Cast(Of TDeclarationNode)(variableDeclaratorSyntax.WithAsClause(newAsClause))

                Case SyntaxKind.Parameter
                    Dim parameterSyntax = DirectCast(syntaxNode, ParameterSyntax)
                    Dim newAsClause = UpdateSimpleAsClause(parameterSyntax.AsClause, newType)
                    Return Cast(Of TDeclarationNode)(parameterSyntax.WithAsClause(newAsClause))

                Case SyntaxKind.CatchStatement
                    Dim catchStatementSyntax = DirectCast(syntaxNode, CatchStatementSyntax)
                    Dim newAsClause = UpdateSimpleAsClause(catchStatementSyntax.AsClause, newType)
                    Return Cast(Of TDeclarationNode)(catchStatementSyntax.WithAsClause(newAsClause))

                Case SyntaxKind.FunctionLambdaHeader, SyntaxKind.SubLambdaHeader
                    Dim lambdaHeaderSyntax = DirectCast(syntaxNode, LambdaHeaderSyntax)
                    Dim newAsClause = UpdateSimpleAsClause(lambdaHeaderSyntax.AsClause, newType)
                    Return Cast(Of TDeclarationNode)(lambdaHeaderSyntax.WithAsClause(newAsClause))

                Case Else
                    Return declaration
            End Select
        End Function

        Public Overrides Function UpdateDeclarationMembers(Of TDeclarationNode As SyntaxNode)(declaration As TDeclarationNode, newMembers As IList(Of ISymbol), Optional options As CodeGenerationOptions = Nothing, Optional cancellationToken As CancellationToken = Nothing) As TDeclarationNode
            Dim syntaxNode = TryCast(declaration, VisualBasicSyntaxNode)
            If syntaxNode IsNot Nothing Then
                Select Case syntaxNode.Kind
                    Case SyntaxKind.EnumBlock, SyntaxKind.StructureBlock, SyntaxKind.InterfaceBlock, SyntaxKind.ClassBlock
                        Return Cast(Of TDeclarationNode)(NamedTypeGenerator.UpdateNamedTypeDeclaration(Me, DirectCast(syntaxNode, StatementSyntax), newMembers, options, cancellationToken))
                    Case SyntaxKind.CompilationUnit, SyntaxKind.NamespaceBlock
                        Return Cast(Of TDeclarationNode)(NamespaceGenerator.UpdateCompilationUnitOrNamespaceDeclaration(Me, syntaxNode, newMembers, options, cancellationToken))
                End Select
            End If

            Return declaration
        End Function
    End Class
End Namespace
