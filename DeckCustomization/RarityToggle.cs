using BepInEx.Configuration;
using HarmonyLib;
using ModdingUtils.Utils;
using RarityLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnboundLib;
using UnboundLib.Utils;
using UnboundLib.Utils.UI;
using UnityEngine;

namespace DeckCustomization
{
    public class RarityToggle
    {
        public static Dictionary<string, List<CardInfo>> cardsWithMod = new Dictionary<string, List<CardInfo>>();
        public static Dictionary<CardInfo, CardInfo.Rarity> cardsDefaultRarity = new Dictionary<CardInfo, CardInfo.Rarity>();
        public static Dictionary<CardInfo, CardInfo.Rarity> cardsRarity = new Dictionary<CardInfo, CardInfo.Rarity>();
        public static List<string> RarityIndex = new List<string>();
        public static Dictionary<string, string> cardRarityIndex = new Dictionary<string, string>();

        internal static void Init(ConfigFile Config)
        {
            RarityIndex.Add("Default");
            foreach (KeyValuePair<int, Rarity> item in RarityLib.Utils.RarityUtils.Rarities)
            {
                RarityIndex.Add(item.Value.name);
            }

            foreach (CardInfo card in DeckCustomization.allCards)
            {
                string mod = CardManager.cards.Values.First(c => c.cardInfo == card).category;

                // Check if the mod already exists in the dictionary
                if (!cardsWithMod.ContainsKey(mod))
                {
                    // If the mod doesn't exist, add it to the dictionary with a new list
                    cardsWithMod.Add(mod, new List<CardInfo>());
                }

                // Add the current card to the list associated with the mod
                cardsWithMod[mod].Add(card);
                cardsDefaultRarity.Add(card, card.rarity);
                cardsRarity.Add(card, card.rarity);
            }
        }

        public static CardInfo.Rarity GetCardRarityOrDefault(CardInfo card, int index)
        {
            if (index == 0) return cardsDefaultRarity[card];
            return RarityLib.Utils.RarityUtils.GetRarity(RarityIndex[index]);
        }

        public static CardInfo ChangeCardRarity(CardInfo card, int index)
        {
            CardInfo.Rarity rarity = GetCardRarityOrDefault(card, index);
            return ChangeCardRarity(card, rarity.ToString());
        }

        public static bool IsCardDefaultDefaultRarity(CardInfo card)
        {
            return cardsDefaultRarity[card].HasFlag(card.rarity);
        }

        public static CardInfo ChangeCardRarity(CardInfo card, string rarity)
        {
            card.rarity = RarityLib.Utils.RarityUtils.GetRarity(rarity);
            if (!cardsRarity.ContainsKey(card))
            {
                cardsRarity.Add(card, card.rarity);
            } else
            {
                cardsRarity[card] = RarityLib.Utils.RarityUtils.GetRarity(rarity);
            }
            return card;
        }

        [Serializable]
        [HarmonyPatch(typeof(ToggleCardsMenuHandler), nameof(ToggleCardsMenuHandler.UpdateVisualsCardObj))]
        public class Patch
        {
            public static void Prefix(GameObject cardObject)
            {
                Unbound.Instance.ExecuteAfterFrames(15, () => {
                    try
                    {
                        GameObject.Destroy(cardObject.GetComponentInChildren<ParticleSystem>().gameObject);
                    } catch { }
                    if (ToggleCardsMenuHandler.cardMenuCanvas.gameObject.activeSelf)
                    {
                        //string name = cardObject.GetComponentInChildren<CardInfo>().name.Substring(0, cardObject.GetComponentInChildren<CardInfo>().name.Length - 7);
                        cardObject.GetComponentInChildren<CardInfo>().rarity = cardObject.GetComponentInChildren<CardInfo>().sourceCard.rarity;
                        cardObject.GetComponentsInChildren<CardRarityColor>().ToList().ForEach(r => r.Toggle(true));
                    }
                });
            }
        }
    }
}
