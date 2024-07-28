// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.ImplementType;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.CodeActions
{
    internal readonly record struct OmniSharpCodeActionOptions(
        OmniSharpImplementTypeOptions ImplementTypeOptions,
        OmniSharpLineFormattingOptions LineFormattingOptions);
}
