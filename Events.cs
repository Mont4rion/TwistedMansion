
// ---
public class Events
{
    public string CurrentRoomName { get; private set; }
    private int guestroomEventCount = 0;
    private int libraryEventCount = 0;
    private bool corridorLibraryDoorUnlocked = false;
    private Player _player;
    private ObjectManager _objectManager;

    public int GetGuestroomEventCount() { return guestroomEventCount; }
    public void SetGuestroomEventCount(int value) { guestroomEventCount = value; }

    public int GetLibraryEventCount() { return libraryEventCount; }
    public void SetLibraryEventCount(int value) { libraryEventCount = value; }

    public bool GetCorridorLibraryDoorUnlocked() { return corridorLibraryDoorUnlocked; }
    public void SetCorridorLibraryDoorUnlocked(bool value) { corridorLibraryDoorUnlocked = value; }


    public Events(string initialRoomName, Player player, ObjectManager objectManager)
    {
        CurrentRoomName = initialRoomName;
        _player = player;
        _objectManager = objectManager;
    }

    public void UpdateCurrentRoom(string newRoomName)
    {
        CurrentRoomName = newRoomName;
    }

    public bool CheckForRoomEvents()
    {
        if (CurrentRoomName == "Guestroom")
        {
            if (guestroomEventCount == 0)
            {
                Console.WriteLine("\n------------------------------------");
                Console.WriteLine("Ein unerklärlicher, eisiger Hauch umgibt dich, als du das Gästezimmer betrittst.");
                Console.WriteLine("Du hörst ein leises, beunruhigendes Wispern, das direkt aus den Schatten zu kommen scheint.");
                Console.WriteLine("------------------------------------");
                Console.WriteLine("Was tust du? (untersuchen / ignorieren)");

                guestroomEventCount = 1;
                return true;
            }
        }
        else if (CurrentRoomName == "Library")
        {
            if (libraryEventCount == 0)
            {
                if (_player.Inventory.Any(item => item.Name.ToLower() == "kitchen knife"))
                {
                    Console.WriteLine("\n------------------------------------");
                    Console.WriteLine("Als du die Bibliothek betrittst, fällt dein Blick auf ein seltsam leuchtendes Buch.");
                    Console.WriteLine("Es pulsiert sanft auf einem der oberen Regale. ");
                    Console.WriteLine("Mit dem Küchenmesser könntest du versuchen, es herunterzuholen.");
                    Console.WriteLine("------------------------------------");
                    Console.WriteLine("Möchtest du versuchen, es zu erreichen? (ja / nein)");

                    libraryEventCount = 1;
                    return true;
                }
                else
                {
                    Console.WriteLine("Die Bibliothek ist still, abgesehen vom Rascheln der Blätter im Wind.");
                }
            }
        }
        else if (CurrentRoomName == "Corridor")
        {
            if (!corridorLibraryDoorUnlocked)
            {
                Console.WriteLine("\n------------------------------------");
                Console.WriteLine("Du bemerkst, dass die Wand zum Westen hin rissig ist. Ein schwacher Lichtschein dringt hindurch.");
                Console.WriteLine("Es scheint, als ob man von dieser Seite aus einen Weg in die Bibliothek schaffen könnte.");
                Console.WriteLine("------------------------------------");

                Room libraryRoom = _objectManager.GetRoom("Library");
                if (libraryRoom != null && libraryRoom.Exits.ContainsKey("east"))
                {
                    libraryRoom.Exits["east"].IsLocked = false;
                    libraryRoom.Exits["east"].RequiredItem = null; // Entferne den RequiredItem-Hinweis hier auch
                    Console.WriteLine("Du schiebst ein lose sitzendes Brett beiseite und hörst ein Klicken.");
                    Console.WriteLine("Der Weg zur Bibliothek ist nun frei!");
                }
                corridorLibraryDoorUnlocked = true;
                return false;
            }
        }
        return false;
    }

    public bool HandleExclusiveEventInput(string command)
    {
        if (CurrentRoomName == "Guestroom")
        {
            if (guestroomEventCount == 1)
            {
                if (command == "untersuchen")
                {
                    Console.WriteLine("Du versuchst, die Quelle des Wisperns zu finden. Der Raum scheint sich zu verdunkeln.");
                    Console.WriteLine("Plötzlich spürst du eine kalte Hand an deinem Nacken. Ein Schrei entweicht dir!");
                    Console.WriteLine("Du stolperst rückwärts und fliehst panisch aus dem Raum.");
                    guestroomEventCount = 2;
                    return false;
                }
                else if (command == "ignorieren")
                {
                    Console.WriteLine("Du versuchst, das Wispern zu ignorieren und schnell den Raum zu verlassen.");
                    Console.WriteLine("Die Stimmen werden lauter, aber du schaffst es, die Tür hinter dir zu schließen.");
                    guestroomEventCount = 2;
                    return false;
                }
                // Ergänzung für den Fall, dass kein passender Befehl eingegeben wird
                else
                {
                    Console.WriteLine("Das ist keine gültige Aktion für dieses Event.");
                    return true; // Bleibe im Event-Modus
                }
            }
        }
        else if (CurrentRoomName == "Library")
        {
            if (libraryEventCount == 1)
            {
                if (command == "ja")
                {
                    if (_player.Inventory.Any(item => item.Name.ToLower() == "kitchen knife"))
                    {
                        Console.WriteLine("Du benutzt das Küchenmesser, um das leuchtende Buch vom Regal zu hebeln.");
                        Item glowingBook = _objectManager.GetItem("Old Book"); // Annahme: "Old Book" ist das leuchtende Buch
                        if (glowingBook != null)
                        {
                            Console.WriteLine($"Das {glowingBook.Name} fällt in deine Hände.");
                            // Entferne das Buch aus dem Raum, falls es noch dort ist (sollte es nicht, da es "genommen" wird)
                            _objectManager.GetRoom("Library").ItemsInRoom.Remove(glowingBook);
                            _player.AddItem(glowingBook); // Füge es dem Inventar hinzu
                        }
                        Console.WriteLine("Das Leuchten des Buches erlischt, sobald du es in der Hand hältst.");
                        libraryEventCount = 2; // Event ist abgeschlossen
                        return false; // Beende den exklusiven Event-Modus
                    }
                    else
                    {
                        Console.WriteLine("Du hast nichts, womit du das Buch erreichen könntest.");
                        return true; // Bleibe im Event-Modus, bis der Spieler eine andere Aktion wählt oder das richtige Item hat
                    }
                }
                else if (command == "nein")
                {
                    Console.WriteLine("Du lässt das leuchtende Buch vorerst in Ruhe.");
                    libraryEventCount= 0; // Event ist abgeschlossen
                    return false; // Beende den exklusiven Event-Modus
                }
                else
                {
                    Console.WriteLine("Das ist keine gültige Aktion für dieses Event. (ja / nein)");
                    return true; // Bleibe im Event-Modus
                }
            }
        }
        return false; // Event ist nicht aktiv oder wurde abgeschlossen
    }
}