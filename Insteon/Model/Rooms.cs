using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insteon.Model;

public class Rooms : List<string>
{
    private House house;

    internal Rooms(House house)
    {
        this.house = house;
        Rebuild();
    }

    /// <summary>
    /// For observers to subscribe to change notifications
    /// </summary>
    /// <param name="observer"></param>
    public Rooms AddObserver(IRoomsObserver observer)
    {
        observers.Add(observer);
        return this;
    }

    /// <summary>
    /// For observers to unsubscribe to change notifications
    /// </summary>
    /// <param name="observer"></param>
    public Rooms RemoveObserver(IRoomsObserver observer)
    {
        observers.Remove(observer);
        return this;
    }

    private List<IRoomsObserver> observers = new();

    // Rebuild this list of rooms based on the devices and scenes in the house
    private void Rebuild()
    {
        var roomSet = new HashSet<string>();

        // Add rooms used by devices
        foreach (var device in house.Devices)
        {
            var room = device.Room;
            if (room != null && room != string.Empty && !roomSet.Contains(room))
            {
                roomSet.Add(room);
            }
        }

        // And rooms used by scenes
        foreach (var scene in house.Scenes)
        {
            var room = scene.Room;
            if (room != null && room != string.Empty && !roomSet.Contains(room))
            {
                roomSet.Add(room);
            }
        }

        Clear();
        foreach (var room in roomSet)
        {
            Add(room);
        }
        Sort();
    }

    /// <summary>
    /// Notify of a change in the rooms
    /// </summary>
    internal void OnRoomsChanged()
    {
        Rebuild();
        observers.ForEach(o => o.RoomsChanged());
    }
}
