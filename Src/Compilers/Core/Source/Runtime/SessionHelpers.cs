using System;
using System.ComponentModel;
using System.Diagnostics;
using Roslyn.Scripting;

namespace Microsoft.CSharp.RuntimeHelpers
{
    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SessionHelpers
    {
        // this method is only run in Submission constructor that doesn't need to be thread-safe
        [Obsolete("do not use this method", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static object GetSubmission(Session session, int id)
        {
            // A call to this helper is not emitted when there are no previous submissions in the session.
            // There are obviously no previous submissions if there is no session at all.
            Debug.Assert(session != null);

            return session.submissions[id];
        }

        // this method is only run in Submission constructor that doesn't need to be thread-safe
        [Obsolete("do not use this method", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static object SetSubmission(Session session, int slotIndex, object submission)
        {
            if (session == null)
            {
                return null;
            }
            
            if (slotIndex >= session.submissions.Length)
            {
                Array.Resize(ref session.submissions, Math.Max(slotIndex + 1, session.submissions.Length * 2));
            }

            if (slotIndex > 0 && session.submissions[slotIndex - 1] == null)
            {
                ThrowPreviousSubmissionNotExecuted();
            }

            session.submissions[slotIndex] = submission;
            return session.hostObject;
        }

        internal static void RequirePreviousSubmissionExecuted(Session session, int slotIndex)
        {
            if (slotIndex > 0 && session.submissions[slotIndex - 1] == null)
            {
                ThrowPreviousSubmissionNotExecuted();
            }
        }

        private static void ThrowPreviousSubmissionNotExecuted()
        {
            throw new InvalidOperationException("Previous submission must execute before this submission.");
        }
    }
}
