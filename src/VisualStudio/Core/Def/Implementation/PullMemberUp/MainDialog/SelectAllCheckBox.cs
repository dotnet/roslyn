// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Windows.Controls;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.MainDialog
{
    /// <summary>
    /// A check box used to imitate the behavior of select all check box of VS.
    /// It reverses the order of three state check box to null -> true -> false
    /// </summary>
    internal class SelectAllCheckBox : CheckBox
    {
        protected override void OnToggle()
        {
            // The order of the three state checkbox is reversed since if the checkbox is in indeterminate (null),
            // click the checkbox will then trigger select all, then in checked state(true).
            if (IsChecked == false)
            {
                IsChecked = IsThreeState ? null : (bool?)true;
            }
            else
            {
                IsChecked = !IsChecked.HasValue;
            }
        }
    }
}
