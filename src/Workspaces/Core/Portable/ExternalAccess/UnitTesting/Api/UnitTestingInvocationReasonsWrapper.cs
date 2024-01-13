// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingInvocationReasonsWrapper
    {
        public static readonly UnitTestingInvocationReasonsWrapper SemanticChanged = new(InvocationReasons.SemanticChanged);
        public static readonly UnitTestingInvocationReasonsWrapper Reanalyze = new(InvocationReasons.Reanalyze);
        public static readonly UnitTestingInvocationReasonsWrapper ProjectConfigurationChanged = new(InvocationReasons.ProjectConfigurationChanged);
        public static readonly UnitTestingInvocationReasonsWrapper SyntaxChanged = new(InvocationReasons.SyntaxChanged);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedDocumentAdded = new(PredefinedInvocationReasons.DocumentAdded);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedReanalyze = new(PredefinedInvocationReasons.Reanalyze);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedSemanticChanged = new(PredefinedInvocationReasons.SemanticChanged);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedSyntaxChanged = new(PredefinedInvocationReasons.SyntaxChanged);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedProjectConfigurationChanged = new(PredefinedInvocationReasons.ProjectConfigurationChanged);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedDocumentOpened = new(PredefinedInvocationReasons.DocumentOpened);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedDocumentClosed = new(PredefinedInvocationReasons.DocumentClosed);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedDocumentRemoved = new(PredefinedInvocationReasons.DocumentRemoved);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedHighPriority = new(PredefinedInvocationReasons.HighPriority);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedProjectParseOptionsChanged = new(PredefinedInvocationReasons.ProjectParseOptionsChanged);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedSolutionRemoved = new(PredefinedInvocationReasons.SolutionRemoved);

        internal InvocationReasons UnderlyingObject { get; }

        internal UnitTestingInvocationReasonsWrapper(InvocationReasons underlyingObject)
            => UnderlyingObject = underlyingObject;

        public UnitTestingInvocationReasonsWrapper(string reason)
            : this(new InvocationReasons(reason)) { }

        public UnitTestingInvocationReasonsWrapper With(UnitTestingInvocationReasonsWrapper reason)
            => new(reason.UnderlyingObject.With(UnderlyingObject));

        public bool IsReanalyze()
            => UnderlyingObject.Contains(PredefinedInvocationReasons.Reanalyze);

        public bool HasSemanticChanged()
            => UnderlyingObject.Contains(PredefinedInvocationReasons.SemanticChanged);

        public bool HasProjectConfigurationChanged()
            => UnderlyingObject.Contains(PredefinedInvocationReasons.ProjectConfigurationChanged);
    }
}
