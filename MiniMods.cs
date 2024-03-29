using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace MiniMods
{
    public class MiniMods : Mod
    {
        public static int ExtraAccSlotAmount = 0;

        public override void Load()
        {
            Properties = new ModProperties()
            {
                Autoload = true,
                AutoloadBackgrounds = true,
                AutoloadSounds = true
            };

            ExtraAccSlotAmount = ModContent.GetInstance<ServerConfig>().ExtraAccSlotAmount;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            var cursorIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Cursor"));
            if (cursorIndex != -1)
            {
                layers.Insert(cursorIndex, new LegacyGameInterfaceLayer(
                    "MiniMods: Custom Cursor",
                    () =>
                    {
                        Main.cursorScale *= ModContent.GetInstance<ClientConfig>().CursorScale;
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            var player = Main.player[Main.myPlayer].GetModPlayer<MiniModsPlayer>();
            player.Draw(spriteBatch);
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            var msg = (PacketMessageType)reader.ReadByte();
            var player = reader.ReadByte();
            var modPlayer = Main.player[player].GetModPlayer<MiniModsPlayer>();

            switch (msg)
            {
                case PacketMessageType.All:
                    for (int i = 0; i < ExtraAccSlotAmount; i++)
                    {
                        var slot = modPlayer.slots[i];
                        slot.equip.Item = ItemIO.Receive(reader);
                        slot.vanity.Item = ItemIO.Receive(reader);
                        slot.dye.Item = ItemIO.Receive(reader);
                    }

                    if (Main.netMode == NetmodeID.Server)
                    {
                        var packet = GetPacket();
                        packet.Write((byte)PacketMessageType.All);
                        packet.Write(player);

                        for (int i = 0; i < ExtraAccSlotAmount; i++)
                        {
                            var slot = modPlayer.slots[i];

                            ItemIO.Send(slot.equip.Item, packet);
                            ItemIO.Send(slot.vanity.Item, packet);
                            ItemIO.Send(slot.dye.Item, packet);
                        }

                        packet.Send(-1, whoAmI);
                    }
                    break;

                case PacketMessageType.EquipSlot:
                    for (int i = 0; i < ExtraAccSlotAmount; i++)
                    {
                        modPlayer.slots[i].equip.Item = ItemIO.Receive(reader);
                    }

                    if (Main.netMode == NetmodeID.Server)
                    {
                        modPlayer.SendItemPacket(PacketMessageType.EquipSlot, modPlayer.ExtractEquip, -1, whoAmI);
                    }
                    break;

                case PacketMessageType.VanitySlot:
                    for (int i = 0; i < ExtraAccSlotAmount; i++)
                    {
                        modPlayer.slots[i].vanity.Item = ItemIO.Receive(reader);
                    }

                    if (Main.netMode == NetmodeID.Server)
                    {
                        modPlayer.SendItemPacket(PacketMessageType.VanitySlot, modPlayer.ExtractVanity, -1, whoAmI);
                    }
                    break;

                case PacketMessageType.DyeSlot:
                    for (int i = 0; i < ExtraAccSlotAmount; i++)
                    {
                        modPlayer.slots[i].dye.Item = ItemIO.Receive(reader);
                    }

                    if (Main.netMode == NetmodeID.Server)
                    {
                        modPlayer.SendItemPacket(PacketMessageType.DyeSlot, modPlayer.ExtractDye, -1, whoAmI);
                    }
                    break;

                default:
                    Logger.Error($"[MiniMods - Extra Acc Slots] Unknown packet type! : {msg}");
                    break;
            }
        }
    }
}