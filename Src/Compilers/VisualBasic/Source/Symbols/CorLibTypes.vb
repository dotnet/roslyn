
'-----------------------------------------------------------------------------
' Copyright (c) Microsoft Corporation. All rights reserved.
'-----------------------------------------------------------------------------

Imports System.Collections.Generic
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.Internal.Contract

Namespace Roslyn.Compilers.VisualBasic

    Friend NotInheritable Class CorLibTypes

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Type ids should be in sync with names in s_EmittedNames array.
        ''' </summary>
        ''' <remarks></remarks>
        Public Enum TypeId
            System_Object
            System_Enum
            System_MulticastDelegate
            System_Delegate
            System_ValueType
            System_Void
            System_Boolean
            System_Char
            System_SByte
            System_Byte
            System_Int16
            System_UInt16
            System_Int32
            System_UInt32
            System_Int64
            System_UInt64
            System_Single
            System_Double
            System_String
            System_IntPtr
            System_UIntPtr
            System_Type
            System_Decimal
            System_DateTime
            System_Nullable
            System_IListArity1
            System_Array

            Count
        End Enum



        ''' <summary>
        ''' Array of names for types from Cor Libraray.
        ''' The names should correspond to ids from TypeId enum so
        ''' that we could use ids to index into the array
        ''' </summary>
        ''' <remarks></remarks>
        Private Shared ReadOnly s_EmittedNames() As String =
           {"System.Object",
            "System.Enum",
            "System.MulticastDelegate",
            "System.Delegate",
            "System.ValueType",
            "System.Void",
            "System.Boolean",
            "System.Char",
            "System.SByte",
            "System.Byte",
            "System.Int16",
            "System.UInt16",
            "System.Int32",
            "System.UInt32",
            "System.Int64",
            "System.UInt64",
            "System.Single",
            "System.Double",
            "System.String",
            "System.IntPtr",
            "System.UIntPtr",
            "System.Type",
            "System.Decimal",
            "System.DateTime",
            "System.Nullable`1",
            "System.IList`1",
            "System.Array"
            }


        Public Shared Function GetEmittedName(ByVal id As TypeId) As String
            Return s_EmittedNames(id)
        End Function
    End Class

End Namespace
