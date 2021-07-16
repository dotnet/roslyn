// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class EventSymbolExtensions
    {
        internal static MethodSymbol GetOwnOrInheritedAccessor(this EventSymbol @event, bool isAdder)
        {
            return isAdder
                ? @event.GetOwnOrInheritedAddMethod()
                : @event.GetOwnOrInheritedRemoveMethod();
        }
    }
}
