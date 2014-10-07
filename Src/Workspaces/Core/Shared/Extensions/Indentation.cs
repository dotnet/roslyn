using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal struct Indentation
    {
        /// <summary>The raw character position of the indentation in the in the line. This will
        /// range between [0, line.Length).</summary> 
        public int LinePosition { get; private set; }

        /// <summary>The column position of the indentation in the line.  This will be equal to the
        /// LinePosition if the indentation is all spaces, but it can greater if there are tabs in
        /// the indent that expand out to more than 1 space.</summary>
        public int Column { get; private set; }

        public Indentation(int linePosition, int column)
            : this()
        {
            this.LinePosition = linePosition;
            this.Column = column;
        }
    }
}