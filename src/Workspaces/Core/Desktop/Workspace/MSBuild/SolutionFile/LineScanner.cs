
namespace Microsoft.CodeAnalysis.MSBuild
{
    internal class LineScanner
    {
        private readonly string _line;
        private int _currentPosition = 0;

        public LineScanner(string line)
        {
            _line = line;
        }

        public string ReadUpToAndEat(string delimiter)
        {
            int index = _line.IndexOf(delimiter, _currentPosition);

            if (index == -1)
            {
                return ReadRest();
            }
            else
            {
                var upToDelimiter = _line.Substring(_currentPosition, index - _currentPosition);
                _currentPosition = index + delimiter.Length;
                return upToDelimiter;
            }
        }

        public string ReadRest()
        {
            var rest = _line.Substring(_currentPosition);
            _currentPosition = _line.Length;
            return rest;
        }
    }
}