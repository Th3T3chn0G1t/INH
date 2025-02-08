using System;
using Hazel;

namespace INH;

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

		public float AnchorForce = 1.0f;
		public float InteractDistance = 4.0f;
		public float AnchorMax = 4.0f;
		public float HoldLinearDrag = 1.0f;
		public float HoldAngularDrag = 1.0f;

		public Entity Camera;	
		public Entity Anchor;	
	/// Properties

	/// Cache
		private RigidBodyComponent _rigidbody;
		private TransformComponent _transform;

		private TransformComponent _cameraTransform;
		private TransformComponent _anchorTransform;
	/// Cache

	/// Input
		private Vector2 _lastMousePosition;
	/// Input

	/// Interaction
		private RaycastData _interactRaycastData;

		struct HeldObject
		{
			public RigidBodyComponent Rigidbody;
			public TransformComponent Transform;

			public float LinearDrag;
			public float AngularDrag;
			public uint Layer;
			// public bool Gravity;
		}

		private HeldObject? _heldObject;

		/// Debug
			private Vector3 _lastInteractPosition = Vector3.Zero;
			private Vector3 _lastInteractDirection = Vector3.Up;
		/// Debug
	/// Interaction

	/// State
		private State _state;
		private State _newState;
	/// State

	protected override void OnCreate()
	{
		_transform = GetComponent<TransformComponent>()!;
		_rigidbody = GetComponent<RigidBodyComponent>()!;

		_cameraTransform = Camera.GetComponent<TransformComponent>()!;
		_anchorTransform = Anchor.GetComponent<TransformComponent>()!;

		_interactRaycastData = new()
		{
			MaxDistance = InteractDistance,
			RequiredComponents = new[] { typeof(StaticMeshComponent) },
			ExcludedEntities = new[] { ID }
		};

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
				//Input.SetCursorMode(CursorMode.Locked);
				break;
			}

			case State.Paused:
			case State.UI:
			{
				//Input.SetCursorMode(CursorMode.Normal);
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

		// Interaction
		{
			DebugRenderer.DrawLine(_lastInteractPosition, _lastInteractDirection * InteractDistance, Color.Green);

			if(Input.IsMouseButtonPressed(MouseButton.Left))
			{
				if(_heldObject != null)
				{
					Drop();
				}
				else
				{
					var wt = _cameraTransform.WorldTransform;

					_lastInteractPosition = _interactRaycastData.Origin = wt.Position;
					_lastInteractDirection = _interactRaycastData.Direction = wt.Forward;

					if(Physics.CastRay(_interactRaycastData, out var hit))
					{
						Interact(hit);
					}
				}
			}

			if(_heldObject != null)
			{
				var delta = _anchorTransform.WorldTransform.Position - _heldObject.Value.Transform.WorldTransform.Position;

				if(delta.Length() > AnchorMax)
				{
					Drop();
				}
				else
				{
					_heldObject.Value.Rigidbody.AddForce(delta * AnchorForce * ts);
				}
			}
		}
	}

	private void Drop()
	{
		_heldObject.Value.Rigidbody.LinearDrag = _heldObject.Value.LinearDrag;
		_heldObject.Value.Rigidbody.AngularDrag = _heldObject.Value.AngularDrag;
		_heldObject.Value.Rigidbody.Layer = _heldObject.Value.Layer;

		_heldObject = null;
	}

	private void Interact(SceneQueryHit hit)
	{
		if(hit.Entity.ID == 0)
		{
			Log.Error("Raycast hit entity was invalid?");
			return;
		}

		Log.Debug($"Interacting with entity '{hit.Entity.Tag}'");

		var script = hit.Entity.GetComponent<ScriptComponent>();
		if(script != null && script.Instance.Get() is Interactable)
		{
			var interactable = (Interactable) script.Instance.Get()!;
			
			interactable.Activate();
			
			return;
		}

		var rigidbody = hit.Entity.GetComponent<RigidBodyComponent>();
		if(rigidbody != null && rigidbody.BodyType == EBodyType.Dynamic)
		{
			// TODO: API to control whether an object experiences gravity.
			_heldObject = new()
			{
				Rigidbody = rigidbody,
				Transform = hit.Entity.GetComponent<TransformComponent>()!,
				
				LinearDrag = rigidbody.LinearDrag,
				AngularDrag = rigidbody.AngularDrag,

				Layer = rigidbody.Layer
			};

			rigidbody.LinearDrag = HoldLinearDrag;
			rigidbody.AngularDrag = HoldAngularDrag;
			rigidbody.LayerName = "Player";

			return;
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
