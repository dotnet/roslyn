// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using System;
using System.Text;

namespace PostSharp.Engineering.BuildTools.Utilities
{
    public static class DotNetHelper
    {
        public static bool Run( BuildContext context, BaseBuildSettings options, string solution, string command, string arguments = "" )
        {
            var argsBuilder = new StringBuilder();

            argsBuilder.Append( $"{command} -p:Configuration={options.BuildConfiguration} \"{solution}\" -v:{options.Verbosity.ToAlias()} --nologo" );

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
                "dotnet",
                argsBuilder.ToString(),
                Environment.CurrentDirectory );
        }
    }
}