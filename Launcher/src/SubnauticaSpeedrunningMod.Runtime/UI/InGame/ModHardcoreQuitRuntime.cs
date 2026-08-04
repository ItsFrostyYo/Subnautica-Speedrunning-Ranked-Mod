using System;
using System.Collections;
using SubnauticaSpeedrunningMod.Runtime.Seeds;
using UWE;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SubnauticaSpeedrunningMod.Runtime.Ui
{
    internal static class ModHardcoreQuitRuntime
    {
        private const string ButtonObjectName = "ModHardcoreQuitWithoutSaving";
        private const string ConfirmationScreenName = "ModQuitWithoutSaveConfirmation";

        private static bool _installed;
        private static IngameMenu _observedMenu;
        private static bool _wasOpen;

        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            _installed = true;
            ModLog.Info("Hardcore quit-without-saving menu support is ready.");
        }

        public static void Update()
        {
            IngameMenu menu = IngameMenu.main;
            if (menu == null)
            {
                _observedMenu = null;
                _wasOpen = false;
                return;
            }

            bool isOpen = menu.gameObject.activeInHierarchy;
            if (!ReferenceEquals(menu, _observedMenu))
            {
                _observedMenu = menu;
                _wasOpen = false;
            }

            if (isOpen && !_wasOpen)
            {
                SyncMenu(menu);
            }

            _wasOpen = isOpen;
        }

        private static void SyncMenu(IngameMenu menu)
        {
            if (menu == null || menu.quitToMainMenuButton == null)
            {
                return;
            }

            Button button = FindButton(menu);
            bool shouldShow = ShouldShowButton();
            if (!shouldShow)
            {
                if (button != null)
                {
                    button.gameObject.SetActive(false);
                }

                return;
            }

            if (button == null)
            {
                button = CreateButton(menu);
            }

            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(true);
            button.interactable = true;
            button.transform.SetSiblingIndex(menu.quitToMainMenuButton.transform.GetSiblingIndex() + 1);

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = Language.main != null ? Language.main.Get("QuitToMainMenu") : "Quit";
            }
        }

        private static bool ShouldShowButton()
        {
            if (!GameModeUtils.IsPermadeath())
            {
                return false;
            }

            if (ModClientSessionMode.IsPracticeSaveSelected)
            {
                return false;
            }

            return !ModSeedRuntimeHost.IsRankedSingleplayerSeedActive() &&
                   !ModSeedRuntimeHost.IsRankedMultiplayerSeedActive();
        }

        private static Button FindButton(IngameMenu menu)
        {
            Transform existing = menu.transform.Find("Main/" + ButtonObjectName);
            if (existing == null)
            {
                existing = FindDescendant(menu.transform, ButtonObjectName);
            }

            return existing != null ? existing.GetComponent<Button>() : null;
        }

        private static Button CreateButton(IngameMenu menu)
        {
            try
            {
                Button source = menu.quitToMainMenuButton;
                GameObject clone = UnityEngine.Object.Instantiate(source.gameObject, source.transform.parent, false);
                clone.name = ButtonObjectName;

                Button button = clone.GetComponent<Button>();
                if (button == null)
                {
                    UnityEngine.Object.Destroy(clone);
                    return null;
                }

                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(new UnityAction(ShowNativeQuitConfirmation));
                clone.SetActive(true);
                return button;
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to create Hardcore quit-without-saving button: " + ex.Message);
                return null;
            }
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (string.Equals(child.name, name, StringComparison.Ordinal))
                {
                    return child;
                }

                Transform nested = FindDescendant(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static void ShowNativeQuitConfirmation()
        {
            IngameMenu menu = IngameMenu.main;
            if (menu == null)
            {
                return;
            }

            Transform confirmation = menu.transform.Find(ConfirmationScreenName);
            if (confirmation == null)
            {
                Transform source = menu.transform.Find("QuitConfirmation");
                if (source == null)
                {
                    ModLog.Warn("Hardcore quit confirmation screen could not be found.");
                    return;
                }

                GameObject clone = UnityEngine.Object.Instantiate(source.gameObject, menu.transform, false);
                clone.name = ConfirmationScreenName;
                confirmation = clone.transform;
                if (!ReplaceConfirmAction(confirmation))
                {
                    UnityEngine.Object.Destroy(clone);
                    ModLog.Warn("Hardcore quit confirmation Yes button could not be resolved.");
                    return;
                }

                clone.SetActive(false);
            }

            menu.ChangeSubscreen(ConfirmationScreenName);
        }

        private static bool ReplaceConfirmAction(Transform confirmation)
        {
            string localizedYes = Language.main != null ? Language.main.Get("Yes") : "Yes";
            Button[] buttons = confirmation.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                Text label = button != null ? button.GetComponentInChildren<Text>(true) : null;
                string text = label != null ? label.text : string.Empty;
                if (!string.Equals(text, localizedYes, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(text, "Yes", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(new UnityAction(StartQuitWithoutSaving));
                return true;
            }

            return false;
        }

        private static void StartQuitWithoutSaving()
        {
            CoroutineHost.StartCoroutine(QuitWithoutSavingAsync());
        }

        private static IEnumerator QuitWithoutSavingAsync()
        {
            if (SaveLoadManager.main != null)
            {
                while (SaveLoadManager.main.isSaving)
                {
                    yield return null;
                }
            }

            UWE.Utils.lockCursor = false;
            SceneCleaner.Open();
        }
    }
}
