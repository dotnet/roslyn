// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Lsif.Generator
{
    internal static class Program
    {
        public static Task Main(string[] args)
        {
            var generateCommand = new RootCommand("generates an LSIF file")
            {
                new Option("--output", "file to write the LSIF output to, instead of the console") { Argument = new Argument<string?>(defaultValue: () => null).LegalFilePathsOnly() },
                new Option("--log", "file to write a log to") { Argument = new Argument<string?>(defaultValue: () => null).LegalFilePathsOnly() }
            };

            generateCommand.Handler = CommandHandler.Create((Func<string?, string?, Task>)GenerateAsync);

            return generateCommand.InvokeAsync(args);
        }

        private static async Task GenerateAsync(string? output, string? log)
        {
            // If we have an output file, we'll write to that, else we'll use Console.Out
            using StreamWriter? outputFile = output != null ? new StreamWriter(output) : null;
            TextWriter outputWriter = outputFile ?? Console.Out;

            using TextWriter logFile = log != null ? new StreamWriter(log) : TextWriter.Null;

            try
            {
                await GenerateAsync(outputWriter, logFile);
            }
            catch (Exception e)
            {
                // If it failed, write out to the logs and error, but propagate the error too
                var message = "Unhandled exception: " + e.ToString();
                await logFile.WriteLineAsync(message);
                Console.Error.WriteLine(message);
                throw;
            }

            await logFile.WriteLineAsync("Generation complete.");
        }

        private static Task GenerateAsync(TextWriter outputWriter, TextWriter logFile)
        {
            return Task.CompletedTask;
        }
    }
}
