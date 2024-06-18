// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls;

internal class NewTypeDestinationSelectionViewModel : AbstractNotifyPropertyChanged
{
    public static NewTypeDestinationSelectionViewModel Default = new(
        string.Empty,
        LanguageNames.CSharp,
        string.Empty,
        string.Empty,
        ImmutableArray<string>.Empty,
        null,
        NewTypeDestination.NewFile);

    private readonly string _fileExtension;
    private readonly string _defaultNamespace;
    private readonly string _generatedNameTypeParameterSuffix;
    private readonly ImmutableArray<string> _conflictingNames;
    private readonly string _defaultName;
    private readonly ISyntaxFactsService? _syntaxFactsService;
    private readonly string _languageName;

    public NewTypeDestinationSelectionViewModel(
        string defaultName,
        string languageName,
        string defaultNamespace,
        string generatedNameTypeParameterSuffix,
        ImmutableArray<string> conflictingNames,
        ISyntaxFactsService? syntaxFactsService,
        NewTypeDestination typeDestination)
    {
        _defaultName = defaultName;
        _fileExtension = languageName == LanguageNames.CSharp ? ".cs" : ".vb";
        _languageName = languageName;
        _generatedNameTypeParameterSuffix = generatedNameTypeParameterSuffix;
        _conflictingNames = conflictingNames;
        _defaultNamespace = defaultNamespace;
        _typeName = _defaultName;
        _syntaxFactsService = syntaxFactsService;
        _fileName = $"{defaultName}{_fileExtension}";
        Destination = typeDestination;
    }

    private string _typeName;
    public string TypeName
    {
        get
        {
            return _typeName;
        }

        set
        {
            if (SetProperty(ref _typeName, value))
            {
                FileName = string.Format("{0}{1}", value.Trim(), _fileExtension);
                NotifyPropertyChanged(nameof(GeneratedName));
            }
        }
    }

    public string GeneratedName
    {
        get
        {
            return string.Format(
                "{0}{1}{2}",
                string.IsNullOrEmpty(_defaultNamespace) ? string.Empty : _defaultNamespace + ".",
                _typeName.Trim(),
                _generatedNameTypeParameterSuffix);
        }
    }

    private string _fileName;
    public string FileName
    {
        get { return _fileName; }
        set { SetProperty(ref _fileName, value); }
    }

    private NewTypeDestination _destination = NewTypeDestination.NewFile;
    public NewTypeDestination Destination
    {
        get { return _destination; }
        set
        {
            if (SetProperty(ref _destination, value))
            {
                NotifyPropertyChanged(nameof(FileNameEnabled));
            }
        }
    }

    internal bool TrySubmit([NotNullWhen(returnValue: false)] out string? message)
    {
        message = null;

        if (_syntaxFactsService is null)
        {
            throw new InvalidOperationException();
        }

        var trimmedName = TypeName.Trim();
        if (_conflictingNames.Contains(trimmedName))
        {
            message = ServicesVSResources.Name_conflicts_with_an_existing_type_name;
            return false;
        }

        if (!_syntaxFactsService.IsValidIdentifier(trimmedName))
        {
            message = string.Format(ServicesVSResources.Name_is_not_a_valid_0_identifier, _languageName);
            return false;
        }

        var trimmedFileName = FileName.Trim();
        if (!Path.GetExtension(trimmedFileName).Equals(_fileExtension, StringComparison.OrdinalIgnoreCase))
        {
            message = string.Format(ServicesVSResources.File_name_must_have_the_0_extension, _fileExtension);
            return false;
        }

        if (trimmedFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            message = ServicesVSResources.Illegal_characters_in_path;
            return false;
        }

        return true;
    }

    public bool FileNameEnabled => Destination == NewTypeDestination.NewFile;
}

internal enum NewTypeDestination
{
    CurrentFile,
    NewFile
};
