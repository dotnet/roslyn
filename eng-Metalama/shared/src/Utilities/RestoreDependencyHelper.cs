// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using PostSharp.Engineering.BuildTools.Build;
using System.IO;

namespace PostSharp.Engineering.BuildTools.Utilities
{
    public static class RestoreDependencyHelper
    {
        public static bool RestoreTool( BuildContext context, string tool )
        {
            var restoreTool = Path.Combine( context.RepoDirectory, context.Product.EngineeringDirectory, "shared", "tools", "Restore.ps1" );

            return ToolInvocationHelper.InvokePowershell(
                context.Console,
                restoreTool,
                tool,
                context.RepoDirectory );
        }
    }
}