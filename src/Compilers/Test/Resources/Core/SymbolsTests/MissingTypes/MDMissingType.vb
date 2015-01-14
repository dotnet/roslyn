' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'vbc /t:library MDMissingTypeLib_1.vb /out:MDMissingTypeLib.dll
'vbc /t:library MDMissingType.vb /r:MDMissingTypeLib.dll
'rename MDMissingTypeLib.dll MDMissingTypeLib_New.dll
'vbc /t:library MDMissingTypeLib_2.vb /out:MDMissingTypeLib.dll

Class TC1
    Inherits MissingNS1.MissingC1
End Class

Class TC2
    Inherits MissingNS2.MissingNS3.MissingC2
End Class

Class TC3
    Inherits NS4.MissingNS5.MissingC3
End Class

Class TC4(Of T1, S1, U)
    Inherits MissingC4(Of T1, S1)
End Class

Class TC5(Of T1, S1, U1, V1, W1)
    Inherits MissingC4(Of T1, S1).MissingC5(Of U1, V1, W1)
End Class

Class TC6(Of U, V)
    Inherits C6.MissingC7(Of U, V)
End Class

Class TC7(Of U, V, W)
    Inherits C6.MissingC7(Of U, V).MissingC8 
End Class

Class TC8(Of U, V)
    Inherits C6.MissingC7(Of U, V).MissingC8.MissingC9  
End Class
