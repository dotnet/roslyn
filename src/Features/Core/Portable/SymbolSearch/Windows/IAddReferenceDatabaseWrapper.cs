// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Elfie.Model;

namespace Microsoft.CodeAnalysis.SymbolSearch;

// Wrapper types to ensure we delay load the elfie database.
internal interface IAddReferenceDatabaseWrapper
{
    AddReferenceDatabase Database { get; }
}

internal sealed class AddReferenceDatabaseWrapper(AddReferenceDatabase database) : IAddReferenceDatabaseWrapper
{
    public AddReferenceDatabase Database { get; } = database;
}
