// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using Microsoft.Extensions.FileSystemGlobbing;
using System.Collections.Immutable;
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

        internal void AddToMatcher( Matcher matcher, VersionInfo parameters )
        {
            foreach ( var pattern in this.Items )
            {
                var file = pattern.Pattern.ToString( parameters );

                if ( pattern.IsExclude )
                {
                    matcher.AddExclude( file );
                }
                else
                {
                    matcher.AddInclude( file );
                }
            }
        }
    }
}