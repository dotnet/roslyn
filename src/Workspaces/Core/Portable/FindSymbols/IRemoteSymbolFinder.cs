// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal interface IRemoteSymbolFinder
    {
        Task FindReferencesAsync(SerializableSymbolAndProjectId symbolAndProjectIdArg, DocumentId[] documentArgs);
        Task FindLiteralReferencesAsync(object value, TypeCode typeCode);

        Task<IList<SerializableSymbolAndProjectId>> FindAllDeclarationsWithNormalQueryAsync(
            ProjectId projectId, string name, SearchKind searchKind, SymbolFilter criteria);

        Task<IList<SerializableSymbolAndProjectId>> FindSolutionSourceDeclarationsWithNormalQueryAsync(
            string name, bool ignoreCase, SymbolFilter criteria);

        Task<IList<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithNormalQueryAsync(
            ProjectId projectId, string name, bool ignoreCase, SymbolFilter criteria);

        Task<IList<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithPatternAsync(
            ProjectId projectId, string pattern, SymbolFilter criteria);
    }
}