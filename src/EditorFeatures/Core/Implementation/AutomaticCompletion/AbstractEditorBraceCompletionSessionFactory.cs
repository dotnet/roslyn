// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion.Sessions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion
{
    internal abstract class AbstractEditorBraceCompletionSessionFactory : ForegroundThreadAffinitizedObject, IEditorBraceCompletionSessionFactory
    {
        private readonly ImmutableArray<IEditorBraceCompletionSession> _braceCompletionSessions;

        protected AbstractEditorBraceCompletionSessionFactory(
            IEnumerable<IBraceCompletionService> braceCompletionServices,
            IThreadingContext threadingContext)
            : base(threadingContext)
        {
            _braceCompletionSessions = braceCompletionServices
                .Select(service => (IEditorBraceCompletionSession)new EditorBraceCompletionSession(service))
                .ToImmutableArray();
        }

        public IEditorBraceCompletionSession? TryCreateSession(Document document, int openingPosition, char openingBrace, CancellationToken cancellationToken)
        {
            this.AssertIsForeground();
            return _braceCompletionSessions.SingleOrDefault(session => session.IsValidForBraceCompletion(openingBrace, openingPosition, document, cancellationToken));
        }
    }
}
