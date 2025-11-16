using Photon.Pun;


namespace Core.Settings
{
    public class GameManager : MonoBehaviourPunCallbacks
    {
        void Start()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                GameTimer.Instance.StartTimer();
            }
        }
    }
}
