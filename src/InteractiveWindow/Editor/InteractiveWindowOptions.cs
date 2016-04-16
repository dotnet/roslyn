// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    public static class InteractiveWindowOptions
    {
        private static readonly EditorOptionKey<bool> s_smartUpDown = new EditorOptionKey<bool>(SmartUpDownOption.OptionName);

        /// <summary>
        /// Indicates that the window should be using smart up/down behavior.  When enabled pressing
        /// the up or down arrow key will navigate history if the caret is at the end of the current
        /// input.  When disabled the up/down arrow keys will always navigate the buffer.
        /// </summary>
        public static EditorOptionKey<bool> SmartUpDown
        {
            get
            {
                return s_smartUpDown;
            }
        }
    }
}
