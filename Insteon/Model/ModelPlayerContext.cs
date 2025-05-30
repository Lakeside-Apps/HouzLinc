using System;

namespace Insteon.Model;

// A context object passed to all the model changes as we are playing them.
// Used to implement functionality requiring0 changes to coordinate with each other,
// such as scene Id adjustment in case of conflit.

internal class ModelPlayerContext
{
    internal ModelPlayerContext() { }

    // Used to adjust scene Ids in case of conflicts while playing the changes.
    // If a scene Id is already in use in the target house, we lookg whether we
    // have an adjustment for that id and use the adjusted Id if so. If we don't
    // we use the next available id in the list of scenes.

    // TODO: Consider removing this and using globally unique ids for scenes 
    // intead, like we do for all-link records
    private Dictionary<int, int> sceneIdAdjustments = new Dictionary<int, int>();
    
    internal int AdjustSceneId(int sceneId)
    {
        if (sceneIdAdjustments.TryGetValue(sceneId, out var adjustedId))
        {
            return adjustedId;
        }
        return sceneId;
    }

    internal void setSceneIdAdjustment(int originalId, int adjustedId)
    {
        if (sceneIdAdjustments.ContainsKey(originalId))
        {
            sceneIdAdjustments[originalId] = adjustedId;
        }
        else
        {
            sceneIdAdjustments.Add(originalId, adjustedId);
        }
    }
}
