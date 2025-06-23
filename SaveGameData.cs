// SaveGameManager.cs

using System;
using System.IO;
using System.Text.Json; // Or using Newtonsoft.Json; if you're using that library
using System.Collections.Generic; // Potentially needed for SaveGameData

public class SaveGameManager
{
    private readonly string _filePath;

    // This is the constructor that is currently missing or not public
    // You need to add this (or make it public if it's already there but private/internal)
    public SaveGameManager(string filePath)
    {
        _filePath = filePath;
        // Optionally, you might want to ensure the directory exists here
        string directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public void SaveGame(SaveGameData data)
    {
        try
        {
            // Using System.Text.Json (built-in .NET Core/.NET 5+)
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(data, options);
            File.WriteAllText(_filePath, jsonString);
            Console.WriteLine($"Game saved successfully to {_filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving game: {ex.Message}");
        }
    }

    public SaveGameData LoadGame()
    {
        if (!File.Exists(_filePath))
        {
            Console.WriteLine($"No save game found at {_filePath}.");
            return null;
        }

        try
        {
            string jsonString = File.ReadAllText(_filePath);
            // Using System.Text.Json
            SaveGameData loadedData = JsonSerializer.Deserialize<SaveGameData>(jsonString);
            Console.WriteLine($"Game loaded successfully from {_filePath}");
            return loadedData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading game: {ex.Message}");
            return null;
        }
    }
}

// You also need your SaveGameData class (likely in its own file, or below SaveGameManager)
// Make sure this class and its properties are public so they can be serialized/deserialized.
public class SaveGameData
{
    public string PlayerName { get; set; }
    public string CurrentRoomName { get; set; }
    public List<string> PlayerInventoryItemNames { get; set; } = new List<string>();
    public int GuestroomEventCount { get; set; }
    public int LibraryEventCount { get; set; }
    public bool CorridorLibraryDoorUnlocked { get; set; }

    // Dictionary to store room exit locked states
    // Key: Room Name (string)
    // Value: Dictionary where Key: Exit Direction (string), Value: IsLocked (bool)
    public Dictionary<string, Dictionary<string, bool>> RoomExitLockedStates { get; set; } = new Dictionary<string, Dictionary<string, bool>>();
}