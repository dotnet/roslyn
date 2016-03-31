Imports EnvDTE


Namespace Microsoft.VisualStudio.Editors.Common

    ''' <summary>
    ''' Utilities related to the code model and code dom
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class CodeModelUtils

        ' When you want an exception that has no error message and 
        ' doesn't assert. Typically used when the user says "no" to 
        ' a "Continue?" question, and the code wants to throw a "silent"
        ' exception to abort the operation.
        Public Const HR_E_CSHARP_USER_CANCEL As Integer = &H80040103

        ''' <summary>
        ''' This is a shared class - disallow instantation.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub New()
        End Sub


        ''' <summary>
        ''' Searches through a code model tree for a given function that handles a particular event.
        ''' </summary>
        ''' <param name="Elements">CodeElements to search through</param>
        ''' <param name="EventName">Handled event name to search for</param>
        ''' <param name="EventHandlerFunctionName">Handled event handler function name to search for.  May be Nothing/empty.</param>
        ''' <param name="AllowMatchToOnlyEventName">If true, and no function matches both by handled event and name, 
        '''   will return any function that handles the given event, regardless of name</param>
        ''' <returns>The function which handles the given event and (optionally) has the given name.</returns>
        ''' <remarks></remarks>
        Public Shared Function FindEventHandler(ByVal Elements As CodeElements, ByVal EventName As String, ByVal EventHandlerFunctionName As String, ByVal AllowMatchToOnlyEventName As Boolean) As CodeFunction
            Dim ExistingHandler As CodeFunction

            'First try to match both event handler function name and handled event name
            ExistingHandler = FindEventHandlerHelper(Elements, EventName, EventHandlerFunctionName)
            If ExistingHandler Is Nothing AndAlso AllowMatchToOnlyEventName Then
                'If that doesn't work, try finding a function that handles the given event, regardless of name
                ExistingHandler = FindEventHandlerHelper(Elements, EventName, Nothing)
            End If

            Return ExistingHandler
        End Function


        ''' <summary>
        ''' Helper function for FindEventHandler.
        ''' </summary>
        ''' <param name="Elements">CodeElements to search through</param>
        ''' <param name="EventName">Handled event name to search for</param>
        ''' <param name="EventHandlerFunctionName">Handled event handler function name to search for.  May be Nothing/empty.</param>
        ''' <returns>The function which handles the given event and (optionally) has the given name.</returns>
        ''' <remarks>This function only works with VB as currently written.</remarks>
        Public Shared Function FindEventHandlerHelper(ByVal Elements As CodeElements, ByVal EventName As String, ByVal EventHandlerFunctionName As String) As CodeFunction
            For Each element As CodeElement In Elements
                If element.Kind = vsCMElement.vsCMElementFunction Then
                    Dim Func As CodeFunction = DirectCast(element, CodeFunction)
                    If Func.FunctionKind = vsCMFunction.vsCMFunctionSub Then
                        'Check the name
                        If EventHandlerFunctionName <> "" AndAlso Not String.Equals(Func.Name, EventHandlerFunctionName, StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        End If

                        'Verify that it handles the given event
                        Dim IEventHandler As Microsoft.VisualStudio.IEventHandler = TryCast(Func, Microsoft.VisualStudio.IEventHandler)
                        If IEventHandler IsNot Nothing Then
                            If IEventHandler.HandlesEvent(EventName) Then
                                'Yes, it does.  We have a match.
                                Return Func
                            End If
                        Else
                            Debug.Fail("This code wasn't written for a non-VB language.  Couldn't get IEventHandler from CodeFunction.")
                        End If
                    End If
                Else
                    'Dig deeper
                    'CONSIDER: using element.Children may not work for non-VB languages
                    Dim RecursiveResult As CodeFunction = FindEventHandlerHelper(element.Children, EventName, EventHandlerFunctionName)
                    If RecursiveResult IsNot Nothing Then
                        Return RecursiveResult
                    End If
                End If
            Next

            Return Nothing
        End Function


        ''' <summary>
        ''' Adds an event handler to a given class, if it doesn't already exist.
        ''' </summary>
        ''' <param name="CodeClass">The class in which to add the handler.</param>
        ''' <param name="EventName">The name of the event (e.g. "MyBase.Click")</param>
        ''' <param name="EventHandlerFunctionName">The name of the function (e.g. "Form1_Click")</param>
        ''' <param name="EventArgsType">The type of event args to use (e.g. GetType("System.EventArgs"))</param>
        ''' <param name="Access">The access level (private/friend/etc)</param>
        ''' <returns>The existing or newly-created event handler function.</returns>
        ''' <remarks>This function is currently written only to work with VB.</remarks>
        Public Shared Function TryAddEventHandler(ByVal CodeClass As CodeClass, ByVal EventName As String, ByVal EventHandlerFunctionName As String, ByVal EventArgsType As Type, ByVal Access As vsCMAccess) As CodeFunction
            Dim HandlerFunction As CodeFunction = FindEventHandler(CodeClass.Members, EventName, EventHandlerFunctionName, True)
            If HandlerFunction Is Nothing Then
                'Doesn't exist.  Let's add a handling method

                HandlerFunction = CodeClass.AddFunction(EventHandlerFunctionName, vsCMFunction.vsCMFunctionSub, Nothing, , Access)
                Dim IEventHandler As Microsoft.VisualStudio.IEventHandler = DirectCast(HandlerFunction, Microsoft.VisualStudio.IEventHandler)
                If IEventHandler IsNot Nothing Then
                    IEventHandler.AddHandler(EventName)
                    HandlerFunction.AddParameter("sender", "Object", 0)
                    HandlerFunction.AddParameter("e", EventArgsType.FullName, 1)
                Else
                    Debug.Fail("This code wasn't written for a non-VB language.  Couldn't get IEventHandler from CodeFunction.")
                End If
            End If
            Return HandlerFunction
        End Function


        ''' <summary>
        ''' Navigates to the given function in the code editor.
        ''' </summary>
        ''' <param name="Func">The function to navigate to.</param>
        ''' <remarks></remarks>
        Public Shared Sub NavigateToFunction(ByVal Func As CodeFunction)
            Try
                'Ensure the document is activated
                If Func.ProjectItem IsNot Nothing Then
                    If Not Func.ProjectItem.IsOpen Then
                        Func.ProjectItem.Open()
                    End If
                    Func.ProjectItem.Document.Activate()
                End If

                'Get the location to navigate to (vsCMPartNavigate gets us to the line
                '  and column where the user would start typing in the body of the
                '  function.
                Dim TextPoint As TextPoint = Func.GetStartPoint(vsCMPart.vsCMPartNavigate)

                'And navigate...
                TextPoint.Parent.Selection.MoveToPoint(TextPoint)
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                Debug.Fail("Navigate to function '" & Func.Name & "' failed: " & ex.ToString)
            End Try
        End Sub


        ''' <summary>
        ''' Searches for a class with a given name
        ''' </summary>
        ''' <param name="CodeNamespace">The namespace to search through</param>
        ''' <param name="ClassName">The class name to search for</param>
        ''' <returns>The CodeClass if found, otherwise Nothing.</returns>
        ''' <remarks></remarks>
        Public Shared Function FindCodeClass(ByVal CodeNamespace As CodeNamespace, ByVal ClassName As String) As CodeClass
            For Each Element As CodeElement In CodeNamespace.Members
                If Element.Kind = vsCMElement.vsCMElementClass Then
                    ' Consider, should we use language case sensitivity instead of ignore case here?
                    If Element.Name.Equals(ClassName, StringComparison.OrdinalIgnoreCase) Then
                        Return DirectCast(Element, CodeClass)
                    End If
                End If
            Next

            Return Nothing
        End Function


        ''' <summary>
        ''' Searches for a class with a given namespace and name
        ''' </summary>
        ''' <param name="CodeElements">The code elements to search through, which is assumed to contain namespaces</param>
        ''' <param name="NamespaceName">The namespace name to search for</param>
        ''' <param name="ClassName">The class name to search for</param>
        ''' <returns>The CodeClass if found, otherwise Nothing.</returns>
        ''' <remarks></remarks>
        Public Shared Function FindCodeClass(ByVal CodeElements As CodeElements, ByVal NamespaceName As String, ByVal ClassName As String) As CodeClass
            For Each Element As CodeElement In CodeElements
                If Element.Kind = vsCMElement.vsCMElementNamespace Then
                    ' Consider, should we use language case sensitivity instead of ignore case here?
                    If Element.Name.Equals(NamespaceName, StringComparison.OrdinalIgnoreCase) Then
                        Return FindCodeClass(DirectCast(Element, CodeNamespace), ClassName)
                    End If
                End If
            Next

            Return Nothing
        End Function

    End Class

End Namespace

