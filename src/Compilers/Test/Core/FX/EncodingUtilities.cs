// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
