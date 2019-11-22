// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
