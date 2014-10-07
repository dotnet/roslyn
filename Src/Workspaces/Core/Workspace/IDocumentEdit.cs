using System;
using Roslyn.Compilers;

namespace Roslyn.Services
{
    public interface IDocumentEdit
    {
        IDocument Document { get; }

        void Delete(TextSpan deleteSpan);

        void Insert(int position, string text);

        void Replace(TextSpan replaceSpan, string replaceWith);
    }
}