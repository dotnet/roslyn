// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    public sealed class SubmissionBufferAddedEventArgs : EventArgs
    {
        private readonly ITextBuffer _newBuffer;

        public SubmissionBufferAddedEventArgs(ITextBuffer newBuffer)
        {
            if (newBuffer == null)
            {
                throw new ArgumentNullException(nameof(newBuffer));
            }

            _newBuffer = newBuffer;
        }

        public ITextBuffer NewBuffer
        {
            get
            {
                return _newBuffer;
            }
        }
    }
}
