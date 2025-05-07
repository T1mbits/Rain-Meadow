using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Menu;
using Menu.Remix.MixedUI;
using MoreSlugcats;
using RainMeadow.UI.Components;
using RWCustom;
using UnityEngine;

namespace RainMeadow.UI;

public class ArenaLobbyMenu2 : SmartMenu, SelectOneButton.SelectOneButtonOwner
{
    public static string[] PainCatNames => ["Inv", "Enot", "Paincat", "Sofanthiel", "Gorbo"]; // not using "???" cause it might cause some confusion to players who don't know Inv
    private ArenaOnlineGameMode Arena => (ArenaOnlineGameMode)OnlineManager.lobby.gameMode;
    public List<SlugcatStats.Name> allSlugcats = ArenaHelpers.AllSlugcats();
    public SimplerButton playButton, slugcatSelectBackButton;
    public FSprite[] settingsDivSprites, slugcatDescriptionGradients;
    public Vector2[] settingsDivSpritesPos, slugcatDescriptionGradientsPos, oldPagesPos = [];
    public Vector2 newPagePos = Vector2.zero;
    public MenuLabel slugcatNameLabel, slugcatDescriptionLabel;
    public RestorableMenuLabel countdownTimerLabel;
    public RestorableMenuLabel? saintAscendanceTimerLabel;
    public SimplerCheckbox spearsHitCheckbox, aggressiveAICheckBox;
    public SimplerCheckbox? maulingCheckBox, artificerStunCheckBox, sainotCheckBox, painCatEggCheckBox, painCatThrowsCheckBox, painCatLizardCheckBox;
    public SimplerMultipleChoiceArray roomRepeatChoiceArray, rainTimerChoiceArray, wildlifeChoiceArray;
    // public TextBox countdownTimerTextBox, saintAscendDurationTimerTextBox;
    // public ComboBox arenaGameModeComboBox;
    public EventfulSelectOneButton[] slugcatSelectButtons;
    public TabContainer tabContainer;
    public PlayerDisplayer? playerDisplayer;
    public ColorSlugcatDialog? colorSlugcatDialog;
    public MenuIllustration competitiveTitle, competitiveShadow;
    public MenuScene.SceneID slugcatScene;
    public string? painCatName;
    public string? painCatDescription;
    public int selectedSlugcatIndex = 0;
    public Page slugcatSelectPage;
    public bool pagesMoving = false, pageFullyTransitioned = true, pendingBgChange = false;
    public float pageMovementProgress = 0, desiredBgCoverAlpha = 0, lastDesiredBgCoverAlpha = 0;
    public override MenuScene.SceneID GetScene => ModManager.MMF ? manager.rainWorld.options.subBackground : MenuScene.SceneID.Landscape_SU;

    public ArenaLobbyMenu2(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.ArenaLobbyMenu)
    {
        RainMeadow.DebugMe();
        if (OnlineManager.lobby == null)
            throw new InvalidOperationException("lobby is null");

        Futile.atlasManager.LoadAtlas("illustrations/arena_ui_elements");

        Competitive competitive = new();
        if (!Arena.registeredGameModes.ContainsKey(competitive))
            Arena.registeredGameModes.Add(new Competitive(), Competitive.CompetitiveMode.value);

        if (Arena.currentGameMode == "" || Arena.currentGameMode == null)
            Arena.currentGameMode = Competitive.CompetitiveMode.value;

        pages.Add(slugcatSelectPage = new Page(this, null, "slugcat select", 1));
        slugcatSelectPage.pos.x += 1500f;

        ChangeScene(slugcatScene = Arena.slugcatSelectMenuScenes[Arena.arenaClientSettings.playingAs.value]);

        competitiveShadow = new MenuIllustration(this, scene, "", "CompetitiveShadow", new Vector2(-2.99f, 265.01f), true, false);
        competitiveTitle = new MenuIllustration(this, scene, "", "CompetitiveTitle", new Vector2(-2.99f, 265.01f), true, false);
        competitiveTitle.sprite.shader = manager.rainWorld.Shaders["MenuText"];

        playButton = new SimplerButton(this, mainPage, Utils.Translate("READY?"), new Vector2(1056f, 50f), new Vector2(110f, 30f));
        // playButton.OnClick += _ => MovePage(new Vector2(-1500f, 0f), 1);

        tabContainer = new TabContainer(this, mainPage, new Vector2(470f, 125f), new Vector2(450, 475));

        mainPage.SafeAddSubobjects(competitiveShadow, competitiveTitle, playButton, tabContainer);

        Vector2 matchSettingsOffset = new(120f, 205f);
        float settingsElementWidth = 300f;

        spearsHitCheckbox = new SimplerCheckbox(this, tabContainer, matchSettingsOffset + new Vector2(0f, 220f), 95f, Translate("Friendly Fire:"));
        spearsHitCheckbox.OnClick += c => RainMeadow.Debug($"friendly fire: {c}");

        aggressiveAICheckBox = new SimplerCheckbox(this, tabContainer, matchSettingsOffset + new Vector2(settingsElementWidth - 24f, 220f), InGameTranslator.LanguageID.UsesLargeFont(CurrLang) ? 120f : 100f, Translate("Aggressive AI:"));
        aggressiveAICheckBox.OnClick += c => RainMeadow.Debug($"aggressive ai: {c}");

        roomRepeatChoiceArray = new SimplerMultipleChoiceArray(this, tabContainer, matchSettingsOffset + new Vector2(0f, 150f), Translate("Repeat Rooms:"), InGameTranslator.LanguageID.UsesLargeFont(CurrLang) ? 115f : 95f, settingsElementWidth, 5, textInBoxes: true);
        roomRepeatChoiceArray.OnClick += i => RainMeadow.Debug($"room repeat: pressed {i}");
        for (int i = 0; i < roomRepeatChoiceArray.buttons.Length; i++)
            roomRepeatChoiceArray.buttons[i].label.text = $"{i + 1}x";

        rainTimerChoiceArray = new(this, tabContainer, matchSettingsOffset + new Vector2(0f, 100f), Translate("Rain Timer:"), InGameTranslator.LanguageID.UsesLargeFont(CurrLang) ? 100f : 95f, settingsElementWidth, 6, splitText: CurrLang == InGameTranslator.LanguageID.French || CurrLang == InGameTranslator.LanguageID.Spanish || CurrLang == InGameTranslator.LanguageID.Portuguese);
        rainTimerChoiceArray.OnClick += i => RainMeadow.Debug($"rain timer: pressed {i}");

        wildlifeChoiceArray = new SimplerMultipleChoiceArray(this, tabContainer, matchSettingsOffset + new Vector2(0f, 50f), Translate("Wildlife:"), 95f, settingsElementWidth, 4);
        wildlifeChoiceArray.OnClick += i => RainMeadow.Debug($"wildlife: pressed {i}");

        countdownTimerLabel = new RestorableMenuLabel(this, tabContainer, Translate("Countdown Timer:"), new Vector2(25f, 160f), new Vector2(105f, 20f), false);

        // settingsMenuLabels = new MenuLabel[2];
        // settingsMenuLabels[0] = new MenuLabel(this, mainPage, "Countdown Timer:", matchSettingsOffset + new Vector2(0f, -17f), new Vector2(0f, 30f), false);
        // settingsMenuLabels[1] = new MenuLabel(this, mainPage, "Saint Ascend Time:", matchSettingsOffset + new Vector2(125f, -67f), new Vector2(0f, 30f), false);
        // mainPage.subObjects.AddRange(settingsMenuLabels);
        // countdownTimerTextBox = new OpTextBox(new Configurable<int>(5), matchSettingsOffset + new Vector2(215f, -20f), 95f);
        // countdownTimerTextBox.OnChange += () => { RainMeadow.Debug($"countdown timer textbox: {countdownTimerTextBox.value}"); };
        // UIelementWrapper countdownTimerTextBoxWrapper = new(tabWrapper, countdownTimerTextBox);
        // mainPage.subObjects.Add(countdownTimerTextBoxWrapper);

        tabContainer.AddTab("Arena Playlist", []);
        tabContainer.AddTab(
            "Match Settings",
            [
                spearsHitCheckbox,
                aggressiveAICheckBox,
                roomRepeatChoiceArray,
                rainTimerChoiceArray,
                wildlifeChoiceArray,
                countdownTimerLabel,
                // countdownTimerTextBox,
                // arenaGameModeComboBox,
            ]
        );

        settingsDivSprites = new FSprite[2];
        settingsDivSpritesPos = new Vector2[2];
        for (int i = 0; i < settingsDivSprites.Length; i++)
        {
            settingsDivSprites[i] = new FSprite("pixel")
            {
                anchorX = 0f,
                scaleX = settingsElementWidth + 95f,
                scaleY = 2f,
                color = MenuRGB(MenuColors.VeryDarkGrey),
            };
            settingsDivSpritesPos[i] = tabContainer.pos + matchSettingsOffset + new Vector2(-95f, 197f - (171 * i));
        }

        if (ModManager.MSC)
        {
            Vector2 abilitySettingsOffset = new(360f, 380f);
            Vector2 abilitySettingsSpacing = new(0f, 50f);
            painCatName = PainCatNames[UnityEngine.Random.Range(0, PainCatNames.Length)];

            maulingCheckBox = new SimplerCheckbox(this, tabContainer, abilitySettingsOffset, 300f, Translate("Enable Mauling:"));
            maulingCheckBox.OnClick += i => RainMeadow.Debug($"mauling: {i}");

            artificerStunCheckBox = new SimplerCheckbox(this, tabContainer, abilitySettingsOffset -= abilitySettingsSpacing, 300f, Translate("Artificer Stuns Players:"));
            artificerStunCheckBox.OnClick += i => RainMeadow.Debug($"artificer stuns: {i}");

            sainotCheckBox = new SimplerCheckbox(this, tabContainer, abilitySettingsOffset -= abilitySettingsSpacing, 300f, "Sain't:");
            sainotCheckBox.OnClick += i => RainMeadow.Debug($"sain't: {i}");

            saintAscendanceTimerLabel = new RestorableMenuLabel(this, tabContainer, "Saint Ascendance Duration:", (abilitySettingsOffset -= abilitySettingsSpacing) - new Vector2(300f, 0f), new Vector2(151f, 20f), false);

            painCatEggCheckBox = new SimplerCheckbox(this, tabContainer, abilitySettingsOffset -= abilitySettingsSpacing, 300f, $"{painCatName} gets egg at 0 throw skill:");
            painCatEggCheckBox.OnClick += c => RainMeadow.Debug($"paincat egg: {c}");

            painCatThrowsCheckBox = new SimplerCheckbox(this, tabContainer, abilitySettingsOffset -= abilitySettingsSpacing, 300f, $"{painCatName} can always throw spears:");
            painCatThrowsCheckBox.OnClick += c => RainMeadow.Debug($"paincat spear: {c}");

            painCatLizardCheckBox = new SimplerCheckbox(this, tabContainer, abilitySettingsOffset -= abilitySettingsSpacing, 300f, $"{painCatName} sometimes gets a friend:");
            painCatLizardCheckBox.OnClick += c => RainMeadow.Debug($"paincat friend: {c}");

            tabContainer.AddTab(
             "Slugcat Abilities",
                [
                    maulingCheckBox,
                    artificerStunCheckBox,
                    sainotCheckBox,
                    saintAscendanceTimerLabel,
                    // saintAscendDurationTimerTextBox,
                    painCatLizardCheckBox,
                    painCatEggCheckBox,
                    painCatThrowsCheckBox,
                ]
            );
        }

        slugcatSelectBackButton = new SimplerButton(this, slugcatSelectPage, "Back To Lobby", new Vector2(200f, 50f), new Vector2(110f, 30f));
        slugcatSelectBackButton.OnClick += _ => MovePage(new Vector2(1500f, 0f), 0);
        slugcatSelectPage.subObjects.Add(slugcatSelectBackButton);

        slugcatSelectButtons = new EventfulSelectOneButton[allSlugcats.Count];
        int buttonsInTopRow = (int)Mathf.Floor(allSlugcats.Count / 2f);
        int buttonsInBottomRow = allSlugcats.Count - buttonsInTopRow;
        float topRowStartingXPos = 633f - (buttonsInTopRow / 2 * 110f - ((buttonsInTopRow % 2 == 0) ? 55f : 0f));
        float bottomRowStartingXPos = 633f - (buttonsInBottomRow / 2 * 110f - ((buttonsInBottomRow % 2 == 0) ? 55f : 0f));

        MenuIllustration painCatPortrait = null;
        EventfulSelectOneButton painCatButton = null;
        for (int i = 0; i < allSlugcats.Count; i++)
        {
            int index = i;

            Vector2 pos = i < buttonsInTopRow ? new Vector2(topRowStartingXPos + 110f * i, 450f) : new Vector2(bottomRowStartingXPos + 110f * (i - buttonsInTopRow), 340f);
            EventfulSelectOneButton btn = new(this, slugcatSelectPage, "", "scug select", pos, new Vector2(100f, 100f), slugcatSelectButtons, i);
            btn.OnClick += _ => SwitchSelectedSlugcat(allSlugcats[index]);

            MenuIllustration portrait = new(this, btn, "", SlugcatColorableButton.GetFileForSlugcat(allSlugcats[i], false), btn.size / 2, true, true);
            btn.subObjects.Add(portrait);

            if (allSlugcats[i] == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
            {
                painCatButton = btn;
                painCatPortrait = portrait;
                painCatDescription = Arena.slugcatSelectPainCatDescriptions
                [
                    UnityEngine.Random.Range(0, 3) == 1 ? // 33% chance for portrait specific description, 66% for general quotes
                    int.Parse(Regex.Match(painCatPortrait.fileName, @"\d+").Value[0].ToString()) :
                    UnityEngine.Random.Range(5, Arena.slugcatSelectPainCatDescriptions.Count)
                ].Replace("<USERNAME>", OnlineManager.mePlayer.id.name);
            }

            slugcatSelectPage.subObjects.Add(btn);
            slugcatSelectButtons[i] = btn;
        }

        SimplerButton randomizePainCat = new(this, slugcatSelectPage, $"Randomize {painCatName} select data", new Vector2(1056, 50), new Vector2(300, 30));
        randomizePainCat.OnClick += _ =>
        {
            painCatPortrait.RemoveSprites();
            slugcatSelectPage.RemoveSubObject(painCatPortrait);
            slugcatSelectPage.SafeAddSubobjects(painCatPortrait = new(this, painCatButton, "", SlugcatColorableButton.GetFileForSlugcat(MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel, false), painCatButton.size / 2, true, true));
            bool usePortraitDescription = UnityEngine.Random.Range(0, 3) == 1;
            int portraitIndex = int.Parse(Regex.Match(painCatPortrait.fileName, @"\d+").Value[0].ToString());
            int descriptionIndex = usePortraitDescription ? UnityEngine.Random.Range(5, Arena.slugcatSelectPainCatDescriptions.Count) : portraitIndex;
            RainMeadow.Debug($"description index: {descriptionIndex}; portrait index: {portraitIndex}; random: {usePortraitDescription}; portrait name: {painCatPortrait.fileName}");

            painCatDescription = Arena.slugcatSelectPainCatDescriptions[descriptionIndex].Replace("<USERNAME>", OnlineManager.mePlayer.id.name);
            SwitchSelectedSlugcat(MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel);
        };
        slugcatSelectPage.subObjects.Add(randomizePainCat);

        MenuLabel chooseYourSlugcatLabel = new(this, slugcatSelectPage, "Choose Your Slugcat", new Vector2(680f, 575f), default, true);
        chooseYourSlugcatLabel.label.color = new Color(0.5f, 0.5f, 0.5f);
        slugcatSelectPage.subObjects.Add(chooseYourSlugcatLabel);

        slugcatNameLabel = new MenuLabel(this, slugcatSelectPage, Arena.slugcatSelectDisplayNames[Arena.arenaClientSettings.playingAs.value], new Vector2(680f, 310f), default, true);
        slugcatSelectPage.subObjects.Add(slugcatNameLabel);

        slugcatDescriptionLabel = new MenuLabel(this, slugcatSelectPage, Arena.slugcatSelectDescriptions[Arena.arenaClientSettings.playingAs.value], new Vector2(680f, 210f), default, bigText: true);
        slugcatDescriptionLabel.label.color = new Color(0.8f, 0.8f, 0.8f);
        slugcatSelectPage.subObjects.Add(slugcatDescriptionLabel);

        slugcatDescriptionGradients = new FSprite[4];
        slugcatDescriptionGradientsPos = new Vector2[4];

        for (int i = 0; i < slugcatDescriptionGradients.Length; i++)
        {
            slugcatDescriptionGradients[i] = new FSprite("LinearGradient200")
            {
                rotation = i % 2 == 0 ? 270f : 90f,
                scaleY = 2f,
                anchorX = 0.6f,
                anchorY = 0f,
            };
            slugcatDescriptionGradientsPos[i] = new Vector2(680f, i > 1 ? 280f : 125f);
            container.AddChild(slugcatDescriptionGradients[i]);
        }

        BuildPlayerDisplay();
        MatchmakingManager.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;
    }

    public void ChangeScene(MenuScene.SceneID sceneID)
    {
        slugcatScene = sceneID;
        pendingBgChange = false;

        if (scene.sceneID == sceneID) return;

        scene.ClearMenuObject(scene);
        scene = new InteractiveMenuScene(this, pages[0], sceneID);
        mainPage.subObjects.Add(scene);
        if (scene.depthIllustrations != null && scene.depthIllustrations.Count > 0)
        {
            int count = scene.depthIllustrations.Count;
            while (count-- > 0)
                scene.depthIllustrations[count].sprite.MoveToBack();
        }
        else
        {
            int count2 = scene.flatIllustrations.Count;
            while (count2-- > 0)
                scene.flatIllustrations[count2].sprite.MoveToBack();
        }
    }

    public void MovePage(Vector2 direction, int index)
    {
        if (pagesMoving) return;

        pagesMoving = true;
        pageMovementProgress = 0f;
        newPagePos = direction;
        oldPagesPos = new Vector2[pages.Count];
        for (int i = 0; i < oldPagesPos.Length; i++)
            oldPagesPos[i] = pages[i].pos;

        currentPage = index;
        pageFullyTransitioned = false;

        PlaySound(SoundID.MENU_Next_Slugcat);
    }

    public void SwitchSelectedSlugcat(SlugcatStats.Name slugcat)
    {
        if (!RainMeadow.isArenaMode(out _))
        {
            RainMeadow.Error("arena is null, slugcat wont be changed!");
            return;
        }
        slugcatScene = Arena.slugcatSelectMenuScenes[slugcat.value];
        Arena.arenaClientSettings.playingAs = slugcat;
        RainMeadow.Debug($"My Slugcat: {Arena.arenaClientSettings.playingAs}, in lobby list of client settings: {(GetArenaClientSettings(OnlineManager.mePlayer)?.playingAs?.value) ?? "NULL!"}");
        if (slugcat == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
        {
            slugcatDescriptionLabel.text = painCatDescription;
            slugcatNameLabel.text = painCatName;
            return;
        }

        slugcatDescriptionLabel.text = Arena.slugcatSelectDescriptions[slugcat.value];
        slugcatNameLabel.text = Arena.slugcatSelectDisplayNames[slugcat.value];
    }
    public override void ShutDownProcess()
    {
        manager.rainWorld.progression.SaveProgression(true, true);
        base.ShutDownProcess();
    }
    public override void Update()
    {
        if (currentPage == 1)
        {
            SmartMenuUpdateNoEscapeCheck();
            if (RWInput.CheckPauseButton(0)) MovePage(new Vector2(1500f, 0f), 0);
        }
        else if (pageFullyTransitioned) base.Update();
        else SmartMenuUpdateNoEscapeCheck();

        foreach (SelectOneButton button in slugcatSelectButtons)
            button.buttonBehav.greyedOut = pendingBgChange;

        lastDesiredBgCoverAlpha = desiredBgCoverAlpha;
        desiredBgCoverAlpha = Mathf.Clamp(desiredBgCoverAlpha + (pendingBgChange ? 0.01f : -0.01f), 0.8f, 1.1f);
        pendingBgChange = pendingBgChange || slugcatScene != scene.sceneID;

        if (pendingBgChange && menuDarkSprite.darkSprite.alpha >= 1) ChangeScene(slugcatScene);
        if (pagesMoving) UpdateMovingPage();
        UpdateOnlineUI();
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        menuDarkSprite.darkSprite.alpha = Mathf.Clamp(Mathf.Lerp(lastDesiredBgCoverAlpha, desiredBgCoverAlpha, timeStacker), 0.8f, 1.1f);
        if (tabContainer.activeIndex == 1)
            for (int i = 0; i < settingsDivSprites.Length; i++)
            {
                container.AddChild(settingsDivSprites[i]);
                var divSprite = settingsDivSprites[i];
                var divSpritePos = settingsDivSpritesPos[i];

                divSprite.x = mainPage.DrawX(timeStacker) + divSpritePos.x;
                divSprite.y = mainPage.DrawY(timeStacker) + divSpritePos.y;
            }
        else
            for (int i = 0; i < settingsDivSprites.Length; i++)
                container.RemoveChild(settingsDivSprites[i]);

        for (int i = 0; i < slugcatDescriptionGradients.Length; i++)
        {
            FSprite gradientSprite = slugcatDescriptionGradients[i];
            Vector2 gradientSpritePos = slugcatDescriptionGradientsPos[i];

            gradientSprite.x = slugcatSelectPage.DrawX(timeStacker) + gradientSpritePos.x;
            gradientSprite.y = slugcatSelectPage.DrawY(timeStacker) + gradientSpritePos.y;
        }
    }
    public void UpdateOnlineUI() //for future online ui stuff
    {
        if (!RainMeadow.isArenaMode(out _)) return;
        if (playerDisplayer == null) return;

        foreach (ButtonScroller.IPartOfButtonScroller button in playerDisplayer.buttons)
        {
            if (button is ArenaPlayerBox playerBox) 
                playerBox.slugcatButton.LoadNewSlugcat(GetArenaClientSettings(playerBox.profileIdentifier)?.playingAs, false, false);

            if (button is ArenaPlayerSmallBox smallPlayerBox)
                smallPlayerBox.slugcatButton.slug = GetArenaClientSettings(smallPlayerBox.profileIdentifier)?.playingAs;
        }

    }
    public void UpdateMovingPage()
    {
        pageMovementProgress += 0.35f;
        float baseMoveSpeed = Mathf.Lerp(8f, 125f, Custom.SCurve(pageMovementProgress, 0.85f));
        for (int i = 0; i < pages.Count; i++)
        {
            float totalTravelDistance = Vector2.Distance(oldPagesPos[i], oldPagesPos[i] + newPagePos);
            float distanceToTravel = Vector2.Distance(pages[i].pos, oldPagesPos[i] + newPagePos);
            float easing = Mathf.Lerp(1f, 0.01f, Mathf.InverseLerp(totalTravelDistance, 0.1f, distanceToTravel));

            pages[i].pos = Custom.MoveTowards(pages[i].pos, oldPagesPos[i] + newPagePos, baseMoveSpeed * easing);

            if (pages[i].pos == oldPagesPos[i] + newPagePos)
            {
                pagesMoving = false;
                pageFullyTransitioned = true;
            }
        }
    }
    public void BuildPlayerDisplay()
    {
        if (playerDisplayer != null) return;

        playerDisplayer = new(this, mainPage, new(960, 130), OnlineManager.players, GetPlayerButton, 4, ArenaPlayerBox.DefaultSize.x + 30, ArenaPlayerBox.DefaultSize.y, 0, ArenaPlayerSmallBox.DefaultSize.y, 10);
        mainPage.subObjects.Add(playerDisplayer);
        playerDisplayer.CallForRefresh();
    }
    public void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
    {
        if (!RainMeadow.isArenaMode(out _)) return;

        RainMeadow.DebugMe();
        BuildPlayerDisplay();
        playerDisplayer?.UpdatePlayerList(OnlineManager.players);
    }
    public ButtonScroller.IPartOfButtonScroller GetPlayerButton(PlayerDisplayer playerDisplay, bool isLargeDisplay, OnlinePlayer player, Vector2 pos)
    {
        if (isLargeDisplay)
        {
            ArenaPlayerBox playerBox = new(this, playerDisplay, player, OnlineManager.lobby?.isOwner == true, pos); //buttons init prevents kick button if isMe
            if (player.isMe)
            {
                playerBox.slugcatButton.OnClick += (_) =>
                {
                    MovePage(new Vector2(-1500f, 0f), 1);
                };
                playerBox.colorInfoButton.OnClick += (_) =>
                {
                    OpenColorConfig(playerBox.slugcatButton.slugcat);
                };
            }
            playerBox.slugcatButton.TryBind(playerDisplay.scrollSlider, true, false, false, false);
            return playerBox;
        }

        ArenaPlayerSmallBox playerSmallBox = new(this, playerDisplay, player, OnlineManager.lobby?.isOwner == true, pos);

        if (player.isMe)
        {
            playerSmallBox.slugcatButton.OnClick += _ => MovePage(new Vector2(-1500f, 0f), 1);
            playerSmallBox.colorKickButton!.OnClick += _ => OpenColorConfig(playerSmallBox.slugcatButton.slug);
        }

        playerSmallBox.playerButton.TryBind(playerDisplay.scrollSlider, true, false, false, false);
        return playerSmallBox;
    }
    public void OpenColorConfig(SlugcatStats.Name? slugcat)
    {
        if (slugcat == null) return;

        PlaySound(SoundID.MENU_Checkbox_Check);
        colorSlugcatDialog = new ColorSlugcatDialog(manager, slugcat, () => { });
        manager.ShowDialog(colorSlugcatDialog);
    }

    public int GetCurrentlySelectedOfSeries(string series)
    {
        return selectedSlugcatIndex; // no need to check series (for now) since there is only one SelectOneButton in this menu
    }

    public void SetCurrentlySelectedOfSeries(string series, int to)
    {
        selectedSlugcatIndex = to; // no need to check series (for now) since there is only one SelectOneButton in this menu
    }
    public ArenaClientSettings? GetArenaClientSettings(OnlinePlayer player)
    {
        if (OnlineManager.lobby == null)
        {
            RainMeadow.Error("Lobby is null!");
            return null;
        }
        return OnlineManager.lobby.clientSettings.TryGetValue(player, out ClientSettings settings) ? settings.GetData<ArenaClientSettings>() : null;
    }
}
