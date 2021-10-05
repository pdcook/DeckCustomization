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
    internal static class RarityUtils
    {

        internal static Color commonColor = new Color(1f, 1f, 1f, 1f);
        internal static Color uncommonColor = new Color(0.1745f, 0.6782f, 1f, 1f);
        internal static Color rareColor = new Color(1f, 0.1765f, 0.7567f, 1f);

        /*
        * Default Rarities are:
        * Common: 1 (66.66667%)
        * Uncommon: 0.4 (26.666667%)
        * Rare: 0.1 (6.6666667%)
        */

        internal const float defaultGeneralRarity = 0.5f;
        internal const float defaultCommon = 1f;
        internal const float defaultUncommon = 0.4f;
        internal const float defaultRare = 0.1f;
        internal static Color RarityColorLerp(float p)
        {
            if (p == 0f)
            {
                return Color.grey;
            }

            float Z = (defaultCommon + defaultUncommon + defaultRare);

            Color c1, c2;
            float p_;
            if (p < defaultUncommon)
            {
                c1 = rareColor;
                c2 = uncommonColor;
                p_ = Mathf.Clamp01((Mathf.Clamp01(p) - defaultRare / Z) / ((defaultUncommon - defaultRare) / Z));
            }
            else
            {
                c1 = uncommonColor;
                c2 = commonColor;
                p_ = Mathf.Clamp01((Mathf.Clamp01(p) - defaultUncommon / Z) / ((defaultCommon - defaultUncommon) / Z));
            }
            return Color.Lerp(c1, c2, p_);

        }
        internal static float mod_Z
        {
            get
            {
                return DeckCustomization.ModRarities.Values.Sum();
            }
        }
        internal static float rarity_Z
        {
            get { return DeckCustomization.RarityRarities.Values.Sum(); }
        }
        internal static float theme_Z
        {
            get { return DeckCustomization.ThemeRarities.Values.Sum(); }
        }
        internal static float Z
        {
            get { return DeckCustomization.activeCards.Select(c => DeckCustomization.RarityRarities[c.rarity] * DeckCustomization.ThemeRarities[c.colorTheme] * DeckCustomization.ModRarities[c.gameObject.GetComponent<CustomCard>() != null ? c.gameObject.GetComponent<CustomCard>().GetModName().ToLower() : DeckCustomization.defaultCardsName]).Sum(); }
        }
        internal static float GetRarityAsPerc(string modName)
        {
            return GetRelativeRarity(modName) / mod_Z;
        }
        internal static float GetRarityAsPerc(CardInfo.Rarity rarity)
        {
            return GetRelativeRarity(rarity) / rarity_Z;
        }
        internal static float GetRarityAsPerc(CardThemeColor.CardThemeColorType theme)
        {
            return GetRelativeRarity(theme) / theme_Z;
        }
        internal static float GetRarityAsPerc(CardInfo card)
        {
            return GetRelativeRarity(card) / Z;
        }
        internal static float GetRelativeRarity(string modName)
        {
            return DeckCustomization.ModRarities[modName];
        }
        internal static float GetRelativeRarity(CardInfo.Rarity rarity)
        {
            return DeckCustomization.RarityRarities[rarity];
        }
        internal static float GetRelativeRarity(CardThemeColor.CardThemeColorType theme)
        {
            return DeckCustomization.ThemeRarities[theme];
        }
        internal static float GetRelativeRarity(CardInfo card)
        {
            return DeckCustomization.RarityRarities[card.rarity] * DeckCustomization.ThemeRarities[card.colorTheme] * DeckCustomization.ModRarities[card.gameObject.GetComponent<CustomCard>() != null ? card.gameObject.GetComponent<CustomCard>().GetModName().ToLower() : DeckCustomization.defaultCardsName];
        }
        internal static string GetThemeAsString(CardThemeColor.CardThemeColorType theme)
        {
            return theme switch
            {
                CardThemeColor.CardThemeColorType.DestructiveRed => "Destructive Red",
                CardThemeColor.CardThemeColorType.FirepowerYellow => "Firepower Yellow",
                CardThemeColor.CardThemeColorType.DefensiveBlue => "Defensive Blue",
                CardThemeColor.CardThemeColorType.TechWhite => "Tech White",
                CardThemeColor.CardThemeColorType.EvilPurple => "Evil Purple",
                CardThemeColor.CardThemeColorType.PoisonGreen => "Poison Green",
                CardThemeColor.CardThemeColorType.NatureBrown => "Nature Brown",
                CardThemeColor.CardThemeColorType.ColdBlue => "Cold Blue",
                CardThemeColor.CardThemeColorType.MagicPink => "Magic Pink",
                _ => "",
            };
        }
        internal static Color GetThemeColor(CardThemeColor.CardThemeColorType theme)
        {
            return CardChoice.instance.cardThemes.Where(t => t.themeType == theme).First().targetColor;
        }
    }

}
