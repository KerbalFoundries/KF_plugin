using System;
using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>KF-friendly animation module.</summary>
	public class KFAnimation : PartModule
	{
		// Persistent fields.
		/// <summary>Persistent bool to track requested start state of the animation.</summary>
		[KSPField(isPersistant = true)]
		public bool startDeployed;
		/// <summary>Persistent bool to track the current state of the animation.</summary>
		[KSPField(isPersistant = true)]
		public bool isDeployed;
		
		// Config fields.
		/// <summary>Animation name in the part model.</summary>
		[KSPField]
		public string animationName;
		/// <summary>Speed in which to run the specified animation in the forward direction.</summary>
		/// <remarks>If unset, will revert to default of "1f" on startup.</remarks>
		[KSPField]
		public float speedForward;
		/// <summary>Speed in which to run the specified animation in the backward direction.</summary>
		/// <remarks>If unset, will revert to default of "1f" on startup.</remarks>
		[KSPField]
		public float speedBackward;
		
		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("KFAnimation");
		
		/// <summary>The animation reference.</summary>
		public Animation Anim
		{
			get
			{
				return part.FindModelAnimators(animationName)[0];
			}
		}
		
		public override void OnStart(PartModule.StartState state)
		{
			try
			{
				Anim[animationName].layer = 2;
				SetInitState();
				base.OnStart(state);
				
				if (Equals(speedForward, 0f))
					speedForward = 1f;
				if (Equals(speedBackward, 0f))
					speedBackward = 1f;
			}
			catch (Exception ex)
			{
				KFLog.Error(string.Format("Error in OnStart.  \"{0}\"", ex.Message));
			}
		}
		
		public override void OnLoad(ConfigNode node)
		{
			try
			{
				Anim[animationName].layer = 2;
				SetLoadState();
			}
			catch (Exception ex)
			{
				KFLog.Error(string.Format("Error in OnLoad.  \"{0}\"", ex.Message));
			}
		}
		
		/// <summary>Sets the state of the animation on load.</summary>
		void SetLoadState()
		{
			if (isDeployed)
				QuickDeploy();
			else // Implied: "isDeployed" = false
				QuickRetract();
		}
		
		/// <summary>Sets the initial state of the animation.</summary>
		void SetInitState()
		{
			if (startDeployed)
			{
				if (!isDeployed)
					QuickDeploy();
			}
			else // Implied: "startDeployed" = false
			{
				if (isDeployed)
					QuickRetract();
			}
		}
		
		/// <summary>Executes a near-instant deploy of the animation.</summary>
		void QuickDeploy()
		{
			SetEventState("Deploy", false);
			SetEventState("Retract", true);
			PlayForwardAnimation(1000f);
			if (!isDeployed)
				isDeployed = true;
		}
		
		/// <summary>Executes a near-instant retract of the animation.</summary>
		void QuickRetract()
		{
			SetEventState("Deploy", true);
			SetEventState("Retract", false);
			PlayReverseAnimation(1000f);
			if (isDeployed)
				isDeployed = false;
		}
		
		/// <summary>Sets the state of the event "active" fields.</summary>
		/// <param name="eventName">The name of the event to modify.</param>
		/// <param name="state">The state to set.</param>
		void SetEventState(string eventName, bool state)
		{
			Events[eventName].active = state;
			Events[eventName].guiActive = state;
			Events[eventName].guiActiveEditor = state;
		}
		
		/// <summary>Plays the animation in a forward direction.</summary>
		/// <param name="animspeed">The speed to run the animation at. (Default: 1f)</param>
		void PlayForwardAnimation(float animspeed = 1f)
		{
			Anim[animationName].speed = animspeed;
			Anim.Play(animationName);
		}
		
		/// <summary>Plays the animation in a reverse direction.</summary>
		/// <param name="animspeed">The speed to run the animation at. (Default: 1f)</param>
		void PlayReverseAnimation(float animspeed = 1f)
		{
			Anim[animationName].time = Anim[animationName].length;
			Anim[animationName].speed = animspeed * -1f; // Even if the animation in the model is set up reversed, multiplying by -1 will still reverse the speed appropriately.
			Anim.Play(animationName);
		}
		
		/// <summary>Action-group "Deploy"</summary>
		/// <param name="param">Unused.</param>
		[KSPAction("Deploy")]
		public void DeployAction(KSPActionParam param)
		{
			DeployEvent();
		}

		/// <summary>Action-group "Retract"</summary>
		/// <param name="param">Unused.</param>
		[KSPAction("Retract")]
		public void RetractAction(KSPActionParam param)
		{
			RetractEvent();
		}

		/// <summary>Action-group "Toggle Deployment"</summary>
		/// <param name="param">Unused.</param>
		[KSPAction("Toggle Deployment")]
		public void ToggleAction(KSPActionParam param)
		{
			if (isDeployed)
			{
				RetractEvent();
				return;
			}
			DeployEvent();
		}

		/// <summary>Deployment event.</summary>
		[KSPEvent(guiName = "Deploy", guiActive = true, guiActiveEditor = true, active = true)]
		public void DeployEvent()
		{
			if (!isDeployed)
			{
				PlayForwardAnimation(speedForward);
				SetEventState("Deploy", false);
				SetEventState("Retract", true);
				isDeployed = true;
			}
		}

		/// <summary>Retraction event.</summary>
		[KSPEvent(guiName = "Retract", guiActive = false, guiActiveEditor = false, active = true)]
		public void RetractEvent()
		{
			if (isDeployed)
			{
				PlayReverseAnimation(speedBackward);
				SetEventState("DeployModule", true);
				SetEventState("RetractModule", false);
				isDeployed = false;
			}
		}
	}
}