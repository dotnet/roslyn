// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows.Forms;

namespace Roslyn.DebuggerVisualizers.UI
{
    public partial class TextViewer : Form
    {
        public TextViewer(string text, string title)
        {
            InitializeComponent();

            IL.Text = text;
            this.Text = title;
        }
    }
}