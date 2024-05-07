// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.CodeAnalysis.GenerateType;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.ProjectManagement;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.GenerateType
{
    internal class GenerateTypeDialogViewModel : AbstractNotifyPropertyChanged
    {
        private readonly Document _document;
        private readonly INotificationService _notificationService;
        private readonly IProjectManagementService _projectManagementService;
        private readonly ISyntaxFactsService _syntaxFactsService;
        private readonly GenerateTypeDialogOptions _generateTypeDialogOptions;
        private string _typeName;
        private bool _isNewFile;

        private Dictionary<string, Accessibility> _accessListMap;
        private Dictionary<string, TypeKind> _typeKindMap;
        private List<string> _csharpAccessList;
        private List<string> _visualBasicAccessList;
        private List<string> _csharpTypeKindList;
        private List<string> _visualBasicTypeKindList;

        private readonly string _csharpExtension = ".cs";
        private readonly string _visualBasicExtension = ".vb";

        // reserved names that cannot be a folder name or filename
        private readonly string[] _reservedKeywords =
#pragma warning disable format // https://github.com/dotnet/roslyn/issues/70711 tracks removing this suppression.
                                                [
                                                    "con", "prn", "aux", "nul",
                                                    "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
                                                    "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9", "clock$"
                                                ];
#pragma warning restore format

        // Below code details with the Access List and the manipulation
        public List<string> AccessList { get; }
        private int _accessSelectIndex;
        public int AccessSelectIndex
        {
            get
            {
                return _accessSelectIndex;
            }

            set
            {
                SetProperty(ref _accessSelectIndex, value);
            }
        }

        private string _selectedAccessibilityString;
        public string SelectedAccessibilityString
        {
            get
            {
                return _selectedAccessibilityString;
            }

            set
            {
                SetProperty(ref _selectedAccessibilityString, value);
            }
        }

        public Accessibility SelectedAccessibility
        {
            get
            {
                Debug.Assert(_accessListMap.ContainsKey(SelectedAccessibilityString), "The Accessibility Key String not present");
                return _accessListMap[SelectedAccessibilityString];
            }
        }

        private List<string> _kindList;
        public List<string> KindList
        {
            get
            {
                return _kindList;
            }

            set
            {
                SetProperty(ref _kindList, value);
            }
        }

        private int _kindSelectIndex;
        public int KindSelectIndex
        {
            get
            {
                return _kindSelectIndex;
            }

            set
            {
                SetProperty(ref _kindSelectIndex, value);
            }
        }

        private string _selectedTypeKindString;
        public string SelectedTypeKindString
        {
            get
            {
                return _selectedTypeKindString;
            }

            set
            {
                SetProperty(ref _selectedTypeKindString, value);
            }
        }

        public TypeKind SelectedTypeKind
        {
            get
            {
                Debug.Assert(_typeKindMap.ContainsKey(SelectedTypeKindString), "The TypeKind Key String not present");
                return _typeKindMap[SelectedTypeKindString];
            }
        }

        private void PopulateTypeKind(TypeKind typeKind, string csharpKey, string visualBasicKey)
        {
            _typeKindMap.Add(visualBasicKey, typeKind);
            _typeKindMap.Add(csharpKey, typeKind);

            _csharpTypeKindList.Add(csharpKey);
            _visualBasicTypeKindList.Add(visualBasicKey);
        }

        private void PopulateTypeKind(TypeKind typeKind, string visualBasicKey)
        {
            _typeKindMap.Add(visualBasicKey, typeKind);
            _visualBasicTypeKindList.Add(visualBasicKey);
        }

        private void PopulateAccessList(string key, Accessibility accessibility, string languageName = null)
        {
            if (languageName == null)
            {
                _csharpAccessList.Add(key);
                _visualBasicAccessList.Add(key);
            }
            else if (languageName == LanguageNames.CSharp)
            {
                _csharpAccessList.Add(key);
            }
            else
            {
                Debug.Assert(languageName == LanguageNames.VisualBasic, "Currently only C# and VB are supported");
                _visualBasicAccessList.Add(key);
            }

            _accessListMap.Add(key, accessibility);
        }

        private void InitialSetup()
        {
            _accessListMap = new Dictionary<string, Accessibility>();
            _typeKindMap = new Dictionary<string, TypeKind>();
            _csharpAccessList = new List<string>();
            _visualBasicAccessList = new List<string>();
            _csharpTypeKindList = new List<string>();
            _visualBasicTypeKindList = new List<string>();

            // Populate the AccessListMap
            if (!_generateTypeDialogOptions.IsPublicOnlyAccessibility)
            {
                PopulateAccessList("Default", Accessibility.NotApplicable);
                PopulateAccessList("internal", Accessibility.Internal, LanguageNames.CSharp);
                PopulateAccessList("Friend", Accessibility.Internal, LanguageNames.VisualBasic);
            }

            PopulateAccessList("public", Accessibility.Public, LanguageNames.CSharp);
            PopulateAccessList("Public", Accessibility.Public, LanguageNames.VisualBasic);

            // Populate the TypeKind
            PopulateTypeKind();
        }

        private void PopulateTypeKind()
        {
            Debug.Assert(_generateTypeDialogOptions.TypeKindOptions != TypeKindOptions.None);

            if (TypeKindOptionsHelper.IsClass(_generateTypeDialogOptions.TypeKindOptions))
            {
                PopulateTypeKind(TypeKind.Class, "class", "Class");
            }

            if (TypeKindOptionsHelper.IsEnum(_generateTypeDialogOptions.TypeKindOptions))
            {
                PopulateTypeKind(TypeKind.Enum, "enum", "Enum");
            }

            if (TypeKindOptionsHelper.IsStructure(_generateTypeDialogOptions.TypeKindOptions))
            {
                PopulateTypeKind(TypeKind.Structure, "struct", "Structure");
            }

            if (TypeKindOptionsHelper.IsInterface(_generateTypeDialogOptions.TypeKindOptions))
            {
                PopulateTypeKind(TypeKind.Interface, "interface", "Interface");
            }

            if (TypeKindOptionsHelper.IsDelegate(_generateTypeDialogOptions.TypeKindOptions))
            {
                PopulateTypeKind(TypeKind.Delegate, "delegate", "Delegate");
            }

            if (TypeKindOptionsHelper.IsModule(_generateTypeDialogOptions.TypeKindOptions))
            {
                _shouldChangeTypeKindListSelectedIndex = true;
                PopulateTypeKind(TypeKind.Module, "Module");
            }
        }

        internal bool TrySubmit()
        {
            if (this.IsNewFile)
            {
                var trimmedFileName = FileName.Trim();

                // Case : \\Something
                if (trimmedFileName.StartsWith(@"\\", StringComparison.Ordinal))
                {
                    SendFailureNotification(ServicesVSResources.Illegal_characters_in_path);
                    return false;
                }

                // Case : something\
                if (string.IsNullOrWhiteSpace(trimmedFileName) || trimmedFileName.EndsWith(@"\", StringComparison.Ordinal))
                {
                    SendFailureNotification(ServicesVSResources.Path_cannot_have_empty_filename);
                    return false;
                }

                if (trimmedFileName.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    SendFailureNotification(ServicesVSResources.Illegal_characters_in_path);
                    return false;
                }

                var isRootOfTheProject = trimmedFileName.StartsWith(@"\", StringComparison.Ordinal);
                string implicitFilePath = null;

                // Construct the implicit file path
                if (isRootOfTheProject || this.SelectedProject != _document.Project)
                {
                    if (!TryGetImplicitFilePath(this.SelectedProject.FilePath ?? string.Empty, ServicesVSResources.Project_Path_is_illegal, out implicitFilePath))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!TryGetImplicitFilePath(_document.FilePath, ServicesVSResources.DocumentPath_is_illegal, out implicitFilePath))
                    {
                        return false;
                    }
                }

                // Remove the '\' at the beginning if present
                trimmedFileName = trimmedFileName.StartsWith(@"\", StringComparison.Ordinal) ? trimmedFileName[1..] : trimmedFileName;

                // Construct the full path of the file to be created
                this.FullFilePath = implicitFilePath + @"\" + trimmedFileName;

                try
                {
                    this.FullFilePath = Path.GetFullPath(this.FullFilePath);
                }
                catch (Exception e)
                {
                    SendFailureNotification(e.Message);
                    return false;
                }

                // Path.GetFullPath does not remove the spaces infront of the filename or folder name . So remove it
                var lastIndexOfSeparatorInFullPath = this.FullFilePath.LastIndexOf('\\');
                if (lastIndexOfSeparatorInFullPath != -1)
                {
                    var fileNameInFullPathInContainers = this.FullFilePath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

                    // Trim spaces of each component of the file name.
                    // Note that path normalization changed between 4.6.1 and 4.6.2 and GetFullPath no longer trims trailing spaces.
                    // See https://blogs.msdn.microsoft.com/jeremykuhne/2016/06/21/more-on-new-net-path-handling/
                    this.FullFilePath = string.Join("\\", fileNameInFullPathInContainers.Select(str => str.Trim()));
                }

                string projectRootPath = null;
                if (this.SelectedProject.FilePath == null)
                {
                    projectRootPath = string.Empty;
                }
                else if (!TryGetImplicitFilePath(this.SelectedProject.FilePath, ServicesVSResources.Project_Path_is_illegal, out projectRootPath))
                {
                    return false;
                }

                if (this.FullFilePath.StartsWith(projectRootPath, StringComparison.Ordinal))
                {
                    // The new file will be within the root of the project
                    var folderPath = this.FullFilePath[projectRootPath.Length..];
                    var containers = folderPath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

                    // Folder name was mentioned
                    if (containers.Length > 1)
                    {
                        _fileName = containers.Last();
                        Folders = new List<string>(containers);
                        Folders.RemoveAt(Folders.Count - 1);

                        if (Folders.Any(folder => !(_syntaxFactsService.IsValidIdentifier(folder) || _syntaxFactsService.IsVerbatimIdentifier(folder))))
                        {
                            _areFoldersValidIdentifiers = false;
                        }
                    }
                    else if (containers.Length == 1)
                    {
                        // File goes at the root of the Directory
                        _fileName = containers[0];
                        Folders = null;
                    }
                    else
                    {
                        SendFailureNotification(ServicesVSResources.Illegal_characters_in_path);
                        return false;
                    }
                }
                else
                {
                    // The new file will be outside the root of the project and folders will be null
                    Folders = null;

                    var lastIndexOfSeparator = this.FullFilePath.LastIndexOf('\\');
                    if (lastIndexOfSeparator == -1)
                    {
                        SendFailureNotification(ServicesVSResources.Illegal_characters_in_path);
                        return false;
                    }

                    _fileName = this.FullFilePath[(lastIndexOfSeparator + 1)..];
                }

                // Check for reserved words in the folder or filename
                if (this.FullFilePath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Any(s => _reservedKeywords.Contains(s, StringComparer.OrdinalIgnoreCase)))
                {
                    SendFailureNotification(ServicesVSResources.File_path_cannot_use_reserved_keywords);
                    return false;
                }

                // We check to see if file path of the new file matches the filepath of any other existing file or if the Folders and FileName matches any of the document then
                // we say that the file already exists.
                if (this.SelectedProject.Documents.Where(n => n != null).Where(n => n.FilePath == FullFilePath).Any() ||
                    (this.Folders != null && this.FileName != null &&
                     this.SelectedProject.Documents.Where(n => n.Name != null && n.Folders.Count > 0 && n.Name == this.FileName && this.Folders.SequenceEqual(n.Folders)).Any()) ||
                     File.Exists(FullFilePath))
                {
                    SendFailureNotification(ServicesVSResources.File_already_exists);
                    return false;
                }
            }

            return true;
        }

        private bool TryGetImplicitFilePath(string implicitPathContainer, string message, out string implicitPath)
        {
            var indexOfLastSeparator = implicitPathContainer.LastIndexOf('\\');
            if (indexOfLastSeparator == -1)
            {
                SendFailureNotification(message);
                implicitPath = null;
                return false;
            }

            implicitPath = implicitPathContainer[..indexOfLastSeparator];
            return true;
        }

        private void SendFailureNotification(string message)
            => _notificationService.SendNotification(message, severity: NotificationSeverity.Information);

        private Project _selectedProject;
        public Project SelectedProject
        {
            get
            {
                return _selectedProject;
            }

            set
            {
                var previousProject = _selectedProject;
                if (SetProperty(ref _selectedProject, value))
                {
                    NotifyPropertyChanged(nameof(DocumentList));
                    this.DocumentSelectIndex = 0;
                    this.ProjectSelectIndex = this.ProjectList.FindIndex(p => p.Project == _selectedProject);
                    if (_selectedProject != _document.Project)
                    {
                        // Restrict the Access List Options
                        // 3 in the list represent the Public. 1-based array.
                        this.AccessSelectIndex = this.AccessList.IndexOf("public") == -1 ?
                            this.AccessList.IndexOf("Public") : this.AccessList.IndexOf("public");
                        Debug.Assert(this.AccessSelectIndex != -1);
                        this.IsAccessListEnabled = false;
                    }
                    else
                    {
                        // Remove restriction
                        this.IsAccessListEnabled = true;
                    }

                    if (previousProject != null && _projectManagementService != null)
                    {
                        this.ProjectFolders = _projectManagementService.GetFolders(this.SelectedProject.Id, this.SelectedProject.Solution.Workspace);
                    }

                    // Update the TypeKindList if required
                    if (previousProject != null && previousProject.Language != _selectedProject.Language)
                    {
                        if (_selectedProject.Language == LanguageNames.CSharp)
                        {
                            var previousSelectedIndex = _kindSelectIndex;
                            this.KindList = _csharpTypeKindList;
                            if (_shouldChangeTypeKindListSelectedIndex)
                            {
                                this.KindSelectIndex = 0;
                            }
                            else
                            {
                                this.KindSelectIndex = previousSelectedIndex;
                            }
                        }
                        else
                        {
                            var previousSelectedIndex = _kindSelectIndex;
                            this.KindList = _visualBasicTypeKindList;
                            if (_shouldChangeTypeKindListSelectedIndex)
                            {
                                this.KindSelectIndex = 0;
                            }
                            else
                            {
                                this.KindSelectIndex = previousSelectedIndex;
                            }
                        }
                    }

                    // Update File Extension
                    UpdateFileNameExtension();
                }
            }
        }

        private int _projectSelectIndex;
        public int ProjectSelectIndex
        {
            get
            {
                return _projectSelectIndex;
            }

            set
            {
                SetProperty(ref _projectSelectIndex, value);
            }
        }

        public List<ProjectSelectItem> ProjectList { get; }

        private Project _previouslyPopulatedProject = null;
        private List<DocumentSelectItem> _previouslyPopulatedDocumentList = null;

        public IEnumerable<DocumentSelectItem> DocumentList
        {
            get
            {
                if (_previouslyPopulatedProject == _selectedProject)
                {
                    return _previouslyPopulatedDocumentList;
                }

                _previouslyPopulatedProject = _selectedProject;
                _previouslyPopulatedDocumentList = new List<DocumentSelectItem>();

                // Check for the current project
                if (_selectedProject == _document.Project)
                {
                    // populate the current document
                    _previouslyPopulatedDocumentList.Add(new DocumentSelectItem(_document, "<Current File>"));

                    // Set the initial selected Document
                    this.SelectedDocument = _document;

                    // Populate the rest of the documents for the project
                    _previouslyPopulatedDocumentList.AddRange(_document.Project.Documents
                        .Where(d => d != _document && !d.IsGeneratedCode(CancellationToken.None))
                        .Select(d => new DocumentSelectItem(d)));
                }
                else
                {
                    _previouslyPopulatedDocumentList.AddRange(_selectedProject.Documents
                        .Where(d => !d.IsGeneratedCode(CancellationToken.None))
                        .Select(d => new DocumentSelectItem(d)));

                    this.SelectedDocument = _selectedProject.Documents.FirstOrDefault();
                }

                this.IsExistingFileEnabled = _previouslyPopulatedDocumentList.Count == 0 ? false : true;
                this.IsNewFile = this.IsExistingFileEnabled ? this.IsNewFile : true;
                return _previouslyPopulatedDocumentList;
            }
        }

        private bool _isExistingFileEnabled = true;
        public bool IsExistingFileEnabled
        {
            get
            {
                return _isExistingFileEnabled;
            }

            set
            {
                SetProperty(ref _isExistingFileEnabled, value);
            }
        }

        private int _documentSelectIndex;
        public int DocumentSelectIndex
        {
            get
            {
                return _documentSelectIndex;
            }

            set
            {
                SetProperty(ref _documentSelectIndex, value);
            }
        }

        private Document _selectedDocument;
        public Document SelectedDocument
        {
            get
            {
                return _selectedDocument;
            }

            set
            {
                SetProperty(ref _selectedDocument, value);
            }
        }

        private string _fileName;
        public string FileName
        {
            get
            {
                return _fileName;
            }

            set
            {
                SetProperty(ref _fileName, value);
            }
        }

        public List<string> Folders;

        public string TypeName
        {
            get
            {
                return _typeName;
            }

            set
            {
                SetProperty(ref _typeName, value);
            }
        }

        public bool IsNewFile
        {
            get
            {
                return _isNewFile;
            }

            set
            {
                SetProperty(ref _isNewFile, value);
            }
        }

        public bool IsExistingFile
        {
            get
            {
                return !_isNewFile;
            }

            set
            {
                SetProperty(ref _isNewFile, !value);
            }
        }

        private bool _isAccessListEnabled;
        private bool _shouldChangeTypeKindListSelectedIndex = false;

        public bool IsAccessListEnabled
        {
            get
            {
                return _isAccessListEnabled;
            }

            set
            {
                SetProperty(ref _isAccessListEnabled, value);
            }
        }

        private bool _areFoldersValidIdentifiers = true;
        public bool AreFoldersValidIdentifiers
        {
            get
            {
                if (_areFoldersValidIdentifiers)
                {
                    /*
                    var workspace = this.SelectedProject.Solution.Workspace as VisualStudioWorkspaceImpl;
                    var project = workspace?.GetHostProject(this.SelectedProject.Id) as AbstractProject;
                    return !(project?.IsWebSite == true);
                    */
                    return false;
                }

                return false;
            }
        }

        public IList<string> ProjectFolders { get; private set; }
        public string FullFilePath { get; private set; }

        internal void UpdateFileNameExtension()
        {
            var currentFileName = this.FileName.Trim();
            if (!string.IsNullOrWhiteSpace(currentFileName) && !currentFileName.EndsWith("\\", StringComparison.Ordinal))
            {
                if (this.SelectedProject.Language == LanguageNames.CSharp)
                {
                    // For CSharp
                    currentFileName = UpdateExtension(currentFileName, _csharpExtension, _visualBasicExtension);
                }
                else
                {
                    // For Visual Basic
                    currentFileName = UpdateExtension(currentFileName, _visualBasicExtension, _csharpExtension);
                }
            }

            this.FileName = currentFileName;
        }

        private static string UpdateExtension(string currentFileName, string desiredFileExtension, string undesiredFileExtension)
        {
            if (currentFileName.EndsWith(desiredFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                // No change required
                return currentFileName;
            }

            // Remove the undesired extension
            if (currentFileName.EndsWith(undesiredFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                currentFileName = currentFileName[..^undesiredFileExtension.Length];
            }

            // Append the desired extension
            return currentFileName + desiredFileExtension;
        }

        internal GenerateTypeDialogViewModel(
            Document document,
            INotificationService notificationService,
            IProjectManagementService projectManagementService,
            ISyntaxFactsService syntaxFactsService,
            GenerateTypeDialogOptions generateTypeDialogOptions,
            string typeName,
            string fileExtension,
            bool isNewFile,
            string accessSelectString,
            string typeKindSelectString)
        {
            _generateTypeDialogOptions = generateTypeDialogOptions;

            InitialSetup();
            var dependencyGraph = document.Project.Solution.GetProjectDependencyGraph();

            // Initialize the dependencies
            var projectListing = new List<ProjectSelectItem>();

            // Populate the project list
            // Add the current project
            projectListing.Add(new ProjectSelectItem(document.Project));

            // Add the rest of the projects
            // Adding dependency graph to avoid cyclic dependency
            projectListing.AddRange(document.Project.Solution.Projects
                                    .Where(p => p != document.Project && !dependencyGraph.GetProjectsThatThisProjectTransitivelyDependsOn(p.Id).Contains(document.Project.Id))
                                    .Select(p => new ProjectSelectItem(p)));

            this.ProjectList = projectListing;

            const string attributeSuffix = "Attribute";
            _typeName = generateTypeDialogOptions.IsAttribute && !typeName.EndsWith(attributeSuffix, StringComparison.Ordinal) ? typeName + attributeSuffix : typeName;
            this.FileName = typeName + fileExtension;

            _document = document;
            this.SelectedProject = document.Project;
            this.SelectedDocument = document;
            _notificationService = notificationService;

            this.AccessList = document.Project.Language == LanguageNames.CSharp
                ? _csharpAccessList
                : _visualBasicAccessList;
            this.AccessSelectIndex = this.AccessList.Contains(accessSelectString)
                ? this.AccessList.IndexOf(accessSelectString) : 0;
            this.IsAccessListEnabled = true;

            this.KindList = document.Project.Language == LanguageNames.CSharp
                ? _csharpTypeKindList
                : _visualBasicTypeKindList;
            this.KindSelectIndex = this.KindList.Contains(typeKindSelectString)
                ? this.KindList.IndexOf(typeKindSelectString) : 0;

            this.ProjectSelectIndex = 0;
            this.DocumentSelectIndex = 0;

            _isNewFile = isNewFile;

            _syntaxFactsService = syntaxFactsService;

            _projectManagementService = projectManagementService;
            if (projectManagementService != null)
            {
                this.ProjectFolders = _projectManagementService.GetFolders(this.SelectedProject.Id, this.SelectedProject.Solution.Workspace);
            }
            else
            {
                this.ProjectFolders = SpecializedCollections.EmptyList<string>();
            }
        }

        public class ProjectSelectItem
        {
            private readonly Project _project;

            public string Name
            {
                get
                {
                    return _project.Name;
                }
            }

            public Project Project
            {
                get
                {
                    return _project;
                }
            }

            public ProjectSelectItem(Project project)
                => _project = project;
        }

        public class DocumentSelectItem
        {
            private readonly Document _document;
            public Document Document
            {
                get
                {
                    return _document;
                }
            }

            private readonly string _name;
            public string Name
            {
                get
                {
                    return _name;
                }
            }

            public DocumentSelectItem(Document document, string documentName)
            {
                _document = document;
                _name = documentName;
            }

            public DocumentSelectItem(Document document)
            {
                _document = document;
                if (document.Folders.Count == 0)
                {
                    _name = document.Name;
                }
                else
                {
                    _name = string.Join("\\", document.Folders) + "\\" + document.Name;
                }
            }
        }
    }
}
