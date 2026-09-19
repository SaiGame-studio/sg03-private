using UnityEngine;

/// <summary>
/// General-purpose spawner for every prefab that derives from <see cref="PoolObj"/>.
/// Requires essential objects like GhostDefeatVfx and GhostDamageVfx to be pre-instantiated in the pool.
/// </summary>
[AddComponentMenu("SG03/Spawner/Object Pool")]
public class ObjectPool : Spawner<PoolObj>
{
    protected override void Start()
    {
        base.Start();
        this.ValidatePrewarmedObjects();
    }

    protected virtual void ValidatePrewarmedObjects()
    {
        this.CheckPrewarmedObject("GhostDefeatVfx");
        this.CheckPrewarmedObject("GhostDamageVfx");
    }

    private void CheckPrewarmedObject(string poolObjName)
    {
        if (!this.HasInPoolObject(poolObjName))
        {
            Debug.LogError($"[ObjectPool] '{poolObjName}' is missing in pool! Please pre-instantiate '{poolObjName}' under PoolHolder in the scene.", this.gameObject);
        }
    }

    public virtual bool HasInPoolObject(string poolObjName)
    {
        if (this.inPoolObjs == null) return false;
        foreach (PoolObj obj in this.inPoolObjs)
        {
            if (obj != null && obj.GetName() == poolObjName) return true;
        }
        return false;
    }

    protected virtual PoolObj SpawnObject(PoolObj prefab, bool activate = true)
    {
        if (prefab == null) return null;

        PoolObj instance = this.GetObjFromPool(prefab);
        if (instance == null)
        {
            if (this.IsRuntimeCreationDisallowed(prefab))
            {
                Debug.LogError($"[ObjectPool] No available '{prefab.GetName()}' instance in pool! Runtime instantiation is disabled. Pre-instantiate it under PoolHolder.", this.gameObject);
                return null;
            }

            instance = Instantiate(prefab);
            this.spawnCount++;
            this.UpdateName(prefab.transform, instance.transform);
        }

        if (this.poolHolder != null) instance.transform.parent = this.poolHolder.transform;
        instance.gameObject.SetActive(activate);
        return instance;
    }

    protected virtual bool IsRuntimeCreationDisallowed(PoolObj prefab)
    {
        if (prefab == null) return false;
        string name = prefab.GetName();
        return name == "GhostDefeatVfx" || name == "GhostDamageVfx";
    }

    public override PoolObj Spawn(PoolObj prefab)
    {
        return this.SpawnObject(prefab, true);
    }

    public override PoolObj Spawn(PoolObj prefab, Vector3 position)
    {
        PoolObj instance = this.SpawnObject(prefab, true);
        if (instance != null) instance.transform.position = position;
        return instance;
    }

    /// <summary>Spawns a pooled object while preserving its concrete component type.</summary>
    public T Spawn<T>(T prefab) where T : PoolObj
    {
        return this.SpawnObject(prefab, true) as T;
    }

    /// <summary>Spawns a pooled object at a world position while preserving its concrete component type.</summary>
    public T Spawn<T>(T prefab, Vector3 position) where T : PoolObj
    {
        T instance = this.SpawnObject(prefab, true) as T;
        if (instance != null) instance.transform.position = position;
        return instance;
    }

    /// <summary>Gets an instance without activating it so callers can configure it first.</summary>
    public T SpawnInactive<T>(T prefab, Vector3 position) where T : PoolObj
    {
        T instance = this.SpawnObject(prefab, false) as T;
        if (instance != null) instance.transform.position = position;
        return instance;
    }
}
