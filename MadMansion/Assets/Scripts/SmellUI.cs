﻿using UnityEngine;
using System.Collections;

public enum SmellState {
	WaitForSmell,
	CanSmell,
	IsSmelling
}

public class SmellUI : MonoBehaviour {

	[SerializeField]
	private Sprite _waitForSmellImage;
	[SerializeField]
	private Sprite _canSmellImage;
	[SerializeField]
	private Sprite _smellingImage;
	[SerializeField]
	private Renderer _radialTimerRenderer;

	[SerializeField]
	private Color _chargingColor;
	[SerializeField]
	private Color _isSmellingColor;

	private SmellState _possessionState;
	public SmellState SmellState {
		set {
			_possessionState = value;
			switch (_possessionState) {
				case SmellState.CanSmell:
					_spriteRenderer.sprite = _canSmellImage;
					break;
				case SmellState.IsSmelling:
					_spriteRenderer.sprite = _smellingImage;
					break;
				default:
					_spriteRenderer.sprite = _waitForSmellImage;
					break;
			}
		}
	}

	private SpriteRenderer _spriteRenderer;

	void Awake () {
		_spriteRenderer = GetComponent<SpriteRenderer>();
	}

	void Start () {
		SmellState = SmellState.WaitForSmell;
		_radialTimerRenderer.material.SetFloat ("_Cutoff", 0f);
	}

	void Update () {
		UpdateSmellTimer();
	}

	public float PercentageFilled {
		set { _radialTimerRenderer.material.SetFloat ("_Cutoff", 1f-value); }
	}

	public Color Color {
		set { _radialTimerRenderer.material.SetColor ("_Color", value); }
	}

	private void UpdateSmellTimer () {
		if (SmellManager.g.IsSmelling) {
			SmellState = SmellState.IsSmelling;
			PercentageFilled = 1f - SmellManager.g.SmellTimerPercentage;
			Color = _isSmellingColor;
		} else if (SmellManager.g.CanSmell) {
			PercentageFilled = 1f;
			SmellState = SmellState.CanSmell;
		} else {
			Color = _chargingColor;
			PercentageFilled = SmellManager.g.SmellChargePercentage;
			SmellState = SmellState.WaitForSmell;
		}
	}
}
