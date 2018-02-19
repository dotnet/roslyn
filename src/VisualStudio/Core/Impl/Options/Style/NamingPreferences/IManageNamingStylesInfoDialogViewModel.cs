// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
{
    internal interface IManageNamingStylesInfoDialogViewModel
    {
        ObservableCollection<INamingStylesInfoDialogViewModel> Items { get; }
        string DialogTitle { get; }
        void AddItem();
        void RemoveItem(INamingStylesInfoDialogViewModel item);
        void EditItem(INamingStylesInfoDialogViewModel item);
    }
}
