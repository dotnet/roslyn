using Spectre.Console.Cli;
using System.ComponentModel;

namespace PostSharp.Engineering.BuildTools.Commands.Coverage
{
    public class AnalyzeCoverageSettings : CommandSettings
    {
        [CommandArgument( 0, "<path>" )]
        [Description( "Path to the OpenCover xml file" )]
        public string Path { get; init; } = null!;
    }
}