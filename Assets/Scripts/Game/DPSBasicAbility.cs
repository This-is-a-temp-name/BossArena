using BossArena.game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossArena.game 
{
    class DPSBasicAbility : TargetedAbilityBase, IDrawIndicator
    {
        //Referencing Tank class structure...

        // Need to have reference to Parent Player Prefab
        //[SerializeField]
        //private GameObject PlayerPrefab;

        // Blink Prefab??
        [SerializeField]
        private GameObject BlinkPrefab;
        private CircleCollider2D BlinkPrefabCollider;
        private SpriteRenderer BlinkPrefabSpriteRenderer;
        private ParticleSystem ps;
        private Rigidbody2D rb;
        private float horizVelocity;
        private float vertVelocity;

        private bool basicActivated = false;
        private bool withinTauntRange = false;

        Vector3 currentMousePosition;

        public override void ActivateAbility(Vector3? mousepos = null)
        {
            BlinkPrefabSpriteRenderer.enabled = false;
            if (onCoolDown)
                return;
            onCoolDown = true;
            timeStart = Time.time;
            ApplyEffect();
            BlinkPrefabCollider.enabled = true;
            BlinkPrefabCollider.enabled = false;
        }

        public override void ApplyEffect()
        {

            UnityEngine.Debug.Log("Blink ability");
            // since this is blink, this may just be apply the change of position
            parentPlayer.transform.position += new Vector3(horizVelocity * 3, vertVelocity * 3, 0);
            var psemit = ps.emission;
            psemit.enabled = true;
            ps.Play();
        }

        public void DrawAbilityIndicator(Vector3 targetLocation)
        {
           
        }


        // Start is called before the first frame update
        protected override void Start()
        {
            timeStart = Time.time;

            rb = parentPlayer.GetComponent<Rigidbody2D>();

        // Get Main Camera
        mainCamera = Camera.main;

            // Get Collider
            BlinkPrefabCollider = GetComponent<CircleCollider2D>();

            // Get SpriteRenderer
            BlinkPrefabSpriteRenderer = GetComponent<SpriteRenderer>();

            // Initially Disable
            BlinkPrefabCollider.enabled = false;
            BlinkPrefabSpriteRenderer.enabled = false;

            //Particles!
            ps = parentPlayer.GetComponent<ParticleSystem>();



            //BlinkPrefab.SetActive(false);

        }

        // Update is called once per frame
        protected override void Update()
        {
            // Every frame, check for cooldowns, set bool accordingly.
            checkCooldown();

        }


    }

}
