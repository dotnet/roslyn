// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace TestRunner.Helix;

internal static class Program
{
    internal static async Task<int> Main(string[] args)
    {
        var options = Options.Parse(args, out var helpShown);
        if (options is null)
        {
            return helpShown ? 0 : 1;
        }

        ConsoleUtil.WriteLine($"Running '{options.DotnetFilePath} --version'..");
        var dotnetResult = await ProcessRunner.CreateProcess(options.DotnetFilePath, arguments: "--version", captureOutput: true).Result;
        ConsoleUtil.WriteLine(string.Join(Environment.NewLine, dotnetResult.OutputLines));
        ConsoleUtil.WriteLine(ConsoleColor.Red, string.Join(Environment.NewLine, dotnetResult.ErrorLines));

        var assemblies = AssemblyDiscovery.GetAssemblyFilePaths(options);
        return await HelixTestRunner.RunAsync(options, assemblies);
    }
}
