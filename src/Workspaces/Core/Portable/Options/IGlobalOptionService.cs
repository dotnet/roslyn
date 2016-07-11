// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Provides services for reading and writing options.
    /// This will provide support for options at the global level (i.e. shared among
    /// all workspaces/services).
    /// </summary>
    internal interface IGlobalOptionService
    {
        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        T GetOption<T>(Option<T> option);

        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        T GetOption<T>(PerLanguageOption<T> option, string languageName);

        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        object GetOption(OptionKey optionKey);

        /// <summary>
        /// Applies a set of options.
        /// </summary>
        void SetOptions(OptionSet optionSet);

        /// <summary>
        /// Returns the set of all registered options.
        /// </summary>
        IEnumerable<IOption> GetRegisteredOptions();

        event EventHandler<OptionChangedEventArgs> OptionChanged;
    }
}