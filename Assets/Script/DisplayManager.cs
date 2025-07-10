using UnityEngine;

public class DisplayManager : MonoBehaviour
{
    #region Singleton

    private static DisplayManager _instance;

    public static DisplayManager Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = FindObjectOfType<DisplayManager>();

            if (_instance == null)
                _instance = new GameObject("DisplayManager", typeof(DisplayManager)).GetComponent<DisplayManager>();

            return _instance;
        }
        private set => _instance = value;
    }

    #endregion

    public TextMesh timer;
    public TextMesh generation;

    private float _time;
    public bool timechk = true;

    private void Update()
    {
        if (timechk)
            _time += Time.deltaTime;

        timer.text = "진행 시간 : " + $"{_time:N2}" + " 초";
    }
}
