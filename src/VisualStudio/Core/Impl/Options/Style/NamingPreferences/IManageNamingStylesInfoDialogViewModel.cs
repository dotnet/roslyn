// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences;

internal interface IManageNamingStylesInfoDialogViewModel
{
    ObservableCollection<INamingStylesInfoDialogViewModel> Items { get; }
    string DialogTitle { get; }
    void AddItem();
    void RemoveItem(INamingStylesInfoDialogViewModel item);
    void EditItem(INamingStylesInfoDialogViewModel item);
}
