// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using PostSharp.Engineering.BuildTools.Utilities;
using System.IO;

namespace PostSharp.Engineering.BuildTools.Build.Model
{
    public class DotNetSolution : Solution
    {
        public DotNetSolution( string solutionPath ) : base( solutionPath ) { }

        public override bool Build( BuildContext context, BuildOptions options ) => this.RunDotNet( context, options, "build" );

        public override bool Pack( BuildContext context, BuildOptions options ) => this.RunDotNet( context, options, "pack" );

        public override bool Test( BuildContext context, TestOptions options ) => this.RunDotNet( context, options, "test", "--no-restore" );

        public override bool Restore( BuildContext context, BaseBuildSettings options ) => this.RunDotNet( context, options, "restore" );

        private bool RunDotNet( BuildContext context, BaseBuildSettings options, string command, string arguments = "" )
            => DotNetHelper.Run( context, options, Path.Combine( context.RepoDirectory, this.SolutionPath ), command, arguments );
    }
}