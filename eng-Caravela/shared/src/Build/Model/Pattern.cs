// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace PostSharp.Engineering.BuildTools.Build.Model
{
    public class Pattern
    {
        private ImmutableArray<(ParametricString Pattern, bool IsExclude)> Items { get; }

        private Pattern( ImmutableArray<(ParametricString Pattern, bool IsExclude)> items )
        {
            this.Items = items;
        }

        public static Pattern Empty { get; } = new( ImmutableArray<(ParametricString Pattern, bool IsExclude)>.Empty );

        public Pattern Add( params ParametricString[] patterns ) => new( this.Items.AddRange( patterns.Select( p => (p, false) ) ) );

        public Pattern Remove( params ParametricString[] patterns ) => new( this.Items.AddRange( patterns.Select( p => (p, true) ) ) );

        internal bool TryGetFiles( string directory, VersionInfo versionInfo, List<FilePatternMatch> files )
        {
            var matches = new List<string>();

            var matcher = new Matcher( StringComparison.OrdinalIgnoreCase );

            foreach ( var pattern in this.Items )
            {
                var file = pattern.Pattern.ToString( versionInfo );

                if ( pattern.IsExclude )
                {
                    matcher.AddExclude( file );
                }
                else
                {
                    matcher.AddInclude( file );
                }
              
            }
var matchingResult =
            matcher.Execute( new DirectoryInfoWrapper( new DirectoryInfo( directory ) ) );

            if ( !matchingResult.HasMatches )
            {
                return false;
            }
            else
            {
                files.AddRange( matchingResult.Files );
            }

            return true;
        }

        public override string ToString()
            => string.Join( " ", this.Items.Select( i => (i.IsExclude ? "-" : "+") + i.Pattern ) );
        
    }
}