// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.CodeAnalysis.Scripting
{
    public struct OutputRedirect : IDisposable  
    {  
        private readonly TextWriter _oldOut;  
        private readonly StringWriter _newOut;  
    
        public OutputRedirect(IFormatProvider formatProvider)  
        {  
            _oldOut = Console.Out;  
            _newOut = new StringWriter(formatProvider);  
            Console.SetOut(_newOut);  
        }  
    
        public string Output => _newOut.ToString();  
    
        void IDisposable.Dispose()  
        {  
            Console.SetOut(_oldOut);  
            _newOut.Dispose();  
        }
    }
}
