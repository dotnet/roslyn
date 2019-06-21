// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Organizing.Organizers
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportSyntaxNodeOrganizerAttribute : ExportAttribute
    {
        public ExportSyntaxNodeOrganizerAttribute(string languageName) :
            base(typeof(ISyntaxOrganizer))
        {
            Language = languageName;
        }

        public string Language { get; }
    }
}
