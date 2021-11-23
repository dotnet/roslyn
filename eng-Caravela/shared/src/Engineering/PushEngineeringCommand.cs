// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Utilities;
using System.IO;

namespace PostSharp.Engineering.BuildTools.Engineering
{
    internal class PushEngineeringCommand : BaseEngineeringCommand<EngineeringSettings>
    {
        protected override bool ExecuteCore( BuildContext context, EngineeringSettings options )
        {
            context.Console.WriteHeading( "Pushing engineering." );

            var sharedRepo = GetEngineeringRepo( context, options );

            if ( sharedRepo == null )
            {
                return false;
            }

            // Copy the files (removing the previous content).
            context.Console.WriteImportantMessage( $"Copying '{context.Product.EngineeringDirectory}' to '{sharedRepo}'." );
            CopyDirectory( Path.Combine( context.RepoDirectory, context.Product.EngineeringDirectory, "shared" ), sharedRepo );

            // Stage all changes to the commit, but does not commit.
            ToolInvocationHelper.InvokeTool( context.Console, "git", $"add --all", sharedRepo );

            ToolInvocationHelper.InvokeTool( context.Console, "git", $"status", sharedRepo );

            context.Console.WriteSuccess( "Pushing engineering was successful. You now need to commit and push manually." );

            return true;
        }
    }
}