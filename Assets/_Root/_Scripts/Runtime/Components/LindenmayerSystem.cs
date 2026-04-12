using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace VoxBotanica.Systems
{
[Serializable]
public class LindenmayerSystem
{
	[OdinSerialize]
	public Dictionary<string, string> Rules { get; private set; } = new()
	{
			{ "F", "F[+F]F[-F]F" },
	};
	public string SententialForm { get; private set; } = string.Empty;
	public string Axiom = "F";
	[MinValue(1)]
	public int Iterations = 3;


	public void Clear()
	{
		SententialForm = string.Empty;
	}

	public void Generate()
	{
		string result = Axiom;
		for (var i = 0; i < Iterations; i++)
		{
			var builder = new StringBuilder();

			foreach (string key in result.Select(command => command.ToString()))
				builder.Append(Rules.GetValueOrDefault(key, key));

			result = builder.ToString();
		}

		SententialForm = result;
		Debug.Log($"Sentential Form: <color=yellow>{SententialForm}</color>");
	}
}
}
