// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CodeAnalysis.Elfie.Model;

namespace Microsoft.CodeAnalysis.SymbolSearch;

internal sealed partial class SymbolSearchUpdateEngine
{
    private sealed class DatabaseFactoryService : IDatabaseFactoryService
    {
        public AddReferenceDatabase CreateDatabaseFromStream(Stream stream, bool isBinary)
        {
            var database = new AddReferenceDatabase(ArdbVersion.V1);

            if (isBinary)
            {
                using var binaryReader = new BinaryReader(stream);
                database.ReadBinary(binaryReader);
            }
            else
            {
                using var streamReader = new StreamReader(stream);
                database.ReadText(streamReader);
            }

            return database;
        }
    }
}
