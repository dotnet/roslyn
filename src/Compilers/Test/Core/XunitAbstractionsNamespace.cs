// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Xunit.Abstractions;

/// <summary>
/// xUnit v3 no longer has an <c>Xunit.Abstractions</c> namespace. Declaring this type keeps the namespace
/// resolvable so test files that still have <c>using Xunit.Abstractions;</c> continue to compile without
/// having to be edited. It is not meant to be used and will be deleted later.
/// </summary>
[Obsolete("Do not use this type; it will be deleted in a follow-up commit.")]
public static class NamespacePlaceholder
{
}
