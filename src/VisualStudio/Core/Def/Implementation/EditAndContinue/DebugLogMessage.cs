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
        private const string SessionId = "SessionId";
        private const string EditSessionId = "EditSessionId";

        private const string SessionCount = "SessionCount";
        private const string EmptySessionCount = "EmptySessionCount";

        private const string HadCompilationErrors = "HadCompilationErrors";
        private const string HadRudeEdits = "HadRudeEdits";
        private const string HadValidChanges = "HadValidChanges";
        private const string HadValidInsignificantChanges = "HadValidInsignificantChanges";
        private const string RudeEditsCount = "RudeEditsCount";
        private const string EmitDeltaErrorIdCount = "EmitDeltaErrorIdCount";

        private const string ErrorId = "ErrorId";

        private const string RudeEditKind = "RudeEditKind";
        private const string RudeEditSyntaxKind = "RudeEditSyntaxKind";
        private const string RudeEditBlocking = "RudeEditBlocking";

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

        private static void CreateSessionKeyValue(Dictionary<string, string> map, int sessionId, EncDebuggingSessionInfo session)
        {
            map[SessionId] = sessionId.ToString();
            map[SessionCount] = session.EditSessions.Count.ToString();
            map[EmptySessionCount] = session.EmptyEditSessions.ToString();
        }

        private static void CreateSessionEditKeyValue(Dictionary<string, string> map, int sessionId, int editSessionId, EncEditSessionInfo editSession)
        {
            map[SessionId] = sessionId.ToString();
            map[EditSessionId] = editSessionId.ToString();

            map[HadCompilationErrors] = editSession.HadCompilationErrors.ToString();
            map[HadRudeEdits] = editSession.HadRudeEdits.ToString();
            map[HadValidChanges] = editSession.HadValidChanges.ToString();
            map[HadValidInsignificantChanges] = editSession.HadValidInsignificantChanges.ToString();

            map[RudeEditsCount] = editSession.RudeEdits.Count.ToString();
            map[EmitDeltaErrorIdCount] = editSession.EmitDeltaErrorIds != null ? editSession.EmitDeltaErrorIds.Count().ToString() : "0";
        }

        private static void CreateEditSessionErrorId(Dictionary<string, string> map, int sessionId, int editSessionId, string error)
        {
            map[SessionId] = sessionId.ToString();
            map[EditSessionId] = editSessionId.ToString();

            map[ErrorId] = error;
        }

        private static void CreateEditSessionRudeEdit(Dictionary<string, string> map, int sessionId, int editSessionId, ValueTuple<ushort, ushort> rudeEdit, bool blocking)
        {
            map[SessionId] = sessionId.ToString();
            map[EditSessionId] = editSessionId.ToString();

            map[RudeEditKind] = rudeEdit.Item1.ToString();
            map[RudeEditSyntaxKind] = rudeEdit.Item2.ToString();
            map[RudeEditBlocking] = blocking.ToString();
        }
    }
}
