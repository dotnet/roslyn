namespace Microsoft.CodeAnalysis.MSBuild
{
    internal class LineScanner
    {
        private readonly string line;
        private int currentPosition = 0;

        public LineScanner(string line)
        {
            this.line = line;
        }

        public string ReadUpToAndEat(string delimiter)
        {
            int index = line.IndexOf(delimiter, currentPosition);

            if (index == -1)
            {
                return ReadRest();
            }
            else
            {
                var upToDelimiter = line.Substring(currentPosition, index - currentPosition);
                currentPosition = index + delimiter.Length;
                return upToDelimiter;
            }
        }

        public string ReadRest()
        {
            var rest = line.Substring(currentPosition);
            currentPosition = line.Length;
            return rest;
        }
    }
}