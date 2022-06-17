using System;
using BepInEx;
using BepInEx.Configuration;
using UnboundLib;
using HarmonyLib;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnboundLib.Utils.UI;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnboundLib.GameModes;
using System.Linq;
using Photon.Pun;
using UnboundLib.Networking;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnboundLib.Utils;
using UnboundLib.Cards;
using UnityEngine.Events;
using RarityLib.Utils;

namespace DeckCustomization
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.moddingutils", BepInDependency.DependencyFlags.HardDependency)] // utilities for cards and cardbars
    [BepInDependency("pykess.rounds.plugins.cardchoicespawnuniquecardpatch", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("root.rarity.lib", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ModId, ModName, "0.2.2")]
    [BepInProcess("Rounds.exe")]
    public class DeckCustomization : BaseUnityPlugin
    {
        private const string ModId = "pykess.rounds.plugins.deckcustomization";
        private const string ModName = "Deck Customization";
        private const string CompatibilityModName = "DeckCustomization";

        internal const string defaultCardsName = "__default__";

        private static ConfigEntry<bool> DisplayRaritiesConfig;
        internal static bool DisplayRarities;
        private static ConfigEntry<bool> DisplayPercConfig;
        internal static bool DisplayPerc;
        //private static ConfigEntry<bool> BetterMethodConfig;
        //internal static bool BetterMethod;

        internal const bool BetterMethod = true; // always use bettermethod

        // for menus
        private static Dictionary<CardInfo.Rarity, TextMeshProUGUI> raritytxt = new Dictionary<CardInfo.Rarity, TextMeshProUGUI>();
        private static Dictionary<CardInfo.Rarity, Slider> raritySliders = new Dictionary<CardInfo.Rarity,Slider>();
        private static Dictionary<CardThemeColor.CardThemeColorType, TextMeshProUGUI> themetxts = new Dictionary<CardThemeColor.CardThemeColorType, TextMeshProUGUI>() { };
        private static Dictionary<CardThemeColor.CardThemeColorType, Slider> themesliders = new Dictionary<CardThemeColor.CardThemeColorType, Slider>() { };
        private static Dictionary<string, TextMeshProUGUI> modtxts = new Dictionary<string, TextMeshProUGUI>() { };
        private static Dictionary<string, Slider> modsliders = new Dictionary<string, Slider>() { };

        private static Dictionary<string, ConfigEntry<float>> ModRaritiesConfig = new Dictionary<string, ConfigEntry<float>>() { };
        internal static Dictionary<string, float> ModRarities = new Dictionary<string, float>() { };
        private static Dictionary<CardInfo.Rarity, ConfigEntry<float>> RarityRaritiesConfig = new Dictionary<CardInfo.Rarity, ConfigEntry<float>>() { };
        internal static Dictionary<CardInfo.Rarity, float> RarityRarities = new Dictionary<CardInfo.Rarity, float>() { };
        private static Dictionary<CardThemeColor.CardThemeColorType, ConfigEntry<float>> ThemeRaritiesConfig = new Dictionary<CardThemeColor.CardThemeColorType, ConfigEntry<float>>() { };
        internal static Dictionary<CardThemeColor.CardThemeColorType, float> ThemeRarities = new Dictionary<CardThemeColor.CardThemeColorType, float>() { };
        private static float MaxCardRarity = 0;

        internal static List<CardInfo> defaultCards
        {
            get
            {
                return ((CardInfo[])typeof(CardManager).GetField("defaultCards", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).ToList();
            }
        }
        internal static List<CardInfo> activeCards
        {
            get
            {
                return ((ObservableCollection<CardInfo>)typeof(CardManager).GetField("activeCards", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).ToList();
            }
        }
        internal static List<CardInfo> inactiveCards
        {
            get
            {
                return (List<CardInfo>)typeof(CardManager).GetField("inactiveCards", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            }
            set { }
        }
        internal static List<CardInfo> allCards
        {
            get
            {
                return activeCards.Concat(inactiveCards).ToList();
            }
            set { }
        }

        private void Awake()
        {
            //BetterMethodConfig = Config.Bind(CompatibilityModName, "BetterMethod", true, "Use a more efficient method of drawing a random card from the deck - should produce identical results to the vanilla game.");
            DisplayRaritiesConfig = Config.Bind(CompatibilityModName, "DisplayRarities", false, "Add text to cards to indicate their rarity (colorblind accessibility option)");
            DisplayPercConfig = Config.Bind(CompatibilityModName, "DisplayPercentages", false, "Add text to cards to indicate their rarity as a percentage.");
            // apply patches
            new Harmony(ModId).PatchAll();
        }
        private void Start()
        {
            // fix default card themes
            FixThemes.FixCardThemes(allCards.ToArray());

            // load settings to prevent them from being orphaned
            //BetterMethod = BetterMethodConfig.Value;
            DisplayRarities = DisplayRaritiesConfig.Value;
            DisplayPerc = DisplayPercConfig.Value;
            Unbound.Instance.ExecuteAfterSeconds(0.5f, () =>
            {
                // load/create settings for each mod, rarity, and cardtheme
                string mod = defaultCardsName;
                foreach (CardInfo card in allCards)
                {
                    if (card.gameObject.GetComponent<CustomCard>() != null)
                    {
                        mod = card.gameObject.GetComponent<CustomCard>().GetModName().ToLower();
                    }
                    else
                    {
                        mod = defaultCardsName;
                    }

                    // mod
                    ModRaritiesConfig[mod] = Config.Bind(CompatibilityModName, mod, RarityUtils.defaultGeneralRarity, "Relative rarity of " + mod + " cards on a scale of 0 (disabled) to 1 (common)");

                }

                // cardtheme
                foreach (CardThemeColor.CardThemeColorType theme in allCards.Select(c => c.colorTheme))
                {
                    ThemeRaritiesConfig[theme] = Config.Bind(CompatibilityModName, theme.ToString(), RarityUtils.defaultGeneralRarity, $"Relative rarity of {theme} cards on a scale of 0 (disabled) to 1 (common)");
                }

                // rarity
                foreach (CardInfo.Rarity r in allCards.Select(c => c.rarity))
                {
                    Rarity rarity = RarityLib.Utils.RarityUtils.GetRarityData(r);
                    RarityRaritiesConfig[rarity.value] = Config.Bind(CompatibilityModName, rarity.name, rarity.relativeRarity, $"Relative rarity of {rarity.name} cards on a scale of 0 (disabled) to 1 (common)");
                    MaxCardRarity = Mathf.Max(MaxCardRarity, rarity.relativeRarity); //find the most common rarity to use as the max value for the rarity sliders
                }

                foreach (CardInfo card in allCards)
                {
                    if (card.gameObject.GetComponent<CustomCard>() != null)
                    {
                        mod = card.gameObject.GetComponent<CustomCard>().GetModName().ToLower();
                    }
                    else
                    {
                        mod = defaultCardsName;
                    }

                    // mod
                    ModRarities[mod] = ModRaritiesConfig[mod].Value;

                }

                // rarity
                foreach(CardInfo.Rarity r in RarityRaritiesConfig.Keys)
                    RarityRarities[r] = RarityRaritiesConfig[r].Value;

                // cardtheme
                foreach (CardThemeColor.CardThemeColorType theme in ThemeRaritiesConfig.Keys)
                    ThemeRarities[theme] = ThemeRaritiesConfig[theme].Value;

            });

            // add credits
            Unbound.RegisterCredits(ModName, new string[] { "Pykess", "Root (RarityLib and CardThemeLib support)"}, new string[] { "github", "Support Pykess" }, new string[] { "https://github.com/pdcook/DeckCustomization", "https://ko-fi.com/pykess" });
             
            // add GUI to modoptions menu
            Unbound.RegisterMenu(ModName, () => { }, (menu) => Unbound.Instance.StartCoroutine(SetupGUI(menu)), null, false);

            // handshake to sync settings
            Unbound.RegisterHandshake(DeckCustomization.ModId, this.OnHandShakeCompleted);

            // Hook to tell RarityLib about the ajusted rarities
            GameModeManager.AddHook(GameModeHooks.HookGameStart, gm => RarityUtils.UpdateRarities());
        }
        private void OnHandShakeCompleted()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                // sync twice just to be safe
                for (int i = 0; i<2; i++)
                {
                    NetworkingManager.RPC_Others(typeof(DeckCustomization), nameof(SyncSettings), new object[] { BetterMethod, ModRarities.Keys.ToArray(), ModRarities.Values.ToArray(), RarityRarities.Keys.Select(k => (byte)k).ToArray(), RarityRarities.Values.ToArray(), ThemeRarities.Keys.Select(k=>(byte)k).ToArray(), ThemeRarities.Values.ToArray() });
                }
            }
        }
        [UnboundRPC]
        private static void SyncSettings(bool better, string[] mods, float[] modrarities, byte[] rarities, float[] rarityrarities, byte[] themes, float[] themerarities)
        {
            //BetterMethod = better;

            for (int i = 0; i<mods.Length; i++)
            {
                ModRarities[mods[i]] = modrarities[i];
            }
            for (int i = 0; i<rarities.Length; i++)
            {
                RarityRarities[(CardInfo.Rarity)rarities[i]] = rarityrarities[i];
            }
            for (int i = 0; i<themes.Length; i++)
            {
                ThemeRarities[(CardThemeColor.CardThemeColorType)themes[i]] = themerarities[i];
            }

        }
        
        private static GameObject CreateSliderWithoutInput(string text, GameObject parent, int fontSize, float minValue, float maxValue, float defaultValue,
            UnityAction<float> onValueChangedAction, out Slider slider, bool wholeNumbers = false, Color? sliderColor = null, Slider.Direction direction = Slider.Direction.LeftToRight, bool forceUpper = true, Color? color = null, TMP_FontAsset font = null, Material fontMaterial = null, TextAlignmentOptions? alignmentOptions = null)
        {
            GameObject sliderObj = MenuHandler.CreateSlider(text, parent, fontSize, minValue, maxValue, defaultValue, onValueChangedAction, out slider, wholeNumbers, sliderColor, direction, forceUpper, color, font, fontMaterial, alignmentOptions);

            UnityEngine.GameObject.Destroy(sliderObj.GetComponentInChildren<TMP_InputField>().gameObject);

            return sliderObj;
        
        }
        private static void UpdatePercs()
        {

            foreach (CardInfo.Rarity rarity in raritytxt.Keys)
            {
                raritytxt[rarity].text = String.Format("{0:0.##}", RarityUtils.GetRarityAsPerc(rarity) * 100f) + $"% {rarity.ToString().ToUpper()}";
                raritytxt[rarity].color = RarityUtils.RarityColorLerp(RarityUtils.GetRarityAsPerc(rarity));
            }

            foreach (CardThemeColor.CardThemeColorType theme in themetxts.Keys.ToArray())
            {
                themetxts[theme].text = String.Format("{0:0.##}", RarityUtils.GetRarityAsPerc(theme) * 100f) + "% " + RarityUtils.GetThemeAsString(theme).ToUpper();
                if (RarityUtils.GetRarityAsPerc(theme) == 0f)
                {
                    themetxts[theme].color = Color.grey;
                }
                else
                {
                    themetxts[theme].color = RarityUtils.GetThemeColor(theme);
                }
            }
            foreach (string mod in modtxts.Keys.ToArray())
            {
                modtxts[mod].text = String.Format("{0:0.##}", RarityUtils.GetRarityAsPerc(mod) * 100f) + "% " + (mod == defaultCardsName ? "default" : mod).ToUpper();
                if (RarityUtils.GetRarityAsPerc(mod) == 0f)
                {
                    modtxts[mod].color = Color.grey;
                }
                else
                {
                    modtxts[mod].color = Color.white;
                }
            }
        }

        private IEnumerator SetupGUI(GameObject menu)
        {
            yield return new WaitUntil(() => RarityRarities.Keys.Count > 0 && RarityRaritiesConfig.Keys.Count == RarityRarities.Keys.Count);
            yield return new WaitForSecondsRealtime(0.1f);
            NewGUI(menu);
            yield break;
        }

        private void NewGUI(GameObject menu)
        {
            MenuHandler.CreateText(ModName + " Options", menu, out TextMeshProUGUI _, 60);
            //void algChanged(bool val)
            //{
            //BetterMethod = val;
            //BetterMethodConfig.Value = val;
            //}
            void displaytext(bool val)
            {
                DisplayRarities = val;
                DisplayRaritiesConfig.Value = val;
            }
            void displayperc(bool val)
            {
                DisplayPerc = val;
                DisplayPercConfig.Value = val;
            }
            //MenuHandler.CreateToggle(BetterMethodConfig.Value, "Use More Efficient Card Draw Algorithm", menu, algChanged, 30);
            MenuHandler.CreateToggle(DisplayRaritiesConfig.Value, "Display Card Rarities as Text", menu, displaytext, 30);
            MenuHandler.CreateToggle(DisplayPercConfig.Value, "Display Card Rarities as Percentages", menu, displayperc, 30);

            void UpdateRarity(float val, CardInfo.Rarity rarity)
            {
                RarityRarities[rarity] = val / 100f;
                RarityRaritiesConfig[rarity].Value = val / 100f;
                RarityLib.Utils.RarityUtils.GetRarityData(rarity).calculatedRarity = val / 100f;
                if (RarityUtils.rarity_Z == 0f)
                {
                    val += 1f;
                    RarityRarities[rarity] = val / 100f;
                    RarityRaritiesConfig[rarity].Value = val / 100f;
                }
                UpdatePercs();
            }

            List<CardInfo.Rarity> rarities = RarityRarities.Keys.ToList();
            rarities.Sort((r1, r2) => RarityLib.Utils.RarityUtils.GetRarityData(r2).relativeRarity.CompareTo(RarityLib.Utils.RarityUtils.GetRarityData(r1).relativeRarity));
            foreach (CardInfo.Rarity rarity in rarities)
            {
                raritytxt[rarity] = CreateSliderWithoutInput(String.Format("{0:0.##}", RarityUtils.GetRarityAsPerc(rarity) * 100f) + $"% {rarity}", menu, 30, 0f, 100f * MaxCardRarity, 100 * RarityRarities[rarity], val => UpdateRarity(val, rarity), out Slider slider, true).GetComponentsInChildren<TextMeshProUGUI>()[2];
                raritySliders[rarity] = slider;
            }

            void ResetRarities()
            {
                foreach (CardInfo.Rarity rarity in RarityRarities.Keys.ToArray())
                {
                    RarityRarities[rarity] = RarityLib.Utils.RarityUtils.GetRarityData(rarity).relativeRarity;
                    RarityRaritiesConfig[rarity].Value = RarityLib.Utils.RarityUtils.GetRarityData(rarity).relativeRarity;
                    UpdateRarity(100 * RarityLib.Utils.RarityUtils.GetRarityData(rarity).relativeRarity, rarity);
                    raritySliders[rarity].value = 100 * RarityLib.Utils.RarityUtils.GetRarityData(rarity).relativeRarity;
                }
                UpdatePercs();
            }
            MenuHandler.CreateButton("Reset Rarities", menu, ResetRarities, 30);

            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);

            GameObject themeMenu = MenuHandler.CreateMenu("Card Theme Rarities", () => { }, menu, 60, true, true, menu.transform.parent.gameObject);
            ThemeRarityGUI(themeMenu);
            GameObject modMenu = MenuHandler.CreateMenu("Card Pack Rarities", () => { }, menu, 60, true, true, menu.transform.parent.gameObject);
            Unbound.Instance.StartCoroutine(SetupModRarityGUI(modMenu));
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            void ResetALL()
            {
                //BetterMethod = false;
                //BetterMethodConfig.Value = false; 
                DisplayRarities = false;
                DisplayRaritiesConfig.Value = false;
                DisplayPerc = false;
                DisplayPercConfig.Value = false;

                for (int i = 0; i < ModRarities.Keys.Count; i++)
                {
                    string mod = ModRarities.Keys.ToArray()[i];
                    ModRarities[mod] = RarityUtils.defaultGeneralRarity;
                    ModRaritiesConfig[mod].Value = RarityUtils.defaultGeneralRarity;

                }
                foreach (string mod in modtxts.Keys.ToArray())
                {
                    modsliders[mod].value = 100 * RarityUtils.defaultGeneralRarity;
                }

                foreach (CardInfo.Rarity rarity in RarityRarities.Keys.ToArray())
                {
                    RarityRarities[rarity] = RarityLib.Utils.RarityUtils.GetRarityData(rarity).relativeRarity;
                    RarityRaritiesConfig[rarity].Value = RarityLib.Utils.RarityUtils.GetRarityData(rarity).relativeRarity;
                    UpdateRarity(100 * RarityLib.Utils.RarityUtils.GetRarityData(rarity).relativeRarity, rarity);
                    raritySliders[rarity].value = 100 * RarityLib.Utils.RarityUtils.GetRarityData(rarity).relativeRarity;
                }

                for (int i = 0; i < ThemeRarities.Keys.Count; i++)
                {
                    CardThemeColor.CardThemeColorType theme = ThemeRarities.Keys.ToArray()[i];
                    ThemeRarities[theme] = RarityUtils.defaultGeneralRarity;
                    ThemeRaritiesConfig[theme].Value = RarityUtils.defaultGeneralRarity;
                }
                foreach (CardThemeColor.CardThemeColorType theme in themetxts.Keys.ToArray())
                {
                    themesliders[theme].value = 100 * RarityUtils.defaultGeneralRarity;
                }
                UpdatePercs();
            }

            MenuHandler.CreateButton("Reset <color=#FF0000><b>All</b></color> To Default", menu, ResetALL, 30);

            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);

            UpdatePercs();
        }
        private void ThemeRarityGUI(GameObject menu)
        {

            foreach (CardThemeColor.CardThemeColorType theme in Enum.GetValues(typeof(CardThemeColor.CardThemeColorType)))
            {
                themetxts[theme] = new TextMeshProUGUI();
            }

            void UpdateTheme(float val, CardThemeColor.CardThemeColorType theme)
            {
                ThemeRarities[theme] = val / 100f;
                ThemeRaritiesConfig[theme].Value = val / 100f;
                if (RarityUtils.theme_Z == 0f)
                {
                    val += 1f;
                    ThemeRarities[theme] = val / 100f;
                    ThemeRaritiesConfig[theme].Value = val / 100f;
                }
                UpdatePercs();
            }
            foreach (CardThemeColor.CardThemeColorType theme in Enum.GetValues(typeof(CardThemeColor.CardThemeColorType)))
            {
                themetxts[theme] = CreateSliderWithoutInput(String.Format("{0:0.##}", RarityUtils.GetRarityAsPerc(theme) * 100f) + "% "+RarityUtils.GetThemeAsString(theme).ToUpper(), menu, 30, 0f, 100f, 100 * ThemeRarities[theme], val => UpdateTheme(val, theme), out Slider temp, true).GetComponentsInChildren<TextMeshProUGUI>()[2];
                themesliders[theme] = temp;
            }
            void ResetRarities()
            {
                for (int i = 0; i < ThemeRarities.Keys.Count; i++)
                {
                    CardThemeColor.CardThemeColorType theme = ThemeRarities.Keys.ToArray()[i];
                    ThemeRarities[theme] = RarityUtils.defaultGeneralRarity;
                    ThemeRaritiesConfig[theme].Value = RarityUtils.defaultGeneralRarity;
                }
                foreach (CardThemeColor.CardThemeColorType theme in themetxts.Keys.ToArray())
                {
                    themesliders[theme].value = 100 * RarityUtils.defaultGeneralRarity;
                }
                UpdatePercs();
            }
            MenuHandler.CreateButton("Reset Rarities", menu, ResetRarities, 30);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
        }
        private IEnumerator SetupModRarityGUI(GameObject menu)
        {
            yield return new WaitUntil(()=>ModRarities.Keys.Count > 0);
            yield return new WaitForSecondsRealtime(0.1f);
            ModRarityGUI(menu);
            yield break;
        }
        private void ModRarityGUI(GameObject menu)
        {

            foreach (string mod in ModRarities.Keys.ToArray())
            {
                modtxts[mod] = new TextMeshProUGUI();
            }

            void UpdateMod(float val, string mod)
            {
                ModRarities[mod] = val / 100f;
                ModRaritiesConfig[mod].Value = val / 100f;
                if (RarityUtils.mod_Z == 0f)
                {
                    val += 1f;
                    ModRarities[mod] = val / 100f;
                    ModRaritiesConfig[mod].Value = val / 100f;
                }
                UpdatePercs();
            }
            foreach (string mod in ModRarities.Keys.ToArray())
            {
                modtxts[mod] = CreateSliderWithoutInput(String.Format("{0:0.##}", RarityUtils.GetRarityAsPerc(mod) * 100f) + "% " + (mod == defaultCardsName ? "default" : mod).ToUpper(), menu, 30, 0f, 100f, 100 * ModRarities[mod], val => UpdateMod(val, mod), out Slider temp, true).GetComponentsInChildren<TextMeshProUGUI>()[2];
                modsliders[mod] = temp;
            }

            void ResetRarities()
            {
                for (int i = 0; i < ModRarities.Keys.Count; i++)
                {
                    string mod = ModRarities.Keys.ToArray()[i];
                    ModRarities[mod] = RarityUtils.defaultGeneralRarity;
                    ModRaritiesConfig[mod].Value = RarityUtils.defaultGeneralRarity;

                }
                foreach (string mod in modtxts.Keys.ToArray())
                {
                    modsliders[mod].value = 100 * RarityUtils.defaultGeneralRarity;
                }
                UpdatePercs();
            }
            MenuHandler.CreateButton("Reset Rarities", menu, ResetRarities, 30);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
        }
    }
    
}
