// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using Spectre.Console.Cli;
using System.ComponentModel;

namespace PostSharp.Engineering.BuildTools.NuGet
{
    public class VerifyPackageSettings : CommandSettings
    {
        [Description( "Directory containing the packages" )]
        [CommandArgument( 0, "<directory>" )]
        public string Directory { get; init; } = null!;
    }
}