// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

namespace PostSharp.Engineering.BuildTools.Build
{
    public class PrepareCommand : BaseCommand<BaseBuildSettings>
    {
        protected override bool ExecuteCore( BuildContext context, BaseBuildSettings options )
        {
            return context.Product.Prepare( context, options );
        }
    }
}