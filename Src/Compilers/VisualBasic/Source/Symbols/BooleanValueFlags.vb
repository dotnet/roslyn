
Namespace Roslyn.Compilers.VisualBasic

    <Flags()>
    Friend Enum BooleanValueFlags As Byte
        Unknown = 0 ' Don't know the answer yet.
        Known = 1   ' If this bit is set, we already know the answer.
        [False] = 1 ' If value == [False], the answer is False.
        [True] = 3  ' If value == [True], the answer is True.
    End Enum

End Namespace