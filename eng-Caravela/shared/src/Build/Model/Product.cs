// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using PostSharp.Engineering.BuildTools.Coverage;
using PostSharp.Engineering.BuildTools.NuGet;
using PostSharp.Engineering.BuildTools.Utilities;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace PostSharp.Engineering.BuildTools.Build.Model
{
    public class Product
    {
        public string EngineeringDirectory { get; init; } = "eng";

        public string ProductName { get; init; } = "Unnamed";

        public string ProductNameWithoutDot => this.ProductName.Replace( ".", "", StringComparison.OrdinalIgnoreCase );

        public ParametricString PrivateArtifactsDirectory { get; init; } = Path.Combine( "artifacts", "private" );

        public ParametricString PublicArtifactsDirectory { get; init; } = Path.Combine( "artifacts", "public" );

        public bool GenerateArcadeProperties { get; init; }

        public ImmutableArray<string> AdditionalDirectoriesToClean { get; init; } = ImmutableArray<string>.Empty;

        public ImmutableArray<Solution> Solutions { get; init; } = ImmutableArray<Solution>.Empty;

        public ImmutableArray<PublishingTarget> PublishingTargets { get; init; } = ImmutableArray<PublishingTarget>.Empty;

        public ImmutableArray<ProductDependency> Dependencies { get; init; } = ImmutableArray<ProductDependency>.Empty;

        public ImmutableDictionary<string, string> SupportedProperties { get; init; } = ImmutableDictionary<string, string>.Empty;

        public bool Build( BuildContext context, BuildOptions options )
        {
            // Validate options.
            if ( options.PublicBuild )
            {
                if ( options.BuildConfiguration != BuildConfiguration.Release )
                {
                    context.Console.WriteError( $"Cannot build a public version of a {options.BuildConfiguration} build without --force." );

                    return false;
                }
            }

            // Build dependencies.
            if ( !options.NoDependencies && !this.Prepare( context, options ) )
            {
                return false;
            }

            // We have to read the version from the file we have generated - using MSBuild, because it contains properties.
            var versionInfo = this.ReadGeneratedVersionFile( context.GetVersionFilePath( options.BuildConfiguration ) );

            var privateArtifactsDir = Path.Combine( context.RepoDirectory, this.PrivateArtifactsDirectory.ToString( versionInfo ) );

            // Build.
            if ( !this.BuildCore( context, options ) )
            {
                return false;
            }

            // Allow for some customization before we create the zip file and copy to the public directory.
            this.BuildCompleted?.Invoke( (context, options, privateArtifactsDir) );

            // Zipping internal artifacts.
            void CreateZip( string directory )
            {
                if ( options.CreateZip )
                {
                    var zipFile = Path.Combine( directory, $"{this.ProductName}-{versionInfo.PackageVersion}.zip" );

                    context.Console.WriteMessage( $"Creating '{zipFile}'." );
                    var tempFile = Path.Combine( Path.GetTempPath(), Guid.NewGuid() + ".zip" );

                    ZipFile.CreateFromDirectory(
                        directory,
                        tempFile,
                        CompressionLevel.Optimal,
                        false );

                    File.Move( tempFile, zipFile );
                }
            }

            CreateZip( privateArtifactsDir );

            // If we're doing a public build, copy public artifacts to the publish directory.
            if ( options.PublicBuild )
            {
                // Copy artifacts.
                context.Console.WriteHeading( "Copying public artifacts" );
                var matcher = new Matcher( StringComparison.OrdinalIgnoreCase );

                foreach ( var publishingTarget in this.PublishingTargets )
                {
                    if ( publishingTarget.SupportsPublicPublishing )
                    {
                        publishingTarget.Artifacts.AddToMatcher( matcher, versionInfo );
                    }
                }

                var publicArtifactsDirectory = Path.Combine( context.RepoDirectory, this.PublicArtifactsDirectory.ToString( versionInfo ) );

                if ( !Directory.Exists( publicArtifactsDirectory ) )
                {
                    Directory.CreateDirectory( publicArtifactsDirectory );
                }

                var matches = matcher.Execute( new DirectoryInfoWrapper( new DirectoryInfo( privateArtifactsDir ) ) );

                if ( matches is { HasMatches: true } )
                {
                    foreach ( var file in matches.Files )
                    {
                        var targetFile = Path.Combine( publicArtifactsDirectory, Path.GetFileName( file.Path ) );

                        context.Console.WriteMessage( file.Path );
                        File.Copy( Path.Combine( privateArtifactsDir, file.Path ), targetFile, true );
                    }
                }

                // Verify that public packages have no private dependencies.
                if ( !VerifyPublicPackageCommand.Execute(
                    context.Console,
                    new VerifyPackageSettings { Directory = publicArtifactsDirectory } ) )
                {
                    return false;
                }

                // Sign public artifacts.
                var signSuccess = true;

                if ( options.Sign )
                {
                    context.Console.WriteHeading( "Signing artifacts" );

                    // Restore signing tools.
                    var signTool = Path.Combine( context.RepoDirectory, "tools", "SignClient.exe" );

                    var signToolConfig = Path.Combine(
                        context.RepoDirectory,
                        this.EngineeringDirectory,
                        "shared",
                        "tools",
                        "signclient-appsettings.json" );

                    var signToolSecret = Environment.GetEnvironmentVariable( "SIGNSERVER_SECRET" );

                    if ( signToolSecret == null )
                    {
                        context.Console.WriteError( "The SIGNSERVER_SECRET environment variable is not defined." );

                        return false;
                    }

                    if ( !RestoreDependencyHelper.RestoreTool( context, "SignClient" ) )
                    {
                        return false;
                    }

                    void Sign( string filter )
                    {
                        if ( Directory.EnumerateFiles( publicArtifactsDirectory, filter ).Any() )
                        {
                            // We don't pass the secret so it does not get printed. We pass an environment variable reference instead.
                            // The ToolInvocationHelper will expand it.

                            signSuccess = signSuccess && ToolInvocationHelper.InvokeTool(
                                context.Console,
                                signTool,
                                $"Sign --baseDirectory {publicArtifactsDirectory} --input {filter} --config {signToolConfig} --name {this.ProductName} --user sign-caravela@postsharp.net --secret %SIGNSERVER_SECRET%",
                                context.RepoDirectory );
                        }
                    }

                    Sign( "*.nupkg" );
                    Sign( "*.vsix" );

                    if ( !signSuccess )
                    {
                        return false;
                    }

                    // Zipping public artifacts.
                    CreateZip( publicArtifactsDirectory );

                    context.Console.WriteSuccess( "Signing artifacts was successful." );
                }
            }
            else if ( options.Sign )
            {
                context.Console.WriteWarning( $"Cannot use --sign option in a non-public build." );

                return false;
            }

            context.Console.WriteSuccess( $"Building the whole {this.ProductName} product was successful." );

            return true;
        }

        private VersionInfo ReadGeneratedVersionFile( string path )
        {
            var versionFilePath = path;
            var versionFile = Project.FromFile( versionFilePath, new ProjectOptions() );

            var packageVersion = versionFile
                .Properties
                .Single( p => p.Name == this.ProductNameWithoutDot + "Version" )
                .EvaluatedValue;

            if ( string.IsNullOrEmpty( packageVersion ) )
            {
                throw new InvalidOperationException( "PackageVersion should not be null." );
            }

            var configuration = versionFile
                .Properties
                .Single( p => p.Name == this.ProductNameWithoutDot + "BuildConfiguration" )
                .EvaluatedValue;

            if ( string.IsNullOrEmpty( configuration ) )
            {
                throw new InvalidOperationException( "BuildConfiguration should not be null." );
            }

            ProjectCollection.GlobalProjectCollection.UnloadAllProjects();

            return new VersionInfo( packageVersion, configuration );
        }

        private static (string MainVersion, string PackageVersionSuffix) ReadMainVersionFile( string path )
        {
            var versionFilePath = path;
            var versionFile = Project.FromFile( versionFilePath, new ProjectOptions() );

            var mainVersion = versionFile
                .Properties
                .SingleOrDefault( p => p.Name == "MainVersion" )
                ?.EvaluatedValue;

            if ( string.IsNullOrEmpty( mainVersion ) )
            {
                throw new InvalidOperationException( $"MainVersion should not be null in '{path}'." );
            }

            var suffix = versionFile
                             .Properties
                             .SingleOrDefault( p => p.Name == "PackageVersionSuffix" )
                             ?.EvaluatedValue
                         ?? "";

            // Empty suffixes are allowed and mean RTM.

            ProjectCollection.GlobalProjectCollection.UnloadAllProjects();

            return (mainVersion, suffix);
        }

        /// <summary>
        /// An event raised when the build is completed.
        /// </summary>
        public event Func<(BuildContext Context, BuildOptions Options, string Directory), bool>? BuildCompleted;

        protected virtual bool BuildCore( BuildContext context, BuildOptions options )
        {
            foreach ( var solution in this.Solutions )
            {
                if ( options.IncludeTests || !solution.IsTestOnly )
                {
                    context.Console.WriteHeading( $"Building {solution.Name}." );

                    if ( !solution.Restore( context, options ) )
                    {
                        return false;
                    }

                    if ( solution.IsTestOnly )
                    {
                        // Never try to pack solutions.
                        if ( !solution.Build( context, options ) )
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if ( solution.PackRequiresExplicitBuild && !options.NoDependencies )
                        {
                            if ( !solution.Build( context, options ) )
                            {
                                return false;
                            }
                        }

                        if ( !solution.Pack( context, options ) )
                        {
                            return false;
                        }
                    }

                    context.Console.WriteSuccess( $"Building {solution.Name} was successful." );
                }
            }

            return true;
        }

        public bool Test( BuildContext context, TestOptions options )
        {
            if ( !options.NoDependencies && !this.Build( context, (BuildOptions) options.WithIncludeTests( true ) ) )
            {
                return false;
            }

            ImmutableDictionary<string, string> properties;
            var testResultsDir = Path.Combine( context.RepoDirectory, "TestResults" );

            if ( options.AnalyzeCoverage )
            {
                // Removing the TestResults directory so that we reset the code coverage information.
                if ( Directory.Exists( testResultsDir ) )
                {
                    Directory.Delete( testResultsDir, true );
                }

                properties = options.AnalyzeCoverage
                    ? ImmutableDictionary.Create<string, string>()
                        .Add( "CollectCoverage", "True" )
                        .Add( "CoverletOutput", testResultsDir + "\\" )
                    : ImmutableDictionary<string, string>.Empty;
            }
            else
            {
                properties = ImmutableDictionary<string, string>.Empty;
            }

            foreach ( var solution in this.Solutions )
            {
                var solutionOptions = options;

                if ( options.AnalyzeCoverage && solution.SupportsTestCoverage )
                {
                    solutionOptions = (TestOptions) options.WithAdditionalProperties( properties ).WithoutConcurrency();
                }

                context.Console.WriteHeading( $"Testing {solution.Name}." );

                if ( !solution.Test( context, solutionOptions ) )
                {
                    return false;
                }

                context.Console.WriteSuccess( $"Testing {solution.Name} was successful" );
            }

            if ( options.AnalyzeCoverage )
            {
                if ( !AnalyzeCoverageCommand.Execute(
                    context.Console,
                    new AnalyzeCoverageSettings { Path = Path.Combine( testResultsDir, "coverage.net5.0.json" ) } ) )
                {
                    return false;
                }
            }

            context.Console.WriteSuccess( $"Testing {this.ProductName} was successful" );

            return true;
        }

        public bool Prepare( BuildContext context, BaseBuildSettings options )
        {
            if ( !options.NoDependencies )
            {
                this.Clean( context, options );
            }

            var (mainVersion, mainPackageVersionSuffix) =
                ReadMainVersionFile( Path.Combine( context.RepoDirectory, this.EngineeringDirectory, "MainVersion.props" ) );

            context.Console.WriteHeading( "Preparing the version file" );

            var configuration = options.BuildConfiguration.ToString().ToLowerInvariant();

            var versionPrefix = mainVersion;
            int patchNumber;
            string versionSuffix;

            switch ( options.VersionSpec.Kind )
            {
                case VersionKind.Local:
                    {
                        // Local build with timestamp-based version and randomized package number. For the assembly version we use a local incremental file stored in the user profile.
                        var localVersionDirectory =
                            Environment.ExpandEnvironmentVariables( "%APPDATA%\\Caravela.Engineering" );

                        var localVersionFile = $"{localVersionDirectory}\\{this.ProductName}.version";
                        int localVersion;

                        if ( File.Exists( localVersionFile ) )
                        {
                            localVersion = int.Parse( File.ReadAllText( localVersionFile ), CultureInfo.InvariantCulture ) + 1;
                        }
                        else
                        {
                            localVersion = 1;
                        }

                        if ( localVersion < 1000 )
                        {
                            localVersion = 1000;
                        }

                        if ( !Directory.Exists( localVersionDirectory ) )
                        {
                            Directory.CreateDirectory( localVersionDirectory );
                        }

                        File.WriteAllText( localVersionFile, localVersion.ToString( CultureInfo.InvariantCulture ) );

                        versionSuffix =
                            $"local-{Environment.UserName}-{configuration}";

                        patchNumber = localVersion;

                        break;
                    }

                case VersionKind.Numbered:
                    {
                        // Build server build with a build number given by the build server
                        patchNumber = options.VersionSpec.Number;
                        versionSuffix = $"dev-{configuration}";

                        break;
                    }

                case VersionKind.Public:
                    // Public build
                    versionSuffix = mainPackageVersionSuffix.TrimStart( '-' );
                    patchNumber = 0;

                    break;

                default:
                    throw new InvalidOperationException();
            }

            var privateArtifactsRelativeDir = this.PrivateArtifactsDirectory.ToString( new VersionInfo( null!, options.BuildConfiguration.ToString() ) );
            var artifactsDir = Path.Combine( context.RepoDirectory, privateArtifactsRelativeDir );

            if ( !Directory.Exists( artifactsDir ) )
            {
                Directory.CreateDirectory( artifactsDir );
            }

            var props = this.GenerateVersionFile( versionPrefix, patchNumber, versionSuffix, configuration );
            var propsFileName = $"{this.ProductName}.version.props";
            var propsFilePath = Path.Combine( artifactsDir, propsFileName );

            context.Console.WriteMessage( $"Writing '{propsFilePath}'." );
            File.WriteAllText( propsFilePath, props );

            // Write a link to this file in the root file of the repo. This file is the interface of the repo, which can be imported by other repos.
            var importFileContent = $@"
<Project>
    <!-- This file must not be added to source control and must not be uploaded as a build artifact.
         It must be imported by other repos as a dependency. 
         Dependent projects should not directly reference the artifacts path, which is considered an implementation detail. -->
    <Import Project=""{Path.Combine( privateArtifactsRelativeDir, propsFileName )}""/>
</Project>
";

            File.WriteAllText( Path.Combine( context.RepoDirectory, this.ProductName + ".Import.props" ), importFileContent );

            context.Console.WriteSuccess(
                $"Preparing the version file was successful. {this.ProductNameWithoutDot}Version={this.ReadGeneratedVersionFile( context.GetVersionFilePath( options.BuildConfiguration ) ).PackageVersion}" );

            return true;
        }

        protected virtual string GenerateVersionFile( string versionPrefix, int patchNumber, string versionSuffix, string configuration )
        {
            var props = $@"
<!-- This file is generated by the engineering tooling -->
<Project>
    <PropertyGroup>";

            if ( this.GenerateArcadeProperties )
            {
                // Caravela.Compiler, because of Arcade, requires the version number to be decomposed in a prefix, patch number, and suffix.
                // In Arcade, the package naming scheme is different because the patch number is not a part of the package name.

                var arcadeSuffix = "";

                if ( !string.IsNullOrEmpty( versionSuffix ) )
                {
                    arcadeSuffix += versionSuffix;
                }

                if ( patchNumber > 0 )
                {
                    if ( arcadeSuffix.Length > 0 )
                    {
                        arcadeSuffix += "-";
                    }
                    else
                    {
                        // It should not happen that we have a patch number without a suffix.
                        arcadeSuffix += "-patch-" + configuration;
                    }

                    arcadeSuffix += patchNumber;
                }

                var packageSuffix = string.IsNullOrEmpty( arcadeSuffix ) ? "" : "-" + arcadeSuffix;

                props += $@"
        <{this.ProductNameWithoutDot}VersionPrefix>{versionPrefix}</{this.ProductNameWithoutDot}VersionPrefix>
        <{this.ProductNameWithoutDot}VersionSuffix>{arcadeSuffix}</{this.ProductNameWithoutDot}VersionSuffix>
        <{this.ProductNameWithoutDot}VersionPatchNumber>{patchNumber}</{this.ProductNameWithoutDot}VersionPatchNumber>
        <{this.ProductNameWithoutDot}Version>{versionPrefix}{packageSuffix}</{this.ProductNameWithoutDot}Version>
        <{this.ProductNameWithoutDot}AssemblyVersion>{versionPrefix}.{patchNumber}</{this.ProductNameWithoutDot}AssemblyVersion>
        <{this.ProductNameWithoutDot}BuildConfiguration>{configuration}</{this.ProductNameWithoutDot}BuildConfiguration>";
            }
            else
            {
                var packageSuffix = string.IsNullOrEmpty( versionSuffix ) ? "" : "-" + versionSuffix;

                props += $@"
        <{this.ProductNameWithoutDot}Version>{versionPrefix}.{patchNumber}{packageSuffix}</{this.ProductNameWithoutDot}Version>
        <{this.ProductNameWithoutDot}AssemblyVersion>{versionPrefix}.{patchNumber}</{this.ProductNameWithoutDot}AssemblyVersion>
        <{this.ProductNameWithoutDot}BuildConfiguration>{configuration}</{this.ProductNameWithoutDot}BuildConfiguration>";
            }

            props += $@"
    </PropertyGroup>
    <PropertyGroup>
        <!-- Adds the local output directories as nuget sources for referencing projects. -->
        <RestoreAdditionalProjectSources>$(RestoreAdditionalProjectSources);$(MSBuildThisFileDirectory)</RestoreAdditionalProjectSources>
    </PropertyGroup>
</Project>
";

            return props;
        }

        public void Clean( BuildContext context, BaseBuildSettings options )
        {
            void DeleteDirectory( string directory )
            {
                if ( Directory.Exists( directory ) )
                {
                    context.Console.WriteMessage( $"Deleting directory '{directory}'." );
                    Directory.Delete( directory, true );
                }
            }

            void CleanRecursive( string directory )
            {
                DeleteDirectory( Path.Combine( directory, "bin" ) );
                DeleteDirectory( Path.Combine( directory, "obj" ) );

                foreach ( var subdirectory in Directory.EnumerateDirectories( directory ) )
                {
                    if ( subdirectory == Path.Combine( context.RepoDirectory, this.EngineeringDirectory ) )
                    {
                        // Skip the engineering directory.
                        continue;
                    }

                    CleanRecursive( subdirectory );
                }
            }

            context.Console.WriteHeading( $"Cleaning {this.ProductName}." );

            foreach ( var directory in this.AdditionalDirectoriesToClean )
            {
                DeleteDirectory( Path.Combine( context.RepoDirectory, directory ) );
            }

            var stringParameters = new VersionInfo( options.BuildConfiguration.ToString(), null! );

            DeleteDirectory( Path.Combine( context.RepoDirectory, this.PrivateArtifactsDirectory.ToString( stringParameters ) ) );
            DeleteDirectory( Path.Combine( context.RepoDirectory, this.PublicArtifactsDirectory.ToString( stringParameters ) ) );
            CleanRecursive( context.RepoDirectory );
        }

        public bool Publish( BuildContext context, PublishOptions options )
        {
            context.Console.WriteHeading( "Publishing files" );

            var versionFile = this.ReadGeneratedVersionFile( context.GetVersionFilePath( options.BuildConfiguration ) );

            var stringParameters = new VersionInfo( versionFile.Configuration, versionFile.PackageVersion );

            var hasTarget = false;

            if ( !this.PublishDirectory(
                context,
                options,
                versionFile,
                Path.Combine( context.RepoDirectory, this.PrivateArtifactsDirectory.ToString( stringParameters ) ),
                false,
                ref hasTarget ) )
            {
                return false;
            }

            if ( options.Public )
            {
                if ( !this.PublishDirectory(
                    context,
                    options,
                    versionFile,
                    Path.Combine( context.RepoDirectory, this.PublicArtifactsDirectory.ToString( stringParameters ) ),
                    true,
                    ref hasTarget ) )
                {
                    return false;
                }
            }

            if ( !hasTarget )
            {
                context.Console.WriteWarning( "No active publishing target was detected." );
            }
            else
            {
                context.Console.WriteSuccess( "Publishing has succeeded." );
            }

            return true;
        }

        private bool PublishDirectory(
            BuildContext context,
            PublishOptions options,
            VersionInfo versionFile,
            string directory,
            bool isPublic,
            ref bool hasTarget )
        {
            var success = true;

            var stringArguments = new VersionInfo( versionFile.Configuration, versionFile.PackageVersion );

            foreach ( var publishingTarget in this.PublishingTargets )
            {
                var matcher = new Matcher( StringComparison.OrdinalIgnoreCase );

                if ( (publishingTarget.SupportsPrivatePublishing && !isPublic) ||
                     (publishingTarget.SupportsPublicPublishing && isPublic) )
                {
                    hasTarget = true;

                    publishingTarget.Artifacts.AddToMatcher( matcher, stringArguments );
                }

                var matchingResult =
                    matcher.Execute( new DirectoryInfoWrapper( new DirectoryInfo( directory ) ) );

                foreach ( var file in matchingResult.Files )
                {
                    if ( Path.GetExtension( file.Path ).Equals( publishingTarget.MainExtension, StringComparison.OrdinalIgnoreCase ) )
                    {
                        if ( file.Path.Contains( "-local-", StringComparison.OrdinalIgnoreCase ) )
                        {
                            context.Console.WriteError( "Cannot publish a local build." );

                            return false;
                        }

                        switch ( publishingTarget.Execute(
                            context,
                            options,
                            Path.Combine( directory, file.Path ),
                            isPublic ) )
                        {
                            case SuccessCode.Success:
                                break;

                            case SuccessCode.Error:
                                success = false;

                                break;

                            case SuccessCode.Fatal:
                                return false;
                        }
                    }
                }
            }

            if ( !success )
            {
                context.Console.WriteError( "Publishing has failed." );
            }

            return success;
        }
    }
}