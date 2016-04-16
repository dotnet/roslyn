// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly IInteractiveWindow _window;
        private readonly SortedSpans _spans;

        internal InteractiveWindowWriter(IInteractiveWindow window, SortedSpans spans)
        {
            Debug.Assert(window != null);
            _window = window;
            _spans = spans;
        }

        public IInteractiveWindow Window
        {
            get { return _window; }
        }

        public SortedSpans Spans
        {
            get
            {
                return _spans;
            }
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

            int offset = _window.Write(value).Start;
            if (_spans != null)
            {
                _spans.Add(new Span(offset, value.Length));
            }
        }

        public override void Write(char[] value, int start, int count)
        {
            Write(new string(value, start, count));
        }

        public override void WriteLine()
        {
            Span span = _window.WriteLine(text: null);
            if (_spans != null)
            {
                _spans.Add(span);
            }
        }

        public override void WriteLine(string str)
        {
            Span span = _window.WriteLine(str);
            if (_spans != null)
            {
                _spans.Add(span);
            }
        }
    }
}
