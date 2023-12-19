// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Input;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal interface IRenameUserInput
{
    string Text { get; set; }

    bool IsFocused { get; }

    int TextSelectionStart { get; set; }

    int TextSelectionLength { get; set; }

    event RoutedEventHandler? TextSelectionChanged;

    event RoutedEventHandler? GotFocus;

    event KeyEventHandler? PreviewKeyDown;

    void Focus();

    void SelectText(int start, int length);

    void SelectAllText();
}
