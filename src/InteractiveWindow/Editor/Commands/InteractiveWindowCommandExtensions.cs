// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    public static class InteractiveWindowCommandExtensions
    {
        /// <summary>
        /// Gets the IInteractiveWindowCommands instance for the current interactive window if one is defined.
        /// 
        /// Returns null if the interactive commands have not been created for this window.
        /// </summary>
        public static IInteractiveWindowCommands GetInteractiveCommands(this IInteractiveWindow window)
        {
            IInteractiveWindowCommands commands;
            if (window.Properties.TryGetProperty(typeof(IInteractiveWindowCommands), out commands))
            {
                return commands;
            }

            return null;
        }
    }
}
