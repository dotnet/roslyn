// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Microsoft.RoslynTools.Logging;

public static class ConsoleLoggerExtensions
{
    public static ILoggingBuilder AddSimplerFormatter(this ILoggingBuilder builder, Action<ConsoleFormatterOptions> configure)
        => builder.AddConsole(options => options.FormatterName = "simpler")
            .AddConsoleFormatter<SimplerConsoleFormatter, ConsoleFormatterOptions>(configure);
}
