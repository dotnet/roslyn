// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Dependencies;
using PostSharp.Engineering.BuildTools.Engineering;
using Spectre.Console.Cli;
using System.Linq;

namespace PostSharp.Engineering.BuildTools
{
    public static class AppExtensions
    {
        public static void AddProductCommands( this CommandApp app, Product? product = null )
        {
            if ( product != null )
            {
                app.Configure(
                    x =>
                    {
                        x.Settings.StrictParsing = true;

                        x.AddCommand<PrepareCommand>( "prepare" )
                            .WithData( product )
                            .WithDescription( "Creates the files that are required to build the product" );

                        x.AddCommand<BuildCommand>( "build" )
                            .WithData( product )
                            .WithDescription( "Builds all packages in the product (implies 'prepare')" );

                        x.AddCommand<TestCommand>( "test" )
                            .WithData( product )
                            .WithDescription( "Builds all packages then run all tests (implies 'build')" );

                        x.AddCommand<PublishCommand>( "publish" )
                            .WithData( product )
                            .WithDescription( "Publishes all packages that have been previously built by the 'build' command" );

                        if ( product.Solutions.Any( s => s.CanFormatCode ) )
                        {
                            x.AddCommand<FormatCommand>( "format" ).WithData( product ).WithDescription( "Formats the code" );
                        }

                        x.AddBranch(
                            "dependencies",
                            configurator =>
                            {
                                configurator.AddCommand<ListDependenciesCommand>( "list" )
                                    .WithData( product )
                                    .WithDescription( "Lists the dependencies of this product" );

                                configurator.AddCommand<GenerateDependencyFileCommand>( "local" )
                                    .WithData( product )
                                    .WithDescription( "Generates the Dependencies.props to consume local repos." );

                                configurator.AddCommand<PrintDependenciesCommand>( "print" )
                                    .WithData( product )
                                    .WithDescription( "Prints the dependency file." );
                            } );

                        x.AddBranch(
                            "engineering",
                            configurator =>
                            {
                                configurator.AddCommand<PushEngineeringCommand>( "push" )
                                    .WithData( product )
                                    .WithDescription(
                                        $"Copies the changes in {product.EngineeringDirectory}/shared to the local engineering repo, but does not commit nor push." );

                                configurator.AddCommand<PullEngineeringCommand>( "pull" )
                                    .WithData( product )
                                    .WithDescription(
                                        $"Copies the remote engineering repo to {product.EngineeringDirectory}/shared. Automatically pulls 'master'." );
                            } );
                    } );
            }
        }
    }
}