using System.Text.Json;

public class SaveGameData
{
    public string PlayerName { get; set; }
    public string CurrentRoomName { get; set; }
    public List<string> PlayerInventoryItemNames { get; set; } = new List<string>();
    public Dictionary<string, Dictionary<string, bool>> RoomExitLockedStates { get; set; } = new Dictionary<string, Dictionary<string, bool>>();

    // Event-Zustände
    public int GuestroomEventCount { get; set; }
    public int LibraryEventCount { get; set; }
    public bool CorridorLibraryDoorUnlocked { get; set; }
}

//----

public class SaveGameManager
{
    private const string SaveFileName = "savegame.json";

    public void SaveGame(SaveGameData data)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(data, options);
            File.WriteAllText(SaveFileName, jsonString);
            Console.WriteLine("\nGame saved successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving game: {ex.Message}");
        }
    }

    public SaveGameData LoadGame()
    {
        if (File.Exists(SaveFileName))
        {
            try
            {
                string jsonString = File.ReadAllText(SaveFileName);
                SaveGameData data = JsonSerializer.Deserialize<SaveGameData>(jsonString);
                Console.WriteLine("Game loaded successfully!");
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading game: {ex.Message}");
                return null;
            }
        }
        else
        {
            Console.WriteLine("No saved game found.");
            return null;
        }
    }
}