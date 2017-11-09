// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;

namespace AnalyzerRunner
{
    class TelemetryCollector : Dictionary<string, List<AnalyzerTelemetryInfo>>
    {
        public void WriteTelemetry()
        {
            foreach (var analyzerPair in this)
            {
                var aggregatedTelemetry = CreateAggregatedTelemetry(analyzerPair.Value);
                WriteTelemetry(analyzerPair.Key, aggregatedTelemetry);
            }
        }

        private static AnalyzerTelemetryInfo CreateAggregatedTelemetry(List<AnalyzerTelemetryInfo> list)
        {
            var result = new AnalyzerTelemetryInfo();
            foreach (var info in list)
            {
                result.CodeBlockActionsCount += info.CodeBlockActionsCount;
                result.CodeBlockEndActionsCount += info.CodeBlockEndActionsCount;
                result.CodeBlockStartActionsCount += info.CodeBlockStartActionsCount;
                result.CompilationActionsCount += info.CompilationActionsCount;
                result.CompilationEndActionsCount += info.CompilationEndActionsCount;
                result.CompilationStartActionsCount += info.CompilationStartActionsCount;
                result.ExecutionTime += info.ExecutionTime;
                result.OperationActionsCount += info.OperationActionsCount;
                result.OperationBlockActionsCount += info.OperationBlockActionsCount;
                result.OperationBlockEndActionsCount += info.OperationBlockEndActionsCount;
                result.OperationBlockStartActionsCount += info.OperationBlockStartActionsCount;
                result.SemanticModelActionsCount += info.SemanticModelActionsCount;
                result.SymbolActionsCount += info.SymbolActionsCount;
                result.SyntaxNodeActionsCount += info.SyntaxNodeActionsCount;
                result.SyntaxTreeActionsCount += info.SyntaxTreeActionsCount;
            }

            return result;
        }

        private void WriteTelemetry(string analyzerName, AnalyzerTelemetryInfo telemetry)
        {
            Utilities.WriteLine($"Statistics for {analyzerName}:", ConsoleColor.DarkCyan);
            Utilities.WriteLine($"Execution time (ms):            {telemetry.ExecutionTime.TotalMilliseconds}", ConsoleColor.White);

            Utilities.WriteLine($"Code Block Actions:             {telemetry.CodeBlockActionsCount}", ConsoleColor.White);
            Utilities.WriteLine($"Code Block Start Actions:       {telemetry.CodeBlockStartActionsCount}", ConsoleColor.White);
            Utilities.WriteLine($"Code Block End Actions:         {telemetry.CodeBlockEndActionsCount}", ConsoleColor.White);

            Utilities.WriteLine($"Compilation Actions:            {telemetry.CompilationActionsCount}", ConsoleColor.White);
            Utilities.WriteLine($"Compilation Start Actions:      {telemetry.CompilationStartActionsCount}", ConsoleColor.White);
            Utilities.WriteLine($"Compilation End Actions:        {telemetry.CompilationEndActionsCount}", ConsoleColor.White);

            Utilities.WriteLine($"Operation Actions:              {telemetry.OperationActionsCount}", ConsoleColor.White);
            Utilities.WriteLine($"Operation Block Actions:        {telemetry.OperationBlockActionsCount}", ConsoleColor.White);
            Utilities.WriteLine($"Operation Block Start Actions:  {telemetry.OperationBlockStartActionsCount}", ConsoleColor.White);
            Utilities.WriteLine($"Operation Block End Actions:    {telemetry.OperationBlockEndActionsCount}", ConsoleColor.White);

            Utilities.WriteLine($"Semantic Model Actions:         {telemetry.SemanticModelActionsCount}", ConsoleColor.White);
            Utilities.WriteLine($"Symbol Actions:                 {telemetry.SymbolActionsCount}", ConsoleColor.White);
            Utilities.WriteLine($"Syntax Node Actions:            {telemetry.SyntaxNodeActionsCount}", ConsoleColor.White);
            Utilities.WriteLine($"Syntax Tree Actions:            {telemetry.SyntaxTreeActionsCount}", ConsoleColor.White);
        }
    }
}
