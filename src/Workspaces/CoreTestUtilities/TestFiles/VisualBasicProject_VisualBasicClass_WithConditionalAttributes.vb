' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

<Assembly: MyAttribute()> 

''' <summary>
''' This is a Visual Basic Class
''' </summary>
''' <remarks></remarks>
Public Class VisualBasicClass
    Inherits CSharpProject.CSharpClass

End Class

<Diagnostics.Conditional("EnableMyAttribute"), AttributeUsage(AttributeTargets.All, AllowMultiple:=True)> Class MyAttribute
    Inherits Attribute
End Class
