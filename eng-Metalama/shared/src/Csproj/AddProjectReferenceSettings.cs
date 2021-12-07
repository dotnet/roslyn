// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using Spectre.Console.Cli;
using System.ComponentModel;

namespace PostSharp.Engineering.BuildTools.Csproj
{
    public class AddProjectReferenceSettings : CommandSettings
    {
        [Description( "Reference path after which the new reference should be added" )]
        [CommandArgument( 0, "<previous>" )]
        public string PreviousReference { get; init; } = null!;

        [Description( "New reference path" )]
        [CommandArgument( 0, "<previous>" )]
        public string NewReference { get; init; } = null!;

        [Description( "Project name filter (a string that may contain *)" )]
        [CommandOption( "--filter" )]
        public string? Filter { get; init; }
    }
}