using System;
using System.Collections.Generic;
using SubnauticaSpeedrunningMod.Runtime.Practice;
using SubnauticaSpeedrunningMod.Runtime;
using SubnauticaSpeedrunningMod.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubnauticaSpeedrunningMod.Runtime.Ui
{
    internal static class ModMainMenuRuntimeHost
    {
        private static readonly string ModClientWatermark = "Subnautica Speedrunning Mod [" + ModClientRelease.DisplayVersion + "]";
        private const string ModModeSelectGroupName = "ModModeSelect";
        private const string ModQueueGroupName = "ModQueue";
        private const string ModMatchmakingGroupName = "ModMatchmaking";
        private const string ModPrivateRaceRoomGroupName = "ModPrivateRaceRoom";
        private const string ModPracticeNewGameGroupName = "ModPracticeNewGame";
        private const string ModPracticeSaveGroupName = "ModPracticeSave";
        private const string LeaderboardHomeGroupName = "ModLeaderboardHome";
        private const string BetterRngSavedGamesButtonLabel = "Start a New BetterRNG Save";
        private const string RaceButtonLabel = "Race";
        private const string QueueRandomRaceComingSoonLabel = "Queue Random Race (Coming Soon)";
        private const string HostRaceLabel = "Host Race";
        private const string JoinRaceLabel = "Join Race";
        private const string SingleplayerRacePracticeLabel = "Singleplayer Race Practice";
        private const string FutureUpdatePlaceholderText = "Coming in a Future Update";
        private const string LeaderboardPlaceholderObjectName = "ModLeaderboardPlaceholder";
        private const string MatchmakingPlaceholderObjectName = "ModMatchmakingPlaceholder";
        private const string PrivateRaceRoomRootName = "ModPrivateRaceRoomRoot";
        private const string PrivateRaceRoomPlaceholderName = "ModPrivateRaceRoomPlaceholder";
        private const string PrivateRaceRoomHostTextName = "ModPrivateRaceRoomHost";
        private const string PrivateRaceRoomJoinerTextName = "ModPrivateRaceRoomJoiner";
        private const string PrivateRaceRoomCopyButtonName = "ModPrivateRaceRoomCopyButton";
        private const string PrivateRaceRoomModeRowName = "ModPrivateRaceRoomMode";
        private const string PrivateRaceRoomStartButtonName = "ModPrivateRaceRoomStart";
        private const string PrivateRaceRoomChoiceValueName = "ModPrivateRaceRoomChoiceValue";
        private const string UpdatePanelTitleText = "Update Beta-0.10.1";
        // Edit this message each release to show the newest client changes on the main menu.
        private const string UpdatePanelBodyText = "Added Hosting and Joining Races for 1v1ing Friends, Seeds will be Added in Future Updates, also updated Hardcore BetterRNG to have more Propulsion Cannon/MVB inside Wrecks in Grassy Plateau, aswell as having a Quit Button to Quit Without Saving.";
        private const string UpdatePanelBodyObjectName = "ModUpdatePanelBody";
        private const int WatermarkFontSize = 18;
        private const int QueueButtonFontSize = 34;
        private const int PanelTitleFontSize = 17;
        private const int UpdatePanelBodyFontSize = 14;
        private const int DisabledQueueButtonFontSize = 19;
        private const int DisabledModeSelectButtonFontSize = 24;
        private const int PracticeNewGameButtonFontSize = 34;
        private const int PracticeNewGameDisabledButtonFontSize = 24;
        private const int BetterRngSavedGamesButtonFontSize = 22;
        private const float ComingSoonRowAlpha = 0.42f;
        private const float MenuPatchRetryIntervalSeconds = 1.25f;
        private const float WatermarkScanIntervalSeconds = 2f;
        private const float PersistentWatermarkRetryIntervalSeconds = 2f;
        private const string FallbackWatermarkRootName = "SubnauticaSpeedrunningMod.FallbackWatermark";

        private static bool _installed;
        private static int _currentMenuInstanceId;
        private static bool _menuSummaryLogged;
        private static bool _menuSceneActive;
        private static bool _homePanelSuppressed;
        private static bool _leftMenuPatched;
        private static bool _rankedModeSelectPatched;
        private static bool _rankedQueuePatched;
        private static bool _rankedMatchmakingPatched;
        private static bool _privateRaceRoomPatched;
        private static bool _rankedPracticeNewGamePatched;
        private static bool _practiceSavePatched;
        private static bool _leaderboardHomePatched;
        private static string _lastPrivateRaceRoomLayoutState = string.Empty;
        private static float _nextMenuPatchAttemptAt;
        private static float _nextWatermarkScanAt;
        private static float _nextPersistentWatermarkRetryAt;
        private static ModUiRuntimeBehaviour _sceneRuntimeBehaviour;
        private static ModUiRuntimeBehaviour _persistentRuntimeBehaviour;
        private static GameObject _fallbackWatermarkRoot;
        private static Text _fallbackWatermarkText;
        private static GameObject _privateRaceConnectingModalRoot;
        private static Text _privateRaceConnectingModalTitleText;
        private static Text _privateRaceConnectingModalBodyText;
        private static Button _privateRaceConnectingModalActionButton;
        private static Button _privateRaceChoicePreviousArrowTemplate;
        private static Button _privateRaceChoiceNextArrowTemplate;
        private static bool _pendingPrivateRaceRoomCodePrompt;
        private static string _pendingPrivateRaceJoinDisplayName = string.Empty;

        public static void Install(RuntimeContext context)
        {
            if (_installed)
            {
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            _installed = true;
            ModLog.Info("Ranked UI host installed.");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _sceneRuntimeBehaviour = null;
            TryAttachPersistentRuntimeBehaviour();
            AttachSceneRuntimeBehaviour(scene);
            _fallbackWatermarkRoot = null;
            _fallbackWatermarkText = null;
            _nextWatermarkScanAt = 0f;
            _nextPersistentWatermarkRetryAt = 0f;
            _menuSceneActive = string.Equals(scene.name, "XMenu", StringComparison.Ordinal);

            if (_menuSceneActive)
            {
                if (ModClientSessionMode.IsPrivateRaceLocalSessionActive)
                {
                    ModPrivateRaceRoomRuntimeHost.ReturnToRoomAfterGameplay();
                }
                else
                {
                    ModClientSessionMode.ResetForMainMenu();
                    ModPrivateRaceRoomRuntimeHost.ResetForMainMenu();
                }

                ResetMenuState();
                _nextMenuPatchAttemptAt = 0f;
                ModLog.Info("Ranked UI detected XMenu scene load.");
            }
        }

        private static void OnRuntimeUpdate()
        {
            TryAttachPersistentRuntimeBehaviour();
            ModHardcoreQuitRuntime.Update();
            ModOverlayRuntime.SetWatermark(ModClientWatermark, true);
            ModPrivateRaceRoomRuntimeHost.Update();
            TryShowPendingPrivateRaceRoomCodePrompt();

            if (ShouldRefreshWatermarkPresentation() && Time.unscaledTime >= _nextWatermarkScanAt)
            {
                RefreshWatermarkPresentation();
                _nextWatermarkScanAt = Time.unscaledTime + WatermarkScanIntervalSeconds;
            }

            if (!_menuSceneActive)
            {
                HidePrivateRaceConnectingModal();
                return;
            }

            if (ModPrivateRaceRoomRuntimeHost.IsConnecting || ModPrivateRaceRoomRuntimeHost.IsError)
            {
                TrySuppressMainMenuHomePanel();
                EnsurePrimaryMenuVisible(uGUI_MainMenu.main, false);
                UpdatePrivateRaceConnectingModal();
                _nextMenuPatchAttemptAt = Time.unscaledTime + 0.1f;
                return;
            }

            bool privateRoomActive = ModPrivateRaceRoomRuntimeHost.IsRoomActive;
            bool optionsVisible = ModOptionsPanelRuntime.IsLivePanelVisible();

            if (privateRoomActive)
            {
                HidePrivateRaceConnectingModal();
                TrySuppressMainMenuHomePanel();
                if (Time.unscaledTime >= _nextMenuPatchAttemptAt)
                {
                    KeepPrivateRaceRoomActive();
                    _nextMenuPatchAttemptAt = Time.unscaledTime + 0.2f;
                }

                return;
            }

            HidePrivateRaceConnectingModal();

            if (!IsMainMenuPatchStable() || optionsVisible)
            {
                TrySuppressMainMenuHomePanel();
                if (optionsVisible)
                {
                    ModOptionsPanelRuntime.RefreshLivePanel();
                }

                _nextMenuPatchAttemptAt = Time.unscaledTime;
                return;
            }

            EnsurePrimaryMenuVisible(uGUI_MainMenu.main, true);

            if (Time.unscaledTime >= _nextMenuPatchAttemptAt)
            {
                TrySuppressMainMenuHomePanel();
                ModOptionsPanelRuntime.RefreshLivePanel();
                _nextMenuPatchAttemptAt = Time.unscaledTime + MenuPatchRetryIntervalSeconds;
            }
        }

        private static bool IsMainMenuPatchStable()
        {
            return _homePanelSuppressed &&
                   _leftMenuPatched &&
                   _rankedModeSelectPatched &&
                   _rankedQueuePatched &&
                   _privateRaceRoomPatched &&
                   _rankedPracticeNewGamePatched &&
                   _practiceSavePatched;
        }

        private static bool ShouldRefreshWatermarkPresentation()
        {
            if (_menuSceneActive)
            {
                return true;
            }

            return Player.main == null || !string.Equals(SceneManager.GetActiveScene().name, "Main", StringComparison.OrdinalIgnoreCase);
        }

        private static void TrySuppressMainMenuHomePanel()
        {
            if (uGUI_MainMenu.main == null || MainMenuRightSide.main == null)
            {
                return;
            }

            uGUI_MainMenu menu = uGUI_MainMenu.main;
            MainMenuRightSide rightSide = MainMenuRightSide.main;
            GameObject homeGroup = rightSide.homeGroup;
            if (homeGroup == null)
            {
                if (!_menuSummaryLogged)
                {
                    ModLog.Warn("Ranked UI could not find MainMenuRightSide.homeGroup.");
                    _menuSummaryLogged = true;
                }

                return;
            }

            int menuInstanceId = menu.GetInstanceID();
            if (_currentMenuInstanceId != menuInstanceId)
            {
                ResetMenuState();
                _currentMenuInstanceId = menuInstanceId;
            }

            if (!_menuSummaryLogged)
            {
                LogMainMenuHomeSummary(homeGroup);
                _menuSummaryLogged = true;
            }
            
            EnsureRankedMenuPatched(menu, rightSide);

            if (homeGroup.activeSelf || !_homePanelSuppressed)
            {
                homeGroup.SetActive(false);
                HideAllRightSideGroupsExceptHome(rightSide);
                EnsureLeaderboardHomeVisible(rightSide);
                _homePanelSuppressed = true;
                RestorePrimaryMenuNavigation(menu);
                ModLog.Info("Suppressed main menu right-side home panel.");
            }

            EnsureLeaderboardHomeVisible(rightSide);
        }

        private static void RestorePrimaryMenuNavigation(uGUI_MainMenu menu)
        {
            if (menu.primaryOptions == null)
            {
                return;
            }

            menu.ShowPrimaryOptions(true);
            if (GamepadInputModule.current != null)
            {
                GamepadInputModule.current.SetCurrentGrid(menu.primaryOptions);
            }

            menu.primaryOptions.DeselectItem();
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private static void EnsurePrimaryMenuVisible(uGUI_MainMenu menu, bool isVisible)
        {
            if (menu == null || menu.primaryOptions == null)
            {
                return;
            }

            if (isVisible)
            {
                menu.ShowPrimaryOptions(true);
            }

            GraphicRaycaster raycaster = menu.primaryOptions.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = isVisible;
            }

            CanvasGroup canvasGroup = menu.primaryOptions.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = menu.primaryOptions.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
            canvasGroup.alpha = isVisible ? 1f : 0f;
        }

        private static void KeepPrivateRaceRoomActive()
        {
            if (uGUI_MainMenu.main == null || MainMenuRightSide.main == null)
            {
                return;
            }
            MainMenuRightSide rightSide = MainMenuRightSide.main;

            EnsurePrivateRaceRoomGroup(rightSide);
            GameObject roomGroup = FindGroup(rightSide, ModPrivateRaceRoomGroupName);
            if (roomGroup == null)
            {
                EnsurePrimaryMenuVisible(uGUI_MainMenu.main, true);
                ModLog.Warn("Private room panel was unavailable. Falling back to the mode select menu instead of leaving the screen blank.");
                OpenRankedModeSelect(uGUI_MainMenu.main, rightSide, FindPrimaryButton("ButtonRanked"));
                return;
            }

            EnsurePrimaryMenuVisible(uGUI_MainMenu.main, false);
            rightSide.OpenGroup(ModPrivateRaceRoomGroupName);
            SyncPrivateRaceRoomGroup(rightSide);
        }

        private static void LogMainMenuHomeSummary(GameObject homeGroup)
        {
            int storeButtons = CountStoreButtons(homeGroup);
            int newsletterWidgets = CountNewsletterWidgets(homeGroup);
            int comingSoonMatches = CountTextsContaining(homeGroup, "coming soon");
            int readMoreMatches = CountTextsContaining(homeGroup, "read more");
            ModLog.Info(
                "Main menu home group detected: name='" + homeGroup.name +
                "', storeButtons=" + storeButtons +
                ", newsletterWidgets=" + newsletterWidgets +
                ", comingSoonTextMatches=" + comingSoonMatches +
                ", readMoreTextMatches=" + readMoreMatches + ".");
        }

        private static int CountStoreButtons(GameObject root)
        {
            int count = 0;
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                if (ButtonCallsMethod(button, "OnButtonStore") || HasText(button.gameObject, "Subnautica Store"))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountNewsletterWidgets(GameObject root)
        {
            int count = 0;
            count += root.GetComponentsInChildren<MainMenuEmailHandler>(true).Length;
            count += root.GetComponentsInChildren<MainMenuEmailCollector>(true).Length;
            count += root.GetComponentsInChildren<EmailBox>(true).Length;
            return count;
        }

        private static int CountTextsContaining(GameObject root, string value)
        {
            int count = 0;
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null || string.IsNullOrEmpty(text.text))
                {
                    continue;
                }

                if (text.text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool ButtonCallsMethod(Button button, string methodName)
        {
            if (button == null || button.onClick == null)
            {
                return false;
            }

            int eventCount = button.onClick.GetPersistentEventCount();
            for (int i = 0; i < eventCount; i++)
            {
                string currentMethod = button.onClick.GetPersistentMethodName(i);
                if (string.Equals(currentMethod, methodName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasText(GameObject root, string value)
        {
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null || string.IsNullOrEmpty(text.text))
                {
                    continue;
                }

                if (text.text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ResetMenuState()
        {
            _currentMenuInstanceId = 0;
            _menuSummaryLogged = false;
            _homePanelSuppressed = false;
            _leftMenuPatched = false;
            _rankedModeSelectPatched = false;
            _rankedQueuePatched = false;
            _rankedMatchmakingPatched = false;
            _privateRaceRoomPatched = false;
            _rankedPracticeNewGamePatched = false;
            _practiceSavePatched = false;
            _leaderboardHomePatched = false;
            _lastPrivateRaceRoomLayoutState = string.Empty;
        }

        private static void EnsureRankedMenuPatched(uGUI_MainMenu menu, MainMenuRightSide rightSide)
        {
            if (!_leftMenuPatched)
            {
                PatchPrimaryOptions(menu, rightSide);
                _leftMenuPatched = true;
            }
            else
            {
                SyncPrimaryOptionLabels(menu);
            }

            if (!_rankedModeSelectPatched)
            {
                EnsureRankedModeSelectGroup(rightSide);
                _rankedModeSelectPatched = true;
            }
            else
            {
                SyncRankedModeSelectLabels(rightSide);
            }

            if (!_rankedQueuePatched)
            {
                EnsureRankedQueueGroup(rightSide);
                _rankedQueuePatched = true;
            }
            else
            {
                SyncRankedQueueLabels(rightSide);
            }

            if (!_rankedMatchmakingPatched)
            {
                EnsureRankedMatchmakingGroup(rightSide);
                _rankedMatchmakingPatched = true;
            }
            else
            {
                SyncRankedMatchmakingGroup(rightSide);
            }

            if (!_privateRaceRoomPatched)
            {
                EnsurePrivateRaceRoomGroup(rightSide);
                _privateRaceRoomPatched = true;
            }
            else
            {
                SyncPrivateRaceRoomGroup(rightSide);
            }

            if (!_rankedPracticeNewGamePatched)
            {
                EnsureRankedPracticeNewGameGroup(rightSide);
                _rankedPracticeNewGamePatched = true;
            }
            else
            {
                SyncRankedPracticeNewGameGroup(rightSide);
            }

            if (!_practiceSavePatched)
            {
                EnsurePracticeSaveGroup(rightSide);
                _practiceSavePatched = true;
            }
            else
            {
                SyncPracticeSaveGroup(rightSide);
            }

            if (!_leaderboardHomePatched)
            {
                EnsureLeaderboardHomeGroup(rightSide);
                _leaderboardHomePatched = true;
            }
            else
            {
                SyncLeaderboardHomeGroup(rightSide);
            }

            RestoreVanillaSingleplayerPanelPresentation(rightSide);
            EnsureBetterRngSavedGamesButton(rightSide);
        }

        private static void PatchPrimaryOptions(uGUI_MainMenu menu, MainMenuRightSide rightSide)
        {
            if (menu.primaryOptions == null)
            {
                return;
            }

            Transform playTransform = FindDescendantByName(menu.primaryOptions.transform, "ButtonPlay");
            Button playButton = playTransform != null ? playTransform.GetComponent<Button>() : null;
            if (playButton == null)
            {
                ModLog.Warn("Ranked UI could not find ButtonPlay.");
                return;
            }

            Transform menuButtons = playButton.transform.parent;
            playButton.onClick.AddListener(new UnityAction(delegate
            {
                ModClientSessionMode.SelectVanilla();
            }));

            Transform practiceTransform = menuButtons.Find("ButtonPractice");
            if (practiceTransform == null)
            {
                GameObject practiceObject = UnityEngine.Object.Instantiate(playButton.gameObject, menuButtons, false);
                practiceObject.name = "ButtonPractice";
                practiceObject.transform.SetSiblingIndex(playButton.transform.GetSiblingIndex());
                RemoveLocalizationComponents(practiceObject);
                SetButtonLabel(practiceObject, "Practice");

                Button practiceButton = practiceObject.GetComponent<Button>();
                if (practiceButton != null)
                {
                    practiceButton.onClick = new Button.ButtonClickedEvent();
                    practiceButton.onClick.AddListener(new UnityAction(delegate
                    {
                        OpenPracticeSaveGroup(menu, rightSide, practiceButton);
                    }));
                }

                ResetMainMenuButtonVisualState(practiceObject);
                ModLog.Info("Inserted Practice button above Ranked.");
            }
            else
            {
                SetButtonLabel(practiceTransform.gameObject, "Practice");
            }

            Transform modTransform = menuButtons.Find("ButtonRanked");
            if (modTransform == null)
            {
                GameObject modObject = UnityEngine.Object.Instantiate(playButton.gameObject, menuButtons, false);
                modObject.name = "ButtonRanked";
                modObject.transform.SetSiblingIndex(playButton.transform.GetSiblingIndex());
                RemoveLocalizationComponents(modObject);
                SetButtonLabel(modObject, RaceButtonLabel);

                Button modButton = modObject.GetComponent<Button>();
                if (modButton != null)
                {
                    modButton.onClick = new Button.ButtonClickedEvent();
                    modButton.onClick.AddListener(new UnityAction(delegate
                    {
                        OpenRankedModeSelect(menu, rightSide, modButton);
                    }));
                }

                ResetMainMenuButtonVisualState(modObject);
                ModLog.Info("Inserted Race button above Play.");
            }
            else
            {
                SetButtonLabel(modTransform.gameObject, RaceButtonLabel);
            }
        }

        private static void EnsureRankedModeSelectGroup(MainMenuRightSide rightSide)
        {
            GameObject modeSelectGroup = FindGroup(rightSide, ModModeSelectGroupName);
            if (modeSelectGroup == null)
            {
                GameObject savedGamesGroup = FindGroup(rightSide, "SavedGames");
                if (savedGamesGroup == null)
                {
                    ModLog.Warn("Ranked UI could not find SavedGames group to clone for Choose Mode.");
                    return;
                }

                modeSelectGroup = UnityEngine.Object.Instantiate(savedGamesGroup, savedGamesGroup.transform.parent, false);
                modeSelectGroup.name = ModModeSelectGroupName;
                modeSelectGroup.transform.SetSiblingIndex(savedGamesGroup.transform.GetSiblingIndex() + 1);
                modeSelectGroup.SetActive(false);
                rightSide.groups.Add(modeSelectGroup);
            }

            DisableSavedGameBehaviors(modeSelectGroup);
            ConfigureRankedModeSelectGroup(modeSelectGroup);
        }

        private static void EnsureRankedQueueGroup(MainMenuRightSide rightSide)
        {
            GameObject modGroup = FindGroup(rightSide, ModQueueGroupName);
            if (modGroup == null)
            {
                GameObject savedGamesGroup = FindGroup(rightSide, "SavedGames");
                if (savedGamesGroup == null)
                {
                    ModLog.Warn("Ranked UI could not find SavedGames group to clone.");
                    return;
                }

                modGroup = UnityEngine.Object.Instantiate(savedGamesGroup, savedGamesGroup.transform.parent, false);
                modGroup.name = ModQueueGroupName;
                modGroup.transform.SetSiblingIndex(savedGamesGroup.transform.GetSiblingIndex() + 1);
                modGroup.SetActive(false);
                rightSide.groups.Add(modGroup);
            }

            DisableSavedGameBehaviors(modGroup);
            ConfigureRankedQueueGroup(modGroup);
        }

        private static void EnsureRankedMatchmakingGroup(MainMenuRightSide rightSide)
        {
            GameObject matchmakingGroup = FindGroup(rightSide, ModMatchmakingGroupName);
            if (matchmakingGroup == null)
            {
                GameObject savedGamesGroup = FindGroup(rightSide, "SavedGames");
                if (savedGamesGroup == null)
                {
                    ModLog.Warn("Ranked UI could not find SavedGames group to clone for Matchmaking.");
                    return;
                }

                matchmakingGroup = UnityEngine.Object.Instantiate(savedGamesGroup, savedGamesGroup.transform.parent, false);
                matchmakingGroup.name = ModMatchmakingGroupName;
                matchmakingGroup.transform.SetSiblingIndex(savedGamesGroup.transform.GetSiblingIndex() + 1);
                matchmakingGroup.SetActive(false);
                rightSide.groups.Add(matchmakingGroup);
            }

            DisableSavedGameBehaviors(matchmakingGroup);
            ConfigureRankedMatchmakingGroup(matchmakingGroup);
        }

        private static void EnsurePrivateRaceRoomGroup(MainMenuRightSide rightSide)
        {
            GameObject roomGroup = FindGroup(rightSide, ModPrivateRaceRoomGroupName);
            if (roomGroup == null)
            {
                GameObject optionsGroup = FindGroup(rightSide, "Options");
                if (optionsGroup == null)
                {
                    ModLog.Warn("Ranked UI could not find Options group to clone for Private Race Room.");
                    return;
                }

                roomGroup = UnityEngine.Object.Instantiate(optionsGroup, optionsGroup.transform.parent, false);
                roomGroup.name = ModPrivateRaceRoomGroupName;
                roomGroup.transform.SetSiblingIndex(optionsGroup.transform.GetSiblingIndex() + 1);
                roomGroup.SetActive(false);
                rightSide.groups.Add(roomGroup);

                CachePrivateRaceChoiceTemplates(roomGroup);
                ConfigurePrivateRaceRoomGroup(roomGroup);
            }
        }

        private static void SyncPrivateRaceRoomGroup(MainMenuRightSide rightSide)
        {
            GameObject roomGroup = FindGroup(rightSide, ModPrivateRaceRoomGroupName);
            if (roomGroup == null)
            {
                return;
            }

            ConfigurePrivateRaceRoomGroup(roomGroup);
        }

        private static void EnsureRankedPracticeNewGameGroup(MainMenuRightSide rightSide)
        {
            GameObject practiceGroup = FindGroup(rightSide, ModPracticeNewGameGroupName);
            if (practiceGroup == null)
            {
                GameObject savedGamesGroup = FindGroup(rightSide, "SavedGames");
                if (savedGamesGroup == null)
                {
                    ModLog.Warn("Ranked UI could not find SavedGames group to clone for Ranked Singleplayer Practice.");
                    return;
                }

                practiceGroup = UnityEngine.Object.Instantiate(savedGamesGroup, savedGamesGroup.transform.parent, false);
                practiceGroup.name = ModPracticeNewGameGroupName;
                practiceGroup.transform.SetSiblingIndex(savedGamesGroup.transform.GetSiblingIndex() + 1);
                practiceGroup.SetActive(false);
                rightSide.groups.Add(practiceGroup);
            }

            DisableSavedGameBehaviors(practiceGroup);
            ConfigureRankedPracticeNewGameGroup(practiceGroup);
        }

        private static void EnsurePracticeSaveGroup(MainMenuRightSide rightSide)
        {
            GameObject practiceSaveGroup = FindGroup(rightSide, ModPracticeSaveGroupName);
            if (practiceSaveGroup == null)
            {
                GameObject savedGamesGroup = FindGroup(rightSide, "SavedGames");
                if (savedGamesGroup == null)
                {
                    ModLog.Warn("Ranked UI could not find SavedGames group to clone for Practice saves.");
                    return;
                }

                practiceSaveGroup = UnityEngine.Object.Instantiate(savedGamesGroup, savedGamesGroup.transform.parent, false);
                practiceSaveGroup.name = ModPracticeSaveGroupName;
                practiceSaveGroup.transform.SetSiblingIndex(savedGamesGroup.transform.GetSiblingIndex() + 1);
                practiceSaveGroup.SetActive(false);
                rightSide.groups.Add(practiceSaveGroup);
            }

            DisableSavedGameBehaviors(practiceSaveGroup);
            ConfigurePracticeSaveGroup(practiceSaveGroup);
        }

        private static void EnsureLeaderboardHomeGroup(MainMenuRightSide rightSide)
        {
            GameObject leaderboardGroup = FindGroup(rightSide, LeaderboardHomeGroupName);
            if (leaderboardGroup == null)
            {
                GameObject savedGamesGroup = FindGroup(rightSide, "SavedGames");
                if (savedGamesGroup == null)
                {
                    ModLog.Warn("Ranked UI could not find SavedGames group to clone for Leaderboard.");
                    return;
                }

                leaderboardGroup = UnityEngine.Object.Instantiate(savedGamesGroup, savedGamesGroup.transform.parent, false);
                leaderboardGroup.name = LeaderboardHomeGroupName;
                leaderboardGroup.transform.SetSiblingIndex(savedGamesGroup.transform.GetSiblingIndex());
                leaderboardGroup.SetActive(false);
                rightSide.groups.Add(leaderboardGroup);
            }

            DisableSavedGameBehaviors(leaderboardGroup);
            ConfigureLeaderboardHomeGroup(leaderboardGroup);
        }

        private static void ConfigureRankedModeSelectGroup(GameObject modeSelectGroup)
        {
            Transform content = modeSelectGroup.transform.Find("Scroll View/Viewport/SavedGameAreaContent");
            if (content == null)
            {
                ModLog.Warn("Ranked UI could not find SavedGameAreaContent in Choose Mode panel.");
                return;
            }

            Transform template = content.Find("NewGame");
            if (template == null)
            {
                ModLog.Warn("Ranked UI could not find NewGame template row in Choose Mode panel.");
                return;
            }

            DestroySiblingRows(content, template);

            template.gameObject.SetActive(true);
            template.name = "NewGame";
            template.SetSiblingIndex(0);
            ConfigureActionRow(
                template.gameObject,
                QueueRandomRaceComingSoonLabel,
                false,
                26,
                null);

            GameObject hostRow = UnityEngine.Object.Instantiate(template.gameObject, content, false);
            hostRow.name = "NewGame";
            hostRow.transform.SetSiblingIndex(1);
            ConfigureActionRow(
                hostRow,
                HostRaceLabel,
                true,
                26,
                delegate
                {
                    PromptAndBeginPrivateRaceHost();
                });

            GameObject joinRow = UnityEngine.Object.Instantiate(template.gameObject, content, false);
            joinRow.name = "NewGame";
            joinRow.transform.SetSiblingIndex(2);
            ConfigureActionRow(
                joinRow,
                JoinRaceLabel,
                true,
                26,
                delegate
                {
                    PromptAndBeginPrivateRaceJoin();
                });

            GameObject practiceRow = UnityEngine.Object.Instantiate(template.gameObject, content, false);
            practiceRow.name = "NewGame";
            practiceRow.transform.SetSiblingIndex(3);
            ConfigureActionRow(
                practiceRow,
                SingleplayerRacePracticeLabel,
                true,
                26,
                delegate
                {
                    ModClientSessionMode.SelectRankedSingleplayerPractice();
                    OpenRankedPracticeNewGame(uGUI_MainMenu.main, MainMenuRightSide.main, FindPrimaryButton("ButtonRanked"));
                });

            SetPanelTitle(modeSelectGroup, "Choose Mode");
        }

        private static void ConfigureRankedQueueGroup(GameObject modGroup)
        {
            Transform content = modGroup.transform.Find("Scroll View/Viewport/SavedGameAreaContent");
            if (content == null)
            {
                ModLog.Warn("Ranked UI could not find SavedGameAreaContent in ModQueue.");
                return;
            }

            Transform template = content.Find("NewGame");
            if (template == null)
            {
                ModLog.Warn("Ranked UI could not find NewGame template row in ModQueue.");
                return;
            }

            DestroySiblingRows(content, template);

            QueueRowDefinition[] rows = new QueueRowDefinition[]
            {
                new QueueRowDefinition("Queue Survival\nComing in Future Update", isInteractable: false, DisabledQueueButtonFontSize),
                new QueueRowDefinition("Queue Creative\nComing in Future Update", isInteractable: false, DisabledQueueButtonFontSize),
                new QueueRowDefinition("Hardcore\nComing in Future Update", isInteractable: false, DisabledQueueButtonFontSize)
            };

            template.gameObject.SetActive(true);
            template.name = "NewGame";
            template.SetSiblingIndex(0);
            ConfigureQueueRow(template.gameObject, rows[0]);

            for (int i = 1; i < rows.Length; i++)
            {
                GameObject row = UnityEngine.Object.Instantiate(template.gameObject, content, false);
                row.name = "NewGame";
                row.transform.SetSiblingIndex(i);
                ConfigureQueueRow(row, rows[i]);
            }

            SetPanelTitle(modGroup, "Queue Random Race");
            ModLog.Info("Configured ModQueue group with " + rows.Length + " queue buttons.");
        }

        private static void ConfigureRankedMatchmakingGroup(GameObject matchmakingGroup)
        {
            if (matchmakingGroup == null)
            {
                return;
            }

            Transform content = matchmakingGroup.transform.Find("Scroll View/Viewport/SavedGameAreaContent");
            if (content == null)
            {
                ModLog.Warn("Ranked UI could not find SavedGameAreaContent in Matchmaking panel.");
                return;
            }

            Transform template = content.Find("NewGame");
            if (template == null)
            {
                ModLog.Warn("Ranked UI could not find NewGame template row in Matchmaking panel.");
                return;
            }

            DestroySiblingRows(content, template);

            template.gameObject.SetActive(true);
            template.name = "NewGame";
            template.SetSiblingIndex(0);
            ConfigureActionRow(
                template.gameObject,
                "Cancel Matchmaking",
                true,
                26,
                delegate
                {
                    ModRankedMatchmakingRuntimeHost.CancelQueue();
                    OpenRankedQueueGroup(uGUI_MainMenu.main, MainMenuRightSide.main, FindPrimaryButton("ButtonRanked"));
                });

            EnsurePanelPlaceholder(
                matchmakingGroup,
                content,
                MatchmakingPlaceholderObjectName,
                ModRankedMatchmakingRuntimeHost.GetPanelMessage());
            SetPanelTitle(matchmakingGroup, ModRankedMatchmakingRuntimeHost.GetPanelTitle());
        }

        private static void ConfigurePrivateRaceRoomGroup(GameObject roomGroup)
        {
            if (roomGroup == null)
            {
                return;
            }

            roomGroup.transform.SetAsLastSibling();

            uGUI_OptionsPanel panel = roomGroup.GetComponent<uGUI_OptionsPanel>();
            if (panel == null)
            {
                panel = roomGroup.GetComponentInChildren<uGUI_OptionsPanel>(true);
            }

            if (panel == null || panel.tabsContainer == null || panel.panesContainer == null)
            {
                ModLog.Warn("Ranked UI could not find options panel components in Private Race Room panel.");
                return;
            }

            ConfigurePrivateRaceLeaveButton(roomGroup);
            EnsureSinglePrivateRaceTab(panel);
            SetPanelTitle(roomGroup, ModPrivateRaceRoomRuntimeHost.IsError
                ? "Connection Failed"
                : (ModPrivateRaceRoomRuntimeHost.IsConnecting ? "Connecting" : "Private Room"));
            SetCustomPanelTitleFontSize(roomGroup, 24);

            Transform paneContent = GetPrivateRacePaneContent(panel);
            if (paneContent == null)
            {
                ModLog.Warn("Ranked UI could not find the Private Race Room pane content root.");
                return;
            }

            GameObject root = EnsurePrivateRaceRoomRoot(paneContent);
            if (root == null)
            {
                ModLog.Warn("Ranked UI could not create the Private Race Room root.");
                return;
            }

            EnsurePrivateRacePlayerText(roomGroup, root.transform, PrivateRaceRoomHostTextName, 0, "Host: " + ModPrivateRaceRoomRuntimeHost.GetRoomHostDisplayText());
            EnsurePrivateRacePlayerText(roomGroup, root.transform, "ModPrivateRaceRoomCode", 1, "Room Code: " + ModPrivateRaceRoomRuntimeHost.GetRoomCodeText());
            EnsurePrivateRaceCopyButton(roomGroup, root.transform, 1);
            EnsurePrivateRacePlayerText(
                roomGroup,
                root.transform,
                PrivateRaceRoomJoinerTextName,
                2,
                ModPrivateRaceRoomRuntimeHost.IsLocalHost
                    ? ("Player: " + ModPrivateRaceRoomRuntimeHost.GetJoinerDisplayText())
                    : ("Player: " + ModPrivateRaceRoomRuntimeHost.GetLocalPlayerDisplayText()));
            EnsurePrivateRacePlayerText(roomGroup, root.transform, "ModPrivateRaceRoomStatus", 3, ModPrivateRaceRoomRuntimeHost.GetStatusMessage());

            if (!ModPrivateRaceRoomRuntimeHost.IsConnecting &&
                !ModPrivateRaceRoomRuntimeHost.IsError &&
                ModPrivateRaceRoomRuntimeHost.IsLocalHost)
            {
                GameObject modeRow = EnsurePrivateRaceModeSelectorRow(roomGroup, root.transform, PrivateRaceRoomModeRowName, 4);
                ConfigurePrivateRaceModeSelector(modeRow);

                GameObject startRow = EnsurePrivateRaceActionButtonRow(roomGroup, root.transform, PrivateRaceRoomStartButtonName, 5);
                bool canStartRace = ModPrivateRaceRoomRuntimeHost.CanStartRace();
                bool hasJoinedOpponent = ModPrivateRaceRoomRuntimeHost.HasJoinedOpponent;
                string startLabel = ModPrivateRaceRoomRuntimeHost.IsLaunching
                    ? "Starting Race..."
                    : (hasJoinedOpponent ? "Start Race" : "Waiting for Joiner");
                ConfigurePrivateRacePanelButton(
                    startRow,
                    startLabel,
                    !ModPrivateRaceRoomRuntimeHost.IsLaunching && canStartRace,
                    delegate
                    {
                        ModPrivateRaceRoomRuntimeHost.StartLocalRace();
                    });
            }
            else
            {
                HidePrivateRaceOptionalRow(root.transform, PrivateRaceRoomModeRowName);
                HidePrivateRaceOptionalRow(root.transform, PrivateRaceRoomStartButtonName);
            }

            _lastPrivateRaceRoomLayoutState = BuildPrivateRaceRoomLayoutState();
        }

        private static void ConfigureRankedPracticeNewGameGroup(GameObject practiceGroup)
        {
            if (practiceGroup == null)
            {
                return;
            }

            Transform content = practiceGroup.transform.Find("Scroll View/Viewport/SavedGameAreaContent");
            if (content == null)
            {
                ModLog.Warn("Ranked UI could not find SavedGameAreaContent in Ranked Singleplayer Practice panel.");
                return;
            }

            Transform template = content.Find("NewGame");
            if (template == null)
            {
                ModLog.Warn("Ranked UI could not find NewGame template row in Ranked Singleplayer Practice panel.");
                return;
            }

            DestroySiblingRows(content, template);

            template.gameObject.SetActive(true);
            template.name = "NewGame";
            template.SetSiblingIndex(0);
            ConfigureActionRow(
                template.gameObject,
                "Survival",
                true,
                PracticeNewGameButtonFontSize,
                delegate
                {
                    StartRankedPracticeNewGame(GameMode.Survival);
                });

            GameObject creativeRow = UnityEngine.Object.Instantiate(template.gameObject, content, false);
            creativeRow.name = "NewGame";
            creativeRow.transform.SetSiblingIndex(1);
            ConfigureActionRow(
                creativeRow,
                "Creative",
                true,
                PracticeNewGameButtonFontSize,
                delegate
                {
                    StartRankedPracticeNewGame(GameMode.Creative);
                });

            GameObject hardcoreRow = UnityEngine.Object.Instantiate(template.gameObject, content, false);
            hardcoreRow.name = "NewGame";
            hardcoreRow.transform.SetSiblingIndex(2);
            ConfigureActionRow(
                hardcoreRow,
                "Hardcore (Coming Soon)",
                false,
                PracticeNewGameDisabledButtonFontSize,
                null);

            SetPanelTitle(practiceGroup, "New Game");
        }

        private static void ConfigureLeaderboardHomeGroup(GameObject leaderboardGroup)
        {
            if (leaderboardGroup == null)
            {
                return;
            }

            Transform content = leaderboardGroup.transform.Find("Scroll View/Viewport/SavedGameAreaContent");
            if (content == null)
            {
                ModLog.Warn("Ranked UI could not find SavedGameAreaContent in Leaderboard panel.");
                return;
            }

            List<GameObject> childrenToDestroy = new List<GameObject>();
            for (int i = 0; i < content.childCount; i++)
            {
                childrenToDestroy.Add(content.GetChild(i).gameObject);
            }

            for (int i = 0; i < childrenToDestroy.Count; i++)
            {
                childrenToDestroy[i].SetActive(false);
                UnityEngine.Object.Destroy(childrenToDestroy[i]);
            }

            ConfigureUpdatePanelLayout(leaderboardGroup);
            EnsureUpdatePanelBodyText(leaderboardGroup, UpdatePanelBodyText);
            SetPanelTitle(leaderboardGroup, UpdatePanelTitleText);
        }

        private static void ConfigurePracticeSaveGroup(GameObject practiceSaveGroup)
        {
            if (practiceSaveGroup == null)
            {
                return;
            }

            Transform content = practiceSaveGroup.transform.Find("Scroll View/Viewport/SavedGameAreaContent");
            if (content == null)
            {
                ModLog.Warn("Ranked UI could not find SavedGameAreaContent in Practice saves panel.");
                return;
            }

            Transform template = content.Find("NewGame");
            if (template == null)
            {
                ModLog.Warn("Ranked UI could not find NewGame template row in Practice saves panel.");
                return;
            }

            DestroySiblingRows(content, template);
            DestroyChildIfPresent(content, LeaderboardPlaceholderObjectName);

            IList<ModPracticeSaveDefinition> definitions = ModPracticeSaveCatalog.GetPrimaryCategoryDefinitions();
            if (definitions.Count <= 0)
            {
                template.gameObject.SetActive(false);
                EnsurePanelPlaceholder(practiceSaveGroup, content, LeaderboardPlaceholderObjectName, FutureUpdatePlaceholderText);
                SetPanelTitle(practiceSaveGroup, ModPracticeSaveCatalog.GetPrimaryCategoryDisplayName());
                return;
            }

            template.gameObject.SetActive(true);
            template.name = "NewGame";
            ApplyPracticeSaveDefinitionToRow(template.gameObject, definitions[0], 0);

            for (int i = 1; i < definitions.Count; i++)
            {
                GameObject row = UnityEngine.Object.Instantiate(template.gameObject, content, false);
                row.name = "NewGame";
                ApplyPracticeSaveDefinitionToRow(row, definitions[i], i);
            }

            SetPanelTitle(practiceSaveGroup, ModPracticeSaveCatalog.GetPrimaryCategoryDisplayName());
        }

        private static void ApplyPracticeSaveDefinitionToRow(GameObject row, ModPracticeSaveDefinition definition, int siblingIndex)
        {
            if (row == null)
            {
                return;
            }

            row.transform.SetSiblingIndex(siblingIndex);
            ConfigureActionRow(
                row,
                definition.DisplayName,
                true,
                PracticeNewGameButtonFontSize,
                delegate
                {
                    ModClientSessionMode.SelectPracticeSave(
                        definition.CategoryId,
                        definition.SaveId,
                        definition.TimerEnabled,
                        definition.StartsWithSuperSeaglide);
                    ModPracticeSaveRuntimeHost.StartPracticeSave(definition);
                });
        }

        private static void ConfigureQueueRow(GameObject row, QueueRowDefinition definition)
        {
            if (row == null)
            {
                return;
            }

            Button button = row.GetComponentInChildren<Button>(true);
            if (button == null)
            {
                ModLog.Warn("Ranked UI could not find queue button for label " + definition.Label + ".");
                return;
            }

            SetButtonLabel(button.gameObject, definition.Label);
            SetButtonLabelFontSize(button.gameObject, definition.FontSize);
            button.onClick = new Button.ButtonClickedEvent();
            if (definition.IsInteractable)
            {
                button.onClick.AddListener(new UnityAction(delegate
                {
                    if (definition.Label.IndexOf("Creative", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        ModLog.Info("Queue Creative pressed.");
                        StartRankedMatchmaking("Creative");
                    }
                    else
                    {
                        ModClientSessionMode.SelectRankedMultiplayer();
                        ModLog.Info(definition.Label + " pressed.");
                    }
                }));
            }

            button.interactable = definition.IsInteractable;
            ResetMainMenuButtonVisualState(row);
            ApplyQueueRowPresentation(row, definition);
        }

        private static void EnsureBetterRngSavedGamesButton(MainMenuRightSide rightSide)
        {
            if (rightSide == null)
            {
                return;
            }

            GameObject savedGamesGroup = FindGroup(rightSide, "SavedGames");
            if (savedGamesGroup == null)
            {
                return;
            }

            Transform content = savedGamesGroup.transform.Find("Scroll View/Viewport/SavedGameAreaContent");
            if (content == null)
            {
                return;
            }

            Transform template = content.Find("NewGame");
            if (template == null)
            {
                return;
            }

            GameObject betterRngRow = FindBetterRngSavedGamesRow(content);
            GameObject legacyNamedRow = null;
            Transform legacyNamedTransform = content.Find("ModBetterRngNewGame");
            if (legacyNamedTransform != null)
            {
                legacyNamedRow = legacyNamedTransform.gameObject;
            }

            if (betterRngRow == null && legacyNamedRow != null)
            {
                betterRngRow = legacyNamedRow;
            }

            if (legacyNamedRow != null && betterRngRow != legacyNamedRow)
            {
                legacyNamedRow.SetActive(false);
                UnityEngine.Object.Destroy(legacyNamedRow);
            }

            if (betterRngRow == null)
            {
                betterRngRow = UnityEngine.Object.Instantiate(template.gameObject, content, false);
            }

            Button betterRngButton = betterRngRow.GetComponentInChildren<Button>(true);
            if (betterRngButton != null)
            {
                betterRngButton.onClick = new Button.ButtonClickedEvent();
                betterRngButton.onClick.AddListener(new UnityAction(delegate
                {
                    ModClientSessionMode.SelectBetterRngSingleplayer();

                    uGUI_MainMenu menu = uGUI_MainMenu.main;
                    if (menu != null)
                    {
                        menu.OnButtonNew();
                    }
                }));

                betterRngButton.interactable = true;
            }

            betterRngRow.SetActive(true);
            betterRngRow.name = "NewGame";
            betterRngRow.transform.SetSiblingIndex(Mathf.Min(1, content.childCount - 1));
            SetButtonLabel(betterRngRow, BetterRngSavedGamesButtonLabel);
            SetButtonLabelFontSize(betterRngRow, BetterRngSavedGamesButtonFontSize);
            SetAlpha(betterRngRow, 1f);
            ResetMainMenuButtonVisualState(betterRngRow);

            ModBetterRngSavedGamesRowMarker marker = betterRngRow.GetComponent<ModBetterRngSavedGamesRowMarker>();
            if (marker == null)
            {
                marker = betterRngRow.AddComponent<ModBetterRngSavedGamesRowMarker>();
            }

            RectTransform contentRect = content as RectTransform;
            if (contentRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }
        }

        private static GameObject FindBetterRngSavedGamesRow(Transform content)
        {
            if (content == null)
            {
                return null;
            }

            for (int i = 0; i < content.childCount; i++)
            {
                Transform child = content.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (child.GetComponent<ModBetterRngSavedGamesRowMarker>() != null)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static void DisableSavedGameBehaviors(GameObject modGroup)
        {
            if (modGroup == null)
            {
                return;
            }

            MainMenuLoadPanel[] loadPanels = modGroup.GetComponentsInChildren<MainMenuLoadPanel>(true);
            for (int i = 0; i < loadPanels.Length; i++)
            {
                MainMenuLoadPanel loadPanel = loadPanels[i];
                if (loadPanel != null)
                {
                    loadPanel.enabled = false;
                    UnityEngine.Object.Destroy(loadPanel);
                }
            }

            MainMenuLoadScroll[] loadScrolls = modGroup.GetComponentsInChildren<MainMenuLoadScroll>(true);
            for (int i = 0; i < loadScrolls.Length; i++)
            {
                MainMenuLoadScroll loadScroll = loadScrolls[i];
                if (loadScroll != null)
                {
                    loadScroll.enabled = false;
                    UnityEngine.Object.Destroy(loadScroll);
                }
            }
        }

        private static GameObject FindGroup(MainMenuRightSide rightSide, string name)
        {
            for (int i = 0; i < rightSide.groups.Count; i++)
            {
                GameObject group = rightSide.groups[i];
                if (group != null && string.Equals(group.name, name, StringComparison.Ordinal))
                {
                    return group;
                }
            }

            return null;
        }

        private static Transform FindDescendantByName(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (string.Equals(root.name, name, StringComparison.Ordinal))
            {
                return root;
            }

            int childCount = root.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform result = FindDescendantByName(root.GetChild(i), name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void OpenRankedModeSelect(uGUI_MainMenu menu, MainMenuRightSide rightSide, Button modButton)
        {
            if (rightSide == null)
            {
                return;
            }

            ModClientSessionMode.SelectVanilla();
            rightSide.OpenGroup(ModModeSelectGroupName);
            SelectPrimaryButton(menu, modButton);
        }

        private static void OpenRankedQueue(uGUI_MainMenu menu, MainMenuRightSide rightSide, Button modButton)
        {
            if (rightSide == null)
            {
                return;
            }

            rightSide.OpenGroup(ModQueueGroupName);
            GameObject modGroup = FindGroup(rightSide, ModQueueGroupName);
            if (modGroup == null)
            {
                return;
            }

            uGUI_INavigableIconGrid grid = modGroup.GetComponentInChildren<uGUI_INavigableIconGrid>();
            if (grid != null)
            {
                grid.DeselectItem();
            }

            SelectPrimaryButton(menu, modButton);
        }

        private static void OpenRankedPracticeNewGame(uGUI_MainMenu menu, MainMenuRightSide rightSide, Button modButton)
        {
            if (rightSide == null)
            {
                return;
            }

            rightSide.OpenGroup(ModPracticeNewGameGroupName);
            SelectPrimaryButton(menu, modButton);
        }

        private static void OpenRankedQueueGroup(uGUI_MainMenu menu, MainMenuRightSide rightSide, Button modButton)
        {
            if (rightSide == null)
            {
                return;
            }

            rightSide.OpenGroup(ModQueueGroupName);
            SelectPrimaryButton(menu, modButton);
        }

        private static void OpenPracticeSaveGroup(uGUI_MainMenu menu, MainMenuRightSide rightSide, Button practiceButton)
        {
            if (rightSide == null)
            {
                return;
            }

            ModClientSessionMode.SelectVanilla();
            rightSide.OpenGroup(ModPracticeSaveGroupName);
            SelectPrimaryButton(menu, practiceButton);
        }

        private static void StartRankedPracticeNewGame(GameMode gameMode)
        {
            ModClientSessionMode.SelectRankedSingleplayerPractice();

            uGUI_MainMenu menu = uGUI_MainMenu.main;
            if (menu == null)
            {
                return;
            }

            switch (gameMode)
            {
                case GameMode.Survival:
                    menu.OnButtonSurvival();
                    break;
                case GameMode.Creative:
                    menu.OnButtonCreative();
                    break;
                case GameMode.Hardcore:
                    menu.OnButtonHardcore();
                    break;
            }
        }

        private static void StartRankedMatchmaking(string mode)
        {
            ModClientSessionMode.SelectRankedMultiplayer(mode);
            ModRankedMatchmakingRuntimeHost.StartQueue(mode);

            MainMenuRightSide rightSide = MainMenuRightSide.main;
            if (rightSide != null)
            {
                rightSide.OpenGroup(ModMatchmakingGroupName);
            }

            Button rankedButton = FindPrimaryButton("ButtonRanked");
            SelectPrimaryButton(uGUI_MainMenu.main, rankedButton);
        }

        private static void PromptAndBeginPrivateRaceHost()
        {
            if (uGUI.main == null || uGUI.main.userInput == null)
            {
                return;
            }

            uGUI.main.userInput.RequestString(
                "Host Name",
                "Connect",
                ModPrivateRaceRoomRuntimeHost.HostDisplayNameOrDefault,
                24,
                delegate(string value)
                {
                    string sanitizedValue = ModPrivateRaceRoomRuntimeHost.SanitizeDisplayName(value);
                    if (string.IsNullOrEmpty(sanitizedValue))
                    {
                        return;
                    }

                    ModPrivateRaceRoomRuntimeHost.BeginHosting(sanitizedValue);
                });
        }

        private static void PromptAndBeginPrivateRaceJoin()
        {
            if (uGUI.main == null || uGUI.main.userInput == null)
            {
                return;
            }

            uGUI.main.userInput.RequestString(
                "Join Name",
                "Continue",
                ModPrivateRaceRoomRuntimeHost.HostDisplayNameOrDefault,
                24,
                delegate(string displayNameValue)
                {
                    string sanitizedDisplayName = ModPrivateRaceRoomRuntimeHost.SanitizeDisplayName(displayNameValue);
                    if (string.IsNullOrEmpty(sanitizedDisplayName))
                    {
                        return;
                    }

                    _pendingPrivateRaceJoinDisplayName = sanitizedDisplayName;
                    _pendingPrivateRaceRoomCodePrompt = true;
                });
        }

        private static void TryShowPendingPrivateRaceRoomCodePrompt()
        {
            if (!_pendingPrivateRaceRoomCodePrompt || uGUI.main == null || uGUI.main.userInput == null)
            {
                return;
            }

            string displayName = _pendingPrivateRaceJoinDisplayName;
            _pendingPrivateRaceRoomCodePrompt = false;
            _pendingPrivateRaceJoinDisplayName = string.Empty;

            uGUI.main.userInput.RequestString(
                "Room Code",
                "Join",
                string.Empty,
                6,
                delegate(string roomCodeValue)
                {
                    string sanitizedRoomCode = ModPrivateRaceRoomRuntimeHost.SanitizeRoomCode(roomCodeValue);
                    if (string.IsNullOrEmpty(sanitizedRoomCode))
                    {
                        return;
                    }

                    ModPrivateRaceRoomRuntimeHost.BeginJoining(displayName, sanitizedRoomCode);
                });
        }

        private static void OpenPrivateRaceRoomGroup(uGUI_MainMenu menu, MainMenuRightSide rightSide, Button modButton)
        {
            if (rightSide == null)
            {
                return;
            }

            EnsurePrivateRaceRoomGroup(rightSide);
            rightSide.OpenGroup(ModPrivateRaceRoomGroupName);
            EnsurePrimaryMenuVisible(menu, false);
            SyncPrivateRaceRoomGroup(rightSide);
        }

        private static void EnsureSinglePrivateRaceTab(uGUI_OptionsPanel panel)
        {
            if (panel == null || panel.tabsContainer == null || panel.panesContainer == null)
            {
                return;
            }

            bool needsReset = panel.tabsContainer.childCount != 1 || panel.panesContainer.childCount != 1;
            if (!needsReset && panel.tabsContainer.childCount > 0)
            {
                Text tabText = panel.tabsContainer.GetChild(0).GetComponentInChildren<Text>(true);
                needsReset = tabText == null || !string.Equals((tabText.text ?? string.Empty).Trim(), "Race", StringComparison.OrdinalIgnoreCase);
            }

            if (needsReset)
            {
                panel.RemoveTabs();
                panel.AddTab("Race");
            }

            if (panel.tabsContainer.childCount > 0)
            {
                for (int i = 0; i < panel.tabsContainer.childCount; i++)
                {
                    Transform tabTransform = panel.tabsContainer.GetChild(i);
                    if (tabTransform == null)
                    {
                        continue;
                    }

                    bool isPrimary = i == 0;
                    tabTransform.gameObject.SetActive(isPrimary);
                    if (!isPrimary)
                    {
                        continue;
                    }

                    RemoveLocalizationComponents(tabTransform.gameObject);
                    SetButtonLabel(tabTransform.gameObject, "Race");
                    Toggle toggle = tabTransform.GetComponentInChildren<Toggle>(true);
                    if (toggle != null)
                    {
                        toggle.isOn = true;
                        toggle.interactable = false;
                    }
                }
            }

            for (int i = 0; i < panel.panesContainer.childCount; i++)
            {
                Transform paneTransform = panel.panesContainer.GetChild(i);
                if (paneTransform != null)
                {
                    paneTransform.gameObject.SetActive(i == 0);
                }
            }
        }

        private static Transform GetPrivateRacePaneContent(uGUI_OptionsPanel panel)
        {
            if (panel == null || panel.panesContainer == null || panel.panesContainer.childCount <= 0)
            {
                return null;
            }

            Transform pane = panel.panesContainer.GetChild(0);
            Transform content = pane != null ? pane.Find("Content") : null;
            return content ?? pane;
        }

        private static void HidePrivateRaceOptionalRow(Transform rootTransform, string objectName)
        {
            if (rootTransform == null || string.IsNullOrEmpty(objectName))
            {
                return;
            }

            Transform childTransform = rootTransform.Find(objectName);
            if (childTransform != null)
            {
                childTransform.gameObject.SetActive(false);
            }
        }

        private static void ConfigurePrivateRaceLeaveButton(GameObject roomGroup)
        {
            Button backButton = FindButtonByTranslationKey(roomGroup, "Back");
            if (backButton == null)
            {
                backButton = FindButtonByText(roomGroup, "Back");
            }

            if (backButton == null)
            {
                return;
            }

            RemoveLocalizationComponents(backButton.gameObject);
            SetButtonLabel(backButton.gameObject, "Leave Room");
            backButton.onClick = new Button.ButtonClickedEvent();
            backButton.onClick.AddListener(new UnityAction(delegate
            {
                if (uGUI.main == null || uGUI.main.confirmation == null)
                {
                    ModPrivateRaceRoomRuntimeHost.LeaveRoom();
                    if (uGUI_MainMenu.main != null)
                    {
                        uGUI_MainMenu.main.OnHome();
                    }

                    return;
                }

                uGUI.main.confirmation.Show("Are you sure you want to leave the private race room?", delegate(bool confirmed)
                {
                    if (!confirmed)
                    {
                        return;
                    }

                    ModPrivateRaceRoomRuntimeHost.LeaveRoom();
                    if (uGUI_MainMenu.main != null)
                    {
                        uGUI_MainMenu.main.OnHome();
                    }
                });
            }));
        }

        private static GameObject EnsurePrivateRaceRoomRoot(Transform contentTransform)
        {
            if (contentTransform == null)
            {
                return null;
            }

            for (int i = contentTransform.childCount - 1; i >= 0; i--)
            {
                Transform child = contentTransform.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (!string.Equals(child.name, PrivateRaceRoomRootName, StringComparison.Ordinal))
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }

            Transform existing = contentTransform.Find(PrivateRaceRoomRootName);
            GameObject rootObject = existing != null ? existing.gameObject : null;
            if (rootObject == null)
            {
                rootObject = new GameObject(PrivateRaceRoomRootName, typeof(RectTransform));
                rootObject.transform.SetParent(contentTransform, false);
            }

            RectTransform rectTransform = rootObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = new Vector2(12f, 12f);
            rectTransform.offsetMax = new Vector2(-12f, -12f);
            rectTransform.localScale = Vector3.one;
            return rootObject;
        }

        private static void EnsurePrivateRacePlayerText(GameObject panelGroup, Transform rootTransform, string objectName, int rowIndex, string value)
        {
            Transform existingTransform = rootTransform.Find(objectName);
            Text text = existingTransform != null ? existingTransform.GetComponent<Text>() : null;
            if (text == null)
            {
                Text template = panelGroup.GetComponentInChildren<Text>(true);
                if (template == null)
                {
                    return;
                }

                GameObject textObject = UnityEngine.Object.Instantiate(template.gameObject, rootTransform, false);
                textObject.name = objectName;
                RemoveLocalizationComponents(textObject);
                text = textObject.GetComponent<Text>();
            }

            RectTransform rectTransform = text.rectTransform;
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(10f, -(26f + (54f * rowIndex)));
            rectTransform.sizeDelta = string.Equals(objectName, "ModPrivateRaceRoomCode", StringComparison.Ordinal)
                ? new Vector2(-160f, 42f)
                : new Vector2(-20f, 42f);
            rectTransform.localScale = Vector3.one;

            text.text = value;
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = Color.white;
            text.raycastTarget = false;
            text.gameObject.SetActive(true);
        }

        private static void EnsurePrivateRaceCopyButton(GameObject panelGroup, Transform rootTransform, int rowIndex)
        {
            if (panelGroup == null || rootTransform == null)
            {
                return;
            }

            Transform existingTransform = rootTransform.Find(PrivateRaceRoomCopyButtonName);
            GameObject buttonObject = existingTransform != null ? existingTransform.gameObject : null;
            if (buttonObject == null)
            {
                Button templateButton = FindButtonByText(panelGroup, "Leave Room");
                if (templateButton == null)
                {
                    templateButton = FindButtonByText(panelGroup, "Back");
                }

                if (templateButton == null)
                {
                    return;
                }

                buttonObject = UnityEngine.Object.Instantiate(templateButton.gameObject, rootTransform, false);
                buttonObject.name = PrivateRaceRoomCopyButtonName;
            }

            RectTransform rectTransform = buttonObject.transform as RectTransform;
            if (rectTransform != null)
            {
                Text roomCodeText = null;
                Transform roomCodeTransform = rootTransform.Find("ModPrivateRaceRoomCode");
                if (roomCodeTransform != null)
                {
                    roomCodeText = roomCodeTransform.GetComponent<Text>();
                }

                float preferredWidth = roomCodeText != null ? roomCodeText.preferredWidth : 220f;
                float buttonLeft = Mathf.Clamp(18f + preferredWidth + 10f, 220f, 460f);
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(0f, 1f);
                rectTransform.pivot = new Vector2(0f, 1f);
                rectTransform.anchoredPosition = new Vector2(buttonLeft, -(27f + (54f * rowIndex)));
                rectTransform.sizeDelta = new Vector2(84f, 34f);
                rectTransform.localScale = Vector3.one;
            }

            Button button = buttonObject.GetComponentInChildren<Button>(true);
            if (button == null)
            {
                buttonObject.SetActive(false);
                return;
            }

            string roomCode = ModPrivateRaceRoomRuntimeHost.GetRoomCodeText();
            bool canCopy = !string.IsNullOrEmpty(roomCode) &&
                !string.Equals(roomCode, "Unavailable", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(roomCode, "Generating...", StringComparison.OrdinalIgnoreCase);

            RemoveLocalizationComponents(button.gameObject);
            SetButtonLabel(button.gameObject, "Copy");
            SetButtonLabelFontSize(button.gameObject, 18);
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(new UnityAction(delegate
            {
                GUIUtility.systemCopyBuffer = ModPrivateRaceRoomRuntimeHost.GetRoomCodeText();
            }));
            button.interactable = canCopy;
            buttonObject.SetActive(true);
        }

        private static GameObject EnsurePrivateRaceChoiceRow(Transform rootTransform, GameObject choiceOptionPrefab, string objectName, int rowIndex)
        {
            Transform existingTransform = rootTransform.Find(objectName);
            GameObject rowObject = existingTransform != null ? existingTransform.gameObject : null;
            if (rowObject == null)
            {
                rowObject = UnityEngine.Object.Instantiate(choiceOptionPrefab) as GameObject;
                if (rowObject == null)
                {
                    return null;
                }

                rowObject.name = objectName;
                rowObject.transform.SetParent(rootTransform, false);
                PreparePrivateRaceChoiceRow(rowObject);
            }

            RectTransform rowRect = rowObject.transform as RectTransform;
            if (rowRect != null)
            {
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.anchoredPosition = new Vector2(0f, -(18f + (98f * rowIndex)));
                rowRect.sizeDelta = new Vector2(0f, 96f);
                rowRect.localScale = Vector3.one;
            }

            rowObject.SetActive(true);
            return rowObject;
        }

        private static GameObject EnsurePrivateRaceModeSelectorRow(GameObject panelGroup, Transform rootTransform, string objectName, int rowIndex)
        {
            if (panelGroup == null || rootTransform == null)
            {
                return null;
            }

            Transform existingTransform = rootTransform.Find(objectName);
            GameObject rowObject = existingTransform != null ? existingTransform.gameObject : null;
            if (rowObject == null)
            {
                rowObject = new GameObject(objectName, typeof(RectTransform));
                rowObject.transform.SetParent(rootTransform, false);
            }

            RectTransform rowRect = rowObject.transform as RectTransform;
            if (rowRect != null)
            {
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.anchoredPosition = new Vector2(0f, -(18f + (98f * rowIndex)));
                rowRect.sizeDelta = new Vector2(0f, 96f);
                rowRect.localScale = Vector3.one;
            }

            EnsurePrivateRaceModeSelectorChildren(panelGroup, rowObject);
            rowObject.SetActive(true);
            return rowObject;
        }

        private static void EnsurePrivateRaceModeSelectorChildren(GameObject panelGroup, GameObject rowObject)
        {
            EnsurePrivateRaceModeSelectorLabel(panelGroup, rowObject);
            EnsurePrivateRaceModeSelectorValue(panelGroup, rowObject);
            EnsurePrivateRaceModeSelectorArrow(panelGroup, rowObject, "Previous", "<");
            EnsurePrivateRaceModeSelectorArrow(panelGroup, rowObject, "Next", ">");
        }

        private static void EnsurePrivateRaceModeSelectorLabel(GameObject panelGroup, GameObject rowObject)
        {
            Transform existingTransform = rowObject.transform.Find("Label");
            Text labelText = existingTransform != null ? existingTransform.GetComponent<Text>() : null;
            if (labelText == null)
            {
                Text template = panelGroup.GetComponentInChildren<Text>(true);
                if (template == null)
                {
                    return;
                }

                labelText = UnityEngine.Object.Instantiate(template, rowObject.transform, false);
                labelText.name = "Label";
                RemoveLocalizationComponents(labelText.gameObject);
            }

            RectTransform rectTransform = labelText.rectTransform;
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(0f, 0.5f);
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(10f, 0f);
            rectTransform.sizeDelta = new Vector2(300f, 48f);
            rectTransform.localScale = Vector3.one;

            labelText.text = "Race Gamemode";
            labelText.fontSize = 28;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            labelText.verticalOverflow = VerticalWrapMode.Truncate;
            labelText.color = Color.white;
            labelText.raycastTarget = false;
            labelText.gameObject.SetActive(true);
        }

        private static void EnsurePrivateRaceModeSelectorValue(GameObject panelGroup, GameObject rowObject)
        {
            Transform existingTransform = rowObject.transform.Find("Value");
            Text valueText = existingTransform != null ? existingTransform.GetComponent<Text>() : null;
            if (valueText == null)
            {
                Text template = panelGroup.GetComponentInChildren<Text>(true);
                if (template == null)
                {
                    return;
                }

                valueText = UnityEngine.Object.Instantiate(template, rowObject.transform, false);
                valueText.name = "Value";
                RemoveLocalizationComponents(valueText.gameObject);
            }

            RectTransform rectTransform = valueText.rectTransform;
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(0f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(562f, 0f);
            rectTransform.sizeDelta = new Vector2(170f, 48f);
            rectTransform.localScale = Vector3.one;

            valueText.text = ModPrivateRaceRoomRuntimeHost.SelectedMode;
            valueText.fontSize = 28;
            valueText.alignment = TextAnchor.MiddleCenter;
            valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
            valueText.verticalOverflow = VerticalWrapMode.Truncate;
            valueText.color = Color.white;
            valueText.raycastTarget = false;
            valueText.gameObject.SetActive(true);
        }

        private static void EnsurePrivateRaceModeSelectorArrow(GameObject panelGroup, GameObject rowObject, string objectName, string label)
        {
            Transform existingTransform = rowObject.transform.Find(objectName);
            Button button = existingTransform != null ? existingTransform.GetComponent<Button>() : null;
            if (button == null)
            {
                bool isPrevious = string.Equals(objectName, "Previous", StringComparison.Ordinal);
                Button templateButton = isPrevious ? _privateRaceChoicePreviousArrowTemplate : _privateRaceChoiceNextArrowTemplate;
                if (templateButton == null)
                {
                    templateButton = FindButtonByText(panelGroup, "Leave Room");
                    if (templateButton == null)
                    {
                        templateButton = FindButtonByText(panelGroup, "Back");
                    }
                }

                if (templateButton == null)
                {
                    return;
                }

                GameObject clone = UnityEngine.Object.Instantiate(templateButton.gameObject, rowObject.transform, false);
                clone.name = objectName;
                button = clone.GetComponent<Button>();
            }

            RectTransform rectTransform = button.transform as RectTransform;
            if (rectTransform != null)
            {
                bool isPrevious = string.Equals(objectName, "Previous", StringComparison.Ordinal);
                rectTransform.anchorMin = new Vector2(0f, 0.5f);
                rectTransform.anchorMax = new Vector2(0f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = new Vector2(isPrevious ? 436f : 688f, 0f);
                rectTransform.sizeDelta = new Vector2(64f, 64f);
                rectTransform.localScale = Vector3.one;
            }

            RemoveLocalizationComponents(button.gameObject);
            if (_privateRaceChoicePreviousArrowTemplate == null || _privateRaceChoiceNextArrowTemplate == null)
            {
                SetButtonLabel(button.gameObject, label);
                SetButtonLabelFontSize(button.gameObject, 30);
            }
            button.interactable = true;
            button.gameObject.SetActive(true);
        }

        private static void PreparePrivateRaceChoiceRow(GameObject rowObject)
        {
            LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = rowObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = 74f;
            layoutElement.preferredHeight = 74f;

            uGUI_Choice choice = rowObject.GetComponentInChildren<uGUI_Choice>(true);
            if (choice == null)
            {
                return;
            }

            RectTransform choiceRect = choice.transform as RectTransform;
            if (choiceRect != null)
            {
                choiceRect.anchorMin = new Vector2(0f, 0f);
                choiceRect.anchorMax = new Vector2(1f, 1f);
                choiceRect.pivot = new Vector2(0.5f, 0.5f);
                choiceRect.offsetMin = new Vector2(360f, 6f);
                choiceRect.offsetMax = new Vector2(-24f, -6f);
                choiceRect.localScale = Vector3.one;
            }

            ConfigurePrivateRaceChoiceWidgetLayout(choice);
        }

        private static void ConfigurePrivateRaceModeSelector(GameObject rowObject)
        {
            if (rowObject == null)
            {
                return;
            }

            Text valueText = null;
            Transform valueTransform = rowObject.transform.Find("Value");
            if (valueTransform != null)
            {
                valueText = valueTransform.GetComponent<Text>();
            }

            if (valueText != null)
            {
                valueText.text = ModPrivateRaceRoomRuntimeHost.SelectedMode;
            }

            Button previousButton = null;
            Transform previousTransform = rowObject.transform.Find("Previous");
            if (previousTransform != null)
            {
                previousButton = previousTransform.GetComponent<Button>();
            }

            Button nextButton = null;
            Transform nextTransform = rowObject.transform.Find("Next");
            if (nextTransform != null)
            {
                nextButton = nextTransform.GetComponent<Button>();
            }

            ModPrivateRaceChoiceRowMarker marker = rowObject.GetComponent<ModPrivateRaceChoiceRowMarker>();
            if (marker == null)
            {
                marker = rowObject.AddComponent<ModPrivateRaceChoiceRowMarker>();
            }

            if (previousButton != null)
            {
                previousButton.onClick = new Button.ButtonClickedEvent();
                previousButton.onClick.AddListener(new UnityAction(delegate
                {
                    ModPrivateRaceRoomRuntimeHost.OffsetSelectedModeIndex(-1);
                    ModLog.Info("Private race mode changed to " + ModPrivateRaceRoomRuntimeHost.SelectedMode + ".");
                    SyncPrivateRaceRoomUiImmediately();
                }));
            }

            if (nextButton != null)
            {
                nextButton.onClick = new Button.ButtonClickedEvent();
                nextButton.onClick.AddListener(new UnityAction(delegate
                {
                    ModPrivateRaceRoomRuntimeHost.OffsetSelectedModeIndex(1);
                    ModLog.Info("Private race mode changed to " + ModPrivateRaceRoomRuntimeHost.SelectedMode + ".");
                    SyncPrivateRaceRoomUiImmediately();
                }));
            }

            if (previousButton != null)
            {
                previousButton.interactable = true;
            }

            if (nextButton != null)
            {
                nextButton.interactable = true;
            }
        }

        private static GameObject EnsurePrivateRaceButtonRow(Transform rootTransform, GameObject buttonPrefab, string objectName, int rowIndex)
        {
            Transform existingTransform = rootTransform.Find(objectName);
            GameObject rowObject = existingTransform != null ? existingTransform.gameObject : null;
            if (rowObject == null)
            {
                rowObject = UnityEngine.Object.Instantiate(buttonPrefab) as GameObject;
                if (rowObject == null)
                {
                    return null;
                }

                rowObject.name = objectName;
                rowObject.transform.SetParent(rootTransform, false);
            }

            RectTransform rowRect = rowObject.transform as RectTransform;
            if (rowRect != null)
            {
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.anchoredPosition = new Vector2(0f, -(28f + (98f * rowIndex)));
                rowRect.sizeDelta = new Vector2(0f, 86f);
                rowRect.localScale = Vector3.one;
            }

            rowObject.SetActive(true);
            return rowObject;
        }

        private static GameObject EnsurePrivateRaceActionButtonRow(GameObject panelGroup, Transform rootTransform, string objectName, int rowIndex)
        {
            if (panelGroup == null || rootTransform == null)
            {
                return null;
            }

            Transform existingTransform = rootTransform.Find(objectName);
            GameObject rowObject = existingTransform != null ? existingTransform.gameObject : null;
            if (rowObject == null)
            {
                Button templateButton = FindButtonByText(panelGroup, "Leave Room");
                if (templateButton == null)
                {
                    templateButton = FindButtonByText(panelGroup, "Back");
                }

                if (templateButton == null)
                {
                    return null;
                }

                rowObject = UnityEngine.Object.Instantiate(templateButton.gameObject, rootTransform, false);
                rowObject.name = objectName;
            }

            RectTransform rowRect = rowObject.transform as RectTransform;
            if (rowRect != null)
            {
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.anchoredPosition = new Vector2(0f, -(28f + (98f * rowIndex)));
                rowRect.sizeDelta = new Vector2(0f, 86f);
                rowRect.localScale = Vector3.one;
            }

            rowObject.SetActive(true);
            return rowObject;
        }

        private static void ConfigurePrivateRaceStartButton(GameObject rowObject)
        {
            if (rowObject == null)
            {
                return;
            }

            Button button = rowObject.GetComponentInChildren<Button>(true);
            if (button == null)
            {
                return;
            }

            RemoveLocalizationComponents(button.gameObject);
            SetButtonLabel(button.gameObject, "Start Race");
            SetButtonLabelFontSize(button.gameObject, 28);
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(new UnityAction(delegate
            {
                ModPrivateRaceRoomRuntimeHost.StartLocalRace();
            }));
            button.interactable = ModPrivateRaceRoomRuntimeHost.CanStartRace();
            ResetMainMenuButtonVisualState(rowObject);
        }

        private static void ConfigurePrivateRaceHeading(GameObject rowObject, string value, int fontSize)
        {
            if (rowObject == null)
            {
                return;
            }

            LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = rowObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = 34f;
            layoutElement.preferredHeight = 34f;

            RemoveLocalizationComponents(rowObject);
            Text text = rowObject.GetComponentInChildren<Text>(true);
            if (text == null)
            {
                return;
            }

            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleLeft;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = Color.white;
            RectTransform rectTransform = text.rectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0f, 0.5f);
                rectTransform.anchorMax = new Vector2(1f, 0.5f);
                rectTransform.pivot = new Vector2(0f, 0.5f);
                rectTransform.offsetMin = new Vector2(0f, -16f);
                rectTransform.offsetMax = new Vector2(0f, 16f);
            }
        }

        private static void ConfigurePrivateRacePanelButton(GameObject rowObject, string label, bool isInteractable, UnityAction onClick)
        {
            if (rowObject == null)
            {
                return;
            }

            LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = rowObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = 72f;
            layoutElement.preferredHeight = 72f;

            Button button = rowObject.GetComponentInChildren<Button>(true);
            if (button == null)
            {
                return;
            }

            RectTransform buttonRect = button.transform as RectTransform;
            if (buttonRect != null)
            {
                buttonRect.anchorMin = new Vector2(0f, 0.5f);
                buttonRect.anchorMax = new Vector2(1f, 0.5f);
                buttonRect.pivot = new Vector2(0.5f, 0.5f);
                buttonRect.anchoredPosition = Vector2.zero;
                buttonRect.sizeDelta = new Vector2(0f, 56f);
                buttonRect.offsetMin = new Vector2(0f, -28f);
                buttonRect.offsetMax = new Vector2(0f, 28f);
            }

            RemoveLocalizationComponents(button.gameObject);
            SetButtonLabel(button.gameObject, label);
            SetButtonLabelFontSize(button.gameObject, 26);
            ModPrivateRaceStartButtonMarker marker = rowObject.GetComponent<ModPrivateRaceStartButtonMarker>();
            if (marker == null)
            {
                marker = rowObject.AddComponent<ModPrivateRaceStartButtonMarker>();
            }

            button.onClick = new Button.ButtonClickedEvent();
            if (onClick != null)
            {
                button.onClick.AddListener(new UnityAction(delegate
                {
                    ModLog.Info("Private race Start Race button clicked.");
                    onClick();
                }));
            }

            button.interactable = isInteractable;
            ResetMainMenuButtonVisualState(rowObject);
            ApplyQueueRowPresentation(rowObject, new QueueRowDefinition(label, isInteractable, 26));
        }

        private static void SyncPrivateRaceRoomUiImmediately()
        {
            if (MainMenuRightSide.main == null)
            {
                return;
            }

            SyncPrivateRaceRoomGroup(MainMenuRightSide.main);
        }

        private static void ReplaceTextByTranslationKey(GameObject root, string translationKey, string value)
        {
            if (root == null || string.IsNullOrEmpty(translationKey))
            {
                return;
            }

            TranslationLiveUpdate[] updates = root.GetComponentsInChildren<TranslationLiveUpdate>(true);
            for (int i = 0; i < updates.Length; i++)
            {
                TranslationLiveUpdate update = updates[i];
                if (update == null || !string.Equals(update.translationKey, translationKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (update.textComponent != null)
                {
                    update.textComponent.text = value;
                }

                UnityEngine.Object.Destroy(update);
            }
        }

        private static Button FindButtonByTranslationKey(GameObject root, string translationKey)
        {
            if (root == null || string.IsNullOrEmpty(translationKey))
            {
                return null;
            }

            TranslationLiveUpdate[] updates = root.GetComponentsInChildren<TranslationLiveUpdate>(true);
            for (int i = 0; i < updates.Length; i++)
            {
                TranslationLiveUpdate update = updates[i];
                if (update == null || !string.Equals(update.translationKey, translationKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return FindButtonAncestor(update.transform);
            }

            return null;
        }

        private static Button FindButtonByText(GameObject root, string textValue)
        {
            if (root == null || string.IsNullOrEmpty(textValue))
            {
                return null;
            }

            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null || !string.Equals(text.text, textValue, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return FindButtonAncestor(text.transform);
            }

            return null;
        }

        private static Button FindButtonAncestor(Transform transform)
        {
            Transform current = transform;
            while (current != null)
            {
                Button button = current.GetComponent<Button>();
                if (button != null)
                {
                    return button;
                }

                current = current.parent;
            }

            return null;
        }

        private static void ConfigurePrivateRaceRowLabel(GameObject rowObject, uGUI_Choice choice, string label)
        {
            Text labelText = FindPrivateRaceRowLabelText(rowObject, choice);
            if (labelText == null)
            {
                return;
            }

            labelText.text = label;
            labelText.resizeTextForBestFit = false;
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            labelText.verticalOverflow = VerticalWrapMode.Truncate;
            labelText.fontSize = 28;
            labelText.alignment = TextAnchor.MiddleLeft;
        }

        private static Text FindPrivateRaceRowLabelText(GameObject rowObject, uGUI_Choice choice)
        {
            if (rowObject == null || choice == null)
            {
                return null;
            }

            Text[] texts = rowObject.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null || text == choice.currentText)
                {
                    continue;
                }

                if (text.transform.IsChildOf(choice.transform))
                {
                    continue;
                }

                return text;
            }

            return null;
        }

        private static void ConfigurePrivateRaceChoiceWidgetLayout(uGUI_Choice choice)
        {
            if (choice == null)
            {
                return;
            }

            ConfigurePrivateRaceChoiceValueText(choice, 26, TextAnchor.MiddleCenter);

            if (choice.previousButton != null)
            {
                RectTransform previousRect = choice.previousButton.transform as RectTransform;
                if (previousRect != null)
                {
                    previousRect.anchorMin = new Vector2(0f, 0.5f);
                    previousRect.anchorMax = new Vector2(0f, 0.5f);
                    previousRect.pivot = new Vector2(0f, 0.5f);
                    previousRect.anchoredPosition = new Vector2(12f, 0f);
                    previousRect.sizeDelta = new Vector2(58f, 72f);
                    previousRect.localScale = Vector3.one;
                }
            }

            if (choice.nextButton != null)
            {
                RectTransform nextRect = choice.nextButton.transform as RectTransform;
                if (nextRect != null)
                {
                    nextRect.anchorMin = new Vector2(1f, 0.5f);
                    nextRect.anchorMax = new Vector2(1f, 0.5f);
                    nextRect.pivot = new Vector2(1f, 0.5f);
                    nextRect.anchoredPosition = new Vector2(-12f, 0f);
                    nextRect.sizeDelta = new Vector2(58f, 72f);
                    nextRect.localScale = Vector3.one;
                }
            }
        }

        private static void ConfigurePrivateRaceChoiceValueText(uGUI_Choice choice, int fontSize, TextAnchor alignment)
        {
            if (choice == null || choice.currentText == null)
            {
                return;
            }

            RectTransform currentTextRect = choice.currentText.transform as RectTransform;
            if (currentTextRect != null)
            {
                currentTextRect.anchorMin = new Vector2(0f, 0f);
                currentTextRect.anchorMax = new Vector2(1f, 1f);
                currentTextRect.pivot = new Vector2(0.5f, 0.5f);
                currentTextRect.offsetMin = new Vector2(92f, 0f);
                currentTextRect.offsetMax = new Vector2(-92f, 0f);
                currentTextRect.localScale = Vector3.one;
            }

            choice.currentText.fontSize = fontSize;
            choice.currentText.alignment = alignment;
            choice.currentText.resizeTextForBestFit = false;
            choice.currentText.horizontalOverflow = HorizontalWrapMode.Overflow;
            choice.currentText.verticalOverflow = VerticalWrapMode.Truncate;
            choice.currentText.raycastTarget = false;
            choice.currentText.color = Color.white;
            choice.currentText.text = string.Empty;
            choice.currentText.gameObject.SetActive(false);

            Text overlay = EnsurePrivateRaceChoiceOverlayText(choice);
            if (overlay != null)
            {
                overlay.fontSize = fontSize;
                overlay.alignment = alignment;
                overlay.resizeTextForBestFit = false;
                overlay.horizontalOverflow = HorizontalWrapMode.Overflow;
                overlay.verticalOverflow = VerticalWrapMode.Truncate;
                overlay.raycastTarget = false;
                overlay.color = Color.white;
                overlay.gameObject.SetActive(true);
            }
        }

        private static void ApplyPrivateRaceChoiceCurrentText(uGUI_Choice choice, string value)
        {
            if (choice == null || choice.currentText == null)
            {
                return;
            }

            RemoveLocalizationComponents(choice.currentText.gameObject);
            TranslationLiveUpdate[] translationUpdates = choice.currentText.GetComponentsInChildren<TranslationLiveUpdate>(true);
            for (int i = 0; i < translationUpdates.Length; i++)
            {
                if (translationUpdates[i] != null)
                {
                    UnityEngine.Object.Destroy(translationUpdates[i]);
                }
            }

            choice.currentText.text = string.Empty;
            choice.currentText.gameObject.SetActive(false);

            Text overlay = EnsurePrivateRaceChoiceOverlayText(choice);
            if (overlay != null)
            {
                overlay.text = value ?? string.Empty;
                overlay.color = Color.white;
                overlay.gameObject.SetActive(true);
            }
        }

        private static Text EnsurePrivateRaceChoiceOverlayText(uGUI_Choice choice)
        {
            if (choice == null)
            {
                return null;
            }

            Transform existingTransform = choice.transform.Find(PrivateRaceRoomChoiceValueName);
            Text overlay = existingTransform != null ? existingTransform.GetComponent<Text>() : null;
            if (overlay == null)
            {
                overlay = UnityEngine.Object.Instantiate(choice.currentText, choice.transform, false);
                if (overlay == null)
                {
                    return null;
                }

                overlay.name = PrivateRaceRoomChoiceValueName;
                RemoveLocalizationComponents(overlay.gameObject);
            }

            RectTransform overlayRect = overlay.rectTransform;
            overlayRect.anchorMin = new Vector2(0f, 0f);
            overlayRect.anchorMax = new Vector2(1f, 1f);
            overlayRect.pivot = new Vector2(0.5f, 0.5f);
            overlayRect.offsetMin = new Vector2(92f, 0f);
            overlayRect.offsetMax = new Vector2(-92f, 0f);
            overlayRect.localScale = Vector3.one;
            return overlay;
        }

        private static void SelectPrimaryButton(uGUI_MainMenu menu, Button button)
        {
            if (menu != null && menu.primaryOptions != null && button != null)
            {
                menu.primaryOptions.SelectItem(button);
            }

            if (EventSystem.current != null && button != null)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }

        private static void SetButtonLabel(GameObject root, string textValue)
        {
            if (root == null)
            {
                return;
            }

            uGUI_Text[] localizers = root.GetComponentsInChildren<uGUI_Text>(true);
            for (int i = 0; i < localizers.Length; i++)
            {
                uGUI_Text localizer = localizers[i];
                if (localizer != null)
                {
                    localizer.SetText(textValue, false);
                    return;
                }
            }

            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text != null)
                {
                    text.text = textValue;
                    return;
                }
            }
        }

        private static void RemoveLocalizationComponents(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            uGUI_Text[] localizers = root.GetComponentsInChildren<uGUI_Text>(true);
            for (int i = 0; i < localizers.Length; i++)
            {
                uGUI_Text localizer = localizers[i];
                if (localizer != null)
                {
                    UnityEngine.Object.Destroy(localizer);
                }
            }

            TranslationLiveUpdate[] translationUpdates = root.GetComponentsInChildren<TranslationLiveUpdate>(true);
            for (int i = 0; i < translationUpdates.Length; i++)
            {
                TranslationLiveUpdate translationUpdate = translationUpdates[i];
                if (translationUpdate != null)
                {
                    UnityEngine.Object.Destroy(translationUpdate);
                }
            }
        }

        private static void ConfigureActionRow(GameObject row, string label, bool isInteractable, int fontSize, UnityAction onClick)
        {
            if (row == null)
            {
                return;
            }

            Button button = row.GetComponentInChildren<Button>(true);
            if (button == null)
            {
                ModLog.Warn("Ranked UI could not find action row button for label " + label + ".");
                return;
            }

            RemoveLocalizationComponents(button.gameObject);
            SetButtonLabel(button.gameObject, label);
            SetButtonLabelFontSize(button.gameObject, fontSize);
            button.onClick = new Button.ButtonClickedEvent();
            if (isInteractable && onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            button.interactable = isInteractable;
            ResetMainMenuButtonVisualState(row);
            ApplyQueueRowPresentation(row, new QueueRowDefinition(label, isInteractable, fontSize));
        }

        private static void DestroySiblingRows(Transform content, Transform template)
        {
            List<GameObject> rowsToDestroy = new List<GameObject>();
            int childCount = content.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = content.GetChild(i);
                if (child != template)
                {
                    rowsToDestroy.Add(child.gameObject);
                }
            }

            for (int i = 0; i < rowsToDestroy.Count; i++)
            {
                rowsToDestroy[i].SetActive(false);
                UnityEngine.Object.Destroy(rowsToDestroy[i]);
            }
        }

        private static Button FindPrimaryButton(string objectName)
        {
            if (uGUI_MainMenu.main == null || uGUI_MainMenu.main.primaryOptions == null)
            {
                return null;
            }

            Transform transform = FindDescendantByName(uGUI_MainMenu.main.primaryOptions.transform, objectName);
            return transform != null ? transform.GetComponent<Button>() : null;
        }

        private static Button FindButtonByPersistentMethod(GameObject root, string methodName)
        {
            if (root == null)
            {
                return null;
            }

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (ButtonCallsMethod(buttons[i], methodName))
                {
                    return buttons[i];
                }
            }

            return null;
        }

        private static void AddSessionModeListener(Button button, ModClientLaunchMode mode)
        {
            if (button == null)
            {
                return;
            }

            ModSessionModeMarker marker = button.gameObject.GetComponent<ModSessionModeMarker>();
            if (marker != null && marker.LaunchMode == mode)
            {
                return;
            }

            if (marker == null)
            {
                marker = button.gameObject.AddComponent<ModSessionModeMarker>();
            }

            marker.LaunchMode = mode;

            button.onClick.AddListener(new UnityAction(delegate
            {
                switch (mode)
                {
                    case ModClientLaunchMode.ModBetterRngSingleplayer:
                        ModClientSessionMode.SelectBetterRngSingleplayer();
                        break;
                    case ModClientLaunchMode.ModSingleplayerPractice:
                        ModClientSessionMode.SelectRankedSingleplayerPractice();
                        break;
                    case ModClientLaunchMode.ModMultiplayer:
                        ModClientSessionMode.SelectRankedMultiplayer();
                        break;
                    default:
                        ModClientSessionMode.SelectVanilla();
                        break;
                }
            }));
        }

        private static void SyncPrimaryOptionLabels(uGUI_MainMenu menu)
        {
            if (menu == null || menu.primaryOptions == null)
            {
                return;
            }

            Transform practiceTransform = FindDescendantByName(menu.primaryOptions.transform, "ButtonPractice");
            if (practiceTransform != null)
            {
                SetButtonLabel(practiceTransform.gameObject, "Practice");
                Button practiceButton = practiceTransform.GetComponent<Button>();
                if (practiceButton != null)
                {
                    practiceButton.onClick = new Button.ButtonClickedEvent();
                    practiceButton.onClick.AddListener(new UnityAction(delegate
                    {
                        OpenPracticeSaveGroup(menu, MainMenuRightSide.main, practiceButton);
                    }));
                }
            }

            Transform modTransform = FindDescendantByName(menu.primaryOptions.transform, "ButtonRanked");
            if (modTransform != null)
            {
                SetButtonLabel(modTransform.gameObject, RaceButtonLabel);
                Button modButton = modTransform.GetComponent<Button>();
                if (modButton != null)
                {
                    modButton.onClick = new Button.ButtonClickedEvent();
                    modButton.onClick.AddListener(new UnityAction(delegate
                    {
                        OpenRankedModeSelect(menu, MainMenuRightSide.main, modButton);
                    }));
                }
            }
        }

        private static void SyncRankedModeSelectLabels(MainMenuRightSide rightSide)
        {
            GameObject modeSelectGroup = FindGroup(rightSide, ModModeSelectGroupName);
            if (modeSelectGroup == null)
            {
                return;
            }

            Transform content = modeSelectGroup.transform.Find("Scroll View/Viewport/SavedGameAreaContent");
            if (content == null)
            {
                return;
            }

            if (content.childCount > 0)
            {
                ConfigureActionRow(
                    content.GetChild(0).gameObject,
                    QueueRandomRaceComingSoonLabel,
                    false,
                    26,
                    null);
            }

            if (content.childCount > 1)
            {
                ConfigureActionRow(
                    content.GetChild(1).gameObject,
                    HostRaceLabel,
                    true,
                    26,
                    delegate
                    {
                        PromptAndBeginPrivateRaceHost();
                    });
            }

            if (content.childCount > 2)
            {
                ConfigureActionRow(
                    content.GetChild(2).gameObject,
                    JoinRaceLabel,
                    true,
                    26,
                    delegate
                    {
                        PromptAndBeginPrivateRaceJoin();
                    });
            }

            if (content.childCount > 3)
            {
                ConfigureActionRow(
                    content.GetChild(3).gameObject,
                    SingleplayerRacePracticeLabel,
                    true,
                    26,
                    delegate
                    {
                        ModClientSessionMode.SelectRankedSingleplayerPractice();
                        OpenRankedPracticeNewGame(uGUI_MainMenu.main, MainMenuRightSide.main, FindPrimaryButton("ButtonRanked"));
                    });
            }

            SetPanelTitle(modeSelectGroup, "Choose Mode");
        }

        private static void CachePrivateRaceChoiceTemplates(GameObject roomGroup)
        {
            if (roomGroup == null)
            {
                return;
            }

            uGUI_Choice templateChoice = roomGroup.GetComponentInChildren<uGUI_Choice>(true);
            if (templateChoice == null)
            {
                return;
            }

            if (templateChoice.previousButton != null)
            {
                _privateRaceChoicePreviousArrowTemplate = templateChoice.previousButton;
            }

            if (templateChoice.nextButton != null)
            {
                _privateRaceChoiceNextArrowTemplate = templateChoice.nextButton;
            }
        }

        private static void SyncRankedQueueLabels(MainMenuRightSide rightSide)
        {
            GameObject modGroup = FindGroup(rightSide, ModQueueGroupName);
            if (modGroup == null)
            {
                return;
            }

            Transform content = modGroup.transform.Find("Scroll View/Viewport/SavedGameAreaContent");
            if (content == null)
            {
                return;
            }

            QueueRowDefinition[] rows = new QueueRowDefinition[]
            {
                new QueueRowDefinition("Queue Survival\nComing in Future Update", isInteractable: false, DisabledQueueButtonFontSize),
                new QueueRowDefinition("Queue Creative\nComing in Future Update", isInteractable: false, DisabledQueueButtonFontSize),
                new QueueRowDefinition("Hardcore\nComing in Future Update", isInteractable: false, DisabledQueueButtonFontSize)
            };

            int rowCount = Math.Min(rows.Length, content.childCount);
            for (int i = 0; i < rowCount; i++)
            {
                GameObject row = content.GetChild(i).gameObject;
                Button button = row.GetComponentInChildren<Button>(true);
                if (button != null)
                {
                    SetButtonLabel(button.gameObject, rows[i].Label);
                    SetButtonLabelFontSize(button.gameObject, rows[i].FontSize);
                    button.interactable = rows[i].IsInteractable;
                }

                ApplyQueueRowPresentation(row, rows[i]);
            }

            SetPanelTitle(modGroup, "Queue Random Race");
        }

        private static void SyncRankedMatchmakingGroup(MainMenuRightSide rightSide)
        {
            GameObject matchmakingGroup = FindGroup(rightSide, ModMatchmakingGroupName);
            if (matchmakingGroup == null)
            {
                return;
            }

            ConfigureRankedMatchmakingGroup(matchmakingGroup);
        }

        private static void SyncRankedPracticeNewGameGroup(MainMenuRightSide rightSide)
        {
            GameObject practiceGroup = FindGroup(rightSide, ModPracticeNewGameGroupName);
            if (practiceGroup == null)
            {
                return;
            }

            ConfigureRankedPracticeNewGameGroup(practiceGroup);
        }

        private static void SyncPracticeSaveGroup(MainMenuRightSide rightSide)
        {
            GameObject practiceSaveGroup = FindGroup(rightSide, ModPracticeSaveGroupName);
            if (practiceSaveGroup == null)
            {
                return;
            }

            ConfigurePracticeSaveGroup(practiceSaveGroup);
        }

        private static void SyncLeaderboardHomeGroup(MainMenuRightSide rightSide)
        {
            GameObject leaderboardGroup = FindGroup(rightSide, LeaderboardHomeGroupName);
            if (leaderboardGroup == null)
            {
                return;
            }

            ConfigureLeaderboardHomeGroup(leaderboardGroup);
        }

        private static void SuppressBuiltInWatermarks()
        {
            MainMenuChangeset[] changesets = UnityEngine.Object.FindObjectsOfType<MainMenuChangeset>();
            for (int i = 0; i < changesets.Length; i++)
            {
                MainMenuChangeset changeset = changesets[i];
                if (changeset != null && changeset.gameObject.activeSelf)
                {
                    changeset.gameObject.SetActive(false);
                }
            }
        }

        private static void RefreshWatermarkPresentation()
        {
            uGUI_BuildWatermark[] watermarks = UnityEngine.Object.FindObjectsOfType<uGUI_BuildWatermark>();
            for (int i = 0; i < watermarks.Length; i++)
            {
                uGUI_BuildWatermark watermark = watermarks[i];
                if (watermark != null && watermark.gameObject.activeSelf)
                {
                    watermark.gameObject.SetActive(false);
                }
            }

            SuppressBuiltInWatermarks();
        }

        private static void RestoreVanillaSingleplayerPanelPresentation(MainMenuRightSide rightSide)
        {
            if (rightSide == null)
            {
                return;
            }

            GameObject savedGamesGroup = FindGroup(rightSide, "SavedGames");
            if (savedGamesGroup != null)
            {
                RemoveCustomPanelTitle(savedGamesGroup);
                RestoreOriginalPanelTitles(savedGamesGroup);
            }
        }

        private static void EnsureLeaderboardHomeVisible(MainMenuRightSide rightSide)
        {
            if (rightSide == null)
            {
                return;
            }

            GameObject leaderboardGroup = FindGroup(rightSide, LeaderboardHomeGroupName);
            if (leaderboardGroup == null)
            {
                return;
            }

            bool shouldBeVisible = ShouldShowUpdateHomePanel(rightSide, leaderboardGroup);
            if (shouldBeVisible)
            {
                leaderboardGroup.transform.SetAsLastSibling();
                if (!leaderboardGroup.activeSelf)
                {
                    leaderboardGroup.SetActive(true);
                }
            }
            else if (leaderboardGroup.activeSelf)
            {
                leaderboardGroup.SetActive(false);
            }
        }

        private static bool ShouldShowUpdateHomePanel(MainMenuRightSide rightSide, GameObject leaderboardGroup)
        {
            if (rightSide == null || leaderboardGroup == null)
            {
                return false;
            }

            for (int i = 0; i < rightSide.groups.Count; i++)
            {
                GameObject group = rightSide.groups[i];
                if (group == null ||
                    group == leaderboardGroup ||
                    group == rightSide.homeGroup)
                {
                    continue;
                }

                if (group.activeSelf)
                {
                    return false;
                }
            }

            return true;
        }

        private static void SetPanelTitle(GameObject panelGroup, string title)
        {
            if (panelGroup == null)
            {
                return;
            }

            EnsureCustomPanelTitle(panelGroup, title);
            HideOriginalPanelTitles(panelGroup);
        }

        private static void RemoveCustomPanelTitle(GameObject panelGroup)
        {
            if (panelGroup == null)
            {
                return;
            }

            Transform existingTransform = panelGroup.transform.Find("ModCustomPanelTitle");
            if (existingTransform == null)
            {
                return;
            }

            existingTransform.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(existingTransform.gameObject);
        }

        private static void ConfigureUpdatePanelLayout(GameObject panelGroup)
        {
            if (panelGroup == null)
            {
                return;
            }

            panelGroup.transform.SetAsLastSibling();

            RectTransform panelRect = panelGroup.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = new Vector2(1f, 0.5f);
                panelRect.anchorMax = new Vector2(1f, 0.5f);
                panelRect.pivot = new Vector2(1f, 0.5f);
                panelRect.anchoredPosition = new Vector2(-18f, -34f);
                panelRect.sizeDelta = new Vector2(420f, 300f);
                panelRect.localScale = Vector3.one;
            }

            Transform scrollViewTransform = panelGroup.transform.Find("Scroll View");
            RectTransform scrollViewRect = scrollViewTransform != null ? scrollViewTransform.GetComponent<RectTransform>() : null;
            if (scrollViewRect != null)
            {
                scrollViewRect.anchorMin = new Vector2(0f, 0f);
                scrollViewRect.anchorMax = new Vector2(1f, 1f);
                scrollViewRect.offsetMin = new Vector2(14f, 14f);
                scrollViewRect.offsetMax = new Vector2(-14f, -46f);
                scrollViewRect.localScale = Vector3.one;
            }

            Transform scrollbarTransform = panelGroup.transform.Find("Scroll View/Scrollbar Vertical");
            if (scrollbarTransform != null)
            {
                scrollbarTransform.gameObject.SetActive(false);
            }
        }

        private static void SetButtonLabelFontSize(GameObject root, int fontSize)
        {
            if (root == null)
            {
                return;
            }

            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null)
                {
                    continue;
                }

                text.fontSize = fontSize;
            }
        }

        private static void ApplyQueueRowPresentation(GameObject row, QueueRowDefinition definition)
        {
            if (row == null)
            {
                return;
            }

            SetAlpha(row, definition.IsInteractable ? 1f : ComingSoonRowAlpha);

            Text[] texts = row.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null)
                {
                    continue;
                }

                text.fontSize = definition.FontSize;
                text.color = definition.IsInteractable ? Color.white : new Color(0.9f, 0.9f, 0.9f, 0.95f);
                text.alignment = TextAnchor.MiddleCenter;
            }
        }

        private static void EnsureCustomPanelTitle(GameObject panelGroup, string title)
        {
            Transform existingTransform = panelGroup.transform.Find("ModCustomPanelTitle");
            Text titleText = existingTransform != null ? existingTransform.GetComponent<Text>() : null;
            if (titleText == null)
            {
                Text template = panelGroup.GetComponentInChildren<Text>(true);
                if (template == null)
                {
                    return;
                }

                GameObject titleObject = UnityEngine.Object.Instantiate(template.gameObject, panelGroup.transform, false);
                titleObject.name = "ModCustomPanelTitle";

                uGUI_Text localizer = titleObject.GetComponent<uGUI_Text>();
                if (localizer != null)
                {
                    UnityEngine.Object.Destroy(localizer);
                }

                titleText = titleObject.GetComponent<Text>();
                if (titleText == null)
                {
                    return;
                }
            }

            RectTransform rectTransform = titleText.rectTransform;
            rectTransform.SetParent(panelGroup.transform, false);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(24f, -22f);
            rectTransform.sizeDelta = new Vector2(-48f, 58f);
            rectTransform.localScale = Vector3.one;

            titleText.text = title;
            titleText.fontSize = PanelTitleFontSize;
            titleText.alignment = TextAnchor.UpperLeft;
            titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleText.verticalOverflow = VerticalWrapMode.Truncate;
            titleText.raycastTarget = false;
            titleText.color = Color.white;
        }

        private static void SetCustomPanelTitleFontSize(GameObject panelGroup, int fontSize)
        {
            if (panelGroup == null)
            {
                return;
            }

            Transform existingTransform = panelGroup.transform.Find("ModCustomPanelTitle");
            Text titleText = existingTransform != null ? existingTransform.GetComponent<Text>() : null;
            if (titleText == null)
            {
                return;
            }

            titleText.fontSize = fontSize;
        }

        private static void UpdatePrivateRaceConnectingModal()
        {
            if (!ModPrivateRaceRoomRuntimeHost.IsConnecting && !ModPrivateRaceRoomRuntimeHost.IsError)
            {
                HidePrivateRaceConnectingModal();
                return;
            }

            GameObject modalRoot = EnsurePrivateRaceConnectingModal();
            if (modalRoot == null)
            {
                return;
            }

            if (_privateRaceConnectingModalTitleText != null)
            {
                _privateRaceConnectingModalTitleText.text = ModPrivateRaceRoomRuntimeHost.IsError ? "Connection Failed" : "Connecting";
            }

            if (_privateRaceConnectingModalBodyText != null)
            {
                _privateRaceConnectingModalBodyText.text = ModPrivateRaceRoomRuntimeHost.GetStatusMessage();
            }

            if (_privateRaceConnectingModalActionButton != null)
            {
                SetButtonLabel(_privateRaceConnectingModalActionButton.gameObject, ModPrivateRaceRoomRuntimeHost.IsError ? "Back" : "Cancel");
                _privateRaceConnectingModalActionButton.onClick = new Button.ButtonClickedEvent();
                _privateRaceConnectingModalActionButton.onClick.AddListener(new UnityAction(delegate
                {
                    ModPrivateRaceRoomRuntimeHost.LeaveRoom();
                    HidePrivateRaceConnectingModal();
                    OpenRankedModeSelect(uGUI_MainMenu.main, MainMenuRightSide.main, FindPrimaryButton("ButtonRanked"));
                }));
            }

            modalRoot.SetActive(true);
            modalRoot.transform.SetAsLastSibling();
        }

        private static void HidePrivateRaceConnectingModal()
        {
            if (_privateRaceConnectingModalRoot != null && _privateRaceConnectingModalRoot.activeSelf)
            {
                _privateRaceConnectingModalRoot.SetActive(false);
            }
        }

        private static GameObject EnsurePrivateRaceConnectingModal()
        {
            if (_privateRaceConnectingModalRoot != null)
            {
                return _privateRaceConnectingModalRoot;
            }

            uGUI_MainMenu menu = uGUI_MainMenu.main;
            if (menu == null)
            {
                return null;
            }

            Canvas parentCanvas = menu.GetComponentInParent<Canvas>();
            Text textTemplate = menu.GetComponentInChildren<Text>(true);
            if (parentCanvas == null || textTemplate == null)
            {
                return null;
            }

            _privateRaceConnectingModalRoot = new GameObject("ModPrivateRaceConnectingModal", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            _privateRaceConnectingModalRoot.transform.SetParent(parentCanvas.transform, false);

            RectTransform rootRect = _privateRaceConnectingModalRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image rootImage = _privateRaceConnectingModalRoot.GetComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0.55f);
            rootImage.raycastTarget = true;

            CanvasGroup rootCanvasGroup = _privateRaceConnectingModalRoot.GetComponent<CanvasGroup>();
            rootCanvasGroup.alpha = 1f;
            rootCanvasGroup.interactable = true;
            rootCanvasGroup.blocksRaycasts = true;

            GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(_privateRaceConnectingModalRoot.transform, false);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 260f);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.16f, 0.57f, 0.91f, 0.97f);
            panelImage.raycastTarget = true;

            _privateRaceConnectingModalTitleText = UnityEngine.Object.Instantiate(textTemplate, panelObject.transform, false);
            _privateRaceConnectingModalTitleText.name = "Title";
            RemoveLocalizationComponents(_privateRaceConnectingModalTitleText.gameObject);
            RectTransform titleRect = _privateRaceConnectingModalTitleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(28f, -80f);
            titleRect.offsetMax = new Vector2(-28f, -20f);
            _privateRaceConnectingModalTitleText.alignment = TextAnchor.MiddleLeft;
            _privateRaceConnectingModalTitleText.fontSize = 32;
            _privateRaceConnectingModalTitleText.color = Color.white;
            _privateRaceConnectingModalTitleText.text = "Connecting";
            _privateRaceConnectingModalTitleText.raycastTarget = false;
            _privateRaceConnectingModalTitleText.gameObject.SetActive(true);

            _privateRaceConnectingModalBodyText = UnityEngine.Object.Instantiate(textTemplate, panelObject.transform, false);
            _privateRaceConnectingModalBodyText.name = "Body";
            RemoveLocalizationComponents(_privateRaceConnectingModalBodyText.gameObject);
            RectTransform bodyRect = _privateRaceConnectingModalBodyText.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 0.5f);
            bodyRect.anchorMax = new Vector2(1f, 0.5f);
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.offsetMin = new Vector2(28f, -24f);
            bodyRect.offsetMax = new Vector2(-28f, 24f);
            _privateRaceConnectingModalBodyText.alignment = TextAnchor.MiddleLeft;
            _privateRaceConnectingModalBodyText.fontSize = 24;
            _privateRaceConnectingModalBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _privateRaceConnectingModalBodyText.verticalOverflow = VerticalWrapMode.Overflow;
            _privateRaceConnectingModalBodyText.color = Color.white;
            _privateRaceConnectingModalBodyText.text = "Connecting to private room...";
            _privateRaceConnectingModalBodyText.raycastTarget = false;
            _privateRaceConnectingModalBodyText.gameObject.SetActive(true);

            GameObject buttonObject = new GameObject("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panelObject.transform, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.sizeDelta = new Vector2(220f, 56f);
            buttonRect.anchoredPosition = new Vector2(0f, 26f);

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.11f, 0.44f, 0.86f, 1f);
            buttonImage.raycastTarget = true;

            _privateRaceConnectingModalActionButton = buttonObject.GetComponent<Button>();
            _privateRaceConnectingModalActionButton.targetGraphic = buttonImage;

            Text buttonText = UnityEngine.Object.Instantiate(textTemplate, buttonObject.transform, false);
            buttonText.name = "Text";
            RemoveLocalizationComponents(buttonText.gameObject);
            RectTransform buttonTextRect = buttonText.rectTransform;
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.fontSize = 26;
            buttonText.color = Color.white;
            buttonText.text = "Cancel";
            buttonText.raycastTarget = false;
            buttonText.gameObject.SetActive(true);

            _privateRaceConnectingModalRoot.SetActive(false);
            return _privateRaceConnectingModalRoot;
        }

        private static string BuildPrivateRaceRoomLayoutState()
        {
            return ModPrivateRaceRoomRuntimeHost.HostDisplayNameOrDefault + "|" +
                   ModPrivateRaceRoomRuntimeHost.GetRoomCodeText() + "|" +
                   ModPrivateRaceRoomRuntimeHost.GetJoinerDisplayText() + "|" +
                   ModPrivateRaceRoomRuntimeHost.GetStatusMessage() + "|" +
                   ModPrivateRaceRoomRuntimeHost.SelectedMode + "|" +
                   ModPrivateRaceRoomRuntimeHost.CanStartRace().ToString() + "|" +
                   ModPrivateRaceRoomRuntimeHost.IsLaunching.ToString() + "|" +
                   ModPrivateRaceRoomRuntimeHost.IsLocalHost.ToString();
        }

        private static void EnsurePanelPlaceholder(GameObject panelGroup, Transform content, string objectName, string textValue)
        {
            if (panelGroup == null || content == null)
            {
                return;
            }

            Transform existingTransform = content.Find(objectName);
            Text placeholderText = existingTransform != null ? existingTransform.GetComponent<Text>() : null;
            if (placeholderText == null)
            {
                Text template = panelGroup.GetComponentInChildren<Text>(true);
                if (template == null)
                {
                    return;
                }

                GameObject placeholderObject = UnityEngine.Object.Instantiate(template.gameObject, content, false);
                placeholderObject.name = objectName;

                uGUI_Text localizer = placeholderObject.GetComponent<uGUI_Text>();
                if (localizer != null)
                {
                    UnityEngine.Object.Destroy(localizer);
                }

                placeholderText = placeholderObject.GetComponent<Text>();
                if (placeholderText == null)
                {
                    return;
                }
            }

            RectTransform rectTransform = placeholderText.rectTransform;
            rectTransform.SetParent(content, false);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -58f);
            rectTransform.sizeDelta = new Vector2(-60f, 60f);
            rectTransform.localScale = Vector3.one;

            placeholderText.text = textValue;
            placeholderText.alignment = TextAnchor.UpperCenter;
            placeholderText.horizontalOverflow = HorizontalWrapMode.Wrap;
            placeholderText.verticalOverflow = VerticalWrapMode.Overflow;
            placeholderText.raycastTarget = false;
            placeholderText.color = Color.white;
            placeholderText.resizeTextForBestFit = false;
            placeholderText.fontSize = Math.Max(placeholderText.fontSize, 17);
        }

        private static void EnsureUpdatePanelBodyText(GameObject panelGroup, string textValue)
        {
            if (panelGroup == null)
            {
                return;
            }

            Transform existingTransform = panelGroup.transform.Find(UpdatePanelBodyObjectName);
            Text bodyText = existingTransform != null ? existingTransform.GetComponent<Text>() : null;
            if (bodyText == null)
            {
                Text template = panelGroup.GetComponentInChildren<Text>(true);
                if (template == null)
                {
                    return;
                }

                GameObject bodyObject = UnityEngine.Object.Instantiate(template.gameObject, panelGroup.transform, false);
                bodyObject.name = UpdatePanelBodyObjectName;

                uGUI_Text localizer = bodyObject.GetComponent<uGUI_Text>();
                if (localizer != null)
                {
                    UnityEngine.Object.Destroy(localizer);
                }

                bodyText = bodyObject.GetComponent<Text>();
                if (bodyText == null)
                {
                    return;
                }
            }

            RectTransform rectTransform = bodyText.rectTransform;
            rectTransform.SetParent(panelGroup.transform, false);
            rectTransform.SetAsLastSibling();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(20f, -54f);
            rectTransform.sizeDelta = new Vector2(-40f, 196f);
            rectTransform.localScale = Vector3.one;

            bodyText.text = textValue;
            bodyText.fontSize = UpdatePanelBodyFontSize;
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            bodyText.raycastTarget = false;
            bodyText.resizeTextForBestFit = false;
            bodyText.color = Color.white;
            bodyText.supportRichText = true;
            bodyText.gameObject.SetActive(true);
        }

        private static void HideOriginalPanelTitles(GameObject panelGroup)
        {
            Text[] texts = panelGroup.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null || string.Equals(text.gameObject.name, "ModCustomPanelTitle", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(text.text, "Play", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text.text, "Play Singleplayer", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text.text, "Options", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text.text, "PrivateRaceRoom", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text.text, "Private Race Room", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text.text, "Choose Mode", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text.text, "New Game", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text.text, "Private Race Room", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text.text, "Connecting", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text.text, ModPracticeSaveCatalog.GetPrimaryCategoryDisplayName(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text.text, "Queue Ranked Matches", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text.text, "Queue Random Race", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text.text, "Leaderboard", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text.text, UpdatePanelTitleText, StringComparison.OrdinalIgnoreCase))
                {
                    text.gameObject.SetActive(false);
                }
            }
        }

        private static void RestoreOriginalPanelTitles(GameObject panelGroup)
        {
            if (panelGroup == null)
            {
                return;
            }

            Text[] texts = panelGroup.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null || string.Equals(text.gameObject.name, "ModCustomPanelTitle", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(text.text, "Play", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text.text, "Play Singleplayer", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text.text, "New Game", StringComparison.OrdinalIgnoreCase))
                {
                    text.gameObject.SetActive(true);
                }
            }
        }

        private static void HideAllRightSideGroupsExceptHome(MainMenuRightSide rightSide)
        {
            if (rightSide == null)
            {
                return;
            }

            for (int i = 0; i < rightSide.groups.Count; i++)
            {
                GameObject group = rightSide.groups[i];
                if (group == null || group == rightSide.homeGroup)
                {
                    continue;
                }

                group.SetActive(false);
            }
        }

        private static void SetAlpha(GameObject root, float alpha)
        {
            if (root == null)
            {
                return;
            }

            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null)
                {
                    continue;
                }

                Color color = graphic.color;
                color.a = alpha;
                graphic.color = color;
            }
        }

        private static void DestroyChildIfPresent(Transform parent, string objectName)
        {
            if (parent == null || string.IsNullOrEmpty(objectName))
            {
                return;
            }

            Transform child = parent.Find(objectName);
            if (child == null)
            {
                return;
            }

            child.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(child.gameObject);
        }

        private static void AttachSceneRuntimeBehaviour(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                _sceneRuntimeBehaviour = root.GetComponent<ModUiRuntimeBehaviour>();
                if (_sceneRuntimeBehaviour == null)
                {
                    _sceneRuntimeBehaviour = root.AddComponent<ModUiRuntimeBehaviour>();
                }

                if (_sceneRuntimeBehaviour != null)
                {
                    ModLog.Info("Attached ranked UI runtime behaviour to scene root '" + root.name + "'.");
                    return;
                }
            }
        }

        private static void TryAttachPersistentRuntimeBehaviour()
        {
            if (_persistentRuntimeBehaviour != null)
            {
                return;
            }

            if (uGUI.main == null)
            {
                return;
            }

            GameObject root = uGUI.main.gameObject;
            _persistentRuntimeBehaviour = root.GetComponent<ModUiRuntimeBehaviour>();
            if (_persistentRuntimeBehaviour == null)
            {
                _persistentRuntimeBehaviour = root.AddComponent<ModUiRuntimeBehaviour>();
            }

            if (_persistentRuntimeBehaviour != null)
            {
                ModLog.Info("Attached ranked UI runtime behaviour to persistent uGUI root.");
            }
        }

        private static void EnsurePersistentWatermark()
        {
            if (_fallbackWatermarkText != null)
            {
                return;
            }

            if (Time.unscaledTime < _nextPersistentWatermarkRetryAt)
            {
                return;
            }

            _nextPersistentWatermarkRetryAt = Time.unscaledTime + PersistentWatermarkRetryIntervalSeconds;

            Canvas canvas = ModOverlayUiUtility.GetOrCreatePersistentOverlayCanvas();
            if (canvas == null)
            {
                return;
            }

            ModLog.Info("Preparing watermark overlay on canvas '" + canvas.name + "'.");

            Transform existingRoot = canvas.transform.Find(FallbackWatermarkRootName);
            GameObject root = existingRoot != null ? existingRoot.gameObject : null;
            if (root == null)
            {
                root = new GameObject(FallbackWatermarkRootName, typeof(RectTransform));
            }

            root.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 0f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            root.transform.SetAsLastSibling();

            Transform existingTextTransform = root.transform.Find("WatermarkText");
            GameObject textObject = existingTextTransform != null ? existingTextTransform.gameObject : null;
            if (textObject == null)
            {
                textObject = new GameObject("WatermarkText", typeof(RectTransform));
            }

            textObject.transform.SetParent(root.transform, false);

            Text text = textObject.GetComponent<Text>();
            if (text == null)
            {
                text = textObject.AddComponent<Text>();
            }

            ModOverlayUiUtility.ApplyTemplate(
                text,
                ModOverlayUiUtility.FindTemplateText(),
                WatermarkFontSize,
                TextAnchor.UpperRight);

            RectTransform rectTransform = text.rectTransform;
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(-12f, -12f);
            rectTransform.sizeDelta = new Vector2(560f, 24f);

            _fallbackWatermarkRoot = root;
            _fallbackWatermarkText = text;
            RefreshPersistentWatermark();
            ModLog.Info("Created fallback watermark overlay under '" + canvas.name + "'.");
        }

        private static Font ResolveWatermarkFont()
        {
            uGUI_BuildWatermark watermark = UnityEngine.Object.FindObjectOfType<uGUI_BuildWatermark>();
            if (watermark != null)
            {
                Text text = watermark.GetComponent<Text>();
                if (text != null && text.font != null)
                {
                    return text.font;
                }
            }

            MainMenuChangeset changeset = UnityEngine.Object.FindObjectOfType<MainMenuChangeset>();
            if (changeset != null)
            {
                Text text = changeset.GetComponent<Text>();
                if (text != null && text.font != null)
                {
                    return text.font;
                }
            }

            return Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        }

        private static void RefreshPersistentWatermark()
        {
            if (_fallbackWatermarkText == null)
            {
                return;
            }

            _fallbackWatermarkText.text = ModClientWatermark;
            _fallbackWatermarkText.fontSize = WatermarkFontSize;

            if (_fallbackWatermarkRoot != null && !_fallbackWatermarkRoot.activeSelf)
            {
                _fallbackWatermarkRoot.SetActive(true);
            }
        }

        private static void ResetMainMenuButtonVisualState(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            MainMenuOffsetAnimator[] animators = root.GetComponentsInChildren<MainMenuOffsetAnimator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                MainMenuOffsetAnimator animator = animators[i];
                if (animator == null || animator.target == null)
                {
                    continue;
                }

                animator.OnPointerExit(null);
                animator.target.localPosition -= animator.offset;
            }

            uGUI_BasicColorSwap[] colorSwaps = root.GetComponentsInChildren<uGUI_BasicColorSwap>(true);
            for (int i = 0; i < colorSwaps.Length; i++)
            {
                uGUI_BasicColorSwap colorSwap = colorSwaps[i];
                if (colorSwap != null)
                {
                    colorSwap.makeTextWhite();
                }
            }
        }

        private struct QueueRowDefinition
        {
            public QueueRowDefinition(string label, bool isInteractable, int fontSize)
            {
                Label = label;
                IsInteractable = isInteractable;
                FontSize = fontSize;
            }

            public string Label { get; private set; }

            public bool IsInteractable { get; private set; }

            public int FontSize { get; private set; }
        }

        private sealed class ModUiRuntimeBehaviour : MonoBehaviour
        {
            private void Update()
            {
                if (_persistentRuntimeBehaviour == null || ReferenceEquals(_persistentRuntimeBehaviour, this))
                {
                    OnRuntimeUpdate();
                    return;
                }

                TryAttachPersistentRuntimeBehaviour();
            }
        }

        private sealed class ModSessionModeMarker : MonoBehaviour
        {
            public ModClientLaunchMode LaunchMode;
        }

        private sealed class ModBetterRngSavedGamesRowMarker : MonoBehaviour
        {
        }

        private sealed class ModPrivateRaceChoiceRowMarker : MonoBehaviour
        {
        }

        private sealed class ModPrivateRaceStartButtonMarker : MonoBehaviour
        {
        }
    }
}
