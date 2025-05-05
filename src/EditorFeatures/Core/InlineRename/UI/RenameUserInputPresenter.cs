// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal sealed class RenameUserInputPresenter : ContentPresenter
{
    internal IRenameUserInput? RenameUserInput => Content as IRenameUserInput;
}
