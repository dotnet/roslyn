// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis;

[Experimental(RoslynExperiments.NullableDisabledSemanticModel, UrlFormat = RoslynExperiments.NullableDisabledSemanticModel_Url)]
[Flags]
public enum SemanticModelOptions
{
    None = 0,
    IgnoreAccessibility = 1 << 0,
    DisableNullableAnalysis = 1 << 1
}
