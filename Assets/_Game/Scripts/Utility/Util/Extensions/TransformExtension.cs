using System;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine.EventSystems;

public static class TransformExtension
{
    public static void OnClick(this Transform transform, Action callback)
    {
        EventTrigger trigger = transform.AddComponent<EventTrigger>();
        var eventTrigger = new EventTrigger.Entry();
        eventTrigger.eventID = EventTriggerType.PointerClick;
        eventTrigger.callback.AddListener((data) => callback.Invoke());
        trigger.triggers.Add(eventTrigger);
    }
}