// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Utilities;
using System.IO;

namespace PostSharp.Engineering.BuildTools.Engineering
{
    internal class PullEngineeringCommand : BaseEngineeringCommand<PullEngineeringSettings>
    {
        protected override bool ExecuteCore( BuildContext context, PullEngineeringSettings options )
        {
            context.Console.WriteHeading( "Pulling engineering" );

            var sharedRepo = GetEngineeringRepo( context, options );

            if ( sharedRepo == null )
            {
                return false;
            }

            var branch = options.Branch ?? "master";

            // Check out master and pull.
            if ( !ToolInvocationHelper.InvokeTool( context.Console, "git", $"checkout {branch}", sharedRepo ) )
            {
                return false;
            }

            if ( !ToolInvocationHelper.InvokeTool( context.Console, "git", $"pull", sharedRepo ) )
            {
                return false;
            }

            if ( !CheckNoChange( context, options, context.RepoDirectory ) )
            {
                return false;
            }

            var targetDirectory = Path.Combine( context.RepoDirectory, context.Product.EngineeringDirectory, "shared" );

            CopyDirectory( sharedRepo, targetDirectory );

            context.Console.WriteSuccess( "Pulling engineering was successful." );

            return true;
        }
    }
}