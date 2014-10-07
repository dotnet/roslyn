using Roslyn.Compilers.CSharp;
using Roslyn.Utilities;

namespace Roslyn.Services.CSharp.Formatting
{
    internal struct TriviaList
    {
        private readonly SyntaxTriviaList[] triviaLists;
        private readonly int count;

        public TriviaList(params SyntaxTriviaList[] lists)
        {
            Contract.ThrowIfNull(lists);
            this.triviaLists = lists;

            this.count = 0;
            for (int i = 0; i < triviaLists.Length; i++)
            {
                this.count += triviaLists[i].Count;
            }
        }

        public int Count
        {
            get
            {
                return count;
            }
        }

        public SyntaxTrivia this[int i]
        {
            get
            {
                Contract.ThrowIfFalse(i >= 0 && i < this.Count);

                int listIndex = 0;
                for (; listIndex < triviaLists.Length; listIndex++)
                {
                    var list = triviaLists[listIndex];
                    if (i < list.Count)
                    {
                        return list[i];
                    }

                    i -= list.Count;
                }

                return Contract.FailWithReturn<SyntaxTrivia>("shouldn't reach here");
            }
        }
    }
}
