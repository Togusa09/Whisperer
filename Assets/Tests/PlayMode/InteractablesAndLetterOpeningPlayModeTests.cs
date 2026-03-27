using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Whisperer;

public class InteractablesAndLetterOpeningPlayModeTests
{
    [UnityTest]
    public IEnumerator DeskModeInteractable_TryInteract_EntersDeskModeInPlay()
    {
        GameObject switcherObject = new GameObject("mode-switcher");
        PlayerModeSwitcher switcher = switcherObject.AddComponent<PlayerModeSwitcher>();

        GameObject interactableObject = new GameObject("desk-interactable");
        DeskModeInteractable interactable = interactableObject.AddComponent<DeskModeInteractable>();

        yield return null;

        bool interacted = interactable.TryInteract(null);

        Assert.IsTrue(interacted);
        Assert.AreEqual(PlayerMode.Desk, switcher.CurrentMode);

        Object.Destroy(interactableObject);
        Object.Destroy(switcherObject);
    }

    [UnityTest]
    public IEnumerator EnvelopeItem_OpenFromDesk_SpawnsLetterAndDestroysEnvelope()
    {
        GameObject carryAnchor = new GameObject("carry-anchor");
        GameObject deskAnchor = new GameObject("desk-anchor");

        GameObject envelopeObject = new GameObject("envelope");
        envelopeObject.AddComponent<BoxCollider>();
        EnvelopeItem envelope = envelopeObject.AddComponent<EnvelopeItem>();

        GameObject openedLetterPrefab = new GameObject("opened-letter-prefab");
        openedLetterPrefab.AddComponent<BoxCollider>();
        openedLetterPrefab.AddComponent<LetterItem>();

        SetPrivateField(envelope, "openedLetterPrefab", openedLetterPrefab);

        envelope.PickUp(carryAnchor.transform);
        Assert.IsTrue(envelope.OpenAtDesk(deskAnchor.transform));

        Assert.IsTrue(envelope.OpenFromDesk(deskAnchor.transform));
        Assert.IsTrue(envelope.IsEnvelopeOpened);

        yield return null;

        Assert.IsTrue(envelope == null, "Envelope root should be destroyed after opening in play mode.");

        LetterItem[] letters = Object.FindObjectsByType<LetterItem>(FindObjectsSortMode.None);
        Assert.GreaterOrEqual(letters.Length, 1);

        Object.Destroy(openedLetterPrefab);
        for (int i = 0; i < letters.Length; i += 1)
        {
            if (letters[i] != null)
            {
                Object.Destroy(letters[i].gameObject);
            }
        }

        Object.Destroy(deskAnchor);
        Object.Destroy(carryAnchor);
    }

    static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}