using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Hands;

[RequireComponent(typeof(XRGrabInteractable))]
public class CamaraAutoGrabHelper : MonoBehaviour
{
    [Header("Ajustes de Agarre")]
    [SerializeField, Tooltip("Distancia en metros desde la mano.")]
    private float distanciaGrab = 0.15f;

    [SerializeField, Tooltip("Tiempo mínimo antes de soltar")]
    private float tiempoMinimoCogida = 0.5f;

    [Header("Umbral apertura de mano")]
    [SerializeField, Tooltip("Fracción mínima de dedos abiertos")]
    private float umbralDedosAbiertos = 0.8f;

    [Header("Detección de controlador")]
    [SerializeField, Tooltip("Activa el modo controller si detecta un mando físico.")]
    private bool autoActivarSinMando = true;

    private XRGrabInteractable _grab;
    private CamaraInteractable _camaraInteractable;
    private Unity.XR.CoreUtils.XROrigin _xrOrigin;
    private XRHandSubsystem _handSubsystem;
    private static readonly List<XRHandSubsystem> s_Subsystems = new();

    private Vector3 _localPosOffset;
    private Quaternion _localRotOffset;
    private Handedness _activeHandedness = Handedness.Invalid;
    private bool _siguiendoMano = false;
    private float _grabTime = 0f;

    private Vector3 _targetPosition;
    private Quaternion _targetRotation;

    private static readonly List<InputDevice> s_Devices = new();

    private void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _camaraInteractable = GetComponent<CamaraInteractable>();
        _xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
    }

    private void OnEnable()
    {
        ConectarSubsistema();
    }

    private void OnDisable()
    {
        DesconectarSubsistema();
        ResetearEstado();
    }

    private void Update()
    {
        if (_handSubsystem == null)
            ConectarSubsistema();

        if (autoActivarSinMando)
        {
            bool hayMando = HayMandoActivo();
            if (hayMando && !_grab.enabled)
            {
                _grab.enabled = true;
            }
            else if (!hayMando && _grab.enabled && !_siguiendoMano)
            {
                _grab.enabled = false;
            }
        }
    }

    private void LateUpdate()
    {
        if (!_siguiendoMano) return;

        transform.position = _targetPosition;
        transform.rotation = _targetRotation;
    }

    private void ConectarSubsistema()
    {
        SubsystemManager.GetSubsystems(s_Subsystems);
        if (s_Subsystems.Count > 0)
        {
            _handSubsystem = s_Subsystems[0];
            _handSubsystem.updatedHands += OnHandsActualizadas;
        }
    }

    private void DesconectarSubsistema()
    {
        if (_handSubsystem != null)
        {
            _handSubsystem.updatedHands -= OnHandsActualizadas;
            _handSubsystem = null;
        }
    }

    private void OnHandsActualizadas(
        XRHandSubsystem subsystem,
        XRHandSubsystem.UpdateSuccessFlags flags,
        XRHandSubsystem.UpdateType updateType)
    {
        if (updateType != XRHandSubsystem.UpdateType.Dynamic) return;

        if (_siguiendoMano)
        {
            XRHand hand = ObtenerMano(subsystem, _activeHandedness);
            if (!hand.isTracked) return;

            if (TryObtenerPosicionMano(hand, out Vector3 worldPalmPos, out Quaternion worldPalmRot))
            {
                _targetPosition = worldPalmPos + worldPalmRot * _localPosOffset;
                _targetRotation = worldPalmRot * _localRotOffset;
            }

            if (Time.time - _grabTime > tiempoMinimoCogida && ManoCasiAbierta(hand))
            {
                ResetearEstado();
                _camaraInteractable?.NotificarSoltadaPorMano();
            }
        }
        else
        {
            if (!HayMandoActivo())
            {
                ComprobarYGrabarMano(subsystem.leftHand, Handedness.Left);
                if (!_siguiendoMano)
                {
                    ComprobarYGrabarMano(subsystem.rightHand, Handedness.Right);
                }
            }
        }
    }

    private void ComprobarYGrabarMano(XRHand hand, Handedness handedness)
    {
        if (!hand.isTracked) return;

        if (TryObtenerPosicionMano(hand, out Vector3 worldPalmPos, out Quaternion worldPalmRot))
        {
            float dist = Vector3.Distance(worldPalmPos, transform.position);
            if (dist <= distanciaGrab)
            {
                _activeHandedness = handedness;
                _siguiendoMano = true;
                _grabTime = Time.time;

                _localPosOffset = Quaternion.Inverse(worldPalmRot) * (transform.position - worldPalmPos);
                _localRotOffset = Quaternion.Inverse(worldPalmRot) * transform.rotation;

                _targetPosition = transform.position;
                _targetRotation = transform.rotation;

                _camaraInteractable?.NotificarCogidaPorMano();
            }
        }
    }

    private bool TryObtenerPosicionMano(XRHand hand, out Vector3 posicionMundial, out Quaternion rotacionMundial)
    {
        posicionMundial = Vector3.zero;
        rotacionMundial = Quaternion.identity;

        var joint = hand.GetJoint(XRHandJointID.Palm);
        if (!joint.TryGetPose(out var pose))
        {
            joint = hand.GetJoint(XRHandJointID.Wrist);
            if (!joint.TryGetPose(out pose))
            {
                return false;
            }
        }

        if (_xrOrigin != null)
        {
            posicionMundial = _xrOrigin.transform.TransformPoint(pose.position);
            rotacionMundial = _xrOrigin.transform.rotation * pose.rotation;
        }
        else
        {
            posicionMundial = pose.position;
            rotacionMundial = pose.rotation;
        }

        return true;
    }

    public void ForzarSuelta()
    {
        ResetearEstado();
    }

    private bool ManoCasiAbierta(XRHand hand)
    {
        int total = 0;
        int abiertos = 0;

        ComprobarDedoAbierto(hand, XRHandJointID.IndexTip,  XRHandJointID.IndexProximal,  ref total, ref abiertos);
        ComprobarDedoAbierto(hand, XRHandJointID.MiddleTip, XRHandJointID.MiddleProximal, ref total, ref abiertos);
        ComprobarDedoAbierto(hand, XRHandJointID.RingTip,   XRHandJointID.RingProximal,   ref total, ref abiertos);
        ComprobarDedoAbierto(hand, XRHandJointID.LittleTip, XRHandJointID.LittleProximal, ref total, ref abiertos);
        ComprobarDedoAbierto(hand, XRHandJointID.ThumbTip,  XRHandJointID.ThumbProximal,  ref total, ref abiertos);

        if (total == 0) return false;
        return (float)abiertos / total >= umbralDedosAbiertos;
    }

    private static void ComprobarDedoAbierto(
        XRHand hand,
        XRHandJointID tipId,
        XRHandJointID proximalId,
        ref int total,
        ref int abiertos)
    {
        if (!(hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out var wristPose)   &&
              hand.GetJoint(tipId).TryGetPose(out var tipPose)                    &&
              hand.GetJoint(proximalId).TryGetPose(out var proximalPose)))
            return;

        total++;

        if ((tipPose.position - wristPose.position).sqrMagnitude >
            (proximalPose.position - wristPose.position).sqrMagnitude)
            abiertos++;
    }

    private void ResetearEstado()
    {
        _siguiendoMano = false;
        _activeHandedness = Handedness.Invalid;
        _grabTime = 0f;
    }

    private static XRHand ObtenerMano(XRHandSubsystem subsystem, Handedness handedness)
        => handedness == Handedness.Left ? subsystem.leftHand : subsystem.rightHand;

    private static bool HayMandoActivo()
    {
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand,
            s_Devices);

        foreach (var d in s_Devices)
        {
            if (d.isValid &&
                d.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) &&
                tracked)
                return true;
        }
        return false;
    }
}
