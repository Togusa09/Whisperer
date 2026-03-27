using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Whisperer;

public class InteractablesAndLetterOpeningEditModeTests
{
    [Test]
    public void DeskModeInteractable_TryInteract_EntersDeskMode()
    {
        GameObject switcherObject = new GameObject("mode-switcher");
        PlayerModeSwitcher switcher = switcherObject.AddComponent<PlayerModeSwitcher>();

        GameObject interactableObject = new GameObject("desk-interactable");
        DeskModeInteractable interactable = interactableObject.AddComponent<DeskModeInteractable>();

        bool interacted = interactable.TryInteract(null);

        Assert.IsTrue(interacted);
        Assert.AreEqual(PlayerMode.Desk, switcher.CurrentMode);
        Assert.AreEqual("Use Desk", interactable.InteractionPrompt);

        Object.DestroyImmediate(interactableObject);
        Object.DestroyImmediate(switcherObject);
    }

    [Test]
    public void LetterItem_OpenAtDesk_RequiresCarryAndMarksOpened()
    {
        GameObject carryAnchor = new GameObject("carry-anchor");
        GameObject deskAnchor = new GameObject("desk-anchor");

        GameObject letterObject = new GameObject("letter");
        letterObject.AddComponent<BoxCollider>();
        LetterItem letter = letterObject.AddComponent<LetterItem>();

        Assert.IsFalse(letter.OpenAtDesk(deskAnchor.transform));

        letter.PickUp(carryAnchor.transform);
        Assert.IsTrue(letter.OpenAtDesk(deskAnchor.transform));
        Assert.IsTrue(letter.IsOpenedAtDesk);
        Assert.AreEqual(deskAnchor.transform, letter.transform.parent);

        Object.DestroyImmediate(letterObject);
        Object.DestroyImmediate(deskAnchor);
        Object.DestroyImmediate(carryAnchor);
    }

    [Test]
    public void LetterItem_PickUpFromDesk_OnlyWorksAfterDeskPlacement()
    {
        GameObject carryAnchor = new GameObject("carry-anchor");
        GameObject deskAnchor = new GameObject("desk-anchor");

        GameObject letterObject = new GameObject("letter");
        letterObject.AddComponent<BoxCollider>();
        LetterItem letter = letterObject.AddComponent<LetterItem>();

        Assert.IsFalse(letter.PickUpFromDesk(carryAnchor.transform));

        letter.PlaceAtDesk(deskAnchor.transform);
        Assert.IsTrue(letter.PickUpFromDesk(carryAnchor.transform));
        Assert.IsTrue(letter.IsCarried);

        Object.DestroyImmediate(letterObject);
        Object.DestroyImmediate(deskAnchor);
        Object.DestroyImmediate(carryAnchor);
    }

    [Test]
    public void EnvelopeItem_OpenFromDesk_SpawnsLetterAndMarksOpened()
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
        Assert.IsTrue(envelope.CanOpenFromDesk);
        Assert.AreEqual("Open", envelope.GetInteractionPrompt(isDeskMode: true));

        Assert.IsTrue(envelope.OpenFromDesk(deskAnchor.transform));
        Assert.IsTrue(envelope.IsEnvelopeOpened);

        LetterItem spawnedLetter = Object.FindFirstObjectByType<LetterItem>();
        Assert.NotNull(spawnedLetter);
        Assert.IsTrue(spawnedLetter.IsOpenedAtDesk);

        Object.DestroyImmediate(openedLetterPrefab);
        Object.DestroyImmediate(spawnedLetter.gameObject);
        Object.DestroyImmediate(deskAnchor);
        Object.DestroyImmediate(carryAnchor);
    }

    [Test]
    public void DeskDrawerInteractable_StoresAndReturnsLetter()
    {
        GameObject carryAnchor = new GameObject("carry-anchor");
        GameObject drawerObject = new GameObject("LeftTopDrawer");
        drawerObject.transform.SetParent(new GameObject("PidgeonHoles").transform, worldPositionStays: false);
        DeskDrawerInteractable drawer = drawerObject.AddComponent<DeskDrawerInteractable>();

        GameObject letterObject = new GameObject("FiledLetter");
        letterObject.AddComponent<BoxCollider>();
        LetterItem letter = letterObject.AddComponent<LetterItem>();
        letter.PickUp(carryAnchor.transform);

        Assert.IsTrue(drawer.StoreItem(letter));
        Assert.AreEqual(1, drawer.StoredItemCount);
        Assert.IsFalse(letter.gameObject.activeSelf);

        Assert.IsTrue(drawer.TakeItem(letter, carryAnchor.transform));
        Assert.AreEqual(0, drawer.StoredItemCount);
        Assert.IsTrue(letter.gameObject.activeSelf);
        Assert.IsTrue(letter.IsCarried);

        Object.DestroyImmediate(drawerObject.transform.parent.gameObject);
        Object.DestroyImmediate(letterObject);
        Object.DestroyImmediate(carryAnchor);
    }

    [Test]
    public void GameCursorController_PushAndPopModalUi_TogglesModalState()
    {
        while (GameCursorController.IsModalUiActive)
        {
            GameCursorController.PopModalUi();
        }

        GameCursorController.PushModalUi();
        Assert.IsTrue(GameCursorController.IsModalUiActive);

        GameCursorController.PopModalUi();
        Assert.IsFalse(GameCursorController.IsModalUiActive);
    }

    static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}