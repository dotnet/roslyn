// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using Microsoft.CodeAnalysis.Elfie.Model;

namespace Microsoft.CodeAnalysis.SymbolSearch;

internal sealed partial class SymbolSearchUpdateEngine
{
    private sealed class DatabaseFactoryService : IDatabaseFactoryService
    {
        public AddReferenceDatabase CreateDatabaseFromBytes(byte[] bytes)
        {
            using var memoryStream = new MemoryStream(bytes);
            using var streamReader = new StreamReader(memoryStream);
            var database = new AddReferenceDatabase(ArdbVersion.V1);
            database.ReadText(streamReader);
            return database;
        }
    }
}
