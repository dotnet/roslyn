// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using PostSharp.Engineering.BuildTools.Utilities;
using System.IO;

namespace PostSharp.Engineering.BuildTools.Build
{
    public class FormatCommand : BaseCommand<BaseCommandSettings>
    {
        protected override bool ExecuteCore( BuildContext context, BaseCommandSettings options )
        {
            context.Console.WriteHeading( "Reformatting the code" );

            if ( !CheckNoChange( context, options, context.RepoDirectory ) )
            {
                return false;
            }

            if ( !RestoreDependencyHelper.RestoreTool( context, "JetBrains.Resharper.GlobalTools" ) )
            {
                return false;
            }

            var signTool = Path.Combine( context.RepoDirectory, "tools", "jb.exe" );
            
            foreach ( var solution in context.Product.Solutions )
            {
                if ( solution.CanFormatCode )
                {
                    if ( !ToolInvocationHelper.InvokeTool(
                        context.Console,
                        signTool,
                        $"cleanupcode --profile:Custom {solution.SolutionPath} --disable-settings-layers:\"GlobalAll;GlobalPerProduct;SolutionPersonal;ProjectPersonal\"",
                        Path.GetDirectoryName( solution.SolutionPath )! ) )
                    {
                        return false;
                    }
                }
            }

            context.Console.WriteSuccess( "Reformatting the code was successful." );

            return true;
        }
    }
}