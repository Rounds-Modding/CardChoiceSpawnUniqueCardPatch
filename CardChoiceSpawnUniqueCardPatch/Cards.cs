using BepInEx; // requires BepInEx.dll and BepInEx.Harmony.dll
using UnityEngine; // requires UnityEngine.dll, UnityEngine.CoreModule.dll, and UnityEngine.AssetBundleModule.dll
using HarmonyLib; // requires 0Harmony.dll
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
// requires Assembly-CSharp.dll
// requires MMHOOK-Assembly-CSharp.dll

namespace CardChoiceSpawnUniqueCardPatch.Utils
{
    // stolen from PCE
    public sealed class Cards
    {
        // singleton design
        public static readonly Cards instance = new Cards();
        private Cards()
        {
            Cards instance = this;
        }
        public bool CardIsUniqueFromCards(CardInfo card, CardInfo[] cards)
        {
            bool unique = true;

            foreach (CardInfo otherCard in cards)
            {
                if (card.cardName == otherCard.cardName)
                {
                    unique = false;
                }
            }

            return unique;
        }

        public bool CardDoesNotConflictWithCards(CardInfo card, CardInfo[] cards)
        {
            bool conflicts = false;

            foreach (CardInfo otherCard in cards)
            {
                if (card.categories.Intersect(otherCard.blacklistedCategories).Any())
                {
                    conflicts = true;
                }
            }

            return conflicts;
        }

        public bool PlayerIsAllowedCard(Player player, CardInfo card)
        {
            bool blacklisted = false;

            foreach (CardInfo currentCard in player.data.currentCards)
            {
                if (card.categories.Intersect(currentCard.blacklistedCategories).Any())
                {
                    blacklisted = true;
                }
            }

            return !blacklisted && (card.allowMultiple || !player.data.currentCards.Where(cardinfo => cardinfo.name == card.name).Any());

        }
        public CardInfo GetRandomCardWithCondition(CardChoice cardChoice, Player player, Func<CardInfo, Player, bool> condition, int maxattempts = 1000)
        {

            CardInfo card = ((GameObject)typeof(CardChoice).InvokeMember("GetRanomCard",
                        BindingFlags.Instance | BindingFlags.InvokeMethod |
                        BindingFlags.NonPublic, null, cardChoice, new object[] { })).GetComponent<CardInfo>();

            int i = 0;

            // draw a random card until it's an uncommon or the maximum number of attempts was reached
            while (!condition(card, player) && i < maxattempts)
            {
                card = ((GameObject)typeof(CardChoice).InvokeMember("GetRanomCard",
                           BindingFlags.Instance | BindingFlags.InvokeMethod |
                           BindingFlags.NonPublic, null, cardChoice, new object[] { })).GetComponent<CardInfo>();
                i++;
            }

            if (!condition(card, player))
            {
                return null;
            }
            else
            {
                return card;
            }

        }

        public CardInfo[] GetAllCardsWithCondition(CardChoice cardChoice, Player player, Func<CardInfo,Player,bool> condition)
        {
            List<CardInfo> validCards = new List<CardInfo>() { };
            
            foreach (CardInfo card in cardChoice.cards)
            {
                if (condition(card,player))
                {
                    validCards.Add(card);
                }
            }

            return validCards.ToArray();
        }

        public CardInfo[] GetAllCardsWithCondition(CardInfo[] cards, Player player, Func<CardInfo, Player, bool> condition)
        {
            List<CardInfo> validCards = new List<CardInfo>() { };

            foreach (CardInfo card in cards)
            {
                if (condition(card, player))
                {
                    validCards.Add(card);
                }
            }

            return validCards.ToArray();
        }

        public int GetCardID(CardInfo card)
        {
            return Array.IndexOf(global::CardChoice.instance.cards, card);
        }
        public CardInfo GetCardWithID(int cardID)
        {
            return global::CardChoice.instance.cards[cardID];
        }
    }

}