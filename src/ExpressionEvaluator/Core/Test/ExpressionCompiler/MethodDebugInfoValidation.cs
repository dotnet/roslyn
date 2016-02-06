// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal static class MethodDebugInfoValidation
    {
        internal static void Verify(this ImmutableArray<ExternAliasRecord> actual, params string[] expected)
        {
            AssertEx.Equal(expected, actual.Select(r => $"{r.Alias} = '{r.TargetAssembly}'"));
        }

        internal static void Verify(this ImmutableArray<ImmutableArray<ImportRecord>> actual, string expected)
        {
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected,
                string.Join(Environment.NewLine,
                    actual.Select(g => string.Join(Environment.NewLine, 
                    new[] { "{" }.Concat(g.Select(i => "    " + DisplayImport(i))).Concat(new[] { "}" })))));
        }

        private static string DisplayImport(ImportRecord record)
        {
            return
                record.TargetKind + ":" +
                (record.Alias != null ? $" alias='{record.Alias}'" : "") +
                (record.TargetAssembly != null ? $" assembly='{record.TargetAssembly}'" : "") +
                (record.TargetAssemblyAlias != null ? $" assembly-alias='{record.TargetAssemblyAlias}'" : "") +
                (record.TargetType != null ? $" type='{record.TargetType}'" : "") +
                (record.TargetString != null ? $" string='{record.TargetString}'" : "");
        }
    }
}
