// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal static class ReferenceFinders
{
    // Rename does not need to include base/this constructor initializer calls (explicit or implicit).
    public static readonly ImmutableArray<IReferenceFinder> DefaultRenameReferenceFinders = [
            AliasSymbolReferenceFinder.Instance,
            ConstructorSymbolReferenceFinder.Instance,
            CrefTypeParameterSymbolReferenceFinder.Instance,
            PropertySymbolReferenceFinder.Instance,
            new DestructorSymbolReferenceFinder(),
            DynamicTypeSymbolReferenceFinder.Instance,
            new EventSymbolReferenceFinder(),
            new ExplicitConversionSymbolReferenceFinder(),
            new ExplicitInterfaceMethodReferenceFinder(),
            new FieldSymbolReferenceFinder(),
            new LabelSymbolReferenceFinder(),
            new LocalSymbolReferenceFinder(),
            MethodTypeParameterSymbolReferenceFinder.Instance,
            new NamedTypeSymbolReferenceFinder(),
            new NamespaceSymbolReferenceFinder(),
            new OperatorSymbolReferenceFinder(),
            new OrdinaryMethodReferenceFinder(),
            new ParameterSymbolReferenceFinder(),
            new PreprocessingSymbolReferenceFinder(),
            new PropertyAccessorSymbolReferenceFinder(),
            new RangeVariableSymbolReferenceFinder(),
            TypeParameterSymbolReferenceFinder.Instance,
        ];

    /// <summary>
    /// The list of common reference finders.
    /// </summary>
    internal static readonly ImmutableArray<IReferenceFinder> DefaultReferenceFinders = [
        .. DefaultRenameReferenceFinders,
        ExplicitConstructorInitializerSymbolReferenceFinder.Instance,
        ImplicitConstructorInitializerSymbolReferenceFinder.Instance];
}
