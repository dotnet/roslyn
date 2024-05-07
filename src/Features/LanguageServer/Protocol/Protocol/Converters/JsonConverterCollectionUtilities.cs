// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using Newtonsoft.Json;

    /// <summary>
    /// Class containing extension method to thread-safely manage <see cref="JsonConverterCollection"/> operations.
    /// </summary>
    internal static class JsonConverterCollectionUtilities
    {
        /// <summary>
        /// Lock used for modifications to Converters collection.
        /// </summary>
        public static readonly object ConvertersLock = new object();
    }
}
