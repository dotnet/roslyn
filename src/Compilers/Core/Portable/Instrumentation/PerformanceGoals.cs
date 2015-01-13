// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Instrumentation
{
    internal static class PerformanceGoals
    {
        internal static readonly string[] Goals;
        internal const string Undefined = "Undefined";

        static PerformanceGoals()
        {
            // An interaction class defines how much time is expected to reach a time point, the response 
            // time point being the most commonly used. The interaction classes correspond to human perception,
            // so, for example, all interactions in the Fast class are perceived as fast and roughly feel like 
            // they have the same performance. By defining these interaction classes, we can describe 
            // performance using adjectives that have a precise, consistent meaning.
            //
            // Name             Target (ms)     Upper Bound (ms)        UX / Feedback
            // Instant          <=50            100                     No noticeable delay
            // Fast             50-100          200                     Minimally noticeable delay
            // Typical          100-300         500                     Slower, but still no feedback necessary
            // Responsive       300-500         1,000                   Slower yet, potentially show Wait cursor
            // Captive          >500            10,000                  Long, show Progress Dialog w/Cancel
            // Extended         >500            >10,000                 Long enough for the user to switch to something else

            // Used for throughput scenarios like parser bytes per second.
            const string Throughput_100 = "Throughput_100";

            Goals = new string[(int)FunctionId.Count];
            Goals[(int)FunctionId.CSharp_SyntaxTree_FullParse] = Throughput_100;
            Goals[(int)FunctionId.VisualBasic_SyntaxTree_FullParse] = Throughput_100;
        }
    }
}
