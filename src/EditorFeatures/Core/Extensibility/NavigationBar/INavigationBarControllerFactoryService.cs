// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface INavigationBarControllerFactoryService
    {
        INavigationBarController CreateController(INavigationBarPresenter presenter, ITextBuffer textBuffer);
    }
}
