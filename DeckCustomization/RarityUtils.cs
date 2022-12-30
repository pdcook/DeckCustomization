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
using System.Text.RegularExpressions;
using ModdingUtils.Utils;

namespace DeckCustomization
{
    internal static class RarityUtils
    {

        internal static Color commonColor = new Color(1f, 1f, 1f, 1f);
        //internal static Color uncommonColor = new Color(0.1745f, 0.6782f, 1f, 1f);
        //internal static Color rareColor = new Color(1f, 0.1765f, 0.7567f, 1f);

        /*
        * Default Rarities are:
        * Common: 1 (66.66667%)
        * Uncommon: 0.4 (26.666667%)
        * Rare: 0.1 (6.6666667%)
        */

        internal const float defaultGeneralRarity = 0.5f;
        //internal const float defaultCommon = 1f;
        //internal const float defaultUncommon = 0.4f;
        //internal const float defaultRare = 0.1f;
        internal static Color RarityColorLerp(float p)
        {
            if (p == 0f)
            {
                return Color.grey;
            }

            List<Rarity> rarities = new List<Rarity>();

            float Z = 0;
            foreach (CardInfo.Rarity rarity in DeckCustomization.RarityRarities.Keys)
            {
                rarities.Add(RarityLib.Utils.RarityUtils.GetRarityData(rarity));
                Z += RarityLib.Utils.RarityUtils.GetRarityData(rarity).relativeRarity;
            }
            rarities.Sort((r1, r2) => r1.relativeRarity.CompareTo(r2.relativeRarity));

            Color c1 = rarities[rarities.Count - 2].color,
            c2 = rarities[rarities.Count-1].color;
            float p_ = (p - rarities[rarities.Count - 2].relativeRarity / Z) / ((rarities[rarities.Count-1].relativeRarity - rarities[rarities.Count - 2].relativeRarity) / Z); ;


            for(int i = 1; i < rarities.Count - 1; i++)
            {
                if(p < (rarities[i].relativeRarity/Z))
                {
                    c1 = rarities[i-1].color;
                    c2 = rarities[i].color;
                    p_ = (p - rarities[i-1].relativeRarity / Z) / ((rarities[i].relativeRarity - rarities[i-1].relativeRarity) / Z);
                    break;
                }
            }
            if (c2 == RarityLib.Utils.RarityUtils.GetRarityData(CardInfo.Rarity.Common).color) c2 = commonColor;
            if (c1 == RarityLib.Utils.RarityUtils.GetRarityData(CardInfo.Rarity.Common).color) c1 = commonColor;
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
            get { return DeckCustomization.activeCards.Select(c => GetRelativeRarity(c.rarity) * GetRelativeRarity(c.colorTheme) * GetRelativeRarity(CardManager.cards.Values.First(card => card.cardInfo == c).category) * RarityLib.Utils.RarityUtils.GetCardRarityModifier(c)).Sum(); }
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
            return RarityLib.Utils.RarityUtils.GetRarityData(rarity).calculatedRarity;
        }
        internal static float GetRelativeRarity(CardThemeColor.CardThemeColorType theme)
        {
            return DeckCustomization.ThemeRarities[theme];
        }
        internal static float GetRelativeRarity(CardInfo card)
        {
            return GetRelativeRarity(card.rarity) * GetRelativeRarity(card.colorTheme) * GetRelativeRarity(CardManager.cards.Values.First(c => c.cardInfo == card).category) * RarityLib.Utils.RarityUtils.GetCardRarityModifier(card);
        }
        internal static string GetThemeAsString(CardThemeColor.CardThemeColorType theme)
        {
            return Regex.Replace(theme.ToString(), @"[A-Z]", " $&", RegexOptions.None).Substring(1);
        }
        internal static Color GetThemeColor(CardThemeColor.CardThemeColorType theme)
        {
            return CardChoice.instance.cardThemes.Where(t => t.themeType == theme).First().targetColor;
        }

        internal static IEnumerator UpdateRarities()
        {
            DeckCustomization.RarityRarities.Keys.ToList().ForEach(r =>
                RarityLib.Utils.RarityUtils.GetRarityData(r).calculatedRarity = DeckCustomization.RarityRarities[r]
            );
            yield break;
        }
    }

}
