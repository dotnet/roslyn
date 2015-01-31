// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface INavigationBarPresenter
    {
        void Disconnect();

        void PresentItems(
            IList<NavigationBarProjectItem> projects,
            NavigationBarProjectItem selectedProject,
            IList<NavigationBarItem> typesWithMembers,
            NavigationBarItem selectedType,
            NavigationBarItem selectedMember);

        ITextView TryGetCurrentView();

        event EventHandler<EventArgs> ViewFocused;
        event EventHandler<CaretPositionChangedEventArgs> CaretMoved;

        event EventHandler DropDownFocused;
        event EventHandler<NavigationBarItemSelectedEventArgs> ItemSelected;
    }
}
