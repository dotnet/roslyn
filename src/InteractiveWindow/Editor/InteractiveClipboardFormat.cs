// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// Implements deserialization for clipboard objects created by Interactive Window Copy operations.
    /// </summary>
    public static class InteractiveClipboardFormat
    {
        /// <summary>
        /// Unique identifier for the clipboard format.
        /// </summary>
        public const string Tag = "89344A36-9821-495A-8255-99A63969F87D";

        /// <summary>
        /// Deserializes clipboard object.
        /// </summary>
        /// <param name="value">Object retrieved fromt the clipboard </param>
        /// <exception cref="InvalidDataException">The value is not of the expected format.</exception>
        public static string Deserialize(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var text = value as string;
            if (text == null)
            {
                throw new InvalidDataException();
            }

            var blocks = BufferBlock.Deserialize(text);

            var result = new StringBuilder();
            foreach (var block in blocks)
            {
                switch (block.Kind)
                {
                    // the actual linebreak was converted to regular Input when copied
                    // This LineBreak block was created by coping box selection and is used as line separator when pasted
                    case ReplSpanKind.LineBreak:
                        result.Append(block.Content);
                        break;

                    case ReplSpanKind.Input:
                    case ReplSpanKind.Output:
                    case ReplSpanKind.StandardInput:
                        result.Append(block.Content);
                        break;
                }
            }

            return result.ToString();
        }
    }
}
