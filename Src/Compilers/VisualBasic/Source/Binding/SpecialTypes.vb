Imports System.Collections.Generic
Imports Roslyn.Compilers.Internal
Namespace Roslyn.Compilers.VisualBasic

    Friend Enum SpecialType
        None = -1
        System_Object = CorLibTypes.TypeId.System_Object
        System_Enum = CorLibTypes.TypeId.System_Enum
        System_MulticastDelegate = CorLibTypes.TypeId.System_MulticastDelegate
        System_Delegate = CorLibTypes.TypeId.System_Delegate
        System_ValueType = CorLibTypes.TypeId.System_ValueType
        System_Void = CorLibTypes.TypeId.System_Void
        System_Boolean = CorLibTypes.TypeId.System_Boolean
        System_Char = CorLibTypes.TypeId.System_Char
        System_SByte = CorLibTypes.TypeId.System_SByte
        System_Byte = CorLibTypes.TypeId.System_Byte
        System_Int16 = CorLibTypes.TypeId.System_Int16
        System_UInt16 = CorLibTypes.TypeId.System_UInt16
        System_Int32 = CorLibTypes.TypeId.System_Int32
        System_UInt32 = CorLibTypes.TypeId.System_UInt32
        System_Int64 = CorLibTypes.TypeId.System_Int64
        System_UInt64 = CorLibTypes.TypeId.System_UInt64
        System_Single = CorLibTypes.TypeId.System_Single
        System_Double = CorLibTypes.TypeId.System_Double
        System_String = CorLibTypes.TypeId.System_String
        System_IntPtr = CorLibTypes.TypeId.System_IntPtr
        System_UIntPtr = CorLibTypes.TypeId.System_UIntPtr
        System_Decimal = CorLibTypes.TypeId.System_Decimal
        System_Type = CorLibTypes.TypeId.System_Type
        System_Array = CorLibTypes.TypeId.System_Array
        Collections_IEnumerable = CorLibTypes.TypeId.System_Collections_IEnumerable
        Generic_IEnumerable_T = CorLibTypes.TypeId.System_Collections_Generic_IEnumerable_T
        Generic_IList_T = CorLibTypes.TypeId.System_Collections_Generic_IList_T
        Generic_ICollection_T = CorLibTypes.TypeId.System_Collections_Generic_ICollection_T
        System_Nullable_T = CorLibTypes.TypeId.System_Nullable_T
        System_DateTime = CorLibTypes.TypeId.System_DateTime
        CompilerServices_IsVolatile = CorLibTypes.TypeId.System_Runtime_CompilerServices_IsVolatile
        Dynamic
        Nullable_Boolean
        Nullable_Byte
        Nullable_SByte
        Nullable_Char
        Nullable_Int16
        Nullable_UInt16
        Nullable_Int32
        Nullable_UInt32
        Nullable_Int64
        Nullable_UInt64
        Nullable_Single
        Nullable_Double
        Nullable_Decimal
    End Enum

    Public NotInheritable Class SpecialTypes
        Private ReadOnly compilation As Compilation
        Private ReadOnly dictionary As Dictionary(Of TypeSymbol, SpecialType)
        Public Property System_Object As NamedTypeSymbol
        Public Property CompilerServices_IsVolatile As NamedTypeSymbol
        Public Property System_DateTime As NamedTypeSymbol
        Public Property System_Void As NamedTypeSymbol
        Public Property System_IntPtr As NamedTypeSymbol
        Public Property System_UIntPtr As NamedTypeSymbol
        Public Property System_Type As NamedTypeSymbol
        Public Property System_Array As NamedTypeSymbol
        Public Property System_ValueType As NamedTypeSymbol
        Public Property System_Enum As NamedTypeSymbol
        Public Property System_Delegate As NamedTypeSymbol
        Public Property System_MulticastDelegate As NamedTypeSymbol
        Public Property System_String As NamedTypeSymbol
        Public Property System_Nullable_T As NamedTypeSymbol
        Public Property Generic_IList_T As NamedTypeSymbol
        Public Property Collections_IEnumerable As NamedTypeSymbol
        Public Property Generic_IEnumerable_T As NamedTypeSymbol
        Public Property Generic_ICollection_T As NamedTypeSymbol
        Public Property System_Boolean As NamedTypeSymbol
        Public Property System_Byte As NamedTypeSymbol
        Public Property System_SByte As NamedTypeSymbol
        Public Property System_Char As NamedTypeSymbol
        Public Property System_Int16 As NamedTypeSymbol
        Public Property System_UInt16 As NamedTypeSymbol
        Public Property System_Int32 As NamedTypeSymbol
        Public Property System_UInt32 As NamedTypeSymbol
        Public Property System_Int64 As NamedTypeSymbol
        Public Property System_UInt64 As NamedTypeSymbol
        Public Property System_Single As NamedTypeSymbol
        Public Property System_Double As NamedTypeSymbol
        Public Property System_Decimal As NamedTypeSymbol
        Public Property Nullable_Boolean As NamedTypeSymbol
        Public Property Nullable_Byte As NamedTypeSymbol
        Public Property Nullable_SByte As NamedTypeSymbol
        Public Property Nullable_Char As NamedTypeSymbol
        Public Property Nullable_Int16 As NamedTypeSymbol
        Public Property Nullable_UInt16 As NamedTypeSymbol
        Public Property Nullable_Int32 As NamedTypeSymbol
        Public Property Nullable_UInt32 As NamedTypeSymbol
        Public Property Nullable_Int64 As NamedTypeSymbol
        Public Property Nullable_UInt64 As NamedTypeSymbol
        Public Property Nullable_Single As NamedTypeSymbol
        Public Property Nullable_Double As NamedTypeSymbol
        Public Property Nullable_Decimal As NamedTypeSymbol

        Friend Sub New(ByVal compilation As Compilation)
            Me.compilation = compilation
            Me.System_Object = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Object)
            Me.CompilerServices_IsVolatile = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Runtime_CompilerServices_IsVolatile)
            Me.System_DateTime = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_DateTime)
            Me.System_Type = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Type)
            Me.System_Void = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Void)
            Me.System_IntPtr = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_IntPtr)
            Me.System_UIntPtr = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_UIntPtr)
            Me.System_Array = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Array)
            Me.System_ValueType = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_ValueType)
            Me.System_Enum = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Enum)
            Me.System_String = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_String)
            Me.System_Delegate = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Delegate)
            Me.System_MulticastDelegate = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_MulticastDelegate)
            Me.Collections_IEnumerable = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Collections_IEnumerable)
            Me.Generic_IList_T = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Collections_Generic_IList_T)
            Me.Generic_IEnumerable_T = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Collections_Generic_IEnumerable_T)
            Me.Generic_ICollection_T = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Collections_Generic_ICollection_T)
            Me.System_Boolean = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Boolean)
            Me.System_Byte = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Byte)
            Me.System_SByte = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_SByte)
            Me.System_Char = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Char)
            Me.System_Int16 = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Int16)
            Me.System_UInt16 = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_UInt16)
            Me.System_Int32 = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Int32)
            Me.System_UInt32 = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_UInt32)
            Me.System_Int64 = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Int64)
            Me.System_UInt64 = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_UInt64)
            Me.System_Single = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Single)
            Me.System_Double = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Double)
            Me.System_Decimal = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Decimal)
            Me.System_Nullable_T = Me.compilation.GetCorLibType(CorLibTypes.TypeId.System_Nullable_T)
            Me.Nullable_Boolean = System_Nullable_T.Construct(System_Boolean)
            Me.Nullable_Byte = System_Nullable_T.Construct(System_Byte)
            Me.Nullable_SByte = System_Nullable_T.Construct(System_SByte)
            Me.Nullable_Char = System_Nullable_T.Construct(System_Char)
            Me.Nullable_Int16 = System_Nullable_T.Construct(System_Int16)
            Me.Nullable_UInt16 = System_Nullable_T.Construct(System_UInt16)
            Me.Nullable_Int32 = System_Nullable_T.Construct(System_Int32)
            Me.Nullable_UInt32 = System_Nullable_T.Construct(System_UInt32)
            Me.Nullable_Int64 = System_Nullable_T.Construct(System_Int64)
            Me.Nullable_UInt64 = System_Nullable_T.Construct(System_UInt64)
            Me.Nullable_Single = System_Nullable_T.Construct(System_Single)
            Me.Nullable_Double = System_Nullable_T.Construct(System_Double)
            Me.Nullable_Decimal = System_Nullable_T.Construct(System_Decimal)
            dictionary = New Dictionary(Of TypeSymbol, SpecialType)(IdentityComparer.Instance) From {{System_Object, SpecialType.System_Object}, {System_Enum, SpecialType.System_Enum}, {System_MulticastDelegate, SpecialType.System_MulticastDelegate}, {System_Delegate, SpecialType.System_Delegate}, {System_ValueType, SpecialType.System_ValueType}, {System_Void, SpecialType.System_Void}, {System_Boolean, SpecialType.System_Boolean}, {System_Char, SpecialType.System_Char}, {System_SByte, SpecialType.System_SByte}, {System_Byte, SpecialType.System_Byte}, {System_Int16, SpecialType.System_Int16}, {System_UInt16, SpecialType.System_UInt16}, {System_Int32, SpecialType.System_Int32}, {System_UInt32, SpecialType.System_UInt32}, {System_Int64, SpecialType.System_Int64}, {System_UInt64, SpecialType.System_UInt64}, {System_Single, SpecialType.System_Single}, {System_Double, SpecialType.System_Double}, {System_String, SpecialType.System_String}, {System_IntPtr, SpecialType.System_IntPtr}, {System_UIntPtr, SpecialType.System_UIntPtr}, {System_Decimal, SpecialType.System_Decimal}, {System_Type, SpecialType.System_Type}, {System_Array, SpecialType.System_Array}, {Collections_IEnumerable, SpecialType.Collections_IEnumerable}, {Generic_IEnumerable_T, SpecialType.Generic_IEnumerable_T}, {Generic_IList_T, SpecialType.Generic_IList_T}, {Generic_ICollection_T, SpecialType.Generic_ICollection_T}, {System_Nullable_T, SpecialType.System_Nullable_T}, {System_DateTime, SpecialType.System_DateTime}, {CompilerServices_IsVolatile, SpecialType.CompilerServices_IsVolatile}, {Nullable_Boolean, SpecialType.Nullable_Boolean}, {Nullable_Byte, SpecialType.Nullable_Byte}, {Nullable_SByte, SpecialType.Nullable_SByte}, {Nullable_Char, SpecialType.Nullable_Char}, {Nullable_Int16, SpecialType.Nullable_Int16}, {Nullable_UInt16, SpecialType.Nullable_UInt16}, {Nullable_Int32, SpecialType.Nullable_Int32}, {Nullable_UInt32, SpecialType.Nullable_UInt32}, {Nullable_Int64, SpecialType.Nullable_Int64}, {Nullable_UInt64, SpecialType.Nullable_UInt64}, {Nullable_Single, SpecialType.Nullable_Single}, {Nullable_Double, SpecialType.Nullable_Double}, {Nullable_Decimal, SpecialType.Nullable_Decimal}}
        End Sub

        Friend Function GetSpecialType(ByVal type As TypeSymbol) As SpecialType
            Dim specialType As SpecialType
            Return If(dictionary.TryGetValue(type, specialType), specialType, specialType.None)
        End Function

        Friend Function MakeArray(ByVal elementType As TypeSymbol, ByVal rank As Integer, ByVal customModifiers As IList(Of CustomModifier)) As ArrayTypeSymbol
            Return New ArrayTypeSymbol(elementType, customModifiers, rank)
        End Function
    End Class

End Namespace

