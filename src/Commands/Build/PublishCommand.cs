using Spectre.Console.Cli;
using System.ComponentModel;

namespace PostSharp.Engineering.BuildTools.Commands.Build
{
    public class PublishOptions : CommandSettings
    {
        [Description( "Includes the public artifacts" )]
        [CommandOption( "--public" )]
        public bool Public { get; protected set; }
        
        [Description( "Prints the command line, but does not execute it" )]
        [CommandOption( "--dry" )]

        public bool Dry { get; protected set; }
    }
    
    public class PublishCommand : BaseBuildCommand<PublishOptions>
    {
        protected override int ExecuteCore( BuildContext context, PublishOptions options ) 
            => context.Product.Publish( context, options ) ? 0 : 2;
    }
}