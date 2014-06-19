Imports System.Collections.Generic
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic

    Partial Friend NotInheritable Class AnonymousTypeManager

        Friend NotInheritable Class AnonymousTypeConstructedTypeSymbol
            Inherits AnonymousTypeOrDelegateConstructedTypeSymbol

            Public Sub New(substitution As TypeSubstitution, typeDescriptor As AnonymousTypeDescriptor)
                MyBase.New(substitution, typeDescriptor)
            End Sub

            Protected Overrides Function CompleteInternalSubstituteTypeParameters(newSubstitution As TypeSubstitution) As TypeSymbol
                Return New AnonymousTypeConstructedTypeSymbol(newSubstitution, TypeDescriptor)
            End Function

            Protected Overrides Function CreateSubstitutedPropertySymbol(memberProperty As PropertySymbol,
                                                                         getMethod As SubstitutedMethodSymbol,
                                                                         setMethod As SubstitutedMethodSymbol) As SubstitutedPropertySymbol
                Return New AnonymousTypeSpecializedPropertySymbol(Me, memberProperty, getMethod, setMethod,
                                                                  Me.TypeDescriptor.Fields(DirectCast(memberProperty, AnonymousTypePropertySymbol).PropertyIndex))
            End Function

            Protected Overrides Function SubstituteTypeParametersForMemberMethod(memberMethod As MethodSymbol) As SubstitutedMethodSymbol
                If memberMethod.MethodKind = MethodKind.PropertyGet OrElse memberMethod.MethodKind = MethodKind.PropertySet Then
                    Debug.Assert(memberMethod.Arity = 0)
                    Return New AnonymousTypeSpecializedPropertyAccessorSymbol(Me, memberMethod)

                ElseIf memberMethod.Parameters.Count = 1 AndAlso memberMethod.Parameters(0).Type Is Me.OriginalDefinition Then
                    Dim iEquatable_Equals = TryCast(memberMethod, AnonymousType_IEquatable_EqualsMethodSymbol)
                    Debug.Assert(iEquatable_Equals IsNot Nothing)

                    If iEquatable_Equals IsNot Nothing Then
                        Return New AnonymousTypeSpecialized_IEquatable_EqualsMethodSymbol(Me, iEquatable_Equals)
                    End If
                End If

                Return MyBase.SubstituteTypeParametersForMemberMethod(memberMethod)
            End Function

            Friend Overrides Function MakeAcyclicInterfaces(diagnostics As DiagnosticBag) As ReadOnlyArray(Of NamedTypeSymbol)
                Dim instanceInterfaces = OriginalNamedTypeDefinition.Interfaces

                If instanceInterfaces.Count = 0 Then
                    Return ReadOnlyArray(Of NamedTypeSymbol).Empty

                ElseIf instanceInterfaces.Count = 1 AndAlso instanceInterfaces(0).TypeArguments.Count = 1 AndAlso
                    instanceInterfaces(0).TypeArguments(0) Is Me.OriginalDefinition Then
                    ' Must be IEquitable(Of T).
                    Return ReadOnlyArray.Singleton(Of NamedTypeSymbol)(instanceInterfaces(0).OriginalNamedTypeDefinition.Construct(Me))
                End If

                Throw Contract.Unreachable
            End Function

            Public Overrides ReadOnly Property DeclaringSyntaxNodes As ReadOnlyArray(Of SyntaxNode)
                Get
                    Return GetDeclaringSyntaxNodeHelper(Of AnonymousObjectCreationExpressionSyntax)(ReadOnlyArray(Of Location).CreateFrom(Me.Locations))
                End Get
            End Property

            Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
                Get
                    Return False
                End Get
            End Property
        End Class

    End Class

End Namespace