using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardDatabase", menuName = "Scriptable Objects/CardDatabase")]
public class CardDatabase : ScriptableObject
{
    public List<CardData> Cards;
    private Dictionary<int, CardData> _lookup;

    public void Initialize()
    {
        _lookup = new();
        foreach (var card in Cards)
            _lookup[card.ID] = card;
    }

    public CardData Get(int id) => _lookup[id];
    public IReadOnlyList<CardData> GetAll() => Cards;
}
