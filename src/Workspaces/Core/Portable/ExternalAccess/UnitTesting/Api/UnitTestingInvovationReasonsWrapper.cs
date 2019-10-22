// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingInvovationReasonsWrapper
    {
        public static readonly UnitTestingInvovationReasonsWrapper SemanticChanged = new UnitTestingInvovationReasonsWrapper(InvocationReasons.SemanticChanged);
        public static readonly UnitTestingInvovationReasonsWrapper Reanalyze = new UnitTestingInvovationReasonsWrapper(InvocationReasons.Reanalyze);
        public static readonly UnitTestingInvovationReasonsWrapper ProjectConfigurationChanged = new UnitTestingInvovationReasonsWrapper(InvocationReasons.ProjectConfigurationChanged);
        public static readonly UnitTestingInvovationReasonsWrapper SyntaxChanged = new UnitTestingInvovationReasonsWrapper(InvocationReasons.SyntaxChanged);

        internal InvocationReasons UnderlyingObject { get; }

        internal UnitTestingInvovationReasonsWrapper(InvocationReasons underlyingObject)
            => UnderlyingObject = underlyingObject;

        public UnitTestingInvovationReasonsWrapper(string reason) : this(new InvocationReasons(reason)) { }

        public bool Contains(string reason)
            => UnderlyingObject.Contains(reason);

        public UnitTestingInvovationReasonsWrapper With(string reason)
            => new UnitTestingInvovationReasonsWrapper(UnderlyingObject.With(reason));
    }
}
