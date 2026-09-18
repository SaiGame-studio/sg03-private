using UnityEngine;

/// <summary>
/// General-purpose spawner for every prefab that derives from <see cref="PoolObj"/>.
/// </summary>
[AddComponentMenu("SG03/Spawner/Object Pool")]
public class ObjectPool : Spawner<PoolObj>
{
    public override PoolObj Spawn(PoolObj prefab)
    {
        PoolObj instance = base.Spawn(prefab);
        if (instance != null) instance.gameObject.SetActive(true);
        return instance;
    }

    public override PoolObj Spawn(PoolObj prefab, Vector3 position)
    {
        PoolObj instance = base.Spawn(prefab, position);
        if (instance != null) instance.gameObject.SetActive(true);
        return instance;
    }

    /// <summary>Gets an instance without activating it so callers can configure it first.</summary>
    public T SpawnInactive<T>(T prefab, Vector3 position) where T : PoolObj
    {
        T instance = base.Spawn(prefab, position) as T;
        if (instance != null) instance.gameObject.SetActive(false);
        return instance;
    }

    /// <summary>Spawns a pooled object while preserving its concrete component type.</summary>
    public T Spawn<T>(T prefab) where T : PoolObj
    {
        return this.Spawn((PoolObj)prefab) as T;
    }

    /// <summary>Spawns a pooled object at a world position while preserving its concrete component type.</summary>
    public T Spawn<T>(T prefab, Vector3 position) where T : PoolObj
    {
        return base.Spawn(prefab, position) as T;
    }
}
