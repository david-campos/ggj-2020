using UnityEditorInternal;
using UnityEngine;

public class TimeCounter
{
    private static TimeCounter _sInstance = null;
    
    private float m_Start = -1f;
    private float m_End;

    private TimeCounter() {}

    public static TimeCounter GetInstance() {
        return _sInstance ?? (_sInstance = new TimeCounter());
    }

    public float Seconds => m_Start > 0
        ? (m_End > 0 ? m_End - m_Start : Time.time - m_Start)
        : 0f;

    public void StartCounting() {
        m_Start = Time.time;
    }

    public void StopCounting() {
        m_End = Time.time;
    }
}