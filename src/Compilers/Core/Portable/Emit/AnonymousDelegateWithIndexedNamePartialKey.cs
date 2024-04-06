// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Emit;

/// <summary>
/// Key used to group anonymous delegate templates by properties that are easy to infer from both source symbols and metadata.
/// </summary>
internal readonly record struct AnonymousDelegateWithIndexedNamePartialKey(int GenericArity, int ParameterCount);
