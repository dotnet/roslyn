using System;
using System.Diagnostics;
using Roslyn.Compilers.Common;

namespace Roslyn.Scripting
{
    /// <summary>
    /// Compiled executable submission.
    /// </summary>
    public sealed class Submission<T>
    {
        private readonly CommonCompilation compilation;
        private readonly Session session;
        private readonly Func<Session, T> factory;

        internal Submission(CommonCompilation compilation, Session session, Func<Session, T> factory)
        {
            Debug.Assert(compilation != null);

            this.compilation = compilation;
            this.session = session;
            this.factory = factory;
        }

        public T Execute()
        {
            return (factory != null) ? factory(session) : default(T);
        }

        public CommonCompilation Compilation
        {
            get
            {
                return compilation;
            }
        }
    }
}