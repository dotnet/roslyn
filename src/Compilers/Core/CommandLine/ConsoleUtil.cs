// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// This will update the <see cref="Console.Out"/> value to have UTF8 encoding for the duration of the 
        /// provided call back.  The newly created <see cref="TextWriter"/> will be passed down to the callback.
        /// </summary>
        internal static T RunWithUtf8Output<T>(Func<TextWriter, T> func)
        {
            TextWriter savedOut = Console.Out;
            try
            {
                using (var streamWriterOut = new StreamWriter(Console.OpenStandardOutput(), s_utf8Encoding))
                {
                    Console.SetOut(streamWriterOut);
                    return func(streamWriterOut);
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
            }
        }
    }
}
