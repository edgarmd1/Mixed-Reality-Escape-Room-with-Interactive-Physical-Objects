using UnityEngine;

public class XROriginFloorAdjust : MonoBehaviour
{
    void Start()
    {
        Debug.LogWarning("[XROriginFloorAdjust] Este script ya no es necesario con el SDK de Meta " +
                         "(se autogestiona en OVRManager eligiendo Tracking Origin Type = Floor Level). " +
                         "Puedes eliminar este componente de tu GameObject.");
    }
}
