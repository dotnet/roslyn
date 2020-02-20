// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

#nullable enable
namespace Microsoft.CodeAnalysis
{
    //PROTOTYPE: we'll wire this up to the project options when we figure that part out
    internal class GeneratorDriverAnalyzerOptions : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return new CompilerAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty);
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return new CompilerAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty);
        }
    }
}
