using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System;
using UnityEngine;

namespace MinimumLevel
{
    [BepInPlugin("com.atomic.minimumlevel", "Minimum Level", "1.0.0")]
    [BepInProcess("Among Us.exe")]
    public class MinimumLevelPlugin : BasePlugin
    {
        public static MinimumLevelPlugin Instance;
        public static ConfigEntry<int> MinimumLevel;
        public static ConfigEntry<int> MaximumLevel;
        public static ConfigEntry<bool> banInsteadOfKick;
        public static ConfigEntry<bool> customUiInGame;

        public Scroller scroller = null;
        public Transform sliderInner = null;
        public GameObject banInsteadOfKickSetting = null;
        public GameObject MinimumLevelSetting = null;
        public GameOptionButton IncreaseMinLevelButton = null;
        public GameOptionButton DecreaseMinLevelButton = null;
        public GameObject MaximumLevelSetting = null;
        public GameOptionButton IncreaseMaxLevelButton = null;
        public GameOptionButton DecreaseMaxLevelButton = null;
        public GameOptionButton BigIncreaseMinLevelButton = null;
        public GameOptionButton BigDecreaseMinLevelButton = null;
        public GameOptionButton BigIncreaseMaxLevelButton = null;
        public GameOptionButton BigDecreaseMaxLevelButton = null;

        public float inputHeldTime = 0f;
        public float nextRepeatTime = 0f;
        private const float SettingSpacing = -0.5f;

        public override void Load()
        {
            Instance = this;
            Harmony harmony = new Harmony("com.atomic.minimumlevel");
            harmony.PatchAll();
            MinimumLevel = Config.Bind("General", "MinimumLevel", 10, "The minimum player level required to join the game.");
            MaximumLevel = Config.Bind("General", "MaximumLevel", 300, "The maximum player level allowed to join the game.");
            banInsteadOfKick = Config.Bind("General", "BanInsteadOfKick", false, "Whether to ban players instead of kicking them if they don't meet level requirements.");
            customUiInGame = Config.Bind("General", "CustomUiInGame", true, "Whether to show custom UI in-game for the settings, this lets values be editted live.");
        }

        public void SyncSettingsUI()
        {
            if (sliderInner == null) return;
            if (banInsteadOfKickSetting != null)
            {
                banInsteadOfKickSetting.transform.Find("Toggle").transform.Find("Check").GetComponent<SpriteRenderer>().enabled = banInsteadOfKick.Value;
            }
            if (MinimumLevelSetting != null)
            {
                MinimumLevelSetting.transform.Find("Value_TMP").GetComponent<TMPro.TextMeshPro>().text = MinimumLevel.Value.ToString();
            }
            if (MaximumLevelSetting != null)
            {
                MaximumLevelSetting.transform.Find("Value_TMP").GetComponent<TMPro.TextMeshPro>().text = MaximumLevel.Value.ToString();
            }
        }

        public GameObject CreateBoolSetting(string name, string title, Vector3 localPos, GameObject boolTemplate, Transform parent, ConfigEntry<bool> config)
        {
            GameObject setting = GameObject.Instantiate(boolTemplate, parent);
            setting.name = name;
            setting.transform.position = boolTemplate.transform.position;
            setting.transform.localPosition = localPos;

            GameObject.Destroy(setting.GetComponent<ToggleOption>());
            GameObject.Destroy(setting.GetComponent<UIScrollbarHelper>());
            setting.transform.Find("Title Text").GetComponent<TMPro.TextMeshPro>().text = title;

            var PassiveButton = setting.transform.Find("Toggle").GetComponent<PassiveButton>();

            PassiveButton.OnClick.AddListener((Action)(() =>
            {
                config.Value = !config.Value;
                SyncSettingsUI();
            }));

            return setting;
        }

        public GameObject CreateNumberSetting(string name, string title, Vector3 localPos, GameObject numberTemplate, Transform parent, out GameOptionButton increaseButton, out GameOptionButton decreaseButton, out GameOptionButton bigIncreaseButton, out GameOptionButton bigDecreaseButton, int min, int max, Func<int> getter, Action<int> setter)
        {
            GameObject setting = GameObject.Instantiate(numberTemplate, parent);
            setting.name = name;
            setting.transform.position = numberTemplate.transform.position;
            setting.transform.localPosition = localPos;
            setting.transform.Find("Title Text").GetComponent<TMPro.TextMeshPro>().text = title;

            GameObject.Destroy(setting.GetComponent<NumberOption>());
            GameObject.Destroy(setting.GetComponent<UIScrollbarHelper>());

            decreaseButton = setting.transform.Find("MinusButton").GetComponent<GameOptionButton>();
            increaseButton = setting.transform.Find("PlusButton").GetComponent<GameOptionButton>();

            decreaseButton.transform.localPosition += new Vector3(0.6f, 0f, 0f);
            increaseButton.transform.localPosition += new Vector3(0.6f, 0f, 0f);


            setting.transform.Find("ValueBox").transform.localPosition += new Vector3(0.6f, 0f, 0f);
            setting.transform.Find("Value_TMP").transform.localPosition += new Vector3(0.6f, 0f, 0f);

            decreaseButton.OnClick.AddListener((Action)(() =>
            {
                int newVal = Math.Max(min, getter() - 1);
                setter(newVal);
                SyncSettingsUI();
            }));
            increaseButton.OnClick.AddListener((Action)(() =>
            {
                int newVal = Math.Min(max, getter() + 1);
                setter(newVal);
                SyncSettingsUI();
            }));

            bigDecreaseButton = GameObject.Instantiate(decreaseButton, setting.transform);
            bigDecreaseButton.name = "BigMinusButton";
            bigDecreaseButton.transform.localPosition = decreaseButton.transform.localPosition + new Vector3(-0.6f, 0f, 0f);
            bigDecreaseButton.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);

            bigIncreaseButton = GameObject.Instantiate(increaseButton, setting.transform);
            bigIncreaseButton.name = "BigPlusButton";
            bigIncreaseButton.transform.localPosition = increaseButton.transform.localPosition + new Vector3(0.6f, 0f, 0f);
            bigIncreaseButton.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);

            bigDecreaseButton.OnClick.AddListener((Action)(() =>
            {
                int newVal = Math.Max(min, getter() - 10);
                setter(newVal);
                SyncSettingsUI();
            }));

            bigIncreaseButton.OnClick.AddListener((Action)(() =>
            {
                int newVal = Math.Min(max, getter() + 10);
                setter(newVal);
                SyncSettingsUI();
            }));

            return setting;
        }

        public void CreateSettingsUI(GameObject GameOptionsMenu)
        {
            if (!customUiInGame.Value) { return; }

            scroller = GameOptionsMenu.transform.Find("Scroller").GetComponent<Scroller>();
            if (scroller == null)
            {
                Debug.LogError("[MinimumLevel] Failed to find Scroller component.");
                return;
            }

            sliderInner = scroller.transform.Find("SliderInner");
            if (sliderInner == null)
            {
                Debug.LogError("[MinimumLevel] Failed to find SliderInner transform.");
                return;
            }

            if (sliderInner.Find("Header_Joining")) { return; }
            scroller.ContentYBounds.max += 2.3f;

            var HeaderTemplate = sliderInner.Find("CategoryHeaderMasked(Clone)");
            if (HeaderTemplate == null)
            {
                Debug.LogError("[MinimumLevel] Failed to find CategoryHeaderMasked template, even with (Clone) suffix.");
                return;
            }

            var NewHeader = GameObject.Instantiate(HeaderTemplate.gameObject, sliderInner);
            NewHeader.name = "Header_Joining";

            NewHeader.transform.position = HeaderTemplate.position;
            NewHeader.transform.localPosition = new Vector3(-0.8828f, -8f - 1.5f);
            NewHeader.GetComponentInChildren<TMPro.TextMeshPro>().text = "Join requirements";

            var BoolSettingTemplate = sliderInner.Find("GameOption_Checkbox(Clone)");
            if (BoolSettingTemplate == null)
            {
                Debug.LogError("[MinimumLevel] Failed to find GameOption_Checkbox template.");
                return;
            }

            banInsteadOfKickSetting = CreateBoolSetting("Setting_BanInsteadOfKick", "Ban instead of kick", new Vector3(0.9228f, -10.1314f, -1.5f), BoolSettingTemplate.gameObject, sliderInner, banInsteadOfKick);

            var NumberSettingTemplate = sliderInner.Find("GameOption_Number(Clone)");
            if (NumberSettingTemplate == null)
            {
                Debug.LogError("[MinimumLevel] Failed to find GameOption_Number template.");
                return;
            }

            MinimumLevelSetting = CreateNumberSetting("Setting_MinimumLevel", "Minimum Level", banInsteadOfKickSetting.transform.localPosition + new Vector3(0f, SettingSpacing, 0f), NumberSettingTemplate.gameObject, sliderInner,
                out GameOptionButton incMin, out GameOptionButton decMin,
                out GameOptionButton bigIncMin, out GameOptionButton bigDecMin,
                0, 1000, () => MinimumLevel.Value, (v) => MinimumLevel.Value = v
            );

            IncreaseMinLevelButton = incMin;
            DecreaseMinLevelButton = decMin;
            BigIncreaseMinLevelButton = bigIncMin;
            BigDecreaseMinLevelButton = bigDecMin;

            MaximumLevelSetting = CreateNumberSetting("Setting_MaximumLevel", "Maximum Level", MinimumLevelSetting.transform.localPosition + new Vector3(0f, SettingSpacing, 0f), NumberSettingTemplate.gameObject, sliderInner,
                out GameOptionButton incMax, out GameOptionButton decMax,
                out GameOptionButton bigIncMax, out GameOptionButton bigDecMax,
                0, 1001, () => MaximumLevel.Value, (v) => MaximumLevel.Value = v
            );

            IncreaseMaxLevelButton = incMax;
            DecreaseMaxLevelButton = decMax;
            BigIncreaseMaxLevelButton = bigIncMax;
            BigDecreaseMaxLevelButton = bigDecMax;

            SyncSettingsUI();
        }
    }

    [HarmonyPatch]
    public static class MinimumLevelPatches
    {

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.OnEnable))]
        public static void GameOptionsMenu_Awake_Postfix(GameOptionsMenu __instance)
        {
            if (!MinimumLevelPlugin.customUiInGame.Value) { return; }

            MinimumLevelPlugin.Instance.CreateSettingsUI(__instance.gameObject);
            MinimumLevelPlugin.Instance.SyncSettingsUI();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.UpdateLevel))]
        public static void UpdateLevel_Postfix(NetworkedPlayerInfo __instance)
        {
            if (__instance.ClientId == AmongUsClient.Instance.ClientId) { return; }

            int level = (int)__instance.PlayerLevel + 1;
            string playerName = __instance.PlayerName;

            if (GameData.Instance.GetHost() == PlayerControl.LocalPlayer.Data)
            {
                Debug.Log($"{playerName} has joined and we are host.");

                if (level < MinimumLevelPlugin.MinimumLevel.Value || level > MinimumLevelPlugin.MaximumLevel.Value)
                {
                    Debug.Log($"Kicking player {playerName} for not meeting level requirements. Level: {level}");
                    AmongUsClient.Instance.KickPlayer(__instance.ClientId, MinimumLevelPlugin.banInsteadOfKick.Value);
                }
                else
                {
                    Debug.Log($"Player {playerName} meets level requirements. Level: {level}");
                }
            }
            else
            {
                Debug.Log($"We are not host, not checking level for {playerName}.");
                Debug.Log($"{GameData.Instance.GetHost()} is the host, and we are {PlayerControl.LocalPlayer.Data}");
            }
        }
    }
}