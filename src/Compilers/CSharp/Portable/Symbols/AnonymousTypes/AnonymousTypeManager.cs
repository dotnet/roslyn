// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Manages anonymous types created in owning compilation. All requests for 
    /// anonymous type symbols go via the instance of this class.
    /// </summary>
    internal sealed partial class AnonymousTypeManager : CommonAnonymousTypeManager
    {
        internal AnonymousTypeManager(CSharpCompilation compilation)
        {
            Debug.Assert(compilation != null);
            this.Compilation = compilation;
        }

        /// <summary> 
        /// Current compilation
        /// </summary>
        public CSharpCompilation Compilation { get; }

        /// <summary>
        /// Given anonymous type descriptor provided constructs an anonymous type symbol.
        /// </summary>
        public NamedTypeSymbol ConstructAnonymousTypeSymbol(AnonymousTypeDescriptor typeDescr)
        {
            return new AnonymousTypePublicSymbol(this, typeDescr);
        }

        /// <summary>
        /// Get a symbol of constructed anonymous type property by property index
        /// </summary>
        internal static PropertySymbol GetAnonymousTypeProperty(NamedTypeSymbol type, int index)
        {
            Debug.Assert((object)type != null);
            Debug.Assert(type.IsAnonymousType);

            var anonymous = (AnonymousTypePublicSymbol)type;
            return anonymous.Properties[index];
        }

        /// <summary>
        /// Retrieves anonymous type properties types
        /// </summary>
        internal static ImmutableArray<TypeSymbolWithAnnotations> GetAnonymousTypePropertyTypes(NamedTypeSymbol type)
        {
            Debug.Assert(type.IsAnonymousType);
            var anonymous = (AnonymousTypePublicSymbol)type;
            var fields = anonymous.TypeDescriptor.Fields;
            var types = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance(fields.Length);
            for (int i = 0; i < fields.Length; i++)
            {
                types.Add(fields[i].Type);
            }

            return types.ToImmutableAndFree();
        }

        /// <summary>
        /// Given an anonymous type and new field types construct a new anonymous type symbol; 
        /// a new type symbol will reuse type descriptor from the constructed type with new type arguments.
        /// </summary>
        public static NamedTypeSymbol ConstructAnonymousTypeSymbol(NamedTypeSymbol type, ImmutableArray<TypeSymbolWithAnnotations> newFieldTypes)
        {
            Debug.Assert(!newFieldTypes.IsDefault);
            Debug.Assert(type.IsAnonymousType);

            var anonymous = (AnonymousTypePublicSymbol)type;
            return anonymous.Manager.ConstructAnonymousTypeSymbol(anonymous.TypeDescriptor.WithNewFieldsTypes(newFieldTypes));
        }

        /// <summary>
        /// Logical equality on anonymous types that ignores custom modifiers and/or the object/dynamic distinction.
        /// Differs from IsSameType for arrays, pointers, and generic instantiations.
        /// </summary>
        internal static bool IsSameType(TypeSymbol type1, TypeSymbol type2, bool ignoreCustomModifiersAndArraySizesAndLowerBounds, bool ignoreDynamic)
        {
            Debug.Assert(type1.IsAnonymousType);
            Debug.Assert(type2.IsAnonymousType);

            if (ignoreCustomModifiersAndArraySizesAndLowerBounds || ignoreDynamic)
            {
                AnonymousTypeDescriptor left = ((AnonymousTypePublicSymbol)type1).TypeDescriptor;
                AnonymousTypeDescriptor right = ((AnonymousTypePublicSymbol)type2).TypeDescriptor;

                if (left.Key != right.Key)
                {
                    return false;
                }

                int count = left.Fields.Length;
                Debug.Assert(right.Fields.Length == count);
                for (int i = 0; i < count; i++)
                {
                    if (!left.Fields[i].Type.TypeSymbol.Equals(right.Fields[i].Type.TypeSymbol, ignoreCustomModifiersAndArraySizesAndLowerBounds, ignoreDynamic))
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return type1 == type2;
            }
        }
    }
}
