using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public Button quickMatchButton;
    public TMP_Text connectionStatusText;
    public GameObject connectingPanel;
    public GameObject settingsCanvas;

    private bool _isConnectedToMaster;
    private PlayerInputActions playerInputActions;

    private void Start()
    {
        // Показываем панель подключения, скрываем кнопку
        ShowConnectingState();
        
        UpdateStatus("Подключение к серверу...");
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.GameVersion = "1.0";
    }
    void Awake()
    {
        playerInputActions = new PlayerInputActions(); // Инициализируем наш Input Actions Asset

        // Подписываемся на событие нажатия кнопки Escape
        // Action Map "UI", Action "Escape"
        playerInputActions.UI.Escape.performed += ctx => OnEscapePressed();
    }
    private void OnEscapePressed()
    {
        // Проверяем, активен ли Canvas настроек, прежде чем закрывать его
        if (settingsCanvas != null && settingsCanvas.activeSelf)
        {
            OpenSettings();
        }
    }

    public void QuickMatch()
    {
        if (!_isConnectedToMaster)
        {
            UpdateStatus("Не подключено к серверу");
            return;
        }
        
        UpdateStatus("Поиск матча...");
        
        if (quickMatchButton != null)
            quickMatchButton.interactable = false;
            
        PhotonNetwork.JoinRandomRoom();
    }

    public void OpenSettings()
    {
        if (settingsCanvas != null)
        {
            settingsCanvas.SetActive(true);  // Активируем Canvas настроек
        }
        Debug.Log("Открыты настройки");
    }

    public override void OnConnectedToMaster()
    {
        _isConnectedToMaster = true;
        UpdateStatus("Подключено к серверу!");
        
        // Скрываем панель подключения, показываем кнопку
        ShowReadyState();
            
        PhotonNetwork.NickName = "Player_" + Random.Range(100, 999);
    }
    
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        UpdateStatus("Комнаты не найдены, создаем новую...");
        
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;
        
        string roomName = "Room_" + Random.Range(1000, 9999);
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }
    
    public override void OnJoinedRoom()
    {
        Debug.Log("Opening game scene");
        UpdateStatus($"Вошли в комнату: {PhotonNetwork.CurrentRoom.Name}");
        PhotonNetwork.LoadLevel("Game");
        Debug.Log("Открыта сцена GameScene");
    }
    
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        UpdateStatus($"Ошибка создания комнаты: {message}");
        
        if (quickMatchButton != null)
            quickMatchButton.interactable = true;
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        _isConnectedToMaster = false;
        UpdateStatus($"Отключено: {cause}");
        
        // Показываем панель подключения при разрыве соединения
        ShowConnectingState();
            
        // Автопереподключение
        PhotonNetwork.ConnectUsingSettings();
    }

    private void UpdateStatus(string status)
    {
        Debug.Log($"MenuManager: {status}");
        
        if (connectionStatusText != null)
            connectionStatusText.text = status;
    }
    
    // Показываем состояние "Подключение к серверу"
    private void ShowConnectingState()
    {
        if (connectingPanel != null)
            connectingPanel.SetActive(true);
            
        if (quickMatchButton != null)
        {
            quickMatchButton.gameObject.SetActive(false);
            quickMatchButton.interactable = false;
        }
    }
    
    // Показываем состояние "Готов к игре"
    private void ShowReadyState()
    {
        if (connectingPanel != null)
            connectingPanel.SetActive(false);
            
        if (quickMatchButton != null)
        {
            quickMatchButton.gameObject.SetActive(true);
            quickMatchButton.interactable = true;
        }
    }
    public void QuitApplication()
    {
        Application.Quit();
    }
}