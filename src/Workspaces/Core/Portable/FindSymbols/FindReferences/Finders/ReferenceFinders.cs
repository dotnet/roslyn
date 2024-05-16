// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal static class ReferenceFinders
{
    private static readonly IReferenceFinder Constructor = ConstructorSymbolReferenceFinder.Instance;
    private static readonly IReferenceFinder ConstructorInitializer = new ConstructorInitializerSymbolReferenceFinder();
    private static readonly IReferenceFinder Destructor = new DestructorSymbolReferenceFinder();
    private static readonly IReferenceFinder ExplicitConversion = new ExplicitConversionSymbolReferenceFinder();
    private static readonly IReferenceFinder ExplicitInterfaceMethod = new ExplicitInterfaceMethodReferenceFinder();
    private static readonly IReferenceFinder Event = new EventSymbolReferenceFinder();
    private static readonly IReferenceFinder Field = new FieldSymbolReferenceFinder();
    private static readonly IReferenceFinder Label = new LabelSymbolReferenceFinder();
    private static readonly IReferenceFinder Local = new LocalSymbolReferenceFinder();
    private static readonly IReferenceFinder MethodTypeParameter = new MethodTypeParameterSymbolReferenceFinder();
    private static readonly IReferenceFinder NamedType = new NamedTypeSymbolReferenceFinder();
    private static readonly IReferenceFinder Namespace = new NamespaceSymbolReferenceFinder();
    private static readonly IReferenceFinder Operator = new OperatorSymbolReferenceFinder();
    private static readonly IReferenceFinder OrdinaryMethod = new OrdinaryMethodReferenceFinder();
    private static readonly IReferenceFinder Parameter = new ParameterSymbolReferenceFinder();
    private static readonly IReferenceFinder Property = PropertySymbolReferenceFinder.Instance;
    private static readonly IReferenceFinder PropertyAccessor = new PropertyAccessorSymbolReferenceFinder();
    private static readonly IReferenceFinder RangeVariable = new RangeVariableSymbolReferenceFinder();
    private static readonly IReferenceFinder TypeParameter = new TypeParameterSymbolReferenceFinder();

    // Rename does not need to include base/this constructor initializer calls
    public static readonly ImmutableArray<IReferenceFinder> DefaultRenameReferenceFinders =
        [
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
            TypeParameter,
        ];

    /// <summary>
    /// The list of common reference finders.
    /// </summary>
    internal static readonly ImmutableArray<IReferenceFinder> DefaultReferenceFinders = [.. DefaultRenameReferenceFinders, ConstructorInitializer];
}
