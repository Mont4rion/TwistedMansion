public class Exit
{
    public Room TargetRoom { get; set; }
    public bool IsLocked { get; set; }
    public string RequiredItem { get; set; } // Name of the item needed to unlock (internal for logic)

    public Exit(Room targetRoom, bool isLocked = false, string requiredItem = null)
    {
        TargetRoom = targetRoom;
        IsLocked = isLocked;
        RequiredItem = requiredItem;
    }
}