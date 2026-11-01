using System;

public class NPCEvents
{
    public event Action<string, string> onNpcGifted;
    public void NpcGifted(string npcId, string itemId)
    {
        if (onNpcGifted != null)
        {
            onNpcGifted(npcId, itemId);
        }
    }
}
