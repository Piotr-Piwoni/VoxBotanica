using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using Sirenix.OdinInspector;

namespace VoxBotanica
{
public class LSystemGenerator : MonoBehaviour
{
	[Title("L-System Settings")]
	public string Axiom = "F";
	[MinValue(1)]
	public int Iterations = 4;
	[Range(0f, 180f)]
	public float Angle = 25f;

	[SerializeField]
	private SerializedDictionary<string, string> _Rules = new()
	{
		{ "F", "F[+F]F[-F]F" }
	};

	private string _CurrentString;


	[Button]
	private void Generate()
	{
		_CurrentString = GenerateLSystem();
		Debug.Log(_CurrentString);
	}

	private void OnDrawGizmos()
	{
		if (string.IsNullOrEmpty(_CurrentString))
			return;

		var stack = new Stack<TurtleState>();
		Vector3 position = Vector3.zero;
		Vector3 direction = Vector3.up;
		const float LENGTH = 1f;

		Gizmos.color = Color.green;

		foreach (char c in _CurrentString)
			switch (c)
			{
			case 'F':
			{
				Vector3 newPos = position + direction * LENGTH;
				Gizmos.DrawLine(position, newPos);
				position = newPos;
				break;
			}
			case '+':
			{
				direction = Quaternion.Euler(0f, 0f, Angle) * direction;
				break;
			}
			case '-':
			{
				direction = Quaternion.Euler(0f, 0f, -Angle) * direction;
				break;
			}
			case '[':
			{
				stack.Push(new TurtleState(position, direction));
				break;
			}
			case ']':
			{
				if (stack.Count <= 0)
					break;

				TurtleState state = stack.Pop();
				position = state.Position;
				direction = state.Direction;
				break;
			}
			}
	}

	private struct TurtleState
	{
		public TurtleState(Vector3 pos, Vector3 dir)
		{
			Position = pos;
			Direction = dir;
		}

		public Vector3 Position;
		public Vector3 Direction;
	}

	private string GenerateLSystem()
	{
		string result = Axiom;

		for (var i = 0; i < Iterations; i++)
		{
			string newString = result.Aggregate(string.Empty, (current, c) =>
				current + (_Rules.ContainsKey(c.ToString())
					? _Rules[c.ToString()]
					: c.ToString()));

			result = newString;
		}

		return result;
	}
}
}