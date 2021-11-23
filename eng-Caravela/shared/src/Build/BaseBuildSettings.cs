// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using PostSharp.Engineering.BuildTools.Build.Model;
using Spectre.Console.Cli;
using System.Collections.Immutable;
using System.ComponentModel;

namespace PostSharp.Engineering.BuildTools.Build
{
    public class BaseBuildSettings : BaseCommandSettings
    {
        [Description( "Sets the build configuration (Debug or Release)" )]
        [CommandOption( "-c|--configuration" )]
        public BuildConfiguration BuildConfiguration { get; protected set; }

        [Description( "Creates a numbered build (typically for an internal CI build)" )]
        [CommandOption( "--numbered" )]
        public int BuildNumber { get; protected set; }

        [Description( "Creates a public build (typically to publish to nuget.org)" )]
        [CommandOption( "--public" )]
        public bool PublicBuild { get; protected set; }

        [Description( "Sets the verbosity" )]
        [CommandOption( "-v|--verbosity" )]
        [DefaultValue( Verbosity.Minimal )]
        public Verbosity Verbosity { get; protected set; }

        [Description( "Executes only the current command, but not the previous command" )]
        [CommandOption( "--no-dependencies" )]
        public bool NoDependencies { get; protected set; }

        [Description( "Determines wether test-only assemblies should be included in the operation" )]
        [CommandOption( "--include-tests" )]
        public bool IncludeTests { get; protected set; }

        [Description( "Disables concurrent processing" )]
        [CommandOption( "--no-concurrency" )]
        public bool NoConcurrency { get; protected set; }

        public BaseBuildSettings WithIncludeTests( bool value )
        {
            var clone = (BaseBuildSettings) this.MemberwiseClone();
            clone.IncludeTests = value;

            return clone;
        }

        public BaseBuildSettings WithoutConcurrency()
        {
            var clone = (BaseBuildSettings) this.MemberwiseClone();
            clone.NoConcurrency = true;

            return clone;
        }

        public BaseBuildSettings WithAdditionalProperties( ImmutableDictionary<string, string> properties )
        {
            if ( properties.IsEmpty )
            {
                return this;
            }

            var clone = (BaseBuildSettings) this.MemberwiseClone();
            clone.Properties = clone.Properties.AddRange( properties );

            return clone;
        }

        public VersionSpec VersionSpec
            => this.BuildNumber > 0
                ? new VersionSpec( VersionKind.Numbered, this.BuildNumber )
                : this.PublicBuild
                    ? new VersionSpec( VersionKind.Public )
                    : new VersionSpec( VersionKind.Local );
    }
}