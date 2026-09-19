using UnityEngine;

/// <summary>Prefab registry used by <see cref="ObjectPool"/>.</summary>
[AddComponentMenu("SG03/Spawner/Object Pool Prefabs")]
public class ObjectPoolPrefabs : PoolPrefabs<PoolObj>
{
    protected override void LoadPrefabs()
    {
        this.prefabs.Clear();
        foreach (Transform child in this.transform)
        {
            PoolObj classPrefab = child.GetComponent<PoolObj>();
            if (classPrefab != null) this.prefabs.Add(classPrefab);
        }
        Debug.Log(this.transform.name + ": LoadPrefabs (" + this.prefabs.Count + ")", this.gameObject);
    }
}
