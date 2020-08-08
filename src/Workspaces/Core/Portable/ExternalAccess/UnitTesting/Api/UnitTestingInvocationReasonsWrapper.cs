// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingInvocationReasonsWrapper
    {
        public static readonly UnitTestingInvocationReasonsWrapper SemanticChanged = new UnitTestingInvocationReasonsWrapper(InvocationReasons.SemanticChanged);
        public static readonly UnitTestingInvocationReasonsWrapper Reanalyze = new UnitTestingInvocationReasonsWrapper(InvocationReasons.Reanalyze);
        public static readonly UnitTestingInvocationReasonsWrapper ProjectConfigurationChanged = new UnitTestingInvocationReasonsWrapper(InvocationReasons.ProjectConfigurationChanged);
        public static readonly UnitTestingInvocationReasonsWrapper SyntaxChanged = new UnitTestingInvocationReasonsWrapper(InvocationReasons.SyntaxChanged);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedDocumentAdded = new UnitTestingInvocationReasonsWrapper(PredefinedInvocationReasons.DocumentAdded);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedReanalyze = new UnitTestingInvocationReasonsWrapper(PredefinedInvocationReasons.Reanalyze);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedSemanticChanged = new UnitTestingInvocationReasonsWrapper(PredefinedInvocationReasons.SemanticChanged);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedSyntaxChanged = new UnitTestingInvocationReasonsWrapper(PredefinedInvocationReasons.SyntaxChanged);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedProjectConfigurationChanged = new UnitTestingInvocationReasonsWrapper(PredefinedInvocationReasons.ProjectConfigurationChanged);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedDocumentOpened = new UnitTestingInvocationReasonsWrapper(PredefinedInvocationReasons.DocumentOpened);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedDocumentRemoved = new UnitTestingInvocationReasonsWrapper(PredefinedInvocationReasons.DocumentRemoved);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedDocumentClosed = new UnitTestingInvocationReasonsWrapper(PredefinedInvocationReasons.DocumentClosed);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedHighPriority = new UnitTestingInvocationReasonsWrapper(PredefinedInvocationReasons.HighPriority);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedProjectParseOptionsChanged = new UnitTestingInvocationReasonsWrapper(PredefinedInvocationReasons.ProjectParseOptionsChanged);
        public static readonly UnitTestingInvocationReasonsWrapper PredefinedSolutionRemoved = new UnitTestingInvocationReasonsWrapper(PredefinedInvocationReasons.SolutionRemoved);

        internal InvocationReasons UnderlyingObject { get; }

        internal UnitTestingInvocationReasonsWrapper(InvocationReasons underlyingObject)
            => UnderlyingObject = underlyingObject;

        public UnitTestingInvocationReasonsWrapper(string reason)
            : this(new InvocationReasons(reason)) { }

        public UnitTestingInvocationReasonsWrapper With(UnitTestingInvocationReasonsWrapper reason)
            => new UnitTestingInvocationReasonsWrapper(reason.UnderlyingObject.With(UnderlyingObject));

        public bool IsReanalyze()
            => UnderlyingObject.Contains(PredefinedInvocationReasons.Reanalyze);

        public bool HasSemanticChanged()
            => UnderlyingObject.Contains(PredefinedInvocationReasons.SemanticChanged);

        public bool HasProjectConfigurationChanged()
            => UnderlyingObject.Contains(PredefinedInvocationReasons.ProjectConfigurationChanged);
    }
}
