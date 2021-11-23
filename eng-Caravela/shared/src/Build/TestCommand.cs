// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

namespace PostSharp.Engineering.BuildTools.Build
{
    public class TestCommand : BaseCommand<TestOptions>
    {
        protected override bool ExecuteCore( BuildContext context, TestOptions options )
        {
            return context.Product.Test( context, options );
        }
    }
}