// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#if NET6_0_OR_GREATER

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal enum QueryKind
{
    Compilation,
    Namespace,
    NamedType,
    Method,
    Field,
    Property,
    Event
}
#endif
