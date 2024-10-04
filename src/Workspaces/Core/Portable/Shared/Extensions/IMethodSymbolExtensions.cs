// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class IMethodSymbolExtensions
{
    public static bool CompatibleSignatureToDelegate(this IMethodSymbol method, INamedTypeSymbol delegateType)
    {
        Contract.ThrowIfFalse(delegateType.TypeKind == TypeKind.Delegate);

        var invoke = delegateType.DelegateInvokeMethod;
        if (invoke == null)
        {
            // It's possible to get events with no invoke method from metadata.  We will assume
            // that no method can be an event handler for one.
            return false;
        }

        if (method.Parameters.Length != invoke.Parameters.Length)
        {
            return false;
        }

        if (method.ReturnsVoid != invoke.ReturnsVoid)
        {
            return false;
        }

        if (!method.ReturnType.InheritsFromOrEquals(invoke.ReturnType))
        {
            return false;
        }

        for (var i = 0; i < method.Parameters.Length; i++)
        {
            if (!invoke.Parameters[i].Type.InheritsFromOrEquals(method.Parameters[i].Type))
            {
                return false;
            }
        }

        return true;
    }

    public static bool? IsMoreSpecificThan(this IMethodSymbol method1, IMethodSymbol method2)
    {
        var p1 = method1.Parameters;
        var p2 = method2.Parameters;

        // If the methods don't have the same parameter count, then method1 can't be more or 
        // less specific than method2.
        if (p1.Length != p2.Length)
        {
            return null;
        }

        // If the methods' parameter types differ, or they have different names, then one can't
        // be more specific than the other.
        if (!SignatureComparer.Instance.HaveSameSignature(method1.Parameters, method2.Parameters) ||
            !method1.Parameters.Select(p => p.Name).SequenceEqual(method2.Parameters.Select(p => p.Name)))
        {
            return null;
        }

        // Ok.  We have two methods that look extremely similar to each other.  However, one might
        // be more specific if, for example, it was actually written with concrete types (like 'int') 
        // versus the other which may have been instantiated from a type parameter.   i.e.
        //
        // class C<T> { void Goo(T t); void Goo(int t); }
        //
        // THe latter Goo is more specific when comparing "C<int>.Goo(int t)" (method1) vs 
        // "C<int>.Goo(int t)" (method2).
        p1 = method1.OriginalDefinition.Parameters;
        p2 = method2.OriginalDefinition.Parameters;
        return p1.Select(p => p.Type).ToList().AreMoreSpecificThan(p2.Select(p => p.Type).ToList());
    }
}
