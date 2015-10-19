// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    [DataContract]
    internal enum ReplSpanKind
    {
        /// <summary>
        /// Primary, secondary, or standard input prompt.
        /// </summary>
        [EnumMember]
        Prompt = 0,

        /// <summary>
        /// The span represents output from the program (standard output).
        /// </summary>
        [EnumMember]
        Output = 1,

        /// <summary>
        /// The span represents code inputted after a prompt or secondary prompt.
        /// </summary>
        [EnumMember]
        Input = 2,

        /// <summary>
        /// The span represents the input for a standard input (non code input).
        /// </summary>
        [EnumMember]
        StandardInput = 3,

        /// <summary>
        /// Line break inserted at end of output.
        /// </summary>
        [EnumMember]
        LineBreak = 4,
    }
}