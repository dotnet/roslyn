using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal sealed class InteractiveWindowWriter : TextWriter
    {
        private readonly IInteractiveWindow window;
        private readonly SortedSpans spans;

        internal InteractiveWindowWriter(IInteractiveWindow window, SortedSpans spans)
        {
            Debug.Assert(window != null);
            this.window = window;
            this.spans = spans;
        }

        public IInteractiveWindow Window
        {
            get { return window; }
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public override IFormatProvider FormatProvider
        {
            get { return CultureInfo.CurrentCulture; }
        }

        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }

        public override void Write(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            int offset = window.Write(value);
            if (spans != null)
            {
                spans.Add(new Span(offset, value.Length));
            }
        }

        public override void Write(char[] value, int start, int count)
        {
            Write(new string(value, start, count));
        }

        public override void WriteLine()
        {
            Span span = window.WriteLine(text: null);
            if (spans != null)
            {
                spans.Add(span);
            }
        }

        public override void WriteLine(string str)
        {
            Span span = window.WriteLine(str);
            if (spans != null)
            {
                spans.Add(span);
            }
        }
    }
}
