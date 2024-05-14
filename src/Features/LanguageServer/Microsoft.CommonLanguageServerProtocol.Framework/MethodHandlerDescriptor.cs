// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

namespace Microsoft.CommonLanguageServerProtocol.Framework;

internal sealed record MethodHandlerDescriptor(
    string MethodName,
    string Language,
    TypeRef? RequestTypeRef,
    TypeRef? ResponseTypeRef,
    TypeRef RequestContextTypeRef);
