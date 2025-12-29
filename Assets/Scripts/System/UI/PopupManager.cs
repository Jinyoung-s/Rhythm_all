using UnityEngine;
using UnityEngine.UIElements;
using System;

public class PopupManager : MonoBehaviour
{
    private static PopupManager _instance;
    public static PopupManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("PopupManager");
                _instance = go.AddComponent<PopupManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private UIDocument _uiDocument;
    private VisualElement _popupRoot;
    private Label _titleLabel;
    private Label _messageLabel;
    private Button _confirmButton;
    private Button _cancelButton;

    private Action _onConfirm;
    private Action _onCancel;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeUI();
    }

    private void InitializeUI()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            _uiDocument = gameObject.AddComponent<UIDocument>();
            
            // 1. Try specific Resource paths
            _uiDocument.panelSettings = Resources.Load<PanelSettings>("UI/Common/PopupPanelSettings") 
                                       ?? Resources.Load<PanelSettings>("UI Toolkit/PanelSettings");
            
            // 2. Fallback: Search for ANY PanelSettings in the project (might find if already loaded)
            if (_uiDocument.panelSettings == null)
            {
                PanelSettings[] allSettings = Resources.FindObjectsOfTypeAll<PanelSettings>();
                if (allSettings != null && allSettings.Length > 0)
                {
                    _uiDocument.panelSettings = allSettings[0];
                    Debug.Log($"[PopupManager] Fallback: Used PanelSettings '{allSettings[0].name}' found in project.");
                }
            }
                                       
            _uiDocument.visualTreeAsset = Resources.Load<VisualTreeAsset>("UI/Common/CommonPopup");
            
            // 💡 Ensure popup is always on top
            _uiDocument.sortingOrder = 999; 
            
            if (_uiDocument.panelSettings == null) 
            {
                Debug.LogError("[PopupManager] ❌ PanelSettings NOT FOUND! Popup will not render. " + 
                               "Please place a PanelSettings asset in a 'Resources' folder or assign it to the PopupManager object.");
            }
            if (_uiDocument.visualTreeAsset == null) Debug.LogError("[PopupManager] ❌ VisualTreeAsset 'CommonPopup' not found in Resources/UI/Common/");
        }

        var root = _uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("[PopupManager] Root VisualElement is null! Rendering might have failed.");
            return;
        }

        _popupRoot = root.Q<VisualElement>("PopupRoot");
        _titleLabel = root.Q<Label>("Title");
        _messageLabel = root.Q<Label>("Message");
        _confirmButton = root.Q<Button>("ConfirmButton");
        _cancelButton = root.Q<Button>("CancelButton");

        if (_popupRoot == null) Debug.LogError("[PopupManager] 'PopupRoot' not found in UXML!");

        _confirmButton.clicked += OnConfirmClicked;
        _cancelButton.clicked += OnCancelClicked;

        Debug.Log("[PopupManager] UI Initialized Successfully");
        Hide();
    }

    public void ShowPopup(string title, string message, string confirmText = "Confirm", Action onConfirm = null, string cancelText = null, Action onCancel = null)
    {
        Debug.Log($"[PopupManager] ShowPopup called: {title}");
        if (_popupRoot == null) 
        {
            Debug.Log("[PopupManager] popupRoot is null, initializing...");
            InitializeUI();
        }

        if (_popupRoot == null)
        {
            Debug.LogError("[PopupManager] Failed to show popup: popupRoot still null after initialization.");
            return;
        }

        _titleLabel.text = title;
        _messageLabel.text = message;
        _confirmButton.text = confirmText;
        _onConfirm = onConfirm;
        _onCancel = onCancel;

        if (string.IsNullOrEmpty(cancelText))
        {
            _cancelButton.AddToClassList("hidden");
        }
        else
        {
            _cancelButton.RemoveFromClassList("hidden");
            _cancelButton.text = cancelText;
        }

        _popupRoot.RemoveFromClassList("hidden");
        _popupRoot.style.display = DisplayStyle.Flex; // 💡 Force display flex just in case
        Debug.Log("[PopupManager] Popup visibility set to visible.");
    }

    public void Hide()
    {
        if (_popupRoot != null)
        {
            _popupRoot.AddToClassList("hidden");
            _popupRoot.style.display = DisplayStyle.None; // 💡 Force display none
            Debug.Log("[PopupManager] Popup hidden.");
        }
    }

    private void OnConfirmClicked()
    {
        Hide();
        _onConfirm?.Invoke();
    }

    private void OnCancelClicked()
    {
        Hide();
        _onCancel?.Invoke();
    }
}
