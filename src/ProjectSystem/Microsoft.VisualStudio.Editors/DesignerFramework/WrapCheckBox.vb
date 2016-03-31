Option Strict On
Option Explicit On
Imports System.Drawing
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.DesignerFramework

    ''' ;WrapCheckBox
    ''' <summary>
    ''' The auto size behavior of CheckBox is the same as Button, 
    ''' it will resize itself to fit the text on one line. 
    ''' This component is a CheckBox that will wrap itself in a TableLayoutPanel or FlowLayoutPanel.
    ''' Based on JFosler's code on http://blogs.msdn.com/jfoscoding/articles/492559.aspx
    ''' </summary>
    Friend Class WrapCheckBox
        Inherits System.Windows.Forms.CheckBox

        Friend Sub New()
            MyBase.New()
            Me.AutoSize = True
        End Sub

        Protected Overrides Sub OnTextChanged(ByVal e As System.EventArgs)
            MyBase.OnTextChanged(e)
            CacheTextSize()
        End Sub

        Protected Overrides Sub OnFontChanged(ByVal e As System.EventArgs)
            MyBase.OnFontChanged(e)
            CacheTextSize()
        End Sub

        Public Overrides Function GetPreferredSize(ByVal proposedsize As System.Drawing.Size) As System.Drawing.Size
            Dim prefSize As Size = MyBase.GetPreferredSize(proposedsize)
            If (proposedsize.Width > 1) AndAlso _
                    (prefSize.Width > proposedsize.Width) AndAlso _
                    (Not String.IsNullOrEmpty(Me.Text) AndAlso _
                    Not proposedsize.Width.Equals(Int32.MaxValue) OrElse _
                    Not proposedsize.Height.Equals(Int32.MaxValue)) Then
                ' we have the possiblility of wrapping... back out the single line of text
                Dim bordersAndPadding As Size = prefSize - cachedSizeOfOneLineOfText
                ' add back in the text size, subtract baseprefsize.width and 3 from proposed size width 
                ' so they wrap properly
                Dim newConstraints As Size = proposedsize - bordersAndPadding - New Size(3, 0)

                ' guarding against errors with newConstraints.
                If newConstraints.Width < 0 Then
                    newConstraints.Width = 0
                End If
                If newConstraints.Height < 0 Then
                    newConstraints.Height = 0
                End If

                If (Not preferredSizeHash.ContainsKey(newConstraints)) Then
                    prefSize = bordersAndPadding + TextRenderer.MeasureText(Me.Text, Me.Font, _
                        newConstraints, TextFormatFlags.WordBreak)
                    preferredSizeHash(newConstraints) = prefSize
                Else
                    prefSize = preferredSizeHash(newConstraints)
                End If
            End If
            Return prefSize
        End Function

        Private Sub CacheTextSize()
            'When the text has changed, the preferredSizeHash is invalid...
            preferredSizeHash.Clear()

            If String.IsNullOrEmpty(Me.Text) Then
                cachedSizeOfOneLineOfText = System.Drawing.Size.Empty
            Else
                cachedSizeOfOneLineOfText = TextRenderer.MeasureText(Me.Text, Me.Font, _
                    New Size(Int32.MaxValue, Int32.MaxValue), TextFormatFlags.WordBreak)
            End If
        End Sub

        Private cachedSizeOfOneLineOfText As System.Drawing.Size = System.Drawing.Size.Empty
        Private preferredSizeHash As New Dictionary(Of Size, Size)()

    End Class
End Namespace
