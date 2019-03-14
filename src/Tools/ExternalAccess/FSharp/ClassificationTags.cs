using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Classification
{
    public static class ClassificationTags
    {
        public static string GetClassificationTypeName(string textTag) => textTag.ToClassificationTypeName();
    }
}
