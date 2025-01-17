using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class InteractableManager : MonoBehaviour
{
    public Transform UiContainer;
    public Transform Player;
    public KeyCode interactionKey = KeyCode.F;

    public static InteractableManager Instance { get; private set; }

    public IInteractable currentInteractable;
    private List<IInteractable> interactables = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (UiContainer != null)
        {
            UiContainer.localScale = Vector3.zero;
        }
    }

    public Vector3 PlayerPosition => Player?.position ?? Vector3.zero;

    public void RegisterInteractable(IInteractable interactable)
    {
        if (!interactables.Contains(interactable))
        {
            interactables.Add(interactable);
        }
    }

    public void UnregisterInteractable(IInteractable interactable)
    {
        if (interactables.Contains(interactable))
        {
            interactables.Remove(interactable);
        }
    }

    private void Update()
    {
        float closestDistance = float.MaxValue;
        IInteractable closestInteractable = null;

        foreach (var interactable in interactables)
        {
            if(interactable == null) continue;
            var interactableGameObject = (MonoBehaviour)interactable;
            if (interactableGameObject == null) continue;
            float distance = Vector3.Distance(PlayerPosition, interactableGameObject.transform.position);

            if (distance < closestDistance && distance <= (interactable).InteractionRange)
            {
                closestDistance = distance;
                closestInteractable = interactable;
            }
        }

        if (closestInteractable != currentInteractable)
        {
            if (currentInteractable != null)
            {
                HideUI();
            }

            if (closestInteractable != null)
            {
                ShowUI((closestInteractable).InteractionMessage, closestInteractable);
            }
        }

        currentInteractable = closestInteractable;

        if (currentInteractable != null && Input.GetKeyDown(interactionKey))
        {
            currentInteractable.Interact();
        }
    }

    public void ShowUI(string message, IInteractable interactable)
    {
        if (UiContainer == null)
            return;

        UpdateInteractableMessage(message);

        UiContainer.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
    }

    public void HideUI()
    {
        if (UiContainer == null)
            return;

        UiContainer.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack);
    }

    public void UpdateInteractableMessage(string text)
    {
        UiContainer.GetComponentInChildren<TextMeshProUGUI>().text = text;
    }
}


public interface IInteractable
{
    void Interact();

    void End()
    {
        
    }

    string InteractionMessage { get; set; }
    float InteractionRange { get; set; }
}