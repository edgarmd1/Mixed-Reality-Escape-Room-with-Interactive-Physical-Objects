using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Hands;

[RequireComponent(typeof(XRGrabInteractable))]
public class NaturalGrabHelper : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Tooltip("Require index, middle, ring, and little fingers to be curled to trigger grab")]
    private bool requireAllFingers = true;

    private XRGrabInteractable _grabInteractable;
    private List<IXRHoverInteractor> _hoveringInteractors = new List<IXRHoverInteractor>();
    private XRHandSubsystem _handSubsystem;
    private static readonly List<XRHandSubsystem> s_Subsystems = new List<XRHandSubsystem>();

    private IXRSelectInteractor _activeHandInteractor;
    private Handedness _activeHandedness = Handedness.Invalid;
    private bool _isGrabbedByHand = false;

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        _grabInteractable.hoverEntered.AddListener(OnHoverEntered);
        _grabInteractable.hoverExited.AddListener(OnHoverExited);

        SubsystemManager.GetSubsystems(s_Subsystems);
        if (s_Subsystems.Count > 0)
        {
            _handSubsystem = s_Subsystems[0];
            _handSubsystem.updatedHands += OnUpdatedHands;
        }
    }

    private void OnDisable()
    {
        _grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
        _grabInteractable.hoverExited.RemoveListener(OnHoverExited);

        if (_handSubsystem != null)
        {
            _handSubsystem.updatedHands -= OnUpdatedHands;
            _handSubsystem = null;
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (!_hoveringInteractors.Contains(args.interactorObject))
        {
            _hoveringInteractors.Add(args.interactorObject);
        }
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        _hoveringInteractors.Remove(args.interactorObject);
    }

    private void OnUpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags, XRHandSubsystem.UpdateType updateType)
    {
        if (updateType != XRHandSubsystem.UpdateType.Dynamic)
            return;

        if (_isGrabbedByHand)
        {
            if (_activeHandInteractor == null || !_grabInteractable.isSelected || !_grabInteractable.interactorsSelecting.Contains(_activeHandInteractor))
            {
                ResetGrabState();
                return;
            }

            var hand = _activeHandedness == Handedness.Left ? subsystem.leftHand : subsystem.rightHand;
            if (!hand.isTracked || !IsHandGrasping(hand))
            {
                if (_grabInteractable.interactionManager != null && _activeHandInteractor != null)
                {
                    _grabInteractable.interactionManager.SelectExit(_activeHandInteractor, _grabInteractable);
                }
                ResetGrabState();
            }
        }
        else
        {
            for (int i = _hoveringInteractors.Count - 1; i >= 0; i--)
            {
                var interactor = _hoveringInteractors[i];
                if (interactor == null)
                {
                    _hoveringInteractors.RemoveAt(i);
                    continue;
                }

                if (TryGetHandedness(interactor, out Handedness handedness))
                {
                    var hand = handedness == Handedness.Left ? subsystem.leftHand : subsystem.rightHand;
                    if (hand.isTracked && IsHandGrasping(hand))
                    {
                        if (interactor is IXRSelectInteractor selectInteractor)
                        {
                            if (!selectInteractor.isSelectActive && !_grabInteractable.isSelected)
                            {
                                _activeHandInteractor = selectInteractor;
                                _activeHandedness = handedness;
                                _isGrabbedByHand = true;

                                if (_grabInteractable.interactionManager != null)
                                {
                                    _grabInteractable.interactionManager.SelectEnter(_activeHandInteractor, _grabInteractable);
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    private void Update()
    {
        if (_handSubsystem == null)
        {
            SubsystemManager.GetSubsystems(s_Subsystems);
            if (s_Subsystems.Count > 0)
            {
                _handSubsystem = s_Subsystems[0];
                _handSubsystem.updatedHands += OnUpdatedHands;
            }
        }
    }

    private void ResetGrabState()
    {
        _activeHandInteractor = null;
        _activeHandedness = Handedness.Invalid;
        _isGrabbedByHand = false;
    }

    private bool TryGetHandedness(IXRHoverInteractor interactor, out Handedness handedness)
    {
        handedness = Handedness.Invalid;
        if (interactor == null || interactor.transform == null) return false;

        var trackingEvents = interactor.transform.GetComponentInParent<XRHandTrackingEvents>();
        if (trackingEvents == null)
        {
            trackingEvents = interactor.transform.GetComponentInChildren<XRHandTrackingEvents>();
        }

        if (trackingEvents != null)
        {
            handedness = trackingEvents.handedness;
            return handedness != Handedness.Invalid;
        }

        string nameLower = interactor.transform.gameObject.name.ToLower();
        Transform parent = interactor.transform.parent;
        while (parent != null && !nameLower.Contains("left") && !nameLower.Contains("right"))
        {
            nameLower += " " + parent.gameObject.name.ToLower();
            parent = parent.parent;
        }

        if (nameLower.Contains("left"))
        {
            handedness = Handedness.Left;
            return true;
        }
        else if (nameLower.Contains("right"))
        {
            handedness = Handedness.Right;
            return true;
        }

        return false;
    }

    private bool IsHandGrasping(XRHand hand)
    {
        bool indexGrabbing  = IsFingerGrabbing(hand, XRHandJointID.IndexTip, XRHandJointID.IndexProximal);
        bool middleGrabbing = IsFingerGrabbing(hand, XRHandJointID.MiddleTip, XRHandJointID.MiddleProximal);
        bool ringGrabbing   = IsFingerGrabbing(hand, XRHandJointID.RingTip, XRHandJointID.RingProximal);
        bool littleGrabbing = IsFingerGrabbing(hand, XRHandJointID.LittleTip, XRHandJointID.LittleProximal);

        if (requireAllFingers)
        {
            return indexGrabbing && middleGrabbing && ringGrabbing && littleGrabbing;
        }
        else
        {
            return middleGrabbing && ringGrabbing && littleGrabbing;
        }
    }

    private bool IsFingerGrabbing(XRHand hand, XRHandJointID tipId, XRHandJointID proximalId)
    {
        if (!(hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out var wristPose) &&
              hand.GetJoint(tipId).TryGetPose(out var tipPose) &&
              hand.GetJoint(proximalId).TryGetPose(out var proximalPose)))
        {
            return false;
        }

        var wristToTip = tipPose.position - wristPose.position;
        var wristToProximal = proximalPose.position - wristPose.position;
        return wristToProximal.sqrMagnitude >= wristToTip.sqrMagnitude;
    }
}
