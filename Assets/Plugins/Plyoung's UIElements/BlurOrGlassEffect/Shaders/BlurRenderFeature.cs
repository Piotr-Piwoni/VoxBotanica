using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlurRenderFeature : ScriptableRendererFeature
{
	public BlurSettings settings = new();

	private CustomRenderPass scriptablePass;

	public override void AddRenderPasses(ScriptableRenderer renderer,
			ref RenderingData renderingData)
	{
		RTHandle src = renderer.cameraColorTargetHandle;
		scriptablePass.Setup(src);
		renderer.EnqueuePass(scriptablePass);
	}

	public override void Create()
	{
		scriptablePass = new CustomRenderPass("BlurPass")
		{
				blurMaterial = settings.blurMaterial,
				passes = settings.blurPasses,
				downsample = settings.downsample,
				copyToFramebuffer = settings.copyToFramebuffer,
				targetName = settings.targetName,
				renderPassEvent = settings.renderPassEvent,
		};
	}

	[Serializable]
	public class BlurSettings
	{
		public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRendering;
		public Material blurMaterial;

		[Range(2, 15)]
		public int blurPasses = 6;

		[Range(1, 4)]
		public int downsample = 2;
		public bool copyToFramebuffer;
		public string targetName = "_blurTexture";
	}

	private class CustomRenderPass : ScriptableRenderPass
	{
		private RTHandle source { get; set; }
		private readonly string profilerTag;
		public Material blurMaterial;
		public bool copyToFramebuffer;
		public int downsample;
		public int passes;
		public string targetName;

		private int tmpId1;
		private int tmpId2;

		private RTHandle tmpRT1;
		private RTHandle tmpRT2;

		public CustomRenderPass(string profilerTag)
		{
			this.profilerTag = profilerTag;
		}

		public void Setup(RTHandle source)
		{
			this.source = source;
		}

		public override void Configure(CommandBuffer cmd,
				RenderTextureDescriptor cameraTextureDescriptor)
		{
			RenderTextureDescriptor desc = cameraTextureDescriptor;
			desc.depthBufferBits = 0;
			desc.width /= downsample;
			desc.height /= downsample;

			tmpRT1 = RTHandles.Alloc(desc, name: "tmpBlurRT1");
			tmpRT2 = RTHandles.Alloc(desc, name: "tmpBlurRT2");

			// KEEPING your double call exactly as requested
			ConfigureTarget(tmpRT1);
			ConfigureTarget(tmpRT2);
		}

		public override void Execute(ScriptableRenderContext context,
				ref RenderingData renderingData)
		{
			CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

			RenderTextureDescriptor opaqueDesc =
					renderingData.cameraData.cameraTargetDescriptor;
			opaqueDesc.depthBufferBits = 0;

			// first pass
			cmd.SetGlobalFloat("_offset", 1.5f);
			cmd.Blit(source, tmpRT1, blurMaterial);

			for (var i = 1; i < passes - 1; i++)
			{
				cmd.SetGlobalFloat("_offset", 0.5f + i);
				cmd.Blit(tmpRT1, tmpRT2, blurMaterial);

				// pingpong
				RTHandle rttmp = tmpRT1;
				tmpRT1 = tmpRT2;
				tmpRT2 = rttmp;
			}

			// final pass
			cmd.SetGlobalFloat("_offset", 0.5f + passes - 1f);
			if (copyToFramebuffer)
				cmd.Blit(tmpRT1, source, blurMaterial);
			else
			{
				cmd.Blit(tmpRT1, tmpRT2, blurMaterial);
				cmd.SetGlobalTexture(targetName, tmpRT2);
			}

			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();

			CommandBufferPool.Release(cmd);
		}

		public override void FrameCleanup(CommandBuffer cmd) { }
	}
}
