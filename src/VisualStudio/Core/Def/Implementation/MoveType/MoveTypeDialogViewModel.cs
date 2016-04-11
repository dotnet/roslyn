// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.ProjectManagement;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveType
{
    internal class MoveTypeDialogViewModel : AbstractNotifyPropertyChanged
    {
        private string _fileName;
        public string FileName
        {
            get { return _fileName; }
            set { SetProperty(ref _fileName, value); }
        }

        private string _projectName;
        public string ProjectName
        {
            get { return _projectName; }
            set { SetProperty(ref _projectName, value); }
        }

        private bool _removeUnusedUsings;
        public bool RemoveUnusedUsings
        {
            get { return _removeUnusedUsings; }
            set { SetProperty(ref _removeUnusedUsings, value); }
        }

        internal MoveTypeDialogViewModel(
            string suggestedFileName,
            Document document,
            INotificationService notificationService,
            IProjectManagementService projectManagementService,
            ISyntaxFactsService syntaxFactsService)
        {
            ProjectName = document.Project.Name + " " + Path.DirectorySeparatorChar;
            FileName = suggestedFileName;
        }

        internal bool TrySubmit()
        {
            // make file checks and raise appropriate failure messages.
            return true;
        }
    }
}
