using PostSharp.Engineering.BuildTools.Commands.Build;
using Spectre.Console.Cli;

namespace PostSharp.Engineering.BuildTools
{
    public static class AppExtensions
    {
        public static void AddProductCommands( this CommandApp app, Product? product = null )
        {
            if ( product != null )
            {
                app.Configure( x =>
                {
                    x.Settings.StrictParsing = true;
                    x.AddCommand<PrepareCommand>( "prepare" ).WithData( product )
                        .WithDescription( "Creates the files that are required to build the product" );
                    x.AddCommand<BuildCommand>( "build" ).WithData( product )
                        .WithDescription( "Builds all packages in the product (implies 'prepare')" );
                    x.AddCommand<TestCommand>( "test" ).WithData( product )
                        .WithDescription( "Builds all packages then run all tests (implies 'build')" );
                    x.AddCommand<PublishCommand>( "publish" ).WithData( product )
                        .WithDescription( "Publishes all packages that have been previously built by the 'build' command" );
                } );
            }
        }
    }
}