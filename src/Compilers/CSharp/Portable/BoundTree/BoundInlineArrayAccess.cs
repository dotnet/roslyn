// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundInlineArrayAccess
    {
        private partial void Validate()
        {
#if DEBUG
            Debug.Assert(!IsValue || GetItemOrSliceHelper == WellKnownMember.System_ReadOnlySpan_T__get_Item);
#pragma warning disable format
            Debug.Assert(Argument.Type is
                             { SpecialType: SpecialType.System_Int32 } or
                             NamedTypeSymbol
                                 {
                                     ContainingSymbol: NamespaceSymbol
                                                       {
                                                           Name: "System",
                                                           ContainingSymbol: NamespaceSymbol { IsGlobalNamespace: true }
                                                       },
                                     Name: "Index" or "Range",
                                     IsGenericType: false
                                 }
                             );
#pragma warning restore format

            if (Argument.Type.Name == "Range")
            {
                Debug.Assert(GetItemOrSliceHelper is
                                WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int or
                                WellKnownMember.System_Span_T__Slice_Int_Int);

                if (GetItemOrSliceHelper is WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int)
                {
                    Debug.Assert(Type.Name == "ReadOnlySpan");
                }
                else
                {
                    Debug.Assert(Type.Name == "Span");
                }

#pragma warning disable format
                Debug.Assert(Type is
                                 NamedTypeSymbol
                                     {
                                         ContainingSymbol: NamespaceSymbol
                                                           {
                                                               Name: "System",
                                                               ContainingSymbol: NamespaceSymbol { IsGlobalNamespace: true }
                                                           },
                                         Arity: 1
                                     }
                                 );
#pragma warning restore format

                Debug.Assert(((NamedTypeSymbol)Type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Equals(Expression.Type?.TryGetInlineArrayElementField()?.TypeWithAnnotations ?? default, TypeCompareKind.ConsiderEverything));
            }
            else
            {
                Debug.Assert(GetItemOrSliceHelper is
                                WellKnownMember.System_ReadOnlySpan_T__get_Item or
                                WellKnownMember.System_Span_T__get_Item);

                Debug.Assert(Type.Equals(Expression.Type?.TryGetInlineArrayElementField()?.Type, TypeCompareKind.ConsiderEverything));
            }
#endif
        }
    }
}

