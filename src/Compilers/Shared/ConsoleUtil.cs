// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Text;

namespace Roslyn.Utilities
{
    internal static class ConsoleUtil
    {
        private static readonly Encoding s_utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// When <paramref name="utf8Encoding"/> is true then <paramref name="func"/> will be run
        /// while both <see cref="Console.Out"/> and <see cref="Console.Error"/> are set 
        /// to set to UTF8 encoding.  Otherwise it will be run with the <see cref="Console"/> in
        /// its current state.
        /// </summary>
        internal static T RunWithOutput<T>(bool utf8Encoding, Func<TextWriter, TextWriter, T> func)
        {
            return utf8Encoding
                ? RunWithEncoding(s_utf8Encoding, func)
                : func(Console.Out, Console.Error);
        }

        /// <summary>
        /// Run the <paramref name="func"/> argument while both <see cref="Console.Out"/> and 
        /// <see cref="Console.Error"/> are set to set to <paramref name="encoding"/>
        /// </summary>
        internal static T RunWithEncoding<T>(Encoding encoding, Func<TextWriter, TextWriter, T> func)
        {
            TextWriter savedOut = Console.Out;
            TextWriter savedError = Console.Error;
            try
            {
                using (var streamWriterOut = new StreamWriter(Console.OpenStandardOutput(), encoding))
                using (var streamWriterError = new StreamWriter(Console.OpenStandardError(), encoding))
                {
                    Console.SetOut(streamWriterOut);
                    Console.SetError(streamWriterError);
                    return func(streamWriterOut, streamWriterError);
                }
            }
            finally
            {
                try
                {
                    Console.SetOut(savedOut);
                }
                catch
                {
                    // Nothing to do if we can't reset the console. 
                }

                try
                {
                    Console.SetError(savedOut);
                }
                catch
                {
                    // Nothing to do if we can't reset the console. 
                }
            }
        }
    }
}
