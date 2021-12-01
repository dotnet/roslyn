// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using PostSharp.Engineering.BuildTools.Utilities;
using System;
using System.IO;
using System.Text;

namespace PostSharp.Engineering.BuildTools.Build.Model
{
    public class MsbuildSolution : Solution
    {
        public MsbuildSolution( string solutionPath ) : base( solutionPath ) { }

        public override bool Build( BuildContext context, BuildOptions options ) => this.RunMsbuild( context, options, "Build" );

        public override bool Pack( BuildContext context, BuildOptions options ) => this.RunMsbuild( context, options, "Pack" );

        public override bool Test( BuildContext context, TestOptions options ) => this.RunMsbuild( context, options, "Test", "-p:RestorePackages=false" );

        public override bool Restore( BuildContext context, BaseBuildSettings options ) => this.RunMsbuild( context, options, "Restore" );

        private bool RunMsbuild( BuildContext context, BaseBuildSettings options, string target, string arguments = "" )
        {
            var argsBuilder = new StringBuilder();
            var path = Path.Combine( context.RepoDirectory, this.SolutionPath );
            argsBuilder.Append( $"-t:{target} -p:Configuration={options.BuildConfiguration} \"{path}\" -v:{options.Verbosity.ToAlias()} -NoLogo" );

            if ( options.NoConcurrency )
            {
                argsBuilder.Append( " -m:1" );
            }

            foreach ( var property in options.Properties )
            {
                argsBuilder.Append( $" -p:{property.Key}={property.Value}" );
            }

            if ( !string.IsNullOrWhiteSpace( arguments ) )
            {
                argsBuilder.Append( " " + arguments.Trim() );
            }

            return ToolInvocationHelper.InvokeTool(
                context.Console,
                "msbuild",
                argsBuilder.ToString(),
                Environment.CurrentDirectory );
        }
    }
}