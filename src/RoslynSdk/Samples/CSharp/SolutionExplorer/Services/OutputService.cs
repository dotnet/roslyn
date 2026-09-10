// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
