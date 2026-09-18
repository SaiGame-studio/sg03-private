using UnityEngine;

/// <summary>Returns a pooled world-space stat UI through the shared <see cref="ObjectPool"/>.</summary>
public class WorldSpaceStatUiDespawn : Despawn<PoolObj>
{
    protected override void Reset()
    {
        base.Reset();
        this.SetDefaultDespawnByTime();
    }

    private void SetDefaultDespawnByTime()
    {
        this.isDespawnByTime = false;
    }

    public override void DoDespawn()
    {
        if (this.spawner == null)
        {
            this.LoadSpawner();
        }

        if (this.spawner != null)
        {
            this.spawner.Despawn(this.parent);
            return;
        }

        if (this.parent != null)
        {
            this.parent.gameObject.SetActive(false);
        }
    }
}
