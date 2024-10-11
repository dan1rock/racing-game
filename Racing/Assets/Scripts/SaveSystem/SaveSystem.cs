using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem
{
    private const string filePath = "/player.data";

    public static void SavePlayer(GameManager player)
    {
        BinaryFormatter formatter = new();
        string path = Application.persistentDataPath + filePath;
        FileStream stream = new(path, FileMode.Create);

        PlayerData data = new(player);

        formatter.Serialize(stream, data);
        stream.Close();
    }

    public static PlayerData LoadPlayer()
    {
        string path = Application.persistentDataPath + filePath;
        
        if (!File.Exists(path)) return null;
        
        BinaryFormatter formatter = new();
        FileStream stream = new(path, FileMode.Open);

        PlayerData data = formatter.Deserialize(stream) as PlayerData;
        stream.Close();

        return data;
    }
}