// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities.Remoting;

namespace Roslyn.VisualStudio.Test.Utilities
{
    public class CSharpInteractiveWindow : InteractiveWindow
    {
        internal const string CreateMethodName = nameof(InteractiveWindowWrapper.CreateForCSharp);
        internal const string DteViewCommand = "View.C#Interactive";
        internal const string DteWindowTitle = "C# Interactive";

        public CSharpInteractiveWindow(VisualStudioInstance visualStudioInstance) : base(visualStudioInstance, DteViewCommand, DteWindowTitle, CreateMethodName)
        {
        }
    }
}
