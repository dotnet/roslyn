// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Instrumentation
{
    /// <summary>
    /// This EventSource exposes our events to ETW.
    /// RoslynCompilerEventSource GUID is {9f93daf9-7fee-5301-ebea-643b538889b4}.
    /// CodeSense.RoslynCompilerEventSource GUID is {08e567fa-f66d-52c7-4e58-d802264cc8db}.
    /// </summary>
#if CODESENSE
    [EventSource(Name = "CodeSense.RoslynCompilerEventSource")]
#else
    [EventSource(Name = "RoslynCompilerEventSource")]
#endif
    internal sealed class RoslynCompilerEventSource : EventSource
    {
        // We might not be "enabled" but we always have this singleton alive.
        public static readonly RoslynCompilerEventSource Instance = new RoslynCompilerEventSource();

        private readonly bool initialized = false;

        private RoslynCompilerEventSource()
        {
            this.initialized = true;
        }

        // Do not change the parameter order for the below methods.
        // The parameter order for each method below must match the parameter order
        // for the WriteEvent() overload that's being invoked inside the method.
        // This is necessary because the ETW schema generation process will reflect
        // over this to determine how to pack event data. If the orders are different,
        // the generated schema will be wrong, and any perf tools will deserialize
        // event data fields in the wrong order.
        //
        // The WriteEvent() overloads are optimized for either:
        //     Up to 3 integer parameters
        //     1 string and up to 2 integer parameters
        // There's also a params object[] overload that is much slower and should be avoided.
        [Event(1)]
        public void LogString(string message, FunctionId functionId)
        {
            WriteEvent(1, message ?? string.Empty, (int)functionId);
        }

        [Event(2)]
        public void BlockStart(string message, FunctionId functionId, int blockId)
        {
            WriteEvent(2, message ?? string.Empty, (int)functionId, blockId);
        }

        [Event(3)]
        public void BlockStop(FunctionId functionId, int number, int blockId)
        {
            WriteEvent(3, (int)functionId, number, blockId);
        }

        [Event(4)]
        public void BlockCanceled(FunctionId functionId, int number, int blockId)
        {
            WriteEvent(4, (int)functionId, number, blockId);
        }

        [Event(5)]
        public void SendFunctionDefinitions(string definitions)
        {
            WriteEvent(5, definitions);
        }

        [NonEvent]
        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            base.OnEventCommand(command);

            if (command.Command == EventCommand.SendManifest ||
                command.Command != EventCommand.Disable ||
                FunctionDefinitionRequested(command))
            {
                // Use helper functions rather than lambdas since the auto-generated manifest
                // doesn't know what to do with compiler generated methods.
                // Here, we use Task to make things run in a background thread.
                if (initialized)
                {
                    SendFunctionDefinitionsAsync();
                }
                else
                {
                    // We're still in the constructor - need to defer sending until we've finished initializing.
                    Task.Yield().GetAwaiter().OnCompleted(SendFunctionDefinitionsAsync);
                }
            }
        }

        [NonEvent]
        private bool FunctionDefinitionRequested(EventCommandEventArgs command)
        {
            return command.Arguments != null &&
                   command.Arguments.Keys.FirstOrDefault() == "SendFunctionDefinitions";
        }

        [NonEvent]
        private void SendFunctionDefinitionsAsync()
        {
            Task.Run((Action)SendFunctionDefinitions);
        }

        [NonEvent]
        private void SendFunctionDefinitions()
        {
            SendFunctionDefinitions(GenerateFunctionDefinitions());
        }

        [NonEvent]
        public static string GenerateFunctionDefinitions()
        {
            // Gets called a couple of times per session, no need to cache data or optimize.

            var output = new StringBuilder();
            var assembly = typeof(FunctionId).GetTypeInfo().Assembly;
            output.AppendLine(assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);

            var functionIds = from field in typeof(FunctionId).GetTypeInfo().DeclaredFields
                              where !field.IsSpecialName && field.Name != "Count"
                              select KeyValuePair.Create(field.Name, (int)field.GetValue(null));

            foreach (var function in functionIds)
            {
                output.Append(function.Value);
                output.Append(' ');
#if DEBUG
                output.Append("Debug_");
#endif
                output.Append(function.Key);
                output.Append(' ');
                output.AppendLine(PerformanceGoals.Goals[function.Value] ?? PerformanceGoals.Undefined);
            }

            // Note that changing the format of this output string will break any ETW listeners
            // that don't have a direct reference to Roslyn.Compilers.dll.
            return output.ToString();
        }
    }
}