using Unity.Netcode;
using UnityEngine;

// Todo : Change to a Singleton
public class ResourceSpawner : NetworkBehaviour
{
    public static ResourceSpawner Instance { get; private set; }

    [SerializeField] private NetworkObject m_woodPrefab, m_stonePrefab;

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

        NetworkObject resource = type == ObjectType.Wood ? m_woodPrefab : m_stonePrefab;
        GameObject instance = Instantiate(resource.gameObject, position, Quaternion.Euler(0, Random.Range(0,360), 0));
        instance.GetComponent<NetworkObject>().Spawn();
    }
}
