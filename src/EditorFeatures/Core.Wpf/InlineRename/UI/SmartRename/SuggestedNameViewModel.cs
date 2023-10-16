// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.InlineRename.UI.SmartRename
{
    internal class SuggestedNameViewModel(string name, SmartRenameViewModel smartRenameViewModel)
    {
        private readonly SmartRenameViewModel _smartRenameViewModel = smartRenameViewModel;

        private bool _isSelect = false;

        public bool IsSelected
        {
            get => _isSelect;
            set
            {
                if (value != _isSelect)
                {
                    _isSelect = value;
                    _smartRenameViewModel.CurrentSelectedName = Name;
                }
            }
        }

        public string Name { get; } = name;
    }
}
