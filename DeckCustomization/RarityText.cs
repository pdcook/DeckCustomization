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
    // patch to add rarity text
    [Serializable]
    [HarmonyPatch(typeof(CardChoice), "Spawn")]
    class CardChoicePatchSpawn
    {
        private static void Postfix(ref GameObject __result)
        {
            __result.GetOrAddComponent<RarityText>();
            NetworkingManager.RPC_Others(typeof(CardChoicePatchSpawn), nameof(RPCO_AddRemotelySpawnedCard), new object[] { __result.GetComponent<PhotonView>().ViewID });
        }
        // tell remote clients that a card has been spawned
        [UnboundRPC]
        private static void RPCO_AddRemotelySpawnedCard(int viewID)
        {
            GameObject card = PhotonView.Find(viewID).gameObject;

            card.GetOrAddComponent<RarityText>();
        }
    }

    // patch to add rarity text
    [Serializable]
    [HarmonyPatch(typeof(CardChoice), "AddCardVisual")]
    class CardChoicePatchAddCardVisual
    {
        private static void Postfix(ref GameObject __result)
        {
            __result.GetOrAddComponent<RarityText>();
        }
    }

    // destroy object once its no longer a child
    public class DestroyOnUnparent : MonoBehaviour
    {
        void LateUpdate()
        {
            if (this.gameObject.transform.parent == null) { Destroy(this.gameObject); }
        }
    }
    internal class RarityText : MonoBehaviour
    {

        private void Start()
        {
            if (!DeckCustomization.DisplayRarities && !DeckCustomization.DisplayPerc) { return; }

            // get the specific card this is attatched to
            CardInfo card = this.gameObject.GetComponent<CardInfo>();

            // create text for rarities
            RectTransform[] allChildrenRecursive = this.gameObject.GetComponentsInChildren<RectTransform>();
            GameObject middleframe = allChildrenRecursive.Where(obj => obj.gameObject.name == "FRAME").FirstOrDefault().gameObject;
            GameObject rarityObj = UnityEngine.GameObject.Instantiate(new GameObject("RarityText", typeof(TextMeshProUGUI), typeof(DestroyOnUnparent)), middleframe.transform.position, middleframe.transform.rotation, middleframe.transform);
            TextMeshProUGUI rarityText = rarityObj.gameObject.GetComponent<TextMeshProUGUI>();
            String text = "";
            Color color = Color.grey;
            if (DeckCustomization.DisplayRarities)
            {
                if (card.rarity == CardInfo.Rarity.Common) color = RarityUtils.commonColor;
                else color = RarityLib.Utils.RarityUtils.GetRarityData(card.rarity).colorOff;
                text += card.rarity.ToString();
            }
            // add a space if both are enabled
            if (DeckCustomization.DisplayRarities && DeckCustomization.DisplayPerc)
            {
                text += " ";
            }    

            // do not add percentage if the card is not in activeCards
            if (DeckCustomization.DisplayPerc && DeckCustomization.activeCards.Select(c => c.cardName).Contains(card.cardName))
            {
                float p = RarityUtils.GetRarityAsPerc(card);
                text += "("+String.Format("{0:0.##}", p*100f)+"%)";
            }
            rarityText.text = text;
            rarityText.color = color;
            rarityText.enableWordWrapping = false;
            rarityObj.transform.localScale = new Vector3(1f, 1f, 1f);
            rarityObj.transform.localPosition = new Vector3(0f, -600f, 0f);
            rarityText.alignment = TextAlignmentOptions.Bottom;
            rarityText.alpha = 0.5f;
            rarityText.fontSize = 65;
        }
    }

}
