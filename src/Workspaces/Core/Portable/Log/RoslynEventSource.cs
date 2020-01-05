// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// This EventSource exposes our events to ETW.
    /// RoslynEventSource GUID is {bf965e67-c7fb-5c5b-d98f-cdf68f8154c2}.
    /// 
    /// When updating this class, use the following to also update Main\Source\Test\Performance\Log\RoslynEventSourceParser.cs:
    /// Main\Tools\Source\TraceParserGen\bin\Debug\TraceParserGen.exe Microsoft.CodeAnalysis.Workspaces.dll -eventsource:RoslynEventSource
    /// 
    /// Use this command to register the ETW manifest on any machine where you need to decode events in xperf/etlstackbrowse:
    /// "\\clrmain\tools\managed\etw\eventRegister\bin\Debug\eventRegister.exe" Microsoft.CodeAnalysis.Workspaces.dll
    /// </summary>
    [EventSource(Name = "RoslynEventSource")]
    internal sealed partial class RoslynEventSource : EventSource
    {
        // might not "enabled" but we always have this singleton alive
        public static readonly RoslynEventSource Instance = new RoslynEventSource();

        private readonly bool _initialized;
        private RoslynEventSource()
        {
            _initialized = true;
        }

        // Do not change the parameter order for this method: it must match the parameter order
        // for WriteEvent() that's being invoked inside it. This is necessary because the ETW schema
        // generation process will reflect over WriteRoslynEvent() to determine how to pack event
        // data. If the orders are different, the generated schema will be wrong, and any perf
        // tools will deserialize event data fields in the wrong order.
        //
        // The WriteEvent overloads are optimized for either:
        //     Up to 3 integer parameters
        //     1 string and up to 2 integer parameters
        // There's also a params object[] overload that is much slower and should be avoided
        [Event(1)]
        public void Log(string message, FunctionId functionId)
        {
            WriteEvent(1, message ?? string.Empty, (int)functionId);
        }

        [Event(2)]
        public void BlockStart(string message, FunctionId functionId, int blockId)
        {
            WriteEvent(2, message ?? string.Empty, (int)functionId, blockId);
        }

        [Event(3)]
        public void BlockStop(FunctionId functionId, int tick, int blockId)
        {
            WriteEvent(3, (int)functionId, tick, blockId);
        }

        [Event(4)]
        public void SendFunctionDefinitions(string definitions)
        {
            WriteEvent(4, definitions);
        }

        [Event(5)]
        public void BlockCanceled(FunctionId functionId, int tick, int blockId)
        {
            WriteEvent(5, (int)functionId, tick, blockId);
        }

        [NonEvent]
        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            base.OnEventCommand(command);

            if (command.Command == EventCommand.SendManifest ||
                command.Command != EventCommand.Disable ||
                FunctionDefinitionRequested(command))
            {
                if (!_initialized)
                {
                    // We're still in the constructor, need to defer sending until we've finished initializing
                    Task.Yield().GetAwaiter().OnCompleted(SendFunctionDefinitionsAsync);
                    return;
                }

                SendFunctionDefinitions();
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
            var output = new StringBuilder();

            var functions = from f in typeof(FunctionId).GetFields()
                            where !f.IsSpecialName
                            select f;

            var assembly = typeof(RoslynEventSource).Assembly;
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            output.AppendLine(fvi.ProductVersion);

            foreach (var function in functions)
            {
                var value = (int)function.GetRawConstantValue();
#if DEBUG
                var name = "Debug_" + function.Name;
#else
                var name = function.Name;
#endif
                var goal = (from attr in function.GetCustomAttributes(false)
                            where attr is PerfGoalAttribute
                            select ((PerfGoalAttribute)attr).InteractionClass).DefaultIfEmpty(InteractionClass.Undefined).First();

                output.Append(value);
                output.Append(" ");
                output.Append(name);
                output.Append(" ");
                output.AppendLine(goal.ToString());
            }

            // Note that changing the format of this output string will break any ETW listeners
            // that don't have a direct reference to Microsoft.CodeAnalysis.Workspaces.dll
            return output.ToString();
        }
    }
}
