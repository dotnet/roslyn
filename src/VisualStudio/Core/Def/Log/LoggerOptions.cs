// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal sealed class LoggerOptions
    {
        public static readonly Option2<bool> EtwLoggerKey = new("dotnet_logger_options_etw_logger_key", defaultValue: true);
        public static readonly Option2<bool> TraceLoggerKey = new("dotnet_logger_options_trace_logger_key", defaultValue: false);
        public static readonly Option2<bool> OutputWindowLoggerKey = new("dotnet_logger_options_output_window_logger_key", defaultValue: false);
    }
}
