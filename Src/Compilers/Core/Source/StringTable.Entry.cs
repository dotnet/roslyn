using System.Text;

namespace Roslyn.Compilers
{
    partial class StringTable
    {
        private class Entry
        {
            internal readonly string Text;
            internal readonly int HashCode;
            internal Entry Next;

            internal Entry(string text, int hashCode, Entry next)
            {
                this.Text = text;
                this.HashCode = hashCode;
                this.Next = next;
            }
        }
    }
}