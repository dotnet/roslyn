using System;
using System.IO;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    //Will not ultimately part of the public API. ReadOnlyArray will be replaced come time to ship.
    public static class ReadOnlyArray
    {
        public static ReadOnlyArray<T> Singleton<T>(T item)
        {
            return ReadOnlyArray<T>.CreateFrom(item);
        }

        public static ReadOnlyArray<T> OneOrZero<T>(T itemOpt)
            where T : class
        {
            return itemOpt != null
                ? ReadOnlyArray<T>.CreateFrom(itemOpt)
                : ReadOnlyArray<T>.Empty;
        }

        public static ReadOnlyArray<T> Pair<T>(T first, T second)
        {
            return ReadOnlyArray<T>.CreateFrom(first, second);
        }

        /// <summary>
        /// Reads content of the specified file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>Read-only binary data read from the file.</returns>
        internal static ReadOnlyArray<byte> ReadFromFile(string path)
        {
            return ReadOnlyArray<byte>.CreateFrom(File.ReadAllBytes(path));
        }
    }
}