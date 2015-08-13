// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;

namespace Roslyn.Test.Utilities
{
    public static class EncodingExtensions
    {
        public static byte[] GetBytesWithPreamble(this Encoding encoding, string text)
        {
            var preamble = encoding.GetPreamble();
            var content = encoding.GetBytes(text);

            byte[] bytes = new byte[preamble.Length + content.Length];
            preamble.CopyTo(bytes, 0);
            content.CopyTo(bytes, preamble.Length);

            return bytes;
        }
    }
}
