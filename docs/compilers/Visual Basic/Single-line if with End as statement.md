# Single-line if with `End` keyword as statement

As reported in [#45158](https://github.com/dotnet/roslyn/issues/45158), it was possible to use something like the following until Visual Studio version (TODO: UNKNOWN):

```vb
Private Sub CloseMyApp(doExit As Boolean)
    If doExit Then End Else MessageBox.Show("Thanks!")
End Sub
```

Starting with Visual Studio version (TODO: UNKNOWN), there was an unintended breaking change that causes the previous snippet to generate a compile error (BC30678 - 'End' statement is not valid).

## Required action

You'll have to use multi-line if statement as follows:

```vb
Private Sub CloseMyApp(doExit As Boolean)
    If doExit Then
        End
    Else
        MessageBox.Show("Thanks!")
    End If
End Sub
```
