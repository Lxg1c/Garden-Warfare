using UnityEngine;
using Photon.Pun;

public class GameTimer : MonoBehaviourPunCallbacks
{
    public static GameTimer Instance;

    public double GameTime { get; private set; }

    private double _startTime;
    private bool _running;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (_running == false) return;
        GameTime = PhotonNetwork.Time - _startTime;
    }

    // вызывается только у хоста
    public void StartTimer()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        _startTime = PhotonNetwork.Time;

        // сохраняем время старта для ВСЕХ, включая будущих игроков
        photonView.RPC("RPC_SetStartTime", RpcTarget.AllBuffered, _startTime);

        _running = true;
    }

    [PunRPC]
    private void RPC_SetStartTime(double start)
    {
        _startTime = start;
        _running = true;
    }
}