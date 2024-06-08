// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

internal readonly struct UnitTestingTextDocumentEventArgsWrapper(TextDocumentEventArgs underlyingObject)
{
    internal TextDocumentEventArgs UnderlyingObject { get; } = underlyingObject ?? throw new ArgumentNullException(nameof(underlyingObject));

    public TextDocument Document => UnderlyingObject.Document;
}
