using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class GameData
{
    public int Gold = 5000;
    public int Level = 1;
    public List<CardInformation> OwnCards = new();
    public List<int> DeckCardIDs = new();
    public float BgmVolume = 0.3f;
    public float SfxVolume = 0.5f;
}

public static class DataManager
{
    public static GameData Data;
    public static bool SaveFlag;
    private static string SavePath => Path.Combine(Application.persistentDataPath, "GameData.json");

    public static void SetDefaultData()
    {
        Data.Gold = 5000;
        Data.Level = 1;
        Data.OwnCards = new();
        Data.DeckCardIDs = new();
        Data.BgmVolume = 0.3f;
        Data.SfxVolume = 0.5f;
    }

    public static bool Load()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                Data = new GameData();
                SetDefaultData();
                Save();
                return true;
            }

            string json = File.ReadAllText(SavePath);
            Data = JsonUtility.FromJson<GameData>(json);

            if (Data == null)
            {
                Data = new GameData();
                SetDefaultData();
                Save();
                return true;
            }

            MigrateData();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Data = new GameData();
            SetDefaultData();
            return true;
        }
        return false;
    }

    private static void MigrateData()
    {
        if (Data.OwnCards == null) Data.OwnCards = new();
        if (Data.DeckCardIDs == null) Data.DeckCardIDs = new();

        foreach (var card in Data.OwnCards)
        {
            if (card.StarLevel <= 0) card.StarLevel = 1;
        }

        Data.BgmVolume = Mathf.Clamp01(Data.BgmVolume);
        Data.SfxVolume = Mathf.Clamp01(Data.SfxVolume);
    }

    public static void RequestSave() => SaveFlag = true;

    public static void Save()
    {
        if (Data == null) return;
        Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
        File.WriteAllText(SavePath, JsonUtility.ToJson(Data, true));
        SaveFlag = false;
    }
}
