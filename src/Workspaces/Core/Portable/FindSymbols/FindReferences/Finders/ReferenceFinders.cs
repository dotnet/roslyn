// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal static class ReferenceFinders
{
    // Rename does not need to include base/this constructor initializer calls
    public static readonly ImmutableArray<IReferenceFinder> DefaultRenameReferenceFinders =
        [
            ConstructorSymbolReferenceFinder.Instance,
            PropertySymbolReferenceFinder.Instance,
            new DestructorSymbolReferenceFinder(),
            new EventSymbolReferenceFinder(),
            new ExplicitConversionSymbolReferenceFinder(),
            new ExplicitInterfaceMethodReferenceFinder(),
            new FieldSymbolReferenceFinder(),
            new LabelSymbolReferenceFinder(),
            new LocalSymbolReferenceFinder(),
            new MethodTypeParameterSymbolReferenceFinder(),
            new NamedTypeSymbolReferenceFinder(),
            new NamespaceSymbolReferenceFinder(),
            new OperatorSymbolReferenceFinder(),
            new OrdinaryMethodReferenceFinder(),
            new ParameterSymbolReferenceFinder(),
            new PreprocessingSymbolReferenceFinder(),
            new PropertyAccessorSymbolReferenceFinder(),
            new RangeVariableSymbolReferenceFinder(),
            new TypeParameterSymbolReferenceFinder(),
        ];

    /// <summary>
    /// The list of common reference finders.
    /// </summary>
    internal static readonly ImmutableArray<IReferenceFinder> DefaultReferenceFinders = [.. DefaultRenameReferenceFinders, new ConstructorInitializerSymbolReferenceFinder()];
}
