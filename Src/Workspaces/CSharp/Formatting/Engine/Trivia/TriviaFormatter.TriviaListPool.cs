using System.Collections.Concurrent;
using System.Collections.Generic;
using Roslyn.Compilers.CSharp;

namespace Roslyn.Services.CSharp.Formatting
{
    internal partial class TriviaFormatter
    {
        private class TriviaListPool
        {
            // maximum memory used by the pool is 16*20*28 (sizeof(SyntaxTrivia)) bytes
            private const int MaxPool = 16;
            private const int Threshold = 20;

            private static readonly ConcurrentQueue<List<SyntaxTrivia>> triviaListPool =
                new ConcurrentQueue<List<SyntaxTrivia>>();

            public static List<SyntaxTrivia> Allocate()
            {
                List<SyntaxTrivia> result;
                if (triviaListPool.TryDequeue(out result))
                {
                    return result;
                }

                return new List<SyntaxTrivia>();
            }

            public static List<SyntaxTrivia> ReturnAndFree(List<SyntaxTrivia> pool)
            {
                var result = new List<SyntaxTrivia>(pool);
                Free(pool);

                return result;
            }

            public static void Free(List<SyntaxTrivia> pool)
            {
                if (triviaListPool.Count >= MaxPool ||
                    pool.Capacity > Threshold)
                {
                    return;
                }

                pool.Clear();
                triviaListPool.Enqueue(pool);
            }
        }
    }
}