﻿using UnityEngine;
using System.Collections;
using InControl;

public enum ControlPriority
{
		Ghost,
		Hunter,
		NPC
}

public class CharacterMotor : MonoBehaviour
{

		[SerializeField]
		private Animator
				_animator;
		[SerializeField]
		private float
				_animationScale = 1f;
		[SerializeField]
		private float
				_confusionDuration = 3f;
		[SerializeField]
		private float
				_reactionTime = 0.2f;

		[SerializeField]
		private float
				_movementSpeed = 7f;
		public float MovementSpeed {
				get { return _movementSpeed; }
		}
		[SerializeField]
		private float
				_rotationSpeed = 10f;
		[SerializeField]
		private float
				_rotationSensitivity = 0.5f;

		[SerializeField]
		private float
				_ghostMovementSensitivity = 0.5f;

		private GhostController _ghostController;
		private HunterController _hunterController;
		private CurrRoomFinder _currRoomFinder;

		private Vector3 _ghostInputVector;
		private Vector3 _hunterInputVector;
		private Vector3 _standardInputVector;
		private Rigidbody _rigidbody;
		private Transform _transform;
		private bool _paused = true;

		private bool _catchHappened = false;

		public bool IsPossessed {
				get { return _ghostController.enabled; }
		}

		public bool IsHunter {
				get { return _hunterController.enabled; }
		}

		public void AddInputWithPriority (Vector3 input, ControlPriority priority)
		{
				input.y = 0f;
				switch (priority) {
				case ControlPriority.Ghost:
						if (!_catchHappened) {
							_ghostInputVector = input;
						} else {
							_ghostInputVector = Vector3.zero;
						}
						break;
				case ControlPriority.Hunter:
						_hunterInputVector = input;
						break;
				default:
						_standardInputVector = input;
						break;
				}
		}

		void Awake ()
		{
				_rigidbody = GetComponent<Rigidbody> ();
				_ghostController = GetComponent<GhostController> ();
				_hunterController = GetComponent<HunterController> ();
				_currRoomFinder = GetComponent<CurrRoomFinder> ();
				_transform = transform;
		}

		void OnEnable ()
		{
				Events.g.AddListener<PauseGameEvent> (PauseMovement);
				Events.g.AddListener<ResumeGameEvent> (ResumeMovement);
				Events.g.AddListener<HauntEvent> (RespondToHaunt);
				Events.g.AddListener<PossessionEvent> (RespondToPossession);

				Events.g.AddListener<CatchEvent>(MarkCatchHappened);

		}

		void OnDisable ()
		{
				Events.g.RemoveListener<PauseGameEvent> (PauseMovement);
				Events.g.RemoveListener<ResumeGameEvent> (ResumeMovement);
				Events.g.RemoveListener<HauntEvent> (RespondToHaunt);
				Events.g.RemoveListener<PossessionEvent> (RespondToPossession);

				Events.g.RemoveListener<CatchEvent>(MarkCatchHappened);
		}

		private void MarkCatchHappened (CatchEvent e) {
			if (e.successful) {
				_catchHappened = true;
			}
		}

		private void RespondToHaunt (HauntEvent e)
		{
				if (!e.succeeded) {
						return;
				}
				if (e.IsStart) {
						StartCoroutine (GetScaredByHaunt (e.room == _currRoomFinder.Room));
				} else {
						StartCoroutine (RecoverFromHaunt (e.room == _currRoomFinder.Room));
				}
		}

		private void RespondToPossession (PossessionEvent e)
		{
				if (e.succeeded) {
						StartCoroutine (GetConfusedByPossession (e.room == _currRoomFinder.Room));
				}
		}

		private IEnumerator GetScaredByHaunt (bool inSameRoom)
		{
				yield return new WaitForSeconds (_reactionTime);
				_animator.SetBool (AnimationConstants.IsScared, true);
		}

		private IEnumerator RecoverFromHaunt (bool inSameRoom)
		{
				yield return new WaitForSeconds (_reactionTime);
				_animator.SetBool (AnimationConstants.IsScared, false);
		}

		private IEnumerator GetConfusedByPossession (bool inSameRoom)
		{
				yield return new WaitForSeconds (_reactionTime);
				_animator.SetBool (AnimationConstants.IsConfused, true);
				yield return StartCoroutine (RecoverFromPossession (inSameRoom));
		}

		private IEnumerator RecoverFromPossession (bool inSameRoom)
		{
				yield return new WaitForSeconds (_confusionDuration);
				_animator.SetBool (AnimationConstants.IsConfused, false);
		}

		private void PauseMovement (PauseGameEvent e)
		{
				_paused = true;
		}

		private void ResumeMovement (ResumeGameEvent e)
		{
				_paused = false;
		}

		void Update ()
		{
				if (_paused)
						return;
				// Rotate towards velocity vector
				if (_rigidbody.velocity.sqrMagnitude > _rotationSensitivity) {
						_transform.forward = Vector3.RotateTowards (_transform.forward, _rigidbody.velocity, Time.deltaTime * (_rotationSpeed * TimeScale), 0.0001f);
				}
		}

		void FixedUpdate ()
		{
				if (_paused)
						return;
				Vector3 inputVector = _standardInputVector;
				if (IsHunter && (!IsPossessed || _ghostInputVector.sqrMagnitude < _ghostMovementSensitivity)) {
						inputVector = _hunterInputVector;
				} else if (IsPossessed) {
						inputVector = _ghostInputVector;
				}
				var relativeVelocity = inputVector.normalized * (_movementSpeed * TimeScale);

				// Calcualte the delta velocity
				var currRelativeVelocity = GetComponent<Rigidbody> ().velocity;
				var velocityChange = relativeVelocity - currRelativeVelocity;

				if (_animator != null) {
						AnimatorStateInfo currentState = _animator.GetCurrentAnimatorStateInfo (0);
						if (currentState.fullPathHash == AnimationConstants.Walking &&
								!_animator.GetBool (AnimationConstants.IsScared) &&
								!_animator.GetBool (AnimationConstants.IsConfused)) {
								_animator.speed = _rigidbody.velocity.magnitude * _animationScale;
						} else {
								_animator.speed = 1f; // So that animation speed is not affected
						}
				}
				_rigidbody.AddForce (velocityChange, ForceMode.VelocityChange);

		}

		private float TimeScale {
				get {
						if (IsHunter) {
								return TimeManager.g.HunterTimeScale;
						} else {
								return TimeManager.g.TimeScale;
						}
				}
		}
}