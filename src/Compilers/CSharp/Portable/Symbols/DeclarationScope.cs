// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    // PROTOTYPE: Internally, scope is represented with this enum, but the public API
    // uses a IsRefScoped and IsValueScoped bools (see ILocalSymbol, IParameterSymbol,
    // and LifetimeAnnotationAttribute). We should have a common representation.
    internal enum DeclarationScope : byte
    {
        Unscoped = 0,
        RefScoped = 1,
        ValueScoped = 2,
    }
}
