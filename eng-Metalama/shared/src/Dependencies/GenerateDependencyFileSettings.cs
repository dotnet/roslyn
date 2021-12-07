// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using PostSharp.Engineering.BuildTools.Build;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;

namespace PostSharp.Engineering.BuildTools.Dependencies
{
    public class GenerateDependencyFileSettings : BaseCommandSettings
    {
        [Description( "The list of local repos, or position of the local repo in the list." )]
        [CommandArgument( 0, "[dependency]" )]
        public string[] Repos { get; protected set; } = Array.Empty<string>();

        [Description( "Specifies that all dependencies are local" )]
        [CommandOption( "--all" )]
        public bool All { get; protected set; }

        [Description( "Specifies that no dependencies are local" )]
        [CommandOption( "--none" )]
        public bool None { get; protected set; }
    }
}