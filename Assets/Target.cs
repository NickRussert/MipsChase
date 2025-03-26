using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public Player m_player;
    public enum eState : int
    {
        kIdle,
        kHopStart,
        kHop,
        kCaught,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
   {
        new Color(255, 0,   0),
        new Color(0,   255, 0),
        new Color(0,   0,   255),
        new Color(255, 255, 255)
   };

    // External tunables.
    public float m_fHopTime = 0.2f;
    public float m_fHopSpeed = 6.5f;
    public float m_fScaredDistance = 3.0f;
    public int m_nMaxMoveAttempts = 50;

    // Internal variables.
    public eState m_nState;
    public float m_fHopStart;
    public Vector3 m_vHopStartPos;
    public Vector3 m_vHopEndPos;

    void Start()
    {
        m_nState = eState.kIdle;
        m_player = FindFirstObjectByType<Player>();
    }


    void FixedUpdate()
    {
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        // Check if this is the player (in this situation it should be!)
        if (collision.gameObject == GameObject.Find("Player"))
        {
            // If the player is diving, it's a catch!
            if (m_player.IsDiving())
            {
                m_nState = eState.kCaught;
                transform.parent = m_player.transform;
                transform.localPosition = new Vector3(0.0f, -0.5f, 0.0f);
            }
        }
    }

    void Update()
    {
        switch (m_nState)
        {
            case eState.kIdle:
                float distToPlayer = Vector3.Distance(transform.position, m_player.transform.position);
                if (distToPlayer < m_fScaredDistance)
                {
                    m_nState = eState.kHopStart;
                }
                break;

            case eState.kHopStart:
                m_fHopStart = Time.time;
                m_vHopStartPos = transform.position;

                // Pick a direction mostly away from the player
                Vector3 awayFromPlayer = (transform.position - m_player.transform.position).normalized;

                // Try up to N times to find a valid hop destination
                for (int i = 0; i < m_nMaxMoveAttempts; ++i)
                {
                    // Randomly vary the direction a bit
                    Vector2 offset = Random.insideUnitCircle * 0.5f;
                    Vector3 dir = (awayFromPlayer + (Vector3)offset).normalized;
                    Vector3 candidate = transform.position + dir * m_fHopSpeed * m_fHopTime;

                    Vector3 screenPoint = Camera.main.WorldToViewportPoint(candidate);
                    if (screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1)
                    {
                        m_vHopEndPos = candidate;
                        break;
                    }
                }

                m_nState = eState.kHop;
                break;

            case eState.kHop:
                float hopProgress = (Time.time - m_fHopStart) / m_fHopTime;
                if (hopProgress < 1.0f)
                {
                    transform.position = Vector3.Lerp(m_vHopStartPos, m_vHopEndPos, hopProgress);
                }
                else
                {
                    m_nState = eState.kIdle;
                }
                break;

            case eState.kCaught:
                // Stay attached to player
                break;
        }
    }


}