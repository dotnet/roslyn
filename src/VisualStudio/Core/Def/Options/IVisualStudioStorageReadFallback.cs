// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal delegate Optional<object?> TryReadValueDelegate(string storageKey, Type storageType, object? defaultValue);

/// <summary>
/// Export an implementation of this interface to instruct <see cref="VisualStudioOptionPersister"/> to read option value
/// from additional storage locations, if it is not found in the primary storage location specified in <see cref="VisualStudioOptionStorage"/>.
/// This is only necessary for backward compatibility when an option changes the VS storage location or format.
/// </summary>
internal interface IVisualStudioStorageReadFallback
{
    Optional<object?> TryRead(string? language, TryReadValueDelegate readValue);
}

