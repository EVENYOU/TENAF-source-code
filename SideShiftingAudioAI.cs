using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Copyright (c) 2026 Daniel Chalkarov (Evenyou Entertainment)
// Licensed under CC BY-NC 4.0
// This script implements a side-shifting AI that uses voice lines to tease the player 
// and attacks from either the left or right side using a secondary entity.

// Funtime Freddy and Bon-bon uses this script in The Endless Night at Freddy

[System.Serializable]
public struct VoiceLine
{
    public AudioClip clip;
    [TextArea] public string subtitleText;
}

public class SideShiftingAudioAI : MonoBehaviour
{
    public enum Side { Left, Right }

    [Header("Current Status")]
    public bool isEnabled = true;
    public Side currentSide = Side.Right;
    private bool _isAttacking = false;

    [Header("Difficulty Settings")]
    public float minWaitTime = 12f;
    public float maxWaitTime = 15f;
    [Tooltip("How much time the player has to close the door after the attack starts")]
    public float attackReactionTime = 3.5f;
    [Tooltip("Delay while the voice line plays before the attack actually begins")]
    public float voiceLineDelay = 3.7f;

    [Header("Voice Lines & Audio")]
    public AudioSource audioSource;
    public AudioClip footstepLeftToRight;
    public AudioClip footstepRightToLeft;
    public AudioClip knockSound;
    public AudioClip jumpscareSound;
    
    [Space(10)]
    public VoiceLine[] introLines;
    public VoiceLine[] tauntLines; // Played when changing sides
    public VoiceLine[] attackLines; // Played when sending the companion

    [Header("Door System")]
    [Tooltip("Update these variables from your main door controller")]
    public bool isLeftDoorClosed;
    public bool isRightDoorClosed;

    [Header("Events")]
    public UnityEvent<string> OnSubtitleUpdate; // Passes the text to your UI manager
    public UnityEvent<Side> OnSideChanged; // Useful for updating UI icons (Left/Right)
    public UnityEvent OnDefended;
    public UnityEvent OnJumpscare;

    private float _attackTimer;
    private Coroutine _aiRoutine;

    private void Start()
    {
        if (isEnabled) StartCoroutine(IntroSequence());
    }

    private void Update()
    {
        if (!isEnabled || Time.timeScale == 0) return;

        if (_isAttacking)
        {
            _attackTimer -= Time.deltaTime;

            if (_attackTimer <= 0)
            {
                CheckDefense();
            }
        }
    }

    private IEnumerator IntroSequence()
    {
        yield return new WaitForSeconds(0.5f);
        
        // Example of moving to starting position
        audioSource.PlayOneShot(footstepRightToLeft);
        currentSide = Side.Right;
        OnSideChanged?.Invoke(currentSide);

        // Play random intro line
        if (introLines.Length > 0)
        {
            VoiceLine intro = introLines[Random.Range(0, introLines.Length)];
            PlayVoiceLine(intro);
            yield return new WaitForSeconds(voiceLineDelay);
            ClearSubtitle();
        }

        StartAI();
    }

    public void StartAI()
    {
        if (_aiRoutine != null) StopCoroutine(_aiRoutine);
        _aiRoutine = StartCoroutine(MechanicRoutine());
    }

    public void StopAI()
    {
        isEnabled = false;
        _isAttacking = false;
        if (_aiRoutine != null) StopCoroutine(_aiRoutine);
        StopAllCoroutines();
    }

    private IEnumerator MechanicRoutine()
    {
        while (isEnabled)
        {
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);

            // 50% chance to change side, 50% chance to attack
            int actionChoice = Random.Range(0, 2); 

            if (actionChoice == 0)
            {
                yield return StartCoroutine(ChangeSideRoutine());
            }
            else
            {
                yield return StartCoroutine(AttackRoutine());
            }
        }
    }

    private IEnumerator ChangeSideRoutine()
    {
        // Swap sides
        currentSide = (currentSide == Side.Right) ? Side.Left : Side.Right;
        
        AudioClip stepSound = (currentSide == Side.Right) ? footstepLeftToRight : footstepRightToLeft;
        if (stepSound) audioSource.PlayOneShot(stepSound);
        
        OnSideChanged?.Invoke(currentSide);

        // Play random taunt
        if (tauntLines.Length > 0)
        {
            VoiceLine taunt = tauntLines[Random.Range(0, tauntLines.Length)];
            PlayVoiceLine(taunt);
        }

        yield return new WaitForSeconds(voiceLineDelay);
        ClearSubtitle();
    }

    private IEnumerator AttackRoutine()
    {
        // Play attack command
        if (attackLines.Length > 0)
        {
            VoiceLine attackTaunt = attackLines[Random.Range(0, attackLines.Length)];
            PlayVoiceLine(attackTaunt);
        }

        yield return new WaitForSeconds(voiceLineDelay);
        ClearSubtitle();

        // Start attack timer
        _isAttacking = true;
        _attackTimer = attackReactionTime;
    }

    private void CheckDefense()
    {
        _isAttacking = false;

        bool isSafe = (currentSide == Side.Right && isRightDoorClosed) || 
                      (currentSide == Side.Left && isLeftDoorClosed);

        if (isSafe)
        {
            OnDefendSuccess();
        }
        else
        {
            TriggerJumpscare();
        }
    }

    private void OnDefendSuccess()
    {
        if (knockSound) audioSource.PlayOneShot(knockSound);
        OnDefended?.Invoke();
        
        // Reset and go back to main loop
        StartAI(); 
    }

    private void TriggerJumpscare()
    {
        isEnabled = false;
        if (audioSource && jumpscareSound) audioSource.PlayOneShot(jumpscareSound);
        OnJumpscare?.Invoke();
        Debug.Log("AI: Companion Jumpscare Triggered!");
    }

    private void PlayVoiceLine(VoiceLine line)
    {
        if (line.clip) audioSource.PlayOneShot(line.clip);
        OnSubtitleUpdate?.Invoke(line.subtitleText);
    }

    private void ClearSubtitle()
    {
        OnSubtitleUpdate?.Invoke("");
    }
}
