// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using Spectre.Console.Cli;
using System.ComponentModel;

namespace PostSharp.Engineering.BuildTools.Build
{
    public class TestOptions : BuildOptions
    {
        [Description( "Analyze the test coverage while and after running the tests" )]
        [CommandOption( "--analyze-coverage" )]
        public bool AnalyzeCoverage { get; init; }
    }
}