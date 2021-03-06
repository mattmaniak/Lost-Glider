﻿using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
class Player : MonoBehaviour
{
    const float initialAltitude = 2.0f;
    const float maxFallingSpeed = 0.1f; // Per one second.
    const float maxSpeed = 4.0f;
    const float maxPositionX = 1000.0f;

    public bool Alive { get; set; } = true;
    public float Speed { get; private set; }

    bool Movement
    {
        set {Speed = value ? maxSpeed : 0.0f; }
    }
    
    bool InSoaringLift
    {
        get { return LiftRatio != 0.0f; }
    }

    float Altitude
    {
        get { return transform.position.y; }
    }
    
    float LiftRatio { get; set; }

    void Awake()
    {
        transform.Translate(-Camera.main.transform.localPosition.x,
            initialAltitude, 0.0f);
    }

    void FixedUpdate()
    {
        var rigidbody = GetComponent<Rigidbody2D>();

        if (InSoaringLift)
        {
            transform.Translate(Vector2.up * LiftRatio * Time.deltaTime);
        }
        if (transform.position.x >= maxPositionX)
        {
            KillPlayer();
        }
        if ((rigidbody.velocity.y < 0.0f)
            && (rigidbody.velocity.magnitude > maxFallingSpeed))
        {
            rigidbody.velocity = rigidbody.velocity.normalized
                * maxFallingSpeed;
        }
        transform.Translate(Vector2.right * Speed * Time.deltaTime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.isTrigger)
        {
            KillPlayer();
        }
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.name.Contains("AtmosphericPhenomenon"))
        {
            var phenomenon = collider.gameObject.
                GetComponent<Level.AtmosphericPhenomenonConfig>();

            LiftRatio = phenomenon.LiftRatio;

            // TODO: DOESN'T WORK.
            Speed += phenomenon.DirectionalSpeed.x;
        }
    }

    void OnTriggerExit2D()
    {
        LiftRatio = 0.0f;
        Speed = maxSpeed;
    }

    void Update()
    {
        CheckPause();
    }

    void CheckPause()
    {
        if (Alive)
        {
            Movement = !FindObjectOfType<Menus.PauseMenuController>().Paused;
        }
    }

    void KillPlayer()
    {
        Movement = Alive = false;
        LiftRatio = 0.0f;
    }
}
