' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Flags which help control the behavior of a PropertyControlData instance.
    ''' </summary>
    ''' <remarks></remarks>
    <Flags()> _
    Public Enum ControlDataFlags
        None = 0                 'No flags
        UserPersisted = &H10       'Property is persisted using custom code (see ReadUserDefinedProperty, WriteUserDefinedProperty, GetUserDefinedProperty).  Note that this is completely independent of whether a custom getter and setter are defined.
        UserHandledEvents = &H20   'If true, no automatic handling of control events are defined.  By default, some events are handled automatically for the FormControl, based on the type of control (certain common control types are handled). 
        Hidden = &H40              'True if the property should be hidden in the UI.

        'Persistence flags - if none are specified, the default assumes changing the property requires checking out the project file
        PersistedInProjectUserFile = &H100 'Changing this property requires checking out the project .user file
        PersistedInVBMyAppFile = &H200 'Changing this property requires checking out the Application.myapp and Application.Designer.vb file (VB only)
        PersistedInAppManifestFile = &H400 'Changing this property requires checking out the app.manifest file
        PersistedInAssemblyInfoFile = &H800 'Changing this property requires checking out the app.manifest file
        PersistedInApplicationDefinitionFile = &H4000 'Changing this property requires checking out the application.xaml file (WPF)
        NoOptimisticFileCheckout = &H8000 'Don't automatically check out any files before the property set
        ProjectMayBeReloadedDuringPropertySet = &H1000 'Setting this property can cause the project to get reloaded
        'If this property value is changed, all properties on the page should be automatically refreshed.
        '  This allows for controls to be enabled/disabled, etc., when a property is changed by the user.
        RefreshAllPropertiesWhenChanged = &H2000
        'Internal-use flags
        <EditorBrowsable(EditorBrowsableState.Never)> _
        Dirty = 1                '(Internal use - property is dirty)
        <EditorBrowsable(EditorBrowsableState.Never)> _
        CommonProperty = 2       '(Internal use - property is a common property)
    End Enum

End Namespace

