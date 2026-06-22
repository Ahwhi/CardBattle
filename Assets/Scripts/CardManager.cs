using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CardInformation
{
    public int ID;
    public int StarLevel;
    public int Quantity;

    public CardInformation() { }

    public CardInformation(int id, int starLevel, int quantity)
    {
        ID = id;
        StarLevel = starLevel;
        Quantity = quantity;
    }
}

public static class CardManager
{
    public static void Add(int id, int amount = 1)
    {
        var card = Get(id);
        if (card == null)
        {
            card = new CardInformation(id, 1, amount);
            DataManager.Data.OwnCards.Add(card);
        }
        else
        {
            card.Quantity += amount;
            TryUpgradeStarLevel(card);
        }
        DataManager.RequestSave();
    }

    public static bool Remove(int id)
    {
        var card = DataManager.Data.OwnCards.Find(x => x.ID == id);
        if (card == null) return false;
        DataManager.Data.OwnCards.Remove(card);
        DataManager.RequestSave();
        return true;
    }

    public static CardInformation Get(int id) =>
        DataManager.Data.OwnCards.Find(x => x.ID == id);

    public static bool Has(int id) =>
        DataManager.Data.OwnCards.Exists(x => x.ID == id);

    private static void TryUpgradeStarLevel(CardInformation card)
    {
        const int upgradeThreshold = 10;
        while (card.Quantity >= upgradeThreshold)
        {
            card.Quantity -= upgradeThreshold;
            card.StarLevel++;
        }
    }
}

public static class DeckManager
{
    public const int MaxDeckSize = 6;

    public static List<int> GetDeck() => DataManager.Data.DeckCardIDs ??= new();

    public static bool AddToDeck(int cardID)
    {
        var deck = GetDeck();
        if (deck.Count >= MaxDeckSize || deck.Contains(cardID)) return false;
        deck.Add(cardID);
        DataManager.RequestSave();
        return true;
    }

    public static bool RemoveFromDeck(int cardID)
    {
        bool removed = GetDeck().Remove(cardID);
        if (removed) DataManager.RequestSave();
        return removed;
    }

    public static bool IsDeckComplete() => GetDeck().Count == MaxDeckSize;

    public static bool IsInDeck(int cardID) => GetDeck().Contains(cardID);

    public static bool ReplaceInDeck(int oldCardID, int newCardID)
    {
        var deck = GetDeck();
        int index = deck.IndexOf(oldCardID);
        if (index < 0 || deck.Contains(newCardID)) return false;
        deck[index] = newCardID;
        DataManager.RequestSave();
        return true;
    }

    public static bool SwapInDeck(int idA, int idB)
    {
        var deck = GetDeck();
        int indexA = deck.IndexOf(idA);
        int indexB = deck.IndexOf(idB);
        if (indexA < 0 || indexB < 0) return false;
        (deck[indexA], deck[indexB]) = (deck[indexB], deck[indexA]);
        DataManager.RequestSave();
        return true;
    }
}
