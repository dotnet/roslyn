// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Tasks.Hosting;
using Microsoft.CodeAnalysis.CompilerServer;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    internal static class TaskUtilities
    {
        internal static int ExecuteTool(BuildRequest request, CancellationToken cancellationToken, out bool canceled, out string output, out string errorOutput)
        {
            canceled = false;

            BuildClient client = new BuildClient();
            Task<BuildResponse> responseTask = client.GetResponseAsync(request, cancellationToken);

            BuildResponse response = null;
            try
            {
                responseTask.Wait(cancellationToken);
                response = responseTask.Result;
            }
            catch (OperationCanceledException)
            {
                canceled = true;
                output = null;
                errorOutput = null;
                return 0;
            }
            catch (AggregateException ae)
            {
                CompilerServerLogger.LogException(ae, "Unexpected failure.");
                foreach (var e in ae.InnerExceptions)
                {
                    CompilerServerLogger.LogException(e, "");
                }
            }

            if (response == null)
            {
                output = null;
                errorOutput = "Fatal Error: Please see the event log for more details.";
                return -1;
            }

            output = response.Output;
            errorOutput = response.ErrorOutput;
            return response.ReturnCode;
        }

        /// <summary>
        /// If not design time build and the globalSessionGuid property was set then add a /globalsessionguid:{guid}
        /// </summary>
        internal static string AppendSessionGuidUnlessDesignTime(string response, string vsSessionGuid, Build.Utilities.Task task)
        {
            if (string.IsNullOrEmpty(vsSessionGuid)) return response;

            var hostObject = task.HostObject;
            if (hostObject == null) return response;

            var csHost = hostObject as ICscHostObject;
            bool isDesignTime = (csHost != null && csHost.IsDesignTime());
            if (!isDesignTime)
            {
                var vbHost = hostObject as IVbcHostObject;
                isDesignTime = (vbHost != null && vbHost.IsDesignTime());
            }

            return isDesignTime ? response : string.Format("{0} /sqmsessionguid:{1}", response, vsSessionGuid);
        }

        /// <summary>
        /// Create the compilation request to send to the server process.
        /// </summary>
        internal static BuildRequest CreateRequest(uint requestId, string rawWorkingDirectory, string[] rawEnvironmentVariables, string rawCommandLineCommands, string rawResponseFileArguments)
        {
            string workingDirectory = CurrentDirectoryToUse(rawWorkingDirectory);
            string libDirectory = LibDirectoryToUse(rawEnvironmentVariables);
            string[] arguments = GetArguments(rawCommandLineCommands, rawResponseFileArguments);

            CompilerServerLogger.Log("BuildRequest: working directory='{0}'", workingDirectory);
            CompilerServerLogger.Log("BuildRequest: lib directory='{0}'", libDirectory);

            var requestArgs = ImmutableArray.CreateBuilder<BuildRequest.Argument>(arguments.Length + 1);

            requestArgs.Add(new BuildRequest.Argument(BuildProtocolConstants.ArgumentId_CurrentDirectory, 0, workingDirectory));

            if (libDirectory != null)
            {
                requestArgs.Add(new BuildRequest.Argument(BuildProtocolConstants.ArgumentId_LibEnvVariable, 0, libDirectory));
            }

            for (int i = 0; i < arguments.Length; ++i)
            {
                CompilerServerLogger.Log("BuildRequest: argument[{0}]='{1}'", i, arguments[i]);
                requestArgs.Add(new BuildRequest.Argument(BuildProtocolConstants.ArgumentId_CommandLineArgument, (uint)i, arguments[i]));
            }

            return new BuildRequest(requestId, requestArgs.ToImmutable());
        }


        /// <summary>
        /// Get the current directory that the compiler should run in.
        /// </summary>
        private static string CurrentDirectoryToUse(string workingDirectory)
        {
            // ToolTask has a method for this. But it may return null. Use the process directory
            // if ToolTask didn't override. MSBuild uses the process directory.
            return string.IsNullOrEmpty(workingDirectory)
                ? Environment.CurrentDirectory
                : workingDirectory;
        }

        /// <summary>
        /// Get the "LIB" environment variable, or NULL if none.
        /// </summary>
        private static string LibDirectoryToUse(string[] environmentVariables)
        {
            // First check the real environment.
            string libDirectory = Environment.GetEnvironmentVariable("LIB");

            // Now go through additional environment variables.
            if (environmentVariables != null)
            {
                foreach (string var in environmentVariables)
                {
                    if (var.StartsWith("LIB=", StringComparison.OrdinalIgnoreCase))
                    {
                        libDirectory = var.Substring(4);
                    }
                }
            }

            return libDirectory;
        }

        /// <summary>
        /// Get the command line arguments to pass to the compiler.
        /// </summary>
        /// <returns></returns>
        private static string[] GetArguments(string commandLineCommands, string responseFileCommands)
        {
            CompilerServerLogger.Log("CommandLine='{0}'", commandLineCommands);
            CompilerServerLogger.Log("BuildResponseFile='{0}'", responseFileCommands);

            string[] commandLineArguments = CommandLineSplitter.SplitCommandLine(commandLineCommands);
            string[] responseFileArguments = CommandLineSplitter.SplitCommandLine(responseFileCommands);

            int numCommandLineArguments = commandLineArguments.Length;
            int numResponseFileArguments = responseFileArguments.Length;
            string[] result = new string[numCommandLineArguments + numResponseFileArguments];
            Array.Copy(commandLineArguments, result, numCommandLineArguments);
            Array.Copy(responseFileArguments, 0, result, numCommandLineArguments, numResponseFileArguments);
            return result;
        }
    }
}
