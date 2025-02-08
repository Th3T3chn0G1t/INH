using System.Diagnostics;
using Hazel;

namespace ImNotHungry
{
	public class Player : Entity
	{
		enum State
		{
			Undefined,
			Normal,
			Paused,
			UI
		}

		/// Properties
			public float Speed = 4.0f;
			public float MouseSensitivity = 10.0f;
		/// Properties

		/// Cache
			private RigidBodyComponent _rigidbody;
			private TransformComponent _transform;
			private TransformComponent _cameraTransform;
		/// Cache

		/// Movement
			private Vector2 _lastMousePosition;
		/// Movement

		/// State
			private State _state;
			private State _newState;
		/// State

		protected override void OnCreate()
		{
			_transform = GetComponent<TransformComponent>()!;
			_rigidbody = GetComponent<RigidBodyComponent>()!;

			_cameraTransform = Scene.FindEntityByTag("Camera")!.GetComponent<TransformComponent>()!;

			TransitionState(State.Normal);
		}

		protected override void OnDestroy()
		{
		}

		protected override void OnUpdate(float ts)
		{
			if(_state != _newState)
			{
				EnterState(_newState);
			}

			switch(_state)
			{
				case State.Normal:
				{
					UpdateNormal(ts);
					break;
				}

				case State.Paused:
				{
					UpdatePaused(ts);
					break;
				}

				case State.UI:
				{
					UpdateUI(ts);
					break;
				}
			}
		}

		private void TransitionState(State state)
		{
			_newState = state;
		}

		private void EnterState(State state)
		{
			_state = state;

			Log.Debug("Entering state '" + state + "'");

			switch(state)
			{
				case State.Normal:
				{
					Input.SetCursorMode(CursorMode.Locked);
					break;
				}

				case State.Paused:
				case State.UI:
				{
					Input.SetCursorMode(CursorMode.Normal);
					break;
				}
			}

			_lastMousePosition = Input.GetMousePosition();
		}

		private void UpdateNormal(float ts)
		{
			// State Transitions
			{
				if(Input.IsKeyPressed(KeyCode.Escape))
				{
					TransitionState(State.Paused);
				}
			}

			// Rotation
			{
				var current = Input.GetMousePosition();
				var delta = _lastMousePosition - current;
				var rotation = new Vector3(delta.Y, delta.X, 0.0f) * MouseSensitivity * ts;

				_cameraTransform.Rotation += rotation;

				_lastMousePosition = current;
			}

			// Movement
			{
				var movement = Vector3.Zero;

				if(Input.IsKeyDown(KeyCode.W))
				{
					movement.Z = 1.0f;
				}
				else if(Input.IsKeyDown(KeyCode.S))
				{
					movement.Z = -1.0f;
				}

				if(Input.IsKeyDown(KeyCode.A))
				{
					movement.X = -1.0f;
				}
				else if(Input.IsKeyDown(KeyCode.D))
				{
					movement.X = 1.0f;
				}

				movement.Normalize();
				movement *= Speed * ts;

				var wt = _cameraTransform.WorldTransform;
				var old = _rigidbody.LinearVelocity;
				var velocity = wt.Forward * movement.Z + wt.Right * movement.X;

				_rigidbody.LinearVelocity = new Vector3(velocity.X, old.Y, velocity.Z);
			}
		}

		private void UpdatePaused(float ts)
		{
			if(Input.IsKeyPressed(KeyCode.Escape))
			{
				TransitionState(State.Normal);
			}
		}

		private void UpdateUI(float ts)
		{
			if(Input.IsKeyPressed(KeyCode.Escape))
			{
				TransitionState(State.Normal);
			}
		}
	}
}
