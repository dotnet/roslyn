// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    /// <summary>
    /// Use this attribute to declare a <see cref="CodeRefactoringProvider"/> implementation so that it can be discovered by the host.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    public class ExportCodeRefactoringProviderAttribute : ExportAttribute
    {
        /// <summary>
        /// The name of the <see cref="CodeRefactoringProvider"/>.  
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The source language this provider can provide refactorings for.  See <see cref="LanguageNames"/>.
        /// </summary>
        public string Language { get; private set; }

        public ExportCodeRefactoringProviderAttribute(string name, string language)
            : base(typeof(CodeRefactoringProvider))
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (language == null)
            {
                throw new ArgumentNullException("languageServices");
            }

            this.Name = name;
            this.Language = language;
        }
    }
}