// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal sealed class LoggerOptionsStorage
    {
        public static readonly Option2<bool> EtwLoggerKey = new("visual_studio_etw_logger_key", defaultValue: true);
        public static readonly Option2<bool> TraceLoggerKey = new("visual_studio_trace_logger_key", defaultValue: false);
        public static readonly Option2<bool> OutputWindowLoggerKey = new("visual_studio_output_window_logger_key", defaultValue: false);
    }
}
