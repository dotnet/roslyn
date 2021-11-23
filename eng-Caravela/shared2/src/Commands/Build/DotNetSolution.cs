using PostSharp.Engineering.BuildTools.Utilities;
using System;
using System.IO;
using System.Text;

namespace PostSharp.Engineering.BuildTools.Commands.Build
{
    public class DotNetSolution : Solution
    {
        public DotNetSolution( string solutionPath ) : base( solutionPath )
        {
        }

        public override bool Build( BuildContext context, BuildOptions options ) =>
            this.RunDotNet( context, options, "build" );

        public override bool Pack( BuildContext context, BuildOptions options ) =>
            this.RunDotNet( context, options, "pack" );

        public override bool Test( BuildContext context, TestOptions options ) =>
            this.RunDotNet( context, options, "test", "--no-restore" );

        public override bool Restore( BuildContext context, CommonOptions options ) =>
            this.RunDotNet( context, options, "restore" );

        private bool RunDotNet( BuildContext context, CommonOptions options, string command, string arguments = "" )
        {
            var argsBuilder = new StringBuilder();
            var path = Path.Combine( context.RepoDirectory, this.SolutionPath );
            argsBuilder.Append(
                $"{command} -p:Configuration={options.BuildConfiguration} \"{path}\" -v:{options.Verbosity.ToAlias()} --nologo" );

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

            return ToolInvocationHelper.InvokeTool( context.Console, "dotnet",
                argsBuilder.ToString(),
                Environment.CurrentDirectory );
        }
    }
}