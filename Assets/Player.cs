using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // External tunables.
    static public float m_fMaxSpeed = 0.10f;
    public float m_fSlowSpeed = m_fMaxSpeed * 0.66f;
    public float m_fIncSpeed = 0.0025f;
    public float m_fMagnitudeFast = 0.6f;
    public float m_fMagnitudeSlow = 0.06f;
    public float m_fFastRotateSpeed = 0.2f;
    public float m_fFastRotateMax = 10.0f;
    public float m_fDiveTime = 0.3f;
    public float m_fDiveRecoveryTime = 0.5f;
    public float m_fDiveDistance = 3.0f;

    // Internal variables.
    public Vector3 m_vDiveStartPos;
    public Vector3 m_vDiveEndPos;
    public float m_fAngle;
    public float m_fSpeed;
    public float m_fTargetSpeed;
    public float m_fTargetAngle;
    public eState m_nState;
    public float m_fDiveStartTime;

    public enum eState : int
    {
        kMoveSlow,
        kMoveFast,
        kDiving,
        kRecovering,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
    {
        new Color(0,     0,   0),
        new Color(255, 255, 255),
        new Color(0,     0, 255),
        new Color(0,   255,   0),
    };

    public bool IsDiving()
    {
        return (m_nState == eState.kDiving);
    }

    void CheckForDive()
    {
        if (Input.GetMouseButton(0) && (m_nState != eState.kDiving && m_nState != eState.kRecovering))
        {
            // Start the dive operation
            m_nState = eState.kDiving;
            m_fSpeed = 0.0f;

            // Store starting parameters.
            m_vDiveStartPos = transform.position;
            m_vDiveEndPos = m_vDiveStartPos - (transform.right * m_fDiveDistance);
            m_fDiveStartTime = Time.time;
        }
    }

    void Start()
    {
        // Initialize variables.
        m_fAngle = 0;
        m_fSpeed = 0;
        m_nState = eState.kMoveSlow;
    }

    void UpdateDirectionAndSpeed()
    {
        // Get relative positions between the mouse and player
        Vector3 vScreenPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 vScreenSize = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
        Vector2 vOffset = new Vector2(transform.position.x - vScreenPos.x, transform.position.y - vScreenPos.y);

        // Find the target angle being requested.
        m_fTargetAngle = Mathf.Atan2(vOffset.y, vOffset.x) * Mathf.Rad2Deg;

        // Calculate how far away from the player the mouse is.
        float fMouseMagnitude = vOffset.magnitude / vScreenSize.magnitude;

        // Based on distance, calculate the speed the player is requesting.
        if (fMouseMagnitude > m_fMagnitudeFast)
        {
            m_fTargetSpeed = m_fMaxSpeed;
        }
        else if (fMouseMagnitude > m_fMagnitudeSlow)
        {
            m_fTargetSpeed = m_fSlowSpeed;
        }
        else
        {
            m_fTargetSpeed = 0.0f;
        }
    }

    void FixedUpdate()
    {
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
    }

    void Update()
    {
        switch (m_nState)
        {
            case eState.kMoveSlow:
                UpdateDirectionAndSpeed();

                // Flip angle so player moves toward mouse
                m_fAngle = (m_fTargetAngle + 180f) % 360f;

                // Speed up
                m_fSpeed = Mathf.Min(m_fSpeed + m_fIncSpeed, m_fTargetSpeed);

                // Transition to fast if moving fast enough
                if (m_fSpeed >= m_fMaxSpeed * 0.8f)
                {
                    m_nState = eState.kMoveFast;
                }

                //  Allow diving ONLY in slow mode
                CheckForDive();
                break;

            case eState.kMoveFast:
                UpdateDirectionAndSpeed();

                float correctedTargetAngle = (m_fTargetAngle + 180f) % 360f;
                float angleDelta = Mathf.DeltaAngle(m_fAngle, correctedTargetAngle);

                //  Sharp turn = instant lockout
                if (Mathf.Abs(angleDelta) > m_fFastRotateMax)
                {
                    // Force recovery state — no diving, no turning
                    m_nState = eState.kRecovering;
                    m_fDiveStartTime = Time.time;  // Reuse dive timer for cooldown
                    return;
                }

                //  Otherwise, allow gentle turning and maintain speed
                m_fAngle += Mathf.Sign(angleDelta) * Mathf.Min(m_fFastRotateSpeed, Mathf.Abs(angleDelta));
                m_fSpeed = Mathf.Min(m_fSpeed + m_fIncSpeed, m_fTargetSpeed);

                // Do not leave fast mode early just because mouse is close!
                // Only leave if player slows down naturally
                if (m_fSpeed < m_fMaxSpeed * 0.5f)
                {
                    m_nState = eState.kMoveSlow;
                }

                break;



            case eState.kDiving:
                float diveProgress = (Time.time - m_fDiveStartTime) / m_fDiveTime;
                if (diveProgress < 1.0f)
                {
                    transform.position = Vector3.Lerp(m_vDiveStartPos, m_vDiveEndPos, diveProgress);
                }
                else
                {
                    m_nState = eState.kRecovering;
                    m_fDiveStartTime = Time.time;
                }
                return;

            case eState.kRecovering:
                // Wait for cooldown to finish
                if ((Time.time - m_fDiveStartTime) > m_fDiveRecoveryTime)
                {
                    m_nState = eState.kMoveSlow;
                }
                return;
        }

        // Apply movement
        float rad = m_fAngle * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
        transform.position += dir * m_fSpeed;

        // Clamp to screen
        Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);
        viewPos.x = Mathf.Clamp01(viewPos.x);
        viewPos.y = Mathf.Clamp01(viewPos.y);
        transform.position = Camera.main.ViewportToWorldPoint(viewPos);

        // Rotate to match movement
        transform.rotation = Quaternion.Euler(0, 0, m_fAngle + 180f);
    }

}
