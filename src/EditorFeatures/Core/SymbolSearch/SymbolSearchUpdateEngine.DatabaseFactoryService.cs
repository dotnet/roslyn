﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.CodeAnalysis.Elfie.Model;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal partial class SymbolSearchUpdateEngine
    {
        private class DatabaseFactoryService : IDatabaseFactoryService
        {
            public AddReferenceDatabase CreateDatabaseFromBytes(byte[] bytes)
            {
                using (var memoryStream = new MemoryStream(bytes))
                using (var streamReader = new StreamReader(memoryStream))
                {
                    var database = new AddReferenceDatabase();
                    database.ReadText(streamReader);
                    return database;
                }
            }
        }
    }
}
