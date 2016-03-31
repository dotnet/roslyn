Imports Microsoft.VisualStudio.Editors
Imports Microsoft.VisualStudio.ManagedInterfaces.ProjectDesigner
Imports System
Imports System.Diagnostics

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' A specialized property page site for hosting child pages from a PropPageUserControlBase page.
    '''   Supports undo and redo as a single transaction on the parent page's Undo stack.
    ''' </summary>
    ''' <remarks></remarks>
    Public Class ChildPageSite
        Implements IPropertyPageSiteInternal
        Implements IVsProjectDesignerPageSite

        ''' <summary>
        ''' The character that separates the property page type name from the property name in the special mangled
        '''   property names that we create.
        ''' </summary>
        ''' <remarks></remarks>
        Public Const NestingCharacter As String = ":"

        Private m_wrappedInternalSite As IPropertyPageSiteInternal 'May *not* be Nothing
        Private m_wrappedUndoSite As IVsProjectDesignerPageSite    'May be Nothing
        Private m_nestedPropertyNamePrefix As String               'Prefix string to be placed at the beginning of PropertyName to distinguish properties from the page hosted by this child page site


        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="childPage">The child page that is to be hosted (required).</param>
        ''' <param name="wrappedInternalSite">The IPropertyPageSiteInternal site (required).</param>
        ''' <param name="wrappedUndoSite">The IVsProjectDesignerPageSite site (optional).</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal childPage As PropPageUserControlBase, ByVal wrappedInternalSite As IPropertyPageSiteInternal, ByVal wrappedUndoSite As IVsProjectDesignerPageSite)
            If childPage Is Nothing Then
                Debug.Fail("childPage missing")
                Throw New ArgumentNullException()
            End If
            If wrappedInternalSite Is Nothing Then
                Debug.Fail("Can't wrap a NULL site!")
                Throw New ArgumentNullException()
            End If
            m_wrappedInternalSite = wrappedInternalSite
            m_wrappedUndoSite = wrappedUndoSite
            m_nestedPropertyNamePrefix = childPage.GetType.FullName & NestingCharacter
        End Sub


        ''' <summary>
        ''' Returns whether or not the property page hosted in this site should be with 
        '''   immediate-apply mode or not)
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property IsImmediateApply() As Boolean Implements IPropertyPageSiteInternal.IsImmediateApply
            Get
                'Child pages are always non-immediate apply (we wait until the user clicks
                '  OK or Cancel)
                Return False
            End Get
        End Property


        ''' <summary>
        ''' Delegate to the wrapped site
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetLocaleID() As Integer Implements IPropertyPageSiteInternal.GetLocaleID
            Return m_wrappedInternalSite.GetLocaleID()
        End Function

        ''' <summary>
        ''' Delegate to the wrapped site
        ''' </summary>
        ''' <param name="ServiceType"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetService(ByVal ServiceType As System.Type) As Object Implements IPropertyPageSiteInternal.GetService
            Return m_wrappedInternalSite.GetService(ServiceType)
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="flags"></param>
        ''' <remarks></remarks>
        Public Sub OnStatusChange(ByVal flags As PROPPAGESTATUS) Implements IPropertyPageSiteInternal.OnStatusChange
            ' We do *not* want to propagate this to our internal site - that would cause this change to
            ' be immediately applied, which is not what we want for child (modal) property pages...
        End Sub

        ''' <summary>
        ''' Instructs the page site to process a keystroke if it desires.
        ''' </summary>
        ''' <param name="msg"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' This function can be called by a property page to give the site a chance to process a message
        '''   before the page does.  Return S_OK to indicate we have handled it, S_FALSE to indicate we did not
        '''   process it, and E_NOTIMPL to indicate that the site does not support keyboard processing.
        ''' </remarks>
        Public Function TranslateAccelerator(ByVal msg As System.Windows.Forms.Message) As Integer Implements IPropertyPageSiteInternal.TranslateAccelerator
            Return m_wrappedInternalSite.TranslateAccelerator(msg)
        End Function


#Region "Undo/redo support for child pages"

        ''' <summary>
        ''' Get a localized name for the undo transaction.  This name appears in the
        '''   Undo/Redo history dropdown in Visual Studio.
        ''' Delegate to wrapped undo site.
        ''' </summary>
        ''' <param name="description"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetTransaction(ByVal description As String) As System.ComponentModel.Design.DesignerTransaction Implements ManagedInterfaces.ProjectDesigner.IVsProjectDesignerPageSite.GetTransaction
            If m_wrappedUndoSite IsNot Nothing Then
                Return m_wrappedUndoSite.GetTransaction(description)
            End If

            Return Nothing
        End Function


        ''' <summary>
        ''' Called by the child page when a change occurs on the page (during Apply).
        ''' </summary>
        ''' <param name="propertyName"></param>
        ''' <param name="propertyDescriptor"></param>
        ''' <param name="oldValue"></param>
        ''' <param name="newValue"></param>
        ''' <remarks></remarks>
        Public Sub OnPropertyChanged(ByVal propertyName As String, ByVal propertyDescriptor As System.ComponentModel.PropertyDescriptor, ByVal oldValue As Object, ByVal newValue As Object) Implements ManagedInterfaces.ProjectDesigner.IVsProjectDesignerPageSite.OnPropertyChanged
            If m_wrappedUndoSite IsNot Nothing Then
                m_wrappedUndoSite.OnPropertyChanged(MungePropertyName(propertyName), propertyDescriptor, oldValue, newValue)
            End If
        End Sub


        ''' <summary>
        ''' Called by the child page when a change occurs on the page (during Apply).
        ''' </summary>
        ''' <param name="propertyName"></param>
        ''' <param name="propertyDescriptor"></param>
        ''' <remarks></remarks>
        Public Sub OnPropertyChanging(ByVal propertyName As String, ByVal propertyDescriptor As System.ComponentModel.PropertyDescriptor) Implements ManagedInterfaces.ProjectDesigner.IVsProjectDesignerPageSite.OnPropertyChanging
            If m_wrappedUndoSite IsNot Nothing Then
                m_wrappedUndoSite.OnPropertyChanging(MungePropertyName(propertyName), propertyDescriptor)
            End If
        End Sub


        ''' <summary>
        ''' Munges a property name into a form that combines that type name of the child page that the
        '''   property came from.
        ''' </summary>
        ''' <param name="propertyName"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function MungePropertyName(ByVal propertyName As String) As String
            'We need to mark properties as having coming from our hosted page.  We're forwarding undo/redo functionality to the same
            '  undo site (IVsPropertyDesignerPageSite) that handles the parent page, so that we create an undo/redo unit on the
            '  parent form that may be undone by the user.  But that means that the parent page will receive the requests (through
            '  IVsPropertyDesignerPage) for looking up and setting properties related to undo/redo functionality.  Prefixing the
            '  property name with the child page's type name lets the parent page know where to forward these requests.
            Return m_nestedPropertyNamePrefix & propertyName
        End Function

#End Region

    End Class

End Namespace