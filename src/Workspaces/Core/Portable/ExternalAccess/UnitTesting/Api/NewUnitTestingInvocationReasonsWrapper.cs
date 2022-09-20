// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct NewUnitTestingInvocationReasonsWrapper
    {
        public static readonly NewUnitTestingInvocationReasonsWrapper SemanticChanged = new(InvocationReasons.SemanticChanged);
        public static readonly NewUnitTestingInvocationReasonsWrapper Reanalyze = new(InvocationReasons.Reanalyze);
        public static readonly NewUnitTestingInvocationReasonsWrapper ProjectConfigurationChanged = new(InvocationReasons.ProjectConfigurationChanged);
        public static readonly NewUnitTestingInvocationReasonsWrapper SyntaxChanged = new(InvocationReasons.SyntaxChanged);
        public static readonly NewUnitTestingInvocationReasonsWrapper PredefinedDocumentAdded = new(PredefinedInvocationReasons.DocumentAdded);
        public static readonly NewUnitTestingInvocationReasonsWrapper PredefinedReanalyze = new(PredefinedInvocationReasons.Reanalyze);
        public static readonly NewUnitTestingInvocationReasonsWrapper PredefinedSemanticChanged = new(PredefinedInvocationReasons.SemanticChanged);
        public static readonly NewUnitTestingInvocationReasonsWrapper PredefinedSyntaxChanged = new(PredefinedInvocationReasons.SyntaxChanged);
        public static readonly NewUnitTestingInvocationReasonsWrapper PredefinedProjectConfigurationChanged = new(PredefinedInvocationReasons.ProjectConfigurationChanged);
        public static readonly NewUnitTestingInvocationReasonsWrapper PredefinedDocumentOpened = new(PredefinedInvocationReasons.DocumentOpened);
        public static readonly NewUnitTestingInvocationReasonsWrapper PredefinedDocumentRemoved = new(PredefinedInvocationReasons.DocumentRemoved);
        public static readonly NewUnitTestingInvocationReasonsWrapper PredefinedDocumentClosed = new(PredefinedInvocationReasons.DocumentClosed);
        public static readonly NewUnitTestingInvocationReasonsWrapper PredefinedHighPriority = new(PredefinedInvocationReasons.HighPriority);
        public static readonly NewUnitTestingInvocationReasonsWrapper PredefinedProjectParseOptionsChanged = new(PredefinedInvocationReasons.ProjectParseOptionsChanged);
        public static readonly NewUnitTestingInvocationReasonsWrapper PredefinedSolutionRemoved = new(PredefinedInvocationReasons.SolutionRemoved);

        internal InvocationReasons UnderlyingObject { get; }

        internal NewUnitTestingInvocationReasonsWrapper(InvocationReasons underlyingObject)
            => UnderlyingObject = underlyingObject;

        public NewUnitTestingInvocationReasonsWrapper(string reason)
            : this(new InvocationReasons(reason)) { }

        public NewUnitTestingInvocationReasonsWrapper With(NewUnitTestingInvocationReasonsWrapper reason)
            => new(reason.UnderlyingObject.With(UnderlyingObject));

        public bool IsReanalyze()
            => UnderlyingObject.Contains(PredefinedInvocationReasons.Reanalyze);

        public bool HasSemanticChanged()
            => UnderlyingObject.Contains(PredefinedInvocationReasons.SemanticChanged);

        public bool HasProjectConfigurationChanged()
            => UnderlyingObject.Contains(PredefinedInvocationReasons.ProjectConfigurationChanged);
    }
}
