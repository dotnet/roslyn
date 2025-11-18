// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

namespace Microsoft.CodeAnalysis;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.GenericParameter)]
internal sealed class NonCopyableAttribute : Attribute
{
}
