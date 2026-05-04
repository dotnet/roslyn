// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CodeAnalysis.Elfie.Model;

namespace Microsoft.CodeAnalysis.SymbolSearch;

internal interface IDatabaseFactoryService
{
    AddReferenceDatabase CreateDatabaseFromStream(Stream stream, bool isBinary);
}
