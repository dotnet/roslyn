// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.StackTraceExplorer
{
    internal class StackFrameParserHelpers
    {
        /// <summary>
        /// Makes sure that the string at least somewhat resembles the correct form.
        /// Does not check validity on class or method identifiers
        /// Example line:
        /// at ConsoleApp4.MyClass.ThrowAtOne(p1, p2,) 
        ///   |-------------------||--------||-------| 
        ///           Class          Method    Args   
        /// </summary>
        /// <remarks>
        /// See https://docs.microsoft.com/en-us/dotnet/api/system.environment.stacktrace for more information
        /// on expected stacktrace form
        /// </remarks>

        public static bool TryParseMethodSignature(string line, int start, int end, out TextSpan classSpan, out TextSpan methodSpan, out TextSpan argsSpan)
        {
            classSpan = default;
            methodSpan = default;
            argsSpan = default;

            line = line[start..end];

            var regex = new Regex(@"(?<class>([a-zA-Z0-9_<\s,>]+\.)+)(?<method>[a-zA-Z0-9_<\s,>]+)\((?<args>.*)\).*");

            var match = regex.Match(line);
            if (!match.Success)
            {
                return false;
            }

            var classGroup = match.Groups["class"];
            if (!classGroup.Success)
            {
                return false;
            }

            var methodGroup = match.Groups["method"];
            if (!methodGroup.Success)
            {
                return false;
            }

            var argsGroup = match.Groups["args"];

            classSpan = new TextSpan(start + classGroup.Index, classGroup.Length);
            methodSpan = new TextSpan(start + methodGroup.Index, methodGroup.Length);
            argsSpan = argsGroup.Success
                ? new TextSpan(start + argsGroup.Index, argsGroup.Length)
                : new TextSpan(start + methodGroup.Index + methodGroup.Length, 0); // Default to a 0 length span at the end of the method text

            return true;
        }
    }
}
