// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;

namespace PostSharp.Engineering.BuildTools.Build
{
    public class BaseCommandSettings : CommandSettings
    {
        private string[] _unparsedProperties = Array.Empty<string>();

        [Description( "Use force" )]
        [CommandOption( "--force" )]
        public bool Force { get; protected set; }

        [Description( "Lists the additional properties supported by the command" )]
        [CommandOption( "--list-properties" )]
        public bool ListProperties { get; protected set; }

        [Description( "Properties in form Name=Value" )]
        [CommandOption( "-p|--property" )]
        public string[] UnparsedProperties
        {
            get => this._unparsedProperties;

            protected set
            {
                this._unparsedProperties = value;

                this.Properties = this.Properties.AddRange(
                    value.Select(
                        v =>
                        {
                            var split = v.Split( '=' );

                            if ( split.Length > 1 )
                            {
                                return new KeyValuePair<string, string>( split[0].Trim(), split[1].Trim() );
                            }
                            else
                            {
                                return new KeyValuePair<string, string>( split[0].Trim(), "True" );
                            }
                        } ) );
            }
        }

        public ImmutableDictionary<string, string> Properties { get; protected set; } =
            ImmutableDictionary.Create<string, string>( StringComparer.OrdinalIgnoreCase );
    }
}