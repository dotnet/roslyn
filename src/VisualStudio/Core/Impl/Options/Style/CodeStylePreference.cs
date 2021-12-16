// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    /// <summary>
    /// Represents a single code style choice.
    /// Typically, a code style offers a list of choices to choose from.
    /// </summary>
    internal class CodeStylePreference
    {
        public CodeStylePreference(string name, bool isChecked)
        {
            Name = name;
            IsChecked = isChecked;
        }

        public string Name { get; set; }
        public bool IsChecked { get; set; }
    }
}
