' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration

    Friend Class VisualBasicCodeGenerationService
        Inherits AbstractCodeGenerationService(Of VisualBasicCodeGenerationContextInfo)

        Public Sub New(provider As HostLanguageServices)
            MyBase.New(provider.GetService(Of ISymbolDeclarationService)())
        End Sub

        Public Overrides ReadOnly Property DefaultOptions As CodeGenerationOptions
            Get
                Return VisualBasicCodeGenerationOptions.Default
            End Get
        End Property

        Public Overrides Function GetCodeGenerationOptions(options As AnalyzerConfigOptions, fallbackOptions As CodeGenerationOptions) As CodeGenerationOptions
            Return VisualBasicCodeGenerationOptions.Default
        End Function

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

        Private Overloads Shared Function GetAvailableInsertionIndices(destination As CompilationUnitSyntax, cancellationToken As CancellationToken) As IList(Of Boolean)
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
                options As VisualBasicCodeGenerationContextInfo,
                availableIndices As IList(Of Boolean),
                cancellationToken As CancellationToken) As TDeclarationNode
            CheckDeclarationNode(Of TypeBlockSyntax)(destinationType)
            Return Cast(Of TDeclarationNode)(AddEventTo(Cast(Of TypeBlockSyntax)(destinationType), [event], options, availableIndices))
        End Function

        Protected Overrides Function AddField(Of TDeclarationNode As SyntaxNode)(
                destinationType As TDeclarationNode,
                field As IFieldSymbol,
                options As VisualBasicCodeGenerationContextInfo,
                availableIndices As IList(Of Boolean),
                cancellationToken As CancellationToken) As TDeclarationNode
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
                options As VisualBasicCodeGenerationContextInfo,
                availableIndices As IList(Of Boolean),
                cancellationToken As CancellationToken) As TDeclarationNode
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
                options As VisualBasicCodeGenerationContextInfo,
                availableIndices As IList(Of Boolean),
                cancellationToken As CancellationToken) As TDeclarationNode
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
                options As VisualBasicCodeGenerationContextInfo,
                availableIndices As IList(Of Boolean),
                cancellationToken As CancellationToken) As TDeclarationNode
            CheckDeclarationNode(Of TypeBlockSyntax, NamespaceBlockSyntax, CompilationUnitSyntax)(destination)
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
                options As VisualBasicCodeGenerationContextInfo,
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
                options As VisualBasicCodeGenerationContextInfo,
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
                        Return AddParametersToMethod(Of TDeclarationNode)(methodStatement, methodBlock, parameters, options, cancellationToken)
                End Select
            Else
                Dim propertyBlock = TryCast(destinationMember, PropertyBlockSyntax)
                If propertyBlock IsNot Nothing Then
                    Return AddParametersToProperty(Of TDeclarationNode)(propertyBlock, parameters, options, cancellationToken)
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

        Private Overloads Shared Function AddParametersToMethod(Of TDeclarationNode As SyntaxNode)(
                methodStatement As MethodBaseSyntax,
                methodBlock As MethodBlockBaseSyntax,
                parameters As IEnumerable(Of IParameterSymbol),
                options As VisualBasicCodeGenerationContextInfo,
                cancellationToken As CancellationToken) As TDeclarationNode
            Dim finalStatement = AddParameterToMethodBase(methodStatement, parameters, options, cancellationToken)

            Dim result As StatementSyntax
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
                options As VisualBasicCodeGenerationContextInfo,
                cancellationToken As CancellationToken) As TDeclarationNode
            Dim propertyStatement = propertyBlock.PropertyStatement
            Dim newPropertyStatement = AddParameterToMethodBase(propertyStatement, parameters, options, cancellationToken)
            Dim newPropertyBlock As SyntaxNode = propertyBlock.WithPropertyStatement(newPropertyStatement)
            Return DirectCast(newPropertyBlock, TDeclarationNode)
        End Function

        Private Overloads Shared Function AddParameterToMethodBase(Of TMethodBase As MethodBaseSyntax)(
                methodBase As TMethodBase,
                parameters As IEnumerable(Of IParameterSymbol),
                options As VisualBasicCodeGenerationContextInfo,
                cancellationToken As CancellationToken) As TMethodBase

            Dim parameterList = methodBase.ParameterList

            Dim parameterCount = If(parameterList IsNot Nothing, parameterList.Parameters.Count, 0)
            Dim seenOptional = parameterCount > 0 AndAlso parameterList.Parameters(parameterCount - 1).Default IsNot Nothing

            Dim editor = New SyntaxEditor(methodBase, VisualBasicSyntaxGenerator.Instance)
            For Each parameter In parameters
                Dim parameterSyntax = ParameterGenerator.GenerateParameter(parameter, seenOptional, options)

                AddParameterEditor.AddParameter(
                    VisualBasicSyntaxFacts.Instance,
                    editor,
                    methodBase,
                    parameterCount,
                    parameterSyntax,
                    cancellationToken)

                seenOptional = seenOptional OrElse parameterSyntax.Default IsNot Nothing
                parameterCount += 1
            Next

            Return DirectCast(editor.GetChangedRoot(), TMethodBase)
        End Function

        Public Overrides Function AddAttributes(Of TDeclarationNode As SyntaxNode)(
                    destination As TDeclarationNode,
                    attributes As IEnumerable(Of AttributeData),
                    target As SyntaxToken?,
                    options As VisualBasicCodeGenerationContextInfo,
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

        Public Overrides Function RemoveAttribute(Of TDeclarationNode As SyntaxNode)(destination As TDeclarationNode, attributeToRemove As AttributeData, options As VisualBasicCodeGenerationContextInfo, cancellationToken As CancellationToken) As TDeclarationNode
            If attributeToRemove.ApplicationSyntaxReference Is Nothing Then
                Throw New ArgumentException(NameOf(attributeToRemove))
            End If

            Dim attributeSyntaxToRemove = attributeToRemove.ApplicationSyntaxReference.GetSyntax(cancellationToken)
            Return RemoveAttribute(destination, attributeSyntaxToRemove, options, cancellationToken)
        End Function

        Public Overrides Function RemoveAttribute(Of TDeclarationNode As SyntaxNode)(destination As TDeclarationNode, attributeToRemove As SyntaxNode, options As VisualBasicCodeGenerationContextInfo, cancellationToken As CancellationToken) As TDeclarationNode
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
                Dim newAttributeLists = RemoveAttributeFromAttributeLists(member.GetAttributes(), attributeToRemove, attributeRemoved, positionOfRemovedNode, triviaOfRemovedNode)
                VerifyAttributeRemoved(attributeRemoved)
                Dim newMember = member.WithAttributeLists(newAttributeLists)
                Return Cast(Of TDeclarationNode)(AppendTriviaAtPosition(newMember, positionOfRemovedNode - destination.FullSpan.Start, triviaOfRemovedNode))
            End If

            ' Handle global attributes
            Dim compilationUnit = TryCast(destination, CompilationUnitSyntax)
            If compilationUnit IsNot Nothing Then
                Dim attributeStatements = compilationUnit.Attributes
                Dim newAttributeStatements = RemoveAttributeFromAttributeStatements(attributeStatements, attributeToRemove, attributeRemoved, positionOfRemovedNode, triviaOfRemovedNode)
                VerifyAttributeRemoved(attributeRemoved)
                Dim newCompilationUnit = compilationUnit.WithAttributes(newAttributeStatements)
                Return Cast(Of TDeclarationNode)(AppendTriviaAtPosition(newCompilationUnit, positionOfRemovedNode - destination.FullSpan.Start, triviaOfRemovedNode))
            End If

            ' Handle parameters
            Dim parameter = TryCast(destination, ParameterSyntax)
            If parameter IsNot Nothing Then
                Dim newAttributeLists = RemoveAttributeFromAttributeLists(parameter.AttributeLists, attributeToRemove, attributeRemoved, positionOfRemovedNode, triviaOfRemovedNode)
                VerifyAttributeRemoved(attributeRemoved)
                Dim newParameter = parameter.WithAttributeLists(newAttributeLists)
                Return Cast(Of TDeclarationNode)(AppendTriviaAtPosition(newParameter, positionOfRemovedNode - destination.FullSpan.Start, triviaOfRemovedNode))
            End If

            Return destination
        End Function

        Private Shared Function RemoveAttributeFromAttributeLists(attributeLists As SyntaxList(Of AttributeListSyntax), attributeToRemove As SyntaxNode,
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

        Private Shared Function RemoveAttributeFromAttributeStatements(attributeStatements As SyntaxList(Of AttributesStatementSyntax), attributeToRemove As SyntaxNode,
                                                                       <Out> ByRef attributeRemoved As Boolean, <Out> ByRef positionOfRemovedNode As Integer, <Out> ByRef triviaOfRemovedNode As SyntaxTriviaList) As SyntaxList(Of AttributesStatementSyntax)
            For Each attributeStatement In attributeStatements
                Dim attributeLists = attributeStatement.AttributeLists
                Dim newAttributeLists = RemoveAttributeFromAttributeLists(attributeLists, attributeToRemove, attributeRemoved, positionOfRemovedNode, triviaOfRemovedNode)
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
                options As VisualBasicCodeGenerationContextInfo,
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

        Private Shared Function AddStatementsWorker(Of TDeclarationNode As SyntaxNode)(
                destinationMember As TDeclarationNode,
                statements As IEnumerable(Of SyntaxNode),
                options As VisualBasicCodeGenerationContextInfo,
                cancellationToken As CancellationToken) As TDeclarationNode
            Dim location = options.Context.BestLocation
            CheckLocation(destinationMember, location)

            Dim token = location.FindToken(cancellationToken)
            Dim oldBlock = token.Parent.GetContainingMultiLineExecutableBlocks().First
            Dim oldBlockStatements = oldBlock.GetExecutableBlockStatements()
            Dim oldBlockStatementsSet = oldBlockStatements.ToSet()

            Dim oldStatement = token.Parent.GetAncestorsOrThis(Of StatementSyntax)().First(AddressOf oldBlockStatementsSet.Contains)
            Dim oldStatementIndex = oldBlockStatements.IndexOf(oldStatement)

            Dim statementArray = statements.OfType(Of StatementSyntax).ToArray()
            Dim newBlock As SyntaxNode
            If options.Context.BeforeThisLocation IsNot Nothing Then
                Dim strippedTrivia As ImmutableArray(Of SyntaxTrivia) = Nothing
                Dim newStatement = VisualBasicFileBannerFacts.Instance.GetNodeWithoutLeadingBannerAndPreprocessorDirectives(
                    oldStatement, strippedTrivia)

                statementArray(0) = statementArray(0).WithLeadingTrivia(strippedTrivia)

                newBlock = oldBlock.ReplaceNode(oldStatement, newStatement)
                newBlock = newBlock.ReplaceStatements(newBlock.GetExecutableBlockStatements().InsertRange(oldStatementIndex, statementArray))
            Else
                newBlock = oldBlock.ReplaceStatements(oldBlockStatements.InsertRange(oldStatementIndex + 1, statementArray))
            End If

            Return destinationMember.ReplaceNode(oldBlock, newBlock)
        End Function

        ' TODO Change to Not return null (https://github.com/dotnet/roslyn/issues/58243)
        Public Overrides Function CreateMethodDeclaration(method As IMethodSymbol,
                                                          destination As CodeGenerationDestination,
                                                          options As VisualBasicCodeGenerationContextInfo,
                                                          cancellationToken As CancellationToken) As SyntaxNode
            ' Synthesized methods for properties/events are not things we actually generate 
            ' declarations for.
            If method.AssociatedSymbol IsNot Nothing Then
                Return Nothing
            End If

            If method.IsConstructor() Then
                Return ConstructorGenerator.GenerateConstructorDeclaration(method, destination, options)
            ElseIf method.IsUserDefinedOperator() Then
                Return OperatorGenerator.GenerateOperatorDeclaration(method, options)
            ElseIf method.IsConversion() Then
                Return ConversionGenerator.GenerateConversionDeclaration(method, options)
            Else
                Return MethodGenerator.GenerateMethodDeclaration(method, destination, options)
            End If
        End Function

        Public Overrides Function CreateEventDeclaration([event] As IEventSymbol,
                                                         destination As CodeGenerationDestination,
                                                         options As VisualBasicCodeGenerationContextInfo,
                                                         cancellationToken As CancellationToken) As SyntaxNode
            Return EventGenerator.GenerateEventDeclaration([event], destination, options)
        End Function

        Public Overrides Function CreateFieldDeclaration(field As IFieldSymbol,
                                                         destination As CodeGenerationDestination,
                                                         options As VisualBasicCodeGenerationContextInfo,
                                                         cancellationToken As CancellationToken) As SyntaxNode
            If destination = CodeGenerationDestination.EnumType Then
                Return EnumMemberGenerator.GenerateEnumMemberDeclaration(field, Nothing, options)
            Else
                Return FieldGenerator.GenerateFieldDeclaration(field, destination, options)
            End If
        End Function

        Public Overrides Function CreatePropertyDeclaration([property] As IPropertySymbol,
                                                            destination As CodeGenerationDestination,
                                                            options As VisualBasicCodeGenerationContextInfo,
                                                            cancellationToken As CancellationToken) As SyntaxNode
            Return PropertyGenerator.GeneratePropertyDeclaration([property], destination, options)
        End Function

        Public Overrides Function CreateNamedTypeDeclaration(namedType As INamedTypeSymbol,
                                                             destination As CodeGenerationDestination,
                                                             options As VisualBasicCodeGenerationContextInfo,
                                                             cancellationToken As CancellationToken) As SyntaxNode
            Return NamedTypeGenerator.GenerateNamedTypeDeclaration(Me, namedType, options, cancellationToken)
        End Function

        Public Overrides Function CreateNamespaceDeclaration([namespace] As INamespaceSymbol,
                                                             destination As CodeGenerationDestination,
                                                             options As VisualBasicCodeGenerationContextInfo,
                                                             cancellationToken As CancellationToken) As SyntaxNode
            Return NamespaceGenerator.GenerateNamespaceDeclaration(Me, [namespace], options, cancellationToken)
        End Function
    End Class
End Namespace
