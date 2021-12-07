// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

namespace PostSharp.Engineering.BuildTools.Build
{
    public class BuildCommand : BaseCommand<BuildOptions>
    {
        protected override bool ExecuteCore( BuildContext context, BuildOptions options )
        {
            return context.Product.Build( context, options );
        }
    }
}