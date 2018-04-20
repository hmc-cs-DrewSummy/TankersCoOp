﻿using UnityEngine;
using System.Collections;
using TeamUtility.IO;

namespace Completed
{
    public class TankPlayer : Tank
    {
        PlayerID PID;
        public int m_PlayerNumber = 1;              // Used to identify which tank belongs to which player. This is set by this tank's manager.
        public TankPlayer teammate = null;

        private Vector3 m_TargetDirection;          // The direction the tank points toward for driving.
        private float m_DriveVerticalValue;         // The vertical component of the drive direction
        private float m_DriveHorizontalValue;       // The horizontal component of the drive direction
        private string m_DriveVerticalName;         // The name of the input axis for moving forward and back.
        private string m_DriveHorizontalName;       // The name of the input axis for turning.
        private string m_AimHorizontalName;         // The name of the vector for aiming.
        private string m_AimVerticalName;           // The name of the vector for aiming.
        private float m_AimHorizontalValue;         // The value of the vector for aiming.
        private float m_AimVerticalValue;           // The value of the vector for aiming.
        private string m_FireName;                  // The name of the float for shooting.
        private float m_FireValue;                  // The value of the float for the trigger.
        private string m_LockName;                  // The name of the bool for locking the tower.
        private bool m_LockValue;                   // The value of the bool for locking the tower.
        private string m_PauseName;                 // The name of the bool for pausing.
        private bool m_PauseValue;                  // The value of the bool for the pause.
        private bool m_SelectValue;
        private string m_SelectName;                // The name of the bool for pausing.
        private bool m_HasShot;                     // The boolean used to permit the tank to shoot once per trigger pull.
        private bool canFire = true;
        private float fireDelay = .04f;
        private bool paused;                        // The boolean for if the game is paused.
        private float joystickMagnitude;            // The magnitude of the joystick for moving.
        public GUI_MiniMap miniMapGUI;
        public GUI_Pause pauseGUI;

        public int killAmount = 0;                  // The number of kills by this tank, kept track of for GUI_Pause.
        public int[] killCounter;                   // An array of number of kills for each type of tank.
        public int deathCounter = 0;                // The number of times this tank has died.
        private bool aimOnly = true;                // Boolean for whether the tank is enabled.
        public GameObject currentRoom;
        private int maxTimeApart = 3;               // Max amount of time players can be seperated.
        private bool startedTimer = false;
        private Vector3 center = new Vector3(11.5f, 0, 11.5f);    // Distance to center of room for teleporting.
        public bool battling = false;               // Boolean indicating the tank is battling; needed for relocating.
        public LevelManager LM;


        new void Awake()
        {
            base.Awake();
            // This needs to be called in awake so that it is instantiated earlier than GUI_MiniMap.
            //TODO: this needs to be set by the person playing the game
            tankColor = tankColors[0];
            //ColorizeTank();
        }

        protected new void OnEnable()
        {
            base.OnEnable();

            // Also reset the input values.
            m_AimHorizontalValue = 0f;
            m_AimVerticalValue = 0f;
            m_DriveHorizontalValue = 0f;
            m_DriveVerticalValue = 0f;
            m_FireValue = 0f;
            m_LockValue = false;
            m_PauseValue = false;
            m_SelectValue = false;

            canFire = true;
        }

        protected new void Start()
        {
            base.Start();

            // Set the player ID.
            PID = TeamUtility.IO.PlayerID.One;
            if (m_PlayerNumber == 2)
            {
                PID = TeamUtility.IO.PlayerID.Two;
            }

            // The axes names are based on player number.
            m_AimHorizontalName = "Right Stick Vertical";
            m_AimVerticalName = "Right Stick Horizontal";
            m_DriveHorizontalName = "Left Stick Horizontal";
            m_DriveVerticalName = "Left Stick Vertical";
            m_FireName = "Right Trigger";
            m_LockName = "Left Trigger";
            m_PauseName = "Start";
            m_SelectName = "Back";

            // Load in the projectile being used from the Resources folder in assets.
            //projectile = Resources.Load("TankResources/Projectile/ShellPlayer") as GameObject;

            // Tanks hasn't shot yet. This is used to allow semi-auto shooting.
            m_HasShot = false;

            paused = false;

            // Make the size of the killCounter the amount of colors.
            killCounter = new int[tankColors.Length];

            // Store the original pitch of the audio source.
            //m_OriginalPitch = m_MovementAudio.pitch;

            // Load in the Audio files.
            m_FireAudioSource = gameObject.GetComponents<AudioSource>()[0];
            //m_MovementAudio = gameObject.GetComponents<AudioSource>()[1];
        }

        private void Update()
        {
            if (alive)
            {
                TakeControllerInputs();
            }

            // Give audio to the movement of the car.
            //EngineAudio();

            // Pausing; must be called in Update because of timescale manipulation.
            Pause();

            //TODO: correct room locations of players.
            if (!battling && teammate != null && m_PlayerNumber == 1)
            {
                checkTeammateRoom();
            }

            updateLocation();
        }

        private void TakeControllerInputs()
        {
            // Store the value of the input axes while exculding deadzones.
            // Get magnitude of joysticks inputs.
            joystickMagnitude = Mathf.Pow(InputManager.GetAxis(m_DriveVerticalName, PID), 2) + Mathf.Pow(InputManager.GetAxis(m_DriveHorizontalName, PID), 2);
            float joystickMagnitude2 = Mathf.Pow(InputManager.GetAxis(m_AimVerticalName, PID), 2) + Mathf.Pow(InputManager.GetAxis(m_AimHorizontalName, PID), 2);

            // Create a deadzone so that small values are discarded.
            // Driving.
            if (joystickMagnitude < .1)
            {
                // This is in the deadzone.
                m_DriveHorizontalValue = 0;
                m_DriveVerticalValue = 0;
            }
            else
            {
                // Get the horizontal and vertical components.
                m_DriveHorizontalValue = -InputManager.GetAxis(m_DriveHorizontalName, PID);
                m_DriveVerticalValue = -InputManager.GetAxis(m_DriveVerticalName, PID);
            }
            // Aiming.
            if (joystickMagnitude2 < .1)
            {
                // This is in the deadzone.
            }
            else
            {
                // Get the horizontal and vertical components.
                m_AimHorizontalValue = -InputManager.GetAxis(m_AimHorizontalName, PID);
                m_AimVerticalValue = -InputManager.GetAxis(m_AimVerticalName, PID);
            }

            // Store the value for firing.
            m_FireValue = InputManager.GetAxis(m_FireName, PID);

            // Store the value for firing.
            m_LockValue = InputManager.GetAxis(m_LockName, PID) == 1;

            // Store the value for pauseing.
            m_PauseValue = InputManager.GetButtonDown(m_PauseName, PID);

            // Store the value for selecting.
            m_SelectValue = InputManager.GetButtonDown(m_SelectName, PID);
        }

        private void FixedUpdate()
        {
            // Adjust the rigidbody's position and orientation in FixedUpdate.
            if (!aimOnly && alive)
            {
                Move();
                Turn();
            }

            // Adjust the rotation of tower in FixedUpdate.
            Aim();

            // Shoot bullets from the tower in FixedUpdate if m_FireValue exceeds .9.
            if (!aimOnly)
            {
                Shoot();
            }

            // Toggle the map mode.
            Select();
        }


        private void Move()
        {
            // Keep track of the direction the joystick is pointed toward.
            Vector3 m_TargetDirection = new Vector3(-m_DriveHorizontalValue, 0, m_DriveVerticalValue);

            // Keep track of the direction the tank is pointed toward.
            m_CurrentDirection = -body.eulerAngles;

            // trueAngle is used because CurrentDirection has a value like (0,-270,0) where it measures the angle from rotating around the y axis.
            // angleTargetToDirection gets a float value to later check if the tank is facing the direction the joystick is pressed.
            Vector3 trueAngle = new Vector3(-Mathf.Sin(m_CurrentDirection.y * Mathf.PI / 180), 0, Mathf.Cos(m_CurrentDirection.y * Mathf.PI / 180));

            // If the tank isn't facing the direction the joystick is pointing, the speed equals 0.
            float speed = 0;
            if (Vector3.Angle(trueAngle, m_TargetDirection) < 5)
            {
                speed = m_Speed;
            }
            else if (175 < Vector3.Angle(trueAngle, m_TargetDirection))
            {
                speed = -m_Speed;
            }

            Vector3 movement = body.forward * speed;
            m_RidgidbodyTank.velocity = movement;

            // Update the velocity.
            velocity = -body.forward * speed;
        }


        private void Turn()
        {
            // moves very slightly and the body doesn't follow. This would cause the speed to be 0 from calculate speed.
            // Keep track of the direction the joystick is pointed toward.
            Vector3 m_TargetDirection = new Vector3(m_DriveHorizontalValue, 0, -m_DriveVerticalValue);

            float step = m_RotateSpeed * Time.deltaTime;
            Vector3 newDir = Vector3.RotateTowards(body.forward, m_TargetDirection, step, .01F);

            if (newDir == Vector3.zero)
            {
                // Don't rotate at all
            }
            // Turn forward or backward depending on which is closer.
            else if (Vector3.Angle(body.forward, m_TargetDirection) < 90)
            {
                // Rotate towards forwards.
                body.rotation = Quaternion.LookRotation(newDir);
            }
            else
            {
                // Rotate towards backwards.
                newDir = Vector3.RotateTowards(body.forward, -m_TargetDirection, step, .01F);
                body.rotation = Quaternion.LookRotation(newDir);
            }
        }


        private void Aim()
        {

            // If the tower isn't locked, keep track of the m_AimRotation for aiming.
            if (!m_LockValue)
            {
                Vector3 m_AimRotation = new Vector3(-m_AimVerticalValue, 0, m_AimHorizontalValue);
                tower.LookAt(tower.position + m_AimRotation);
            }
        }


        protected new void Fire()
        {
            if (canFire)
            {
                base.Fire();
                StartCoroutine(DelayFire());
            }
        }
        private IEnumerator DelayFire()
        {
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            GetComponent<Rigidbody>().freezeRotation = true;
            canFire = false;
            aimOnly = true;
            yield return new WaitForSeconds(fireDelay);
            canFire = true;
            aimOnly = false;
        }


        private void Shoot()
        {
            if (m_FireValue == 1)
            {
                if (m_HasShot == false)
                {
                    // If there are any projectiles left, call Fire.
                    if (projectileCount > 0)
                    {
                        Fire();

                        GameObject.FindGameObjectWithTag("HUD").GetComponent<GUI_HUD>().UpdateProjectiles();

                        // Tank shot.
                        m_HasShot = true;
                    }
                    else
                    {
                        EmptyFire();

                        // Tank shot.
                        m_HasShot = true;
                    }
                }
            }
            else
            {
                m_HasShot = false;
            }



            //TODO: https://www.youtube.com/watch?v=J9ErQDWR44k
        }

        private void Select()
        {
            if (m_SelectValue)
            {
                miniMapGUI.MapAndUnmap();
            }
        }

        private void Pause()
        {
            if (!paused)
            {
                if (m_PauseValue)
                {
                    pauseGUI.Pause(PID);

                    paused = true;
                }
            }
            else
            {
                if (m_PauseValue)
                {
                    pauseGUI.Unpause();

                    paused = false;
                }
            }
        }

        public override void DestroyTank()
        {
            if (alive)
            {
                // Freeze the tank from moving.
                m_RidgidbodyTank.velocity = Vector3.zero;
                m_RidgidbodyTank.freezeRotation = true;

                // Give projectiles to the room's projectileHolder.
                TransferProjectiles();

                // Immaterialize the tank.
                for (int i = 0; i < GetComponentsInChildren<MeshRenderer>().Length; i++)
                {
                    GetComponentsInChildren<MeshRenderer>()[i].enabled = false;
                }
                hitbox.enabled = false;

                // Set alive to be false. Other scripts depend on this.
                alive = false;

                // Set projectile count to 0.
                projectileCount = 0;
                GameObject.FindGameObjectWithTag("HUD").GetComponent<GUI_HUD>().UpdateProjectiles();

                // Update the death counter.
                deathCounter++;

                // Move tank downward to avoid getting hit again.
                transform.position += new Vector3(0, -10, 0);

                // Decrease LevelManager's player counter.
                LM.playerDied();
            }
        }


        private void EngineAudio()
        //TODO

        {
            // If there is a small input for driving, play the idle audio clip.
            if (Mathf.Abs(joystickMagnitude) < 0.1f)
            {
                m_MovementAudio.clip = m_IdleAudio;
                if (m_MovementAudio.clip == m_IdleAudio)
                {
                    // TODO: Figure out another audio to play different from the driving one.
                    //m_MovementAudio.Play();
                }
            }
            else
            {
                m_MovementAudio.clip = m_IdleAudio;
                // Otherwise play this soon to be different audio clip.
                m_MovementAudio.Play();

                //TODO: may need another if else case inside both these cases to switch m_MovementAudio.clip
            }
        }

        public Vector3 SendPosition()
        {
            return this.transform.position;
        }

        public void updateKillTracker(Material material)
        {
            // Find which material is being updated from enemyTankColors and increment the respective killTracker.
            for (int m = 0; m < tankColors.Length; m++)
            {
                if (tankColors[m] == material)
                {
                    killCounter[m]++;
                }
            }
        }

        public int getProjectileAmount()
        {
            return projectileAmount;
        }

        public int getProjectileCount()
        {
            return projectileCount;
        }

        // Used by RoomManager to prevent tanks from moving or shooting at the beginning of a battle.
        public void rotateOnly(bool b)
        {
            aimOnly = b;
        }

        // Used by RoomManager to prevent tanks from shooting at the end of a battle.
        public void disableShoot(bool b)
        {
            if (b)
            {
                projectileCount = 0;
                aimOnly = true; //TODO: maybe this should be different like a new variable enabled (keeps tank from aiming as well)
            }
            else
            {
                projectileCount = projectileAmount;
                aimOnly = false;
            }
            GameObject.FindGameObjectWithTag("HUD").GetComponent<GUI_HUD>().UpdateProjectiles();
        }

        // Relocates player2 to player1's room if they are apart for too long.
        private void checkTeammateRoom()
        {
            SetRoom();
            if (teammate.currentRoom != currentRoom && !startedTimer)
            {
                startedTimer = true;
                Debug.Log("start timer");
                StartCoroutine(relocate());
            }
        }

        // Changes the current room occupied by the tank and updates the minimap if necessary.
        public void SetRoom()
        {
            GameObject room = LM.DeterminePlayerRoom(transform.position);
            if (currentRoom != room)
            {
                currentRoom = room;
                miniMapGUI.visitedRoom(LM.DeterminePlayerCoord(transform.position));
            }
        }

        private void updateLocation()
        {
            GameObject room = LM.DeterminePlayerRoom(transform.position);
            currentRoom = room;
        }

        // Helper function for checkTeammateRoom.
        private IEnumerator relocate()
        {
            // Start a timer and if the players are seperated after maxTimeApart, relocate player2.
            int timer = 0;
            while (teammate.currentRoom != currentRoom && timer < 3)
            {
                //TODO: show some type of counter, maybe sound or light blinking under tank
                yield return new WaitForSeconds(1);
                timer++;
            }
            if (teammate.currentRoom != currentRoom && timer == maxTimeApart)
            {
                // Relocate.
                teammate.transform.position = currentRoom.transform.position + center;
                teammate.currentRoom = currentRoom;
            }
            startedTimer = false;
        }

        // Respawns the player.
        public void respawn()
        {
            // Materialize the tank.
            for (int i = 0; i < GetComponentsInChildren<MeshRenderer>().Length; i++)
            {
                GetComponentsInChildren<MeshRenderer>()[i].enabled = true;
            }
            body.GetComponent<BoxCollider>().enabled = true;

            alive = true;
            // Place in the center of the room the player was in.
            transform.position = teammate.currentRoom.transform.position + center;
            currentRoom = teammate.currentRoom;

            // Set projectile count to projectileAmount.
            projectileCount = projectileAmount;
            GameObject.FindGameObjectWithTag("HUD").GetComponent<GUI_HUD>().UpdateProjectiles();
        }
    }
}