// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

var args = System.Environment.GetCommandLineArgs();
if (args.Length != 4)
{
    Console.WriteLine("Usage: runner.csx <path/to/StackDepthTest.exe> <upper bound>");
    foreach (var s in args)
    {
        Console.WriteLine(s);
    }
    return -1;
}
var testerLocation = args[2];
var upperBound = 0;

if (int.TryParse(args[3], out var ub))
{
    upperBound = ub;
}
else
{
    Console.WriteLine("<upper bound> must be an integer");
    return -1;
}

bool runTest(int n)
{
    var proc = System.Diagnostics.Process.Start(testerLocation, $"{n}");
    proc.StartInfo.UseShellExecute = false;
    proc.StartInfo.CreateNoWindow = true;
    proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
    proc.Start();
    proc.WaitForExit();
    return proc.ExitCode == 0;
}

// Sanity check
if (!runTest(0))
{
    Console.WriteLine("Test failed on the lower bound!");
    return -1;
}
if (runTest(upperBound))
{
    Console.WriteLine("Test passed on the upper bound!");
    return -1;
}

var low = 0;
var high = upperBound;

while (low != high)
{
    var mid = (low + high) / 2;
    if (mid == low || mid == high) { break; }

    Console.Write($"Running {mid}: ");
    if (runTest(mid))
    {
        low = mid;
        Console.WriteLine("pass");
    }
    else
    {
        high = mid;
        Console.WriteLine("fail");
    }
}

Console.WriteLine($"Break even point: {low}");
