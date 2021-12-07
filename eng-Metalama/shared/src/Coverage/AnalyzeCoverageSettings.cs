// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using Spectre.Console.Cli;
using System.ComponentModel;

namespace PostSharp.Engineering.BuildTools.Coverage
{
    public class AnalyzeCoverageSettings : CommandSettings
    {
        [CommandArgument( 0, "<path>" )]
        [Description( "Path to the OpenCover xml file" )]
        public string Path { get; init; } = null!;
    }
}