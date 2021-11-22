using PostSharp.Engineering.BuildTools.Commands.Csproj;
using PostSharp.Engineering.BuildTools.Commands.NuGet;
using Spectre.Console.Cli;

namespace PostSharp.Engineering.BuildTools
{
    internal class Program
    {
        private static int Main( string[] args )
        {
            var commandApp = new CommandApp();

            commandApp.Configure( c =>
            {
                c.AddBranch( "csproj",
                    x => x.AddCommand<AddProjectReferenceCommand>( "add-project-reference" )
                        .WithDescription( "Adds a <ProjectReference> item to *.csproj in a directory" ) );
                c.AddBranch( "nuget", x =>
                {
                    x.AddCommand<RenamePackagesCommand>( "rename" )
                        .WithDescription( "Renames all packages in a directory" );
                    x.AddCommand<VerifyPublicPackageCommand>( "verify-public" ).WithDescription(
                        "Verifies that all packages in a directory have only references to packages published on nuget.org." );
                } );
            } );

            return commandApp.Run( args );
        }
    }
}