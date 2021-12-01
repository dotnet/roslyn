// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using PostSharp.Engineering.BuildTools.Utilities;
using System;

namespace PostSharp.Engineering.BuildTools.Build.Model
{
    public class VsixPublishTarget : PublishingTarget
    {
        public VsixPublishTarget( Pattern artifacts )
        {
            this.Artifacts = artifacts;
        }

        public override bool SupportsPublicPublishing => true;

        public override bool SupportsPrivatePublishing => false;

        public override string MainExtension => ".vsix";

        public override Pattern Artifacts { get; }

        public override SuccessCode Execute( BuildContext context, PublishOptions options, string file, bool isPublic )
        {
            var hasEnvironmentError = false;

            if ( string.IsNullOrEmpty( Environment.GetEnvironmentVariable( "VSSDKINSTALL" ) ) )
            {
                context.Console.WriteError( $"The VSSDKINSTALL environment variable is not defined." );
                hasEnvironmentError = true;
            }

            if ( string.IsNullOrEmpty( Environment.GetEnvironmentVariable( "VS_MARKETPLACE_ACCESS_TOKEN" ) ) )
            {
                context.Console.WriteError( $"The VS_MARKETPLACE_ACCESS_TOKEN environment variable is not defined." );
                hasEnvironmentError = true;
            }

            if ( hasEnvironmentError )
            {
                return SuccessCode.Fatal;
            }

            var vsSdkDir = Environment.GetEnvironmentVariable( "VSSDKINSTALL" );

            var exe = $@"{vsSdkDir}\VisualStudioIntegration\Tools\Bin\VsixPublisher.exe";

            var args =
                $" publish -payload \"{file}\" -publishManifest \"{file}.json\" -personalAccessToken \"%VS_MARKETPLACE_ACCESS_TOKEN%\"";

            if ( options.Dry )
            {
                context.Console.WriteImportantMessage( $"Dry run: {exe} " + args );

                return SuccessCode.Success;
            }
            else
            {
                return ToolInvocationHelper.InvokeTool(
                    context.Console,
                    exe,
                    args,
                    Environment.CurrentDirectory )
                    ? SuccessCode.Error
                    : SuccessCode.Fatal;
            }
        }
    }
}