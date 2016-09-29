// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue
{
    internal static class DebugLogMessage
    {
        private const string SessionId = nameof(SessionId);
        private const string EditSessionId = nameof(EditSessionId);

        private const string SessionCount = nameof(SessionCount);
        private const string EmptySessionCount = nameof(EmptySessionCount);

        private const string HadCompilationErrors = nameof(HadCompilationErrors);
        private const string HadRudeEdits = nameof(HadRudeEdits);
        private const string HadValidChanges = nameof(HadValidChanges);
        private const string HadValidInsignificantChanges = nameof(HadValidInsignificantChanges);
        private const string RudeEditsCount = nameof(RudeEditsCount);
        private const string EmitDeltaErrorIdCount = nameof(EmitDeltaErrorIdCount);

        private const string ErrorId = nameof(ErrorId);

        private const string RudeEditKind = nameof(RudeEditKind);
        private const string RudeEditSyntaxKind = nameof(RudeEditSyntaxKind);
        private const string RudeEditBlocking = nameof(RudeEditBlocking);

        public static KeyValueLogMessage Create(int sessionId, EncDebuggingSessionInfo session)
        {
            return KeyValueLogMessage.Create(m => CreateSessionKeyValue(m, sessionId, session));
        }

        public static KeyValueLogMessage Create(int sessionId, int editSessionId, EncEditSessionInfo editSession)
        {
            return KeyValueLogMessage.Create(m => CreateSessionEditKeyValue(m, sessionId, editSessionId, editSession));
        }

        public static KeyValueLogMessage Create(int sessionId, int editSessionId, string error)
        {
            return KeyValueLogMessage.Create(m => CreateEditSessionErrorId(m, sessionId, editSessionId, error));
        }

        public static KeyValueLogMessage Create(int sessionId, int editSessionId, ValueTuple<ushort, ushort> rudeEdit, bool blocking)
        {
            return KeyValueLogMessage.Create(m => CreateEditSessionRudeEdit(m, sessionId, editSessionId, rudeEdit, blocking));
        }

        public static int GetNextId()
        {
            return LogAggregator.GetNextId();
        }

        private static void CreateSessionKeyValue(Dictionary<string, object> map, int sessionId, EncDebuggingSessionInfo session)
        {
            map[SessionId] = sessionId;
            map[SessionCount] = session.EditSessions.Count;
            map[EmptySessionCount] = session.EmptyEditSessions;
        }

        private static void CreateSessionEditKeyValue(Dictionary<string, object> map, int sessionId, int editSessionId, EncEditSessionInfo editSession)
        {
            map[SessionId] = sessionId;
            map[EditSessionId] = editSessionId;

            map[HadCompilationErrors] = editSession.HadCompilationErrors;
            map[HadRudeEdits] = editSession.HadRudeEdits;
            map[HadValidChanges] = editSession.HadValidChanges;
            map[HadValidInsignificantChanges] = editSession.HadValidInsignificantChanges;

            map[RudeEditsCount] = editSession.RudeEdits.Count;
            map[EmitDeltaErrorIdCount] = editSession.EmitDeltaErrorIds != null ? editSession.EmitDeltaErrorIds.Count() : 0;
        }

        private static void CreateEditSessionErrorId(Dictionary<string, object> map, int sessionId, int editSessionId, string error)
        {
            map[SessionId] = sessionId;
            map[EditSessionId] = editSessionId;

            map[ErrorId] = error;
        }

        private static void CreateEditSessionRudeEdit(Dictionary<string, object> map, int sessionId, int editSessionId, ValueTuple<ushort, ushort> rudeEdit, bool blocking)
        {
            map[SessionId] = sessionId;
            map[EditSessionId] = editSessionId;

            map[RudeEditKind] = rudeEdit.Item1;
            map[RudeEditSyntaxKind] = rudeEdit.Item2;
            map[RudeEditBlocking] = blocking;
        }
    }
}
