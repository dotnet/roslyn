// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using PostSharp.Engineering.BuildTools.Build;
using System.IO;

namespace PostSharp.Engineering.BuildTools.Dependencies
{
    public class PrintDependenciesCommand : BaseCommand<BaseCommandSettings>
    {
        protected override bool ExecuteCore( BuildContext context, BaseCommandSettings options )
        {
            var path = Path.Combine( context.RepoDirectory, context.Product.EngineeringDirectory, "Dependencies.props" );

            if ( File.Exists( path ) )
            {
                context.Console.WriteImportantMessage( $"'{path}' has the following content:" );
                context.Console.WriteMessage( File.ReadAllText( path ) );
            }
            else
            {
                context.Console.WriteWarning( $"The file '{path}' does not exist. There are no local dependencies." );
            }

            return true;
        }
    }
}