using System;
using UnboundLib;
using HarmonyLib;
using UnityEngine;
using ModdingUtils.Utils;
using System.Linq;
using CardChoiceSpawnUniqueCardPatch;
using System.Reflection;
using System.Collections.Generic;

namespace DeckCustomization
{
    // patch to use custom rarities
    [Serializable]
    [HarmonyPatch(typeof(ModdingUtils.Patches.CardChoicePatchGetRanomCard), "OrignialGetRanomCard", new Type[] {typeof(CardInfo[])})]
    class OrignialGetRanomCardPatch
	{
        private static bool Prefix(CardChoice __instance, ref GameObject __result, CardInfo[] cards)
        {
			float num = 0f;
			for (int i = 0; i < cards.Length; i++)
			{
				num += RarityUtils.GetRelativeRarity(cards[i]);
			}
			float num2 = UnityEngine.Random.Range(0f, num);
			for (int j = 0; j < cards.Length; j++)
			{
				num2 -= RarityUtils.GetRelativeRarity(cards[j]);

				if (num2 <= 0f)
				{
					__result = cards[j].gameObject;
					break;
				}
			}

			return false; // always skip original

        }
    }

}
