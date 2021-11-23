using Spectre.Console.Cli;
using System.ComponentModel;

namespace PostSharp.Engineering.BuildTools.Commands.Csproj
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