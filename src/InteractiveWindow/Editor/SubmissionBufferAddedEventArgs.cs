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
        private readonly ITextBuffer newBuffer;

        public SubmissionBufferAddedEventArgs(ITextBuffer newBuffer)
        {
            if (newBuffer == null)
            {
                throw new ArgumentNullException("newBuffer");
            }
            
            this.newBuffer = newBuffer;
        }

        public ITextBuffer NewBuffer
        {
            get
            {
                return newBuffer;
            }
        }
    }
}
