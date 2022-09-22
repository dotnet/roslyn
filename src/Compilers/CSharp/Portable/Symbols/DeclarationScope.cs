// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    // https://github.com/dotnet/roslyn/issues/61647: Internally, scope is represented with this enum,
    // but the public API uses a pair of IsRefScoped and IsValueScoped bools (see ILocalSymbol,
    // IParameterSymbol, and ScopedRefAttribute). We should have a common representation.
    // And we should use common terms for the attribute and enum names.
    internal enum DeclarationScope : byte
    {
        Unscoped = 0,
        RefScoped = 1,
        ValueScoped = 2,
    }
}
