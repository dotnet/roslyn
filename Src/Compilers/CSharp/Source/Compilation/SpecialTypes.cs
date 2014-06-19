using System.Collections.Generic;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp
{
    internal enum SpecialType
    {
        None = -1,
        System_Object = CorLibTypes.TypeId.System_Object,
        System_Enum = CorLibTypes.TypeId.System_Enum,
        System_MulticastDelegate = CorLibTypes.TypeId.System_MulticastDelegate,
        System_Delegate = CorLibTypes.TypeId.System_Delegate,
        System_ValueType = CorLibTypes.TypeId.System_ValueType,
        System_Void = CorLibTypes.TypeId.System_Void,
        System_Boolean = CorLibTypes.TypeId.System_Boolean,
        System_Char = CorLibTypes.TypeId.System_Char,
        System_SByte = CorLibTypes.TypeId.System_SByte,
        System_Byte = CorLibTypes.TypeId.System_Byte,
        System_Int16 = CorLibTypes.TypeId.System_Int16,
        System_UInt16 = CorLibTypes.TypeId.System_UInt16,
        System_Int32 = CorLibTypes.TypeId.System_Int32,
        System_UInt32 = CorLibTypes.TypeId.System_UInt32,
        System_Int64 = CorLibTypes.TypeId.System_Int64,
        System_UInt64 = CorLibTypes.TypeId.System_UInt64,
        System_Single = CorLibTypes.TypeId.System_Single,
        System_Double = CorLibTypes.TypeId.System_Double,
        System_String = CorLibTypes.TypeId.System_String,
        System_IntPtr = CorLibTypes.TypeId.System_IntPtr,
        System_UIntPtr = CorLibTypes.TypeId.System_UIntPtr,
        System_Decimal = CorLibTypes.TypeId.System_Decimal,
        System_Type = CorLibTypes.TypeId.System_Type,
        System_Array = CorLibTypes.TypeId.System_Array,
        Collections_IEnumerable = CorLibTypes.TypeId.System_Collections_IEnumerable,
        Generic_IEnumerable_T = CorLibTypes.TypeId.System_Collections_Generic_IEnumerable_T,
        Generic_IList_T = CorLibTypes.TypeId.System_Collections_Generic_IList_T,
        Generic_ICollection_T = CorLibTypes.TypeId.System_Collections_Generic_ICollection_T,
        System_Nullable_T = CorLibTypes.TypeId.System_Nullable_T,
        System_DateTime = CorLibTypes.TypeId.System_DateTime,
        CompilerServices_IsVolatile = CorLibTypes.TypeId.System_Runtime_CompilerServices_IsVolatile,

        Dynamic,
        Nullable_Boolean,
        Nullable_Byte,
        Nullable_SByte,
        Nullable_Char,
        Nullable_Int16,
        Nullable_UInt16,
        Nullable_Int32,
        Nullable_UInt32,
        Nullable_Int64,
        Nullable_UInt64,
        Nullable_Single,
        Nullable_Double,
        Nullable_Decimal,
    }

    public sealed class SpecialTypes
    {
        private readonly Compilation compilation;
        private readonly Dictionary<TypeSymbol, SpecialType> dictionary;

        public NamedTypeSymbol System_Object { get; private set; }
        public NamedTypeSymbol CompilerServices_IsVolatile { get; private set; }
        public NamedTypeSymbol System_DateTime { get; private set; }
        public NamedTypeSymbol System_Void { get; private set; }
        public NamedTypeSymbol System_IntPtr { get; private set; }
        public NamedTypeSymbol System_UIntPtr { get; private set; }
        public NamedTypeSymbol System_Type { get; private set; }
        public NamedTypeSymbol System_Array { get; private set; }
        public NamedTypeSymbol System_ValueType { get; private set; }
        public NamedTypeSymbol System_Enum { get; private set; }
        public NamedTypeSymbol System_Delegate { get; private set; }
        public NamedTypeSymbol System_MulticastDelegate { get; private set; }
        public NamedTypeSymbol System_String { get; private set; }
        public NamedTypeSymbol System_Nullable_T { get; private set; }
        public NamedTypeSymbol Generic_IList_T { get; private set; }
        public NamedTypeSymbol Collections_IEnumerable { get; private set; }
        public NamedTypeSymbol Generic_IEnumerable_T { get; private set; }
        public NamedTypeSymbol Generic_ICollection_T { get; private set; }

        public NamedTypeSymbol System_Boolean { get; private set; }
        public NamedTypeSymbol System_Byte { get; private set; }
        public NamedTypeSymbol System_SByte { get; private set; }
        public NamedTypeSymbol System_Char { get; private set; }
        public NamedTypeSymbol System_Int16 { get; private set; }
        public NamedTypeSymbol System_UInt16 { get; private set; }
        public NamedTypeSymbol System_Int32 { get; private set; }
        public NamedTypeSymbol System_UInt32 { get; private set; }
        public NamedTypeSymbol System_Int64 { get; private set; }
        public NamedTypeSymbol System_UInt64 { get; private set; }
        public NamedTypeSymbol System_Single { get; private set; }
        public NamedTypeSymbol System_Double { get; private set; }
        public NamedTypeSymbol System_Decimal { get; private set; }

        public NamedTypeSymbol Nullable_Boolean { get; private set; }
        public NamedTypeSymbol Nullable_Byte { get; private set; }
        public NamedTypeSymbol Nullable_SByte { get; private set; }
        public NamedTypeSymbol Nullable_Char { get; private set; }
        public NamedTypeSymbol Nullable_Int16 { get; private set; }
        public NamedTypeSymbol Nullable_UInt16 { get; private set; }
        public NamedTypeSymbol Nullable_Int32 { get; private set; }
        public NamedTypeSymbol Nullable_UInt32 { get; private set; }
        public NamedTypeSymbol Nullable_Int64 { get; private set; }
        public NamedTypeSymbol Nullable_UInt64 { get; private set; }
        public NamedTypeSymbol Nullable_Single { get; private set; }
        public NamedTypeSymbol Nullable_Double { get; private set; }
        public NamedTypeSymbol Nullable_Decimal { get; private set; }

        public TypeSymbol Dynamic { get; private set; }

        internal SpecialTypes(Compilation compilation)
        {
            this.compilation = compilation;

            this.System_Object = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Object);
            this.CompilerServices_IsVolatile = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Runtime_CompilerServices_IsVolatile);
            this.System_DateTime = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_DateTime);
            this.System_Type = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Type);
            this.System_Void = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Void);
            this.System_IntPtr = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_IntPtr);
            this.System_UIntPtr = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_UIntPtr);
            this.Dynamic = new DynamicTypeSymbol();
            this.System_Array = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Array);
            this.System_ValueType = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_ValueType);
            this.System_Enum = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Enum);
            this.System_String = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_String);
            this.System_Delegate = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Delegate);
            this.System_MulticastDelegate = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_MulticastDelegate);
            this.Collections_IEnumerable = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Collections_IEnumerable);
            this.Generic_IList_T = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Collections_Generic_IList_T);
            this.Generic_IEnumerable_T = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Collections_Generic_IEnumerable_T);
            this.Generic_ICollection_T = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Collections_Generic_ICollection_T);

            this.System_Boolean = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Boolean);
            this.System_Byte = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Byte);
            this.System_SByte = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_SByte);
            this.System_Char = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Char);
            this.System_Int16 = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Int16);
            this.System_UInt16 = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_UInt16);
            this.System_Int32 = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Int32);
            this.System_UInt32 = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_UInt32);
            this.System_Int64 = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Int64);
            this.System_UInt64 = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_UInt64);
            this.System_Single = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Single);
            this.System_Double = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Double);
            this.System_Decimal = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Decimal);

            this.System_Nullable_T = this.compilation.GetCorLibType(CorLibTypes.TypeId.System_Nullable_T);

            this.Nullable_Boolean = System_Nullable_T.Construct(System_Boolean);
            this.Nullable_Byte = System_Nullable_T.Construct(System_Byte);
            this.Nullable_SByte = System_Nullable_T.Construct(System_SByte);
            this.Nullable_Char = System_Nullable_T.Construct(System_Char);
            this.Nullable_Int16 = System_Nullable_T.Construct(System_Int16);
            this.Nullable_UInt16 = System_Nullable_T.Construct(System_UInt16);
            this.Nullable_Int32 = System_Nullable_T.Construct(System_Int32);
            this.Nullable_UInt32 = System_Nullable_T.Construct(System_UInt32);
            this.Nullable_Int64 = System_Nullable_T.Construct(System_Int64);
            this.Nullable_UInt64 = System_Nullable_T.Construct(System_UInt64);
            this.Nullable_Single = System_Nullable_T.Construct(System_Single);
            this.Nullable_Double = System_Nullable_T.Construct(System_Double);
            this.Nullable_Decimal = System_Nullable_T.Construct(System_Decimal);

            dictionary = new Dictionary<TypeSymbol, SpecialType>(IdentityComparer.Instance)
            {
                {System_Object, SpecialType.System_Object },
                {System_Enum, SpecialType.System_Enum },
                {System_MulticastDelegate, SpecialType.System_MulticastDelegate },
                {System_Delegate, SpecialType.System_Delegate },
                {System_ValueType, SpecialType.System_ValueType },
                {System_Void, SpecialType.System_Void },
                {System_Boolean, SpecialType.System_Boolean },
                {System_Char, SpecialType.System_Char },
                {System_SByte, SpecialType.System_SByte },
                {System_Byte, SpecialType.System_Byte },
                {System_Int16, SpecialType.System_Int16 },
                {System_UInt16, SpecialType.System_UInt16 },
                {System_Int32, SpecialType.System_Int32 },
                {System_UInt32, SpecialType.System_UInt32 },
                {System_Int64, SpecialType.System_Int64 },
                {System_UInt64, SpecialType.System_UInt64 },
                {System_Single, SpecialType.System_Single },
                {System_Double, SpecialType.System_Double },
                {System_String, SpecialType.System_String },
                {System_IntPtr, SpecialType.System_IntPtr },
                {System_UIntPtr, SpecialType.System_UIntPtr },
                {System_Decimal, SpecialType.System_Decimal },
                {System_Type, SpecialType.System_Type },
                {System_Array, SpecialType.System_Array },
                {Collections_IEnumerable, SpecialType.Collections_IEnumerable },
                {Generic_IEnumerable_T, SpecialType.Generic_IEnumerable_T },
                {Generic_IList_T, SpecialType.Generic_IList_T },
                {Generic_ICollection_T, SpecialType.Generic_ICollection_T },
                {System_Nullable_T, SpecialType.System_Nullable_T },
                {System_DateTime, SpecialType.System_DateTime },
                {CompilerServices_IsVolatile, SpecialType.CompilerServices_IsVolatile },
                {Dynamic, SpecialType.Dynamic },
                {Nullable_Boolean, SpecialType.Nullable_Boolean },
                {Nullable_Byte, SpecialType.Nullable_Byte },
                {Nullable_SByte, SpecialType.Nullable_SByte },
                {Nullable_Char, SpecialType.Nullable_Char },
                {Nullable_Int16, SpecialType.Nullable_Int16 },
                {Nullable_UInt16, SpecialType.Nullable_UInt16 },
                {Nullable_Int32, SpecialType.Nullable_Int32 },
                {Nullable_UInt32, SpecialType.Nullable_UInt32 },
                {Nullable_Int64, SpecialType.Nullable_Int64 },
                {Nullable_UInt64, SpecialType.Nullable_UInt64 },
                {Nullable_Single, SpecialType.Nullable_Single },
                {Nullable_Double, SpecialType.Nullable_Double },
                {Nullable_Decimal, SpecialType.Nullable_Decimal },
            };
        }

        internal SpecialType GetSpecialType(TypeSymbol type)
        {
            SpecialType specialType;
            return dictionary.TryGetValue(type, out specialType) ? specialType : SpecialType.None;
        }

        internal ArrayTypeSymbol MakeArray(TypeSymbol elementType, int rank, IList<CustomModifier> customModifiers)
        {
            return new ArrayTypeSymbol(elementType, customModifiers, rank, compilation);
        }

        internal bool IsNullableType(TypeSymbol type)
        {
            return type.OriginalDefinition == System_Nullable_T;
        }
    }
}
