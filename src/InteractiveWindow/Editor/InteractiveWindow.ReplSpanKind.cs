// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal partial class InteractiveWindow
    {
        private enum ReplSpanKind
        {
            /// <summary>
            /// Primary, secondary, or standard input prompt.
            /// </summary>
            Prompt,

            /// <summary>
            /// Line break inserted at end of output.
            /// </summary>
            LineBreak,

            /// <summary>
            /// The span represents output from the program (standard output).
            /// </summary>
            Output,

            /// <summary>
            /// The span represents code inputted after a prompt or secondary prompt.
            /// </summary>
            Language,

            /// <summary>
            /// The span represents the input for a standard input (non code input).
            /// </summary>
            StandardInput,
        }
    }
}