using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using TerraUI;
using TerraUI.Objects;

namespace MiniMods
{
    internal struct AccessorySlot
    {
        public UIItemSlot equip, vanity, dye;

        public void Clone(AccessorySlot source)
        {
            equip.Item = source.equip.Item.Clone();
            vanity.Item = source.equip.Item.Clone();
            dye.Item = source.equip.Item.Clone();
        }

        public void Initialize(DrawHandler drawBG, DrawHandler drawDyeBG)
        {
            equip = new UIItemSlot(
                position: Vector2.Zero,
                context: ItemSlot.Context.EquipAccessory,
                conditions: item => item.accessory,
                drawBackground: drawBG,
                scaleToInventory: true
            );

            vanity = new UIItemSlot(
                position: Vector2.Zero,
                context: ItemSlot.Context.EquipAccessoryVanity,
                conditions: item => item.accessory,
                drawBackground: drawBG,
                scaleToInventory: true
            );

            dye = new UIItemSlot(
                position: Vector2.Zero,
                context: ItemSlot.Context.EquipDye,
                conditions: item => item.dye > 0 && item.hairDye < 0,
                drawBackground: drawDyeBG,
                scaleToInventory: true
            );

            vanity.Partner = equip;
            equip.BackOpacity = vanity.BackOpacity = dye.BackOpacity = .8f;

            equip.Item = new Item();
            equip.Item.SetDefaults(0, true);

            vanity.Item = new Item();
            vanity.Item.SetDefaults(0, true);
            
            dye.Item = new Item();
            dye.Item.SetDefaults(0, true);
        }

        public void CheckItemSame(AccessorySlot target, Action<PacketMessageType, Item> whenNotSame = null)
        {
            if (equip.Item.IsNotTheSameAs(target.equip.Item))
            {
                whenNotSame?.Invoke(PacketMessageType.EquipSlot, equip.Item);
            }

            if (vanity.Item.IsNotTheSameAs(target.vanity.Item))
            {
                whenNotSame?.Invoke(PacketMessageType.VanitySlot, vanity.Item);
            }

            if (dye.Item.IsNotTheSameAs(target.dye.Item))
            {
                whenNotSame?.Invoke(PacketMessageType.DyeSlot, dye.Item);
            }
        }

        public void SetPosition(int rX, int rY, int xOffset)
        {
            equip.Position = new Vector2(rX, rY);
            vanity.Position = new Vector2(rX - xOffset, rY);
            dye.Position = new Vector2(rX - xOffset * 2, rY);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            equip.Draw(spriteBatch);
            vanity.Draw(spriteBatch);
            dye.Draw(spriteBatch);
        }

        public void Update()
        {
            equip.Update();
            vanity.Update();
            dye.Update();
        }
    }

    internal class MiniModsPlayer : ModPlayer
    {
        private const string PREFIX = "minimods";
        public AccessorySlot[] slots;

        public override void clientClone(ModPlayer clientClone)
        {
            var clone = clientClone as MiniModsPlayer;

            if (clone == null) return;

            for (int i = 0, max = MiniMods.ExtraAccSlotAmount; i < max; i++)
            {
                clone.slots[i].Clone(slots[i]);
            }
        }

        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            var clone = clientPlayer as MiniModsPlayer;

            if (clone == null) return;

            for (int i = 0, max = MiniMods.ExtraAccSlotAmount; i < max; i++)
            {
                clone.slots[i].CheckItemSame(slots[i], (msg, item) => SendItemPacket(msg, item, -1, player.whoAmI));
            }
        }

        internal void SendItemPacket(PacketMessageType type, Item item, int toWho, int fromWho)
        {
            var packet = mod.GetPacket();
            packet.Write((byte)type);
            packet.Write((byte)player.whoAmI);
            ItemIO.Send(item, packet);
            packet.Send(toWho, fromWho);
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            var packet = mod.GetPacket();
            packet.Write((byte)PacketMessageType.All);
            packet.Write((byte)player.whoAmI);

            for (int i = 0, max = MiniMods.ExtraAccSlotAmount; i < max; i++)
            {
                ItemIO.Send(slots[i].equip.Item, packet);
                ItemIO.Send(slots[i].vanity.Item, packet);
                ItemIO.Send(slots[i].dye.Item, packet);
            }

            packet.Send(toWho, fromWho);
        }

        public override void Initialize()
        {
            slots = new AccessorySlot[MiniMods.ExtraAccSlotAmount];

            for (int i = 0, max = MiniMods.ExtraAccSlotAmount; i < max; i++)
            {
                slots[i].Initialize(DrawSlotBG, DrawSlotDyeBG);
            }
        }

        private static bool ShouldDrawSlots()
            => Main.playerInventory && Main.EquipPage == 0;

        private void DrawSlotBG(UIObject sender, SpriteBatch spriteBatch)
        {
            UIItemSlot slot = (UIItemSlot)sender;

            if (ShouldDrawSlots())
            {
                slot.OnDrawBackground(spriteBatch);
            }
        }

        private void DrawSlotDyeBG(UIObject sender, SpriteBatch spriteBatch)
        {
            UIItemSlot slot = (UIItemSlot)sender;

            if (ShouldDrawSlots())
            {
                slot.OnDrawBackground(spriteBatch);

                if (slot.Item.stack != 0)
                {
                    return;
                }

                Texture2D tex = Main.extraTexture[54];
                Rectangle rectangle = tex.Frame(3, 6, 1 % 3);
                rectangle.Width -= 2;
                rectangle.Height -= 2;
                Vector2 origin = rectangle.Size() / 2f * Main.inventoryScale;
                Vector2 position = slot.Rectangle.TopLeft();

                spriteBatch.Draw(
                    tex,
                    position + (slot.Rectangle.Size() / 2f) - (origin / 2f),
                    rectangle,
                    Color.White * 0.35f,
                    0f,
                    origin,
                    Main.inventoryScale,
                    SpriteEffects.None,
                    0f); // layer depth 0 = front
            }
        }

        public override void UpdateEquips(ref bool wallSpeedBuff, ref bool tileSpeedBuff, ref bool tileRangeBuff)
        {
            for (int i = 0, max = MiniMods.ExtraAccSlotAmount; i < max; i++)
            {
                var slot = slots[i];
                var item = slot.equip.Item;
                var vanityItem = slot.vanity.Item;

                if (item.stack > 0)
                {
                    player.VanillaUpdateAccessory(player.whoAmI, item, false, ref wallSpeedBuff, ref tileSpeedBuff, ref tileRangeBuff);
                    player.VanillaUpdateEquip(item);
                }

                if (vanityItem.stack > 0)
                {
                    player.VanillaUpdateVanityAccessory(vanityItem);
                }
            }
        }

        public override TagCompound Save()
        {
            var compound = new TagCompound();

            for (int i = 0, max = MiniMods.ExtraAccSlotAmount; i < max; i++)
            {
                var slot = slots[i];
                compound.Add($"{PREFIX}{i + 1}_VISIBLE", slot.equip.ItemVisible);
                compound.Add($"{PREFIX}{i + 1}_EQUIP", ItemIO.Save(slot.equip.Item));
                compound.Add($"{PREFIX}{i + 1}_VANITY", ItemIO.Save(slot.vanity.Item));
                compound.Add($"{PREFIX}{i + 1}_DYE", ItemIO.Save(slot.dye.Item));
            }

            return compound;
        }

        public override void Load(TagCompound tag)
        {
            for (int i = 0, max = MiniMods.ExtraAccSlotAmount; i < max; i++)
            {
                var equip = ItemIO.Load(tag.GetCompound($"{PREFIX}{i + 1}_EQUIP"));
                var vanity = ItemIO.Load(tag.GetCompound($"{PREFIX}{i + 1}_VANITY"));
                var dye = ItemIO.Load(tag.GetCompound($"{PREFIX}{i + 1}_DYE"));

                slots[i].equip.Item = equip.Clone();
                slots[i].vanity.Item = vanity.Clone();
                slots[i].dye.Item = dye.Clone();
                slots[i].equip.ItemVisible = tag.GetBool($"{PREFIX}{i + 1}_VISIBLE");
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!ShouldDrawSlots()) return;

            int mapH = 0;
            int rX;
            int rY;
            int magic = 47;
            float origScale = Main.inventoryScale;

            Main.inventoryScale = 0.85f;

            if (Main.mapEnabled)
            {
                if (!Main.mapFullscreen && Main.mapStyle == 1)
                {
                    mapH = 256;
                }
            }

            if (Main.mapEnabled)
            {
                int adjustY = 600;


                if (Main.player[Main.myPlayer].ExtraAccessorySlotsShouldShow)
                {
                    adjustY = 610 + PlayerInput.UsingGamepad.ToInt() * 30;
                }

                if ((mapH + adjustY) > Main.screenHeight)
                {
                    mapH = Main.screenHeight - adjustY;
                }
            }

            int rowPerCol = 6;
            for (int i = 0; i < slots.Length; i++)
            {
                int row = i % rowPerCol;
                int col = i / rowPerCol;

                rX = Main.screenWidth - 92 - 14 - (magic * 3 * (col + 1)) - (int)(Main.extraTexture[58].Width * Main.inventoryScale);
                rY = (int)(mapH + 174 + 4 + row * 56 * Main.inventoryScale);

                slots[i].SetPosition(rX, rY, magic);
            }

            for (int i = 0, max = MiniMods.ExtraAccSlotAmount; i < max; i++)
            {
                slots[i].Draw(spriteBatch);
            }

            Main.inventoryScale = origScale;

            for (int i = 0, max = MiniMods.ExtraAccSlotAmount; i < max; i++)
            {
                slots[i].Update();
            }
        }
    }
}
