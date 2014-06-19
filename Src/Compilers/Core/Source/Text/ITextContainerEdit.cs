using System;

namespace Roslyn.Compilers
{
    public interface ITextContainerEdit : IDisposable
    {
        bool Delete(TextSpan deleteSpan);
        bool Delete(int startPosition, int charsToDelete);
        bool Insert(int position, string text);
        bool Insert(int position, char[] characterBuffer, int startIndex, int length);
        bool Replace(TextSpan replaceSpan, string replaceWith);
        bool Replace(int startPosition, int charsToReplace, string replaceWith);

        bool Canceled { get; }
        IText Text { get; }

        IText Apply();
        void Cancel();
    }
}