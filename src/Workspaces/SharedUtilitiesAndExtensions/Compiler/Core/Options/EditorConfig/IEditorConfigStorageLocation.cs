// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

namespace Microsoft.CodeAnalysis.Options
{
    internal interface IEditorConfigStorageLocation
    {
        bool TryGetOption(StructuredAnalyzerConfigOptions options, Type type, out object? value);
    }
}
