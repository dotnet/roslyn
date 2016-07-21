// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal static class ReferenceFinders
    {
        public static readonly IReferenceFinder Constructor = new ConstructorSymbolReferenceFinder();
        public static readonly IReferenceFinder ConstructorInitializer = new ConstructorInitializerSymbolReferenceFinder();
        public static readonly IReferenceFinder Destructor = new DestructorSymbolReferenceFinder();
        public static readonly IReferenceFinder ExplicitInterfaceMethod = new ExplicitInterfaceMethodReferenceFinder();
        public static readonly IReferenceFinder Event = new EventSymbolReferenceFinder();
        public static readonly IReferenceFinder Field = new FieldSymbolReferenceFinder();
        public static readonly IReferenceFinder Label = new LabelSymbolReferenceFinder();
        public static readonly IReferenceFinder LinkedFiles = new LinkedFileReferenceFinder();
        public static readonly IReferenceFinder Local = new LocalSymbolReferenceFinder();
        public static readonly IReferenceFinder MethodTypeParameter = new MethodTypeParameterSymbolReferenceFinder();
        public static readonly IReferenceFinder NamedType = new NamedTypeSymbolReferenceFinder();
        public static readonly IReferenceFinder Namespace = new NamespaceSymbolReferenceFinder();
        public static readonly IReferenceFinder Operator = new OperatorSymbolReferenceFinder();
        public static readonly IReferenceFinder OrdinaryMethod = new OrdinaryMethodReferenceFinder();
        public static readonly IReferenceFinder Parameter = new ParameterSymbolReferenceFinder();
        public static readonly IReferenceFinder Property = new PropertySymbolReferenceFinder();
        public static readonly IReferenceFinder PropertyAccessor = new PropertyAccessorSymbolReferenceFinder();
        public static readonly IReferenceFinder RangeVariable = new RangeVariableSymbolReferenceFinder();
        public static readonly IReferenceFinder TypeParameter = new TypeParameterSymbolReferenceFinder();

        /// <summary>
        /// The list of common reference finders.
        /// </summary>
        public static readonly ImmutableArray<IReferenceFinder> DefaultReferenceFinders;

        // Rename does not need to include base/this constructor initializer calls
        internal static readonly ImmutableArray<IReferenceFinder> DefaultRenameReferenceFinders;

        static ReferenceFinders()
        {
            DefaultRenameReferenceFinders = ImmutableArray.Create(
                Constructor,
                Destructor,
                Event,
                ExplicitInterfaceMethod,
                Field,
                Label,
                LinkedFiles,
                Local,
                MethodTypeParameter,
                NamedType,
                Namespace,
                Operator,
                OrdinaryMethod,
                Parameter,
                Property,
                PropertyAccessor,
                RangeVariable,
                TypeParameter);
            DefaultReferenceFinders = DefaultRenameReferenceFinders.Add(ConstructorInitializer);
        }
    }
}
