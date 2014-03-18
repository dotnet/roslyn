// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows.Forms;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public partial class ProgressDialog
    {
        public ProgressDialog()
        {
            InitializeComponent();
        }

        public int Maximum
        {
            get;
            set;
        }

        private void ProgressDialog_Load(object sender, EventArgs e)
        {
            Label2.Text = "0";
            ProgressBar1.Maximum = Maximum;
        }

        public void Increment()
        {
            Label2.Text = "1";
            ProgressBar1.Increment(1);
            Application.DoEvents();
        }
    }
}