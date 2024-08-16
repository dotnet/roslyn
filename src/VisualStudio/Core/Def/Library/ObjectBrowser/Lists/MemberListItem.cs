// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists;

internal class MemberListItem : SymbolListItem<ISymbol>
{
    internal MemberListItem(ProjectId projectId, ISymbol symbol, string displayText, string fullNameText, string searchText, bool isHidden, bool isInherited)
        : base(projectId, symbol, displayText, fullNameText, searchText, isHidden)
    {
        IsInherited = isInherited;

        switch (symbol.Kind)
        {
            case SymbolKind.Event:
                Kind = MemberKind.Event;
                break;

            case SymbolKind.Field:
                var fieldSymbol = (IFieldSymbol)symbol;
                if (fieldSymbol.ContainingType.TypeKind == TypeKind.Enum)
                {
                    Kind = MemberKind.EnumMember;
                }
                else
                {
                    Kind = fieldSymbol.IsConst
                        ? MemberKind.Constant
                        : MemberKind.Field;
                }

                break;

            case SymbolKind.Method:
                var methodSymbol = (IMethodSymbol)symbol;
                Kind = methodSymbol.MethodKind is MethodKind.Conversion or
                                  MethodKind.UserDefinedOperator
                    ? MemberKind.Operator
                    : MemberKind.Method;

                break;

            case SymbolKind.Property:
                Kind = MemberKind.Property;
                break;

            default:
                Debug.Fail("Unsupported symbol for member: " + symbol.Kind.ToString());
                Kind = MemberKind.None;
                break;
        }
    }

    public bool IsInherited { get; }

    public MemberKind Kind { get; }
}
