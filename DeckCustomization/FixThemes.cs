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
    internal static class FixThemes
    {
        /* "Incorrect" card themes:
         * 
         * BigBullet -> DestructiveRed
         * BuckShot -> FirepowerYellow
         * DrillAmmo -> TechWhite
         * Echo -> DefensiveBlue
         * Empower -> FirepowerYellow
         * ExplosiveBullet -> FirepowerYellow
         * Implode -> MagicPink
         * Saw -> TechWhite
         * Shockwave -> DestructiveRed
         * TacticalReload -> FirepowerYellow
         * TasteOfBlood -> EvilPurple
         * Teleport -> MagicPink
         * 
         */

        internal static void FixCardThemes(CardInfo[] cards)
        {
            foreach (CardInfo card in cards)
            {
                switch (card.CardName.ToLower())
                {
                    case "big bullet":
                        card.colorTheme = CardThemeColor.CardThemeColorType.DestructiveRed;
                        break;
                    case "buckshot":
                        card.colorTheme = CardThemeColor.CardThemeColorType.FirepowerYellow;
                        break;
                    case "drill ammo":
                        card.colorTheme = CardThemeColor.CardThemeColorType.TechWhite;
                        break;
                    case "echo":
                        card.colorTheme = CardThemeColor.CardThemeColorType.DefensiveBlue;
                        break;
                    case "empower":
                        card.colorTheme = CardThemeColor.CardThemeColorType.FirepowerYellow;
                        break;
                    case "explosive bullet":
                        card.colorTheme = CardThemeColor.CardThemeColorType.FirepowerYellow;
                        break;
                    case "implode":
                        card.colorTheme = CardThemeColor.CardThemeColorType.MagicPink;
                        break;
                    case "saw":
                        card.colorTheme = CardThemeColor.CardThemeColorType.TechWhite;
                        break;
                    case "shockwave":
                        card.colorTheme = CardThemeColor.CardThemeColorType.DestructiveRed;
                        break;
                    case "tactical reload":
                        card.colorTheme = CardThemeColor.CardThemeColorType.FirepowerYellow;
                        break;
                    case "taste of blood":
                        card.colorTheme = CardThemeColor.CardThemeColorType.EvilPurple;
                        break;
                    case "teleport":
                        card.colorTheme = CardThemeColor.CardThemeColorType.MagicPink;
                        break;
                }
            }
        }

    }

}
