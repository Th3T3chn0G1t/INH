using System;
using Hazel;

using Coral.Managed.Interop;

namespace HazelQA 
{
	public class FPSPlayer : Entity
	{
		public float WalkingSpeed = 4.0f;
		public float RunSpeed = 8.0f;
		public float JumpForce = 3.0f;

		public Prefab Missile;

		public Entity[] EntityArray;

		[NonSerialized]
		public float MouseSensitivity = 10.0f;

		private bool m_Colliding = false;
		public float CurrentSpeed { get; private set; }

		private RigidBodyComponent m_RigidBody;
		private TransformComponent m_Transform;
		private TransformComponent m_CameraTransform;

		private Entity m_CameraEntity;

		private Vector2 m_LastMousePosition;

		private float m_CurrentYMovement = 0.0f;

		private Vector2 m_MovementDirection = new Vector2(0.0F);
		private bool m_ShouldJump = false;

		private Vector3 m_PreviousPosition;
		float m_DistanceTraveled = 0.0f;
		public int PlayFootstepSounds = 0;
		public float FootstepLength_m = 1.4f;
		public float FootstepsVolume = 0.22f;

		private RaycastData m_RaycastData;

		private Entity m_ProjectileParent;

		private readonly AudioCommandID m_PlayShotSoundGraphSoundID = new AudioCommandID("play_soundgraph");
		private readonly AudioCommandID m_PlayShotSoundID = new AudioCommandID("play_nonsoundgraph");

		


		private void PlayFootstepSound()
		{
			float rv = Hazel.Random.Float() * 0.06f;
			float rp = Hazel.Random.Float() * 0.3f;
			//int idx = rand.Next(m_FootstepSounds.Length);
			//Audio.PlaySound2D(m_FootstepSounds[idx], FootstepsVolume - rv, 1.0f - rp);

			var audioObj = Audio.CreateAudioEntity("Player_Footsteps", Transform.LocalTransform);
			Audio.PostEvent("play_footstep", audioObj.ID);
			//Audio.ReleaseAudioObject(audioObj);
			

			m_DistanceTraveled = 0.0f;
		}

		//Trigger checks
		private bool m_ParentingTrigger;
		private bool m_ParentingScaleTrigger;
		private bool m_ParentingRotationTrigger;
		private bool m_AssigningRigidBodyTrigger;
		private bool m_RotationChangeTrigger;
		private bool m_ScriptInvadersLockFlagsTrigger;
		private bool m_ScriptInvadersMissileRotationTrigger;

        private void TriggerCheck(Entity other)
        {
			
			if(other.Tag == "ParentingTest_Trigger")
				m_ParentingTrigger = true;
			if(other.Tag == "ParentingScaleTest_Trigger")
				m_ParentingScaleTrigger = true;
			if(other.Tag == "ParentingRotationTest_Trigger")
				m_ParentingRotationTrigger = true;
			if(other.Tag == "AssigningRigidBodyTest_Trigger")
				m_AssigningRigidBodyTrigger = true;
			if(other.Tag == "RotationChangeThroughScriptTest_Trigger")
				m_RotationChangeTrigger = true;
			if(other.Tag == "ScriptInvaders_LockFlagsTest_Trigger")
				m_ScriptInvadersLockFlagsTrigger = true;
			if(other.Tag == "ScriptInvaders_MissileRotationTest_Trigger")
				m_ScriptInvadersMissileRotationTrigger = true;
        }
        private void TriggerCheckEnd(Entity other)
        {
            if(other.Tag == "ParentingTest_Trigger")
				m_ParentingTrigger = false;
			if(other.Tag == "ParentingScaleTest_Trigger")
				m_ParentingScaleTrigger = false;
			if(other.Tag == "ParentingRotationTest_Trigger")
				m_ParentingRotationTrigger = false;
			if(other.Tag == "AssigningRigidBodyTest_Trigger")
				m_AssigningRigidBodyTrigger = false;
			if(other.Tag == "RotationChangeThroughScriptTest_Trigger")
				m_RotationChangeTrigger = false;
			if(other.Tag == "ScriptInvaders_LockFlagsTest_Trigger")
				m_ScriptInvadersLockFlagsTrigger = false;
			if(other.Tag == "ScriptInvaders_MissileRotationTest_Trigger")
				m_ScriptInvadersMissileRotationTrigger = false;
        }


        protected override void OnCreate()
		{
			m_Transform = GetComponent<TransformComponent>();
			m_RigidBody = GetComponent<RigidBodyComponent>();

			CurrentSpeed = WalkingSpeed;

			CollisionBeginEvent += (n) => { m_Colliding = true; };
			CollisionEndEvent += (n) => { m_Colliding = false; };

			m_CameraEntity = Scene.FindEntityByTag("Camera");
			m_CameraTransform = m_CameraEntity.GetComponent<TransformComponent>();

			m_LastMousePosition = Input.GetMousePosition();

			Input.SetCursorMode(CursorMode.Locked);

			// Setup footsteps
			//GetFoostepSounds();
			m_PreviousPosition = Transform.Translation;
			FootstepLength_m = Math.Max(0.0f, FootstepLength_m);
			if (FootstepsVolume < 0.0f)
				FootstepsVolume = 0.22f;
			else
				FootstepsVolume = Math.Min(1.0f, FootstepsVolume);

			m_RaycastData = new RaycastData();
			m_RaycastData.MaxDistance = 50.0f;
			m_RaycastData.RequiredComponents = new[] { typeof(MeshComponent) };

			m_ProjectileParent = Scene.FindEntityByTag("ProjectileHolder");
			TriggerBeginEvent += TriggerCheck;
			TriggerEndEvent += TriggerCheckEnd;

			Log.Warn("Test");
		}

		protected override void OnDestroy()
		{
		}

		protected override void OnUpdate(float ts)
		{
			if (Input.IsKeyPressed(KeyCode.Escape) && Input.GetCursorMode() == CursorMode.Locked)
				Input.SetCursorMode(CursorMode.Normal);

			if (Input.IsMouseButtonPressed(MouseButton.Left) && Input.GetCursorMode() == CursorMode.Normal)
            {
				Input.SetCursorMode(CursorMode.Locked);
				m_LastMousePosition = Input.GetMousePosition();
            }

			CurrentSpeed = Input.IsKeyPressed(KeyCode.LeftControl) ? RunSpeed : WalkingSpeed;

			UpdateRaycasting();
			UpdateMovementInput();
			UpdateRotation(ts);
			UpdateInteraction();

            // Play footstep sounds
            m_DistanceTraveled += Transform.Translation.Distance(m_PreviousPosition);
			m_PreviousPosition = Transform.Translation;
			if (m_DistanceTraveled >= FootstepLength_m)
			{
				// Making sure we're touching the ground
				if (m_Colliding && PlayFootstepSounds == 1)
				{
					PlayFootstepSound();
				}
			}
		}

        private void UpdateInteraction()
        {
			if(m_ParentingTrigger)
            {
				Entity cubeToBeParented, parentCube0, parentCube1, parentCubeIndicator;
				cubeToBeParented = FindEntityByTag("ParentingTest_Cube");
				parentCube0 = FindEntityByTag("ParentingTest_Parent0");
				parentCube1 = FindEntityByTag("ParentingTest_Parent1");
				parentCubeIndicator = FindEntityByTag("ParentingTest_ParentIndicator");
				
				if(Input.IsKeyPressed(KeyCode.E))
                {
					if(cubeToBeParented.Parent == null || cubeToBeParented.Parent.Tag == parentCube1.Tag)
					{
						cubeToBeParented.Parent = parentCube0;
						parentCubeIndicator.Translation = new Vector3(parentCube0.Translation.X, parentCubeIndicator.Translation.Y, parentCube0.Translation.Z);
					}
					else
					{
						cubeToBeParented.Parent = parentCube1;
						parentCubeIndicator.Translation = new Vector3(parentCube1.Translation.X, parentCubeIndicator.Translation.Y, parentCube1.Translation.Z);
					}
                }
            }
			if(m_ParentingScaleTrigger)
            {
				Entity cubeToBeParented, parentCube0, parentCube1, parentCubeIndicator;
				cubeToBeParented = FindEntityByTag("ParentingScaleTest_Cube");
				parentCube0 = FindEntityByTag("ParentingScaleTest_Parent0");
				parentCube1 = FindEntityByTag("ParentingScaleTest_Parent1");
				parentCubeIndicator = FindEntityByTag("ParentingScaleTest_ParentIndicator");
				
				if(Input.IsKeyPressed(KeyCode.E))
                {
					if(cubeToBeParented.Parent == null || cubeToBeParented.Parent.Tag == parentCube1.Tag)
					{
						cubeToBeParented.Parent = parentCube0;
						parentCubeIndicator.Translation = new Vector3(parentCube0.Translation.X, parentCubeIndicator.Translation.Y, parentCube0.Translation.Z);
					}
					else
					{
						cubeToBeParented.Parent = parentCube1;
						parentCubeIndicator.Translation = new Vector3(parentCube1.Translation.X, parentCubeIndicator.Translation.Y, parentCube1.Translation.Z);
					}
                }
            }
			if(m_ParentingRotationTrigger)
            {
				Entity cubeToBeParented, parentCube0, parentCube1, parentCubeIndicator;
				cubeToBeParented = FindEntityByTag("ParentingRotationTest_Cube");
				parentCube0 = FindEntityByTag("ParentingRotationTest_Parent0");
				parentCube1 = FindEntityByTag("ParentingRotationTest_Parent1");
				parentCubeIndicator = FindEntityByTag("ParentingRotationTest_ParentIndicator");
				
				if(Input.IsKeyPressed(KeyCode.E))
                {
					if(cubeToBeParented.Parent == null || cubeToBeParented.Parent.Tag == parentCube1.Tag)
					{
						cubeToBeParented.Parent = parentCube0;
						parentCubeIndicator.Translation = new Vector3(parentCube0.Translation.X, parentCubeIndicator.Translation.Y, parentCube0.Translation.Z);
					}
					else
					{
						cubeToBeParented.Parent = parentCube1;
						parentCubeIndicator.Translation = new Vector3(parentCube1.Translation.X, parentCubeIndicator.Translation.Y, parentCube1.Translation.Z);
					}
                }
            }
			if(m_AssigningRigidBodyTrigger)
            {
				Entity cube = FindEntityByTag("AssigningRigidBodyTest_Cube");
				
				if(Input.IsKeyPressed(KeyCode.D1))
                {
					cube.CreateComponent<RigidBodyComponent>();
					
                }
				if(Input.IsKeyPressed(KeyCode.D2))
                {
					cube.RemoveComponent<RigidBodyComponent>();
                }
				if(Input.IsKeyPressed(KeyCode.D3))
                {
					cube.CreateComponent<BoxColliderComponent>();
                }
				if(Input.IsKeyPressed(KeyCode.D4))
                {
					cube.RemoveComponent<BoxColliderComponent>();
                }
				if(Input.IsKeyPressed(KeyCode.E))
                {
					//Reset
					cube.RemoveComponent<RigidBodyComponent>();
					cube.RemoveComponent<BoxColliderComponent>();
					cube.Translation = new Vector3(-0.4f, 3.0f, -10.5f);
                }
            }
			if(m_RotationChangeTrigger)
            {
				Entity cube = FindEntityByTag("RotationChangeThroughScriptTest_Cube");
				if(Input.IsKeyPressed(KeyCode.D1))
                {
					cube.Rotation = new Vector3(cube.Rotation.X + Mathf.PI / 2.0f, cube.Rotation.Y, cube.Rotation.Z);
                }
				if(Input.IsKeyPressed(KeyCode.D2))
                {
					cube.Rotation = new Vector3(cube.Rotation.X, cube.Rotation.Y + Mathf.PI / 2.0f, cube.Rotation.Z);
                }
				if(Input.IsKeyPressed(KeyCode.D3))
                {
					cube.Rotation = new Vector3(cube.Rotation.X, cube.Rotation.Y, cube.Rotation.Z  + Mathf.PI / 2.0f);
                }
				if(Input.IsKeyPressed(KeyCode.E))
                {
					cube.Rotation = Vector3.Zero;
                }
            }
			if(m_ScriptInvadersLockFlagsTrigger)
            {
				//(NOTE)TIM: Maybe make code dependant if need be
				//Entity bulletSpawner = FindEntityByTag("");
            }
			if(m_ScriptInvadersMissileRotationTrigger)
            {
				if(Input.IsKeyPressed(KeyCode.E))
                {
					Vector3 translation = FindEntityByTag("ScriptInvaders_Player").Translation;
					Instantiate(Missile, translation);
                }
            }
        }

		private float GetMovementAxis(int axis, float deadzone = 0.2f)
        {
            float value = Input.GetControllerAxis(0, axis);
            return Mathf.Abs(value) < deadzone ? 0.0f : value;
        }

		private float GetHorizontalMovementAxis(float deadzone = 0.2f)
        {
            // Dpad
            byte hat = Input.GetControllerHat(0, 0);
			if ((hat & 2) != 0)
				return 1.0f;
            if ((hat & 8) != 0)
                return -1.0f;

			// Analogue stick
			return GetMovementAxis(0, deadzone);
		}

        private float GetVerticalMovementAxis(float deadzone = 0.2f)
        {
			// Dpad
            byte hat = Input.GetControllerHat(0, 0);
            if ((hat & 4) != 0)
                return 1.0f;
            if ((hat & 1) != 0)
                return -1.0f;

            // Analogue stick
            return GetMovementAxis(1, deadzone);
		}

        private float GetTriggerAxis(float deadzone = 0.2f)
        {
            return GetMovementAxis(5, deadzone);
        }

        private void UpdateMovementInput()
		{
			m_MovementDirection.X = 0.0f;
			m_MovementDirection.Y = 0.0f;

			m_MovementDirection.X = GetHorizontalMovementAxis();
			m_MovementDirection.Y = -GetVerticalMovementAxis();

			if (Input.IsKeyDown(KeyCode.W))
				m_MovementDirection.Y = 1.0f;
			else if (Input.IsKeyDown(KeyCode.S))
				m_MovementDirection.Y = -1.0f;

			if (Input.IsKeyDown(KeyCode.A))
				m_MovementDirection.X = -1.0f;
			else if (Input.IsKeyDown(KeyCode.D))
				m_MovementDirection.X = 1.0f;

			m_ShouldJump = (Input.IsKeyPressed(KeyCode.Space) || Input.IsControllerButtonPressed(0, 0)) && !m_ShouldJump;
		}

		SceneQueryHit[] colliders = new SceneQueryHit[10];

		private void UpdateRaycasting()
		{
			SceneQueryHit hitInfo;
			m_RaycastData.Origin = m_CameraTransform.Translation + m_CameraTransform.WorldTransform.Forward * 2.0f;
			m_RaycastData.Direction = m_CameraTransform.WorldTransform.Forward;

			if (Input.IsKeyPressed(KeyCode.H) && Hazel.Physics.CastRay(m_RaycastData, out hitInfo))
			{
				Entity entity = hitInfo.Entity;
				TagComponent tag = entity.GetComponent<TagComponent>();
				MeshComponent mesh = entity.GetComponent<MeshComponent>();

				Log.Info("Raycast hit: {0}", tag.Tag);
				mesh.Mesh.BaseMaterial.AlbedoColor = new Vector3(1.0f, 0.0f, 0.0f);
			}

			if (Input.IsKeyPressed(KeyCode.L))
			{
				// NOTE: The NonAlloc version of Overlap functions should be used when possible since it doesn't allocate a new array
				//			whenever you call it. The normal versions allocates a brand new array every time.

				ShapeQueryData shapeQueryData = new ShapeQueryData();
				shapeQueryData.Origin = m_Transform.Translation;
				shapeQueryData.ShapeData = new Hazel.BoxShape(new Vector3(1.0f));

				int numHits = Hazel.Physics.OverlapShape(shapeQueryData, out colliders);

				Log.Info("Colliders: {0}", numHits);

				// When using NonAlloc it's not safe to use a for each loop since some of the colliders may not exist
				for (int i = 0; i < numHits; i++)
					Log.Info(colliders[i]);
			}
		}

		protected override void OnPhysicsUpdate(float fixedTimeStep)
		{
			UpdateMovement();
		}

		private void UpdateRotation(float ts)
		{
			if (Input.GetCursorMode() != CursorMode.Locked)
				return;
			
			
			{
				float sensitivity = 3.0f;

				float hAxis = -GetMovementAxis(2);
				float vAxis = -GetMovementAxis(3);
				Vector2 delta = new Vector2(hAxis * hAxis, vAxis * vAxis);
				delta *= new Vector2(Math.Sign(hAxis), Math.Sign(vAxis));
				m_CurrentYMovement = delta.X * sensitivity;
				float xRotation = delta.Y * sensitivity * ts;

				if (xRotation != 0.0f)
					m_CameraTransform.Rotation += new Vector3(xRotation, 0.0f, 0.0f);

				m_CameraTransform.Rotation = new Vector3(Mathf.Clamp(m_CameraTransform.Rotation.X * Mathf.Rad2Deg, -80.0f, 80.0f), 0.0f, 0.0f) * Mathf.Deg2Rad;
			}

            // Mouse
            {
                // TODO: Mouse position should be relative to the viewport
                Vector2 currentMousePosition = Input.GetMousePosition();
                Vector2 delta = m_LastMousePosition - currentMousePosition;
				if (delta.X != 0.0f)
					m_CurrentYMovement = delta.X * MouseSensitivity * ts;
				float xRotation = delta.Y * (MouseSensitivity * 0.05f) * ts;

				if (xRotation != 0.0f)
					m_CameraTransform.Rotation += new Vector3(xRotation, 0.0f, 0.0f);

				m_CameraTransform.Rotation = new Vector3(Mathf.Clamp(m_CameraTransform.Rotation.X * Mathf.Rad2Deg, -80.0f, 80.0f), 0.0f, 0.0f) * Mathf.Deg2Rad;
				m_LastMousePosition = currentMousePosition;
			}
		}

		private void UpdateMovement()
		{
            if (Input.GetCursorMode() != CursorMode.Locked)
                return;

			Rotation += m_CurrentYMovement * Vector3.Up * 0.05f;

			Vector3 movement = m_CameraTransform.WorldTransform.Right * m_MovementDirection.X + m_CameraTransform.WorldTransform.Forward * m_MovementDirection.Y;
			movement.Y = 0.0f;

			if (movement.Length() > 0.0f)
			{
				movement.Normalize();
				Vector3 velocity = movement * CurrentSpeed;
				velocity.Y = m_RigidBody.LinearVelocity.Y;
				m_RigidBody.LinearVelocity = velocity;
			}

			if (m_ShouldJump && m_Colliding)
			{
				m_RigidBody.AddForce(Vector3.Up * JumpForce, EForceMode.Impulse);
				m_ShouldJump = false;
			}
		}
	}
}
