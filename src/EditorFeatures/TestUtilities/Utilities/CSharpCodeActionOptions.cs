// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Simplification;

namespace Microsoft.CodeAnalysis.Test.Utilities;

internal static class CSharpCodeActionOptions
{
    public static CodeActionOptions Default = new(
        new CodeCleanupOptions(
            CSharpSyntaxFormattingOptions.Default,
            CSharpSimplifierOptions.Default,
            AddImportPlacementOptions.Default),
        CSharpCodeGenerationOptions.Default);
}
