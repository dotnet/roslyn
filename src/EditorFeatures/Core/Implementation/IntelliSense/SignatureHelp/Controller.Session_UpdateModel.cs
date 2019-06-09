// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
{
    internal partial class Controller
    {
        internal partial class Session
        {
            internal struct SignatureHelpSelection
            {
                private readonly SignatureHelpItem _selectedItem;
                private readonly bool _userSelected;
                private readonly int? _selectedParameter;

                public SignatureHelpSelection(SignatureHelpItem selectedItem, bool userSelected, int? selectedParameter) : this()
                {
                    _selectedItem = selectedItem;
                    _userSelected = userSelected;
                    _selectedParameter = selectedParameter;
                }

                public int? SelectedParameter => _selectedParameter;
                public SignatureHelpItem SelectedItem => _selectedItem;
                public bool UserSelected => _userSelected;
            }

            internal static class DefaultSignatureHelpSelector
            {
                public static SignatureHelpSelection GetSelection(
                    IList<SignatureHelpItem> items,
                    SignatureHelpItem selectedItem,
                    bool userSelected,
                    int argumentIndex,
                    int argumentCount,
                    string argumentName,
                    bool isCaseSensitive)
                {
                    SelectBestItem(ref selectedItem, ref userSelected, items, argumentIndex, argumentCount, argumentName, isCaseSensitive);
                    var selectedParameter = GetSelectedParameter(selectedItem, argumentIndex, argumentName, isCaseSensitive);
                    return new SignatureHelpSelection(selectedItem, userSelected, selectedParameter);
                }

                private static int GetSelectedParameter(SignatureHelpItem bestItem, int parameterIndex, string parameterName, bool isCaseSensitive)
                {
                    if (!string.IsNullOrEmpty(parameterName))
                    {
                        var comparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
                        var index = bestItem.Parameters.IndexOf(p => comparer.Equals(p.Name, parameterName));
                        if (index >= 0)
                        {
                            return index;
                        }
                    }

                    return parameterIndex;
                }

                private static void SelectBestItem(ref SignatureHelpItem currentItem, ref bool userSelected,
                    IList<SignatureHelpItem> filteredItems, int selectedParameter, int argumentCount, string name, bool isCaseSensitive)
                {
                    // If the current item is still applicable, then just keep it.
                    if (filteredItems.Contains(currentItem) &&
                        IsApplicable(currentItem, argumentCount, name, isCaseSensitive))
                    {
                        // If the current item was user-selected, we keep it as such.
                        return;
                    }

                    // If the current item is no longer applicable, we'll be choosing a new one,
                    // which was definitely not previously user-selected.
                    userSelected = false;

                    // Try to find the first applicable item.  If there is none, then that means the
                    // selected parameter was outside the bounds of all methods.  i.e. all methods only
                    // went up to 3 parameters, and selected parameter is 3 or higher.  In that case,
                    // just pick the very last item as it is closest in parameter count.
                    var result = filteredItems.FirstOrDefault(i => IsApplicable(i, argumentCount, name, isCaseSensitive));
                    if (result != null)
                    {
                        currentItem = result;
                        return;
                    }

                    // if we couldn't find a best item, and they provided a name, then try again without
                    // a name.
                    if (name != null)
                    {
                        SelectBestItem(ref currentItem, ref userSelected, filteredItems, selectedParameter, argumentCount, null, isCaseSensitive);
                        return;
                    }

                    // If we don't have an item that can take that number of parameters, then just pick
                    // the last item.  Or stick with the current item if the last item isn't any better.
                    var lastItem = filteredItems.Last();
                    if (currentItem.IsVariadic || currentItem.Parameters.Length == lastItem.Parameters.Length)
                    {
                        return;
                    }

                    currentItem = lastItem;
                }

                private static bool IsApplicable(SignatureHelpItem item, int argumentCount, string name, bool isCaseSensitive)
                {
                    // If they provided a name, then the item is only valid if it has a parameter that
                    // matches that name.
                    if (name != null)
                    {
                        var comparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
                        return item.Parameters.Any(p => comparer.Equals(p.Name, name));
                    }

                    // An item is applicable if it has at least as many parameters as the selected
                    // parameter index.  i.e. if it has 2 parameters and we're at index 0 or 1 then it's
                    // applicable.  However, if it has 2 parameters and we're at index 2, then it's not
                    // applicable.  
                    if (item.Parameters.Length >= argumentCount)
                    {
                        return true;
                    }

                    // However, if it is variadic then it is applicable as it can take any number of
                    // items.
                    if (item.IsVariadic)
                    {
                        return true;
                    }

                    // Also, we special case 0.  that's because if the user has "Goo(" and goo takes no
                    // arguments, then we'll see that it's arg count is 0.  We still want to consider
                    // any item applicable here though.
                    return argumentCount == 0;
                }
            }
        }
    }
}
