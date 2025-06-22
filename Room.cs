public class Room
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, Exit> Exits { get; set; }
    public List<Item> ItemsInRoom { get; set; }

    public Room(string name, string description)
    {
        this.Name = name;
        this.Description = description;
        this.Exits = new Dictionary<string, Exit>();
        this.ItemsInRoom = new List<Item>();
    }

    public void AddExit(string direction, Room targetRoom, bool isLocked = false, string requiredItem = null)
    {
        Exits.Add(direction.ToLower(), new Exit(targetRoom, isLocked, requiredItem));
    }

    public string GetAvailableExits()
    {
        List<string> exitDescriptions = new List<string>();
        foreach (var exitEntry in Exits)
        {
            string direction = exitEntry.Key;
            Exit exit = exitEntry.Value;

            if (exit.IsLocked)
            {
                // GEÄNDERT: requiredItem wird NICHT mehr angezeigt
                exitDescriptions.Add($"'{direction}' leads to a locked {exit.TargetRoom.Name} door");
            }
            else
            {
                exitDescriptions.Add($"'{direction}' leads to the {exit.TargetRoom.Name}");
            }
        }
        if (exitDescriptions.Any())
        {
             return "You see exits: " + string.Join(", ", exitDescriptions) + ".";
        }
        else
        {
            return "There are no obvious exits.";
        }
    }
}