// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.InteractiveWindow 
{
    public interface IInteractiveWindow2 : IInteractiveWindow 
    {
        /// <summary>
        /// Adds <paramref name="input"/> to the history as if it has been executed.
        /// Method doesn't execute <paramref name="input"/> and doesn't affect current user input.
        /// </summary>
        /// <exception cref="InvalidOperationException">The interactive window has not been initialized or is resettings.</exception>
        void AddToHistory(string input);
    }
}