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

namespace DeckCustomization
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.moddingutils", BepInDependency.DependencyFlags.HardDependency)] // utilities for cards and cardbars
    [BepInPlugin(ModId, ModName, "0.0.0")]
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
        private static ConfigEntry<bool> BetterMethodConfig;
        internal static bool BetterMethod;

        // for menus
        private static TextMeshProUGUI commontxt = new TextMeshProUGUI(), uncommontxt = new TextMeshProUGUI(), raretxt = new TextMeshProUGUI();
        private static Slider commonSlider, uncommonSlider, rareSlider;
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

        private void Awake()
        {
            BetterMethodConfig = Config.Bind(CompatibilityModName, "BetterMethod", false, "Use a more efficient method of drawing a random card from the deck - should produce identical results to the vanilla game.");
            DisplayRaritiesConfig = Config.Bind(CompatibilityModName, "DisplayRarities", false, "Add text to cards to indicate their rarity (colorblind accessibility option)");
            DisplayPercConfig = Config.Bind(CompatibilityModName, "DisplayPercentages", false, "Add text to cards to indicate their rarity as a percentage.");
            // apply patches
            new Harmony(ModId).PatchAll();
        }
        private void Start()
        {
            // load settings to prevent them from being orphaned
            BetterMethod = BetterMethodConfig.Value;
            DisplayRarities = DisplayRaritiesConfig.Value;
            DisplayPerc = DisplayPercConfig.Value;
            Unbound.Instance.ExecuteAfterSeconds(0.5f, () =>
            {
                // load/create settings for each mod, rarity, and cardtheme
                string mod = defaultCardsName;
                foreach (CardInfo card in activeCards)
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


                foreach (CardInfo card in activeCards)
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

            });

            // rarity
            RarityRaritiesConfig[CardInfo.Rarity.Common] = Config.Bind(CompatibilityModName, "Common", RarityUtils.defaultCommon, "Relative rarity of Common cards on a scale of 0 (disabled) to 1 (common)");
            RarityRaritiesConfig[CardInfo.Rarity.Uncommon] = Config.Bind(CompatibilityModName, "Uncommon", RarityUtils.defaultUncommon, "Relative rarity of Uncommon cards on a scale of 0 (disabled) to 1 (common)");
            RarityRaritiesConfig[CardInfo.Rarity.Rare] = Config.Bind(CompatibilityModName, "Rare", RarityUtils.defaultRare, "Relative rarity of Rare cards on a scale of 0 (disabled) to 1 (common)");

            // cardtheme
            ThemeRaritiesConfig[CardThemeColor.CardThemeColorType.ColdBlue] = Config.Bind(CompatibilityModName, "ColdBlue", RarityUtils.defaultGeneralRarity, "Relative rarity of ColdBlue cards on a scale of 0 (disabled) to 1 (common)");
            ThemeRaritiesConfig[CardThemeColor.CardThemeColorType.DefensiveBlue] = Config.Bind(CompatibilityModName, "DefensiveBlue", RarityUtils.defaultGeneralRarity, "Relative rarity of DefensiveBlue cards on a scale of 0 (disabled) to 1 (common)");
            ThemeRaritiesConfig[CardThemeColor.CardThemeColorType.DestructiveRed] = Config.Bind(CompatibilityModName, "DestructiveRed", RarityUtils.defaultGeneralRarity, "Relative rarity of DestructiveRed cards on a scale of 0 (disabled) to 1 (common)");
            ThemeRaritiesConfig[CardThemeColor.CardThemeColorType.EvilPurple] = Config.Bind(CompatibilityModName, "EvilPurple", RarityUtils.defaultGeneralRarity, "Relative rarity of EvilPurple cards on a scale of 0 (disabled) to 1 (common)");
            ThemeRaritiesConfig[CardThemeColor.CardThemeColorType.FirepowerYellow] = Config.Bind(CompatibilityModName, "FirepowerYellow", RarityUtils.defaultGeneralRarity, "Relative rarity of FirepowerYellow cards on a scale of 0 (disabled) to 1 (common)");
            ThemeRaritiesConfig[CardThemeColor.CardThemeColorType.MagicPink] = Config.Bind(CompatibilityModName, "MagicPink", RarityUtils.defaultGeneralRarity, "Relative rarity of MagicPink cards on a scale of 0 (disabled) to 1 (common)");
            ThemeRaritiesConfig[CardThemeColor.CardThemeColorType.NatureBrown] = Config.Bind(CompatibilityModName, "NatureBrown", RarityUtils.defaultGeneralRarity, "Relative rarity of NatureBrown cards on a scale of 0 (disabled) to 1 (common)");
            ThemeRaritiesConfig[CardThemeColor.CardThemeColorType.PoisonGreen] = Config.Bind(CompatibilityModName, "PoisonGreen", RarityUtils.defaultGeneralRarity, "Relative rarity of PoisonGreen cards on a scale of 0 (disabled) to 1 (common)");
            ThemeRaritiesConfig[CardThemeColor.CardThemeColorType.TechWhite] = Config.Bind(CompatibilityModName, "TechWhite", RarityUtils.defaultGeneralRarity, "Relative rarity of TechWhite cards on a scale of 0 (disabled) to 1 (common)");


            // rarity
            RarityRarities[CardInfo.Rarity.Common] = RarityRaritiesConfig[CardInfo.Rarity.Common].Value;
            RarityRarities[CardInfo.Rarity.Uncommon] = RarityRaritiesConfig[CardInfo.Rarity.Uncommon].Value;
            RarityRarities[CardInfo.Rarity.Rare] = RarityRaritiesConfig[CardInfo.Rarity.Rare].Value;

            // cardtheme
            ThemeRarities[CardThemeColor.CardThemeColorType.ColdBlue] = ThemeRaritiesConfig[CardThemeColor.CardThemeColorType.ColdBlue].Value;
            ThemeRarities[CardThemeColor.CardThemeColorType.DefensiveBlue] = ThemeRaritiesConfig[CardThemeColor.CardThemeColorType.DefensiveBlue].Value;
            ThemeRarities[CardThemeColor.CardThemeColorType.DestructiveRed] = ThemeRaritiesConfig[CardThemeColor.CardThemeColorType.DestructiveRed].Value;
            ThemeRarities[CardThemeColor.CardThemeColorType.EvilPurple] = ThemeRaritiesConfig[CardThemeColor.CardThemeColorType.EvilPurple].Value;
            ThemeRarities[CardThemeColor.CardThemeColorType.FirepowerYellow] = ThemeRaritiesConfig[CardThemeColor.CardThemeColorType.FirepowerYellow].Value;
            ThemeRarities[CardThemeColor.CardThemeColorType.MagicPink] = ThemeRaritiesConfig[CardThemeColor.CardThemeColorType.MagicPink].Value;
            ThemeRarities[CardThemeColor.CardThemeColorType.NatureBrown] = ThemeRaritiesConfig[CardThemeColor.CardThemeColorType.NatureBrown].Value;
            ThemeRarities[CardThemeColor.CardThemeColorType.PoisonGreen] = ThemeRaritiesConfig[CardThemeColor.CardThemeColorType.PoisonGreen].Value;
            ThemeRarities[CardThemeColor.CardThemeColorType.TechWhite] = ThemeRaritiesConfig[CardThemeColor.CardThemeColorType.TechWhite].Value;


            // add credits
            Unbound.RegisterCredits(ModName, new string[] { "Pykess" }, new string[] { "github", "Buy me a coffee" }, new string[] { "https://github.com/pdcook/DeckCustomization", "https://www.buymeacoffee.com/Pykess" });

            // add GUI to modoptions menu
            Unbound.RegisterMenu(ModName, () => { }, this.NewGUI, null, false);

            // handshake to sync settings
            Unbound.RegisterHandshake(DeckCustomization.ModId, this.OnHandShakeCompleted);
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
            BetterMethod = better;

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
            commontxt.text = String.Format("{0:0.##}", RarityUtils.GetRarityAsPerc(CardInfo.Rarity.Common) * 100f) + "% COMMON";
            commontxt.color = RarityUtils.RarityColorLerp(RarityUtils.GetRarityAsPerc(CardInfo.Rarity.Common));
            uncommontxt.text = String.Format("{0:0.##}", RarityUtils.GetRarityAsPerc(CardInfo.Rarity.Uncommon) * 100f) + "% UNCOMMON";
            uncommontxt.color = RarityUtils.RarityColorLerp(RarityUtils.GetRarityAsPerc(CardInfo.Rarity.Uncommon));
            raretxt.text = String.Format("{0:0.##}", RarityUtils.GetRarityAsPerc(CardInfo.Rarity.Rare) * 100f) + "% RARE";
            raretxt.color = RarityUtils.RarityColorLerp(RarityUtils.GetRarityAsPerc(CardInfo.Rarity.Rare));

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
        private void NewGUI(GameObject menu)
        {
            MenuHandler.CreateText(ModName + " Options", menu, out TextMeshProUGUI _, 60);
            void algChanged(bool val)
            {
                BetterMethod = val;
                BetterMethodConfig.Value = val;
            }
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
            MenuHandler.CreateToggle(BetterMethodConfig.Value, "Use More Efficient Card Draw Algorithm", menu, algChanged, 30);
            MenuHandler.CreateToggle(DisplayRaritiesConfig.Value, "Display Card Rarities as Text", menu, displaytext, 30);
            MenuHandler.CreateToggle(DisplayPercConfig.Value, "Display Card Rarities as Percentages", menu, displayperc, 30);

            void UpdateRarity(float val, CardInfo.Rarity rarity)
            {
                RarityRarities[rarity] = val / 100f;
                RarityRaritiesConfig[rarity].Value = val / 100f;
                if (RarityUtils.rarity_Z == 0f)
                {
                    val += 1f;
                    RarityRarities[rarity] = val / 100f;
                    RarityRaritiesConfig[rarity].Value = val / 100f;
                }
                UpdatePercs();
            }
            commontxt = CreateSliderWithoutInput(String.Format("{0:0.##}", RarityUtils.GetRarityAsPerc(CardInfo.Rarity.Common) * 100f) + "% Common", menu, 30, 0f, 100f, 100 * RarityRarities[CardInfo.Rarity.Common], val => UpdateRarity(val, CardInfo.Rarity.Common), out commonSlider, true).GetComponentsInChildren<TextMeshProUGUI>()[2];
            uncommontxt = CreateSliderWithoutInput(String.Format("{0:0.##}", RarityUtils.GetRarityAsPerc(CardInfo.Rarity.Uncommon) * 100f) + "% Uncommon", menu, 30, 0f, 100f, 100 * RarityRarities[CardInfo.Rarity.Uncommon], val => UpdateRarity(val, CardInfo.Rarity.Uncommon), out uncommonSlider, true).GetComponentsInChildren<TextMeshProUGUI>()[2];
            raretxt = CreateSliderWithoutInput(String.Format("{0:0.##}", RarityUtils.GetRarityAsPerc(CardInfo.Rarity.Rare) * 100f) + "% Rare", menu, 30, 0f, 100f, 100 * RarityRarities[CardInfo.Rarity.Rare], val => UpdateRarity(val, CardInfo.Rarity.Rare), out rareSlider, true).GetComponentsInChildren<TextMeshProUGUI>()[2];
            void ResetRarities()
            {
                RarityRarities[CardInfo.Rarity.Common] = RarityUtils.defaultCommon;
                RarityRaritiesConfig[CardInfo.Rarity.Common].Value = RarityUtils.defaultCommon;
                UpdateRarity(100 * RarityUtils.defaultCommon, CardInfo.Rarity.Common);
                commonSlider.value = 100 * RarityUtils.defaultCommon;
                RarityRarities[CardInfo.Rarity.Uncommon] = RarityUtils.defaultUncommon;
                RarityRaritiesConfig[CardInfo.Rarity.Uncommon].Value = RarityUtils.defaultUncommon;
                UpdateRarity(100 * RarityUtils.defaultUncommon, CardInfo.Rarity.Uncommon);
                uncommonSlider.value = 100 * RarityUtils.defaultUncommon;
                RarityRarities[CardInfo.Rarity.Rare] = RarityUtils.defaultRare;
                RarityRaritiesConfig[CardInfo.Rarity.Rare].Value = RarityUtils.defaultRare;
                UpdateRarity(100 * RarityUtils.defaultRare, CardInfo.Rarity.Rare);
                rareSlider.value = 100 * RarityUtils.defaultRare;
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
                BetterMethod = false;
                BetterMethodConfig.Value = false; 
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


                RarityRarities[CardInfo.Rarity.Common] = RarityUtils.defaultCommon;
                RarityRaritiesConfig[CardInfo.Rarity.Common].Value = RarityUtils.defaultCommon;
                UpdateRarity(100 * RarityUtils.defaultCommon, CardInfo.Rarity.Common);
                commonSlider.value = 100 * RarityUtils.defaultCommon;
                RarityRarities[CardInfo.Rarity.Uncommon] = RarityUtils.defaultUncommon;
                RarityRaritiesConfig[CardInfo.Rarity.Uncommon].Value = RarityUtils.defaultUncommon;
                UpdateRarity(100 * RarityUtils.defaultUncommon, CardInfo.Rarity.Uncommon);
                uncommonSlider.value = 100 * RarityUtils.defaultUncommon;
                RarityRarities[CardInfo.Rarity.Rare] = RarityUtils.defaultRare;
                RarityRaritiesConfig[CardInfo.Rarity.Rare].Value = RarityUtils.defaultRare;
                UpdateRarity(100 * RarityUtils.defaultRare, CardInfo.Rarity.Rare);
                rareSlider.value = 100 * RarityUtils.defaultRare;

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
