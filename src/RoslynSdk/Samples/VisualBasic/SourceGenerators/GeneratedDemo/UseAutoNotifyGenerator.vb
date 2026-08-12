Option Explicit On
Option Strict On
Option Infer On

Imports AutoNotify

' The view model we'd like to augment
Partial Public Class ExampleViewModel

    <AutoNotify>
    Private _text As String = "private field text"

    <AutoNotify(PropertyName:="Count")>
    Private _amount As Integer = 5

End Class

Public Module UseAutoNotifyGenerator

    Public Sub Run()

        Dim vm As New ExampleViewModel()

        ' we didn't explicitly create the 'Text' property, it was generated for us 
        Dim text = vm.Text
        Console.WriteLine($"Text = {text}")

        ' Properties can have differnt names generated based on the PropertyName argument of the attribute
        Dim count = vm.Count
        Console.WriteLine($"Count = {count}")

        ' the viewmodel will automatically implement INotifyPropertyChanged
        AddHandler vm.PropertyChanged, Sub(o, e) Console.WriteLine($"Property {e.PropertyName} was changed")
        vm.Text = "abc"
        vm.Count = 123

        ' Try adding fields to the ExampleViewModel class above and tagging them with the <AutoNotify> attribute
        ' You'll see the matching generated properties visibile in IntelliSense in realtime

    End Sub

End Module
