using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal interface IProjectCodeModelFactory
    {
        IProjectCodeModel CreateProjectCodeModel(ProjectId id, ICodeModelInstanceFactory codeModelInstanceFactory);
        EnvDTE.FileCodeModel GetOrCreateFileCodeModel(ProjectId id, string filePath);
    }
}
