// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RequestOrdering
{
    internal class OrderedLspRequest
    {
        public string MethodName { get; }
        public int RequestOrder { get; set; }

        public OrderedLspRequest(string methodName)
        {
            MethodName = methodName;
        }
    }
}
