// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using Spectre.Console.Cli;
using System.ComponentModel;

namespace PostSharp.Engineering.BuildTools.Build
{
    public class PublishOptions : BaseCommandSettings
    {
        [Description(
            "Sets the build configuration (Debug or Release) to publish. This option is irrelevant unless the artifact paths depend on the build configuration." )]
        [CommandOption( "-c|--configuration" )]
        public BuildConfiguration BuildConfiguration { get; protected set; }

        [Description( "Includes the public artifacts" )]
        [CommandOption( "--public" )]
        public bool Public { get; protected set; }

        [Description( "Prints the command line, but does not execute it" )]
        [CommandOption( "--dry" )]

        public bool Dry { get; protected set; }
    }

    public class PublishCommand : BaseCommand<PublishOptions>
    {
        protected override bool ExecuteCore( BuildContext context, PublishOptions options ) => context.Product.Publish( context, options );
    }
}