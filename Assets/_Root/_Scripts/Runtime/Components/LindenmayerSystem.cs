using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace VoxBotanica.Systems
{
/// <summary>
///     Represents a Lindenmayer System (L-system) used for procedural string generation.
///     <para>
///         <para>
///             An L-system consists of:
///             <list type="bullet">
///                 <item>
///                     <description>An axiom (initial string).</description>
///                 </item>
///                 <item>
///                     <description>A set of rewriting rules.</description>
///                 </item>
///                 <item>
///                     <description>A number of iterations.</description>
///                 </item>
///             </list>
///         </para>
///     </para>
///     The system generates a final sentential form by repeatedly applying rules to each symbol
///     in the string. If a symbol has no matching rule, it is carried over unchanged.
/// </summary>
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

	/// <summary>
	///     Clears the current generated sentential form.
	/// </summary>
	public void Clear()
	{
		SententialForm = string.Empty;
	}

	/// <summary>
	///     Generates the sentential form by applying rewriting rules over multiple iterations.
	///     Starts from the axiom and expands it Iterations times using the Rules dictionary.
	///     The final result is stored in SententialForm and logged to the console.
	/// </summary>
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
