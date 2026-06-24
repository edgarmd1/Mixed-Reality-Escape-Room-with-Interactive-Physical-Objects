using System.Collections;
using UnityEngine;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(XROrigin))]
public class XROriginFloorAdjust : MonoBehaviour
{
    [Tooltip("Altura Y del suelo virtual.")]
    [SerializeField] private float alturaEstandarVR = 0f;

    private XROrigin _xrOrigin;

    void Awake()
    {
        _xrOrigin = GetComponent<XROrigin>();
    }

    void Start()
    {
        StartCoroutine(AjustarAltura());
    }

    private IEnumerator AjustarAltura()
    {
        Camera cam = _xrOrigin.Camera;
        if (cam == null)
        {
            yield break;
        }

        yield return null;

        float t = 0f;
        while (cam.transform.localPosition.y < 0.05f && t < 3f)
        {
            t += Time.deltaTime;
            yield return null;
        }

        float nuevaY = alturaEstandarVR - cam.transform.localPosition.y;
        Vector3 pos = transform.position;
        pos.y = nuevaY;
        transform.position = pos;
    }
}
