// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract partial class AbstractPackage<TPackage, TLanguageService, TProject> : AbstractPackage<TPackage, TLanguageService>
        where TPackage : AbstractPackage<TPackage, TLanguageService, TProject>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService, TProject>
        where TProject : AbstractProject
    {
        protected AbstractPackage() : base()
        {
        }
    }
}
