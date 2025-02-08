using Hazel;

namespace INH;

public class Interactable : Entity
{
	protected override void OnCreate()
	{
	}

	protected override void OnDestroy()
	{
	}

	protected override void OnUpdate(float ts)
	{
	}

	public void Activate()
	{
		Log.Debug("Activated interactable!");
	}
}
