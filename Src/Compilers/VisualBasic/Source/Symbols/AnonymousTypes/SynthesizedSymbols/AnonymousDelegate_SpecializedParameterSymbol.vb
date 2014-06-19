Imports System.Threading

Namespace Roslyn.Compilers.VisualBasic
    Partial Friend NotInheritable Class AnonymousTypeManager

        Private NotInheritable Class AnonymousDelegateSpecializedParameterSymbol
            Inherits SubstitutedParameterSymbol.SubstitutedMethodParameterSymbol

            Private ReadOnly m_newName As String
            Private ReadOnly m_newLocations As ReadOnlyArray(Of Location)

            Public Sub New(type As AnonymousDelegateConstructedTypeSymbol,
                           method As AnonymousDelegateSpecializedMethodSymbol,
                           originalDefinition As AnonymousDelegateParameterSymbol)
                MyBase.New(method, originalDefinition)
                Debug.Assert(method.ContainingSymbol Is type)
                Debug.Assert(originalDefinition.CorrespondingInvokeParameter <> -1)

                Dim parameterDescriptor As AnonymousTypeField = type.TypeDescriptor.Parameters(originalDefinition.CorrespondingInvokeParameter)
                Me.m_newName = parameterDescriptor.Name
                Me.m_newLocations = ReadOnlyArray(Of Location).CreateFrom(parameterDescriptor.Location)
            End Sub

            Public Overrides ReadOnly Property Name As String
                Get
                    Return m_newName
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ReadOnlyArray(Of Location)
                Get
                    Return m_newLocations
                End Get
            End Property
        End Class

    End Class
End Namespace