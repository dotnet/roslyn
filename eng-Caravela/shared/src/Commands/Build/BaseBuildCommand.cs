using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Diagnostics;

#pragma warning disable 8765

namespace PostSharp.Engineering.BuildTools.Commands.Build
{
    public abstract class BaseBuildCommand<T> : Command<T>
        where T : CommandSettings
    {
        public sealed override int Execute( CommandContext context, T settings )
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                if ( !BuildContext.TryCreate( context, out var buildContext ) )
                {
                    return 1;
                }
                else
                {
                    buildContext.Console.Out.Write( new FigletText( buildContext.Product.ProductName )
                        .LeftAligned()
                        .Color( Color.Purple ) );


                    var exitCode = this.ExecuteCore( buildContext, settings );

                    buildContext.Console.WriteMessage( $"Finished at {DateTime.Now} after {stopwatch.Elapsed}." );

                    return exitCode;
                }
            }
            catch ( Exception ex )
            {
                AnsiConsole.WriteException( ex );
                return 10;
            }
        }

        protected abstract int ExecuteCore( BuildContext context, T options );
    }
}