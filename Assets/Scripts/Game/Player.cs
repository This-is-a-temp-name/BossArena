﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace BossArena.game
{
    /// </summary>
    class Player : EntityBase, IFriendly, IThreat
    {

        public Animator anim;
        private Renderer rend;

        private ParticleSystem ps;
        private float horizVelocity;
        private float vertVelocity;
        private bool isAttacking;
        private bool isAbiliting;
        private bool isUlting;

        public int dodgeCooldown;

        [SerializeField]
        public NetworkVariable<Archetypes> Archetype = new NetworkVariable<Archetypes>();

        public Archetype m_Archetype;
        public AbilityBase BasicAttack;
        public AbilityBase BasicAbility;
        public AbilityBase UltimateAbility;

        //Since this isn't a monobehaviour, we can't simply use gameObject to reference the attached gameobject
        //So we kinda have to do this
        //(That or i'm just dumb lol)
        public GameObject playerObj;

        [SerializeField]
        private Material _DamageMaterial;
        [SerializeField]
        private Material _DefaultMaterial;
        private SpriteRenderer playerSpriteRenderer;

        //public Player(Archetype archetype) : base()
        //{
        //    Archetype = archetype;
        //}

        protected override void Start()
        {
            base.Start();
            //SetHealth(Archetype.MaxHealth);
            Debug.Log(playerObj);
            rb = playerObj.GetComponent<Rigidbody2D>();
            ps = playerObj.GetComponent<ParticleSystem>();
            dodgeCooldown = 0;
            m_Archetype = InGameRunner.Instance.ArchetypeDictionary.GetValueOrDefault(Archetype.Value);
            initAbilities(GetComponent<NetworkObject>().OwnerClientId);
            // Assign Renderer component to rend variable
            rend = GetComponent<Renderer>();
            // Change sprite color to selected color
            rend.material.color = m_Archetype.classColor;

            AddPlayerToGame();
        }

        protected void initAbilities(ulong clientId)
        {
            if (IsServer)
            {
                spawnAbilities(clientId);
            }


        }

        [ClientRpc]
        private void setAbilitiesClientRPC()
        {
            setAbilities();
        }

        private void setAbilities()
        {
            BasicAttack = transform.GetChild(0).GetComponent<AbilityBase>();
            BasicAbility = transform.GetChild(1).GetComponent<AbilityBase>();
            UltimateAbility = transform.GetChild(2).GetComponent<AbilityBase>();

            if (BasicAttack is TargetedAbilityBase)
            {
                ((TargetedAbilityBase) BasicAttack).SetParent(gameObject);
            }
            if (BasicAbility is TargetedAbilityBase)
            {
                ((TargetedAbilityBase) BasicAbility).SetParent(gameObject);
            }
            if (UltimateAbility is TargetedAbilityBase)
            {
                ((TargetedAbilityBase) UltimateAbility).SetParent(gameObject);
            }
        }

        private void spawnAbilities(ulong clientId)
        {
            GameObject basicAttack = (GameObject) Instantiate(m_Archetype.BasicAttack, transform.position, playerObj.transform.rotation);
            basicAttack.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
            basicAttack.transform.SetParent(transform, false);

            GameObject basicAbility = (GameObject) Instantiate(m_Archetype.BasicAbility, transform.position, playerObj.transform.rotation);
            basicAbility.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
            basicAbility.transform.SetParent(transform, false);

            GameObject ultimateAbility = (GameObject) Instantiate(m_Archetype.UltimateAbility, transform.position, playerObj.transform.rotation);
            ultimateAbility.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
            ultimateAbility.transform.SetParent(transform, false);
            setAbilitiesClientRPC();
        }

        protected override void Update()
        {
            if (!IsOwner)
                return;


            horizVelocity = Input.GetAxisRaw("Horizontal");
            vertVelocity = Input.GetAxisRaw("Vertical");
            anim.SetFloat("Horizontal", horizVelocity);
            anim.SetFloat("Vertical", vertVelocity);
            anim.SetBool("IsHMove", horizVelocity != 0);
            anim.SetBool("IsVMove", vertVelocity != 0);

            isAttacking = Input.GetAxisRaw("Fire1") != 0;
            isAbiliting = Input.GetAxisRaw("Fire2") != 0;
            isUlting = Input.GetAxisRaw("Fire1") != 0;

            if (BasicAttack is IDrawIndicator)
            {
                ((IDrawIndicator) BasicAttack).DrawAbilityIndicator(Input.mousePosition);
            }

            //Ability Section
            if (Input.GetButtonDown("Fire1"))
            {
                BasicAttack.ActivateAbility();
            }

            if (BasicAbility is IDrawIndicator && Input.GetButton("Fire2"))
            {
                ((IDrawIndicator) BasicAbility).DrawAbilityIndicator(Input.mousePosition);
            }

            if (Input.GetButtonUp("Fire2"))
            {
                BasicAbility.ActivateAbility(Input.mousePosition);
            }

            if (UltimateAbility is IDrawIndicator && Input.GetButton("Fire3"))
            {
                ((IDrawIndicator) UltimateAbility).DrawAbilityIndicator(Input.mousePosition);
            }

            if (Input.GetButtonUp("Fire3"))
            {
                UltimateAbility.ActivateAbility(Input.mousePosition);
            }

            //Make the player dash a short distance on spacebar press
            if (Input.GetKeyDown(KeyCode.Space) && dodgeCooldown < 1)
            {
                var psemit = ps.emission;
                psemit.enabled = true;
                ps.Play();
            }


        }

        [ServerRpc]
        private void SendClientInputServerRpc()
        {

        }

        protected override void FixedUpdate()
        {
            //Actually moving the player by changing their rigidbody velocity
            rb.velocity = (new Vector2(horizVelocity * currentMoveSpeed, vertVelocity * currentMoveSpeed));
            timerCheck();

            // Incremental decrease of player's threat level.
            if (ThreatLevel.Value < 0)
            {
                ThreatLevel.Value = 0;
            } else
            {
                // Decrease Threat Level
                ThreatLevel.Value -= 1;
            }
        }

        protected override void LateUpdate()
        {
            if (!IsOwner)
                return;

            //Make the player dash a short distance on spacebar press
            if (Input.GetKeyDown(KeyCode.Space) && dodgeCooldown < 1)
            {
                dash();
                dodgeCooldown = 90;
            }

        }

        void dash()
        {
            playerObj.transform.position += new Vector3(horizVelocity * 3, vertVelocity * 3, 0);
        }

        void timerCheck()
        {
            if (dodgeCooldown > 0)
            {
                dodgeCooldown--;
            }
        }

        void AddPlayerToGame()
        {
            //if (!IsHost)
            //{
            //    AddPlayerToGameServerRpc();
            //}
            InGameRunner.Instance.AddPlayer(gameObject);
        }

        [ServerRpc]
        void AddPlayerToGameServerRpc()
        {
            InGameRunner.Instance.AddPlayer(gameObject);
        }

        protected override void HandleCollision(Collision2D collision)
        {
            if (!IsOwner)
                return;
            var tempMonoArray = collision.gameObject.GetComponents<MonoBehaviour>();
            foreach (var monoBehaviour in tempMonoArray)
            {
                if (monoBehaviour is IHostile)
                {
                    Debug.Log($"{OwnerClientId}: Owie bad man touch me.");

                    // Collide with something that hursts me
                    playerSpriteRenderer.material = _DamageMaterial;
                    StartCoroutine(switchDefaultMaterial());

                    continue;
                }
                Debug.Log($"{OwnerClientId}: Huh? Must be the wind.");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void HitFriendlyServerRpc(ulong hitter)
        {
            Debug.Log($"{hitter}: Hiting friendly player {OwnerClientId}");
        }

        IEnumerator switchDefaultMaterial()
        {
            // Wait _ seconds before switching back to default material.
            yield return new WaitForSeconds(1);
            playerSpriteRenderer.material = _DefaultMaterial;
        }

    }


    }
