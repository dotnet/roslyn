// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    internal static class StateDispatchILCounter
    {
        internal static int CountStateDispatchChecks(string il)
        {
            var lines = il.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (!TryGetCachedStateLocalOperand(lines, out var cachedStateLocalOperand))
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < lines.Length - 1; i++)
            {
                if (IsCachedStateLocalLoad(lines[i], cachedStateLocalOperand) && IsStateDispatchBranch(lines[i + 1]))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryGetCachedStateLocalOperand(string[] lines, out string operand)
        {
            operand = "";
            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains("ldfld", StringComparison.Ordinal) ||
                    !lines[i].Contains(".<>1__state\"", StringComparison.Ordinal))
                {
                    continue;
                }

                for (int j = i + 1; j < lines.Length && j <= i + 4; j++)
                {
                    var stlocIndex = lines[j].IndexOf("stloc", StringComparison.Ordinal);
                    if (stlocIndex < 0)
                    {
                        continue;
                    }

                    var tail = lines[j].Substring(stlocIndex + "stloc".Length).Trim();
                    if (tail.StartsWith(".", StringComparison.Ordinal))
                    {
                        tail = tail.Substring(1).Trim();
                    }

                    if (tail.StartsWith("s", StringComparison.Ordinal))
                    {
                        tail = tail.Substring(1).Trim();
                    }

                    operand = tail;
                    return operand.Length > 0;
                }
            }

            return false;
        }

        private static bool IsCachedStateLocalLoad(string line, string operand)
        {
            if (operand.Length == 1 && char.IsDigit(operand[0]))
            {
                return line.Contains($"ldloc.{operand}", StringComparison.Ordinal);
            }

            return line.Contains($"ldloc.s   {operand}", StringComparison.Ordinal) ||
                   line.Contains($"ldloc      {operand}", StringComparison.Ordinal) ||
                   line.Contains($"ldloc.{operand}", StringComparison.Ordinal);
        }

        private static bool IsStateDispatchBranch(string line)
            => line.Contains("brfalse", StringComparison.Ordinal) ||
               line.Contains("beq", StringComparison.Ordinal) ||
               line.Contains("switch", StringComparison.Ordinal);
    }
}
