// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using PostSharp.Engineering.BuildTools.Utilities;
using System;

namespace PostSharp.Engineering.BuildTools.Build.Model
{
    public class NugetPublishTarget : PublishingTarget
    {
        public NugetSource PrivateSource { get; }

        public NugetSource? PublicSource { get; }

        public NugetPublishTarget( Pattern packages, NugetSource privateSource, NugetSource? publicSource = null )
        {
            this.Artifacts = packages;
            this.PrivateSource = privateSource;
            this.PublicSource = publicSource;
        }

        public override bool SupportsPublicPublishing => this.PublicSource != null;

        public override bool SupportsPrivatePublishing => true;

        public override string MainExtension => ".nupkg";

        public override Pattern Artifacts { get; }

        public override SuccessCode Execute( BuildContext context, PublishOptions options, string file, bool isPublic )
        {
            var source = isPublic ? this.PublicSource : this.PrivateSource;

            if ( source == null )
            {
                throw new InvalidOperationException();
            }

            var hasEnvironmentError = false;

            context.Console.WriteMessage( $"Publishing {file}." );

            var server = Environment.ExpandEnvironmentVariables( source.Source );

            // Check if environment variables have been defined.
            if ( string.IsNullOrEmpty( server ) )
            {
                context.Console.WriteError( $"The {source.Source} environment variable is not defined." );
                hasEnvironmentError = true;
            }

            var apiKey = Environment.ExpandEnvironmentVariables( source.ApiKey );

            if ( string.IsNullOrEmpty( apiKey ) )
            {
                context.Console.WriteError( $"The {source.ApiKey} environment variable is not defined." );
                hasEnvironmentError = true;
            }

            if ( hasEnvironmentError )
            {
                return SuccessCode.Fatal;
            }

            // Note that we don't expand the ApiKey environment variable so we don't expose passwords to logs.
            var arguments =
                $"nuget push {file} --source {server} --api-key {Environment.ExpandEnvironmentVariables( source.ApiKey )} --skip-duplicate";

            if ( options.Dry )
            {
                context.Console.WriteImportantMessage( "Dry run: dotnet " + arguments );

                return SuccessCode.Success;
            }
            else
            {
                return ToolInvocationHelper.InvokeTool(
                    context.Console,
                    "dotnet",
                    arguments,
                    Environment.CurrentDirectory )
                    ? SuccessCode.Success
                    : SuccessCode.Error;
            }
        }
    }
}