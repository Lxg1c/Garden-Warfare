using UnityEngine;
using Photon.Pun;
using TMPro;

namespace UI.HUD.GameTimer
{
    public class GameTimer : MonoBehaviourPun, IPunObservable
    {
        public TMP_Text timerText;
        
        private float _currentTime;
        private bool _isTimerRunning;
        
        void Start()
        {
            _currentTime = 0f;
            UpdateTimerDisplay();
            
            if (PhotonNetwork.IsMasterClient)
            {
                _isTimerRunning = true;
            }
        }
        
        void Update()
        {
            if (_isTimerRunning && PhotonNetwork.IsMasterClient)
            {
                _currentTime += Time.deltaTime; 
                UpdateTimerDisplay();
            }
        }
        
        void UpdateTimerDisplay()
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(_currentTime / 60f);
                int seconds = Mathf.FloorToInt(_currentTime % 60f);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }
        
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(_currentTime);
                stream.SendNext(_isTimerRunning);
            }
            else
            {
                _currentTime = (float)stream.ReceiveNext();
                _isTimerRunning = (bool)stream.ReceiveNext();
                UpdateTimerDisplay();
            }
        }
    }
}