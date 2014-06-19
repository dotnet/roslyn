Imports System.Collections.Generic
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic

    Partial Friend NotInheritable Class AnonymousTypeManager

        Friend NotInheritable Class AnonymousDelegateConstructedTypeSymbol
            Inherits AnonymousTypeOrDelegateConstructedTypeSymbol

            Public Sub New(substitution As TypeSubstitution, descriptor As AnonymousTypeDescriptor)
                MyBase.New(substitution, descriptor)
            End Sub

            Protected Overrides Function CompleteInternalSubstituteTypeParameters(newSubstitution As TypeSubstitution) As TypeSymbol
                Return New AnonymousDelegateConstructedTypeSymbol(newSubstitution, TypeDescriptor)
            End Function

            Protected Overrides Function SubstituteTypeParametersForMemberMethod(memberMethod As MethodSymbol) As SubstitutedMethodSymbol
                Dim haveParametersToRename As Boolean = False

                For Each param As AnonymousDelegateParameterSymbol In memberMethod.Parameters
                    If param.CorrespondingInvokeParameter <> -1 Then
                        haveParametersToRename = True
                        Exit For
                    End If
                Next

                If Not haveParametersToRename Then
                    Return MyBase.SubstituteTypeParametersForMemberMethod(memberMethod)
                End If

                Return New AnonymousDelegateSpecializedMethodSymbol(Me, DirectCast(memberMethod, AnonymousDelegateMethodSymbol))
                
            End Function

        End Class

    End Class

End Namespace