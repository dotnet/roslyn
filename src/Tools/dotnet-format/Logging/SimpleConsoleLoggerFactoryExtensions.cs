// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace Microsoft.CodeAnalysis.Tools.Logging
{
    internal static class SimpleConsoleLoggerFactoryExtensions
    {
        public static ILoggerFactory AddSimpleConsole(this ILoggerFactory factory, IConsole console, LogLevel logLevel)
        {
            factory.AddProvider(new SimpleConsoleLoggerProvider(console, logLevel));
            return factory;
        }
    }
}
