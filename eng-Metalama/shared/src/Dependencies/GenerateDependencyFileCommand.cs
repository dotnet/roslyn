// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PostSharp.Engineering.BuildTools.Dependencies
{
    public class GenerateDependencyFileCommand : BaseCommand<GenerateDependencyFileSettings>
    {
        protected override bool ExecuteCore( BuildContext context, GenerateDependencyFileSettings options )
        {
            context.Console.WriteHeading( "Setting the local dependencies" );

            if ( context.Product.Dependencies.IsDefaultOrEmpty )
            {
                context.Console.WriteError( "This product has no dependency." );

                return false;
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine( "<Project>" );

            if ( options.Repos.Length == 0 && !(options.All || options.None) )
            {
                context.Console.WriteError( "No dependency was specified. Use --all or --none." );

                return false;
            }

            var path = Path.Combine( context.RepoDirectory, context.Product.EngineeringDirectory, "Dependencies.props" );

            if ( options.None )
            {
                if ( File.Exists( path ) )
                {
                    context.Console.WriteMessage( $"Deleting '{path}'." );
                    File.Delete( path );
                }
                else
                {
                    context.Console.WriteMessage( "Nothing to do." );
                }
            }
            else
            {
                var localRepos = options.All ? context.Product.Dependencies.Select( x => x.Name ) : options.Repos;

                foreach ( var localDependency in localRepos )
                {
                    ProductDependency? dependency;

                    if ( int.TryParse( localDependency, out var index ) )
                    {
                        if ( index < 1 || index > context.Product.Dependencies.Length )
                        {
                            context.Console.WriteError( $"'{index}' is not a valid dependency index. Use the 'dependencies list' command." );

                            return false;
                        }

                        dependency = context.Product.Dependencies[index - 1];
                    }
                    else
                    {
                        dependency = context.Product.Dependencies.FirstOrDefault( d => d.Name.Equals( localDependency, StringComparison.OrdinalIgnoreCase ) );

                        if ( dependency == null )
                        {
                            context.Console.WriteError(
                                $"'{localDependency}' is not a valid dependency name for this product. Use the 'dependencies list' command." );

                            return false;
                        }
                    }

                    var importProjectFile = Path.GetFullPath( Path.Combine( context.RepoDirectory, "..", dependency.Name, dependency.Name + ".Import.props" ) );

                    if ( !File.Exists( importProjectFile ) )
                    {
                        context.Console.WriteError( $"The file '{importProjectFile}' does not exist. Make sure the dependency repo is built." );

                        return false;
                    }

                    stringBuilder.AppendLine( $"   <Import Project=\"{importProjectFile}\"/>" );
                }

                stringBuilder.AppendLine( "</Project>" );

                context.Console.WriteMessage( $"Generating '{path}'" );
                File.WriteAllText( path, stringBuilder.ToString() );
            }

            context.Console.WriteSuccess( "Setting local dependencies was successful." );

            return true;
        }
    }
}