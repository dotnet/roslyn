// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Moq;
using System.Linq;

namespace Roslyn.Test.Utilities
{
    public static class MoqExtensions
    {
        public static void VerifyAndClear(this IInvocationList invocations, params (string Name, object[] Args)[] expectedInvocations)
        {
            AssertEx.Equal(
                expectedInvocations.Select(i => $"{i.Name}: {string.Join(",", i.Args)}"),
                invocations.Select(i => $"{i.Method.Name}: {string.Join(",", i.Arguments)}"));

            for (int i = 0; i < expectedInvocations.Length; i++)
            {
                AssertEx.Equal(expectedInvocations[i].Args, invocations[i].Arguments);
            }

            invocations.Clear();
        }
    }
}
