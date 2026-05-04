// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Text;

namespace Microsoft.CodeAnalysis.CommandLine
{
    internal static class ConsoleUtil
    {
        private static readonly Encoding s_utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// This will update the <see cref="Console.Out"/> value to have UTF-8 encoding for the duration of the 
        /// provided call back.  The newly created <see cref="TextWriter"/> will be passed down to the callback.
        /// </summary>
        internal static T RunWithUtf8Output<T>(Func<TextWriter, T> func)
        {
            Encoding savedEncoding = Console.OutputEncoding;
            try
            {
                Console.OutputEncoding = s_utf8Encoding;
                return func(Console.Out);
            }
            finally
            {
                try
                {
                    Console.OutputEncoding = savedEncoding;
                }
                catch
                {
                    // Nothing to do if we can't reset the console. 
                }
            }
        }

        internal static T RunWithUtf8Output<T>(bool utf8Output, TextWriter textWriter, Func<TextWriter, T> func)
        {
            if (utf8Output && textWriter.Encoding.CodePage != s_utf8Encoding.CodePage)
            {
                if (textWriter != Console.Out)
                {
                    throw new InvalidOperationException("Utf8Output is only supported when writing to Console.Out");
                }

                return RunWithUtf8Output(func);
            }

            return func(textWriter);
        }
    }
}
