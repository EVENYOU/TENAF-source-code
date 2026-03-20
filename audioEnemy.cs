using UnityEngine;
using System.Collections;
using UnityEngine.Events;

// Copyright (c) 2026 Daniel Chalkarov (Evenyou Entertainment)
// Licensed under CC BY-NC 4.0
// This script implements a "sound-based" enemy movement using stereo panning.
// The player must listen to which side the music is louder to react.

// In TENAF, Ballora uses this script.

public class StereoAudioEnemyAI : MonoBehaviour
{
    private enum State { Waiting, Approaching, PreAttack, AtDoor, Retreating }
    
    [Header("Current Status")]
    [SerializeField] private State currentState = State.Waiting;
    public bool isEnabled = true;

    [Header("Audio Settings")]
    public AudioSource musicSource; 
    public float idleVolume = 0.05f;
    public float maxVolume = 1.0f;
    public float panLerpSpeed = 1.5f; 

    [Header("Movement AI Settings")]
    public float approachSpeed = 0.02f; 
    public float reactionTime = 2.0f; // Time player has to close the door
    public float waitTimeMin = 10f;
    public float waitTimeMax = 20f;
    
    [Header("Door System")]
    [Tooltip("Manually set these via your door script or Unity Events")]
    public bool isLeftDoorClosed;
    public bool isRightDoorClosed;

    [Header("Events")]
    public UnityEvent OnJumpscare;
    public UnityEvent OnRetreat; // Triggered when player successfully repels the enemy

    private float _distance = 1.0f; 
    private float _targetPan = 0f; 
    private float _currentPan = 0f;
    private float _timer;

    void Start()
    {
        if (musicSource != null)
        {
            musicSource.spatialBlend = 0; // Force 2D mode for clean Stereo Panning
        }
        ResetAI();
    }

    void Update()
    {
        if (!isEnabled || Time.timeScale == 0) return;

        // Smoothly interpolate the audio pan (Left to Right)
        _currentPan = Mathf.Lerp(_currentPan, _targetPan, Time.deltaTime * panLerpSpeed);
        musicSource.panStereo = _currentPan;

        // State Machine logic
        switch (currentState)
        {
            case State.Waiting: UpdateWaiting(); break;
            case State.Approaching: UpdateApproaching(); break;
            case State.PreAttack: UpdatePreAttack(); break;
            case State.AtDoor: UpdateAtDoor(); break;
            case State.Retreating: UpdateRetreating(); break;
        }
    }

    private void UpdateWaiting()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            currentState = State.Approaching;
            _distance = 1.0f;
            _targetPan = 0f; 
            musicSource.volume = idleVolume;
            musicSource.Play();
        }
    }

    private void UpdateApproaching()
    {
        _distance -= approachSpeed * Time.deltaTime;
        musicSource.volume = Mathf.Lerp(maxVolume * 0.5f, idleVolume, 1f - _distance);

        if (_distance <= 0.4f) 
        {
            // Randomly choose a side (Left or Right)
            _targetPan = Random.value > 0.5f ? -1f : 1f;
            currentState = State.PreAttack;
        }
    }

    private void UpdatePreAttack()
    {
        _distance -= approachSpeed * Time.deltaTime;
        musicSource.volume = Mathf.Lerp(maxVolume * 0.8f, idleVolume, 1f - _distance);

        if (_distance <= 0.05f) 
        {
            currentState = State.AtDoor;
            musicSource.volume = maxVolume; 
            _timer = 0; 
        }
    }

    private void UpdateAtDoor()
    {
        bool doorClosed = (_targetPan < 0) ? isLeftDoorClosed : isRightDoorClosed;

        if (!doorClosed)
        {
            _timer += Time.deltaTime;
            if (_timer > reactionTime) TriggerJumpscare();
        }
        else
        {
            OnRetreat?.Invoke();
            currentState = State.Retreating;
        }
    }

    private void UpdateRetreating()
    {
        musicSource.volume -= Time.deltaTime * 0.8f;
        if (musicSource.volume <= 0) ResetAI();
    }

    public void ResetAI()
    {
        musicSource.Stop();
        musicSource.volume = 0;
        _currentPan = 0;
        _targetPan = 0;
        _distance = 1.0f;
        _timer = Random.Range(waitTimeMin, waitTimeMax);
        currentState = State.Waiting;
    }

    private void TriggerJumpscare()
    {
        isEnabled = false;
        OnJumpscare?.Invoke();
        Debug.Log("AI: Stereo Enemy Jumpscare Triggered!");
    }
}
