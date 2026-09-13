using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class Resource
{
    public ObjectType objectType;
    public NetworkObject objectPrefab;
}
public class ResourceSpawner : NetworkBehaviour
{
    public static ResourceSpawner Instance { get; private set; }

    [SerializeField] private List<Resource> m_resources;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Singleton for AudioManager already exist, destroying the old one");
            Destroy(Instance.gameObject);
        }

        Instance = this;
    }
    public void SpawnResource(ObjectType type, Vector3 position)
    {
        if (IsServer == false)
            return;

        foreach (Resource resource in m_resources)
        {
            if(type == resource.objectType)
            {
                NetworkObject networkResource = resource.objectPrefab;
                GameObject instance = Instantiate(networkResource.gameObject, position, Quaternion.Euler(0, UnityEngine.Random.Range(0,360), 0));
                instance.GetComponent<NetworkObject>().Spawn();
            }
        }
        
    }
}
