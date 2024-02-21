// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.ExtractMethod;

namespace Microsoft.CodeAnalysis.UnitTests;

/// <summary>
/// Currently VB does not support required members, so it can't create instances of some of our option types.
/// This class is a workaround until VB implements the feature.
/// </summary>
internal static class VBOptionsFactory
{
    public static ExtractMethodGenerationOptions CreateExtractMethodGenerationOptions(CodeGenerationOptions codeGenerationOptions, CodeCleanupOptions codeCleanupOptions, ExtractMethodOptions extractOptions)
        => new()
        {
            CodeGenerationOptions = codeGenerationOptions,
            CodeCleanupOptions = codeCleanupOptions,
            ExtractOptions = extractOptions
        };
}
