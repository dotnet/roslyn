// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis;

[Flags]
internal enum CompilerFeatureRequiredFeatures
{
    None = 0,
    RefStructs = 1 << 0,
    RequiredMembers = 1 << 1,
    UserDefinedCompoundAssignmentOperators = 1 << 2,
}
