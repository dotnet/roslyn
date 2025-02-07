// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Analyzer.Utilities.Extensions
{
    /// <summary>
    /// Class that contains extensions to <see cref="SourceText"/>.
    /// </summary>
    internal static partial class SourceTextExtensions
    {
        /// <summary>
        /// Reads the <paramref name="text"/> contents into a stream and returns the result of calling the
        /// <paramref name="parser"/> function on that stream.
        /// </summary>
        /// <typeparam name="T">Type to deserialize from the <paramref name="text"/>.</typeparam>
        /// <param name="text">Abstraction for an additional file's contents.</param>
        /// <param name="parser">Function that will parse <paramref name="text"/> into <typeparamref name="T"/>.</param>
        /// <returns>Output from <paramref name="parser"/>.</returns>
        public static T Parse<T>(this SourceText text, Func<StreamReader, T> parser)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }

            using var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                text.Write(writer);
            }

            stream.Position = 0;

            using var reader = new StreamReader(stream);
            return parser(reader);
        }
    }
}
