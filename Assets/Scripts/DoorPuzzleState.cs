public static class DoorPuzzleState
{
    public static readonly System.Collections.Generic.HashSet<int> TablonesRotosIndices =
        new System.Collections.Generic.HashSet<int>();
    public static bool PuzzleCompletado { get; set; } = false;

    public static void Reset()
    {
        TablonesRotosIndices.Clear();
        PuzzleCompletado = false;
    }
}
