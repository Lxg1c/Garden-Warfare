using System.Collections.Generic;
using UnityEngine;

public class SpawnPointGroup : MonoBehaviour
{
    public List<NeutralAI> spawnedNeutrals = new List<NeutralAI>();
    
    public void RegisterNeutral(NeutralAI neutral)
    {
        if (!spawnedNeutrals.Contains(neutral))
        {
            spawnedNeutrals.Add(neutral);
            neutral.SetSpawnGroup(this);
        }
    }
    
    public void UnregisterNeutral(NeutralAI neutral)
    {
        spawnedNeutrals.Remove(neutral);
    }
    
    public void NotifyGroupAggro(Transform aggroSource, Transform target)
    {
        foreach (NeutralAI neutral in spawnedNeutrals)
        {
            if (neutral != null && 
                neutral.gameObject.activeInHierarchy && 
                neutral != aggroSource.GetComponent<NeutralAI>()) // Не уведомляем того кто вызвал
            {
                neutral.OnGroupAggroTriggered(aggroSource, target);
            }
        }
    }
}