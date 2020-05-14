// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingInvocationReasonsWrapper
    {
        public static readonly UnitTestingInvocationReasonsWrapper SemanticChanged = new UnitTestingInvocationReasonsWrapper(InvocationReasons.SemanticChanged);
        public static readonly UnitTestingInvocationReasonsWrapper Reanalyze = new UnitTestingInvocationReasonsWrapper(InvocationReasons.Reanalyze);
        public static readonly UnitTestingInvocationReasonsWrapper ProjectConfigurationChanged = new UnitTestingInvocationReasonsWrapper(InvocationReasons.ProjectConfigurationChanged);
        public static readonly UnitTestingInvocationReasonsWrapper SyntaxChanged = new UnitTestingInvocationReasonsWrapper(InvocationReasons.SyntaxChanged);

        internal InvocationReasons UnderlyingObject { get; }

        internal UnitTestingInvocationReasonsWrapper(InvocationReasons underlyingObject)
            => UnderlyingObject = underlyingObject;

        public UnitTestingInvocationReasonsWrapper(string reason) : this(new InvocationReasons(reason)) { }

        public bool Contains(string reason)
            => UnderlyingObject.Contains(reason);

        public UnitTestingInvocationReasonsWrapper With(string reason)
            => new UnitTestingInvocationReasonsWrapper(UnderlyingObject.With(reason));
    }
}
