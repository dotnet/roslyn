Imports System.Threading

Namespace Roslyn.Compilers.VisualBasic
    Partial Friend NotInheritable Class AnonymousTypeManager

        Private NotInheritable Class AnonymousDelegateSpecializedMethodSymbol
            Inherits SubstitutedMethodSymbol.SpecializedNonGenericMethod

            Public Sub New(container As AnonymousDelegateConstructedTypeSymbol, originalDefinition As AnonymousDelegateMethodSymbol)
                MyBase.New(container, originalDefinition)
            End Sub

            Protected Overrides Function SubstituteParameters() As ReadOnlyArray(Of ParameterSymbol)

                Dim container = DirectCast(ContainingSymbol, AnonymousDelegateConstructedTypeSymbol)
                Dim params As ReadOnlyArray(Of ParameterSymbol) = OriginalMethodDefinition.Parameters
                Debug.Assert(params.Count > 0)
                Dim substituted As ParameterSymbol() = New ParameterSymbol(params.Count - 1) {}

                For i = 0 To substituted.Length - 1
                    Dim param = DirectCast(params(i), AnonymousDelegateParameterSymbol)

                    If param.CorrespondingInvokeParameter = -1 Then
                        substituted(i) = SubstitutedParameterSymbol.CreateMethodParameter(Me, param)
                    Else
                        substituted(i) = New AnonymousDelegateSpecializedParameterSymbol(container, Me, param)
                    End If
                Next

                Return substituted.AsReadOnlyWrap()
            End Function
        End Class

    End Class
End Namespace