using UnityEngine;

public class AttackTrigger : MonoBehaviour
{
    private NeutralAI _owner;

    private void Awake()
    {
        _owner = GetComponentInParent<NeutralAI>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_owner.playerTag))
            _owner.OnEnterAttackRange(other.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(_owner.playerTag))
            _owner.OnExitAttackRange(other.transform);
    }
}