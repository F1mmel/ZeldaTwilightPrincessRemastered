using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class InteractableManager : MonoBehaviour
{
    public Transform UiContainer; // Container für die Interaktions-UI
    public Transform Player; // Referenz zum Spieler
    public KeyCode interactionKey = KeyCode.F; // Taste für die Interaktion

    public static InteractableManager Instance { get; private set; }

    public IInteractable currentInteractable; // Das aktuell nahe Interaktionsobjekt
    private List<IInteractable> interactables = new(); // Liste aller Interaktionsobjekte

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

        // Sicherstellen, dass die UI zu Beginn unsichtbar ist
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

        // Suche das nächste Interaktionsobjekt
        foreach (var interactable in interactables)
        {
            if(interactable == null) continue;
            var interactableGameObject = (MonoBehaviour)interactable;
            if (interactableGameObject == null) continue;
            float distance = Vector3.Distance(PlayerPosition, interactableGameObject.transform.position);

            if (distance < closestDistance && distance <= (interactable).InteractionRange) // Cast für spezifischen Zugriff
            {
                closestDistance = distance;
                closestInteractable = interactable;
            }
        }

        // UI entsprechend aktualisieren
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

        // Prüfen, ob die Interaktionstaste gedrückt wird
        if (currentInteractable != null && Input.GetKeyDown(interactionKey))
        {
            currentInteractable.Interact();
        }
    }

    public void ShowUI(string message, IInteractable interactable)
    {
        if (UiContainer == null)
            return;

        // Nachricht setzen
        UpdateInteractableMessage(message);

        // Smooth einblenden
        UiContainer.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
    }

    public void HideUI()
    {
        if (UiContainer == null)
            return;

        // Smooth ausblenden
        UiContainer.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack);
    }

    public void UpdateInteractableMessage(string text)
    {
        // Nachricht im UI-Container aktualisieren
        UiContainer.GetComponentInChildren<TextMeshProUGUI>().text = text;
    }
}


public interface IInteractable
{
    // Definieren Sie die Aktionen, die beim Interagieren stattfinden sollen
    void Interact();

    void End()
    {
        
    }

    string InteractionMessage { get; set; }
    float InteractionRange { get; set; }
}