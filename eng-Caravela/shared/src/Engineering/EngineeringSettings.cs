// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using PostSharp.Engineering.BuildTools.Build;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace PostSharp.Engineering.BuildTools.Engineering
{
    internal class EngineeringSettings : BaseCommandSettings
    {
        [Description( "Clones the repo if it does not exist." )]
        [CommandOption( "--create" )]
        public bool Create { get; init; }

        [Description( "Remote URL of the repo." )]
        [CommandOption( "-u|--url" )]
        public string Url { get; init; } = "https://postsharp@dev.azure.com/postsharp/Caravela/_git/Caravela.Engineering";
    }

    internal class PullEngineeringSettings : EngineeringSettings
    {
        [Description( "Name of the branch. The default is 'develop' for push and 'master' for pull." )]
        [CommandOption( "-b|--branch" )]
        public string? Branch { get; init; }
    }
}