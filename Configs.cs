using System.ComponentModel;
using System.Runtime.Serialization;
using Terraria;
using Terraria.ModLoader.Config;

namespace MiniMods
{
    [Label("Config")]
    public class ClientConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        private const float MIN_SCALE = 1, MAX_SCALE = 10;

        [Range(MIN_SCALE, MAX_SCALE)]
        [DefaultValue(MIN_SCALE)]
        public float CursorScale;

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            CursorScale = Utils.Clamp(CursorScale, MIN_SCALE, MAX_SCALE);
        }
    }

    public class ServerConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        private const int MIN_EX_ACC_SLOT = 0, MAX_EX_ACC_SLOT = 10;

        [Range(MIN_EX_ACC_SLOT, MAX_EX_ACC_SLOT)]
        [DefaultValue(MIN_EX_ACC_SLOT)]
        [ReloadRequired]
        public int ExtraAccSlotAmount;

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            ExtraAccSlotAmount = Utils.Clamp(ExtraAccSlotAmount, MIN_EX_ACC_SLOT, MAX_EX_ACC_SLOT);
        }
    }
}