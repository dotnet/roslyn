// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;

/// <summary>
/// Mock object for editor options.
/// not sure why, but Moq throw Null exception when DefaultOptions.StaticXXXField is used in Moq
/// </summary>
public sealed class TestEditorOptions : IEditorOptions
{
    public static IEditorOptions Instance = new TestEditorOptions();

    private readonly bool _convertTabsToSpaces;
    private readonly int _tabSize;
    private readonly int _indentSize;

    public TestEditorOptions()
    {
        _convertTabsToSpaces = true;
        _tabSize = 4;
        _indentSize = 4;
    }

    public TestEditorOptions(bool convertTabsToSpaces, int tabSize, int indentSize)
    {
        _convertTabsToSpaces = convertTabsToSpaces;
        _tabSize = tabSize;
        _indentSize = indentSize;
    }

    public T GetOptionValue<T>(EditorOptionKey<T> key)
    {
        if (key.Equals(DefaultOptions.ConvertTabsToSpacesOptionId))
        {
            return (T)(object)_convertTabsToSpaces;
        }
        else if (key.Equals(DefaultOptions.TabSizeOptionId))
        {
            return (T)(object)_tabSize;
        }
        else if (key.Equals(DefaultOptions.IndentSizeOptionId))
        {
            return (T)(object)_indentSize;
        }

        throw new ArgumentException("key", "unexpected key");
    }

    #region not implemented
    public bool ClearOptionValue<T>(EditorOptionKey<T> key)
        => throw new NotImplementedException();

    public bool ClearOptionValue(string optionId)
        => throw new NotImplementedException();

    public object GetOptionValue(string optionId)
        => throw new NotImplementedException();

    public T GetOptionValue<T>(string optionId)
        => throw new NotImplementedException();

    public IEditorOptions GlobalOptions
    {
        get { throw new NotImplementedException(); }
    }

    public bool IsOptionDefined<T>(EditorOptionKey<T> key, bool localScopeOnly)
        => throw new NotImplementedException();

    public bool IsOptionDefined(string optionId, bool localScopeOnly)
        => throw new NotImplementedException();

#pragma warning disable 67
    public event EventHandler<EditorOptionChangedEventArgs> OptionChanged;

    public IEditorOptions Parent
    {
        get
        {
            throw new NotImplementedException();
        }

        set
        {
            throw new NotImplementedException();
        }
    }

    public void SetOptionValue<T>(EditorOptionKey<T> key, T value)
        => throw new NotImplementedException();

    public void SetOptionValue(string optionId, object value)
        => throw new NotImplementedException();

    public System.Collections.Generic.IEnumerable<EditorOptionDefinition> SupportedOptions
    {
        get { throw new NotImplementedException(); }
    }
    #endregion
}
