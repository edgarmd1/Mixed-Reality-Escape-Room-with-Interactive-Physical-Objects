/// <summary>
/// Estado estático del puzzle de la puerta que persiste entre cargas de escena.
/// Al ser estático vive durante toda la sesión de juego (no se destruye con la escena).
/// </summary>
public static class DoorPuzzleState
{
    /// <summary>Índices de los tablones que ya han sido rotos (por su índice en el array del DoorPuzzleManager).</summary>
    public static readonly System.Collections.Generic.HashSet<int> TablonesRotosIndices =
        new System.Collections.Generic.HashSet<int>();

    /// <summary>El puzzle de destrucción de tablones está completado.</summary>
    public static bool PuzzleCompletado { get; set; } = false;

    /// <summary>Reinicia el estado (útil si se empieza una nueva partida).</summary>
    public static void Reset()
    {
        TablonesRotosIndices.Clear();
        PuzzleCompletado = false;
    }
}
