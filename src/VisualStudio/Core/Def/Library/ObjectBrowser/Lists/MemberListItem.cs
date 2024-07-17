// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists;

internal class MemberListItem : SymbolListItem<ISymbol>
{
    private readonly MemberKind _kind;
    private readonly bool _isInherited;

    internal MemberListItem(ProjectId projectId, ISymbol symbol, string displayText, string fullNameText, string searchText, bool isHidden, bool isInherited)
        : base(projectId, symbol, displayText, fullNameText, searchText, isHidden)
    {
        _isInherited = isInherited;

        switch (symbol.Kind)
        {
            case SymbolKind.Event:
                _kind = MemberKind.Event;
                break;

            case SymbolKind.Field:
                var fieldSymbol = (IFieldSymbol)symbol;
                if (fieldSymbol.ContainingType.TypeKind == TypeKind.Enum)
                {
                    _kind = MemberKind.EnumMember;
                }
                else
                {
                    _kind = fieldSymbol.IsConst
                        ? MemberKind.Constant
                        : MemberKind.Field;
                }

                break;

            case SymbolKind.Method:
                var methodSymbol = (IMethodSymbol)symbol;
                _kind = methodSymbol.MethodKind is MethodKind.Conversion or
                                  MethodKind.UserDefinedOperator
                    ? MemberKind.Operator
                    : MemberKind.Method;

                break;

            case SymbolKind.Property:
                _kind = MemberKind.Property;
                break;

            default:
                Debug.Fail("Unsupported symbol for member: " + symbol.Kind.ToString());
                _kind = MemberKind.None;
                break;
        }
    }

    public bool IsInherited
    {
        get { return _isInherited; }
    }

    public MemberKind Kind
    {
        get { return _kind; }
    }
}
