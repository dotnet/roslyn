using Microsoft.Extensions.FileSystemGlobbing;
using System.Collections.Immutable;
using System.Linq;

namespace PostSharp.Engineering.BuildTools.Commands.Build
{
    public class Pattern
    {
        private ImmutableArray<(string Pattern, bool IsExclude)> Items { get; }

        private Pattern( ImmutableArray<(string Pattern, bool IsExclude)> items )
        {
            this.Items = items;
        }

        public static Pattern Empty { get; } = new( ImmutableArray<(string Pattern, bool IsExclude)>.Empty );

        public Pattern Add( params string[] patterns ) => new( this.Items.AddRange( patterns.Select( p => (p, false) ) ) );
        public Pattern Remove( params string[] patterns  ) => new( this.Items.AddRange( patterns.Select( p => (p, true) ) ) );

        internal void AddToMatcher( Matcher matcher, string packageVersion, string configuration )
        {
            foreach ( var pattern in this.Items )
            {
                var file =
                    pattern.Pattern
                        .Replace( "$(PackageVersion)", packageVersion )
                        .Replace( "$(Configuration)", configuration );
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
    public abstract class PublishingTarget
    {
        /// <summary>
        /// Gets a value indicating whether the target support public publishing, i.e. if it should be included
        /// when the <see cref="PublishOptions.Public"/> option is specified.
        /// </summary>
        public abstract bool SupportsPublicPublishing { get; }
        
        /// <summary>
        /// Gets a value indicating whether the target support private publishing, i.e. if it should be included
        /// when the <see cref="PublishOptions.Public"/> option is not specified.
        /// </summary>
        public abstract bool SupportsPrivatePublishing { get; }
        
        /// <summary>
        /// Gets the extension of the principal artifacts of this target (e.g. <c>.nupkg</c> for a package).
        /// </summary>
        public abstract string MainExtension { get; }

        /// <summary>
        /// Gets the patterns, relatively to the fully-qualified private artifact directory, that select the artefacts
        /// are a part of this target. These artifacts will be copied to the public directory if the target
        /// is executed publicly. From those artifacts, those that have the extension <see cref="MainExtension"/>
        /// will be subject to an invocation by <see cref="Execute"/>.
        /// </summary>
        /// <value></value>
        public abstract Pattern Artifacts { get; }

        /// <summary>
        /// Executes the target for a specified artefact.
        /// </summary>
        public abstract SuccessCode Execute( BuildContext context, PublishOptions options, string file, bool isPublic );
    }
}