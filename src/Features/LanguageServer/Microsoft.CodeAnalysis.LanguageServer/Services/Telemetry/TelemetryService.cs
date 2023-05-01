// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.LanguageServer.Services.Telemetry
{
    internal class TelemetryService : IDisposable
    {
        private const string AssemblyName = "Microsoft.VisualStudio.LanguageServices.DevKit.dll";
        private const string ClassName = "Microsoft.VisualStudio.LanguageServices.DevKit.VSCodeTelemetryLogger";

        private readonly AssemblyLoadContextWrapper _alcWrapper;
        private readonly Lazy<MethodInfo> _logMethod;
        private readonly Lazy<MethodInfo> _logBlockStartMethod;
        private readonly Lazy<MethodInfo> _logBlockEndMethod;
        private readonly Lazy<MethodInfo> _reportFaultMethod;

        public TelemetryService(AssemblyLoadContextWrapper alcWrapper)
        {
            _alcWrapper = alcWrapper;
            _logMethod = new(() => alcWrapper.GetMethodInfo(AssemblyName, ClassName, "Log"));
            _logBlockStartMethod = new(() => alcWrapper.GetMethodInfo(AssemblyName, ClassName, "LogBlockStart"));
            _logBlockEndMethod = new(() => alcWrapper.GetMethodInfo(AssemblyName, ClassName, "LogBlockEnd"));
            _reportFaultMethod = new(() => alcWrapper.GetMethodInfo(AssemblyName, ClassName, "ReportFault"));
        }

        public static TelemetryService? TryCreate(string telemetryLevel, string location)
        {
            var alcService = AssemblyLoadContextWrapper.TryCreate("RoslynVSCodeTelemetry", location, logger: null);
            if (alcService is null)
            {
                return null;
            }

            var initializeMethod = alcService.TryGetMethodInfo(AssemblyName, ClassName, "Initialize");
            if (initializeMethod is null)
            {
                return null;
            }

            initializeMethod.Invoke(null, new object[] { telemetryLevel });
            return new(alcService);
        }

        public void Dispose()
        {
            _alcWrapper.Dispose();
        }

        internal void Log(string name, ImmutableDictionary<string, object?> properties)
        {
            _logMethod.Value.Invoke(null, new object[] { name, properties });
        }

        internal void LogBlockStart(string eventName, int kind, int blockId)
        {
            _logBlockStartMethod.Value.Invoke(null, new object[] { eventName, kind, blockId });
        }

        internal void LogBlockEnd(int blockId, ImmutableDictionary<string, object?> properties, CancellationToken cancellationToken)
        {
            _logBlockEndMethod.Value.Invoke(null, new object[] { blockId, properties, cancellationToken });
        }

        internal void ReportFault(string eventName, string description, int logLevel, bool forceDump, int processId, Exception exception)
        {
            _reportFaultMethod.Value.Invoke(null, new object[] { eventName, description, logLevel, forceDump, processId, exception });
        }
    }
}
