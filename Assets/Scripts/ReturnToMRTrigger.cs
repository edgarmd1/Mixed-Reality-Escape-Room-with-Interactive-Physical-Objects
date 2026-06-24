using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ReturnToMRTrigger : MonoBehaviour
{
    private BoxCollider _triggerZone;
    private KnockPuzzleManager _knockPuzzle;
    private bool _triggered = false;

    void Start()
    {
        _triggerZone = GetComponent<BoxCollider>();
        _triggerZone.isTrigger = true;

        _knockPuzzle = FindObjectOfType<KnockPuzzleManager>();
    }

    void Update()
    {
        if (_triggered || _knockPuzzle == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        if (PuntoEnBox(cam.transform.position))
        {
            _triggered = true;
            _knockPuzzle.OnVolverAMR();
        }
    }

    private bool PuntoEnBox(Vector3 worldPoint)
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        Vector3 center = _triggerZone.center;
        Vector3 halfSize = _triggerZone.size * 0.5f;

        return localPoint.x >= center.x - halfSize.x && localPoint.x <= center.x + halfSize.x &&
               localPoint.y >= center.y - halfSize.y && localPoint.y <= center.y + halfSize.y &&
               localPoint.z >= center.z - halfSize.z && localPoint.z <= center.z + halfSize.z;
    }
}
