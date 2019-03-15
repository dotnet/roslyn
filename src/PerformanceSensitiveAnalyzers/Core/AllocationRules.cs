// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.PerformanceSensitive.Analyzers
{
    internal static class AllocationRules
    {
        private static readonly HashSet<(string, string)> IgnoredAttributes = new HashSet<(string, string)>
        {
            ("System.Runtime.CompilerServices", "CompilerGeneratedAttribute"),
            ("System.CodeDom.Compiler", "GeneratedCodeAttribute")
        };

        public const string PerformanceSensitiveAttributeName = "Roslyn.Utilities.PerformanceSensitiveAttribute";

        public static bool IsIgnoredFile(string filePath)
        {
            return filePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsIgnoredAttribute(AttributeData attribute)
        {
            return IgnoredAttributes.Contains((attribute.AttributeClass.ContainingNamespace.ToString(), attribute.AttributeClass.Name));
        }
    }
}
