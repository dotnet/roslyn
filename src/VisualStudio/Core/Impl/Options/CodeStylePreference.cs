// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
