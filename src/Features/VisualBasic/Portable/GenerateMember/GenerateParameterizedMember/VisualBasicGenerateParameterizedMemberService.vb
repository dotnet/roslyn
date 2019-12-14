' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
Imports Microsoft.CodeAnalysis.Utilities
Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateMember.GenerateMethod
    Partial Friend MustInherit Class VisualBasicGenerateParameterizedMemberService(Of TService As AbstractGenerateParameterizedMemberService(Of TService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax))
        Inherits AbstractGenerateParameterizedMemberService(Of TService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax)

        Partial Friend Class InvocationExpressionInfo
            Inherits AbstractInvocationInfo

            Public ReadOnly InvocationExpression As InvocationExpressionSyntax

            Public Sub New(document As SemanticDocument, state As AbstractGenerateParameterizedMemberService(Of TService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax).State)
                MyBase.New(document, state)

                Me.InvocationExpression = state.InvocationExpressionOpt
            End Sub

            Protected Overrides Function DetermineParameterNames(cancellationToken As CancellationToken) As ImmutableArray(Of ParameterName)
                Dim typeParametersNames = Me.DetermineTypeParameters(cancellationToken).SelectAsArray(Function(t) t.Name)
                Return Me.Document.SemanticModel.GenerateParameterNames(
                    Me.InvocationExpression.ArgumentList, reservedNames:=typeParametersNames, cancellationToken:=cancellationToken)
            End Function

            Protected Overrides Function DetermineRefKind(cancellationToken As CancellationToken) As RefKind
                Return RefKind.None
            End Function

            Protected Overrides Function DetermineReturnTypeWorker(cancellationToken As CancellationToken) As ITypeSymbol
                Select Case Me.State.IdentifierToken.GetTypeCharacter()
                    Case TypeCharacter.Integer
                        Return Me.Document.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Int32)
                    Case TypeCharacter.Long
                        Return Me.Document.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Int64)
                    Case TypeCharacter.Decimal
                        Return Me.Document.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Decimal)
                    Case TypeCharacter.Single
                        Return Me.Document.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Single)
                    Case TypeCharacter.Double
                        Return Me.Document.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Double)
                    Case TypeCharacter.String
                        Return Me.Document.SemanticModel.Compilation.GetSpecialType(SpecialType.System_String)
                End Select

                Dim typeInference = Document.Project.LanguageServices.GetService(Of ITypeInferenceService)()
                Dim inferredType = typeInference.InferType(
                    Document.SemanticModel, Me.InvocationExpression, objectAsDefault:=True,
                    name:=Me.State.IdentifierToken.ValueText, cancellationToken:=cancellationToken)
                Return inferredType
            End Function

            Protected Overrides Function GetCapturedTypeParameters(cancellationToken As CancellationToken) As ImmutableArray(Of ITypeParameterSymbol)
                Dim result = New List(Of ITypeParameterSymbol)()
                If Not Me.InvocationExpression.ArgumentList Is Nothing Then
                    For Each argument In Me.InvocationExpression.ArgumentList.Arguments
                        Dim type = DetermineParameterType(argument, cancellationToken)
                        type.GetReferencedTypeParameters(result)
                    Next
                End If

                Return result.ToImmutableArray()
            End Function

            Protected Overrides Function GenerateTypeParameters(cancellationToken As CancellationToken) As ImmutableArray(Of ITypeParameterSymbol)
                ' Generate dummy type parameter names for a generic method.  If the user is inside a
                ' generic method, and calls a generic method with type arguments from the outer
                ' method, then use those same names for the generated type parameters.
                '
                ' TODO(cyrusn): If we do capture method type variables, then we should probably
                ' capture their constraints as well.
                Dim genericName = DirectCast(Me.State.SimpleNameOpt, GenericNameSyntax)
                If genericName.TypeArgumentList.Arguments.Count = 1 Then
                    Dim typeParameter = GetUniqueTypeParameter(
                        genericName.TypeArgumentList.Arguments.First(),
                        Function(s) Not State.TypeToGenerateIn.GetAllTypeParameters().Any(Function(t) t.Name = s),
                        cancellationToken)

                    Return ImmutableArray.Create(typeParameter)
                End If

                Dim usedIdentifiers = New HashSet(Of String) From {"T"}

                Dim list = ArrayBuilder(Of ITypeParameterSymbol).GetInstance()
                For Each typeArgument In genericName.TypeArgumentList.Arguments
                    Dim typeParameter = GetUniqueTypeParameter(typeArgument,
                        Function(s) Not usedIdentifiers.Contains(s) AndAlso Not State.TypeToGenerateIn.GetAllTypeParameters().Any(Function(t) t.Name = s),
                        cancellationToken)

                    usedIdentifiers.Add(typeParameter.Name)

                    list.Add(typeParameter)
                Next

                Return list.ToImmutableAndFree()
            End Function

            Private Function GetUniqueTypeParameter(type As TypeSyntax,
                                                    isUnique As Func(Of String, Boolean),
                                                    cancellationToken As CancellationToken) As ITypeParameterSymbol

                Dim methodTypeParameter = GetMethodTypeParameter(type, cancellationToken)
                Return If(methodTypeParameter IsNot Nothing,
                           methodTypeParameter,
                           CodeGenerationSymbolFactory.CreateTypeParameterSymbol(NameGenerator.GenerateUniqueName("T", isUnique)))
            End Function

            Private Function GetMethodTypeParameter(type As TypeSyntax, cancellationToken As CancellationToken) As ITypeParameterSymbol
                If TypeOf type Is IdentifierNameSyntax Then
                    Dim info = Me.Document.SemanticModel.GetTypeInfo(type, cancellationToken)
                    If TypeOf info.Type Is ITypeParameterSymbol AndAlso
                        DirectCast(info.Type, ITypeParameterSymbol).TypeParameterKind = TypeParameterKind.Method Then
                        Return DirectCast(info.Type, ITypeParameterSymbol)
                    End If
                End If

                Return Nothing
            End Function

            Protected Overrides Function DetermineParameterModifiers(cancellationToken As CancellationToken) As ImmutableArray(Of RefKind)
                Return If(Me.InvocationExpression.ArgumentList IsNot Nothing AndAlso Me.InvocationExpression.ArgumentList.GetArgumentCount() > 0,
                          Me.InvocationExpression.ArgumentList.Arguments.Select(Function(a) RefKind.None).ToImmutableArray(),
                          ImmutableArray(Of RefKind).Empty)
            End Function

            Protected Overrides Function DetermineParameterTypes(cancellationToken As CancellationToken) As ImmutableArray(Of ITypeSymbol)
                Return If(Me.InvocationExpression.ArgumentList IsNot Nothing AndAlso Me.InvocationExpression.ArgumentList.GetArgumentCount() > 0,
                          Me.InvocationExpression.ArgumentList.Arguments.Select(Function(a) DetermineParameterType(a, cancellationToken)).ToImmutableArray(),
                          ImmutableArray(Of ITypeSymbol).Empty)
            End Function

            Protected Overrides Function DetermineParameterOptionality(cancellationToken As CancellationToken) As ImmutableArray(Of Boolean)
                Return If(Me.InvocationExpression.ArgumentList IsNot Nothing AndAlso Me.InvocationExpression.ArgumentList.GetArgumentCount() > 0,
                          Me.InvocationExpression.ArgumentList.Arguments.Select(Function(a) DetermineParameterOptionality(a, cancellationToken)).ToImmutableArray(),
                          ImmutableArray(Of Boolean).Empty)
            End Function

            Private Overloads Function DetermineParameterOptionality(argument As ArgumentSyntax,
                                                    cancellationToken As CancellationToken) As Boolean
                Return TypeOf argument Is OmittedArgumentSyntax
            End Function

            Private Function DetermineParameterType(argument As ArgumentSyntax,
                                                    cancellationToken As CancellationToken) As ITypeSymbol
                Return argument.DetermineType(Me.Document.SemanticModel, cancellationToken)
            End Function

            Protected Overrides Function IsIdentifierName() As Boolean
                Return Me.State.SimpleNameOpt.Kind = SyntaxKind.IdentifierName
            End Function

            Protected Overrides Function IsImplicitReferenceConversion(compilation As Compilation, sourceType As ITypeSymbol, targetType As ITypeSymbol) As Boolean
                Dim conversion = compilation.ClassifyConversion(sourceType, targetType)
                Return conversion.IsWidening AndAlso conversion.IsReference
            End Function

            Protected Overrides Function DetermineTypeArguments(cancellationToken As CancellationToken) As ImmutableArray(Of ITypeSymbol)
                Dim Result = ArrayBuilder(Of ITypeSymbol).GetInstance()

                If TypeOf State.SimpleNameOpt Is GenericNameSyntax Then
                    For Each typeArgument In DirectCast(State.SimpleNameOpt, GenericNameSyntax).TypeArgumentList.Arguments
                        Dim Type = Me.Document.SemanticModel.GetTypeInfo(typeArgument, cancellationToken).Type
                        Result.Add(Type)
                    Next
                End If

                Return Result.ToImmutableAndFree()
            End Function
        End Class
    End Class
End Namespace
