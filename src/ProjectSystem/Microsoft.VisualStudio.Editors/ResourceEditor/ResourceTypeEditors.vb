Option Explicit On
Option Strict On
Option Compare Binary

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' Provides a set of ResourceTypeEditor instances for convenience.
    ''' 
    ''' *** IMPORTANT ***
    '''
    ''' You should *never* do a reference comparison against a particular resource type 
    '''   editor, because there can be different instances of them, so that doesn't 
    '''   mean a thing.  For instance, the resource type editor that you get from the
    '''   Resource class is created through TypeDescriptor.GetEditor, and it will *not*
    '''   be the same instance as those in this class.  
    ''' If you need to compare two resource editor editors (in general this shouldn't
    '''   be necessary anyway), then use ResourceTypeEditor.Equals().  It will do the
    '''   right thing.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ResourceTypeEditors
        ''' <summary>
        ''' A instance of a ResourceTypeEditor that handles audio data (wav files)
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared Audio As New ResourceTypeEditorAudio

        ''' <summary>
        ''' A instance of a ResourceTypeEditor that handles binary files
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared BinaryFile As New ResourceTypeEditorBinaryFile

        ''' <summary>
        ''' A instance of a ResourceTypeEditor that handles bitmaps (BMP, JPG, GIF)
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared Bitmap As New ResourceTypeEditorBitmap

        ''' <summary>
        ''' A instance of a ResourceTypeEditor that handles icons
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared Icon As New ResourceTypeEditorIcon

        ''' <summary>
        ''' An instance of a ResourceTypeEditor that handles resources which are not convertible to/from string,
        '''   and which otherwise are not treated specially by the resource editor.
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared NonStringConvertible As New ResourceTypeEditorNonStringConvertible

        ''' <summary>
        ''' An instance of a ResourceTypeEditor that handles ResXNullRef (a value of Nothing)
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared [Nothing] As New ResourceTypeEditorNothing

        ''' <summary>
        ''' An instance of a ResourceTypeEditor that handles strings
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared [String] As New ResourceTypeEditorString

        ''' <summary>
        ''' An instance of a ResourceTypeEditor that handles values which are convertible to/from strings.
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared StringConvertible As New ResourceTypeEditorStringConvertible

        ''' <summary>
        ''' A instance of a ResourceTypeEditor that handles text files
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared TextFile As New ResourceTypeEditorTextFile

    End Class

End Namespace
