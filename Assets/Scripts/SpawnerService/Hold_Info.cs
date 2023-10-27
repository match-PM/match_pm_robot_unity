using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hold_Info : MonoBehaviour
{
    List<string> spawnNamesList = new List<string>();
    List<string> frameNamesList = new List<string>();

    // Start is called before the first frame update
    void Start()
    {
        GameObject[] GameObjects = GameObject.FindGameObjectsWithTag("spawned");
        foreach(GameObject go in GameObjects)
        {
            spawnNamesList.Add(go.name);
        }
        GameObject[] FrameObjects = GameObject.FindGameObjectsWithTag("RefFrame");
        foreach(GameObject fo in FrameObjects)
        {
            spawnNamesList.Add(fo.name);
        }
    }

    //ObjectNames
    public void addToSpawnNamesList(string add)
    {
        spawnNamesList.Add(add);
        Debug.Log("Added " + add);
    }

    public void removeFromSpawnNamesList(string remove)
    {
        spawnNamesList.Remove(remove);
        Debug.Log("Removed " + remove);
    }
    
    public string[] getSpawnNamesList()
    { 
        return spawnNamesList.ToArray();
    }

    //RefFrames
    public void addToFrameNamesList(string add)
    {
        frameNamesList.Add(add);
        Debug.Log("Added " + add);
    }

    public void removeFromFrameNamesList(string remove)
    {
        frameNamesList.Remove(remove);
        Debug.Log("Removed " + remove);
    }
    
    public string[] getFrameNamesList()
    { 
        return frameNamesList.ToArray();
    }
}
