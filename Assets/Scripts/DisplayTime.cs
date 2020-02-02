using System.Globalization;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class DisplayTime : MonoBehaviour
{
    private TextMeshProUGUI m_Text;

    private void Start() {
        m_Text = GetComponent<TextMeshProUGUI>();
    }

    void Update() {
        var seconds = TimeCounter.GetInstance().Seconds;
        int minutes = Mathf.FloorToInt(seconds / 60);
        seconds -= minutes * 60;
        seconds = Mathf.Round(seconds * 100) / 100;
        m_Text.SetText(
            "Time afloat: "
            + minutes + "' "
            + seconds.ToString("0.00", CultureInfo.InvariantCulture) + "\""
        );
    }
}