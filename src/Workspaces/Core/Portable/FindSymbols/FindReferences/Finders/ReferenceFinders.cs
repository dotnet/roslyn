// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal static class ReferenceFinders
    {
        public static readonly IReferenceFinder Constructor = ConstructorSymbolReferenceFinder.Instance;
        public static readonly IReferenceFinder ConstructorInitializer = new ConstructorInitializerSymbolReferenceFinder();
        public static readonly IReferenceFinder Destructor = new DestructorSymbolReferenceFinder();
        public static readonly IReferenceFinder ExplicitConversion = new ExplicitConversionSymbolReferenceFinder();
        public static readonly IReferenceFinder ExplicitInterfaceMethod = new ExplicitInterfaceMethodReferenceFinder();
        public static readonly IReferenceFinder Event = new EventSymbolReferenceFinder();
        public static readonly IReferenceFinder Field = new FieldSymbolReferenceFinder();
        public static readonly IReferenceFinder Label = new LabelSymbolReferenceFinder();
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
                ExplicitConversion,
                ExplicitInterfaceMethod,
                Field,
                Label,
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
