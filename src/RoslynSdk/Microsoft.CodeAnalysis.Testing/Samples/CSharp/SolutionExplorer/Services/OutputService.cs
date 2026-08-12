using System;
using System.Text;

namespace MSBuildWorkspaceTester.Services
{
    internal class OutputService
    {
        private readonly StringBuilder _text = new StringBuilder();

        public void WriteLine(string message)
        {
            _text.AppendLine(message);

            TextChanged?.Invoke(this, EventArgs.Empty);
        }

        public string GetText()
            => _text.ToString();

        public event EventHandler<EventArgs> TextChanged;
    }
}
