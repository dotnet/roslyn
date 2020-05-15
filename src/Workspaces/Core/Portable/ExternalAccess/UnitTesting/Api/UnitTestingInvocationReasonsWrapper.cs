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
        public static readonly UnitTestingInvocationReasonsWrapper DocumentAdded = new UnitTestingInvocationReasonsWrapper(InvocationReasons.DocumentAdded);

        internal InvocationReasons UnderlyingObject { get; }

        internal UnitTestingInvocationReasonsWrapper(InvocationReasons underlyingObject)
            => UnderlyingObject = underlyingObject;

        public UnitTestingInvocationReasonsWrapper(string reason) : this(new InvocationReasons(reason)) { }

        public bool IsReanalyze()
            => UnderlyingObject.Contains(PredefinedInvocationReasons.Reanalyze);

        public bool HasSemanticChanged()
            => UnderlyingObject.Contains(PredefinedInvocationReasons.SemanticChanged);

        public bool HasProjectConfigurationChanged()
            => UnderlyingObject.Contains(PredefinedInvocationReasons.ProjectConfigurationChanged);

        public UnitTestingInvocationReasonsWrapper WithReanalyze()
            => new UnitTestingInvocationReasonsWrapper(UnderlyingObject.With(PredefinedInvocationReasons.Reanalyze));

        public UnitTestingInvocationReasonsWrapper WithSemanticChanged()
            => new UnitTestingInvocationReasonsWrapper(UnderlyingObject.With(PredefinedInvocationReasons.SemanticChanged));

        public UnitTestingInvocationReasonsWrapper WithSyntaxChanged()
            => new UnitTestingInvocationReasonsWrapper(UnderlyingObject.With(PredefinedInvocationReasons.SyntaxChanged));

        public UnitTestingInvocationReasonsWrapper WithProjectConfigurationChanged()
            => new UnitTestingInvocationReasonsWrapper(UnderlyingObject.With(PredefinedInvocationReasons.ProjectConfigurationChanged));

        public UnitTestingInvocationReasonsWrapper WithDocumentAdded()
            => new UnitTestingInvocationReasonsWrapper(UnderlyingObject.With(PredefinedInvocationReasons.DocumentAdded));

        public UnitTestingInvocationReasonsWrapper WithDocumentOpened()
            => new UnitTestingInvocationReasonsWrapper(UnderlyingObject.With(PredefinedInvocationReasons.DocumentOpened));

        public UnitTestingInvocationReasonsWrapper WithDocumentRemoved()
            => new UnitTestingInvocationReasonsWrapper(UnderlyingObject.With(PredefinedInvocationReasons.DocumentRemoved));

        public UnitTestingInvocationReasonsWrapper WithDocumentClosed()
            => new UnitTestingInvocationReasonsWrapper(UnderlyingObject.With(PredefinedInvocationReasons.DocumentClosed));

        public UnitTestingInvocationReasonsWrapper WithHighPriority()
            => new UnitTestingInvocationReasonsWrapper(UnderlyingObject.With(PredefinedInvocationReasons.HighPriority));

        public UnitTestingInvocationReasonsWrapper WithProjectParseOptionsChanged()
            => new UnitTestingInvocationReasonsWrapper(UnderlyingObject.With(PredefinedInvocationReasons.ProjectParseOptionsChanged));

        public UnitTestingInvocationReasonsWrapper WithSolutionRemoved()
            => new UnitTestingInvocationReasonsWrapper(UnderlyingObject.With(PredefinedInvocationReasons.SolutionRemoved));
    }
}
