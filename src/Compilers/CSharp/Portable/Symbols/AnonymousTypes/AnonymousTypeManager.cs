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
        internal static ImmutableArray<TypeWithAnnotations> GetAnonymousTypePropertyTypesWithAnnotations(NamedTypeSymbol type)
        {
            Debug.Assert(type.IsAnonymousType);
            var anonymous = (AnonymousTypePublicSymbol)type;
            var fields = anonymous.TypeDescriptor.Fields;
            return fields.SelectAsArray(f => f.TypeWithAnnotations);
        }

        /// <summary>
        /// Given an anonymous type and new field types construct a new anonymous type symbol; 
        /// a new type symbol will reuse type descriptor from the constructed type with new type arguments.
        /// </summary>
        public static NamedTypeSymbol ConstructAnonymousTypeSymbol(NamedTypeSymbol type, ImmutableArray<TypeWithAnnotations> newFieldTypes)
        {
            Debug.Assert(!newFieldTypes.IsDefault);
            Debug.Assert(type.IsAnonymousType);

            var anonymous = (AnonymousTypePublicSymbol)type;
            return anonymous.Manager.ConstructAnonymousTypeSymbol(anonymous.TypeDescriptor.WithNewFieldsTypes(newFieldTypes));
        }
    }
}
